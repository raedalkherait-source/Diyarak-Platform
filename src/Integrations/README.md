# src/Integrations

This directory contains concrete infrastructure implementations and external-system adapters.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql` provides PostgreSQL persistence through EF Core.
- Market Property persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketPropertyPersistenceBaseline` migration.
- Market Listing persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketListingPersistenceBaseline` migration.
- `MarketListingPublisherOwnership` adds the required `publisher_user_id` column without a default and stops before changing the table when existing Listings require an authoritative ownership migration.
- `PostgreSqlPropertyExistenceChecker` implements the Application-owned `IPropertyExistenceChecker` port.
- `PostgreSqlMarketListingRepository` implements the Application-owned `IMarketListingRepository` port.
- `AddPostgreSqlPersistence` registers both Application-port adapters as scoped services backed by `PlatformDbContext`.

Integrations may persist business Modules and implement Application-owned ports while keeping domain models independent of infrastructure concerns. See ADR-0020, ADR-0021, and ADR-0024.
