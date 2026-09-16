using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class MarketManagementCollectionPagination : Migration
{
    private static readonly string[] ListingColumns =
    [
        "publisher_user_id",
        "id",
    ];

    private static readonly string[] PropertyColumns =
    [
        "owner_user_id",
        "id",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_listings_publisher_user_id",
            schema: "market",
            table: "listings");

        migrationBuilder.DropIndex(
            name: "ix_market_properties_owner_user_id",
            schema: "market",
            table: "properties");

        migrationBuilder.CreateIndex(
            name: "ix_market_listings_publisher_user_id",
            schema: "market",
            table: "listings",
            columns: ListingColumns);

        migrationBuilder.CreateIndex(
            name: "ix_market_properties_owner_user_id",
            schema: "market",
            table: "properties",
            columns: PropertyColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_listings_publisher_user_id",
            schema: "market",
            table: "listings");

        migrationBuilder.DropIndex(
            name: "ix_market_properties_owner_user_id",
            schema: "market",
            table: "properties");

        migrationBuilder.CreateIndex(
            name: "ix_market_listings_publisher_user_id",
            schema: "market",
            table: "listings",
            column: "publisher_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_market_properties_owner_user_id",
            schema: "market",
            table: "properties",
            column: "owner_user_id");
    }
}
