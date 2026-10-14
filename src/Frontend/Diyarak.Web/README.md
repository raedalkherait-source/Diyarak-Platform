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

Collection URLs restore `city`, `postalCode`, `transactionIntent`, `propertyCategory`, `commercialSubtype`, `furnishingQuality`, repeated `propertyFeature`, `page`,
`pageSize`, `priceMin`, `priceMax`, `priceCurrency`, `livingAreaMin`, `livingAreaMax`, `livingAreaUnit`, `totalRoomsMin`, `totalRoomsMax`, `bedroomCountMin`,
`bedroomCountMax`, `bathroomCountMin`, `bathroomCountMax`, `constructionYearMin`, `constructionYearMax`,
`parkingSpaceCountMin`, and `parkingSpaceCountMax`, including their filter form values. Applying filters, clearing,
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

## Living area range filters

Optional `livingAreaMin` and `livingAreaMax` accept non-negative decimal bounds;
one bound is sufficient and minimum must not exceed maximum. Choose
`livingAreaUnit`: SquareMeter, SquareFoot, Hectare, or Dunum. A unit alone
is not a valid range. Values are sent as entered; the backend compares areas
across units and excludes properties without a known living area.

Invalid form values show the existing inline alert without changing results
or the URL. The API remains authoritative for decimal precision and search
validation, including values restored from bookmarks. Area filters combine
with city, transaction, category, and price filters and persist through page
navigation, Refresh, Retry, reload, and detail return links. Clear resets
all area controls and dismisses validation. Controls are disabled during
loading and the form stays hidden in the detail view.

## Room count range filters

The form sends optional `totalRoomsMin`/`totalRoomsMax`,
`bedroomCountMin`/`bedroomCountMax`, and `bathroomCountMin`/`bathroomCountMax`.
Each range accepts one or both bounds. Bounds are non-negative and minimum
must not exceed maximum. Total rooms supports decimals; bedrooms and bathrooms
require whole numbers within the API's Int32 range (0 to 2147483647).
Zero is sent explicitly, while blank inputs are omitted. No rounding occurs.

These filters combine with discovery, price, and area filters. The backend
excludes listings missing the count being filtered. Invalid form values show
an inline alert without clearing results or changing the URL. Supported values
from bookmarks remain subject to API validation. All six controls restore
from URLs, persist in pagination and detail return links, reset with Clear,
and stay disabled during loading. Refresh retains applied filters and returns
to page one; Retry repeats the failed request.

## Construction year and parking filters

Optional `constructionYearMin`/`constructionYearMax` and
`parkingSpaceCountMin`/`parkingSpaceCountMax` accept one or both bounds.
Years must be whole numbers from 1; parking counts must be whole numbers from
0. Both use the API Int32 upper bound of 2147483647, and minimum must not
exceed maximum. No current-year limit is added to the API contract.
Blank values are omitted; parking zero is transmitted explicitly.

Filters combine with existing discovery, price, area and room ranges.
The backend excludes missing values for the field being filtered. Inline
validation retains current results and URL. Bookmarks remain subject to
API validation. All four fields restore from URLs and persist through page
links, Refresh, Retry and detail return links. Clear resets them; loading
disables the form and details keep it hidden.

## Furnishing quality filter

The optional multiple-choice `furnishingQuality` filter uses the existing API
values Simple, Normal, Upscale, and Luxury. No selection omits the parameter.
The backend filters the property's furnishing quality; properties without a
matching value are excluded when a quality is selected.

This filter combines with all existing discovery and numeric filters.
Applying starts on page one; the choice restores from bookmarks and persists
through server page links, Refresh, Retry, and detail return navigation.
Clear deselects all qualities. Unsubmitted edits leave applied search intact.
Loading disables the form, which remains hidden on details. The API validates
supported query values; no ranking or new backend behavior is introduced.

## Commercial property type filter

Optional `commercialSubtype` uses OfficeOrPractice, RestaurantOrHotel,
HallOrProduction, Retail, or CommercialPlot. No selection omits the
parameter. Selecting subtypes searches commercial properties matching any
selected subtype. Categories remain the user's choices; excluding CommercialProperty can
produce an empty intersection. The help text explains this behavior.

This multiple-choice filter combines with existing discovery and numeric
filters, restores from bookmarks and persists through pagination, Refresh,
Retry, and detail return links. Clear resets it; loading disables the form
and details keep it hidden. Unsubmitted edits do not change applied search.
The API validates supported values; no backend changes are introduced.

## Postal code filter

The optional text field sends `postalCode` using the existing API contract.
Surrounding whitespace is trimmed; blank values omit the parameter.
Leading zeros, letters, internal spaces and punctuation are preserved and
URL-encoded. The backend matches the entire value without letter-case
sensitivity; wildcard characters are treated literally. No numeric coercion
or country-specific format restriction is added.

Postal code combines with city and all existing filters. Applying starts
at page one; the field restores from bookmarks and persists through server
page links, Refresh, Retry and detail return navigation. Clear resets it.
Unsubmitted edits do not change applied search; loading disables the form
and detail views keep it hidden. The API validates supported query values.

## Required property features

Ten labeled checkboxes use the existing `propertyFeature` API parameter.
Each selected feature becomes a separate repeated parameter (not a comma list).
The API requires all selected features; no selection omits this filter.
The choices use the same known enum values and display labels as property cards.

Selections combine with other filters, restore from repeated URL parameters,
and persist through pagination, Refresh, Retry, reload and detail return links.
Clear unchecks every feature. Unsubmitted edits leave applied search unchanged.
The outer disabled fieldset disables checkboxes during loading, and detail views
hide the form. Unknown bookmark values remain subject to API validation;
only supported values are shown as checkbox choices. Applying the form sends
only its supported selected choices. No backend changes are introduced.

## Applied search filter summary

An accessible text summary above the collection shows the filters in the
current API request, using public enum labels and all repeated feature values.
Numeric bounds preserve their entered strings, including zero and fractions.
Page and page size are not filters and do not appear in the summary.
Unfiltered searches show No filters applied.

The summary follows bookmarks, applied form values, server page links,
Refresh and Retry. It describes the requested search even when loading or
when the API rejects it; it does not claim that results loaded successfully.
Unsubmitted form edits and invalid submissions leave the summary unchanged.
Clear restores No filters applied. Details hide the summary. All content is
assigned with textContent, including city and postal values; query content
cannot create markup. No new API behavior or navigation state is introduced.

## Listings per page

An optional numeric `pageSize` control follows the public collection contract:
whole numbers from 1 to 100, blank for the default of 20. Applying sends the
entered size with all filters and starts at page one. Bookmarks restore any
supported size. Clear resets both filters and size to the unfiltered default.

The size persists through server page links, Refresh, Retry, reload and detail
return navigation. Unsubmitted edits leave the applied request unchanged.
Invalid form values retain results, URL and filter summary. Supported bookmark
parameters remain subject to API validation. Loading disables the field and
details hide it with the form. Page size remains excluded from the filter
summary because it controls pagination rather than matching properties.

## Go to listing page

The separate Go to page form changes only `page` on the current requested
collection query, retaining filters, repeated feature values and page size.
The input accepts whole numbers from 1 to the API Int32 limit; the API remains
authoritative for the page-size-dependent skip limit. No total or last page
is inferred. Empty pages use the normal empty result message and any server
previous-page link. Refresh still returns to the first page of applied search.

Unsubmitted filter and size edits are ignored. Invalid page input leaves
results, URL and summary intact and shows a separate alert. The form is disabled
while loading, hidden on details, and available after failures so users can
correct a rejected page. Retry repeats the same failed request. The input
tracks the requested page and then the API's returned page. Applying filters
or Clear resets the page through the normal collection request.

## Public request timeout

Collection and detail requests have a 15-second deadline covering fetch and
JSON body reading. Timeout aborts the request, shows a specific message and
re-enables Refresh, filters and page navigation controls as appropriate.
Retry repeats the exact failed page/search or listing. Timeout does not
clear search state from the URL or change applied filter semantics.

Timers are cleared after success, HTTP failure, network failure and invalid
JSON. Late completion cannot render stale data or change controls after a
new request begins. HTTP statuses and ordinary network failures retain their
existing messages; no automatic retry or backend changes are introduced.

## Loading and validation accessibility

The live results region uses `aria-busy` while collection or detail requests
are pending. Success, empty results, HTTP errors, network failures and timeout
all clear busy state. Invalid detail identifiers leave the region ready.
This allows assistive technology to defer partial result announcements.

Inline filter and page-jump validation alerts are programmatically focusable
with `tabindex=-1`. When JavaScript validation rejects a submission, focus moves
to the corresponding visible alert so keyboard and screen-reader users can
find its message. The alerts do not add normal Tab stops. Browser-native input
constraints continue to provide their own validation. Successful requests,
Refresh, Retry and normal navigation do not move focus. Search state, request
semantics and timeout behavior remain unchanged.

## Public response validation

Successful collection responses must be objects with an item array of objects,
integer page (1 to Int32 maximum), integer page size (1 to 100), boolean hasMore,
and no more items than page size. Missing or malformed collection metadata
shows an invalid-response message instead of an empty result or undefined page.
Detail responses must be objects with a valid listingId matching the requested
identifier (case-insensitive). Optional display fields retain existing Unknown
or unavailable fallbacks; this is not a full replacement for API schema validation.

Malformed JSON and rejected shapes offer Retry for the exact request, release
busy state and keep applied URL/search intact. Collection navigation remains
disabled until a valid response supplies links. The whole collection is checked
before any cards are appended, preventing partial rendering. HTTP and timeout
handling remain unchanged. No API or database changes are introduced.

## Decimal range comparison

Minimum/maximum comparisons use normalized decimal strings instead of converting
bounds to JavaScript Number. This preserves ordering for very close fractions
and large values that binary floating-point would otherwise treat as equal.
Leading zeros and trailing fractional zeros compare by value; forms such as
.5, 0.50 and 000.500 remain equivalent. Zero and a trailing decimal point are
supported by the existing input syntax.

The helper is used only after syntax validation and compares non-negative
bounds. Input strings are still sent unchanged after trimming; no rounding or
conversion is introduced. The API remains authoritative for decimal precision
and allowed magnitude. Rendering of API numeric values is unchanged.

## Multiple transaction types

Transaction intent now uses a native multiple select with Rent, Sell and
RentForLimitedPeriod. Select one or more options, or none for any transaction.
Each selection is sent as a repeated `transactionIntent` parameter. The backend
matches any selected intent, unlike required property features which require
all selected features. The help text explains mouse and keyboard selection.

Bookmarks restore all supported choices. Page links, Refresh, Retry, reload
and detail return navigation preserve repeated values; the applied summary
shows all selected labels. Applying sends choices in control order, once per
option, and retains other filters and size. Clear deselects all options.
Unsubmitted edits leave the applied search intact. The outer disabled fieldset
disables selection during loading, and details hide the form. Unknown bookmark
values remain API-validated and are not added as choices. No backend changes.

## Multiple property categories

Property category uses a native multiple select with all six existing API
categories. No selection means any category. Each selected option becomes
a repeated `propertyCategory` parameter and the backend matches any chosen
category. Selection help describes Ctrl/Command-click and Shift-arrow use.

Bookmarks restore all supported categories; page links, Refresh, Retry,
reload, direct page jumps and detail return links preserve repeated values.
The applied summary shows all selected category labels. Applying sends each
selected option once in control order, alongside other filters and page size.
Clear deselects all options. Loading disables the form; details hide it.

A commercial subtype still restricts results to matching commercial properties.
If categories are selected, CommercialProperty must be among them to allow
those matches. Other categories alone produce an empty intersection according
to the API. Unknown bookmark values remain API-validated and are not added to
the options. No API or database changes are introduced.

## Multiple furnishing qualities

The furnishing select supports one or more of Simple, Normal, Upscale and
Luxury. Each selection becomes a repeated `furnishingQuality` parameter.
The API matches any selected quality; required property features still require
all selected features. No selection leaves furnishing quality unrestricted.

Bookmarks restore all supported choices. Pagination, Refresh, Retry, reload,
page jumps and detail return preserve repeated values; the applied summary
shows all selected labels. Applying sends each chosen option once in control
order alongside other filters and page size. Clear deselects all options.
Loading disables the form; details hide it. Unknown bookmark values remain
API-validated and are not added as options. No backend changes are introduced.

## Multiple commercial property types

Commercial type supports one or more of OfficeOrPractice, RestaurantOrHotel,
HallOrProduction, Retail and CommercialPlot. Each selected option becomes a
repeated `commercialSubtype` parameter. The API matches any selected subtype
on commercial properties. No selection leaves subtype unrestricted.

Bookmarks restore all supported choices. Pagination, Refresh, Retry, reload,
page jumps and detail return retain repeated values. The summary shows all
selected labels. Applying sends each selected option once in control order
alongside other filters and page size. Clear deselects all options; loading
disables the form and details hide it. Unknown bookmark values remain
API-validated and are not added as options.

If categories are also selected, include CommercialProperty to allow subtype
matches; other categories alone produce an empty intersection. Required
property features still require every selected feature. No backend changes.

## Restore applied filters

Restore applied filters discards unsubmitted form edits and restores all fields
from the current requested collection search, including multi-select choices,
required features, numeric bounds and page size. It dismisses filter validation
without changing results, URL, summary, current page, or issuing an API request.
It does not clear the applied search. Clear filters retains its separate role
of loading the unfiltered first page with default size.

The button shares loading disablement with the form and is hidden on details.
After a failed request it restores that requested search; Retry still repeats
the exact failure. Applying restored fields uses normal form validation and
starts at page one. Unknown query values remain subject to API validation;
only supported choices can be shown in native selects. Bookmark initialization,
Clear and Restore use the same field restoration helper.

## Unapplied filter changes

A polite live message appears when input/change events reveal differences
from the form values last restored or submitted. It covers text, price/area,
page size, numeric ranges, multiple selects and required feature checkboxes.
Returning fields to that same snapshot hides the message. Draft detection
compares form values, so whitespace edits also count as changes.

Applying valid filters records the submitted form and dismisses the message,
even when the request later fails: the search now describes that request.
Invalid submissions retain the draft notice. Restore and Clear dismiss it.
Refresh, Retry, pagination and page jumps do not apply draft edits or discard
the notice. Loading ignores form events; details hide the form. The notice
never changes URL, summary, cards, requests or keyboard focus.
