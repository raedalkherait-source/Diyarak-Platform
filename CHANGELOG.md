# Changelog

All notable changes follow Keep a Changelog conventions. Versions follow Semantic Versioning.

## [Unreleased]

### Added
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
