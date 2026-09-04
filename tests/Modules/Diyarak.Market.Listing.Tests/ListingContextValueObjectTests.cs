using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingContextValueObjectTests
{
    [Fact]
    public void Constructor_sets_publishing_role_and_transaction_intent()
    {
        var context = new ListingContext(
            PublishingRole.Owner,
            TransactionIntent.Sell);

        Assert.Equal(PublishingRole.Owner, context.PublishingRole);
        Assert.Equal(TransactionIntent.Sell, context.TransactionIntent);
    }

    [Fact]
    public void Equal_contexts_are_equal()
    {
        var first = new ListingContext(
            PublishingRole.ProfessionalOrAgent,
            TransactionIntent.Rent);

        var second = new ListingContext(
            PublishingRole.ProfessionalOrAgent,
            TransactionIntent.Rent);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Constructor_rejects_invalid_publishing_role()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListingContext(
                (PublishingRole)0,
                TransactionIntent.Rent));
    }

    [Fact]
    public void Constructor_rejects_invalid_transaction_intent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ListingContext(
                PublishingRole.Tenant,
                (TransactionIntent)0));
    }
}
