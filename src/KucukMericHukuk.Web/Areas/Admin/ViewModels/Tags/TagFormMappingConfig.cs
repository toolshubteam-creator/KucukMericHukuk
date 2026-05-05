using KucukMericHukuk.Core.DTOs.Tag;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Tags;

public class TagFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TagFormViewModel, TagInputDto>();
        config.NewConfig<TagTranslationFormViewModel, TagTranslationInputDto>();

        config.NewConfig<TagInputDto, TagFormViewModel>();
        config.NewConfig<TagTranslationInputDto, TagTranslationFormViewModel>();

        config.NewConfig<TagAdminDto, TagFormViewModel>();
        config.NewConfig<TagTranslationDto, TagTranslationFormViewModel>();
    }
}
