using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using DataAccessUnitOfWork = KucukMericHukuk.DataAccess.UnitOfWork.UnitOfWork;

namespace KucukMericHukuk.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        // Generic repository (open generic)
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Özel repository'ler
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAttorneyRepository, AttorneyRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<ITestimonialRepository, TestimonialRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISubscriberRepository, SubscriberRepository>();
        services.AddScoped<INewsletterJobRepository, NewsletterJobRepository>();

        // Faz 7.1: AuditSaveChangesInterceptor — Program.cs'te AddDbContext .AddInterceptors(sp.GetRequiredService<...>()) ile bağlanır.
        services.AddScoped<KucukMericHukuk.DataAccess.Interceptors.AuditSaveChangesInterceptor>();

        // UnitOfWork
        services.AddScoped<IUnitOfWork, DataAccessUnitOfWork>();

        return services;
    }
}
