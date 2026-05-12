using System.Security.Cryptography;
using FluentValidation;
using KucukMericHukuk.Business.Common;
using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Media;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KucukMericHukuk.Business.Services;

public class MediaService : IMediaService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;
    private readonly IImageProcessor _processor;
    private readonly IMapper _mapper;
    private readonly IValidator<MediaUploadInputDto> _uploadValidator;
    private readonly IValidator<MediaUpdateInputDto> _updateValidator;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IUnitOfWork uow,
        IFileStorageService storage,
        IImageProcessor processor,
        IMapper mapper,
        IValidator<MediaUploadInputDto> uploadValidator,
        IValidator<MediaUpdateInputDto> updateValidator,
        ILogger<MediaService> logger)
    {
        _uow = uow;
        _storage = storage;
        _processor = processor;
        _mapper = mapper;
        _uploadValidator = uploadValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<Result<MediaFileDto>> UploadAsync(MediaUploadInputDto input, CancellationToken ct = default)
    {
        var validation = await _uploadValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult<MediaFileDto>();

        // SHA256 hash hesapla
        input.FileStream.Position = 0;
        string sha256;
        using (var sha = SHA256.Create())
        {
            var hashBytes = await sha.ComputeHashAsync(input.FileStream, ct);
            sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Dedup: aynı hash varsa mevcut MediaFile'ı döndür (disk yazma yok)
        var existing = await _uow.MediaFiles.GetByHashAsync(sha256, ct);
        if (existing is not null)
        {
            return Result.Success(MapWithUrls(existing));
        }

        // Image processing
        ProcessedImageResult processed;
        try
        {
            input.FileStream.Position = 0;
            processed = await _processor.ProcessAsync(input.FileStream, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Görsel işleme başarısız: {FileName}", input.OriginalFileName);
            return Result.Failure<MediaFileDto>(
                new Error(ErrorCodes.Media.ProcessingFailed, "Görsel işlenirken hata oluştu."));
        }

        var now = DateTime.UtcNow;
        var fileName = $"{sha256}.webp";
        var thumbnailName = $"{sha256}_thumb.webp";
        var folder = Path.Combine("uploads", now.Year.ToString("D4"), now.Month.ToString("D2"));
        var mainRelative = Path.Combine(folder, fileName).Replace('\\', '/');
        var thumbRelative = Path.Combine(folder, thumbnailName).Replace('\\', '/');

        var savedMain = false;
        var savedThumb = false;
        try
        {
            await _storage.SaveAsync(processed.MainImage, mainRelative, ct);
            savedMain = true;
            await _storage.SaveAsync(processed.Thumbnail, thumbRelative, ct);
            savedThumb = true;

            processed.MainImage.Position = 0;
            var fileSize = processed.MainImage.Length;

            var entity = new MediaFile
            {
                FileName = fileName,
                OriginalFileName = input.OriginalFileName,
                RelativePath = mainRelative,
                ThumbnailRelativePath = thumbRelative,
                ContentType = "image/webp",
                FileSizeBytes = fileSize,
                Width = processed.Width,
                Height = processed.Height,
                Sha256 = sha256,
                AltText = input.AltText,
                UploadedByUserId = input.UploadedByUserId,
                CreatedAt = now,
            };

            await _uow.MediaFiles.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return Result.Success(MapWithUrls(entity));
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Race condition: aynı hash başka thread tarafından eklenmiş
            if (savedMain) await _storage.DeleteAsync(mainRelative, ct);
            if (savedThumb) await _storage.DeleteAsync(thumbRelative, ct);

            var winner = await _uow.MediaFiles.GetByHashAsync(sha256, ct);
            if (winner is not null)
                return Result.Success(MapWithUrls(winner));

            return Result.Failure<MediaFileDto>(
                new Error(ErrorCodes.Common.Conflict, "Yükleme sırasında çakışma oluştu."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Medya yüklemesi başarısız: {FileName}", input.OriginalFileName);
            if (savedMain) await _storage.DeleteAsync(mainRelative, ct);
            if (savedThumb) await _storage.DeleteAsync(thumbRelative, ct);
            return Result.Failure<MediaFileDto>(
                new Error(ErrorCodes.Media.StorageFailed, "Dosya kaydedilirken hata oluştu."));
        }
        finally
        {
            await processed.MainImage.DisposeAsync();
            await processed.Thumbnail.DisposeAsync();
        }
    }

    public async Task<Result<MediaFileDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var media = await _uow.MediaFiles.GetByIdAsync(id, ct);
        if (media is null)
            return Result.Failure<MediaFileDto>(new Error(ErrorCodes.Media.NotFound, "Medya bulunamadı."));

        return Result.Success(MapWithUrls(media));
    }

    public async Task<Result<PagedResult<MediaFileListDto>>> GetPagedAsync(
        string? keyword, int page, int pageSize, bool includeDeleted, CancellationToken ct = default)
    {
        var paged = await _uow.MediaFiles.GetAdminPagedAsync(keyword, page, pageSize, includeDeleted, ct);

        var mapped = paged.Items.Select(m =>
        {
            var dto = _mapper.Map<MediaFileListDto>(m);
            dto.Url = _storage.GetPublicUrl(m.RelativePath);
            dto.ThumbnailUrl = _storage.GetPublicUrl(m.ThumbnailRelativePath);
            return dto;
        }).ToList();

        return Result.Success(new PagedResult<MediaFileListDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }

    public async Task<Result> UpdateAsync(MediaUpdateInputDto input, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(input, ct);
        if (!validation.IsValid)
            return validation.ToFailureResult();

        var media = await _uow.MediaFiles.GetByIdAsync(input.Id, ct);
        if (media is null)
            return Result.Failure(new Error(ErrorCodes.Media.NotFound, "Medya bulunamadı."));

        media.AltText = input.AltText;
        media.IsPublic = input.IsPublic;
        media.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<PagedResult<MediaFilePublicDto>> GetPublicGalleryAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 24;
        if (pageSize > 100) pageSize = 100;

        var paged = await _uow.MediaFiles.GetPublicPagedAsync(page, pageSize, ct);

        var mapped = paged.Items.Select(m => new MediaFilePublicDto
        {
            Id = m.Id,
            Url = _storage.GetPublicUrl(m.RelativePath),
            ThumbnailUrl = _storage.GetPublicUrl(m.ThumbnailRelativePath),
            Width = m.Width,
            Height = m.Height,
            AltText = m.AltText,
            CreatedAt = m.CreatedAt,
        }).ToList();

        return new PagedResult<MediaFilePublicDto>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var media = await _uow.MediaFiles.GetByIdAsync(id, ct);
        if (media is null)
            return Result.Failure(new Error(ErrorCodes.Media.NotFound, "Medya bulunamadı."));

        _uow.MediaFiles.Delete(media);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct = default)
    {
        var media = await _uow.MediaFiles.GetByIdIncludingDeletedAsync(id, ct);
        if (media is null)
            return Result.Failure(new Error(ErrorCodes.Media.NotFound, "Medya bulunamadı."));

        if (!media.IsDeleted)
            return Result.Success();

        _uow.MediaFiles.Restore(media);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> HardDeleteAsync(int id, CancellationToken ct = default)
    {
        var media = await _uow.MediaFiles.GetByIdIncludingDeletedAsync(id, ct);
        if (media is null)
            return Result.Failure(new Error(ErrorCodes.Media.NotFound, "Medya bulunamadı."));

        var mainPath = media.RelativePath;
        var thumbPath = media.ThumbnailRelativePath;

        _uow.MediaFiles.HardDelete(media);
        await _uow.SaveChangesAsync(ct);

        // DB başarılı, fiziksel dosyaları da sil (best-effort)
        await _storage.DeleteAsync(mainPath, ct);
        await _storage.DeleteAsync(thumbPath, ct);

        return Result.Success();
    }

    private MediaFileDto MapWithUrls(MediaFile entity)
    {
        var dto = _mapper.Map<MediaFileDto>(entity);
        dto.Url = _storage.GetPublicUrl(entity.RelativePath);
        dto.ThumbnailUrl = _storage.GetPublicUrl(entity.ThumbnailRelativePath);
        return dto;
    }
}
