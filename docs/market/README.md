# Diyarak Market 1.0

This area records concrete Diyarak Market product and domain requirements before implementation.

## Confirmed scope

- Property module.
- Company module.
- Public and administrative APIs.
- Search and listings.

## Requirements

- `property-listing-requirements.md` records the currently confirmed Property and Listing concepts derived from the supplied Market reference flow.

## Architectural constraint

A property is a persistent domain asset; a listing is a market publication with an independent lifecycle. See `../adr/ADR-0006.md`.

## Current implementation status

- `Diyarak.Market.Property` has an initial domain baseline with aggregate identity, supported top-level property categories, and a required property address.
- `PropertyAddress` captures street, house number, postal code, city, and an optional geographic location using the shared `GeoCoordinate` primitive.
- `Diyarak.Market.Property.Tests` verifies aggregate invariants, address validation and equality, and geographic-location assignment.
- Additional Property characteristics documented in `property-listing-requirements.md` are not yet implemented.
- `Diyarak.Market.Company` remains a foundation scaffold pending concrete company requirements.
- Public and administrative endpoint requirements are not yet defined.
- Authentication and authorization requirements for administrative APIs are not yet defined.
- Listing lifecycle and publication workflow are not yet defined.
- Search behavior is not yet defined.

Implementation should not introduce speculative domain fields, lifecycle states, API contracts, or dependencies before concrete requirements are documented.
