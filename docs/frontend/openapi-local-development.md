# OpenAPI Local Development

## Purpose

This document explains how frontend development can inspect the backend OpenAPI document locally.

The API host registers OpenAPI and maps the OpenAPI endpoint in the Development environment.

The OpenAPI route is covered by:

- `tests/Hosts/Diyarak.Api.Tests/OpenApiEndpointTests.cs`

## Local endpoint

When `Diyarak.Api` runs in the Development environment, the OpenAPI document is available at:

- `/openapi/v1.json`

## Public Listing routes expected in OpenAPI

The OpenAPI document must expose:

- `GET /api/market/public/listings`
- `HEAD /api/market/public/listings`
- `GET /api/market/public/listings/{listingId}`
- `HEAD /api/market/public/listings/{listingId}`

These routes are required for anonymous public listing collection and detail frontend integration.

## Frontend usage

Frontend developers may use the OpenAPI document to:

- inspect available public routes;
- confirm route parameters;
- confirm HTTP methods;
- generate temporary local API clients;
- compare implementation behavior with `docs/market/public-listing-api-contract.md`.

The API contract document remains the human-readable source for public Listing response semantics.

## Environment behavior

The OpenAPI endpoint is intended for local Development usage.

Production exposure is not part of this increment.

## Compatibility rule

If public Listing routes change, update all of the following in the same change:

- OpenAPI route verification tests;
- `docs/market/public-listing-api-contract.md`;
- `docs/frontend/public-listing-integration.md`;
- this document.
