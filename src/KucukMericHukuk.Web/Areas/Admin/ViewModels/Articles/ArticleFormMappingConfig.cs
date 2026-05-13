using KucukMericHukuk.Core.DTOs.Article;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Articles;

public class ArticleFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ArticleFormViewModel, ArticleInputDto>()
            .Ignore(dest => dest.AuthorId!)
            .Ignore(dest => dest.EditorId!);
        config.NewConfig<ArticleTranslationFormViewModel, ArticleTranslationInputDto>();
    }
}
