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

        // KPIs
        setK('o_jobs', int(k.totalJobs ?? k.TotalJobs), 'in selected period');
        setK('o_nsv', cur(k.netServiceValue ?? k.NetServiceValue), 'total billed');
        setK('o_gp', cur(k.grossProfit ?? k.GrossProfit), 'net margin');
        setK('o_due', cur(k.totalDueOutstanding ?? k.TotalDueOutstanding), 'customer receivables');
        setK('o_cash', cur(k.cashInHand ?? k.CashInHand), 'active cash accounts');
        setK('o_bank', cur(k.bankBalance ?? k.BankBalance), 'liquid bank funds');

        // Chart 1: Monthly Revenue & Profit Trend
        const trends = data.monthlyTrends || [];
        const mLabels = trends.map(t => t.monthLabel);
        const revData = trends.map(t => t.revenue);
        const prfData = trends.map(t => t.profit);
        const gpMargins = trends.map(t => t.gpMargin);

        mkChart('o_ch1', 'bar', mLabels, [
            barDs('Revenue', revData, cssVar('--blue') || '#0b57d0'),
            barDs('Profit', prfData, cssVar('--green') || '#1b8a3c'),
            {
                type: 'line',
                label: 'GP %',
                data: gpMargins,
                borderColor: cssVar('--purple') || '#8e24aa',
                backgroundColor: cssVar('--purple') || '#8e24aa',
                yAxisID: 'y2',
                tension: 0.3,
                pointRadius: 4
            }
        ], { money: true, y2: true });

        // Chart 2: Job Status Share (Doughnut)
        const statuses = data.statusShares || [];
        mkChart('o_ch2', 'doughnut', statuses.map(s => s.statusName), [
            pieDs(statuses.map(s => s.jobCount), [
                cssVar('--orange') || '#f08c00',
                cssVar('--purple') || '#8e24aa',
                cssVar('--green') || '#1b8a3c'
            ])
        ]);

        // Chart 3: Top Customers by Billed Value
        const topCust = data.topCustomers || [];
        mkChart('o_ch3', 'bar', topCust.map(c => c.customerName), [
            barDs('Billed Value', topCust.map(c => c.totalBillAmount), cssVar('--blue') || '#0b57d0')
        ], { horizontal: true, money: true, legend: false });

        // Chart 4: Outstanding Ageing
        const age = data.ageingBrackets || {};
        const ageLabels = ['0–30 Days', '31–60 Days', '61–90 Days', '90+ Days'];
        const ageData = [
            age.bracket_0_30 || age.Bracket_0_30 || 0,
            age.bracket_31_60 || age.Bracket_31_60 || 0,
            age.bracket_61_90 || age.Bracket_61_90 || 0,
            age.bracket_90_Plus || age.Bracket_90_Plus || 0
        ];
        mkChart('o_ch4', 'doughnut', ageLabels, [
            pieDs(ageData, [
                cssVar('--green') || '#1b8a3c',
                cssVar('--blue') || '#0b57d0',
                cssVar('--orange') || '#f08c00',
                cssVar('--red') || '#d92d3a'
            ])
        ], { money: true });

        // Overdue Bills Table
        const overdue = data.overdueBills || [];
        const obBadge = document.getElementById('o_overdue_badge');
        if (obBadge) obBadge.textContent = `${overdue.length} overdue`;

        const t1Body = document.querySelector('#o_t1 tbody');
        if (t1Body) {
            if (!overdue.length) {
                t1Body.innerHTML = '<tr class="empty-row"><td colspan="5">No overdue bills found.</td></tr>';
            } else {
                t1Body.innerHTML = overdue.slice(0, 10).map(b => `
                    <tr>
                        <td class="mono nw">${esc(b.billNo)}</td>
                        <td>${esc(b.customerName)}</td>
                        <td class="nw">${esc(b.billDate)}</td>
                        <td class="num font-bold text-red-600">${money(b.dueAmount)}</td>
                        <td class="ctr"><span class="badge s-rejected">${b.daysOverdue} d</span></td>
                    </tr>
                `).join('');
            }
        }

        // Upcoming Arrivals Table
        const arrivals = data.upcomingArrivals || [];
        const arBadge = document.getElementById('o_arrivals_badge');
        if (arBadge) arBadge.textContent = `${arrivals.length} upcoming`;

        const t2Body = document.querySelector('#o_t2 tbody');
        if (t2Body) {
            if (!arrivals.length) {
                t2Body.innerHTML = '<tr class="empty-row"><td colspan="6">No upcoming arrivals in this period.</td></tr>';
            } else {
                t2Body.innerHTML = arrivals.slice(0, 10).map(a => `
                    <tr>
                        <td class="mono nw">${esc(a.jobNo)}</td>
                        <td>${esc(a.customerName)}</td>
                        <td>${esc(a.shipmentMode)}</td>
                        <td>${esc(a.portName)}</td>
                        <td class="nw font-semibold">${esc(a.eta)}</td>
                        <td>${esc(a.vesselName || '-')}</td>
                    </tr>
                `).join('');
            }
        }
    }
};

