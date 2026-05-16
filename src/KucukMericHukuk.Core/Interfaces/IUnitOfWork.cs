using KucukMericHukuk.Core.Interfaces.Repositories;

namespace KucukMericHukuk.Core.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IPageRepository Pages { get; }
    IServiceRepository Services { get; }
    IAttorneyRepository Attorneys { get; }
    ICategoryRepository Categories { get; }
    ITagRepository Tags { get; }
    IArticleRepository Articles { get; }
    IMediaFileRepository MediaFiles { get; }
    IFaqRepository Faqs { get; }
    IContactMessageRepository ContactMessages { get; }
    ISiteSettingRepository SiteSettings { get; }
    ITestimonialRepository Testimonials { get; }
    IAppointmentRepository Appointments { get; }
    IAuditLogRepository AuditLogs { get; }
    ISubscriberRepository Subscribers { get; }
    INewsletterJobRepository NewsletterJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
