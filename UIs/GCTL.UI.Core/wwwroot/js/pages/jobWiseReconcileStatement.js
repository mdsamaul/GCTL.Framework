/**
 * Job Wise Reconcile Statement Page Script
 * Uses Application Global Classes: custom-select-single, custom-select-multi, custom-datepicker
 * Auto-Grid Loading on change & .xlsx Export
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let filterDebounceTimer = null;
    let isInternalUpdating = false;

    $(document).ready(function () {
        bindEvents();
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

    /**
     * Helper to safely extract date value from custom-datepicker
     */
    function getDateValue(id) {
        if (window.custom_Datepicker && typeof window.custom_Datepicker.getValue === 'function') {
            var val = window.custom_Datepicker.getValue('#' + id);
            if (val) return val;
        }
        return ($('#' + id).val() || '').trim();
    }

    function formatNumber(val) {
        if (val === null || val === undefined || val === '') return '';
        const num = parseFloat(val);
        if (isNaN(num)) return '';
        if (num === 0) return '';
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
        const $thead = $('#reconcileTableHeader');
        const $tbody = $('#reconcileTableBody');
        const $tfoot = $('#reconcileTableFooter');

        $thead.empty();
        $tbody.empty();
        $tfoot.empty();

        // 1. Header
        let headerHtml = '<tr>';
        headers.forEach(function (col) {
            let title = col;
            let alignClass = 'text-center';
            if (col.toLowerCase() === 'billamount') {
                title = 'Revenue/ Total Bill';
                alignClass = 'text-end';
            } else if (col.toLowerCase() === 'jobno') {
                title = 'Job No.';
                alignClass = 'text-center';
            } else if (col.toLowerCase() === 'customername') {
                title = 'Customer Name';
                alignClass = 'text-start';
            } else if (col.toLowerCase() === 'totalexpense' || col.toLowerCase() === 'profit') {
                alignClass = 'text-end';
            } else {
                alignClass = 'text-end';
            }
            headerHtml += `<th class="${alignClass}">${title}</th>`;
        });
        headerHtml += '</tr>';
        $thead.html(headerHtml);

        // 2. Data Rows
        let bodyHtml = '';
        rows.forEach(function (row) {
            bodyHtml += '<tr>';
            headers.forEach(function (col) {
                const val = row[col];
                const colLower = col.toLowerCase();

                if (colLower === 'jobno') {
                    bodyHtml += `<td class="text-center fw-medium">${val || ''}</td>`;
                } else if (colLower === 'customername') {
                    bodyHtml += `<td class="text-start">${val || ''}</td>`;
                } else {
                    const formatted = formatNumber(val);
                    const colorStyle = (colLower === 'profit' && parseFloat(val) < 0) ? 'color: #dc2626; font-weight: 600;' : '';
                    bodyHtml += `<td class="text-end" style="${colorStyle}">${formatted}</td>`;
                }
            });
            bodyHtml += '</tr>';
        });
        $tbody.html(bodyHtml);

        // 3. Footer Row
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
    }

    function updateKpis(summary) {
        if (!summary) return;
        $('#statTotalJobs').text(summary.totalJobs || 0);
        $('#statTotalRevenue').text(formatCurrency(summary.totalRevenue));
        $('#statTotalExpense').text(formatCurrency(summary.totalExpense));
        $('#statNetProfit').text(formatCurrency(summary.netProfit));

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
    }

    function triggerExport() {
        const params = getFilterParams();
        params.exportFormat = 'XLSX';
        const query = $.param(params);
        window.location.href = `/JobWiseReconcileStatement/ExportExcel?${query}`;
    }

    function bindEvents() {
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

            const today = new Date();
            const yyyy = today.getFullYear();
            const mm = String(today.getMonth() + 1).padStart(2, '0');
            const dd = String(today.getDate()).padStart(2, '0');
            const firstDay = `01/${mm}/${yyyy}`;
            const currentDay = `${dd}/${mm}/${yyyy}`;

            if (window.custom_Datepicker && typeof window.custom_Datepicker.setValue === 'function') {
                window.custom_Datepicker.setValue('#filterFromDate', firstDay);
                window.custom_Datepicker.setValue('#filterToDate', currentDay);
            } else {
                $('#filterFromDate').val(firstDay);
                $('#filterToDate').val(currentDay);
            }

            isInternalUpdating = false;
            loadJobNos();
            loadReportData();
        });

        // Automatic data loading on change
        $('#filterShipmentMode, #filterCustomerId').on('change', function () {
            onFilterChanged();
            loadJobNos();
        });

        $('#filterJobNo').on('change', function () {
            onFilterChanged();
        });

        $('#filterFromDate, #filterToDate').on('change', function () {
            onFilterChanged();
            loadJobNos();
        });
    }

})(jQuery);
