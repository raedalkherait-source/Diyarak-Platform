namespace Diyarak.Platform.Messaging.Http;

public enum HttpSmsGatewayEncoding
{
    /// <summary>Parameters are appended to the endpoint query string (typical GET gateways).</summary>
    Query = 1,

    /// <summary>Parameters are sent as an <c>application/x-www-form-urlencoded</c> body.</summary>
    Form = 2,

    /// <summary>Parameters are sent as a flat JSON object body.</summary>
    Json = 3,
}

/// <summary>
/// Describes an SMS provider's HTTP API without provider-specific code. Parameter and header
/// values are templates that may use <c>{phone}</c> (E.164, e.g. +963912345678),
/// <c>{phoneDigits}</c> (963912345678), <c>{phoneLocal}</c> (0912345678 for Syrian numbers,
/// otherwise digits), <c>{message}</c>, <c>{code}</c> and <c>{apiKey}</c>.
/// </summary>
public sealed class HttpSmsGatewayOptions
{
    public const string SectionName = "Messaging:Sms:Http";

    public const string DefaultMessageTemplate = "رمز التحقق في ديارك: {code}\nلا تشارك هذا الرمز مع أي أحد.";

    public Uri? Endpoint { get; init; }

    /// <summary><c>GET</c> or <c>POST</c>.</summary>
    public string Method { get; init; } = "POST";

    public HttpSmsGatewayEncoding Encoding { get; init; } = HttpSmsGatewayEncoding.Json;

    public Dictionary<string, string> Parameters { get; init; } = [];

    public Dictionary<string, string> Headers { get; init; } = [];

    /// <summary>Provider credential substituted for <c>{apiKey}</c>. Supply it from a secret store.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Optional user name for HTTP Basic authentication (for example a Twilio API key SID). When
    /// set, requests carry <c>Authorization: Basic base64(BasicAuthUsername:ApiKey)</c>.
    /// </summary>
    public string? BasicAuthUsername { get; init; }

    /// <summary>Message text; must contain <c>{code}</c>.</summary>
    public string MessageTemplate { get; init; } = DefaultMessageTemplate;

    /// <summary>
    /// Optional text the response body must contain for the send to count as successful, for
    /// gateways that report errors with HTTP 200.
    /// </summary>
    public string? SuccessBodyContains { get; init; }

    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>Throws <see cref="InvalidOperationException"/> describing the first invalid setting.</summary>
    public void Validate(bool allowInsecureHttp)
    {
        if (Endpoint is null || !Endpoint.IsAbsoluteUri)
            throw new InvalidOperationException($"{SectionName}:Endpoint must be an absolute URL.");

        if (Endpoint.Scheme != Uri.UriSchemeHttps && !(allowInsecureHttp && Endpoint.Scheme == Uri.UriSchemeHttp))
            throw new InvalidOperationException($"{SectionName}:Endpoint must use HTTPS.");

        if (!string.Equals(Method, "GET", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{SectionName}:Method must be GET or POST.");
        }

        if (!Enum.IsDefined(Encoding))
            throw new InvalidOperationException($"{SectionName}:Encoding must be Query, Form or Json.");

        if (string.Equals(Method, "GET", StringComparison.OrdinalIgnoreCase) && Encoding != HttpSmsGatewayEncoding.Query)
            throw new InvalidOperationException($"{SectionName}: GET requests must use Query encoding.");

        if (Parameters.Count == 0)
            throw new InvalidOperationException($"{SectionName}:Parameters must describe at least the recipient and message.");

        bool referencesPhone = Parameters.Values.Concat(Headers.Values).Any(
            value => value.Contains("{phone", StringComparison.Ordinal));
        bool referencesMessage = Parameters.Values.Any(
            value => value.Contains("{message}", StringComparison.Ordinal) || value.Contains("{code}", StringComparison.Ordinal));

        if (!referencesPhone || !referencesMessage)
            throw new InvalidOperationException($"{SectionName}:Parameters must reference a phone placeholder and {{message}} or {{code}}.");

        if (!MessageTemplate.Contains("{code}", StringComparison.Ordinal))
            throw new InvalidOperationException($"{SectionName}:MessageTemplate must contain {{code}}.");

        bool usesApiKey = Parameters.Values.Concat(Headers.Values).Any(value => value.Contains("{apiKey}", StringComparison.Ordinal));
        if (usesApiKey && string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException($"{SectionName}:ApiKey is referenced but not configured.");

        if (!string.IsNullOrEmpty(BasicAuthUsername))
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new InvalidOperationException($"{SectionName}:ApiKey is required with BasicAuthUsername.");

            if (BasicAuthUsername.Contains(':', StringComparison.Ordinal) || BasicAuthUsername.Any(char.IsControl))
                throw new InvalidOperationException($"{SectionName}:BasicAuthUsername contains an invalid character.");

            if (Headers.Keys.Any(name => string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"{SectionName}: use either BasicAuthUsername or an Authorization header, not both.");
        }

        foreach ((string name, string value) in Headers)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl) || value.Any(character => character is '\r' or '\n'))
                throw new InvalidOperationException($"{SectionName}:Headers contains an invalid header.");
        }

        if (TimeoutSeconds is < 1 or > 60)
            throw new InvalidOperationException($"{SectionName}:TimeoutSeconds must be between 1 and 60.");
    }
}
