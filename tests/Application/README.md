# Diyarak Application Tests

This directory contains tests for application-level use-case orchestration.

Current coverage:

- `Diyarak.Market.Application.Tests` verifies validation failure for an empty Listing identifier, not-found failure without a Property check or save when the Listing is missing, conflict without a save when the referenced Property is missing, conflict without a save when the Listing cannot be published from its current state, and successful publication and saving when all requirements are met.

Concrete persistence adapters are not required for these tests; Application-owned ports are replaced with test doubles.
