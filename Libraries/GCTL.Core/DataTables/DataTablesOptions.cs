using System.ComponentModel;

namespace GCTL.Core.DataTables;

public class Search
{
    public string? Value { get; set; }
    public string? Regex { get; set; }
}

public class Column
{
    public string? Data { get; set; }
    public string? Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public Search? Search { get; set; }
}

public class Order
{
    public int Column { get; set; }
    public string? Dir { get; set; }

    public ListSortDirection GetSortDirection()
    {
        if (string.IsNullOrWhiteSpace(Dir))
        {
            return ListSortDirection.Ascending;
        }

        return Dir.Equals("asc", StringComparison.CurrentCultureIgnoreCase)
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;
    }
}

public class PaginationParams
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
}

public class SortOptions
{
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
}

public class DataTablesOptions
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public List<Column> Columns { get; set; } = new();
    public Search? Search { get; set; }
    public List<Order> Order { get; set; } = new();

    public IList<Column> GetSearchableColums() => Columns.Where(c => c.Searchable).ToList();
    public IList<Column> GetSortableColumns() => Columns.Where(c => c.Orderable).ToList();
}

public class DataTablesOptions<F> : DataTablesOptions where F : class
{
    public F? Filter { get; set; }
}
