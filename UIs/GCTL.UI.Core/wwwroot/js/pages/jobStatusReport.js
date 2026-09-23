/**
 * Job Status Dashboard Page Script
 * Sea & Air DataTables with Custom Multi/Single Select Filters
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let seaTable = null;
    let airTable = null;

    $(document).ready(function () {
        initDefaultDates();
        initDataTables();
        bindEvents();
        loadJobStatusData();
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

    function initDataTables() {
        // Destroy existing instances if any
        if ($.fn.DataTable.isDataTable('#seaJobTable')) {
            $('#seaJobTable').DataTable().destroy();
        }
        if ($.fn.DataTable.isDataTable('#airJobTable')) {
            $('#airJobTable').DataTable().destroy();
        }

        // Initialize Sea Freight DataTable
        seaTable = $('#seaJobTable').DataTable({
            processing: true,
            responsive: true,
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[0, 'asc']],
            autoWidth: false,
            language: {
                emptyTable: "No sea freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search sea jobs..."
            },
            columns: [
                { data: 'sl', className: 'text-center', width: '45px' },
                { 
                    data: 'jobNo', 
                    className: 'job-no-cell',
                    render: function (data) {
                        return escapeHtml(data);
                    }
                },
                { data: 'jobDate' },
                { data: 'expenseType' },
                { data: 'portName' },
                { data: 'docReceivedDate' },
                { data: 'invoiceNo' },
                { data: 'invoiceValue' },
                { data: 'containerPkgs' },
                { data: 'billNo' },
                { data: 'vesselName' },
                { data: 'containerNo' },
                { data: 'etdDate' },
                { data: 'shipArrivedDate' },
                { data: 'dischargeDate' },
                { data: 'actualDeliveryDate' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                }
            ]
        });

        // Initialize Air Freight DataTable
        airTable = $('#airJobTable').DataTable({
            processing: true,
            responsive: true,
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[0, 'asc']],
            autoWidth: false,
            language: {
                emptyTable: "No air freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search air jobs..."
            },
            columns: [
                { data: 'sl', className: 'text-center', width: '45px' },
                { 
                    data: 'jobNo', 
                    className: 'job-no-cell',
                    render: function (data) {
                        return escapeHtml(data);
                    }
                },
                { data: 'jobDate' },
                { data: 'expenseType' },
                { data: 'portName' },
                { data: 'docReceivedDate' },
                { data: 'invoiceNo' },
                { data: 'invoiceValue' },
                { data: 'materialNamePkgsQty' },
                { data: 'lcNo' },
                { data: 'hawbNo' },
                { data: 'mawbNo' },
                { data: 'etdDate' },
                { data: 'flightActualDeliveryDate' },
                { data: 'actualDeliveryDate' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                }
            ]
        });

        // Auto recalculate column sizing on tab switch
        $('button[data-bs-toggle="pill"]').on('shown.bs.tab', function () {
            if ($.fn.DataTable) {
                $.fn.DataTable.tables({ visible: true, api: true }).columns.adjust();
            }
        });
    }

    function bindEvents() {
        // Search Button Click
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            loadJobStatusData();
        });

        // Search on Enter key in Date inputs
        $('#filterFromDate, #filterToDate').on('keypress', function (e) {
            if (e.which === 13) {
                $('#btnSearch').trigger('click');
            }
        });

        // Stat Card Click to Filter by Status
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            setDropdownValue('filterJobStatus', statusFilter);
            loadJobStatusData();
        });

        // Export Report Click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });
    }

    /**
     * Helper to safely extract value from custom-select-multi or custom-select-single
     */
    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        // Check customDropdown instance if available
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
     * Helper to set value of a dropdown
     */
    function setDropdownValue(id, value) {
        var $el = $('#' + id);
        if (!$el.length) return;

        $el.val(value);
        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.setSelected === 'function') {
            inst.setSelected(value);
        }
        $el.trigger('change');
    }

    function getFilterParams() {
        return {
            FromDate: $('#filterFromDate').val() || '',
            ToDate: $('#filterToDate').val() || '',
            CustomerId: getDropdownValue('filterCustomerId'),
            ShipmentMode: getDropdownValue('filterShipmentMode'),
            PortType: getDropdownValue('filterPortType'),
            JobStatus: getDropdownValue('filterJobStatus'),
            ReportOption: 'Job Report'
        };
    }

    function loadJobStatusData() {
        var filters = getFilterParams();

        // Highlight matching stat card
        highlightStatCard(filters.JobStatus);

        // Show table processing state
        if (seaTable) {
            seaTable.processing(true);
        }
        if (airTable) {
            airTable.processing(true);
        }

        $.ajax({
            url: '/JobStatusReport/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    // 1. Update Animated Counter for Stat Cards
                    animateCounter('#statTotalJobs', res.total || 0);
                    animateCounter('#statPendingJobs', res.pending || 0);
                    animateCounter('#statTransitJobs', res.transit || 0);
                    animateCounter('#statDeliveredJobs', res.delivered || 0);

                    // 2. Update Sea Freight DataTable
                    var seaData = res.seaData || [];
                    $('#seaCountBadge').text(seaData.length.toLocaleString());
                    if (seaTable) {
                        seaTable.clear().rows.add(seaData).draw();
                    }

                    // 3. Update Air Freight DataTable
                    var airData = res.airData || [];
                    $('#airCountBadge').text(airData.length.toLocaleString());
                    if (airTable) {
                        airTable.clear().rows.add(airData).draw();
                    }

                    // Auto-adjust column headers
                    setTimeout(function () {
                        if ($.fn.DataTable) {
                            $.fn.DataTable.tables({ visible: true, api: true }).columns.adjust();
                        }
                    }, 100);
                } else {
                    clearTables();
                    showError(res ? res.message : 'Failed to retrieve data.');
                }
            },
            error: function () {
                clearTables();
                showError('Error connecting to server. Please try again.');
            },
            complete: function () {
                if (seaTable) seaTable.processing(false);
                if (airTable) airTable.processing(false);
            }
        });
    }

    function clearTables() {
        if (seaTable) seaTable.clear().draw();
        if (airTable) airTable.clear().draw();
        $('#seaCountBadge').text('0');
        $('#airCountBadge').text('0');
    }

    function highlightStatCard(status) {
        $('.stat-card').removeClass('active-card');
        var s = (status || '').toLowerCase().trim();
        if (!s) {
            $('.stat-card[data-status-filter=""]').addClass('active-card');
        } else if (s.indexOf('pend') !== -1) {
            $('.stat-card[data-status-filter="Pending"]').addClass('active-card');
        } else if (s.indexOf('transit') !== -1) {
            $('.stat-card[data-status-filter="In Transit"]').addClass('active-card');
        } else if (s.indexOf('deliver') !== -1) {
            $('.stat-card[data-status-filter="Delivered"]').addClass('active-card');
        }
    }

    function animateCounter(selector, target) {
        var $el = $(selector);
        var current = parseInt($el.text().replace(/,/g, ''), 10) || 0;
        var duration = 500;
        var steps = 20;
        var stepTime = duration / steps;
        var diff = target - current;

        var step = 0;
        var timer = setInterval(function () {
            step++;
            var val = Math.round(current + (diff * (step / steps)));
            $el.text(val.toLocaleString());
            if (step >= steps) {
                clearInterval(timer);
                $el.text(target.toLocaleString());
            }
        }, stepTime);
    }

    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = $('#filterExportFormat').val() || 'PDF';

        var queryParams = $.param({
            FromDate: filters.FromDate,
            ToDate: filters.ToDate,
            CustomerId: filters.CustomerId,
            ShipmentMode: filters.ShipmentMode,
            PortType: filters.PortType,
            JobStatus: filters.JobStatus,
            ReportOption: filters.ReportOption,
            exportFormat: exportFormat
        });

        window.location.href = '/JobStatusReport/Export?' + queryParams;
    }

    function escapeHtml(str) {
        if (!str) return '';
        return $('<div>').text(str).html();
    }

    function showError(msg) {
        if (window.toastr) {
            toastr.error(msg);
        } else {
            console.error(msg);
        }
    }

})(jQuery);
