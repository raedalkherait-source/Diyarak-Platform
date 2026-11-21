using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketPaymentsAndPromotions : Migration
{
    private static readonly string[] IndexColumns0 = ["listing_id", "ends_at"];

    private static readonly string[] IndexColumns1 = ["status", "created_at"];

    private static readonly string[] IndexColumns2 = ["user_id", "created_at"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "listing_promotions",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_listing_promotions", x => x.id);
                table.CheckConstraint("ck_market_listing_promotions_period", "ends_at > starts_at");
            });

        migrationBuilder.CreateTable(
            name: "payment_orders",
            schema: "market",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                product = table.Column<int>(type: "integer", nullable: false),
                listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                currency = table.Column<string>(type: "character(3)", nullable: false),
                method = table.Column<int>(type: "integer", nullable: false),
                payer_reference = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                decision_note = table.Column<string>(type: "text", nullable: true),
                decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                decided_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_market_payment_orders", x => x.id);
                table.CheckConstraint("ck_market_payment_orders_amount", "amount > 0");
                table.CheckConstraint("ck_market_payment_orders_decision", "(status = 1) = (decided_at IS NULL)");
                table.CheckConstraint("ck_market_payment_orders_rejection_note", "status <> 3 OR decision_note IS NOT NULL");
            });

        migrationBuilder.CreateIndex(
            name: "ix_market_listing_promotions_listing_id_ends_at",
            schema: "market",
            table: "listing_promotions",
            columns: IndexColumns0);

        migrationBuilder.CreateIndex(
            name: "ux_market_listing_promotions_order_id",
            schema: "market",
            table: "listing_promotions",
            column: "order_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_market_payment_orders_status_created_at",
            schema: "market",
            table: "payment_orders",
            columns: IndexColumns1);

        migrationBuilder.CreateIndex(
            name: "ix_market_payment_orders_user_id_created_at",
            schema: "market",
            table: "payment_orders",
            columns: IndexColumns2);

        migrationBuilder.CreateIndex(
            name: "ux_market_payment_orders_pending_listing",
            schema: "market",
            table: "payment_orders",
            column: "listing_id",
            unique: true,
            filter: "status = 1");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "listing_promotions",
            schema: "market");

        migrationBuilder.DropTable(
            name: "payment_orders",
            schema: "market");
    }
}
