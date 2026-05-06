using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.DTOs.Media;
using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.Areas.Admin.ViewModels.Medias;
using KucukMericHukuk.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/medias")]
[Authorize(Roles = "Admin,Editor")]
public class MediasController : Controller
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediasController> _logger;

    public MediasController(IMediaService mediaService, ILogger<MediasController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index([FromQuery] MediaQuery query, CancellationToken ct)
    {
        ViewData["Title"] = "Medya";

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 24;

        var result = await _mediaService.GetPagedAsync(query.Keyword, query.Page, query.PageSize, query.IncludeDeleted, ct);

        if (result.IsFailure)
        {
            _logger.LogError("Medya listesi alınamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = "Medya listesi yüklenirken bir sorun oluştu.";
            return View(new MediaListViewModel { Query = query });
        }

        var vm = new MediaListViewModel
        {
            Query = query,
            Items = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            HasPrevious = result.Value.HasPrevious,
            HasNext = result.Value.HasNext,
        };

        return View(vm);
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Medya Detayı";
        var result = await _mediaService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Media.NotFound)
                return NotFound();
            TempData["Error"] = "Medya yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        ViewData["Title"] = "Medya Düzenle";
        var result = await _mediaService.GetByIdAsync(id, ct);

        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Media.NotFound)
                return NotFound();
            TempData["Error"] = "Medya yüklenirken bir sorun oluştu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new MediaEditFormViewModel
        {
            Id = result.Value.Id,
            AltText = result.Value.AltText,
        };

        ViewBag.Media = result.Value;
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MediaEditFormViewModel form, CancellationToken ct)
    {
        ViewData["Title"] = "Medya Düzenle";

        if (form.Id != id) return BadRequest();

        if (!ModelState.IsValid)
        {
            var get = await _mediaService.GetByIdAsync(id, ct);
            if (get.IsSuccess) ViewBag.Media = get.Value;
            return View(form);
        }

        var input = new MediaUpdateInputDto { Id = id, AltText = form.AltText };
        var result = await _mediaService.UpdateAsync(input, ct);

        if (result.IsFailure)
        {
            ModelState.AddErrors(result);
            var get = await _mediaService.GetByIdAsync(id, ct);
            if (get.IsSuccess) ViewBag.Media = get.Value;
            return View(form);
        }

        TempData["Success"] = "Medya güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediaService.DeleteAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Media.NotFound) return NotFound();
            _logger.LogWarning("Medya silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = result.FirstError?.Message ?? "Silinirken hata oluştu.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["Success"] = "Medya silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var result = await _mediaService.RestoreAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Media.NotFound) return NotFound();
            _logger.LogWarning("Medya geri yüklenemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = result.FirstError?.Message ?? "Geri yüklenirken hata oluştu.";
            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }
        TempData["Success"] = "Medya geri yüklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("hard-delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
    {
        var result = await _mediaService.HardDeleteAsync(id, ct);
        if (result.IsFailure)
        {
            if (result.FirstError?.Code == ErrorCodes.Media.NotFound) return NotFound();
            _logger.LogWarning("Medya kalıcı silinemedi: id={Id}, errors={Errors}",
                id, string.Join("; ", result.Errors.Select(e => e.Message)));
            TempData["Error"] = result.FirstError?.Message ?? "Kalıcı silinirken hata oluştu.";
            return RedirectToAction(nameof(Index), new { includeDeleted = true });
        }
        TempData["Success"] = "Medya kalıcı olarak silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? altText, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Json(new MediaUploadResponseViewModel { Success = false, ErrorMessage = "Dosya bulunamadı." });

        await using var stream = file.OpenReadStream();
        var input = new MediaUploadInputDto
        {
            FileStream = stream,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AltText = altText,
            UploadedByUserId = User.GetUserIdOrNull(),
        };

        var result = await _mediaService.UploadAsync(input, ct);

        if (result.IsFailure)
        {
            return Json(new MediaUploadResponseViewModel
            {
                Success = false,
                ErrorMessage = result.FirstError?.Message ?? "Yükleme başarısız."
            });
        }

        var dto = result.Value;
        return Json(new MediaUploadResponseViewModel
        {
            Success = true,
            Id = dto.Id,
            Url = dto.Url,
            ThumbnailUrl = dto.ThumbnailUrl,
            OriginalFileName = dto.OriginalFileName,
            AltText = dto.AltText,
        });
    }

    [HttpGet("picker-list")]
    public async Task<IActionResult> PickerList([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 24;

        var result = await _mediaService.GetPagedAsync(keyword, page, pageSize, includeDeleted: false, ct);

        if (result.IsFailure)
            return Json(new MediaPickerListResponse());

        var items = result.Value.Items.Select(m => new MediaPickerItemViewModel
        {
            Id = m.Id,
            OriginalFileName = m.OriginalFileName,
            Url = m.Url,
            ThumbnailUrl = m.ThumbnailUrl,
            AltText = m.AltText,
            Width = m.Width,
            Height = m.Height,
        }).ToList();

        return Json(new MediaPickerListResponse
        {
            Items = items,
            TotalCount = result.Value.TotalCount,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            HasNext = result.Value.HasNext,
        });
    }
}
