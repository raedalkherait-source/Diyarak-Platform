using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity.Application;

namespace Diyarak.Platform.Messaging.Http;

/// <summary>
/// Sends one-time codes through any SMS provider that accepts a simple HTTP request, described
/// entirely by <see cref="HttpSmsGatewayOptions"/>.
/// </summary>
public sealed class HttpSmsGatewayOtpSender : IOtpSender
{
    private readonly HttpClient _httpClient;
    private readonly HttpSmsGatewayOptions _options;

    public HttpSmsGatewayOtpSender(HttpClient httpClient, HttpSmsGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    public async Task SendAsync(PhoneNumber phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        using HttpRequestMessage request = BuildRequest(phoneNumber, code);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OtpDeliveryException("The SMS gateway did not respond in time.");
        }
        catch (HttpRequestException exception)
        {
            // The inner exception may describe the endpoint but never the code or credential.
            throw new OtpDeliveryException("The SMS gateway could not be reached.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new OtpDeliveryException($"The SMS gateway rejected the request with HTTP {(int)response.StatusCode}.");

            if (!string.IsNullOrEmpty(_options.SuccessBodyContains))
            {
                string body = await response.Content.ReadAsStringAsync(timeout.Token);
                if (!body.Contains(_options.SuccessBodyContains, StringComparison.Ordinal))
                    throw new OtpDeliveryException("The SMS gateway response did not confirm delivery.");
            }
        }
    }

    internal HttpRequestMessage BuildRequest(PhoneNumber phoneNumber, string code)
    {
        Dictionary<string, string> values = CreatePlaceholderValues(phoneNumber, code);
        var parameters = _options.Parameters.ToDictionary(
            pair => pair.Key,
            pair => Render(pair.Value, values),
            StringComparer.Ordinal);

        bool isGet = string.Equals(_options.Method, "GET", StringComparison.OrdinalIgnoreCase);
        Uri endpoint = _options.Endpoint ?? throw new InvalidOperationException("The SMS gateway endpoint is not configured.");

        if (_options.Encoding == HttpSmsGatewayEncoding.Query)
            endpoint = AppendQuery(endpoint, parameters);

        var request = new HttpRequestMessage(isGet ? HttpMethod.Get : HttpMethod.Post, endpoint);

        if (!isGet)
        {
            request.Content = _options.Encoding switch
            {
                HttpSmsGatewayEncoding.Form => new FormUrlEncodedContent(parameters),
                HttpSmsGatewayEncoding.Json => new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json"),
                _ => new ByteArrayContent([]),
            };
        }

        foreach ((string name, string template) in _options.Headers)
        {
            string value = Render(template, values);

            if (value.Any(character => character is '\r' or '\n'))
                throw new OtpDeliveryException("An SMS gateway header contains a line break.");

            if (!request.Headers.TryAddWithoutValidation(name, value))
                request.Content?.Headers.TryAddWithoutValidation(name, value);
        }

        if (!string.IsNullOrEmpty(_options.BasicAuthUsername))
        {
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.BasicAuthUsername}:{_options.ApiKey}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        return request;
    }

    private Dictionary<string, string> CreatePlaceholderValues(PhoneNumber phoneNumber, string code)
    {
        string digits = phoneNumber.Value.TrimStart('+');
        string local = digits.StartsWith("963", StringComparison.Ordinal) ? $"0{digits[3..]}" : digits;

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{phone}"] = phoneNumber.Value,
            ["{phoneDigits}"] = digits,
            ["{phoneLocal}"] = local,
            ["{code}"] = code,
            ["{apiKey}"] = _options.ApiKey,
        };

        values["{message}"] = Render(_options.MessageTemplate, values);
        return values;
    }

    /// <summary>Replaces placeholders in one pass so substituted values are never re-expanded.</summary>
    private static string Render(string template, Dictionary<string, string> values)
    {
        var builder = new StringBuilder(template.Length + 32);
        int index = 0;

        while (index < template.Length)
        {
            if (template[index] == '{')
            {
                int end = template.IndexOf('}', index);
                if (end > index && values.TryGetValue(template[index..(end + 1)], out string? value))
                {
                    builder.Append(value);
                    index = end + 1;
                    continue;
                }
            }

            builder.Append(template[index]);
            index++;
        }

        return builder.ToString();
    }

    private static Uri AppendQuery(Uri endpoint, Dictionary<string, string> parameters)
    {
        var query = new StringBuilder(endpoint.Query.TrimStart('?'));

        foreach ((string name, string value) in parameters)
        {
            if (query.Length > 0)
                query.Append('&');

            query.Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(value));
        }

        return new UriBuilder(endpoint) { Query = query.ToString() }.Uri;
    }
}
