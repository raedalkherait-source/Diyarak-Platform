using Xunit;

namespace Diyarak.Api.Tests;

public sealed class FrontendStaticContractTests
{
    [Fact]
    public void Index_contains_listing_controls_and_retry_button()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("id=\"load-listings\"", html);
        Assert.Contains("Refresh listings", html);
        Assert.Contains("id=\"retry-listings\"", html);
        Assert.Contains("Retry", html);
        Assert.Contains("id=\"status\"", html);
        Assert.Contains("id=\"listing-grid\"", html);
        Assert.Contains("listing-card-template", html);
    }

    [Fact]
    public void App_javascript_uses_public_listing_endpoint_and_safe_states()
    {
        string javascript = ReadFrontendFile("src", "app.js");

        Assert.Contains("/api/market/public/listings", javascript);
        Assert.Contains("setLoadingState", javascript);
        Assert.Contains("setReadyState", javascript);
        Assert.Contains("setErrorState", javascript);
        Assert.Contains("retryButton.hidden = false", javascript);
        Assert.Contains("loadButton.disabled = true", javascript);
    }

    [Fact]
    public void Styles_define_loading_retry_and_error_state_rules()
    {
        string css = ReadFrontendFile("src", "styles", "app.css");

        Assert.Contains("button:disabled", css);
        Assert.Contains(".secondary-button", css);
        Assert.Contains("#status[data-state=\"error\"]", css);
    }

    private static string ReadFrontendFile(params string[] relativeParts)
    {
        string path = Path.Combine(
            "C:",
            "Diyarak",
            "src",
            "Frontend",
            "Diyarak.Web",
            Path.Combine(relativeParts));

        return File.ReadAllText(path);
    }
}