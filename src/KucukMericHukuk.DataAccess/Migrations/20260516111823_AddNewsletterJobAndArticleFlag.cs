using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KucukMericHukuk.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterJobAndArticleFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NewsletterSentAt",
                table: "Articles",
                type: "datetime2",
                nullable: true);

            // Faz 7.2b-1: Mevcut makaleler "zaten bulten gonderildi" sayilir.
            // Aksi takdirde migration sonrasi TUM eski yayinlanan makaleler "bulten bekliyor"
            // listesine duser ve admin yanlislikla mukerrer gonderebilir.
            // COALESCE: oncelik PublishedAt (yayin tarihi), yoksa CreatedAt (kayit tarihi).
            // Draft makaleler de backfill olur ama pending listesi Status=Published filtresi ile
            // onlari zaten dislar — yine de tutarli bir flag deger atamak guvenli.
            migrationBuilder.Sql(@"
                UPDATE Articles
                SET NewsletterSentAt = COALESCE(PublishedAt, CreatedAt)
                WHERE NewsletterSentAt IS NULL;");

            migrationBuilder.CreateTable(
                name: "NewsletterJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalRecipients = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsletterJobs_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterJobs_ArticleId",
                table: "NewsletterJobs",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterJobs_CreatedAt",
                table: "NewsletterJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterJobs_Status",
                table: "NewsletterJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsletterJobs");

            migrationBuilder.DropColumn(
                name: "NewsletterSentAt",
                table: "Articles");
        }
    }
}
