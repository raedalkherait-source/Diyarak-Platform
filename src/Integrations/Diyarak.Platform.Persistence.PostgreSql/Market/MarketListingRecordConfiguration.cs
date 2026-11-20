using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketListingRecordConfiguration
    : IEntityTypeConfiguration<MarketListingRecord>
{
    public void Configure(
        EntityTypeBuilder<MarketListingRecord> builder)
    {
        builder.ToTable(
            "listings",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_listings_contact_options",
                    "(contact_allow_calls IS NULL) = (contact_allow_whatsapp IS NULL) AND " +
                    "(contact_allow_calls IS NULL OR contact_allow_calls OR contact_allow_whatsapp)");
                tableBuilder.HasCheckConstraint(
                    "ck_market_listings_closure_reason",
                    "(status = 4) = (closure_reason IS NOT NULL)");
            });

        builder.HasKey(record => record.Id);

        builder.HasIndex(
                record => new
                {
                    record.Status,
                    record.Id,
                })
            .HasDatabaseName(
                "ix_market_listings_status_id");

        builder.HasIndex(
                record => new
                {
                    record.PublisherUserId,
                    record.Id,
                })
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

        builder.Property(record => record.ContactAllowCalls)
            .HasColumnName("contact_allow_calls")
            .HasColumnType("boolean");

        builder.Property(record => record.ContactAllowWhatsApp)
            .HasColumnName("contact_allow_whatsapp")
            .HasColumnType("boolean");

        builder.Property(record => record.ClosureReason)
            .HasColumnName("closure_reason")
            .HasColumnType("integer");
    }
}
