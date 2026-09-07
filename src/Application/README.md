# Diyarak Application

This directory contains application-level use-case orchestration across Diyarak business modules.

Current Diyarak Market application structure:

- `Diyarak.Market.Application` — application orchestration for Market use cases.
- `PublishListingUseCase` verifies that the Property referenced by a ready Market Listing exists before calling the Listing aggregate's `Publish()` behavior.
- `IPropertyExistenceChecker` is the Application-owned port used for the Property existence check.

Application code may coordinate business modules but does not contain persistence implementations or Host composition logic.

The current Property publication-availability rule is existence only. Additional Property lifecycle or availability rules are not introduced until concrete requirements define them.
