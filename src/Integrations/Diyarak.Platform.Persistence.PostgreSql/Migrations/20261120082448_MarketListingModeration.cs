using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingModeration : Migration
{
    private static readonly string[] ListingCreatedColumns =
    [
        "listing_id",
        "created_at",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "moderation_note",
            schema: "market",
            table: "listings",
            type: "text",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "listing_reports",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                reason = table.Column<int>(type: "integer", nullable: false),
                comment = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                resolution = table.Column<int>(type: "integer", nullable: true),
                resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_listing_reports", x => x.id);
                table.CheckConstraint("ck_market_listing_reports_comment_length", "comment IS NULL OR char_length(comment) <= 500");
                table.CheckConstraint("ck_market_listing_reports_resolution", "(resolution IS NULL) = (resolved_at IS NULL) AND (resolution IS NULL) = (resolved_by_user_id IS NULL)");
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_market_listings_moderation_note",
            schema: "market",
            table: "listings",
            sql: "(status = 5) = (moderation_note IS NOT NULL)");

        migrationBuilder.CreateIndex(
            name: "ix_market_listing_reports_listing_id_created_at",
            schema: "market",
            table: "listing_reports",
            columns: ListingCreatedColumns);

        migrationBuilder.CreateIndex(
            name: "ix_market_listing_reports_open_created_at",
            schema: "market",
            table: "listing_reports",
            column: "created_at",
            filter: "resolution IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_reports",
            schema: "market");

        migrationBuilder.DropCheckConstraint(
            name: "ck_market_listings_moderation_note",
            schema: "market",
            table: "listings");

        migrationBuilder.DropColumn(
            name: "moderation_note",
            schema: "market",
            table: "listings");
    }
}
