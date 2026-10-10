using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Diyarak.Api.Tests;

public sealed class OpenApiEndpointTests
{
    [Fact]
    public async Task Development_openapi_document_exposes_public_listing_routes()
    {
        using var factory = new TestApiFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.True(
            paths.TryGetProperty(
                "/api/market/public/listings",
                out JsonElement collectionPath));
        Assert.True(
            collectionPath.TryGetProperty("get", out _));
        Assert.True(
            collectionPath.TryGetProperty("head", out _));

        Assert.True(
            paths.TryGetProperty(
                "/api/market/public/listings/{listingId}",
                out JsonElement detailPath));
        Assert.True(
            detailPath.TryGetProperty("get", out _));
        Assert.True(
            detailPath.TryGetProperty("head", out _));
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");

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
