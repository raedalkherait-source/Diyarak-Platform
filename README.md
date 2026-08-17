# Diyarak Platform — Foundation Enterprise v1.0

Production-oriented foundation for **Diyarak**, a modular platform for the Syrian real-estate and construction sector.

## Included

- .NET 10 LTS foundation libraries.
- Domain primitives with validation and arithmetic.
- DDD shared kernel.
- Results, errors, guards, clock, and pagination building blocks.
- Transport-neutral contracts.
- Unit and architecture tests.
- Central package management and deterministic builds.
- CI, CodeQL, dependency review, package release workflow, and Dependabot.
- Docker-based local dependencies and a VS Code dev container.
- Engineering, security, testing, release, and architecture documentation.

## Quick start

```bash
cp docker/.env.example docker/.env
dotnet restore Diyarak.Platform.All.sln
dotnet build Diyarak.Platform.All.sln -c Release --no-restore
dotnet test Diyarak.Platform.All.sln -c Release --no-build
```

Or run:

```bash
./scripts/bootstrap.sh
./scripts/verify.sh
```

Windows PowerShell equivalents are provided in `scripts/*.ps1`.

## Solutions

- `Diyarak.Platform.Foundation.sln` — foundation libraries and their tests.
- `Diyarak.Platform.All.sln` — all current projects, including architecture tests.

## Repository boundaries

```text
Foundation
├── Diyarak.Platform.Domain.Primitives
├── Diyarak.Platform.SharedKernel
├── Diyarak.Platform.BuildingBlocks
└── Diyarak.Platform.Contracts
```

`Core`, `Modules`, `Integrations`, and `Hosts` are reserved for subsequent milestones. See `docs/architecture/dependency-rules.md`.

## Important deployment note

The included `compose.yaml` is a **local development stack**, not a production deployment topology. Production infrastructure requires environment-specific security, backups, observability, capacity planning, and legal review.
