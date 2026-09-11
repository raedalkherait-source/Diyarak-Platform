# Diyarak Integration Tests

This directory contains tests for concrete infrastructure and external-system integrations.

Current structure:

- `Diyarak.Platform.Persistence.PostgreSql.Tests` verifies Market Property and Market Listing EF Core model metadata, domain-to-record mappings including immutable Listing publisher ownership, the Property existence checker, the Market Listing repository, dependency-injection registration for Application-owned ports, and the safe no-default ownership migration operations.

Provider-specific behavior must be verified against PostgreSQL when it depends on PostgreSQL semantics. Tests that inspect EF Core model metadata or exercise adapter behavior with EF Core's in-memory provider do not require a running PostgreSQL database.
