# Diyarak Module Tests

This directory contains tests for implemented business module behavior.

Current Diyarak Market modules:

- `Diyarak.Market.Property` — initial domain baseline implemented and covered by tests for aggregate invariants, property address validation and equality, optional geographic location, optional living, usable, sales, and total areas, optional room-count validation, optional furnishing-quality validation, property-feature validation, optional building-year validation, parking-space-count validation, and commercial-property subtype rules.
- `Diyarak.Market.Company` — foundation scaffold; no tests yet.
- `Diyarak.Market.Listing` — initial baseline covered by tests for Listing identity, required subject-reference assignment, unsupported subject-type rejection, initial `Draft` status, required publication data, the `Draft` to `Published` transition, rejection of repeated publication, rejection of core publication-data changes after publication, confirmed publishing-role and transaction-intent value sets, `ListingContext` equality and invalid-value rejection, `ListingPrice` known/on-request behavior and negative-price rejection, `ListingHeadline` assignment, equality, and blank-value rejection, optional `ListingAvailableFromDate` assignment and value equality, and the stable `market.property` subject-type value.

Empty test projects are not created. Tests are added when concrete domain behavior is implemented.
