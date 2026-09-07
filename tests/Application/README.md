# Diyarak Application Tests

This directory contains tests for application-level use-case orchestration.

Current coverage:

- `Diyarak.Market.Application.Tests` — verifies that `PublishListingUseCase` rejects publication when the referenced Property does not exist and publishes a ready Listing when the Property exists.

Concrete persistence adapters are not required for these tests; application ports are replaced with test doubles.
