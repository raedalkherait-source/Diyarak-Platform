using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketContractors : Migration
{
    private static readonly string[] PublishedIndexColumns = ["is_published", "created_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contractors",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                governorate = table.Column<string>(type: "text", nullable: false),
                district = table.Column<string>(type: "text", nullable: true),
                specialty = table.Column<int>(type: "integer", nullable: false),
                phone = table.Column<string>(type: "text", nullable: false),
                is_published = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_contractors", x => x.id);
                table.CheckConstraint("ck_market_contractors_name_length", "char_length(name) BETWEEN 2 AND 100");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_contractors_owner_user_id",
            schema: "market",
            table: "contractors",
            column: "owner_user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_market_contractors_published",
            schema: "market",
            table: "contractors",
            columns: PublishedIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "contractors",
            schema: "market");
    }
}
