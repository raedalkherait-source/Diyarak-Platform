using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Listing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Tests;

public sealed class PublishListingEndpointTests
{
    private const string Issuer = "https://idp.example.test/";
    private const string Audience = "diyarak-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(
            Encoding.UTF8.GetBytes(
                "diyarak-api-tests-signing-key-32-bytes-minimum-2026"))
        {
            KeyId = "diyarak-api-tests",
        };

    [Fact]
    public async Task Publication_route_is_not_mapped_when_authentication_is_disabled()
    {
        using var factory = new TestApiFactory(
            userId: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Creation_route_is_not_mapped_when_authentication_is_disabled()
    {
        using var factory = new TestApiFactory(
            userId: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendCreateAsync(
            client,
            Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.AddedListing);
    }

    [Fact]
    public async Task Create_without_access_token_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendCreateAsync(
            client,
            Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.AddedListing);
    }

    [Fact]
    public async Task Create_with_valid_unmapped_identity_returns_403()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "unmapped-user"));

        HttpResponseMessage response = await SendCreateAsync(
            client,
            Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.AddedListing);
    }

    [Fact]
    public async Task Create_with_malformed_property_identifier_returns_400()
    {
        using var factory = new TestApiFactory(Guid.NewGuid());
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-user"));

        HttpResponseMessage response = await SendCreateAsync(
            client,
            "not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            CreateListingErrors.InvalidPropertyIdentifier.Code);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.AddedListing);
    }

    [Fact]
    public async Task Create_with_missing_property_returns_409()
    {
        using var factory = new TestApiFactory(
            Guid.NewGuid(),
            propertyExists: false);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-user"));

        HttpResponseMessage response = await SendCreateAsync(
            client,
            Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            CreateListingErrors.PropertyNotFound.Code);
        Assert.Equal(1, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.AddedListing);
    }

    [Fact]
    public async Task Create_with_mapped_user_returns_201_and_persists_owned_draft()
    {
        Guid userId = Guid.NewGuid();
        Guid propertyId = Guid.NewGuid();
        using var factory = new TestApiFactory(userId);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-user"));

        Guid forgedPublisherUserId = Guid.NewGuid();
        Assert.NotEqual(userId, forgedPublisherUserId);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/market/listings",
            new
            {
                propertyId = propertyId.ToString(),
                publisherUserId = forgedPublisherUserId,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, factory.PropertyChecker.CallCount);

        MarketListing listing = Assert.IsType<MarketListing>(
            factory.ListingRepository.AddedListing);

        Assert.Equal(userId, listing.PublisherUserId);
        Assert.Equal(propertyId, listing.SubjectReference.SubjectId);
        Assert.Equal(
            MarketListingSubjectTypes.Property,
            listing.SubjectReference.SubjectType);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Null(factory.ListingRepository.SavedListing);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Guid responseListingId =
            document.RootElement
                .GetProperty("listingId")
                .GetGuid();

        Assert.Equal(listing.Id, responseListingId);
        Assert.Equal(
            listing.Version,
            document.RootElement.GetProperty("version").GetInt64());
        Assert.Equal(
            $"/api/market/listings/{listing.Id}",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Publish_without_access_token_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_invalid_access_token_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                "not-a-jwt");

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_wrong_signature_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        var untrustedKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                "untrusted-tests-signing-key-32-bytes-minimum-2026"))
        {
            KeyId = "untrusted-tests",
        };

        AddBearerToken(
            client,
            CreateToken(
                subject: "external-user",
                signingKey: untrustedKey));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_untrusted_issuer_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(
                subject: "external-user",
                issuer: "https://untrusted.example.test/"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_wrong_audience_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(
                subject: "external-user",
                audience: "some-other-api"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_expired_access_token_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(
                subject: "external-user",
                expires: DateTime.UtcNow.AddMinutes(-2)));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_id_token_shape_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(
                subject: "external-user",
                tokenType: "JWT"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_valid_unmapped_identity_returns_403()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "unmapped-user"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_mapped_non_owner_returns_concealed_404()
    {
        MarketListing listing = CreateReadyListing();
        Guid nonOwnerUserId = Guid.NewGuid();
        Assert.NotEqual(listing.PublisherUserId, nonOwnerUserId);

        using var factory = new TestApiFactory(
            nonOwnerUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-non-owner"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            PublishListingErrors.NotFound.Code);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Publish_with_mapped_user_and_missing_listing_returns_same_404_code()
    {
        using var factory = new TestApiFactory(
            Guid.NewGuid(),
            listing: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-user"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            PublishListingErrors.NotFound.Code);
        Assert.Equal(1, factory.ListingRepository.FindCallCount);
        Assert.Equal(0, factory.PropertyChecker.CallCount);
    }

    [Fact]
    public async Task Publish_with_malformed_listing_identifier_returns_400()
    {
        Guid userId = Guid.NewGuid();
        using var factory = new TestApiFactory(userId);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-user"));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/market/listings/not-a-guid/publish");
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            PublishListingErrors.InvalidIdentifier.Code);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Publish_with_missing_property_returns_409()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing,
            propertyExists: false);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            PublishListingErrors.PropertyNotFound.Code);
        Assert.Equal(1, factory.PropertyChecker.CallCount);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Publish_with_concurrent_modification_returns_409()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        factory.ListingRepository.SaveAccepted = false;
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            PublishListingErrors.ConcurrentModification.Code);
        Assert.Equal(
            listing.Version,
            factory.ListingRepository.LastExpectedVersion);
        Assert.Equal(1, factory.PropertyChecker.CallCount);
    }

    [Fact]
    public async Task Publish_with_mapped_owner_returns_204_and_persists_publication()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(
            client,
            CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendPublishAsync(
            client,
            listing.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Same(
            listing,
            factory.ListingRepository.SavedListing);
        Assert.Equal(
            listing.Version,
            factory.ListingRepository.LastExpectedVersion);
        Assert.Equal(1, factory.PropertyChecker.CallCount);
    }

    [Fact]
    public async Task Update_without_access_token_returns_401()
    {
        using var factory = new TestApiFactory(Guid.NewGuid());
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            Guid.NewGuid(),
            """{"version":1,"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Update_with_valid_unmapped_identity_returns_403()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "unmapped-user"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            Guid.NewGuid(),
            """{"version":1,"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Update_with_mapped_non_owner_returns_concealed_404()
    {
        MarketListing listing = CreateReadyListing();
        Guid nonOwnerUserId = Guid.NewGuid();
        using var factory = new TestApiFactory(nonOwnerUserId, listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-non-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version}},"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, UpdateListingErrors.NotFound.Code);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Update_with_invalid_patch_returns_400()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            """{"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            UpdateListingErrors.InvalidPatch.Code);
        Assert.Equal(0, factory.ListingRepository.FindCallCount);
    }

    [Fact]
    public async Task Update_with_stale_version_returns_409()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version + 1}},"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            UpdateListingErrors.ConcurrentModification.Code);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Update_published_listing_returns_409()
    {
        MarketListing listing = CreateReadyListing();
        listing.Publish();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version}},"headline":"Updated"}""");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertProblemCodeAsync(response, UpdateListingErrors.CannotEdit.Code);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Update_with_mapped_owner_applies_partial_patch_and_returns_next_version()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version}},"headline":"Updated headline"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated headline", listing.Headline!.Value);
        Assert.Same(listing, factory.ListingRepository.SavedListing);
        Assert.Equal(
            listing.Version,
            factory.ListingRepository.LastExpectedVersion);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal(listing.Id, document.RootElement.GetProperty("listingId").GetGuid());
        Assert.Equal(
            listing.Version + 1,
            document.RootElement.GetProperty("version").GetInt64());
    }

    [Fact]
    public async Task Update_can_replace_context_and_known_price()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$$"""{"version":{{{listing.Version}}},"context":{"publishingRole":"ProfessionalOrAgent","transactionIntent":"Sell"},"price":{"isOnRequest":false,"amount":250000,"currency":"EUR"}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            PublishingRole.ProfessionalOrAgent,
            listing.Context!.PublishingRole);
        Assert.Equal(TransactionIntent.Sell, listing.Context.TransactionIntent);
        Assert.False(listing.Price!.IsOnRequest);
        Assert.Equal(250_000m, listing.Price.Amount!.Value.Amount);
        Assert.Equal("EUR", listing.Price.Amount.Value.Currency.Code);
    }

    [Fact]
    public async Task Update_with_version_only_returns_400_no_changes()
    {
        MarketListing listing = CreateReadyListing();
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version}}}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, UpdateListingErrors.NoChanges.Code);
        Assert.Null(factory.ListingRepository.SavedListing);
    }

    [Fact]
    public async Task Update_can_clear_available_from_date_with_explicit_null()
    {
        MarketListing listing = CreateReadyListing();
        listing.SetAvailableFromDate(
            new ListingAvailableFromDate(new DateOnly(2026, 10, 1)));
        using var factory = new TestApiFactory(
            listing.PublisherUserId,
            listing);
        using HttpClient client = factory.CreateClient();

        AddBearerToken(client, CreateToken(subject: "mapped-owner"));

        HttpResponseMessage response = await SendUpdateAsync(
            client,
            listing.Id,
            $$"""{"version":{{listing.Version}},"availableFromDate":null}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(listing.AvailableFromDate);
    }

    private static Task<HttpResponseMessage> SendCreateAsync(
        HttpClient client,
        string propertyId) =>
        client.PostAsJsonAsync(
            "/api/market/listings",
            new
            {
                propertyId,
            });

    private static async Task<HttpResponseMessage> SendUpdateAsync(
        HttpClient client,
        Guid listingId,
        string json)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/market/listings/{listingId}")
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"),
        };

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendPublishAsync(
        HttpClient client,
        Guid listingId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/market/listings/{listingId}/publish");

        return await client.SendAsync(request);
    }

    private static void AddBearerToken(
        HttpClient client,
        string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                JwtBearerDefaults.AuthenticationScheme,
                token);
    }

    private static string CreateToken(
        string subject,
        string issuer = Issuer,
        string audience = Audience,
        string tokenType = "at+jwt",
        SecurityKey? signingKey = null,
        DateTime? expires = null)
    {
        DateTime now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            signingKey ?? SigningKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim("sub", subject),
            ],
            notBefore: now.AddMinutes(-10),
            expires: expires ?? now.AddMinutes(5),
            signingCredentials: credentials);

        token.Header["typ"] = tokenType;

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private static MarketListing CreateReadyListing()
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

        return listing;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly Guid? _userId;

        private readonly bool _authenticationEnabled;

        public TestApiFactory(
            Guid? userId,
            MarketListing? listing = null,
            bool propertyExists = true,
            bool authenticationEnabled = true)
        {
            _userId = userId;
            _authenticationEnabled = authenticationEnabled;
            ListingRepository =
                new StubMarketListingRepository(listing);
            PropertyChecker =
                new StubPropertyExistenceChecker(propertyExists);
        }

        public StubMarketListingRepository ListingRepository { get; }

        public StubPropertyExistenceChecker PropertyChecker { get; }

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
                            ["Authentication:Issuer"] = Issuer,
                            ["Authentication:Audience"] = Audience,
                            ["Authentication:RequireHttpsMetadata"] = "true",
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
                    services.RemoveAll<IExternalIdentityResolver>();
                    services.AddSingleton<IExternalIdentityResolver>(
                        new StubExternalIdentityResolver(_userId));

                    services.RemoveAll<IMarketListingRepository>();
                    services.AddSingleton<IMarketListingRepository>(
                        ListingRepository);

                    services.RemoveAll<IPropertyExistenceChecker>();
                    services.AddSingleton<IPropertyExistenceChecker>(
                        PropertyChecker);

                    services.RemoveAll<IMarketTransactionRunner>();
                    services.AddSingleton<IMarketTransactionRunner>(
                        new StubMarketTransactionRunner());

                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            var configuration =
                                new OpenIdConnectConfiguration
                                {
                                    Issuer = Issuer,
                                };

                            configuration.SigningKeys.Add(SigningKey);
                            options.Configuration = configuration;
                            options.ConfigurationManager =
                                new StaticConfigurationManager<
                                    OpenIdConnectConfiguration>(
                                    configuration);
                        });
                });
        }
    }

    private sealed class StubMarketTransactionRunner
        : IMarketTransactionRunner
    {
        public async Task<Result> ExecuteAsync(
            Func<CancellationToken, Task<Result>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return await operation(cancellationToken);
        }

        public async Task<Result<T>> ExecuteAsync<T>(
            Func<CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return await operation(cancellationToken);
        }
    }

    private sealed class StubExternalIdentityResolver(
        Guid? userId)
        : IExternalIdentityResolver
    {
        public Task<Guid?> ResolveUserIdAsync(
            ExternalIdentity identity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId);
    }

    public sealed class StubMarketListingRepository(
        MarketListing? listing)
        : IMarketListingRepository
    {
        public int FindCallCount { get; private set; }

        public MarketListing? SavedListing { get; private set; }

        public Task<MarketListing?> FindByIdAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;

            MarketListing? result =
                listing?.Id == listingId
                    ? listing
                    : null;

            return Task.FromResult(result);
        }

        public MarketListing? AddedListing { get; private set; }

        public Task AddAsync(
            MarketListing listing,
            CancellationToken cancellationToken = default)
        {
            AddedListing = listing;
            return Task.CompletedTask;
        }

        public bool SaveAccepted { get; set; } = true;

        public long? LastExpectedVersion { get; private set; }

        public Task<bool> TrySaveAsync(
            MarketListing listing,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            SavedListing = listing;
            LastExpectedVersion = expectedVersion;
            return Task.FromResult(SaveAccepted);
        }
    }

    public sealed class StubPropertyExistenceChecker(
        bool exists)
        : IPropertyExistenceChecker
    {
        public int CallCount { get; private set; }

        public Task<bool> ExistsAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(exists);
        }
    }
}

