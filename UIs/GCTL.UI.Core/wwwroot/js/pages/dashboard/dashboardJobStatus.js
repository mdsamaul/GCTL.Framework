/**
 * Job Status Tab Module
 */
'use strict';

window.DashboardJobStatus = {
    grid: null,

    initGrid() {
        if (!this.grid) {
            this.grid = new Grid('j_g', {
                card: 'g760',
                totals: false,
                columns: [
                    { t: 'sn', l: 'SL', w: '40px' },
                    { k: 'jobNo', l: 'Job No', cls: 'mono nw' },
                    { k: 'jobDate', l: 'Date', cls: 'nw' },
                    { k: 'customerName', l: 'Customer' },
                    { k: 'expenseType', l: 'Mode', hide: 'md' },
                    { k: 'portName', l: 'Port', hide: 'md' },
                    { k: 'beNo', l: 'B/E No', hide: 'lg' },
                    { k: 'lcValue', l: 'LC Value', t: 'money' },
                    { k: 'grossWeight', l: 'Gross Wt', t: 'amt', hide: 'lg' },
                    { k: 'shipmentStatus', l: 'Status', t: 'badge' }
                ]
            });
        }
    },

    async load(filter) {
        this.initGrid();
        if (window.DashboardApp) window.DashboardApp.showLoader('p-jobs');
        try {
            const resp = await fetch('/DashboardJobStatus/GetData', {
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
            console.error('Error loading Job Status data:', err);
        } finally {
            if (window.DashboardApp) window.DashboardApp.hideLoader('p-jobs');
        }
    },

    render(data) {
        const k = data.kpis || data.kpIs || data.KPIs || {};

        // KPIs
        setK('j_n', int(k.totalJobs ?? k.TotalJobs), 'jobs handled');
        setK('j_pen', int(k.pendingJobs ?? k.PendingJobs), 'awaiting action');
        setK('j_tr', int(k.inTransitJobs ?? k.InTransitJobs), 'clearance ongoing');
        setK('j_del', int(k.deliveredJobs ?? k.DeliveredJobs), 'completed & handed over');
        setK('j_lc', cur(k.totalLCValue ?? k.TotalLCValue), 'declared consignment value');

        // Minis
        const setMini = (id, val) => {
            const el = document.getElementById(id);
            if (el) {
                const vEl = el.querySelector('.mval');
                if (vEl) vEl.textContent = val;
            }
        };

        setMini('jm_dr', pct(k.deliveryRate, 1));
        setMini('jm_wt', amt(k.totalGrossWeight) + ' MT');

        const modes = data.modeDistributions || [];
        setMini('jm_topm', modes.length ? modes[0].modeName : '-');

        const ports = data.portWorkloads || [];
        setMini('jm_topp', ports.length ? ports[0].portName : '-');

        const topCust = data.topCustomers || [];
        setMini('jm_topc', topCust.length ? topCust[0].customerName : '-');

        // Chart 1: Shipment Mode Distribution
        mkChart('j_ch1', 'doughnut', modes.map(m => m.modeName), [
            pieDs(modes.map(m => m.jobCount))
        ]);

        // Chart 2: Status Breakdown
        const statuses = data.statusShares || [];
        mkChart('j_ch2', 'doughnut', statuses.map(s => s.statusName), [
            pieDs(statuses.map(s => s.jobCount), [
                cssVar('--orange') || '#f08c00',
                cssVar('--purple') || '#8e24aa',
                cssVar('--green') || '#1b8a3c'
            ])
        ]);

        // Chart 3: Port Workload
        mkChart('j_ch3', 'bar', ports.map(p => p.portName), [
            barDs('Jobs', ports.map(p => p.jobCount), cssVar('--sky') || '#0a9ac9')
        ], { horizontal: true, legend: false });

        // Chart 4: Monthly Trend
        const trends = data.monthlyTrends || [];
        mkChart('j_ch4', 'bar', trends.map(t => t.monthLabel), [
            barDs('Total Jobs', trends.map(t => t.totalJobs), cssVar('--blue') || '#0b57d0'),
            barDs('Delivered', trends.map(t => t.deliveredJobs), cssVar('--green') || '#1b8a3c')
        ]);

        // Chart 5: Top Customers
        mkChart('j_ch5', 'bar', topCust.map(c => c.customerName), [
            barDs('Jobs', topCust.map(c => c.totalJobs), cssVar('--purple') || '#8e24aa')
        ], { horizontal: true, legend: false });

        // Grid
        if (this.grid) {
            this.grid.setRows(data.jobList || []);
        }
    }
};

