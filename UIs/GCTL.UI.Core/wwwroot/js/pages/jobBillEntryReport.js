/**
 * Job Bill Entry Report Page Script
 * CustomGrid with Multi-Select Auto-Filtering & Top KPI Cards
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let gridTable = null;
    let filterDebounceTimer = null;
    let dateDebounceTimer = null;
    let branchDebounceTimer = null;
    let shipmentModeDebounceTimer = null;
    let customerDebounceTimer = null;
    let currentFilterXhr = null;
    let isQuickRangeApplying = false;

    $(document).ready(function () {
        initDefaultDates();
        initDatepickers();
        initGrid();
        bindEvents();
        loadSummaryData();
    });

    function initDefaultDates() {
        var now = new Date();
        var fromDate = new Date(now);
        fromDate.setMonth(now.getMonth() - 3);

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        if (!$('#filterFromDate').val()) {
            $('#filterFromDate').val(formatDate(fromDate));
        }
        if (!$('#filterToDate').val()) {
            $('#filterToDate').val(formatDate(now));
        }
        setSelectValue('filterQuickRange', '3m');
    }

    function setDateValue(id, date) {
        var el = document.getElementById(id);
        if (!el) return;
        var pad = function (n) { return String(n).padStart(2, '0'); };
        var dStr = pad(date.getDate()) + '/' + pad(date.getMonth() + 1) + '/' + date.getFullYear();
        var fp = (window.jQuery ? $(el).data('customFpInstance') : null) || el._flatpickr;
        if (fp) {
            fp.setDate(date, false);
        }
        el.value = dStr;
        if (window.jQuery) {
            $(el).trigger('change');
        }
    }

    function setSelectValue(id, val) {
        var $el = $('#' + id);
        if (!$el.length) return;
        $el.val(val);
        if (window.choiceManager) {
            window.choiceManager.setValue(id, val);
        }
        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.setValue === 'function') {
            inst.setValue(val);
        }
    }

    function applyQuickRange(code) {
        var today = new Date();
        var from = new Date(today);
        var to = new Date(today);

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

        isQuickRangeApplying = true;
        setDateValue('filterFromDate', from);
        setDateValue('filterToDate', to);
        setSelectValue('filterQuickRange', code);
        isQuickRangeApplying = false;
        onDateFilterChanged();
    }

    function parseDate(str) {
        if (!str) return null;
        var parts = String(str).split('/');
        if (parts.length === 3) {
            var day = parseInt(parts[0], 10);
            var month = parseInt(parts[1], 10) - 1;
            var year = parseInt(parts[2], 10);
            var d = new Date(year, month, day);
            if (!isNaN(d.getTime())) return d;
        }
        var d2 = new Date(str);
        return !isNaN(d2.getTime()) ? d2 : null;
    }

    function updateDateLimits() {
        var fromVal = $('#filterFromDate').val();
        var toVal = $('#filterToDate').val();

        var fromDate = parseDate(fromVal);
        var toDate = parseDate(toVal);

        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');

        var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

        if (fromFp) {
            fromFp.set('maxDate', toDate ? toDate : null);
        }
        if (toFp) {
            toFp.set('minDate', fromDate ? fromDate : null);
        }

        if ($.fn.datepicker) {
            if (toDate) {
                $('#filterFromDate').datepicker('setEndDate', toDate);
            } else {
                $('#filterFromDate').datepicker('setEndDate', false);
            }

            if (fromDate) {
                $('#filterToDate').datepicker('setStartDate', fromDate);
            } else {
                $('#filterToDate').datepicker('setStartDate', false);
            }
        }
    }

    function bindFlatpickrEvents() {
        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');

        var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

        if (fromFp && fromFp.config && Array.isArray(fromFp.config.onChange)) {
            fromFp.config.onChange.push(function (selectedDates) {
                var selected = selectedDates && selectedDates[0];
                if (toFp) {
                    toFp.set('minDate', selected || null);
                }
                if (!isQuickRangeApplying) {
                    setSelectValue('filterQuickRange', '');
                }
                onDateFilterChanged();
            });
        }

        if (toFp && toFp.config && Array.isArray(toFp.config.onChange)) {
            toFp.config.onChange.push(function (selectedDates) {
                var selected = selectedDates && selectedDates[0];
                if (fromFp) {
                    fromFp.set('maxDate', selected || null);
                }
                if (!isQuickRangeApplying) {
                    setSelectValue('filterQuickRange', '');
                }
                onDateFilterChanged();
            });
        }
    }

    function initDatepickers() {
        setTimeout(function () {
            bindFlatpickrEvents();
            updateDateLimits();
        }, 150);

        if ($.fn.datepicker) {
            $('.custom-datepicker').datepicker({
                format: 'dd/mm/yyyy',
                autoclose: true,
                todayHighlight: true
            }).on('changeDate', function () {
                if (!isQuickRangeApplying) {
                    setSelectValue('filterQuickRange', '');
                }
                onDateFilterChanged();
            });
        }
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'jobBillEntryReportTable',
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.FromDate;
                    d.toDate = filters.ToDate;
                    d.branchCode = filters.BranchCode;
                    d.shipmentModes = filters.ShipmentModes;
                    d.customerIds = filters.CustomerIds;
                    d.billNos = filters.BillNos;
                }
            });
        }
    }

    function bindEvents() {
        // Auto filter on date input change & enforce limits (cascades to all dropdowns)
        $('#filterFromDate, #filterToDate').on('change keyup', function () {
            if (!isQuickRangeApplying) {
                setSelectValue('filterQuickRange', '');
            }
            onDateFilterChanged();
        });

        // Quick range change
        $('#filterQuickRange').on('change', function () {
            var code = $(this).val();
            if (code) {
                applyQuickRange(code);
            }
        });

        // Branch change: cascades to Shipment Mode, Customer and Bill No, then reloads grid
        $('#filterBranch').on('change', function () {
            onBranchChanged();
        });

        // Shipment Mode change: cascades to Customer and Bill No, then reloads grid
        $('#filterShipmentMode').on('change', function () {
            onShipmentModeChanged();
        });

        // Customer change: cascades to Bill No, then reloads grid
        $('#filterCustomerId').on('change', function () {
            onCustomerChanged();
        });

        // Bill No change: reloads grid & summary
        $('#filterBillNo').on('change', function () {
            triggerFilterChange();
        });

        // Delegated clicks for custom-select-single options (Quick range & Branch)
        $(document).on('click', '.custom-dd-option, .custom-dd-item', function () {
            var $wrap = $(this).closest('.custom-dd-wrapper');
            var origId = $wrap.prev('select').attr('id');
            if (origId === 'filterQuickRange') {
                setTimeout(function () {
                    var code = $('#filterQuickRange').val();
                    if (code) {
                        applyQuickRange(code);
                    }
                }, 30);
            } else if (origId === 'filterBranch') {
                setTimeout(function () {
                    onBranchChanged();
                }, 30);
            }
        });

        // Reset Button Click
        $('#btnReset').on('click', function (e) {
            e.preventDefault();
            resetFilters();
        });

        // Preview PDF Button Click (opens in new tab)
        $('#btnPreview').on('click', function (e) {
            e.preventDefault();
            previewPdf();
        });

        // Export PDF Button Click (directly downloads file)
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportPdf();
        });
    }

    function resetFilters() {
        // 1. Reset Quick Range to '3m' (Last 3 Months) and set dates
        applyQuickRange('3m');
        setSelectValue('filterQuickRange', '3m');

        // 2. Clear other dropdowns
        if (typeof window.clearCustomDropdownById === 'function') {
            window.clearCustomDropdownById('filterBranch');
            window.clearCustomDropdownById('filterShipmentMode');
            window.clearCustomDropdownById('filterCustomerId');
            window.clearCustomDropdownById('filterBillNo');
        } else {
            $('#filterBranch').val('');
            $('#filterShipmentMode').val('');
            $('#filterCustomerId').val('');
            $('#filterBillNo').val('');
        }
        setSelectValue('filterBranch', '');

        // 3. Trigger reload
        onBranchChanged();
    }

    function onDateFilterChanged() {
        clearTimeout(dateDebounceTimer);
        dateDebounceTimer = setTimeout(function () {
            updateDateLimits();
            loadFilterOptions(true, true, true, function () {
                reloadGrid();
                loadSummaryData();
            });
        }, 350);
    }

    function onBranchChanged() {
        clearTimeout(branchDebounceTimer);
        branchDebounceTimer = setTimeout(function () {
            loadFilterOptions(true, true, true, function () {
                reloadGrid();
                loadSummaryData();
            });
        }, 150);
    }

    function onShipmentModeChanged() {
        clearTimeout(shipmentModeDebounceTimer);
        shipmentModeDebounceTimer = setTimeout(function () {
            loadFilterOptions(false, true, true, function () {
                reloadGrid();
                loadSummaryData();
            });
        }, 200);
    }

    function onCustomerChanged() {
        clearTimeout(customerDebounceTimer);
        customerDebounceTimer = setTimeout(function () {
            loadFilterOptions(false, false, true, function () {
                reloadGrid();
                loadSummaryData();
            });
        }, 200);
    }

    function loadFilterOptions(reloadShipmentModes, reloadCustomers, reloadBillNos, callback) {
        var fromDate = $('#filterFromDate').val() || '';
        var toDate = $('#filterToDate').val() || '';
        var branchCode = $('#filterBranch').val() || '';
        var shipmentModes = getDropdownValue('filterShipmentMode');
        var customerIds = getDropdownValue('filterCustomerId');

        if (currentFilterXhr && typeof currentFilterXhr.abort === 'function') {
            currentFilterXhr.abort();
        }

        currentFilterXhr = $.ajax({
            url: '/JobBillEntryReport/GetFilterOptions',
            type: 'GET',
            data: {
                fromDate: fromDate,
                toDate: toDate,
                branchCode: branchCode,
                shipmentModes: shipmentModes,
                customerIds: customerIds
            },
            success: function (res) {
                if (res && res.success) {
                    if (reloadShipmentModes) {
                        updateSelectOptions('filterShipmentMode', res.shipmentModes || []);
                    }
                    if (reloadCustomers) {
                        updateSelectOptions('filterCustomerId', res.customers || []);
                    }
                    if (reloadBillNos) {
                        updateSelectOptions('filterBillNo', res.billNos || []);
                    }
                }
                if (typeof callback === 'function') {
                    callback();
                }
            },
            error: function (xhr, status) {
                if (status !== 'abort') {
                    if (typeof callback === 'function') {
                        callback();
                    }
                }
            }
        });
    }

    function updateSelectOptions(selectId, items) {
        var $el = $('#' + selectId);
        if (!$el.length) return;

        var selectedValues = getDropdownValueArray(selectId);
        $el.empty();

        if (Array.isArray(items)) {
            items.forEach(function (item) {
                var val = String(item.value !== undefined ? item.value : (item.id !== undefined ? item.id : item));
                var text = String(item.text !== undefined ? item.text : (item.name !== undefined ? item.name : val));
                var isSelected = selectedValues.indexOf(val) !== -1;
                var $opt = $('<option></option>').val(val).text(text);
                if (isSelected) {
                    $opt.prop('selected', true);
                }
                $el.append($opt);
            });
        }

        if (typeof window.reinitCustomDropdownById === 'function') {
            window.reinitCustomDropdownById(selectId);
        }
    }

    function triggerFilterChange() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
            loadSummaryData();
        }, 300);
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('jobBillEntryReportTable', true);
        } else if (gridTable && gridTable.ajax) {
            gridTable.ajax.reload(null, true);
        }
    }

    /**
     * Helper to safely extract values from custom-select-multi dropdowns
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

    /**
     * Helper to get dropdown value as an array (for checking selection retention)
     */
    function getDropdownValueArray(id) {
        var raw = getDropdownValue(id);
        if (!raw) return [];
        return raw.split(',').map(function (v) { return v.trim(); }).filter(function (v) { return !!v; });
    }

    function getFilterParams() {
        return {
            FromDate: $('#filterFromDate').val() || '',
            ToDate: $('#filterToDate').val() || '',
            BranchCode: $('#filterBranch').val() || '',
            ShipmentModes: getDropdownValue('filterShipmentMode'),
            CustomerIds: getDropdownValue('filterCustomerId'),
            BillNos: getDropdownValue('filterBillNo')
        };
    }

    function loadSummaryData() {
        var filters = getFilterParams();

        $.ajax({
            url: '/JobBillEntryReport/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    animateCounter('#statTotalBill', res.total || 0, false);
                    animateCounter('#statNetServiceValue', res.netServiceValue || 0, true);
                    animateCounter('#statTotalDue', res.totalDue || 0, true);
                    animateCounter('#statLastServiceValue', res.lastServiceValue || 0, true);
                    var serviceDateText = res.lastServiceDate || '-';
                    $('#statLastServiceDate').text(serviceDateText).attr('title', serviceDateText);
                    var billNoText = res.lastBillNo || '-';
                    $('#statLastBillNo').text(billNoText).attr('title', billNoText);
                }
            },
            error: function () {
                // Keep default or current values on network failure
            }
        });
    }

    function animateCounter(selector, target, isCurrency) {
        var $el = $(selector);
        if (!$el.length) return;

        var currentText = $el.text().replace(/,/g, '');
        var current = parseFloat(currentText) || 0;
        var diff = target - current;
        var steps = 15;
        var stepTime = 25;
        var step = 0;

        if (diff === 0) {
            $el.text(isCurrency ? target.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : target.toLocaleString());
            return;
        }

        var timer = setInterval(function () {
            step++;
            var val = current + (diff * (step / steps));
            if (isCurrency) {
                $el.text(val.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
            } else {
                $el.text(Math.round(val).toLocaleString());
            }

            if (step >= steps) {
                clearInterval(timer);
                if (isCurrency) {
                    $el.text(target.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                } else {
                    $el.text(target.toLocaleString());
                }
            }
        }, stepTime);
    }

    function getExportQueryParams(isDownload) {
        var filters = getFilterParams();

        // Collect checked bill numbers from the grid checkboxes
        var selectedBills = [];
        $('#jobBillEntryReportTable tbody input.chk-bill-item:checked, #jobBillEntryReportTable tbody input[type="checkbox"]:checked').each(function () {
            var val = $(this).val() || $(this).data('id');
            if (val) {
                selectedBills.push(val);
            }
        });

        return $.param({
            FromDate: filters.FromDate,
            ToDate: filters.ToDate,
            BranchCode: filters.BranchCode,
            ShipmentModes: filters.ShipmentModes,
            CustomerIds: filters.CustomerIds,
            BillNos: filters.BillNos,
            SelectedBillNos: selectedBills.join(','),
            download: isDownload ? true : false
        });
    }

    function previewPdf() {
        var queryParams = getExportQueryParams(false);
        window.open('/JobBillEntryReport/ExportPdf?' + queryParams, '_blank');
    }

    function exportPdf() {
        var queryParams = getExportQueryParams(true);
        window.location.href = '/JobBillEntryReport/ExportPdf?' + queryParams;
    }

})(jQuery);
