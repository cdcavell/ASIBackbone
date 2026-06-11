# CDCavell.AsiBackbone.AspNetCore

ASP.NET Core host integration scaffold for ASI Backbone governance primitives.

This package is intended to act as a thin web-host adapter around `CDCavell.AsiBackbone.Core`.

> [!IMPORTANT]
> This package provides thin host adapters only. It does not currently provide concrete middleware, endpoint mapping, Problem Details integration, authentication integration, or policy enforcement. Those features should be added through follow-up implementation issues.

## Service registration

Register the ASP.NET Core integration package from a plain ASP.NET Core host through `IServiceCollection`.

```csharp
using CDCavell.AsiBackbone.AspNetCore.DependencyInjection;

builder.Services.AddAsiBackboneAspNetCore();
```

Host applications may configure the first integration options explicitly.

```csharp
builder.Services.AddAsiBackboneAspNetCore(options =>
{
    options.IncludeRouteValues = true;
    options.IncludeEndpointMetadata = true;
    options.IncludeRequestMethod = true;
    options.IncludeRequestPath = false;
    options.CorrelationIdHeaderNames = ["X-Correlation-ID", "X-Request-ID"];
});
```

The registration is intentionally narrow. It does not register persistence, EF Core, authentication handlers, MVC, Razor Pages, Minimal API endpoints, middleware, policy evaluators, or host-specific authorization behavior.

## Request correlation and audit enrichment

`IAsiBackboneHttpRequestCorrelationResolver` resolves request correlation data from the current `HttpContext` without making Core depend on ASP.NET Core types.

The default resolver:

- checks configured correlation headers such as `X-Correlation-ID` and `X-Request-ID`;
- falls back to `HttpContext.TraceIdentifier` when no configured header is present;
- captures a trace identifier from `Activity.Current` or the ASP.NET Core trace identifier;
- emits safe request metadata such as method, route pattern, endpoint display name, and route values;
- excludes sensitive request data such as headers, query strings, request bodies, cookies, and tokens by default.

Example usage:

```csharp
using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.Core.Audit;

AsiBackboneHttpRequestCorrelation correlation = correlationResolver.ResolveRequestCorrelation();

AuditResidue residue = correlation.CreateAuditResidue(
    actor,
    "ApproveWidget",
    decision);
```

Use `AsiBackboneHttpRequestCorrelation.ToEvaluationContext(...)` when a web host needs to carry the resolved correlation identifier and safe request metadata into a framework-neutral Core policy evaluation context.

## Current boundary

This package may eventually provide:

- ASP.NET Core service registration extensions;
- request-aware policy context building seams;
- current-user/current-actor resolution from `HttpContext`;
- optional middleware for request context preparation;
- optional endpoint mapping helpers;
- optional HTTP and Problem Details mapping helpers.

This package should avoid:

- Entity Framework Core persistence;
- database provider assumptions;
- direct dependencies on NetCoreApplicationTemplate;
- authentication-provider assumptions;
- robotics or physical execution dependencies;
- AI model hosting, training, inference, or orchestration.

See `docs/articles/aspnetcore-integration-boundary.md` for the intended design boundary.
