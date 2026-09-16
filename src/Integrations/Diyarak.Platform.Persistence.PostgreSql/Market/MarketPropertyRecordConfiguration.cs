using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Diyarak.Platform.Persistence.PostgreSql.Market;

internal sealed class MarketPropertyRecordConfiguration
    : IEntityTypeConfiguration<MarketPropertyRecord>
{
    public void Configure(
        EntityTypeBuilder<MarketPropertyRecord> builder)
    {
        builder.ToTable(
            "properties",
            "market",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_market_properties_owner_user_id_non_empty",
                    "owner_user_id IS NULL OR owner_user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        builder.HasKey(record => record.Id);

        builder.HasIndex(record => record.OwnerUserId)
            .HasDatabaseName(
                "ix_market_properties_owner_user_id");

        builder.Property(record => record.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(record => record.OwnerUserId)
            .HasColumnName("owner_user_id")
            .HasColumnType("uuid");

        builder.Property(record => record.Version)
            .HasColumnName("version")
            .HasColumnType("bigint")
            .IsConcurrencyToken();

        builder.Property(record => record.Category)
            .HasColumnName("category")
            .HasColumnType("integer");

        builder.Property(record => record.Street)
            .HasColumnName("street")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.HouseNumber)
            .HasColumnName("house_number")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.PostalCode)
            .HasColumnName("postal_code")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.City)
            .HasColumnName("city")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(record => record.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("double precision");

        builder.Property(record => record.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("double precision");

        builder.Property(record => record.LivingAreaValue)
            .HasColumnName("living_area_value")
            .HasColumnType("numeric");

        builder.Property(record => record.LivingAreaUnit)
            .HasColumnName("living_area_unit")
            .HasColumnType("integer");

        builder.Property(record => record.UsableAreaValue)
            .HasColumnName("usable_area_value")
            .HasColumnType("numeric");

        builder.Property(record => record.UsableAreaUnit)
            .HasColumnName("usable_area_unit")
            .HasColumnType("integer");

        builder.Property(record => record.TotalRooms)
            .HasColumnName("total_rooms")
            .HasColumnType("numeric");

        builder.Property(record => record.BedroomCount)
            .HasColumnName("bedroom_count")
            .HasColumnType("integer");

        builder.Property(record => record.BathroomCount)
            .HasColumnName("bathroom_count")
            .HasColumnType("integer");

        builder.Property(record => record.FurnishingQuality)
            .HasColumnName("furnishing_quality")
            .HasColumnType("integer");

        builder.Property(record => record.Features)
            .HasColumnName("features")
            .HasColumnType("integer[]")
            .IsRequired();

        builder.Property(record => record.ConstructionYear)
            .HasColumnName("construction_year")
            .HasColumnType("integer");

        builder.Property(record => record.LastModernizationYear)
            .HasColumnName("last_modernization_year")
            .HasColumnType("integer");

        builder.Property(record => record.CommercialSubtype)
            .HasColumnName("commercial_subtype")
            .HasColumnType("integer");

        builder.Property(record => record.SalesAreaValue)
            .HasColumnName("sales_area_value")
            .HasColumnType("numeric");

        builder.Property(record => record.SalesAreaUnit)
            .HasColumnName("sales_area_unit")
            .HasColumnType("integer");

        builder.Property(record => record.TotalAreaValue)
            .HasColumnName("total_area_value")
            .HasColumnType("numeric");

        builder.Property(record => record.TotalAreaUnit)
            .HasColumnName("total_area_unit")
            .HasColumnType("integer");

        builder.Property(record => record.ParkingSpaceCount)
            .HasColumnName("parking_space_count")
            .HasColumnType("integer");
    }
}
