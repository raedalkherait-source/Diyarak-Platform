# Diyarak Platform

**Diyarak** is a modular platform for the Syrian real-estate and construction sector. Foundation 1.0 is complete, while Platform Core and Diyarak Market are being developed incrementally from confirmed requirements.

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
src/
|-- Foundation
|-- Core
|-- Modules
|-- Application
|-- Integrations
`-- Hosts
```

Diyarak is a modular monolith. Foundation contains stable cross-cutting abstractions; domain rules live in Modules or Core; Application coordinates use cases; Integrations implement infrastructure and Application-owned ports; and Hosts compose the application with concrete adapters. See `docs/architecture/overview.md` and `docs/architecture/dependency-rules.md`.

## Important deployment note

The included `compose.yaml` is a **local development stack**, not a production deployment topology. Production infrastructure requires environment-specific security, backups, observability, capacity planning, and legal review.
