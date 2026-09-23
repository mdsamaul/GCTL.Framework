/**
 * Job Status Dashboard Page Script
 * Ultra-Fast Dedicated Architecture with Millisecond Response & Immediate Filter Changes
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let seaTable = null;
    let airTable = null;
    let debounceTimer = null;
    let currentAjaxRequest = null;

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

        // Initialize Sea Freight DataTable (All 26 columns, fast deferred rendering & pagination)
        seaTable = $('#seaJobTable').DataTable({
            processing: true,
            deferRender: true, // Only render visible rows for instant rendering (< 5ms)
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            language: {
                emptyTable: "No sea freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search sea jobs...",
                processing: '<div class="spinner-border text-primary spinner-border-sm me-2" role="status"></div> Loading sea jobs...'
            },
            columns: [
                { 
                    data: 'slNo', 
                    className: 'text-center', 
                    width: '40px',
                    render: function (data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                { data: 'jobNo', className: 'job-no-cell fw-bold text-center' },
                { data: 'jobDate', className: 'text-center' },
                { data: 'shipmentMode' },
                { data: 'port' },
                { data: 'docReceivedDate', className: 'text-center' },
                { data: 'invoiceNo' },
                { data: 'invoiceValue', className: 'text-end' },
                { data: 'containerPkgs', className: 'table-warning text-dark' },
                { data: 'shipper' },
                { data: 'blNo', className: 'text-center' },
                { data: 'etdDate', className: 'text-center' },
                { data: 'finalIgmRecDt', className: 'text-center' },
                { data: 'shipArrivedDate', className: 'text-center' },
                { data: 'dischargeDate', className: 'text-center' },
                { data: 'docsBankSignDate', className: 'text-center' },
                { data: 'beNo', className: 'text-center' },
                { data: 'beDate', className: 'text-center' },
                { data: 'estDeliveryDate', className: 'text-center' },
                { data: 'actualDeliveryDate', className: 'text-center' },
                { 
                    data: 'shipmentStatus',
                    className: 'text-center',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks' },
                { data: 'term', className: 'text-center' },
                { data: 'vesselName' },
                { data: 'containerNo' },
                { data: 'freightForwarder' }
            ]
        });

        // Initialize Air Freight DataTable (All 21 columns, fast deferred rendering & pagination)
        airTable = $('#airJobTable').DataTable({
            processing: true,
            deferRender: true,
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            language: {
                emptyTable: "No air freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search air jobs...",
                processing: '<div class="spinner-border text-primary spinner-border-sm me-2" role="status"></div> Loading air jobs...'
            },
            columns: [
                { 
                    data: 'slNo', 
                    className: 'text-center', 
                    width: '40px',
                    render: function (data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                { data: 'jobNo', className: 'job-no-cell fw-bold text-center' },
                { data: 'jobDate', className: 'text-center' },
                { data: 'shipmentMode' },
                { data: 'port' },
                { data: 'docReceivedDate', className: 'text-center' },
                { data: 'invoiceNo' },
                { data: 'invoiceValue', className: 'text-end' },
                { data: 'materialNamePkgsQty', className: 'table-warning text-dark' },
                { data: 'expressNameCountry' },
                { data: 'lcNo', className: 'text-center' },
                { data: 'hawbNo', className: 'text-center' },
                { data: 'mawbNo', className: 'text-center' },
                { data: 'etdDate', className: 'text-center' },
                { data: 'flightActualDeliveryDate', className: 'table-warning text-dark text-center' },
                { data: 'arrivalNoticeRcvdExpressDate', className: 'table-warning text-dark text-center' },
                { data: 'beNo', className: 'text-center' },
                { data: 'beDate', className: 'text-center' },
                { data: 'actualDeliveryDate', className: 'text-center' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks' }
            ]
        });

        // Recalculate columns on tab switch
        $('button[data-bs-toggle="pill"]').on('shown.bs.tab', function () {
            if ($.fn.DataTable) {
                $.fn.DataTable.tables({ visible: true, api: true }).columns.adjust();
            }
        });
    }

    function bindEvents() {
        // Instant response on Datepicker Change
        $('#filterFromDate, #filterToDate').on('input change changeDate dp.change apply.daterangepicker', function () {
            triggerInstantFilterChange();
        });

        // Instant response on native/select2 changes
        $('#filterCustomerId, #filterJobStatus, #filterShipmentMode, #filterPortType').on('change', function () {
            triggerInstantFilterChange();
        });

        // Immediate click capture for custom-dropdown plugin inside filter-section
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-clear-icon, .custom-dd-item', function () {
            // Slight tick to let custom dropdown update internal state
            setTimeout(function () {
                triggerInstantFilterChange();
            }, 30);
        });

        // Stat Card Click to Filter by Status
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            setDropdownValue('filterJobStatus', statusFilter);
            triggerInstantFilterChange();
        });

        // Export Report Click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });
    }

    function triggerInstantFilterChange() {
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }
        debounceTimer = setTimeout(function () {
            loadJobStatusData();
        }, 120); // Fast 120ms debounce
    }

    /**
     * Helper to safely extract value from custom-select-multi or custom-select-single
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
     * Helper to set value of a dropdown
     */
    function setDropdownValue(id, value) {
        var $el = $('#' + id);
        if (!$el.length) return;

        $el.val(value);
        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.setSelected === 'function') {
            inst.setSelected(value);
        } else if (inst && typeof inst.setValue === 'function') {
            inst.setValue(value);
        }
        $el.trigger('change');
    }

    function getFilterParams() {
        var activeTabMode = $('#jobModeTabs button.active').data('tab-mode') || 'Sea';
        return {
            FromDate: $('#filterFromDate').val() || '',
            ToDate: $('#filterToDate').val() || '',
            CustomerId: getDropdownValue('filterCustomerId'),
            ShipmentMode: getDropdownValue('filterShipmentMode'),
            PortType: getDropdownValue('filterPortType'),
            JobStatus: getDropdownValue('filterJobStatus'),
            ActiveTab: activeTabMode,
            ReportOption: 'Job Report'
        };
    }

    function loadJobStatusData() {
        var filters = getFilterParams();

        // Highlight matching stat card
        highlightStatCard(filters.JobStatus);

        // Abort previous in-flight request if user rapidly changed dropdowns
        if (currentAjaxRequest && currentAjaxRequest.readyState !== 4) {
            currentAjaxRequest.abort();
        }

        // Show table processing indicators
        if (seaTable) seaTable.processing(true);
        if (airTable) airTable.processing(true);

        currentAjaxRequest = $.ajax({
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
                        seaTable.clear().rows.add(seaData).draw(false);
                    }

                    // 3. Update Air Freight DataTable
                    var airData = res.airData || [];
                    $('#airCountBadge').text(airData.length.toLocaleString());
                    if (airTable) {
                        airTable.clear().rows.add(airData).draw(false);
                    }

                    // Adjust columns without blocking
                    requestAnimationFrame(function () {
                        if ($.fn.DataTable) {
                            $.fn.DataTable.tables({ visible: true, api: true }).columns.adjust();
                        }
                    });
                } else {
                    clearTables();
                    showError(res ? res.message : 'Failed to retrieve data.');
                }
            },
            error: function (xhr, status) {
                if (status !== 'abort') {
                    clearTables();
                    showError('Error connecting to server. Please try again.');
                }
            },
            complete: function () {
                if (seaTable) seaTable.processing(false);
                if (airTable) airTable.processing(false);
                currentAjaxRequest = null;
            }
        });
    }

    function clearTables() {
        if (seaTable) seaTable.clear().draw(false);
        if (airTable) airTable.clear().draw(false);
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
        var diff = target - current;
        if (diff === 0) return;

        var duration = 250; // Snappy 250ms animation
        var steps = 10;
        var stepTime = duration / steps;
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
            ActiveTab: filters.ActiveTab,
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
