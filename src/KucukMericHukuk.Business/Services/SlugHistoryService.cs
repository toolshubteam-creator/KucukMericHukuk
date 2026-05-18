using KucukMericHukuk.Core.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.Core.Interfaces.Services;

namespace KucukMericHukuk.Business.Services;

/// <summary>
/// Faz 7.4.3a — SlugHistory yazma servisi. 6 entity servisi (Article/Page/Service/
/// Attorney/Category/Tag) Update akışında merge sonrası çağırır.
///
/// Atomic: AddAsync ChangeTracker'a yazar, caller UoW SaveChangesAsync ile birlikte
/// commit eder (translation update + slug history insert tek transaction).
/// </summary>
public class SlugHistoryService : ISlugHistoryService
{
    private readonly IUnitOfWork _uow;

    public SlugHistoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task RecordIfChangedAsync(
        SluggedEntityType entityType,
        int entityId,
        string languageCode,
        string? oldSlug,
        string newSlug,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(oldSlug)) return;
        if (string.Equals(oldSlug, newSlug, StringComparison.Ordinal)) return;

        await _uow.SlugHistories.AddAsync(new SlugHistory
        {
            EntityType = entityType,
            EntityId = entityId,
            LanguageCode = languageCode,
            OldSlug = oldSlug,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
