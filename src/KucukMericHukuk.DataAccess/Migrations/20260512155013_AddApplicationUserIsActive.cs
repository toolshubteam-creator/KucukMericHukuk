using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
