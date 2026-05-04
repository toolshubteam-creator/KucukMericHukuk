using KucukMericHukuk.Core.DTOs.Page;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class PageMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend Detail (translation flatten)
        config.NewConfig<Page, PageDetailDto>()
            .Map(dest => dest.Title, src => src.Translations.Select(t => t.Title).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Content, src => src.Translations.Select(t => t.Content).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.MetaTitle, src => src.Translations.Select(t => t.MetaTitle).FirstOrDefault())
            .Map(dest => dest.MetaDescription, src => src.Translations.Select(t => t.MetaDescription).FirstOrDefault())
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty);

        // Read: Entity → Admin
        config.NewConfig<Page, PageAdminDto>();
        config.NewConfig<PageTranslation, PageTranslationDto>();

        // Write: Input → Entity
        config.NewConfig<PageInputDto, Page>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!);

        config.NewConfig<PageTranslationInputDto, PageTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
