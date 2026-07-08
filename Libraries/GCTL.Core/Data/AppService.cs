namespace GCTL.Core.Data;

/// <summary>
/// RECONSTRUCTED base service class.
///
/// Your original zip's GCTL.Service project referenced this class
/// (e.g. `AdmissionTypeService : AppService&lt;HmsAdmissionType&gt;, IAdmissionTypeService`)
/// but AppService.cs itself wasn't present in the files you uploaded — it must
/// have lived in a file that wasn't included in the export.
///
/// This version is rebuilt to match the exact calling pattern seen in
/// AdmissionTypeService.cs: GetAll(), Add(entity), Update(entity). If your
/// real AppService had more members (validation hooks, logging, current-user
/// context, etc.), paste that file's content and I'll merge it in exactly.
/// </summary>
public abstract class AppService<T> where T : class
{
    protected readonly IRepository<T> Repository;

    protected AppService(IRepository<T> repository)
    {
        Repository = repository;
    }

    protected virtual List<T> GetAll()
    {
        return Repository.GetAll().ToList();
    }

    protected virtual T? GetById(object id)
    {
        return Repository.GetById(id);
    }

    protected virtual T Add(T entity)
    {
        return Repository.Add(entity);
    }

    protected virtual void Update(T entity)
    {
        Repository.Update(entity);
    }

    protected virtual void Delete(T entity)
    {
        Repository.Delete(entity);
    }

    protected virtual void Delete(object id)
    {
        Repository.Delete(id);
    }

    protected virtual IQueryable<T> Query()
    {
        return Repository.All();
    }
}
