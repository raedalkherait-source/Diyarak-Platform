using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PhoneVerificationChallengeRecordConfiguration
    : IEntityTypeConfiguration<PhoneVerificationChallengeRecord>
{
    public void Configure(
        EntityTypeBuilder<PhoneVerificationChallengeRecord> builder)
    {
        builder.ToTable(
            "phone_verification_challenges",
            "identity",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_phone_verification_challenges_failed_attempts_range",
                    "failed_attempts >= 0 AND failed_attempts <= 5");
                tableBuilder.HasCheckConstraint(
                    "ck_phone_verification_challenges_expiry_after_issue",
                    "expires_at > issued_at");
            });

        builder.HasKey(record => record.Id)
            .HasName("pk_phone_verification_challenges");

        builder.HasIndex(
                record => new
                {
                    record.PhoneNumber,
                    record.IssuedAt,
                })
            .HasDatabaseName("ix_phone_verification_challenges_phone_number_issued_at");

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(record => record.PhoneNumber)
            .HasColumnName("phone_number")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.CodeHash)
            .HasColumnName("code_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(record => record.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        // Both mutable fields are concurrency tokens so two concurrent verification attempts
        // cannot both consume the same challenge or lose a failed-attempt increment.
        builder.Property(record => record.FailedAttempts)
            .HasColumnName("failed_attempts")
            .HasColumnType("integer")
            .IsConcurrencyToken();

        builder.Property(record => record.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("timestamp with time zone")
            .IsConcurrencyToken();
    }
}
