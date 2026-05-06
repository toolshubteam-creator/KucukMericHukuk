using KucukMericHukuk.Core.DTOs.Attorney;
using Mapster;

namespace KucukMericHukuk.Web.Areas.Admin.ViewModels.Attorneys;

public class AttorneyFormMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AttorneyFormViewModel, AttorneyInputDto>();
        config.NewConfig<AttorneyTranslationFormViewModel, AttorneyTranslationInputDto>();

        config.NewConfig<AttorneyInputDto, AttorneyFormViewModel>();
        config.NewConfig<AttorneyTranslationInputDto, AttorneyTranslationFormViewModel>();

        config.NewConfig<AttorneyAdminDto, AttorneyFormViewModel>()
            .Map(dest => dest.ServiceIds,
                 src => src.Services != null
                     ? src.Services.Select(s => s.Id).ToList()
                     : new List<int>());

        config.NewConfig<AttorneyTranslationDto, AttorneyTranslationFormViewModel>();
    }
}
