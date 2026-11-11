/**
 * GP Analysis Tab Module
 */
'use strict';

window.DashboardGPAnalysis = {
    grid: null,

    initGrid() {
        if (!this.grid) {
            this.grid = new Grid('g_g', {
                card: 'g760',
                totals: true,
                columns: [
                    { t: 'sn', l: 'SL', w: '38px' },
                    { k: 'billingDate', l: 'Bill Date', cls: 'nw' },
                    { k: 'billNo', l: 'Bill No', cls: 'mono nw' },
                    { k: 'jobNo', l: 'Job No', cls: 'mono nw', hide: 'md' },
                    { k: 'customerName', l: 'Customer' },
                    { k: 'expenseType', l: 'Mode', hide: 'md' },
                    { k: 'commission', l: 'Commission', t: 'money', tot: true },
                    { k: 'othersBill', l: 'Others Bill', t: 'money', tot: true, hide: 'lg' },
                    { k: 'transportBill', l: 'Trans Bill', t: 'money', tot: true, hide: 'lg' },
                    { k: 'totalBill', l: 'Total Bill', t: 'money', tot: true },
                    { k: 'cnfCost', l: 'C&F Cost', t: 'money', tot: true, hide: 'lg' },
                    { k: 'transCost', l: 'Trans Cost', t: 'money', tot: true, hide: 'lg' },
                    { k: 'totalCost', l: 'Total Cost', t: 'money', tot: true },
                    { k: 'cnfgp', l: 'C&F GP', t: 'money', tot: true, hide: 'md' },
                    { k: 'transGP', l: 'Trans GP', t: 'money', tot: true, hide: 'md' },
                    { k: 'totalGP', l: 'Total GP', t: 'money', tot: true },
                    { k: 'gpPercent', l: 'GP %', t: 'pct' }
                ]
            });
        }
    },

    async load(filter) {
        this.initGrid();
        if (window.DashboardApp) window.DashboardApp.showLoader('p-gp');
        try {
            const resp = await fetch('/DashboardGPAnalysis/GetData', {
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
            console.error('Error loading GP Analysis data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-gp');
        }
    },

    render(data) {
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs
        setK('g_n', int(k.totalBills ?? k.TotalBills), 'bills analysed');
        setK('g_nbv', cur(k.netBillValue ?? k.NetBillValue), 'total revenue');
        setK('g_cost', cur(k.totalCosting ?? k.TotalCosting), 'operational expenses');
        setK('g_gp', cur(k.grossProfit ?? k.GrossProfit), 'net earnings');
        setK('g_pct', pct(k.gpMargin ?? k.GPMargin, 2), 'overall margin');

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

        setMini('gm_cnf_p', money(k.cnfProfit), 'commission + rec/non-rec');
        setMini('gm_tr_p', money(k.transportProfit), 'transport margin');
        setMini('gm_ae_p', money(k.airExportProfit), 'air export net');
        setMini('gm_ae_pct', pct(k.airExportGPPercent, 2), 'air export margin');
        setMini('gm_best', pct(k.bestModeGP, 2), k.bestMode || '-');
        setMini('gm_low', pct(k.lowestModeGP, 2), k.lowestMode || '-');
        setMini('gm_avg', money(k.avgProfitPerBill), 'mean profit');

        // Chart 1: Costing Composition
        const costs = data.costCompositions || [];
        mkChart('g_ch1', 'doughnut', costs.map(c => c.costHead), [
            pieDs(costs.map(c => c.costAmount))
        ], { money: true });

        // Chart 2: GP Margin by Mode
        const modeMargins = data.modeMargins || [];
        mkChart('g_ch2', 'bar', modeMargins.map(m => m.modeName), [
            barDs('GP %', modeMargins.map(m => m.gpPercent), cssVar('--blue') || '#0b57d0')
        ], { horizontal: true, legend: false });

        // Chart 3: Billing vs Costing (C&F vs Transport)
        const bvc = data.billingVsCostings || [];
        mkChart('g_ch3', 'bar', bvc.map(b => b.category), [
            barDs('Billing', bvc.map(b => b.billing), cssVar('--blue') || '#0b57d0'),
            barDs('Costing', bvc.map(b => b.costing), cssVar('--orange') || '#f08c00')
        ], { money: true });

        // Chart 4: Monthly Revenue, Cost & GP%
        const trends = data.monthlyTrends || [];
        mkChart('g_ch4', 'bar', trends.map(t => t.monthLabel), [
            barDs('Revenue', trends.map(t => t.revenue), cssVar('--blue') || '#0b57d0'),
            barDs('Cost', trends.map(t => t.cost), cssVar('--orange') || '#f08c00'),
            {
                type: 'line',
                label: 'GP %',
                data: trends.map(t => t.gpMargin),
                borderColor: cssVar('--green') || '#1b8a3c',
                backgroundColor: cssVar('--green') || '#1b8a3c',
                yAxisID: 'y2',
                tension: 0.3,
                pointRadius: 4
            }
        ], { money: true, y2: true });

        // Chart 5: Top Customers by Gross Profit
        const topCust = data.topCustomers || [];
        mkChart('g_ch5', 'bar', topCust.map(c => c.customerName), [
            barDs('Gross Profit', topCust.map(c => c.grossProfit), cssVar('--green') || '#1b8a3c')
        ], { horizontal: true, money: true, legend: false });

        // Grid
        if (this.grid) {
            this.grid.setRows(data.gpList || []);
        }
    }
};

