using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task Requests_beyond_client_permit_limit_return_429_with_retry_after()
    {
        using var factory = new TestApiFactory(permitLimit: 2);
        using HttpClient client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);

        HttpResponseMessage rejected = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Public_listing_routes_are_rate_limited()
    {
        using var factory = new TestApiFactory(permitLimit: 1);
        using HttpClient client = factory.CreateClient();

        _ = await client.GetAsync("/");

        HttpResponseMessage rejected = await client.GetAsync(
            "/api/market/public/listings?page=0");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task Health_probes_are_not_rate_limited()
    {
        using var factory = new TestApiFactory(permitLimit: 1);
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            HttpResponseMessage response = await client.GetAsync("/health/live");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [Fact]
    public async Task Root_endpoint_does_not_disclose_environment()
    {
        using var factory = new TestApiFactory(permitLimit: 10);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/");
        using JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.False(document.RootElement.TryGetProperty("environment", out _));
    }

    private sealed class TestApiFactory(int permitLimit) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureHostConfiguration(
                configuration =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Postgres"] =
                                "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                            ["Authentication:Enabled"] = "false",
                            ["RateLimiting:PermitLimit"] =
                                permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["RateLimiting:WindowSeconds"] = "3600",
                        });
                });

            return base.CreateHost(builder);
        }
    }
}
