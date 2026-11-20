using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingContactOptions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "contact_allow_calls",
            schema: "market",
            table: "listings",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "contact_allow_whatsapp",
            schema: "market",
            table: "listings",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_market_listings_contact_options",
            schema: "market",
            table: "listings",
            sql: "(contact_allow_calls IS NULL) = (contact_allow_whatsapp IS NULL) AND (contact_allow_calls IS NULL OR contact_allow_calls OR contact_allow_whatsapp)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_market_listings_contact_options",
            schema: "market",
            table: "listings");

        migrationBuilder.DropColumn(
            name: "contact_allow_calls",
            schema: "market",
            table: "listings");

        migrationBuilder.DropColumn(
            name: "contact_allow_whatsapp",
            schema: "market",
            table: "listings");
    }
}
