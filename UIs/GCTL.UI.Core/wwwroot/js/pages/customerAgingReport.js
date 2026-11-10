(function ($) {
    'use strict';

    // Global Renderers
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.renderSelectCheckbox = function (data, type, row) {
        var val = data || (row && (row.customerNo || row.CustomerNo || row.billNo || row.BillNo)) || '';
        return '<input type="checkbox" class="form-check-input row-select-chk m-0 shadow-none" value="' + val + '" />';
    };

    window.gctlRenderers.formatCurrency = function (data, type) {
        if (data === null || data === undefined || data === '' || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '0';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') {
            return num;
        }
        if (Math.abs(num) < 0.00001) {
            return '0';
        }
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    let gridTable = null;
    let filterDebounceTimer = null;
    let isInitialized = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        initFlatpickr();
        initGrid();
        bindEvents();
    });

    function initFlatpickr() {
        var $el = $('#filterAgingDate');
        if ($el.length && typeof flatpickr !== 'undefined') {
            $el.flatpickr({
                dateFormat: 'd/m/Y',
                allowInput: true,
                defaultDate: new Date()
            });
        }
    }

    function getDateValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
        if (fp) {
            if (fp.altInput && fp.altInput.value) {
                return fp.altInput.value.trim();
            }
            if (fp.selectedDates && fp.selectedDates.length > 0) {
                var d = fp.selectedDates[0];
                var day = String(d.getDate()).padStart(2, '0');
                var month = String(d.getMonth() + 1).padStart(2, '0');
                var year = d.getFullYear();
                return day + '/' + month + '/' + year;
            }
        }

        var $alt = $el.siblings('.flatpickr-alt-input');
        if ($alt.length && $alt.val()) {
            return $alt.val().trim();
        }

        return ($el.val() || '').trim();
    }

    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst) {
            if (Array.isArray(inst.selected)) {
                return inst.selected
                    .filter(function (v) { return v !== null && v !== undefined && String(v).trim() !== ''; })
                    .join(',');
            }
            if (inst.selected !== undefined && inst.selected !== null) {
                return String(inst.selected).trim();
            }
        }

        var val = $el.val();
        if (Array.isArray(val)) {
            return val
                .filter(function (v) { return v !== null && v !== undefined && String(v).trim() !== ''; })
                .join(',');
        }

        return (val || '').trim();
    }

    function getFilterParams(includeSelectedRows) {
        var agingDate = getDateValue('filterAgingDate');
        var customerId = getDropdownValue('filterCustomerId');
        var reportType = getDropdownValue('filterReportType') || $('#filterReportType').val() || 'Customer Aging';
        var exportFormat = getDropdownValue('filterExportFormat') || $('#filterExportFormat').val() || 'PDF';

        if (includeSelectedRows) {
            var selectedIds = [];
            $('#customerAgingReportTable tbody .row-select-chk:checked').each(function () {
                var v = $(this).val();
                if (v && selectedIds.indexOf(v) === -1) {
                    selectedIds.push(v);
                }
            });
            if (selectedIds.length > 0) {
                customerId = selectedIds.join(',');
            }
        }

        return {
            agingDate: agingDate,
            customerId: customerId,
            reportType: reportType,
            exportFormat: exportFormat
        };
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'customerAgingReportTable',
                extraOptions: {
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ],
                    drawCallback: function () {
                        var $table = $('#customerAgingReportTable');
                        $table.removeClass('gctl-table-loading');
                        $table.find('tbody').css('min-height', '');
                        $table.find('tbody tr.skeleton-row').remove();
                    }
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.agingDate = filters.agingDate;
                    d.customerId = filters.customerId;
                    d.reportType = filters.reportType;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    var $table = $('#customerAgingReportTable');
                    $table.removeClass('gctl-table-loading');
                    $table.find('tbody').css('min-height', '');
                    $table.find('tbody tr.skeleton-row').remove();

                    if (json && json.summary) {
                        updateKpiCards(json.summary);
                    }
                    adjustBillNoColumn();
                }
            });

            $('#customerAgingReportTable').on('xhr.dt', function () {
                var $table = $('#customerAgingReportTable');
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').css('min-height', '');
                $table.find('tbody tr.skeleton-row').remove();
            });

            $('#customerAgingReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error in CustomerAgingReport:', techNote, message);
                var $table = $('#customerAgingReportTable');
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').html('<tr><td colspan="17" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function adjustBillNoColumn() {
        var reportType = getDropdownValue('filterReportType') || $('#filterReportType').val() || 'Customer Aging';
        var isBillWise = reportType.indexOf('Bill') !== -1;
        if ($.fn.dataTable.isDataTable('#customerAgingReportTable')) {
            var table = $('#customerAgingReportTable').DataTable();
            // Column 2 is "Customer ID", Column 3 is "Bill No"
            var changed = false;
            // Customer ID visible when Customer Aging (summary), hidden when Bill Wise
            if (table.column(2).visible() !== !isBillWise) {
                table.column(2).visible(!isBillWise, false);
                changed = true;
            }
            // Bill No visible when Bill Wise, hidden when Customer Aging
            if (table.column(3).visible() !== isBillWise) {
                table.column(3).visible(isBillWise, false);
                changed = true;
            }
            if (changed) {
                table.columns.adjust();
            }
        }
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('customerAgingReportTable', true);
        } else if (gridTable && gridTable.ajax) {
            gridTable.ajax.reload(null, true);
        }
    }

    function onFilterChanged() {
        if (!isInitialized) return;
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            adjustBillNoColumn();
            reloadGrid();
        }, 150);
    }

    function updateKpiCards(summary) {
        if (!summary) return;
        $('#statTotalOutstanding').text(window.gctlRenderers.formatCurrency(summary.totalOutstanding));
        $('#stat_0_30').text(window.gctlRenderers.formatCurrency(summary.total_0_30));
        $('#stat_31_90').text(window.gctlRenderers.formatCurrency(summary.total_31_90));
        $('#stat_91_180').text(window.gctlRenderers.formatCurrency(summary.total_91_180));
        $('#stat_Over180').text(window.gctlRenderers.formatCurrency(summary.total_Over180));
    }

    function openPreviewModal() {
        var filters = getFilterParams(true);
        var qs = $.param(filters);
        var url = '/CustomerAgingReport/PreviewPdf?' + qs;
        window.open(url, '_blank');
    }

    function exportReport(format) {
        var filters = getFilterParams(true);
        var exportFormat = (format || filters.exportFormat || 'PDF').trim().toUpperCase();
        filters.exportFormat = exportFormat;
        var queryParams = $.param(filters);
        window.location.href = '/CustomerAgingReport/Export?' + queryParams;
    }

    function handleExportAction(action) {
        if (!action) return;
        var act = String(action).trim().toUpperCase();

        if (act === 'PREVIEW' || act.indexOf('PREVIEW') !== -1) {
            openPreviewModal();
        } else if (act === 'PDF') {
            exportReport('PDF');
        } else if (act === 'EXCEL' || act === 'XLSX') {
            exportReport('EXCEL');
        }

        setTimeout(function () {
            if (typeof window.clearCustomDropdownById === 'function') {
                window.clearCustomDropdownById('filterExportFormat');
            } else {
                var $el = $('#filterExportFormat');
                var inst = $el.data('customDdInstance');
                if (inst && typeof inst.clear === 'function') {
                    inst.clear();
                } else {
                    $el.val('');
                }
            }
        }, 350);
    }

    function bindEvents() {
        // Master Checkbox click handler
        $(document).on('click', '#chkSelectAll', function () {
            var isChecked = $(this).prop('checked');
            $('#customerAgingReportTable').find('.row-select-chk').prop('checked', isChecked);
        });

        // Individual checkbox click handler
        $(document).on('click', '.row-select-chk', function () {
            var total = $('#customerAgingReportTable').find('.row-select-chk').length;
            var checked = $('#customerAgingReportTable').find('.row-select-chk:checked').length;
            $('#chkSelectAll').prop('checked', total > 0 && total === checked);
        });

        $(document).on('change', '#filterAgingDate, #filterReportType', function () {
            onFilterChanged();
        });

        $('#filterCustomerId').on('change', function () {
            onFilterChanged();
        });

        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-tag-remove, .custom-dd-select-all', function () {
            setTimeout(function () {
                onFilterChanged();
            }, 30);
        });

        // Export Dropdown
        $('#filterExportFormat').on('change', function () {
            var val = $(this).val();
            if (val) {
                handleExportAction(val);
            }
        });

        $('.filter-section').on('click', '#filterExportFormat ~ .custom-dd-wrapper .custom-dd-option', function () {
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = $('#filterExportFormat').val() || val;
                if (currentVal) {
                    handleExportAction(currentVal);
                }
            }, 50);
        });
    }

})(jQuery);
