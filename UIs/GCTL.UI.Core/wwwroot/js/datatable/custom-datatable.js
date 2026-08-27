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
            if (!settings.ajaxUrl) {
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

                $table.find('tbody tr:not(.skeleton-row) .target-text, tbody tr:not(.skeleton-row) .dynamic-grid-link').each(function () {
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
                    searchPlaceholder: "Search records...",
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
                drawCallback: function () {
                    var dt = this.api();
                    applyStriping();
                    applyHighlight(dt);
                },
                destroy: true
            }, settings.extraOptions);

            // Shimmer Skeleton placeholder on preXhr
            if (settings.showSkeletonLoading) {
                $table.off('preXhr.dt.gctlSkeleton').on('preXhr.dt.gctlSkeleton', function () {
                    var colCount = settings.columns.length || $table.find('thead th').length || 6;
                    var skeletonHtml = '';
                    for (var i = 0; i < (settings.pageLength || 10); i++) {
                        skeletonHtml += '<tr class="skeleton-row">';
                        for (var j = 0; j < colCount; j++) {
                            var width = j === 0 ? '16px' : (40 + (j * 15) % 45) + '%';
                            skeletonHtml += '<td><span class="skeleton-box" style="width:' + width + ';"></span></td>';
                        }
                        skeletonHtml += '</tr>';
                    }
                    $table.find('tbody').html(skeletonHtml);
                });
            }

            var dataTable = $table.DataTable(dtOptions);

            $table.off('xhr.dt.gctlGrid').on('xhr.dt.gctlGrid', function (e, s, json, xhr) {
                settings.onLoaded(json);
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
                    var editLink = $(this).find('a.dynamic-grid-link, a.js-grid-edit, a[data-id]').first();
                    if (editLink.length) {
                        editLink.trigger('click');
                    }
                });
            }

            // 1-Click Copy Click Handler
            $table.off('click.gctlCopy').on('click.gctlCopy', '.btn-copy-code', function (e) {
                e.stopPropagation();
                var text = $(this).data('copy-text') || $(this).closest('td').find('.dynamic-grid-link, .target-text').text().trim();
                if (text && navigator.clipboard) {
                    navigator.clipboard.writeText(text);
                    var tooltip = $(this).siblings('.copy-tooltip');
                    if (tooltip.length) {
                        tooltip.addClass('show');
                        setTimeout(function () { tooltip.removeClass('show'); }, 1200);
                    }
                }
            });

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

            var config = $table.data('grid-config');
            if (!config) {
                return null;
            }

            var columns = [];
            var sortFieldMap = [];
            var nonOrderableCols = [];

            // 1. Sub-Row Expander Column
            if (config.hasSubRow) {
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
            if (config.showRowNumbers) {
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
            var hasSelectCol = config.showCheckbox || config.useRadio;
            if (hasSelectCol) {
                var selectColIdx = columns.length;
                columns.push({
                    data: config.useRadio ? null : config.checkColumnData,
                    className: "text-center",
                    orderable: false,
                    searchable: false,
                    width: "3%",
                    render: function (data, type, row) {
                        var cls = config.checkboxClass || 'form-check-input checkbox m-0 shadow-none';
                        return config.useRadio
                            ? '<input type="radio" class="' + cls + '" name="' +
                            (config.radioName || 'rowSelect') + '" value="' +
                            (row[config.checkColumnData] != null ? row[config.checkColumnData] : '') + '">'
                            : '<input type="checkbox" class="' + cls + '" data-id="' + (data != null ? data : '') + '" value="' +
                            (data != null ? data : '') + '">';
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(selectColIdx);
            }

            // 4. Data Columns
            var visibleCols = (config.columns || []).filter(function (c) {
                return c.visible !== false;
            });

            var defaultSortIdx = columns.length;

            visibleCols.forEach(function (c, i) {
                var colIdx = columns.length;
                if (config.defaultSortColumn && c.data === config.defaultSortColumn) {
                    defaultSortIdx = colIdx;
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

                        var linkHtml = '<a class="dynamic-grid-link ' + editClass + '" data-id="' + idVal + '"' + extraAttrs + '>' + label + '</a>';

                        if (c.isCopyable) {
                            linkHtml += '<span class="position-relative copy-wrapper ms-1">' +
                                '<i class="bi bi-copy btn-copy-code" data-copy-text="' + idVal + '" title="Copy"></i>' +
                                '<span class="copy-tooltip">Copied!</span>' +
                                '</span>';
                        }

                        return linkHtml;
                    };
                } else if (c.isTruncate) {
                    renderFn = function (data) {
                        var text = data != null ? data : '';
                        return '<span class="cell-truncate target-text" title="' + text + '">' + text + '</span>';
                    };
                } else if (c.renderFn && window.gctlRenderers && window.gctlRenderers[c.renderFn]) {
                    renderFn = window.gctlRenderers[c.renderFn];
                } else {
                    renderFn = function (data) {
                        var text = data != null ? data : '';
                        var html = '<span class="target-text">' + text + '</span>';
                        if (c.isCopyable && text) {
                            html += '<span class="position-relative copy-wrapper ms-1">' +
                                '<i class="bi bi-copy btn-copy-code" data-copy-text="' + text + '" title="Copy"></i>' +
                                '<span class="copy-tooltip">Copied!</span>' +
                                '</span>';
                        }
                        return html;
                    };
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

            // 5. Action Menu Column (3-dots)
            if (config.showActionMenu) {
                var actionIdx = columns.length;
                columns.push({
                    data: null,
                    className: "text-center col-actions",
                    orderable: false,
                    searchable: false,
                    width: "6%",
                    render: function (data, type, row) {
                        var id = row[config.checkColumnData] || '';
                        return '<div class="dropdown">' +
                            '<button class="btn-action-menu" data-bs-toggle="dropdown"><i class="bi bi-three-dots"></i></button>' +
                            '<ul class="dropdown-menu dropdown-menu-end shadow-sm" style="font-size:0.72rem;">' +
                            '<li><a class="dropdown-item py-1 js-grid-edit" data-id="' + id + '"><i class="bi bi-pencil me-1 text-primary"></i> Edit</a></li>' +
                            '<li><hr class="dropdown-divider my-1"></li>' +
                            '<li><a class="dropdown-item py-1 text-danger js-grid-delete" data-id="' + id + '"><i class="bi bi-trash me-1"></i> Delete</a></li>' +
                            '</ul></div>';
                    }
                });
                sortFieldMap.push(null);
                nonOrderableCols.push(actionIdx);
            }

            var pageLengths = config.pageLengths || [10, 25, 50, 100];

            var builtSettings = {
                gridSelector: '#' + config.gridId,
                ajaxUrl: config.ajaxUrl,
                ajaxType: config.ajaxMethod || "GET",
                serverSide: !!config.serverSide,
                columns: columns,
                checkBoxClass: config.checkboxClass || 'form-check-input checkbox m-0 shadow-none',
                checkAllSelector: (config.showCheckbox && !config.disableCheckAll && config.checkAllId)
                    ? ('#' + config.checkAllId)
                    : null,
                order: [[defaultSortIdx, config.defaultSortDirection || "desc"]],
                pageLength: config.defaultPageLength || 10,
                lengthMenu: [pageLengths, pageLengths],
                columnDefs: nonOrderableCols.length ? [{ targets: nonOrderableCols, orderable: false }] : [],
                sortFieldMap: sortFieldMap,
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
                    return `<a class='dynamic-grid-link ${editSelectorClass}' data-id='${row[idField] || ''}'>${data || ''}</a>`;
                }
            };
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

}(jQuery));