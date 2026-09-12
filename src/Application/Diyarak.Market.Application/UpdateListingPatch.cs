using Diyarak.Market.Listing;

namespace Diyarak.Market.Application;

public sealed record UpdateListingPatch(
    long ExpectedVersion,
    bool UpdateContext,
    ListingContext? Context,
    bool UpdateHeadline,
    ListingHeadline? Headline,
    bool UpdatePrice,
    ListingPrice? Price,
    bool UpdateAvailableFromDate,
    ListingAvailableFromDate? AvailableFromDate)
{
    public bool HasChanges =>
        UpdateContext ||
        UpdateHeadline ||
        UpdatePrice ||
        UpdateAvailableFromDate;
}
