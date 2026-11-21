using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketAgencyPackages : Migration
{
    private static readonly string[] IndexColumns = ["agency_id", "expires_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agency_packages",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                agency_id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_quota = table.Column<int>(type: "integer", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                note = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_agency_packages", x => x.id);
                table.CheckConstraint("ck_market_agency_packages_expiry", "expires_at > created_at");
                table.CheckConstraint("ck_market_agency_packages_quota", "listing_quota > 0");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_agency_packages_agency_expires",
            schema: "market",
            table: "agency_packages",
            columns: IndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "agency_packages",
            schema: "market");
    }
}
