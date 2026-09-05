# Dependency Rules

- `Domain.Primitives` has no internal project references.
- `SharedKernel` may reference `Domain.Primitives` only.
- `BuildingBlocks` has no internal project references.
- `Contracts` has no internal project references.
- Core projects may reference Foundation, never Modules.
- Modules may reference Foundation and explicitly approved Core contracts, never another module's implementation. A Module-to-Core dependency is approved only by an accepted ADR that identifies the exact dependency. ADR-0010 currently approves `Diyarak.Market.Listing` → `Diyarak.Platform.Listing` for the sector-agnostic listing subject-reference contract.
- Hosts compose the system but contain no business rules.
- Projects named Common, Utils, or Helpers are forbidden.
