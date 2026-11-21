using Diyarak.Market.Application.PriceReports;
using Diyarak.Market.Property;

namespace Diyarak.Api.Market;

public static class MarketPriceReportEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketPriceReportEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/market/public/price-reports", GetGovernorateReportAsync).AllowAnonymous();
        endpoints.MapGet("/api/market/public/price-reports/{governorate}", GetDistrictReportAsync).AllowAnonymous();

        return endpoints;
    }

    internal static async Task<IResult> GetGovernorateReportAsync(
        IAreaPriceReportQuery query,
        CancellationToken cancellationToken)
    {
        var entries = await query.GetGovernorateReportAsync(cancellationToken);
        return Results.Ok(new PriceReportResponse(entries.Select(ToDto).ToArray()));
    }

    internal static async Task<IResult> GetDistrictReportAsync(
        string governorate,
        IAreaPriceReportQuery query,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SyrianGovernorate>(governorate, ignoreCase: true, out var parsed))
            return Results.NotFound();

        var entries = await query.GetDistrictReportAsync(parsed, cancellationToken);
        return Results.Ok(new PriceReportResponse(entries.Select(ToDto).ToArray()));
    }

    private static PriceReportEntryDto ToDto(AreaPriceReportEntry entry) => new(
        entry.Area,
        entry.TransactionIntent,
        entry.PropertyCategory,
        entry.ListingCount,
        entry.AveragePrice,
        entry.MinPrice,
        entry.MaxPrice,
        entry.Currency);

    internal sealed record PriceReportResponse(IReadOnlyList<PriceReportEntryDto> Entries);

    internal sealed record PriceReportEntryDto(
        string Area,
        string TransactionIntent,
        string PropertyCategory,
        int ListingCount,
        decimal AveragePrice,
        decimal MinPrice,
        decimal MaxPrice,
        string Currency);
}
