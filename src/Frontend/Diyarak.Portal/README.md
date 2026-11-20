# Diyarak Portal

Arabic, right-to-left public portal for Diyarak listings, built with Next.js (App Router) and
rendered on the server for search-engine visibility. See ADR-0054.

## Pages

- `/`: search form and latest listings.
- `/search`: filters (offer type, category, governorate, district, price and currency, legal
  title, finishing, bedrooms) and paginated results.
- `/listings/{id}`: photo gallery, property facts, features and price.

Photos are served from the portal's own origin: `/media/listings/{id}/photos/{photoId}`
proxies to the API's public photo route.

## Development

```bash
npm install
DIYARAK_API_URL=http://localhost:5080 npm run dev
```

- `DIYARAK_API_URL`: server-side base URL of `Diyarak.Api` (default `http://localhost:5080`).
- `PORTAL_PUBLIC_URL`: public origin used for absolute Open Graph image URLs
  (default `http://localhost:3000`).

## Checks

```bash
npm test          # Vitest: labels, search query mapping, formatting
npm run typecheck
npm run build
```

`tests/labels.test.ts` reads the C# enum sources and fails when a backend value has no Arabic
label.
