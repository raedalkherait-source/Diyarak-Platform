using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed record PublishedListingProjection(
    MarketListing Listing,
    MarketProperty Property);