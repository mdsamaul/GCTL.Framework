namespace GCTL.UI.Core.ViewModels
{
    public class GridColumn
    {
        public string? Data { get; set; }            // maps to DataTables columns.data
        public string? Header { get; set; }
        public string? ShortHeader { get; set; }      // Short / compact header shown on < 1600px screens
        public string? ShortName { get => ShortHeader; set => ShortHeader = value; } // Alias for ShortHeader
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
        public bool IsCopyable { get; set; } = false;          // 1-click copy button with mini tooltip
        public bool TabularNumber { get; set; } = false;       // tabular monospaced numbers alignment
        public bool IsTruncate { get; set; } = false;          // single-line text ellipsis with title tooltip

        // Strips any HTML tags from Header for clean tooltip display
        public string CleanHeader => !string.IsNullOrEmpty(Header)
            ? System.Text.RegularExpressions.Regex.Replace(Header, "<.*?>", string.Empty).Trim()
            : string.Empty;

        public string CssClass => Align switch
        {
            "left" => "text-start",
            "right" => "text-end",
            _ => "text-center"
        } + (IsWrapable ? "" : " text-nowrap")
          + (TabularNumber ? " tabular-number" : "");
    }

    public class GridModel
    {
        public string GridId { get; set; } = "customGrid";
        public List<GridColumn> Columns { get; set; } = new();
        public int CompactBreakpoint { get; set; } = 1600; // Breakpoint in px below which ShortHeader is shown

        // Selection
        public bool ShowCheckbox { get; set; } = false;
        public string CheckColumnData { get; set; } = "id";   // field DataTables reads for the checkbox value
        public string CheckboxClass { get; set; } = "checkbox";
        public string CheckAllId { get; set; } = "checkAll";
        public bool DisableCheckAll { get; set; } = false;
        public bool ShowCheckAll { get; set; } = true;
        public bool UseRadio { get; set; } = false;             // radio instead of checkbox (single-select rows)
        public string RadioName { get; set; } = "rowSelect";
        public bool SelectRowHighlight { get; set; } = false;   // soft blue highlight on selected rows

        // Styling
        public bool Striped { get; set; } = true;              // custom light gray on odd rows
        public bool Bordered { get; set; } = true;
        public bool Hover { get; set; } = true;
        public bool Compact { get; set; } = true;
        public bool CustomGridWrapper { get; set; } = true;
        public bool StickyHeader { get; set; } = false;
        public string ContainerCssClass { get; set; } = "";
        public string? FooterHtml { get; set; }

        // Features & Behavior
        public bool ShowRowNumbers { get; set; } = false;      // serial number (#) column
        public bool ShowRefreshButton { get; set; } = false;   // AJAX refresh button in toolbar
        public bool EnableDoubleClickEdit { get; set; } = false;// double-click row to edit (ignores buttons/inputs)
        public bool HasSubRow { get; set; } = false;           // expandable sub-row accordion
        public string? SubRowRenderFn { get; set; }            // JS function name for rendering sub-row details
        public bool ShowActionMenu { get; set; } = false;      // 3-dots action dropdown column
        public bool ShowSearchHighlight { get; set; } = true;  // highlights search query in #FEFFE5
        public bool ShowSkeletonLoading { get; set; } = true;  // shimmer placeholder rows during AJAX load
        public bool ShowPagination { get; set; } = true;

        // Data & AJAX
        public bool ServerSide { get; set; } = true;
        public string? AjaxUrl { get; set; }
        public string AjaxMethod { get; set; } = "GET";
        public int[] PageLengths { get; set; } = { 10, 25, 50, 100, -1 };
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

        // Card Header Action Button
        public string? AddButtonUrl { get; set; }
        public string AddButtonText { get; set; } = "Add New";
        public string? AddButtonId { get; set; }
    }
}