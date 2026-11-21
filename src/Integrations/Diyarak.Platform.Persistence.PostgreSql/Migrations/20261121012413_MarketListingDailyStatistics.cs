using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingDailyStatistics : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "listing_daily_statistics",
            schema: "market",
            columns: table => new
            {
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                day = table.Column<DateOnly>(type: "date", nullable: false),
                views = table.Column<int>(type: "integer", nullable: false),
                contact_reveals = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_listing_daily_statistics", x => new { x.listing_id, x.day });
                table.CheckConstraint("ck_market_listing_daily_statistics_non_negative", "views >= 0 AND contact_reveals >= 0");
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_daily_statistics",
            schema: "market");
    }
}
