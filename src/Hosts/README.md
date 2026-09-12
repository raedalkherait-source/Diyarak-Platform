# Diyarak Hosts

Hosts compose the Diyarak application and contain no business rules.

Current host:

- `Diyarak.Api` — ASP.NET Core composition host for platform integrations and business modules.

ADR-0022 defines the Market Listing publication contract as `POST /api/market/listings/{listingId}/publish`, with `204`, `400`, `404`, and `409` outcomes represented through HTTP status codes and RFC Problem Details where applicable. ADR-0026 defines `POST /api/market/listings` for creating an actor-owned `Draft` Listing for an existing Property, returning `201 Created` on success.

ADR-0023 permits publication only for the authenticated creator identified by the Listing's immutable `PublisherUserId`. A non-owner receives the same `404 Not Found` response as a missing Listing.

Market Listing routes are mapped only when `Authentication:Enabled` is `true`. They require the ADR-0025 `mapped-user` authorization policy, resolve the exact external `(iss, sub)` identity to an internal `User.Id`, and pass only that internal identifier into Application orchestration. The creation route never derives `PublisherUserId` from request data. Missing or invalid access tokens return `401`, valid but unmapped external identities return `403`, and mapped non-owners retain the concealed `404` required by ADR-0023 for publication.

Other Diyarak Market public and administrative endpoints remain pending concrete endpoint and authorization requirements.
