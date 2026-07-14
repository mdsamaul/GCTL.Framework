using GCTL.Core.Data;
using GCTL.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GCTL.Data.Repositories;

/// <summary>
/// Implements the single-generic-argument <see cref="IGenericRepository{T}"/>.
/// Sits alongside <see cref="GenericRepository{T}"/> (which implements
/// <see cref="IRepository{T}"/>) — same underlying DbContext, same EF Core
/// calls, different interface surface. See
/// GCTL.Core/Data/REPOSITORY_GUIDE.md for which one to inject where.
/// </summary>
public class GcGenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly GctlHrmContext _context;

    public GcGenericRepository(GctlHrmContext context)
    {
        _context = context;
    }

    public IQueryable<T> All() => _context.Set<T>();

    public IEnumerable<T> GetAll() => _context.Set<T>().ToList();

    public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();

    public T? GetById(object id) => _context.Set<T>().Find(id);

    public async Task<T?> GetByIdAsync(object id) => await _context.Set<T>().FindAsync(id);

    public IQueryable<T> FindBy(Expression<Func<T, bool>> expression) =>
        _context.Set<T>().Where(expression);

    public async Task<IQueryable<T>> FindByAsync(Expression<Func<T, bool>> expression) =>
        await Task.FromResult(_context.Set<T>().Where(expression));

    public bool Any(Expression<Func<T, bool>> predicate) => _context.Set<T>().Any(predicate);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) =>
        await _context.Set<T>().AnyAsync(predicate);

    public T Add(T entity)
    {
        _context.Set<T>().Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public void Add(IEnumerable<T> entities)
    {
        _context.Set<T>().AddRange(entities);
        _context.SaveChanges();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
        _context.SaveChanges();
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public void Update(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        _context.SaveChanges();
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        await _context.SaveChangesAsync();
    }

    public void Delete(object id)
    {
        var entity = GetById(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            _context.SaveChanges();
        }
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
        _context.SaveChanges();
    }

    public async Task DeleteAsync(object id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public void Delete(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        _context.SaveChanges();
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.CommitAsync();
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
        }
    }

    public async Task DisposeTransactionAsync()
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.DisposeAsync();
        }
    }

    public string GetTableName()
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        return entityType?.GetTableName() ?? typeof(T).Name;
    }
}
