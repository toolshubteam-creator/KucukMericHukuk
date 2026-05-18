using KucukMericHukuk.Core.DTOs.Redirect;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class RedirectMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Redirect, RedirectListDto>();
    }
}
