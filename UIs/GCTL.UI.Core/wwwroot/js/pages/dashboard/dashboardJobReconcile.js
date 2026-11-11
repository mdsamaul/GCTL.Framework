/**
 * Job Reconcile (Actual) Tab Module
 * Integrated with _CustomGrid and JobWiseReconcileStatementController
 */
'use strict';

// Register formatters for CustomDataTable
window.gctlRenderers = window.gctlRenderers || {};
if (!window.gctlRenderers.formatMoney) {
    window.gctlRenderers.formatMoney = function (data, type) {
        if (data === null || data === undefined || data === '' || (typeof data === 'string' && data.trim() === '') || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '-';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') return num;
        if (num === 0) return '-';
        return typeof formatMoney === 'function' ? formatMoney(num) : num.toLocaleString('en-IN');
    };
}
if (!window.gctlRenderers.formatPercent) {
    window.gctlRenderers.formatPercent = function (data, type) {
        if (data === null || data === undefined || data === '' || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '0.00%';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') return num;
        return num.toFixed(2) + '%';
    };
}

window.DashboardJobReconcile = {
    gridTable: null,
    isGridInitialized: false,

    initGrid() {
        if (this.isGridInitialized) return;
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            this.gridTable = $.gctlGrid.initFromModel({
                gridId: 'jobReconcileTable'
            });

            // Enforce side-by-side flex layout on DataTables top and bottom toolbars
            $('#jobReconcileTable').on('init.dt draw.dt', function () {
                const $container = $(this).closest('.dt-container, .dataTables_wrapper');
                $container.find('> .row, .dt-layout-row').each(function () {
                    if ($(this).find('table').length === 0) {
                        $(this).attr('style', 'display: flex !important; flex-direction: row !important; justify-content: space-between !important; align-items: center !important; flex-wrap: nowrap !important; width: 100% !important; margin: 6px 0 !important;');
                        $(this).find('.dt-layout-start, [class*="col-"]:first-child').attr('style', 'display: flex !important; align-items: center !important; justify-content: flex-start !important; flex: 0 0 auto !important; width: auto !important;');
                        $(this).find('.dt-layout-end, [class*="col-"]:last-child').attr('style', 'display: flex !important; align-items: center !important; justify-content: flex-end !important; flex: 0 0 auto !important; margin-left: auto !important; width: auto !important;');
                    } else {
                        $(this).attr('style', 'display: block !important; width: 100% !important;');
                    }
                });
            });

            this.isGridInitialized = true;
        }
    },

    async load(filter) {
        this.initGrid();

        if (window.DashboardApp) window.DashboardApp.showLoader('p-recon');
        try {
            const resp = await fetch('/DashboardJobReconcile/GetData', {
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
            console.error('Error loading Job Reconcile data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-recon');
        }
    },

    render(data) {
        if (!data) return;
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs (Formatted with Indian lakh/crore commas, no abbreviations)
        setK('r_n', int(k.totalBills ?? k.TotalBills), 'jobs reconciled');
        setK('r_nbv', formatMoney(k.netBillValue ?? k.NetBillValue), 'total revenue');
        setK('r_cost', formatMoney(k.totalCosting ?? k.TotalCosting), 'actual expense');
        setK('r_gp', formatMoney(k.grossProfit ?? k.GrossProfit), 'net profit');
        setK('r_pct', pct(k.gpMargin ?? k.GPMargin, 2), 'gross margin');

        // Minis (No subtext, full money format)
        const setMini = (id, val) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
            }
        };

        setMini('rm_avg', formatMoney(k.avgProfitPerJob));
        setMini('rm_ae', formatMoney(k.avgExpensePerJob));
        setMini('rm_best', pct(k.bestCustomerGP, 1));
        setMini('rm_low', pct(k.lowestMarginGP, 1));
        setMini('rm_top', k.largestExpenseHead || '-');

        // Chart 1: Actual Expense by Head (Doughnut with centerText)
        const heads = data.topExpenseHeads || [];
        const topHeads = heads.slice(0, 7);
        const restAmt = heads.slice(7).reduce((acc, cur) => acc + (Number(cur.amount) || 0), 0);
        const pieHeads = topHeads.map(h => [h.expenseHead, Number(h.amount) || 0]);
        if (restAmt > 0) pieHeads.push(['Others', restAmt]);

        mkChart('r_ch1', 'doughnut', pieHeads.map(x => x[0]), [
            pieDs(pieHeads.map(x => x[1]))
        ], {
            money: true,
            center: { text: formatMoney(k.totalCosting ?? 0), sub: 'actual expense' }
        });

        // Chart 2: Revenue Split (Expense vs Profit Doughnut with centerText)
        mkChart('r_ch2', 'doughnut', ['Total Expense', 'Gross Profit'], [
            pieDs([k.totalCosting, k.grossProfit], [
                cssVar('--orange') || '#f08c00',
                cssVar('--green') || '#1b8a3c'
            ])
        ], {
            money: true,
            center: { text: pct(k.gpMargin ?? 0, 1), sub: 'GP margin' }
        });

        // Chart 3: Job GP% Distribution (Brackets)
        const brk = data.gpBrackets || {};
        const brkLabels = ['< 0%', '0–20%', '20–30%', '30–40%', '40–50%', '50%+'];
        const brkData = [
            brk.bracket_Neg || 0,
            brk.bracket_0_20 || 0,
            brk.bracket_20_30 || 0,
            brk.bracket_30_40 || 0,
            brk.bracket_40_50 || 0,
            brk.bracket_50_Plus || 0
        ];
        mkChart('r_ch3', 'bar', brkLabels, [
            barDs('Jobs', brkData, [
                cssVar('--red') || '#d92d3a',
                cssVar('--orange') || '#f08c00',
                cssVar('--sky') || '#0a9ac9',
                cssVar('--blue') || '#0b57d0',
                cssVar('--purple') || '#8e24aa',
                cssVar('--green') || '#1b8a3c'
            ])
        ], { legend: false });

        // Chart 4: Customer-wise Revenue, Expense & Profit
        const topCust = data.topCustomers || [];
        mkChart('r_ch4', 'bar', topCust.map(c => c.customerName), [
            barDs('Revenue', topCust.map(c => c.revenue), cssVar('--blue') || '#0b57d0'),
            barDs('Expense', topCust.map(c => c.expense), cssVar('--orange') || '#f08c00'),
            barDs('Profit', topCust.map(c => c.profit), cssVar('--green') || '#1b8a3c')
        ], { money: true, legendAlign: 'end' });

        // Chart 5: Top Expense Heads (Horizontal Bar)
        mkChart('r_ch5', 'bar', heads.map(h => h.expenseHead), [
            barDs('Amount', heads.map(h => h.amount), cssVar('--orange') || '#f08c00')
        ], { horizontal: true, money: true, legend: false });

        // Grid: Update Dynamic Column Headers & Populate Rows
        const rows = data.reconcileList || [];
        if (this.gridTable) {
            if (rows.length > 0) {
                const first = rows[0];
                const h1 = first.head1Name || 'Head 1';
                const h2 = first.head2Name || 'Head 2';
                const h3 = first.head3Name || 'Head 3';

                const updateHeader = (colIdx, title) => {
                    const $hdr = $(this.gridTable.column(colIdx).header());
                    const $title = $hdr.find('.dt-column-title, .th-full, span').first();
                    if ($title.length) {
                        $title.text(title);
                    } else {
                        $hdr.text(title);
                    }
                };
                updateHeader(4, h1);
                updateHeader(5, h2);
                updateHeader(6, h3);
            }
            this.gridTable.clear().rows.add(rows).draw();
            this.gridTable.columns.adjust();
            setTimeout(() => {
                if (this.gridTable) this.gridTable.columns.adjust();
            }, 100);
        }
    },

    exportExcel() {
        const f = (window.DashboardApp && typeof window.DashboardApp.getFilterState === 'function')
            ? window.DashboardApp.getFilterState()
            : {};
        const params = new URLSearchParams({
            ExpenseTypeId: f.shipmentMode || $('#fMode').val() || '',
            FromDate: f.fromDate || $('#fFrom').val() || '',
            ToDate: f.toDate || $('#fTo').val() || '',
            CustomerId: f.customerID || f.customerId || $('#fCust').val() || '',
            ExportFormat: 'XLSX'
        });
        window.location.href = '/JobWiseReconcileStatement/ExportExcel?' + params.toString();
    }
};

// Wire Export Dropdown specifically for #recon tab
(function () {
    function updateDropdownVisibility() {
        const isRecon = document.getElementById('p-recon')?.classList.contains('on');
        const isBills = document.getElementById('p-bills')?.classList.contains('on');
        const previewBtn = document.getElementById('xPreview');
        const pdfBtn = document.getElementById('xPdf');
        const excelBtn = document.getElementById('xExcel');

        if (isRecon) {
            if (previewBtn) previewBtn.style.display = 'none';
            if (pdfBtn) pdfBtn.style.display = 'none';
            if (excelBtn) excelBtn.style.display = '';
            setTimeout(() => {
                if (window.DashboardJobReconcile && window.DashboardJobReconcile.gridTable) {
                    window.DashboardJobReconcile.gridTable.columns.adjust();
                }
            }, 60);
        } else {
            // Restore buttons when leaving #recon tab (unless #bills is active)
            if (previewBtn && previewBtn.style.display === 'none') previewBtn.style.display = '';
            if (pdfBtn && pdfBtn.style.display === 'none') pdfBtn.style.display = '';
            if (excelBtn && !isBills && excelBtn.style.display === 'none') excelBtn.style.display = '';
        }
    }

    function initExportWiring() {
        const pRecon = document.getElementById('p-recon');
        if (pRecon) {
            const observer = new MutationObserver(updateDropdownVisibility);
            observer.observe(pRecon, { attributes: true, attributeFilter: ['class'] });
            updateDropdownVisibility();
        }

        // Also check on tab buttons click
        document.querySelectorAll('#tabs .tab').forEach(btn => {
            btn.addEventListener('click', () => setTimeout(updateDropdownVisibility, 50));
        });

        // Intercept Excel click for #recon tab in capture phase
        document.getElementById('xExcel')?.addEventListener('click', function (e) {
            if (document.getElementById('p-recon')?.classList.contains('on')) {
                e.stopImmediatePropagation();
                e.preventDefault();
                document.getElementById('expMenu')?.classList.remove('open');
                window.DashboardJobReconcile.exportExcel();
            }
        }, true);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initExportWiring);
    } else {
        initExportWiring();
    }
})();
