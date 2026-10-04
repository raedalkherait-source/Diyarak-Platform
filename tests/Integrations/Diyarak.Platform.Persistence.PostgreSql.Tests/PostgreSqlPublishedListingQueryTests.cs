using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlPublishedListingQueryTests
{
    [Fact]
    public async Task ListPageAsync_returns_only_published_listings()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing published = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true);
        MarketListing draft = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: false);

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(published),
            MarketListingRecordMapper.FromDomain(draft));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);

        MarketListing[] result =
            (await query.ListPageAsync(skip: 0, take: 10)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(published.Id, item.Id);
        Assert.Equal(ListingStatus.Published, item.Status);
    }

    [Fact]
    public async Task ListPageAsync_filters_by_transaction_intent_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing rent = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            transactionIntent: TransactionIntent.Rent);
        MarketListing sell = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            transactionIntent: TransactionIntent.Sell);

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(rent),
            MarketListingRecordMapper.FromDomain(sell));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            TransactionIntents:
            [
                TransactionIntent.Sell,
            ]);

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(sell.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_known_price_by_range_and_currency_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing belowRange = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            price: ListingPrice.Known(
                new Money(100_000m, Currency.Eur)));

        MarketListing wrongCurrency = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            price: ListingPrice.Known(
                new Money(250_000m, Currency.Usd)));

        MarketListing matching = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            published: true,
            price: ListingPrice.Known(
                new Money(250_000m, Currency.Eur)));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(belowRange),
            MarketListingRecordMapper.FromDomain(wrongCurrency),
            MarketListingRecordMapper.FromDomain(matching));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: 200_000m,
                Maximum: 300_000m,
                Currency: Currency.Eur));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matching.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_price_range_excludes_on_request_and_includes_boundaries()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing onRequest = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true);

        MarketListing exactBoundary = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            price: ListingPrice.Known(
                new Money(200_000m, Currency.Eur)));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(onRequest),
            MarketListingRecordMapper.FromDomain(exactBoundary));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Price: new PublishedListingPriceSearchCriteria(
                Minimum: 200_000m,
                Maximum: 200_000m,
                Currency: Currency.Eur));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(exactBoundary.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_by_property_category_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid apartmentId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid houseId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var apartment = new MarketProperty(
            apartmentId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"));

        var house = new MarketProperty(
            houseId,
            PropertyCategory.House,
            new PropertyAddress(
                "Second Street",
                "2",
                "20095",
                "Hamburg"));

        MarketListing apartmentListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: apartmentId);

        MarketListing houseListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: houseId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(apartment),
            MarketPropertyRecordMapper.FromDomain(house));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(apartmentListing),
            MarketListingRecordMapper.FromDomain(houseListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Categories:
                [
                    PropertyCategory.House,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(houseListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_by_commercial_subtype_without_requiring_category_filter()
    {
        await using PlatformDbContext context = CreateContext();

        Guid retailId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid officeId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var retail = new MarketProperty(
            retailId,
            PropertyCategory.CommercialProperty,
            new PropertyAddress(
                "Retail Street",
                "1",
                "10115",
                "Berlin"),
            commercialSubtype: CommercialPropertySubtype.Retail);

        var office = new MarketProperty(
            officeId,
            PropertyCategory.CommercialProperty,
            new PropertyAddress(
                "Office Street",
                "2",
                "20095",
                "Hamburg"),
            commercialSubtype:
                CommercialPropertySubtype.OfficeOrPractice);

        MarketListing retailListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: retailId);

        MarketListing officeListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: officeId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(retail),
            MarketPropertyRecordMapper.FromDomain(office));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(retailListing),
            MarketListingRecordMapper.FromDomain(officeListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                CommercialSubtypes:
                [
                    CommercialPropertySubtype.OfficeOrPractice,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(officeListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_returns_empty_for_category_excluding_commercial_with_commercial_subtype()
    {
        await using PlatformDbContext context = CreateContext();

        Guid propertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");

        var property = new MarketProperty(
            propertyId,
            PropertyCategory.CommercialProperty,
            new PropertyAddress(
                "Office Street",
                "1",
                "10115",
                "Berlin"),
            commercialSubtype:
                CommercialPropertySubtype.OfficeOrPractice);

        MarketListing listing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: propertyId);

        context.MarketProperties.Add(
            MarketPropertyRecordMapper.FromDomain(property));

        context.MarketListings.Add(
            MarketListingRecordMapper.FromDomain(listing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Categories:
                [
                    PropertyCategory.Apartment,
                ],
                CommercialSubtypes:
                [
                    CommercialPropertySubtype.OfficeOrPractice,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        Assert.Empty(result);
    }
    [Fact]
    public void BuildQuery_city_filter_translates_to_postgresql_ilike()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_query_translation_test")
                .Options;

        using var context = new PlatformDbContext(options);
        var query = new PostgreSqlPublishedListingQuery(context);

        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Location: new PublishedPropertyLocationSearchCriteria(
                    City: @"ber%_lin\city",
                    PostalCode: null)));

        string sql =
            query.BuildQuery(criteria)
                .ToQueryString();

        Assert.Contains(
            "ILIKE",
            sql,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            @"@cityPattern='ber\%\_lin\\city'",
            sql,
            StringComparison.Ordinal);

        Assert.Contains(
            @"ESCAPE '\'",
            sql,
            StringComparison.Ordinal);
    }
    [Fact]
    public void BuildQuery_postal_code_filter_translates_to_exact_postgresql_ilike()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_query_translation_test")
                .Options;

        using var context = new PlatformDbContext(options);
        var query = new PostgreSqlPublishedListingQuery(context);

        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Location: new PublishedPropertyLocationSearchCriteria(
                    City: null,
                    PostalCode: @"ab%_12\cd")));

        string sql =
            query.BuildQuery(criteria)
                .ToQueryString();

        Assert.Contains(
            "ILIKE",
            sql,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            @"@postalCodePattern='ab\%\_12\\cd'",
            sql,
            StringComparison.Ordinal);

        Assert.Contains(
            @"ESCAPE '\'",
            sql,
            StringComparison.Ordinal);
    }
    [Fact]
    public async Task ListPageAsync_filters_living_area_across_units_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid squareMeterPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid squareFootPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var squareMeterProperty = new MarketProperty(
            squareMeterPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            livingArea: new Area(
                90m,
                AreaUnit.SquareMeter));

        var squareFootProperty = new MarketProperty(
            squareFootPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            livingArea: new Area(
                1000m,
                AreaUnit.SquareFoot));

        MarketListing squareMeterListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: squareMeterPropertyId);

        MarketListing squareFootListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: squareFootPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(squareMeterProperty),
            MarketPropertyRecordMapper.FromDomain(squareFootProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(squareMeterListing),
            MarketListingRecordMapper.FromDomain(squareFootListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 92m,
                    Maximum: 94m,
                    Unit: AreaUnit.SquareMeter)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(squareFootListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_living_area_filter_excludes_missing_area_and_includes_boundary()
    {
        await using PlatformDbContext context = CreateContext();

        Guid missingAreaPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid boundaryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var missingAreaProperty = new MarketProperty(
            missingAreaPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"));

        var boundaryProperty = new MarketProperty(
            boundaryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            livingArea: new Area(
                100m,
                AreaUnit.SquareMeter));

        MarketListing missingAreaListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: missingAreaPropertyId);

        MarketListing boundaryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: boundaryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(missingAreaProperty),
            MarketPropertyRecordMapper.FromDomain(boundaryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(missingAreaListing),
            MarketListingRecordMapper.FromDomain(boundaryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 100m,
                    Maximum: 100m,
                    Unit: AreaUnit.SquareMeter)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(boundaryListing.Id, item.Id);
    }
    [Fact]
    public void BuildQuery_living_area_filter_translates_to_postgresql()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_query_translation_test")
                .Options;

        using var context = new PlatformDbContext(options);
        var query = new PostgreSqlPublishedListingQuery(context);

        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                LivingArea: new PublishedAreaSearchCriteria(
                    Minimum: 92m,
                    Maximum: 94m,
                    Unit: AreaUnit.SquareMeter)));

        string sql =
            query.BuildQuery(criteria)
                .ToQueryString();

        Assert.Contains(
            "living_area_value",
            sql,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "living_area_unit",
            sql,
            StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ListPageAsync_filters_total_rooms_as_decimal_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid smallerPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var smallerProperty = new MarketProperty(
            smallerPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            totalRooms: 2.5m);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            totalRooms: 3.5m);

        MarketListing smallerListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: smallerPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(smallerProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(smallerListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    TotalRooms: new PublishedDecimalRange(
                        Minimum: 3.5m,
                        Maximum: 3.5m))));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_total_rooms_filter_excludes_missing_value_and_includes_boundary()
    {
        await using PlatformDbContext context = CreateContext();

        Guid missingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid boundaryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var missingProperty = new MarketProperty(
            missingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"));

        var boundaryProperty = new MarketProperty(
            boundaryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            totalRooms: 3.5m);

        MarketListing missingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: missingPropertyId);

        MarketListing boundaryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: boundaryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(missingProperty),
            MarketPropertyRecordMapper.FromDomain(boundaryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(missingListing),
            MarketListingRecordMapper.FromDomain(boundaryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    TotalRooms: new PublishedDecimalRange(
                        Minimum: 3.5m,
                        Maximum: 3.5m))));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(boundaryListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_bedroom_count_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid smallerPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var smallerProperty = new MarketProperty(
            smallerPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            bedroomCount: 1);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            bedroomCount: 3);

        MarketListing smallerListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: smallerPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(smallerProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(smallerListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BedroomCount: new PublishedIntegerRange(
                        Minimum: 3,
                        Maximum: 3))));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_bathroom_count_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid smallerPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var smallerProperty = new MarketProperty(
            smallerPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            bathroomCount: 1);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            bathroomCount: 2);

        MarketListing smallerListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: smallerPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(smallerProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(smallerListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                Rooms: new PublishedPropertyRoomSearchCriteria(
                    BathroomCount: new PublishedIntegerRange(
                        Minimum: 2,
                        Maximum: 2))));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_furnishing_quality_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid simplePropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid luxuryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var simpleProperty = new MarketProperty(
            simplePropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            furnishingQuality: FurnishingQuality.Simple);

        var luxuryProperty = new MarketProperty(
            luxuryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            furnishingQuality: FurnishingQuality.Luxury);

        MarketListing simpleListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: simplePropertyId);

        MarketListing luxuryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: luxuryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(simpleProperty),
            MarketPropertyRecordMapper.FromDomain(luxuryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(simpleListing),
            MarketListingRecordMapper.FromDomain(luxuryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                FurnishingQualities:
                [
                    FurnishingQuality.Luxury,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(luxuryListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_furnishing_quality_filter_uses_or_and_excludes_missing_value()
    {
        await using PlatformDbContext context = CreateContext();

        Guid missingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid normalPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");
        Guid luxuryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000003");

        var missingProperty = new MarketProperty(
            missingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress("First Street", "1", "10115", "Berlin"));

        var normalProperty = new MarketProperty(
            normalPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress("Second Street", "2", "10115", "Berlin"),
            furnishingQuality: FurnishingQuality.Normal);

        var luxuryProperty = new MarketProperty(
            luxuryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress("Third Street", "3", "10115", "Berlin"),
            furnishingQuality: FurnishingQuality.Luxury);

        MarketListing missingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: missingPropertyId);

        MarketListing normalListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: normalPropertyId);

        MarketListing luxuryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            published: true,
            subjectId: luxuryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(missingProperty),
            MarketPropertyRecordMapper.FromDomain(normalProperty),
            MarketPropertyRecordMapper.FromDomain(luxuryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(missingListing),
            MarketListingRecordMapper.FromDomain(normalListing),
            MarketListingRecordMapper.FromDomain(luxuryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                FurnishingQualities:
                [
                    FurnishingQuality.Normal,
                    FurnishingQuality.Luxury,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        Assert.Equal(
            [normalListing.Id, luxuryListing.Id],
            result.Select(listing => listing.Id).ToArray());
    }
    [Fact]
    public async Task ListPageAsync_requires_all_property_features_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid partialPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var partialProperty = new MarketProperty(
            partialPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            features:
            [
                PropertyFeature.FittedKitchen,
            ]);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            features:
            [
                PropertyFeature.FittedKitchen,
                PropertyFeature.Elevator,
            ]);

        MarketListing partialListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: partialPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(partialProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(partialListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                RequiredFeatures:
                [
                    PropertyFeature.FittedKitchen,
                    PropertyFeature.Elevator,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public void BuildQuery_required_features_filter_translates_to_postgresql()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=diyarak_query_translation_test")
                .Options;

        using var context = new PlatformDbContext(options);
        var query = new PostgreSqlPublishedListingQuery(context);

        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                RequiredFeatures:
                [
                    PropertyFeature.FittedKitchen,
                    PropertyFeature.Elevator,
                ]));

        string sql =
            query.BuildQuery(criteria)
                .ToQueryString();

        Assert.Contains(
            "features",
            sql,
            StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ListPageAsync_duplicate_required_features_have_no_semantic_effect()
    {
        await using PlatformDbContext context = CreateContext();

        Guid propertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");

        var property = new MarketProperty(
            propertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            features:
            [
                PropertyFeature.FittedKitchen,
                PropertyFeature.Elevator,
            ]);

        MarketListing listing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: propertyId);

        context.MarketProperties.Add(
            MarketPropertyRecordMapper.FromDomain(property));

        context.MarketListings.Add(
            MarketListingRecordMapper.FromDomain(listing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                RequiredFeatures:
                [
                    PropertyFeature.FittedKitchen,
                    PropertyFeature.FittedKitchen,
                    PropertyFeature.Elevator,
                ]));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(listing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_construction_year_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid olderPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var olderProperty = new MarketProperty(
            olderPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            constructionYear: 1990);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            constructionYear: 2020);

        MarketListing olderListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: olderPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(olderProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(olderListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ConstructionYear: new PublishedIntegerRange(
                    Minimum: 2020,
                    Maximum: 2020)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_construction_year_filter_excludes_missing_value_and_includes_boundary()
    {
        await using PlatformDbContext context = CreateContext();

        Guid missingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid boundaryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var missingProperty = new MarketProperty(
            missingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"));

        var boundaryProperty = new MarketProperty(
            boundaryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            constructionYear: 2020);

        MarketListing missingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: missingPropertyId);

        MarketListing boundaryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: boundaryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(missingProperty),
            MarketPropertyRecordMapper.FromDomain(boundaryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(missingListing),
            MarketListingRecordMapper.FromDomain(boundaryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ConstructionYear: new PublishedIntegerRange(
                    Minimum: 2020,
                    Maximum: 2020)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(boundaryListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_filters_parking_space_count_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        Guid smallerPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid matchingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var smallerProperty = new MarketProperty(
            smallerPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"),
            parkingSpaceCount: 1);

        var matchingProperty = new MarketProperty(
            matchingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            parkingSpaceCount: 3);

        MarketListing smallerListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: smallerPropertyId);

        MarketListing matchingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: matchingPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(smallerProperty),
            MarketPropertyRecordMapper.FromDomain(matchingProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(smallerListing),
            MarketListingRecordMapper.FromDomain(matchingListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ParkingSpaceCount: new PublishedIntegerRange(
                    Minimum: 3,
                    Maximum: 3)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 1,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(matchingListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_parking_space_count_filter_excludes_missing_value_and_includes_boundary()
    {
        await using PlatformDbContext context = CreateContext();

        Guid missingPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000001");
        Guid boundaryPropertyId =
            Guid.Parse("10000000-0000-0000-0000-000000000002");

        var missingProperty = new MarketProperty(
            missingPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "First Street",
                "1",
                "10115",
                "Berlin"));

        var boundaryProperty = new MarketProperty(
            boundaryPropertyId,
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Second Street",
                "2",
                "10115",
                "Berlin"),
            parkingSpaceCount: 3);

        MarketListing missingListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true,
            subjectId: missingPropertyId);

        MarketListing boundaryListing = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true,
            subjectId: boundaryPropertyId);

        context.MarketProperties.AddRange(
            MarketPropertyRecordMapper.FromDomain(missingProperty),
            MarketPropertyRecordMapper.FromDomain(boundaryProperty));

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(missingListing),
            MarketListingRecordMapper.FromDomain(boundaryListing));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);
        var criteria = new PublishedListingSearchCriteria(
            Property: new PublishedPropertySearchCriteria(
                ParkingSpaceCount: new PublishedIntegerRange(
                    Minimum: 3,
                    Maximum: 3)));

        MarketListing[] result =
            (await query.ListPageAsync(
                skip: 0,
                take: 10,
                criteria: criteria)).Select(static projection => projection.Listing).ToArray();

        MarketListing item = Assert.Single(result);
        Assert.Equal(boundaryListing.Id, item.Id);
    }
    [Fact]
    public async Task ListPageAsync_orders_by_identifier_before_applying_page_window()
    {
        await using PlatformDbContext context = CreateContext();

        MarketListing first = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            published: true);
        MarketListing second = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            published: true);
        MarketListing third = CreateListing(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            published: true);

        AddListingsWithDefaultProperties(context,
            MarketListingRecordMapper.FromDomain(third),
            MarketListingRecordMapper.FromDomain(first),
            MarketListingRecordMapper.FromDomain(second));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new PostgreSqlPublishedListingQuery(context);

        MarketListing[] result =
            (await query.ListPageAsync(skip: 1, take: 2)).Select(static projection => projection.Listing).ToArray();

        Assert.Equal(2, result.Length);
        Assert.Equal(second.Id, result[0].Id);
        Assert.Equal(third.Id, result[1].Id);
    }

    private static void AddListingsWithDefaultProperties(
        PlatformDbContext context,
        params MarketListingRecord[] listings)
    {
        context.MarketListings.AddRange(listings);

        foreach (MarketListingRecord listing in listings)
        {
            if (listing.SubjectType != MarketListingSubjectTypes.Property)
                continue;

            bool propertyAlreadyTracked =
                context.MarketProperties.Local.Any(
                    property => property.Id == listing.SubjectId);

            if (propertyAlreadyTracked)
                continue;

            context.MarketProperties.Add(
                MarketPropertyRecordMapper.FromDomain(
                    new MarketProperty(
                        listing.SubjectId,
                        PropertyCategory.Apartment,
                        new PropertyAddress(
                            "Test Street",
                            "1",
                            "10115",
                            "Berlin"))));
        }
    }
    private static PlatformDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new PlatformDbContext(options);
    }

    private static MarketListing CreateListing(
        Guid listingId,
        bool published,
        TransactionIntent transactionIntent = TransactionIntent.Sell,
        ListingPrice? price = null,
        Guid? subjectId = null)
    {
        var listing = new MarketListing(
            listingId,
            Guid.NewGuid(),
            new ListingSubjectReference(
                subjectId ?? Guid.NewGuid(),
                MarketListingSubjectTypes.Property));

        listing.SetContext(
            new ListingContext(
                PublishingRole.Owner,
                transactionIntent));
        listing.SetHeadline(
            new ListingHeadline("Published property"));
        listing.SetPrice(price ?? ListingPrice.OnRequest());

        if (published)
            listing.Publish();

        return listing;
    }
}
