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
    }

    public PlatformListing.ListingSubjectReference SubjectReference { get; }
}
