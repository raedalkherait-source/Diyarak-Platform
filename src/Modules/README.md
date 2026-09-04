# Diyarak Modules

This directory contains business modules built on top of Platform Foundation and Platform Core.

Current Diyarak Market 1.0 structure:

- `Diyarak.Market.Property` — property module with an initial domain baseline covering aggregate identity, supported top-level property categories, required property address, optional geographic location, optional living, usable, sales, and total areas, optional room counts, optional furnishing quality, optional property features, optional building years, optional parking-space count, and optional commercial-property subtype.
- `Diyarak.Market.Company` — company module foundation.
- `Diyarak.Market.Listing` — Market-specific listing module with initial `ListingContext`, `ListingPrice`, and `ListingHeadline` value objects for confirmed listing context, price, and headline concepts.

Additional Property behavior, Company domain models, Listing behavior, APIs, and dependencies are added only when supported by concrete requirements.
