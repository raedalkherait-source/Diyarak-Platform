using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingReportRecordConfiguration
    : IEntityTypeConfiguration<MarketListingReportRecord>
{
    public void Configure(EntityTypeBuilder<MarketListingReportRecord> builder)
    {
        builder.ToTable(
            "listing_reports",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_listing_reports_resolution",
                    "(resolution IS NULL) = (resolved_at IS NULL) AND (resolution IS NULL) = (resolved_by_user_id IS NULL)");
                tableBuilder.HasCheckConstraint(
                    "ck_market_listing_reports_comment_length",
                    "comment IS NULL OR char_length(comment) <= 500");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_listing_reports");

        builder.HasIndex(record => new { record.ListingId, record.CreatedAt })
            .HasDatabaseName("ix_market_listing_reports_listing_id_created_at");

        // Partial index keeps the moderation queue lookup small as resolved reports accumulate.
        builder.HasIndex(record => record.CreatedAt)
            .HasFilter("resolution IS NULL")
            .HasDatabaseName("ix_market_listing_reports_open_created_at");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.ListingId).HasColumnName("listing_id").HasColumnType("uuid");
        builder.Property(record => record.Reason).HasColumnName("reason").HasColumnType("integer");
        builder.Property(record => record.Comment).HasColumnName("comment").HasColumnType("text");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.Resolution).HasColumnName("resolution").HasColumnType("integer");
        builder.Property(record => record.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.ResolvedByUserId).HasColumnName("resolved_by_user_id").HasColumnType("uuid");
    }
}
