# Public Listing Search Requirements

This document records the confirmed requirements, implemented baseline, and remaining unresolved behavior for public discovery and filtering of published Diyarak Market Listings.

## Confirmed requirements

- Public search operates on published Market Listings; Draft Listings are not publicly discoverable.
- A Listing remains separate from the persistent Property asset that it publishes.
- The current Market Property subject type is `market.property`.
- Search requirements must not move persistent Property facts into the Listing domain merely because those facts are useful for discovery or presentation.
- Search may use confirmed Listing publication data and confirmed Property facts without changing their domain ownership.
- Public search must not expose management-only data such as Listing version, lifecycle status, `PublisherUserId`, Property `OwnerUserId`, or internal subject identifiers merely to support filtering.
- Public address visibility is a presentation decision and must not be inferred from the existence of persisted Property address fields.
- Existing bounded public pagination remains the baseline: positive `page`, bounded `pageSize`, deterministic technical ordering, and `hasMore`.
- Search-result filtering and search-result presentation are separate concerns. A Property fact may participate in filtering without automatically becoming part of the public response representation.
- Reference applications and screenshots are product-research inputs only. Their fields, labels, categories, ranking rules, and product-specific behavior are not Diyarak requirements unless explicitly confirmed here.

## Implemented baseline

- `GET /api/market/public/listings` anonymously returns a bounded page of `Published` Listings.
- `HEAD /api/market/public/listings` follows the same validation, visibility, pagination, caching, ETag, and navigation semantics as `GET` without returning a response body.
- The current collection accepts only `page` and `pageSize` query parameters.
- `ListPublishedListingsUseCase` validates pagination and requests one extra row to derive `hasMore` without a total-count query.
- `IPublishedListingQuery` currently exposes pagination through `ListPageAsync(skip, take)` and returns Listing domain objects.
- The PostgreSQL implementation filters `market.listings` to `Published`, orders by Listing identifier, applies `Skip`/`Take`, and does not currently join or filter `market.properties`.
- The current public collection response contains Listing identifier, publishing role, transaction intent, headline, price, and optional available-from date. It does not contain a public Property projection.
- Successful public collection representations use `Cache-Control: public, no-cache` and opaque ETag revalidation.
- Public collection navigation exposes requestable `prev` and `next` `Link` metadata based on `page`, `pageSize`, and `hasMore`.
- Existing pagination links preserve `pageSize` only; no search or filter parameters are currently defined or preserved.
- The current collection ordering by Listing identifier is a deterministic technical ordering, not a relevance, ranking, or business-sort contract.
- PostgreSQL currently has an index on `market.listings(status, id)` for the implemented published-collection access pattern.
- Persisted Property records already contain the current modeled Property facts, including category, address, optional coordinates, areas, room counts, furnishing quality, features, building years, commercial subtype, and parking-space count.
- Persisted Listing records identify their published subject through `SubjectId` and `SubjectType`; the confirmed Property subject type is `market.property`.

## Search MVP requirements

The first public Listing search increment is limited to filtering by data already owned by the existing Listing and Property models.

The MVP filter scope is:

- Transaction intent.
- Property category.
- Commercial Property subtype.
- City.
- Postal code.
- Listing price range with an explicit currency.
- Living-area range.
- Total room count.
- Bedroom count.
- Bathroom count.
- Furnishing quality.
- Property features.
- Construction year.
- Parking-space count.

### Transaction intent and Property category semantics

- `TransactionIntent` and `PropertyCategory` filters are optional. Omitting either filter does not restrict results by that dimension.
- Each filter may contain one or more values from its existing domain enum.
- Multiple values within the same filter use OR semantics.
- Different supplied filters combine using AND semantics.
- `TransactionIntent` accepts only the currently defined values: `Rent`, `Sell`, and `RentForLimitedPeriod`.
- `PropertyCategory` accepts only the currently defined values: `Apartment`, `House`, `Plot`, `InvestmentProperty`, `GarageOrParkingSpace`, and `CommercialProperty`.
- Unknown, malformed, or undefined enum values are invalid search input and produce `400 Bad Request`; they are not silently ignored.
- The search contract does not introduce aliases such as `Buy` for `Sell` or new categories derived from reference applications.

### Commercial Property subtype semantics

- `CommercialPropertySubtype` is an optional filter.
- The filter may contain one or more values from the existing `CommercialPropertySubtype` enum.
- Multiple subtype values use OR semantics.
- Supplying this filter restricts results to `CommercialProperty` subjects whose subtype matches one of the supplied values.
- `PropertyCategory=CommercialProperty` does not have to be supplied separately when a commercial subtype filter is present.
- If an explicitly supplied Property-category filter excludes `CommercialProperty` while a commercial subtype filter is also supplied, the valid but contradictory filter combination produces no matching results; it is not a malformed request.
- Accepted subtype values are `OfficeOrPractice`, `RestaurantOrHotel`, `HallOrProduction`, `Retail`, and `CommercialPlot`.
- Unknown, malformed, or undefined subtype values produce `400 Bad Request`.
- Search does not introduce additional commercial subtypes from reference applications.

### City and postal-code semantics

- `City` and `PostalCode` are optional filters.
- Each filter accepts at most one textual value in the MVP.
- Leading and trailing whitespace in a supplied search value is ignored.
- A supplied value that is empty or whitespace-only after trimming is invalid and produces `400 Bad Request`.
- City matching is a case-insensitive exact textual match; it is not substring, prefix, fuzzy, transliterated, or geocoded matching.
- Postal-code matching is a case-insensitive exact textual match; it is not prefix or range matching.
- When both City and postal code are supplied, both conditions must match.
- These filters operate on the persisted Property address but do not make the Property address part of the public Listing response.
- Street and house-number filtering are outside the MVP.
- The MVP does not perform city-name canonicalization, transliteration, geocoding, administrative-boundary expansion, radius search, or nearby-place resolution.

### Listing price and currency semantics

- `MinPrice` and `MaxPrice` are optional numeric filters over a known Listing price.
- Each supplied price bound must be greater than or equal to zero.
- Price bounds are inclusive.
- If both bounds are supplied, `MinPrice` must be less than or equal to `MaxPrice`; otherwise the request produces `400 Bad Request`.
- Supplying either price bound requires an explicit `PriceCurrency`.
- `PriceCurrency` uses the existing `Currency` contract: a three-letter alphabetic currency code normalized to uppercase.
- Supplying `PriceCurrency` without either `MinPrice` or `MaxPrice` is invalid and produces `400 Bad Request`.
- A numeric price filter matches only Listings with a known price in the requested currency.
- A Listing whose price is `OnRequest` does not match a numeric price-range filter.
- Listings with a known price in another currency do not match the filter.
- The MVP performs no currency conversion, exchange-rate lookup, or cross-currency comparison.
- `OnRequest` remains a distinct Listing-price state and must not be interpreted as zero, null/missing data, or an unbounded numeric price.
- Price filtering does not introduce cold-rent, total-rent, price-per-area, installment, or other price semantics not present in the current Listing model.

### Living-area semantics

- `MinLivingArea` and `MaxLivingArea` are optional numeric filters over the Property `LivingArea`.
- Each supplied area bound must be greater than or equal to zero.
- Area bounds are inclusive.
- Supplying either area bound requires an explicit `LivingAreaUnit`.
- `LivingAreaUnit` accepts only the existing `AreaUnit` values: `SquareMeter`, `SquareFoot`, `Hectare`, and `Dunum`.
- Supplying `LivingAreaUnit` without either `MinLivingArea` or `MaxLivingArea` is invalid and produces `400 Bad Request`.
- If both bounds are supplied, `MinLivingArea` must be less than or equal to `MaxLivingArea`; otherwise the request produces `400 Bad Request`.
- A Property whose `LivingArea` is absent does not match a supplied living-area filter.
- A Property whose `LivingArea` uses a different supported unit is compared after conversion according to the existing `Area` conversion semantics.
- Search does not change the persisted Property area or its original unit.
- This MVP filter applies only to `LivingArea`; it does not implicitly substitute `UsableArea`, `SalesArea`, or `TotalArea` when `LivingArea` is absent.
- Separate filtering semantics for usable, sales, and total area are outside this MVP.

### Room-count semantics

- `MinTotalRooms` and `MaxTotalRooms` are optional decimal filters over the Property `TotalRooms` value.
- `MinBedroomCount` and `MaxBedroomCount` are optional integer filters over the Property `BedroomCount`.
- `MinBathroomCount` and `MaxBathroomCount` are optional integer filters over the Property `BathroomCount`.
- Every supplied room-count bound must be greater than or equal to zero.
- All room-count bounds are inclusive.
- For each room-count dimension, when both minimum and maximum are supplied, the minimum must be less than or equal to the maximum; otherwise the request produces `400 Bad Request`.
- A Property whose corresponding room-count value is absent does not match a supplied filter for that dimension.
- Different supplied room-count dimensions combine using AND semantics.
- `TotalRooms` retains its existing decimal semantics; search does not round, truncate, or reinterpret it as an integer.
- Bedroom and bathroom counts retain their existing integer semantics.
- Search does not derive bedroom or bathroom counts from `TotalRooms`, or derive `TotalRooms` from the other counts.

### Furnishing-quality and Property-feature semantics

- `FurnishingQuality` is an optional filter that may contain one or more values from the existing `FurnishingQuality` enum.
- Multiple furnishing-quality values use OR semantics.
- Accepted furnishing-quality values are `Simple`, `Normal`, `Upscale`, and `Luxury`.
- A Property whose `FurnishingQuality` is absent does not match a supplied furnishing-quality filter.
- Unknown, malformed, or undefined furnishing-quality values produce `400 Bad Request`.
- `PropertyFeature` is an optional multi-value filter using the existing `PropertyFeature` enum.
- When multiple Property features are supplied, the Property must contain all supplied features; feature filtering uses ALL semantics rather than ANY semantics.
- Accepted Property features are `FittedKitchen`, `Elevator`, `BalconyOrTerrace`, `GuestToilet`, `GardenOrSharedGardenUse`, `Basement`, `StepFreeAccess`, `SuitableForVacationRental`, `HistoricMonument`, and `GarageOrParkingSpace`.
- A Property with no features does not match a non-empty Property-feature filter.
- Unknown, malformed, or undefined Property-feature values produce `400 Bad Request`.
- Repeating the same valid feature does not change the meaning of the filter.
- Search does not introduce additional furnishing levels or Property features from reference applications.

### Construction-year and parking-space semantics

- `MinConstructionYear` and `MaxConstructionYear` are optional integer filters over the Property `ConstructionYear`.
- Each supplied construction-year bound must be greater than or equal to `1`.
- Construction-year bounds are inclusive.
- If both construction-year bounds are supplied, `MinConstructionYear` must be less than or equal to `MaxConstructionYear`; otherwise the request produces `400 Bad Request`.
- A Property whose `ConstructionYear` is absent does not match a supplied construction-year filter.
- Construction-year filtering does not use `LastModernizationYear` as a substitute or additional condition.
- `MinParkingSpaceCount` and `MaxParkingSpaceCount` are optional integer filters over the Property `ParkingSpaceCount`.
- Each supplied parking-space-count bound must be greater than or equal to zero.
- Parking-space-count bounds are inclusive.
- If both parking-space-count bounds are supplied, `MinParkingSpaceCount` must be less than or equal to `MaxParkingSpaceCount`; otherwise the request produces `400 Bad Request`.
- A Property whose `ParkingSpaceCount` is absent does not match a supplied parking-space-count filter.
- Parking-space-count filtering is independent of the `GarageOrParkingSpace` Property feature and does not infer one from the other.

### Common search semantics

- Every Search MVP filter is optional. A request with no search filters preserves the existing published-Listing collection behavior.
- Different supplied filter dimensions combine using AND semantics unless a more specific rule above explicitly defines behavior within that dimension.
- Filtering is applied to the complete eligible published-Listing query before technical ordering, pagination, and `hasMore` evaluation.
- Pagination therefore operates over the filtered result set; filtering must not be applied only to an already paged collection.
- Only `Published` Listings are eligible regardless of supplied filters.
- Property-based filters apply to Listings whose subject is the confirmed `market.property` subject type and whose referenced Property satisfies the supplied conditions.
- A Listing cannot satisfy a Property-based filter merely because similarly shaped data exists elsewhere; filtering uses the referenced Property identified by the Listing subject reference.
- Valid filter combinations that happen to match no Listings return a successful empty page rather than a validation error.
- Malformed filter values, undefined enum values, invalid ranges, and invalid dependent-parameter combinations defined above produce `400 Bad Request`.
- Search validation occurs before persistence query execution where the invalidity can be determined from the request alone.
- Search does not mutate Listing or Property state.
- The existing deterministic Listing-identifier ordering remains the MVP ordering after filters are applied. It does not become a relevance or business-ranking contract.
- The MVP retains the existing `page`, `pageSize`, and `hasMore` pagination model and does not add a total count.
- Active defined search filters must be preserved in public collection `prev` and `next` navigation links.
- The unfiltered public collection remains backward-compatible with the existing public Listing response representation.

Filtering is the first increment. It does not by itself expand the existing public Listing response with Property data. Public Property/search-result presentation is specified separately.

The following capabilities are outside this MVP:

- Keyword or full-text search.
- Natural-language or AI search.
- Relevance ranking or recommendation.
- Semantic or user-selectable sorting.
- Nearby-place or point-of-interest filtering.
- Radius, polygon, or other map/geospatial search.
- Heating or energy-efficiency filters.
- Commission-related filters.
- Coming-soon behavior.
- Hidden-listing or favorite-based personalization.
- Total result counts.
- New Property or Listing attributes introduced solely for search.

## Search-result presentation

The first Search MVP increment does not introduce a new public Property projection or expand the existing public Listing collection JSON representation.

- Search results continue to use the existing public Listing response representation.
- Property data used to evaluate a search filter is not automatically exposed in the response.
- City and postal code may participate in filtering without exposing the Property address.
- Living area, room counts, furnishing quality, features, construction year, commercial subtype, and parking-space count may participate in filtering without becoming response fields.
- Internal Listing subject identifiers and Property ownership data remain absent from the public response.
- Search-result presentation observed in reference products, including primary images, area summaries, location summaries, favorite actions, maps, and richer Property facts, does not become part of this filtering increment.
- A future public Property or subject projection requires separate explicit requirements before the public Listing representation is expanded.
- Future presentation work must separately define address visibility and must not infer permission to expose street or house-number data from their persistence in the Property record.

## Remaining open requirements

The following behavior remains undefined and must not be invented:

- The exact HTTP query-parameter names and transport encoding for the confirmed Search MVP filters, including representation of multi-value filters.
- Whether the existing Application published-Listing query contract is evolved for filtering or a separate search/read-side contract is introduced.
- Whether future public search results expose a dedicated Property or subject projection.
- Public presentation of Property address data, including city, postal code, street, house number, and coordinates.
- Keyword and full-text search semantics.
- Natural-language or AI-assisted search semantics.
- Relevance ranking, recommendation, and business ordering.
- User-selectable sorting.
- Total-count semantics.
- Cursor, snapshot, first-page, last-page, or other pagination semantics beyond the existing page model.
- Radius, bounding-area, polygon, map, geocoding, nearby-place, and point-of-interest search.
- Search filters for `UsableArea`, `SalesArea`, `TotalArea`, or `LastModernizationYear`.
- Heating, energy-efficiency, commission, and other Property characteristics not present in the current confirmed model.
- Cold-rent, total-rent, price-per-area, installment, exchange-rate conversion, and other additional price models.
- Coming-soon discovery or any Listing lifecycle beyond the currently defined Draft and Published states.
- Hidden-listing, favorite, saved-search, or other user-personalization behavior.
- Search-result media and primary-image selection.
- Search-specific persistence indexes, denormalized projections, or dedicated search infrastructure beyond what is justified by the confirmed query contract and measured access patterns.
- Public search behavior for future Listing subject types other than the currently confirmed `market.property` subject.

These questions require separate product or architecture decisions and must not be inferred from reference applications, screenshots, incidental persistence shape, or incomplete future scaffolding.
