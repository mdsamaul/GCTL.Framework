/**
 * Income Statement Sub-Tab Module (Accounts)
 * Matches CNF_Dashboard.html behavior and charts
 */
'use strict';

window.DashboardIncomeStatement = {
    grid: null,
    rawData: null,
    lastFilter: null,

    initGrid() {
        if (this.grid) return;

        this.grid = new Grid('i_g', {
            card: 'g760',
            sortable: false,
            search: true,
            size: 10,
            sizes: [10, 25, 50, 'All'],
            rowCls: r => r.kind || r.Kind,
            columns: [
                {
                    k: 'label',
                    l: 'Particulars',
                    r: r => {
                        const kind = r.kind || r.Kind;
                        const label = r.label || r.Label;
                        const isHead = kind === 'head';
                        const isTotal = kind === 'total';
                        const isSub = kind === 'sub';
                        const indent = (!isHead && !isTotal && !isSub) ? 18 : 0;
                        const weight = isTotal ? 'font-weight:800;' : (isHead || isSub ? 'font-weight:700;' : '');
                        return `<span style="padding-left:${indent}px;${weight}">${esc(label)}</span>`;
                    },
                    x: r => r.label || r.Label
                },
                {
                    k: 'amt',
                    l: 'Amount (BDT)',
                    t: 'money',
                    r: r => {
                        const val = r.amt ?? r.Amt;
                        if (val == null) return '';
                        if (val === 0) return '–';
                        if (val < 0) return `(${amt(Math.abs(val))})`;
                        return amt(val);
                    },
                    x: r => r.amt ?? r.Amt
                },
                {
                    k: 'pct',
                    l: '% of Revenue',
                    t: 'pct',
                    r: r => {
                        const val = r.pct ?? r.Pct;
                        return val == null ? '' : pct(val, 2);
                    },
                    x: r => r.pct ?? r.Pct
                }
            ]
        });
    },

    async load(filter) {
        this.lastFilter = filter;
        this.initGrid();
        if (window.DashboardApp) window.DashboardApp.showLoader('s-is');

        try {
            const resp = await fetch('/DashboardIncomeStatement/GetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter || {})
            });
            const res = await resp.json();
            if (!res.isSuccess) {
                console.error(res.message);
                return;
            }
            this.rawData = res.data;
            this.render(res.data);
        } catch (err) {
            console.error('Error loading Income Statement data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('s-is');
        }
    },

    render(data) {
        if (!data) return;
        const k = data.kpis || data.kpIs || data.KPIs || {};

        const rev = k.totalRevenue ?? k.TotalRevenue ?? 0;
        const opRev = k.operatingRevenue ?? k.OperatingRevenue ?? 0;
        const dir = k.directExpenses ?? k.DirectExpenses ?? 0;
        const gp = k.grossProfit ?? k.GrossProfit ?? 0;
        const ind = k.indirectExpenses ?? k.IndirectExpenses ?? 0;
        const fin = k.financeCost ?? k.FinanceCost ?? 0;
        const tax = k.taxExpense ?? k.TaxExpense ?? 0;
        const np = k.netProfit ?? k.NetProfit ?? 0;

        const gpMargin = k.gpMarginPct ?? k.GPMarginPct ?? (opRev ? (gp / opRev * 100) : 0);
        const npMargin = k.npMarginPct ?? k.NPMarginPct ?? (rev ? (np / rev * 100) : 0);
        const dirPct = k.directPctOfRevenue ?? k.DirectPctOfRevenue ?? (opRev ? (dir / opRev * 100) : 0);

        // 1. KPIs (Matching CNF_Dashboard.html renderIS)
        setK('i_rev', cur(rev), `Operating ${compact(opRev)}`);
        setK('i_dir', cur(dir), `${pct(dirPct, 1)} of operating income`);
        setK('i_gp', cur(gp), `GP margin ${pct(gpMargin, 1)}`);
        setK('i_ind', cur(ind + fin), `Indirect ${compact(ind)} · Finance ${compact(fin)}`);
        setK('i_np', cur(np), `Net margin ${pct(npMargin, 1)} · Tax ${compact(tax)}`);

        // 2. Chart 1: Profit Build-up (Bar)
        mkChart('i_ch1', 'bar', ['Revenue', 'Direct Exp.', 'Gross Profit', 'Indirect', 'Finance', 'Tax', 'Net Profit'], [
            barDs('BDT', [rev, dir, gp, ind, fin, tax, np], [
                cssVar('--blue') || '#0b57d0',
                cssVar('--orange') || '#f08c00',
                cssVar('--purple') || '#8e24aa',
                cssVar('--red') || '#d92d3a',
                cssVar('--slate') || '#64748b',
                '#a16207',
                cssVar('--green') || '#1b8a3c'
            ])
        ], { money: true, legend: false });

        // 3. Chart 2: Expense Composition (Doughnut)
        const expList = data.expenseComposition || data.ExpenseComposition || [];
        if (expList.length > 0) {
            const labels = expList.map(x => (x.label || x.Label || '').replace(/ ?&? ?$/, ''));
            const amounts = expList.map(x => x.amount ?? x.Amount ?? 0);
            const totalExp = amounts.reduce((a, b) => a + b, 0);
            mkChart('i_ch2', 'doughnut', labels, [pieDs(amounts)], {
                money: true,
                center: { text: compact(totalExp), sub: 'expenses' }
            });
        }

        // 4. Chart 3: Monthly Revenue, Expense & Net (Bar + Line)
        const trends = data.monthlyTrends || data.MonthlyTrends || [];
        if (trends.length > 0) {
            const mLabels = trends.map(x => x.monthLabel || x.MonthLabel || x.monthKey);
            const mRevs = trends.map(x => x.revenue ?? x.Revenue ?? 0);
            const mExps = trends.map(x => x.expense ?? x.Expense ?? 0);
            const mNps = trends.map(x => x.netProfit ?? x.NetProfit ?? 0);

            mkChart('i_ch3', 'bar', mLabels, [
                barDs('Revenue', mRevs, cssVar('--blue') || '#0b57d0'),
                barDs('Expense', mExps, cssVar('--orange') || '#f08c00'),
                {
                    type: 'line',
                    label: 'Net Profit',
                    data: mNps,
                    borderColor: cssVar('--green') || '#1b8a3c',
                    backgroundColor: cssVar('--green') || '#1b8a3c',
                    tension: 0.3,
                    pointRadius: 4
                }
            ], { money: true });
        }

        // 5. Grid Rows
        if (this.grid) {
            const rows = data.rows || data.Rows || [];
            this.grid.setRows(rows);
        }
    },

    // ---------------- Report Export (Preview / PDF / Excel) ----------------
    isActive() {
        const panelOn = document.getElementById('p-accounts')?.classList.contains('on');
        const subOn = document.getElementById('s-is')?.classList.contains('on');
        return !!(panelOn && subOn);
    },

    getExportParams(extra = {}) {
        const app = (typeof DashboardApp !== 'undefined') ? DashboardApp : window.DashboardApp;
        const f = (app && typeof app.getFilterState === 'function') ? app.getFilterState() : (this.lastFilter || {});
        const params = new URLSearchParams();
        if (f.fromDate) params.append('fromDate', f.fromDate);
        if (f.toDate) params.append('toDate', f.toDate);
        if (f.branchCode) params.append('branchCode', f.branchCode);
        if (f.companyCode) params.append('companyCode', f.companyCode);

        Object.keys(extra).forEach(k => params.append(k, extra[k]));
        return params.toString();
    },

    handleReportExport(format) {
        const fmt = (format || 'preview').toLowerCase();
        if (fmt === 'preview') {
            const url = '/DashboardIncomeStatement/ExportPdf?' + this.getExportParams({ download: 'false' });
            const app = (typeof DashboardApp !== 'undefined') ? DashboardApp : window.DashboardApp;
            if (app && typeof app.openReportPreviewModal === 'function') {
                app.openReportPreviewModal('Income Statement Report Preview', url);
            } else {
                window.open(url, '_blank');
            }
        } else if (fmt === 'pdf') {
            window.location.href = '/DashboardIncomeStatement/ExportPdf?' + this.getExportParams({ download: 'true' });
        } else if (fmt === 'excel') {
            window.location.href = '/DashboardIncomeStatement/ExportExcel?' + this.getExportParams();
        }
    }
};

// Intercept global Export menu when Accounts > Income Statement is active (capture phase)
(function () {
    function wire() {
        const map = { xPreview: 'Preview', xPdf: 'PDF', xExcel: 'Excel' };
        Object.keys(map).forEach(id => {
            document.getElementById(id)?.addEventListener('click', function (e) {
                const is = window.DashboardIncomeStatement;
                if (!is || !is.isActive()) return;
                e.stopImmediatePropagation();
                e.preventDefault();
                document.getElementById('expMenu')?.classList.remove('open');
                is.handleReportExport(map[id]);
            }, true);
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', wire);
    else wire();
})();

