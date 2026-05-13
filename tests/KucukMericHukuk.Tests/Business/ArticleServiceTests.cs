using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Article;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Identity;
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

public class ArticleServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<ArticleInputDto> _validator;

    public ArticleServiceTests()
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
        var sanitizer = new HtmlSanitizerService();
        return new ArticleService(uow, slugService, _mapper, _validator, sanitizer);
    }

    private static ArticleInputDto BuildValidInput(
        string title = "Hukuki Süreçler",
        string content = "<p>Makale içeriği burada yer alıyor.</p>",
        string? excerpt = null,
        string? slug = null,
        ArticleStatus status = ArticleStatus.Draft,
        DateTime? publishedAt = null,
        bool isFeatured = false,
        int? authorId = null,
        int? editorId = null,
        int? categoryId = null,
        int? id = null,
        List<int>? tagIds = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new ArticleInputDto
        {
            Id = id,
            AuthorId = authorId,
            EditorId = editorId,
            CategoryId = categoryId,
            Status = status,
            PublishedAt = publishedAt,
            IsFeatured = isFeatured,
            FeaturedImageUrl = null,
            TagIds = tagIds ?? new List<int>(),
            Translations = new List<ArticleTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Title = title,
                    Slug = slug ?? string.Empty,
                    Excerpt = excerpt,
                    Content = content,
                    MetaTitle = null,
                    MetaDescription = null,
                }
            }
        };
    }

    private static int SeedCategory(AppDbContext context, string name = "İcra Hukuku")
    {
        var cat = new Category
        {
            IsActive = true,
            DisplayOrder = 0,
            Translations = new List<CategoryTranslation>
            {
                new()
                {
                    LanguageCode = LanguageCodes.Turkish,
                    Name = name,
                    Slug = name.ToLowerInvariant().Replace(' ', '-'),
                    CreatedAt = DateTime.UtcNow,
                }
            },
            CreatedAt = DateTime.UtcNow,
        };
        context.Set<Category>().Add(cat);
        context.SaveChanges();
        return cat.Id;
    }

    private static int SeedUser(AppDbContext context, string emailPrefix = "author")
    {
        var user = new ApplicationUser
        {
            UserName = $"{emailPrefix}@test.com",
            NormalizedUserName = $"{emailPrefix.ToUpper()}@TEST.COM",
            Email = $"{emailPrefix}@test.com",
            NormalizedEmail = $"{emailPrefix.ToUpper()}@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };
        context.Set<ApplicationUser>().Add(user);
        context.SaveChanges();
        return user.Id;
    }

    private static List<int> SeedTags(AppDbContext context, params string[] names)
    {
        var ids = new List<int>();
        foreach (var name in names)
        {
            var tag = new Tag
            {
                IsActive = true,
                Translations = new List<TagTranslation>
                {
                    new()
                    {
                        LanguageCode = LanguageCodes.Turkish,
                        Name = name,
                        Slug = name.ToLowerInvariant().Replace(' ', '-'),
                        CreatedAt = DateTime.UtcNow,
                    }
                },
                CreatedAt = DateTime.UtcNow,
            };
            context.Set<Tag>().Add(tag);
            context.SaveChanges();
            ids.Add(tag.Id);
        }
        return ids;
    }

    // -------------------- CREATE --------------------

    [Fact]
    public async Task CreateAsync_ValidDraft_ShouldPersistDraftWithoutPublishedAt()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(status: ArticleStatus.Draft));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == result.Value);
        article.Status.Should().Be(ArticleStatus.Draft);
        article.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidPublished_ShouldAutoSetPublishedAt()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await sut.CreateAsync(BuildValidInput(status: ArticleStatus.Published));
        var after = DateTime.UtcNow.AddSeconds(1);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == result.Value);
        article.PublishedAt.Should().NotBeNull();
        article.PublishedAt!.Value.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public async Task CreateAsync_PublishedWithManualDate_ShouldKeepManualDate()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var manual = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var result = await sut.CreateAsync(BuildValidInput(
            status: ArticleStatus.Published,
            publishedAt: manual));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == result.Value);
        article.PublishedAt.Should().Be(manual);
    }

    [Fact]
    public async Task CreateAsync_NoTranslationFilled_ShouldFailWithTranslationRule()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations[0].Title = string.Empty;
        input.Translations[0].Content = string.Empty;

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Field == nameof(ArticleInputDto.Translations) ||
            (e.Message != null && e.Message.Contains("başlık", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task CreateAsync_TitleWithoutContent_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations[0].Title = "Sadece başlık var";
        input.Translations[0].Content = string.Empty;

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e =>
            e.Field != null && e.Field.Contains("Content"));
    }

    [Fact]
    public async Task CreateAsync_WithTags_ShouldAttachAllTags()
    {
        await using var context = _factory.CreateContext();
        var tagIds = SeedTags(context, "Etiket1", "Etiket2", "Etiket3");

        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(tagIds: tagIds));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>()
            .Include(a => a.Tags)
            .First(a => a.Id == result.Value);
        article.Tags.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_NonexistentCategory_ShouldFailWithCategoryNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(categoryId: 9999));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Article.CategoryNotFound);
    }

    [Fact]
    public async Task CreateAsync_ValidCategory_ShouldAttach()
    {
        await using var context = _factory.CreateContext();
        var catId = SeedCategory(context);
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(categoryId: catId));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == result.Value);
        article.CategoryId.Should().Be(catId);
    }

    [Fact]
    public async Task CreateAsync_AuthorIdProvided_ShouldPersistOnEntity()
    {
        await using var context = _factory.CreateContext();
        var userId = SeedUser(context);
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(authorId: userId));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().IgnoreQueryFilters().First(a => a.Id == result.Value);
        article.AuthorId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateAsync_AutoExcerpt_ShouldExtractFromContent()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var longContent = "<p>" + string.Join(" ", Enumerable.Repeat("Hukuki", 50)) + "</p>";
        var result = await sut.CreateAsync(BuildValidInput(content: longContent, excerpt: null));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ArticleTranslation>().First(t => t.ArticleId == result.Value);
        translation.Excerpt.Should().NotBeNull();
        translation.Excerpt!.Length.Should().BeLessThanOrEqualTo(170); // 160 + ellipsis trim
        translation.Excerpt.Should().NotContain("<");
    }

    [Fact]
    public async Task CreateAsync_ManualExcerpt_ShouldKeepIt()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(excerpt: "Manuel kısa açıklama."));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ArticleTranslation>().First(t => t.ArticleId == result.Value);
        translation.Excerpt.Should().Be("Manuel kısa açıklama.");
    }

    [Fact]
    public async Task CreateAsync_ReadingTime_ShouldBeCalculatedFromWordCount()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        // 400 kelime → 200 wpm → 2 dakika
        var content = "<p>" + string.Join(" ", Enumerable.Repeat("kelime", 400)) + "</p>";
        var result = await sut.CreateAsync(BuildValidInput(content: content));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ArticleTranslation>().First(t => t.ArticleId == result.Value);
        translation.ReadingTimeMinutes.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_ContentWithScript_ShouldSanitize()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations[0].Content = "<p>OK</p><script>alert('xss')</script>";

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<ArticleTranslation>().First(t => t.ArticleId == result.Value);
        translation.Content.Should().Contain("<p>OK</p>");
        translation.Content.Should().NotContain("script");
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_DraftToPublished_ShouldSetPublishedAt()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(status: ArticleStatus.Draft));

        var updateInput = BuildValidInput(
            id: created.Value,
            status: ArticleStatus.Published);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == created.Value);
        article.Status.Should().Be(ArticleStatus.Published);
        article.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_PublishedToDraft_ShouldKeepPublishedAt()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var manual = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = await sut.CreateAsync(BuildValidInput(
            status: ArticleStatus.Published, publishedAt: manual));

        var updateInput = BuildValidInput(
            id: created.Value,
            status: ArticleStatus.Draft,
            publishedAt: null);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == created.Value);
        article.Status.Should().Be(ArticleStatus.Draft);
        article.PublishedAt.Should().Be(manual);
    }

    [Fact]
    public async Task UpdateAsync_TagsReplaced_ShouldReflectNewSet()
    {
        await using var context = _factory.CreateContext();
        var initial = SeedTags(context, "İlk1", "İlk2");
        var replacement = SeedTags(context, "Yeni1", "Yeni2");
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(tagIds: initial));

        var updateInput = BuildValidInput(
            id: created.Value,
            status: ArticleStatus.Draft,
            tagIds: replacement);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>()
            .Include(a => a.Tags)
            .First(a => a.Id == created.Value);
        article.Tags.Select(t => t.Id).Should().BeEquivalentTo(replacement);
    }

    [Fact]
    public async Task UpdateAsync_NoId_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Id = null;

        var result = await sut.UpdateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Common.Validation);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(id: 9999);
        var result = await sut.UpdateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Article.NotFound);
    }

    // -------------------- DELETE / RESTORE --------------------

    [Fact]
    public async Task DeleteAsync_SoftDelete_ShouldSetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().IgnoreQueryFilters().First(a => a.Id == created.Value);
        article.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DeletedArticle_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var article = verify.Set<Article>().First(a => a.Id == created.Value);
        article.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_DeletedArticle_ShouldRemoveFromDb()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Article>().IgnoreQueryFilters().Any(a => a.Id == created.Value);
        exists.Should().BeFalse();
    }

    // -------------------- AUTHOR FILTER (Faz 6.10) --------------------

    [Fact]
    public async Task GetPagedAsync_AdminNoAuthorFilter_ShouldReturnAllArticles()
    {
        await using var context = _factory.CreateContext();
        var editorId = SeedUser(context, "editor");
        var otherId = SeedUser(context, "other");
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(title: "Editor Makalesi 1", authorId: editorId));
        await sut.CreateAsync(BuildValidInput(title: "Editor Makalesi 2", authorId: editorId));
        await sut.CreateAsync(BuildValidInput(title: "Diger Makale", authorId: otherId));

        var query = new ArticleQueryDto
        {
            LanguageCode = LanguageCodes.Turkish,
            Page = 1,
            PageSize = 50,
            AuthorIdFilter = null,
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_AuthorFilter_ShouldReturnOnlyOwnArticles()
    {
        await using var context = _factory.CreateContext();
        var editorId = SeedUser(context, "editor");
        var otherId = SeedUser(context, "other");
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(title: "Editor Makalesi 1", authorId: editorId));
        await sut.CreateAsync(BuildValidInput(title: "Editor Makalesi 2", authorId: editorId));
        await sut.CreateAsync(BuildValidInput(title: "Diger Makale", authorId: otherId));

        var query = new ArticleQueryDto
        {
            LanguageCode = LanguageCodes.Turkish,
            Page = 1,
            PageSize = 50,
            AuthorIdFilter = editorId,
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(a => a.AuthorId == editorId);
    }

    [Fact]
    public async Task GetPagedAsync_AuthorFilterMatchingNobody_ShouldReturnEmpty()
    {
        await using var context = _factory.CreateContext();
        var editorId = SeedUser(context, "editor");
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(title: "Editor Makalesi", authorId: editorId));

        var query = new ArticleQueryDto
        {
            LanguageCode = LanguageCodes.Turkish,
            Page = 1,
            PageSize = 50,
            AuthorIdFilter = 999_999,
        };

        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    // -------------------- EDITOR ID (Faz 6.10a) --------------------

    [Fact]
    public async Task CreateAsync_WithEditorId_PersistsToDb()
    {
        await using var context = _factory.CreateContext();
        var authorId = SeedUser(context, "author");
        var editorId = SeedUser(context, "editor");
        var sut = CreateSut(context);

        var input = BuildValidInput(authorId: authorId, editorId: editorId);
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var stored = await verify.Set<Article>().FirstAsync(a => a.Id == result.Value);
        stored.AuthorId.Should().Be(authorId);
        stored.EditorId.Should().Be(editorId);
    }

    [Fact]
    public async Task CreateAsync_NoEditorId_PersistsAsNull()
    {
        await using var context = _factory.CreateContext();
        var authorId = SeedUser(context, "author");
        var sut = CreateSut(context);

        var input = BuildValidInput(authorId: authorId);
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var stored = await verify.Set<Article>().FirstAsync(a => a.Id == result.Value);
        stored.EditorId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ChangesEditorId_PersistsNewValue()
    {
        await using var context = _factory.CreateContext();
        var authorId = SeedUser(context, "author");
        var editor1Id = SeedUser(context, "editor1");
        var editor2Id = SeedUser(context, "editor2");
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(authorId: authorId, editorId: editor1Id));

        var get = await sut.GetByIdAsync(created.Value);
        var input = BuildValidInput(
            id: created.Value,
            title: get.Value.Translations[0].Title,
            slug: get.Value.Translations[0].Slug,
            authorId: authorId,
            editorId: editor2Id);

        var result = await sut.UpdateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var stored = await verify.Set<Article>().FirstAsync(a => a.Id == created.Value);
        stored.EditorId.Should().Be(editor2Id);
    }

    [Fact]
    public async Task UpdateAsync_ClearEditorId_PersistsNull()
    {
        await using var context = _factory.CreateContext();
        var authorId = SeedUser(context, "author");
        var editorId = SeedUser(context, "editor");
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(authorId: authorId, editorId: editorId));

        var get = await sut.GetByIdAsync(created.Value);
        var input = BuildValidInput(
            id: created.Value,
            title: get.Value.Translations[0].Title,
            slug: get.Value.Translations[0].Slug,
            authorId: authorId,
            editorId: null);

        var result = await sut.UpdateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var stored = await verify.Set<Article>().FirstAsync(a => a.Id == created.Value);
        stored.EditorId.Should().BeNull();
    }

    public void Dispose() => _factory.Dispose();
}
