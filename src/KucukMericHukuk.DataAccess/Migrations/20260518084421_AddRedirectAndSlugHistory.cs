using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRedirectAndSlugHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Redirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ToPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false, defaultValue: 301),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HitCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastHitAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redirects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlugHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OldSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlugHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_FromPath",
                table: "Redirects",
                column: "FromPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Redirects_IsActive",
                table: "Redirects",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SlugHistories_EntityType_EntityId",
                table: "SlugHistories",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlugHistories_EntityType_LanguageCode_OldSlug",
                table: "SlugHistories",
                columns: new[] { "EntityType", "LanguageCode", "OldSlug" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Redirects");

            migrationBuilder.DropTable(
                name: "SlugHistories");
        }
    }
}
