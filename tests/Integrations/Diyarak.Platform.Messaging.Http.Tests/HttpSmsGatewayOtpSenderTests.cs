using System.Net;
using System.Text.Json;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity.Application;
using Xunit;

namespace Diyarak.Platform.Messaging.Http.Tests;

public sealed class HttpSmsGatewayOtpSenderTests
{
    private static readonly PhoneNumber Phone = PhoneNumber.Create("+963912345678");

    [Fact]
    public async Task Get_query_gateway_encodes_values_and_local_phone_format()
    {
        var handler = new RecordingHandler();
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://sms.example.test/send?account=diyarak"),
            Method = "GET",
            Encoding = HttpSmsGatewayEncoding.Query,
            ApiKey = "s3cr&t",
            Parameters = new() { ["to"] = "{phoneLocal}", ["text"] = "{message}", ["key"] = "{apiKey}" },
            MessageTemplate = "رمزك: {code}",
        };

        await Send(handler, options);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("sms.example.test", request.RequestUri!.Host);
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        Assert.Equal("diyarak", query["account"]);
        Assert.Equal("0912345678", query["to"]);
        Assert.Equal("رمزك: 123456", query["text"]);
        Assert.Equal("s3cr&t", query["key"]);
        Assert.Null(request.Content);
    }

    [Fact]
    public async Task Post_json_gateway_sends_flat_object_and_rendered_headers()
    {
        var handler = new RecordingHandler();
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://api.example.test/v1/messages"),
            Method = "POST",
            Encoding = HttpSmsGatewayEncoding.Json,
            ApiKey = "token-123",
            Headers = new() { ["Authorization"] = "Bearer {apiKey}", ["X-Sender"] = "Diyarak" },
            Parameters = new() { ["recipient"] = "{phone}", ["body"] = "{message}", ["digits"] = "{phoneDigits}" },
        };

        await Send(handler, options);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer token-123", request.Headers.GetValues("Authorization").Single());
        Assert.Equal("Diyarak", request.Headers.GetValues("X-Sender").Single());
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);

        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("+963912345678", body.RootElement.GetProperty("recipient").GetString());
        Assert.Equal("963912345678", body.RootElement.GetProperty("digits").GetString());
        Assert.Contains("123456", body.RootElement.GetProperty("body").GetString(), StringComparison.Ordinal);
        Assert.Contains("ديارك", body.RootElement.GetProperty("body").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_form_gateway_url_encodes_body()
    {
        var handler = new RecordingHandler();
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://sms.example.test/api"),
            Encoding = HttpSmsGatewayEncoding.Form,
            Parameters = new() { ["mobile"] = "{phoneDigits}", ["msg"] = "Code {code} & more" },
        };

        await Send(handler, options);

        Assert.Equal("application/x-www-form-urlencoded", handler.Requests.Single().Content!.Headers.ContentType!.MediaType);
        var form = System.Web.HttpUtility.ParseQueryString(handler.Bodies.Single());
        Assert.Equal("963912345678", form["mobile"]);
        Assert.Equal("Code 123456 & more", form["msg"]);
    }

    [Fact]
    public async Task Basic_auth_gateway_shaped_like_twilio_sends_form_and_credentials()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created);
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://api.twilio.test/2010-04-01/Accounts/AC123/Messages.json"),
            Encoding = HttpSmsGatewayEncoding.Form,
            BasicAuthUsername = "SK123",
            ApiKey = "secret-xyz",
            Parameters = new() { ["To"] = "{phone}", ["From"] = "+15005550006", ["Body"] = "{message}" },
        };
        options.Validate(allowInsecureHttp: false);

        await Send(handler, options);

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(
            "SK123:secret-xyz",
            System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));
        var form = System.Web.HttpUtility.ParseQueryString(handler.Bodies.Single());
        Assert.Equal("+963912345678", form["To"]);
        Assert.Equal("+15005550006", form["From"]);
        Assert.Contains("123456", form["Body"], StringComparison.Ordinal);
    }

    [Fact]
    public void Basic_auth_requires_secret_and_excludes_authorization_header()
    {
        static HttpSmsGatewayOptions With(string? key, Dictionary<string, string>? headers = null, string user = "SK1") => new()
        {
            Endpoint = new Uri("https://x.test"),
            BasicAuthUsername = user,
            ApiKey = key ?? string.Empty,
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
            Headers = headers ?? [],
        };

        Assert.Throws<InvalidOperationException>(() => With(null).Validate(false));
        Assert.Throws<InvalidOperationException>(() => With("s", new() { ["Authorization"] = "Bearer x" }).Validate(false));
        Assert.Throws<InvalidOperationException>(() => With("s", user: "a:b").Validate(false));
        With("s").Validate(false);
    }

    [Fact]
    public async Task Substituted_values_are_not_expanded_again()
    {
        var handler = new RecordingHandler();
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://sms.example.test/api"),
            Encoding = HttpSmsGatewayEncoding.Json,
            ApiKey = "{code}",
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}", ["key"] = "{apiKey}" },
        };

        await Send(handler, options);

        using JsonDocument body = JsonDocument.Parse(handler.Bodies.Single());
        Assert.Equal("{code}", body.RootElement.GetProperty("key").GetString());
    }

    [Fact]
    public async Task Non_success_status_throws_delivery_exception_without_secrets()
    {
        var handler = new RecordingHandler(HttpStatusCode.Unauthorized);
        var options = ValidJsonOptions();

        OtpDeliveryException exception = await Assert.ThrowsAsync<OtpDeliveryException>(() => Send(handler, options));

        Assert.Contains("401", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("123456", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("912345678", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("key-xyz", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gateways_reporting_errors_with_200_can_require_success_text()
    {
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://sms.example.test/api"),
            Encoding = HttpSmsGatewayEncoding.Json,
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
            SuccessBodyContains = "\"status\":\"ok\"",
        };

        await Send(new RecordingHandler(responseBody: "{\"status\":\"ok\"}"), options);
        await Assert.ThrowsAsync<OtpDeliveryException>(() => Send(new RecordingHandler(responseBody: "{\"status\":\"error\"}"), options));
    }

    [Fact]
    public async Task Unreachable_or_slow_gateway_throws_delivery_exception()
    {
        await Assert.ThrowsAsync<OtpDeliveryException>(() => Send(new RecordingHandler(throwOnSend: true), ValidJsonOptions()));

        var slow = ValidJsonOptions();
        await Assert.ThrowsAsync<OtpDeliveryException>(() => Send(new RecordingHandler(delay: TimeSpan.FromSeconds(5)), new HttpSmsGatewayOptions
        {
            Endpoint = slow.Endpoint,
            Encoding = slow.Encoding,
            Parameters = slow.Parameters,
            ApiKey = slow.ApiKey,
            TimeoutSeconds = 1,
        }));
    }

    [Theory]
    [InlineData("http://sms.example.test/api", "POST", HttpSmsGatewayEncoding.Json, false, "Endpoint")]
    [InlineData("https://sms.example.test/api", "PUT", HttpSmsGatewayEncoding.Json, false, "Method")]
    [InlineData("https://sms.example.test/api", "GET", HttpSmsGatewayEncoding.Json, false, "GET")]
    public void Validation_rejects_unsafe_or_inconsistent_settings(string endpoint, string method, HttpSmsGatewayEncoding encoding, bool allowHttp, string expected)
    {
        var options = new HttpSmsGatewayOptions
        {
            Endpoint = new Uri(endpoint),
            Method = method,
            Encoding = encoding,
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => options.Validate(allowHttp));
        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_requires_phone_message_code_and_configured_api_key()
    {
        Assert.Throws<InvalidOperationException>(() => new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://x.test"),
            Parameters = new() { ["text"] = "{message}" },
        }.Validate(false));

        Assert.Throws<InvalidOperationException>(() => new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://x.test"),
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
            MessageTemplate = "no code here",
        }.Validate(false));

        Assert.Throws<InvalidOperationException>(() => new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://x.test"),
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}", ["key"] = "{apiKey}" },
        }.Validate(false));

        Assert.Throws<InvalidOperationException>(() => new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("https://x.test"),
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
            Headers = new() { ["X-Bad"] = "a\r\nInjected: yes" },
        }.Validate(false));

        new HttpSmsGatewayOptions
        {
            Endpoint = new Uri("http://localhost:9999/sms"),
            Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}" },
        }.Validate(allowInsecureHttp: true);
    }

    private static HttpSmsGatewayOptions ValidJsonOptions() => new()
    {
        Endpoint = new Uri("https://sms.example.test/api"),
        Encoding = HttpSmsGatewayEncoding.Json,
        ApiKey = "key-xyz",
        Parameters = new() { ["to"] = "{phone}", ["text"] = "{message}", ["key"] = "{apiKey}" },
    };

    private static Task Send(RecordingHandler handler, HttpSmsGatewayOptions options) =>
        new HttpSmsGatewayOtpSender(new HttpClient(handler), options).SendAsync(Phone, "123456");

    private sealed class RecordingHandler(
        HttpStatusCode status = HttpStatusCode.OK,
        string responseBody = "",
        bool throwOnSend = false,
        TimeSpan? delay = null) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));

            if (throwOnSend)
                throw new HttpRequestException("connection refused");

            if (delay is { } wait)
                await Task.Delay(wait, cancellationToken);

            return new HttpResponseMessage(status) { Content = new StringContent(responseBody) };
        }
    }
}
