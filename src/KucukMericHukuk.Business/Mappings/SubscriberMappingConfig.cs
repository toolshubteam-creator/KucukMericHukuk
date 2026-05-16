using KucukMericHukuk.Core.DTOs.Subscriber;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class SubscriberMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Subscriber, SubscriberListDto>();
    }
}
