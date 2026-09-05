using Xunit;

namespace Diyarak.Platform.Listing.Tests;

public sealed class ListingSubjectReferenceTests
{
    [Fact]
    public void Constructor_sets_subject_identifier_and_type()
    {
        var subjectId = Guid.NewGuid();

        var reference = new ListingSubjectReference(subjectId, "market.property");

        Assert.Equal(subjectId, reference.SubjectId);
        Assert.Equal("market.property", reference.SubjectType);
    }

    [Fact]
    public void Equal_references_are_equal()
    {
        var subjectId = Guid.NewGuid();

        var first = new ListingSubjectReference(subjectId, "market.property");
        var second = new ListingSubjectReference(subjectId, "market.property");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_or_whitespace_subject_type(string subjectType)
    {
        Assert.Throws<ArgumentException>(
            () => new ListingSubjectReference(Guid.NewGuid(), subjectType));
    }
}
