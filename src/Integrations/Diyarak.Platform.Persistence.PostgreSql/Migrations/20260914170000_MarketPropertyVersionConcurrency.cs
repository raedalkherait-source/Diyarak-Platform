using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class MarketPropertyVersionConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "version",
            schema: "market",
            table: "properties",
            type: "bigint",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE market.properties SET version = 1 WHERE version IS NULL;");

        migrationBuilder.AlterColumn<long>(
            name: "version",
            schema: "market",
            table: "properties",
            type: "bigint",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "version",
            schema: "market",
            table: "properties");
    }
}
