using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Application.BulkImport;
using Diyarak.Market.Company;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.Tests;

public sealed class BulkImportTests
{
    [Fact]
    public void CsvParser_parses_valid_csv()
    {
        const string csv = """
            category,city,governorate,headline,publishingRole,transactionIntent,price,currency,isOnRequest,allowCalls,allowWhatsApp
            Apartment,دمشق,Damascus,شقة للبيع,Owner,Sell,50000000,SYP,false,true,true
            Plot,حلب,Aleppo,أرض للبيع,Owner,Sell,,,,true,false
            """;

        Result<IReadOnlyList<BulkImportRow>> result = CsvParser.Parse(csv);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("Apartment", result.Value[0].Category);
        Assert.Equal("دمشق", result.Value[0].City);
        Assert.Equal("Damascus", result.Value[0].Governorate);
        Assert.Equal(50000000m, result.Value[0].Price);
        Assert.False(result.Value[0].IsOnRequest);
        Assert.True(result.Value[1].AllowCalls);
        Assert.False(result.Value[1].AllowWhatsApp);
    }

    [Fact]
    public void CsvParser_rejects_empty_input()
    {
        Assert.False(CsvParser.Parse("").IsSuccess);
        Assert.False(CsvParser.Parse("  ").IsSuccess);
    }

    [Fact]
    public void CsvParser_rejects_missing_required_headers()
    {
        const string csv = "category,city\nApartment,دمشق";
        Assert.False(CsvParser.Parse(csv).IsSuccess);
    }

    [Fact]
    public void CsvParser_parses_isOnRequest_rows()
    {
        const string csv = """
            category,city,headline,publishingRole,transactionIntent,isOnRequest,allowCalls,allowWhatsApp
            House,حمص,بيت للبيع,Owner,Sell,true,true,true
            """;

        Result<IReadOnlyList<BulkImportRow>> result = CsvParser.Parse(csv);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.True(result.Value[0].IsOnRequest);
        Assert.Null(result.Value[0].Price);
    }

    [Fact]
    public async Task Execute_rejects_empty_actor()
    {
        var useCase = CreateUseCase(withAgency: false);
        Result<BulkImportResult> result = await useCase.ExecuteAsync(Guid.Empty, [MakeRow()]);
        Assert.Equal(BulkImportErrors.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task Execute_rejects_empty_rows()
    {
        var useCase = CreateUseCase(withAgency: false);
        Result<BulkImportResult> result = await useCase.ExecuteAsync(Guid.NewGuid(), []);
        Assert.Equal(BulkImportErrors.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task Execute_rejects_too_many_rows()
    {
        var useCase = CreateUseCase(withAgency: false);
        var rows = Enumerable.Range(0, 201).Select(_ => MakeRow()).ToList();
        Result<BulkImportResult> result = await useCase.ExecuteAsync(Guid.NewGuid(), rows);
        Assert.Equal(BulkImportErrors.TooManyRows, result.Error);
    }

    [Fact]
    public async Task Execute_rejects_non_agency_user()
    {
        var useCase = CreateUseCase(withAgency: false);
        Result<BulkImportResult> result = await useCase.ExecuteAsync(Guid.NewGuid(), [MakeRow()]);
        Assert.Equal(BulkImportErrors.NotAnAgency, result.Error);
    }

    [Fact]
    public async Task Execute_creates_property_and_draft_listing()
    {
        (BulkImportListingsUseCase useCase, Guid userId, InMemoryPropertyRepo props, InMemoryListingRepo listings) = CreateUseCaseWithAgency();

        Result<BulkImportResult> result = await useCase.ExecuteAsync(userId, [MakeRow()]);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(0, result.Value.Failed);
        Assert.Single(props.All);
        Assert.Single(listings.All);
        Assert.Equal(ListingStatus.Draft, listings.All[0].Status);
    }

    [Fact]
    public async Task Execute_reports_invalid_rows_without_stopping()
    {
        (BulkImportListingsUseCase useCase, Guid userId, _, _) = CreateUseCaseWithAgency();

        var rows = new List<BulkImportRow>
        {
            MakeRow(),
            MakeRow(category: "InvalidCategory"),
            MakeRow(publishingRole: "BadRole"),
            MakeRow(),
        };

        Result<BulkImportResult> result = await useCase.ExecuteAsync(userId, rows);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Created);
        Assert.Equal(2, result.Value.Failed);
        Assert.Equal(2, result.Value.Errors.Count);
        Assert.Contains("Row 2", result.Value.Errors[0]);
        Assert.Contains("Row 3", result.Value.Errors[1]);
    }

    [Fact]
    public async Task Execute_validates_governorate()
    {
        (BulkImportListingsUseCase useCase, Guid userId, _, _) = CreateUseCaseWithAgency();
        Result<BulkImportResult> result = await useCase.ExecuteAsync(userId, [MakeRow(governorate: "FakeGov")]);
        Assert.Equal(1, result.Value.Failed);
        Assert.Contains("governorate", result.Value.Errors[0]);
    }

    [Fact]
    public async Task Execute_validates_price()
    {
        (BulkImportListingsUseCase useCase, Guid userId, _, _) = CreateUseCaseWithAgency();
        var row = new BulkImportRow("Apartment", "دمشق", null, null, "شقة", "Owner", "Sell", null, null, false, true, true);
        Result<BulkImportResult> result = await useCase.ExecuteAsync(userId, [row]);
        Assert.Equal(1, result.Value.Failed);
        Assert.Contains("price", result.Value.Errors[0]);
    }

    [Fact]
    public async Task Execute_with_on_request_price()
    {
        (BulkImportListingsUseCase useCase, Guid userId, _, InMemoryListingRepo listings) = CreateUseCaseWithAgency();
        var row = new BulkImportRow("Apartment", "دمشق", "Damascus", null, "شقة بسعر على الطلب", "Owner", "Sell", null, null, true, true, true);
        Result<BulkImportResult> result = await useCase.ExecuteAsync(userId, [row]);
        Assert.Equal(1, result.Value.Created);
        Assert.True(listings.All[0].Price!.IsOnRequest);
    }

    private static BulkImportRow MakeRow(
        string category = "Apartment",
        string city = "دمشق",
        string? governorate = "Damascus",
        string headline = "شقة للبيع في دمشق",
        string publishingRole = "Owner",
        string transactionIntent = "Sell",
        decimal? price = 50000000,
        string? currency = "SYP",
        bool isOnRequest = false) =>
        new(category, city, governorate, null, headline, publishingRole, transactionIntent, price, currency, isOnRequest, true, true);

    private static BulkImportListingsUseCase CreateUseCase(bool withAgency)
    {
        var agencies = new InMemoryAgencyRepo();
        return new BulkImportListingsUseCase(agencies, new InMemoryPropertyRepo(), new InMemoryListingRepo());
    }

    private static (BulkImportListingsUseCase, Guid, InMemoryPropertyRepo, InMemoryListingRepo) CreateUseCaseWithAgency()
    {
        Guid userId = Guid.NewGuid();
        Agency agency = Agency.Create(Guid.NewGuid(), userId, "Test Agency", null, null, "دمشق");
        var agencies = new InMemoryAgencyRepo();
        agencies.Add(agency);
        var props = new InMemoryPropertyRepo();
        var listings = new InMemoryListingRepo();
        return (new BulkImportListingsUseCase(agencies, props, listings), userId, props, listings);
    }

    private sealed class InMemoryAgencyRepo : IAgencyRepository
    {
        private readonly List<Agency> _agencies = [];
        public void Add(Agency a) => _agencies.Add(a);
        public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken ct = default) =>
            Task.FromResult(_agencies.Find(a => a.Id == agencyId));
        public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(_agencies.Find(a => a.OwnerUserId == ownerUserId));
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(_agencies.Where(a => ids.Contains(a.OwnerUserId)).ToDictionary(a => a.OwnerUserId));
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> agencyIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(_agencies.Where(a => agencyIds.Contains(a.Id)).ToDictionary(a => a.Id));
        public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Agency>>([.. _agencies.Where(a => a.VerificationStatus == status).Take(take)]);
        public Task<bool> TryAddAsync(Agency agency, CancellationToken ct = default)
        {
            if (_agencies.Exists(a => a.OwnerUserId == agency.OwnerUserId)) return Task.FromResult(false);
            _agencies.Add(agency);
            return Task.FromResult(true);
        }
        public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    internal sealed class InMemoryPropertyRepo : IMarketPropertyRepository
    {
        public List<MarketProperty> All { get; } = [];
        public Task<MarketProperty?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(All.Find(p => p.Id == id));
        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([.. All.Where(p => p.OwnerUserId == ownerUserId)]);
        public Task AddAsync(MarketProperty property, CancellationToken ct = default)
        {
            All.Add(property);
            return Task.CompletedTask;
        }
        public Task<bool> TrySaveAsync(MarketProperty property, long expectedVersion, CancellationToken ct = default) =>
            Task.FromResult(true);
    }

    internal sealed class InMemoryListingRepo : IMarketListingRepository
    {
        public List<MarketListing> All { get; } = [];
        public Task<MarketListing?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(All.Find(l => l.Id == id));
        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. All.Where(l => l.PublisherUserId == publisherUserId)]);
        public Task AddAsync(MarketListing listing, CancellationToken ct = default)
        {
            All.Add(listing);
            return Task.CompletedTask;
        }
        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken ct = default) =>
            Task.FromResult(true);
    }
}
