using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Diyarak.Api.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public const string MappedUserPolicy = "mapped-user";

    public static IServiceCollection AddDiyarakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        OidcAuthenticationOptions settings =
            OidcAuthenticationOptions.Load(configuration);

        var authentication = services.AddAuthentication(
            options =>
            {
                if (!settings.Enabled)
                    return;

                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            });

        if (settings.Enabled)
        {
            authentication.AddJwtBearer(
                options =>
                {
                    options.Authority = settings.Issuer;
                    options.Audience = settings.Audience;
                    options.RequireHttpsMetadata =
                        settings.RequireHttpsMetadata;
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
                            RequireSignedTokens = true,
                            ValidTypes = ["at+jwt"],
                            ClockSkew = TimeSpan.FromMinutes(1),
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (context.Principal is null ||
                                !context.Principal.TryGetExternalIdentity(
                                    out _))
                            {
                                context.Fail(
                                    "The access token must contain exactly one non-empty iss claim and one non-empty sub claim.");
                            }

                            return Task.CompletedTask;
                        },
                    };
                });
        }

        services.AddHttpContextAccessor();
        services.AddScoped<
            IAuthenticatedActorAccessor,
            AuthenticatedActorAccessor>();
        services.AddScoped<
            IAuthorizationHandler,
            MappedExternalIdentityAuthorizationHandler>();

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    MappedUserPolicy,
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.AddRequirements(
                            new MappedExternalIdentityRequirement());
                    });
            });

        return services;
    }
}
