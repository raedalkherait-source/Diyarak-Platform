# Diyarak Modules

This directory contains business modules built on top of Platform Foundation and Platform Core.

Current Diyarak Market 1.0 structure:

- `Diyarak.Market.Property` — property module with an initial domain baseline covering aggregate identity, immutable internal management ownership for newly created assets with nullable legacy rehydration, supported top-level property categories, required property address, optional geographic location, optional living, usable, sales, and total areas, optional room counts, optional furnishing quality, optional property features, optional building years, optional parking-space count, and optional commercial-property subtype.
- `Diyarak.Market.Company` — company module foundation.
- `Diyarak.Market.Listing` — Market-specific listing module with an initial `Listing` aggregate using a required immutable sector-agnostic subject reference, currently accepting only the supported `market.property` subject type, starting as `Draft`, requiring `ListingContext`, `ListingHeadline`, and `ListingPrice` before publication, keeping `ListingAvailableFromDate` optional, restricting core publication-data changes to `Draft`, and supporting the explicit `Draft` to `Published` transition.

Additional Property behavior, Company domain models, Listing behavior, APIs, and dependencies are added only when supported by concrete requirements.

ADR-0034 adds immutable internal management ownership to newly created Market Property aggregates while preserving nullable ownership only for legacy rehydration; this is an authorization concept, not legal title.
