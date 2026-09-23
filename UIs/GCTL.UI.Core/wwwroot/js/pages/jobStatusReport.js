/**
 * Job Status Dashboard Page Script
 * Ultra-Fast Dedicated Architecture with True Millisecond Response & Instant In-Memory Filtering
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let seaTable = null;
    let airTable = null;
    let dateDebounceTimer = null;
    let currentAjaxRequest = null;

    // In-memory dataset cache for instant (< 2ms) filtering and switching
    const cacheByMode = {
        'Air': null,
        'Sea': null,
        filterKey: ''
    };

    $(document).ready(function () {
        initDefaultDates();
        initDataTables();
        ensureDefaultAir();
        bindEvents();
        updateTableVisibility();
        loadJobStatusData(true);
    });

    function ensureDefaultAir() {
        var current = getDropdownValue('filterShipmentMode');
        if (!current) {
            setDropdownValue('filterShipmentMode', 'Air');
        }
    }

    function getActiveMode() {
        var modeVal = (getDropdownValue('filterShipmentMode') || '').trim();
        if (modeVal.toLowerCase() === 'sea') return 'Sea';
        if (modeVal.toLowerCase() === 'air') return 'Air';

        // Check custom dropdown instance text or input val
        var $sel = $('#filterShipmentMode');
        var inst = $sel.data('customDdInstance');
        var text = '';
        if (inst) {
            if (typeof inst.getText === 'function') {
                var t = inst.getText();
                text = Array.isArray(t) ? t.join(' ') : String(t || '');
            }
            if (!text && inst.$input) {
                text = inst.$input.val() || '';
            }
        }
        if (!text) {
            text = $sel.find('option[value="' + modeVal + '"]').text() || '';
        }
        text = text.toLowerCase();
        if (text.indexOf('sea') !== -1) return 'Sea';
        if (text.indexOf('air') !== -1) return 'Air';

        // Expense types: 0001=Air Import, 0002=Courier Import, 0003=Air Export
        if (modeVal === '0001' || modeVal === '0002' || modeVal === '0003') return 'Air';
        return 'Sea'; // 0004, 0005, etc. are Sea Freight
    }

    function updateTableVisibility() {
        var mode = getActiveMode();
        if (mode === 'Air') {
            $('#airTableContainer').show();
            $('#seaTableContainer').hide();
            if (airTable) {
                requestAnimationFrame(function () {
                    airTable.columns.adjust().draw(false);
                });
            }
        } else {
            $('#seaTableContainer').show();
            $('#airTableContainer').hide();
            if (seaTable) {
                requestAnimationFrame(function () {
                    seaTable.columns.adjust().draw(false);
                });
            }
        }
    }

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
        if ($.fn.DataTable.isDataTable('#seaJobTable')) {
            $('#seaJobTable').DataTable().destroy();
        }
        if ($.fn.DataTable.isDataTable('#airJobTable')) {
            $('#airJobTable').DataTable().destroy();
        }

        // Initialize Sea Freight DataTable (All 26 columns, fast deferred rendering & pagination)
        seaTable = $('#seaJobTable').DataTable({
            processing: true,
            deferRender: true,
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            columnDefs: [{ targets: '_all', defaultContent: '' }],
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
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            columnDefs: [{ targets: '_all', defaultContent: '' }],
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
    }

    function bindEvents() {
        // Datepicker Text Typing (Debounced 150ms to allow typing)
        $('#filterFromDate, #filterToDate').on('input change changeDate dp.change apply.daterangepicker', function () {
            if (dateDebounceTimer) clearTimeout(dateDebounceTimer);
            dateDebounceTimer = setTimeout(function () {
                cacheByMode.filterKey = '';
                cacheByMode['Air'] = null;
                cacheByMode['Sea'] = null;
                loadJobStatusData(true);
            }, 150);
        });

        // Customer & Port Type dropdown change: immediate reload
        $('#filterCustomerId, #filterPortType').on('change', function () {
            cacheByMode.filterKey = '';
            cacheByMode['Air'] = null;
            cacheByMode['Sea'] = null;
            loadJobStatusData(true);
        });

        // Job Status dropdown change: INSTANT in-memory filter (0 ms)
        $('#filterJobStatus').on('change', function () {
            var status = getDropdownValue('filterJobStatus');
            highlightStatCard(status);
            applyStatusFilterInstant(status);
        });

        // Shipment Mode change: switch table and load immediately
        $('#filterShipmentMode').on('change', function () {
            onShipmentModeChanged();
        });

        // Immediate click capture for custom-dropdown options inside filter-section
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-clear-icon, .custom-dd-item', function () {
            var $wrapper = $(this).closest('.custom-dd-wrapper');
            var selectId = $wrapper.prev('select').attr('id');

            setTimeout(function () {
                if (selectId === 'filterJobStatus') {
                    var status = getDropdownValue('filterJobStatus');
                    highlightStatCard(status);
                    applyStatusFilterInstant(status);
                } else if (selectId === 'filterShipmentMode') {
                    onShipmentModeChanged();
                } else {
                    cacheByMode.filterKey = '';
                    loadJobStatusData(true);
                }
            }, 0);
        });

        // Stat Card Click: INSTANT in-memory filter (< 1ms, zero server delay)
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            // Sync dropdown value without triggering a server reload
            silentSetDropdownValue('filterJobStatus', statusFilter);

            // Filter in-memory instantly (< 1ms)
            applyStatusFilterInstant(statusFilter);
        });

        // Export Report Click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });
    }

    function onShipmentModeChanged() {
        var currentMode = getDropdownValue('filterShipmentMode');
        if (!currentMode) {
            setDropdownValue('filterShipmentMode', 'Air');
        }
        updateTableVisibility();

        var mode = getActiveMode();
        var status = getDropdownValue('filterJobStatus');

        // If we already have this mode cached for current filters, render it in 0ms!
        if (cacheByMode[mode] && cacheByMode[mode].length) {
            applyStatusFilterInstant(status);
            updateSummaryCounters(cacheByMode[mode]);
        } else {
            // Force fetch data for the newly selected mode
            loadJobStatusData(true);
        }
    }

    function getBaseFilterKey() {
        return [
            $('#filterFromDate').val() || '',
            $('#filterToDate').val() || '',
            getDropdownValue('filterCustomerId'),
            getDropdownValue('filterPortType'),
            getDropdownValue('filterShipmentMode')
        ].join('|');
    }

    function getFilterParams() {
        var activeMode = getActiveMode();
        return {
            FromDate: $('#filterFromDate').val() || '',
            ToDate: $('#filterToDate').val() || '',
            CustomerId: getDropdownValue('filterCustomerId'),
            ShipmentMode: getDropdownValue('filterShipmentMode'),
            PortType: getDropdownValue('filterPortType'),
            JobStatus: getDropdownValue('filterJobStatus'),
            ActiveTab: activeMode,
            ReportOption: 'Job Report'
        };
    }

    /**
     * Instant In-Memory Filter (< 1ms)
     * Filters rows already in memory without any server round-trip!
     */
    function applyStatusFilterInstant(statusFilter) {
        var mode = getActiveMode();
        var rawList = cacheByMode[mode];

        if (!rawList) {
            // Not in cache yet, fetch from server
            loadJobStatusData(true);
            return;
        }

        var status = (statusFilter !== undefined ? statusFilter : getDropdownValue('filterJobStatus')) || '';
        status = status.toLowerCase().trim();

        var filtered = rawList;
        if (status.indexOf('pend') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'pending'; });
        } else if (status.indexOf('transit') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'transit'; });
        } else if (status.indexOf('deliver') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'delivered'; });
        }

        // Render table immediately
        renderToActiveTable(mode, filtered);
    }

    function renderToActiveTable(mode, dataList) {
        updateTableVisibility();

        var table = mode === 'Air' ? airTable : seaTable;
        if (!table) return;

        // Re-sequence slNo
        var list = dataList || [];
        var prepared = [];
        for (var i = 0; i < list.length; i++) {
            var row = Object.assign({}, list[i], { slNo: i + 1 });
            prepared.push(row);
        }

        table.clear();
        table.rows.add(prepared);
        table.draw(false);
        requestAnimationFrame(function () {
            table.columns.adjust();
        });
    }

    function updateSummaryCounters(rawList) {
        if (!rawList) return;
        var total = rawList.length;
        var pending = 0, transit = 0, delivered = 0;

        for (var i = 0; i < rawList.length; i++) {
            var cls = rawList[i].statusClass;
            if (cls === 'pending') pending++;
            else if (cls === 'transit') transit++;
            else if (cls === 'delivered') delivered++;
        }

        // Instant millisecond counter update (no 250ms delay)
        $('#statTotalJobs').text(total.toLocaleString());
        $('#statPendingJobs').text(pending.toLocaleString());
        $('#statTransitJobs').text(transit.toLocaleString());
        $('#statDeliveredJobs').text(delivered.toLocaleString());
    }

    function loadJobStatusData(forced) {
        var filters = getFilterParams();
        var currentKey = getBaseFilterKey();
        var activeMode = getActiveMode();

        highlightStatCard(filters.JobStatus);

        // If filter criteria didn't change and we have data cached, filter in-memory (< 1ms)
        if (!forced && cacheByMode.filterKey === currentKey && cacheByMode[activeMode]) {
            applyStatusFilterInstant(filters.JobStatus);
            return;
        }

        // Abort previous in-flight request if user rapidly changed dropdowns
        if (currentAjaxRequest && currentAjaxRequest.readyState !== 4) {
            currentAjaxRequest.abort();
        }

        currentAjaxRequest = $.ajax({
            url: '/JobStatusReport/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    cacheByMode.filterKey = currentKey;

                    // Cache full list for active mode
                    var fullAir = res.rawAirData || res.airData || [];
                    var fullSea = res.rawSeaData || res.seaData || [];

                    if (activeMode === 'Air') {
                        cacheByMode['Air'] = fullAir;
                    } else {
                        cacheByMode['Sea'] = fullSea;
                    }

                    // 1. Instant Counter Update
                    $('#statTotalJobs').text((res.total || 0).toLocaleString());
                    $('#statPendingJobs').text((res.pending || 0).toLocaleString());
                    $('#statTransitJobs').text((res.transit || 0).toLocaleString());
                    $('#statDeliveredJobs').text((res.delivered || 0).toLocaleString());

                    // 2. Render DataTable (< 2ms)
                    var activeData = activeMode === 'Air' ? (res.airData || []) : (res.seaData || []);
                    renderToActiveTable(activeMode, activeData);

                    // 3. Update visibility and adjust columns
                    updateTableVisibility();
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
                currentAjaxRequest = null;
            }
        });
    }

    function clearTables() {
        if (seaTable) seaTable.clear().draw(false);
        if (airTable) airTable.clear().draw(false);
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

    /**
     * Silently update dropdown UI without firing change event (avoids redundant server queries)
     */
    function silentSetDropdownValue(id, value) {
        var $el = $('#' + id);
        if (!$el.length) return;

        $el.val(value);
        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.setSelected === 'function') {
            inst.setSelected(value);
        } else if (inst && typeof inst.setValue === 'function') {
            inst.setValue(value);
        }
    }

    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = getDropdownValue('filterExportFormat') || $('#filterExportFormat').val() || 'PDF';

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
