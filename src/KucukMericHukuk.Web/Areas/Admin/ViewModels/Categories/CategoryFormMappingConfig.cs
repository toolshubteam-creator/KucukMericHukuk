using KucukMericHukuk.Core.DTOs.Category;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Categories;

public class CategoryFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CategoryFormViewModel, CategoryInputDto>();
        config.NewConfig<CategoryTranslationFormViewModel, CategoryTranslationInputDto>();

        config.NewConfig<CategoryInputDto, CategoryFormViewModel>();
        config.NewConfig<CategoryTranslationInputDto, CategoryTranslationFormViewModel>();

        config.NewConfig<CategoryAdminDto, CategoryFormViewModel>();
        config.NewConfig<CategoryTranslationDto, CategoryTranslationFormViewModel>();
    }
}
