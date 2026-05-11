using KucukMericHukuk.Core.DTOs.Faq;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class FaqMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend List
        config.NewConfig<Faq, FaqListDto>()
            .Map(dest => dest.Question, src => src.Translations.Select(t => t.Question).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Answer, src => src.Translations.Select(t => t.Answer).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty);

        // Read: Entity → Admin
        config.NewConfig<Faq, FaqAdminDto>();
        config.NewConfig<FaqTranslation, FaqTranslationDto>();

        // Write
        config.NewConfig<FaqInputDto, Faq>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!);

        config.NewConfig<FaqTranslationInputDto, FaqTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
