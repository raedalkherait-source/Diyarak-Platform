# Local Static Frontend Hosting

## Purpose

This document explains how to run the current static frontend scaffold through `Diyarak.Api` during local Development.

The frontend scaffold lives at:

- `src/Frontend/Diyarak.Web`

The API serves that folder only in the Development environment.

## Local frontend URL

When `Diyarak.Api` is running in Development, open:

- `/app`

The API serves:

- `/app` from `src/Frontend/Diyarak.Web/index.html`
- `/app/src/app.js` from `src/Frontend/Diyarak.Web/src/app.js`
- `/app/src/styles/app.css` from `src/Frontend/Diyarak.Web/src/styles/app.css`

## Why this exists

Node.js and npm are not required for the first static frontend increment.

Serving the frontend through `Diyarak.Api` gives the page the same origin as the API, so browser calls to `/api/market/public/listings` work without a development proxy.

## Current scope

The static frontend currently supports:

- loading anonymous public Listing collection items;
- rendering public Listing headlines;
- rendering public price state;
- rendering safe public city and postal-code location;
- rendering selected safe public Property facts.

The static frontend intentionally does not support:

- authentication;
- management workflows;
- contact or lead workflows;
- favorites;
- analytics;
- maps;
- exact coordinates;
- agent or company presentation.

## Backend requirements

The backend must continue to expose:

- `GET /api/market/public/listings`
- the public Listing response contract documented in `docs/market/public-listing-api-contract.md`.

## Verification

Static frontend hosting is covered by:

- `tests/Hosts/Diyarak.Api.Tests/FrontendStaticFileEndpointTests.cs`

Those tests verify that:

- `/app` serves the frontend index document in Development;
- `/app/src/app.js` serves the frontend JavaScript in Development.

## Compatibility rule

If the local frontend route changes, update all of the following in the same change:

- `src/Hosts/Diyarak.Api/Program.cs`
- `tests/Hosts/Diyarak.Api.Tests/FrontendStaticFileEndpointTests.cs`
- `src/Frontend/Diyarak.Web/README.md`
- this document.
