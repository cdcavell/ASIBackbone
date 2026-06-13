# Contributing to AsiBackbone

Thank you for helping improve AsiBackbone.

In this software project, **ASI** means **Accountable Systems Infrastructure**. AsiBackbone is a .NET package family for governance-oriented decision flow. Contributions should keep the project grounded as practical governance infrastructure, not an artificial superintelligence implementation, AI model host, legal/compliance guarantee, or robot controller.

## Contribution goals

Contributions should preserve the stable package direction:

- keep `CDCavell.AsiBackbone.Core` framework-neutral;
- keep host ownership explicit;
- keep package boundaries clear;
- avoid hidden infrastructure assumptions;
- document public behavior before presenting it as stable;
- avoid overclaiming around signing, tamper-evidence, privacy, compliance, AI, or physical execution.

## Local setup

Install:

- .NET SDK supported by the repository;
- Git;
- a code editor such as Visual Studio, Visual Studio Code, or JetBrains Rider.

Restore local tools after cloning:

```bash
dotnet tool restore
```

## Build and test locally

Run the standard solution checks before opening a pull request:

```bash
dotnet restore AsiBackbone.slnx
dotnet build AsiBackbone.slnx --configuration Release
dotnet test AsiBackbone.slnx --configuration Release --no-build --no-restore
```

When package-consumer behavior changes, also run the smoke tests when possible:

```bash
bash ./eng/smoke-tests/external-consumer-smoke.sh
bash ./eng/smoke-tests/stable-package-integration-smoke.sh
```

The smoke scripts pack local package artifacts, create temporary external consumer projects, install packages from the local NuGet output, and validate that package consumers can wire the implemented package family without repository project references.

## Documentation checks

Build the documentation site locally when documentation changes or public behavior changes:

```bash
dotnet tool restore
dotnet tool run docfx docs/docfx.json --serve
```

Documentation should clearly separate:

- implemented stable behavior;
- alpha, preview, or sample behavior;
- host responsibilities;
- future provider work;
- conceptual ASI Backbone inspiration;
- practical software claims.

Use cautious language. For example, say records are **signing-ready** only when built-in signing is not implemented. Do not claim tamper-evidence, legal compliance, or non-repudiation unless the implementation and documentation explicitly support those claims.

## Issue guidance

Before starting work:

- check whether an issue already exists;
- keep the issue scope small enough to review;
- describe the package boundary involved;
- identify whether the work affects Core, ASP.NET Core, EF Core, in-memory storage, docs, samples, tests, or release process;
- call out any stable API or serialized/persisted schema implications.

Good issues should include acceptance criteria or a clear expected outcome.

## Pull request guidance

Pull requests should include:

- a clear title;
- a short summary of what changed;
- testing performed, or a clear note when tests were not run;
- documentation updates when public behavior changes;
- issue closure text such as `Closes #123` when appropriate.

Prefer focused pull requests. Avoid mixing unrelated code, documentation, release, and formatting work unless the issue explicitly calls for it.

## Stable package family checklist

Before opening a pull request, consider the checklist in [Developer Checklist](docs/articles/developer-checklist.md).

At minimum, ask:

- Does this preserve Core's framework-neutral boundary?
- Does this preserve host ownership of infrastructure, persistence, routing, authentication, authorization, and execution?
- Does this change public APIs or documented behavior?
- Does this affect persisted or serialized records?
- Does this require schema-version guidance?
- Does this need package-consumer smoke coverage?
- Does this require documentation updates?
- Does the wording avoid overclaiming?

## Release-readiness reminders

For stable release work, confirm:

- package versions and release notes agree;
- public documentation links resolve;
- known limitations are documented;
- API compatibility expectations are clear;
- privacy and signing boundaries are clear;
- provider work is not implied to be stable unless explicitly released as stable;
- tests and smoke checks have been run or the PR explains why they were not run.

## Security and sensitive information

Do not include secrets, credentials, tokens, private keys, passwords, confidential data, or raw sensitive records in issues, pull requests, examples, tests, screenshots, or documentation.

Use synthetic examples and opaque identifiers in documentation and tests.
