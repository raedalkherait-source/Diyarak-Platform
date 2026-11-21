using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Application.Moderation;
using Diyarak.Market.Listing;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Listing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Tests;

public sealed class ModerationEndpointTests
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "diyarak-api";
    private static readonly SymmetricSecurityKey SigningKey = new(Enumerable.Range(7, 32).Select(static value => (byte)value).ToArray());

    private static readonly Guid ModeratorId = Guid.Parse("10000000-0000-0000-0000-00000000000a");
    private static readonly Guid PublisherId = Guid.Parse("10000000-0000-0000-0000-00000000000b");
    private const string ModeratorPhone = "+963911000111";

    [Fact]
    public async Task Only_configured_moderators_can_open_the_queue()
    {
        using var factory = new TestApiFactory([ModeratorPhone]);

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/moderation/queue")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientFor(PublisherId).GetAsync("/api/moderation/queue")).StatusCode);

        HttpResponseMessage allowed = await factory.ClientFor(ModeratorId).GetAsync("/api/moderation/queue");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("no-store", allowed.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task No_configured_moderators_means_nobody_is_a_moderator()
    {
        using var factory = new TestApiFactory([]);

        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientFor(ModeratorId).GetAsync("/api/moderation/queue")).StatusCode);
    }

    [Fact]
    public async Task Report_then_suspend_hides_listing_and_reinstate_restores_it()
    {
        using var factory = new TestApiFactory([ModeratorPhone]);
        MarketListing listing = factory.AddListing(publish: true);
        HttpClient anonymous = factory.CreateClient();
        HttpClient moderator = factory.ClientFor(ModeratorId);

        HttpResponseMessage reported = await anonymous.PostAsJsonAsync(
            $"/api/market/public/listings/{listing.Id}/reports",
            new { reason = "FraudOrScam", comment = "يطلب دفعة مقدمة قبل المعاينة" });
        Assert.Equal(HttpStatusCode.Accepted, reported.StatusCode);

        using (JsonDocument queue = JsonDocument.Parse(await moderator.GetStringAsync("/api/moderation/queue")))
        {
            JsonElement item = Assert.Single(queue.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(listing.Id, item.GetProperty("listing").GetProperty("listingId").GetGuid());
            Assert.Equal("FraudOrScam", Assert.Single(item.GetProperty("openReports").EnumerateArray()).GetProperty("reason").GetString());
        }

        HttpResponseMessage suspended = await moderator.PostAsJsonAsync(
            $"/api/moderation/listings/{listing.Id}/suspend",
            new { note = "احتيال: طلب دفعة مقدمة" });
        Assert.Equal(HttpStatusCode.NoContent, suspended.StatusCode);
        Assert.Equal(ListingStatus.Suspended, listing.Status);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/market/public/listings/{listing.Id}")).StatusCode);

        using (JsonDocument queue = JsonDocument.Parse(await moderator.GetStringAsync("/api/moderation/queue")))
            Assert.Empty(queue.RootElement.GetProperty("items").EnumerateArray());

        using (JsonDocument list = JsonDocument.Parse(await moderator.GetStringAsync("/api/moderation/suspended")))
            Assert.Equal("احتيال: طلب دفعة مقدمة", Assert.Single(list.RootElement.GetProperty("items").EnumerateArray()).GetProperty("moderationNote").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await moderator.PostAsync($"/api/moderation/listings/{listing.Id}/reinstate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/market/public/listings/{listing.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_reports_are_rejected()
    {
        using var factory = new TestApiFactory([ModeratorPhone]);
        MarketListing draft = factory.AddListing(publish: false);
        MarketListing published = factory.AddListing(publish: true);
        HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/market/public/listings/{draft.Id}/reports", new { reason = "Other" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/market/public/listings/{published.Id}/reports", new { reason = "Ugly" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/market/public/listings/{published.Id}/reports", new { reason = "Other", comment = new string('x', 501) })).StatusCode);
    }

    [Fact]
    public async Task Suspend_without_note_is_rejected()
    {
        using var factory = new TestApiFactory([ModeratorPhone]);
        MarketListing listing = factory.AddListing(publish: true);

        HttpResponseMessage response = await factory.ClientFor(ModeratorId).PostAsJsonAsync($"/api/moderation/listings/{listing.Id}/suspend", new { note = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ListingStatus.Published, listing.Status);
    }

    [Fact]
    public void Invalid_moderator_phone_configuration_fails_at_startup()
    {
        using var factory = new TestApiFactory(["0911000111"]);

        Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient());
    }

    private sealed class TestApiFactory(string[] moderatorPhones) : WebApplicationFactory<Program>
    {
        private readonly InMemoryListings _listings = new();
        private readonly InMemoryReports _reports = new();

        public MarketListing AddListing(bool publish)
        {
            var listing = new MarketListing(
                Guid.NewGuid(),
                PublisherId,
                new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
            listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
            listing.SetHeadline(new ListingHeadline("Apartment"));
            listing.SetPrice(ListingPrice.OnRequest());
            if (publish)
                listing.Publish();
            _listings.Items[listing.Id] = listing;
            return listing;
        }

        public HttpClient ClientFor(Guid userId)
        {
            HttpClient client = CreateClient();
            DateTime now = DateTime.UtcNow;
            string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                TokenType = "at+jwt",
                Subject = new ClaimsIdentity([new Claim("sub", userId.ToString("D"))]),
                SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                ["Authentication:Enabled"] = "true",
                ["Authentication:Issuer"] = Issuer,
                ["Authentication:Audience"] = Audience,
            };
            for (int i = 0; i < moderatorPhones.Length; i++)
                settings[$"Moderation:ModeratorPhoneNumbers:{i}"] = moderatorPhones[i];

            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(settings));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IExternalIdentityResolver>();
                services.AddSingleton<IExternalIdentityResolver>(new SubjectResolver());
                services.RemoveAll<IPublisherPhoneLookup>();
                services.AddSingleton<IPublisherPhoneLookup>(new Phones());
                services.RemoveAll<IMarketListingRepository>();
                services.AddSingleton<IMarketListingRepository>(_listings);
                services.RemoveAll<IMarketPropertyRepository>();
                services.AddSingleton<IMarketPropertyRepository>(new AnyProperty());
                services.RemoveAll<Diyarak.Market.Application.Photos.IListingPhotoRepository>();
                services.AddSingleton<Diyarak.Market.Application.Photos.IListingPhotoRepository>(new EmptyListingPhotoRepository());
                services.RemoveAll<Diyarak.Market.Application.Agencies.IAgencyRepository>();
                services.AddSingleton<Diyarak.Market.Application.Agencies.IAgencyRepository>(new EmptyAgencyRepository());
                services.RemoveAll<IListingReportRepository>();
                services.AddSingleton<IListingReportRepository>(_reports);
                services.RemoveAll<IListingModerationQuery>();
                services.AddSingleton<IListingModerationQuery>(_listings);
                services.RemoveAll<IMarketTransactionRunner>();
                services.AddSingleton<IMarketTransactionRunner>(new PassThroughTransactionRunner());
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    configuration.SigningKeys.Add(SigningKey);
                    options.Configuration = configuration;
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
            });
        }
    }

    private sealed class SubjectResolver : IExternalIdentityResolver
    {
        public Task<Guid?> ResolveUserIdAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.TryParse(identity.Subject, out Guid id) ? id : (Guid?)null);
    }

    private sealed class Phones : IPublisherPhoneLookup
    {
        public Task<PhoneNumber?> FindVerifiedPhoneAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PhoneNumber?>(
                userId == ModeratorId ? PhoneNumber.Create(ModeratorPhone)
                : userId == PublisherId ? PhoneNumber.Create("+963922000222")
                : null);
    }

    private sealed class PassThroughTransactionRunner : IMarketTransactionRunner
    {
        public Task<Diyarak.Platform.BuildingBlocks.Result> ExecuteAsync(
            Func<CancellationToken, Task<Diyarak.Platform.BuildingBlocks.Result>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<Diyarak.Platform.BuildingBlocks.Result<T>> ExecuteAsync<T>(
            Func<CancellationToken, Task<Diyarak.Platform.BuildingBlocks.Result<T>>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class InMemoryListings : IMarketListingRepository, IListingModerationQuery
    {
        public Dictionary<Guid, MarketListing> Items { get; } = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. Items.Values.Where(item => item.PublisherUserId == publisherUserId)]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<MarketListing>> ListByStatusAsync(ListingStatus status, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. Items.Values.Where(item => item.Status == status).Skip(skip).Take(take)]);
    }

    private sealed class InMemoryReports : IListingReportRepository
    {
        private readonly List<ListingReport> _items = [];

        public Task AddAsync(ListingReport report, CancellationToken cancellationToken = default)
        {
            _items.Add(report);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ListingReport>> ListOpenAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingReport>>([.. _items.Where(item => item.IsOpen).Skip(skip).Take(take)]);

        public Task<IReadOnlyList<ListingReport>> ListOpenByListingAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListingReport>>([.. _items.Where(item => item.IsOpen && item.ListingId == listingId)]);

        public Task<int> CountRecentAsync(Guid listingId, DateTimeOffset since, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(item => item.ListingId == listingId && item.CreatedAt >= since));

        public Task SaveResolutionsAsync(IReadOnlyCollection<ListingReport> reports, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AnyProperty : IMarketPropertyRepository
    {
        public Task<MarketProperty?> FindByIdAsync(Guid propertyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(new MarketProperty(propertyId, Diyarak.Market.Property.PropertyCategory.Apartment, new Diyarak.Market.Property.PropertyAddress(null, null, null, "Damascus")));

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([]);

        public Task AddAsync(MarketProperty property, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketProperty property, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
