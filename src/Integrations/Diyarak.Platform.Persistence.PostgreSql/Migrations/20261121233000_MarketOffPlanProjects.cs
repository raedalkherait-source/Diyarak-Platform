using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketOffPlanProjects : Migration
{
    private static readonly string[] PublishedIndexColumns = ["is_published", "created_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "off_plan_projects",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                governorate = table.Column<string>(type: "text", nullable: false),
                district = table.Column<string>(type: "text", nullable: false),
                construction_status = table.Column<int>(type: "integer", nullable: false),
                total_units = table.Column<int>(type: "integer", nullable: false),
                min_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                max_price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                expected_completion = table.Column<DateOnly>(type: "date", nullable: true),
                is_published = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_off_plan_projects", x => x.id);
                table.CheckConstraint("ck_market_off_plan_projects_name_length", "char_length(name) BETWEEN 3 AND 120");
                table.CheckConstraint("ck_market_off_plan_projects_total_units", "total_units > 0");
                table.CheckConstraint("ck_market_off_plan_projects_price_range", "min_price_usd IS NULL OR max_price_usd IS NULL OR min_price_usd <= max_price_usd");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_off_plan_projects_agency_id",
            schema: "market",
            table: "off_plan_projects",
            column: "agency_id");

        migrationBuilder.CreateIndex(
            name: "ix_market_off_plan_projects_published",
            schema: "market",
            table: "off_plan_projects",
            columns: PublishedIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "off_plan_projects",
            schema: "market");
    }
}
