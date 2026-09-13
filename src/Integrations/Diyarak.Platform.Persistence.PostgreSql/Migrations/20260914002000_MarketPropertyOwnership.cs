using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class MarketPropertyOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "owner_user_id",
            schema: "market",
            table: "properties",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_market_properties_owner_user_id_non_empty",
            schema: "market",
            table: "properties",
            sql: "owner_user_id IS NULL OR owner_user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_market_properties_owner_user_id_non_empty",
            schema: "market",
            table: "properties");

        migrationBuilder.DropColumn(
            name: "owner_user_id",
            schema: "market",
            table: "properties");
    }
}
