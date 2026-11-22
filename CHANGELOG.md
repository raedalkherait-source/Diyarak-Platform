# Changelog

All notable changes follow Keep a Changelog conventions. Versions follow Semantic Versioning.

## [Unreleased]

### Added
- Private listing notes: collapsible per-listing notepad on detail pages, stored
  in localStorage, never shared; indicator dot when a note exists.
- Neighborhood info section on listing detail pages: governorate description,
  contextual links to similar listings, area prices and the value estimator.
- Real-estate tools hub (`/tools`): mortgage payment calculator with sliders for
  principal, term and interest rate; property value estimator using live price
  report data per governorate and category. Footer link and sitemap entries.
- SEO structured data: BreadcrumbList JSON-LD on listing detail, contractors,
  projects, prices, FAQ and contact pages; WebSite JSON-LD with SearchAction
  for sitelinks search box; Organization JSON-LD on the home page.
- Accessibility: skip-to-content link, `focus-visible` outlines on all interactive
  elements, gallery keyboard navigation (arrow keys) with prev/next buttons and
  counter overlay, `alt` text on listing card images.
- Performance: lazy loading for below-fold listing card images, `prefers-reduced-motion`
  media query disabling animations and transitions.
- Contact page (`/contact`) with email, WhatsApp and a structured inquiry form.
- FAQ page (`/faq`) with 12 collapsible questions covering registration, publishing,
  payments, search, verification, privacy, reviews, comparison and currency.
- Footer navigation links to FAQ and contact pages.
- Sitemap expanded with `/faq` and `/contact`.
- Notification center: bell icon on the dashboard showing new listings from saved
  searches; read/unread tracking in localStorage.
- User settings page (`/my/settings`): preferred currency, exchange rate, default
  governorate and notification preferences, all persisted in localStorage.
- Listing comparison: select up to 4 listings to compare side-by-side in a table
  with all property details; persistent sticky bar and compare toggle on listing pages.
- Multi-currency price display: USD/SYP toggle on search results with user-editable
  exchange rate stored in localStorage; compare page respects the chosen currency.
- Theme toggle: manual light/dark/system switch in the header; respects
  `prefers-color-scheme` and persists choice in localStorage.
- Print styles: clean print layout for listing detail pages, hides navigation
  and interactive elements.
- CSS fix: added `--border` alias for `--line` so inline styles render borders
  correctly in both themes.
- Home page quick links: shortcuts to contractors directory, off-plan projects and price reports.
- Search history: recent searches saved locally and shown as chips on the home page.
- Share buttons on listing detail pages (WhatsApp, Telegram, copy link, native share).
- Recently viewed listings sidebar on listing detail page (client-side, localStorage).
- SEO: expanded sitemap with contractors, projects, prices and specialty filter pages;
  OpenGraph metadata on contractors, projects, prices and agency detail pages;
  JSON-LD structured data on project detail pages.
- User reviews: authenticated users rate contractors and agencies (1–5 stars with optional comment);
  one review per user per subject; public review list and average rating at
  `/api/market/public/reviews`; review form on contractor cards; authors can delete their own
  reviews; review section on agency detail page (ADR-0078).
- Contractors directory: users create a contractor profile with specialty, governorate and phone;
  public browsing at `/contractors` with governorate and specialty filters, management at
  `/my/contractor`; navigation link in header (ADR-0077).
- Area price reports: aggregated average, min and max prices per governorate and district,
  split by transaction intent and property category; public API at `/api/market/public/price-reports`,
  portal page at `/prices` with governorate drill-down; navigation link in header (ADR-0076).
- Off-plan projects: agencies list upcoming construction projects with unit count, price range
  and expected completion; public browsing at `/projects` with governorate filter, agency
  management at `/my/projects`; navigation link in header (ADR-0075).
- Progressive Web App (PWA): installable on home screen, offline fallback page,
  web app manifest with Arabic metadata and icons (ADR-0074).
- CSV bulk import for agencies: upload a CSV file to create properties and draft listings
  in bulk (max 200 rows). API: `POST /api/market/import/csv`, portal: `/my/import` (ADR-0073).
- Agency packages and listing quotas: moderators assign quota packages to agencies;
  `PublishListingUseCase` enforces the limit (default free quota: 5). Dashboard shows
  quota bar. API: `GET /api/market/quota`, moderation package CRUD (ADR-0072).
- Featured listings and payment orders: owners submit a payment request (bank transfer,
  Syriatel Cash, MTN Cash) to feature their listing for 7 or 30 days; a moderator confirms
  or rejects the payment; confirmed listings appear first in search results with a "★ مميز"
  badge. Catalog is configuration-driven (`Payments:Products:*`, `Payments:Methods:*`).
  Public response ETag bumped to v6 (ADR-0071).
- SMS alerts for saved searches: opt-in per search, at most one daily summary SMS per user
  within a daytime window, off by default (`SavedSearchAlerts:*`) (ADR-0070).
- Map-area search: `bbox=south,west,north,east` on public search (matched on the snapped grid
  so it cannot locate a building) and "ابحث في هذه المنطقة" on the search map (ADR-0069).
- Real-estate agency pages: agency profile at `/my/agency`, public `/agencies/{id}` with the
  agency's listings, moderator verification with a ✓ badge on the agency page and its listings;
  renaming removes verification (ADR-0068).
- Publisher statistics: daily views (bot-filtered beacon, owner views excluded), contact
  reveals and favorites per listing, with a 30-day chart on the listing editor and totals on
  `/my` (ADR-0067).
- Saved searches with in-site "new since last visit" counts (`/my/searches`, `/api/market/saved-searches`),
  listing publication time (`first_published_at`) and newest-first public search; portal search
  caches are invalidated when listings change (ADR-0066).
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
- Agency packages and listing quotas: moderators assign quota packages to agencies;
  `PublishListingUseCase` enforces the limit (default free quota: 5). Dashboard shows
  quota bar. API: `GET /api/market/quota`, moderation package CRUD (ADR-0072).
- Enterprise repository bootstrap targeting .NET 10 LTS.
- Foundation libraries, tests, CI/CD, development services, documentation, and governance files.
