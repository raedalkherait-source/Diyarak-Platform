# Diyarak Market 1.0

This area records concrete Diyarak Market product and domain requirements before implementation.

## Confirmed scope

- Property module.
- Company module.
- Public and administrative APIs.
- Search and listings.

## Architectural constraint

A property is a persistent domain asset; a listing is a market publication with an independent lifecycle. See `../adr/ADR-0006.md`.

## Current implementation status

- `Diyarak.Market.Property` is a foundation scaffold.
- `Diyarak.Market.Company` is a foundation scaffold.
- Concrete property and company domain behavior is not yet defined.
- Public and administrative endpoint requirements are not yet defined.
- Authentication and authorization requirements for administrative APIs are not yet defined.
- Search and listing behavior is not yet defined.

Implementation should not introduce speculative domain fields, lifecycle states, API contracts, or dependencies before concrete requirements are documented.
