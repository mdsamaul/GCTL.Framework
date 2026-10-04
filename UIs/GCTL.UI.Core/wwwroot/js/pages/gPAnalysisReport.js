/**
 * GP Analysis Report Page Script
 * CustomGrid with Multi-Level Headers, Server-Side Pagination (Default: 10)
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    // Global Renderers for Currency
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.formatCurrency = function (data) {
        if (data === null || data === undefined || data === '') return '0.00';
        var num = parseFloat(data);
        if (isNaN(num)) return data;
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    let gridTable = null;
    let filterDebounceTimer = null;

    $(document).ready(function () {
        initDefaultDates();
        initDatepickers();
        checkAndLoadFilterOptions();
        initGrid();
        bindEvents();
    });

    function initDefaultDates() {
        var now = new Date();
        var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        if (!$('#filterFromDate').val()) {
            $('#filterFromDate').val(formatDate(firstDay));
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

    /**
     * Helper to safely extract values from custom dropdowns or multi-selects
     */
    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst) {
            if (Array.isArray(inst.selected)) {
                return inst.selected.filter(function (v) { return !!v; }).join(',');
            }
            if (inst.selected !== undefined && inst.selected !== null) {
                return String(inst.selected).trim();
            }
        }

        var val = $el.val();
        if (Array.isArray(val)) {
            return val.filter(function (v) { return !!v; }).join(',');
        }

        return (val || '').trim();
    }

    function getFilterParams() {
        var fromDate = $('#filterFromDate').val() || '';
        var toDate = $('#filterToDate').val() || '';
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
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.fromDate;
                    d.toDate = filters.toDate;
                    d.shipmentMode = filters.shipmentMode;
                    d.customerId = filters.customerId;
                    d.dateUse = filters.dateUse;
                },
                extraOptions: {
                    pageLength: 10,
                    lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'All']],
                    drawCallback: function (settings) {
                        var json = settings.json;
                        if (json && json.summary) {
                            updateKpiCards(json.summary);
                        }
                    }
                }
            });
        }
    }

    function reloadGrid() {
        if (gridTable) {
            gridTable.ajax.reload(null, true);
        }
    }

    function onFilterChanged() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 250);
    }

    function bindEvents() {
        $('#filterFromDate, #filterToDate').on('change', function () {
            onFilterChanged();
        });

        $('#filterShipmentMode, #filterCustomerId').on('change', function () {
            onFilterChanged();
        });

        $('#btnPreview').on('click', function (e) {
            e.preventDefault();
            reloadGrid();
        });

        // PDF Export
        $('#btnExportPdf').on('click', function (e) {
            e.preventDefault();
            var params = getFilterParams();
            var url = '/GPAnalysisReport/ExportPdf?' + $.param(params);
            window.open(url, '_blank');
        });

        // Excel Export
        $('#btnExportExcel').on('click', function (e) {
            e.preventDefault();
            var params = getFilterParams();
            var url = '/GPAnalysisReport/ExportExcel?' + $.param(params);
            window.location.href = url;
        });
    }

})(jQuery);
