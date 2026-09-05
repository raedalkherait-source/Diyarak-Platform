# Listing Subject Reference Requirements

This document records the confirmed requirements and remaining unresolved behavior for identifying the subject published by a listing.

ADR-0010 defines the initial sector-agnostic contract. ADR-0011 defines the Listing subject reference as immutable for the lifetime of a Listing.

## Confirmed requirements

- A listing is separate from the persistent asset that it publishes.
- Creating another listing for an existing property must not require creating a duplicate property asset.
- A listing therefore needs a way to identify its published subject.
- The subject-reference contract must remain sector-agnostic.
- `Diyarak.Platform.Listing` must not depend on a business module such as `Diyarak.Market.Property`.
- `Diyarak.Market.Listing` must not depend directly on another business module implementation such as `Diyarak.Market.Property`.
- A Module-to-Core dependency requires explicit approval through an accepted architecture decision.
- The initial Market use case is publishing a Property while preserving a design that can support additional subject types later.

## Implemented contract

ADR-0010 defines `ListingSubjectReference` in `Diyarak.Platform.Listing`.

The reference contains:

- a `Guid` subject identifier;
- a non-empty opaque string subject type.

Platform Listing does not define a business-sector enum for subject types.

Business modules own the meaning of their subject-type values.

`Diyarak.Market.Listing` defines the confirmed Property subject type as `market.property` and is explicitly approved to reference `Diyarak.Platform.Listing` for this contract.

ADR-0011 requires a Listing to keep the same `ListingSubjectReference` for its lifetime. Publishing a different subject requires creating a new Listing.

## Remaining open requirements

The following behavior remains undefined and must not be invented:

- Which component verifies that the referenced subject exists.
- Which component verifies that the referenced subject is of the expected type.
- What happens to a listing when its referenced subject is removed, archived, or otherwise becomes unavailable.
- Persistence representation beyond the domain contract.
- Transport representation beyond the domain contract.
- Listing lifecycle behavior associated with the subject reference.
- Approval of any additional Module-to-Core dependency or additional Market subject type.

These questions must be resolved from concrete product and architecture requirements rather than inferred from the current Market implementation.
