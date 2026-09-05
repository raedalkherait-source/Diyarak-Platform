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
- Location classification
- Access or supply classification

The exact value sets and semantics of location classification and access or supply classification are not yet defined for Diyarak.

Whether an address is publicly displayed is a listing concern rather than a property identity concern.

### Physical characteristics

Observed characteristics include:

- Living area
- Usable area
- Sales area
- Total area
- Total rooms
- Number of bedrooms
- Number of bathrooms
- Floor
- Building floor information
- Apartment subtype
- Furnishing quality
- Shop-front width
- Floor covering
- Number of parking spaces

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

The reference shows individual values for some commercial-property characteristics, but their complete value sets are not yet known and must not be inferred from a single published listing.

### Building and condition

Observed building data includes:

- Property condition
- Last modernization year
- Year of construction
- Heating type
- Main energy source
- Energy certificate information

The reference shows individual condition and heating values, but their complete value sets are not yet defined for Diyarak.

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

A published commercial-property example confirms that the transaction or commercialisation type is publication data rather than a persistent property fact.

### Commercial terms

Observed listing terms include:

- Asking or purchase price
- Rent per month
- Price on request
- Additional or service costs
- Deposit
- Tenant commission information
- Available-from date

The exact financial fields may vary by transaction type and must be modeled only when the corresponding listing behavior is implemented.

Currency, charging periods, commission rules, deposit rules, and other jurisdiction-specific financial semantics are not yet defined for Diyarak.

### Public presentation

Observed listing content includes:

- Headline
- Property description
- Furnishing description
- Location description
- Additional descriptive text
- Option to hide detailed street and house number
- Contact visibility preferences

A listing detail page combines publication data with persistent property facts for presentation. This does not make the displayed property facts owned by the listing.

### Media and documents

Observed publication media includes:

- Images
- Floor plans
- Documents
- Video
- Primary image presentation

Media storage and delivery remain Platform Core concerns; a listing should reference media rather than own storage infrastructure.

### Publisher presentation

Published listing pages may present information about the publisher or agent, including:

- Person or agent name
- Identity-verification indicator
- Company or business name
- Business address
- Company branding
- Rating information
- Follower information
- Publisher website
- Publisher legal-information link
- Call and message actions

These observations describe publication presentation only. They do not yet define a complete `Diyarak.Market.Company` domain model, verification workflow, rating model, follower model, or messaging contract.

### Search and detail presentation

Observed search-result presentation includes:

- Primary image
- Listing headline
- Price summary
- Area summary
- Location summary
- Favorite action

Observed listing-detail presentation also includes:

- Image gallery
- Map presentation
- Property facts
- Listing commercial terms
- Descriptive content
- Publisher information
- Contact actions

These observations do not yet define search ranking, filtering, recommendation, favorite persistence, mapping infrastructure, or contact workflows.

### External services

Published listing pages may display third-party or adjacent services such as connectivity or relocation offers.

Such services are not intrinsic property or listing domain data and must not be incorporated into the core Property or Listing model merely because they appear on the publication page.

## Reuse

The reference flow allows an existing property/address to be selected when creating another listing.

Diyarak should preserve the same architectural principle: creating a new listing for an existing property must not require creating a duplicate property asset.

The listing therefore needs an architectural way to identify its published subject without making Platform Core depend on a Market module. ADR-0010 defines that relationship through the sector-agnostic `ListingSubjectReference` contract, ADR-0011 keeps the reference immutable for the lifetime of a Listing, and ADR-0012 assigns supported subject-type validation to the consuming business module. Subject existence validation and behavior when a referenced subject becomes unavailable remain unresolved.

## Company

The supplied reference confirms that a Professional/Agent publisher may have a person identity, company presentation, business address, branding, verification indicator, ratings, followers, external links, and contact actions.

This is still insufficient to define company ownership, membership, verification, rating, follower, or lifecycle behavior.

`Diyarak.Market.Company` therefore remains a foundation scaffold until those behaviors and invariants are documented.

## Implementation boundary

The first Property implementation should introduce only stable asset concepts supported by these requirements.

Observed fields whose complete value sets or semantics are unknown must not be converted into speculative enums or domain rules.

Listing lifecycle states, publication workflows, company behavior, authorization rules, search behavior, persistence mappings, contact workflows, and API contracts must not be invented before their requirements are defined.
