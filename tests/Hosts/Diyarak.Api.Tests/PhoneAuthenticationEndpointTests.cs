using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
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
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Api.Tests;

public sealed class PhoneAuthenticationEndpointTests
{
    private const string Phone = "+963912345678";
    private const string PhoneIssuer = "https://auth.diyarak.test";
    private const string PhoneAudience = "diyarak-api";
    private const string OidcIssuer = "https://issuer.example.test";

    private static readonly string PhoneSigningKey =
        Convert.ToBase64String(Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray());

    private static readonly SymmetricSecurityKey OidcSigningKey =
        new(Enumerable.Range(100, 32).Select(static value => (byte)value).ToArray());

    [Fact]
    public async Task Phone_sign_in_issues_token_accepted_by_management_routes()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage requested = await client.PostAsJsonAsync(
            "/api/auth/phone/request",
            new { phoneNumber = Phone });

        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        Assert.Equal("no-store", requested.Headers.CacheControl?.ToString());
        (PhoneNumber sentTo, string code) = Assert.Single(factory.Sender.Sent);
        Assert.Equal(Phone, sentTo.Value);

        HttpResponseMessage verified = await client.PostAsJsonAsync(
            "/api/auth/phone/verify",
            new { phoneNumber = Phone, code });

        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.Equal("no-store", verified.Headers.CacheControl?.ToString());

        using JsonDocument body = JsonDocument.Parse(await verified.Content.ReadAsStringAsync());
        string accessToken = body.RootElement.GetProperty("accessToken").GetString()!;
        Guid userId = body.RootElement.GetProperty("userId").GetGuid();
        Assert.Equal("Bearer", body.RootElement.GetProperty("tokenType").GetString());
        Assert.True(body.RootElement.GetProperty("isNewUser").GetBoolean());

        JsonWebToken token = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
        Assert.Equal(PhoneIssuer, token.Issuer);
        Assert.Equal(userId.ToString("D"), token.Subject);
        Assert.Equal("at+jwt", token.Typ);
        Assert.DoesNotContain(Phone, accessToken, StringComparison.Ordinal);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage properties = await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.OK, properties.StatusCode);
        Assert.Equal(userId, factory.Properties.LastOwnerUserId);
    }

    [Fact]
    public async Task Refresh_token_renews_session_rotates_and_supports_logout()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = Phone });
        HttpResponseMessage verified = await client.PostAsJsonAsync(
            "/api/auth/phone/verify",
            new { phoneNumber = Phone, code = factory.Sender.Sent[0].Code });
        using JsonDocument signIn = JsonDocument.Parse(await verified.Content.ReadAsStringAsync());
        string refresh1 = signIn.RootElement.GetProperty("refreshToken").GetString()!;
        Assert.True(signIn.RootElement.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddDays(29));

        HttpResponseMessage refreshed = await client.PostAsJsonAsync("/api/auth/session/refresh", new { refreshToken = refresh1 });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal("no-store", refreshed.Headers.CacheControl?.ToString());
        using JsonDocument tokens = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        string refresh2 = tokens.RootElement.GetProperty("refreshToken").GetString()!;
        string access2 = tokens.RootElement.GetProperty("accessToken").GetString()!;
        Assert.NotEqual(refresh1, refresh2);

        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/market/properties"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access2);
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        }

        HttpResponseMessage stale = await client.PostAsJsonAsync("/api/auth/session/refresh", new { refreshToken = refresh1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await AssertProblemCodeAsync(stale, SessionErrors.Stale.Code);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/session/logout", new { refreshToken = refresh2 })).StatusCode);
        HttpResponseMessage afterLogout = await client.PostAsJsonAsync("/api/auth/session/refresh", new { refreshToken = refresh2 });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
        await AssertProblemCodeAsync(afterLogout, SessionErrors.Invalid.Code);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/session/logout", new { refreshToken = "garbage" })).StatusCode);
    }

    [Fact]
    public async Task Logout_all_requires_sign_in_and_revokes_every_session()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/session/logout-all", null)).StatusCode);

        async Task<(string Access, string Refresh)> SignIn(int index)
        {
            await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = Phone });
            HttpResponseMessage verified = await client.PostAsJsonAsync(
                "/api/auth/phone/verify",
                new { phoneNumber = Phone, code = factory.Sender.Sent[index].Code });
            using JsonDocument body = JsonDocument.Parse(await verified.Content.ReadAsStringAsync());
            return (body.RootElement.GetProperty("accessToken").GetString()!, body.RootElement.GetProperty("refreshToken").GetString()!);
        }

        (string access, string refreshA) = await SignIn(0);
        (_, string refreshB) = await SignIn(1);

        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/session/logout-all"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/session/refresh", new { refreshToken = refreshA })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/session/refresh", new { refreshToken = refreshB })).StatusCode);
    }

    [Fact]
    public async Task Wrong_code_returns_400_with_stable_error_code()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = Phone });
        string wrongCode = factory.Sender.Sent[0].Code == "000000" ? "111111" : "000000";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/phone/verify",
            new { phoneNumber = Phone, code = wrongCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, PhoneAuthenticationErrors.VerificationFailed.Code);
    }

    [Fact]
    public async Task Invalid_phone_number_returns_400()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/phone/request",
            new { phoneNumber = "0912345678" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, PhoneAuthenticationErrors.InvalidPhoneNumber.Code);
        Assert.Empty(factory.Sender.Sent);
    }

    [Fact]
    public async Task Per_phone_throttle_returns_429()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < RequestPhoneVerificationUseCase.MaximumCodesPerWindow; i++)
        {
            Assert.Equal(
                HttpStatusCode.Accepted,
                (await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = Phone })).StatusCode);
        }

        HttpResponseMessage throttled = await client.PostAsJsonAsync(
            "/api/auth/phone/request",
            new { phoneNumber = Phone });

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        await AssertProblemCodeAsync(throttled, PhoneAuthenticationErrors.TooManyRequests.Code);
    }

    [Fact]
    public async Task Otp_endpoints_have_stricter_per_client_rate_limit()
    {
        using var factory = new TestApiFactory(otpPermitLimit: 2);
        using HttpClient client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = "+963911111111" });
        await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = "+963922222222" });

        HttpResponseMessage rejected = await client.PostAsJsonAsync(
            "/api/auth/phone/request",
            new { phoneNumber = "+963933333333" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(2, factory.Sender.Sent.Count);
    }

    [Fact]
    public async Task Tampered_platform_token_returns_401()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        string token = CreateToken(
            PhoneIssuer,
            PhoneAudience,
            Guid.NewGuid().ToString("D"),
            new SymmetricSecurityKey(new byte[32]));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync("/api/market/properties");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task With_oidc_enabled_both_platform_and_oidc_tokens_are_accepted()
    {
        using var factory = new TestApiFactory(oidcEnabled: true);
        using HttpClient client = factory.CreateClient();

        Guid phoneUserId = Guid.NewGuid();
        factory.Store.Mappings[new ExternalIdentity(PhoneIssuer, phoneUserId.ToString("D"))] = phoneUserId;

        Guid oidcUserId = Guid.NewGuid();
        factory.Store.Mappings[new ExternalIdentity(OidcIssuer, "oidc-subject")] = oidcUserId;

        string phoneToken = CreateToken(
            PhoneIssuer,
            PhoneAudience,
            phoneUserId.ToString("D"),
            new SymmetricSecurityKey(Convert.FromBase64String(PhoneSigningKey)));

        string oidcToken = CreateToken(
            OidcIssuer,
            PhoneAudience,
            "oidc-subject",
            OidcSigningKey);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", phoneToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/market/properties")).StatusCode);
        Assert.Equal(phoneUserId, factory.Properties.LastOwnerUserId);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", oidcToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/market/properties")).StatusCode);
        Assert.Equal(oidcUserId, factory.Properties.LastOwnerUserId);

        // A token claiming the platform issuer but signed with the OIDC key must not pass.
        string forged = CreateToken(PhoneIssuer, PhoneAudience, phoneUserId.ToString("D"), OidcSigningKey);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", forged);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/market/properties")).StatusCode);
    }

    [Fact]
    public async Task Phone_routes_are_not_mapped_when_disabled()
    {
        using var factory = new TestApiFactory(phoneEnabled: false);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/phone/request",
            new { phoneNumber = Phone });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delivery_failure_returns_503_with_stable_code()
    {
        using var factory = new TestApiFactory();
        factory.Sender.Fail = true;
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/phone/request", new { phoneNumber = Phone });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertProblemCodeAsync(response, PhoneAuthenticationErrors.DeliveryFailed.Code);
    }

    [Fact]
    public void Http_gateway_sender_is_allowed_in_production_and_registered()
    {
        using var factory = new TestApiFactory(
            environment: "Production",
            otpSender: "HttpGateway",
            extraSettings: GatewaySettings("https://sms.example.test/send"),
            replaceSender: false);

        using HttpClient client = factory.CreateClient();

        Assert.IsType<Diyarak.Platform.Messaging.Http.HttpSmsGatewayOtpSender>(
            factory.Services.CreateScope().ServiceProvider.GetRequiredService<IOtpSender>());
    }

    [Theory]
    [InlineData("http://sms.example.test/send")]
    [InlineData("not a url")]
    public void Http_gateway_with_unsafe_endpoint_fails_at_startup_in_production(string endpoint)
    {
        using var factory = new TestApiFactory(
            environment: "Production",
            otpSender: "HttpGateway",
            extraSettings: GatewaySettings(endpoint),
            replaceSender: false);

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    private static Dictionary<string, string?> GatewaySettings(string endpoint) => new()
    {
        ["Messaging:Sms:Http:Endpoint"] = endpoint,
        ["Messaging:Sms:Http:Method"] = "GET",
        ["Messaging:Sms:Http:Encoding"] = "Query",
        ["Messaging:Sms:Http:ApiKey"] = "test-key",
        ["Messaging:Sms:Http:Parameters:to"] = "{phoneLocal}",
        ["Messaging:Sms:Http:Parameters:text"] = "{message}",
        ["Messaging:Sms:Http:Parameters:key"] = "{apiKey}",
    };

    [Theory]
    [InlineData("Production", "DevelopmentLog", null)]
    [InlineData("Development", "Twilio", null)]
    [InlineData("Development", "DevelopmentLog", "c2hvcnQ=")]
    public void Unsafe_phone_configuration_fails_at_startup(
        string environment,
        string sender,
        string? signingKey)
    {
        using var factory = new TestApiFactory(
            environment: environment,
            otpSender: sender,
            signingKey: signingKey ?? PhoneSigningKey);

        Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient());
    }

    private static string CreateToken(
        string issuer,
        string audience,
        string subject,
        SecurityKey key)
    {
        DateTime now = DateTime.UtcNow;

        return new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(10),
                TokenType = "at+jwt",
                Subject = new ClaimsIdentity([new Claim("sub", subject)]),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            });
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        using JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
    }

    private sealed class TestApiFactory(
        bool phoneEnabled = true,
        bool oidcEnabled = false,
        string environment = "Development",
        string otpSender = "DevelopmentLog",
        string? signingKey = null,
        int otpPermitLimit = 100,
        Dictionary<string, string?>? extraSettings = null,
        bool replaceSender = true)
        : WebApplicationFactory<Program>
    {
        public CapturingOtpSender Sender { get; } = new();

        public InMemoryIdentityStore Store { get; } = new();

        public RecordingPropertyRepository Properties { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment(environment);

            builder.ConfigureHostConfiguration(
                configuration =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Postgres"] =
                                "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                            ["Authentication:Enabled"] = oidcEnabled.ToString(),
                            ["Authentication:Issuer"] = OidcIssuer,
                            ["Authentication:Audience"] = PhoneAudience,
                            ["Authentication:Phone:Enabled"] = phoneEnabled.ToString(),
                            ["Authentication:Phone:Issuer"] = PhoneIssuer,
                            ["Authentication:Phone:Audience"] = PhoneAudience,
                            ["Authentication:Phone:SigningKey"] = signingKey ?? PhoneSigningKey,
                            ["Authentication:Phone:OtpSender"] = otpSender,
                            ["RateLimiting:Otp:PermitLimit"] =
                                otpPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        });

                    if (extraSettings is not null)
                        configuration.AddInMemoryCollection(extraSettings);
                });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                services =>
                {
                    if (replaceSender)
                    {
                        services.RemoveAll<IOtpSender>();
                        services.AddSingleton<IOtpSender>(Sender);
                    }

                    services.RemoveAll<IPhoneVerificationChallengeRepository>();
                    services.RemoveAll<IPhoneAccountRepository>();
                    services.RemoveAll<IIdentityUnitOfWork>();
                    services.AddSingleton<IPhoneVerificationChallengeRepository>(Store);
                    services.AddSingleton<IPhoneAccountRepository>(Store);
                    services.AddSingleton<IIdentityUnitOfWork>(Store);
                    services.RemoveAll<IUserSessionRepository>();
                    services.AddSingleton<IUserSessionRepository>(Store);

                    services.RemoveAll<IExternalIdentityResolver>();
                    services.AddSingleton<IExternalIdentityResolver>(Store);

                    services.RemoveAll<IMarketPropertyRepository>();
                    services.AddSingleton<IMarketPropertyRepository>(Properties);

                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            var configuration = new OpenIdConnectConfiguration { Issuer = OidcIssuer };
                            configuration.SigningKeys.Add(OidcSigningKey);
                            options.Configuration = configuration;
                            options.ConfigurationManager =
                                new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                        });
                });
        }
    }

    internal sealed class CapturingOtpSender : IOtpSender
    {
        public List<(PhoneNumber Phone, string Code)> Sent { get; } = [];

        public bool Fail { get; set; }

        public Task SendAsync(PhoneNumber phoneNumber, string code, CancellationToken cancellationToken = default)
        {
            if (Fail)
                throw new OtpDeliveryException("gateway down");

            lock (Sent)
                Sent.Add((phoneNumber, code));

            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingPropertyRepository : IMarketPropertyRepository
    {
        public Guid? LastOwnerUserId { get; private set; }

        public Task<MarketProperty?> FindByIdAsync(Guid propertyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketProperty?>(null);

        public Task<IReadOnlyList<MarketProperty>> FindByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
        {
            LastOwnerUserId = ownerUserId;
            return Task.FromResult<IReadOnlyList<MarketProperty>>([]);
        }

        public Task AddAsync(MarketProperty property, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TrySaveAsync(MarketProperty property, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    /// <summary>Thread-safe in-memory stand-in for the PostgreSQL identity adapters.</summary>
    internal sealed class InMemoryIdentityStore
        : IPhoneVerificationChallengeRepository,
          IPhoneAccountRepository,
          IIdentityUnitOfWork,
          IExternalIdentityResolver,
          IUserSessionRepository
    {
        private readonly Dictionary<Guid, UserSession> _sessions = [];

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default) =>
            Stage(() => _sessions[session.Id] = session);

        public Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _sessions.TryGetValue(sessionId, out UserSession? s)
                        ? UserSession.Restore(s.Id, s.UserId, s.CurrentTokenHash, s.PreviousTokenHash, s.CreatedAt, s.LastRotatedAt, s.ExpiresAt, s.RevokedAt)
                        : null);
            }
        }

        public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default) =>
            Stage(() => _sessions[session.Id] = session);

        public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (UserSession session in _sessions.Values.Where(item => item.UserId == userId))
                    session.Revoke(now);
            }

            return Task.CompletedTask;
        }

        private readonly Lock _gate = new();
        private readonly List<Action> _staged = [];
        private readonly Dictionary<Guid, PhoneVerificationChallenge> _challenges = [];
        private readonly Dictionary<string, PhoneAccount> _accounts = [];

        public Dictionary<ExternalIdentity, Guid> Mappings { get; } = [];

        public Task AddAsync(PhoneVerificationChallenge challenge, CancellationToken cancellationToken = default) =>
            Stage(() => _challenges[challenge.Id] = challenge);

        public Task<PhoneVerificationChallenge?> GetLatestAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _challenges.Values
                        .Where(challenge => challenge.PhoneNumber == phoneNumber)
                        .OrderByDescending(challenge => challenge.IssuedAt)
                        .FirstOrDefault());
            }
        }

        public Task<int> CountIssuedSinceAsync(PhoneNumber phoneNumber, DateTimeOffset since, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    _challenges.Values.Count(
                        challenge => challenge.PhoneNumber == phoneNumber && challenge.IssuedAt >= since));
            }
        }

        public Task UpdateAsync(PhoneVerificationChallenge challenge, CancellationToken cancellationToken = default) =>
            Stage(() => _challenges[challenge.Id] = challenge);

        public Task<PhoneAccount?> FindAsync(PhoneNumber phoneNumber, CancellationToken cancellationToken = default)
        {
            lock (_gate)
                return Task.FromResult(_accounts.GetValueOrDefault(phoneNumber.Value));
        }

        public Task AddAsync(PhoneAccount account, ExternalIdentity tokenIdentity, CancellationToken cancellationToken = default) =>
            Stage(
                () =>
                {
                    _accounts[account.PhoneNumber.Value] = account;
                    Mappings[tokenIdentity] = account.UserId;
                });

        public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                foreach (Action action in _staged)
                    action();

                _staged.Clear();
            }

            return Task.FromResult(true);
        }

        public Task<Guid?> ResolveUserIdAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(
                    Mappings.TryGetValue(identity, out Guid userId) ? userId : (Guid?)null);
            }
        }

        private Task Stage(Action action)
        {
            lock (_gate)
                _staged.Add(action);

            return Task.CompletedTask;
        }
    }
}
