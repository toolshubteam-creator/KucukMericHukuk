using FluentValidation;
using KucukMericHukuk.Business.Common;
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

    public FaqService(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<FaqInputDto> validator)
    {
        _uow = uow;
        _mapper = mapper;
        _validator = validator;
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

        foreach (var t in input.Translations)
        {
            faq.Translations.Add(new FaqTranslation
            {
                LanguageCode = t.LanguageCode,
                Question = t.Question.Trim(),
                Answer = t.Answer.Trim(),
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

        faq.Translations.Clear();
        foreach (var t in input.Translations)
        {
            faq.Translations.Add(new FaqTranslation
            {
                LanguageCode = t.LanguageCode,
                Question = t.Question.Trim(),
                Answer = t.Answer.Trim(),
                CreatedAt = DateTime.UtcNow,
            });
        }

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
}
