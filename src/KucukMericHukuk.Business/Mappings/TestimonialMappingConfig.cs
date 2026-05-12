using KucukMericHukuk.Core.DTOs.Testimonial;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class TestimonialMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend Public DTO (translation flatten)
        config.NewConfig<Testimonial, TestimonialListDto>()
            .Map(dest => dest.Content,
                src => src.Translations.Select(t => t.Content).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.LanguageCode,
                src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty);

        // Read: Entity → Admin DTO (translations list)
        config.NewConfig<Testimonial, TestimonialAdminDto>();
        config.NewConfig<TestimonialTranslation, TestimonialTranslationDto>();

        // Write: Input → Entity (audit alanları Ignore)
        config.NewConfig<TestimonialInputDto, Testimonial>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!);

        config.NewConfig<TestimonialTranslationInputDto, TestimonialTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
