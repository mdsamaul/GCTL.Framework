(function ($) {
    'use strict';

    // Global Renderers for Currency
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.renderSelectCheckbox = function (data, type, row) {
        var ofrNo = data || (row && (row.ofrNo || row.Ofrno)) || '';
        return '<input type="checkbox" class="form-check-input row-select-chk m-0 shadow-none" value="' + ofrNo + '" />';
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
    let isInitialized = false;
    let isProgrammaticDateUpdate = false;
    let isSilentDropdownUpdate = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        initDefaultDates();
        initGrid();
        bindEvents();

        // Always ensure Job Nos are loaded and initialized in the multi-select dropdown
        if ($('#filterJobNo option').length === 0) {
            reloadJobNos();
        }

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

        // Sync Flatpickr instances so the calendar has the date selected like in GPAnalysisReport
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
                reloadJobNos();
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
                reloadJobNos();
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

    // Safely extract values from custom dropdown widget or standard select
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
        return {
            fromDate: getDateValue('filterFromDate'),
            toDate: getDateValue('filterToDate'),
            branchCode: getDropdownValue('filterBranchCode'),
            shipmentModeId: getDropdownValue('filterShipmentMode'),
            customerId: getDropdownValue('filterCustomerId'),
            jobNo: getDropdownValue('filterJobNo'),
            ofrReportType: getDropdownValue('filterReportType') || '',
            serviceProvidedById: getDropdownValue('filterServiceProvidedBy'),
            exportFormat: getDropdownValue('filterExportFormat') || 'PDF'
        };
    }

    function buildQueryString(extraParams) {
        var filters = getFilterParams();
        var params = new URLSearchParams();

        if (filters.fromDate) params.append('fromDate', filters.fromDate);
        if (filters.toDate) params.append('toDate', filters.toDate);
        if (filters.branchCode) params.append('branchCode', filters.branchCode);
        if (filters.shipmentModeId) params.append('shipmentModeId', filters.shipmentModeId);
        if (filters.customerId) params.append('customerId', filters.customerId);
        if (filters.jobNo) params.append('jobNo', filters.jobNo);
        if (filters.ofrReportType) params.append('ofrReportType', filters.ofrReportType);
        if (filters.serviceProvidedById) params.append('serviceProvidedById', filters.serviceProvidedById);
        if (filters.exportFormat) params.append('exportFormat', filters.exportFormat);

        if (extraParams && typeof extraParams === 'object') {
            for (var key in extraParams) {
                if (extraParams.hasOwnProperty(key)) {
                    params.set(key, extraParams[key]);
                }
            }
        }

        return params.toString();
    }

    function updateKpiCards(summary) {
        if (!summary) return;

        $('#statTotalRequisitions').text(summary.totalRequisitions != null ? summary.totalRequisitions.toLocaleString() : '0');
        $('#statTotalReqValue').text(summary.totalRequisitionAmount != null ? window.gctlRenderers.formatCurrency(summary.totalRequisitionAmount) : '0.00');
        $('#statTotalAppValue').text(summary.totalApprovedAmount != null ? window.gctlRenderers.formatCurrency(summary.totalApprovedAmount) : '0.00');
        $('#statTotalReceiptable').text(summary.totalReceiptableAmount != null ? window.gctlRenderers.formatCurrency(summary.totalReceiptableAmount) : '0.00');
        $('#statTotalNonReceiptable').text(summary.totalNonReceiptableAmount != null ? window.gctlRenderers.formatCurrency(summary.totalNonReceiptableAmount) : '0.00');
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'ofrReportTable',
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
                    d.shipmentModeId = filters.shipmentModeId;
                    d.customerId = filters.customerId;
                    d.jobNo = filters.jobNo;
                    d.ofrReportType = filters.ofrReportType;
                    d.serviceProvidedById = filters.serviceProvidedById;
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
            $('#ofrReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error in OFRReport:', techNote, message);
                $('#ofrReportTable tbody').html('<tr><td colspan="12" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('ofrReportTable', true);
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


    var jobNoDebounceTimer = null;
    function reloadJobNos() {
        clearTimeout(jobNoDebounceTimer);
        jobNoDebounceTimer = setTimeout(function () {
            var filters = getFilterParams();
            var $jobSelect = $('#filterJobNo');
            var currentRaw = getDropdownValue('filterJobNo');
            var currentArr = currentRaw ? currentRaw.split(',') : [];

            $.ajax({
                url: '/OFRReport/GetJobNos',
                type: 'GET',
                data: {
                    customerId: filters.customerId || '',
                    shipmentModeId: filters.shipmentModeId || ''
                },
                success: function (data) {
                    $jobSelect.empty();

                    if (Array.isArray(data) && data.length > 0) {
                        $.each(data, function (i, job) {
                            var isSel = currentArr.indexOf(job) !== -1 ? 'selected' : '';
                            $jobSelect.append('<option value="' + job + '" ' + isSel + '>' + job + '</option>');
                        });
                    }

                    if (typeof window.reinitCustomDropdownById === 'function') {
                        window.reinitCustomDropdownById('filterJobNo');
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error fetching cascading Job Nos:', error);
                }
            });
        }, 150);
    }

    function openPreviewModal(specificJobNo) {
        var extra = {};
        if (specificJobNo) {
            extra.jobNo = specificJobNo;
        }

        var qs = buildQueryString(extra);
        var url = '/OFRReport/PreviewPdf?' + qs;

        // Open in a new browser tab
        window.open(url, '_blank');
    }

    function exportReport(format) {
        var filters = getFilterParams();
        var exportFormat = (format || filters.exportFormat || 'PDF').trim().toUpperCase();

        var selectedOfrs = [];
        $('#ofrReportTable tbody .row-select-chk:checked').each(function () {
            var val = $(this).val();
            if (val && val.trim() !== '') {
                selectedOfrs.push(val.trim());
            }
        });

        var queryParams = $.param({
            fromDate: filters.fromDate,
            toDate: filters.toDate,
            branchCode: filters.branchCode,
            shipmentModeId: filters.shipmentModeId,
            customerId: filters.customerId,
            jobNo: filters.jobNo,
            ofrReportType: filters.ofrReportType,
            serviceProvidedById: filters.serviceProvidedById,
            selectedOfrNos: selectedOfrs.join(','),
            exportFormat: exportFormat
        });

        window.location.href = '/OFRReport/Export?' + queryParams;
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
        // Quick Range selection
        $(document).on('change', '#filterQuickRange', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).val();
            applyQuickRange(val);
            onFilterChanged();
            reloadJobNos();
        });

        $('.filter-section').on('click', '#filterQuickRange ~ .custom-dd-wrapper .custom-dd-option', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterQuickRange') || val;
                applyQuickRange(currentVal);
                onFilterChanged();
                reloadJobNos();
            }, 50);
        });

        // Flatpickr & standard input change
        $(document).on('change', '#filterFromDate, #filterToDate', function () {
            if (!isProgrammaticDateUpdate) {
                silentSetDropdownValue('filterQuickRange', 'custom');
            }
            onFilterChanged();
            reloadJobNos();
        });

        $(document).on('change', '#filterBranchCode', function () {
            onFilterChanged();
            reloadJobNos();
        });

        $('#filterShipmentMode, #filterCustomerId, #filterJobNo, #filterReportType, #filterServiceProvidedBy')
            .on('change', function () {
                onFilterChanged();
            });

        // Header Select All Checkbox Handler
        $(document).on('change', '#chkSelectAll', function () {
            var isChecked = $(this).is(':checked');
            $('#ofrReportTable tbody .row-select-chk').prop('checked', isChecked);
        });

        // Individual Row Checkbox Handler
        $(document).on('change', '#ofrReportTable tbody .row-select-chk', function () {
            var total = $('#ofrReportTable tbody .row-select-chk').length;
            var checked = $('#ofrReportTable tbody .row-select-chk:checked').length;
            $('#chkSelectAll').prop('checked', total > 0 && total === checked);
        });

        // Delegate clicks on custom dropdown options, checkboxes, tag removals, and clear buttons (from custom-select-2 widget)
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-tag-remove, .custom-dd-select-all', function () {
            setTimeout(function () {
                onFilterChanged();
            }, 25);
        });

        // Cascade JobNos when Customer or ShipmentMode changes (option or clear click)
        $('.filter-section').on('click', '#filterCustomerId ~ .custom-dd-wrapper .custom-dd-option, #filterCustomerId ~ .custom-dd-wrapper .custom-dd-clear, #filterShipmentMode ~ .custom-dd-wrapper .custom-dd-option, #filterShipmentMode ~ .custom-dd-wrapper .custom-dd-clear', function () {
            setTimeout(function () {
                reloadJobNos();
            }, 100);
        });

        $('#filterCustomerId, #filterShipmentMode').on('change', function () {
            reloadJobNos();
        });

        // Export dropdown selection: Preview / PDF / Excel action
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

        // Row preview button (inside grid)
        $(document).on('click', '.btn-preview-row', function (e) {
            e.preventDefault();
            var job = $(this).data('job');
            openPreviewModal(job);
        });

        // Modal PDF download button
        $('#btnModalDownloadPdf').on('click', function (e) {
            e.preventDefault();
            var qs = buildQueryString();
            window.location.href = '/OFRReport/ExportPdf?' + qs;
        });

        // Clean modal frame on hide
        $('#ofrPreviewModal').on('hidden.bs.modal', function () {
            $('#modalPdfFrame').attr('src', '');
        });
    }

})(jQuery);
