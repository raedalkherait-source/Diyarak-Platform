using System.Net;
using System.Text.Json;
using Diyarak.Api.Market;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Tests;

public sealed class ListingContactEndpointTests
{
    private static readonly Guid Publisher = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public async Task Published_opted_in_listing_reveals_number_without_caching()
    {
        using var factory = new TestApiFactory();
        MarketListing listing = factory.AddListing(new ListingContactOptions(true, true), publish: true);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/market/public/listings/{listing.Id}/contact");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").Single());

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("+963912345678", body.RootElement.GetProperty("phoneNumber").GetString());
        Assert.Equal("tel:+963912345678", body.RootElement.GetProperty("callUrl").GetString());
        Assert.Equal("https://wa.me/963912345678", body.RootElement.GetProperty("whatsAppUrl").GetString());
    }

    [Fact]
    public async Task Disallowed_channel_urls_are_null()
    {
        using var factory = new TestApiFactory();
        MarketListing listing = factory.AddListing(new ListingContactOptions(false, true), publish: true);
        using HttpClient client = factory.CreateClient();

        using JsonDocument body = JsonDocument.Parse(
            await client.GetStringAsync($"/api/market/public/listings/{listing.Id}/contact"));

        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("callUrl").ValueKind);
        Assert.Equal("https://wa.me/963912345678", body.RootElement.GetProperty("whatsAppUrl").GetString());
    }

    [Fact]
    public async Task Draft_or_opted_out_listing_returns_404()
    {
        using var factory = new TestApiFactory();
        MarketListing draft = factory.AddListing(new ListingContactOptions(true, true), publish: false);
        MarketListing optedOut = factory.AddListing(null, publish: true);
        using HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/market/public/listings/{draft.Id}/contact")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/market/public/listings/{optedOut.Id}/contact")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/market/public/listings/not-a-guid/contact")).StatusCode);
    }

    [Fact]
    public async Task Public_listing_exposes_channels_but_not_the_number()
    {
        using var factory = new TestApiFactory();
        MarketListing listing = factory.AddListing(new ListingContactOptions(false, true), publish: true);
        using HttpClient client = factory.CreateClient();

        string json = await client.GetStringAsync($"/api/market/public/listings/{listing.Id}");
        using JsonDocument body = JsonDocument.Parse(json);

        JsonElement contact = body.RootElement.GetProperty("contact");
        Assert.False(contact.GetProperty("allowCalls").GetBoolean());
        Assert.True(contact.GetProperty("allowWhatsApp").GetBoolean());
        Assert.DoesNotContain("912345678", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Contact_route_has_stricter_per_client_limit()
    {
        using var factory = new TestApiFactory(contactPermitLimit: 2);
        MarketListing listing = factory.AddListing(new ListingContactOptions(true, false), publish: true);
        using HttpClient client = factory.CreateClient();
        string url = $"/api/market/public/listings/{listing.Id}/contact";

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/market/public/listings/{listing.Id}")).StatusCode);
    }

    [Fact]
    public void Invalid_trusted_proxy_configuration_fails_at_startup()
    {
        using var factory = new TestApiFactory(knownProxy: "not-an-ip");

        Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient());
    }

    [Theory]
    [InlineData("""{ "version": 1, "contact": { "allowCalls": true, "allowWhatsApp": false } }""", true, true, false)]
    [InlineData("""{ "version": 1, "contact": null }""", true, null, null)]
    public void Update_parser_accepts_contact_options(string json, bool expectedUpdate, bool? calls, bool? whatsApp)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.True(MarketListingUpdateRequestParser.TryParse(document.RootElement, out UpdateListingPatch patch));
        Assert.Equal(expectedUpdate, patch.UpdateContact);
        Assert.Equal(calls, patch.Contact?.AllowCalls);
        Assert.Equal(whatsApp, patch.Contact?.AllowWhatsApp);
    }

    [Theory]
    [InlineData("""{ "version": 1, "contact": { "allowCalls": false, "allowWhatsApp": false } }""")]
    [InlineData("""{ "version": 1, "contact": { "allowCalls": true } }""")]
    [InlineData("""{ "version": 1, "contact": { "allowCalls": "yes", "allowWhatsApp": true } }""")]
    [InlineData("""{ "version": 1, "contact": "+963912345678" }""")]
    public void Update_parser_rejects_invalid_contact_options(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.False(MarketListingUpdateRequestParser.TryParse(document.RootElement, out _));
    }

    private sealed class TestApiFactory(int contactPermitLimit = 30, string? knownProxy = null)
        : WebApplicationFactory<Program>
    {
        private readonly ListingContactInMemoryRepository _listings = new();

        public MarketListing AddListing(ListingContactOptions? contact, bool publish)
        {
            var listing = new MarketListing(
                Guid.NewGuid(),
                Publisher,
                new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
            listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
            listing.SetHeadline(new ListingHeadline("Apartment"));
            listing.SetPrice(ListingPrice.OnRequest());

            if (contact is not null)
                listing.SetContact(contact);

            if (publish)
                listing.Publish();

            _listings.Items[listing.Id] = listing;
            return listing;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                ["Authentication:Enabled"] = "false",
                ["RateLimiting:Contact:PermitLimit"] =
                    contactPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            if (knownProxy is not null)
                settings["ReverseProxy:KnownProxies:0"] = knownProxy;

            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(settings));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                services =>
                {
                    services.RemoveAll<IMarketListingRepository>();
                    services.AddSingleton<IMarketListingRepository>(_listings);
                    services.RemoveAll<IMarketPropertyRepository>();
                    services.AddSingleton<IMarketPropertyRepository>(new ListingContactPropertyRepository());
                    services.RemoveAll<Diyarak.Market.Application.Photos.IListingPhotoRepository>();
                    services.AddSingleton<Diyarak.Market.Application.Photos.IListingPhotoRepository>(new EmptyListingPhotoRepository());
                    services.RemoveAll<IPublisherPhoneLookup>();
                    services.AddSingleton<IPublisherPhoneLookup>(new FixedPhoneLookup());
                });
        }
    }

    private sealed class FixedPhoneLookup : IPublisherPhoneLookup
    {
        public Task<PhoneNumber?> FindVerifiedPhoneAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == Publisher ? PhoneNumber.Create("+963912345678") : null);
    }

    private sealed class ListingContactInMemoryRepository : IMarketListingRepository
    {
        public Dictionary<Guid, MarketListing> Items { get; } = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. Items.Values]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class ListingContactPropertyRepository : IMarketPropertyRepository
    {
        public Task<MarketProperty?> FindByIdAsync(Guid propertyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(
                new MarketProperty(
                    propertyId,
                    Diyarak.Market.Property.PropertyCategory.Apartment,
                    new Diyarak.Market.Property.PropertyAddress(null, null, null, "Damascus")));

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([]);

        public Task AddAsync(MarketProperty property, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketProperty property, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
