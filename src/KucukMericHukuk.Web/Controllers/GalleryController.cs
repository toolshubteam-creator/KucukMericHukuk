using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Gallery;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class GalleryController : Controller
{
    private const int PageSize = 24;

    private readonly IMediaService _mediaService;

    public GalleryController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    [Route("{culture:trCulture}/galeri", Order = 0)]
    [Route("{culture:enCulture}/gallery", Order = 0)]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var paged = await _mediaService.GetPublicGalleryAsync(page, PageSize, ct);

        var vm = new GalleryListViewModel { Items = paged };
        return View(vm);
    }
}
