using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketContractorRecordConfiguration
    : IEntityTypeConfiguration<MarketContractorRecord>
{
    public void Configure(EntityTypeBuilder<MarketContractorRecord> builder)
    {
        builder.ToTable(
            "contractors",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_contractors_name_length",
                    "char_length(name) BETWEEN 2 AND 100");
            });

        builder.HasKey(record => record.Id)
            .HasName("pk_market_contractors");

        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.OwnerUserId).HasColumnName("owner_user_id");
        builder.Property(record => record.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(record => record.Name).HasColumnName("name");
        builder.Property(record => record.Description).HasColumnName("description");
        builder.Property(record => record.Governorate).HasColumnName("governorate");
        builder.Property(record => record.District).HasColumnName("district");
        builder.Property(record => record.Specialty).HasColumnName("specialty");
        builder.Property(record => record.Phone).HasColumnName("phone");
        builder.Property(record => record.IsPublished).HasColumnName("is_published");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(record => record.OwnerUserId)
            .HasDatabaseName("ix_market_contractors_owner_user_id")
            .IsUnique();

        builder.HasIndex(record => new { record.IsPublished, record.CreatedAt })
            .HasDatabaseName("ix_market_contractors_published");
    }
}
