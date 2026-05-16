using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.Core.Interfaces;

namespace KucukMericHukuk.Core.Entities;

public class Article : BaseEntity, ITranslatable<ArticleTranslation>
{
    public int? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    public int? EditorId { get; set; }
    public ApplicationUser? Editor { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? FeaturedImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Faz 7.2b-1: NULL → bülten henüz gönderilmedi (Published + NULL = pending).
    /// Dolu → bülten gönderildi (NewsletterJob Completed sırasında set edilir).
    /// </summary>
    public DateTime? NewsletterSentAt { get; set; }

    public ICollection<ArticleTranslation> Translations { get; set; } = new List<ArticleTranslation>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
