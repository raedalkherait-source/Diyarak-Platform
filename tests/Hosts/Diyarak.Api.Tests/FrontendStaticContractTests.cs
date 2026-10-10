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
        Assert.Contains("Open API JSON", html);
        Assert.Contains("href=\"/api/market/public/listings\"", html);
        Assert.Contains("rel=\"noopener\"", html);
        Assert.Contains("OpenAPI", html);
        Assert.Contains("href=\"/openapi/v1.json\"", html);
        Assert.Contains("Frontend Home", html);
        Assert.Contains("href=\"/app\"", html);
        Assert.Contains("id=\"status\"", html);
        Assert.Contains("id=\"listing-grid\"", html);
        Assert.Contains("listing-card-template", html);
        Assert.Contains("data-field=\"livingArea\"", html);
        Assert.Contains("data-field=\"bedrooms\"", html);
        Assert.Contains("data-field=\"bathrooms\"", html);
        Assert.Contains("data-field=\"features\"", html);
        Assert.Contains("data-field=\"constructionYear\"", html);
        Assert.Contains("data-field=\"parking\"", html);
    }

    [Fact]
    public void App_javascript_uses_public_listing_endpoint_and_safe_states()
    {
        string javascript = ReadFrontendFile("src", "app.js");

        Assert.Contains("/api/market/public/listings", javascript);
        Assert.Contains("setLoadingState", javascript);
        Assert.Contains("formatLocation", javascript);
        Assert.Contains("Location unknown", javascript);
        Assert.Contains("setReadyState", javascript);
        Assert.Contains("listingCount", javascript);
        Assert.Contains("listingLabel", javascript);
        Assert.Contains("More pages are available; pagination controls are coming soon", javascript);
        Assert.Contains("No public listings found. Try again later or adjust your filters.", javascript);
        Assert.Contains("setErrorState", javascript);
        Assert.Contains("retryButton.hidden = false", javascript);
        Assert.Contains("loadButton.disabled = true", javascript);
        Assert.Contains("formatLivingArea", javascript);
        Assert.Contains("formatNumber", javascript);
        Assert.Contains("formatFeatures", javascript);
        Assert.Contains("formatDate", javascript);
        Assert.Contains("Intl.NumberFormat", javascript);
        Assert.Contains("style: \"currency\"", javascript);
        Assert.Contains("maximumFractionDigits", javascript);
        Assert.Contains("listing.availableFromDate", javascript);
        Assert.Contains("property.bedroomCount", javascript);
        Assert.Contains("property.bathroomCount", javascript);
        Assert.Contains("property.parkingSpaceCount", javascript);
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
