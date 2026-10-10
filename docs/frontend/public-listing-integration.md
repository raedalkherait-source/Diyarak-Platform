# Public Listing Frontend Integration

## Purpose

This document describes how the frontend consumes the anonymous public Listing API.

The backend contract is documented in:

- `docs/market/public-listing-api-contract.md`

The frontend should treat that document and the paired API contract tests as the source of truth for response shape.

## Public collection

Use the collection endpoint for listing discovery pages:

- `GET /api/market/public/listings`

The response contains:

- `page`
- `pageSize`
- `hasMore`
- `items`

Each object in `items` is a public Listing card/detail-compatible representation.

Frontend behavior:

- Render `items` in the returned order.
- Use `hasMore` and the HTTP `Link` header for next-page navigation.
- Do not infer a total count.
- Do not infer a last page.
- Treat missing optional values as unknown.

## Public detail

Use the detail endpoint for public Listing pages:

- `GET /api/market/public/listings/{listingId}`

Frontend behavior:

- Use `listingId` from the collection item when navigating to detail.
- Treat `404` as not found or no longer public.
- Do not expose any management-only concepts in the UI.

## Listing fields

The frontend may display:

- `headline`
- `context.publishingRole`
- `context.transactionIntent`
- `price.isOnRequest`
- `price.amount`
- `price.currency`
- `availableFromDate`

Price behavior:

- When `price.isOnRequest` is `true`, show an on-request label instead of a numeric price.
- When `price.amount` or `price.currency` is `null`, treat the price as incomplete.

## Property fields

The frontend may display:

- `property.category`
- `property.location.city`
- `property.location.postalCode`
- `property.livingArea.value`
- `property.livingArea.unit`
- `property.totalRooms`
- `property.bedroomCount`
- `property.bathroomCount`
- `property.furnishingQuality`
- `property.features`
- `property.constructionYear`
- `property.commercialSubtype`
- `property.parkingSpaceCount`

Location behavior:

- Show city and postal code only.
- Do not expect street, house number, or exact coordinates from this API.

## Private fields

The frontend must not depend on private fields that are intentionally absent, including:

- Listing publisher user identifiers.
- Listing lifecycle status.
- Listing internal subject identifiers.
- Listing version values.
- Property identifiers.
- Property owner user identifiers.
- Property version values.
- Street and house-number fields.
- Exact coordinates.

## Caching

The API returns opaque ETags for successful public responses.

Frontend behavior:

- The frontend may use normal browser revalidation behavior.
- Do not parse or display ETag values.
- Do not treat ETags as public identifiers.

## Empty and nullable values

Optional values may be `null` or empty arrays.

Frontend behavior:

- Display unknown values gracefully.
- Do not render `null` as text.
- Do not treat omitted optional values as `false`.

## Initial UI scope

The first frontend increment can safely build:

- Public listing cards from collection items.
- Public listing detail pages.
- Basic next-page navigation.
- Safe city/postal-code location display.
- Safe public property facts display.

The first frontend increment should not build:

- Contact workflows.
- Favorites.
- Analytics.
- View counters.
- Agent or company presentation.
- Public property detail pages.
- Map pins requiring exact coordinates.
