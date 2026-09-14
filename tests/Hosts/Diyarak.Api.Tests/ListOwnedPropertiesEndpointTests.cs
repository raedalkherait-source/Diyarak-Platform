using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Market.Property;
using Diyarak.Platform.Domain.Primitives;
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

public sealed class ListOwnedPropertiesEndpointTests
{
    private const string Issuer = "https://issuer.example.test";
    private const string Audience = "diyarak-api";

    private static readonly SymmetricSecurityKey SigningKey =
        new(
            Encoding.UTF8.GetBytes(
                "diyarak-api-tests-signing-key-which-is-long-enough-123456"))
        {
            KeyId = "diyarak-api-tests",
        };

    private static readonly PropertyFeature[] ExistingFeatures =
    [
        PropertyFeature.FittedKitchen,
        PropertyFeature.BalconyOrTerrace,
    ];

    [Fact]
    public async Task Route_is_not_mapped_when_authentication_is_disabled()
    {
        using var factory = new TestApiFactory(
            userId: null,
            properties: [],
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task List_without_access_token_returns_401()
    {
        using var factory = new TestApiFactory(
            userId: null,
            properties: []);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task List_with_valid_unmapped_identity_returns_403()
    {
        using var factory = new TestApiFactory(
            userId: null,
            properties: []);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("unmapped-user"));

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindByOwnerCallCount);
    }

    [Fact]
    public async Task List_with_mapped_user_returns_empty_array_when_no_property_exists()
    {
        Guid actorUserId = Guid.NewGuid();
        using var factory = new TestApiFactory(
            actorUserId,
            properties: []);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Repository.FindByOwnerCallCount);
        Assert.Equal(actorUserId, factory.Repository.LastOwnerUserId);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task List_with_mapped_user_does_not_expose_other_or_legacy_unowned_properties()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty other = CreateProperty(Guid.NewGuid(), "Other Street");
        MarketProperty legacy = CreateProperty(null, "Legacy Street");
        using var factory = new TestApiFactory(
            actorUserId,
            [other, legacy]);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task List_with_mapped_owner_returns_management_representation()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty owned = CreateProperty(actorUserId, "Market Street");
        MarketProperty other = CreateProperty(Guid.NewGuid(), "Other Street");
        MarketProperty legacy = CreateProperty(null, "Legacy Street");
        using var factory = new TestApiFactory(
            actorUserId,
            [owned, other, legacy]);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Repository.FindByOwnerCallCount);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        Assert.Equal(1, root.GetArrayLength());

        JsonElement item = root[0];
        Assert.Equal(
            owned.Id,
            item.GetProperty("propertyId").GetGuid());
        Assert.False(item.TryGetProperty("ownerUserId", out _));
        Assert.Equal(
            "Apartment",
            item.GetProperty("category").GetString());
        Assert.Equal(
            "Market Street",
            item.GetProperty("address")
                .GetProperty("street")
                .GetString());
        Assert.Equal(
            82.5m,
            item.GetProperty("livingArea")
                .GetProperty("value")
                .GetDecimal());
        Assert.Contains(
            item.GetProperty("features").EnumerateArray(),
            feature => feature.GetString() == "BalconyOrTerrace");
    }

    private static MarketProperty CreateProperty(
        Guid? ownerUserId,
        string street)
    {
        return new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            new PropertyAddress(
                street,
                "12A",
                "23552",
                "Luebeck",
                new GeoCoordinate(53.8655, 10.6866)),
            livingArea: new Area(82.5m, AreaUnit.SquareMeter),
            usableArea: new Area(91m, AreaUnit.SquareMeter),
            totalRooms: 3.5m,
            bedroomCount: 2,
            bathroomCount: 1,
            furnishingQuality: FurnishingQuality.Upscale,
            features: ExistingFeatures,
            constructionYear: 1998,
            lastModernizationYear: 2024,
            parkingSpaceCount: 1,
            ownerUserId: ownerUserId);
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

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly Guid? _userId;
        private readonly bool _authenticationEnabled;

        public TestApiFactory(
            Guid? userId,
            IReadOnlyList<MarketProperty> properties,
            bool authenticationEnabled = true)
        {
            _userId = userId;
            _authenticationEnabled = authenticationEnabled;
            Repository = new StubMarketPropertyRepository(properties);
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

    public sealed class StubMarketPropertyRepository(
        IReadOnlyList<MarketProperty> properties)
        : IMarketPropertyRepository
    {
        public int FindByOwnerCallCount { get; private set; }

        public Guid? LastOwnerUserId { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(null);

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default)
        {
            FindByOwnerCallCount++;
            LastOwnerUserId = ownerUserId;

            IReadOnlyList<MarketProperty> owned = properties
                .Where(property => property.OwnerUserId == ownerUserId)
                .ToArray();

            return Task.FromResult(owned);
        }

        public Task AddAsync(
            MarketProperty property,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(
            MarketProperty property,
            long expectedVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
