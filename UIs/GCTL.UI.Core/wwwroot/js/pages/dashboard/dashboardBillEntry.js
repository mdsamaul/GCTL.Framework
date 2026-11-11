/**
 * Bill Entry Tab Module
 */
'use strict';

window.DashboardBillEntry = {
    grid: null,

    initGrid() {
        if (!this.grid) {
            this.grid = new Grid('b_g', {
                card: 'g760',
                totals: true,
                columns: [
                    { t: 'sn', l: 'SL', w: '40px' },
                    { k: 'billNo', l: 'Bill No', cls: 'mono nw' },
                    { k: 'billDate', l: 'Bill Date', cls: 'nw' },
                    { k: 'jobNo', l: 'Job No', cls: 'mono nw', hide: 'md' },
                    { k: 'customerName', l: 'Customer' },
                    { k: 'expenseType', l: 'Mode', hide: 'md' },
                    { k: 'agencyCommission', l: 'Commission', t: 'money', tot: true },
                    { k: 'totalBillAmount', l: 'Total Bill', t: 'money', tot: true },
                    { k: 'receivedAmount', l: 'Received', t: 'money', tot: true },
                    { k: 'dueAmount', l: 'Due Amount', t: 'money', tot: true },
                    { k: 'billStatus', l: 'Status', t: 'badge' }
                ]
            });
        }
    },

    async load(filter) {
        this.initGrid();
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
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs
        setK('b_n', int(k.totalBills ?? k.TotalBills), 'bills issued');
        setK('b_nbv', cur(k.netBillValue ?? k.NetBillValue), 'total billed');
        setK('b_col', cur(k.totalCollected ?? k.TotalCollected), 'realized funds');
        setK('b_due', cur(k.totalOutstandingDue ?? k.TotalOutstandingDue), 'uncollected balance');
        setK('b_comm', cur(k.agencyCommission ?? k.AgencyCommission), 'fee component');

        // Minis
        const setMini = (id, val) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
            }
        };

        setMini('bm_eff', pct(k.collectionEfficiency, 1));
        setMini('bm_avg', money(k.avgBillValue));

        const topCust = data.topCustomers || [];
        setMini('bm_top', topCust.length ? topCust[0].customerName : '-');

        const comps = data.serviceValueCompositions || [];
        const trComp = comps.find(c => c.serviceHead.toLowerCase().includes('transport'));
        setMini('bm_tr', cur(trComp ? trComp.amount : 0));

        const otherSum = comps.filter(c => !c.serviceHead.toLowerCase().includes('transport')).reduce((s, c) => s + c.amount, 0);
        setMini('bm_cnf', cur(otherSum));

        // Chart 1: Service Value Composition
        mkChart('b_ch1', 'doughnut', comps.map(c => c.serviceHead), [
            pieDs(comps.map(c => c.amount))
        ], { money: true });

        // Chart 2: Mode-wise Billing
        const modes = data.modeBillings || [];
        mkChart('b_ch2', 'bar', modes.map(m => m.modeName), [
            barDs('Billing', modes.map(m => m.totalBilling), cssVar('--blue') || '#0b57d0')
        ], { horizontal: true, money: true, legend: false });

        // Chart 3: Collection Status
        const colls = data.collectionStatuses || [];
        mkChart('b_ch3', 'doughnut', colls.map(c => c.statusName), [
            pieDs(colls.map(c => c.amount), [
                cssVar('--green') || '#1b8a3c',
                cssVar('--orange') || '#f08c00'
            ])
        ], { money: true });

        // Chart 4: Monthly Billing & Collections
        const trends = data.monthlyTrends || [];
        mkChart('b_ch4', 'bar', trends.map(t => t.monthLabel), [
            barDs('Billed', trends.map(t => t.billedAmount), cssVar('--blue') || '#0b57d0'),
            barDs('Collected', trends.map(t => t.collectedAmount), cssVar('--green') || '#1b8a3c')
        ], { money: true });

        // Chart 5: Top Customers
        mkChart('b_ch5', 'bar', topCust.map(c => c.customerName), [
            barDs('Billed', topCust.map(c => c.totalBilled), cssVar('--purple') || '#8e24aa')
        ], { horizontal: true, money: true, legend: false });

        // Grid
        if (this.grid) {
            this.grid.setRows(data.billList || []);
        }
    }
};

