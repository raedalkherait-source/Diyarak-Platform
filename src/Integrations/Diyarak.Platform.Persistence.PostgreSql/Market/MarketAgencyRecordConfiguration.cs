using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketAgencyRecordConfiguration
    : IEntityTypeConfiguration<MarketAgencyRecord>
{
    public void Configure(EntityTypeBuilder<MarketAgencyRecord> builder)
    {
        builder.ToTable(
            "agencies",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_market_agencies_name_length", "char_length(name) BETWEEN 2 AND 80");
                tableBuilder.HasCheckConstraint(
                    "ck_market_agencies_verified_at",
                    "(verification_status = 2) = (verified_at IS NOT NULL)");
                tableBuilder.HasCheckConstraint(
                    "ck_market_agencies_registration",
                    "verification_status = 0 OR registration_number IS NOT NULL");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_agencies");

        // One agency per user.
        builder.HasIndex(record => record.OwnerUserId).IsUnique().HasDatabaseName("ux_market_agencies_owner_user_id");

        // Moderation queue of pending requests.
        builder.HasIndex(record => record.VerificationStatus).HasDatabaseName("ix_market_agencies_verification_status");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.OwnerUserId).HasColumnName("owner_user_id").HasColumnType("uuid");
        builder.Property(record => record.Version).HasColumnName("version").HasColumnType("bigint").IsConcurrencyToken();
        builder.Property(record => record.Name).HasColumnName("name").HasColumnType("text");
        builder.Property(record => record.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(record => record.Governorate).HasColumnName("governorate").HasColumnType("text");
        builder.Property(record => record.City).HasColumnName("city").HasColumnType("text");
        builder.Property(record => record.VerificationStatus).HasColumnName("verification_status").HasColumnType("integer");
        builder.Property(record => record.RegistrationNumber).HasColumnName("registration_number").HasColumnType("text");
        builder.Property(record => record.VerificationNote).HasColumnName("verification_note").HasColumnType("text");
        builder.Property(record => record.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
    }
}
