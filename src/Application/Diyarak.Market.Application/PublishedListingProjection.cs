using Diyarak.Market.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application;

public sealed record PublishedListingProjection(
    MarketListing Listing,
    MarketProperty Property)
{
    /// <summary>Photos in display order; the first is the cover photo.</summary>
    public IReadOnlyList<ListingPhoto> Photos { get; init; } = [];

    /// <summary>Whether a paid featured period is running (ADR-0071).</summary>
    public bool IsFeatured { get; init; }
}
