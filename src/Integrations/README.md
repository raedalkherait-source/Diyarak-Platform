# src/Integrations

This directory contains concrete infrastructure implementations and external-system adapters.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql` provides PostgreSQL persistence through EF Core.
- Market Property persistence uses an Integration-owned record, explicit EF Core configuration, explicit domain-to-record mapping, and the `MarketPropertyPersistenceBaseline` migration.
- `PostgreSqlPropertyExistenceChecker` implements the Application-owned `IPropertyExistenceChecker` port and is registered by `AddPostgreSqlPersistence`.

Integrations may persist business Modules and implement Application-owned ports while keeping domain models independent of infrastructure concerns. See ADR-0020.
