# Diyarak.Web

Static frontend scaffold for the Diyarak public listing experience.

This scaffold intentionally uses plain HTML, CSS, and JavaScript until Node.js tooling is installed.

Backend integration references:

- `docs/frontend/public-listing-integration.md`
- `docs/frontend/openapi-local-development.md`
- `docs/market/public-listing-api-contract.md`

Initial scope:

- public listing collection page;
- public listing detail page placeholder;
- safe rendering of public Listing and Property fields;
- no authentication;
- no contact, favorites, analytics, maps, or agent/company workflows.
## Development links

When `Diyarak.Api` runs in `Development`, the static frontend is available at:

- `/app` - static frontend shell.
- `/api/market/public/listings` - public listings JSON used by the frontend.
- `/openapi/v1.json` - OpenAPI document for local API contract inspection.
