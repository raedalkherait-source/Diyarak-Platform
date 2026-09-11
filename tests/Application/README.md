# Diyarak Application Tests

This directory contains tests for application-level use-case orchestration.

Current coverage:

- `Diyarak.Market.Application.Tests` verifies validation failures for empty Listing and actor identifiers, not-found results without a Property check or save for missing and non-owned Listings, conflict without a save when the referenced Property is missing, conflict without a save when the Listing cannot be published from its current state, and successful creator-owned publication and saving.

Concrete persistence adapters are not required for these tests; Application-owned ports are replaced with test doubles.
