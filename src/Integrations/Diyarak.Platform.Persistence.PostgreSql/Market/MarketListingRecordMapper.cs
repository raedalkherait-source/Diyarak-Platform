using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal static class MarketListingRecordMapper
{
    internal static MarketListingRecord FromDomain(
        MarketListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        Money? knownPrice = listing.Price?.Amount;

        return new MarketListingRecord
        {
            Id = listing.Id,
            PublisherUserId = listing.PublisherUserId,
            SubjectId = listing.SubjectReference.SubjectId,
            SubjectType = listing.SubjectReference.SubjectType,
            Status = (int)listing.Status,
            Version = listing.Version,
            PublishingRole =
                listing.Context is null
                    ? null
                    : (int)listing.Context.PublishingRole,
            TransactionIntent =
                listing.Context is null
                    ? null
                    : (int)listing.Context.TransactionIntent,
            Headline = listing.Headline?.Value,
            PriceIsOnRequest = listing.Price?.IsOnRequest,
            PriceAmount = knownPrice?.Amount,
            PriceCurrency = knownPrice?.Currency.Code,
            AvailableFromDate =
                listing.AvailableFromDate?.Value,
        };
    }

    internal static MarketListing ToDomain(
        MarketListingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var listing = MarketListing.Restore(
            record.Id,
            record.PublisherUserId,
            new ListingSubjectReference(
                record.SubjectId,
                record.SubjectType),
            record.Version);

        RestoreContext(record, listing);

        if (record.Headline is not null)
            listing.SetHeadline(
                new ListingHeadline(record.Headline));

        RestorePrice(record, listing);

        if (record.AvailableFromDate is { } availableFromDate)
            listing.SetAvailableFromDate(
                new ListingAvailableFromDate(
                    availableFromDate));

        var status =
            (ListingStatus)record.Status;

        if (!Enum.IsDefined(status))
            throw new InvalidOperationException(
                "Persisted Listing status is unsupported.");

        if (status == ListingStatus.Published)
            listing.Publish();

        return listing;
    }

    private static void RestoreContext(
        MarketListingRecord record,
        MarketListing listing)
    {
        int? publishingRole = record.PublishingRole;
        int? transactionIntent = record.TransactionIntent;

        if (
            publishingRole is null &&
            transactionIntent is null
        )
        {
            return;
        }

        if (
            publishingRole is null ||
            transactionIntent is null
        )
        {
            throw new InvalidOperationException(
                "Persisted Listing context must contain both publishing role and transaction intent.");
        }

        listing.SetContext(
            new ListingContext(
                (PublishingRole)publishingRole.Value,
                (TransactionIntent)transactionIntent.Value));
    }

    private static void RestorePrice(
        MarketListingRecord record,
        MarketListing listing)
    {
        bool? isOnRequest = record.PriceIsOnRequest;
        decimal? amount = record.PriceAmount;
        string? currency = record.PriceCurrency;

        if (isOnRequest is null)
        {
            if (amount is not null || currency is not null)
                throw new InvalidOperationException(
                    "Persisted Listing price data requires a price state.");

            return;
        }

        if (isOnRequest.Value)
        {
            if (amount is not null || currency is not null)
                throw new InvalidOperationException(
                    "A persisted on-request price cannot contain an amount or currency.");

            listing.SetPrice(ListingPrice.OnRequest());

            return;
        }

        if (amount is null || currency is null)
            throw new InvalidOperationException(
                "A persisted known price must contain both amount and currency.");

        listing.SetPrice(
            ListingPrice.Known(
                new Money(
                    amount.Value,
                    Currency.Create(currency))));
    }
}
