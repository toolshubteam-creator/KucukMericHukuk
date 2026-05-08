using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class ServiceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend List
        config.NewConfig<Service, ServiceListDto>()
            .Map(dest => dest.Name, src => src.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.ShortDescription, src => src.Translations.Select(t => t.ShortDescription).FirstOrDefault());

        // Read: Entity → Frontend Detail
        config.NewConfig<Service, ServiceDetailDto>()
            .Map(dest => dest.Name, src => src.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.ShortDescription, src => src.Translations.Select(t => t.ShortDescription).FirstOrDefault())
            .Map(dest => dest.FullDescription, src => src.Translations.Select(t => t.FullDescription).FirstOrDefault())
            .Map(dest => dest.MetaTitle, src => src.Translations.Select(t => t.MetaTitle).FirstOrDefault())
            .Map(dest => dest.MetaDescription, src => src.Translations.Select(t => t.MetaDescription).FirstOrDefault())
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Attorneys, src => src.Attorneys);

        // Read: Entity → Admin
        config.NewConfig<Service, ServiceAdminDto>()
            .Map(dest => dest.Attorneys,
                 src => src.Attorneys.Select(a => new LookupDto
                 {
                     Id = a.Id,
                     Name = a.Translations.Select(t => t.FullName).FirstOrDefault() ?? string.Empty
                 }));
        config.NewConfig<ServiceTranslation, ServiceTranslationDto>();

        // Write
        config.NewConfig<ServiceInputDto, Service>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!)
            .Ignore(dest => dest.Attorneys);

        config.NewConfig<ServiceTranslationInputDto, ServiceTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
