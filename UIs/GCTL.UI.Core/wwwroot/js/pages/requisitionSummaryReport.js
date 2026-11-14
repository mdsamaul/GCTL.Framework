/**
 * Requisition Summary Report Page Script
 * CustomGrid with Multi-Select Auto-Filtering, KPI Cards & QuestPDF Export
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    // Global Renderers for Currency & Checkbox
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.renderSelectCheckbox = function (data, type, row) {
        var jobNo = data || (row && (row.jobNo || row.JobNo)) || '';
        return '<input type="checkbox" class="form-check-input chk-requisition-item m-0 shadow-none" value="' + jobNo + '" />';
    };

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
    let dateDebounceTimer = null;
    let currentFilterXhr = null;
    let isInitialized = false;
    let isProgrammaticDateUpdate = false;
    let isSilentDropdownUpdate = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }
        initDefaultDates();
        initDatepickers();
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
        loadSummaryData();
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
        };

        syncFlatpickr();
        setTimeout(syncFlatpickr, 100);
        setTimeout(syncFlatpickr, 300);
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
                onDateFilterChanged();
            });
        }

        if (toFp && toFp.config && Array.isArray(toFp.config.onChange)) {
            toFp.config.onChange.push(function (selectedDates) {
                var selected = selectedDates && selectedDates[0];
                if (fromFp) {
                    fromFp.set('maxDate', selected || null);
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
                onDateFilterChanged();
            });
        }
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'requisitionSummaryReportTable',
                extraOptions: {
                    lengthMenu: [[5, 10, 25, 100, -1], [5, 10, 25, 100, 'All']],
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ]
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.FromDate;
                    d.toDate = filters.ToDate;
                    d.FromDate = filters.FromDate;
                    d.ToDate = filters.ToDate;
                    d.customerIds = filters.CustomerIds;
                    d.portIds = filters.PortIds;
                    d.shipmentModeIds = filters.ShipmentModeIds;
                    d.jobNos = filters.JobNos;
                },
                onLoaded: function (json) {
                    $('#chkSelectAllRequisitions').prop('checked', false);
                }
            });

            // Prevent grid from getting stuck on skeleton rows if any unexpected error occurs
            $('#requisitionSummaryReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error in RequisitionSummaryReport:', techNote, message);
                $('#requisitionSummaryReportTable tbody').html('<tr><td colspan="9" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function bindEvents() {
        // Quick Range selection
        $(document).on('change', '#filterQuickRange', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).val();
            applyQuickRange(val);
            triggerFilterChange();
        });

        $('.filter-section').on('click', '#filterQuickRange ~ .custom-dd-wrapper .custom-dd-option', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterQuickRange') || val;
                applyQuickRange(currentVal);
                triggerFilterChange();
            }, 50);
        });

        // Date filter change
        $('#filterFromDate, #filterToDate').on('change keyup', function () {
            if (!isProgrammaticDateUpdate) {
                silentSetDropdownValue('filterQuickRange', 'custom');
            }
            onDateFilterChanged();
        });

        // Dropdown changes trigger grid & summary reload
        $('#filterCustomerId, #filterPortId, #filterShipmentMode, #filterJobNo').on('change', function () {
            triggerFilterChange();
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

    function onDateFilterChanged() {
        clearTimeout(dateDebounceTimer);
        dateDebounceTimer = setTimeout(function () {
            updateDateLimits();
            reloadGrid();
            loadSummaryData();
        }, 350);
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
            $.gctlGrid.reload('requisitionSummaryReportTable', true);
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

    function getFilterParams() {
        return {
            FromDate: getDateValue('filterFromDate'),
            ToDate: getDateValue('filterToDate'),
            CustomerIds: getDropdownValue('filterCustomerId'),
            PortIds: getDropdownValue('filterPortId'),
            ShipmentModeIds: getDropdownValue('filterShipmentMode'),
            JobNos: getDropdownValue('filterJobNo')
        };
    }

    function loadSummaryData() {
        var filters = getFilterParams();

        $.ajax({
            url: '/RequisitionSummaryReport/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    animateCounter('#statTotalJobs', res.totalJobs || 0, false);
                    animateCounter('#statTotalReceiptable', res.totalReceiptable || 0, true);
                    animateCounter('#statTotalNonReceiptable', res.totalNonReceiptable || 0, true);
                    animateCounter('#statGrandTotal', res.grandTotal || 0, true);
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

        // Collect checked Job numbers from the grid checkboxes
        var selectedJobs = [];
        $('#requisitionSummaryReportTable tbody input.chk-requisition-item:checked, #requisitionSummaryReportTable tbody input[type="checkbox"]:checked').each(function () {
            var val = $(this).val() || $(this).data('id');
            if (val) {
                selectedJobs.push(val);
            }
        });

        return $.param({
            FromDate: filters.FromDate,
            ToDate: filters.ToDate,
            CustomerIds: filters.CustomerIds,
            PortIds: filters.PortIds,
            ShipmentModeIds: filters.ShipmentModeIds,
            JobNos: filters.JobNos,
            SelectedJobNos: selectedJobs.join(','),
            download: isDownload ? true : false
        });
    }

    function previewPdf() {
        var queryParams = getExportQueryParams(false);
        window.open('/RequisitionSummaryReport/ExportPdf?' + queryParams, '_blank');
    }

    function exportPdf() {
        var queryParams = getExportQueryParams(true);
        window.location.href = '/RequisitionSummaryReport/ExportPdf?' + queryParams;
    }

})(jQuery);
