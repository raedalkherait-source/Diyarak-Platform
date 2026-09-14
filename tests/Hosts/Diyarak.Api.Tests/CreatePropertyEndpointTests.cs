using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Property;
using Diyarak.Platform.Identity;
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
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Tests;

public sealed class CreatePropertyEndpointTests
{
    private const string Issuer = "https://idp.example.test/";
    private const string Audience = "diyarak-api";

    private static readonly string[] PropertyFeatures =
    [
        "FittedKitchen",
        "BalconyOrTerrace",
    ];

    private static readonly SymmetricSecurityKey SigningKey =
        new(
            Encoding.UTF8.GetBytes(
                "diyarak-api-tests-signing-key-32-bytes-minimum-2026"))
        {
            KeyId = "diyarak-api-tests",
        };

    [Fact]
    public async Task Route_is_not_mapped_when_authentication_is_disabled()
    {
        using var factory = new TestApiFactory(
            userId: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendValidCreateAsync(client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(factory.Repository.AddedProperty);
    }

    [Fact]
    public async Task Create_without_access_token_returns_401()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await SendValidCreateAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(factory.Repository.AddedProperty);
    }

    [Fact]
    public async Task Create_with_valid_unmapped_identity_returns_403()
    {
        using var factory = new TestApiFactory(userId: null);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("unmapped-user"));

        HttpResponseMessage response = await SendValidCreateAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(factory.Repository.AddedProperty);
    }

    [Fact]
    public async Task Create_with_invalid_request_returns_400()
    {
        using var factory = new TestApiFactory(Guid.NewGuid());
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/market/properties",
            new
            {
                category = "UnknownCategory",
                address = new
                {
                    street = "Market Street",
                    houseNumber = "12A",
                    postalCode = "23552",
                    city = "Luebeck",
                },
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            CreatePropertyErrors.InvalidRequest.Code);
        Assert.Null(factory.Repository.AddedProperty);
    }

    [Fact]
    public async Task Create_with_mapped_user_returns_201_and_persists_actor_owned_property()
    {
        Guid actorUserId = Guid.NewGuid();
        using var factory = new TestApiFactory(actorUserId);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response = await SendValidCreateAsync(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        MarketProperty property = Assert.IsType<MarketProperty>(
            factory.Repository.AddedProperty);

        Assert.Equal(actorUserId, property.OwnerUserId);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal("Market Street", property.Address.Street);
        Assert.Equal("12A", property.Address.HouseNumber);
        Assert.Equal("23552", property.Address.PostalCode);
        Assert.Equal("Luebeck", property.Address.City);
        Assert.Equal(53.8655, property.Address.Location?.Latitude);
        Assert.Equal(10.6866, property.Address.Location?.Longitude);
        Assert.Equal(82.5m, property.LivingArea?.Value);
        Assert.Equal(3.5m, property.TotalRooms);
        Assert.Equal(FurnishingQuality.Upscale, property.FurnishingQuality);
        Assert.Contains(
            PropertyFeature.BalconyOrTerrace,
            property.Features);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Guid responsePropertyId =
            document.RootElement.GetProperty("propertyId").GetGuid();

        Assert.False(
            document.RootElement.TryGetProperty(
                "ownerUserId",
                out _));
        Assert.Equal(property.Id, responsePropertyId);
        Assert.Equal(
            $"/api/market/properties/{property.Id}",
            response.Headers.Location?.OriginalString);
    }

    private static Task<HttpResponseMessage> SendValidCreateAsync(
        HttpClient client)
    {
        return client.PostAsJsonAsync(
            "/api/market/properties",
            new
            {
                category = "Apartment",
                address = new
                {
                    street = "Market Street",
                    houseNumber = "12A",
                    postalCode = "23552",
                    city = "Luebeck",
                    location = new
                    {
                        latitude = 53.8655,
                        longitude = 10.6866,
                    },
                },
                livingArea = new
                {
                    value = 82.5m,
                    unit = "SquareMeter",
                },
                totalRooms = 3.5m,
                bedroomCount = 2,
                bathroomCount = 1,
                furnishingQuality = "Upscale",
                features = PropertyFeatures,
                constructionYear = 1998,
                lastModernizationYear = 2024,
                parkingSpaceCount = 1,
            });
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

    private static string CreateToken(string subject)
    {
        DateTime now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            SigningKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim("sub", subject),
            ],
            notBefore: now.AddMinutes(-10),
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        token.Header["typ"] = "at+jwt";

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);

        Assert.Equal(
            expectedCode,
            document.RootElement.GetProperty("code").GetString());
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly Guid? _userId;
        private readonly bool _authenticationEnabled;

        public TestApiFactory(
            Guid? userId,
            bool authenticationEnabled = true)
        {
            _userId = userId;
            _authenticationEnabled = authenticationEnabled;
            Repository = new StubMarketPropertyRepository();
        }

        public StubMarketPropertyRepository Repository { get; }

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

                    services.RemoveAll<IMarketPropertyRepository>();
                    services.AddSingleton<IMarketPropertyRepository>(
                        Repository);

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

    private sealed class StubExternalIdentityResolver(Guid? userId)
        : IExternalIdentityResolver
    {
        public Task<Guid?> ResolveUserIdAsync(
            ExternalIdentity identity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId);
    }

    public sealed class StubMarketPropertyRepository
        : IMarketPropertyRepository
    {
        public MarketProperty? AddedProperty { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(null);

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([]);

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default)
        {
            AddedProperty = property;
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync(
            MarketProperty property,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
