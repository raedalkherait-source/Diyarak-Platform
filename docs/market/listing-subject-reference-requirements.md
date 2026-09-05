# Listing Subject Reference Requirements

This document records the confirmed constraints and unresolved requirements for identifying the subject published by a listing.

It does not define the concrete contract yet.

## Confirmed requirements

- A listing is separate from the persistent asset that it publishes.
- Creating another listing for an existing property must not require creating a duplicate property asset.
- A listing therefore needs a way to identify its published subject.
- The subject-reference contract must remain sector-agnostic.
- `Diyarak.Platform.Listing` must not depend on a business module such as `Diyarak.Market.Property`.
- `Diyarak.Market.Listing` must not depend directly on another business module implementation such as `Diyarak.Market.Property`.
- A Module-to-Core dependency requires explicit approval through an accepted architecture decision.
- The concrete subject-reference contract remains deferred until the requirements below are resolved.

## Open requirements

The following questions must be answered before implementing the contract:

- What information uniquely identifies a published subject?
- Does the reference require both a subject identifier and a sector-agnostic subject type or kind?
- Which Platform capability owns the subject-reference contract?
- Which component verifies that the referenced subject exists?
- Which component verifies that the referenced subject is of the expected type?
- Can the subject reference change after a listing is created?
- What happens to a listing when its referenced subject is removed, archived, or otherwise becomes unavailable?
- Does the contract need persistence or transport representation requirements beyond its domain representation?
- Which Module-to-Core dependency, if any, should be explicitly approved to consume the contract?

These questions must be resolved from concrete product and architecture requirements rather than inferred from the current Market implementation.
