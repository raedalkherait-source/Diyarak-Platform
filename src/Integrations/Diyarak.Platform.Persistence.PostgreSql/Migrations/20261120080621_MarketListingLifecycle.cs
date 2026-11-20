using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingLifecycle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "closure_reason",
            schema: "market",
            table: "listings",
            type: "integer",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_market_listings_closure_reason",
            schema: "market",
            table: "listings",
            sql: "(status = 4) = (closure_reason IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_market_listings_closure_reason",
            schema: "market",
            table: "listings");

        migrationBuilder.DropColumn(
            name: "closure_reason",
            schema: "market",
            table: "listings");
    }
}
