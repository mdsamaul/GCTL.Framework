/**
 * Job Reconcile (Actual) Tab Module
 */
'use strict';

window.DashboardJobReconcile = {
    grid: null,

    initGrid(firstRow) {
        const h1 = firstRow?.head1Name || 'Expense Head 1';
        const h2 = firstRow?.head2Name || 'Expense Head 2';
        const h3 = firstRow?.head3Name || 'Expense Head 3';

        this.grid = new Grid('r_g', {
            card: 'g760',
            totals: true,
            columns: [
                { t: 'sn', l: 'SL', w: '38px' },
                { k: 'jobNo', l: 'Job No', cls: 'mono nw' },
                { k: 'customerName', l: 'Customer', hide: 'md' },
                { k: 'revenue', l: 'Revenue / Bill', t: 'money', tot: true },
                { k: 'head1Amt', l: h1, t: 'money', tot: true, hide: 'md' },
                { k: 'head2Amt', l: h2, t: 'money', tot: true, hide: 'md' },
                { k: 'head3Amt', l: h3, t: 'money', tot: true, hide: 'lg' },
                { k: 'otherExpenses', l: 'Other Expenses', t: 'money', tot: true, hide: 'lg' },
                { k: 'totalExpense', l: 'Total Expense', t: 'money', tot: true },
                { k: 'profit', l: 'Profit', t: 'money', tot: true },
                { k: 'gpPercent', l: 'GP %', t: 'pct' }
            ]
        });
    },

    async load(filter) {
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
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs
        setK('r_n', int(k.totalBills ?? k.TotalBills), 'jobs reconciled');
        setK('r_nbv', cur(k.netBillValue ?? k.NetBillValue), 'total revenue');
        setK('r_cost', cur(k.totalCosting ?? k.TotalCosting), 'actual expense');
        setK('r_gp', cur(k.grossProfit ?? k.GrossProfit), 'net profit');
        setK('r_pct', pct(k.gpMargin ?? k.GPMargin, 2), 'gross margin');

        // Minis
        const setMini = (id, val, sub = null) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
                if (sub !== null) {
                    const sEl = el.querySelector('.msub');
                    if (sEl) sEl.textContent = sub;
                }
            }
        };

        setMini('rm_avg', money(k.avgProfitPerJob), 'mean profit');
        setMini('rm_ae', money(k.avgExpensePerJob), 'mean expense');
        setMini('rm_best', pct(k.bestCustomerGP, 1), k.bestCustomer || '-');
        setMini('rm_low', pct(k.lowestMarginGP, 1), k.lowestMarginJob || '-');
        setMini('rm_top', k.largestExpenseHead || '-', cur(k.largestExpenseHeadAmt));

        // Chart 1: Actual Expense by Head (Doughnut)
        const heads = data.topExpenseHeads || [];
        mkChart('r_ch1', 'doughnut', heads.map(h => h.expenseHead), [
            pieDs(heads.map(h => h.amount))
        ], { money: true });

        // Chart 2: Revenue Split (Expense vs Profit Doughnut)
        mkChart('r_ch2', 'doughnut', ['Total Expense', 'Gross Profit'], [
            pieDs([k.totalCosting, k.grossProfit], [
                cssVar('--orange') || '#f08c00',
                cssVar('--green') || '#1b8a3c'
            ])
        ], { money: true });

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
        ], { money: true });

        // Chart 5: Top Expense Heads
        mkChart('r_ch5', 'bar', heads.map(h => h.expenseHead), [
            barDs('Amount', heads.map(h => h.amount), cssVar('--purple') || '#8e24aa')
        ], { horizontal: true, money: true, legend: false });

        // Grid
        const rows = data.reconcileList || [];
        this.initGrid(rows[0]);
        if (this.grid) {
            this.grid.setRows(rows);
        }
    }
};

