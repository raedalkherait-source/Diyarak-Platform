using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingFavorites : Migration
{
    private static readonly string[] UserIdCreatedAtColumns = ["user_id", "created_at"];

    private static readonly bool[] UserIdCreatedAtDescending = [false, true];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "listing_favorites",
            schema: "market",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_listing_favorites", x => new { x.user_id, x.listing_id });
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_listing_favorites_user_id_created_at",
            schema: "market",
            table: "listing_favorites",
            columns: UserIdCreatedAtColumns,
            descending: UserIdCreatedAtDescending);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_favorites",
            schema: "market");
    }
}
