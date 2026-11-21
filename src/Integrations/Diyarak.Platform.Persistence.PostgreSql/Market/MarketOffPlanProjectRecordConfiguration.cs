using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketOffPlanProjectRecordConfiguration
    : IEntityTypeConfiguration<MarketOffPlanProjectRecord>
{
    public void Configure(EntityTypeBuilder<MarketOffPlanProjectRecord> builder)
    {
        builder.ToTable(
            "off_plan_projects",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_market_off_plan_projects_name_length", "char_length(name) BETWEEN 3 AND 120");
                tableBuilder.HasCheckConstraint("ck_market_off_plan_projects_total_units", "total_units > 0");
                tableBuilder.HasCheckConstraint("ck_market_off_plan_projects_price_range", "min_price_usd IS NULL OR max_price_usd IS NULL OR min_price_usd <= max_price_usd");
            });

        builder.HasKey(record => record.Id).HasName("pk_market_off_plan_projects");

        builder.HasIndex(record => record.AgencyId).HasDatabaseName("ix_market_off_plan_projects_agency_id");
        builder.HasIndex(record => new { record.IsPublished, record.CreatedAt }).HasDatabaseName("ix_market_off_plan_projects_published");

        builder.Property(record => record.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(record => record.AgencyId).HasColumnName("agency_id").HasColumnType("uuid");
        builder.Property(record => record.Version).HasColumnName("version").HasColumnType("bigint").IsConcurrencyToken();
        builder.Property(record => record.Name).HasColumnName("name").HasColumnType("text");
        builder.Property(record => record.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(record => record.Governorate).HasColumnName("governorate").HasColumnType("text");
        builder.Property(record => record.District).HasColumnName("district").HasColumnType("text");
        builder.Property(record => record.ConstructionStatus).HasColumnName("construction_status").HasColumnType("integer");
        builder.Property(record => record.TotalUnits).HasColumnName("total_units").HasColumnType("integer");
        builder.Property(record => record.MinPriceUsd).HasColumnName("min_price_usd").HasColumnType("numeric");
        builder.Property(record => record.MaxPriceUsd).HasColumnName("max_price_usd").HasColumnType("numeric");
        builder.Property(record => record.ExpectedCompletion).HasColumnName("expected_completion").HasColumnType("date");
        builder.Property(record => record.IsPublished).HasColumnName("is_published").HasColumnType("boolean");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
    }
}
