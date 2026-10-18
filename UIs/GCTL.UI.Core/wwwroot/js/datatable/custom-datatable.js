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
                showSkeletonLoading: true,
                showSearchHighlight: true,
                enableDoubleClickEdit: false,
                selectRowHighlight: false,
                onLoaded: function (json) { }
            }, options);

            if (!settings.gridSelector || !$(settings.gridSelector).length) {
                return null;
            }
            if (!settings.ajaxUrl && settings.serverSide) {
                return null;
            }

            var $table = $(settings.gridSelector);

            // Striped helper
            function applyStriping() {
                var visibleRows = $table.find('tbody tr:not(.dt-hasChild):not(.skeleton-row)');
                visibleRows.each(function (idx) {
                    if (idx % 2 === 1) {
                        $(this).addClass('even-striped');
                    } else {
                        $(this).removeClass('even-striped');
                    }
                });
            }

            // Search highlight helper
            function applyHighlight(dt) {
                if (!settings.showSearchHighlight) return;
                var query = dt.search() ? dt.search().trim() : '';
                var originalMap = window._gctlHighlightMap || new Map();
                window._gctlHighlightMap = originalMap;

                $table.find('tbody tr:not(.skeleton-row) .target-text, tbody tr:not(.skeleton-row) .custom-grid-link').each(function () {
                    var cell = this;
                    if (!originalMap.has(cell)) {
                        originalMap.set(cell, $(cell).text());
                    }
                    var original = originalMap.get(cell);

                    if (query && original && original.toLowerCase().includes(query.toLowerCase())) {
                        var regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
                        cell.innerHTML = original.replace(regex, '<mark class="search-highlight">$1</mark>');
                    } else {
                        cell.textContent = original;
                    }
                });
            }

            var dtOptions = $.extend(true, {
                autoWidth: false,
                deferRender: true,
                serverSide: settings.serverSide,
                columnDefs: [{ targets: '_all', orderSequence: ['asc', 'desc'] }].concat(settings.columnDefs),
                columns: settings.columns,
                lengthChange: true,
                pageLength: settings.pageLength,
                lengthMenu: settings.lengthMenu,
                order: settings.order,
                layout: {
                    topStart: {
                        rowClass: "row mt-1 justify-content-between",
                        features: ["pageLength"]
                    }
                },
                language: {
                    emptyTable: "No records available",
                    zeroRecords: "No matching records found",
                    processing: "<span class='spinner-border spinner-border-sm me-1'></span> Loading...",
                    search: "_INPUT_",
                    searchPlaceholder: "Search records...",
                    lengthMenu: "Show _MENU_ entries",
                    info: "Showing _START_ to _END_ of _TOTAL_ entries",
                    infoEmpty: "Showing 0 to 0 of 0 entries",
                    infoFiltered: "(filtered from _MAX_ total entries)",
                    paginate: {
                        first: "<i class='bi bi-chevron-double-left'></i>",
                        previous: "<i class='bi bi-chevron-left'></i>",
                        next: "<i class='bi bi-chevron-right'></i>",
                        last: "<i class='bi bi-chevron-double-right'></i>"
                    }
                },
                drawCallback: function () {
                    var dt = this.api();
                    $table.removeClass('gctl-table-loading');
                    $table.find('tbody').css('min-height', '');
                    applyStriping();
                    applyHighlight(dt);
                    if (settings.extraOptions && typeof settings.extraOptions.drawCallback === 'function') {
                        settings.extraOptions.drawCallback.call(this);
                    }
                },
                destroy: true
            }, settings.extraOptions);

            // Shimmer Skeleton on initial load & In-Place Seamless Transition on reloads
            var isFirstLoad = true;
            if (settings.showSkeletonLoading) {
                $table.off('preXhr.dt.gctlSkeleton').on('preXhr.dt.gctlSkeleton', function () {
                    var $tbody = $table.find('tbody');
                    var existingRows = $tbody.find('tr:not(.skeleton-row)');

                    if (!isFirstLoad && existingRows.length > 0) {
                        // In-place transition: preserve exact height & keep existing multi-line rows in place
                        var currentHeight = $tbody.outerHeight();
                        if (currentHeight > 0) {
                            $tbody.css('min-height', currentHeight + 'px');
                        }
                        $table.addClass('gctl-table-loading');
                    } else {
                        // Cold / First Load: Show skeleton placeholder rows
                        var colCount = settings.columns.length || $table.find('thead th').length || 6;
                        var targetCount = settings.pageLength > 0 ? settings.pageLength : 5;
                        if (targetCount > 10) targetCount = 10;
                        var skeletonHtml = '';
                        for (var i = 0; i < targetCount; i++) {
                            skeletonHtml += '<tr class="skeleton-row">';
                            for (var j = 0; j < colCount; j++) {
                                var width = j === 0 ? '16px' : (40 + (j * 15) % 45) + '%';
                                skeletonHtml += '<td><span class="skeleton-box" style="width:' + width + ';"></span></td>';
                            }
                            skeletonHtml += '</tr>';
                        }
                        $tbody.html(skeletonHtml);
                    }
                });
            }

            if (settings.ajaxUrl) {
                dtOptions.ajax = {
                    url: settings.ajaxUrl,
                    type: settings.ajaxType,
                    data: function (d) {
                        if (settings.serverSide) {
                            d.pageNumber = Math.floor(d.start / d.length) + 1;
                            d.pageSize = d.length;

                            if (d.order && d.order.length) {
                                var colIndex = d.order[0].column;
                                var sortField = (settings.sortFieldMap && settings.sortFieldMap[colIndex])
                                    || (d.columns && d.columns[colIndex] && d.columns[colIndex].data);
                                d.sortColumn = sortField || "";
                                d.sortOrder = d.order[0].dir || "";
                            } else {
                                d.sortColumn = "";
                                d.sortOrder = "";
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
                        if (!json) return [];
                        if (json.recordsTotal === undefined && json.totalCount !== undefined) {
                            json.recordsTotal = json.totalCount;
                        }
                        if (json.recordsFiltered === undefined && json.filteredCount !== undefined) {
                            json.recordsFiltered = json.filteredCount;
                        }
                        if (json.data !== undefined) {
                            return json.data !== null ? json.data : [];
                        }
                        return Array.isArray(json) ? json : [];
                    }
                };
            }

            var dataTable = $table.DataTable(dtOptions);

            $table.off('xhr.dt.gctlGrid').on('xhr.dt.gctlGrid', function (e, s, json, xhr) {
                isFirstLoad = false;
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').css('min-height', '');
                settings.onLoaded(json);
            });

            $table.off('error.dt.gctlGrid').on('error.dt.gctlGrid', function () {
                isFirstLoad = false;
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').css('min-height', '');
            });

            // Master Checkbox click handler
            if (settings.checkAllSelector) {
                $("body").off("click.gctlGrid", settings.checkAllSelector)
                    .on("click.gctlGrid", settings.checkAllSelector, function () {
                        var isChecked = $(this).prop('checked');
                        $table.find("." + settings.checkBoxClass).prop('checked', isChecked).trigger('change');
                    });
            }

            // Row Checkbox change & Selection styling
            $table.off('change.gctlRowSelect').on('change.gctlRowSelect', '.' + settings.checkBoxClass, function () {
                var tr = $(this).closest('tr');
                if ($(this).prop('checked')) {
                    tr.addClass('selected-row');
                } else {
                    tr.removeClass('selected-row');
                }

                if (settings.checkAllSelector) {
                    var allCbs = $table.find('.' + settings.checkBoxClass);
                    var checkedCbs = allCbs.filter(':checked');
                    $(settings.checkAllSelector).prop('checked', allCbs.length > 0 && allCbs.length === checkedCbs.length);
                }
            });

            // Double Click Row Edit Handler (Ignores interactive elements)
            if (settings.enableDoubleClickEdit) {
                $table.off('dblclick.gctlDblClick').on('dblclick.gctlDblClick', 'tbody tr', function (e) {
                    if ($(e.target).closest('input, button, a, .dropdown, .dropdown-menu, .btn-copy-code, .btn-expand-row, .form-check-input, .btn-action-menu').length > 0) {
                        return;
                    }
                    var editLink = $(this).find('a.custom-grid-link, a.js-grid-edit, a[data-id]').first();
                    if (editLink.length) {
                        editLink.trigger('click');
                    }
                });
            }

            // 1-Click Copy Click Handler
            $table.off('click.gctlCopy').on('click.gctlCopy', '.btn-copy-code', function (e) {
                e.stopPropagation();
                var text = $(this).data('copy-text') || $(this).closest('td').find('.custom-grid-link, .target-text').text().trim();
                if (text && navigator.clipboard) {
                    navigator.clipboard.writeText(text);
                    var tooltip = $(this).siblings('.copy-tooltip');
                    if (tooltip.length) {
                        tooltip.addClass('show');
                        setTimeout(function () { tooltip.removeClass('show'); }, 1200);
                    }
                }
            });

            // Initialize Bootstrap tooltips for compact headers
            if (window.bootstrap && typeof window.bootstrap.Tooltip === 'function') {
                $table.find('thead [data-bs-toggle="tooltip"]').each(function () {
                    if (!bootstrap.Tooltip.getInstance(this)) {
                        new bootstrap.Tooltip(this);
                    }
                });
            }

            return dataTable;
        },

        // Builds DataTables options from GridModel JSON embedded on the table
        initFromModel: function (overrides) {
            overrides = overrides || {};
            var $table = overrides.gridSelector
                ? $(overrides.gridSelector)
                : $('#' + overrides.gridId);

            if (!$table.length) {
                return null;
            }

            var config = $table.data('grid-config') || $table.attr('data-grid-config');
            if (typeof config === 'string') {
                try { config = JSON.parse(config); } catch (e) { console.error("Invalid grid config JSON", e); return null; }
            }
            if (!config) {
                return null;
            }

            var columns = [];
            var sortFieldMap = [];
            var nonOrderableCols = [];

            // 1. Sub-Row Expander Column
            var hasSubRow = config.hasSubRow !== undefined ? config.hasSubRow : config.HasSubRow;
            if (hasSubRow) {
                var subRowIdx = columns.length;
                columns.push({
                    data: null,
                    className: "text-center col-subrow",
                    orderable: false,
                    searchable: false,
                    width: "24px",
                    render: function () {
                        return '<i class="bi bi-chevron-right btn-expand-row"></i>';
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(subRowIdx);
            }

            // 2. Row Number Column (#)
            var showRowNumbers = config.showRowNumbers !== undefined ? config.showRowNumbers : config.ShowRowNumbers;
            if (showRowNumbers) {
                var rowNumIdx = columns.length;
                columns.push({
                    data: null,
                    className: "text-center col-sl-no fw-medium text-muted",
                    orderable: false,
                    searchable: false,
                    width: "32px",
                    render: function (data, type, row, meta) {
                        return (meta.row + 1) + (meta.settings._iDisplayStart || 0);
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(rowNumIdx);
            }

            // 3. Selection Column (Checkbox / Radio)
            var showCheckbox = config.showCheckbox !== undefined ? config.showCheckbox : config.ShowCheckbox;
            var useRadio = config.useRadio !== undefined ? config.useRadio : config.UseRadio;
            var hasSelectCol = showCheckbox || useRadio;
            if (hasSelectCol) {
                var selectColIdx = columns.length;
                var checkColData = config.checkColumnData || config.CheckColumnData;
                columns.push({
                    data: useRadio ? null : checkColData,
                    className: "text-center col-select-cell",
                    orderable: false,
                    searchable: false,
                    width: "45px",
                    render: function (data, type, row) {
                        var cls = config.checkboxClass || config.CheckboxClass || 'form-check-input checkbox m-0 shadow-none';
                        return useRadio
                            ? '<input type="radio" class="' + cls + '" name="' +
                            (config.radioName || config.RadioName || 'rowSelect') + '" value="' +
                            (row[checkColData] != null ? row[checkColData] : '') + '">'
                            : '<input type="checkbox" class="' + cls + '" data-id="' + (data != null ? data : '') + '" value="' +
                            (data != null ? data : '') + '">';
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(selectColIdx);
            }

            // 4. Data Columns
            var confCols = config.columns || config.Columns || [];
            var visibleCols = confCols.filter(function (c) {
                var isVisible = c.visible !== undefined ? c.visible : c.Visible;
                return isVisible !== false;
            });

            var defaultSortIdx = -1;

            visibleCols.forEach(function (c, i) {
                var colIdx = columns.length;
                var defaultSortCol = config.defaultSortColumn || config.DefaultSortColumn;
                var colData = c.data !== undefined ? c.data : c.Data;
                var colSortField = c.sortField !== undefined ? c.sortField : c.SortField;
                var isEditLink = c.isEditLink !== undefined ? c.isEditLink : c.IsEditLink;
                var isTruncate = c.isTruncate !== undefined ? c.isTruncate : c.IsTruncate;
                var isCopyable = c.isCopyable !== undefined ? c.isCopyable : c.IsCopyable;

                if (defaultSortCol && (colData === defaultSortCol || colSortField === defaultSortCol)) {
                    defaultSortIdx = colIdx;
                }

                var renderFn;

                if (isEditLink) {
                    renderFn = function (data, type, row) {
                        var editClass = c.editSelectorClass || c.EditSelectorClass || 'edit-link';
                        var cEditLabel = c.editLabel !== undefined ? c.editLabel : c.EditLabel;
                        var label = cEditLabel != null ? cEditLabel : (data != null ? data : '');
                        var idValField = c.idField || c.IdField || colData;
                        var idVal = row && row[idValField] != null ? row[idValField] : (data != null ? data : '');

                        var extraAttrs = '';
                        var dAttrs = c.dataAttributes || c.DataAttributes;
                        if (dAttrs && Array.isArray(dAttrs)) {
                            dAttrs.forEach(function (attrField) {
                                var kebabAttr = attrField.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
                                var attrVal = row && row[attrField] != null ? row[attrField] : '';
                                extraAttrs += ' data-' + kebabAttr + '="' + attrVal + '"';
                            });
                        }

                        var linkHtml = '<a class="custom-grid-link ' + editClass + '" data-id="' + idVal + '"' + extraAttrs + '>' + label + '</a>';

                        if (isCopyable) {
                            linkHtml += '<span class="position-relative copy-wrapper ms-1">' +
                                '<i class="bi bi-copy btn-copy-code" data-copy-text="' + idVal + '" title="Copy"></i>' +
                                '<span class="copy-tooltip">Copied!</span>' +
                                '</span>';
                        }

                        return linkHtml;
                    };
                } else if (isTruncate) {
                    renderFn = function (data) {
                        var text = data != null ? data : '';
                        return '<span class="cell-truncate target-text" title="' + text + '">' + text + '</span>';
                    };
                } else {
                    var cRenderFn = c.renderFn || c.RenderFn;
                    if (cRenderFn && window.gctlRenderers && window.gctlRenderers[cRenderFn]) {
                        renderFn = window.gctlRenderers[cRenderFn];
                    } else {
                        renderFn = function (data) {
                            var text = data != null ? data : '';
                            var html = '<span class="target-text">' + text + '</span>';
                            if (isCopyable && text) {
                                html += '<span class="position-relative copy-wrapper ms-1">' +
                                    '<i class="bi bi-copy btn-copy-code" data-copy-text="' + text + '" title="Copy"></i>' +
                                    '<span class="copy-tooltip">Copied!</span>' +
                                    '</span>';
                            }
                            return html;
                        };
                    }
                }

                var cssClass = c.cssClass || c.CssClass;
                var sortable = c.sortable !== undefined ? c.sortable : c.Sortable;
                var searchable = c.searchable !== undefined ? c.searchable : c.Searchable;

                columns.push({
                    data: colData,
                    className: cssClass || "text-center",
                    orderable: sortable !== false,
                    searchable: searchable !== false,
                    render: renderFn
                });

                sortFieldMap.push(colSortField || colData);
            });

            // 5. Action Menu Column (3-dots)
            var showActionMenu = config.showActionMenu !== undefined ? config.showActionMenu : config.ShowActionMenu;
            if (showActionMenu) {
                var actionIdx = columns.length;
                columns.push({
                    data: null,
                    className: "text-center col-actions",
                    orderable: false,
                    searchable: false,
                    width: "6%",
                    render: function (data, type, row) {
                        var checkCol = config.checkColumnData || config.CheckColumnData;
                        var id = (checkCol && row[checkCol]) ? row[checkCol] : '';
                        return '<div class="d-flex justify-content-center gap-2">' +
                            '<a href="javascript:void(0)" class="text-primary js-grid-edit" data-id="' + id + '" title="Edit" style="font-size: 1.1rem;"><i class="bi bi-pencil-square"></i></a>' +
                            '<a href="javascript:void(0)" class="text-danger js-grid-delete" data-id="' + id + '" title="Delete" style="font-size: 1.1rem;"><i class="bi bi-trash"></i></a>' +
                            '</div>';
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(actionIdx);
            }

            var pageLengths = config.pageLengths || [10, 25, 50, 100, -1];
            var lengthValues = [];
            var lengthLabels = [];
            pageLengths.forEach(function (len) {
                lengthValues.push(len);
                lengthLabels.push(len === -1 ? 'All' : len);
            });

            var initialOrder = (defaultSortIdx >= 0)
                ? [[defaultSortIdx, config.defaultSortDirection || "desc"]]
                : [];

            var showCheckAll = config.showCheckAll !== undefined
                ? config.showCheckAll
                : (config.ShowCheckAll !== undefined ? config.ShowCheckAll : true);
            if (config.checkAllId === "none" || config.checkAllId === "false" || !config.checkAllId) {
                showCheckAll = false;
            }

            var showPagination = config.showPagination !== undefined ? config.showPagination : config.ShowPagination;
            var showSearchBox = config.showSearchBox !== undefined ? config.showSearchBox : config.ShowSearchBox;
            var configExtraOptions = {};
            if (showPagination === false) {
                configExtraOptions.paging = false;
            }
            if (showSearchBox === false) {
                configExtraOptions.searching = false;
            }

            var builtSettings = {
                gridSelector: '#' + config.gridId,
                ajaxUrl: config.ajaxUrl,
                ajaxType: config.ajaxMethod || "GET",
                serverSide: !!config.serverSide,
                ajaxData: overrides.ajaxData,
                columns: columns,
                checkBoxClass: config.checkboxClass || 'form-check-input checkbox m-0 shadow-none',
                checkAllSelector: (config.showCheckbox && !config.disableCheckAll && showCheckAll && config.checkAllId && config.checkAllId !== 'none')
                    ? ('#' + config.checkAllId)
                    : null,
                order: initialOrder,
                pageLength: config.defaultPageLength || 10,
                lengthMenu: [lengthValues, lengthLabels],
                columnDefs: nonOrderableCols.length ? [{ targets: nonOrderableCols, orderable: false }] : [],
                sortFieldMap: sortFieldMap,
                extraOptions: configExtraOptions,
                showSkeletonLoading: config.showSkeletonLoading !== false,
                showSearchHighlight: config.showSearchHighlight !== false,
                enableDoubleClickEdit: !!config.enableDoubleClickEdit,
                selectRowHighlight: !!config.selectRowHighlight
            };

            var dataTable = this.init($.extend(true, builtSettings, overrides));

            // Sub-row accordion click handler
            if (config.hasSubRow && dataTable) {
                $table.off('click.gctlSubRow').on('click.gctlSubRow', '.btn-expand-row', function (e) {
                    e.stopPropagation();
                    var tr = $(this).closest('tr');
                    var row = dataTable.row(tr);

                    if (row.child.isShown()) {
                        row.child.hide();
                        $(this).removeClass('expanded');
                        tr.removeClass('dt-hasChild');
                    } else {
                        var rowData = row.data();
                        var detailHtml = '';
                        if (config.subRowRenderFn && window.gctlRenderers && window.gctlRenderers[config.subRowRenderFn]) {
                            detailHtml = window.gctlRenderers[config.subRowRenderFn](rowData);
                        } else {
                            detailHtml = '<div class="detail-card text-muted">No additional details configured.</div>';
                        }
                        row.child(detailHtml, 'detail-row').show();
                        $(this).addClass('expanded');
                        tr.addClass('dt-hasChild');
                    }
                });
            }

            return dataTable;
        },

        checkboxColumn: function (idField, checkBoxClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "2%",
                orderable: false,
                "render": function (data, type, row) {
                    return `<input type="checkbox" class="${checkBoxClass || 'checkBox m-0 shadow-none'}" value="${row[idField] || ''}" />`;
                }
            };
        },

        editLinkColumn: function (idField, editSelectorClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "7%",
                "render": function (data, type, row) {
                    return `<a class='custom-grid-link ${editSelectorClass}' data-id='${row[idField] || ''}'>${data || ''}</a>`;
                }
            };
        },

        reload: function (gridId, resetPaging) {
            if (!gridId) return;
            var selector = gridId.startsWith('#') ? gridId : '#' + gridId;
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(selector)) {
                $(selector).DataTable().ajax.reload(null, resetPaging === true);
            }
        },

        getTable: function (gridId) {
            if (!gridId) return null;
            var selector = gridId.startsWith('#') ? gridId : '#' + gridId;
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(selector)) {
                return $(selector).DataTable();
            }
            return null;
        }
    };

    // Aliases for backward compatibility
    window.CustomDataTable = $.gctlGrid;
    window.GctlGrid = $.gctlGrid;

    // Registry that GridColumn.RenderFn names are resolved against.
    window.gctlRenderers = window.gctlRenderers || {};

    if (!window.gctlRenderers.formatDate) {
        window.gctlRenderers.formatDate = function (data) {
            if (!data) return '';
            var d = new Date(data);
            if (isNaN(d.getTime())) return data;
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };
    }

    window.gctlRenderers.formatCurrency = function (data, type, row) {
        if (data === null || data === undefined || data === '' || (typeof data === 'string' && data.trim() === '') || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '-';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') {
            return num;
        }
        if (num === 0) {
            return '-';
        }
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

}(jQuery));