/**
 * Job Wise Reconcile Statement Page Script
 * Uses Application Global Classes: custom-select-single, custom-select-multi, custom-datepicker
 * Auto-Grid Loading on change, Quick Range (Last 3 Months default), DataTables (5, 10, 25, 100, All) & .xlsx Export
 * Aligned with C&F Management Dashboard (SP_Dashboard_JobReconcile)
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let gridTable = null;
    let filterDebounceTimer = null;
    let dateDebounceTimer = null;
    let isInternalUpdating = false;
    let isProgrammaticDateUpdate = false;
    let isSilentDropdownUpdate = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }
        initDefaultDates();
        initDatepickers();
        bindEvents();
        setTimeout(function () {
            if (!getDropdownValue('filterQuickRange')) {
                silentSetDropdownValue('filterQuickRange', '3m');
            }
        }, 50);
        // Automatically load data on initial page load
        loadReportData();
    });

    /**
     * Helper to safely extract values from custom-select-single / custom-select-multi dropdowns
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

    /**
     * Helper to safely extract date value from custom-datepicker or flatpickr
     */
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

        if (window.custom_Datepicker && typeof window.custom_Datepicker.getValue === 'function') {
            var val = window.custom_Datepicker.getValue('#' + id);
            if (val) return val;
        }

        return ($el.val() || '').trim();
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
        var fromVal = getDateValue('filterFromDate');
        var toVal = getDateValue('filterToDate');

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
    }

    function initDatepickers() {
        setTimeout(function () {
            var fromEl = document.getElementById('filterFromDate');
            var toEl = document.getElementById('filterToDate');
            var fromFp = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
            var toFp = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

            if (fromFp && fromFp.config && Array.isArray(fromFp.config.onChange)) {
                fromFp.config.onChange.push(function (selectedDates) {
                    var selected = selectedDates && selectedDates[0];
                    if (toFp) toFp.set('minDate', selected || null);
                    onDateFilterChanged();
                });
            }
            if (toFp && toFp.config && Array.isArray(toFp.config.onChange)) {
                toFp.config.onChange.push(function (selectedDates) {
                    var selected = selectedDates && selectedDates[0];
                    if (fromFp) fromFp.set('maxDate', selected || null);
                    onDateFilterChanged();
                });
            }
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

    function formatNumber(val) {
        if (val === null || val === undefined || val === '') return '-';
        const num = parseFloat(val);
        if (isNaN(num)) return '-';
        if (Math.abs(num) < 0.00001) return '-';
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatCurrency(val) {
        if (val === null || val === undefined || val === '') return '0.00';
        const num = parseFloat(val);
        if (isNaN(num)) return '0.00';
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function getFilterParams() {
        return {
            expenseTypeId: getDropdownValue('filterShipmentMode'),
            fromDate: getDateValue('filterFromDate'),
            toDate: getDateValue('filterToDate'),
            customerId: getDropdownValue('filterCustomerId'),
            jobNo: getDropdownValue('filterJobNo'),
            exportFormat: 'XLSX'
        };
    }

    function onDateFilterChanged() {
        clearTimeout(dateDebounceTimer);
        dateDebounceTimer = setTimeout(function () {
            updateDateLimits();
            loadJobNos();
            loadReportData();
        }, 350);
    }

    function onFilterChanged() {
        if (isInternalUpdating) return;
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            loadReportData();
        }, 300);
    }

    function loadJobNos() {
        const params = {
            expenseTypeId: getDropdownValue('filterShipmentMode'),
            customerId: getDropdownValue('filterCustomerId'),
            fromDate: getDateValue('filterFromDate'),
            toDate: getDateValue('filterToDate')
        };

        $.ajax({
            url: '/JobWiseReconcileStatement/GetJobNos',
            type: 'GET',
            data: params,
            success: function (data) {
                const $jobSelect = $('#filterJobNo');
                const prevVal = getDropdownValue('filterJobNo');
                const selectedList = prevVal ? prevVal.split(',') : [];

                isInternalUpdating = true;
                $jobSelect.empty();

                if (Array.isArray(data)) {
                    data.forEach(function (jobNo) {
                        const isSelected = selectedList.includes(jobNo);
                        const opt = new Option(jobNo, jobNo, isSelected, isSelected);
                        $jobSelect.append(opt);
                    });
                }

                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById('filterJobNo');
                }

                isInternalUpdating = false;
            }
        });
    }

    function loadReportData() {
        const params = getFilterParams();

        $('#loadingIndicator').show();
        $('#emptyPlaceholder').hide();
        $('#tableWrapper').hide();

        $.ajax({
            url: '/JobWiseReconcileStatement/GetGridData',
            type: 'POST',
            data: params,
            success: function (res) {
                $('#loadingIndicator').hide();
                if (!res || !res.success || !res.headers || res.headers.length === 0 || !res.data || res.data.length === 0) {
                    $('#emptyPlaceholder').show();
                    resetKpis();
                    return;
                }

                renderGrid(res.headers, res.data, res.summary);
                updateKpis(res.summary);
                $('#tableWrapper').show();
            },
            error: function (xhr, status, err) {
                $('#loadingIndicator').hide();
                $('#emptyPlaceholder').show();
                resetKpis();
                console.error("Error loading reconcile report:", err);
            }
        });
    }

    function renderGrid(headers, rows, summary) {
        const $table = $('#reconcileReportTable');
        const $thead = $('#reconcileTableHeader');
        const $tbody = $('#reconcileTableBody');
        const $tfoot = $('#reconcileTableFooter');

        // Destroy previous DataTable instance cleanly
        if ($.fn.DataTable.isDataTable('#reconcileReportTable')) {
            $table.DataTable().clear().destroy();
        }

        $thead.empty();
        $tbody.empty();
        $tfoot.empty();

        // 1. Build Header & Column Definitions
        let headerHtml = '<tr>';
        let columnDefs = [];
        let orderColIdx = 2; // Default to BillAmount column

        headers.forEach(function (col, idx) {
            let title = col;
            let colLower = col.toLowerCase();
            let alignClass = 'text-end';
            let isText = false;

            if (colLower === 'billamount') {
                title = 'Revenue/ Total Bill';
                alignClass = 'text-end';
                orderColIdx = idx;
            } else if (colLower === 'jobno') {
                title = 'Job No.';
                alignClass = 'text-center';
                isText = true;
            } else if (colLower === 'customername') {
                title = 'Customer Name';
                alignClass = 'text-start';
                isText = true;
            } else if (colLower === 'totalexpense') {
                title = 'Total Expense';
                alignClass = 'text-end';
            } else if (colLower === 'profit') {
                title = 'Gross Profit';
                alignClass = 'text-end';
            }

            headerHtml += `<th class="${alignClass}">${title}</th>`;

            columnDefs.push({
                data: col,
                className: alignClass,
                orderable: true,
                defaultContent: '-',
                render: function (data, type, row) {
                    if (type === 'sort' || type === 'type') {
                        if (isText) return data || '';
                        var num = parseFloat(data);
                        return isNaN(num) ? 0 : num;
                    }
                    if (isText) {
                        return data || '';
                    }
                    var formatted = formatNumber(data);
                    if (colLower === 'profit') {
                        var pVal = parseFloat(data);
                        if (!isNaN(pVal) && pVal < 0) {
                            return `<span style="color: #dc2626; font-weight: 600;">${formatted}</span>`;
                        }
                    }
                    return formatted;
                }
            });
        });
        headerHtml += '</tr>';
        $thead.html(headerHtml);

        // 2. Build Footer Row with overall column totals
        if (summary && summary.columnTotals) {
            let footHtml = '<tr>';
            headers.forEach(function (col, idx) {
                const colLower = col.toLowerCase();
                if (idx === 0) {
                    footHtml += '<td class="text-center fw-bold">TOTAL</td>';
                } else if (colLower === 'customername') {
                    footHtml += '<td></td>';
                } else {
                    const totalVal = summary.columnTotals[col] || 0;
                    const formatted = formatCurrency(totalVal);
                    const colorStyle = (colLower === 'profit' && totalVal < 0) ? 'color: #dc2626;' : '';
                    footHtml += `<td class="text-end fw-bold" style="${colorStyle}">${formatted}</td>`;
                }
            });
            footHtml += '</tr>';
            $tfoot.html(footHtml);
        }

        // 3. Initialize DataTables with 5, 10, 25, 100, All pagination
        gridTable = $table.DataTable({
            data: rows,
            columns: columnDefs,
            pageLength: 10,
            lengthMenu: [[5, 10, 25, 100, -1], [5, 10, 25, 100, 'All']],
            destroy: true,
            autoWidth: false,
            order: [[orderColIdx, 'desc']],
            language: {
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
            }
        });
    }

    function updateKpis(summary) {
        if (!summary) return;
        $('#statTotalJobs').text((summary.totalJobs || 0).toLocaleString());
        $('#statTotalRevenue').text(formatCurrency(summary.totalRevenue));
        $('#statTotalExpense').text(formatCurrency(summary.totalExpense));
        $('#statNetProfit').text(formatCurrency(summary.netProfit));

        var gpMargin = summary.gpMargin;
        if (gpMargin === undefined || gpMargin === null) {
            gpMargin = (summary.totalRevenue > 0) ? ((summary.netProfit * 100) / summary.totalRevenue) : 0;
        }
        $('#statGpMargin').text((gpMargin || 0).toFixed(2) + '%');

        if (summary.netProfit < 0) {
            $('#statNetProfit').css('color', '#dc2626');
        } else {
            $('#statNetProfit').css('color', '#2e7d32');
        }
    }

    function resetKpis() {
        $('#statTotalJobs').text('0');
        $('#statTotalRevenue').text('0.00');
        $('#statTotalExpense').text('0.00');
        $('#statNetProfit').text('0.00').css('color', 'var(--secondary)');
        $('#statGpMargin').text('0.00%');
    }

    function triggerExport() {
        const params = getFilterParams();
        params.exportFormat = 'XLSX';
        const query = $.param(params);
        window.location.href = `/JobWiseReconcileStatement/ExportExcel?${query}`;
    }

    function bindEvents() {
        // Quick Range selection
        $(document).on('change', '#filterQuickRange', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).val();
            applyQuickRange(val);
            onDateFilterChanged();
        });

        $('.filter-section').on('click', '#filterQuickRange ~ .custom-dd-wrapper .custom-dd-option', function () {
            if (isSilentDropdownUpdate) return;
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterQuickRange') || val;
                applyQuickRange(currentVal);
                onDateFilterChanged();
            }, 50);
        });

        // Date filter change
        $('#filterFromDate, #filterToDate').on('change keyup', function () {
            if (!isProgrammaticDateUpdate) {
                silentSetDropdownValue('filterQuickRange', 'custom');
            }
            onDateFilterChanged();
        });

        // Single Excel download button (.xlsx)
        $('#btnExportExcel').on('click', function () {
            triggerExport();
        });

        // Reset button
        $('#btnReset').on('click', function () {
            isInternalUpdating = true;

            if (typeof window.clearCustomDropdownById === 'function') {
                window.clearCustomDropdownById('filterShipmentMode');
                window.clearCustomDropdownById('filterCustomerId');
                window.clearCustomDropdownById('filterJobNo');
            } else {
                $('#filterShipmentMode').val('');
                $('#filterCustomerId').val('');
                $('#filterJobNo').val('');
            }

            silentSetDropdownValue('filterQuickRange', '3m');
            applyQuickRange('3m');

            isInternalUpdating = false;
            loadJobNos();
            loadReportData();
        });

        // Automatic data loading on dropdown change
        $('#filterShipmentMode, #filterCustomerId').on('change', function () {
            onFilterChanged();
            loadJobNos();
        });

        $('#filterJobNo').on('change', function () {
            onFilterChanged();
        });
    }

})(jQuery);
