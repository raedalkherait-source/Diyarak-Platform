using Diyarak.Platform.SharedKernel;
using PlatformListing = Diyarak.Platform.Listing;

namespace Diyarak.Market.Listing;

public sealed class Listing : AggregateRoot<Guid>
{
    public Listing(
        Guid id,
        PlatformListing.ListingSubjectReference subjectReference)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(subjectReference);

        if (subjectReference.SubjectType != MarketListingSubjectTypes.Property)
            throw new ArgumentException(
                "Unsupported Market listing subject type.",
                nameof(subjectReference));

        SubjectReference = subjectReference;
        Status = ListingStatus.Draft;
    }

    public PlatformListing.ListingSubjectReference SubjectReference { get; }

    public ListingStatus Status { get; private set; }

    public ListingContext? Context { get; private set; }

    public ListingHeadline? Headline { get; private set; }

    public ListingPrice? Price { get; private set; }

    public ListingAvailableFromDate? AvailableFromDate { get; private set; }

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
