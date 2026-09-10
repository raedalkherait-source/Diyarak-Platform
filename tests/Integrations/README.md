# Diyarak Integration Tests

This directory contains tests for concrete infrastructure and external-system integrations.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql.Tests` verifies PostgreSQL-specific persistence mappings and Application-port adapters.

Provider-specific behavior must be verified against PostgreSQL when it depends on PostgreSQL semantics. Tests that inspect EF Core model metadata do not require a running database.
