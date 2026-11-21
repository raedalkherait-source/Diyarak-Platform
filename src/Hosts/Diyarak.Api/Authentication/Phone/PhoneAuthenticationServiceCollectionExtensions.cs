using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Identity.Application;
using Diyarak.Platform.Messaging.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Diyarak.Api.Authentication.Phone;

internal static class PhoneAuthenticationServiceCollectionExtensions
{
    internal const string PhoneScheme = "PhoneJwt";

    internal const string SelectorScheme = "DiyarakBearer";

    /// <summary>
    /// Registers phone sign-in and validation of platform-issued tokens. When external OIDC is
    /// also enabled, a policy scheme routes each bearer token by its unvalidated <c>iss</c>;
    /// the selected scheme then fully validates it.
    /// </summary>
    internal static PhoneAuthenticationOptions AddPhoneAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);

        PhoneAuthenticationOptions settings =
            PhoneAuthenticationOptions.Load(configuration, environment);

        if (!settings.Enabled)
            return settings;

        bool oidcEnabled = configuration.GetValue<bool>(
            $"{OidcAuthenticationOptions.SectionName}:Enabled");

        services.AddSingleton(settings);
        services.TryAddSingleton<IClock>(Diyarak.Platform.BuildingBlocks.SystemClock.Instance);
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        if (settings.OtpSender == PhoneAuthenticationOptions.HttpGatewaySender)
        {
            HttpSmsGatewayOptions gateway =
                configuration.GetSection(HttpSmsGatewayOptions.SectionName).Get<HttpSmsGatewayOptions>()
                ?? new HttpSmsGatewayOptions();
            gateway.Validate(allowInsecureHttp: environment.IsDevelopment());

            services.AddSingleton(gateway);
            // The concrete client is also used for saved-search alerts (ADR-0070).
            services.AddHttpClient<HttpSmsGatewayOtpSender>();
            services.AddTransient<IOtpSender>(provider => provider.GetRequiredService<HttpSmsGatewayOtpSender>());
        }
        else
        {
            services.AddSingleton<IOtpSender, DevelopmentLogOtpSender>();
        }
        services.AddScoped<RequestPhoneVerificationUseCase>();
        services.AddScoped<VerifyPhoneUseCase>();
        services.AddScoped<RefreshSessionUseCase>();
        services.AddScoped<RevokeSessionUseCase>();
        services.AddScoped<RevokeAllSessionsUseCase>();

        AuthenticationBuilder authentication = services.AddAuthentication();

        authentication.AddJwtBearer(
            PhoneScheme,
            options =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = settings.Audience,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = JwtAccessTokenIssuer.CreateSigningKey(settings),
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        RequireSignedTokens = true,
                        ValidTypes = ["at+jwt"],
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
            });

        if (oidcEnabled)
        {
            authentication.AddPolicyScheme(
                SelectorScheme,
                displayName: null,
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                        IsPlatformToken(context, settings.Issuer)
                            ? PhoneScheme
                            : JwtBearerDefaults.AuthenticationScheme;
                });
        }

        string defaultScheme = oidcEnabled ? SelectorScheme : PhoneScheme;

        services.Configure<AuthenticationOptions>(
            options =>
            {
                options.DefaultScheme = defaultScheme;
                options.DefaultAuthenticateScheme = defaultScheme;
                options.DefaultChallengeScheme = defaultScheme;
            });

        return settings;
    }

    private static bool IsPlatformToken(HttpContext context, string platformIssuer)
    {
        string? authorization = context.Request.Headers.Authorization;

        if (authorization is null ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = authorization["Bearer ".Length..].Trim();
        var handler = new JsonWebTokenHandler();

        if (!handler.CanReadToken(token))
            return false;

        try
        {
            return string.Equals(
                handler.ReadJsonWebToken(token).Issuer,
                platformIssuer,
                StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
