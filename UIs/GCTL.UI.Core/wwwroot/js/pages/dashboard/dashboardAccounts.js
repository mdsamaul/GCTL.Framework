/**
 * Accounts Tab Module
 */
'use strict';

window.DashboardAccounts = {
    currentSub: 'cb',
    lastFilter: null,
    lastData: null,
    gridCB: null,
    gridTB: null,
    gridIS: null,
    gridBS: null,

    initSubnav() {
        const subnav = document.getElementById('subnav');
        if (subnav) {
            subnav.onclick = async e => {
                const b = e.target.closest('button');
                if (!b) return;
                this.currentSub = b.dataset.s;
                d$$('#subnav button').forEach(x => x.classList.toggle('on', x.dataset.s === this.currentSub));
                d$$('#p-accounts .sub').forEach(s => s.classList.toggle('on', s.id === 's-' + this.currentSub));

                if (this.currentSub === 'bs') {
                    const bs = this.lastData?.balanceSheet || this.lastData?.BalanceSheet;
                    if (bs && (bs.kpIs || bs.kpis || bs.KPIs)) {
                        this.renderBS(bs);
                    } else if (this.lastFilter) {
                        await this.loadBalanceSheet(this.lastFilter);
                    }
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

        if (!this.gridBS) {
            this.gridBS = new Grid('s_g', {
                card: 'g760',
                sortable: false,
                size: 50,
                sizes: [15, 25, 50, 'All'],
                rowCls: r => r.kind || r.Kind,
                columns: [
                    {
                        k: 'label',
                        l: 'Particulars',
                        r: r => {
                            const kind = r.kind || r.Kind;
                            const label = r.label || r.Label;
                            return `<span style="padding-left:${kind === 'item' ? 18 : 0}px;${kind === 'head' ? 'font-weight:700;' : (kind === 'total' ? 'font-weight:800;' : '')}">${esc(label)}</span>`;
                        },
                        x: r => r.label || r.Label
                    },
                    {
                        k: 'amt',
                        l: 'Amount (BDT)',
                        t: 'money',
                        r: r => {
                            const val = r.amt ?? r.Amt;
                            return val == null ? '' : (val === 0 ? '–' : (val < 0 ? `(${amt(Math.abs(val))})` : amt(val)));
                        },
                        x: r => {
                            const val = r.amt ?? r.Amt;
                            return val == null ? '' : amt(val);
                        }
                    },
                    {
                        k: 'pct',
                        l: '% of Assets',
                        t: 'pct',
                        r: r => {
                            const val = r.pct ?? r.Pct;
                            return val == null ? '' : pct(val, 2);
                        },
                        x: r => {
                            const val = r.pct ?? r.Pct;
                            return val == null ? '' : pct(val, 2);
                        }
                    }
                ]
            });
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
            this.lastData = res.data;
            this.render(res.data);
            const bsData = res.data?.balanceSheet || res.data?.BalanceSheet;
            if (bsData) {
                this.renderBS(bsData);
            }
        } catch (err) {
            console.error('Error loading Accounts data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-accounts');
        }
    },

    async loadBalanceSheet(filter) {
        if (window.DashboardApp) window.DashboardApp.showLoader('p-accounts');
        try {
            const resp = await fetch('/DashboardAccounts/GetBalanceSheetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter)
            });
            const res = await resp.json();
            if (res.isSuccess && res.data) {
                const bsData = res.data.balanceSheet || res.data.BalanceSheet || res.data;
                if (this.lastData) this.lastData.balanceSheet = bsData;
                this.renderBS(bsData);
            }
        } catch (err) {
            console.error('Error loading Balance Sheet data:', err);
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
    },

    renderBS(bs) {
        if (!bs) return;
        const k = bs.kpIs || bs.kpis || bs.KPIs || {};
        const T = {
            assets: k.totalAssets ?? k.TotalAssets ?? 0,
            nca: k.nonCurrentAssets ?? k.NonCurrentAssets ?? 0,
            ca: k.currentAssets ?? k.CurrentAssets ?? 0,
            liab: k.totalLiabilities ?? k.TotalLiabilities ?? 0,
            ncl: k.nonCurrentLiabilities ?? k.NonCurrentLiabilities ?? 0,
            cl: k.currentLiabilities ?? k.CurrentLiabilities ?? 0,
            eq: k.totalEquity ?? k.TotalEquity ?? 0,
            profit: k.profitOrLoss ?? k.ProfitOrLoss ?? 0,
            wc: k.workingCapital ?? k.WorkingCapital ?? 0,
            cr: k.currentRatio ?? k.CurrentRatio ?? '–'
        };

        // 1. KPIs
        setK('s_ta', cur(T.assets), `Non-current ${compact(T.nca)} · Current ${compact(T.ca)}`);
        setK('s_tl', cur(T.liab), `Current ${compact(T.cl)}`);
        setK('s_te', cur(T.eq), `incl. profit ${compact(T.profit)}`);
        setK('s_wc', cur(T.wc), 'current assets − current liabilities');
        setK('s_cr', T.cr, 'liquidity health');

        // 2. Chart 1: Assets Composition (Doughnut)
        const assetsParts = bs.assetsComposition || bs.AssetsComposition || [];
        if (assetsParts.length) {
            mkChart('s_ch1', 'doughnut', assetsParts.map(x => x.label || x.Label), [
                pieDs(assetsParts.map(x => Math.max(0, x.amount ?? x.Amount)))
            ], { money: true, center: { text: compact(T.assets), sub: 'assets' } });
        } else {
            mkChart('s_ch1', 'doughnut', ['Non-current Assets', 'Current Assets'], [
                pieDs([T.nca, T.ca], [cssVar('--blue') || '#0b57d0', cssVar('--green') || '#1b8a3c'])
            ], { money: true, center: { text: compact(T.assets), sub: 'assets' } });
        }

        // 3. Chart 2: Equity & Liabilities Composition (Doughnut)
        const leParts = bs.equityLiabilitiesComposition || bs.EquityLiabilitiesComposition || [];
        if (leParts.length) {
            mkChart('s_ch2', 'doughnut', leParts.map(x => x.label || x.Label), [
                pieDs(leParts.map(x => Math.max(0, x.amount ?? x.Amount)))
            ], { money: true, center: { text: compact(T.assets), sub: 'equity + liab' } });
        } else {
            mkChart('s_ch2', 'doughnut', ['Total Equity', 'Current Liabilities'], [
                pieDs([T.eq, T.cl], [cssVar('--green') || '#1b8a3c', cssVar('--orange') || '#f08c00'])
            ], { money: true, center: { text: compact(T.assets), sub: 'equity + liab' } });
        }

        // 4. Chart 3: Assets vs Equity & Liabilities (Stacked Bar)
        mkChart('s_ch3', 'bar', ['Assets', 'Equity & Liabilities'], [
            barDs('Non-current / Equity', [T.nca, T.eq], cssVar('--blue') || '#0b57d0'),
            barDs('Current / Non-current liab.', [T.ca, T.ncl], cssVar('--green') || '#1b8a3c'),
            barDs('Current liabilities', [0, T.cl], cssVar('--orange') || '#f08c00')
        ], { money: true, stacked: true });

        // 5. Grid
        if (this.gridBS) {
            const rows = bs.rows || bs.Rows || [];
            this.gridBS.setRows(rows);
        }
    }
};

