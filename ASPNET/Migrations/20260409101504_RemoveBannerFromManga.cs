using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBannerFromManga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Mangas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Mangas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
