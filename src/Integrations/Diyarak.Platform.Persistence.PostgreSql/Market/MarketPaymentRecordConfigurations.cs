using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketPaymentOrderRecordConfiguration
    : IEntityTypeConfiguration<MarketPaymentOrderRecord>
{
    public void Configure(EntityTypeBuilder<MarketPaymentOrderRecord> builder)
    {
        builder.ToTable(
            "payment_orders",
            "market",
            table =>
            {
                table.HasCheckConstraint("ck_market_payment_orders_amount", "amount > 0");
                table.HasCheckConstraint("ck_market_payment_orders_decision", "(status = 1) = (decided_at IS NULL)");
                table.HasCheckConstraint("ck_market_payment_orders_rejection_note", "status <> 3 OR decision_note IS NOT NULL");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_payment_orders");

        // At most one order awaiting confirmation per listing.
        builder.HasIndex(record => record.ListingId)
            .IsUnique()
            .HasFilter("status = 1")
            .HasDatabaseName("ux_market_payment_orders_pending_listing");
        builder.HasIndex(record => new { record.UserId, record.CreatedAt }).HasDatabaseName("ix_market_payment_orders_user_id_created_at");
        builder.HasIndex(record => new { record.Status, record.CreatedAt }).HasDatabaseName("ix_market_payment_orders_status_created_at");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(record => record.Product).HasColumnName("product").HasColumnType("integer");
        builder.Property(record => record.ListingId).HasColumnName("listing_id").HasColumnType("uuid");
        builder.Property(record => record.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
        builder.Property(record => record.Currency).HasColumnName("currency").HasColumnType("character(3)");
        builder.Property(record => record.Method).HasColumnName("method").HasColumnType("integer");
        builder.Property(record => record.PayerReference).HasColumnName("payer_reference").HasColumnType("text");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.Status).HasColumnName("status").HasColumnType("integer").IsConcurrencyToken();
        builder.Property(record => record.DecisionNote).HasColumnName("decision_note").HasColumnType("text");
        builder.Property(record => record.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.DecidedBy).HasColumnName("decided_by").HasColumnType("uuid");
    }
}

internal sealed class MarketListingPromotionRecordConfiguration
    : IEntityTypeConfiguration<MarketListingPromotionRecord>
{
    public void Configure(EntityTypeBuilder<MarketListingPromotionRecord> builder)
    {
        builder.ToTable(
            "listing_promotions",
            "market",
            table => table.HasCheckConstraint("ck_market_listing_promotions_period", "ends_at > starts_at"));

        builder.HasKey(record => record.Id).HasName("pk_market_listing_promotions");
        builder.HasIndex(record => record.OrderId).IsUnique().HasDatabaseName("ux_market_listing_promotions_order_id");
        builder.HasIndex(record => new { record.ListingId, record.EndsAt }).HasDatabaseName("ix_market_listing_promotions_listing_id_ends_at");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.ListingId).HasColumnName("listing_id").HasColumnType("uuid");
        builder.Property(record => record.OrderId).HasColumnName("order_id").HasColumnType("uuid");
        builder.Property(record => record.StartsAt).HasColumnName("starts_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.EndsAt).HasColumnName("ends_at").HasColumnType("timestamp with time zone");
    }
}
