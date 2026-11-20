using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketPropertySyrianMarketDetails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "street",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "postal_code",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "house_number",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<int>(
            name: "building_floor_count",
            schema: "market",
            table: "properties",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "district",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "finishing_level",
            schema: "market",
            table: "properties",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "floor_number",
            schema: "market",
            table: "properties",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "governorate",
            schema: "market",
            table: "properties",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "legal_title_type",
            schema: "market",
            table: "properties",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "building_floor_count",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "district",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "finishing_level",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "floor_number",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "governorate",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "legal_title_type",
            schema: "market",
            table: "properties");

        // Rows created after this migration may have no street, house number, or postal
        // code. Restore the previous NOT NULL contract with empty strings for those rows.
        migrationBuilder.Sql(
            "UPDATE market.properties SET street = COALESCE(street, ''), house_number = COALESCE(house_number, ''), postal_code = COALESCE(postal_code, '') WHERE street IS NULL OR house_number IS NULL OR postal_code IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "street",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "postal_code",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "house_number",
            schema: "market",
            table: "properties",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
