using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class ExternalIdentityMappingRecordConfiguration
    : IEntityTypeConfiguration<ExternalIdentityMappingRecord>
{
    public void Configure(
        EntityTypeBuilder<ExternalIdentityMappingRecord> builder)
    {
        builder.ToTable(
            "external_identity_mappings",
            "identity",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_external_identity_mappings_user_id_non_empty",
                    "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(
                record => new
                {
                    record.Issuer,
                    record.Subject,
                })
            .HasName("pk_external_identity_mappings");

        builder.Property(record => record.Issuer)
            .HasColumnName("issuer")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.Subject)
            .HasColumnName("subject")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(record => record.UserId)
            .HasDatabaseName(
                "ix_external_identity_mappings_user_id");
    }
}
