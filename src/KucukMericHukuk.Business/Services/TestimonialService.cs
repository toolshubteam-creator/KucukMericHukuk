using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Business.Helpers;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Testimonial;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;

namespace KucukMericHukuk.Business.Services;

public class TestimonialService : ITestimonialService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<TestimonialInputDto> _validator;

    public TestimonialService(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<TestimonialInputDto> validator)
    {
        _uow = uow;
        _mapper = mapper;
        _validator = validator;
    }

    // -------------------- PUBLIC --------------------

    public async Task<IReadOnlyList<TestimonialListDto>> GetActiveOrderedAsync(
        string languageCode, CancellationToken ct = default)
    {
        var items = await _uow.Testimonials.GetActiveOrderedAsync(languageCode, ct);
        return _mapper.Map<List<TestimonialListDto>>(items);
    }

    public async Task<IReadOnlyList<TestimonialListDto>> GetFeaturedOrderedAsync(
        string languageCode, int maxCount, CancellationToken ct = default)
    {
        var items = await _uow.Testimonials.GetFeaturedOrderedAsync(languageCode, maxCount, ct);
        return _mapper.Map<List<TestimonialListDto>>(items);
    }

    public async Task<PagedResult<TestimonialListDto>> GetPublicPagedAsync(
        string languageCode, int page, int pageSize, CancellationToken ct = default)
    {
        var paged = await _uow.Testimonials.GetPublicPagedAsync(languageCode, page, pageSize, ct);
        var mapped = paged.Items.Select(t => _mapper.Map<TestimonialListDto>(t)).ToList();
        return new PagedResult<TestimonialListDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    // -------------------- ADMIN --------------------

    public async Task<Result<TestimonialAdminDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Testimonials.GetByIdIncludingDeletedAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure<TestimonialAdminDto>(
                new Error(ErrorCodes.Testimonial.NotFound, "Müvekkil yorumu bulunamadı."));
        }

        return Result.Success(_mapper.Map<TestimonialAdminDto>(entity));
    }

    public async Task<Result<PagedResult<TestimonialAdminDto>>> GetPagedAsync(
        TestimonialQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Testimonials.GetAdminPagedAsync(
            query.Keyword, query.LanguageCode,
            query.Page, query.PageSize,
            query.IncludeDeleted, ct);

        var mapped = paged.Items.Select(t => _mapper.Map<TestimonialAdminDto>(t)).ToList();
        var result = new PagedResult<TestimonialAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(TestimonialInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        var entity = new Testimonial
        {
            AuthorInitials = string.IsNullOrWhiteSpace(input.AuthorInitials) ? null : input.AuthorInitials.Trim(),
            AuthorRole = string.IsNullOrWhiteSpace(input.AuthorRole) ? null : input.AuthorRole.Trim(),
            Rating = input.Rating,
            DisplayOrder = input.DisplayOrder,
            IsActive = input.IsActive,
            IsFeatured = input.IsFeatured,
            CreatedAt = DateTime.UtcNow,
        };

        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        foreach (var t in activeTranslations)
        {
            entity.Translations.Add(new TestimonialTranslation
            {
                LanguageCode = t.LanguageCode,
                Content = t.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _uow.Testimonials.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(TestimonialInputDto input, CancellationToken ct = default)
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

        var entity = await _uow.Testimonials.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Testimonial.NotFound, "Müvekkil yorumu bulunamadı."));
        }

        entity.AuthorInitials = string.IsNullOrWhiteSpace(input.AuthorInitials) ? null : input.AuthorInitials.Trim();
        entity.AuthorRole = string.IsNullOrWhiteSpace(input.AuthorRole) ? null : input.AuthorRole.Trim();
        entity.Rating = input.Rating;
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsActive = input.IsActive;
        entity.IsFeatured = input.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;

        // Faz 7.1.2: Translation diff-based merge (Id + CreatedAt korunur).
        // Önceki "full-replace (Faq pattern)" yorumu: DEFERRED'daki "Translation full-replace
        // stratejisi" maddesi Faz 7.1.2'de kapatıldı, helper'a geçildi.
        var activeTranslations = input.Translations.Where(IsActiveTranslation).ToList();
        var incomingTranslations = activeTranslations.Select(t => new TestimonialTranslation
        {
            LanguageCode = t.LanguageCode,
            Content = t.Content.Trim(),
        }).ToList();

        TranslationMergeHelper.Merge(entity.Translations, incomingTranslations, (target, source) =>
        {
            target.Content = source.Content;
        });

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Testimonials.GetByIdAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Testimonial.NotFound, "Müvekkil yorumu bulunamadı."));
        }

        _uow.Testimonials.Delete(entity);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Testimonials.GetByIdIncludingDeletedAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Testimonial.NotFound, "Müvekkil yorumu bulunamadı."));
        }

        if (!entity.IsDeleted) return Result.Success();

        _uow.Testimonials.Restore(entity);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Testimonials.GetByIdIncludingDeletedAsync(id, ct);
        if (entity is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Testimonial.NotFound, "Müvekkil yorumu bulunamadı."));
        }

        _uow.Testimonials.HardDelete(entity);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static bool IsActiveTranslation(TestimonialTranslationInputDto t)
    {
        return !string.IsNullOrWhiteSpace(t.Content);
    }
}
