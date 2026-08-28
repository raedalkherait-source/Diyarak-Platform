# Property and Listing Requirements

These requirements are derived from the Diyarak Market reference flow supplied for the Market 1.0 milestone.

They are intentionally separated according to ADR-0006:

> A property is a persistent domain asset; a listing is a market publication with an independent lifecycle.

## Property asset

The property represents persistent physical and descriptive facts independent of a particular market publication.

### Classification

Supported top-level property categories observed in the reference flow:

- Apartment
- House
- Plot
- Investment property
- Garage or parking space
- Commercial property

Commercial property subtypes observed:

- Office or practice
- Restaurant or hotel
- Hall or production
- Retail
- Commercial plot

Exact subtype sets remain extensible and should not be coupled to listing lifecycle behavior.

### Location

Observed property location data:

- Street
- House number
- Postal code
- City
- Geographic location for map positioning

Whether an address is publicly displayed is a listing concern rather than a property identity concern.

### Physical characteristics

Observed characteristics include:

- Living area
- Usable area
- Total rooms
- Number of bedrooms
- Number of bathrooms
- Floor
- Building floor information
- Apartment subtype
- Furnishing quality

Observed property features include:

- Fitted kitchen
- Elevator
- Balcony or terrace
- Guest toilet
- Garden or shared garden use
- Basement
- Step-free access
- Suitable for vacation rental
- Historic monument
- Garage or parking space

### Building and condition

Observed building data includes:

- Property condition
- Last modernization year
- Year of construction
- Heating type
- Main energy source
- Energy certificate information

The exact energy-certificate model is not yet defined for Diyarak and must not be inferred from jurisdiction-specific reference behavior.

## Listing publication

A listing represents the market publication of a property and must remain separate from the persistent property asset.

### Listing context

Observed publishing roles:

- Owner
- Tenant
- Professional or agent

Observed transaction intentions:

- Rent
- Sell
- Rent for a limited period

### Commercial terms

Observed listing terms include:

- Asking or purchase price
- Price on request
- Service charge
- Available-from date

The exact financial fields may vary by transaction type and must be modeled only when the corresponding listing behavior is implemented.

### Public presentation

Observed listing content includes:

- Headline
- Property description
- Location description
- Additional descriptive text
- Option to hide detailed street and house number
- Contact visibility preferences

### Media and documents

Observed publication media includes:

- Images
- Floor plans
- Documents
- Video

Media storage and delivery remain Platform Core concerns; a listing should reference media rather than own storage infrastructure.

## Reuse

The reference flow allows an existing property/address to be selected when creating another listing.

Diyarak should preserve the same architectural principle: creating a new listing for an existing property must not require creating a duplicate property asset.

## Company

The supplied reference shows a Professional/Agent publishing role but does not define sufficient company-specific data or behavior.

`Diyarak.Market.Company` therefore remains a foundation scaffold until concrete company requirements are documented.

## Implementation boundary

The first Property implementation should introduce only stable asset concepts supported by these requirements.

Listing lifecycle states, publication workflows, company behavior, authorization rules, persistence mappings, and API contracts must not be invented before their requirements are defined.
