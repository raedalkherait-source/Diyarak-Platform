using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Listing;
using Diyarak.Platform.Storage.FileSystem;
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

public sealed class ListingPhotoEndpointTests : IDisposable
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "diyarak-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(Enumerable.Range(50, 32).Select(static value => (byte)value).ToArray());

    private static readonly Guid Owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Stranger = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly string _storageRoot =
        Path.Combine(Path.GetTempPath(), "diyarak-photo-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_storageRoot))
            Directory.Delete(_storageRoot, recursive: true);
    }

    [Fact]
    public async Task Owner_uploads_lists_downloads_and_deletes_sanitized_photo()
    {
        using var factory = new TestApiFactory(_storageRoot);
        MarketListing listing = factory.AddListing(published: false);
        using HttpClient client = factory.CreateClientFor(Owner);

        HttpResponseMessage uploaded = await UploadAsync(client, listing.Id, Fixture("exif-gps.jpg"), "image/jpeg");

        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        Assert.Equal("no-store", uploaded.Headers.CacheControl?.ToString());
        using JsonDocument created = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        Guid photoId = created.RootElement.GetProperty("photoId").GetGuid();
        Assert.Equal(0, created.RootElement.GetProperty("position").GetInt32());
        Assert.Equal("image/jpeg", created.RootElement.GetProperty("contentType").GetString());
        Assert.Equal(
            $"/api/market/listings/{listing.Id}/photos/{photoId}/content",
            uploaded.Headers.Location?.ToString());

        HttpResponseMessage listed = await client.GetAsync($"/api/market/listings/{listing.Id}/photos");
        using JsonDocument collection = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        Assert.Equal(photoId, Assert.Single(collection.RootElement.GetProperty("items").EnumerateArray()).GetProperty("photoId").GetGuid());

        HttpResponseMessage content = await client.GetAsync(uploaded.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("image/jpeg", content.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", content.Headers.CacheControl?.ToString());
        byte[] bytes = await content.Content.ReadAsByteArrayAsync();
        Assert.Equal(-1, bytes.AsSpan().IndexOf("DiyarakTestCam"u8));

        string storedFile = Path.Combine(_storageRoot, "listings", listing.Id.ToString("N"), $"{photoId:N}.jpg");
        Assert.True(File.Exists(storedFile));

        HttpResponseMessage deleted = await client.DeleteAsync($"/api/market/listings/{listing.Id}/photos/{photoId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(File.Exists(storedFile));
        Assert.Empty(factory.Photos.Photos);
    }

    [Fact]
    public async Task Upload_rejects_unsupported_and_oversized_content()
    {
        using var factory = new TestApiFactory(_storageRoot);
        MarketListing listing = factory.AddListing(published: false);
        using HttpClient client = factory.CreateClientFor(Owner);

        HttpResponseMessage svg = await UploadAsync(client, listing.Id, "<svg/>"u8.ToArray(), "image/svg+xml");
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, svg.StatusCode);
        await AssertProblemCodeAsync(svg, ListingPhotoErrors.UnsupportedFormat.Code);

        byte[] oversized = new byte[ListingPhoto.MaximumSizeBytes + 1];
        HttpResponseMessage large = await UploadAsync(client, listing.Id, oversized, "image/jpeg");
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, large.StatusCode);

        Assert.False(Directory.Exists(Path.Combine(_storageRoot, "listings")));
    }

    [Fact]
    public async Task Non_owner_and_closed_listing_cannot_upload()
    {
        using var factory = new TestApiFactory(_storageRoot);
        MarketListing draft = factory.AddListing(published: false);
        MarketListing published = factory.AddListing(published: true);

        using HttpClient stranger = factory.CreateClientFor(Stranger);
        HttpResponseMessage concealed = await UploadAsync(stranger, draft.Id, Fixture("text-exif.png"), "image/png");
        Assert.Equal(HttpStatusCode.NotFound, concealed.StatusCode);

        using HttpClient owner = factory.CreateClientFor(Owner);
        Assert.Equal(
            HttpStatusCode.Created,
            (await UploadAsync(owner, published.Id, Fixture("text-exif.png"), "image/png")).StatusCode);

        published.Close(ListingClosureReason.Sold);
        HttpResponseMessage locked = await UploadAsync(owner, published.Id, Fixture("text-exif.png"), "image/png");
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        await AssertProblemCodeAsync(locked, ListingPhotoErrors.ListingNotEditable.Code);
    }

    [Fact]
    public async Task Public_photo_is_hidden_until_published_then_cacheable_forever()
    {
        using var factory = new TestApiFactory(_storageRoot);
        MarketListing listing = factory.AddListing(published: false);
        using HttpClient owner = factory.CreateClientFor(Owner);

        HttpResponseMessage uploaded = await UploadAsync(owner, listing.Id, Fixture("exif-xmp.webp"), "image/webp");
        using JsonDocument created = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        Guid photoId = created.RootElement.GetProperty("photoId").GetGuid();
        string publicUrl = $"/api/market/public/listings/{listing.Id}/photos/{photoId}";

        using HttpClient anonymous = factory.CreateClient();

        HttpResponseMessage hidden = await anonymous.GetAsync(publicUrl);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("no-store", hidden.Headers.CacheControl?.ToString());

        listing.Publish();

        HttpResponseMessage visible = await anonymous.GetAsync(publicUrl);
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        Assert.Equal("image/webp", visible.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=31536000, immutable", visible.Headers.CacheControl?.ToString());
        Assert.Equal($"\"{photoId:N}\"", visible.Headers.ETag?.Tag);
        Assert.Equal(-1, (await visible.Content.ReadAsByteArrayAsync()).AsSpan().IndexOf("secret-xmp"u8));

        using var conditional = new HttpRequestMessage(HttpMethod.Get, publicUrl);
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{photoId:N}\""));
        HttpResponseMessage notModified = await anonymous.SendAsync(conditional);
        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
        Assert.Empty(await notModified.Content.ReadAsByteArrayAsync());

        using var head = new HttpRequestMessage(HttpMethod.Head, publicUrl);
        HttpResponseMessage headResponse = await anonymous.SendAsync(head);
        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.Empty(await headResponse.Content.ReadAsByteArrayAsync());

        HttpResponseMessage detail = await anonymous.GetAsync($"/api/market/public/listings/{listing.Id}");
        using JsonDocument detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        JsonElement photo = Assert.Single(detailJson.RootElement.GetProperty("photos").EnumerateArray());
        Assert.Equal(photoId, photo.GetProperty("photoId").GetGuid());
        Assert.Equal(publicUrl, photo.GetProperty("url").GetString());
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid listingId, byte[] content, string contentType)
    {
        var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return client.PostAsync($"/api/market/listings/{listingId}/photos", body);
    }

    private static byte[] Fixture(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
    }

    private sealed class TestApiFactory(string storageRoot) : WebApplicationFactory<Program>
    {
        public InMemoryListingRepository Listings { get; } = new();

        public EmptyListingPhotoRepository Photos { get; } = new();

        public MarketListing AddListing(bool published)
        {
            var listing = new MarketListing(
                Guid.NewGuid(),
                Owner,
                new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property));
            listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Rent));
            listing.SetHeadline(new ListingHeadline("Apartment in Mezzeh"));
            listing.SetPrice(ListingPrice.OnRequest());

            if (published)
                listing.Publish();

            Listings.Items[listing.Id] = listing;
            return listing;
        }

        public HttpClient CreateClientFor(Guid userId)
        {
            HttpClient client = CreateClient();
            DateTime now = DateTime.UtcNow;
            string token = new JsonWebTokenHandler().CreateToken(
                new SecurityTokenDescriptor
                {
                    Issuer = Issuer,
                    Audience = Audience,
                    IssuedAt = now,
                    NotBefore = now,
                    Expires = now.AddMinutes(10),
                    TokenType = "at+jwt",
                    Subject = new ClaimsIdentity([new Claim("sub", userId.ToString("D"))]),
                    SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
                });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(
                configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] =
                            "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                        ["Authentication:Enabled"] = "true",
                        ["Authentication:Issuer"] = Issuer,
                        ["Authentication:Audience"] = Audience,
                        ["Media:FileSystem:RootPath"] = storageRoot,
                    }));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                services =>
                {
                    services.RemoveAll<IExternalIdentityResolver>();
                    services.AddSingleton<IExternalIdentityResolver>(new SubjectAsUserResolver());

                    services.RemoveAll<IMarketListingRepository>();
                    services.AddSingleton<IMarketListingRepository>(Listings);

                    services.RemoveAll<IMarketPropertyRepository>();
                    services.AddSingleton<IMarketPropertyRepository>(new AnyPropertyRepository());

                    services.RemoveAll<IListingPhotoRepository>();
                    services.AddSingleton<IListingPhotoRepository>(Photos);

                    services.RemoveAll<IListingPhotoStorage>();
                    services.AddSingleton<IListingPhotoStorage>(new FileSystemListingPhotoStorage(storageRoot));

                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                            configuration.SigningKeys.Add(SigningKey);
                            options.Configuration = configuration;
                            options.ConfigurationManager =
                                new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                        });
                });
        }
    }

    private sealed class SubjectAsUserResolver : IExternalIdentityResolver
    {
        public Task<Guid?> ResolveUserIdAsync(ExternalIdentity identity, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.TryParse(identity.Subject, out Guid userId) ? userId : (Guid?)null);
    }

    internal sealed class InMemoryListingRepository : IMarketListingRepository
    {
        public Dictionary<Guid, MarketListing> Items { get; } = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. Items.Values.Where(item => item.PublisherUserId == publisherUserId)]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class AnyPropertyRepository : IMarketPropertyRepository
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
