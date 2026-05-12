using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Repositories;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace KucukMericHukuk.DataAccess.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IPageRepository? _pages;
    private IServiceRepository? _services;
    private IAttorneyRepository? _attorneys;
    private ICategoryRepository? _categories;
    private ITagRepository? _tags;
    private IArticleRepository? _articles;
    private IMediaFileRepository? _mediaFiles;
    private IFaqRepository? _faqs;
    private IContactMessageRepository? _contactMessages;
    private ISiteSettingRepository? _siteSettings;
    private ITestimonialRepository? _testimonials;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IPageRepository Pages => _pages ??= new PageRepository(_context);
    public IServiceRepository Services => _services ??= new ServiceRepository(_context);
    public IAttorneyRepository Attorneys => _attorneys ??= new AttorneyRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public ITagRepository Tags => _tags ??= new TagRepository(_context);
    public IArticleRepository Articles => _articles ??= new ArticleRepository(_context);
    public IMediaFileRepository MediaFiles => _mediaFiles ??= new MediaFileRepository(_context);
    public IFaqRepository Faqs => _faqs ??= new FaqRepository(_context);
    public IContactMessageRepository ContactMessages => _contactMessages ??= new ContactMessageRepository(_context);
    public ISiteSettingRepository SiteSettings => _siteSettings ??= new SiteSettingRepository(_context);
    public ITestimonialRepository Testimonials => _testimonials ??= new TestimonialRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is not null) return;
        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null) return;
        try
        {
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null) return;
        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
