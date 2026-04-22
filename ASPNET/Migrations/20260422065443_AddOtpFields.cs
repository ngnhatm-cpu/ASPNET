using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetOtp",
                table: "Users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetOtpExpiry",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResetOtp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetOtpExpiry",
                table: "Users");
        }
    }
}
