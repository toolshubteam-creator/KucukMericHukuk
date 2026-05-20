using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Business.Helpers;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Page;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class PageService : IPageService
{
    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly ISlugHistoryService _slugHistory;
    private readonly IMapper _mapper;
    private readonly IValidator<PageInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public PageService(
        IUnitOfWork uow,
        ISlugService slugService,
        ISlugHistoryService slugHistory,
        IMapper mapper,
        IValidator<PageInputDto> validator,
        IHtmlSanitizerService sanitizer)
    {
        _uow = uow;
        _slugService = slugService;
        _slugHistory = slugHistory;
        _mapper = mapper;
        _validator = validator;
        _sanitizer = sanitizer;
    }

    public async Task<Result<PageDetailDto>> GetByPageKeyAsync(
        string pageKey,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetByPageKeyAsync(pageKey, languageCode, cancellationToken);
        if (page is null || !page.IsActive)
        {
            return Result.Failure<PageDetailDto>(
                new Error(ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        return Result.Success(_mapper.Map<PageDetailDto>(page));
    }

    public async Task<Result<PageDetailDto>> GetBySlugAsync(
        string slug,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetBySlugAsync(languageCode, slug, cancellationToken);
        if (page is null || !page.IsActive)
        {
            return Result.Failure<PageDetailDto>(
                new Error(ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        return Result.Success(_mapper.Map<PageDetailDto>(page));
    }

    public async Task<Result<PageAdminDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetByIdWithTranslationsAsync(id, cancellationToken);
        if (page is null)
        {
            return Result.Failure<PageAdminDto>(
                new Error(ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        return Result.Success(_mapper.Map<PageAdminDto>(page));
    }

    public async Task<Result<PagedResult<PageAdminDto>>> GetPagedAsync(
        PageQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var paged = await _uow.Pages.GetAdminPagedAsync(
            keyword: query.Keyword,
            languageCode: query.LanguageCode,
            page: query.Page,
            pageSize: query.PageSize,
            includeDeleted: query.IncludeDeleted,
            ct: cancellationToken);

        var mapped = paged.Items.Select(p => _mapper.Map<PageAdminDto>(p)).ToList();
        var result = new PagedResult<PageAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(
        PageInputDto input,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        if (await _uow.Pages.PageKeyExistsAsync(input.PageKey, excludeId: null, cancellationToken))
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Page.PageKeyExists,
                $"'{input.PageKey}' anahtarı başka bir sayfada kullanılıyor.",
                field: nameof(input.PageKey)));
        }

        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        var slugResult = await ResolveSlugsAsync(activeTranslations, excludeId: null, cancellationToken);
        if (slugResult.IsFailure)
            return Result.Failure<int>(slugResult.Errors);

        var page = new Page
        {
            PageKey = input.PageKey,
            IsSystem = input.IsSystem,
            IsActive = input.IsActive,
            DisplayOrder = input.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in activeTranslations)
        {
            page.Translations.Add(new PageTranslation
            {
                LanguageCode = t.LanguageCode,
                Title = t.Title,
                Slug = t.Slug,
                Content = _sanitizer.Sanitize(t.Content),
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _uow.Pages.AddAsync(page, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure<int>(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success(page.Id);
    }

    public async Task<Result> UpdateAsync(
        PageInputDto input,
        CancellationToken cancellationToken = default)
    {
        if (!input.Id.HasValue)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation,
                "Güncelleme için ID gerekli.",
                field: nameof(input.Id)));
        }

        var validation = await _validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        var page = await _uow.Pages.GetByIdWithTranslationsAsync(input.Id.Value, cancellationToken);
        if (page is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        if (await _uow.Pages.PageKeyExistsAsync(input.PageKey, excludeId: input.Id.Value, cancellationToken))
        {
            return Result.Failure(new Error(
                ErrorCodes.Page.PageKeyExists,
                $"'{input.PageKey}' anahtarı başka bir sayfada kullanılıyor.",
                field: nameof(input.PageKey)));
        }

        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        var slugResult = await ResolveSlugsAsync(activeTranslations, excludeId: input.Id.Value, cancellationToken);
        if (slugResult.IsFailure)
            return slugResult;

        page.PageKey = input.PageKey;
        page.IsSystem = input.IsSystem;
        page.IsActive = input.IsActive;
        page.DisplayOrder = input.DisplayOrder;
        page.UpdatedAt = DateTime.UtcNow;

        // Faz 7.1.2: Translation diff-based merge (Id + CreatedAt korunur).
        var incomingTranslations = activeTranslations.Select(t => new PageTranslation
        {
            LanguageCode = t.LanguageCode,
            Title = t.Title,
            Slug = t.Slug,
            Content = _sanitizer.Sanitize(t.Content),
            MetaTitle = t.MetaTitle,
            MetaDescription = t.MetaDescription,
        }).ToList();

        // Faz 7.4.3a: slug-change snapshot (merge target.Slug overwrite eder).
        var oldSlugsByLang = page.Translations
            .ToDictionary(t => t.LanguageCode, t => t.Slug, StringComparer.OrdinalIgnoreCase);

        TranslationMergeHelper.Merge(page.Translations, incomingTranslations, (target, source) =>
        {
            target.Title = source.Title;
            target.Slug = source.Slug;
            target.Content = source.Content;
            target.MetaTitle = source.MetaTitle;
            target.MetaDescription = source.MetaDescription;
        });

        foreach (var translation in page.Translations)
        {
            if (oldSlugsByLang.TryGetValue(translation.LanguageCode, out var oldSlug))
            {
                await _slugHistory.RecordIfChangedAsync(
                    SluggedEntityType.Page, page.Id, translation.LanguageCode,
                    oldSlug, translation.Slug, cancellationToken);
            }
        }

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Conflict,
                "Kayıt sırasında çakışma oluştu. Lütfen tekrar deneyin."));
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetByIdAsync(id, cancellationToken);
        if (page is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        _uow.Pages.Delete(page);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (page is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        if (!page.IsDeleted)
        {
            return Result.Success();
        }

        _uow.Pages.Restore(page);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var page = await _uow.Pages.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (page is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Page.NotFound, "Sayfa bulunamadı."));
        }

        _uow.Pages.HardDelete(page);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> ResolveSlugsAsync(
        List<PageTranslationInputDto> translations,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var errors = new List<Error>();

        for (var i = 0; i < translations.Count; i++)
        {
            var t = translations[i];
            try
            {
                t.Slug = string.IsNullOrWhiteSpace(t.Slug)
                    ? await _slugService.GenerateUniqueAsync(
                        t.Title, t.LanguageCode, SluggedEntityType.Page,
                        excludeId, cancellationToken)
                    : await _slugService.EnsureUniqueAsync(
                        t.Slug, t.LanguageCode, SluggedEntityType.Page,
                        excludeId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Page.SlugExists,
                    ex.Message,
                    field: $"Translations[{i}].Slug"));
            }
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

    private static bool IsActiveTranslation(PageTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Title)
            || !string.IsNullOrWhiteSpace(t.Slug)
            || !string.IsNullOrWhiteSpace(t.Content)
            || !string.IsNullOrWhiteSpace(t.MetaTitle)
            || !string.IsNullOrWhiteSpace(t.MetaDescription);
    }
}
