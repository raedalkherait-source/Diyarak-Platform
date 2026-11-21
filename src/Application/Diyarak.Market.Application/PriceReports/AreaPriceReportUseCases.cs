using Diyarak.Market.Property;

namespace Diyarak.Market.Application.PriceReports;

public sealed record AreaPriceReportEntry(
    string Area,
    string TransactionIntent,
    string PropertyCategory,
    int ListingCount,
    decimal MedianPrice,
    decimal AveragePrice,
    decimal MinPrice,
    decimal MaxPrice,
    string Currency);

public interface IAreaPriceReportQuery
{
    public Task<IReadOnlyList<AreaPriceReportEntry>> GetGovernorateReportAsync(
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<AreaPriceReportEntry>> GetDistrictReportAsync(
        SyrianGovernorate governorate,
        CancellationToken cancellationToken = default);
}
