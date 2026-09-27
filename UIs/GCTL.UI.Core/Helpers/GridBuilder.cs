using System;
using System.Collections.Generic;
using System.Linq;
using GCTL.UI.Core.ViewModels;

namespace GCTL.UI.Core.Helpers
{
    /// <summary>
    /// Generic helper utility for constructing dynamic grid models with minimal controller boilerplate.
    /// </summary>
    public static class GridBuilder
    {
        /// <summary>
        /// Builds a fully configured <see cref="DynamicGridModel"/> from a data collection and pagination parameters.
        /// </summary>
        /// <typeparam name="T">Entity or DTO model type.</typeparam>
        /// <param name="data">Paged list of domain items.</param>
        /// <param name="totalRecords">Total count of records across all pages.</param>
        /// <param name="currentPage">Current page index (1-based).</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="headers">List of column header titles.</param>
        /// <param name="rowMapper">Lambda mapping entity object to dictionary cell values.</param>
        /// <param name="ajaxUrl">AJAX URL endpoint for page switching requests.</param>
        /// <param name="primaryKey">Row unique key property name (default: "Id").</param>
        /// <param name="hasCheckbox">Enable multi-select checkboxes.</param>
        /// <param name="actionUrlPattern">Dynamic link URL format containing "{id}" placeholder.</param>
        /// <param name="linkColumn">Column key to wrap with link (defaults to primaryKey).</param>
        /// <param name="gridId">Unique container DOM element ID.</param>
        /// <returns>A populated <see cref="DynamicGridModel"/> ready to render.</returns>
        public static DynamicGridModel BuildGrid<T>(
            IEnumerable<T> data,
            int totalRecords,
            int currentPage,
            int pageSize,
            List<string> headers,
            Func<T, Dictionary<string, object?>> rowMapper,
            string ajaxUrl,
            string primaryKey = "Id",
            bool hasCheckbox = false,
            string? actionUrlPattern = null,
            string? linkColumn = null,
            string gridId = "dynamicGridContainer",
            Dictionary<string, string>? columnAlignments = null,
            string? searchTerm = null,
            Dictionary<string, string>? columnWidths = null,
            string? editClass = null)
        {
            currentPage = Math.Max(1, currentPage);
            pageSize = Math.Max(1, pageSize);
            int totalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;

            var rows = data != null
                ? data.Select(rowMapper).ToList()
                : new List<Dictionary<string, object?>>();

            return new DynamicGridModel
            {
                GridId = gridId,
                Headers = headers ?? new List<string>(),
                Rows = rows,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                SearchTerm = searchTerm,
                AjaxUrl = ajaxUrl,
                PrimaryKey = primaryKey,
                HasCheckbox = hasCheckbox,
                ActionUrlPattern = actionUrlPattern,
                LinkColumn = linkColumn ?? primaryKey,
                EditClass = editClass ?? "js-grid-edit",
                ColumnAlignments = columnAlignments ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                ColumnWidths = columnWidths ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
