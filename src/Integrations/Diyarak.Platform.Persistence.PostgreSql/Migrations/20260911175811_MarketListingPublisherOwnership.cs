using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diyarak.Platform.Persistence.PostgreSql.Migrations;

/// <inheritdoc />
public partial class MarketListingPublisherOwnership : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM market.listings) THEN
                    RAISE EXCEPTION 'Cannot add publisher_user_id because market.listings contains rows requiring explicit ownership assignment.';
                END IF;
            END
            $$;
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "publisher_user_id",
            schema: "market",
            table: "listings",
            type: "uuid",
            nullable: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "publisher_user_id",
            schema: "market",
            table: "listings");
    }
}
