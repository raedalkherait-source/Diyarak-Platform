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
- `listing-subject-reference-requirements.md` records the confirmed subject-reference and publication-availability requirements, the decisions implemented by ADR-0010 through ADR-0024, and the remaining unresolved authentication implementation, claims mapping, endpoint exposure, concurrency, Listing creation, ownership transfer, and post-publication availability behavior.

## Architectural constraint

A Property is a persistent domain asset; a Listing is a market publication with an independent lifecycle. See `../adr/ADR-0006.md`. Market-specific Listing behavior belongs to `Diyarak.Market.Listing` while Platform Listing remains sector-agnostic; see `../adr/ADR-0008.md`.

ADR-0010 defines the sector-agnostic `ListingSubjectReference` contract and explicitly approves the `Diyarak.Market.Listing` dependency on `Diyarak.Platform.Listing`. ADR-0011 keeps that subject reference immutable for the lifetime of a Listing. ADR-0012 assigns supported subject-type validation to the consuming business module. ADR-0013 defines the initial Market Listing lifecycle as `Draft` followed by an explicit transition to `Published`. ADR-0014 defines the core publication-readiness requirements. ADR-0015 restricts core Listing edits to the `Draft` state.

ADR-0016 requires application-level subject-availability validation before publication. ADR-0017 treats Property existence as availability until a Property lifecycle exists. ADR-0018 introduces the Application layer for cross-module orchestration. ADR-0019 defines the Application-owned `IPropertyExistenceChecker` port and keeps `Diyarak.Market.Application` independent of `Diyarak.Market.Property`.

ADR-0020 keeps EF Core persistence records and mappings inside Integrations, requires explicit translation through valid domain APIs, and permits Integrations to implement Application-owned ports without introducing infrastructure concerns into business Modules.

ADR-0021 places Listing loading and saving inside publication orchestration through the Application-owned `IMarketListingRepository` port. Repository save is the current persistence commit boundary; explicit transaction, locking, isolation, and optimistic-concurrency behavior remain undefined.

ADR-0022 defines the future Listing publication HTTP contract and its stable validation, not-found, and conflict error codes.

ADR-0023 assigns every Listing an immutable non-empty `PublisherUserId` and permits publication only for that authenticated creator. A non-owner receives the same not-found result as a missing Listing, and ownership is checked before Property existence, domain mutation, or persistence.

ADR-0024 adds `publisher_user_id` as a required PostgreSQL `uuid` without a default. The ownership migration stops before changing `market.listings` when legacy rows require an authoritative ownership mapping.

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
- `Diyarak.Market.Listing` has an initial aggregate baseline. `Listing` uses a `Guid` identity, requires an immutable non-empty `PublisherUserId`, requires a sector-agnostic `ListingSubjectReference`, keeps both values immutable for its lifetime, and currently accepts only the supported `market.property` subject type.
- `ListingContext` combines the confirmed publishing roles `Owner`, `Tenant`, and `ProfessionalOrAgent` with the confirmed transaction intentions `Rent`, `Sell`, and `RentForLimitedPeriod`.
- `ListingPrice` represents either a known non-negative `Money` amount or price on request.
- `ListingHeadline` represents a non-empty listing headline without imposing an undocumented maximum length.
- `ListingAvailableFromDate` represents the confirmed available-from calendar date using `DateOnly` without imposing undocumented past/future validation.
- `MarketListingSubjectTypes.Property` defines the stable sector-qualified `market.property` subject type consumed through the Platform Listing subject-reference contract without creating a dependency on `Diyarak.Market.Property`.
- `Diyarak.Market.Listing.Tests` verifies Listing identity, required immutable publisher ownership, rejection of an empty publisher identifier, required subject-reference assignment, rejection of unsupported Market subject types, initial `Draft` status, publication-readiness enforcement, the `Draft` to `Published` transition, rejection of repeated publication, rejection of core publication-data edits after publication, the confirmed publishing-role and transaction-intent value sets, `ListingContext` assignment and equality, rejection of unsupported enum values, `ListingPrice` known/on-request behavior and negative-price rejection, `ListingHeadline` assignment, equality, and blank-value rejection, optional `ListingAvailableFromDate` assignment and value equality, and the stable Property subject-type value.
- `Diyarak.Platform.Listing` now provides the sector-agnostic `ListingSubjectReference` value object using a `Guid` subject identifier and a non-empty opaque subject-type string.
- New Market Listings start as `Draft`. `ListingContext`, `ListingHeadline`, and `ListingPrice` are required before `Publish()` can transition the Listing to `Published`; `ListingAvailableFromDate` remains optional. These core publication values can be assigned or changed only while the Listing is `Draft`, and repeated publication is rejected.
- `Diyarak.Market.Application` owns `IMarketListingRepository` and `IPropertyExistenceChecker`. `PublishListingUseCase` receives Listing and authenticated actor user identifiers, validates them, loads the aggregate, returns the same not-found result for a missing or non-owned Listing, verifies the referenced Property only for the owner, calls `Listing.Publish()`, saves the resulting state, and returns a classified `Result` for expected failures.
- `PublishListingErrors` defines the stable `market.listing.invalid_id`, `market.listing.invalid_actor_id`, `market.listing.not_found`, `market.listing.property_not_found`, and `market.listing.cannot_publish` errors.
- `Diyarak.Market.Application.Tests` verifies invalid Listing and actor identifiers, concealed not-found behavior for missing and non-owned Listings without a Property check or save, missing-Property conflict, publication-state conflict, and successful creator-owned publication and saving.
- `Diyarak.Platform.Persistence.PostgreSql` contains the initial full-state `MarketPropertyRecord`, its explicit EF Core mapping to `market.properties`, conversion to and from `Diyarak.Market.Property.Property`, and the `MarketPropertyPersistenceBaseline` migration.
- It also contains the full-state `MarketListingRecord`, including `PublisherUserId`, its explicit EF Core mapping to `market.listings`, conversion to and from `Diyarak.Market.Listing.Listing`, the `MarketListingPersistenceBaseline` migration, and the safe no-default `MarketListingPublisherOwnership` migration.
- `PostgreSqlPropertyExistenceChecker` implements `IPropertyExistenceChecker` with a no-tracking key-existence query.
- `PostgreSqlMarketListingRepository` implements `IMarketListingRepository` and loads and saves Market Listing state through `PlatformDbContext`.
- `AddPostgreSqlPersistence` registers both Application-port adapters as scoped services.
- `Diyarak.Platform.Persistence.PostgreSql.Tests` verifies Property and Listing table mappings, complete domain-record round trips including publisher ownership, found and missing Property checks, Listing repository loading and saving, dependency-injection registration, and ownership migration safety.
- `Diyarak.Api` references the Application layer, registers `PublishListingUseCase`, and conditionally maps the ADR-0022 publication route when ADR-0025 authentication is enabled. The route requires a mapped internal actor before Application orchestration and preserves ADR-0023 concealed not-found behavior for non-owners.
- Property creation, loading, update, and save workflows beyond existence checks; Listing creation persistence; identity-provider vendor selection and external-identity provisioning transport; administrative overrides and ownership transfer; authoritative migration of any legacy Listing ownership; explicit transaction, locking, and isolation guarantees; optimistic concurrency; behavior when a referenced subject later becomes unavailable; additional publication-readiness requirements; additional Listing lifecycle states and transitions; transaction-specific commercial terms; and published-Listing editing workflows remain deferred pending concrete requirements. The publication endpoint is exposed only when ADR-0025 authentication is explicitly enabled and requires a mapped internal user.
- Other public and administrative endpoint requirements are not yet defined.
- Authentication and authorization requirements for administrative APIs are not yet defined.
- Search behavior is not yet defined.

Implementation should not introduce speculative domain fields, lifecycle states, API contracts, or dependencies before concrete requirements are documented.
