using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class AttorneyService : IAttorneyService
{
    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly IMapper _mapper;
    private readonly IValidator<AttorneyInputDto> _validator;
    private readonly IHtmlSanitizerService _sanitizer;

    public AttorneyService(
        IUnitOfWork uow,
        ISlugService slugService,
        IMapper mapper,
        IValidator<AttorneyInputDto> validator,
        IHtmlSanitizerService sanitizer)
    {
        _uow = uow;
        _slugService = slugService;
        _mapper = mapper;
        _validator = validator;
        _sanitizer = sanitizer;
    }

    public async Task<Result<AttorneyDetailDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default)
    {
        var attorney = await _uow.Attorneys.GetBySlugAsync(languageCode, slug, ct);
        if (attorney is null || !attorney.IsActive)
        {
            return Result.Failure<AttorneyDetailDto>(
                new Error(ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }
        return Result.Success(_mapper.Map<AttorneyDetailDto>(attorney));
    }

    public async Task<IReadOnlyList<AttorneyListDto>> GetActiveOrderedAsync(
        string languageCode, int? take = null, CancellationToken ct = default)
    {
        var entities = await _uow.Attorneys.GetActiveOrderedAsync(languageCode, ct);
        var source = take.HasValue ? entities.Take(take.Value) : entities;
        return _mapper.Map<List<AttorneyListDto>>(source.ToList());
    }

    public async Task<Result<AttorneyAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var attorney = await _uow.Attorneys.GetByIdWithTranslationsAsync(id, ct);
        if (attorney is null)
        {
            return Result.Failure<AttorneyAdminDto>(
                new Error(ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }
        return Result.Success(_mapper.Map<AttorneyAdminDto>(attorney));
    }

    public async Task<Result<PagedResult<AttorneyAdminDto>>> GetPagedAsync(
        AttorneyQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Attorneys.GetAdminPagedAsync(
            query.Keyword, query.LanguageCode, query.ServiceId,
            query.Page, query.PageSize, query.IncludeDeleted, ct);

        var mapped = paged.Items.Select(a => _mapper.Map<AttorneyAdminDto>(a)).ToList();
        var result = new PagedResult<AttorneyAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(
        AttorneyInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid) return validation.ToFailureResult<int>();

        var userResult = await ValidateUserIdAsync(input.UserId, excludeAttorneyId: null, ct);
        if (userResult.IsFailure) return Result.Failure<int>(userResult.Errors);

        var serviceResult = await ValidateServiceIdsAsync(input.ServiceIds, ct);
        if (serviceResult.IsFailure) return Result.Failure<int>(serviceResult.Errors);

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: null, ct);
        if (slugResult.IsFailure) return Result.Failure<int>(slugResult.Errors);

        var attorney = new Attorney
        {
            UserId = input.UserId,
            IsActive = input.IsActive,
            DisplayOrder = input.DisplayOrder,
            BarRegistrationNumber = input.BarRegistrationNumber,
            BarName = input.BarName,
            Email = input.Email,
            PhoneNumber = input.PhoneNumber,
            LinkedInUrl = input.LinkedInUrl,
            ProfileImageUrl = input.ProfileImageUrl,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in input.Translations)
        {
            attorney.Translations.Add(BuildTranslation(t));
        }

        if (input.ServiceIds != null && input.ServiceIds.Count > 0)
        {
            var services = await _uow.Services.GetByIdsAsync(input.ServiceIds, ct);
            foreach (var service in services)
                attorney.Services.Add(service);
        }

        await _uow.Attorneys.AddAsync(attorney, ct);

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

        return Result.Success(attorney.Id);
    }

    public async Task<Result> UpdateAsync(
        AttorneyInputDto input, CancellationToken ct = default)
    {
        if (!input.Id.HasValue)
        {
            return Result.Failure(new Error(
                ErrorCodes.Common.Validation,
                "Güncelleme için ID gerekli.",
                field: nameof(input.Id)));
        }

        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid) return validation.ToFailureResult();

        var attorney = await _uow.Attorneys.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (attorney is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }

        var userResult = await ValidateUserIdAsync(input.UserId, excludeAttorneyId: input.Id.Value, ct);
        if (userResult.IsFailure) return userResult;

        var serviceResult = await ValidateServiceIdsAsync(input.ServiceIds, ct);
        if (serviceResult.IsFailure) return serviceResult;

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: input.Id.Value, ct);
        if (slugResult.IsFailure) return slugResult;

        attorney.UserId = input.UserId;
        attorney.IsActive = input.IsActive;
        attorney.DisplayOrder = input.DisplayOrder;
        attorney.BarRegistrationNumber = input.BarRegistrationNumber;
        attorney.BarName = input.BarName;
        attorney.Email = input.Email;
        attorney.PhoneNumber = input.PhoneNumber;
        attorney.LinkedInUrl = input.LinkedInUrl;
        attorney.ProfileImageUrl = input.ProfileImageUrl;
        attorney.UpdatedAt = DateTime.UtcNow;

        attorney.Translations.Clear();
        foreach (var t in input.Translations)
        {
            attorney.Translations.Add(BuildTranslation(t));
        }

        attorney.Services.Clear();
        if (input.ServiceIds != null && input.ServiceIds.Count > 0)
        {
            var services = await _uow.Services.GetByIdsAsync(input.ServiceIds, ct);
            foreach (var service in services)
                attorney.Services.Add(service);
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
        var attorney = await _uow.Attorneys.GetByIdAsync(id, ct);
        if (attorney is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }
        _uow.Attorneys.Delete(attorney);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var attorney = await _uow.Attorneys.GetByIdIncludingDeletedAsync(id, ct);
        if (attorney is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }
        if (!attorney.IsDeleted) return Result.Success();
        _uow.Attorneys.Restore(attorney);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var attorney = await _uow.Attorneys.GetByIdIncludingDeletedAsync(id, ct);
        if (attorney is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.NotFound, "Avukat bulunamadı."));
        }
        _uow.Attorneys.HardDelete(attorney);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result> ValidateUserIdAsync(
        int? userId, int? excludeAttorneyId, CancellationToken ct)
    {
        if (!userId.HasValue) return Result.Success();

        var exists = await _uow.Attorneys.UserIdExistsAsync(userId.Value, excludeAttorneyId, ct);
        if (exists)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.UserAlreadyLinked,
                "Bu kullanıcı zaten başka bir avukat profiline bağlı.",
                field: nameof(AttorneyInputDto.UserId)));
        }
        return Result.Success();
    }

    private async Task<Result> ValidateServiceIdsAsync(
        List<int>? serviceIds, CancellationToken ct)
    {
        if (serviceIds == null || serviceIds.Count == 0) return Result.Success();

        var allExist = await _uow.Services.ServicesExistAsync(serviceIds, ct);
        if (!allExist)
        {
            return Result.Failure(new Error(
                ErrorCodes.Attorney.ServiceNotFound,
                "Seçilen hizmetlerden en az biri bulunamadı.",
                field: nameof(AttorneyInputDto.ServiceIds)));
        }
        return Result.Success();
    }

    private async Task<Result> ResolveSlugsAsync(
        List<AttorneyTranslationInputDto> translations,
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
                        t.FullName, t.LanguageCode, SluggedEntityType.Attorney,
                        excludeId, ct)
                    : await _slugService.EnsureUniqueAsync(
                        t.Slug, t.LanguageCode, SluggedEntityType.Attorney,
                        excludeId, ct);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Attorney.SlugExists, ex.Message,
                    field: $"Translations[{i}].Slug"));
            }
        }
        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

    private AttorneyTranslation BuildTranslation(AttorneyTranslationInputDto t)
    {
        return new AttorneyTranslation
        {
            LanguageCode = t.LanguageCode,
            FullName = t.FullName,
            Title = t.Title,
            Slug = t.Slug,
            ShortBio = t.ShortBio,
            FullBio = string.IsNullOrWhiteSpace(t.FullBio)
                ? null
                : _sanitizer.Sanitize(t.FullBio),
            Education = t.Education,
            Publications = t.Publications,
            MetaTitle = t.MetaTitle,
            MetaDescription = t.MetaDescription,
            CreatedAt = DateTime.UtcNow,
        };
    }

}
