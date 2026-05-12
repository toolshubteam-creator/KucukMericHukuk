using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Testimonial;

namespace KucukMericHukuk.Web.ViewModels.Testimonial;

public class TestimonialListViewModel
{
    public PagedResult<TestimonialListDto> Testimonials { get; init; } = new();
}
