# Dependency Rules

- `Domain.Primitives` has no internal project references.
- `SharedKernel` may reference `Domain.Primitives` only.
- `BuildingBlocks` has no internal project references.
- `Contracts` has no internal project references.
- Core projects may reference Foundation, never Modules.
- Modules may reference Foundation and approved Core contracts, never another module's implementation.
- Hosts compose the system but contain no business rules.
- Projects named Common, Utils, or Helpers are forbidden.
