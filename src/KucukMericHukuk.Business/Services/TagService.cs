using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Tag;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.Business.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _uow;
    private readonly ISlugService _slugService;
    private readonly IMapper _mapper;
    private readonly IValidator<TagInputDto> _validator;

    public TagService(
        IUnitOfWork uow,
        ISlugService slugService,
        IMapper mapper,
        IValidator<TagInputDto> validator)
    {
        _uow = uow;
        _slugService = slugService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<Result<TagListDto>> GetBySlugAsync(
        string slug, string languageCode, CancellationToken ct = default)
    {
        var tag = await _uow.Tags.GetBySlugAsync(languageCode, slug, ct);
        if (tag is null || !tag.IsActive)
        {
            return Result.Failure<TagListDto>(
                new Error(ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        return Result.Success(_mapper.Map<TagListDto>(tag));
    }

    public async Task<Result<TagAdminDto>> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        var tag = await _uow.Tags.GetByIdWithTranslationsAsync(id, ct);
        if (tag is null)
        {
            return Result.Failure<TagAdminDto>(
                new Error(ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        return Result.Success(_mapper.Map<TagAdminDto>(tag));
    }

    public async Task<Result<PagedResult<TagAdminDto>>> GetPagedAsync(
        TagQueryDto query, CancellationToken ct = default)
    {
        var paged = await _uow.Tags.GetAdminPagedAsync(
            keyword: query.Keyword,
            languageCode: query.LanguageCode,
            page: query.Page,
            pageSize: query.PageSize,
            includeDeleted: query.IncludeDeleted,
            ct: ct);

        var mapped = paged.Items.Select(t => _mapper.Map<TagAdminDto>(t)).ToList();
        var result = new PagedResult<TagAdminDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);

        return Result.Success(result);
    }

    public async Task<Result<int>> CreateAsync(
        TagInputDto input, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<int>();

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: null, ct);
        if (slugResult.IsFailure)
            return Result.Failure<int>(slugResult.Errors);

        var tag = new Tag
        {
            IsActive = input.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var t in input.Translations)
        {
            tag.Translations.Add(new TagTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _uow.Tags.AddAsync(tag, ct);

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

        return Result.Success(tag.Id);
    }

    public async Task<Result> UpdateAsync(
        TagInputDto input, CancellationToken ct = default)
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

        var tag = await _uow.Tags.GetByIdWithTranslationsAsync(input.Id.Value, ct);
        if (tag is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        var slugResult = await ResolveSlugsAsync(input.Translations, excludeId: input.Id.Value, ct);
        if (slugResult.IsFailure)
            return slugResult;

        tag.IsActive = input.IsActive;
        tag.UpdatedAt = DateTime.UtcNow;

        tag.Translations.Clear();
        foreach (var t in input.Translations)
        {
            tag.Translations.Add(new TagTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Slug = t.Slug,
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
        var tag = await _uow.Tags.GetByIdAsync(id, ct);
        if (tag is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        _uow.Tags.Delete(tag);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var tag = await _uow.Tags.GetByIdIncludingDeletedAsync(id, ct);
        if (tag is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        if (!tag.IsDeleted)
        {
            return Result.Success();
        }

        _uow.Tags.Restore(tag);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var tag = await _uow.Tags.GetByIdIncludingDeletedAsync(id, ct);
        if (tag is null)
        {
            return Result.Failure(new Error(
                ErrorCodes.Tag.NotFound, "Etiket bulunamadı."));
        }

        _uow.Tags.HardDelete(tag);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<Result> ResolveSlugsAsync(
        List<TagTranslationInputDto> translations,
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
                        t.Name, t.LanguageCode, SluggedEntityType.Tag,
                        excludeId, ct)
                    : await _slugService.EnsureUniqueAsync(
                        t.Slug, t.LanguageCode, SluggedEntityType.Tag,
                        excludeId, ct);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new Error(
                    ErrorCodes.Tag.SlugExists,
                    ex.Message,
                    field: $"Translations[{i}].Slug"));
            }
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }

}
