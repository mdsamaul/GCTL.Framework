/**
 * GP Analysis Tab Module
 */
'use strict';

window.DashboardGPAnalysis = {
    grid: null,

    initGrid() {
        if (!this.grid) {
            this.grid = new Grid('g_g', {
                card: 'g_gp_table',
                totals: true,
                customHeader: () => `
                    <thead>
                        <tr class="table-header-tier-1">
                            <th rowspan="3" style="width:38px; min-width:38px;">
                                <span class="user-select-none">Sel.</span><br/>
                                <input type="checkbox" class="form-check-input m-0 shadow-none" id="chkSelectAllGP" title="Select All" />
                            </th>
                            <th rowspan="3" style="width:75px;">Billing Date</th>
                            <th rowspan="3" style="width:130px;">Bill No</th>
                            <th rowspan="3" style="width:85px;">Total Bill<br/>(BDT)</th>
                            <th colspan="5">Billing</th>
                            <th colspan="7">Costing</th>
                            <th colspan="6">PNL</th>
                        </tr>
                        <tr class="table-header-tier-2">
                            <th colspan="3">C &amp; F</th>
                            <th rowspan="2" style="width:70px;">Transport</th>
                            <th rowspan="2" style="width:80px;">TOT Bill</th>
                            <th colspan="3">C &amp; F Cost</th>
                            <th colspan="3">Transport Cost</th>
                            <th rowspan="2" style="width:75px;">TOT Cost</th>
                            <th colspan="3">GP</th>
                            <th colspan="3">GP%</th>
                        </tr>
                        <tr class="table-header-tier-3">
                            <th style="width:70px;">Comm.</th>
                            <th style="width:75px;">Others</th>
                            <th style="width:75px;">TOT</th>
                            <th style="width:70px;">Rece</th>
                            <th style="width:70px;">NON Rece</th>
                            <th style="width:75px;">TOT Cost</th>
                            <th style="width:60px;">Swift</th>
                            <th style="width:65px;">Hair</th>
                            <th style="width:65px;">TOT</th>
                            <th style="width:75px;">C &amp; F</th>
                            <th style="width:70px;">TRANS</th>
                            <th style="width:75px;">TOT</th>
                            <th style="width:60px;">C &amp; F</th>
                            <th style="width:60px;">TRANS</th>
                            <th style="width:60px;">G. GP</th>
                        </tr>
                    </thead>`,
                columns: [
                    // 1: Sel checkbox
                    {
                        t: 'chk',
                        l: 'Sel.',
                        w: '38px',
                        cls: 'ctr',
                        r: r => `<input type="checkbox" class="form-check-input m-0 shadow-none gp-row-chk" value="${r.billNo ?? r.BillNo ?? ''}">`
                    },
                    // 2: Billing Date
                    { k: 'billingDate', l: 'Billing Date', cls: 'nw ctr', v: r => r.billingDate ?? r.BillingDate },
                    // 3: Bill No
                    { k: 'billNo', l: 'Bill No', cls: 'mono nw', v: r => r.billNo ?? r.BillNo },
                    // 4: Total Bill (BDT)
                    { k: 'totalBill', l: 'Total Bill (BDT)', t: 'amt', tot: true, v: r => r.totalBill ?? r.TotalBill },

                    // 5-9: Billing (Comm., Others, TOT, Transport, TOT Bill)
                    { k: 'commission', l: 'Comm.', t: 'amt', tot: true, v: r => r.commission ?? r.Commission },
                    { k: 'othersBill', l: 'Others', t: 'amt', tot: true, v: r => r.othersBill ?? r.OthersBill },
                    { k: 'totalCNFBilling', l: 'TOT', t: 'amt', tot: true, v: r => r.totalCNFBilling ?? r.TotalCNFBilling },
                    { k: 'transportBill', l: 'Transport', t: 'amt', tot: true, v: r => r.transportBill ?? r.TransportBill },
                    { k: 'totalBill', l: 'TOT Bill', t: 'amt', tot: true, v: r => r.totalBill ?? r.TotalBill },

                    // 10-16: Costing (Rece, NON Rece, TOT Cost, Swift, Hair, TOT, TOT Cost)
                    { k: 'costRec', l: 'Rece', t: 'amt', tot: true, v: r => r.costRec ?? r.CostRec },
                    { k: 'costNonRec', l: 'NON Rece', t: 'amt', tot: true, v: r => r.costNonRec ?? r.CostNonRec },
                    { k: 'cnfCost', l: 'TOT Cost', t: 'amt', tot: true, v: r => r.cnfCost ?? r.CNFCost },
                    { k: 'swiftCost', l: 'Swift', t: 'amt', tot: true, v: r => r.swiftCost ?? r.SwiftCost },
                    { k: 'hairCost', l: 'Hair', t: 'amt', tot: true, v: r => r.hairCost ?? r.HairCost },
                    { k: 'transCost', l: 'TOT', t: 'amt', tot: true, v: r => r.transCost ?? r.TransCost },
                    { k: 'totalCost', l: 'TOT Cost', t: 'amt', tot: true, v: r => r.totalCost ?? r.TotalCost },

                    // 17-22: PNL (C & F, TRANS, TOT, C & F %, TRANS %, G. GP %)
                    { k: 'cnfgp', l: 'C & F', t: 'amt', tot: true, v: r => r.cnfgp ?? r.CNFGP },
                    { k: 'transGP', l: 'TRANS', t: 'amt', tot: true, v: r => r.transGP ?? r.TransGP },
                    { k: 'totalGP', l: 'TOT', t: 'amt', tot: true, v: r => r.totalGP ?? r.TotalGP },
                    { 
                        k: 'cnfgpPercent', 
                        l: 'C & F', 
                        t: 'pct', 
                        v: r => r.cnfgpPercent != null && r.cnfgpPercent !== '' ? (Number(r.cnfgpPercent) || 0).toFixed(2) + '%' : '0.00%',
                        totf: rows => {
                            const totBilling = (rows || []).reduce((s, x) => s + (Number(x.totalCNFBilling ?? x.TotalCNFBilling) || 0), 0);
                            const totCNFGP = (rows || []).reduce((s, x) => s + (Number(x.cnfgp ?? x.CNFGP) || 0), 0);
                            return totBilling > 0 ? (totCNFGP / totBilling * 100).toFixed(2) + '%' : '0.00%';
                        }
                    },
                    { 
                        k: 'transportGPPercent', 
                        l: 'TRANS', 
                        t: 'pct', 
                        v: r => r.transportGPPercent != null && r.transportGPPercent !== '' ? (Number(r.transportGPPercent) || 0).toFixed(2) + '%' : '0.00%',
                        totf: rows => {
                            const totTransBill = (rows || []).reduce((s, x) => s + (Number(x.transportBill ?? x.TransportBill) || 0), 0);
                            const totTransGP = (rows || []).reduce((s, x) => s + (Number(x.transGP ?? x.TransGP) || 0), 0);
                            return totTransBill > 0 ? (totTransGP / totTransBill * 100).toFixed(2) + '%' : '0.00%';
                        }
                    },
                    { 
                        k: 'gpPercent', 
                        l: 'G. GP', 
                        t: 'pct', 
                        v: r => r.gpPercent != null && r.gpPercent !== '' ? (Number(r.gpPercent) || 0).toFixed(2) + '%' : '0.00%',
                        totf: rows => {
                            const totBilling = (rows || []).reduce((s, x) => s + (Number(x.totalBill ?? x.TotalBill) || 0), 0);
                            const totGP = (rows || []).reduce((s, x) => s + (Number(x.totalGP ?? x.TotalGP) || 0), 0);
                            return totBilling > 0 ? (totGP / totBilling * 100).toFixed(2) + '%' : '0.00%';
                        }
                    }
                ]
            });

            // Bind Select All checkbox
            document.addEventListener('change', e => {
                if (e.target && e.target.id === 'chkSelectAllGP') {
                    const checked = e.target.checked;
                    document.querySelectorAll('#g_g .gp-row-chk').forEach(chk => {
                        chk.checked = checked;
                    });
                }
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

        // Full currency formatter for GP Analysis (e.g., ৳ 1,55,37,167.00)
        const fullCur = v => {
            const num = Number(v) || 0;
            return '৳ ' + num.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        // Compact with Lac / Cr for donut center (e.g., 79.07 Lac)
        const lacCrCompact = v => {
            const num = Number(v) || 0;
            const a = Math.abs(num);
            if (a >= 1e7) return (num / 1e7).toFixed(2) + ' Cr';
            if (a >= 1e5) return (num / 1e5).toFixed(2) + ' Lac';
            if (a >= 1e3) return (num / 1e3).toFixed(1) + ' k';
            return num.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        // KPIs
        setK('g_n', int(k.totalBills ?? k.TotalBills), 'bills in period');
        setK('g_nbv', fullCur(k.netBillValue ?? k.NetBillValue), '');
        setK('g_cost', fullCur(k.totalCosting ?? k.TotalCosting), pct(k.netBillValue || k.NetBillValue ? ((k.totalCosting ?? k.TotalCosting) / (k.netBillValue ?? k.NetBillValue) * 100) : 0, 1) + ' of bill value');
        setK('g_gp', fullCur(k.grossProfit ?? k.GrossProfit), '');
        setK('g_pct', pct(k.gpMargin ?? k.GPMargin, 2), 'gross margin');

        // Minis
        const setMini = (id, val, sub = null) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.kval');
                if (vEl) vEl.textContent = val;
                if (sub !== null) {
                    const sEl = el.querySelector('.ksub');
                    if (sEl) sEl.innerHTML = sub;
                }
            }
        };

        setMini('gm_cnf', fullCur(k.cnfProfit ?? k.CNFProfit), 'commission + rec/non-rec');
        setMini('gm_tr', fullCur(k.transportProfit ?? k.TransportProfit), 'transport margin');
        setMini('gm_best', pct(k.bestModeGP ?? k.BestModeGP, 1), k.bestMode || k.BestMode || '-');
        setMini('gm_low', pct(k.lowestModeGP ?? k.LowestModeGP, 1), k.lowestMode || k.LowestMode || '-');
        setMini('gm_avg', fullCur(k.avgProfitPerBill ?? k.AvgProfitPerBill), 'mean profit');

        // Chart 1: Costing Composition
        const costs = data.costCompositions || data.CostCompositions || [];
        mkChart('g_ch1', 'doughnut', costs.map(c => c.costHead || c.CostHead), [
            pieDs(costs.map(c => c.costAmount ?? c.CostAmount))
        ], { money: true, center: { text: lacCrCompact(k.totalCosting ?? k.TotalCosting), sub: 'total cost' } });

        // Chart 2: GP Margin by Mode
        const modeMargins = data.modeMargins || data.ModeMargins || [];
        const modeGPValues = modeMargins.map(m => m.gpPercent ?? m.GPPercent);
        mkChart('g_ch2', 'bar', modeMargins.map(m => m.modeName || m.ModeName), [
            barDs('GP %', modeGPValues, modeGPValues.map(v => v >= 50 ? (cssVar('--green') || '#1b8a3c') : (cssVar('--orange') || '#f08c00')))
        ], { legend: false, afmt: v => v + '%', tfmt: v => pct(v, 2) });

        // Chart 3: Billing vs Costing (C&F vs Transport)
        const bvc = data.billingVsCostings || data.BillingVsCostings || [];
        const cnfItem = bvc.find(x => (x.category || x.Category) === 'C&F') || {};
        const trItem = bvc.find(x => (x.category || x.Category) === 'Transport') || {};
        const cnfProfit = (cnfItem.billing ?? cnfItem.Billing ?? 0) - (cnfItem.costing ?? cnfItem.Costing ?? 0);
        const trProfit = (trItem.billing ?? trItem.Billing ?? 0) - (trItem.costing ?? trItem.Costing ?? 0);

        mkChart('g_ch3', 'bar', ['C&F', 'Transport'], [
            barDs('Billing', [cnfItem.billing ?? cnfItem.Billing ?? 0, trItem.billing ?? trItem.Billing ?? 0], cssVar('--blue') || '#0b57d0'),
            barDs('Costing', [cnfItem.costing ?? cnfItem.Costing ?? 0, trItem.costing ?? trItem.Costing ?? 0], cssVar('--orange') || '#f08c00'),
            barDs('Profit', [cnfProfit, trProfit], cssVar('--green') || '#1b8a3c')
        ], { money: true });

        // Chart 4: Monthly Revenue, Cost & GP%
        const trends = data.monthlyTrends || data.MonthlyTrends || [];
        mkChart('g_ch4', 'bar', trends.map(t => t.monthLabel || t.MonthLabel), [
            barDs('Revenue', trends.map(t => t.revenue ?? t.Revenue), cssVar('--blue') || '#0b57d0'),
            barDs('Cost', trends.map(t => t.cost ?? t.Cost), cssVar('--orange') || '#f08c00'),
            {
                type: 'line',
                label: 'GP %',
                data: trends.map(t => t.gpMargin ?? t.GPMargin),
                borderColor: cssVar('--green') || '#1b8a3c',
                backgroundColor: cssVar('--green') || '#1b8a3c',
                yAxisID: 'y2',
                tension: 0.3,
                pointRadius: 4
            }
        ], { money: true, y2: true });

        // Chart 5: Top Customers by Gross Profit
        // In Chart.js horizontal bar (indexAxis: 'y'), array is ordered by GrossProfit DESC.
        const topCust = (data.topCustomers || data.TopCustomers || []).slice(0, 7);
        mkChart('g_ch5', 'bar', topCust.map(c => c.customerName || c.CustomerName || '-'), [
            barDs('Gross Profit', topCust.map(c => c.grossProfit ?? c.GrossProfit), cssVar('--green') || '#1b8a3c')
        ], { horizontal: true, money: true, legend: false });

        // Grid
        if (this.grid) {
            this.grid.setRows(data.gpList || data.GPList || []);
        }
    }
};

