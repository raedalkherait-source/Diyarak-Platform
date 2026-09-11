using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingPersistenceBaseline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "listings",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_type = table.Column<string>(type: "text", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                publishing_role = table.Column<int>(type: "integer", nullable: true),
                transaction_intent = table.Column<int>(type: "integer", nullable: true),
                headline = table.Column<string>(type: "text", nullable: true),
                price_is_on_request = table.Column<bool>(type: "boolean", nullable: true),
                price_amount = table.Column<decimal>(type: "numeric", nullable: true),
                price_currency = table.Column<string>(type: "text", nullable: true),
                available_from_date = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_listings", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listings",
            schema: "market");
    }
}
