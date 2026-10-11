# Diyarak.Web

Static frontend scaffold for the Diyarak public listing experience.

This scaffold intentionally uses plain HTML, CSS, and JavaScript until Node.js tooling is installed.

Backend integration references:

- `docs/frontend/public-listing-integration.md`
- `docs/frontend/openapi-local-development.md`
- `docs/market/public-listing-api-contract.md`

Initial scope:

- public listing collection page;
- public listing detail view;
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

## Public listing details

Cards link to `/app/index.html?listingId=<listingId>`. This URL can be opened
or bookmarked directly. The detail view loads the anonymous detail endpoint
and reuses the safe public card renderer, adding publishing role, transaction
intent, furnishing quality, and commercial subtype. Optional values remain
unknown; no private fields are rendered.

Malformed identifiers do not trigger an API request. A 404 shows a not-found
message; HTTP and network failures offer Retry for the same listing.
Refresh listing reloads the current detail. Back to listings opens the initial
collection. This view uses the existing development-only static hosting.

The frontend behavior test file covers pagination and detail loading.

## Development content security policy

Development responses under `/app` allow scripts, styles, and API connections
from the same origin. Other resource types are blocked by `default-src 'none'`;
inline scripts/styles and external dependencies are not enabled. Frame embedding
and form submission are blocked. API and other routes retain the restrictive
policy; the static frontend remains unavailable in Production.

Open `http://localhost:<port>/app/` through the running API, rather than opening
HTML from disk. If the page remains unstyled or stuck on Ready, check the browser
console and Network panel for CSP violations and failed local asset requests.
