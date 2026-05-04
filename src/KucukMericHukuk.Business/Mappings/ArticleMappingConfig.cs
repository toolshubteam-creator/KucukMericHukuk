using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Tag;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class ArticleMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend List
        config.NewConfig<Article, ArticleListDto>()
            .Map(dest => dest.Title, src => src.Translations.Select(t => t.Title).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Excerpt, src => src.Translations.Select(t => t.Excerpt).FirstOrDefault())
            .Map(dest => dest.ReadingTimeMinutes, src => src.Translations.Select(t => t.ReadingTimeMinutes).FirstOrDefault())
            .Map(dest => dest.AuthorName,
                 src => src.Author != null ? (src.Author.FullName ?? src.Author.UserName) : null)
            .Map(dest => dest.CategoryName,
                 src => src.Category != null
                     ? src.Category.Translations.Select(t => t.Name).FirstOrDefault()
                     : null)
            .Map(dest => dest.CategorySlug,
                 src => src.Category != null
                     ? src.Category.Translations.Select(t => t.Slug).FirstOrDefault()
                     : null);

        // Read: Entity → Frontend Detail
        config.NewConfig<Article, ArticleDetailDto>()
            .Map(dest => dest.Title, src => src.Translations.Select(t => t.Title).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Excerpt, src => src.Translations.Select(t => t.Excerpt).FirstOrDefault())
            .Map(dest => dest.Content, src => src.Translations.Select(t => t.Content).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.ReadingTimeMinutes, src => src.Translations.Select(t => t.ReadingTimeMinutes).FirstOrDefault())
            .Map(dest => dest.MetaTitle, src => src.Translations.Select(t => t.MetaTitle).FirstOrDefault())
            .Map(dest => dest.MetaDescription, src => src.Translations.Select(t => t.MetaDescription).FirstOrDefault())
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.AuthorName,
                 src => src.Author != null ? (src.Author.FullName ?? src.Author.UserName) : null)
            .Map(dest => dest.CategoryName,
                 src => src.Category != null
                     ? src.Category.Translations.Select(t => t.Name).FirstOrDefault()
                     : null)
            .Map(dest => dest.CategorySlug,
                 src => src.Category != null
                     ? src.Category.Translations.Select(t => t.Slug).FirstOrDefault()
                     : null)
            .Map(dest => dest.Tags, src => src.Tags.Adapt<List<TagListDto>>());

        // Read: Entity → Admin
        config.NewConfig<Article, ArticleAdminDto>()
            .Map(dest => dest.AuthorName,
                 src => src.Author != null ? (src.Author.FullName ?? src.Author.UserName) : null)
            .Map(dest => dest.CategoryName,
                 src => src.Category != null
                     ? src.Category.Translations.Select(t => t.Name).FirstOrDefault()
                     : null)
            .Map(dest => dest.Tags,
                 src => src.Tags.Select(t => new LookupDto
                 {
                     Id = t.Id,
                     Name = t.Translations.Select(tr => tr.Name).FirstOrDefault() ?? string.Empty
                 }));
        config.NewConfig<ArticleTranslation, ArticleTranslationDto>();

        // Write
        config.NewConfig<ArticleInputDto, Article>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!)
            .Ignore(dest => dest.ViewCount)
            .Ignore(dest => dest.Author!)
            .Ignore(dest => dest.Category!)
            .Ignore(dest => dest.Tags);

        config.NewConfig<ArticleTranslationInputDto, ArticleTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.ReadingTimeMinutes);
    }
}
