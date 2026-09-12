# Diyarak Application

This directory contains application-level use-case orchestration across Diyarak business modules.

Current Diyarak Market application structure:

- `Diyarak.Market.Application` — application orchestration for Market use cases.
- `CreateListingUseCase` receives a Property identifier and authenticated actor user identifier, verifies that the Property exists, creates an actor-owned `Draft` Listing, adds it through the repository, and returns the generated Listing identifier.
- `CreateListingErrors` defines stable validation and Property-existence errors for expected creation outcomes.
- `PublishListingUseCase` receives Listing and authenticated actor user identifiers, loads the Listing, verifies creator ownership, verifies that the referenced Property exists, invokes `Listing.Publish()`, and conditionally saves the resulting state only when the persisted status is still `Draft`.
- `PublishListingErrors` defines stable validation, not-found, publication-state, Property-existence, and concurrent-modification conflicts for expected publication outcomes.
- `IMarketListingRepository` is the Application-owned port used to add and load Market Listings and to conditionally save a Listing against an expected persisted status.
- `IPropertyExistenceChecker` is the Application-owned port used for the Property existence check.
- `IMarketTransactionRunner` is the Application-owned port that wraps persistence-sensitive Listing creation and publication work in an explicit transaction.

Application code may coordinate business modules but does not contain persistence implementations or Host composition logic.

The Host authenticates the caller and supplies a non-empty actor user identifier. An authenticated non-owner receives the same not-found result as a missing Listing; ownership is checked before Property existence, domain mutation, or persistence.

Expected creation and publication failures are returned as classified errors. Unexpected persistence or infrastructure exceptions continue to the Host's exception-handling boundary.

ADR-0027 makes the transaction boundary explicit. After required identifier validation, `CreateListingUseCase` and `PublishListingUseCase` execute their persistence-sensitive work through `IMarketTransactionRunner`. The PostgreSQL implementation commits successful results and rolls back expected failures or exceptions. ADR-0028 first adds optimistic publication concurrency. ADR-0029 introduces `UpdateListingUseCase` for authenticated owner-only Draft editing and replaces the status-only token with an explicit numeric Listing version used by both editing and publication. Stale writes return `market.listing.concurrent_modification`. Row locking and stronger isolation remain separate requirements.

The current Property publication-availability rule is existence only. Additional Property lifecycle or availability rules are not introduced until concrete requirements define them.
