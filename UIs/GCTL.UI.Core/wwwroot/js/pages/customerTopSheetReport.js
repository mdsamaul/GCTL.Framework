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
        initGrid();
        bindEvents();

        if ($('#filterBillNo option').length === 0) {
            reloadBillNos();
        }

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

        var fromStr = formatDate(currentMonthFirstDay);
        var toStr = formatDate(now);

        var $from = $('#filterFromDate');
        var $to = $('#filterToDate');

        if (!$from.val()) {
            $from.val(fromStr);
        }
        if (!$to.val()) {
            $to.val(toStr);
        }

        var syncFlatpickr = function () {
            var fpFrom = $from.data('customFpInstance') || ($from[0] ? $from[0]._flatpickr : null);
            if (fpFrom) {
                if (!fpFrom.selectedDates || !fpFrom.selectedDates.length) {
                    fpFrom.setDate(currentMonthFirstDay, false);
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
                    fpTo.setDate(now, false);
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
                onFilterChanged();
                reloadBillNos();
            });
        }

        if (toFp.config && Array.isArray(toFp.config.onChange)) {
            toFp.config.onChange.push(function (selectedDates) {
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
                onFilterChanged();
                reloadBillNos();
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
            if (fp.selectedDates && fp.selectedDates.length) {
                var d = fp.selectedDates[0];
                return String(d.getDate()).padStart(2, '0') + '/' +
                    String(d.getMonth() + 1).padStart(2, '0') + '/' +
                    d.getFullYear();
            }
        }
        return $el.val() ? $el.val().trim() : '';
    }

    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.getValue === 'function') {
            var val = inst.getValue();
            if (Array.isArray(val)) {
                return val.join(',');
            }
            return (val !== null && val !== undefined) ? String(val).trim() : '';
        }

        var nativeVal = $el.val();
        if (Array.isArray(nativeVal)) {
            return nativeVal.join(',');
        }
        return nativeVal ? String(nativeVal).trim() : '';
    }

    function getFilterParams() {
        return {
            fromDate: getDateValue('filterFromDate'),
            toDate: getDateValue('filterToDate'),
            shipmentModeId: getDropdownValue('filterShipmentMode'),
            customerId: getDropdownValue('filterCustomerId'),
            billNo: getDropdownValue('filterBillNo'),
            exportFormat: getDropdownValue('filterExportFormat')
        };
    }

    function buildQueryString(extraParams) {
        var p = getFilterParams();
        if (extraParams) {
            $.extend(p, extraParams);
        }
        var clean = {};
        $.each(p, function (k, v) {
            if (v !== null && v !== undefined && String(v).trim() !== '') {
                clean[k] = v;
            }
        });
        return $.param(clean);
    }

    function updateKpiCards(summary) {
        if (!summary) return;

        $('#statTotalCustomers').text(summary.totalCustomers || 0);
        $('#statTotalBills').text(summary.totalBills || 0);
        $('#statTotalBillAmount').text(window.gctlRenderers.formatCurrency(summary.totalBillAmount || 0));
        $('#statTotalTransportLabour').text(window.gctlRenderers.formatCurrency(summary.totalTransportLabour || 0));
        $('#statTotalCommission').text(window.gctlRenderers.formatCurrency(summary.totalCommission || 0));
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'customerTopSheetReportTable',
                extraOptions: {
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ]
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.fromDate;
                    d.toDate = filters.toDate;
                    d.shipmentModeId = filters.shipmentModeId;
                    d.customerId = filters.customerId;
                    d.billNo = filters.billNo;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    if (json && json.summary) {
                        updateKpiCards(json.summary);
                    }
                }
            });

            $('#customerTopSheetReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error in CustomerTopSheetReport:', techNote, message);
                $('#customerTopSheetReportTable tbody').html('<tr><td colspan="16" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('customerTopSheetReportTable', true);
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
        }, 100);
    }

    var billNoDebounceTimer = null;
    function reloadBillNos() {
        clearTimeout(billNoDebounceTimer);
        billNoDebounceTimer = setTimeout(function () {
            var filters = getFilterParams();
            var $billSelect = $('#filterBillNo');
            var currentRaw = getDropdownValue('filterBillNo');
            var currentArr = currentRaw ? currentRaw.split(',') : [];

            $.ajax({
                url: '/CustomerTopSheetReport/GetBillNos',
                type: 'GET',
                data: {
                    customerId: filters.customerId || '',
                    shipmentModeId: filters.shipmentModeId || '',
                    fromDate: filters.fromDate || '',
                    toDate: filters.toDate || ''
                },
                success: function (data) {
                    $billSelect.empty();

                    if (Array.isArray(data) && data.length > 0) {
                        $.each(data, function (i, bill) {
                            var isSel = currentArr.indexOf(bill) !== -1 ? 'selected' : '';
                            $billSelect.append('<option value="' + bill + '" ' + isSel + '>' + bill + '</option>');
                        });
                    }

                    if (typeof window.reinitCustomDropdownById === 'function') {
                        window.reinitCustomDropdownById('filterBillNo');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error fetching cascading Bill Nos:', error);
                }
            });
        }, 150);
    }

    function openPreviewModal(specificBillNo) {
        var extra = {};
        if (specificBillNo) {
            extra.billNo = specificBillNo;
        }

        var qs = buildQueryString(extra);
        var url = '/CustomerTopSheetReport/PreviewPdf?' + qs;

        // Open PDF Preview in a new browser tab
        window.open(url, '_blank');
    }

    function exportReport(format) {
        var filters = getFilterParams();
        var exportFormat = (format || filters.exportFormat || 'PDF').trim().toUpperCase();

        var queryParams = $.param({
            fromDate: filters.fromDate,
            toDate: filters.toDate,
            shipmentModeId: filters.shipmentModeId,
            customerId: filters.customerId,
            billNo: filters.billNo,
            exportFormat: exportFormat
        });

        window.location.href = '/CustomerTopSheetReport/Export?' + queryParams;
    }

    function handleExportAction(action) {
        if (!action) return;
        var act = String(action).trim().toUpperCase();

        if (act === 'PREVIEW' || act.indexOf('PREVIEW') !== -1) {
            openPreviewModal();
        } else if (act === 'PDF') {
            exportReport('PDF');
        } else if (act.indexOf('EXCEL') !== -1 || act === 'XLSX') {
            exportReport('EXCEL');
        } else if (act === 'CSV') {
            exportReport('CSV');
        }

        // Reset dropdown back to placeholder so user can select again
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
        $(document).on('change', '#filterFromDate, #filterToDate', function () {
            onFilterChanged();
            reloadBillNos();
        });

        $('#filterShipmentMode, #filterCustomerId, #filterBillNo')
            .on('change', function () {
                onFilterChanged();
            });

        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-tag-remove, .custom-dd-select-all', function () {
            setTimeout(function () {
                onFilterChanged();
            }, 25);
        });

        $('.filter-section').on('click', '#filterCustomerId ~ .custom-dd-wrapper .custom-dd-option, #filterCustomerId ~ .custom-dd-wrapper .custom-dd-clear, #filterShipmentMode ~ .custom-dd-wrapper .custom-dd-option, #filterShipmentMode ~ .custom-dd-wrapper .custom-dd-clear', function () {
            setTimeout(function () {
                reloadBillNos();
            }, 100);
        });

        $('#filterCustomerId, #filterShipmentMode').on('change', function () {
            reloadBillNos();
        });

        $('#filterExportFormat').on('change', function () {
            var val = $(this).val();
            if (val) {
                handleExportAction(val);
            }
        });

        $('.filter-section').on('click', '#filterExportFormat ~ .custom-dd-wrapper .custom-dd-option', function () {
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterExportFormat') || val;
                if (currentVal) {
                    handleExportAction(currentVal);
                }
            }, 50);
        });

        $('#btnModalDownloadPdf').on('click', function (e) {
            e.preventDefault();
            var qs = buildQueryString();
            window.location.href = '/CustomerTopSheetReport/ExportPdf?' + qs;
        });

        $('#btnModalDownloadExcel').on('click', function (e) {
            e.preventDefault();
            var qs = buildQueryString();
            window.location.href = '/CustomerTopSheetReport/ExportExcel?' + qs;
        });

        $('#customerTopSheetPreviewModal').on('hidden.bs.modal', function () {
            $('#topSheetModalPdfFrame').attr('src', '');
        });
    }

})(jQuery);
