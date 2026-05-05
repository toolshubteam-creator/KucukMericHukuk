using FluentAssertions;
using FluentValidation;
using KucukMericHukuk.Business.Mappings;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Business.Validators;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Constants;
using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.UnitOfWork;
using KucukMericHukuk.Tests.Infrastructure;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Tests.Business;

public class CategoryServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMapper _mapper;
    private readonly IValidator<CategoryInputDto> _validator;

    public CategoryServiceTests()
    {
        _factory = new TestDbContextFactory();

        var config = new TypeAdapterConfig();
        config.Scan(typeof(CategoryMappingConfig).Assembly);
        _mapper = new Mapper(config);

        _validator = new CategoryInputValidator();
    }

    private CategoryService CreateSut(AppDbContext context)
    {
        var uow = new UnitOfWork(context);
        var slugService = new SlugService(uow);
        return new CategoryService(uow, slugService, _mapper, _validator);
    }

    private static CategoryInputDto BuildValidInput(
        string name = "Hukuk",
        string? slug = null,
        bool isActive = true,
        int displayOrder = 0,
        int? id = null,
        int? parentCategoryId = null,
        string languageCode = LanguageCodes.Turkish)
    {
        return new CategoryInputDto
        {
            Id = id,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            ParentCategoryId = parentCategoryId,
            Translations = new List<CategoryTranslationInputDto>
            {
                new()
                {
                    LanguageCode = languageCode,
                    Name = name,
                    Slug = slug ?? string.Empty,
                    Description = "Açıklama"
                }
            }
        };
    }

    /// <summary>
    /// Hiyerarşi seed: Cat1 (root) → Cat2 → Cat3 → Cat4 ; Cat5 (root, bağımsız).
    /// </summary>
    private async Task<Dictionary<string, int>> SeedHierarchyAsync(CategoryService sut)
    {
        var ids = new Dictionary<string, int>();
        var c1 = await sut.CreateAsync(BuildValidInput(name: "Root1", slug: "root1"));
        ids["Cat1"] = c1.Value;
        var c2 = await sut.CreateAsync(BuildValidInput(name: "Sub2", slug: "sub2", parentCategoryId: c1.Value));
        ids["Cat2"] = c2.Value;
        var c3 = await sut.CreateAsync(BuildValidInput(name: "Sub3", slug: "sub3", parentCategoryId: c2.Value));
        ids["Cat3"] = c3.Value;
        var c4 = await sut.CreateAsync(BuildValidInput(name: "Sub4", slug: "sub4", parentCategoryId: c3.Value));
        ids["Cat4"] = c4.Value;
        var c5 = await sut.CreateAsync(BuildValidInput(name: "Root5", slug: "root5"));
        ids["Cat5"] = c5.Value;
        return ids;
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
        var translation = verify.Set<CategoryTranslation>()
            .First(t => t.CategoryId == result.Value);
        translation.Slug.Should().Be("icra-hukuku");
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
        result.Errors.Should().Contain(e => e.Field == nameof(CategoryInputDto.Translations));
    }

    [Fact]
    public async Task CreateAsync_WithValidParent_ShouldLink()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var parent = await sut.CreateAsync(BuildValidInput(name: "Ana", slug: "ana"));
        var child = await sut.CreateAsync(BuildValidInput(name: "Alt", slug: "alt", parentCategoryId: parent.Value));

        child.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var dbChild = verify.Set<Category>().First(c => c.Id == child.Value);
        dbChild.ParentCategoryId.Should().Be(parent.Value);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidParent_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(BuildValidInput(parentCategoryId: 9999));

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.ParentNotFound);
    }

    // -------------------- UPDATE --------------------

    [Fact]
    public async Task UpdateAsync_ValidInput_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput(name: "Eski"));
        var update = BuildValidInput(name: "Yeni", id: created.Value);

        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();
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
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ChangeParent_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var p1 = await sut.CreateAsync(BuildValidInput(name: "P1", slug: "p1"));
        var p2 = await sut.CreateAsync(BuildValidInput(name: "P2", slug: "p2"));
        var child = await sut.CreateAsync(BuildValidInput(name: "C", slug: "c", parentCategoryId: p1.Value));

        var update = BuildValidInput(name: "C", slug: "c", id: child.Value, parentCategoryId: p2.Value);
        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------- HIERARCHY GUARDS --------------------

    [Fact]
    public async Task UpdateAsync_SelfParent_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var update = BuildValidInput(id: created.Value, parentCategoryId: created.Value);

        var result = await sut.UpdateAsync(update);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Field == nameof(CategoryInputDto.ParentCategoryId));
    }

    [Fact]
    public async Task UpdateAsync_CircularParent_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var ids = await SeedHierarchyAsync(sut);

        // Cat1 → Cat2 → Cat3 hiyerarşisinde, Cat1.Parent = Cat3 yapma denemesi
        // Cat1.descendants = [Cat2, Cat3, Cat4]; Cat3 burada → CircularParent.
        var update = BuildValidInput(name: "Root1", slug: "root1", id: ids["Cat1"], parentCategoryId: ids["Cat3"]);
        var result = await sut.UpdateAsync(update);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.CircularParent);
    }

    [Fact]
    public async Task UpdateAsync_DeepCircular_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var ids = await SeedHierarchyAsync(sut);

        // Cat1.Parent = Cat4 (4 seviye derinde); Cat1.descendants Cat4'ü içerir
        var update = BuildValidInput(name: "Root1", slug: "root1", id: ids["Cat1"], parentCategoryId: ids["Cat4"]);
        var result = await sut.UpdateAsync(update);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.CircularParent);
    }

    [Fact]
    public async Task UpdateAsync_NonCircular_ShouldSucceed()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var ids = await SeedHierarchyAsync(sut);

        // Cat3.Parent = Cat5 (Cat5, Cat3'ün descendant'ı değil) — geçerli
        var update = BuildValidInput(name: "Sub3", slug: "sub3", id: ids["Cat3"], parentCategoryId: ids["Cat5"]);
        var result = await sut.UpdateAsync(update);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------- DELETE --------------------

    [Fact]
    public async Task DeleteAsync_NoChildren_ShouldSoftDelete()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        var result = await sut.DeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var cat = verify.Set<Category>().IgnoreQueryFilters().First(c => c.Id == created.Value);
        cat.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_HasChildren_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var parent = await sut.CreateAsync(BuildValidInput(name: "P", slug: "p"));
        await sut.CreateAsync(BuildValidInput(name: "C", slug: "c", parentCategoryId: parent.Value));

        var result = await sut.DeleteAsync(parent.Value);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.HasChildren);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.NotFound);
    }

    [Fact]
    public async Task RestoreAsync_DeletedCategory_ShouldUnsetIsDeleted()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);
        var result = await sut.RestoreAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var cat = verify.Set<Category>().First(c => c.Id == created.Value);
        cat.IsDeleted.Should().BeFalse();
    }

    // -------------------- HARD DELETE --------------------

    [Fact]
    public async Task HardDeleteAsync_NoChildren_ShouldRemoveFromDb()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var created = await sut.CreateAsync(BuildValidInput());
        await sut.DeleteAsync(created.Value);
        var result = await sut.HardDeleteAsync(created.Value);

        result.IsSuccess.Should().BeTrue();

        await using var verify = _factory.CreateContext();
        var exists = verify.Set<Category>().IgnoreQueryFilters().Any(c => c.Id == created.Value);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_HasChildren_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var parent = await sut.CreateAsync(BuildValidInput(name: "P", slug: "p"));
        await sut.CreateAsync(BuildValidInput(name: "C", slug: "c", parentCategoryId: parent.Value));

        var result = await sut.HardDeleteAsync(parent.Value);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.HasChildren);
    }

    [Fact]
    public async Task HardDeleteAsync_NotFound_ShouldFail()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.HardDeleteAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.NotFound);
    }

    // -------------------- GET --------------------

    [Fact]
    public async Task GetByIdAsync_NotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(9999);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_InactiveCategory_ShouldReturnNotFound()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        await sut.CreateAsync(BuildValidInput(name: "Pasif", slug: "pasif", isActive: false));

        var result = await sut.GetBySlugAsync("pasif", LanguageCodes.Turkish);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Code.Should().Be(ErrorCodes.Category.NotFound);
    }

    [Fact]
    public async Task GetPagedAsync_KeywordFilter()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);
        await sut.CreateAsync(BuildValidInput(name: "Aile", slug: "aile"));
        await sut.CreateAsync(BuildValidInput(name: "Ceza", slug: "ceza"));
        await sut.CreateAsync(BuildValidInput(name: "Borc", slug: "borc"));

        var query = new CategoryQueryDto { Keyword = "Aile", LanguageCode = LanguageCodes.Turkish };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Translations[0].Name.Should().Be("Aile");
    }

    [Fact]
    public async Task GetPagedAsync_ParentCategoryFilter()
    {
        await using var context = _factory.CreateContext();
        var sut = CreateSut(context);

        var p = await sut.CreateAsync(BuildValidInput(name: "P", slug: "p"));
        await sut.CreateAsync(BuildValidInput(name: "C1", slug: "c1", parentCategoryId: p.Value));
        await sut.CreateAsync(BuildValidInput(name: "C2", slug: "c2", parentCategoryId: p.Value));
        await sut.CreateAsync(BuildValidInput(name: "Other", slug: "other"));

        var query = new CategoryQueryDto
        {
            ParentCategoryId = p.Value,
            LanguageCode = LanguageCodes.Turkish
        };
        var result = await sut.GetPagedAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    public void Dispose() => _factory.Dispose();
}
