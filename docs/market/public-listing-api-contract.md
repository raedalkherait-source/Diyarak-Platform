# Public Listing API Contract

## Purpose

This document captures the anonymous public Listing response contract used by the frontend-facing Market API.

The contract exposes published Listing presentation data and a safe public Property projection only. It must not expose management, ownership, lifecycle, concurrency, or internal subject-reference data.

Paired API contract tests:

- `tests/Hosts/Diyarak.Api.Tests/GetPublishedListingEndpointTests.cs`
- `tests/Hosts/Diyarak.Api.Tests/ListPublishedListingsEndpointTests.cs`

## Endpoints

- `GET /api/market/public/listings/{listingId}` returns one published public Listing representation.
- `HEAD /api/market/public/listings/{listingId}` returns the same validators and cache headers without a body.
- `GET /api/market/public/listings` returns page metadata and public Listing items.
- `HEAD /api/market/public/listings` returns collection validators and cache headers without a body.

Missing, malformed, Draft, or otherwise non-public Listings are concealed according to the existing published Listing read rules.

## Collection response

The collection response contains these top-level fields:

- `page`
- `pageSize`
- `hasMore`
- `items`

Each object in `items` uses the same public Listing representation as the detail endpoint.

## Public Listing representation

The public Listing object currently exposes these top-level fields in the API contract tests:

- `listingId`
- `context`
- `headline`
- `price`
- `property`
- `availableFromDate`

`context` exposes:

- `publishingRole`
- `transactionIntent`

`price` exposes:

- `isOnRequest`
- `amount`
- `currency`

## Public Property representation

`property` exposes:

- `category`
- `location`
- `livingArea`
- `totalRooms`
- `bedroomCount`
- `bathroomCount`
- `furnishingQuality`
- `features`
- `constructionYear`
- `commercialSubtype`
- `parkingSpaceCount`

`property.location` exposes:

- `city`
- `postalCode`

`property.livingArea`, when present, exposes:

- `value`
- `unit`

## Fields that must remain private

The public Listing representation must not expose:

- Listing `PublisherUserId`.
- Listing lifecycle `Status`.
- Listing internal subject object.
- Listing internal `subjectId`.
- Listing internal `subjectType`.
- Listing concurrency `Version`.
- Property identifier.
- Property `OwnerUserId`.
- Property `Version`.
- Property street.
- Property house number.
- Exact geographic coordinates.

## Cache and validators

Successful public Listing detail and collection responses use the existing public revalidation policy.

ETags are opaque. They must not expose raw Listing identifiers or internal version values. The validator must change when the public Listing representation changes, including changes in the public Property projection.

## Compatibility

Existing public field names and meanings should not be changed silently. Any intentional contract change must update this document and the paired API contract tests in the same change.
