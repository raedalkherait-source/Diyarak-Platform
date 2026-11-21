using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingDailyStatisticsRecordConfiguration
    : IEntityTypeConfiguration<MarketListingDailyStatisticsRecord>
{
    public void Configure(EntityTypeBuilder<MarketListingDailyStatisticsRecord> builder)
    {
        builder.ToTable(
            "listing_daily_statistics",
            "market",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_market_listing_daily_statistics_non_negative",
                "views >= 0 AND contact_reveals >= 0"));

        builder.HasKey(record => new { record.ListingId, record.Day })
            .HasName("pk_market_listing_daily_statistics");

        builder.Property(record => record.ListingId).HasColumnName("listing_id").HasColumnType("uuid");
        builder.Property(record => record.Day).HasColumnName("day").HasColumnType("date");
        builder.Property(record => record.Views).HasColumnName("views").HasColumnType("integer");
        builder.Property(record => record.ContactReveals).HasColumnName("contact_reveals").HasColumnType("integer");
    }
}
