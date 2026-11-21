using System.Globalization;
using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Listing;
using MarketListing = Diyarak.Market.Listing.Listing;
using MarketProperty = Diyarak.Market.Property.Property;

namespace Diyarak.Market.Application.BulkImport;

public sealed record BulkImportRow(
    string Category,
    string City,
    string? Governorate,
    string? District,
    string Headline,
    string PublishingRole,
    string TransactionIntent,
    decimal? Price,
    string? Currency,
    bool IsOnRequest,
    bool AllowCalls,
    bool AllowWhatsApp);

public sealed record BulkImportResult(int Created, int Failed, IReadOnlyList<string> Errors);

public static class BulkImportErrors
{
    public static Error InvalidRequest { get; } =
        Error.Validation("market.import.invalid_request", "The import data is invalid.");

    public static Error NotAnAgency { get; } =
        Error.Validation("market.import.not_an_agency", "Only agency owners can bulk-import listings.");

    public static Error TooManyRows { get; } =
        Error.Validation("market.import.too_many_rows", "The import exceeds the maximum of 200 rows.");
}

public sealed class BulkImportListingsUseCase(
    IAgencyRepository agencies,
    IMarketPropertyRepository properties,
    IMarketListingRepository listings)
{
    public const int MaximumRows = 200;

    public async Task<Result<BulkImportResult>> ExecuteAsync(
        Guid actorUserId,
        IReadOnlyList<BulkImportRow> rows,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure<BulkImportResult>(BulkImportErrors.InvalidRequest);

        if (rows is null || rows.Count == 0)
            return Result.Failure<BulkImportResult>(BulkImportErrors.InvalidRequest);

        if (rows.Count > MaximumRows)
            return Result.Failure<BulkImportResult>(BulkImportErrors.TooManyRows);

        Agency? agency = await agencies.FindByOwnerAsync(actorUserId, cancellationToken);
        if (agency is null)
            return Result.Failure<BulkImportResult>(BulkImportErrors.NotAnAgency);

        int created = 0;
        List<string> errors = [];

        for (int i = 0; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            BulkImportRow row = rows[i];

            string? error = ParseAndCreate(row, rowNumber, actorUserId, out MarketProperty? property, out MarketListing? listing);
            if (error is not null)
            {
                errors.Add(error);
                continue;
            }

            await properties.AddAsync(property!, cancellationToken);
            await listings.AddAsync(listing!, cancellationToken);
            created++;
        }

        return Result.Success(new BulkImportResult(created, errors.Count, errors));
    }

    private static string? ParseAndCreate(
        BulkImportRow row,
        int rowNumber,
        Guid actorUserId,
        out MarketProperty? property,
        out MarketListing? listing)
    {
        property = null;
        listing = null;

        if (!Enum.TryParse(row.Category, ignoreCase: true, out PropertyCategory category) || !Enum.IsDefined(category))
            return $"Row {rowNumber}: invalid category '{row.Category}'";

        if (!Enum.TryParse(row.PublishingRole, ignoreCase: true, out PublishingRole role) || !Enum.IsDefined(role))
            return $"Row {rowNumber}: invalid publishingRole '{row.PublishingRole}'";

        if (!Enum.TryParse(row.TransactionIntent, ignoreCase: true, out TransactionIntent intent) || !Enum.IsDefined(intent))
            return $"Row {rowNumber}: invalid transactionIntent '{row.TransactionIntent}'";

        SyrianGovernorate? governorate = null;
        if (!string.IsNullOrWhiteSpace(row.Governorate))
        {
            if (!Enum.TryParse(row.Governorate, ignoreCase: true, out SyrianGovernorate parsed) || !Enum.IsDefined(parsed))
                return $"Row {rowNumber}: invalid governorate '{row.Governorate}'";
            governorate = parsed;
        }

        if (string.IsNullOrWhiteSpace(row.City))
            return $"Row {rowNumber}: city is required";

        if (string.IsNullOrWhiteSpace(row.Headline))
            return $"Row {rowNumber}: headline is required";

        ListingPrice price;
        if (row.IsOnRequest)
        {
            price = ListingPrice.OnRequest();
        }
        else if (row.Price is > 0 && Currency.TryCreate(row.Currency, out Currency? currency))
        {
            price = ListingPrice.Known(new Money(row.Price.Value, currency));
        }
        else
        {
            return $"Row {rowNumber}: price requires either isOnRequest=true or a positive amount with a valid currency code";
        }

        try
        {
            var address = new PropertyAddress(null, null, null, row.City, governorate: governorate, district: row.District);
            property = new MarketProperty(Guid.NewGuid(), category, address, ownerUserId: actorUserId);

            var subjectReference = new ListingSubjectReference(property.Id, MarketListingSubjectTypes.Property);
            listing = new MarketListing(Guid.NewGuid(), actorUserId, subjectReference);
            listing.SetContext(new ListingContext(role, intent));
            listing.SetHeadline(new ListingHeadline(row.Headline));
            listing.SetPrice(price);
            listing.SetContact(new ListingContactOptions(row.AllowCalls, row.AllowWhatsApp));
        }
        catch (ArgumentException ex)
        {
            return $"Row {rowNumber}: {ex.Message}";
        }

        return null;
    }
}

public static class CsvParser
{
    public static Result<IReadOnlyList<BulkImportRow>> Parse(string csvContent)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            return Result.Failure<IReadOnlyList<BulkImportRow>>(BulkImportErrors.InvalidRequest);

        string[] lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            return Result.Failure<IReadOnlyList<BulkImportRow>>(BulkImportErrors.InvalidRequest);

        string[] headers = lines[0].Split(',');
        Dictionary<string, int> headerIndex = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            headerIndex[headers[i].Trim()] = i;

        string[] required = ["category", "city", "headline", "publishingRole", "transactionIntent"];
        foreach (string header in required)
        {
            if (!headerIndex.ContainsKey(header))
                return Result.Failure<IReadOnlyList<BulkImportRow>>(BulkImportErrors.InvalidRequest);
        }

        List<BulkImportRow> rows = [];
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            if (fields.Length < headers.Length) continue;

            string Get(string name) => headerIndex.TryGetValue(name, out int idx) && idx < fields.Length ? fields[idx].Trim() : "";
            bool GetBool(string name) => string.Equals(Get(name), "true", StringComparison.OrdinalIgnoreCase) || Get(name) == "1";

            string priceStr = Get("price");
            decimal? price = decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) ? p : null;

            rows.Add(new BulkImportRow(
                Get("category"),
                Get("city"),
                NullIfEmpty(Get("governorate")),
                NullIfEmpty(Get("district")),
                Get("headline"),
                Get("publishingRole"),
                Get("transactionIntent"),
                price,
                NullIfEmpty(Get("currency")),
                GetBool("isOnRequest"),
                GetBool("allowCalls"),
                GetBool("allowWhatsApp")));
        }

        return Result.Success<IReadOnlyList<BulkImportRow>>(rows);
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
