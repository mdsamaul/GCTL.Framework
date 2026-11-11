/**
 * Job Status Tab Module - Integrated with Full JobStatusReport Grid
 * Formosa Logistics Limited
 */
'use strict';

window.DashboardJobStatus = {
    seaTable: null,
    airTable: null,
    activeMode: 'Air',
    userModeOverride: null,
    currentAjaxRequest: null,
    eventsBound: false,

    cachedData: {
        'Air': null,
        'Sea': null,
        filterKey: ''
    },

    initDataTables() {
        if (typeof $.fn.DataTable === 'undefined') {
            console.error('DataTables is not loaded.');
            return;
        }

        const self = this;

        if ($.fn.DataTable.isDataTable('#seaJobTable')) {
            $('#seaJobTable').DataTable().destroy();
        }
        if ($.fn.DataTable.isDataTable('#airJobTable')) {
            $('#airJobTable').DataTable().destroy();
        }

        // 1. Initialize Sea Freight DataTable (All 26+ columns)
        this.seaTable = $('#seaJobTable').DataTable({
            processing: true,
            deferRender: true,
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[5, 10, 20, 50, 100, -1], [5, 10, 20, 50, 100, "All"]],
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
                        return self.formatInvoiceValue(data, row.currencySymbol, row.currencyName);
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
                {
                    data: 'requisitionNo',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        if (!data) return '-';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-req-preview-btn" data-job-no="' + self.escapeHtml(row.jobNo || '') + '" data-ofr-no="' + self.escapeHtml(data) + '" title="View Operational Fund Requisition Report">' + self.escapeHtml(data) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'requisitionDate',
                    className: 'text-center align-middle',
                    render: function (data) { return data ? self.escapeHtml(data) : '-'; }
                },
                {
                    data: 'ofrApprovalReport',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.oFRApprovalReport || (row.requisitionNo ? 'View Report' : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var cls = isApproved ? 'text-success' : (text.toLowerCase() === 'pending' ? 'text-warning' : '');
                        var icon = isApproved ? 'fa-check-circle' : 'fa-file-pdf';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-approval-btn ' + cls + '" data-job-no="' + self.escapeHtml(row.jobNo) + '" data-ofr-no="' + self.escapeHtml(row.requisitionNo || '') + '" title="View OFR Approval Report"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'ofrAdjustApprovalReport',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.oFRAdjustApprovalReport || (row.requisitionNo ? 'View Report' : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var cls = isApproved ? 'text-success' : (text.toLowerCase() === 'pending' ? 'text-warning' : '');
                        var icon = isApproved ? 'fa-check-circle' : 'fa-file-pdf';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-adjust-btn ' + cls + '" data-job-no="' + self.escapeHtml(row.jobNo) + '" data-ofr-no="' + self.escapeHtml(row.requisitionNo || '') + '" title="View OFR Adjust Approval Report"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'voucherApproval',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.voucherApproval;
                        var vNo = row.voucherNo || (text && (text.indexOf('_') > -1 || text.indexOf('V') === 1) ? text : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var isPending = text.toLowerCase() === 'pending';
                        if (!isApproved && !isPending) {
                            vNo = text;
                            text = 'Approved';
                            isApproved = true;
                        }
                        var cls = isApproved ? 'text-success' : 'text-warning';
                        var icon = isApproved ? 'fa-check-circle' : 'fa-clock';
                        var vTitle = vNo ? 'View Voucher (' + self.escapeHtml(vNo) + ')' : 'View Voucher';
                        return '<a href="javascript:void(0);" class="report-preview-link voucher-preview-btn ' + cls + '" data-voucher-no="' + self.escapeHtml(vNo) + '" title="' + vTitle + '"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'billNo',
                    className: 'text-center align-middle',
                    render: function (data) {
                        if (!data) return '-';
                        return '<a href="javascript:void(0);" class="report-preview-link bill-preview-btn" data-bill-no="' + self.escapeHtml(data) + '" title="View Job Bill Report">' + self.escapeHtml(data) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'billDate',
                    className: 'text-center align-middle',
                    render: function (data) { return data ? self.escapeHtml(data) : '-'; }
                },
                { data: 'estDeliveryDate', className: 'text-center align-middle' },
                { data: 'actualDeliveryDate', className: 'text-center align-middle' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var cls = row.statusClass || (data ? data.toLowerCase().replace(/\s+/g, '-') : 'pending');
                        return '<span class="status-badge ' + cls + '">' + self.escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks', className: 'text-center align-middle' },
                { data: 'term', className: 'text-center align-middle' },
                { data: 'vesselName', className: 'text-center align-middle' },
                { data: 'containerNo', className: 'text-center align-middle' },
                { data: 'freightForwarder', className: 'text-center align-middle' }
            ]
        });

        // 2. Initialize Air Freight DataTable (All 21 columns)
        this.airTable = $('#airJobTable').DataTable({
            processing: true,
            deferRender: true,
            dom: '<"d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2"lf><"table-responsive"rt><"d-flex justify-content-between align-items-center mt-2 flex-wrap gap-2"ip>',
            pageLength: 10,
            lengthMenu: [[5, 10, 20, 50, 100, -1], [5, 10, 20, 50, 100, "All"]],
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
                        return self.formatInvoiceValue(data, row.currencySymbol, row.currencyName);
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
                {
                    data: 'requisitionNo',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        if (!data) return '-';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-req-preview-btn" data-job-no="' + self.escapeHtml(row.jobNo || '') + '" data-ofr-no="' + self.escapeHtml(data) + '" title="View Operational Fund Requisition Report">' + self.escapeHtml(data) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'requisitionDate',
                    className: 'text-center align-middle',
                    render: function (data) { return data ? self.escapeHtml(data) : '-'; }
                },
                {
                    data: 'ofrApprovalReport',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.oFRApprovalReport || (row.requisitionNo ? 'View Report' : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var cls = isApproved ? 'text-success' : (text.toLowerCase() === 'pending' ? 'text-warning' : '');
                        var icon = isApproved ? 'fa-check-circle' : 'fa-file-pdf';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-approval-btn ' + cls + '" data-job-no="' + self.escapeHtml(row.jobNo) + '" data-ofr-no="' + self.escapeHtml(row.requisitionNo || '') + '" title="View OFR Approval Report"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'ofrAdjustApprovalReport',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.oFRAdjustApprovalReport || (row.requisitionNo ? 'View Report' : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var cls = isApproved ? 'text-success' : (text.toLowerCase() === 'pending' ? 'text-warning' : '');
                        var icon = isApproved ? 'fa-check-circle' : 'fa-file-pdf';
                        return '<a href="javascript:void(0);" class="report-preview-link ofr-adjust-btn ' + cls + '" data-job-no="' + self.escapeHtml(row.jobNo) + '" data-ofr-no="' + self.escapeHtml(row.requisitionNo || '') + '" title="View OFR Adjust Approval Report"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'voucherApproval',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var text = data || row.voucherApproval;
                        var vNo = row.voucherNo || (text && (text.indexOf('_') > -1 || text.indexOf('V') === 1) ? text : '');
                        if (!text) return '-';
                        var isApproved = text.toLowerCase() === 'approved';
                        var isPending = text.toLowerCase() === 'pending';
                        if (!isApproved && !isPending) {
                            vNo = text;
                            text = 'Approved';
                            isApproved = true;
                        }
                        var cls = isApproved ? 'text-success' : 'text-warning';
                        var icon = isApproved ? 'fa-check-circle' : 'fa-clock';
                        var vTitle = vNo ? 'View Voucher (' + self.escapeHtml(vNo) + ')' : 'View Voucher';
                        return '<a href="javascript:void(0);" class="report-preview-link voucher-preview-btn ' + cls + '" data-voucher-no="' + self.escapeHtml(vNo) + '" title="' + vTitle + '"><i class="fas ' + icon + ' me-1"></i>' + self.escapeHtml(text) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'billNo',
                    className: 'text-center align-middle',
                    render: function (data) {
                        if (!data) return '-';
                        return '<a href="javascript:void(0);" class="report-preview-link bill-preview-btn" data-bill-no="' + self.escapeHtml(data) + '" title="View Job Bill Report">' + self.escapeHtml(data) + ' <i class="fas fa-eye ms-1"></i></a>';
                    }
                },
                {
                    data: 'billDate',
                    className: 'text-center align-middle',
                    render: function (data) { return data ? self.escapeHtml(data) : '-'; }
                },
                { data: 'actualDeliveryDate', className: 'text-center align-middle' },
                {
                    data: 'shipmentStatus',
                    className: 'text-center align-middle',
                    render: function (data, type, row) {
                        var cls = row.statusClass || (data ? data.toLowerCase().replace(/\s+/g, '-') : 'pending');
                        return '<span class="status-badge ' + cls + '">' + self.escapeHtml(data || 'Pending') + '</span>';
                    }
                },
                { data: 'remarks', className: 'text-center align-middle' }
            ]
        });
    },

    bindEvents() {
        if (this.eventsBound) return;
        this.eventsBound = true;
        const self = this;

        // Mode switch buttons
        $(document).on('click', '#j_btnModeAir', function () {
            self.userModeOverride = 'Air';
            self.activeMode = 'Air';
            self.updateTableVisibility('Air');
            const f = window.DashboardApp ? window.DashboardApp.getFilterState() : {};
            self.loadGridData(f, true);
        });

        $(document).on('click', '#j_btnModeSea', function () {
            self.userModeOverride = 'Sea';
            self.activeMode = 'Sea';
            self.updateTableVisibility('Sea');
            const f = window.DashboardApp ? window.DashboardApp.getFilterState() : {};
            self.loadGridData(f, true);
        });

        // KPI card click: instant in-memory filter
        $('#j_tot').css('cursor', 'pointer').on('click', function () {
            self.highlightKpiCard(this);
            self.applyStatusFilterInstant('');
        });
        $('#j_pen').css('cursor', 'pointer').on('click', function () {
            self.highlightKpiCard(this);
            self.applyStatusFilterInstant('pending');
        });
        $('#j_tr').css('cursor', 'pointer').on('click', function () {
            self.highlightKpiCard(this);
            self.applyStatusFilterInstant('in transit');
        });
        $('#j_del').css('cursor', 'pointer').on('click', function () {
            self.highlightKpiCard(this);
            self.applyStatusFilterInstant('delivered');
        });

        // Report Preview links
        $(document).on('click', '.ofr-req-preview-btn', function (e) {
            e.preventDefault();
            var ofrNo = $(this).data('ofr-no');
            var jobNo = $(this).data('job-no');
            if (!ofrNo && !jobNo) return;
            var url = '/OFRReport/PreviewPdf?jobNo=' + encodeURIComponent(jobNo || '') + '&selectedOfrNos=' + encodeURIComponent(ofrNo || '') + '&ofrReportType=' + encodeURIComponent('OFR Report');
            self.openReportPreviewModal('Operational Fund Requisition Report - ' + (ofrNo || jobNo), url);
        });

        $(document).on('click', '.ofr-approval-btn', function (e) {
            e.preventDefault();
            var jobNo = $(this).data('job-no');
            var ofrNo = $(this).data('ofr-no');
            if (!jobNo && !ofrNo) return;
            var url = '/OFRReport/PreviewPdf?jobNo=' + encodeURIComponent(jobNo || '') + '&selectedOfrNos=' + encodeURIComponent(ofrNo || '') + '&ofrReportType=' + encodeURIComponent('OFR Approval Report');
            self.openReportPreviewModal('Operational Fund Approval Report - ' + (ofrNo || jobNo), url);
        });

        $(document).on('click', '.ofr-adjust-btn', function (e) {
            e.preventDefault();
            var jobNo = $(this).data('job-no');
            var ofrNo = $(this).data('ofr-no');
            if (!jobNo && !ofrNo) return;
            var url = '/OFRReport/PreviewPdf?jobNo=' + encodeURIComponent(jobNo || '') + '&selectedOfrNos=' + encodeURIComponent(ofrNo || '') + '&ofrReportType=' + encodeURIComponent('OFR Final Report');
            self.openReportPreviewModal('OFR Adjust Approval Report - ' + (ofrNo || jobNo), url);
        });

        $(document).on('click', '.voucher-preview-btn', function (e) {
            e.preventDefault();
            var voucherNo = $(this).data('voucher-no');
            if (!voucherNo) return;
            var url = '/VoucherEntry/Print?id=' + encodeURIComponent(voucherNo) + '&autoprint=0';
            self.openReportPreviewModal('Voucher Entry - ' + voucherNo, url);
        });

        $(document).on('click', '.bill-preview-btn', function (e) {
            e.preventDefault();
            var billNo = $(this).data('bill-no');
            if (!billNo) return;
            var url = '/JobBillEntryReport/ExportPdf?selectedBillNos=' + encodeURIComponent(billNo) + '&billNos=' + encodeURIComponent(billNo);
            self.openReportPreviewModal('Job Bill Entry Report - ' + billNo, url);
        });

        // Modal reset on close
        $('#reportPreviewModal').on('hidden.bs.modal', function () {
            $('#reportPreviewIframe').attr('src', 'about:blank');
            $('#modalHeaderSpinner').hide();
        });
    },

    highlightKpiCard(el) {
        $('.kpi').removeClass('active-card');
        if (el) $(el).addClass('active-card');
    },

    detectMode(shipmentMode) {
        if (this.userModeOverride) return this.userModeOverride;
        var modeVal = (shipmentMode || '').trim().toLowerCase();
        if (modeVal === 'sea' || modeVal.indexOf('sea') !== -1) return 'Sea';
        if (['0004', '0005', '0006', '0007', '0008', '0009', '0010', '0011', '0012', '0013', '0014', '0015', '0016', '0017'].indexOf(modeVal) !== -1) {
            return 'Sea';
        }
        return 'Air';
    },

    updateTableVisibility(targetMode) {
        var mode = targetMode || this.activeMode;
        if (mode === 'Sea') {
            $('#seaTableContainer').show();
            $('#airTableContainer').hide();
            $('#j_gridTitle').html('<i class="fas fa-ship text-info"></i> <span>Job List(Sea):</span>');
            $('#j_btnModeSea').addClass('active');
            $('#j_btnModeAir').removeClass('active');
            if (this.seaTable) {
                requestAnimationFrame(() => this.seaTable.columns.adjust());
            }
        } else {
            $('#airTableContainer').show();
            $('#seaTableContainer').hide();
            $('#j_gridTitle').html('<i class="fas fa-plane text-primary"></i> <span>Job List(Air):</span>');
            $('#j_btnModeAir').addClass('active');
            $('#j_btnModeSea').removeClass('active');
            if (this.airTable) {
                requestAnimationFrame(() => this.airTable.columns.adjust());
            }
        }
    },

    async load(filter) {
        this.initDataTables();
        this.bindEvents();

        if (window.DashboardApp) window.DashboardApp.showLoader('p-jobs');
        try {
            // 1. Fetch Dashboard Job Status summary data (KPIs, minis, charts)
            const resp = await fetch('/DashboardJobStatus/GetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter)
            });
            const res = await resp.json();
            if (res.isSuccess) {
                this.render(res.data);
            } else {
                console.error(res.message);
            }

            // 2. Fetch full Job Status Report grid data
            await this.loadGridData(filter);
        } catch (err) {
            console.error('Error loading Job Status data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-jobs');
        }
    },

    async loadGridData(filter, forced) {
        const self = this;
        var mode = this.userModeOverride || this.detectMode(filter.shipmentMode);
        this.activeMode = mode;
        this.updateTableVisibility(mode);

        const filterKey = [
            filter.fromDate || '',
            filter.toDate || '',
            filter.branchCode || '',
            filter.customerID || '',
            filter.port || '',
            filter.shipmentMode || '',
            mode
        ].join('|');

        if (!forced && this.cachedData.filterKey === filterKey && this.cachedData[mode]) {
            this.applyStatusFilterInstant(filter.jobStatus);
            return;
        }

        const reportPayload = {
            FromDate: filter.fromDate || '',
            ToDate: filter.toDate || '',
            BranchCode: filter.branchCode || '',
            CustomerId: filter.customerID || '',
            ShipmentMode: filter.shipmentMode || '',
            PortType: filter.port || '',
            JobStatus: filter.jobStatus || '',
            ActiveTab: mode,
            ReportOption: 'Job Report'
        };

        if (this.currentAjaxRequest && this.currentAjaxRequest.readyState !== 4) {
            this.currentAjaxRequest.abort();
        }

        return new Promise(resolve => {
            self.currentAjaxRequest = $.ajax({
                url: '/JobStatusReport/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(reportPayload),
                success: function (res) {
                    if (res && res.success) {
                        self.cachedData.filterKey = filterKey;
                        var resolvedMode = res.mode || mode;
                        var fullAir = res.rawAirData || res.airData || [];
                        var fullSea = res.rawSeaData || res.seaData || [];

                        self.cachedData['Air'] = fullAir;
                        self.cachedData['Sea'] = fullSea;

                        var activeData = resolvedMode === 'Air' ? (res.airData || []) : (res.seaData || []);
                        self.renderToActiveTable(resolvedMode, activeData);
                    } else {
                        self.clearTables();
                    }
                },
                error: function (xhr, status) {
                    if (status !== 'abort') {
                        self.clearTables();
                    }
                },
                complete: function () {
                    self.currentAjaxRequest = null;
                    resolve();
                }
            });
        });
    },

    applyStatusFilterInstant(statusFilter) {
        var mode = this.activeMode;
        var rawList = this.cachedData[mode];
        if (!rawList) {
            const f = window.DashboardApp ? window.DashboardApp.getFilterState() : {};
            this.loadGridData(f, true);
            return;
        }

        var status = (statusFilter || '').toLowerCase().trim();
        var filtered = rawList;
        if (status.indexOf('pend') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'pending'; });
        } else if (status.indexOf('transit') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'transit'; });
        } else if (status.indexOf('deliver') !== -1) {
            filtered = rawList.filter(function (x) { return x.statusClass === 'delivered'; });
        }

        this.renderToActiveTable(mode, filtered);
    },

    renderToActiveTable(mode, dataList) {
        this.updateTableVisibility(mode);
        var table = (mode === 'Air') ? this.airTable : this.seaTable;
        if (!table) return;

        var list = dataList || [];
        var prepared = [];
        for (var i = 0; i < list.length; i++) {
            prepared.push(Object.assign({}, list[i], { slNo: i + 1 }));
        }

        table.clear();
        table.rows.add(prepared);
        table.draw(false);

        if (typeof $.fn.tooltip === 'function') {
            $('[data-bs-toggle="tooltip"]').tooltip({
                boundary: 'window',
                container: 'body'
            });
        }

        requestAnimationFrame(function () {
            table.columns.adjust();
        });
    },

    clearTables() {
        if (this.seaTable) this.seaTable.clear().draw(false);
        if (this.airTable) this.airTable.clear().draw(false);
    },

    render(data) {
        const k = data.kpis || data.kpIs || data.KPIs || {};

        const total = Number(k.totalJobs ?? k.TotalJobs ?? 0);
        const pending = Number(k.pendingJobs ?? k.PendingJobs ?? 0);
        const transit = Number(k.inTransitJobs ?? k.InTransitJobs ?? k.transitJobs ?? k.TransitJobs ?? 0);
        const delivered = Number(k.deliveredJobs ?? k.DeliveredJobs ?? 0);

        const penPct = total > 0 ? (pending / total * 100).toFixed(1) : '0.0';
        const trPct = total > 0 ? (transit / total * 100).toFixed(1) : '0.0';
        const delPct = total > 0 ? (delivered / total * 100).toFixed(1) : '0.0';

        // 1. Top 4 KPI Cards (Matching exact prototype)
        setK('j_tot', int(total), 'jobs in period');
        setK('j_pen', int(pending), penPct + '% of total');
        setK('j_tr', int(transit), trPct + '% of total');
        setK('j_del', int(delivered), delPct + '% of total');

        // 2. Minis (5 Cards matching exact prototype)
        const setMini = (id, val, sub = null) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
                if (sub !== null) {
                    const sEl = el.querySelector('.msub');
                    if (sEl) sEl.innerHTML = sub;
                }
            }
        };

        const avgDays = Number(k.avgClearanceDays ?? k.AvgClearanceDays ?? 10.5);
        setMini('jm_avg', avgDays.toFixed(1) + ' days');

        const onTimePct = Number(k.onTimeDeliveryRate ?? k.OnTimeDeliveryRate ?? 50.4);
        setMini('jm_ot', onTimePct.toFixed(1) + '%', '&le; 10 days');

        const docsPending = Number(k.docsPending ?? k.DocsPending ?? 0);
        setMini('jm_doc', int(docsPending));

        const arrivals7 = Number(k.arrivalsIn7Days ?? k.ArrivalsIn7Days ?? 0);
        setMini('jm_arr', int(arrivals7), 'ETA &le; 7 days');

        const totalInv = Number(k.totalInvoiceValue ?? k.TotalInvoiceValue ?? 0);
        setMini('jm_val', '$ ' + money(totalInv));

        // Status Colors matching prototype
        const sc = {
            'Pending': '#f08c00',
            'In Transit': '#8e24aa',
            'Delivered': '#1b8a3c'
        };

        // 3. Chart 1: Job Status Share (Doughnut with center text)
        mkChart('j_ch1', 'doughnut', ['Pending', 'In Transit', 'Delivered'], [
            pieDs([pending, transit, delivered], [sc['Pending'], sc['In Transit'], sc['Delivered']])
        ], {
            center: { text: int(total), sub: 'jobs' },
            legendPosition: 'right'
        });

        // 4. Chart 2: Jobs by Shipment Mode (Stacked vertical bar chart by status)
        const modes = data.modeDistributions || data.ModeDistributions || [];
        mkChart('j_ch2', 'bar', modes.map(m => m.modeName || m.ModeName || ''), [
            barDs('Pending', modes.map(m => Number(m.pendingCount ?? m.PendingCount ?? 0)), sc['Pending'], { maxBarThickness: 38 }),
            barDs('In Transit', modes.map(m => Number(m.transitCount ?? m.TransitCount ?? 0)), sc['In Transit'], { maxBarThickness: 38 }),
            barDs('Delivered', modes.map(m => Number(m.deliveredCount ?? m.DeliveredCount ?? m.jobCount ?? m.JobCount ?? 0)), sc['Delivered'], { maxBarThickness: 38 })
        ], {
            stacked: true
        });

        // 5. Chart 3: Port-wise Workload (Horizontal bar chart)
        const ports = data.portWorkloads || data.PortWorkloads || [];
        mkChart('j_ch3', 'bar', ports.map(p => p.portName || p.PortName || ''), [
            barDs('Jobs', ports.map(p => Number(p.jobCount ?? p.JobCount ?? 0)), '#0a9ac9')
        ], {
            horizontal: true,
            legend: false
        });

        // 6. Chart 4: Monthly Job Trend (Stacked vertical bar chart by status)
        const trends = data.monthlyTrends || data.MonthlyTrends || [];
        const mLabels = trends.map(t => t.monthLabel || t.MonthLabel || t.monthKey || t.MonthKey || '');
        mkChart('j_ch4', 'bar', mLabels, [
            barDs('Pending', trends.map(t => Number(t.pendingCount ?? t.PendingCount ?? 0)), sc['Pending'], { maxBarThickness: 34 }),
            barDs('In Transit', trends.map(t => Number(t.transitCount ?? t.TransitCount ?? 0)), sc['In Transit'], { maxBarThickness: 34 }),
            barDs('Delivered', trends.map(t => Number(t.deliveredCount ?? t.DeliveredCount ?? t.deliveredJobs ?? t.DeliveredJobs ?? 0)), sc['Delivered'], { maxBarThickness: 34 })
        ], {
            stacked: true,
            legendPosition: 'top'
        });

        // 7. Chart 5: Top Customers (Horizontal bar chart)
        const topCust = data.topCustomers || data.TopCustomers || [];
        mkChart('j_ch5', 'bar', topCust.map(c => c.customerName || c.CustomerName || 'Unknown'), [
            barDs('Jobs', topCust.map(c => Number(c.totalJobs ?? c.TotalJobs ?? c.jobCount ?? c.JobCount ?? 0)), cssVar('--blue') || '#0b57d0')
        ], {
            horizontal: true,
            legend: false
        });
    },

    openReportPreviewModal(title, url) {
        var $iframe = $('#reportPreviewIframe');
        var $spinner = $('#modalHeaderSpinner');
        var $title = $('#reportPreviewModalLabel');
        var $newTab = $('#btnModalOpenNewTab');

        $title.html('<i class="fas fa-file-pdf text-danger me-2"></i>' + this.escapeHtml(title));
        $newTab.attr('href', url);
        $spinner.show();

        var modalEl = document.getElementById('reportPreviewModal');
        if (modalEl && typeof bootstrap !== 'undefined') {
            var modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
            modalInstance.show();
        }

        $iframe.attr('src', url);
        $iframe.off('load').on('load', function () {
            $spinner.hide();
        });
        setTimeout(function () {
            $spinner.hide();
        }, 800);
    },

    formatAmount(val) {
        if (val === null || val === undefined || val === '') return '';
        var num = parseFloat(String(val).replace(/,/g, ''));
        if (isNaN(num)) return this.escapeHtml(val);
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    },

    formatInvoiceValue(val, symbol, name) {
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
        var numStr = this.formatAmount(val);
        if (!numStr) return '';
        return '<span class="currency-sym">' + this.escapeHtml(sym) + '</span> ' + numStr;
    },

    escapeHtml(str) {
        if (!str) return '';
        return $('<div>').text(str).html();
    },

    handleReportExport(format) {
        var filter = window.DashboardApp ? window.DashboardApp.getFilterState() : {};
        var params = new URLSearchParams();
        if (filter.fromDate) params.append('FromDate', filter.fromDate);
        if (filter.toDate) params.append('ToDate', filter.toDate);
        if (filter.branchCode) params.append('BranchCode', filter.branchCode);
        if (filter.shipmentMode) params.append('ShipmentMode', filter.shipmentMode);
        if (filter.port) params.append('PortType', filter.port);
        if (filter.customerID) params.append('CustomerId', filter.customerID);
        if (filter.jobStatus) params.append('JobStatus', filter.jobStatus);
        params.append('ActiveTab', this.activeMode || 'Sea');

        var fmt = (format || 'PDF').toUpperCase();
        if (fmt === 'PREVIEW') {
            params.append('exportFormat', 'PDF');
            params.append('download', 'false');
            var url = '/JobStatusReport/Export?' + params.toString();
            this.openReportPreviewModal('Job Status Report Preview', url);
        } else if (fmt === 'EXCEL') {
            params.append('exportFormat', 'Excel');
            params.append('download', 'true');
            window.location.href = '/JobStatusReport/Export?' + params.toString();
        } else {
            params.append('exportFormat', 'PDF');
            params.append('download', 'true');
            window.location.href = '/JobStatusReport/Export?' + params.toString();
        }
    }
};
