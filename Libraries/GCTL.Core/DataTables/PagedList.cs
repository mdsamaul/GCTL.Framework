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

#region new Pagination
[AttributeUsage(AttributeTargets.Property)]
public class SearchableAttribute : Attribute { }
public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public string? SortOrder { get; set; } = "desc";
}

public class PaginationResponse<T>
{
    public IEnumerable<T>? Data { get; set; }
    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }
    public PaginationInfo2? PaginationInfo { get; set; }

    public class PaginationInfo2
    {
        public int StartItem { get; set; }
        public int EndItem { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
#endregion