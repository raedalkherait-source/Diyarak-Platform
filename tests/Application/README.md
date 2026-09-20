# Diyarak Application Tests

This directory contains tests for application-level use-case orchestration.

Current coverage:

- `Diyarak.Market.Application.Tests` verifies Market use-case orchestration for Property creation, owner-only direct loading, bounded owner-scoped collection loading, and full replacement; Listing creation, creator-only direct loading, bounded creator-scoped collection loading, Draft partial editing, and publication; and anonymous published-Listing direct and bounded collection reads. Coverage includes input validation, concealed ownership failures, subject authorization and availability, pagination and `hasMore`, fail-closed adapter results, optimistic-concurrency conflicts, and successful persistence paths.

Concrete persistence adapters are not required for these tests; Application-owned ports are replaced with test doubles.
