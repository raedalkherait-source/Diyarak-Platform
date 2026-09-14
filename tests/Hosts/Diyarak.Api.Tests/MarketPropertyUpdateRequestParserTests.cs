using System.Text.Json;
using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Diyarak.Market.Property;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class MarketPropertyUpdateRequestParserTests
{
    [Fact]
    public void TryParse_accepts_full_replacement_with_positive_version()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "version": 3,
              "category": "Apartment",
              "address": {
                "street": "New Street",
                "houseNumber": "9",
                "postalCode": "23552",
                "city": "Luebeck"
              },
              "livingArea": {
                "value": 82,
                "unit": "SquareMeter"
              }
            }
            """);

        bool parsed = MarketPropertyUpdateRequestParser.TryParse(
            document.RootElement,
            out UpdatePropertyCommand command);

        Assert.True(parsed);
        Assert.Equal(3, command.ExpectedVersion);
        Assert.Equal(PropertyCategory.Apartment, command.Replacement.Category);
        Assert.Equal("New Street", command.Replacement.Address.Street);
        Assert.Equal(82m, command.Replacement.LivingArea?.Value);
    }

    [Fact]
    public void TryParse_rejects_missing_or_non_positive_version()
    {
        using JsonDocument missing = JsonDocument.Parse(
            """{"category":"House","address":{"street":"A","houseNumber":"1","postalCode":"23552","city":"Luebeck"}}""");
        using JsonDocument zero = JsonDocument.Parse(
            """{"version":0,"category":"House","address":{"street":"A","houseNumber":"1","postalCode":"23552","city":"Luebeck"}}""");

        Assert.False(
            MarketPropertyUpdateRequestParser.TryParse(
                missing.RootElement,
                out _));
        Assert.False(
            MarketPropertyUpdateRequestParser.TryParse(
                zero.RootElement,
                out _));
    }

    [Fact]
    public void TryParse_treats_omitted_optional_fields_as_cleared_replacement_values()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "version": 1,
              "category": "House",
              "address": {
                "street": "A",
                "houseNumber": "1",
                "postalCode": "23552",
                "city": "Luebeck"
              }
            }
            """);

        bool parsed = MarketPropertyUpdateRequestParser.TryParse(
            document.RootElement,
            out UpdatePropertyCommand command);

        Assert.True(parsed);
        Assert.Null(command.Replacement.LivingArea);
        Assert.Null(command.Replacement.UsableArea);
        Assert.Null(command.Replacement.TotalRooms);
        Assert.Null(command.Replacement.Features);
    }
}
