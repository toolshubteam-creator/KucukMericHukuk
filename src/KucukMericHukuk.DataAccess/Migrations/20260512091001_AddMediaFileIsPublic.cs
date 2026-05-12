using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "MediaFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_IsPublic",
                table: "MediaFiles",
                column: "IsPublic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_IsPublic",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "MediaFiles");
        }
    }
}
