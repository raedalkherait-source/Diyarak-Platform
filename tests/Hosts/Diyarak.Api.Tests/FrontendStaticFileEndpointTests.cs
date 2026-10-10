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
        Assert.Contains("Load public listings", body);
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
