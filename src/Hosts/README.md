# Diyarak Hosts

Hosts compose the Diyarak application and contain no business rules.

Current host:

- `Diyarak.Api` — ASP.NET Core composition host for platform integrations and business modules.

ADR-0022 defines the future Market Listing publication contract as `POST /api/market/listings/{listingId}/publish`, with `204`, `400`, `404`, and `409` outcomes represented through HTTP status codes and RFC Problem Details where applicable.

ADR-0023 permits publication only for the authenticated creator identified by the Listing's immutable `PublisherUserId`. A non-owner receives the same `404 Not Found` response as a missing Listing.

The publication route is not currently mapped or exposed. It remains blocked until an explicit authentication mechanism and claims mapping are accepted and configured.

Other Diyarak Market public and administrative endpoints remain pending concrete endpoint and authorization requirements.
