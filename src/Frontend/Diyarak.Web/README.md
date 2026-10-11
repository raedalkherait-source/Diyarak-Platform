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
failed page request; Refresh listings returns to the first page while retaining applied filters.
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
Refresh listing reloads the current detail. Back to listings restores the
collection search carried by the detail link. This view uses the existing development-only static hosting.

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

## Public field formatting

Known public enum values have explicit English display labels in both collection
cards and details. For example, `FittedKitchen` displays as Fitted kitchen and
`Sell` as For sale. Unknown or missing enum values display Unknown.
Area units display as m², ft², ha, or dunum without converting the stored value.
An absent or unknown unit is marked unit unknown. Room counts and areas follow
the browser's number locale, retain fractional values, and preserve zero.
Construction years remain ungrouped. Missing or invalid numeric values display
Unknown; empty feature lists display None listed. Price and date formatting follow the rules below.

## Price and available-date formatting

Only explicit `isOnRequest: true` displays Price on request. Missing, incomplete,
or invalid prices display Price unavailable. Known prices retain fractional
amounts with locale-aware currency formatting (up to JavaScript's numeric
precision), including zero. Currency codes follow the API's three-letter uppercase
contract. No currency conversion is performed.

`availableFromDate` is a calendar date, not an instant. Valid `yyyy-MM-dd` values
are formatted in UTC to preserve the same calendar day in every browser time zone.
Missing, malformed, or impossible dates display Unknown. Leap dates and the
DateOnly year range 0001 through 9999 are supported.

## Initial discovery filters

The collection form uses the existing anonymous API's `city`,
`transactionIntent`, and `propertyCategory` parameters. Empty inputs are omitted;
city whitespace is trimmed and all values are URL-encoded. Applying filters
starts at the first page. Refresh listings retains applied filters and starts
at the first page; Retry repeats the failed request. Server Link headers retain
filters during pagination. Clear filters resets the form and reloads the initial
unfiltered collection. Controls are disabled during loading; filters are hidden
on detail views. Unsubmitted form edits do not change the current search.

Filters and page coordinates are stored in the collection URL. Detail navigation
includes the return search, so Back to listings restores that page and its filters.
The form does not add ranking or total-count semantics.

## Shareable search and return navigation

Collection URLs restore `city`, `transactionIntent`, `propertyCategory`, `page`,
`pageSize`, `priceMin`, `priceMax`, and `priceCurrency`, including their filter form values. Applying filters, clearing,
or following server page links replaces the current URL without a document
reload. Browser reload/bookmarks preserve the current page; Refresh listings
still starts on page one with the applied filters. Unsubmitted form edits are
not written to the URL. Failed requests remain in the URL for retry/reload.

Card links include an encoded `returnSearch` query when there is collection
state to restore. Back to listings always points to the local index page and
uses only the supported collection parameters; arbitrary redirect destinations
are never accepted. Direct detail links without returnSearch return to the
initial collection. No local storage or extra entries per pagination click are
introduced. Unsupported URL parameters are ignored; the API validates supported
parameter values, including malformed page coordinates.

## Price range filters

Optional minimum and maximum prices use the existing `priceMin`, `priceMax`,
and `priceCurrency` API parameters. One bound is sufficient; both must be
non-negative and minimum must not exceed maximum. Any supplied bound requires
a three-letter currency code, normalized to uppercase. Currency alone is not
a valid price filter. Bounds are transmitted as entered without conversion.

Invalid form values show an accessible inline message and leave the current
results and URL intact. The API remains authoritative for decimal precision
and search validation. Price ranges exclude on-request prices, as defined by
the existing backend. Clear resets all price fields; price filters persist
in page links, shareable URLs, Retry, and return navigation from details.
