using KucukMericHukuk.Core.DTOs.Tag;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class TagMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → List
        config.NewConfig<Tag, TagListDto>()
            .Map(dest => dest.Name, src => src.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty);

        // Read: Entity → Admin
        config.NewConfig<Tag, TagAdminDto>()
            .Map(dest => dest.ArticleCount, src => src.Articles.Count);
        config.NewConfig<TagTranslation, TagTranslationDto>();

        // Write
        config.NewConfig<TagInputDto, Tag>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!)
            .Ignore(dest => dest.Articles);

        config.NewConfig<TagTranslationInputDto, TagTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
