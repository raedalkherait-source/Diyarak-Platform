# Diyarak Hosts

Hosts compose the Diyarak application and contain no business rules.

Current host:

- `Diyarak.Api` — ASP.NET Core composition host for platform integrations and business modules.

ADR-0022 defines the future Market Listing publication contract as `POST /api/market/listings/{listingId}/publish`, with `204`, `400`, `404`, and `409` outcomes represented through HTTP status codes and RFC Problem Details where applicable.

The publication route is not currently mapped or exposed. It remains blocked until an explicit authentication and authorization model is accepted and configured.

Other Diyarak Market public and administrative endpoints remain pending concrete endpoint and authorization requirements.
