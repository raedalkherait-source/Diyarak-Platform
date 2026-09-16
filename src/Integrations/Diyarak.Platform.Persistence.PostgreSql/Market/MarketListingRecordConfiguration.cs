using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingRecordConfiguration
    : IEntityTypeConfiguration<MarketListingRecord>
{
    public void Configure(
        EntityTypeBuilder<MarketListingRecord> builder)
    {
        builder.ToTable("listings", "market");

        builder.HasKey(record => record.Id);

        builder.HasIndex(
                record => new
                {
                    record.Status,
                    record.Id,
                })
            .HasDatabaseName(
                "ix_market_listings_status_id");

        builder.HasIndex(record => record.PublisherUserId)
            .HasDatabaseName(
                "ix_market_listings_publisher_user_id");

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(record => record.PublisherUserId)
            .HasColumnName("publisher_user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(record => record.SubjectId)
            .HasColumnName("subject_id")
            .HasColumnType("uuid");

        builder.Property(record => record.SubjectType)
            .HasColumnName("subject_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.Status)
            .HasColumnName("status")
            .HasColumnType("integer");

        builder.Property(record => record.Version)
            .HasColumnName("version")
            .HasColumnType("bigint")
            .IsConcurrencyToken();

        builder.Property(record => record.PublishingRole)
            .HasColumnName("publishing_role")
            .HasColumnType("integer");

        builder.Property(record => record.TransactionIntent)
            .HasColumnName("transaction_intent")
            .HasColumnType("integer");

        builder.Property(record => record.Headline)
            .HasColumnName("headline")
            .HasColumnType("text");

        builder.Property(record => record.PriceIsOnRequest)
            .HasColumnName("price_is_on_request")
            .HasColumnType("boolean");

        builder.Property(record => record.PriceAmount)
            .HasColumnName("price_amount")
            .HasColumnType("numeric");

        builder.Property(record => record.PriceCurrency)
            .HasColumnName("price_currency")
            .HasColumnType("text");

        builder.Property(record => record.AvailableFromDate)
            .HasColumnName("available_from_date")
            .HasColumnType("date");
    }
}
