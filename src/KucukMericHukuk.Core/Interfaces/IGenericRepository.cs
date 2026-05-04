using System.Linq.Expressions;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;

namespace KucukMericHukuk.Core.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    // Read
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    // Pagination shortcut
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    // Write
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);

    // Delete (soft / hard)
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    void HardDelete(T entity);
    void HardDeleteRange(IEnumerable<T> entities);

    // Restore
    void Restore(T entity);

    // Query escape — sadece DataAccess katmanı kullanmalı, service'e leak ETMEMELİ
    IQueryable<T> Query();
    IQueryable<T> QueryWithDeleted();
}
