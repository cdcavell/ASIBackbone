using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Results;

public sealed class AsiBackboneHttpResultMappingExtensionsTests
{
    [Fact]
    public async Task ToHttpResultMapsAllowedDecisionToSuccessResponse()
    {
        GovernanceDecision decision = GovernanceDecision.Allow(correlationId: " correlation-123 ");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"allowed\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Allowed", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-123", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsWarningDecisionToConfiguredWarningStatusCode()
    {
        GovernanceDecision decision = GovernanceDecision.Warning("policy.warning", "Warning detail.");
        AsiBackboneHttpResultMappingOptions options = new()
        {
            WarningStatusCode = StatusCodes.Status206PartialContent,
        };

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Equal(StatusCodes.Status206PartialContent, capture.StatusCode);
        Assert.Contains("Warning", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.warning", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeniedDecisionToProblemDetails()
    {
        GovernanceDecision decision = GovernanceDecision.Deny(
            "policy.denied",
            "Sensitive policy internals.",
            correlationId: "correlation-deny",
            traceId: "trace-deny",
            policyVersion: "v1",
            policyHash: "hash-secret");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status403Forbidden, capture.StatusCode);
        Assert.Contains("Governance decision denied execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive policy internals", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-secret", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeferredDecisionToAcceptedProblemDetails()
    {
        GovernanceDecision decision = GovernanceDecision.Defer("policy.deferred", "Try again later.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status202Accepted, capture.StatusCode);
        Assert.Contains("Governance decision deferred execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.deferred", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsAcknowledgmentRequiredDecisionToPreconditionProblemDetails()
    {
        GovernanceDecision decision = GovernanceDecision.RequireAcknowledgment("ack.required", "User acknowledgment required.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status428PreconditionRequired, capture.StatusCode);
        Assert.Contains("Governance decision requires acknowledgment.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("ack.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsEscalationRecommendedDecisionToConflictProblemDetails()
    {
        GovernanceDecision decision = GovernanceDecision.Escalate("escalation.required", "Manual review required.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status409Conflict, capture.StatusCode);
        Assert.Contains("Governance decision recommends escalation.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("escalation.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultCanExposeReasonMessagesAndDiagnosticMetadataWhenConfigured()
    {
        GovernanceDecision decision = GovernanceDecision.Deny(
            "policy.denied",
            "Host-approved public detail.",
            correlationId: "correlation-public",
            traceId: "trace-public",
            policyVersion: "v2",
            policyHash: "hash-public");
        AsiBackboneHttpResultMappingOptions options = new()
        {
            IncludeReasonMessages = true,
            IncludeTraceId = true,
            IncludePolicyMetadata = true,
        };

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Contains("Host-approved public detail.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("trace-public", capture.Body, StringComparison.Ordinal);
        Assert.Contains("v2", capture.Body, StringComparison.Ordinal);
        Assert.Contains("hash-public", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsSuccessfulOperationResultToSuccessResponse()
    {
        OperationResult result = OperationResult.Success(["Completed with warning."]);

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"succeeded\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Completed with warning.", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsFailedOperationResultToProblemDetailsWithoutReasonMessagesByDefault()
    {
        OperationResult result = OperationResult.Failure("operation.denied", "Sensitive failure detail.");

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, capture.StatusCode);
        Assert.Contains("operation.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("The operation did not complete successfully.", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive failure detail", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHttpResultRejectsNullDecision()
    {
        GovernanceDecision? decision = null;

        _ = Assert.Throws<ArgumentNullException>(() => decision!.ToHttpResult());
    }

    [Fact]
    public void ResultMappingOptionsRejectInvalidStatusCode()
    {
        AsiBackboneHttpResultMappingOptions options = new()
        {
            DeniedStatusCode = 99,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpResultMappingOptions.DeniedStatusCode), exception.Message, StringComparison.Ordinal);
    }

    private static async Task<HttpResultCapture> ExecuteAsync(IResult result)
    {
        DefaultHttpContext httpContext = new();
        await using MemoryStream body = new();
        httpContext.Response.Body = body;

        await result.ExecuteAsync(httpContext);

        _ = body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(body);
        string content = await reader.ReadToEndAsync();

        return new HttpResultCapture(httpContext.Response.StatusCode, content);
    }

    private sealed record HttpResultCapture(int StatusCode, string Body);
}
