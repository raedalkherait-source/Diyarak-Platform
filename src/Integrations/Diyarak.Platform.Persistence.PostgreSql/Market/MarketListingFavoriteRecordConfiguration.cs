using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingFavoriteRecordConfiguration
    : IEntityTypeConfiguration<MarketListingFavoriteRecord>
{
    public void Configure(EntityTypeBuilder<MarketListingFavoriteRecord> builder)
    {
        builder.ToTable("listing_favorites", "market");

        builder.HasKey(record => new { record.UserId, record.ListingId })
            .HasName("pk_market_listing_favorites");

        // Serves the "my favorites" page: newest first per user.
        builder.HasIndex(record => new { record.UserId, record.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_market_listing_favorites_user_id_created_at");

        builder.Property(record => record.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(record => record.ListingId).HasColumnName("listing_id").HasColumnType("uuid");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
    }
}
