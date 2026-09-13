# Diyarak Application

This directory contains application-level use-case orchestration across Diyarak business modules.

Current Diyarak Market application structure:

- `Diyarak.Market.Application` — application orchestration for Market use cases.
- `CreatePropertyUseCase` validates and constructs the existing Market Property aggregate and inserts it through `IMarketPropertyRepository`; it does not introduce Property ownership semantics.
- `GetPropertyUseCase` validates a Property identifier and returns the persisted aggregate or a stable not-found result.
- `IMarketPropertyRepository` is the Application-owned Property persistence port for direct loading and initial creation.
- `CreateListingUseCase` receives a Property identifier and authenticated actor user identifier, verifies that the Property exists, creates an actor-owned `Draft` Listing, adds it through the repository, and returns the generated Listing identifier.
- `GetListingUseCase` loads a Listing by identifier for its authenticated creator and conceals non-owned Listings with the same not-found result as missing Listings.
- `ListOwnedListingsUseCase` loads the current mapped actor's own Listing management collection and never accepts a publisher identifier from request data.
- `CreateListingErrors` defines stable validation and Property-existence errors for expected creation outcomes.
- `PublishListingUseCase` receives Listing and authenticated actor user identifiers, loads the Listing, verifies creator ownership, verifies that the referenced Property exists, invokes `Listing.Publish()`, and conditionally saves the resulting state against the loaded persisted version.
- `PublishListingErrors` defines stable validation, not-found, publication-state, Property-existence, and concurrent-modification conflicts for expected publication outcomes.
- `IMarketListingRepository` is the Application-owned port used to add Listings, load by identifier, load by publisher ownership, and conditionally save a Listing against an expected persisted version.
- `IPropertyExistenceChecker` is the Application-owned port used for the Property existence check.
- `IMarketTransactionRunner` is the Application-owned port that wraps persistence-sensitive Listing creation, Draft editing, and publication work in an explicit transaction.

Application code may coordinate business modules but does not contain persistence implementations or Host composition logic.

The Host authenticates the caller and supplies a non-empty actor user identifier. An authenticated non-owner receives the same not-found result as a missing Listing; ownership is checked before Property existence, domain mutation, or persistence.

Expected Property creation, Listing creation/editing, and publication failures are returned as classified errors. Unexpected persistence or infrastructure exceptions continue to the Host's exception-handling boundary.

ADR-0027 makes the transaction boundary explicit. After required identifier validation, `CreateListingUseCase`, `UpdateListingUseCase`, and `PublishListingUseCase` execute their persistence-sensitive work through `IMarketTransactionRunner`. The PostgreSQL implementation commits successful results and rolls back expected failures or exceptions. ADR-0028 first adds optimistic publication concurrency. ADR-0029 introduces `UpdateListingUseCase` for authenticated owner-only Draft editing and replaces the status-only token with an explicit numeric Listing version used by both editing and publication. Stale writes return `market.listing.concurrent_modification`. Row locking and stronger isolation remain separate requirements.

ADR-0030 adds authenticated Property creation through the existing aggregate and full-state persistence model. ADR-0031 adds mapped-user-only direct Property loading by identifier through `GetPropertyUseCase`; the read does not add ownership or public-presentation semantics. The current Property publication-availability rule remains existence only. Additional Property lifecycle or availability rules are not introduced until concrete requirements define them.

ADR-0032 adds creator-only direct Listing loading through `GetListingUseCase`. The read returns current aggregate state to the owner without a transaction or Property re-check and does not define public Listing presentation. ADR-0033 adds `ListOwnedListingsUseCase` for the mapped creator's management collection. The repository query filters by `PublisherUserId`; the collection has no search, pagination, or ordering contract.
