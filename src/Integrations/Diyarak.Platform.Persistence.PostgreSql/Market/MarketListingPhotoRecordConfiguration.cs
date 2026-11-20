using Diyarak.Market.Listing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingPhotoRecordConfiguration
    : IEntityTypeConfiguration<MarketListingPhotoRecord>
{
    public void Configure(
        EntityTypeBuilder<MarketListingPhotoRecord> builder)
    {
        builder.ToTable(
            "listing_photos",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_listing_photos_position_range",
                    $"position >= 0 AND position < {ListingPhoto.MaximumPhotosPerListing}");
                tableBuilder.HasCheckConstraint(
                    "ck_market_listing_photos_size_range",
                    $"size_bytes > 0 AND size_bytes <= {ListingPhoto.MaximumSizeBytes}");
            });

        builder.HasKey(record => record.Id)
            .HasName("pk_market_listing_photos");

        // Unique position per Listing: concurrent uploads cannot both take the same slot.
        builder.HasIndex(
                record => new
                {
                    record.ListingId,
                    record.Position,
                })
            .IsUnique()
            .HasDatabaseName("ux_market_listing_photos_listing_id_position");

        builder.HasIndex(record => record.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_market_listing_photos_storage_key");

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(record => record.ListingId)
            .HasColumnName("listing_id")
            .HasColumnType("uuid");

        builder.Property(record => record.StorageKey)
            .HasColumnName("storage_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.Format)
            .HasColumnName("format")
            .HasColumnType("integer");

        builder.Property(record => record.SizeBytes)
            .HasColumnName("size_bytes")
            .HasColumnType("bigint");

        builder.Property(record => record.Position)
            .HasColumnName("position")
            .HasColumnType("integer");

        builder.Property(record => record.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone");
    }
}
