# Public Property Projection Requirements

## Context

Anonymous public Listing detail and collection responses currently expose a reduced Listing projection
only. They include the public Listing identifier, context, headline, price, and optional
available-from date. They intentionally omit management-only data and internal subject identifiers.

Public Listing search filtering can already use selected Property data internally, but the public
Listing response still does not expose a Property projection. This document defines the first narrow
public Property projection increment for already-published Listings.

## Goals

- Add a small public `property` object to anonymous published Listing detail and collection
  responses.
- Reuse only Property fields that are already part of the current Property aggregate.
- Keep management ownership, concurrency, and internal subject-reference data private.
- Avoid exposing exact address or coordinate data in the first increment.
- Preserve the existing public Listing pagination, cache, ETag, HEAD, and Link semantics unless a
  later decision explicitly changes them.

## Public response shape

Each public Listing representation must include a nested `property` object.

The first MVP projection includes:

- `category`
- `location.city`
- `location.postalCode`
- `livingArea.value`
- `livingArea.unit`
- `totalRooms`
- `bedroomCount`
- `bathroomCount`
- `furnishingQuality`
- `features`
- `constructionYear`
- `commercialSubtype`
- `parkingSpaceCount`

Optional Property values remain nullable or empty in the public projection according to the existing
Property aggregate state.

## Explicit non-goals

This increment does not expose:

- Property identifier.
- Property `OwnerUserId`.
- Property `Version`.
- Listing `PublisherUserId`.
- Listing lifecycle `Status`.
- Listing internal `subjectId` or `subjectType`.
- Property street.
- Property house number.
- Exact geographic coordinates.
- Public Property detail endpoints.
- Public Property search independent of Listings.
- Ranking, recommendation, semantic sorting, total-count, or `last` page semantics.
- Contact, lead, favorite, analytics, view-counter, or agent/company presentation workflows.

## Visibility and fail-closed behavior

Public Property projection is available only through already-public published Listing resources.

A public Listing response must not expose a Property projection for Draft Listings, missing Listings,
or Listings that are not publicly visible under the existing published-Listing rules.

If a published Listing cannot be safely paired with its supported Property projection, the public read
path must fail closed rather than leaking internal identifiers or returning partial management data.

## Caching and validators

Successful public Listing detail and collection responses continue to use the existing public
revalidation policy.

Because the JSON representation changes when the Property projection changes, public ETag generation
must account for the Property projection version or an equivalent stable change signal before this
projection is enabled.

## Compatibility

This is an intentional public response expansion. Existing public Listing fields keep their current
names and meanings.

Clients must continue to treat omitted optional Property values as unknown rather than false.
