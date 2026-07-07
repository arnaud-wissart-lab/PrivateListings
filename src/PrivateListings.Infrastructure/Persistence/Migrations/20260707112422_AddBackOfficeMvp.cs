using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrivateListings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackOfficeMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "suspended_at",
                table: "identity_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "identity_users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_users_suspended_at",
                table: "identity_users",
                column: "suspended_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_identity_users_suspended_at",
                table: "identity_users");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "identity_users");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "identity_users");
        }
    }
}
