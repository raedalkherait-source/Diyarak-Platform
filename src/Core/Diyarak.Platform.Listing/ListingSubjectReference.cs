using Diyarak.Platform.SharedKernel;

namespace Diyarak.Platform.Listing;

public sealed class ListingSubjectReference : ValueObject
{
    public ListingSubjectReference(Guid subjectId, string subjectType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectType);

        SubjectId = subjectId;
        SubjectType = subjectType;
    }

    public Guid SubjectId { get; }

    public string SubjectType { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SubjectId;
        yield return SubjectType;
    }
}
