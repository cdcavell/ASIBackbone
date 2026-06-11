using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Evaluation;
using CDCavell.AsiBackbone.EntityFrameworkCore;
using CDCavell.AsiBackbone.EntityFrameworkCore.Audit;
using CDCavell.AsiBackbone.Storage.InMemory.Audit;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAsiBackboneAspNetCore();

builder.Services.AddDbContext<PlainHostAsiBackboneDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("AsiBackbone") ?? "Data Source=asi-backbone-sample.db");
});

builder.Services.AddScoped<DbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<PlainHostAsiBackboneDbContext>());
builder.Services.AddScoped<IAsiBackboneAuditLedgerStore, EfCoreAuditLedgerStore>();

builder.Services.AddSingleton<InMemoryAuditLedger>();
builder.Services.AddSingleton<IAsiBackboneAuditSink>(serviceProvider =>
    serviceProvider.GetRequiredService<InMemoryAuditLedger>());

builder.Services.AddSingleton<IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>, RegionConstraint>();
builder.Services.AddSingleton<IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>, ConsequentialActionDecisionPolicy>();
builder.Services.AddSingleton<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>, DefaultAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>();

WebApplication app = builder.Build();

app.MapGet("/", () => Results.Redirect("/sample/decision"));

app.MapGet("/sample/decision", async (
    HttpContext httpContext,
    IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext> evaluator,
    IAsiBackboneAuditSink auditSink,
    IAsiBackboneAuditLedgerStore ledgerStore,
    CancellationToken cancellationToken) =>
{
    string correlationId = httpContext.TraceIdentifier;

    var context = new AsiBackboneConstraintEvaluationContext(
        correlationId: correlationId,
        policyVersion: "sample-policy-v1",
        policyHash: "sample-policy-hash",
        metadata: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["region"] = "US-LA",
            ["risk"] = "consequential",
            ["intent"] = "external-api-call"
        });

    GovernanceDecision decision = await evaluator
        .EvaluateAsync(context, cancellationToken)
        .ConfigureAwait(false);

    AsiBackboneActorContext actor = AsiBackboneActorContext.Human(
        actorId: "sample-user",
        displayName: "Sample User");

    AuditResidue residue = AuditResidue.FromDecision(
        actor,
        "sample.external-api-call",
        decision,
        metadata: context.Metadata);

    await auditSink.WriteAsync(residue, cancellationToken).ConfigureAwait(false);

    AuditLedgerRecord record = AuditLedgerRecord.FromResidue(residue);
    _ = await ledgerStore.AppendAsync(record, cancellationToken).ConfigureAwait(false);

    return Results.Ok(new
    {
        decision = decision.Outcome.ToString(),
        decision.CanProceed,
        decision.RequiresAcknowledgment,
        decision.ReasonCodes,
        decision.CorrelationId,
        decision.PolicyVersion,
        decision.PolicyHash,
        auditEventId = residue.EventId,
        ledgerRecordId = record.RecordId
    });
});

app.MapGet("/sample/audit/{correlationId}", (
    string correlationId,
    InMemoryAuditLedger auditLedger) =>
{
    return Results.Ok(auditLedger.GetByCorrelationId(correlationId));
});

app.MapGet("/sample/ledger/{correlationId}", async (
    string correlationId,
    IAsiBackboneAuditLedgerStore ledgerStore,
    CancellationToken cancellationToken) =>
{
    IReadOnlyList<AuditLedgerRecord> records = await ledgerStore
        .FindByCorrelationIdAsync(correlationId, cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(records);
});

app.Run();

internal sealed class PlainHostAsiBackboneDbContext(DbContextOptions<PlainHostAsiBackboneDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyAsiBackboneConfigurations();
    }
}

internal sealed class RegionConstraint : IAsiBackboneConstraint<AsiBackboneConstraintEvaluationContext>
{
    public string Name => "sample.region";

    public ValueTask<ConstraintEvaluationResult> EvaluateAsync(
        AsiBackboneConstraintEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool hasRegion = context.Metadata.TryGetValue("region", out string? region)
            && !string.IsNullOrWhiteSpace(region);

        return ValueTask.FromResult(hasRegion
            ? ConstraintEvaluationResult.Allow()
            : ConstraintEvaluationResult.Deny(
                "sample.region.missing",
                "A region is required before this host allows the operation to continue."));
    }
}

internal sealed class ConsequentialActionDecisionPolicy : IAsiBackboneDecisionPolicy<AsiBackboneConstraintEvaluationContext>
{
    public ValueTask<GovernanceDecision> ApplyAsync(
        AsiBackboneConstraintEvaluationContext context,
        GovernanceDecision composedDecision,
        IReadOnlyList<ConstraintEvaluationResult> constraintResults,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!composedDecision.CanProceed)
        {
            return ValueTask.FromResult(composedDecision);
        }

        bool isConsequential = context.Metadata.TryGetValue("risk", out string? risk)
            && string.Equals(risk, "consequential", StringComparison.OrdinalIgnoreCase);

        return ValueTask.FromResult(isConsequential
            ? GovernanceDecision.RequireAcknowledgment(
                "sample.acknowledgment.required",
                "Consequential actions require host-owned acknowledgment before execution.",
                correlationId: context.CorrelationId,
                policyVersion: context.PolicyVersion,
                policyHash: context.PolicyHash)
            : composedDecision);
    }
}
