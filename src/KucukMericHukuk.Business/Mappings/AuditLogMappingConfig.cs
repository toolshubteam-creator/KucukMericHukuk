using KucukMericHukuk.Core.DTOs.AuditLog;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class AuditLogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Aynı isimli alanlar Mapster tarafından otomatik eşlenir; özel dönüşüm gerekmiyor.
        config.NewConfig<AuditLog, AuditLogListDto>();
        config.NewConfig<AuditLog, AuditLogDetailDto>();
    }
}
