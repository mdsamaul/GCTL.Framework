namespace GCTL.Core.DataTables;

/// <summary>
/// Restored from your original GCTL.Core.DataTables.PagedList, unchanged logic.
/// </summary>
public class PagedList<T> : List<T>
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int PagesCount { get; set; }

    private PagedList(IQueryable<T> query, DataTablesOptions paginationData)
    {
        TotalCount = query.Count();
        PageNumber = (int)Math.Ceiling(paginationData.Start / (double)paginationData.Length) + 1;
        PageSize = paginationData.Length;
        PagesCount = PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        AddRange(query.Skip(paginationData.Start).Take(PageSize).ToList());
    }

    public static PagedList<T> Create(IQueryable<T> source, DataTablesOptions paginationData) =>
        new(source, paginationData);
}

public static class PagedListExtensions
{
    public static PagedList<T> ToPagedList<T>(this IQueryable<T> query, DataTablesOptions options) =>
        PagedList<T>.Create(query, options);
}
