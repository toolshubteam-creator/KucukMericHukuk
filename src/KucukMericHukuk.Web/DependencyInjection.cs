using Mapster;

namespace KucukMericHukuk.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddWebMappings(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        return services;
    }
}
