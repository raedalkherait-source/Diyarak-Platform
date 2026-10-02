using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class ListPublishedListingsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_rejects_non_positive_page_without_querying()
    {
        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 0,
                pageSize: 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidPagination,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_page_size_above_maximum_without_querying()
    {
        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize:
                    ListPublishedListingsUseCase.MaximumPageSize + 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidPagination,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_price_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: -1m,
                Maximum: null,
                Currency: Diyarak.Platform.Domain.Primitives.Currency.Usd));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_price_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: 200m,
                Maximum: 100m,
                Currency: Diyarak.Platform.Domain.Primitives.Currency.Usd));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_price_bound_without_currency_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: 100m,
                Maximum: null,
                Currency: null));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_price_currency_without_bounds_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: null,
                Maximum: null,
                Currency: Diyarak.Platform.Domain.Primitives.Currency.Usd));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_living_area_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: -1m,
                    Maximum: null,
                    Unit: Diyarak.Platform.Domain.Primitives.AreaUnit.SquareMeter)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_living_area_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 200m,
                    Maximum: 100m,
                    Unit: Diyarak.Platform.Domain.Primitives.AreaUnit.SquareMeter)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_living_area_bound_without_unit_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 100m,
                    Maximum: null,
                    Unit: null)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_living_area_unit_without_bounds_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: null,
                    Maximum: null,
                    Unit: Diyarak.Platform.Domain.Primitives.AreaUnit.SquareMeter)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_living_area_unit_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 100m,
                    Maximum: 200m,
                    Unit: (Diyarak.Platform.Domain.Primitives.AreaUnit)999)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_total_rooms_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    TotalRooms: new PublishedDecimalRange(
                        Minimum: -1m,
                        Maximum: null))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_total_rooms_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    TotalRooms: new PublishedDecimalRange(
                        Minimum: 5m,
                        Maximum: 3m))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_bedroom_count_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BedroomCount: new PublishedIntegerRange(
                        Minimum: -1,
                        Maximum: null))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_bedroom_count_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BedroomCount: new PublishedIntegerRange(
                        Minimum: 4,
                        Maximum: 2))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_bathroom_count_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BathroomCount: new PublishedIntegerRange(
                        Minimum: -1,
                        Maximum: null))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_bathroom_count_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BathroomCount: new PublishedIntegerRange(
                        Minimum: 3,
                        Maximum: 1))));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_construction_year_below_one_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ConstructionYear: new PublishedIntegerRange(
                    Minimum: 0,
                    Maximum: null)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_construction_year_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ConstructionYear: new PublishedIntegerRange(
                    Minimum: 2025,
                    Maximum: 2000)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_negative_minimum_parking_space_count_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ParkingSpaceCount: new PublishedIntegerRange(
                    Minimum: -1,
                    Maximum: null)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_parking_space_count_range_when_minimum_exceeds_maximum_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ParkingSpaceCount: new PublishedIntegerRange(
                    Minimum: 3,
                    Maximum: 1)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_transaction_intent_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            TransactionIntents:
            [
                (TransactionIntent)999,
            ]);

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_property_category_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Categories:
                [
                    (Diyarak.Market.Property.PropertyCategory)999,
                ]));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_commercial_property_subtype_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                CommercialSubtypes:
                [
                    (Diyarak.Market.Property.CommercialPropertySubtype)999,
                ]));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_furnishing_quality_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                FurnishingQualities:
                [
                    (Diyarak.Market.Property.FurnishingQuality)999,
                ]));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_undefined_required_feature_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                RequiredFeatures:
                [
                    (Diyarak.Market.Property.PropertyFeature)999,
                ]));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_empty_transaction_intents_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            TransactionIntents: []);

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_empty_property_categories_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Categories: []));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_empty_commercial_property_subtypes_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                CommercialSubtypes: []));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_empty_furnishing_qualities_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                FurnishingQualities: []));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_empty_required_features_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                RequiredFeatures: []));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_whitespace_city_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Location: new PublishedPropertyLocationSearchCriteria(
                    City: "   ",
                    PostalCode: null)));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_rejects_whitespace_postal_code_without_querying()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Location: new PublishedPropertyLocationSearchCriteria(
                    City: null,
                    PostalCode: "   ")));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ListPublishedListingsErrors.InvalidSearchCriteria,
            result.Error);
        Assert.Equal(0, query.CallCount);
    }
    [Fact]
    public async Task ExecuteAsync_allows_category_excluding_commercial_with_commercial_subtype()
    {
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Categories:
                [
                    Diyarak.Market.Property.PropertyCategory.Apartment,
                ],
                CommercialSubtypes:
                [
                    Diyarak.Market.Property.CommercialPropertySubtype.OfficeOrPractice,
                ]));

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, query.CallCount);
        Assert.Same(criteria, query.LastCriteria);
    }
    [Fact]
    public async Task ExecuteAsync_requests_one_extra_row_and_reports_has_more()
    {
        MarketListing first = CreatePublishedListing();
        MarketListing second = CreatePublishedListing();
        MarketListing third = CreatePublishedListing();
        var query = new StubPublishedListingQuery(
            [first, second, third]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 2,
                pageSize: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, query.LastSkip);
        Assert.Equal(3, query.LastTake);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Same(first, result.Value.Items[0]);
        Assert.Same(second, result.Value.Items[1]);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.True(result.Value.HasMore);
    }

    [Fact]
    public async Task ExecuteAsync_fails_closed_when_query_returns_draft_listing()
    {
        var draft = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));
        var query = new StubPublishedListingQuery([draft]);
        var useCase = new ListPublishedListingsUseCase(query);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(
                page: 1,
                pageSize: 20));
    }

    [Fact]
    public async Task ExecuteAsync_reports_last_page_when_query_returns_no_extra_row()
    {
        MarketListing listing = CreatePublishedListing();
        var query = new StubPublishedListingQuery([listing]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 2);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.HasMore);
    }

    [Fact]
    public async Task ExecuteAsync_passes_search_criteria_to_query()
    {
        var criteria = new PublishedListingSearchCriteria(
            TransactionIntents:
            [
                TransactionIntent.Rent,
                TransactionIntent.Sell,
            ]);

        var query = new StubPublishedListingQuery([]);
        var useCase = new ListPublishedListingsUseCase(query);

        Result<PublishedListingPage> result =
            await useCase.ExecuteAsync(
                page: 1,
                pageSize: 20,
                criteria: criteria);

        Assert.True(result.IsSuccess);
        Assert.Same(criteria, query.LastCriteria);
    }
    private static MarketListing CreatePublishedListing()
    {
        var listing = new MarketListing(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new ListingSubjectReference(
                Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                TransactionIntent.Sell));
        listing.SetHeadline(
            new ListingHeadline("Published property"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.Publish();

        return listing;
    }

    private sealed class StubPublishedListingQuery(
        IReadOnlyList<MarketListing> result)
        : IPublishedListingQuery
    {
        public int CallCount { get; private set; }

        public int LastSkip { get; private set; }

        public int LastTake { get; private set; }

        public PublishedListingSearchCriteria? LastCriteria { get; private set; }

        public Task<IReadOnlyList<MarketListing>> ListPageAsync(
            int skip,
            int take,
            PublishedListingSearchCriteria? criteria = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSkip = skip;
            LastTake = take;
            LastCriteria = criteria;
            return Task.FromResult(result);
        }
    }
}
