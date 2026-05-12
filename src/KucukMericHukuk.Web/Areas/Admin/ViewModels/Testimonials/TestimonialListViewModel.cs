using KucukMericHukuk.Core.DTOs.Testimonial;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Testimonials;

public class TestimonialListViewModel
{
    public TestimonialQueryDto Query { get; set; } = new();
    public IReadOnlyList<TestimonialAdminDto> Items { get; set; } = Array.Empty<TestimonialAdminDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
