namespace GCTL.UI.Core.ViewModels
{
    public class GridColumn
    {
        public string? Data { get; set; }            // maps to DataTables columns.data
        public string? Header { get; set; }
        public string? HeaderBangla { get; set; }
        public string? Width { get; set; }
        public bool Sortable { get; set; } = true;
        public bool Searchable { get; set; } = true;
        public bool Visible { get; set; } = true;
        public string? RenderFn { get; set; }         // name of a JS function registered in window.gctlRenderers
        public string Align { get; set; } = "center";  // left | center | right
        public bool IsEditLink { get; set; } = false;
        public string? EditSelectorClass { get; set; }   // CSS class applied to the <a>, e.g. "edit-job-btn"
        public string? EditLabel { get; set; }
        public string? SortField { get; set; }      // IF need to sort based on another column, not base column
        public string? IdField { get; set; }             // Field name to use for data-id attribute (defaults to Data if null)
        public List<string>? DataAttributes { get; set; } // Additional data-* attribute field names to attach to edit link <a>
        public bool IsWrapable { get; set; } = true;
        public string CssClass => Align switch
        {
            "left" => "text-start",
            "right" => "text-end",
            _ => "text-center"
        } + (IsWrapable ? "" : " text-nowrap");
    }

    public class GridModel
    {
        public string GridId { get; set; } = "customGrid";
        public List<GridColumn> Columns { get; set; } = new();

        // Selection
        public bool ShowCheckbox { get; set; } = false;
        public string CheckColumnData { get; set; } = "id";   // field DataTables reads for the checkbox value
        public string CheckboxClass { get; set; } = "checkbox";
        public string CheckAllId { get; set; } = "checkAll";
        public bool DisableCheckAll { get; set; } = false;
        public bool UseRadio { get; set; } = false;             // radio instead of checkbox (single-select rows)
        public string RadioName { get; set; } = "rowSelect";


        // Styling
        public bool Striped { get; set; } = false;
        public bool Bordered { get; set; } = true;
        public bool Hover { get; set; } = true;
        public bool Compact { get; set; } = true;
        public string ContainerCssClass { get; set; } = "";

        // Behavior
        public bool ServerSide { get; set; } = true;
        public string? AjaxUrl { get; set; }
        public string AjaxMethod { get; set; } = "GET";
        public int[] PageLengths { get; set; } = { 10, 25, 50, 100 };
        public int DefaultPageLength { get; set; } = 10;
        public string? DefaultSortColumn { get; set; }
        public string DefaultSortDirection { get; set; } = "desc";
        public bool ShowSearchBox { get; set; } = true;
        public string EmptyStateText { get; set; } = "No records found.";

        // Optional Card / Collapsible Container
        public bool CardWrapper { get; set; } = false;
        public string? CardTitle { get; set; }
        public string? IconClass { get; set; }
        public bool IsCollapsible { get; set; } = false;
        public bool IsCollapsedDefault { get; set; } = false;
        public bool CustomGridWrapper { get; set; } = true;
    }
}