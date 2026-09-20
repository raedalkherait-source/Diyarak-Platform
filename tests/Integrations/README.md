# Diyarak Integration Tests

This directory contains tests for concrete infrastructure and external-system integrations.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql.Tests` verifies PostgreSQL persistence model metadata and domain-to-record mappings for Market Property and Market Listing, external-identity mapping and resolution, Property and Listing repositories, Property existence and Listing-creation authorization checks, published-Listing queries, dependency-injection registration for Application-owned ports, optimistic-concurrency persistence, ownership migrations, and the confirmed public and management query-index migrations.

Provider-specific behavior must be verified against PostgreSQL when it depends on PostgreSQL semantics. Tests that inspect EF Core model metadata or exercise adapter behavior with EF Core's in-memory provider do not require a running PostgreSQL database.
