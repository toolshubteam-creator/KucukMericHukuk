using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNotFoundLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotFoundLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(850)", maxLength: 850, nullable: false),
                    Referer = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFoundLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotFoundLogs_HitCount",
                table: "NotFoundLogs",
                column: "HitCount",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NotFoundLogs_LastSeenAt",
                table: "NotFoundLogs",
                column: "LastSeenAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NotFoundLogs_Url",
                table: "NotFoundLogs",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotFoundLogs");
        }
    }
}
