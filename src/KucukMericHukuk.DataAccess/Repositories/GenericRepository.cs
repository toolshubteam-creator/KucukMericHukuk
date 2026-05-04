using System.Linq.Expressions;
using KucukMericHukuk.Core.DTOs.Common;
using KucukMericHukuk.Core.Entities;
using KucukMericHukuk.Core.Interfaces;
using KucukMericHukuk.DataAccess.Context;
using KucukMericHukuk.DataAccess.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KucukMericHukuk.DataAccess.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _dbSet.AnyAsync(predicate, ct);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? _dbSet.CountAsync(ct) : _dbSet.CountAsync(predicate, ct);

    public virtual Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        => _dbSet.AsNoTracking().OrderByDescending(e => e.CreatedAt).ToPagedListAsync(pageNumber, pageSize, ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public virtual void Update(T entity) => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public virtual void Delete(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        var now = DateTime.UtcNow;
        var list = entities.ToList();
        foreach (var entity in list)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
        }
        _dbSet.UpdateRange(list);
    }

    public virtual void HardDelete(T entity) => _dbSet.Remove(entity);

    public virtual void HardDeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    public virtual void Restore(T entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        _dbSet.Update(entity);
    }

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();

    public virtual IQueryable<T> QueryWithDeleted() => _dbSet.IgnoreQueryFilters();
}
