using System.Linq.Expressions;

namespace GCTL.Core.Data;

/// <summary>
/// Generic repository contract. Restored from your original GCTL.Core.Data.IRepository.
/// </summary>
public interface IRepository<T> where T : class
{
    T Add(T entity);
    void Add(IEnumerable<T> entities);
    IQueryable<T> All();
    IQueryable<T> FindBy(Expression<Func<T, bool>> expression);
    IEnumerable<T> GetAll();
    T? GetById(object id);
    void Update(T entity);
    void Update(IEnumerable<T> entities);
    void Delete(object id);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);

    // async
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<IQueryable<T>> AllAsync();
    Task<IQueryable<T>> FindByAsync(Expression<Func<T, bool>> expression);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(object id);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task DeleteAsync(object id);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task DisposeTransactionAsync();
    string GetTableName();

    Task<GCTL.Core.ViewModels.DeleteHistories.DependencyCheckResult> CheckDependenciesAsync(
        string keyColumnName,
        List<string> keyValues,
        List<string>? alternateColumns = null,
        List<string>? ignoreTables = null);

    Task<(bool Success, string Message)> DeleteWithAuditAsync(
        string keyColumnName,
        List<string> keyValues,
        List<string>? alternateColumns = null,
        List<string>? ignoreTables = null,
        GCTL.Core.ViewModels.DeleteHistories.DeleteHistoryViewModel? auditInfo = null);
}
