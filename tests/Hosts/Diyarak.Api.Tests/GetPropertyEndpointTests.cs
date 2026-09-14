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

public sealed class GetPropertyEndpointTests
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
        MarketProperty property = CreateProperty(Guid.NewGuid());
        using var factory = new TestApiFactory(
            property,
            userId: null,
            authenticationEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_without_access_token_returns_401()
    {
        MarketProperty property = CreateProperty(Guid.NewGuid());
        using var factory = new TestApiFactory(property, userId: null);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_valid_unmapped_identity_returns_403()
    {
        MarketProperty property = CreateProperty(Guid.NewGuid());
        using var factory = new TestApiFactory(property, userId: null);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("unmapped-user"));

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_invalid_identifier_returns_400()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(actorUserId);
        using var factory = new TestApiFactory(property, actorUserId);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync("/api/market/properties/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetPropertyErrors.InvalidIdentifier.Code);
        Assert.Equal(0, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_missing_property_returns_404()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(actorUserId);
        using var factory = new TestApiFactory(property, actorUserId);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetPropertyErrors.NotFound.Code);
        Assert.Equal(1, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_mapped_non_owner_returns_concealed_404()
    {
        MarketProperty property = CreateProperty(Guid.NewGuid());
        using var factory = new TestApiFactory(property, Guid.NewGuid());
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-non-owner"));

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetPropertyErrors.NotFound.Code);
        Assert.Equal(1, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_legacy_unowned_property_returns_concealed_404()
    {
        MarketProperty property = CreateProperty(ownerUserId: null);
        using var factory = new TestApiFactory(property, Guid.NewGuid());
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(
            response,
            GetPropertyErrors.NotFound.Code);
        Assert.Equal(1, factory.Repository.FindCallCount);
    }

    [Fact]
    public async Task Get_with_owner_returns_complete_property_state()
    {
        Guid actorUserId = Guid.NewGuid();
        MarketProperty property = CreateProperty(actorUserId);
        using var factory = new TestApiFactory(property, actorUserId);
        using HttpClient client = factory.CreateClient();
        AddBearerToken(client, CreateToken("mapped-user"));

        HttpResponseMessage response =
            await client.GetAsync($"/api/market/properties/{property.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Repository.FindCallCount);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(
            property.Id,
            root.GetProperty("propertyId").GetGuid());
        Assert.False(
            root.TryGetProperty(
                "ownerUserId",
                out _));
        Assert.Equal(
            "Apartment",
            root.GetProperty("category").GetString());

        JsonElement address = root.GetProperty("address");
        Assert.Equal(
            "Market Street",
            address.GetProperty("street").GetString());
        Assert.Equal(
            "12A",
            address.GetProperty("houseNumber").GetString());
        Assert.Equal(
            "23552",
            address.GetProperty("postalCode").GetString());
        Assert.Equal(
            "Luebeck",
            address.GetProperty("city").GetString());
        Assert.Equal(
            53.8655,
            address.GetProperty("location")
                .GetProperty("latitude")
                .GetDouble());

        Assert.Equal(
            82.5m,
            root.GetProperty("livingArea")
                .GetProperty("value")
                .GetDecimal());
        Assert.Equal(
            "SquareMeter",
            root.GetProperty("livingArea")
                .GetProperty("unit")
                .GetString());
        Assert.Equal(
            3.5m,
            root.GetProperty("totalRooms").GetDecimal());
        Assert.Equal(
            "Upscale",
            root.GetProperty("furnishingQuality").GetString());
        Assert.Contains(
            root.GetProperty("features").EnumerateArray(),
            feature => feature.GetString() == "BalconyOrTerrace");
        Assert.Equal(
            1,
            root.GetProperty("parkingSpaceCount").GetInt32());
    }

    private static MarketProperty CreateProperty(
        Guid? ownerUserId)
    {
        return new MarketProperty(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            new PropertyAddress(
                "Market Street",
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
            MarketProperty property,
            Guid? userId,
            bool authenticationEnabled = true)
        {
            _userId = userId;
            _authenticationEnabled = authenticationEnabled;
            Repository = new StubMarketPropertyRepository(property);
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
        MarketProperty existingProperty)
        : IMarketPropertyRepository
    {
        public int FindCallCount { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            FindCallCount++;
            return Task.FromResult(
                existingProperty.Id == propertyId
                    ? existingProperty
                    : null);
        }

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketProperty>>([]);

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
