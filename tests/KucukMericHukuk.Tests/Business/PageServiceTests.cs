using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Page;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Infrastructure.Security;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class PageServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<PageInputDto> _validator;

    public PageServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(PageMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new PageInputValidator();
    }

    private PageService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var sanitizer = new HtmlSanitizerService();
        return new PageService(uow, slugService, _mapper, _validator, sanitizer);
    }

    private static PageInputDto BuildValidInput(
        string pageKey = "hakkimizda",
        string title = "Hakkımızda",
        string? slug = null,
        bool isActive = true,
        int displayOrder = 0,
        int? id = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new PageInputDto
        {
            Id = id,
            PageKey = pageKey,
            IsSystem = false,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            Translations = new List<PageTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Title = title,
                    Slug = slug ?? string.Empty,
                    Content = "<p>İçerik</p>",
                    MetaTitle = "Meta",
                    MetaDescription = "Meta açıklama"
                }
            }
        };
    }

    // -------------------- CREATE --------------------

    [Fact]
    public async Task CreateAsync_ValidInput_ShouldReturnNewId()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePageKey_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput("hakkimizda"));
        var result = await sut.CreateAsync(BuildValidInput("hakkimizda", title: "Tekrar"));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Page.PageKeyExists);
    }

    [Fact]
    public async Task CreateAsync_SlugBos_OtomatikUretilir()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(
            BuildValidInput(pageKey: "icra", title: "İcra Hukuku", slug: null));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<PageTranslation>()
            .First(t => t.PageId == result.Value);
        translation.Slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task CreateAsync_SlugManuel_ValidFormat_ShouldPersistAsIs()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(pageKey: "manuel", title: "Manuel Sayfa");
        input.Translations[0].Slug = "manuel-slug";

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<PageTranslation>()
            .First(t => t.PageId == result.Value);
        translation.Slug.Should().Be("manuel-slug");
    }

    [Fact]
    public async Task CreateAsync_SlugManuel_InvalidFormat_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(pageKey: "manuel-bad", title: "Manuel Sayfa");
        input.Translations[0].Slug = "Manuel SLUG";

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field != null && e.Field.Contains("Slug"));
    }

    [Fact]
    public async Task CreateAsync_PageKeyRegexInvalid_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(pageKey: "Hakkımızda BÜYÜK"));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(PageInputDto.PageKey));
    }

    [Fact]
    public async Task CreateAsync_NoTranslations_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput();
        input.Translations.Clear();

        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(PageInputDto.Translations));
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_ValidInput_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var createResult = await sut.CreateAsync(BuildValidInput());
        var updateInput = BuildValidInput(
            pageKey: "hakkimizda",
            title: "Hakkımızda Güncel",
            id: createResult.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_PreservesTranslationIdAndCreatedAt()
    {
        // Faz 7.1.2: diff-based merge mevcut translation Id ve CreatedAt'i korumalı.
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var createResult = await sut.CreateAsync(BuildValidInput());
        var originalId = context.Set<PageTranslation>().First(t => t.PageId == createResult.Value).Id;
        var originalCreatedAt = context.Set<PageTranslation>().First(t => t.PageId == createResult.Value).CreatedAt;

        var updateInput = BuildValidInput(pageKey: "hakkimizda", title: "Yeni Başlık", id: createResult.Value);
        var result = await sut.UpdateAsync(updateInput);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var t = verify.Set<PageTranslation>().First(x => x.PageId == createResult.Value);
        t.Id.Should().Be(originalId, "Translation Id KORUNMALI (diff-merge)");
        t.CreatedAt.Should().Be(originalCreatedAt);
        t.Title.Should().Be("Yeni Başlık");
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
        result.FirstError!.Code.Should().Be(ErrorCodes.Page.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_DuplicatePageKey_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(pageKey: "a", title: "A"));
        var b = await sut.CreateAsync(BuildValidInput(pageKey: "b", title: "B"));

        var input = BuildValidInput(pageKey: "a", title: "B yenilendi", id: b.Value);
        var result = await sut.UpdateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Page.PageKeyExists);
    }

    [Fact]
    public async Task UpdateAsync_SamePageKey_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(pageKey: "hakkimizda", title: "Eski"));

        var input = BuildValidInput(pageKey: "hakkimizda", title: "Yeni", id: created.Value);
        var result = await sut.UpdateAsync(input);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------- DELETE / RESTORE / HARD DELETE --------------------

    [Fact]
    public async Task DeleteAsync_SoftDelete_IsDeletedTrue()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var page = verify.Set<Page>().IgnoreQueryFilters().First(p => p.Id == created.Value);
        page.IsDeleted.Should().BeTrue();
        page.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_DeletedPage_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var page = verify.Set<Page>().First(p => p.Id == created.Value);
        page.IsDeleted.Should().BeFalse();
        page.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_DeletedPage_ShouldRemoveFromDb()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Page>().IgnoreQueryFilters().Any(p => p.Id == created.Value);
        exists.Should().BeFalse();
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByPageKeyAsync_ActivePage_ShouldReturn()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(pageKey: "active", title: "Aktif", isActive: true));

        var result = await sut.GetByPageKeyAsync("active", LanguageCodes.Turkish);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageKey.Should().Be("active");
        result.Value.Title.Should().Be("Aktif");
    }

    [Fact]
    public async Task GetByPageKeyAsync_InactivePage_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(pageKey: "inactive", title: "Pasif", isActive: false));

        var result = await sut.GetByPageKeyAsync("inactive", LanguageCodes.Turkish);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Page.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ContentWithScript_ShouldSanitize()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(pageKey: "xss-test");
        input.Translations[0].Content = "<p>OK</p><script>alert('xss')</script>";

        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<PageTranslation>()
            .First(t => t.PageId == result.Value);
        translation.Content.Should().Contain("<p>OK</p>");
        translation.Content.Should().NotContain("script");
        translation.Content.Should().NotContain("alert");
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter_ShouldFilterByTitle()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(pageKey: "alfa", title: "Alfa Sayfası"));
        await sut.CreateAsync(BuildValidInput(pageKey: "beta", title: "Beta Sayfası"));
        await sut.CreateAsync(BuildValidInput(pageKey: "gama", title: "Gama Sayfası"));

        var query = new PageQueryDto { Keyword = "Alfa", LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].PageKey.Should().Be("alfa");
    }

    public void Dispose() => _factory.Dispose();
}
