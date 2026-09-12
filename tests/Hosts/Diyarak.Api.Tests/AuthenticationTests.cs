using System.Security.Claims;
using Diyarak.Api.Authentication;
using Diyarak.Platform.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public void Authentication_configuration_enforces_required_JWT_validation()
    {
        using ServiceProvider provider =
            BuildServices(
                new StubExternalIdentityResolver(
                    Guid.NewGuid()));

        JwtBearerOptions options =
            provider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(
            "https://idp.example.test/",
            options.Authority);
        Assert.Equal("diyarak-api", options.Audience);
        Assert.False(options.MapInboundClaims);
        Assert.False(options.SaveToken);
        Assert.True(
            options.TokenValidationParameters.ValidateIssuer);
        Assert.True(
            options.TokenValidationParameters.ValidateAudience);
        Assert.True(
            options.TokenValidationParameters.ValidateLifetime);
        Assert.True(
            options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(
            options.TokenValidationParameters.RequireSignedTokens);
        Assert.True(
            options.TokenValidationParameters.RequireExpirationTime);
        Assert.Equal(
            ["at+jwt"],
            options.TokenValidationParameters.ValidTypes);
    }

    [Theory]
    [InlineData("Authentication:Issuer", "*")]
    [InlineData("Authentication:Audience", "*")]
    public void Authentication_configuration_rejects_wildcard_trust(
        string key,
        string value)
    {
        Dictionary<string, string?> configurationValues =
            ValidConfiguration();
        configurationValues[key] = value;

        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddDiyarakAuthentication(
                configuration));
    }

    [Fact]
    public void External_identity_reader_requires_exact_single_iss_and_sub_claims()
    {
        ClaimsPrincipal principal = CreatePrincipal(
            "https://idp.example.test/",
            "Subject-A");

        bool success =
            principal.TryGetExternalIdentity(
                out ExternalIdentity? identity);

        Assert.True(success);
        Assert.NotNull(identity);
        Assert.Equal(
            "https://idp.example.test/",
            identity.Issuer);
        Assert.Equal("Subject-A", identity.Subject);
    }

    [Fact]
    public void External_identity_reader_rejects_duplicate_subject_claims()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(
                        "iss",
                        "https://idp.example.test/"),
                    new Claim("sub", "one"),
                    new Claim("sub", "two"),
                ],
                authenticationType: "test"));

        Assert.False(
            principal.TryGetExternalIdentity(out _));
    }

    [Fact]
    public async Task Mapped_user_policy_stores_internal_actor_identifier()
    {
        Guid userId = Guid.NewGuid();
        using ServiceProvider provider =
            BuildServices(
                new StubExternalIdentityResolver(userId));
        using IServiceScope scope = provider.CreateScope();

        AuthorizationPolicy policy =
            (await scope.ServiceProvider
                .GetRequiredService<IAuthorizationPolicyProvider>()
                .GetPolicyAsync(
                    Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy))!;

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = CreatePrincipal(
                "https://idp.example.test/",
                "subject-a"),
        };

        AuthorizationResult result =
            await scope.ServiceProvider
                .GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(
                    httpContext.User,
                    httpContext,
                    policy);

        Assert.True(result.Succeeded);
        Assert.Equal(
            userId,
            httpContext.Features
                .Get<AuthenticatedActorFeature>()?
                .UserId);
    }

    [Fact]
    public async Task Mapped_user_policy_fails_for_unmapped_external_identity()
    {
        using ServiceProvider provider =
            BuildServices(
                new StubExternalIdentityResolver(userId: null));
        using IServiceScope scope = provider.CreateScope();

        AuthorizationPolicy policy =
            (await scope.ServiceProvider
                .GetRequiredService<IAuthorizationPolicyProvider>()
                .GetPolicyAsync(
                    Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy))!;

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = CreatePrincipal(
                "https://idp.example.test/",
                "unmapped-subject"),
        };

        AuthorizationResult result =
            await scope.ServiceProvider
                .GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(
                    httpContext.User,
                    httpContext,
                    policy);

        Assert.False(result.Succeeded);
        Assert.Null(
            httpContext.Features
                .Get<AuthenticatedActorFeature>());
    }

    private static ServiceProvider BuildServices(
        IExternalIdentityResolver resolver)
    {
        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    ValidConfiguration())
                .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(resolver);
        services.AddDiyarakAuthentication(configuration);

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?>
        ValidConfiguration() =>
        new()
        {
            ["Authentication:Enabled"] = "true",
            ["Authentication:Issuer"] =
                "https://idp.example.test/",
            ["Authentication:Audience"] = "diyarak-api",
            ["Authentication:RequireHttpsMetadata"] = "true",
        };

    private static ClaimsPrincipal CreatePrincipal(
        string issuer,
        string subject) =>
        new(
            new ClaimsIdentity(
                [
                    new Claim("iss", issuer),
                    new Claim("sub", subject),
                ],
                authenticationType: "test"));

    private sealed class StubExternalIdentityResolver(
        Guid? userId)
        : IExternalIdentityResolver
    {
        public Task<Guid?> ResolveUserIdAsync(
            ExternalIdentity identity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId);
    }
}
