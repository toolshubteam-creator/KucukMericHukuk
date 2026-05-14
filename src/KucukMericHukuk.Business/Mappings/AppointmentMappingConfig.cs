using KucukMericHukuk.Core.DTOs.Appointment;
using KucukMericHukuk.Core.Entities;
using Mapster;

namespace KucukMericHukuk.Business.Mappings;

public class AppointmentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Ayni isimli alanlar Mapster tarafindan otomatik eslenir; ozel donusum gerekmez.
        config.NewConfig<Appointment, AppointmentListDto>();
        config.NewConfig<Appointment, AppointmentAdminDto>();
    }
}
