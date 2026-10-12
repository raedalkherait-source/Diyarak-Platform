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
        Assert.Contains("aria-label=\"Listing controls\"", html);
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
        Assert.Contains("More pages are available.", javascript);
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
        Assert.Contains("flex-wrap: wrap", css);
        Assert.Contains("flex-basis: 100%", css);
        Assert.Contains(".listing-card__headline", css);
        Assert.Contains(".listing-card__price", css);
        Assert.Contains(".listing-card__facts", css);
        Assert.Contains("row-gap: 0.75rem", css);
        Assert.Contains("#status[data-state=\"error\"]", css);
    }

    [Fact]
    public void Index_contains_accessible_listing_page_controls()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("aria-label=\"Listing pages\"", html);
        Assert.Contains("id=\"previous-page\"", html);
        Assert.Contains("id=\"next-page\"", html);
        Assert.Contains("id=\"current-page\"", html);
    }

    [Fact]
    public void Index_contains_public_listing_detail_navigation_and_facts()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("listing-card__detail-link", html);
        Assert.Contains("id=\"back-to-listings\"", html);
        Assert.Contains("id=\"listing-heading\"", html);
        Assert.Contains("data-detail=\"publishingRole\"", html);
        Assert.Contains("data-detail=\"transactionIntent\"", html);
        Assert.Contains("data-detail=\"furnishingQuality\"", html);
        Assert.Contains("data-detail=\"commercialSubtype\"", html);
    }

    [Fact]
    public void Index_contains_labeled_public_listing_filters()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("id=\"listing-filters\"", html);
        Assert.Contains("id=\"listing-filter-fields\"", html);
        Assert.Contains("for=\"filter-city\"", html);
        Assert.Contains("name=\"city\"", html);
        Assert.Contains("for=\"filter-intent\"", html);
        Assert.Contains("name=\"transactionIntent\"", html);
        Assert.Contains("for=\"filter-category\"", html);
        Assert.Contains("name=\"propertyCategory\"", html);
        Assert.Contains("id=\"clear-filters\"", html);
    }

    [Fact]
    public void Index_contains_labeled_price_filters_and_accessible_validation()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("for=\"filter-price-min\"", html);
        Assert.Contains("name=\"priceMin\"", html);
        Assert.Contains("for=\"filter-price-max\"", html);
        Assert.Contains("name=\"priceMax\"", html);
        Assert.Contains("for=\"filter-price-currency\"", html);
        Assert.Contains("name=\"priceCurrency\"", html);
        Assert.Contains("id=\"filter-error\"", html);
        Assert.Contains("role=\"alert\"", html);
        Assert.Contains("id=\"price-filter-help\"", html);
    }

    [Fact]
    public void Index_contains_labeled_living_area_filters_and_unit_choices()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("for=\"filter-area-min\"", html);
        Assert.Contains("name=\"livingAreaMin\"", html);
        Assert.Contains("for=\"filter-area-max\"", html);
        Assert.Contains("name=\"livingAreaMax\"", html);
        Assert.Contains("for=\"filter-area-unit\"", html);
        Assert.Contains("name=\"livingAreaUnit\"", html);
        foreach (string unit in new[] { "SquareMeter", "SquareFoot", "Hectare", "Dunum" })
        {
            Assert.Contains($"value=\"{unit}\"", html);
        }
        Assert.Contains("aria-describedby=\"area-filter-help filter-error\"", html);
        Assert.Contains("id=\"area-filter-help\"", html);
    }

    [Fact]
    public void Index_contains_labeled_room_ranges_with_accessible_help()
    {
        string html = ReadFrontendFile("index.html");

        foreach (var (control, parameter) in new[]
        {
            ("rooms", "totalRooms"),
            ("bedrooms", "bedroomCount"),
            ("bathrooms", "bathroomCount")
        })
        {
            Assert.Contains($"for=\"filter-{control}-min\"", html);
            Assert.Contains($"for=\"filter-{control}-max\"", html);
            Assert.Contains($"name=\"{parameter}Min\"", html);
            Assert.Contains($"name=\"{parameter}Max\"", html);
        }
        Assert.Contains("id=\"room-filter-help\"", html);
        Assert.Contains("aria-describedby=\"room-filter-help filter-error\"", html);
    }

    [Fact]
    public void Index_contains_labeled_construction_year_and_parking_ranges()
    {
        string html = ReadFrontendFile("index.html");

        foreach (var (control, parameter) in new[]
        {
            ("year", "constructionYear"),
            ("parking", "parkingSpaceCount")
        })
        {
            Assert.Contains($"for=\"filter-{control}-min\"", html);
            Assert.Contains($"for=\"filter-{control}-max\"", html);
            Assert.Contains($"name=\"{parameter}Min\"", html);
            Assert.Contains($"name=\"{parameter}Max\"", html);
        }
        Assert.Contains("id=\"year-parking-filter-help\"", html);
        Assert.Contains("aria-describedby=\"year-parking-filter-help filter-error\"", html);
    }

    [Fact]
    public void Index_contains_labeled_furnishing_quality_filter()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("for=\"filter-furnishing\"", html);
        Assert.Contains("name=\"furnishingQuality\"", html);
        foreach (string quality in new[] { "Simple", "Normal", "Upscale", "Luxury" })
        {
            Assert.Contains($"value=\"{quality}\"", html);
        }
    }

    [Fact]
    public void Index_contains_labeled_commercial_subtype_filter_and_help()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("for=\"filter-commercial\"", html);
        Assert.Contains("name=\"commercialSubtype\"", html);
        Assert.Contains("aria-describedby=\"commercial-filter-help\"", html);
        Assert.Contains("id=\"commercial-filter-help\"", html);
        foreach (string subtype in new[]
        {
            "OfficeOrPractice", "RestaurantOrHotel", "HallOrProduction", "Retail", "CommercialPlot"
        })
        {
            Assert.Contains($"value=\"{subtype}\"", html);
        }
    }

    [Fact]
    public void Index_contains_labeled_text_postal_code_filter_and_help()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("for=\"filter-postal\"", html);
        Assert.Contains("name=\"postalCode\"", html);
        Assert.Contains("id=\"filter-postal\" name=\"postalCode\" type=\"text\"", html);
        Assert.Contains("autocomplete=\"postal-code\"", html);
        Assert.Contains("aria-describedby=\"postal-filter-help\"", html);
        Assert.Contains("id=\"postal-filter-help\"", html);
    }

    private static string ReadFrontendFile(params string[] relativeParts)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "FrontendFiles",
            Path.Combine(relativeParts));

        return File.ReadAllText(path);
    }

    [Fact]
    public void Static_frontend_readme_documents_development_links()
    {
        string readme = ReadFrontendFile("README.md");

        Assert.Contains("Development links", readme);
        Assert.Contains("/app", readme);
        Assert.Contains("/api/market/public/listings", readme);
        Assert.Contains("/openapi/v1.json", readme);
        Assert.Contains("Local assets only", readme);
        Assert.Contains("avoids CDN-hosted libraries", readme);
        Assert.Contains("external runtime dependencies", readme);
    }

    [Fact]
    public void Static_frontend_uses_only_local_assets()
    {
        string html = ReadFrontendFile("index.html");

        Assert.DoesNotContain("https://", html);
        Assert.DoesNotContain("http://", html);
        Assert.DoesNotContain("integrity=", html);
        Assert.DoesNotContain("crossorigin=", html);
        Assert.Contains("href=\"./src/styles/app.css\"", html);
        Assert.Contains("src=\"./src/app.js\"", html);
    }

    [Fact]
    public void Static_frontend_readme_keeps_core_sections()
    {
        string readme = ReadFrontendFile("README.md");

        Assert.Contains("Initial scope:", readme);
        Assert.Contains("## Development links", readme);
        Assert.Contains("## Local assets only", readme);
        Assert.Contains("## External asset attributes", readme);
    }

    [Fact]
    public void Index_keeps_diyarak_page_title()
    {
        string html = ReadFrontendFile("index.html");

        Assert.Contains("<title>Diyarak</title>", html);
    }
}
