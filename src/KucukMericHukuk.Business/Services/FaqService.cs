using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Business.Helpers;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

public class FaqService : IFaqService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<FaqInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public FaqService(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<FaqInputDto> validator,
        IHtmlSanitizerService sanitizer)
    {
        _uow = uow;
        _mapper = mapper;
        _validator = validator;
        _sanitizer = sanitizer;
    }

    public async Task<IReadOnlyList<FaqListDto>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default)
    {
        var faqs = await _uow.Faqs.GetActiveOrderedAsync(languageCode, ct);
        return _mapper.Map<List<FaqListDto>>(faqs);
    }

    public async Task<Result<FaqAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var faq = await _uow.Faqs.GetByIdIncludingDeletedAsync(id, ct);
        if (faq is null)
        {
            return Result.Failure<FaqAdminDto>(
                new Error(ErrorCodes.Faq.NotFound, "SSS kaydı bulunamadı."));
        }

        return Result.Success(_mapper.Map<FaqAdminDto>(faq));
    }

    public async Task<Result<PagedResult<FaqAdminDto>>> GetPagedAsync(
        FaqQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Faqs.GetAdminPagedAsync(
            query.Keyword, query.LanguageCode,
            query.Page, query.PageSize,
            query.IncludeDeleted, ct);

        var mapped = paged.Items.Select(f => _mapper.Map<FaqAdminDto>(f)).ToList();
        var result = new PagedResult<FaqAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(FaqInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        var faq = new Faq
        {
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        foreach (var t in activeTranslations)
        {
            faq.Translations.Add(new FaqTranslation
            {
                LanguageCode = t.LanguageCode,
                Question = t.Question.Trim(),
                Answer = _sanitizer.Sanitize((t.Answer ?? string.Empty).Trim()),
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _uow.Faqs.AddAsync(faq, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(faq.Id);
    }

    public async Task<Result> UpdateAsync(FaqInputDto input, CancellationToken ct = default)
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

        var faq = await _uow.Faqs.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (faq is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.NotFound, "SSS kaydı bulunamadı."));
        }

        faq.DisplayOrder = input.DisplayOrder;
        faq.IsActive = input.IsActive;
        faq.UpdatedAt = DateTime.UtcNow;

        // Faz 7.1.2: Translations.Clear()+Add() yerine diff-based merge.
        // LanguageCode bazlı eşleşme → mevcut translation Id + CreatedAt korunur,
        // audit log Modified delta olarak görünür (önceki Deleted+Added gürültüsü temizlendi).
        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        var incomingTranslations = activeTranslations.Select(t => new FaqTranslation
        {
            LanguageCode = t.LanguageCode,
            Question = t.Question.Trim(),
            Answer = _sanitizer.Sanitize((t.Answer ?? string.Empty).Trim()),
        }).ToList();

        TranslationMergeHelper.Merge(faq.Translations, incomingTranslations, (target, source) =>
        {
            target.Question = source.Question;
            target.Answer = source.Answer;
        });

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var faq = await _uow.Faqs.GetByIdAsync(id, ct);
        if (faq is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.NotFound, "SSS kaydı bulunamadı."));
        }

        _uow.Faqs.Delete(faq);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var faq = await _uow.Faqs.GetByIdIncludingDeletedAsync(id, ct);
        if (faq is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.NotFound, "SSS kaydı bulunamadı."));
        }

        if (!faq.IsDeleted) return Result.Success();

        _uow.Faqs.Restore(faq);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var faq = await _uow.Faqs.GetByIdIncludingDeletedAsync(id, ct);
        if (faq is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.NotFound, "SSS kaydı bulunamadı."));
        }

        _uow.Faqs.HardDelete(faq);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReorderAsync(
        IReadOnlyList<FaqReorderItemDto> items, CancellationToken ct = default)
    {
        if (items is null || items.Count == 0)
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.ReorderInvalid, "Sıralama listesi boş olamaz."));
        }

        var allFaqs = await _uow.Faqs.GetAllForReorderAsync(ct);

        var inputIds = items.Select(x => x.Id).ToHashSet();
        var dbIds = allFaqs.Select(x => x.Id).ToHashSet();

        // Concurrent silme/ekleme korunması: input ID'ler mevcut DB ID'leriyle birebir eşleşmeli
        if (!inputIds.SetEquals(dbIds))
        {
            return Result.Failure(new Error(
                ErrorCodes.Faq.ReorderInvalid,
                "Sıralama listesi mevcut SSS'lerle eşleşmiyor. Sayfayı yenileyip tekrar deneyin."));
        }

        var now = DateTime.UtcNow;
        foreach (var faq in allFaqs)
        {
            var item = items.First(x => x.Id == faq.Id);
            if (faq.DisplayOrder != item.DisplayOrder)
            {
                faq.DisplayOrder = item.DisplayOrder;
                faq.UpdatedAt = now;
            }
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static bool IsActiveTranslation(FaqTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Question)
            || !string.IsNullOrWhiteSpace(t.Answer);
    }
}
