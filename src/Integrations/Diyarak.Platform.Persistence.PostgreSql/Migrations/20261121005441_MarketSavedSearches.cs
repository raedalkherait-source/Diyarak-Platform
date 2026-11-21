using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketSavedSearches : Migration
{
    private static readonly string[] UserIdQueryColumns = ["user_id", "query"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "saved_searches",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                query = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_saved_searches", x => x.id);
                table.CheckConstraint("ck_market_saved_searches_name_length", "char_length(name) BETWEEN 1 AND 80");
                table.CheckConstraint("ck_market_saved_searches_query_length", "char_length(query) <= 1000");
            });

        migrationBuilder.CreateIndex(
            name: "ux_market_saved_searches_user_id_query",
            schema: "market",
            table: "saved_searches",
            columns: UserIdQueryColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "saved_searches",
            schema: "market");
    }
}
