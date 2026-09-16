using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class MarketManagementOwnershipQueryIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_properties_owner_user_id",
            schema: "market",
            table: "properties");

        migrationBuilder.DropIndex(
            name: "ix_market_listings_publisher_user_id",
            schema: "market",
            table: "listings");
    }
}
