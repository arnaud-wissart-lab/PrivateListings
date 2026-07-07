using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateListings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingMvpFields : Migration
    {
        private static readonly string[] StatusExpiresAtColumns = ["status", "expires_at"];
        private static readonly string[] TypeStatusColumns = ["type", "status"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "approximate_location",
                table: "listings",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "event_date",
                table: "listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "listings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_listings_status_expires_at",
                table: "listings",
                columns: StatusExpiresAtColumns);

            migrationBuilder.CreateIndex(
                name: "ix_listings_type_status",
                table: "listings",
                columns: TypeStatusColumns);

            migrationBuilder.AddCheckConstraint(
                name: "ck_listings_price_positive",
                table: "listings",
                sql: "price IS NULL OR price >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_listings_status_expires_at",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "ix_listings_type_status",
                table: "listings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_listings_price_positive",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "approximate_location",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "event_date",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "price",
                table: "listings");
        }
    }
}
