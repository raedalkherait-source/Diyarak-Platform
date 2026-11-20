using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

/// <summary>Resolves a user's verified phone number without Market depending on Identity.</summary>
public interface IPublisherPhoneLookup
{
    public Task<PhoneNumber?> FindVerifiedPhoneAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record ListingContactDetails(
    PhoneNumber PhoneNumber,
    bool AllowCalls,
    bool AllowWhatsApp);

public static class GetListingContactErrors
{
    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing_contact.not_found",
            "No contact details are available for this Listing.");
}

/// <summary>Reveals contact details for a Published Listing whose publisher opted in.</summary>
public sealed class GetListingContactUseCase(
    IMarketListingRepository listings,
    IPublisherPhoneLookup phones)
{
    public async Task<Result<ListingContactDetails>> ExecuteAsync(
        Guid listingId,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty)
            return Result.Failure<ListingContactDetails>(GetListingContactErrors.NotFound);

        MarketListing? listing = await listings.FindByIdAsync(listingId, cancellationToken);

        if (listing is not { Status: ListingStatus.Published, Contact: { } contact })
            return Result.Failure<ListingContactDetails>(GetListingContactErrors.NotFound);

        PhoneNumber? phone = await phones.FindVerifiedPhoneAsync(listing.PublisherUserId, cancellationToken);

        return phone is null
            ? Result.Failure<ListingContactDetails>(GetListingContactErrors.NotFound)
            : Result.Success(new ListingContactDetails(phone, contact.AllowCalls, contact.AllowWhatsApp));
    }
}
