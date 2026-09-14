# Diyarak Hosts

Hosts compose the Diyarak application and contain no business rules.

Current host:

- `Diyarak.Api` — ASP.NET Core composition host for platform integrations and business modules.

ADR-0030 defines authenticated `POST /api/market/properties` creation for the already modeled Property fields, returning `201 Created` with the server-generated Property identifier. ADR-0031 adds authenticated `GET /api/market/properties/{propertyId}` direct loading. ADR-0034 assigns the mapped internal actor as owner for every new Property and makes that direct read owner-only with concealed `404` for missing, non-owned, and legacy-unowned resources. ADR-0035 adds mapped-user-only `GET /api/market/properties` for the current owner's management collection, excluding other users' and legacy-unowned rows and without defining public Property discovery, pagination, or ordering.

ADR-0022 defines the Market Listing publication contract as `POST /api/market/listings/{listingId}/publish`, with `204`, `400`, `404`, and `409` outcomes represented through HTTP status codes and RFC Problem Details where applicable. ADR-0026 defines `POST /api/market/listings` for creating an actor-owned `Draft` Listing for an existing Property, returning `201 Created` on success.

ADR-0023 permits publication only for the authenticated creator identified by the Listing's immutable `PublisherUserId`. A non-owner receives the same `404 Not Found` response as a missing Listing.

Market Property creation/loading and Market Listing routes are mapped only when `Authentication:Enabled` is `true`. They require the ADR-0025 `mapped-user` authorization policy, which resolves the exact external `(iss, sub)` identity to an internal `User.Id`. Listing and Property orchestration receive only that mapped internal identifier as their actor; Property creation persists it as internal management `OwnerUserId`, never from request data. The Listing creation route never derives `PublisherUserId` from request data and, under ADR-0037, permits creation only when the mapped actor owns the referenced Property. Missing or invalid access tokens return `401`, valid but unmapped external identities return `403`, and mapped non-owners retain concealed `404` behavior for owner-controlled Listing operations and ADR-0034 Property direct loading. ADR-0035 Property collection reads are filtered to the mapped internal owner and return an empty array when no owned rows exist.

ADR-0029 adds authenticated owner-only `PATCH /api/market/listings/{listingId}` Draft editing with explicit Listing-version conflict handling. ADR-0032 adds creator-only `GET /api/market/listings/{listingId}` direct loading with the same concealed `404` behavior for non-owners. ADR-0033 adds mapped-user-only `GET /api/market/listings` for the authenticated creator's own management collection. Both reads use the internal ownership model and do not define public Listing detail, search, pagination, or discovery contracts.

Other Diyarak Market public and administrative endpoints remain pending concrete endpoint and authorization requirements.
