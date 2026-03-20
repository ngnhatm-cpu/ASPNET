using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class DigitalUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Mangas_MangaId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_MangaId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "MangaId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Mangas");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "OrderItems",
                newName: "ChapterId");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MangaId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Mangas_MangaId",
                        column: x => x.MangaId,
                        principalTable: "Mangas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLibraries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLibraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLibraries_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLibraries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ChapterId",
                table: "OrderItems",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_MangaId",
                table: "Chapters",
                column: "MangaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLibraries_ChapterId",
                table: "UserLibraries",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLibraries_UserId",
                table: "UserLibraries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Chapters_ChapterId",
                table: "OrderItems",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Chapters_ChapterId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "UserLibraries");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ChapterId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ChapterId",
                table: "OrderItems",
                newName: "Quantity");

            migrationBuilder.AddColumn<int>(
                name: "MangaId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Mangas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MangaId",
                table: "OrderItems",
                column: "MangaId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Mangas_MangaId",
                table: "OrderItems",
                column: "MangaId",
                principalTable: "Mangas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
