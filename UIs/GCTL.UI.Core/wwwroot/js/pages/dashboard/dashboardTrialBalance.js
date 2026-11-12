/**
 * Trial Balance Sub-Tab Module (Accounts)
 */
'use strict';

window.DashboardTrialBalance = {
    grid: null,
    rawData: null,
    lastFilter: null,

    initGrid() {
        if (this.grid) return;

        const lv = r => `<span style="padding-left:${((r.level || r.Level || 1) - 1) * 14}px">${r.code || r.Code ? esc(r.code || r.Code) + ' – ' : ''}${esc(r.particularName || r.ParticularName || r.name || '')}</span>`;

        this.grid = new Grid('t_g', {
            card: 'g760',
            sortable: false,
            size: 'All',
            sizes: [10, 25, 50, 100, 'All'],
            rowCls: r => 'l' + (r.level || r.Level || 1),
            groups: [
                ['', 1],
                ['Opening Balance', 2],
                ['Transactions', 2],
                ['Closing Balance', 2]
            ],
            columns: [
                {
                    k: 'particularName',
                    l: 'Particulars',
                    r: lv,
                    v: r => ((r.code || r.Code) ? (r.code || r.Code) + ' ' : '') + (r.particularName || r.ParticularName || r.name || '') + ' ' + (r.particulars || r.Particulars || ''),
                    x: r => ((r.code || r.Code) ? (r.code || r.Code) + ' – ' : '') + (r.particularName || r.ParticularName || '')
                },
                { k: 'opDr', l: 'Debit', t: 'money', v: r => r.opDr ?? r.OpDr },
                { k: 'opCr', l: 'Credit', t: 'money', v: r => r.opCr ?? r.OpCr },
                { k: 'trDr', l: 'Debit', t: 'money', v: r => r.trDr ?? r.TrDr },
                { k: 'trCr', l: 'Credit', t: 'money', v: r => r.trCr ?? r.TrCr },
                { k: 'clDr', l: 'Debit', t: 'money', v: r => r.clDr ?? r.ClDr },
                { k: 'clCr', l: 'Credit', t: 'money', v: r => r.clCr ?? r.ClCr }
            ]
        });

        const levelEl = document.getElementById('tbLevel');
        if (levelEl) levelEl.onchange = () => this.filterAndRenderRows();
        const zeroEl = document.getElementById('tbZero');
        if (zeroEl) zeroEl.onchange = () => this.filterAndRenderRows();
    },

    async load(filter) {
        this.lastFilter = filter;
        this.initGrid();
        try {
            const resp = await fetch('/DashboardTrialBalance/GetData', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(filter || {})
            });
            const res = await resp.json();
            if (!res.isSuccess) {
                console.error(res.message);
                return;
            }
            this.rawData = res.data;
            this.render(res.data);
        } catch (err) {
            console.error('Error loading Trial Balance data:', err);
        }
    },

    render(data) {
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
            mkChart('t_ch1', 'bar', labels, [
                barDs('Debit', groups.map(g => g.closingDebit ?? g.ClosingDebit ?? 0), cssVar('--blue') || '#0b57d0'),
                barDs('Credit', groups.map(g => g.closingCredit ?? g.ClosingCredit ?? 0), cssVar('--green') || '#1b8a3c')
            ], { money: true });
        }

        // Control Doughnut Chart (t_ch2)
        const controls = data.controlChart || data.ControlChart || [];
        if (controls.length > 0) {
            const cLabels = controls.map(c => (c.code || c.Code) + ' ' + (c.controlName || c.ControlName || ''));
            const cDrs = controls.map(c => c.closingDebit ?? c.ClosingDebit ?? 0);
            const totalDrs = cDrs.reduce((a, b) => a + b, 0);
            mkChart('t_ch2', 'doughnut', cLabels, [pieDs(cDrs)], {
                money: true,
                center: { text: compact(totalDrs), sub: 'debit' }
            });
        }

        this.filterAndRenderRows();
    },

    filterAndRenderRows() {
        if (!this.rawData) return;
        const allRows = this.rawData.rows || this.rawData.Rows || [];
        const lvlEl = document.getElementById('tbLevel');
        const maxLevel = lvlEl ? +lvlEl.value : 4;
        const zeroEl = document.getElementById('tbZero');
        const hideZero = zeroEl ? zeroEl.checked : true;

        const filtered = allRows.filter(r => {
            const lvl = r.level ?? r.Level ?? 1;
            if (lvl > maxLevel) return false;
            if (hideZero) {
                const vals = [r.opDr ?? r.OpDr, r.opCr ?? r.OpCr, r.trDr ?? r.TrDr, r.trCr ?? r.TrCr, r.clDr ?? r.ClDr, r.clCr ?? r.ClCr];
                if (vals.every(v => !v)) return false;
            }
            return true;
        });

        if (this.grid) this.grid.setRows(filtered);
    },

    // ---------------- Report Export (Preview / PDF / Excel) ----------------
    isActive() {
        const panelOn = document.getElementById('p-accounts')?.classList.contains('on');
        const subOn = document.getElementById('s-tb')?.classList.contains('on');
        return !!(panelOn && subOn);
    },

    getExportParams(extra = {}) {
        const app = (typeof DashboardApp !== 'undefined') ? DashboardApp : window.DashboardApp;
        const f = (app && typeof app.getFilterState === 'function') ? app.getFilterState() : (this.lastFilter || {});
        const params = new URLSearchParams();
        if (f.fromDate) params.append('fromDate', f.fromDate);
        if (f.toDate) params.append('toDate', f.toDate);
        if (f.branchCode) params.append('branchCode', f.branchCode);

        const lvlEl = document.getElementById('tbLevel');
        params.append('level', lvlEl ? lvlEl.value : '4');
        const zeroEl = document.getElementById('tbZero');
        params.append('hideZero', zeroEl ? String(zeroEl.checked) : 'true');

        Object.keys(extra).forEach(k => params.append(k, extra[k]));
        return params.toString();
    },

    handleReportExport(format) {
        const fmt = (format || 'preview').toLowerCase();
        if (fmt === 'preview') {
            const url = '/DashboardTrialBalance/ExportPdf?' + this.getExportParams({ download: 'false' });
            const app = (typeof DashboardApp !== 'undefined') ? DashboardApp : window.DashboardApp;
            if (app && typeof app.openReportPreviewModal === 'function') {
                app.openReportPreviewModal('Trial Balance Report Preview', url);
            } else {
                window.open(url, '_blank');
            }
        } else if (fmt === 'pdf') {
            window.location.href = '/DashboardTrialBalance/ExportPdf?' + this.getExportParams({ download: 'true' });
        } else if (fmt === 'excel') {
            window.location.href = '/DashboardTrialBalance/ExportExcel?' + this.getExportParams();
        }
    }
};

// Intercept global Export menu when Accounts > Trial Balance is active (capture phase)
(function () {
    function wire() {
        const map = { xPreview: 'Preview', xPdf: 'PDF', xExcel: 'Excel' };
        Object.keys(map).forEach(id => {
            document.getElementById(id)?.addEventListener('click', function (e) {
                const tb = window.DashboardTrialBalance;
                if (!tb || !tb.isActive()) return;
                e.stopImmediatePropagation();
                e.preventDefault();
                document.getElementById('expMenu')?.classList.remove('open');
                tb.handleReportExport(map[id]);
            }, true);
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', wire);
    else wire();
})();

