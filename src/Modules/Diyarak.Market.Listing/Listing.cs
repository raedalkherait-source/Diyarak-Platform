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

        SubjectReference = subjectReference;
    }

    public PlatformListing.ListingSubjectReference SubjectReference { get; }
}
