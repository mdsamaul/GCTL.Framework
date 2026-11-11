/**
 * Overview Tab Module
 */
'use strict';

window.DashboardOverview = {
    async load(filter) {
        if (window.DashboardApp) window.DashboardApp.showLoader('p-overview');
        try {
            const resp = await fetch('/DashboardOverview/GetData', {
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
            console.error('Error loading Overview data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-overview');
        }
    },

    render(data) {
        const k = data.kpis || data.kpIs || data.KPIs || {};
        const fTo = d$('#fTo')?.value || 'Active Period';

        // 1. Main KPIs (6 top cards)
        setK('o_jobs', int(k.totalJobs ?? 0), `Pending <b>${int(k.pendingJobs ?? 0)}</b> · In Transit <b>${int(k.transitJobs ?? 0)}</b> · Delivered <b>${int(k.deliveredJobs ?? 0)}</b>`);
        setK('o_nsv', cur(k.netServiceValue ?? 0), `${int(k.totalBills ?? 0)} bills`);
        setK('o_gp', cur(k.grossProfit ?? 0), `GP margin <b>${pct(k.gpMargin ?? 0, 2)}</b>`);
        setK('o_due', cur(k.totalDueOutstanding ?? 0), `${k.netServiceValue ? pct((k.totalDueOutstanding / k.netServiceValue) * 100, 1) : '0%'} of billed value`);
        setK('o_cash', cur(k.cashInHand ?? 0), 'as on ' + fTo);
        setK('o_bank', cur(k.bankBalance ?? 0), 'as on ' + fTo);

        // 2. Mini KPI Cards (6 cards in 2nd row)
        const netProfit = Number(k.netProfitAccounts ?? 0);
        const netService = Number(k.netServiceValue ?? 0);
        const marginPct = netService > 0 ? (netProfit / netService * 100) : 0;
        setK('m_np', cur(netProfit), `Margin ${pct(marginPct, 1)}`);

        const colRate = Number(k.collectionRate ?? 0);
        setK('m_col', pct(colRate, 1), 'collected of billed');

        const avgClear = Number(k.avgClearanceDays ?? 0);
        setK('m_avg', avgClear.toFixed(1) + ' days', 'job date → delivery');

        const onTime = Number(k.onTimeDeliveryPct ?? 0);
        setK('m_ot', pct(onTime, 1), 'delivered ≤ 10 days');

        setK('m_doc', int(k.docsPendingCount ?? 0), 'jobs with incomplete documents');

        const reqCount = int(k.pendingReqCount ?? 0);
        const reqTotal = int(k.totalReqCount ?? 0);
        setK('m_req', cur(k.pendingReqAmount ?? 0), `${reqCount} of ${reqTotal} requests`);

        // 3. Chart 1: Revenue vs Direct Cost vs Gross Profit (Monthly Bar Chart)
        const trends = data.monthlyTrends || [];
        const mLabels = trends.map(t => t.monthLabel);
        const revData = trends.map(t => Number(t.revenue || 0));
        const costData = trends.map(t => Number(t.directCost ?? t.cost ?? t.DirectCost ?? 0));
        const prfData = trends.map(t => Number(t.profit || 0));

        mkChart('o_ch1', 'bar', mLabels, [
            barDs('Net Service Value', revData, cssVar('--blue') || '#0b57d0'),
            barDs('Direct Cost', costData, cssVar('--orange') || '#f08c00'),
            barDs('Gross Profit', prfData, cssVar('--green') || '#1b8a3c')
        ], { money: true });

        // 4. Chart 2: Job Status (Doughnut with Center Text)
        const statuses = data.statusShares || [];
        const statusLabels = statuses.map(s => s.statusName);
        const statusCounts = statuses.map(s => Number(s.jobCount || 0));
        const totalJobsCount = statusCounts.reduce((a, b) => a + b, 0) || Number(k.totalJobs || 0);

        mkChart('o_ch2', 'doughnut', statusLabels.length ? statusLabels : ['Pending', 'In Transit', 'Delivered'], [
            pieDs(statusCounts.length ? statusCounts : [k.pendingJobs || 0, k.transitJobs || 0, k.deliveredJobs || 0], [
                cssVar('--orange') || '#f08c00',
                cssVar('--purple') || '#8e24aa',
                cssVar('--green') || '#1b8a3c'
            ])
        ], {
            center: { text: int(totalJobsCount), sub: 'jobs' }
        });

        // 5. Chart 3: Top Customers by Service Value (Horizontal Bar)
        const topCust = data.topCustomers || [];
        mkChart('o_ch3', 'bar', topCust.map(c => c.customerName), [
            barDs('Service Value', topCust.map(c => Number(c.totalBillAmount || 0)), cssVar('--blue') || '#0b57d0')
        ], { horizontal: true, money: true, legend: false });

        // 6. Chart 4: Receivable Ageing (Due Outstanding) - Vertical Bar Chart
        const age = data.ageingBrackets || {};
        const ageLabels = ['0–30 days', '31–60', '61–90', '90+'];
        const ageData = [
            Number(age.bracket_0_30 || age.Bracket_0_30 || 0),
            Number(age.bracket_31_60 || age.Bracket_31_60 || 0),
            Number(age.bracket_61_90 || age.Bracket_61_90 || 0),
            Number(age.bracket_90_Plus || age.Bracket_90_Plus || 0)
        ];
        const ageColors = [
            cssVar('--green') || '#1b8a3c',
            cssVar('--orange') || '#f08c00',
            cssVar('--red') || '#d92d3a',
            '#8b1d28'
        ];
        mkChart('o_ch4', 'bar', ageLabels, [
            barDs('Due', ageData, ageColors)
        ], { money: true, legend: false });

        // 7. Chart 5: Operational Fund Requisitions (Doughnut with Center Text)
        const reqs = data.requisitionBreakdowns || [];
        const reqLabels = reqs.map(r => r.statusName || r.category || 'Status');
        const reqAmounts = reqs.map(r => Number(r.totalAmount || r.amount || 0));
        const totalReq = reqAmounts.reduce((a, b) => a + b, 0);

        mkChart('o_ch5', 'doughnut', reqLabels.length ? reqLabels : ['Approved', 'Pending', 'Rejected'], [
            pieDs(reqAmounts.length ? reqAmounts : [0, 0, 0], [
                cssVar('--green') || '#1b8a3c',
                cssVar('--orange') || '#f08c00',
                cssVar('--red') || '#d92d3a'
            ])
        ], {
            money: true,
            center: { text: compact(totalReq), sub: 'requested' }
        });

        // 8. Grids Initialization & Data Population
        if (!GRIDS.o_g1 && document.getElementById('o_g1')) {
            new Grid('o_g1', {
                size: 5,
                sizes: [5, 10, 15, 20],
                search: true,
                card: 'g760',
                columns: [
                    { k: 'billNo', l: 'Bill No', cls: 'mono nw' },
                    { k: 'billDate', l: 'Bill Date', t: 'date', cls: 'nw' },
                    { k: 'customerName', l: 'Customer', hide: 'md' },
                    { k: 'daysOverdue', l: 'Age (days)', t: 'int' },
                    { k: 'dueAmount', l: 'Due (BDT)', t: 'amt' }
                ]
            });
        }
        if (!GRIDS.o_g2 && document.getElementById('o_g2')) {
            new Grid('o_g2', {
                size: 5,
                sizes: [5, 10, 15, 20],
                search: true,
                card: 'g760',
                columns: [
                    { k: 'jobNo', l: 'Job No', cls: 'mono nw' },
                    { k: 'eta', l: 'ETA', t: 'date', cls: 'nw' },
                    { k: 'customerName', l: 'Customer', hide: 'md' },
                    { k: 'shipmentMode', l: 'Mode', hide: 'md' },
                    { k: 'status', l: 'Status', t: 'badge' }
                ]
            });
        }

        const overdue = data.overdueBills || [];
        if (GRIDS.o_g1) {
            GRIDS.o_g1.setRows(overdue);
        }

        const arrivals = (data.upcomingArrivals || []).map(a => ({
            ...a,
            status: a.status || 'In Transit'
        }));
        if (GRIDS.o_g2) {
            GRIDS.o_g2.setRows(arrivals);
        }
    }
};
