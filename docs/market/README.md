# Diyarak Market 1.0

This area records concrete Diyarak Market product and domain requirements before implementation.

## Confirmed scope

- Property module.
- Company module.
- Listing module.
- Public and administrative APIs.
- Search.

## Requirements

- `property-listing-requirements.md` records the currently confirmed Property and Listing concepts derived from the supplied Market reference flow.

## Architectural constraint

A property is a persistent domain asset; a listing is a market publication with an independent lifecycle. See `../adr/ADR-0006.md`. Market-specific listing behavior belongs to `Diyarak.Market.Listing` while Platform Listing remains sector-agnostic; see `../adr/ADR-0008.md`.

## Current implementation status

- `Diyarak.Market.Property` has an initial domain baseline with aggregate identity, supported top-level property categories, a required property address, optional living, usable, sales, and total areas, optional room counts, optional furnishing quality, optional property features, optional building years, optional parking-space count, and optional commercial-property subtype.
- `PropertyAddress` captures street, house number, postal code, city, and an optional geographic location using the shared `GeoCoordinate` primitive.
- Living, usable, sales, and total areas use the shared `Area` primitive and remain optional.
- Total rooms, bedroom count, and bathroom count are optional and reject negative values.
- Furnishing quality is optional and supports the confirmed values `Simple`, `Normal`, `Upscale`, and `Luxury`.
- Property features are optional, de-duplicated, and limited to the currently confirmed feature set.
- Construction year and last modernization year are optional; positive values are accepted and `null` represents an unknown or unspecified year.
- Parking-space count is optional and rejects negative values.
- Commercial-property subtype is optional, limited to the confirmed subtype set, and can only be assigned when the top-level category is `CommercialProperty`.
- `Diyarak.Market.Property.Tests` verifies aggregate invariants, address validation and equality, geographic-location assignment, optional living, usable, sales, and total area assignment, room-count validation, furnishing-quality validation, property-feature validation, building-year validation, parking-space-count validation, and commercial-subtype rules.
- Additional Property characteristics documented in `property-listing-requirements.md` are not yet implemented.
- `Diyarak.Market.Company` remains a foundation scaffold pending concrete company requirements.
- `Diyarak.Market.Listing` has an initial context baseline with a `ListingContext` value object combining the confirmed publishing roles `Owner`, `Tenant`, and `ProfessionalOrAgent` with the confirmed transaction intentions `Rent`, `Sell`, and `RentForLimitedPeriod`.
- `ListingPrice` represents either a known non-negative `Money` amount or price on request.
- `ListingHeadline` represents a non-empty listing headline without imposing an undocumented maximum length.
- `Diyarak.Market.Listing.Tests` verifies the confirmed publishing-role and transaction-intent value sets, `ListingContext` assignment and equality, rejection of unsupported enum values, `ListingPrice` known/on-request behavior and negative-price rejection, and `ListingHeadline` assignment, equality, and blank-value rejection.
- The Listing aggregate, subject-reference contract, lifecycle, transaction-specific commercial terms, and publication workflow remain deferred pending concrete requirements.
- Public and administrative endpoint requirements are not yet defined.
- Authentication and authorization requirements for administrative APIs are not yet defined.
- Search behavior is not yet defined.

Implementation should not introduce speculative domain fields, lifecycle states, API contracts, or dependencies before concrete requirements are documented.
