using System.Reflection;
using FluentValidation;
using KucukMericHukuk.Business.Services;
using KucukMericHukuk.Core.Interfaces.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace KucukMericHukuk.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Mapster: tüm IRegister sınıflarını assembly'den tara
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // FluentValidation: tüm IValidator<T>'leri assembly'den tara.
        // Auto MVC validation YOK — service'lerde manuel ValidateAsync.
        services.AddValidatorsFromAssembly(
            Assembly.GetExecutingAssembly(),
            ServiceLifetime.Scoped,
            includeInternalTypes: false);

        // Service'ler
        services.AddScoped<ISlugService, SlugService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAttorneyService, AttorneyService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IFaqService, FaqService>();
        services.AddScoped<IContactMessageService, ContactMessageService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<ITestimonialService, TestimonialService>();

        // SiteSettingsService memory cache kullanır (5dk TTL, update sonrası InvalidateCache).
        services.AddMemoryCache();

        return services;
    }
}
