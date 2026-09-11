# Diyarak Application

This directory contains application-level use-case orchestration across Diyarak business modules.

Current Diyarak Market application structure:

- `Diyarak.Market.Application` — application orchestration for Market use cases.
- `PublishListingUseCase` receives Listing and authenticated actor user identifiers, loads the Listing, verifies creator ownership, verifies that the referenced Property exists, invokes `Listing.Publish()`, saves the resulting state, and returns a classified `Result`.
- `PublishListingErrors` defines stable validation, not-found, and conflict errors for expected publication outcomes.
- `IMarketListingRepository` is the Application-owned port used to load and save Market Listings.
- `IPropertyExistenceChecker` is the Application-owned port used for the Property existence check.

Application code may coordinate business modules but does not contain persistence implementations or Host composition logic.

The Host authenticates the caller and supplies a non-empty actor user identifier. An authenticated non-owner receives the same not-found result as a missing Listing; ownership is checked before Property existence, domain mutation, or persistence.

Expected publication failures are returned as classified errors. Unexpected persistence or infrastructure exceptions continue to the Host's exception-handling boundary.

The repository save operation is the current persistence commit boundary. No explicit transaction, lock, or isolation guarantee currently spans the Property existence check and Listing save.

The current Property publication-availability rule is existence only. Additional Property lifecycle or availability rules are not introduced until concrete requirements define them.
