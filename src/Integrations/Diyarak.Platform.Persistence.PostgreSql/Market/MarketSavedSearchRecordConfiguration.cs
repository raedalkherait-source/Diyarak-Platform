using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketSavedSearchRecordConfiguration
    : IEntityTypeConfiguration<MarketSavedSearchRecord>
{
    public void Configure(EntityTypeBuilder<MarketSavedSearchRecord> builder)
    {
        builder.ToTable(
            "saved_searches",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_saved_searches_name_length",
                    "char_length(name) BETWEEN 1 AND 80");
                tableBuilder.HasCheckConstraint(
                    "ck_market_saved_searches_query_length",
                    "char_length(query) <= 1000");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_saved_searches");

        // One row per user and query: saving the same search twice is a no-op conflict.
        builder.HasIndex(record => new { record.UserId, record.Query })
            .IsUnique()
            .HasDatabaseName("ux_market_saved_searches_user_id_query");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(record => record.Name).HasColumnName("name").HasColumnType("text");
        builder.Property(record => record.Query).HasColumnName("query").HasColumnType("text");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamp with time zone");
    }
}
