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
    tbRawData: null,
    lastFilter: null,

    initSubnav() {
        const subnav = document.getElementById('subnav');
        if (subnav && !this._subnavBound) {
            this._subnavBound = true;
            subnav.onclick = e => {
                const b = e.target.closest('button');
                if (!b) return;
                this.currentSub = b.dataset.s;
                d$$('#subnav button').forEach(x => x.classList.toggle('on', x.dataset.s === this.currentSub));
                d$$('#p-accounts .sub').forEach(s => s.classList.toggle('on', s.id === 's-' + this.currentSub));
                if (this.currentSub === 'tb' && !this.tbRawData) {
                    this.loadTB(this.lastFilter);
                }
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

        if (!this.gridTB) {
            const lv = r => `<span style="padding-left:${((r.level || r.Level || 1) - 1) * 14}px">${r.code || r.Code ? esc(r.code || r.Code) + ' – ' : ''}${esc(r.particularName || r.ParticularName || r.name || '')}</span>`;
            this.gridTB = new Grid('t_g', {
                card: 'g760',
                sortable: false,
                size: 15,
                sizes: [15, 25, 50, 100],
                rowCls: r => 'l' + (r.level || r.Level || 1),
                groups: [
                    ['', 1],
                    ['Opening Balance', 2],
                    ['Transactions', 2],
                    ['Closing Balance', 2]
                ],
                columns: [
                    { k: 'particularName', l: 'Particulars', r: lv, x: r => ((r.code || r.Code) ? (r.code || r.Code) + ' – ' : '') + (r.particularName || r.ParticularName || '') },
                    { k: 'opDr', l: 'Debit', t: 'money' },
                    { k: 'opCr', l: 'Credit', t: 'money' },
                    { k: 'trDr', l: 'Debit', t: 'money' },
                    { k: 'trCr', l: 'Credit', t: 'money' },
                    { k: 'clDr', l: 'Debit', t: 'money' },
                    { k: 'clCr', l: 'Credit', t: 'money' }
                ]
            });

            const levelEl = document.getElementById('tbLevel');
            if (levelEl) {
                levelEl.onchange = () => this.filterAndRenderTBRows();
            }
            const zeroEl = document.getElementById('tbZero');
            if (zeroEl) {
                zeroEl.onchange = () => this.filterAndRenderTBRows();
            }
        }
    },

    async load(filter) {
        this.lastFilter = filter;
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

            // Also load TB data
            await this.loadTB(filter);
        } catch (err) {
            console.error('Error loading Accounts data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-accounts');
        }
    },

    async loadTB(filter) {
        try {
            const resp = await fetch('/DashboardAccounts/GetTrialBalanceData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter || {})
            });
            const res = await resp.json();
            if (!res.isSuccess) {
                console.error(res.message);
                return;
            }
            this.tbRawData = res.data;
            this.renderTB(res.data);
        } catch (err) {
            console.error('Error loading Trial Balance data:', err);
        }
    },

    renderTB(data) {
        if (!data) return;
        const k = data.kpis || data.kpIs || data.KPIs || {};

        const clDr = k.closingDebit ?? k.ClosingDebit ?? 0;
        const clCr = k.closingCredit ?? k.ClosingCredit ?? 0;
        const trDr = k.periodDebit ?? k.PeriodDebit ?? 0;
        const trCr = k.periodCredit ?? k.PeriodCredit ?? 0;
        const isBalanced = k.isBalanced ?? k.IsBalanced ?? (Math.abs(clDr - clCr) < 1);
        const diff = k.difference ?? k.Difference ?? (clDr - clCr);

        setK('t_dr', cur(clDr), 'closing');
        setK('t_cr', cur(clCr), 'closing');
        setK('t_pd', cur(trDr), 'Cr ' + money(trCr));
        setK('t_st', isBalanced ? '✓ Balanced' : 'Not balanced', 'Debit − Credit = ' + money(diff));

        // Group Chart (t_ch1 - Bar)
        const groups = data.groupChart || data.GroupChart || [];
        if (groups.length > 0) {
            const labels = groups.map(g => (g.code || g.Code) + ' – ' + (g.groupName || g.GroupName || '').replace(/,.*$/, ''));
            const drs = groups.map(g => g.closingDebit ?? g.ClosingDebit ?? 0);
            const crs = groups.map(g => g.closingCredit ?? g.ClosingCredit ?? 0);
            mkChart('t_ch1', 'bar', labels, [
                barDs('Debit', drs, cssVar('--blue') || '#0b57d0'),
                barDs('Credit', crs, cssVar('--green') || '#1b8a3c')
            ], { money: true });
        }

        // Control Doughnut Chart (t_ch2 - Doughnut)
        const controls = data.controlChart || data.ControlChart || [];
        if (controls.length > 0) {
            const cLabels = controls.map(c => (c.code || c.Code) + ' ' + (c.controlName || c.ControlName || ''));
            const cDrs = controls.map(c => c.closingDebit ?? c.ClosingDebit ?? 0);
            const totalDrs = cDrs.reduce((a, b) => a + b, 0);
            mkChart('t_ch2', 'doughnut', cLabels, [
                pieDs(cDrs)
            ], {
                money: true,
                center: { text: compact(totalDrs), sub: 'debit' }
            });
        }

        this.filterAndRenderTBRows();
    },

    filterAndRenderTBRows() {
        if (!this.tbRawData) return;
        const allRows = this.tbRawData.rows || this.tbRawData.Rows || [];
        const lvlEl = document.getElementById('tbLevel');
        const maxLevel = lvlEl ? +lvlEl.value : 4;
        const zeroEl = document.getElementById('tbZero');
        const hideZero = zeroEl ? zeroEl.checked : true;

        const filtered = allRows.filter(r => {
            const lvl = r.level ?? r.Level ?? 1;
            if (lvl > maxLevel) return false;

            if (hideZero) {
                const opDr = r.opDr ?? r.OpDr ?? 0;
                const opCr = r.opCr ?? r.OpCr ?? 0;
                const trDr = r.trDr ?? r.TrDr ?? 0;
                const trCr = r.trCr ?? r.TrCr ?? 0;
                const clDr = r.clDr ?? r.ClDr ?? 0;
                const clCr = r.clCr ?? r.ClCr ?? 0;
                if (!opDr && !opCr && !trDr && !trCr && !clDr && !clCr) return false;
            }
            return true;
        });

        if (this.gridTB) {
            this.gridTB.setRows(filtered);
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

        setMini('am_top', k.largestBank || '-', cur(k.largestBankAmt));
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
