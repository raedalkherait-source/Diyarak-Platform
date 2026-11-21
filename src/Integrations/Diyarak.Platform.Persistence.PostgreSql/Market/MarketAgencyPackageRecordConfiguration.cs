using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketAgencyPackageRecordConfiguration
    : IEntityTypeConfiguration<MarketAgencyPackageRecord>
{
    public void Configure(EntityTypeBuilder<MarketAgencyPackageRecord> builder)
    {
        builder.ToTable(
            "agency_packages",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_market_agency_packages_quota", "listing_quota > 0");
                tableBuilder.HasCheckConstraint("ck_market_agency_packages_expiry", "expires_at > created_at");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_agency_packages");

        builder.HasIndex(record => new { record.AgencyId, record.ExpiresAt })
            .HasDatabaseName("ix_market_agency_packages_agency_expires");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.AgencyId).HasColumnName("agency_id").HasColumnType("uuid");
        builder.Property(record => record.ListingQuota).HasColumnName("listing_quota").HasColumnType("integer");
        builder.Property(record => record.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.Note).HasColumnName("note").HasColumnType("text");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.IsRevoked).HasColumnName("is_revoked").HasColumnType("boolean");
        builder.Property(record => record.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
    }
}
