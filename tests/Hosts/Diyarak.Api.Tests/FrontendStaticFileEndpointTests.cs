using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class FrontendStaticFileEndpointTests
{
    [Fact]
    public async Task Development_app_path_serves_frontend_index()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/app");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("<title>Diyarak</title>", body);
        Assert.Contains("Refresh listings", body);
    }

    [Fact]
    public async Task Development_frontend_javascript_is_served()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/app/src/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        Assert.Contains("/api/market/public/listings", body);
    }

    [Theory]
    [InlineData("/app")]
    [InlineData("/app/")]
    [InlineData("/app/index.html")]
    [InlineData("/app/index.html?listingId=12345678-1234-1234-1234-123456789abc")]
    [InlineData("/app/src/app.js")]
    [InlineData("/app/src/styles/app.css")]
    public async Task Development_frontend_allows_same_origin_assets_and_disables_caching(string path)
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; base-uri 'self'; form-action 'none'; frame-ancestors 'none'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal(
            "nosniff",
            Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.True(response.Headers.CacheControl is { NoStore: true });

        using var headRequest = new HttpRequestMessage(HttpMethod.Head, path);
        using HttpResponseMessage headResponse = await client.SendAsync(headRequest);

        Assert.Equal(HttpStatusCode.OK, headResponse.StatusCode);
        Assert.True(headResponse.Headers.CacheControl is { NoStore: true });

        Assert.NotNull(response.Headers.ETag);

        using var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, path);
        conditionalRequest.Headers.IfNoneMatch.Add(response.Headers.ETag!);

        using HttpResponseMessage conditionalResponse = await client.SendAsync(conditionalRequest);

        Assert.Equal(HttpStatusCode.NotModified, conditionalResponse.StatusCode);
        Assert.True(conditionalResponse.Headers.CacheControl is { NoStore: true });
    }

    [Theory]
    [InlineData("Development", "/")]
    [InlineData("Development", "/health/live")]
    [InlineData("Development", "/application")]
    [InlineData("Development", "/app-other")]
    [InlineData("Production", "/app/")]
    [InlineData("Production", "/")]
    public async Task Other_routes_keep_restrictive_content_security_policy(
        string environment,
        string path)
    {
        using var factory = new TestApiFactory(environment);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(
            "default-src 'none'; frame-ancestors 'none'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")));

        if (environment == "Production" && path == "/app/")
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _environment;

        public TestApiFactory(string environment = "Development")
        {
            _environment = environment;
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment(_environment);

            builder.ConfigureHostConfiguration(
                configuration =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Postgres"] =
                                "Host=localhost;Port=5432;Database=diyarak_tests;Username=test;Password=test",
                            ["Authentication:Enabled"] = "false",
                            ["Authentication:Issuer"] =
                                "https://issuer.example.test",
                            ["Authentication:Audience"] =
                                "diyarak-api",
                            ["Authentication:RequireHttpsMetadata"] =
                                "true",
                            ["Cors:AllowedOrigins:0"] =
                                "http://localhost:5173",
                        });
                });

            return base.CreateHost(builder);
        }
    }
}
