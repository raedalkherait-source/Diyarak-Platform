using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

public partial class ExternalIdentityMappings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "external_identity_mappings",
            schema: "identity",
            columns: table => new
            {
                issuer = table.Column<string>(
                    type: "text",
                    nullable: false),
                subject = table.Column<string>(
                    type: "text",
                    nullable: false),
                user_id = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "pk_external_identity_mappings",
                    x => new
                    {
                        x.issuer,
                        x.subject,
                    });

                table.CheckConstraint(
                    "ck_external_identity_mappings_user_id_non_empty",
                    "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });

        migrationBuilder.CreateIndex(
            name: "ix_external_identity_mappings_user_id",
            schema: "identity",
            table: "external_identity_mappings",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "external_identity_mappings",
            schema: "identity");
    }
}
