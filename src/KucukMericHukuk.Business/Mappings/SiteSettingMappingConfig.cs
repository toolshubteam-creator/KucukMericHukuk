using KucukMericHukuk.Core.DTOs.SiteSetting;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class SiteSettingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SiteSetting, SiteSettingDto>();
    }
}
