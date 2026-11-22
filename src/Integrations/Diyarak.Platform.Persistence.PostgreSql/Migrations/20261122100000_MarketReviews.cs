using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketReviews : Migration
{
    private static readonly string[] AuthorSubjectIndexColumns = ["author_user_id", "subject_id"];
    private static readonly string[] SubjectCreatedIndexColumns = ["subject_id", "created_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "reviews",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_type = table.Column<int>(type: "integer", nullable: false),
                rating = table.Column<int>(type: "integer", nullable: false),
                comment = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_reviews", x => x.id);
                table.CheckConstraint("ck_market_reviews_rating", "rating BETWEEN 1 AND 5");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_reviews_author_subject",
            schema: "market",
            table: "reviews",
            columns: AuthorSubjectIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_market_reviews_subject_created",
            schema: "market",
            table: "reviews",
            columns: SubjectCreatedIndexColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "reviews",
            schema: "market");
    }
}
