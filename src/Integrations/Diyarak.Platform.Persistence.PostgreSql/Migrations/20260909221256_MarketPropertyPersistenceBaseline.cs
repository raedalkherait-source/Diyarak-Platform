using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketPropertyPersistenceBaseline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "market");

        migrationBuilder.CreateTable(
            name: "properties",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                category = table.Column<int>(type: "integer", nullable: false),
                street = table.Column<string>(type: "text", nullable: false),
                house_number = table.Column<string>(type: "text", nullable: false),
                postal_code = table.Column<string>(type: "text", nullable: false),
                city = table.Column<string>(type: "text", nullable: false),
                latitude = table.Column<double>(type: "double precision", nullable: true),
                longitude = table.Column<double>(type: "double precision", nullable: true),
                living_area_value = table.Column<decimal>(type: "numeric", nullable: true),
                living_area_unit = table.Column<int>(type: "integer", nullable: true),
                usable_area_value = table.Column<decimal>(type: "numeric", nullable: true),
                usable_area_unit = table.Column<int>(type: "integer", nullable: true),
                total_rooms = table.Column<decimal>(type: "numeric", nullable: true),
                bedroom_count = table.Column<int>(type: "integer", nullable: true),
                bathroom_count = table.Column<int>(type: "integer", nullable: true),
                furnishing_quality = table.Column<int>(type: "integer", nullable: true),
                features = table.Column<int[]>(type: "integer[]", nullable: false),
                construction_year = table.Column<int>(type: "integer", nullable: true),
                last_modernization_year = table.Column<int>(type: "integer", nullable: true),
                commercial_subtype = table.Column<int>(type: "integer", nullable: true),
                sales_area_value = table.Column<decimal>(type: "numeric", nullable: true),
                sales_area_unit = table.Column<int>(type: "integer", nullable: true),
                total_area_value = table.Column<decimal>(type: "numeric", nullable: true),
                total_area_unit = table.Column<int>(type: "integer", nullable: true),
                parking_space_count = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_properties", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "properties",
            schema: "market");
    }
}
