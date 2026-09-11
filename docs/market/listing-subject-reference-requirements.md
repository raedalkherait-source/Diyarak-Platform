# Listing Subject Reference Requirements

This document records the confirmed requirements, implemented decisions, and remaining unresolved behavior for identifying and validating the subject published by a Listing.

ADR-0010 through ADR-0012 define the subject-reference contract and its ownership. ADR-0016 through ADR-0021 define the initial subject-availability rule, its application-layer orchestration, its Application-owned ports, the infrastructure boundary for persistence-backed implementations, and the loading and saving boundary for publication.

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
- Publication orchestration receives a Listing identifier and loads and saves the aggregate through an Application-owned repository port.

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

The PostgreSQL integration also maps the complete current Market Listing state to `market.listings` through `MarketListingRecord`, explicit EF Core configuration, and explicit conversion through valid Listing domain APIs. `MarketListingPersistenceBaseline` creates the table.

`PostgreSqlMarketListingRepository` implements `IMarketListingRepository` for existing Listing loading and saving. It and `PostgreSqlPropertyExistenceChecker` are registered as scoped services backed by the same `PlatformDbContext`.

No explicit transaction, lock, or isolation guarantee currently spans the Property existence query and Listing save.

## Remaining open requirements

The following behavior remains undefined and must not be invented:

- Property creation, loading, update, and save workflows beyond the current full-state record, mapping, and existence query.
- Listing creation persistence and its application workflow.
- Explicit transaction, locking, and isolation guarantees spanning the Property existence check and Listing save.
- Optimistic concurrency and behavior for competing publication attempts.
- Transport representation, publication endpoints, and API error mapping.
- What happens to a Listing when its referenced subject is removed, archived, or otherwise becomes unavailable after publication.
- Subject resolution beyond the current Property-existence check.
- Any additional lifecycle behavior associated with subject availability.
- Approval of any additional Module-to-Core dependency or additional Market subject type.

These questions must be resolved from concrete product and architecture requirements rather than inferred from the current Market implementation.
