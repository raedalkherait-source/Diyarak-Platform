# Diyarak Module Tests

This directory contains tests for implemented business module behavior.

Current Diyarak Market modules:

- `Diyarak.Market.Property` — domain behavior covered by tests for aggregate identity, optional legacy-safe management ownership, persisted version restoration, full detail replacement while preserving ownership and version, property address validation and equality, optional geographic location, living, usable, sales, and total areas, room-count validation, furnishing quality, property features, building years, parking-space count, and commercial-property subtype rules.
- `Diyarak.Market.Company` — foundation scaffold; no tests yet.
- `Diyarak.Market.Listing` — domain behavior covered by tests for Listing identity, required publisher ownership, required subject-reference assignment, unsupported subject-type rejection, persisted version restoration, initial `Draft` status, required publication data, the `Draft` to `Published` transition, rejection of repeated publication and post-publication core-data changes, publishing-role and transaction-intent value sets, `ListingContext`, `ListingPrice`, `ListingHeadline`, optional `ListingAvailableFromDate` assignment and clearing while Draft, and the stable `market.property` subject-type value.

Empty test projects are not created. Tests are added when concrete domain behavior is implemented.
