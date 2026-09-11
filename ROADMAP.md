# Engineering Roadmap

1. **Foundation 1.0** — repository, primitives, shared kernel, building blocks, contracts.
   - Status: baseline complete.

2. **Platform Core 0.5** — identity, authorization, reference data, audit, media, relationship, listing, search, notification.
   - Status: structure complete. Identity has an implemented domain baseline, and Platform Listing has an initial sector-agnostic subject-reference baseline; remaining capabilities are foundation scaffolds pending concrete requirements.

3. **Diyarak Market 1.0** — property, company, and listing modules, public/admin APIs, and search.
   - Status: in progress. Property has an initial domain baseline covering aggregate identity, supported top-level categories, required property address, optional geographic location, optional living, usable, sales, and total areas, optional room counts, optional furnishing quality, optional property features, optional building years, optional parking-space count, and optional commercial-property subtype; Company remains a foundation scaffold. Market Listing has an initial aggregate baseline with Listing identity and a required immutable sector-agnostic subject reference that currently accepts only `market.property`, plus `ListingContext`, `ListingPrice`, `ListingHeadline`, and optional `ListingAvailableFromDate`. The initial Listing lifecycle supports creation as `Draft` and an explicit transition to `Published`; publication requires context, headline, and price, and core publication data can be edited only while the Listing is `Draft`. `Diyarak.Market.Application` owns the `IMarketListingRepository` and `IPropertyExistenceChecker` ports. `PublishListingUseCase` receives a Listing identifier, loads the aggregate, verifies the referenced Property, invokes `Listing.Publish()`, saves the resulting state, and returns classified validation, not-found, or conflict errors for expected failures. `Diyarak.Platform.Persistence.PostgreSql` provides full-state Property and Listing records, explicit domain mappings, the `market.properties` and `market.listings` migrations, the Property existence checker, and the Market Listing repository. ADR-0022 defines the future publication contract as `POST /api/market/listings/{listingId}/publish`, returning `204` on success and RFC Problem Details for `400`, `404`, and `409` failures. `Diyarak.Api` registers `PublishListingUseCase`, but the publication route remains unmapped until an explicit authentication and authorization policy is accepted and configured. Listing creation persistence, actor identity, ownership and publishing permissions, endpoint exposure, explicit transaction, locking, or isolation guarantees, optimistic concurrency, additional publication-readiness requirements, lifecycle states and transitions, search, and additional Market behavior remain pending concrete requirements.

4. **Sector Expansion** — services, materials, equipment, opportunities.
   - Status: not started.

5. **Projects and Property Management** — projects, units, management workflows.
   - Status: not started.
