using KucukMericHukuk.Core.DTOs.Service;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Services;

public class ServiceFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ServiceFormViewModel, ServiceInputDto>();
        config.NewConfig<ServiceTranslationFormViewModel, ServiceTranslationInputDto>();

        config.NewConfig<ServiceInputDto, ServiceFormViewModel>();
        config.NewConfig<ServiceTranslationInputDto, ServiceTranslationFormViewModel>();

        config.NewConfig<ServiceAdminDto, ServiceFormViewModel>()
            .Map(dest => dest.AttorneyIds,
                 src => src.Attorneys != null
                     ? src.Attorneys.Select(a => a.Id).ToList()
                     : new List<int>());

        config.NewConfig<ServiceTranslationDto, ServiceTranslationFormViewModel>();
    }
}
