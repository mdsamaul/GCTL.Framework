/**
 * Accounts Tab Module
 */
'use strict';

window.DashboardAccounts = {
    currentSub: 'cb',
    lastFilter: null,
    lastData: null,
    gridBS: null,
    _subnavBound: false,

    initSubnav() {
        const subnav = document.getElementById('subnav');
        if (subnav && !this._subnavBound) {
            this._subnavBound = true;
            subnav.onclick = e => {
                const b = e.target.closest('button');
                if (!b) return;
                const sub = b.dataset.s;
                this.switchSub(sub, this.lastFilter);
            };
        }
    },

    switchSub(subId, filter = null) {
        if (!subId) return;
        const normalized = (subId === 'summary') ? 'cb' : subId;
        this.currentSub = normalized;

        sessionStorage.setItem('dashboard_accounts_subtab', normalized);
        if (window.history && window.history.replaceState) {
            window.history.replaceState(null, '', '#accounts/' + normalized);
        }

        d$$('#subnav button').forEach(x => {
            const s = x.dataset.s;
            x.classList.toggle('on', s === normalized || (s === 'cb' && normalized === 'summary'));
        });

        d$$('#p-accounts .sub').forEach(s => {
            s.classList.toggle('on', s.id === 's-' + normalized || (s.id === 's-cb' && normalized === 'summary'));
        });

        this.loadCurrentSub(filter || this.lastFilter);
    },

    loadCurrentSub(filter = null) {
        const activeFilter = filter || this.lastFilter || (window.DashboardApp ? window.DashboardApp.getFilterState() : {});
        this.lastFilter = activeFilter;

        if (this.currentSub === 'cb' || this.currentSub === 'summary') {
            if (window.DashboardCashBank) {
                window.DashboardCashBank.load(activeFilter);
            }
        } else if (this.currentSub === 'tb') {
            if (window.DashboardTrialBalance && typeof window.DashboardTrialBalance.load === 'function') {
                window.DashboardTrialBalance.load(activeFilter);
            }
        } else if (this.currentSub === 'is') {
            if (window.DashboardIncomeStatement && typeof window.DashboardIncomeStatement.load === 'function') {
                window.DashboardIncomeStatement.load(activeFilter);
            }
        } else if (this.currentSub === 'bs') {
            if (window.DashboardBalanceSheet && typeof window.DashboardBalanceSheet.load === 'function') {
                window.DashboardBalanceSheet.load(activeFilter);
            } else {
                this.loadBalanceSheet(activeFilter);
            }
        }
    },

    initGrids() {
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

        // Restore active sub-tab from URL hash or sessionStorage
        let targetSub = 'cb';
        const hash = (window.location.hash || '').replace('#', '').trim();
        if (hash.startsWith('accounts/')) {
            targetSub = hash.substring('accounts/'.length);
        } else if (hash.startsWith('accounts-')) {
            targetSub = hash.substring('accounts-'.length);
        } else {
            const stored = sessionStorage.getItem('dashboard_accounts_subtab');
            if (stored) targetSub = stored;
        }

        this.switchSub(targetSub, filter);
    },

    async loadBalanceSheet(filter) {
        this.initGrids();
        if (window.DashboardApp) window.DashboardApp.showLoader('s-bs');
        try {
            const resp = await fetch('/DashboardAccounts/GetBalanceSheetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter || {})
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
            if (window.DashboardApp) window.DashboardApp.hideLoader('s-bs');
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
