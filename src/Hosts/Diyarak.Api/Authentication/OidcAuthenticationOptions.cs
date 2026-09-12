namespace Diyarak.Api.Authentication;

public sealed class OidcAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; init; }

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;

    internal static OidcAuthenticationOptions Load(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options =
            configuration
                .GetSection(SectionName)
                .Get<OidcAuthenticationOptions>()
            ?? new OidcAuthenticationOptions();

        if (!options.Enabled)
            return options;

        ValidateEnabledOptions(options);
        return options;
    }

    private static void ValidateEnabledOptions(
        OidcAuthenticationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException(
                "Authentication:Issuer must be configured when authentication is enabled.");
        }

        if (!Uri.TryCreate(
                options.Issuer,
                UriKind.Absolute,
                out Uri? issuerUri))
        {
            throw new InvalidOperationException(
                "Authentication:Issuer must be an absolute URI.");
        }

        if (options.RequireHttpsMetadata &&
            !string.Equals(
                issuerUri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Authentication:Issuer must use HTTPS when Authentication:RequireHttpsMetadata is true.");
        }

        if (options.Issuer.Contains('*'))
        {
            throw new InvalidOperationException(
                "Authentication:Issuer must not contain wildcards.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException(
                "Authentication:Audience must be configured when authentication is enabled.");
        }

        if (options.Audience.Contains('*'))
        {
            throw new InvalidOperationException(
                "Authentication:Audience must not contain wildcards.");
        }
    }
}
