/**
 * GP Analysis Report Page Script
 * CustomGrid with Multi-Level Headers, Server-Side Pagination & Customer Grouping
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
        initGrid();
        bindEvents();
        loadSummaryData();
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

    function getFilterParams() {
        var shipmentModeVal = $('#filterShipmentMode').val();
        var customerIdVal = $('#filterCustomerId').val();

        var shipmentMode = Array.isArray(shipmentModeVal) ? shipmentModeVal.join(',') : (shipmentModeVal || '');
        var customerId = Array.isArray(customerIdVal) ? customerIdVal.join(',') : (customerIdVal || '');

        var fromDate = $('#filterFromDate').val() || '';
        var toDate = $('#filterToDate').val() || '';

        return {
            fromDate: fromDate,
            toDate: toDate,
            shipmentMode: shipmentMode,
            customerId: customerId,
            dateUse: (fromDate && toDate) ? 'Y' : 'N'
        };
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
                    drawCallback: function (settings) {
                        var api = this.api();
                        var rows = api.rows({ page: 'current' }).nodes();
                        var lastCustomer = null;

                        api.rows({ page: 'current' }).data().each(function (row, idx) {
                            var customer = row.customerName || (row.customerID ? 'Customer ID: ' + row.customerID : '');
                            if (customer && lastCustomer !== customer) {
                                $(rows).eq(idx).before(
                                    '<tr class="customer-group-row">' +
                                    '<td colspan="22"><i class="fas fa-building text-primary me-2"></i>Customer: ' + customer + '</td>' +
                                    '</tr>'
                                );
                                lastCustomer = customer;
                            }
                        });
                    }
                }
            });
        }
    }

    function reloadGrid() {
        if (gridTable) {
            gridTable.ajax.reload(null, true);
        }
        loadSummaryData();
    }

    function onFilterChanged() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 300);
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

        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            window.print();
        });
    }

    function loadSummaryData() {
        var filters = getFilterParams();

        $.ajax({
            url: '/GPAnalysisReport/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res) {
                    $('#statTotalBills').text(res.totalBills != null ? res.totalBills.toLocaleString() : '0');
                    $('#statNetBillValue').text(res.netBillValue != null ? window.gctlRenderers.formatCurrency(res.netBillValue) : '0.00');
                    $('#statTotalDue').text(res.totalCosting != null ? window.gctlRenderers.formatCurrency(res.totalCosting) : '0.00');
                    $('#statprofit').text(res.profit != null ? window.gctlRenderers.formatCurrency(res.profit) : '0.00');
                    $('#statLastServiceDate').text(res.gpPercent || '0.00%');
                }
            },
            error: function () {
                $('#statTotalBills').text('0');
                $('#statNetBillValue').text('0.00');
                $('#statTotalDue').text('0.00');
                $('#statprofit').text('0.00');
                $('#statLastServiceDate').text('0.00%');
            }
        });
    }

})(jQuery);
