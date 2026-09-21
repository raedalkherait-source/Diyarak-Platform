# Dependency Rules

- `Domain.Primitives` has no internal project references.
- `SharedKernel` may reference `Domain.Primitives` only.
- `BuildingBlocks` has no internal project references.
- `Contracts` has no internal project references.
- Core projects may reference Foundation, never Modules, Application, Integrations, or Hosts.
- Modules may reference Foundation and explicitly approved Core contracts, never another module's implementation, Application, Integrations, or Hosts. A Module-to-Core dependency is approved only by an accepted ADR that identifies the exact dependency. ADR-0010 currently approves `Diyarak.Market.Listing` → `Diyarak.Platform.Listing` for the sector-agnostic listing subject-reference contract.
- Application projects may reference Foundation, approved Core contracts, and business Modules required by their use cases, but never Integrations or Hosts.
- Modules must not depend on Application.
- Hosts compose Application use cases and Integrations but contain no business rules.
- Integrations may reference Foundation, approved Core contracts, business Modules they persist or adapt, and Application projects whose ports they implement, but never Hosts or another Integration implementation. They must not move business rules out of the domain or Application layers. ADR-0020 requires Entity Framework Core persistence models and mappings to remain Integration-owned. ADR-0025 currently approves `Diyarak.Platform.Persistence.PostgreSql` → `Diyarak.Platform.Identity` for the `ExternalIdentity` and `IExternalIdentityResolver` contracts.
- Projects named Common, Utils, or Helpers are forbidden.
