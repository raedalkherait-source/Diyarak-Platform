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
            ClosureReason = listing.ClosureReason is { } closureReason
                ? (int)closureReason
                : null,
            ModerationNote = listing.ModerationNote,
            ContactAllowCalls = listing.Contact?.AllowCalls,
            ContactAllowWhatsApp = listing.Contact?.AllowWhatsApp,
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

        if (record.ContactAllowCalls is not null || record.ContactAllowWhatsApp is not null)
        {
            if (record.ContactAllowCalls is null || record.ContactAllowWhatsApp is null)
                throw new InvalidOperationException(
                    "Persisted Listing contact options must contain both channels.");

            listing.SetContact(
                new ListingContactOptions(
                    record.ContactAllowCalls.Value,
                    record.ContactAllowWhatsApp.Value));
        }

        var status =
            (ListingStatus)record.Status;

        if (!Enum.IsDefined(status))
            throw new InvalidOperationException(
                "Persisted Listing status is unsupported.");

        // Replay the lifecycle so the aggregate enforces the same invariants on rehydration.
        if (status is ListingStatus.Published or ListingStatus.Paused or ListingStatus.Closed or ListingStatus.Suspended)
            listing.Publish();

        if (status == ListingStatus.Paused)
            listing.Pause();

        if (status == ListingStatus.Suspended)
        {
            if (string.IsNullOrWhiteSpace(record.ModerationNote))
                throw new InvalidOperationException(
                    "A persisted Suspended Listing must contain a moderation note.");

            listing.Suspend(record.ModerationNote);
        }
        else if (record.ModerationNote is not null)
        {
            throw new InvalidOperationException(
                "Only a persisted Suspended Listing may contain a moderation note.");
        }

        if (status == ListingStatus.Closed)
        {
            if (record.ClosureReason is not { } closureReason)
                throw new InvalidOperationException(
                    "A persisted Closed Listing must contain a closure reason.");

            listing.Close((ListingClosureReason)closureReason);
        }
        else if (record.ClosureReason is not null)
        {
            throw new InvalidOperationException(
                "Only a persisted Closed Listing may contain a closure reason.");
        }

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
