/**
 * Job Status Dashboard Page Script
 * Ultra-Fast Dedicated Architecture with Millisecond Response, Dual Grid Isolation & Instant Filtering
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let seaTable = null;
    let airTable = null;
    let reloadDebounceTimer = null;
    let currentAjaxRequest = null;

    // In-memory dataset cache for instant (< 2ms) filtering and switching
    const cachedData = {
        'Air': null,
        'Sea': null,
        filterKey: ''
    };

    $(document).ready(function () {
        initDefaultDates();
        initDataTables();
        ensureDefaultAir();
        bindEvents();
        updateTableVisibility('Air');
        loadJobStatusData(true);
    });

    /**
     * Ensure "Air Freight" is selected as default if nothing is selected
     */
    function ensureDefaultAir() {
        var current = getDropdownValue('filterShipmentMode');
        if (!current) {
            silentSetDropdownValue('filterShipmentMode', 'Air');
        }
    }

    /**
     * Determines whether the active mode is 'Air' or 'Sea'.
     * Defaults to 'Air' under all circumstances unless explicitly 'Sea'.
     */
    function getActiveMode() {
        var modeVal = (getDropdownValue('filterShipmentMode') || '').trim();
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
        if (!text && modeVal) {
            text = $sel.find('option[value="' + modeVal + '"]').text() || '';
        }

        var lowerVal = modeVal.toLowerCase();
        var lowerText = (text || '').toLowerCase();

        // 1. Explicit Sea checks
        if (lowerVal === 'sea' || lowerText.indexOf('sea') !== -1) {
            return 'Sea';
        }
        // Known Sea expense types (0004=Sea Import CTG, 0005=Sea Export CTG, 0006=Sea Export Pangaon, etc.)
        if (['0004', '0005', '0006', '0007', '0008', '0009', '0010', '0011', '0012', '0013', '0014', '0015', '0016', '0017'].indexOf(modeVal) !== -1) {
            return 'Sea';
        }

        // 2. Explicit Air checks
        if (lowerVal === 'air' || lowerText.indexOf('air') !== -1 || ['0001', '0002', '0003'].indexOf(modeVal) !== -1) {
            return 'Air';
        }

        // 3. Fallback: Default mode is ALWAYS 'Air'
        return 'Air';
    }

    /**
     * Shows the corresponding grid container and hides the other
     */
    function updateTableVisibility(targetMode) {
        var mode = targetMode || getActiveMode();
        if (mode === 'Sea') {
            $('#seaTableContainer').show();
            $('#airTableContainer').hide();
            $('#gridTitle').html('<i class="fas fa-ship text-info me-2"></i>Job List(Sea):');
            if (seaTable) {
                requestAnimationFrame(function () {
                    seaTable.columns.adjust();
                });
            }
        } else {
            $('#airTableContainer').show();
            $('#seaTableContainer').hide();
            $('#gridTitle').html('<i class="fas fa-plane text-primary me-2"></i>Job List(Air):');
            if (airTable) {
                requestAnimationFrame(function () {
                    airTable.columns.adjust();
                });
            }
        }
    }

    /**
     * Initialize default date values (First day of month to today)
     */
    function initDefaultDates() {
        var now = new Date();
        var firstDay = new Date(now.getFullYear(), now.getMonth(), 1);

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        var fromStr = formatDate(firstDay);
        var toStr = formatDate(now);

        var $from = $('#filterFromDate');
        var $to = $('#filterToDate');

        if (!$from.val()) {
            $from.val(fromStr);
        }
        if (!$to.val()) {
            $to.val(toStr);
        }

        // Sync Flatpickr instances if already initialized by layout
        var fpFrom = $from.data('customFpInstance') || ($from[0] ? $from[0]._flatpickr : null);
        if (fpFrom) {
            if (!fpFrom.selectedDates || !fpFrom.selectedDates.length) {
                fpFrom.setDate(firstDay, false);
            }
            if (fpFrom.altInput && !fpFrom.altInput.value) {
                fpFrom.altInput.value = fromStr;
            }
        }

        var fpTo = $to.data('customFpInstance') || ($to[0] ? $to[0]._flatpickr : null);
        if (fpTo) {
            if (!fpTo.selectedDates || !fpTo.selectedDates.length) {
                fpTo.setDate(now, false);
            }
            if (fpTo.altInput && !fpTo.altInput.value) {
                fpTo.altInput.value = toStr;
            }
        }
    }

    /**
     * Safely get the value from datepicker input (checks flatpickr instance, altInput, or raw value)
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

        return ($el.val() || '').trim();
    }

    function formatAmount(val) {
        if (val === null || val === undefined || val === '') return '';
        var num = parseFloat(String(val).replace(/,/g, ''));
        if (isNaN(num)) return escapeHtml(val);
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatInvoiceValue(val, symbol, name) {
        if (val === null || val === undefined || val === '') return '';
        var sym = (symbol || '').trim();
        if (!sym) {
            var cName = (name || '').toUpperCase();
            if (cName.indexOf('BDT') !== -1 || cName.indexOf('TK') !== -1 || cName.indexOf('TAKA') !== -1) {
                sym = '৳';
            } else {
                sym = '$';
            }
        }
        var numStr = formatAmount(val);
        if (!numStr) return '';
        return '<span class="currency-sym">' + escapeHtml(sym) + '</span> ' + numStr;
    }

    /**
     * Initialize both DataTables with full responsive options and column definitions
     */
    function initDataTables() {
        if ($.fn.DataTable.isDataTable('#seaJobTable')) {
            $('#seaJobTable').DataTable().destroy();
        }
        if ($.fn.DataTable.isDataTable('#airJobTable')) {
            $('#airJobTable').DataTable().destroy();
        }

        // Initialize Sea Freight DataTable (All 26 columns)
        seaTable = $('#seaJobTable').DataTable({
            processing: true,
            deferRender: true,
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            columnDefs: [{ targets: '_all', defaultContent: '', className: 'text-center align-middle' }],
            language: {
                emptyTable: "No sea freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search sea jobs...",
                processing: '<div class="spinner-border text-primary spinner-border-sm me-2" role="status"></div> Loading sea jobs...'
            },
            columns: [
                { 
                    data: 'slNo', 
                    className: 'text-center align-middle', 
                    width: '40px',
                    render: function (data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                { data: 'jobNo', className: 'job-no-cell fw-bold text-center align-middle' },
                { data: 'jobDate', className: 'text-center align-middle' },
                { data: 'shipmentMode', className: 'text-center align-middle' },
                { data: 'port', className: 'text-center align-middle' },
                { data: 'docReceivedDate', className: 'text-center align-middle' },
                { data: 'invoiceNo', className: 'text-center align-middle' },
                { 
                    data: 'invoiceValue', 
                    className: 'text-end align-middle amount-cell',
                    render: function (data, type, row) {
                        return formatInvoiceValue(data, row.currencySymbol, row.currencyName);
                    }
                },
                { data: 'containerPkgs', className: 'text-start align-middle cell-wrap' },
                { data: 'shipper', className: 'text-center align-middle' },
                { data: 'blNo', className: 'text-center align-middle' },
                { data: 'etdDate', className: 'text-center align-middle' },
                { data: 'finalIgmRecDt', className: 'text-center align-middle' },
                { data: 'shipArrivedDate', className: 'text-center align-middle' },
                { data: 'dischargeDate', className: 'text-center align-middle' },
                { data: 'docsBankSignDate', className: 'text-center align-middle' },
                { data: 'beNo', className: 'text-center align-middle' },
                { data: 'beDate', className: 'text-center align-middle' },
                { data: 'estDeliveryDate', className: 'text-center align-middle' },
                { data: 'actualDeliveryDate', className: 'text-center align-middle' },
                { 
                    data: 'shipmentStatus',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks', className: 'text-center align-middle' },
                { data: 'term', className: 'text-center align-middle' },
                { data: 'vesselName', className: 'text-center align-middle' },
                { data: 'containerNo', className: 'text-center align-middle' },
                { data: 'freightForwarder', className: 'text-center align-middle' }
            ]
        });

        // Initialize Air Freight DataTable (All 21 columns)
        airTable = $('#airJobTable').DataTable({
            processing: true,
            deferRender: true,
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
            order: [[1, 'asc']],
            autoWidth: false,
            columnDefs: [{ targets: '_all', defaultContent: '', className: 'text-center align-middle' }],
            language: {
                emptyTable: "No air freight records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search air jobs...",
                processing: '<div class="spinner-border text-primary spinner-border-sm me-2" role="status"></div> Loading air jobs...'
            },
            columns: [
                { 
                    data: 'slNo', 
                    className: 'text-center align-middle', 
                    width: '40px',
                    render: function (data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                { data: 'jobNo', className: 'job-no-cell fw-bold text-center align-middle' },
                { data: 'jobDate', className: 'text-center align-middle' },
                { data: 'shipmentMode', className: 'text-center align-middle' },
                { data: 'port', className: 'text-center align-middle' },
                { data: 'docReceivedDate', className: 'text-center align-middle' },
                { data: 'invoiceNo', className: 'text-center align-middle' },
                { 
                    data: 'invoiceValue', 
                    className: 'text-end align-middle amount-cell',
                    render: function (data, type, row) {
                        return formatInvoiceValue(data, row.currencySymbol, row.currencyName);
                    }
                },
                { data: 'materialNamePkgsQty', className: 'text-start align-middle cell-wrap' },
                { data: 'expressNameCountry', className: 'text-center align-middle' },
                { data: 'lcNo', className: 'text-center align-middle' },
                { data: 'hawbNo', className: 'text-center align-middle' },
                { data: 'mawbNo', className: 'text-center align-middle' },
                { data: 'etdDate', className: 'text-center align-middle' },
                { data: 'flightActualDeliveryDate', className: 'text-center align-middle' },
                { data: 'arrivalNoticeRcvdExpressDate', className: 'text-center align-middle' },
                { data: 'beNo', className: 'text-center align-middle' },
                { data: 'beDate', className: 'text-center align-middle' },
                { data: 'actualDeliveryDate', className: 'text-center align-middle' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var cls = row.statusClass || 'pending';
                        return '<span class="status-badge ' + cls + '">' + escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks', className: 'text-center align-middle' }
            ]
        });
    }

    /**
     * Debounced reload helper to eliminate conflicting rapid triggers
     */
    function requestDataReload(debounceMs) {
        if (reloadDebounceTimer) {
            clearTimeout(reloadDebounceTimer);
        }
        reloadDebounceTimer = setTimeout(function () {
            cachedData.filterKey = '';
            cachedData['Air'] = null;
            cachedData['Sea'] = null;
            loadJobStatusData(true);
        }, debounceMs !== undefined ? debounceMs : 40);
    }

    /**
     * Bind all user interaction events cleanly
     */
    function bindEvents() {
        // 1. Date inputs (typing, picker selection, alt-input change)
        $(document).on('change', '#filterFromDate, #filterToDate, .filter-section .flatpickr-alt-input', function () {
            requestDataReload(150);
        });
        $(document).on('input', '#filterFromDate, #filterToDate', function () {
            requestDataReload(350);
        });

        // 2. Standard select change events
        $('#filterCustomerId, #filterPortType').on('change', function () {
            requestDataReload(30);
        });

        // 3. Shipment Mode change: update visibility immediately, then reload
        $('#filterShipmentMode').on('change', function () {
            var mode = getActiveMode();
            updateTableVisibility(mode);
            requestDataReload(30);
        });

        // 4. Job Status dropdown change: Instant in-memory filter (0 ms)
        $('#filterJobStatus').on('change', function () {
            var status = getDropdownValue('filterJobStatus');
            highlightStatCard(status);
            applyStatusFilterInstant(status);
        });

        // 5. Delegated custom dropdown option clicks & clear buttons
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-clear-icon, .custom-dd-item', function () {
            var $wrapper = $(this).closest('.custom-dd-wrapper');
            var selectId = $wrapper.prev('select').attr('id');

            setTimeout(function () {
                if (selectId === 'filterJobStatus') {
                    var status = getDropdownValue('filterJobStatus');
                    highlightStatCard(status);
                    applyStatusFilterInstant(status);
                } else if (selectId === 'filterShipmentMode') {
                    var mode = getActiveMode();
                    updateTableVisibility(mode);
                    requestDataReload(30);
                } else {
                    requestDataReload(30);
                }
            }, 30);
        });

        // 6. Stat Cards Click: Instant in-memory filter
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            silentSetDropdownValue('filterJobStatus', statusFilter);
            applyStatusFilterInstant(statusFilter);
        });

        // 7. Export Button
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });
    }

    /**
     * Generates a unique key for the current base filter values
     */
    function getBaseFilterKey() {
        return [
            getDateValue('filterFromDate'),
            getDateValue('filterToDate'),
            getDropdownValue('filterCustomerId'),
            getDropdownValue('filterPortType'),
            getDropdownValue('filterShipmentMode')
        ].join('|');
    }

    /**
     * Extracts full filter payload for API requests
     */
    function getFilterParams() {
        var activeMode = getActiveMode();
        return {
            FromDate: getDateValue('filterFromDate'),
            ToDate: getDateValue('filterToDate'),
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
     * Filters cached rows without any server round-trip
     */
    function applyStatusFilterInstant(statusFilter) {
        var mode = getActiveMode();
        var rawList = cachedData[mode];

        if (!rawList) {
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

        renderToActiveTable(mode, filtered);
    }

    /**
     * Renders rows into the active DataTable and adjusts column widths
     */
    function renderToActiveTable(mode, dataList) {
        updateTableVisibility(mode);

        var table = (mode === 'Air') ? airTable : seaTable;
        if (!table) return;

        var list = dataList || [];
        var prepared = [];
        for (var i = 0; i < list.length; i++) {
            prepared.push(Object.assign({}, list[i], { slNo: i + 1 }));
        }

        table.clear();
        table.rows.add(prepared);
        table.draw(false);

        initTooltips();

        requestAnimationFrame(function () {
            table.columns.adjust();
        });
    }

    function initTooltips() {
        if (typeof $.fn.tooltip === 'function') {
            $('[data-bs-toggle="tooltip"]').tooltip({
                boundary: 'window',
                container: 'body'
            });
        }
    }

    /**
     * Updates top stat summary cards
     */
    function updateSummaryCounters(total, pending, transit, delivered) {
        $('#statTotalJobs').text((total || 0).toLocaleString());
        $('#statPendingJobs').text((pending || 0).toLocaleString());
        $('#statTransitJobs').text((transit || 0).toLocaleString());
        $('#statDeliveredJobs').text((delivered || 0).toLocaleString());
    }

    /**
     * Loads job status data from backend with instant caching and mode protection
     */
    function loadJobStatusData(forced) {
        var filters = getFilterParams();
        var currentKey = getBaseFilterKey();
        var activeMode = getActiveMode();

        highlightStatCard(filters.JobStatus);

        // If base filters didn't change and we have data cached, filter in-memory (< 1ms)
        if (!forced && cachedData.filterKey === currentKey && cachedData[activeMode]) {
            applyStatusFilterInstant(filters.JobStatus);
            return;
        }

        // Abort previous in-flight request if user rapidly changed dropdowns
        if (currentAjaxRequest && currentAjaxRequest.readyState !== 4) {
            currentAjaxRequest.abort();
        }

        // Show table processing state
        var table = activeMode === 'Air' ? airTable : seaTable;
        if (table) {
            // keep previous rows visible until new rows arrive
        }

        currentAjaxRequest = $.ajax({
            url: '/JobStatusReport/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    cachedData.filterKey = currentKey;

                    // Server returns mode = 'Air' or 'Sea'
                    var resolvedMode = res.mode || activeMode;

                    var fullAir = res.rawAirData || res.airData || [];
                    var fullSea = res.rawSeaData || res.seaData || [];

                    cachedData['Air'] = fullAir;
                    cachedData['Sea'] = fullSea;

                    // 1. Update summary counters
                    updateSummaryCounters(res.total, res.pending, res.transit, res.delivered);

                    // 2. Render appropriate DataTable
                    var activeData = resolvedMode === 'Air' ? (res.airData || []) : (res.seaData || []);
                    renderToActiveTable(resolvedMode, activeData);

                    // 3. Highlight selected stat card
                    highlightStatCard(filters.JobStatus);
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
     * Safely extract value from custom-dropdown or standard select
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
     * Programmatically set value of a dropdown
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
     * Silently update dropdown UI without firing change event
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

    /**
     * Export report handler (PDF or Excel)
     */
    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = (getDropdownValue('filterExportFormat') || $('#filterExportFormat').val() || '').trim();
        if (!exportFormat) exportFormat = 'PDF';

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
