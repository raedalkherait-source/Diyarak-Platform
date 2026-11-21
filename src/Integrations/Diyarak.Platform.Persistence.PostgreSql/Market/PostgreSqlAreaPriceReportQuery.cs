using Diyarak.Market.Application.PriceReports;
using Diyarak.Market.Listing;
using Diyarak.Market.Property;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class PostgreSqlAreaPriceReportQuery(PlatformDbContext context)
    : IAreaPriceReportQuery
{
    private static readonly int[] ReportableCategories =
    [
        (int)PropertyCategory.Apartment,
        (int)PropertyCategory.House,
        (int)PropertyCategory.Plot,
        (int)PropertyCategory.CommercialProperty,
    ];

    public async Task<IReadOnlyList<AreaPriceReportEntry>> GetGovernorateReportAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await BaseQuery()
            .Where(pair => pair.Property.Governorate.HasValue)
            .GroupBy(pair => new
            {
                pair.Property.Governorate,
                pair.Listing.TransactionIntent,
                pair.Property.Category,
                pair.Listing.PriceCurrency,
            })
            .Where(g => g.Count() >= 3)
            .Select(g => new
            {
                g.Key.Governorate,
                g.Key.TransactionIntent,
                g.Key.Category,
                g.Key.PriceCurrency,
                Count = g.Count(),
                Avg = g.Average(x => x.Listing.PriceAmount!.Value),
                Min = g.Min(x => x.Listing.PriceAmount!.Value),
                Max = g.Max(x => x.Listing.PriceAmount!.Value),
            })
            .OrderBy(x => x.Governorate)
            .ThenBy(x => x.TransactionIntent)
            .ThenBy(x => x.Category)
            .ToListAsync(cancellationToken);

        return groups.Select(g => new AreaPriceReportEntry(
            ((SyrianGovernorate)g.Governorate!.Value).ToString(),
            ((TransactionIntent)g.TransactionIntent!.Value).ToString(),
            ((PropertyCategory)g.Category).ToString(),
            g.Count,
            g.Avg,
            g.Avg,
            g.Min,
            g.Max,
            g.PriceCurrency!)).ToArray();
    }

    public async Task<IReadOnlyList<AreaPriceReportEntry>> GetDistrictReportAsync(
        SyrianGovernorate governorate,
        CancellationToken cancellationToken = default)
    {
        int governorateValue = (int)governorate;

        var groups = await BaseQuery()
            .Where(pair =>
                pair.Property.Governorate == governorateValue &&
                pair.Property.District != null)
            .GroupBy(pair => new
            {
                pair.Property.District,
                pair.Listing.TransactionIntent,
                pair.Property.Category,
                pair.Listing.PriceCurrency,
            })
            .Where(g => g.Count() >= 3)
            .Select(g => new
            {
                g.Key.District,
                g.Key.TransactionIntent,
                g.Key.Category,
                g.Key.PriceCurrency,
                Count = g.Count(),
                Avg = g.Average(x => x.Listing.PriceAmount!.Value),
                Min = g.Min(x => x.Listing.PriceAmount!.Value),
                Max = g.Max(x => x.Listing.PriceAmount!.Value),
            })
            .OrderBy(x => x.District)
            .ThenBy(x => x.TransactionIntent)
            .ThenBy(x => x.Category)
            .ToListAsync(cancellationToken);

        return groups.Select(g => new AreaPriceReportEntry(
            g.District!,
            ((TransactionIntent)g.TransactionIntent!.Value).ToString(),
            ((PropertyCategory)g.Category).ToString(),
            g.Count,
            g.Avg,
            g.Avg,
            g.Min,
            g.Max,
            g.PriceCurrency!)).ToArray();
    }

    private IQueryable<ListingWithProperty> BaseQuery()
    {
        return context.MarketListings
            .AsNoTracking()
            .Where(l =>
                l.Status == (int)ListingStatus.Published &&
                l.SubjectType == MarketListingSubjectTypes.Property &&
                l.PriceIsOnRequest == false &&
                l.PriceAmount.HasValue &&
                l.PriceCurrency != null &&
                l.TransactionIntent.HasValue)
            .Join(
                context.MarketProperties.AsNoTracking()
                    .Where(p => ReportableCategories.Contains(p.Category)),
                l => l.SubjectId,
                p => p.Id,
                (l, p) => new ListingWithProperty { Listing = l, Property = p });
    }

    private sealed class ListingWithProperty
    {
        public MarketListingRecord Listing { get; init; } = null!;
        public MarketPropertyRecord Property { get; init; } = null!;
    }
}
