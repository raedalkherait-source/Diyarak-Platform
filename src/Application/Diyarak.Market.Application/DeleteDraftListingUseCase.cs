using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application;

/// <summary>Removes the rows that belong to a draft Listing.</summary>
public interface IDraftListingDeletionStore
{
    /// <returns><c>false</c> when the Listing no longer has <paramref name="expectedVersion"/>.</returns>
    public Task<bool> TryDeleteListingAsync(
        Guid listingId,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    /// <returns>The storage keys of the deleted photos.</returns>
    public Task<IReadOnlyList<string>> DeletePhotosAsync(
        Guid listingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the Property when it belongs to <paramref name="ownerUserId"/> and no Listing
    /// refers to it any more; otherwise leaves it in place.
    /// </summary>
    public Task DeletePropertyIfUnreferencedAsync(
        Guid propertyId,
        Guid ownerUserId,
        CancellationToken cancellationToken = default);
}

public static class DeleteDraftListingErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation(
            "market.listing_delete.invalid_request",
            "The Listing identifier, actor and expected version must be valid.");

    public static Error NotFound { get; } =
        Error.NotFound(
            "market.listing_delete.not_found",
            "The Listing does not exist.");

    public static Error NotDraft { get; } =
        Error.Conflict(
            "market.listing_delete.not_draft",
            "Only draft Listings can be deleted; close a published Listing instead.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "market.listing_delete.concurrent_modification",
            "The Listing changed while it was being deleted.");
}

/// <summary>
/// Permanently deletes a draft Listing owned by the actor, its photos and, when no other Listing
/// uses it, its Property. Drafts were never public, so nothing else refers to them.
/// </summary>
public sealed class DeleteDraftListingUseCase
{
    private readonly IMarketListingRepository _listings;
    private readonly IListingPhotoStorage _storage;
    private readonly IDraftListingDeletionStore _deletion;
    private readonly IMarketTransactionRunner _transactionRunner;

    public DeleteDraftListingUseCase(
        IMarketListingRepository listings,
        IListingPhotoStorage storage,
        IDraftListingDeletionStore deletion,
        IMarketTransactionRunner transactionRunner)
    {
        ArgumentNullException.ThrowIfNull(listings);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(deletion);
        ArgumentNullException.ThrowIfNull(transactionRunner);

        _listings = listings;
        _storage = storage;
        _deletion = deletion;
        _transactionRunner = transactionRunner;
    }

    public async Task<Result> ExecuteAsync(
        Guid listingId,
        Guid actorUserId,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (listingId == Guid.Empty || actorUserId == Guid.Empty || expectedVersion <= 0)
            return Result.Failure(DeleteDraftListingErrors.InvalidRequest);

        Result<IReadOnlyList<string>> deleted = await _transactionRunner.ExecuteAsync(
            async token =>
            {
                MarketListing? listing = await _listings.FindByIdAsync(listingId, token);

                // Missing and non-owned Listings are indistinguishable (ADR-0023).
                if (listing is null || listing.PublisherUserId != actorUserId)
                    return Result.Failure<IReadOnlyList<string>>(DeleteDraftListingErrors.NotFound);

                if (listing.Status != ListingStatus.Draft)
                    return Result.Failure<IReadOnlyList<string>>(DeleteDraftListingErrors.NotDraft);

                if (listing.Version != expectedVersion)
                    return Result.Failure<IReadOnlyList<string>>(DeleteDraftListingErrors.ConcurrentModification);

                if (!await _deletion.TryDeleteListingAsync(listingId, expectedVersion, token))
                    return Result.Failure<IReadOnlyList<string>>(DeleteDraftListingErrors.ConcurrentModification);

                IReadOnlyList<string> storageKeys = await _deletion.DeletePhotosAsync(listingId, token);

                if (listing.SubjectReference.SubjectType == MarketListingSubjectTypes.Property)
                {
                    await _deletion.DeletePropertyIfUnreferencedAsync(
                        listing.SubjectReference.SubjectId,
                        actorUserId,
                        token);
                }

                return Result.Success(storageKeys);
            },
            cancellationToken);

        if (!deleted.IsSuccess)
            return Result.Failure(deleted.Error);

        // Binaries are removed only after the rows are gone, so a failure here leaves unreachable
        // files rather than rows pointing at missing files.
        foreach (string key in deleted.Value)
            await _storage.DeleteAsync(key, CancellationToken.None);

        return Result.Success();
    }
}
