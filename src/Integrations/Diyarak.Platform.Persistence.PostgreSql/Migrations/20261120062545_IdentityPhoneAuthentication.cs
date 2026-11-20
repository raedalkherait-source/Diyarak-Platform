using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class IdentityPhoneAuthentication : Migration
{
    private static readonly string[] ChallengeLookupColumns =
    [
        "phone_number",
        "issued_at",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "phone_accounts",
            schema: "identity",
            columns: table => new
            {
                phone_number = table.Column<string>(type: "text", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_phone_accounts", x => x.phone_number);
                table.CheckConstraint("ck_phone_accounts_user_id_non_empty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        migrationBuilder.CreateTable(
            name: "phone_verification_challenges",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                phone_number = table.Column<string>(type: "text", nullable: false),
                code_hash = table.Column<string>(type: "text", nullable: false),
                issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                failed_attempts = table.Column<int>(type: "integer", nullable: false),
                consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_phone_verification_challenges", x => x.id);
                table.CheckConstraint("ck_phone_verification_challenges_expiry_after_issue", "expires_at > issued_at");
                table.CheckConstraint("ck_phone_verification_challenges_failed_attempts_range", "failed_attempts >= 0 AND failed_attempts <= 5");
            });

        migrationBuilder.CreateIndex(
            name: "ux_phone_accounts_user_id",
            schema: "identity",
            table: "phone_accounts",
            column: "user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_phone_verification_challenges_phone_number_issued_at",
            schema: "identity",
            table: "phone_verification_challenges",
            columns: ChallengeLookupColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "phone_accounts",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "phone_verification_challenges",
            schema: "identity");
    }
}
