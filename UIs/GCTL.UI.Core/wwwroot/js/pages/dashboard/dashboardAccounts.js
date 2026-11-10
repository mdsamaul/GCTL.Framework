/**
 * Accounts Tab Module
 */
'use strict';

window.DashboardAccounts = {
    currentSub: 'cb',
    gridCB: null,
    gridTB: null,
    gridIS: null,
    gridBS: null,

    initSubnav() {
        const subnav = document.getElementById('subnav');
        if (subnav) {
            subnav.onclick = e => {
                const b = e.target.closest('button');
                if (!b) return;
                this.currentSub = b.dataset.s;
                d$$('#subnav button').forEach(x => x.classList.toggle('on', x.dataset.s === this.currentSub));
                d$$('#p-accounts .sub').forEach(s => s.classList.toggle('on', s.id === 's-' + this.currentSub));
            };
        }
    },

    initGrids() {
        if (!this.gridCB) {
            this.gridCB = new Grid('a_g', {
                card: 'g760',
                totals: true,
                columns: [
                    { t: 'sn', l: 'SL', w: '38px' },
                    { k: 'accType', l: 'Type', cls: 'nw' },
                    { k: 'branch', l: 'Branch', hide: 'md' },
                    { k: 'accCode', l: 'Account Code', cls: 'mono nw' },
                    { k: 'accName', l: 'Account Head' },
                    { k: 'opening', l: 'Opening', t: 'money', tot: true },
                    { k: 'receipts', l: 'Receipts (Dr)', t: 'money', tot: true },
                    { k: 'payments', l: 'Payments (Cr)', t: 'money', tot: true },
                    { k: 'closing', l: 'Closing', t: 'money', tot: true }
                ]
            });
        }
    },

    async load(filter) {
        this.initSubnav();
        this.initGrids();
        if (window.DashboardApp) window.DashboardApp.showLoader('p-accounts');
        try {
            const resp = await fetch('/DashboardAccounts/GetData', {
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
            console.error('Error loading Accounts data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-accounts');
        }
    },

    render(data) {
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs
        setK('a_cash', cur(k.cashInHand ?? k.CashInHand), 'all cash registers');
        setK('a_bank', cur(k.bankBalance ?? k.BankBalance), 'active bank accounts');
        setK('a_dr', cur(k.totalReceipts ?? k.TotalReceipts), 'cash/bank inflow');
        setK('a_cr', cur(k.totalPayments ?? k.TotalPayments), 'cash/bank outflow');
        setK('a_net', cur(k.netFlow ?? k.NetFlow), 'inflow - outflow');

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

        setMini('am_top', k.largestBank || '-', compact(k.largestBankAmt));
        setMini('am_cnt', String(k.activeAccounts || 0));

        // Chart 1: Cash & Bank Composition (Doughnut)
        const comps = data.accountCompositions || [];
        mkChart('a_ch1', 'doughnut', comps.map(c => c.accName), [
            pieDs(comps.map(c => c.closingBalance))
        ], { money: true });

        // Chart 2: Cash + Bank Trend (Monthly Net Flow)
        const monthly = data.monthlyCashFlows || [];
        mkChart('a_ch2', 'bar', monthly.map(m => m.monthLabel), [
            barDs('Net Flow', monthly.map(m => m.netFlow), cssVar('--blue') || '#0b57d0')
        ], { money: true });

        // Chart 3: Receipts vs Payments
        mkChart('a_ch3', 'bar', monthly.map(m => m.monthLabel), [
            barDs('Receipts', monthly.map(m => m.receipts), cssVar('--green') || '#1b8a3c'),
            barDs('Payments', monthly.map(m => m.payments), cssVar('--orange') || '#f08c00')
        ], { money: true });

        // Grid
        if (this.gridCB) {
            this.gridCB.setRows(data.accountsList || []);
        }
    }
};

