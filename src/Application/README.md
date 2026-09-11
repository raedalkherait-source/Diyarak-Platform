# Diyarak Application

This directory contains application-level use-case orchestration across Diyarak business modules.

Current Diyarak Market application structure:

- `Diyarak.Market.Application` — application orchestration for Market use cases.
- `PublishListingUseCase` loads an existing Market Listing by identifier, rejects a missing Listing, verifies that its referenced Property exists, invokes the Listing aggregate's `Publish()` behavior, and saves the resulting state.
- `IMarketListingRepository` is the Application-owned port used to load and save Market Listings.
- `IPropertyExistenceChecker` is the Application-owned port used for the Property existence check.

Application code may coordinate business modules but does not contain persistence implementations or Host composition logic.

The repository save operation is the current persistence commit boundary. No explicit transaction, lock, or isolation guarantee currently spans the Property existence check and Listing save.

The current Property publication-availability rule is existence only. Additional Property lifecycle or availability rules are not introduced until concrete requirements define them.
