(function ($) {
    $.gctlGrid = {
        init: function (options) {
            var settings = $.extend({
                gridSelector: null,
                ajaxUrl: null,
                ajaxType: "GET",
                ajaxData: null,              // function(d) or plain object — extra request params
                serverSide: false,           // set true for backend-paginated grids
                columns: [],
                checkAllSelector: null,
                checkBoxClass: "form-check-input",
                order: [[1, "desc"]],
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'All']],
                columnDefs: [{ targets: [0], orderable: false }],
                extraOptions: {},
                onLoaded: function (json) { }
            }, options);

            if (!settings.gridSelector || !$(settings.gridSelector).length) {
                return null;
            }
            if (!settings.ajaxUrl) {
                return null;
            }

            var dtOptions = $.extend(true, {
                serverSide: settings.serverSide,
                ajax: {
                    url: settings.ajaxUrl,
                    type: settings.ajaxType,
                    data: function (d) {
                        if (settings.serverSide) {
                            d.pageNumber = Math.floor(d.start / d.length) + 1;
                            d.pageSize = d.length;

                            if (d.order && d.order.length && d.columns && d.columns[d.order[0].column]) {
                                d.sortColumn = d.columns[d.order[0].column].data;
                                d.sortOrder = d.order[0].dir;
                            }

                            d.searchTerm = d.search ? d.search.value : "";
                        }
                        if (typeof settings.ajaxData === "function") {
                            settings.ajaxData(d);
                        } else if (settings.ajaxData) {
                            $.extend(d, settings.ajaxData);
                        }
                    },
                    dataSrc: function (json) {
                        if (json.recordsTotal === undefined && json.totalCount !== undefined) {
                            json.recordsTotal = json.totalCount;
                        }
                        if (json.recordsFiltered === undefined && json.filteredCount !== undefined) {
                            json.recordsFiltered = json.filteredCount;
                        }
                        return json.data !== undefined ? json.data : json;
                    }
                },
                columnDefs: [{ targets: '_all', orderSequence: ['asc', 'desc'] }].concat(settings.columnDefs),
                columns: settings.columns,
                lengthChange: true,
                pageLength: settings.pageLength,
                lengthMenu: settings.lengthMenu,
                order: settings.order,
                language: {
                    emptyTable: "No records available",
                    zeroRecords: "No matching records found",
                    processing: "<span class='spinner-border spinner-border-sm me-1'></span> Loading...",
                    search: "_INPUT_",
                    searchPlaceholder: "Search...",
                    lengthMenu: "Show _MENU_ entries",
                    info: "Showing _START_ to _END_ of _TOTAL_ entries",
                    infoEmpty: "Showing 0 to 0 of 0 entries",
                    infoFiltered: "(filtered from _MAX_ total entries)",
                    paginate: {
                        first: "First",
                        last: "Last",
                        next: "Next",
                        previous: "Previous"
                    }
                },
                destroy: true
            }, settings.extraOptions);

            var dataTable = $(settings.gridSelector).DataTable(dtOptions);

            $(settings.gridSelector).off('xhr.dt.gctlGrid').on('xhr.dt.gctlGrid', function (e, s, json, xhr) {
                settings.onLoaded(json);
            });

            if (settings.checkAllSelector) {
                $("body").off("click.gctlGrid", settings.checkAllSelector)
                    .on("click.gctlGrid", settings.checkAllSelector, function () {
                        $(settings.gridSelector + " ." + settings.checkBoxClass)
                            .prop('checked', $(this).prop('checked'));
                    });
            }

            return dataTable;
        },

        // NEW: builds dtOptions from the GridModel JSON embedded on the table
        // via data-grid-config in _CustomGrid.cshtml, then delegates to init().
        initFromModel: function (overrides) {
            overrides = overrides || {};
            var $table = overrides.gridSelector
                ? $(overrides.gridSelector)
                : $('#' + overrides.gridId);

            if (!$table.length) {
                return null;
            }

            var config = $table.data('grid-config');
            if (!config) {
                return null;
            }

            var columns = [];
            var hasSelectCol = config.showCheckbox || config.useRadio;

            if (hasSelectCol) {
                columns.push({
                    data: config.useRadio ? null : config.checkColumnData,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    render: function (data, type, row) {
                        var cls = config.checkboxClass || 'form-check-input checkbox';
                        return config.useRadio
                            ? '<input type="radio" class="' + cls + '" name="' +
                            (config.radioName || 'rowSelect') + '" value="' +
                            (row[config.checkColumnData] != null ? row[config.checkColumnData] : '') + '">'
                            : '<input type="checkbox" class="' + cls + '" value="' +
                            (data != null ? data : '') + '">';
                    }
                });
            }

            var visibleCols = (config.columns || []).filter(function (c) {
                return c.visible !== false;
            });

            var sortColIndex = hasSelectCol ? 1 : 0;

            visibleCols.forEach(function (c, i) {
                var idx = i + (hasSelectCol ? 1 : 0);
                if (config.defaultSortColumn && c.data === config.defaultSortColumn) {
                    sortColIndex = idx;
                }

                var renderFn;
                
                if (c.isEditLink) {
                    renderFn = function (data, type, row) {
                        var editClass = c.editSelectorClass || 'edit-link';
                        var label = c.editLabel != null ? c.editLabel : (data != null ? data : '');
                        return $.gctlGrid.editLinkColumn
                            ? '<a class="btn ' + editClass + '" data-id="' + (row[c.data] != null ? row[c.data] : '') + '"><i>' + label + '</i></a>'
                            : '';
                    };
                } else if (c.renderFn && window.gctlRenderers && window.gctlRenderers[c.renderFn]) {
                    renderFn = window.gctlRenderers[c.renderFn];
                }

                columns.push({
                    data: c.data,
                    className: c.cssClass || "text-center",
                    orderable: c.sortable !== false,
                    searchable: c.searchable !== false,
                    render: renderFn
                });
            });

            var pageLengths = config.pageLengths || [10, 25, 50, 100];

            var builtSettings = {
                gridSelector: '#' + config.gridId,
                ajaxUrl: config.ajaxUrl,
                ajaxType: config.ajaxMethod || "GET",
                serverSide: !!config.serverSide,
                columns: columns,
                checkBoxClass: config.checkboxClass || 'form-check-input checkbox', 
                checkAllSelector: (config.showCheckbox && !config.disableCheckAll && config.checkAllId)
                    ? ('#' + config.checkAllId)
                    : null,
                order: [[sortColIndex, config.defaultSortDirection || "desc"]],
                pageLength: config.defaultPageLength || 10,
                lengthMenu: [pageLengths, pageLengths],
                columnDefs: hasSelectCol ? [{ targets: [0], orderable: false }] : []
            };

            return this.init($.extend(true, builtSettings, overrides));
        },

        checkboxColumn: function (idField, checkBoxClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "2%",
                orderable: false,
                "render": function (data, type, row) {
                    return `<input type="checkbox" class="${checkBoxClass || 'checkBox'}" value="${row[idField] || ''}" />`;
                }
            };
        },

        editLinkColumn: function (idField, editSelectorClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "7%",
                "render": function (data, type, row) {
                    return `<a class='btn ${editSelectorClass}' data-id='${row[idField] || ''}'><i>${data || ''}</i></a>`;
                }
            };
        }
    };

    // Registry that GridColumn.RenderFn names are resolved against.
    // Populate on each page before calling initFromModel, e.g.:
    //   window.gctlRenderers.formatDate = function (data) { return formatDate(data); };
    window.gctlRenderers = window.gctlRenderers || {};

}(jQuery));


// function loadDeductionTypes(baseUrl, gridSelector) {
//     return $.gctlGrid.init({
//         gridSelector: gridSelector,
//         ajaxUrl: baseUrl + "/grid",
//         checkAllSelector: "#deduction-type-check-all",
//         columns: [
//             $.gctlGrid.checkboxColumn("tc"),
//             $.gctlGrid.editLinkColumn("deductionTypeId", "js-deduction-type-edit"),
//             { "data": "deductionType", "autowidth": true, "className": "text-center" },
//             { "data": "shortName", "autowidth": true, "className": "text-center" }
//         ]
//     });
// }