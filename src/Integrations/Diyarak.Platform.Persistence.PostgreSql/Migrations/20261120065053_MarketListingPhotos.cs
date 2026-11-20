using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingPhotos : Migration
{
    private static readonly string[] ListingPositionColumns =
    [
        "listing_id",
        "position",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "listing_photos",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                storage_key = table.Column<string>(type: "text", nullable: false),
                format = table.Column<int>(type: "integer", nullable: false),
                size_bytes = table.Column<long>(type: "bigint", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
                uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_listing_photos", x => x.id);
                table.CheckConstraint("ck_market_listing_photos_position_range", "position >= 0 AND position < 30");
                table.CheckConstraint("ck_market_listing_photos_size_range", "size_bytes > 0 AND size_bytes <= 10485760");
            });

        migrationBuilder.CreateIndex(
            name: "ux_market_listing_photos_listing_id_position",
            schema: "market",
            table: "listing_photos",
            columns: ListingPositionColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_market_listing_photos_storage_key",
            schema: "market",
            table: "listing_photos",
            column: "storage_key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_photos",
            schema: "market");
    }
}
