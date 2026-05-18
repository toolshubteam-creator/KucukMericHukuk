using KucukMericHukuk.Core.DTOs.NotFoundLog;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class NotFoundLogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Aynı isimli alanlar — özel dönüşüm yok.
        config.NewConfig<NotFoundLog, NotFoundLogListDto>();
    }
}
