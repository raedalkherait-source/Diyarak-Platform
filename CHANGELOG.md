# Changelog

All notable changes follow Keep a Changelog conventions. Versions follow Semantic Versioning.

## [Unreleased]

### Added
- Maps: publishers can pin the property on a map; visitors see only an approximate 500 m area
  on the listing page and a map of search results. The public API exposes
  `property.location.approximate` snapped to a 0.004° grid, never the exact point. Tile provider
  configured with `MAP_TILE_URL`/`MAP_ATTRIBUTION` (ADR-0065).
- Favorites: signed-in users save published listings from the listing page and find them under
  `/my/favorites`; `GET|PUT|DELETE /api/market/favorites/{id}` and `GET /api/market/favorites`;
  migration `MarketListingFavorites` (ADR-0064).
- Draft listings can be deleted permanently, together with their photos and an otherwise unused
  property: `DELETE /api/market/listings/{id}?version=` and a portal button (ADR-0063).
- Portal: publishers can edit property details (location, area, rooms, floor, legal title,
  finishing, features) of a listing that is not closed. Fields the portal does not show are
  preserved, and edits made elsewhere in the meantime are reported instead of overwritten.
- `Messaging:Sms:Http:BasicAuthUsername` for SMS providers that use HTTP Basic authentication,
  such as Twilio API keys; a Twilio example in the development runbook and `api.env.example`.
- Long-lived sessions: rotating 30-day refresh tokens with reuse detection, refresh, logout and
  logout-all endpoints, and automatic renewal in the portal middleware (ADR-0062).
- Search-engine visibility: `robots.txt`, `sitemap.xml`, schema.org `RealEstateListing` data,
  canonical URLs, and `noindex` on deep filter combinations; listing photos served as resized
  AVIF/WebP (ADR-0061).
- Production container deployment: API, migrations and portal images, a single-server compose
  stack with Caddy HTTPS, PostgreSQL and scheduled backups, `ReverseProxy:KnownNetworks`, CI
  image builds, and an Arabic operations runbook (ADR-0060).
- Configurable HTTP SMS gateway (`Authentication:Phone:OtpSender = HttpGateway`,
  `Messaging:Sms:Http`) for production one-time codes; delivery failures return `503`
  (ADR-0059).
- Listing reports by visitors, a moderation queue, and moderator suspend, reinstate and dismiss
  actions; `/admin` in the portal; moderators configured by verified phone number (ADR-0058).
- Listing lifecycle after publication: edit price, headline, contact and photos; pause and resume;
  close as sold, rented or withdrawn (ADR-0057).
- Publisher dashboard in the portal: phone sign-in with an httpOnly session cookie, "my listings",
  a one-page listing form with Arabic validation, photo upload and deletion, and publishing
  (ADR-0056).
- Contact the publisher by phone call or WhatsApp using the publisher's verified number, revealed
  on demand with its own rate limit; portal contact button with a pre-filled WhatsApp message
  (ADR-0055).
- `ReverseProxy:KnownProxies` to trust `X-Forwarded-For` from known proxies only (ADR-0055).
- Arabic right-to-left public portal (`src/Frontend/Diyarak.Portal`, Next.js): home, search
  with Syrian filters, listing detail with photo gallery; server-rendered (ADR-0054).
- Listing photos (JPEG/PNG/WebP, up to 30 × 10 MB) with metadata stripping that removes EXIF
  GPS location, owner-only management, and immutable cacheable public photo URLs (ADR-0053).
- Phone-number sign-in with one-time codes and platform-issued access tokens:
  `POST /api/auth/phone/request` and `POST /api/auth/phone/verify` (ADR-0052).
- Syrian market Property details: governorate, district, legal title type, finishing level,
  floor number, building floor count, and local utility features (ADR-0051).
- Public Listing filters for governorate, district, legal title type, finishing level, and floor.

### Changed
- Published and Paused listings can be edited. Only Closed listings are read-only (supersedes the
  Draft-only rule of ADR-0029).
- Public listing ETags include photo identifiers.
- Property street, house number, and postal code are now optional (ADR-0051).

### Security
- Per-client (IP-partitioned) rate limiting on all API routes, with `Retry-After` (ADR-0050).
- Public Listing pagination is bounded to a 10,000-row result window (ADR-0050).
- `GET /` no longer discloses the hosting environment.
- `docker/.env` is no longer tracked.

## [1.0.0] - 2026-08-03

### Added
- Enterprise repository bootstrap targeting .NET 10 LTS.
- Foundation libraries, tests, CI/CD, development services, documentation, and governance files.
