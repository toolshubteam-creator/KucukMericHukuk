using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class FaqMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Faq, FaqListDto>()
            .Map(dest => dest.Question, src => src.Translations.Select(t => t.Question).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Answer, src => src.Translations.Select(t => t.Answer).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty);
    }
}
