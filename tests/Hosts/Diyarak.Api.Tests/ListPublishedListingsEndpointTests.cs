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

        public Task<IReadOnlyList<MarketListing>> ListPageAsync(
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastSkip = skip;
            LastTake = take;
            return Task.FromResult(listings);
        }
    }
}
