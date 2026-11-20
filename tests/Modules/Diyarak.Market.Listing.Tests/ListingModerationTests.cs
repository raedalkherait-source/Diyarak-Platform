using Diyarak.Platform.Listing;
using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingModerationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Suspension_hides_listing_and_blocks_owner_resume_until_reinstated()
    {
        Listing listing = CreatePublished();

        listing.Suspend("  صور مضللة  ");

        Assert.Equal(ListingStatus.Suspended, listing.Status);
        Assert.Equal("صور مضللة", listing.ModerationNote);
        Assert.False(listing.IsPubliclyVisible);
        Assert.True(listing.IsEditable);
        Assert.Throws<InvalidOperationException>(listing.Resume);
        Assert.Throws<InvalidOperationException>(listing.Pause);

        listing.Reinstate();

        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Null(listing.ModerationNote);
    }

    [Fact]
    public void Suspended_listing_can_be_withdrawn_by_owner()
    {
        Listing listing = CreatePublished();
        listing.Suspend("Duplicate");

        listing.Close(ListingClosureReason.Withdrawn);

        Assert.Equal(ListingStatus.Closed, listing.Status);
        Assert.Null(listing.ModerationNote);
    }

    [Fact]
    public void Suspension_requires_note_and_suspendable_status()
    {
        Listing listing = CreatePublished();
        Assert.Throws<ArgumentException>(() => listing.Suspend(" "));
        Assert.Throws<ArgumentException>(() => listing.Suspend(new string('x', Listing.MaximumModerationNoteLength + 1)));

        Listing draft = CreatePublished();
        draft.Close(ListingClosureReason.Sold);
        Assert.Throws<InvalidOperationException>(() => draft.Suspend("x"));
        Assert.Throws<InvalidOperationException>(CreatePublished().Reinstate);
    }

    [Fact]
    public void Report_trims_comment_and_resolves_once()
    {
        Guid listingId = Guid.NewGuid();
        var report = new ListingReport(Guid.NewGuid(), listingId, ListingReportReason.FraudOrScam, "  طلب دفعة مقدمة  ", Now);

        Assert.True(report.IsOpen);
        Assert.Equal("طلب دفعة مقدمة", report.Comment);

        Guid moderator = Guid.NewGuid();
        report.Resolve(ListingReportResolution.ListingSuspended, moderator, Now.AddHours(1));

        Assert.False(report.IsOpen);
        Assert.Equal(moderator, report.ResolvedByUserId);
        Assert.Throws<InvalidOperationException>(() => report.Resolve(ListingReportResolution.Dismissed, moderator, Now));
    }

    [Fact]
    public void Report_validates_input()
    {
        Assert.Null(new ListingReport(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Other, "   ", Now).Comment);
        Assert.Throws<ArgumentException>(() => new ListingReport(Guid.NewGuid(), Guid.Empty, ListingReportReason.Other, null, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingReport(Guid.NewGuid(), Guid.NewGuid(), (ListingReportReason)99, null, Now));
        Assert.Throws<ArgumentException>(() => new ListingReport(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Other, new string('x', 501), Now));
        Assert.Throws<ArgumentException>(() => ListingReport.Restore(Guid.NewGuid(), Guid.NewGuid(), ListingReportReason.Other, null, Now, ListingReportResolution.Dismissed, null, null));
    }

    private static Listing CreatePublished()
    {
        var listing = new Listing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Apartment"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();
        return listing;
    }
}
