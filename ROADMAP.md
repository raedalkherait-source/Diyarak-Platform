# Engineering Roadmap

1. **Foundation 1.0** — repository, primitives, shared kernel, building blocks, contracts.
   - Status: baseline complete.

2. **Platform Core 0.5** — identity, authorization, reference data, audit, media, relationship, listing, search, notification.
   - Status: structure complete. Identity has an implemented domain baseline, and Platform Listing has an initial sector-agnostic subject-reference baseline; remaining capabilities are foundation scaffolds pending concrete requirements.

3. **Diyarak Market 1.0** — property, company, and listing modules, public/admin APIs, and search.
   - Status: in progress. Property has an initial domain baseline covering aggregate identity, supported top-level categories, required property address, optional geographic location, optional living, usable, sales, and total areas, optional room counts, optional furnishing quality, optional property features, optional building years, optional parking-space count, and optional commercial-property subtype; Company remains a foundation scaffold. Market Listing has an initial aggregate baseline with Listing identity and a required immutable sector-agnostic subject reference that currently accepts only `market.property`, plus ListingContext for confirmed publishing roles and transaction intentions, ListingPrice for a known price or price on request, ListingHeadline for a non-empty listing headline, ListingAvailableFromDate for the confirmed available-from calendar date, and the stable Property subject-type value used with the Platform Listing subject-reference contract; The initial Listing lifecycle now supports creation as `Draft` and an explicit transition to `Published`; publication readiness, additional lifecycle states and transitions, API behavior, search, and additional market behavior remain pending concrete requirements.

4. **Sector Expansion** — services, materials, equipment, opportunities.
   - Status: not started.

5. **Projects and Property Management** — projects, units, management workflows.
   - Status: not started.
