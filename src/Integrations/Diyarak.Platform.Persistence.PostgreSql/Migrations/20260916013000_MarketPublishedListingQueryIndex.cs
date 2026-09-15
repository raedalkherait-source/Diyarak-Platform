using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class MarketPublishedListingQueryIndex : Migration
{
    private static readonly string[] PublishedListingQueryIndexColumns =
    [
        "status",
        "id",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_market_listings_status_id",
            schema: "market",
            table: "listings",
            columns: PublishedListingQueryIndexColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_listings_status_id",
            schema: "market",
            table: "listings");
    }
}
