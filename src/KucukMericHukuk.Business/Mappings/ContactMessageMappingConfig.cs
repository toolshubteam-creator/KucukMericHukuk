using KucukMericHukuk.Core.DTOs.Contact;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class ContactMessageMappingConfig : IRegister
{
    private const int PreviewLength = 120;

    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ContactMessage, ContactMessageListDto>()
            .Map(dest => dest.MessagePreview,
                 src => src.Message.Length > PreviewLength
                     ? src.Message.Substring(0, PreviewLength) + "..."
                     : src.Message);

        config.NewConfig<ContactMessage, ContactMessageAdminDto>();
    }
}
