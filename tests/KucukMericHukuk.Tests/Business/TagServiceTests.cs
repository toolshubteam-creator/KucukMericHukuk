using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Tag;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class TagServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<TagInputDto> _validator;

    public TagServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(TagMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new TagInputValidator();
    }

    private TagService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        var slugHistoryService = new SlugHistoryService(uow);
        return new TagService(uow, slugService, slugHistoryService, _mapper, _validator);
    }

    private static TagInputDto BuildValidInput(
        string name = "Hukuk",
        string? slug = null,
        bool isActive = true,
        int? id = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new TagInputDto
        {
            Id = id,
            IsActive = isActive,
            Translations = new List<TagTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Name = name,
                    Slug = slug ?? string.Empty
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
    public async Task CreateAsync_SlugBos_OtomatikUretilir()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(name: "İcra Hukuku"));

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<TagTranslation>().First(t => t.TagId == result.Value);
        translation.Slug.Should().Be("icra-hukuku");
    }

    [Fact]
    public async Task CreateAsync_SlugManuel_ValidFormat_ShouldPersistAsIs()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: "Manuel", slug: "manuel-tag");
        var result = await sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<TagTranslation>().First(t => t.TagId == result.Value);
        translation.Slug.Should().Be("manuel-tag");
    }

    [Fact]
    public async Task CreateAsync_SlugManuel_InvalidFormat_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: "Bad", slug: "BUYUK SLUG");
        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field != null && e.Field.Contains("Slug"));
    }

    [Fact]
    public async Task CreateAsync_NameTooLong_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: new string('a', 51));
        var result = await sut.CreateAsync(input);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field != null && e.Field.Contains("Name"));
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
        result.Errors.Should().Contain(e => e.Field == nameof(TagInputDto.Translations));
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_ValidInput_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Eski"));
        var updateInput = BuildValidInput(name: "Yeni", id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_PreservesTranslationIdAndCreatedAt()
    {
        // Faz 7.1.2: diff-based merge mevcut translation Id ve CreatedAt'i korumalı.
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Eski Etiket"));
        var originalId = context.Set<TagTranslation>().First(t => t.TagId == created.Value).Id;
        var originalCreatedAt = context.Set<TagTranslation>().First(t => t.TagId == created.Value).CreatedAt;

        var updateInput = BuildValidInput(name: "Yeni Etiket", id: created.Value);
        var result = await sut.UpdateAsync(updateInput);
        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var t = verify.Set<TagTranslation>().First(x => x.TagId == created.Value);
        t.Id.Should().Be(originalId, "Translation Id KORUNMALI (diff-merge)");
        t.CreatedAt.Should().Be(originalCreatedAt);
        t.Name.Should().Be("Yeni Etiket");
    }

    /// <summary>Faz 7.4.3a — Tag slug-change SlugHistory satırı üretmeli.</summary>
    [Fact]
    public async Task UpdateAsync_SlugChanged_RecordsSlugHistory()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Hukuk", slug: "hukuk"));
        var updateInput = BuildValidInput(name: "Hukuk", slug: "hukuk-yeni", id: created.Value);
        (await sut.UpdateAsync(updateInput)).IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var histories = await verify.SlugHistories.AsNoTracking().ToListAsync();
        histories.Should().HaveCount(1);
        histories[0].EntityType.Should().Be(SluggedEntityType.Tag);
        histories[0].EntityId.Should().Be(created.Value);
        histories[0].OldSlug.Should().Be("hukuk");
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
        result.FirstError!.Code.Should().Be(ErrorCodes.Tag.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_SlugExclude_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Hukuk", slug: "hukuk"));
        var updateInput = BuildValidInput(name: "Hukuk Guncel", slug: "hukuk", id: created.Value);

        var result = await sut.UpdateAsync(updateInput);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var translation = verify.Set<TagTranslation>().First(t => t.TagId == created.Value);
        translation.Slug.Should().Be("hukuk");
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
        var tag = verify.Set<Tag>().IgnoreQueryFilters().First(t => t.Id == created.Value);
        tag.IsDeleted.Should().BeTrue();
        tag.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_DeletedTag_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var tag = verify.Set<Tag>().First(t => t.Id == created.Value);
        tag.IsDeleted.Should().BeFalse();
        tag.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_DeletedTag_ShouldRemoveFromDb()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);

        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Tag>().IgnoreQueryFilters().Any(t => t.Id == created.Value);
        exists.Should().BeFalse();
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Tag.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_InactiveTag_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var input = BuildValidInput(name: "Pasif", slug: "pasif", isActive: false);
        await sut.CreateAsync(input);

        var result = await sut.GetBySlugAsync("pasif", LanguageCodes.Turkish);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Tag.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter_ShouldFilterByName()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(name: "Alfa"));
        await sut.CreateAsync(BuildValidInput(name: "Beta"));
        await sut.CreateAsync(BuildValidInput(name: "Gama"));

        var query = new TagQueryDto { Keyword = "Alfa", LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Translations.Should().HaveCount(1);
        result.Value.Items[0].Translations[0].Name.Should().Be("Alfa");
    }

    public void Dispose() => _factory.Dispose();
}
