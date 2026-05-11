using KucukMericHukuk.Core.DTOs.Faq;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Faqs;

public class FaqFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FaqFormViewModel, FaqInputDto>();
        config.NewConfig<FaqTranslationFormViewModel, FaqTranslationInputDto>();

        config.NewConfig<FaqInputDto, FaqFormViewModel>();
        config.NewConfig<FaqTranslationInputDto, FaqTranslationFormViewModel>();

        config.NewConfig<FaqAdminDto, FaqFormViewModel>();
        config.NewConfig<FaqTranslationDto, FaqTranslationFormViewModel>();
    }
}
