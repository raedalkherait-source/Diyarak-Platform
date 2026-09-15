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

public sealed class GetPublishedListingEndpointTests
{
    [Fact]
    public async Task Public_route_is_mapped_when_authentication_is_disabled()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Public_route_does_not_require_access_token_when_authentication_is_enabled()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_malformed_listing_identifier_returns_400()
    {
        using var factory = new TestApiFactory(
            listing: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/market/public/listings/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetListingErrors.InvalidIdentifier.Code);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Get_missing_listing_returns_404()
    {
        using var factory = new TestApiFactory(
            listing: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetListingErrors.NotFound.Code);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Get_draft_listing_returns_concealed_404()
    {
        MarketListing listing = CreateReadyDraftListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetListingErrors.NotFound.Code);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Get_published_listing_returns_public_projection_only()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(
            listing.Id,
            root.GetProperty("listingId").GetGuid());
        Assert.Equal(
            "Owner",
            root.GetProperty("context")
                .GetProperty("publishingRole")
                .GetString());
        Assert.Equal(
            "Sell",
            root.GetProperty("context")
                .GetProperty("transactionIntent")
                .GetString());
        Assert.Equal(
            "Property for sale",
            root.GetProperty("headline").GetString());
        Assert.True(
            root.GetProperty("price")
                .GetProperty("isOnRequest")
                .GetBoolean());
        Assert.Equal(
            "2026-10-01",
            root.GetProperty("availableFromDate").GetString());

        Assert.False(root.TryGetProperty("version", out _));
        Assert.False(root.TryGetProperty("status", out _));
        Assert.False(root.TryGetProperty("subject", out _));
        Assert.False(root.TryGetProperty("publisherUserId", out _));
    }

    private static Task<HttpResponseMessage> SendGetAsync(
        HttpClient client,
        Guid listingId) =>
        client.GetAsync(
            $"/api/market/public/listings/{listingId}");

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

    private static MarketListing CreateReadyDraftListing()
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
            new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.SetAvailableFromDate(
            new ListingAvailableFromDate(
                new DateOnly(2026, 10, 1)));

        return listing;
    }

    private static MarketListing CreatePublishedListing()
    {
        MarketListing listing = CreateReadyDraftListing();
        listing.Publish();
        return listing;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly bool _authenticationEnabled;

        public TestApiFactory(
            MarketListing? listing,
            bool authenticationEnabled)
        {
            _authenticationEnabled = authenticationEnabled;
            ListingRepository =
                new StubMarketListingRepository(listing);
        }

        public StubMarketListingRepository ListingRepository { get; }

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
                    services.RemoveAll<IMarketListingRepository>();
                    services.AddSingleton<IMarketListingRepository>(
                        ListingRepository);
                });
        }
    }

    public sealed class StubMarketListingRepository(
        MarketListing? listing)
        : IMarketListingRepository
    {
        public int FindCallCount { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;

            return Task.FromResult(
                listing?.Id == listingId
                    ? listing
                    : null);
        }

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(
            Guid publisherUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>(
                Array.Empty<MarketListing>());

        public Task AddAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(
            MarketListing listing,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
