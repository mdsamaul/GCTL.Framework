using System.Linq.Expressions;

namespace GCTL.Core.Data;

/// <summary>
/// Single-generic-argument repository contract (uses <see cref="object"/> for
/// the key type — same convention as <see cref="IRepository{T}"/> in this project).
///
/// Your original GCTL_ERP_HRM.zip also had a separate, two-argument
/// <c>IGenericRepository&lt;T, TKey&gt;</c> (with an explicit key type and
/// <c>GenericRepositoryTest&lt;T, TKey&gt;</c> implementation), but that pair
/// was not carried over into this Framework project. This single-argument
/// version is what your pasted <c>OFRApprovalService</c> code needs:
///
///   private readonly IGenericRepository&lt;CF_OperationalFundRequisitionEntry&gt; _repository;
///
/// See GCTL.Core/Data/REPOSITORY_GUIDE.md for when to reach for this one
/// instead of <see cref="IRepository{T}"/>.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    IQueryable<T> All();
    IEnumerable<T> GetAll();
    Task<IEnumerable<T>> GetAllAsync();
    T? GetById(object id);
    Task<T?> GetByIdAsync(object id);

    IQueryable<T> FindBy(Expression<Func<T, bool>> expression);
    Task<IQueryable<T>> FindByAsync(Expression<Func<T, bool>> expression);
    bool Any(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    T Add(T entity);
    Task AddAsync(T entity);
    void Add(IEnumerable<T> entities);
    Task AddRangeAsync(IEnumerable<T> entities);

    void Update(T entity);
    Task UpdateAsync(T entity);
    void Update(IEnumerable<T> entities);
    Task UpdateRangeAsync(IEnumerable<T> entities);

    void Delete(object id);
    void Delete(T entity);
    Task DeleteAsync(object id);
    Task DeleteAsync(T entity);
    void Delete(IEnumerable<T> entities);
    Task DeleteRangeAsync(IEnumerable<T> entities);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    Task DisposeTransactionAsync();

    string GetTableName();
}
