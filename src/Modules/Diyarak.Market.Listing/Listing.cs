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

    public void Publish()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException(
                "Only a Draft listing can be published.");

        Status = ListingStatus.Published;
    }
}
