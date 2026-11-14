(function ($) {
    'use strict';

    // Global Renderers for Currency & Percentages
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.renderSelectCheckbox = function (data, type, row) {
        var billNo = data || (row && (row.billNo || row.BillNo)) || '';
        return '<input type="checkbox" class="form-check-input row-select-chk m-0 shadow-none" value="' + billNo + '" />';
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

    window.gctlRenderers.formatPercent = function (data, type) {
        if (data === null || data === undefined || data === '') {
            return (type === 'sort' || type === 'type') ? 0 : '0';
        }
        var s = String(data).trim();
        if (s === '0.00%' || s === '0.00' || s === '0%' || s === '0') {
            return (type === 'sort' || type === 'type') ? 0 : '0';
        }
        if (type === 'sort' || type === 'type') {
            var num = parseFloat(s.replace('%', ''));
            return isNaN(num) ? 0 : num;
        }
        return s;
    };

    let gridTable = null;
    let filterDebounceTimer = null;
    let isInitialized = false;
    let isProgrammaticDateUpdate = false;
    let isSilentDropdownUpdate = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }
        initDefaultDates();
        checkAndLoadFilterOptions();
        initGrid();
        bindEvents();
        setTimeout(function () {
            if (!getDropdownValue('filterQuickRange')) {
                silentSetDropdownValue('filterQuickRange', '3m');
            }
        }, 50);
        setTimeout(function () {
            isInitialized = true;
        }, 500);
    });

    function silentSetDropdownValue(id, value, text) {
        var $el = $('#' + id);
        if (!$el.length) return;

        isSilentDropdownUpdate = true;
        try {
            $el.val(value);
            var inst = $el.data('customDdInstance');
            if (inst && typeof inst.setSelected === 'function') {
                inst.setSelected(value);
            } else if (inst && typeof inst.setValue === 'function') {
                inst.setValue(value, text);
            } else if (typeof window.setCustomDropdownValue === 'function') {
                window.setCustomDropdownValue(id, value, text);
            }
        } finally {
            isSilentDropdownUpdate = false;
        }
    }

    function setDateValue(id, dateObj) {
        var el = document.getElementById(id);
        if (!el || !dateObj) return;

        var day = String(dateObj.getDate()).padStart(2, '0');
        var month = String(dateObj.getMonth() + 1).padStart(2, '0');
        var year = dateObj.getFullYear();
        var dmy = day + '/' + month + '/' + year;
        var iso = year + '-' + month + '-' + day;

        isProgrammaticDateUpdate = true;
        try {
            var $el = $(el);
            var fp = $el.data('customFpInstance') || el._flatpickr;
            if (fp && typeof fp.setDate === 'function') {
                fp.setDate(dateObj, false);
                if (fp.altInput) fp.altInput.value = dmy;
                el.value = iso;
            } else if (typeof window.setDate === 'function') {
                window.setDate('#' + id, dmy);
            } else {
                el.value = dmy;
            }

            if (el.nextElementSibling && el.nextElementSibling.classList.contains('flatpickr-alt-input')) {
                el.nextElementSibling.value = dmy;
            }
        } finally {
            setTimeout(function () {
                isProgrammaticDateUpdate = false;
            }, 0);
        }
    }

    function applyQuickRange(code) {
        if (!code || code === 'custom') return;

        var today = new Date();
        var from = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        var to = new Date(today.getFullYear(), today.getMonth(), today.getDate());

        switch (code) {
            case 'today':
                break;
            case '7d':
                from.setDate(today.getDate() - 7);
                break;
            case '30d':
                from.setDate(today.getDate() - 30);
                break;
            case 'this_month':
                from = new Date(today.getFullYear(), today.getMonth(), 1);
                break;
            case 'last_month':
                from = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                to = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case '3m':
                from.setMonth(today.getMonth() - 3);
                break;
            case '6m':
                from.setMonth(today.getMonth() - 6);
                break;
            case '1y':
                from.setFullYear(today.getFullYear() - 1);
                break;
            default:
                return;
        }

        // Relax existing Flatpickr min/max constraints before applying a new range
        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');
        var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');
        if (fromFp && typeof fromFp.set === 'function') fromFp.set('maxDate', null);
        if (toFp && typeof toFp.set === 'function') toFp.set('minDate', null);

        setDateValue('filterFromDate', from);
        setDateValue('filterToDate', to);
        updateDateLimits();
    }

    function initDefaultDates() {
        var now = new Date();
        var defaultFromDate = new Date(now.getFullYear(), now.getMonth() - 3, now.getDate());

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        var fromStr = formatDate(defaultFromDate);
        var toStr = formatDate(now);

        var $from = $('#filterFromDate');
        var $to = $('#filterToDate');

        if (!$from.val()) {
            $from.val(fromStr);
        }
        if (!$to.val()) {
            $to.val(toStr);
        }

        if (!getDropdownValue('filterQuickRange')) {
            silentSetDropdownValue('filterQuickRange', '3m');
        }

        // Sync Flatpickr instances so the calendar has the default 3-month range selected
        var syncFlatpickr = function () {
            var fpFrom = $from.data('customFpInstance') || ($from[0] ? $from[0]._flatpickr : null);
            if (fpFrom) {
                if (!fpFrom.selectedDates || !fpFrom.selectedDates.length) {
                    isProgrammaticDateUpdate = true;
                    try {
                        fpFrom.setDate(defaultFromDate, false);
                    } finally {
                        isProgrammaticDateUpdate = false;
                    }
                }
                if (fpFrom.altInput && !fpFrom.altInput.value) {
                    fpFrom.altInput.value = fromStr;
                }
            } else if (window.setDate) {
                window.setDate('#filterFromDate', fromStr);
            }

            var fpTo = $to.data('customFpInstance') || ($to[0] ? $to[0]._flatpickr : null);
            if (fpTo) {
                if (!fpTo.selectedDates || !fpTo.selectedDates.length) {
                    isProgrammaticDateUpdate = true;
                    try {
                        fpTo.setDate(now, false);
                    } finally {
                        isProgrammaticDateUpdate = false;
                    }
                }
                if (fpTo.altInput && !fpTo.altInput.value) {
                    fpTo.altInput.value = toStr;
                }
            } else if (window.setDate) {
                window.setDate('#filterToDate', toStr);
            }

            updateDateLimits();
            bindFlatpickrDateLimits();
        };

        syncFlatpickr();
        setTimeout(syncFlatpickr, 100);
        setTimeout(syncFlatpickr, 300);
    }

    function parseDate(str) {
        if (!str) return null;
        if (str instanceof Date) {
            var d = new Date(str.getTime());
            d.setHours(0, 0, 0, 0);
            return d;
        }
        var s = String(str).trim();
        var dmy = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
        if (dmy) {
            return new Date(parseInt(dmy[3], 10), parseInt(dmy[2], 10) - 1, parseInt(dmy[1], 10), 0, 0, 0, 0);
        }
        var iso = s.match(/^(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})/);
        if (iso) {
            return new Date(parseInt(iso[1], 10), parseInt(iso[2], 10) - 1, parseInt(iso[3], 10), 0, 0, 0, 0);
        }
        var p = new Date(s);
        if (!isNaN(p.getTime())) {
            p.setHours(0, 0, 0, 0);
            return p;
        }
        return null;
    }

    function updateDateLimits() {
        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');

        var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

        if (!fromFp || !toFp) return;

        var fromVal = getDateValue('filterFromDate');
        var toVal = getDateValue('filterToDate');

        var fromDate = parseDate(fromVal);
        var toDate = parseDate(toVal);

        if (toDate && (!fromFp.config._maxDate || fromFp.config._maxDate.getTime() !== toDate.getTime())) {
            fromFp.set('maxDate', toDate);
        }
        if (fromDate && (!toFp.config._minDate || toFp.config._minDate.getTime() !== fromDate.getTime())) {
            toFp.set('minDate', fromDate);
        }
    }

    var dateLimitsBound = false;
    function bindFlatpickrDateLimits() {
        if (dateLimitsBound) return;
        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');

        var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

        if (!fromFp || !toFp) return;
        dateLimitsBound = true;

        if (fromFp.config && Array.isArray(fromFp.config.onChange)) {
            fromFp.config.onChange.push(function (selectedDates) {
                if (isProgrammaticDateUpdate) return;
                var selected = selectedDates && selectedDates[0];
                if (selected && toFp && typeof toFp.set === 'function') {
                    if (!toFp.config._minDate || toFp.config._minDate.getTime() !== selected.getTime()) {
                        toFp.set('minDate', selected);
                    }
                    var curTo = toFp.selectedDates && toFp.selectedDates[0];
                    if (curTo && curTo < selected) {
                        toFp.setDate(selected, false);
                    }
                }
                silentSetDropdownValue('filterQuickRange', 'custom');
                onFilterChanged();
            });
        }

        if (toFp.config && Array.isArray(toFp.config.onChange)) {
            toFp.config.onChange.push(function (selectedDates) {
                if (isProgrammaticDateUpdate) return;
                var selected = selectedDates && selectedDates[0];
                if (selected && fromFp && typeof fromFp.set === 'function') {
                    if (!fromFp.config._maxDate || fromFp.config._maxDate.getTime() !== selected.getTime()) {
                        fromFp.set('maxDate', selected);
                    }
                    var curFrom = fromFp.selectedDates && fromFp.selectedDates[0];
                    if (curFrom && curFrom > selected) {
                        fromFp.setDate(selected, false);
                    }
                }
                silentSetDropdownValue('filterQuickRange', 'custom');
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

    // * Helper to safely extract values from custom dropdowns or multi-selects
    
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
        var branchCode = getDropdownValue('filterBranchCode') || $('#filterBranchCode').val() || '';
        var shipmentMode = getDropdownValue('filterShipmentMode');
        var customerId = getDropdownValue('filterCustomerId');

        return {
            fromDate: fromDate,
            toDate: toDate,
            branchCode: branchCode,
            shipmentMode: shipmentMode,
            customerId: customerId,
            dateUse: (fromDate && toDate) ? 'Y' : 'N'
        };
    }

    function checkAndLoadFilterOptions() {
        var $branchSelect = $('#filterBranchCode');
        var $modeSelect = $('#filterShipmentMode');
        var $custSelect = $('#filterCustomerId');

        if ($branchSelect.children('option').length <= 1 || $modeSelect.children('option').length === 0 || $custSelect.children('option').length === 0) {
            $.ajax({
                url: '/GPAnalysisReport/GetFilterOptions',
                type: 'GET',
                success: function (res) {
                    if (res && res.success) {
                        if ($branchSelect.children('option').length <= 1 && res.branches) {
                            res.branches.forEach(function (b) {
                                $branchSelect.append($('<option>', { value: b.value, text: b.text }));
                            });
                            if (typeof window.reinitCustomDropdownById === 'function') {
                                window.reinitCustomDropdownById('filterBranchCode');
                            }
                        }
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
        $('#statNetBillValue').text(summary.netBillValue != null ? window.gctlRenderers.formatCurrency(summary.netBillValue) : '0');
        $('#statTotalDue').text(summary.totalCosting != null ? window.gctlRenderers.formatCurrency(summary.totalCosting) : '0');
        $('#statprofit').text(summary.profit != null ? window.gctlRenderers.formatCurrency(summary.profit) : '0');
        var gp = summary.gpPercent || '0';
        if (gp === '0.00%' || gp === '0.00' || gp === '0%') gp = '0';
        $('#statLastServiceDate').text(gp);
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'gPAnalysisReportTable',
                extraOptions: {
                    lengthMenu: [[5, 10, 25, 100, -1], [5, 10, 25, 100, 'All']],
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ]
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.fromDate;
                    d.toDate = filters.toDate;
                    d.branchCode = filters.branchCode;
                    d.shipmentMode = filters.shipmentMode;
                    d.customerId = filters.customerId;
                    d.dateUse = filters.dateUse;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    $('#chkSelectAll').prop('checked', false);
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

        var fromVal = getDateValue('filterFromDate');
        var toVal = getDateValue('filterToDate');
        var fromDate = parseDate(fromVal);
        var toDate = parseDate(toVal);

        // From Date cannot be after To Date
        if (fromDate && toDate && fromDate > toDate) {
            var $from = $('#filterFromDate');
            $from.val(toVal);
            var fpFrom = $from.data('customFpInstance') || ($from[0] ? $from[0]._flatpickr : null);
            if (fpFrom) {
                fpFrom.setDate(toDate, false);
                if (fpFrom.altInput) fpFrom.altInput.value = toVal;
            }
        }

        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 300);
    }

    function getSelectedBillNos() {
        var selected = [];
        $('#gPAnalysisReportTable tbody .row-select-chk:checked').each(function () {
            var val = $(this).val();
            if (val && val.trim() !== '') {
                selected.push(val.trim());
            }
        });
        return selected.join(',');
    }

     //* Export report handler (PDF or Excel, defaults to PDF if nothing is selected)
    
    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = (getDropdownValue('filterExportFormat') || $('#filterExportFormat').val() || '').trim();
        if (!exportFormat) exportFormat = 'PDF';

        var selectedBills = getSelectedBillNos();

        var queryParams = $.param({
            fromDate: filters.fromDate,
            toDate: filters.toDate,
            branchCode: filters.branchCode,
            shipmentMode: filters.shipmentMode,
            customerId: filters.customerId,
            selectedBillNos: selectedBills,
            dateUse: filters.dateUse,
            exportFormat: exportFormat
        });

        window.location.href = '/GPAnalysisReport/Export?' + queryParams;
    }

    function onQuickRangeChange() {
        if (isSilentDropdownUpdate) return;
        var code = getDropdownValue('filterQuickRange');
        if (code && code !== 'custom') {
            applyQuickRange(code);
            isInitialized = true;
            onFilterChanged();
        }
    }

    function bindEvents() {
        $(document).on('change', '#filterFromDate, #filterToDate, .filter-section .flatpickr-alt-input', function () {
            if (isProgrammaticDateUpdate) return;
            silentSetDropdownValue('filterQuickRange', 'custom');
            onFilterChanged();
        });

        $('#filterQuickRange').on('change', onQuickRangeChange);

        $('#filterBranchCode, #filterShipmentMode, #filterCustomerId').on('change', function () {
            onFilterChanged();
        });

        // Header Select All Checkbox Handler
        $(document).on('change', '#chkSelectAll', function () {
            var isChecked = $(this).is(':checked');
            $('#gPAnalysisReportTable tbody .row-select-chk').prop('checked', isChecked);
        });

        // Individual Row Checkbox Handler
        $(document).on('change', '#gPAnalysisReportTable tbody .row-select-chk', function () {
            var total = $('#gPAnalysisReportTable tbody .row-select-chk').length;
            var checked = $('#gPAnalysisReportTable tbody .row-select-chk:checked').length;
            $('#chkSelectAll').prop('checked', total > 0 && total === checked);
        });

        // Delegate clicks on custom dropdown options, checkboxes, tag removals, and clear buttons
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-clear-icon, .custom-dd-item, .custom-dd-tag-remove, .custom-dd-select-all', function () {
            var $wrapper = $(this).closest('.custom-dd-wrapper');
            var selectId = $wrapper.prev('select').attr('id');
            setTimeout(function () {
                if (selectId === 'filterQuickRange') {
                    onQuickRangeChange();
                } else {
                    onFilterChanged();
                }
            }, 60);
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

        // Window resize & sidebar toggle column adjust
        var resizeDebounce = null;
        $(window).on('resize', function () {
            clearTimeout(resizeDebounce);
            resizeDebounce = setTimeout(function () {
                if (gridTable && typeof gridTable.columns === 'function' && typeof gridTable.columns.adjust === 'function') {
                    gridTable.columns.adjust();
                }
            }, 150);
        });
    }

})(jQuery);
