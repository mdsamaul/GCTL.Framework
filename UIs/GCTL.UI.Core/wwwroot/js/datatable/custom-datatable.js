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
                autoWidth: false,
                serverSide: settings.serverSide,
                ajax: {
                    url: settings.ajaxUrl,
                    type: settings.ajaxType,
                    data: function (d) {
                        if (settings.serverSide) {
                            d.pageNumber = Math.floor(d.start / d.length) + 1;
                            d.pageSize = d.length;

                            if (d.order && d.order.length) {
                                var colIndex = d.order[0].column;
                                console.log(settings.sortFieldMap);
                                var sortField = (settings.sortFieldMap && settings.sortFieldMap[colIndex])
                                    || (d.columns && d.columns[colIndex] && d.columns[colIndex].data);
                                d.sortColumn = sortField;
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
                        console.log(json);
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
                pagingType: 'simple_numbers',
                language: {
                    emptyTable: "<div class='py-2 text-center'><i class='bi bi-inbox fs-4 text-muted d-block mb-1'></i><span class='text-muted small'>No records available</span></div>",
                    zeroRecords: "<div class='py-2 text-center'><i class='bi bi-inbox fs-4 text-muted d-block mb-1'></i><span class='text-muted small'>No matching records found</span></div>",
                    processing: "<span class='spinner-border spinner-border-sm me-1'></span> Loading...",
                    search: "_INPUT_",
                    searchPlaceholder: "Search...",
                    lengthMenu: "Show _MENU_ entries",
                    info: "Showing _START_ to _END_ of _TOTAL_ entries",
                    infoEmpty: "Showing 0 to 0 of 0 entries",
                    infoFiltered: "(filtered from _MAX_ total entries)",
                    paginate: {
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

            // Active row highlight on draw
            $(settings.gridSelector).off('draw.dt.gctlHighlight').on('draw.dt.gctlHighlight', function () {
                $(settings.gridSelector).find('tbody tr').each(function () {
                    var $tr = $(this);
                    var $checkedInput = $tr.find('input[type="radio"]:checked, input[type="checkbox"]:checked');
                    $tr.toggleClass('selected-row table-active', $checkedInput.length > 0);
                });
            });

            // Active row highlight on selection change
            $(settings.gridSelector).off('change.gctlRowSelect', 'input[type="radio"], input[type="checkbox"]')
                .on('change.gctlRowSelect', 'input[type="radio"], input[type="checkbox"]', function () {
                    var $input = $(this);
                    var $tr = $input.closest('tr');
                    if ($input.is(':radio')) {
                        $(settings.gridSelector).find('tbody tr').removeClass('selected-row table-active');
                        if ($input.is(':checked')) {
                            $tr.addClass('selected-row table-active');
                        }
                    } else if ($input.is(':checkbox')) {
                        $tr.toggleClass('selected-row table-active', $input.is(':checked'));
                        if (settings.checkAllSelector) {
                            var total = $(settings.gridSelector + " ." + settings.checkBoxClass).length;
                            var checked = $(settings.gridSelector + " ." + settings.checkBoxClass + ":checked").length;
                            var $checkAll = $(settings.checkAllSelector);
                            if (checked === 0) {
                                $checkAll.prop('checked', false).prop('indeterminate', false);
                            } else if (checked === total) {
                                $checkAll.prop('checked', true).prop('indeterminate', false);
                            } else {
                                $checkAll.prop('checked', false).prop('indeterminate', true);
                            }
                        }
                    }
                });

            if (settings.checkAllSelector) {
                $("body").off("click.gctlGrid", settings.checkAllSelector)
                    .on("click.gctlGrid", settings.checkAllSelector, function () {
                        var isChecked = $(this).prop('checked');
                        $(settings.gridSelector + " ." + settings.checkBoxClass)
                            .prop('checked', isChecked)
                            .closest('tr').toggleClass('selected-row table-active', isChecked);
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
            var sortFieldMap = [];

            var hasSelectCol = config.showCheckbox || config.useRadio;

            if (hasSelectCol) {
                columns.push({
                    data: config.useRadio ? null : config.checkColumnData,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    width: "3%",
                    render: function (data, type, row) {
                        var cls = config.checkboxClass || 'form-check-input checkbox';
                        return config.useRadio
                            ? '<input type="radio" class="' + cls + '" name="' +
                            (config.radioName || 'rowSelect') + '" value="' +
                            (row[config.checkColumnData] != null ? row[config.checkColumnData] : '') + '">'
                            : '<input type="checkbox" class="' + cls + '" data-id="'+(data != null ? data : '')+'" value="' +
                            (data != null ? data : '') + '">';
                    }
                });
                sortFieldMap.push(null);
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
                        var idValField = c.idField || c.data;
                        var idVal = row && row[idValField] != null ? row[idValField] : (data != null ? data : '');

                        var extraAttrs = '';
                        if (c.dataAttributes && Array.isArray(c.dataAttributes)) {
                            c.dataAttributes.forEach(function (attrField) {
                                var kebabAttr = attrField.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
                                var attrVal = row && row[attrField] != null ? row[attrField] : '';
                                extraAttrs += ' data-' + kebabAttr + '="' + attrVal + '"';
                            });
                        }

                        return '<a class="dynamic-grid-link ' + editClass + '" data-id="' + idVal + '"' + extraAttrs + '>' + label + '</a>';
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

                sortFieldMap.push(c.sortField || c.data);
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
                columnDefs: hasSelectCol ? [{ targets: [0], orderable: false }] : [],
                sortFieldMap: sortFieldMap
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
    window.gctlRenderers = window.gctlRenderers || {};

    if (!window.gctlRenderers.formatDate) {
        window.gctlRenderers.formatDate = function (data) {
            if (!data) return '';
            var d = new Date(data);
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };
    }

    window.CustomDataTable = $.gctlGrid;
    window.GctlGrid = $.gctlGrid;

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