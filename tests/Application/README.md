# Diyarak Application Tests

This directory contains tests for application-level use-case orchestration.

Current coverage:

- `Diyarak.Market.Application.Tests` verifies that `PublishListingUseCase` rejects a missing Listing without checking or saving it, rejects publication without saving when the referenced Property does not exist, and publishes and saves an existing ready Listing when the Property exists.

Concrete persistence adapters are not required for these tests; Application-owned ports are replaced with test doubles.
