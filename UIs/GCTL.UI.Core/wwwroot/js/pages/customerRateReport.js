/**
 * Customer Rate Report Page Script
 * CustomGrid with Multi-Select Customer Filter, KPI Cards, Preview, PDF & Excel Export
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let gridTable = null;
    let filterDebounceTimer = null;

    $(document).ready(function () {
        initGrid();
        bindEvents();
    });

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

    function initGrid() {
        var $table = $('#customerRateReportTable');
        if (!$table.length) return;

        gridTable = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: false,
            paging: false,
            info: false,
            ordering: false,
            ajax: {
                url: '/CustomerRateReport/GetGridData',
                type: 'POST',
                data: function (d) {
                    d.CustomerIds = getDropdownValue('filterCustomerId');
                }
            },
            columns: [
                {
                    data: 'section',
                    className: 'text-center fw-bold',
                    width: '6%',
                    render: function (data) {
                        return data || '';
                    }
                },
                {
                    data: 'particulars',
                    className: 'text-start',
                    width: '54%',
                    render: function (data, type, row) {
                        if (row.isCustomerHeader) {
                            return '<span class="fw-bold text-primary fs-6">' + (data || '') + '</span>';
                        }
                        if (row.isHeader) {
                            return '<span class="fw-bold text-dark">' + (data || '') + '</span>';
                        }
                        return data || '';
                    }
                },
                {
                    data: 'formattedAmount',
                    className: 'text-end',
                    width: '20%',
                    render: function (data, type, row) {
                        if (row.isHeader || row.isCustomerHeader) {
                            return '';
                        }
                        return data || '';
                    }
                },
                {
                    data: 'remarks',
                    className: 'text-start',
                    width: '20%',
                    render: function (data) {
                        return data || '';
                    }
                }
            ],
            rowCallback: function (row, data) {
                if (data.isCustomerHeader) {
                    $(row).addClass('customer-header-row table-secondary');
                } else if (data.isHeader) {
                    $(row).addClass('rate-section-header table-light');
                }
            },
            language: {
                emptyTable: 'Please select one or more customers to view rate details.'
            }
        });
    }

    function bindEvents() {
        $('#filterCustomerId').on('change', function () {
            triggerFilterChange();
        });

        // Handle Export Format dropdown selection (native change)
        $('#filterExportFormat').on('change', function () {
            var val = $(this).val();
            if (val) {
                handleExportAction(val);
            }
        });

        // Handle Export Format dropdown selection (custom-select-2 click)
        $('.filter-section').on('click', '#filterExportFormat ~ .custom-dd-wrapper .custom-dd-option', function () {
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterExportFormat') || val;
                if (currentVal) {
                    handleExportAction(currentVal);
                }
            }, 50);
        });
    }

    function handleExportAction(action) {
        if (!action) return;
        var act = String(action).trim().toUpperCase();

        var customerIds = getDropdownValue('filterCustomerId');
        if (!customerIds) {
            if (typeof toastr !== 'undefined') {
                toastr.warning('Please select at least one Customer.');
            } else {
                alert('Please select at least one Customer.');
            }
            clearExportDropdown();
            return;
        }

        var queryParams = getExportQueryParams();

        if (act === 'PREVIEW' || act.indexOf('PREVIEW') !== -1) {
            window.open('/CustomerRateReport/ExportPdf?' + queryParams, '_blank');
        } else if (act === 'PDF') {
            window.location.href = '/CustomerRateReport/ExportPdf?' + queryParams + '&download=true';
        } else if (act === 'EXCEL' || act.indexOf('EXCEL') !== -1 || act === 'XLSX') {
            window.location.href = '/CustomerRateReport/ExportExcel?' + queryParams;
        }

        clearExportDropdown();
    }

    function clearExportDropdown() {
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

    function triggerFilterChange() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            onCustomerChanged();
        }, 250);
    }

    function onCustomerChanged() {
        if (gridTable) {
            gridTable.ajax.reload();
        }
        loadSummaryData();
    }

    function loadSummaryData() {
        var customerIds = getDropdownValue('filterCustomerId');

        if (!customerIds) {
            resetCounters();
            return;
        }

        $.ajax({
            url: '/CustomerRateReport/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ CustomerIds: customerIds }),
            success: function (res) {
                if (res && res.success) {
                    animateCounter('#statTotalChcItems', res.totalChcItems || 0, false);
                    animateCounter('#statTotalHandlingAmount', res.totalHandlingAmount || 0, true);
                    animateCounter('#statMinAgencyCommission', res.minAgencyCommission || 0, true);
                    $('#statOnInvoiceValue').text((res.onInvoiceValue != null ? Number(res.onInvoiceValue).toFixed(2) : '0.00') + '%');
                } else {
                    resetCounters();
                }
            },
            error: function () {
                resetCounters();
            }
        });
    }

    function resetCounters() {
        $('#statTotalChcItems').text('0');
        $('#statTotalHandlingAmount').text('0.00');
        $('#statMinAgencyCommission').text('0.00');
        $('#statOnInvoiceValue').text('0.00%');
    }

    function animateCounter(selector, target, isCurrency) {
        var $el = $(selector);
        if (!$el.length) return;

        var currentText = $el.text().replace(/,/g, '');
        var current = parseFloat(currentText) || 0;
        var diff = target - current;
        var steps = 12;
        var stepTime = 20;
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

    function getExportQueryParams() {
        var customerIds = getDropdownValue('filterCustomerId');
        return $.param({
            CustomerIds: customerIds
        });
    }

})(jQuery);
