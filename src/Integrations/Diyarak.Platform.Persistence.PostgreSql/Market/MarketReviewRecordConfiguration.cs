using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketReviewRecordConfiguration
    : IEntityTypeConfiguration<MarketReviewRecord>
{
    public void Configure(EntityTypeBuilder<MarketReviewRecord> builder)
    {
        builder.ToTable(
            "reviews",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_reviews_rating",
                    "rating BETWEEN 1 AND 5");
            });

        builder.HasKey(record => record.Id)
            .HasName("pk_market_reviews");

        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.AuthorUserId).HasColumnName("author_user_id");
        builder.Property(record => record.SubjectId).HasColumnName("subject_id");
        builder.Property(record => record.SubjectType).HasColumnName("subject_type");
        builder.Property(record => record.Rating).HasColumnName("rating");
        builder.Property(record => record.Comment).HasColumnName("comment");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(record => new { record.AuthorUserId, record.SubjectId })
            .HasDatabaseName("ix_market_reviews_author_subject")
            .IsUnique();

        builder.HasIndex(record => new { record.SubjectId, record.CreatedAt })
            .HasDatabaseName("ix_market_reviews_subject_created");
    }
}
