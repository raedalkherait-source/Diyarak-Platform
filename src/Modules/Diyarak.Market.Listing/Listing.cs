using Diyarak.Platform.SharedKernel;
using PlatformListing = Diyarak.Platform.Listing;

namespace Diyarak.Market.Listing;

public sealed class Listing : AggregateRoot<Guid>
{
    public const long InitialVersion = 1;

    public Listing(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference)
        : this(id, publisherUserId, subjectReference, version: InitialVersion)
    {
    }

    private Listing(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference,
        long version)
        : base(id)
    {
        if (publisherUserId == Guid.Empty)
            throw new ArgumentException(
                "Publisher user identifier cannot be empty.",
                nameof(publisherUserId));

        ArgumentNullException.ThrowIfNull(subjectReference);

        if (version <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "Listing version must be positive.");

        if (subjectReference.SubjectType != MarketListingSubjectTypes.Property)
            throw new ArgumentException(
                "Unsupported Market listing subject type.",
                nameof(subjectReference));

        PublisherUserId = publisherUserId;
        SubjectReference = subjectReference;
        Version = version;
        Status = ListingStatus.Draft;
    }

    public Guid PublisherUserId { get; }

    public PlatformListing.ListingSubjectReference SubjectReference { get; }

    public long Version { get; }

    public ListingStatus Status { get; private set; }

    public ListingContext? Context { get; private set; }

    public ListingHeadline? Headline { get; private set; }

    public ListingPrice? Price { get; private set; }

    public ListingAvailableFromDate? AvailableFromDate { get; private set; }

    public static Listing Restore(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference,
        long version) =>
        new(id, publisherUserId, subjectReference, version);

    public void SetContext(ListingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureDraftForEditing();

        Context = context;
    }

    public void SetHeadline(ListingHeadline headline)
    {
        ArgumentNullException.ThrowIfNull(headline);
        EnsureDraftForEditing();

        Headline = headline;
    }

    public void SetPrice(ListingPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        EnsureDraftForEditing();

        Price = price;
    }

    public void SetAvailableFromDate(ListingAvailableFromDate availableFromDate)
    {
        ArgumentNullException.ThrowIfNull(availableFromDate);
        EnsureDraftForEditing();

        AvailableFromDate = availableFromDate;
    }

    public void ClearAvailableFromDate()
    {
        EnsureDraftForEditing();

        AvailableFromDate = null;
    }

    public void Publish()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException(
                "Only a Draft listing can be published.");

        if (Context is null || Headline is null || Price is null)
            throw new InvalidOperationException(
                "Listing is not ready for publication.");

        Status = ListingStatus.Published;
    }

    private void EnsureDraftForEditing()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException(
                "Only a Draft listing can be edited.");
    }
}
