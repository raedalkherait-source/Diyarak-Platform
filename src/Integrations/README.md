# src/Integrations

This directory contains concrete infrastructure implementations and external-system adapters.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql` provides PostgreSQL persistence through EF Core.
- Market Property persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketPropertyPersistenceBaseline` migration.
- Market Listing persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketListingPersistenceBaseline` migration.
- `MarketListingPublisherOwnership` adds the required `publisher_user_id` column without a default and stops before changing the table when existing Listings require an authoritative ownership migration.
- `PostgreSqlPropertyExistenceChecker` implements the Application-owned `IPropertyExistenceChecker` port.
- `PostgreSqlMarketListingRepository` implements the Application-owned `IMarketListingRepository` port for Listing insertion, loading, and expected-status conditional saving; `MarketListingRecord.Status` is an EF Core concurrency token for publication races.
- `PostgreSqlMarketTransactionRunner` implements the Application-owned `IMarketTransactionRunner` port with an explicit `ReadCommitted` EF Core transaction executed through the configured retry execution strategy.
- `AddPostgreSqlPersistence` registers the Market Application-port adapters as scoped services backed by the same `PlatformDbContext`.

Integrations may persist business Modules and implement Application-owned ports while keeping domain models independent of infrastructure concerns. See ADR-0020, ADR-0021, ADR-0024, ADR-0026, ADR-0027, and ADR-0028.
