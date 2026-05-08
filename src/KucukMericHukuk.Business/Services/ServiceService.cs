using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class ServiceService : IServiceService
{
    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly IMapper _mapper;
    private readonly IValidator<ServiceInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public ServiceService(
        IUnitOfWork uow,
        ISlugService slugService,
        IMapper mapper,
        IValidator<ServiceInputDto> validator,
        IHtmlSanitizerService sanitizer)
    {
        _uow = uow;
        _slugService = slugService;
        _mapper = mapper;
        _validator = validator;
        _sanitizer = sanitizer;
    }

    public async Task<Result<ServiceDetailDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default)
    {
        var svc = await _uow.Services.GetBySlugAsync(languageCode, slug, ct);
        if (svc is null || !svc.IsActive)
        {
            return Result.Failure<ServiceDetailDto>(
                new Error(ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        return Result.Success(_mapper.Map<ServiceDetailDto>(svc));
    }

    public async Task<IReadOnlyList<ServiceListDto>> GetActiveOrderedAsync(
        string languageCode, int? take = null, CancellationToken ct = default)
    {
        var entities = await _uow.Services.GetActiveOrderedAsync(languageCode, ct);
        var source = take.HasValue ? entities.Take(take.Value) : entities;
        return _mapper.Map<List<ServiceListDto>>(source.ToList());
    }

    public async Task<Result<ServiceAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var svc = await _uow.Services.GetByIdWithTranslationsAsync(id, ct);
        if (svc is null)
        {
            return Result.Failure<ServiceAdminDto>(
                new Error(ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        return Result.Success(_mapper.Map<ServiceAdminDto>(svc));
    }

    public async Task<Result<PagedResult<ServiceAdminDto>>> GetPagedAsync(
        ServiceQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Services.GetAdminPagedAsync(
            query.Keyword, query.LanguageCode,
            query.Page, query.PageSize,
            query.IncludeDeleted, ct);

        var mapped = paged.Items.Select(s => _mapper.Map<ServiceAdminDto>(s)).ToList();
        var result = new PagedResult<ServiceAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(
        ServiceInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        var attorneyResult = await ValidateAttorneyIdsAsync(input.AttorneyIds, ct);
        if (attorneyResult.IsFailure)
            return Result.Failure<int>(attorneyResult.Errors);

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: null, ct);
        if (slugResult.IsFailure)
            return Result.Failure<int>(slugResult.Errors);

        var svc = new Service
        {
            Icon = input.Icon,
            FeaturedImage = input.FeaturedImage,
            IsActive = input.IsActive,
            DisplayOrder = input.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in input.Translations)
        {
            svc.Translations.Add(new ServiceTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
                ShortDescription = t.ShortDescription,
                FullDescription = _sanitizer.Sanitize(t.FullDescription),
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (input.AttorneyIds != null && input.AttorneyIds.Count > 0)
        {
            var attorneys = await _uow.Attorneys.GetByIdsAsync(input.AttorneyIds, ct);
            foreach (var attorney in attorneys)
                svc.Attorneys.Add(attorney);
        }

        await _uow.Services.AddAsync(svc, ct);

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

        return Result.Success(svc.Id);
    }

    public async Task<Result> UpdateAsync(
        ServiceInputDto input, CancellationToken ct = default)
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

        var svc = await _uow.Services.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (svc is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        var attorneyResult = await ValidateAttorneyIdsAsync(input.AttorneyIds, ct);
        if (attorneyResult.IsFailure)
            return attorneyResult;

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: input.Id.Value, ct);
        if (slugResult.IsFailure)
            return slugResult;

        svc.Icon = input.Icon;
        svc.FeaturedImage = input.FeaturedImage;
        svc.IsActive = input.IsActive;
        svc.DisplayOrder = input.DisplayOrder;
        svc.UpdatedAt = DateTime.UtcNow;

        svc.Translations.Clear();
        foreach (var t in input.Translations)
        {
            svc.Translations.Add(new ServiceTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
                ShortDescription = t.ShortDescription,
                FullDescription = _sanitizer.Sanitize(t.FullDescription),
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CreatedAt = DateTime.UtcNow,
            });
        }

        svc.Attorneys.Clear();
        if (input.AttorneyIds != null && input.AttorneyIds.Count > 0)
        {
            var attorneys = await _uow.Attorneys.GetByIdsAsync(input.AttorneyIds, ct);
            foreach (var attorney in attorneys)
                svc.Attorneys.Add(attorney);
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
        var svc = await _uow.Services.GetByIdAsync(id, ct);
        if (svc is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        _uow.Services.Delete(svc);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var svc = await _uow.Services.GetByIdIncludingDeletedAsync(id, ct);
        if (svc is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        if (!svc.IsDeleted) return Result.Success();

        _uow.Services.Restore(svc);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var svc = await _uow.Services.GetByIdIncludingDeletedAsync(id, ct);
        if (svc is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Service.NotFound, "Hizmet bulunamadı."));
        }

        _uow.Services.HardDelete(svc);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> ValidateAttorneyIdsAsync(
        List<int>? attorneyIds, CancellationToken ct)
    {
        if (attorneyIds == null || attorneyIds.Count == 0)
            return Result.Success();

        var allExist = await _uow.Attorneys.AttorneysExistAsync(attorneyIds, ct);
        if (!allExist)
        {
            return Result.Failure(new Error(
                ErrorCodes.Service.AttorneyNotFound,
                "Seçilen avukatlardan en az biri bulunamadı.",
                field: nameof(ServiceInputDto.AttorneyIds)));
        }

        return Result.Success();
    }

    private async Task<Result> ResolveSlugsAsync(
        List<ServiceTranslationInputDto> translations,
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
                        t.Name, t.LanguageCode, SluggedEntityType.Service,
                        excludeId, ct)
                    : await _slugService.EnsureUniqueAsync(
                        t.Slug, t.LanguageCode, SluggedEntityType.Service,
                        excludeId, ct);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Service.SlugExists,
                    ex.Message,
                    field: $"Translations[{i}].Slug"));
            }
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

}
