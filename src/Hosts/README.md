# Diyarak Hosts

Hosts compose the Diyarak application and contain no business rules.

Current host:

- `Diyarak.Api` — ASP.NET Core composition host for platform integrations and business modules.

Diyarak Market public and administrative API endpoints will be composed through the host when concrete endpoint and authorization requirements are defined. Administrative routes are not exposed without an explicit authentication and authorization model.
