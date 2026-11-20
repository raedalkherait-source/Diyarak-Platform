using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class PhoneAccountRecordConfiguration
    : IEntityTypeConfiguration<PhoneAccountRecord>
{
    public void Configure(
        EntityTypeBuilder<PhoneAccountRecord> builder)
    {
        builder.ToTable(
            "phone_accounts",
            "identity",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_phone_accounts_user_id_non_empty",
                    "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(record => record.PhoneNumber)
            .HasName("pk_phone_accounts");

        builder.HasIndex(record => record.UserId)
            .IsUnique()
            .HasDatabaseName("ux_phone_accounts_user_id");

        builder.Property(record => record.PhoneNumber)
            .HasColumnName("phone_number")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");
    }
}
