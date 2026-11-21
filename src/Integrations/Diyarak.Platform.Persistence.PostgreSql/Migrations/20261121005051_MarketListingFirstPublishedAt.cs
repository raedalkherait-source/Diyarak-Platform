using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingFirstPublishedAt : Migration
{
    private static readonly string[] StatusFirstPublishedAtIdColumns = ["status", "first_published_at", "id"];

    private static readonly bool[] StatusFirstPublishedAtIdDescending = [false, true, false];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "first_published_at",
            schema: "market",
            table: "listings",
            type: "timestamp with time zone",
            nullable: true);

        // Listings published before this migration have no recorded time; the migration time is
        // the best available value and keeps them ordered before anything published later.
        migrationBuilder.Sql(
            "UPDATE market.listings SET first_published_at = now() WHERE status <> 1 AND first_published_at IS NULL;");

        migrationBuilder.CreateIndex(
            name: "ix_market_listings_status_first_published_at",
            schema: "market",
            table: "listings",
            columns: StatusFirstPublishedAtIdColumns,
            descending: StatusFirstPublishedAtIdDescending);

        migrationBuilder.AddCheckConstraint(
            name: "ck_market_listings_first_published_at",
            schema: "market",
            table: "listings",
            sql: "(status = 1) = (first_published_at IS NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_listings_status_first_published_at",
            schema: "market",
            table: "listings");

        migrationBuilder.DropCheckConstraint(
            name: "ck_market_listings_first_published_at",
            schema: "market",
            table: "listings");

        migrationBuilder.DropColumn(
            name: "first_published_at",
            schema: "market",
            table: "listings");
    }
}
