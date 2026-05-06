using FluentValidation;
using Mapster;

namespace KucukMericHukuk.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddWebMappings(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
        return services;
    }

    // Form-level FluentValidation validator'ları (PageFormViewModelValidator, vb.)
    // FluentValidation client-side adapter bunları okuyup HTML data-val-* attribute'larına
    // çevirir. Server-side validation hâlâ service katmanında manuel
    // (Input DTO validator'ları) — duplicate önlenmiş olur.
    public static IServiceCollection AddWebValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
