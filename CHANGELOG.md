# Changelog

All notable changes follow Keep a Changelog conventions. Versions follow Semantic Versioning.

## [Unreleased]

### Added
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
