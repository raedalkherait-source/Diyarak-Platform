# Listing Subject Reference Requirements

This document records the confirmed requirements, implemented decisions, and remaining unresolved behavior for identifying and validating the subject published by a Listing.

ADR-0010 through ADR-0012 define the subject-reference contract and its ownership. ADR-0016 through ADR-0035 define the initial subject-availability rule, its application-layer orchestration, its Application-owned ports, the infrastructure boundary for persistence-backed implementations, the loading and saving boundary, the HTTP result contracts, creator ownership, safe ownership persistence migration, authenticated creation, and the explicit transaction boundary.

## Confirmed requirements

- A Listing is separate from the persistent asset that it publishes.
- Creating another Listing for an existing Property must not require creating a duplicate Property asset.
- A Listing therefore needs a way to identify its published subject.
- The subject-reference contract must remain sector-agnostic.
- `Diyarak.Platform.Listing` must not depend on a business module such as `Diyarak.Market.Property`.
- `Diyarak.Market.Listing` must not depend directly on another business module implementation such as `Diyarak.Market.Property`.
- A Module-to-Core dependency requires explicit approval through an accepted architecture decision.
- The initial Market use case is publishing a Property Listing while preserving a design that can support additional subject types later.
- Publishing a Listing requires its referenced subject to exist and be available for publication.
- Until a Property lifecycle is defined, an existing Property is considered available for Listing publication.
- Subject-availability validation occurs in application-level orchestration before the Listing aggregate is asked to publish itself.
- Publication orchestration receives Listing and authenticated actor user identifiers and loads and saves the aggregate through an Application-owned repository port.
- Every Listing has a required immutable non-empty `PublisherUserId` that identifies its creator without depending on the Identity implementation.
- Only the authenticated creator may publish a Listing; a non-owner receives the same not-found result as a missing Listing before Property existence, domain mutation, or persistence is attempted.

## Implemented contract

ADR-0010 defines `ListingSubjectReference` in `Diyarak.Platform.Listing`.

The reference contains:

- a `Guid` subject identifier;
- a non-empty opaque string subject type.

Platform Listing does not define a business-sector enum for subject types.

Business modules own the meaning of their subject-type values.

`Diyarak.Market.Listing` defines the confirmed Property subject type as `market.property` and is explicitly approved to reference `Diyarak.Platform.Listing` for this contract.

ADR-0011 requires a Listing to keep the same `ListingSubjectReference` for its lifetime. Publishing a different subject requires creating a new Listing.

ADR-0012 requires the consuming business module to validate which subject types it supports. `Diyarak.Market.Listing` currently accepts only `market.property` and rejects unsupported subject types. This validation remains separate from verifying that the referenced Property exists.

ADR-0016 places subject-availability validation in application-level publication orchestration before `Listing.Publish()` is called.

ADR-0017 defines Property availability as existence until a Property lifecycle introduces additional availability states.

ADR-0018 introduces the Application layer for cross-module use-case orchestration while preserving Module isolation.

ADR-0019 defines the Application-owned `IPropertyExistenceChecker` port. `Diyarak.Market.Application` uses this port without referencing `Diyarak.Market.Property` directly.

ADR-0020 keeps EF Core persistence records and mappings inside Integrations and requires explicit translation through valid domain APIs.

ADR-0021 defines the Application-owned `IMarketListingRepository` port and requires `PublishListingUseCase` to receive a Listing identifier, load the aggregate, reject a missing Listing, verify the referenced Property, invoke `Listing.Publish()`, and save the resulting state. Repository save is the current persistence commit boundary.

`Diyarak.Platform.Persistence.PostgreSql` implements `IPropertyExistenceChecker` with a no-tracking key query against the mapped `market.properties` table. The initial Property persistence representation stores the aggregate's complete current state and converts explicitly between the persistence record and the domain aggregate.

The PostgreSQL integration also maps the complete current Market Listing state, including `PublisherUserId`, to `market.listings` through `MarketListingRecord`, explicit EF Core configuration, and explicit conversion through valid Listing domain APIs. `MarketListingPersistenceBaseline` creates the table, and `MarketListingPublisherOwnership` adds the required owner column without a default.

`PostgreSqlMarketListingRepository` implements `IMarketListingRepository` for Listing insertion plus existing Listing loading and saving. It and `PostgreSqlPropertyExistenceChecker` are registered as scoped services backed by the same `PlatformDbContext`.

ADR-0027 introduces an explicit Application-owned transaction boundary for Listing creation and publication. The PostgreSQL adapter executes the persistence-sensitive workflow in one `ReadCommitted` transaction using the same scoped `PlatformDbContext`; successful Results commit, while expected failures and unexpected exceptions roll back. No explicit Property row lock or stronger isolation guarantee is introduced.

ADR-0022 defines the future publication route as `POST /api/market/listings/{listingId}/publish`. Successful publication returns `204 No Content`; invalid Listing identifiers return `400 Bad Request`; a missing or non-owned Listing returns `404 Not Found`; and a missing Property or invalid publication state returns `409 Conflict`.

ADR-0023 assigns each Listing an immutable `PublisherUserId` and restricts publication to that authenticated creator. `ListingContext.PublishingRole` remains commercial context and is not authorization proof.

ADR-0024 requires the ownership migration to stop before changing `market.listings` when legacy rows exist, rather than inventing an owner or using `Guid.Empty`.

`PublishListingUseCase` receives Listing and actor user identifiers and returns a classified `Result` through stable `market.listing.*` error codes. It validates both identifiers, conceals missing and non-owned Listings with the same not-found result, and performs the ownership check before the Property query. Unexpected persistence and infrastructure exceptions continue to the Host exception boundary.

ADR-0025 is implemented by the Host and PostgreSQL integration. `Diyarak.Api` validates explicitly configured JWT bearer access tokens, preserves exact `iss` and `sub` claims, resolves them through persistent external-identity mappings to an internal `User.Id`, returns `401 Unauthorized` for missing or invalid access tokens and `403 Forbidden` for valid but unmapped identities, and conditionally maps the publication route only when authentication is enabled. The mapped internal `User.Id` is passed to `PublishListingUseCase`, while mapped non-owners continue to receive the same concealed `404 Not Found` result as missing Listings.

ADR-0026 defines and implements the initial Listing creation workflow. `POST /api/market/listings` accepts only a Property identifier, requires the ADR-0025 mapped-user policy, uses the mapped internal `User.Id` as the immutable `PublisherUserId`, verifies Property existence through `IPropertyExistenceChecker`, creates a new `Draft` Listing for the `market.property` subject type, and inserts it through `IMarketListingRepository.AddAsync`. Creation returns `201 Created` with the generated Listing identifier; invalid Property identifiers return `400 Bad Request`, missing Properties return `409 Conflict`, and authentication failures retain the ADR-0025 `401`/`403` behavior.

ADR-0037 narrows creation authorization for the current Property subject type. `CreateListingUseCase` now requires `IPropertyListingAuthorizationChecker` to confirm that the mapped actor matches persisted `Property.OwnerUserId`. Missing, non-owned, and legacy-unowned Properties all return the existing concealed `409 Conflict` with `market.listing.property_not_found`; the API does not disclose which condition occurred. `IPropertyExistenceChecker` remains the publication-time subject-availability port.

ADR-0027 defines and implements the explicit transaction boundary for the existing creation and publication workflows through `IMarketTransactionRunner`. Required identifier validation remains outside the transaction; persistence-sensitive work executes inside one PostgreSQL `ReadCommitted` transaction. ADR-0028 first adds optimistic concurrency for publication. ADR-0029 replaces the status-only token with a required positive numeric Listing version shared by publication and Draft editing. `PATCH /api/market/listings/{listingId}` allows only the mapped owner to partially replace context, headline, price, or the optional available-from date while the Listing is `Draft`; omitted fields remain unchanged, explicit `availableFromDate: null` clears that optional value, stale versions return `market.listing.concurrent_modification` as `409 Conflict`, and successful edits return the next version.

ADR-0030 adds authenticated Property asset creation through `POST /api/market/properties`. ADR-0031 adds authenticated direct loading through `GET /api/market/properties/{propertyId}` using a no-tracking repository read and the same full-state mapping. ADR-0034 assigns every newly created Property the mapped internal actor as immutable management `OwnerUserId`; direct loading is now owner-only and conceals missing, non-owned, and legacy-unowned Properties with the same `404`. The nullable PostgreSQL owner column has no default or legacy backfill. This ownership controls Diyarak management access and does not assert legal real-estate ownership. ADR-0035 adds mapped-user-only `GET /api/market/properties` for the current owner's management collection; the query filters by persisted `OwnerUserId` and excludes legacy-unowned rows. ADR-0042 later defines bounded technical pagination with `page`/`pageSize`, identifier ordering, and `hasMore`; search and business ordering remain undefined. ADR-0032 adds creator-only direct Listing loading through `GET /api/market/listings/{listingId}`. Missing and non-owned Listings share the same concealed `404`; successful reads return current Listing management state. ADR-0033 adds mapped-user-only `GET /api/market/listings` for the current creator's own management collection. The query filters by persisted `PublisherUserId` and returns the same management representation. ADR-0042 later defines bounded technical pagination with `page`/`pageSize`, identifier ordering, and `hasMore`; search and business ordering remain undefined. ADR-0038 adds anonymous `GET /api/market/public/listings/{listingId}` for `Published` Listings only. Draft and missing Listings share the same `404`, and the public projection omits version/status, publisher ownership, and subject identifiers until public Property/subject representation is separately defined. ADR-0039 adds anonymous `GET /api/market/public/listings` for bounded enumeration of `Published` Listings only, using `page`/`pageSize`, deterministic identifier ordering, `hasMore`, and the same reduced public projection. Search ranking, filtering, recommendation, semantic sorting, and public Property/subject presentation remain undefined. ADR-0044 adds an opaque strong `ETag` to successful direct public Listing detail responses and returns `304 Not Modified` for matching `If-None-Match` validators; the internal Listing version remains absent from the public JSON and no public freshness lifetime is defined. ADR-0045 adds the same conditional-revalidation behavior to each bounded public Listing collection page with a page-specific opaque validator derived from page coordinates, `hasMore`, and the ordered Listing identifier/version sequence; it does not add freshness, search, ranking, or snapshot guarantees.

## Remaining open requirements

The following behavior remains undefined and must not be invented:

- Property search, update, removal/archive, ownership transfer/delegation, and administrative access beyond actor-owned creation, owner-only direct/collection loading, the current full-state record/mapping, and the existence query.
- An authoritative owner mapping and separately reviewed migration or provisioning process for legacy Property rows whose `owner_user_id` remains `NULL`.
- Delegated Listing-creation authority for agents, companies, administrators, or other non-owner actors beyond ADR-0037 owner-only Property authorization.
- Property row-locking or stronger-than-`ReadCommitted` isolation guarantees spanning the Property existence check and Listing write.
- Administrative publication overrides and Listing ownership transfer.
- An authoritative ownership mapping and separately reviewed data migration for any environment containing legacy Listing rows.
- Transport representation and API behavior for operations other than the defined creation, creator direct-loading, creator collection-loading, public published direct/collection loading, Draft editing, and publication contracts.
- What happens to a Listing when its referenced subject is removed, archived, or otherwise becomes unavailable after publication.
- Subject resolution beyond the current Property-existence check.
- Any additional lifecycle behavior associated with subject availability.
- Approval of any additional Module-to-Core dependency or additional Market subject type.

These questions must be resolved from concrete product and architecture requirements rather than inferred from the current Market implementation.


ADR-0036 adds owner-only Property replacement and Property version concurrency. ADR-0037 subsequently requires matching Property management ownership for new Listing creation while leaving delegated authority, agency/company authorization, and administrative overrides undefined.
