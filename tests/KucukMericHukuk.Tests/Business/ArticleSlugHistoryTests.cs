using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Enums;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

/// <summary>
/// Faz 7.4.3a — Article PİLOT: slug-change yakalama (TranslationMergeHelper'a
/// dokunmadan servis-içi snapshot pattern). Diğer 5 servis aynı pattern'i izler.
/// </summary>
public class ArticleSlugHistoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<ArticleInputDto> _validator;

    public ArticleSlugHistoryTests()
    {
        _factory = new TestDbContextFactory();
        var config = new TypeAdapterConfig();
        config.Scan(typeof(ArticleMappingConfig).Assembly);
        _mapper = new Mapper(config);
        _validator = new ArticleInputValidator();
    }

    private ArticleService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var slugHistoryService = new SlugHistoryService(uow);
        var sanitizer = new HtmlSanitizerService();
        return new ArticleService(uow, slugService, slugHistoryService, _mapper, _validator, sanitizer);
    }

    private static int SeedPublishedArticle(AppDbContext context, string slug, string? secondLangSlug = null)
    {
        var article = new Article
        {
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Translations = new List<ArticleTranslation>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Title = "Test",
                    Slug = slug,
                    Content = "<p>x</p>",
                    CreatedAt = DateTime.UtcNow,
                }
            }
        };
        if (secondLangSlug is not null)
        {
            article.Translations.Add(new ArticleTranslation
            {
                LanguageCode = "en-US",
                Title = "Test EN",
                Slug = secondLangSlug,
                Content = "<p>x</p>",
                CreatedAt = DateTime.UtcNow,
            });
        }
        context.Set<Article>().Add(article);
        context.SaveChanges();
        return article.Id;
    }

    private static ArticleInputDto BuildUpdateInput(int id, string slug, string lang = LanguageCodes.Turkish)
    {
        return new ArticleInputDto
        {
            Id = id,
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Translations = new List<ArticleTranslationInputDto>
            {
                new()
                {
                    LanguageCode = lang,
                    Title = "Test",
                    Slug = slug,
                    Content = "<p>x</p>"
                }
            },
            TagIds = new List<int>()
        };
    }

    [Fact]
    public async Task UpdateAsync_SlugChanged_RecordsSlugHistory()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            articleId = SeedPublishedArticle(seed, slug: "eski-slug");
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.UpdateAsync(BuildUpdateInput(articleId, slug: "yeni-slug"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var histories = await verify.SlugHistories.AsNoTracking().ToListAsync();
        histories.Should().HaveCount(1);
        histories[0].EntityType.Should().Be(SluggedEntityType.Article);
        histories[0].EntityId.Should().Be(articleId);
        histories[0].LanguageCode.Should().Be(LanguageCodes.Turkish);
        histories[0].OldSlug.Should().Be("eski-slug");
    }

    [Fact]
    public async Task UpdateAsync_SlugNotChanged_DoesNotRecordSlugHistory()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            articleId = SeedPublishedArticle(seed, slug: "ayni-slug");
        }

        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.UpdateAsync(BuildUpdateInput(articleId, slug: "ayni-slug"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var histories = await verify.SlugHistories.AsNoTracking().ToListAsync();
        histories.Should().BeEmpty("slug degismediginde SlugHistory satiri olusmamali");
    }

    [Fact]
    public async Task CreateAsync_NewArticle_DoesNotRecordSlugHistory()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new ArticleInputDto
        {
            Status = ArticleStatus.Draft,
            Translations = new List<ArticleTranslationInputDto>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Title = "Yeni Makale",
                    Slug = "yeni-makale",
                    Content = "<p>x</p>"
                }
            },
            TagIds = new List<int>()
        });

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var histories = await verify.SlugHistories.AsNoTracking().ToListAsync();
        histories.Should().BeEmpty("create akisinda eski slug yok - SlugHistory olusmamali");
    }

    /// <summary>
    /// Çoklu update: aynı article 2 kez slug değişimi → 2 SlugHistory satırı
    /// (history zinciri korunur, sadece son değişim değil).
    /// </summary>
    [Fact]
    public async Task UpdateAsync_TwoSequentialSlugChanges_RecordsTwoHistories()
    {
        int articleId;
        await using (var seed = _factory.CreateContext())
        {
            articleId = SeedPublishedArticle(seed, slug: "ilk-slug");
        }

        await using (var context = _factory.CreateContext())
        {
            var sut = CreateSut(context);
            (await sut.UpdateAsync(BuildUpdateInput(articleId, slug: "ikinci-slug")))
                .IsSuccess.Should().BeTrue();
        }

        await using (var context = _factory.CreateContext())
        {
            var sut = CreateSut(context);
            (await sut.UpdateAsync(BuildUpdateInput(articleId, slug: "ucuncu-slug")))
                .IsSuccess.Should().BeTrue();
        }

        await using var verify = _factory.CreateContext();
        var histories = await verify.SlugHistories
            .AsNoTracking()
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
        histories.Should().HaveCount(2, "iki ardisik slug degisikligi - iki history satiri");
        histories.Select(h => h.OldSlug).Should().ContainInOrder("ilk-slug", "ikinci-slug");
    }

    public void Dispose() => _factory.Dispose();
}
