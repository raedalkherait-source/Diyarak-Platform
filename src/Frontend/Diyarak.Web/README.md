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

## Local assets only

The static frontend currently uses only local HTML, CSS, and JavaScript assets. It intentionally avoids CDN-hosted libraries or external runtime dependencies.

## External asset attributes

Because the static frontend uses only local assets, `index.html` should not include external asset markers such as `integrity` or `crossorigin` unless the architecture explicitly changes.

## Collection pagination

Previous and next buttons follow the API's `Link` header. Next also requires
`hasMore: true`. Links retain the API's page size and query parameters; the UI
shows the current page without inferring a total count or last page.
Only links to the same-origin public collection endpoint are accepted.
Navigation is disabled while loading and after failures. Retry repeats the
failed page request; Refresh listings returns to the first page.
Empty pages retain any previous-page link supplied by the API.

Run the frontend behavior tests with Node.js (no package installation needed):

```sh
node --test tests/Frontend/public-listing-pagination.test.cjs
```
