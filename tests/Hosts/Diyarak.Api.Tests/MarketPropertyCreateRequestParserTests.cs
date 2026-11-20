using System.Text.Json;
using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Diyarak.Market.Property;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class MarketPropertyCreateRequestParserTests
{
    [Fact]
    public void TryParse_accepts_syrian_address_and_market_details()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "category": "Apartment",
              "address": {
                "governorate": "Damascus",
                "city": "دمشق",
                "district": "المزة"
              },
              "legalTitleType": "GreenTitleDeed",
              "finishingLevel": "SuperDeluxe",
              "floorNumber": 0,
              "buildingFloorCount": 5,
              "features": ["SolarPowerSystem", "GeneratorSubscription", "WaterTank"]
            }
            """);

        bool parsed = MarketPropertyCreateRequestParser.TryParse(
            document.RootElement,
            out CreatePropertyCommand command);

        Assert.True(parsed);
        Assert.Equal(SyrianGovernorate.Damascus, command.Address.Governorate);
        Assert.Equal("دمشق", command.Address.City);
        Assert.Equal("المزة", command.Address.District);
        Assert.Null(command.Address.Street);
        Assert.Null(command.Address.HouseNumber);
        Assert.Null(command.Address.PostalCode);
        Assert.Equal(LegalTitleType.GreenTitleDeed, command.LegalTitleType);
        Assert.Equal(FinishingLevel.SuperDeluxe, command.FinishingLevel);
        Assert.Equal(0, command.FloorNumber);
        Assert.Equal(5, command.BuildingFloorCount);
        Assert.Equal(3, command.Features!.Count);
    }

    [Theory]
    [InlineData("""{ "category": "Apartment", "address": { "district": "Mezzeh" } }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus", "street": " " } }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus", "governorate": "Bavaria" } }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus" }, "legalTitleType": "Unknown" }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus" }, "finishingLevel": 3 }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus" }, "floorNumber": -6 }""")]
    [InlineData("""{ "category": "Apartment", "address": { "city": "Damascus" }, "buildingFloorCount": 0 }""")]
    public void TryParse_rejects_invalid_syrian_market_values(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.False(
            MarketPropertyCreateRequestParser.TryParse(
                document.RootElement,
                out _));
    }
}
