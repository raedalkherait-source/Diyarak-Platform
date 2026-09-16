# src/Integrations

This directory contains concrete infrastructure implementations and external-system adapters.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql` provides PostgreSQL persistence through EF Core.
- Market Property persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketPropertyPersistenceBaseline` migration.
- `MarketPropertyOwnership` adds nullable `owner_user_id` without a default or historical backfill; its check constraint permits legacy `NULL` rows while rejecting an all-zero owner identifier when ownership is present.
- Market Listing persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketListingPersistenceBaseline` migration.
- `MarketListingPublisherOwnership` adds the required `publisher_user_id` column without a default and stops before changing the table when existing Listings require an authoritative ownership migration.
- `PostgreSqlMarketPropertyRepository` implements the Application-owned `IMarketPropertyRepository` port for actor-owned Property insertion, no-tracking direct-by-id loading, and no-tracking owner-filtered collection loading using the full-state record mapping, including legacy-safe nullable management ownership.
- `PostgreSqlPropertyExistenceChecker` implements the Application-owned `IPropertyExistenceChecker` port for publication-time subject availability.
- `PostgreSqlPropertyListingAuthorizationChecker` implements `IPropertyListingAuthorizationChecker` with a no-tracking `Property.Id + OwnerUserId` query for Listing creation.
- `PostgreSqlMarketListingRepository` implements the Application-owned `IMarketListingRepository` port for Listing insertion, no-tracking loading by identifier or publisher ownership, and expected-version conditional saving; `MarketListingRecord.Version` is the EF Core concurrency token for both Draft editing and publication races.
- `PostgreSqlPublishedListingQuery` implements the Application-owned `IPublishedListingQuery` port with a no-tracking `Published` filter, deterministic `ListingId` ordering, and bounded `Skip`/`Take` pagination for the anonymous public collection. ADR-0040 adds `ix_market_listings_status_id` on `(status, id)` so PostgreSQL has an index aligned with that confirmed filter/order access pattern without changing the public contract.
- ADR-0041 adds `ix_market_listings_publisher_user_id` and `ix_market_properties_owner_user_id` so the existing authenticated management collection equality filters on persisted Listing publisher ownership and Property management ownership have dedicated indexes without introducing collection ordering or pagination semantics.
- `PostgreSqlMarketTransactionRunner` implements the Application-owned `IMarketTransactionRunner` port with an explicit `ReadCommitted` EF Core transaction executed through the configured retry execution strategy.
- `AddPostgreSqlPersistence` registers the Market Application-port adapters as scoped services backed by the same `PlatformDbContext`.

Integrations may persist business Modules and implement Application-owned ports while keeping domain models independent of infrastructure concerns. See ADR-0020, ADR-0021, ADR-0024, ADR-0026, ADR-0027, ADR-0028, ADR-0029, ADR-0030, ADR-0031, ADR-0032, ADR-0033, ADR-0034, ADR-0035, ADR-0036, ADR-0037, ADR-0038, ADR-0039, ADR-0040, and ADR-0041.

ADR-0029 stores a required numeric Listing version and uses it as the EF Core concurrency token for both Draft editing and publication; the migration backfills existing Listing rows to version `1` before enforcing non-null storage.

ADR-0042 replaces the ADR-0041 single-column management ownership indexes with composite ownership-plus-identifier indexes matching bounded deterministic management pagination.
