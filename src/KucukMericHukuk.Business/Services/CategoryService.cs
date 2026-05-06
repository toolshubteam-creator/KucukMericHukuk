using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Category;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly IMapper _mapper;
    private readonly IValidator<CategoryInputDto> _validator;

    public CategoryService(
        IUnitOfWork uow,
        ISlugService slugService,
        IMapper mapper,
        IValidator<CategoryInputDto> validator)
    {
        _uow = uow;
        _slugService = slugService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Result<CategoryListDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetBySlugAsync(languageCode, slug, ct);
        if (category is null || !category.IsActive)
        {
            return Result.Failure<CategoryListDto>(
                new Error(ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        return Result.Success(_mapper.Map<CategoryListDto>(category));
    }

    public async Task<Result<CategoryAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdWithTranslationsAsync(id, ct);
        if (category is null)
        {
            return Result.Failure<CategoryAdminDto>(
                new Error(ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        return Result.Success(_mapper.Map<CategoryAdminDto>(category));
    }

    public async Task<Result<PagedResult<CategoryAdminDto>>> GetPagedAsync(
        CategoryQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Categories.GetAdminPagedAsync(
            query.Keyword, query.LanguageCode,
            query.ParentCategoryId,
            query.Page, query.PageSize,
            query.IncludeDeleted, ct);

        var mapped = paged.Items.Select(c => _mapper.Map<CategoryAdminDto>(c)).ToList();
        var result = new PagedResult<CategoryAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(
        CategoryInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        if (input.ParentCategoryId.HasValue)
        {
            var parent = await _uow.Categories.GetByIdAsync(input.ParentCategoryId.Value, ct);
            if (parent is null)
            {
                return Result.Failure<int>(new Error(
                    ErrorCodes.Category.ParentNotFound,
                    "Seçilen üst kategori bulunamadı.",
                    field: nameof(input.ParentCategoryId)));
            }
        }

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: null, ct);
        if (slugResult.IsFailure)
            return Result.Failure<int>(slugResult.Errors);

        var category = new Category
        {
            ParentCategoryId = input.ParentCategoryId,
            IsActive = input.IsActive,
            DisplayOrder = input.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in input.Translations)
        {
            category.Translations.Add(new CategoryTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _uow.Categories.AddAsync(category, ct);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success(category.Id);
    }

    public async Task<Result> UpdateAsync(
        CategoryInputDto input, CancellationToken ct = default)
    {
        if (!input.Id.HasValue)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation,
                "Güncelleme için ID gerekli.",
                field: nameof(input.Id)));
        }

        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        var category = await _uow.Categories.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (category is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        if (input.ParentCategoryId.HasValue)
        {
            var parent = await _uow.Categories.GetByIdAsync(input.ParentCategoryId.Value, ct);
            if (parent is null)
            {
                return Result.Failure(new Error(
                    ErrorCodes.Category.ParentNotFound,
                    "Seçilen üst kategori bulunamadı.",
                    field: nameof(input.ParentCategoryId)));
            }

            // Circular parent: yeni parent, mevcut kategorinin descendant'ı olamaz
            var descendants = await _uow.Categories.GetDescendantIdsAsync(input.Id.Value, ct);
            if (descendants.Contains(input.ParentCategoryId.Value))
            {
                return Result.Failure(new Error(
                    ErrorCodes.Category.CircularParent,
                    "Üst kategori, bu kategorinin alt kategorisi olamaz (döngü oluşur).",
                    field: nameof(input.ParentCategoryId)));
            }
        }

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: input.Id.Value, ct);
        if (slugResult.IsFailure)
            return slugResult;

        category.ParentCategoryId = input.ParentCategoryId;
        category.IsActive = input.IsActive;
        category.DisplayOrder = input.DisplayOrder;
        category.UpdatedAt = DateTime.UtcNow;

        category.Translations.Clear();
        foreach (var t in input.Translations)
        {
            category.Translations.Add(new CategoryTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, ct);
        if (category is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        var hasChildren = await _uow.Categories.HasChildrenAsync(id, ct);
        if (hasChildren)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.HasChildren,
                "Bu kategorinin alt kategorileri var. Önce alt kategorileri silin veya başka bir üst kategoriye taşıyın."));
        }

        _uow.Categories.Delete(category);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdIncludingDeletedAsync(id, ct);
        if (category is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        if (!category.IsDeleted) return Result.Success();

        // NOT: Parent IsDeleted ise orphan parent referansı kalır.
        // 2.10'da değerlendirilecek (DEFERRED).

        _uow.Categories.Restore(category);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdIncludingDeletedAsync(id, ct);
        if (category is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.NotFound, "Kategori bulunamadı."));
        }

        var hasChildren = await _uow.Categories.HasChildrenAsync(id, ct);
        if (hasChildren)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.HasChildren,
                "Bu kategorinin alt kategorileri var. Kalıcı silme için önce tüm alt kategorileri kalıcı olarak silin."));
        }

        _uow.Categories.HardDelete(category);

        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Result.Failure(new Error(
                ErrorCodes.Category.HasChildren,
                "Bu kategoriye bağlı alt kayıtlar var. Önce onları temizleyin."));
        }

        return Result.Success();
    }

    private async Task<Result> ResolveSlugsAsync(
        List<CategoryTranslationInputDto> translations,
        int? excludeId,
        CancellationToken ct)
    {
        var errors = new List<Error>();

        for (var i = 0; i < translations.Count; i++)
        {
            var t = translations[i];
            try
            {
                t.Slug = string.IsNullOrWhiteSpace(t.Slug)
                    ? await _slugService.GenerateUniqueAsync(
                        t.Name, t.LanguageCode, SluggedEntityType.Category,
                        excludeId, ct)
                    : await _slugService.EnsureUniqueAsync(
                        t.Slug, t.LanguageCode, SluggedEntityType.Category,
                        excludeId, ct);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Category.SlugExists, ex.Message,
                    field: $"Translations[{i}].Slug"));
            }
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

}
