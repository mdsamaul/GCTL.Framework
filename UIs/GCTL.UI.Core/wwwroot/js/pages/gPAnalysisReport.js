/**
 * GP Analysis Report Page Script
 * CustomGrid with Multi-Level Headers, Server-Side Pagination (Default: 10)
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    // Global Renderers for Currency
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.formatCurrency = function (data, type) {
        if (data === null || data === undefined || data === '' || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '0.00';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') {
            return num;
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
        initDefaultDates();
        initDatepickers();
        checkAndLoadFilterOptions();
        initGrid();
        bindEvents();
        setTimeout(function () {
            isInitialized = true;
        }, 500);
    });

    function initDefaultDates() {
        var now = new Date();
        var currentMonthFirstDay = new Date(now.getFullYear(), now.getMonth(), 1);

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        if (!$('#filterFromDate').val()) {
            $('#filterFromDate').val(formatDate(currentMonthFirstDay));
        }
        if (!$('#filterToDate').val()) {
            $('#filterToDate').val(formatDate(now));
        }
    }

    function initDatepickers() {
        if ($.fn.datepicker) {
            $('.custom-datepicker').datepicker({
                format: 'dd/mm/yyyy',
                autoclose: true,
                todayHighlight: true
            }).on('changeDate', function () {
                onFilterChanged();
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

    /**
     * Helper to safely extract values from custom dropdowns or multi-selects
     */
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

    function getFilterParams() {
        var fromDate = getDateValue('filterFromDate');
        var toDate = getDateValue('filterToDate');
        var shipmentMode = getDropdownValue('filterShipmentMode');
        var customerId = getDropdownValue('filterCustomerId');

        return {
            fromDate: fromDate,
            toDate: toDate,
            shipmentMode: shipmentMode,
            customerId: customerId,
            dateUse: (fromDate && toDate) ? 'Y' : 'N'
        };
    }

    function checkAndLoadFilterOptions() {
        var $modeSelect = $('#filterShipmentMode');
        var $custSelect = $('#filterCustomerId');

        if ($modeSelect.children('option').length === 0 || $custSelect.children('option').length === 0) {
            $.ajax({
                url: '/GPAnalysisReport/GetFilterOptions',
                type: 'GET',
                success: function (res) {
                    if (res && res.success) {
                        if ($modeSelect.children('option').length === 0 && res.shipmentModes) {
                            res.shipmentModes.forEach(function (m) {
                                $modeSelect.append($('<option>', { value: m.value, text: m.text }));
                            });
                            if (typeof window.reinitCustomDropdownById === 'function') {
                                window.reinitCustomDropdownById('filterShipmentMode');
                            }
                        }
                        if ($custSelect.children('option').length === 0 && res.customers) {
                            res.customers.forEach(function (c) {
                                $custSelect.append($('<option>', { value: c.value, text: c.text }));
                            });
                            if (typeof window.reinitCustomDropdownById === 'function') {
                                window.reinitCustomDropdownById('filterCustomerId');
                            }
                        }
                    }
                }
            });
        }
    }

    function updateKpiCards(summary) {
        if (!summary) return;
        $('#statTotalBills').text(summary.totalBills != null ? summary.totalBills.toLocaleString() : '0');
        $('#statNetBillValue').text(summary.netBillValue != null ? window.gctlRenderers.formatCurrency(summary.netBillValue) : '0.00');
        $('#statTotalDue').text(summary.totalCosting != null ? window.gctlRenderers.formatCurrency(summary.totalCosting) : '0.00');
        $('#statprofit').text(summary.profit != null ? window.gctlRenderers.formatCurrency(summary.profit) : '0.00');
        $('#statLastServiceDate').text(summary.gpPercent || '0.00%');
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'gPAnalysisReportTable',
                extraOptions: {
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ]
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.fromDate;
                    d.toDate = filters.toDate;
                    d.shipmentMode = filters.shipmentMode;
                    d.customerId = filters.customerId;
                    d.dateUse = filters.dateUse;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    if (json && json.summary) {
                        updateKpiCards(json.summary);
                    }
                }
            });

            // Prevent grid from getting stuck on skeleton rows if an error occurs
            $('#gPAnalysisReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error:', techNote, message);
                $('#gPAnalysisReportTable tbody').html('<tr><td colspan="22" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('gPAnalysisReportTable', true);
        } else if (gridTable && gridTable.ajax) {
            gridTable.ajax.reload(null, true);
        }
    }

    function onFilterChanged() {
        if (!isInitialized) return;
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 300);
    }

    /**
     * Export report handler (PDF or Excel, defaults to PDF if nothing is selected)
     */
    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = (getDropdownValue('filterExportFormat') || $('#filterExportFormat').val() || '').trim();
        if (!exportFormat) exportFormat = 'PDF';

        var queryParams = $.param({
            fromDate: filters.fromDate,
            toDate: filters.toDate,
            shipmentMode: filters.shipmentMode,
            customerId: filters.customerId,
            dateUse: filters.dateUse,
            exportFormat: exportFormat
        });

        window.location.href = '/GPAnalysisReport/Export?' + queryParams;
    }

    function bindEvents() {
        $(document).on('change', '#filterFromDate, #filterToDate, .filter-section .flatpickr-alt-input', function () {
            onFilterChanged();
        });

        $('#filterShipmentMode, #filterCustomerId').on('change', function () {
            onFilterChanged();
        });

        // Delegate clicks on custom dropdown options, checkboxes, tag removals, and clear buttons
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-tag-remove, .custom-dd-select-all', function () {
            setTimeout(onFilterChanged, 60);
        });

        $('#btnPreview').on('click', function (e) {
            e.preventDefault();
            reloadGrid();
        });

        // Export button (defaults to PDF if format is not selected)
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });

        // Direct legacy buttons if triggered
        $('#btnExportPdf').on('click', function (e) {
            e.preventDefault();
            var params = getFilterParams();
            window.location.href = '/GPAnalysisReport/Export?' + $.param($.extend({}, params, { exportFormat: 'PDF' }));
        });

        $('#btnExportExcel').on('click', function (e) {
            e.preventDefault();
            var params = getFilterParams();
            window.location.href = '/GPAnalysisReport/Export?' + $.param($.extend({}, params, { exportFormat: 'Excel' }));
        });

        $('#btnExportCsv').on('click', function (e) {
            e.preventDefault();
            var params = getFilterParams();
            window.location.href = '/GPAnalysisReport/Export?' + $.param($.extend({}, params, { exportFormat: 'CSV' }));
        });
    }

})(jQuery);
