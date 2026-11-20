using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Identity;

internal sealed class UserSessionRecordConfiguration : IEntityTypeConfiguration<UserSessionRecord>
{
    public void Configure(EntityTypeBuilder<UserSessionRecord> builder)
    {
        builder.ToTable(
            "user_sessions",
            "identity",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_user_sessions_user_id_non_empty",
                "user_id <> '00000000-0000-0000-0000-000000000000'::uuid"));

        builder.HasKey(record => record.Id).HasName("pk_user_sessions");
        builder.HasIndex(record => record.UserId).HasDatabaseName("ix_user_sessions_user_id");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.UserId).HasColumnName("user_id").HasColumnType("uuid");

        // Concurrency token: two requests rotating the same refresh token cannot both succeed.
        builder.Property(record => record.CurrentTokenHash)
            .HasColumnName("current_token_hash").HasColumnType("text").IsRequired().IsConcurrencyToken();
        builder.Property(record => record.PreviousTokenHash).HasColumnName("previous_token_hash").HasColumnType("text");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.LastRotatedAt).HasColumnName("last_rotated_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
    }
}
