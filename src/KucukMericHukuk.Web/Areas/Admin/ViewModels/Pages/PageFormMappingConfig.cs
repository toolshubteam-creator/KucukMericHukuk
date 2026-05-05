using KucukMericHukuk.Core.DTOs.Page;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Pages;

public class PageFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PageFormViewModel, PageInputDto>();
        config.NewConfig<PageTranslationFormViewModel, PageTranslationInputDto>();

        config.NewConfig<PageInputDto, PageFormViewModel>();
        config.NewConfig<PageTranslationInputDto, PageTranslationFormViewModel>();

        config.NewConfig<PageAdminDto, PageFormViewModel>();
        config.NewConfig<PageTranslationDto, PageTranslationFormViewModel>();
    }
}
