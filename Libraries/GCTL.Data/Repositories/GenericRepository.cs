using GCTL.Core.Data;
using GCTL.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GCTL.Data.Repositories;

/// <summary>
/// Generic repository, restored from your original GCTL.Data.GenericRepository.
/// Retargeted from the old GCTL_ERP_DB_DatapathContext to the new
/// GctlHrmContext (the placeholder that EF Power Tools will fill in once you
/// reverse-engineer your real database). Everything else works identically
/// to your original implementation.
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    private readonly GctlHrmContext _context;

    public GenericRepository(GctlHrmContext context)
    {
        _context = context;
    }

    public IQueryable<T> All() => _context.Set<T>();

    public IEnumerable<T> GetAll() => _context.Set<T>().ToList();

    public IQueryable<T> FindBy(Expression<Func<T, bool>> expression) =>
        _context.Set<T>().Where(expression);

    public IQueryable<T> Get(FormattableString sql) =>
        _context.Set<T>().FromSqlInterpolated(sql);

    public T? GetById(object id) => _context.Set<T>().Find(id);

    public T Add(T entity)
    {
        _context.Set<T>().Add(entity);
        _context.SaveChanges();
        return entity;
    }

    public void Add(IEnumerable<T> entities)
    {
        _context.Set<T>().AddRange(entities);
        _context.SaveChanges();
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
        _context.SaveChanges();
    }

    public void Update(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        _context.SaveChanges();
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

    public void Delete(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        _context.SaveChanges();
    }

    // async
    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<IQueryable<T>> AllAsync() => await Task.FromResult(_context.Set<T>());

    public async Task<IQueryable<T>> FindByAsync(Expression<Func<T, bool>> expression) =>
        await Task.FromResult(_context.Set<T>().Where(expression));

    public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();

    public async Task<T?> GetByIdAsync(object id) => await _context.Set<T>().FindAsync(id);

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
        await _context.SaveChangesAsync();
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
