using KucukMericHukuk.Core.DTOs.Attorney;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.DTOs.Service;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Entities.Translations;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class AttorneyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Read: Entity → Frontend List
        config.NewConfig<Attorney, AttorneyListDto>()
            .Map(dest => dest.FullName, src => src.Translations.Select(t => t.FullName).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Title, src => src.Translations.Select(t => t.Title).FirstOrDefault())
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.ShortBio, src => src.Translations.Select(t => t.ShortBio).FirstOrDefault());

        // Read: Entity → Frontend Detail
        config.NewConfig<Attorney, AttorneyDetailDto>()
            .Map(dest => dest.FullName, src => src.Translations.Select(t => t.FullName).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.Title, src => src.Translations.Select(t => t.Title).FirstOrDefault())
            .Map(dest => dest.Slug, src => src.Translations.Select(t => t.Slug).FirstOrDefault() ?? string.Empty)
            .Map(dest => dest.ShortBio, src => src.Translations.Select(t => t.ShortBio).FirstOrDefault())
            .Map(dest => dest.FullBio, src => src.Translations.Select(t => t.FullBio).FirstOrDefault())
            .Map(dest => dest.Education, src => src.Translations.Select(t => t.Education).FirstOrDefault())
            .Map(dest => dest.Publications, src => src.Translations.Select(t => t.Publications).FirstOrDefault())
            .Map(dest => dest.MetaTitle, src => src.Translations.Select(t => t.MetaTitle).FirstOrDefault())
            .Map(dest => dest.MetaDescription, src => src.Translations.Select(t => t.MetaDescription).FirstOrDefault())
            .Map(dest => dest.LanguageCode, src => src.Translations.Select(t => t.LanguageCode).FirstOrDefault() ?? string.Empty);

        // Read: Entity → Admin
        config.NewConfig<Attorney, AttorneyAdminDto>()
            .Map(dest => dest.UserEmail, src => src.User != null ? src.User.Email : null)
            .Map(dest => dest.Services,
                 src => src.Services.Select(s => new LookupDto
                 {
                     Id = s.Id,
                     Name = s.Translations.Select(t => t.Name).FirstOrDefault() ?? string.Empty
                 }));
        config.NewConfig<AttorneyTranslation, AttorneyTranslationDto>();

        // Write
        config.NewConfig<AttorneyInputDto, Attorney>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.DeletedAt!)
            .Ignore(dest => dest.User!)
            .Ignore(dest => dest.Services);

        config.NewConfig<AttorneyTranslationInputDto, AttorneyTranslation>()
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt!);
    }
}
