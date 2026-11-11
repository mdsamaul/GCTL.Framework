/**
 * Bill Entry Tab Module
 * Integrated with JobBillEntryReport Grid and CNF_Dashboard KPIs/Charts
 */
'use strict';

window.DashboardBillEntry = {
    gridTable: null,
    isGridInitialized: false,

    initGrid() {
        if (this.isGridInitialized) return;
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            this.gridTable = $.gctlGrid.initFromModel({
                gridId: 'jobBillEntryReportTable',
                ajaxData: function (d) {
                    const f = window.DashboardApp ? window.DashboardApp.getFilter() : {};
                    d.fromDate = f.fromDate || $('#fFrom').val() || '';
                    d.toDate = f.toDate || $('#fTo').val() || '';
                    d.shipmentModes = f.shipmentMode || $('#fMode').val() || '';
                    d.customerIds = f.customerId || $('#fCust').val() || '';
                }
            });

            // Enforce side-by-side flex layout on DataTables top and bottom toolbars
            $('#jobBillEntryReportTable').on('init.dt draw.dt', function () {
                const $container = $(this).closest('.dt-container, .dataTables_wrapper');
                $container.find('> .row, .dt-layout-row').each(function () {
                    if ($(this).find('table').length === 0) {
                        $(this).attr('style', 'display: flex !important; flex-direction: row !important; justify-content: space-between !important; align-items: center !important; flex-wrap: nowrap !important; width: 100% !important; margin: 6px 0 !important;');
                        $(this).find('.dt-layout-start, [class*="col-"]:first-child').attr('style', 'display: flex !important; align-items: center !important; justify-content: flex-start !important; flex: 0 0 auto !important; width: auto !important;');
                        $(this).find('.dt-layout-end, [class*="col-"]:last-child').attr('style', 'display: flex !important; align-items: center !important; justify-content: flex-end !important; flex: 0 0 auto !important; margin-left: auto !important; width: auto !important;');
                    }
                });
            });

            this.isGridInitialized = true;
        }
    },

    reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('jobBillEntryReportTable', true);
        } else if (this.gridTable && this.gridTable.ajax) {
            this.gridTable.ajax.reload(null, true);
        }
    },

    async load(filter) {
        this.initGrid();
        this.reloadGrid();

        if (window.DashboardApp) window.DashboardApp.showLoader('p-bills');
        try {
            const resp = await fetch('/DashboardBillEntry/GetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter)
            });
            const res = await resp.json();
            if (!res.isSuccess) {
                console.error(res.message);
                return;
            }
            this.render(res.data);
        } catch (err) {
            console.error('Error loading Bill Entry data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-bills');
        }
    },

    render(data) {
        if (!data) return;
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // 1. Top 5 Main KPIs (Full numbers with Indian grouping, no abbreviation)
        setK('b_n', int(k.totalBills ?? k.TotalBills), 'bills in period');
        setK('b_nbv', formatMoney(k.netBillValue ?? k.NetBillValue), 'after deductions');

        const netVal = Number(k.netBillValue ?? k.NetBillValue ?? 0);
        const dueVal = Number(k.totalOutstandingDue ?? k.TotalOutstandingDue ?? 0);
        const duePct = netVal > 0 ? ((dueVal / netVal) * 100).toFixed(1) + '% of billed' : 'uncollected balance';
        setK('b_due', formatMoney(dueVal), duePct);

        const lastVal = Number(k.lastServiceValue ?? k.LastServiceValue ?? 0);
        const lastBill = k.lastBillNo ?? k.LastBillNo ?? '';
        setK('b_lv', formatMoney(lastVal), lastBill && lastBill !== '-' ? `Last bill: <b>${esc(lastBill)}</b>` : 'last bill');

        const lastDate = k.lastServiceDate ?? k.LastServiceDate ?? '–';
        setK('b_ld', lastDate, lastBill && lastBill !== '-' ? `Bill No: <b>${esc(lastBill)}</b>` : 'last bill date');

        // 2. 5 Minis (Second KPI Row - Indian grouping, no abbreviation)
        const setMini = (id, val) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
            }
        };

        setMini('bm_col', formatMoney(k.totalCollected ?? k.TotalCollected));
        setMini('bm_rate', pct(k.collectionEfficiency ?? k.CollectionEfficiency, 1));
        setMini('bm_avg', formatMoney(k.avgBillValue ?? k.AvgBillValue));
        setMini('bm_comm', formatMoney(k.agencyCommission ?? k.AgencyCommission));
        setMini('bm_ded', formatMoney(k.totalDeductions ?? k.TotalDeductions));

        // 3. Chart 1: Service Value Composition (doughnut with legend on the right)
        const comps = data.serviceValueCompositions || [];
        const totalGross = comps.reduce((s, c) => s + (c.amount || 0), 0);
        mkChart('b_ch1', 'doughnut', comps.map(c => c.serviceHead), [
            pieDs(comps.map(c => c.amount))
        ], { money: true, center: { text: compact(totalGross), sub: 'gross billed' } });

        // 4. Chart 2: Net Service Value by Shipment Mode
        const modes = data.modeBillings || [];
        mkChart('b_ch2', 'bar', modes.map(m => m.modeName), [
            barDs('Net Service Value', modes.map(m => m.totalBilling), cssVar('--green') || '#1b8a3c')
        ], { money: true, legend: false });

        // 5. Chart 3: Collection Status (doughnut with legend on the right)
        const colls = data.collectionStatuses || [];
        const colAmt = (colls.find(c => c.statusName === 'Collected') || {}).amount || 0;
        const totalAmt = colls.reduce((s, c) => s + (c.amount || 0), 0);
        const colPct = totalAmt > 0 ? ((colAmt / totalAmt) * 100).toFixed(0) : '0';
        mkChart('b_ch3', 'doughnut', colls.map(c => c.statusName), [
            pieDs(colls.map(c => c.amount), [
                cssVar('--green') || '#1b8a3c',
                cssVar('--orange') || '#f08c00'
            ])
        ], { money: true, center: { text: colPct + '%', sub: 'collected' } });

        // 6. Chart 4: Monthly Billing & Collections (Collected and Due legend right-aligned)
        const trends = data.monthlyTrends || [];
        mkChart('b_ch4', 'bar', trends.map(t => t.monthLabel), [
            barDs('Collected', trends.map(t => t.collectedAmount), cssVar('--green') || '#1b8a3c'),
            barDs('Due', trends.map(t => t.dueAmount), cssVar('--orange') || '#f08c00')
        ], { money: true, stacked: true, legendAlign: 'end' });

        // 7. Chart 5: Top Customers by Billing (horizontal bar with customer names on Y-axis and vertical grid lines)
        const topCust = data.topCustomers || [];
        mkChart('b_ch5', 'bar', topCust.map(c => c.customerName), [
            barDs('Net Amount', topCust.map(c => c.totalBilled), cssVar('--blue') || '#0b57d0')
        ], { horizontal: true, money: true, legend: false });
    },

    getExportQueryParams(isDownload) {
        const f = window.DashboardApp ? window.DashboardApp.getFilter() : {};
        const fromDate = f.fromDate || $('#fFrom').val() || '';
        const toDate = f.toDate || $('#fTo').val() || '';
        const shipmentModes = f.shipmentMode || $('#fMode').val() || '';
        const customerIds = f.customerId || $('#fCust').val() || '';

        // Collect checked bill numbers from the bottom grid checkboxes
        const selectedBills = [];
        $('#jobBillEntryReportTable tbody input.chk-bill-item:checked, #jobBillEntryReportTable tbody input[type="checkbox"]:checked').each(function () {
            const val = $(this).val() || $(this).data('id');
            if (val) selectedBills.push(val);
        });

        const params = {
            FromDate: fromDate,
            ToDate: toDate,
            ShipmentModes: shipmentModes,
            CustomerIds: customerIds,
            SelectedBillNos: selectedBills.join(','),
            download: isDownload ? true : false
        };
        return $.param(params);
    },

    previewPdf() {
        const queryParams = this.getExportQueryParams(false);
        window.open('/JobBillEntryReport/ExportPdf?' + queryParams, '_blank');
    },

    exportPdf() {
        const queryParams = this.getExportQueryParams(true);
        window.location.href = '/JobBillEntryReport/ExportPdf?' + queryParams;
    }
};

// Wire Export Dropdown specifically for #bills tab
(function () {
    function updateExcelVisibility() {
        const isBills = document.getElementById('p-bills')?.classList.contains('on');
        const excelBtn = document.getElementById('xExcel');
        if (excelBtn) {
            excelBtn.style.display = isBills ? 'none' : '';
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initExportWiring);
    } else {
        initExportWiring();
    }

    function initExportWiring() {
        const pBills = document.getElementById('p-bills');
        if (pBills) {
            const observer = new MutationObserver(updateExcelVisibility);
            observer.observe(pBills, { attributes: true, attributeFilter: ['class'] });
            updateExcelVisibility();
        }

        // Also check on tab buttons click
        document.querySelectorAll('#tabs .tab').forEach(btn => {
            btn.addEventListener('click', () => setTimeout(updateExcelVisibility, 50));
        });

        // Intercept Print / Preview for #bills tab in capture phase
        document.getElementById('xPreview')?.addEventListener('click', function (e) {
            if (document.getElementById('p-bills')?.classList.contains('on')) {
                e.stopImmediatePropagation();
                e.preventDefault();
                document.getElementById('expMenu')?.classList.remove('open');
                window.DashboardBillEntry.previewPdf();
            }
        }, true);

        // Intercept PDF for #bills tab in capture phase
        document.getElementById('xPdf')?.addEventListener('click', function (e) {
            if (document.getElementById('p-bills')?.classList.contains('on')) {
                e.stopImmediatePropagation();
                e.preventDefault();
                document.getElementById('expMenu')?.classList.remove('open');
                window.DashboardBillEntry.exportPdf();
            }
        }, true);
    }
})();
