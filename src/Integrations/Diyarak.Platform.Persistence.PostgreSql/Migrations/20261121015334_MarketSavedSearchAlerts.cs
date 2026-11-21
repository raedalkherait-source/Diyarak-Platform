using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketSavedSearchAlerts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "alerts_enabled",
            schema: "market",
            table: "saved_searches",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "last_alerted_at",
            schema: "market",
            table: "saved_searches",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_market_saved_searches_alerting_user_id",
            schema: "market",
            table: "saved_searches",
            column: "user_id",
            filter: "alerts_enabled");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_market_saved_searches_alerting_user_id",
            schema: "market",
            table: "saved_searches");

        migrationBuilder.DropColumn(
            name: "alerts_enabled",
            schema: "market",
            table: "saved_searches");

        migrationBuilder.DropColumn(
            name: "last_alerted_at",
            schema: "market",
            table: "saved_searches");
    }
}
