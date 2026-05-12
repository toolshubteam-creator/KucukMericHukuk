using KucukMericHukuk.Core.Interfaces.Services;
using KucukMericHukuk.Web.ViewModels.Testimonial;
using Microsoft.AspNetCore.Mvc;

namespace KucukMericHukuk.Web.Controllers;

public class TestimonialsController : Controller
{
    private const int PageSize = 12;

    private readonly ITestimonialService _service;

    public TestimonialsController(ITestimonialService service)
    {
        _service = service;
    }

    [HttpGet]
    [Route("{culture:culture}/Referanslar")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var lang = System.Globalization.CultureInfo.CurrentUICulture.Name;
        if (page < 1) page = 1;

        var paged = await _service.GetPublicPagedAsync(lang, page, PageSize, ct);

        var vm = new TestimonialListViewModel { Testimonials = paged };
        return View(vm);
    }
}
