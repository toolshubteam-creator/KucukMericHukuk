using FluentAssertions;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using Mapster;
using MapsterMapper;

namespace KucukMericHukuk.Tests.Mapping;

public class MappingTests
{
    private readonly IMapper _mapper;

    public MappingTests()
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ServiceMappingConfig).Assembly);
        _mapper = new Mapper(config);
    }

    [Fact]
    public void Service_To_ServiceListDto_ShouldFlattenTranslation()
    {
        var service = new Service
        {
            Id = 1,
            Icon = "scale",
            DisplayOrder = 5,
            FeaturedImage = "/img/aile.png",
            Translations = new List<ServiceTranslation>
            {
                new() { Id = 10, LanguageCode = LanguageCodes.Turkish, ServiceId = 1, Name = "Aile Hukuku", Slug = "aile-hukuku", ShortDescription = "Boşanma..." }
            }
        };

        var dto = _mapper.Map<ServiceListDto>(service);

        dto.Id.Should().Be(1);
        dto.Name.Should().Be("Aile Hukuku");
        dto.Slug.Should().Be("aile-hukuku");
        dto.ShortDescription.Should().Be("Boşanma...");
        dto.Icon.Should().Be("scale");
        dto.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public void Article_To_ArticleListDto_ShouldFlattenTranslationAndAuthor()
    {
        var article = new Article
        {
            Id = 100,
            AuthorId = 5,
            Author = new ApplicationUser { Id = 5, FullName = "Ayşe Küçükmeriç", UserName = "ayse" },
            CategoryId = 3,
            Category = new Category
            {
                Id = 3,
                Translations = new List<CategoryTranslation>
                {
                    new() { LanguageCode = LanguageCodes.Turkish, CategoryId = 3, Name = "Aile", Slug = "aile" }
                }
            },
            FeaturedImageUrl = "/img/article.jpg",
            PublishedAt = DateTime.UtcNow,
            Status = ArticleStatus.Published,
            ViewCount = 42,
            Translations = new List<ArticleTranslation>
            {
                new() { Id = 50, LanguageCode = LanguageCodes.Turkish, ArticleId = 100, Title = "Boşanma davaları", Slug = "bosanma-davalari", Excerpt = "Özet...", Content = "İçerik...", ReadingTimeMinutes = 5 }
            }
        };

        var dto = _mapper.Map<ArticleListDto>(article);

        dto.Title.Should().Be("Boşanma davaları");
        dto.Slug.Should().Be("bosanma-davalari");
        dto.Excerpt.Should().Be("Özet...");
        dto.AuthorName.Should().Be("Ayşe Küçükmeriç");
        dto.CategoryName.Should().Be("Aile");
        dto.CategorySlug.Should().Be("aile");
        dto.ViewCount.Should().Be(42);
        dto.ReadingTimeMinutes.Should().Be(5);
    }

    [Fact]
    public void Article_To_ArticleAdminDto_ShouldIncludeAllTranslations()
    {
        var article = new Article
        {
            Id = 200,
            Status = ArticleStatus.Draft,
            Translations = new List<ArticleTranslation>
            {
                new() { Id = 1, LanguageCode = "tr-TR", ArticleId = 200, Title = "TR başlık", Content = "TR içerik" },
                new() { Id = 2, LanguageCode = "en-US", ArticleId = 200, Title = "EN title", Content = "EN content" }
            }
        };

        var dto = _mapper.Map<ArticleAdminDto>(article);

        dto.Translations.Should().HaveCount(2);
        dto.Translations.Should().Contain(t => t.LanguageCode == "tr-TR" && t.Title == "TR başlık");
        dto.Translations.Should().Contain(t => t.LanguageCode == "en-US" && t.Title == "EN title");
    }
}
