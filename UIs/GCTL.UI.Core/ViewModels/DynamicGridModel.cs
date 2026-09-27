using System;
using System.Collections.Generic;

namespace GCTL.UI.Core.ViewModels
{
    /// <summary>
    /// View model for the server-side paginated dynamic grid component.
    /// Controls column headers, dynamic row dictionary mapping, pagination state, and feature flags.
    /// </summary>
    public class DynamicGridModel
    {
        /// <summary>
        /// Unique DOM container identifier for the grid wrapper (e.g., "customerGridContainer").
        /// </summary>
        public string GridId { get; set; } = "dynamicGridContainer";

        /// <summary>
        /// List of header labels to render in the grid standard table head.
        /// </summary>
        public List<string> Headers { get; set; } = new();

        /// <summary>
        /// Dynamic row values where key corresponds to a header label or property name, and value is the cell object.
        /// </summary>
        public List<Dictionary<string, object?>> Rows { get; set; } = new();

        #region Server-Side Pagination Properties

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string AjaxUrl { get; set; } = string.Empty;

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => TotalRecords == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalRecords);

        #endregion

        #region Feature Flags & Configuration

        /// <summary>
        /// Flag to show master "Select All" header checkbox and row selection checkboxes.
        /// </summary>
        public bool HasCheckbox { get; set; } = false;

        /// <summary>
        /// Property key name used for row value lookup (e.g., "Id", "CustomerId").
        /// </summary>
        public string PrimaryKey { get; set; } = "Id";

        /// <summary>
        /// Optional URL template pattern for generating dynamic links (e.g., "/Customer/Details/{id}").
        /// </summary>
        public string? ActionUrlPattern { get; set; }

        /// <summary>
        /// The column name/key to wrap with the ActionUrlPattern link. Defaults to PrimaryKey if not set.
        /// </summary>
        public string? LinkColumn { get; set; }

        /// <summary>
        /// CSS class applied to the dynamic link column anchor tag (defaults to "js-grid-edit").
        /// </summary>
        public string EditClass { get; set; } = "js-grid-edit";

        /// <summary>
        /// Text message rendered when the grid dataset is empty.
        /// </summary>
        public string EmptyText { get; set; } = "No records found matching your criteria.";

        /// <summary>
        /// Column text alignment configuration where key is column header/field name and value is alignment ("center", "left", "right").
        /// </summary>
        public Dictionary<string, string> ColumnAlignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Column width configuration where key is column header/field name (or "Checkbox") and value is width (e.g., "120px", "30%").
        /// </summary>
        public Dictionary<string, string> ColumnWidths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        #endregion
    }
}
