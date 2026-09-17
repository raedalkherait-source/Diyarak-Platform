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

    [Fact]
    public async Task Published_listing_200_uses_public_no_cache_policy()
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
        Assert.True(
            response.Headers.CacheControl is
            { Public: true, NoCache: true, NoStore: false });
    }

    [Fact]
    public async Task Published_listing_304_uses_public_no_cache_policy()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(
            client,
            listing.Id);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id,
            entityTag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.True(
            response.Headers.CacheControl is
            { Public: true, NoCache: true, NoStore: false });
    }

    [Fact]
    public async Task Public_listing_404_uses_no_store_policy()
    {
        using var factory = new TestApiFactory(
            listing: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(
            response.Headers.CacheControl is { NoStore: true });
    }

    [Fact]
    public async Task Get_published_listing_returns_opaque_strong_etag()
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
    public async Task Get_with_matching_if_none_match_returns_304_without_body()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(
            client,
            listing.Id);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id,
            entityTag);

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
    public async Task Get_with_weak_matching_if_none_match_returns_304()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(
            client,
            listing.Id);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id,
            $"W/{entityTag}");

        Assert.Equal(
            HttpStatusCode.NotModified,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_with_if_none_match_wildcard_returns_304()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id,
            "*");

        Assert.Equal(
            HttpStatusCode.NotModified,
            response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task Get_with_non_matching_if_none_match_returns_200_with_current_etag()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendGetAsync(
            client,
            listing.Id,
            "\"different\"");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));
    }

    [Fact]
    public async Task Published_listing_etag_changes_when_persisted_version_changes()
    {
        Guid listingId = Guid.NewGuid();
        MarketListing firstVersion = CreatePublishedListing(
            listingId,
            version: 3);
        MarketListing secondVersion = CreatePublishedListing(
            listingId,
            version: 4);

        string firstEntityTag;
        using (var factory = new TestApiFactory(
                   firstVersion,
                   authenticationEnabled: false))
        using (HttpClient client = factory.CreateClient())
        {
            HttpResponseMessage response = await SendGetAsync(
                client,
                listingId);
            firstEntityTag = Assert.Single(
                response.Headers.GetValues("ETag"));
        }

        string secondEntityTag;
        using (var factory = new TestApiFactory(
                   secondVersion,
                   authenticationEnabled: false))
        using (HttpClient client = factory.CreateClient())
        {
            HttpResponseMessage response = await SendGetAsync(
                client,
                listingId);
            secondEntityTag = Assert.Single(
                response.Headers.GetValues("ETag"));
        }

        Assert.NotEqual(firstEntityTag, secondEntityTag);
    }

    [Fact]
    public async Task Public_listing_response_is_not_forced_no_store()
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
        Assert.False(response.Headers.CacheControl is { NoStore: true });
    }

    [Fact]
    public async Task Head_published_listing_returns_200_without_body_and_matches_get_etag()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage getResponse = await SendGetAsync(
            client,
            listing.Id);
        string entityTag = Assert.Single(
            getResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendHeadAsync(
            client,
            listing.Id);

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
    public async Task Head_with_matching_if_none_match_returns_304_without_body()
    {
        MarketListing listing = CreatePublishedListing();
        using var factory = new TestApiFactory(
            listing,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage initialResponse = await SendGetAsync(
            client,
            listing.Id);
        string entityTag = Assert.Single(
            initialResponse.Headers.GetValues("ETag"));

        HttpResponseMessage response = await SendHeadAsync(
            client,
            listing.Id,
            entityTag);

        Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        Assert.Equal(
            entityTag,
            Assert.Single(response.Headers.GetValues("ETag")));
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Head_missing_listing_returns_404_without_body_and_no_store()
    {
        using var factory = new TestApiFactory(
            listing: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendHeadAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());
        Assert.True(
            response.Headers.CacheControl is { NoStore: true });
    }

    private static async Task<HttpResponseMessage> SendGetAsync(
        HttpClient client,
        Guid listingId,
        string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/market/public/listings/{listingId}");

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
        Guid listingId,
        string? ifNoneMatch = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Head,
            $"/api/market/public/listings/{listingId}");

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

    private static MarketListing CreateReadyDraftListing(
        Guid? listingId = null,
        long version = MarketListing.InitialVersion)
    {
        var listing = MarketListing.Restore(
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
        listing.SetHeadline(
            new ListingHeadline("Property for sale"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.SetAvailableFromDate(
            new ListingAvailableFromDate(
                new DateOnly(2026, 10, 1)));

        return listing;
    }

    private static MarketListing CreatePublishedListing(
        Guid? listingId = null,
        long version = MarketListing.InitialVersion)
    {
        MarketListing listing = CreateReadyDraftListing(
            listingId,
            version);
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
