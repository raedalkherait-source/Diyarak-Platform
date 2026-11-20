namespace Diyarak.Api.Authentication.Phone;

/// <summary>
/// Settings for platform-issued access tokens obtained by phone-number verification.
/// Bound from the <c>Authentication:Phone</c> configuration section.
/// </summary>
public sealed class PhoneAuthenticationOptions
{
    public const string SectionName = "Authentication:Phone";

    public const string DevelopmentLogSender = "DevelopmentLog";

    public const string HttpGatewaySender = "HttpGateway";

    public const int MinimumSigningKeyBytes = 32;

    public bool Enabled { get; init; }

    /// <summary>The <c>iss</c> written into and required from platform tokens (absolute URI).</summary>
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>Base64-encoded HMAC-SHA256 key of at least 32 bytes. Supply via secrets, never source control.</summary>
    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    /// <summary>The OTP delivery adapter: <see cref="HttpGatewaySender"/> or, in Development only, <see cref="DevelopmentLogSender"/>.</summary>
    public string OtpSender { get; init; } = string.Empty;

    internal byte[] SigningKeyBytes { get; private init; } = [];

    internal TimeSpan AccessTokenLifetime =>
        TimeSpan.FromMinutes(AccessTokenLifetimeMinutes);

    internal static PhoneAuthenticationOptions Load(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        PhoneAuthenticationOptions options =
            configuration
                .GetSection(SectionName)
                .Get<PhoneAuthenticationOptions>()
            ?? new PhoneAuthenticationOptions();

        if (!options.Enabled)
            return options;

        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out _) ||
            options.Issuer.Contains('*'))
        {
            throw new InvalidOperationException(
                $"{SectionName}:Issuer must be an absolute URI without wildcards.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience) ||
            options.Audience.Contains('*'))
        {
            throw new InvalidOperationException(
                $"{SectionName}:Audience must be configured without wildcards.");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(options.SigningKey);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                $"{SectionName}:SigningKey must be base64-encoded.");
        }

        if (key.Length < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"{SectionName}:SigningKey must decode to at least {MinimumSigningKeyBytes} bytes.");
        }

        if (options.AccessTokenLifetimeMinutes is < 5 or > 1440)
        {
            throw new InvalidOperationException(
                $"{SectionName}:AccessTokenLifetimeMinutes must be between 5 and 1440.");
        }

        if (!string.Equals(options.OtpSender, DevelopmentLogSender, StringComparison.Ordinal) &&
            !string.Equals(options.OtpSender, HttpGatewaySender, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{SectionName}:OtpSender must be '{HttpGatewaySender}' or, in Development, '{DevelopmentLogSender}'.");
        }

        if (string.Equals(options.OtpSender, DevelopmentLogSender, StringComparison.Ordinal) &&
            !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"{SectionName}:OtpSender '{DevelopmentLogSender}' writes codes to the log and is only allowed in Development.");
        }

        return new PhoneAuthenticationOptions
        {
            Enabled = true,
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningKey = options.SigningKey,
            AccessTokenLifetimeMinutes = options.AccessTokenLifetimeMinutes,
            OtpSender = options.OtpSender,
            SigningKeyBytes = key,
        };
    }
}
