using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketAgencies : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agencies",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                governorate = table.Column<string>(type: "text", nullable: true),
                city = table.Column<string>(type: "text", nullable: false),
                verification_status = table.Column<int>(type: "integer", nullable: false),
                registration_number = table.Column<string>(type: "text", nullable: true),
                verification_note = table.Column<string>(type: "text", nullable: true),
                verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_agencies", x => x.id);
                table.CheckConstraint("ck_market_agencies_name_length", "char_length(name) BETWEEN 2 AND 80");
                table.CheckConstraint("ck_market_agencies_registration", "verification_status = 0 OR registration_number IS NOT NULL");
                table.CheckConstraint("ck_market_agencies_verified_at", "(verification_status = 2) = (verified_at IS NOT NULL)");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_agencies_verification_status",
            schema: "market",
            table: "agencies",
            column: "verification_status");

        migrationBuilder.CreateIndex(
            name: "ux_market_agencies_owner_user_id",
            schema: "market",
            table: "agencies",
            column: "owner_user_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "agencies",
            schema: "market");
    }
}
