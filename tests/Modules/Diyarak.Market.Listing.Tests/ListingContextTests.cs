using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingContextTests
{
    [Fact]
    public void Publishing_roles_match_confirmed_market_roles()
    {
        PublishingRole[] expected =
        [
            PublishingRole.Owner,
            PublishingRole.Tenant,
            PublishingRole.ProfessionalOrAgent,
        ];

        Assert.Equal(expected, Enum.GetValues<PublishingRole>());
    }

    [Fact]
    public void Transaction_intents_match_confirmed_market_intents()
    {
        TransactionIntent[] expected =
        [
            TransactionIntent.Rent,
            TransactionIntent.Sell,
            TransactionIntent.RentForLimitedPeriod,
        ];

        Assert.Equal(expected, Enum.GetValues<TransactionIntent>());
    }
}
