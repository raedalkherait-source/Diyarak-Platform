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

Transaction intent uses native checkboxes for Rent, Sell and
RentForLimitedPeriod. Check one or more choices, or leave all unchecked for any transaction.
Each selection is sent as a repeated `transactionIntent` parameter. The backend
matches any selected intent, unlike required property features which require
all selected features. Each checkbox has a visible label within a fieldset
with a Transaction intent legend. Use Tab to move between choices and Space
to toggle; pointer/touch selection does not require modifier keys.

Bookmarks restore all supported choices. Page links, Refresh, Retry, reload
and detail return navigation preserve repeated values; the applied summary
shows all selected labels. Applying sends choices in control order, once per
choice, and retains other filters and size. Clear unchecks all choices.
Unsubmitted edits leave the applied search intact. The outer disabled fieldset
disables selection during loading, and details hide the form. Unknown bookmark
values remain API-validated and are not added as choices. No backend changes.

## Multiple property categories

Property category uses native labeled checkboxes for all six existing API
categories. Leaving all unchecked means any category. Each checked choice becomes
a repeated `propertyCategory` parameter and the backend matches any chosen
category. Use Tab to move between choices and Space to toggle; no modifier
keys are needed for pointer or touch selection.

Bookmarks restore all supported categories; page links, Refresh, Retry,
reload, direct page jumps and detail return links preserve repeated values.
The applied summary shows all selected category labels. Applying sends each
checked choice once in control order, alongside other filters and page size.
Clear unchecks all choices. Loading disables the form; details hide it.

A commercial subtype still restricts results to matching commercial properties.
If categories are selected, CommercialProperty must be among them to allow
those matches. Other categories alone produce an empty intersection according
to the API. Unknown bookmark values remain API-validated and are not added to
the checkbox choices. No API or database changes are introduced.

## Multiple furnishing qualities

The furnishing checkboxes support one or more of Simple, Normal, Upscale and
Luxury. Each selection becomes a repeated `furnishingQuality` parameter.
The API matches any selected quality; required property features still require
all selected features. No selection leaves furnishing quality unrestricted.

Bookmarks restore all supported choices. Pagination, Refresh, Retry, reload,
page jumps and detail return preserve repeated values; the applied summary
shows all selected labels. Applying sends each checked choice once in control
order alongside other filters and page size. Clear unchecks all choices.
Loading disables the form; details hide it. Unknown bookmark values remain
API-validated and are not added as choices. No backend changes are introduced.

## Multiple commercial property types

Commercial type checkboxes support one or more of OfficeOrPractice, RestaurantOrHotel,
HallOrProduction, Retail and CommercialPlot. Each checked choice becomes a
repeated `commercialSubtype` parameter. The API matches any selected subtype
on commercial properties. No selection leaves subtype unrestricted.

Bookmarks restore all supported choices. Pagination, Refresh, Retry, reload,
page jumps and detail return retain repeated values. The summary shows all
selected labels. Applying sends each checked choice once in control order
alongside other filters and page size. Clear unchecks all choices; loading
disables the form and details hide it. Unknown bookmark values remain
API-validated and are not added as choices.

If categories are also selected, include CommercialProperty to allow subtype
matches; other categories alone produce an empty intersection. Required
property features still require every selected feature. No backend changes.

## Restore applied filters

Restore applied filters discards unsubmitted form edits and restores all fields
from the current requested collection search, including checkbox selections,
required features, numeric bounds and page size. It dismisses filter validation
without changing results, URL, summary, current page, or issuing an API request.
It does not clear the applied search. Clear filters retains its separate role
of loading the unfiltered first page with default size.

The button shares loading disablement with the form and is hidden on details.
After a failed request it restores that requested search; Retry still repeats
the exact failure. Applying restored fields uses normal form validation and
starts at page one. Unknown query values remain subject to API validation;
only supported choices can be shown in native checkboxes. Bookmark initialization,
Clear and Restore use the same field restoration helper.

## Unapplied filter changes

A polite live message appears when input/change events reveal differences
from the form values last restored or submitted. It covers text, price/area,
page size, numeric ranges, multiple-choice checkboxes and required feature checkboxes.
Returning fields to that same snapshot hides the message. Draft detection
compares form values, so whitespace edits also count as changes.

Applying valid filters records the submitted form and dismisses the message,
even when the request later fails: the search now describes that request.
Invalid submissions retain the draft notice. Restore and Clear dismiss it.
Refresh, Retry, pagination and page jumps do not apply draft edits or discard
the notice. Loading ignores form events; details hide the form. The notice
never changes URL, summary, cards, requests or keyboard focus.

## Collapsible search filters

The existing form is inside a native details/summary panel titled Search
filters, open by default. Activate its summary with a pointer or keyboard to
collapse/expand the controls. Native browser semantics communicate expanded
state; no extra click handler or stored preference is introduced.

The applied filter summary, listing status, pagination and results remain
outside the panel. Collapsing does not submit, clear or restore filters, and
retains draft edits and messages for reopening. The entire panel is hidden on
detail views. All request, validation and search-state behavior is unchanged.

## Edit filters from search results

The Edit search filters button beside the applied-search summary opens the
filter panel and focuses City. Keyboard users can continue through the form
with Tab. Opening the panel preserves draft values, validation messages,
applied filters, results and the current page without making a request.
The button is disabled while loading, available after empty/error responses,
and hidden on listing details. Initial loading never moves focus to the form.

## Copy the applied search link

Copy search link copies an absolute local frontend URL with the requested
filters, repeated selections, page and page size. Draft edits and unsupported
URL parameters are excluded. It does not submit filters, fetch listings,
change browser history or move focus. It shares the search, not a snapshot of
listing data; available listings may change when the link is opened later.

A polite status confirms copying. If the browser clipboard is unavailable or
permission is denied, the same URL is displayed as text for manual copying.
The button is hidden on details and disabled during loading or a pending copy.
Starting a new listing request clears the message; an older pending copy
cannot show a stale confirmation after navigation.

## Browser Back and Forward

Apply, Clear, previous/next page, direct page jumps and Refresh record changed
search URLs in browser history. Initialization, Retry and repeating the same
URL replace the current entry instead of adding duplicates. Invalid form
submissions and editing/restoring/copying filters do not add history entries.

Back/Forward restores supported filters, repeated selections, page size and
page, clears draft validation state, and fetches the selected search without
adding a new history entry or moving focus. Refresh then uses the restored
search's first page. History restoration replaces any unapplied form edits.

If navigation occurs during loading, the old fetch is aborted and the latest
history destination is loaded. Superseded responses do not render cards or
errors. Empty/failed searches remain navigable and Retry uses their URL.
Detail links still use normal document navigation with the return-search URL.

## Remove applied filter groups from results

Buttons beneath the applied-search summary remove city, postal code, transaction
intent, property category, commercial type, furnishing quality, price range,
living area range, each numeric range, or required features independently.
Removing a multiple-choice group removes all selections in that group. Price
and area bounds are removed together with their currency/unit, avoiding an
incomplete range. Other applied filters and page size are retained.

Removal replaces unapplied edits with the remaining applied search, clears
validation messages and starts on page one. It creates a browser history entry,
so Back restores the previous search. Focus moves to the updated summary,
which remains available after the activated button is removed. Buttons are
disabled while loading, available after empty/error results, and hidden when
no filter groups are applied or when viewing listing details. Page/page size
alone do not create removal buttons.

## Checkbox filter groups

Transaction intent, property category, commercial type and furnishing quality
now all use native checkboxes with visible labels and fieldset legends. Each
group has associated help text. The outer fieldset disables them while loading.
The existing responsive checkbox layout is shared with required features.

Each checked value is sent once as a repeated parameter in the established
control order. Leaving a group unchecked omits that restriction. Matching stays
OR within these groups and AND across filter groups; required features still
require all selected values. No group is selected or cleared automatically
based on another group, including category/commercial-type intersections.

Bookmarks, draft detection, Restore/Clear, removal buttons, Copy search link,
browser Back/Forward and detail return links retain their existing behavior.

## Pagination below results

Previous/Next controls and the current page are available above and below the
listing cards. Both sets use the same validated server links, applied filters
and page size, and remain synchronized during loading, errors, Retry and
browser history navigation. Draft edits are not applied by either set.

The lower navigation has its own accessible landmark name. Only the original
page indicator announces live updates, avoiding duplicate page announcements.
Both sets are hidden on details and disabled when their links are unavailable.
An empty later page can still use its valid previous link from either set.

## Focus after page navigation

Activating either Previous/Next control or submitting a valid page jump moves
focus to the results heading after a successful response, including an empty
page. Native focus scrolling brings the heading into view without scripted
smooth animation. If that navigation fails, focus moves to the visible status
message instead, where the user can find the error and Retry control.

Both targets use tabindex=-1 and do not add ordinary Tab stops. Controls finish
updating before focus moves. Initial load, Refresh, Retry, Apply/Clear, filter
restoration and browser Back/Forward retain their existing focus behavior.
History traversal that cancels an in-flight page request does not let that
superseded request move focus. Invalid page jumps still focus their validation
message and do not make a request.

## Browser tab titles

Collection titles identify public listings, the applied city/postal code when
present, and page numbers above one. They follow page navigation, validated
response pages, browser history and filter removal. Unapplied form edits do
not change the title. Empty/failed searches retain their search context.

Detail titles use the loaded headline and public city/postal code. Missing
headlines fall back to Listing details. Invalid, unavailable and failed detail
requests have distinct fallback titles; refreshing a detail clears its old
headline until the new request completes. The brand suffix is always Diyarak.

Values are assigned through document.title as text. Display values have
collapsed whitespace and a limit of 80 Unicode code points per field; this
does not alter filters, URLs, API requests or rendered listing fields. The
static HTML title remains the fallback before JavaScript runs.

## Copy a public listing link

After a detail response is validated and displayed, Copy listing link shares
an absolute frontend URL containing only the listing identifier. The identifier
is normalized to lowercase; return-search filters and unrelated URL parameters
are excluded. Back to listings still retains the original return search.

A polite status confirms copying. Unavailable/denied clipboard access displays
the link as text for manual copying. Copying changes no data, history, title,
results or focus, and makes no API request. Repeated activation is blocked
while a copy is pending. The control is hidden in collection view and until a
detail loads successfully, including after invalid/not-found/failed requests.

Refreshing details clears the copy status and hides the control until the
new response succeeds. A copy that completes after refresh cannot display an
outdated confirmation or fallback URL. The search-copy control keeps its
existing collection behavior. Links may become unavailable if an advertisement
is subsequently unpublished.


## Empty listing results

A successful collection response with no items displays a message beside the
results. A filtered search offers Clear filters and show listings, resetting
the search and page size to their defaults. An empty later page offers Return
to first page, preserving applied filters and page size. Both actions discard
unapplied form edits, record the changed search in browser history, and focus
the results heading after success (or the error status after failure).

An empty first page without filters explains that no public listings are
available. Errors and timeouts retain the existing error/Retry behavior and
do not display an empty-result message. The panel is hidden during requests,
after nonempty results, and on detail pages.

## Print a public listing

Print listing appears after a valid detail response is displayed. It opens the
browser's print dialog for printing or saving a PDF, using the loaded data
without an API request or a change to the URL, search, history or title.
Canceling the dialog leaves the detail page available.

The print stylesheet retains Diyarak branding and the public listing's details
while hiding the hero introduction, filters, navigation, status and action
controls. It removes decorative card shadows and keeps individual fact rows
together where possible. Paper size and printer settings remain browser choices.
These styles apply only while a valid detail is displayed; refreshing clears
that state until the next response succeeds. The button is hidden for collection,
invalid, unavailable and failed detail views, and during refresh.
