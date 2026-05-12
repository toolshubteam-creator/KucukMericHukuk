using KucukMericHukuk.Core.DTOs.Testimonial;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Testimonials;

public class TestimonialFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TestimonialFormViewModel, TestimonialInputDto>();
        config.NewConfig<TestimonialTranslationFormViewModel, TestimonialTranslationInputDto>();

        config.NewConfig<TestimonialInputDto, TestimonialFormViewModel>();
        config.NewConfig<TestimonialTranslationInputDto, TestimonialTranslationFormViewModel>();

        config.NewConfig<TestimonialAdminDto, TestimonialFormViewModel>();
        config.NewConfig<TestimonialTranslationDto, TestimonialTranslationFormViewModel>();
    }
}
