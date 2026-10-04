using System.Net;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.Listing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Tests;

public sealed class ListPublishedListingsEndpointTests
{
    [Fact]
    public async Task Public_collection_is_mapped_when_authentication_is_disabled()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);
    }

    [Fact]
    public async Task Public_collection_does_not_require_access_token_when_authentication_is_enabled()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);
    }

    [Fact]
    public async Task Invalid_page_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?page=not-a-number");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidPagination.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }

    [Fact]
    public async Task Page_size_above_maximum_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidPagination.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }

    [Fact]
    public async Task Public_collection_parses_transaction_intent_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?transactionIntent=Rent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        TransactionIntent intent =
            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyCollection<TransactionIntent>>(
                    criteria.TransactionIntents));

        Assert.Equal(TransactionIntent.Rent, intent);
    }
    [Fact]
    public async Task Public_collection_parses_repeated_transaction_intent_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?transactionIntent=Rent&transactionIntent=Sell");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        IReadOnlyCollection<TransactionIntent> intents =
            Assert.IsAssignableFrom<IReadOnlyCollection<TransactionIntent>>(
                criteria.TransactionIntents);

        Assert.Equal(
            [TransactionIntent.Rent, TransactionIntent.Sell],
            intents);
    }
    [Fact]
    public async Task Invalid_transaction_intent_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?transactionIntent=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_parses_property_category_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyCategory=Apartment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        Diyarak.Market.Property.PropertyCategory category =
            Assert.Single(
                Assert.IsAssignableFrom<
                    IReadOnlyCollection<Diyarak.Market.Property.PropertyCategory>>(
                    property.Categories));

        Assert.Equal(
            Diyarak.Market.Property.PropertyCategory.Apartment,
            category);
    }
    [Fact]
    public async Task Public_collection_parses_repeated_property_category_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyCategory=Apartment&propertyCategory=House");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        IReadOnlyCollection<Diyarak.Market.Property.PropertyCategory>
            categories =
                Assert.IsAssignableFrom<
                    IReadOnlyCollection<Diyarak.Market.Property.PropertyCategory>>(
                    property.Categories);

        Assert.Equal(
            [
                Diyarak.Market.Property.PropertyCategory.Apartment,
                Diyarak.Market.Property.PropertyCategory.House,
            ],
            categories);
    }
    [Fact]
    public async Task Invalid_property_category_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyCategory=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_parses_commercial_subtype_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?commercialSubtype=OfficeOrPractice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        Diyarak.Market.Property.CommercialPropertySubtype subtype =
            Assert.Single(
                Assert.IsAssignableFrom<
                    IReadOnlyCollection<Diyarak.Market.Property.CommercialPropertySubtype>>(
                    property.CommercialSubtypes));

        Assert.Equal(
            Diyarak.Market.Property.CommercialPropertySubtype.OfficeOrPractice,
            subtype);
    }
    [Fact]
    public async Task Public_collection_parses_repeated_commercial_subtype_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?commercialSubtype=OfficeOrPractice&commercialSubtype=Retail");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        IReadOnlyCollection<Diyarak.Market.Property.CommercialPropertySubtype>
            subtypes =
                Assert.IsAssignableFrom<
                    IReadOnlyCollection<Diyarak.Market.Property.CommercialPropertySubtype>>(
                    property.CommercialSubtypes);

        Assert.Equal(
            [
                Diyarak.Market.Property.CommercialPropertySubtype.OfficeOrPractice,
                Diyarak.Market.Property.CommercialPropertySubtype.Retail,
            ],
            subtypes);
    }
    [Fact]
    public async Task Invalid_commercial_subtype_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?commercialSubtype=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_trims_and_parses_city_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?city=%20Berlin%20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyLocationSearchCriteria location =
            Assert.IsType<PublishedPropertyLocationSearchCriteria>(
                property.Location);

        Assert.Equal("Berlin", location.City);
        Assert.Null(location.PostalCode);
    }
    [Fact]
    public async Task Whitespace_only_city_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?city=%20%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Repeated_city_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?city=Berlin&city=Hamburg");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_trims_and_parses_postal_code_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?postalCode=%2010100%20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyLocationSearchCriteria location =
            Assert.IsType<PublishedPropertyLocationSearchCriteria>(
                property.Location);

        Assert.Null(location.City);
        Assert.Equal("10100", location.PostalCode);
    }
    [Fact]
    public async Task Whitespace_only_postal_code_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?postalCode=%20%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Repeated_postal_code_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?postalCode=10100&postalCode=10115");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_composes_city_and_postal_code_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?city=%20Berlin%20&postalCode=%2010100%20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyLocationSearchCriteria location =
            Assert.IsType<PublishedPropertyLocationSearchCriteria>(
                property.Location);

        Assert.Equal("Berlin", location.City);
        Assert.Equal("10100", location.PostalCode);
    }
    [Fact]
    public async Task Public_collection_parses_price_range_and_currency_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceMin=100000.50&priceMax=250000.75&priceCurrency=SYP");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedListingPriceSearchCriteria price =
            Assert.IsType<PublishedListingPriceSearchCriteria>(
                criteria.Price);

        Assert.Equal(100000.50m, price.Minimum);
        Assert.Equal(250000.75m, price.Maximum);
        Assert.Equal("SYP", price.Currency?.Code);
    }
    [Fact]
    public async Task Malformed_price_minimum_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceMin=not-a-number&priceCurrency=SYP");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Malformed_price_currency_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceMin=100000&priceCurrency=INVALID");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Price_bound_without_currency_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceMin=100000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Price_currency_without_bound_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceCurrency=SYP");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Inverted_price_range_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?priceMin=250000&priceMax=100000&priceCurrency=SYP");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            ListPublishedListingsErrors.InvalidSearchCriteria.Code);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_parses_living_area_range_and_unit_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaMin=75.5&livingAreaMax=150.25&livingAreaUnit=SquareMeter");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedAreaSearchCriteria livingArea =
            Assert.IsType<PublishedAreaSearchCriteria>(
                property.LivingArea);

        Assert.Equal(75.5m, livingArea.Minimum);
        Assert.Equal(150.25m, livingArea.Maximum);
        Assert.Equal(
            Diyarak.Platform.Domain.Primitives.AreaUnit.SquareMeter,
            livingArea.Unit);
    }
    [Fact]
    public async Task Living_area_bound_without_unit_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaMin=75.5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Living_area_unit_without_bound_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaUnit=SquareMeter");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Inverted_living_area_range_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaMin=150&livingAreaMax=75&livingAreaUnit=SquareMeter");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Negative_living_area_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaMin=-1&livingAreaUnit=SquareMeter");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Invalid_living_area_unit_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?livingAreaMin=75&livingAreaUnit=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_parses_total_rooms_range_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?totalRoomsMin=2.5&totalRoomsMax=4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyRoomSearchCriteria rooms =
            Assert.IsType<PublishedPropertyRoomSearchCriteria>(
                property.Rooms);

        PublishedDecimalRange totalRooms =
            Assert.IsType<PublishedDecimalRange>(
                rooms.TotalRooms);

        Assert.Equal(2.5m, totalRooms.Minimum);
        Assert.Equal(4m, totalRooms.Maximum);
    }
    [Fact]
    public async Task Public_collection_parses_bedroom_count_range_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?bedroomCountMin=2&bedroomCountMax=4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyRoomSearchCriteria rooms =
            Assert.IsType<PublishedPropertyRoomSearchCriteria>(
                property.Rooms);

        PublishedIntegerRange bedroomCount =
            Assert.IsType<PublishedIntegerRange>(
                rooms.BedroomCount);

        Assert.Equal(2, bedroomCount.Minimum);
        Assert.Equal(4, bedroomCount.Maximum);
    }
    [Fact]
    public async Task Public_collection_parses_bathroom_count_range_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?bathroomCountMin=1&bathroomCountMax=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedPropertyRoomSearchCriteria rooms =
            Assert.IsType<PublishedPropertyRoomSearchCriteria>(
                property.Rooms);

        PublishedIntegerRange bathroomCount =
            Assert.IsType<PublishedIntegerRange>(
                rooms.BathroomCount);

        Assert.Equal(1, bathroomCount.Minimum);
        Assert.Equal(3, bathroomCount.Maximum);
    }
    [Fact]
    public async Task Public_collection_parses_parking_space_count_range_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?parkingSpaceCountMin=1&parkingSpaceCountMax=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedIntegerRange parkingSpaceCount =
            Assert.IsType<PublishedIntegerRange>(
                property.ParkingSpaceCount);

        Assert.Equal(1, parkingSpaceCount.Minimum);
        Assert.Equal(3, parkingSpaceCount.Maximum);
    }
    [Fact]
    public async Task Public_collection_parses_construction_year_range_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?constructionYearMin=1990&constructionYearMax=2020");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        PublishedIntegerRange constructionYear =
            Assert.IsType<PublishedIntegerRange>(
                property.ConstructionYear);

        Assert.Equal(1990, constructionYear.Minimum);
        Assert.Equal(2020, constructionYear.Maximum);
    }
    [Fact]
    public async Task Public_collection_parses_furnishing_quality_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?furnishingQuality=Luxury");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        Diyarak.Market.Property.FurnishingQuality furnishingQuality =
            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyCollection<Diyarak.Market.Property.FurnishingQuality>>(
                    property.FurnishingQualities));

        Assert.Equal(
            Diyarak.Market.Property.FurnishingQuality.Luxury,
            furnishingQuality);
    }
    [Fact]
    public async Task Public_collection_parses_repeated_furnishing_quality_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?furnishingQuality=Simple&furnishingQuality=Luxury");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        IReadOnlyCollection<Diyarak.Market.Property.FurnishingQuality> furnishingQualities =
            Assert.IsAssignableFrom<IReadOnlyCollection<Diyarak.Market.Property.FurnishingQuality>>(
                property.FurnishingQualities);

        Assert.Equal(
            [
                Diyarak.Market.Property.FurnishingQuality.Simple,
                Diyarak.Market.Property.FurnishingQuality.Luxury,
            ],
            furnishingQualities);
    }
    [Fact]
    public async Task Invalid_furnishing_quality_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?furnishingQuality=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_parses_property_feature_filter()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyFeature=Elevator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        Diyarak.Market.Property.PropertyFeature requiredFeature =
            Assert.Single(
                Assert.IsAssignableFrom<IReadOnlyCollection<Diyarak.Market.Property.PropertyFeature>>(
                    property.RequiredFeatures));

        Assert.Equal(
            Diyarak.Market.Property.PropertyFeature.Elevator,
            requiredFeature);
    }
    [Fact]
    public async Task Public_collection_parses_repeated_property_feature_filters()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyFeature=Elevator&propertyFeature=BalconyOrTerrace");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Query.CallCount);

        PublishedListingSearchCriteria criteria =
            Assert.IsType<PublishedListingSearchCriteria>(
                factory.Query.LastCriteria);

        PublishedPropertySearchCriteria property =
            Assert.IsType<PublishedPropertySearchCriteria>(
                criteria.Property);

        IReadOnlyCollection<Diyarak.Market.Property.PropertyFeature> requiredFeatures =
            Assert.IsAssignableFrom<IReadOnlyCollection<Diyarak.Market.Property.PropertyFeature>>(
                property.RequiredFeatures);

        Assert.Equal(
            [
                Diyarak.Market.Property.PropertyFeature.Elevator,
                Diyarak.Market.Property.PropertyFeature.BalconyOrTerrace,
            ],
            requiredFeatures);
    }
    [Fact]
    public async Task Invalid_property_feature_returns_400_without_querying()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?propertyFeature=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Query.CallCount);
    }
    [Fact]
    public async Task Public_collection_page_links_preserve_search_filters()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("First filtered listing"),
                CreatePublishedListing("Second filtered listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?city=Damascus&propertyFeature=Elevator&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "</api/market/public/listings?city=Damascus&propertyFeature=Elevator&page=2&pageSize=1>; rel=\"next\"",
            GetLinkHeader(response));
    }
    [Fact]
    public async Task Collection_uses_requested_page_and_returns_public_projection_only()
    {
        MarketListing first = CreatePublishedListing("First listing");
        MarketListing second = CreatePublishedListing("Second listing");
        MarketListing extra = CreatePublishedListing("Extra listing");
        using var factory = new TestApiFactory(
            [first, second, extra],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, factory.Query.LastSkip);
        Assert.Equal(3, factory.Query.LastTake);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(2, root.GetProperty("page").GetInt32());
        Assert.Equal(2, root.GetProperty("pageSize").GetInt32());
        Assert.True(root.GetProperty("hasMore").GetBoolean());

        JsonElement.ArrayEnumerator items =
            root.GetProperty("items").EnumerateArray();
        JsonElement[] materialized = items.ToArray();

        Assert.Equal(2, materialized.Length);
        Assert.Equal(
            first.Id,
            materialized[0].GetProperty("listingId").GetGuid());
        Assert.Equal(
            "First listing",
            materialized[0].GetProperty("headline").GetString());
        Assert.Equal(
            second.Id,
            materialized[1].GetProperty("listingId").GetGuid());

        Assert.False(materialized[0].TryGetProperty("version", out _));
        Assert.False(materialized[0].TryGetProperty("status", out _));
        Assert.False(materialized[0].TryGetProperty("subject", out _));
        Assert.False(materialized[0].TryGetProperty("publisherUserId", out _));
    }

    [Fact]
    public async Task Collection_uses_default_page_and_reports_last_page()
    {
        MarketListing listing = CreatePublishedListing("Only listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, factory.Query.LastSkip);
        Assert.Equal(21, factory.Query.LastTake);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(20, root.GetProperty("pageSize").GetInt32());
        Assert.False(root.GetProperty("hasMore").GetBoolean());
        Assert.Equal(1, root.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Public_collection_first_page_with_more_items_exposes_next_link()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("First page listing"),
                CreatePublishedListing("Next page listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "</api/market/public/listings?page=2&pageSize=1>; rel=\"next\"",
            GetLinkHeader(response));
    }

    [Fact]
    public async Task Public_collection_middle_page_exposes_previous_and_next_links()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("Middle page listing"),
                CreatePublishedListing("Following page listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "</api/market/public/listings?page=1&pageSize=1>; rel=\"prev\", " +
            "</api/market/public/listings?page=3&pageSize=1>; rel=\"next\"",
            GetLinkHeader(response));
    }

    [Fact]
    public async Task Public_collection_last_page_exposes_previous_link_only()
    {
        using var factory = new TestApiFactory(
            [CreatePublishedListing("Last page listing")],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "</api/market/public/listings?page=1&pageSize=1>; rel=\"prev\"",
            GetLinkHeader(response));
    }

    [Fact]
    public async Task Public_collection_maximum_page_omits_unaddressable_next_link()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("Maximum page listing"),
                CreatePublishedListing("Unaddressable next listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=2147483647&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "</api/market/public/listings?page=2147483646&pageSize=1>; rel=\"prev\"",
            GetLinkHeader(response));
    }

    [Fact]
    public async Task Public_collection_single_first_page_omits_link_header()
    {
        using var factory = new TestApiFactory(
            [CreatePublishedListing("Only page listing")],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Link"));
    }

    [Fact]
    public async Task Public_collection_304_preserves_page_navigation_link()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("Conditional page listing"),
                CreatePublishedListing("Conditional next listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(
            client,
            query: "?page=1&pageSize=1");
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));
        string link = GetLinkHeader(initialResponse);

        HttpResponseMessage response = await SendGetAsync(
            client,
            query: "?page=1&pageSize=1",
            ifNoneMatch: entityTag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(
            link,
            GetLinkHeader(response));
    }

    [Fact]
    public async Task Head_collection_exposes_same_page_navigation_link_as_get()
    {
        using var factory = new TestApiFactory(
            [
                CreatePublishedListing("HEAD page listing"),
                CreatePublishedListing("HEAD next listing"),
            ],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage getResponse = await SendGetAsync(
            client,
            query: "?page=1&pageSize=1");
        HttpResponseMessage headResponse = await SendHeadAsync(
            client,
            query: "?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.Equal(
            GetLinkHeader(getResponse),
            GetLinkHeader(headResponse));
        Assert.Equal(
            string.Empty,
            await headResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Public_collection_200_uses_public_no_cache_policy()
    {
        MarketListing listing = CreatePublishedListing("Cacheable listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.CacheControl is
            { Public: true, NoCache: true, NoStore: false });
    }

    [Fact]
    public async Task Public_collection_304_uses_public_no_cache_policy()
    {
        MarketListing listing = CreatePublishedListing("Conditional cache listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(client);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            ifNoneMatch: entityTag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.True(
            response.Headers.CacheControl is
            { Public: true, NoCache: true, NoStore: false });
    }

    [Fact]
    public async Task Public_collection_400_uses_no_store_policy()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings?page=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(
            response.Headers.CacheControl is { NoStore: true });
    }

    [Fact]
    public async Task Public_collection_returns_opaque_strong_etag()
    {
        MarketListing listing = CreatePublishedListing("ETag listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string entityTag = Assert.Single(
            response.Headers.GetValues("ETag"));

        Assert.StartsWith("\"", entityTag);
        Assert.EndsWith("\"", entityTag);
        Assert.DoesNotContain("W/", entityTag);
        Assert.False(
            entityTag.Contains(
                listing.Id.ToString("N"),
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Public_collection_with_matching_if_none_match_returns_304_without_body()
    {
        MarketListing listing = CreatePublishedListing("Conditional listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(client);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            ifNoneMatch: entityTag);

        Assert.Equal(
            HttpStatusCode.NotModified,
            response.StatusCode);
        Assert.Equal(
            entityTag,
            Assert.Single(response.Headers.GetValues("ETag")));
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Public_collection_with_weak_matching_if_none_match_returns_304()
    {
        MarketListing listing = CreatePublishedListing("Weak validator listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(client);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            ifNoneMatch: $"W/{entityTag}");

        Assert.Equal(
            HttpStatusCode.NotModified,
            response.StatusCode);
    }

    [Fact]
    public async Task Public_collection_with_if_none_match_wildcard_returns_304()
    {
        MarketListing listing = CreatePublishedListing("Wildcard listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            ifNoneMatch: "*");

        Assert.Equal(
            HttpStatusCode.NotModified,
            response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task Public_collection_with_non_matching_if_none_match_returns_200()
    {
        MarketListing listing = CreatePublishedListing("Fresh listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            ifNoneMatch: "\"different\"");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task Public_collection_etag_changes_when_item_version_changes()
    {
        Guid listingId = Guid.NewGuid();
        MarketListing firstVersion = CreatePublishedListing(
            "Versioned listing",
            listingId,
            version: 3);
        MarketListing secondVersion = CreatePublishedListing(
            "Versioned listing",
            listingId,
            version: 4);

        string firstEntityTag;
        using (var factory = new TestApiFactory(
                   [firstVersion],
                   authenticationEnabled: false))
        using (HttpClient client = factory.CreateClient())
        {
            HttpResponseMessage response = await SendGetAsync(client);
            firstEntityTag = Assert.Single(
                response.Headers.GetValues("ETag"));
        }

        string secondEntityTag;
        using (var factory = new TestApiFactory(
                   [secondVersion],
                   authenticationEnabled: false))
        using (HttpClient client = factory.CreateClient())
        {
            HttpResponseMessage response = await SendGetAsync(client);
            secondEntityTag = Assert.Single(
                response.Headers.GetValues("ETag"));
        }

        Assert.NotEqual(firstEntityTag, secondEntityTag);
    }

    [Fact]
    public async Task Public_collection_etag_changes_with_pagination_coordinates()
    {
        MarketListing listing = CreatePublishedListing("Paged listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage firstPage = await SendGetAsync(
            client,
            query: "?page=1&pageSize=1");
        HttpResponseMessage secondPage = await SendGetAsync(
            client,
            query: "?page=2&pageSize=1");

        string firstEntityTag = Assert.Single(
            firstPage.Headers.GetValues("ETag"));
        string secondEntityTag = Assert.Single(
            secondPage.Headers.GetValues("ETag"));

        Assert.NotEqual(firstEntityTag, secondEntityTag);
    }

    [Fact]
    public async Task Head_collection_returns_200_without_body_and_matches_get_etag()
    {
        MarketListing listing = CreatePublishedListing("HEAD listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage getResponse = await SendGetAsync(client);
        string entityTag = Assert.Single(
            getResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendHeadAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            entityTag,
            Assert.Single(response.Headers.GetValues("ETag")));
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
        Assert.True(
            response.Headers.CacheControl is
            { Public: true, NoCache: true, NoStore: false });
    }

    [Fact]
    public async Task Head_collection_with_matching_if_none_match_returns_304_without_body()
    {
        MarketListing listing = CreatePublishedListing(
            "Conditional HEAD listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(client);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendHeadAsync(
            client,
            ifNoneMatch: entityTag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(
            entityTag,
            Assert.Single(response.Headers.GetValues("ETag")));
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Head_collection_invalid_pagination_returns_400_without_body_and_no_store()
    {
        using var factory = new TestApiFactory(
            [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendHeadAsync(
            client,
            query: "?page=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
        Assert.True(
            response.Headers.CacheControl is { NoStore: true });
    }

    [Fact]
    public async Task Head_collection_respects_requested_page_coordinates()
    {
        MarketListing listing = CreatePublishedListing("Paged HEAD listing");
        using var factory = new TestApiFactory(
            [listing],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage getResponse = await SendGetAsync(
            client,
            query: "?page=2&pageSize=1");
        HttpResponseMessage headResponse = await SendHeadAsync(
            client,
            query: "?page=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.Equal(
            Assert.Single(getResponse.Headers.GetValues("ETag")),
            Assert.Single(headResponse.Headers.GetValues("ETag")));
        Assert.Equal(2, factory.Query.LastTake);
    }

    private static string GetLinkHeader(
        HttpResponseMessage response)
    {
        return string.Join(
            ", ",
            response.Headers.GetValues("Link"));
    }

    private static async Task<HttpResponseMessage> SendGetAsync(
        HttpClient client,
        string query = "",
        string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/market/public/listings{query}");

        if (ifNoneMatch is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "If-None-Match",
                ifNoneMatch);
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendHeadAsync(
        HttpClient client,
        string query = "",
        string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/api/market/public/listings{query}");

        if (ifNoneMatch is not null)
        {
            request.Headers.TryAddWithoutValidation(
                "If-None-Match",
                ifNoneMatch);
        }

        return await client.SendAsync(request);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(
            expectedCode,
            root.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                root.GetProperty("traceId").GetString()));
    }

    private static MarketListing CreatePublishedListing(
        string headline,
        Guid? listingId = null,
        long version = MarketListing.InitialVersion)
    {
        MarketListing listing = MarketListing.Restore(
            listingId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property),
            version);

        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline(headline));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();

        return listing;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly bool _authenticationEnabled;

        public TestApiFactory(
            IReadOnlyList<MarketListing> listings,
            bool authenticationEnabled)
        {
            _authenticationEnabled = authenticationEnabled;
            Query = new StubPublishedListingQuery(listings);
        }

        public StubPublishedListingQuery Query { get; }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(
                configuration =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Postgres"] =
                                "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                            ["Authentication:Enabled"] =
                                _authenticationEnabled.ToString(),
                            ["Authentication:Issuer"] =
                                "https://issuer.example.test",
                            ["Authentication:Audience"] =
                                "diyarak-api",
                            ["Authentication:RequireHttpsMetadata"] =
                                "true",
                            ["Cors:AllowedOrigins:0"] =
                                "http://localhost:5173",
                        });
                });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                services =>
                {
                    services.RemoveAll<IPublishedListingQuery>();
                    services.AddSingleton<IPublishedListingQuery>(Query);
                });
        }
    }

    public sealed class StubPublishedListingQuery(
        IReadOnlyList<MarketListing> listings)
        : IPublishedListingQuery
    {
        public int CallCount { get; private set; }

        public int LastSkip { get; private set; }

        public int LastTake { get; private set; }

        public PublishedListingSearchCriteria? LastCriteria { get; private set; }

        public Task<IReadOnlyList<PublishedListingProjection>> ListPageAsync(
            int skip,
            int take,
            PublishedListingSearchCriteria? criteria = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSkip = skip;
            LastTake = take;
            LastCriteria = criteria;

            var projections = new PublishedListingProjection[listings.Count];

            for (int i = 0; i < listings.Count; i++)
            {
                MarketListing listing = listings[i];

                projections[i] = new PublishedListingProjection(
                    listing,
                    new Diyarak.Market.Property.Property(
                        listing.SubjectReference.SubjectId,
                        Diyarak.Market.Property.PropertyCategory.Apartment,
                        new Diyarak.Market.Property.PropertyAddress(
                            "Test Street",
                            "1",
                            "10115",
                            "Berlin")));
            }

            return Task.FromResult<IReadOnlyList<PublishedListingProjection>>(
                projections);
        }
    }
}
