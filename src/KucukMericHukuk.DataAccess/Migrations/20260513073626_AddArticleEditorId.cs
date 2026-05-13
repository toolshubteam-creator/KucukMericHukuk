using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleEditorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EditorId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_EditorId",
                table: "Articles",
                column: "EditorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Users_EditorId",
                table: "Articles",
                column: "EditorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Users_EditorId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_EditorId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "EditorId",
                table: "Articles");
        }
    }
}
