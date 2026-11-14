/**
 * Cash & Bank Summary Sub-Tab Module
 */
'use strict';

window.DashboardCashBank = {
    gridCB: null,
    isLoaded: false,

    initGrid() {
        if (!this.gridCB) {
            this.gridCB = new Grid('a_g', {
                card: 'g760',
                totals: true,
                sizes: [10, 20, 50, 100, 'All'],
                size: 20,
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
        this.initGrid();
        if (window.DashboardApp) window.DashboardApp.showLoader('s-cb');
        try {
            const resp = await fetch('/DashboardCashBank/GetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter || {})
            });
            const res = await resp.json();
            if (!res.isSuccess) {
                console.error('Failed to load Cash & Bank data:', res.message);
                return;
            }
            this.render(res.data, filter);
            this.isLoaded = true;
        } catch (err) {
            console.error('Error loading Cash & Bank Summary:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('s-cb');
        }
    },

    render(data, filter) {
        if (!data) return;
        const k = data.kpis || data.kpIs || data.KPIs || {};

        const toDateStr = filter?.toDate ? this.formatDate(filter.toDate) : this.formatDate(new Date());

        // 1. Six KPIs
        const cashBal = Number(k.cashInHand ?? k.CashInHand) || 0;
        const bankBal = Number(k.bankBalance ?? k.BankBalance) || 0;
        const tbTotal = Number(k.trialBalanceTotal ?? k.TrialBalanceTotal) || 0;
        const isTbBal = Boolean(k.isTrialBalanced ?? k.IsTrialBalanced);
        const tbDiff = Number(k.trialBalanceDiff ?? k.TrialBalanceDiff) || 0;
        const netProfit = Number(k.netProfit ?? k.NetProfit) || 0;
        const totRev = Number(k.totalRevenue ?? k.TotalRevenue) || 0;
        const totAssets = Number(k.totalAssets ?? k.TotalAssets) || 0;
        const totLiab = Number(k.totalLiabilities ?? k.TotalLiabilities) || 0;
        const currRatio = Number(k.currentRatio ?? k.CurrentRatio) || 0;

        setK('a_cash', cur(cashBal), 'as on ' + toDateStr);
        setK('a_bank', cur(bankBal), 'as on ' + toDateStr);

        const tbSubHtml = isTbBal 
            ? '<b class="up" style="color:var(--green)">✓ Balanced</b> (Debit = Credit)'
            : '<b class="dn" style="color:var(--red)">Difference ' + cur(tbDiff) + '</b>';
        setK('a_tb', cur(tbTotal), tbSubHtml);

        setK('a_np', cur(netProfit), `Revenue ${compact(totRev)}`);
        setK('a_ta', cur(totAssets), `Liabilities ${compact(totLiab)}`);
        setK('a_cr', (currRatio > 0 ? currRatio.toFixed(2) + ' : 1' : '–'), 'current assets / current liabilities');

        // 2. Five Mini Cards
        setK('am_ar', cur(k.tradeReceivable ?? k.TradeReceivable ?? 0));
        setK('am_ap', cur(k.tradePayable ?? k.TradePayable ?? 0));
        setK('am_wc', cur(k.workingCapital ?? k.WorkingCapital ?? 0));
        
        const qr = Number(k.quickRatio ?? k.QuickRatio) || 0;
        setK('am_qr', (qr > 0 ? qr.toFixed(2) + ' : 1' : '–'));

        const npm = Number(k.netProfitMargin ?? k.NetProfitMargin) || 0;
        setK('am_nm', npm.toFixed(1) + '%');

        // 3. Chart 1: Cash & Bank Composition (Doughnut)
        const totalCashBank = Math.max(0, cashBal) + Math.max(0, bankBal);
        mkChart('a_ch1', 'doughnut', ['Cash in Hand', 'Bank Balance'], [
            pieDs([Math.max(0, cashBal), Math.max(0, bankBal)], [
                cssVar('--sky') || '#0284c7',
                cssVar('--teal') || '#0d9488'
            ])
        ], {
            money: true,
            center: { text: compact(totalCashBank), sub: 'cash + bank' }
        });

        // 4. Chart 2: Cash + Bank Trend (Stacked Bar)
        const cbTrends = data.cashBankTrends || [];
        if (cbTrends.length > 0) {
            mkChart('a_ch2', 'bar', cbTrends.map(x => x.monthLabel), [
                barDs('Cash in Hand', cbTrends.map(x => x.cashAmount), cssVar('--sky') || '#0284c7'),
                barDs('Bank', cbTrends.map(x => x.bankAmount), cssVar('--teal') || '#0d9488')
            ], { money: true, stacked: true });
        } else {
            mkChart('a_ch2', 'bar', ['No Data'], [
                barDs('Cash in Hand', [0], cssVar('--sky') || '#0284c7'),
                barDs('Bank', [0], cssVar('--teal') || '#0d9488')
            ], { money: true, stacked: true });
        }

        // 5. Chart 3: Income vs Expense Monthly (Bar)
        const ieTrends = data.incomeExpenseTrends || [];
        if (ieTrends.length > 0) {
            mkChart('a_ch3', 'bar', ieTrends.map(x => x.monthLabel), [
                barDs('Income', ieTrends.map(x => x.revenue), cssVar('--green') || '#1b8a3c'),
                barDs('Expense', ieTrends.map(x => x.expense), cssVar('--red') || '#dc2626')
            ], { money: true });
        } else {
            mkChart('a_ch3', 'bar', ['No Data'], [
                barDs('Income', [0], cssVar('--green') || '#1b8a3c'),
                barDs('Expense', [0], cssVar('--red') || '#dc2626')
            ], { money: true });
        }

        // 6. Data Grid (Displays every single account from DB)
        if (this.gridCB) {
            const list = (data.accountsList || []).map((row, idx) => ({
                ...row,
                sn: idx + 1
            }));
            this.gridCB.setRows(list);
        }
    },

    formatDate(d) {
        if (!d) return '';
        const date = new Date(d);
        if (isNaN(date.getTime())) return String(d);
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        return `${day}/${month}/${year}`;
    }
};
