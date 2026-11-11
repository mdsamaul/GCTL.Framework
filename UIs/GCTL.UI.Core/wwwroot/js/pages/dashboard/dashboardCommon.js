/**
 * C&F Management Dashboard - Common Core Module
 * Handles global state, filters, tab switching, reusable Grid component, Chart.js wrappers, and export routines.
 */
'use strict';

const CONFIG = {
    title: 'C&F Management Dashboard',
    fyStart: '2026-07-01'
};

const d$ = s => document.querySelector(s);
const d$$ = s => Array.from(document.querySelectorAll(s));
window.d$ = d$;
window.d$$ = d$$;

// Formatting helpers
const int = n => Math.round(Number(n) || 0).toLocaleString('en-US');
const amt = n => {
    const v = Number(n) || 0;
    return v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};
const money = n => amt(n);

// Reusable Indian Lakh/Crore Formatter
const formatMoney = n => {
    if (n === null || n === undefined || n === '') return '0';
    const v = Number(n) || 0;
    const hasDec = (v % 1 !== 0);
    return v.toLocaleString('en-IN', {
        minimumFractionDigits: hasDec ? 2 : 0,
        maximumFractionDigits: 2
    });
};
window.formatMoney = formatMoney;
const pct = (n, d = 2) => (Number(n) || 0).toFixed(d) + '%';
const compact = n => {
    const v = Number(n) || 0;
    const a = Math.abs(v);
    if (a >= 1e7) return (v / 1e7).toFixed(2) + ' Cr';
    if (a >= 1e5) return (v / 1e5).toFixed(2) + ' L';
    if (a >= 1e3) return (v / 1e3).toFixed(1) + ' k';
    return amt(v);
};
const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const slug = s => String(s ?? '').toLowerCase().replace(/[^a-z0-9]+/g, '-');
const cssVar = n => getComputedStyle(document.documentElement).getPropertyValue(n).trim();

// Chart color palette
const PAL = () => [
    cssVar('--blue') || '#0b57d0',
    cssVar('--green') || '#1b8a3c',
    cssVar('--orange') || '#f08c00',
    cssVar('--purple') || '#8e24aa',
    cssVar('--sky') || '#0a9ac9',
    cssVar('--red') || '#d92d3a',
    cssVar('--teal') || '#0f9d8a',
    cssVar('--slate') || '#64748b',
    '#d946ef', '#84cc16', '#e879a8', '#a16207'
];

// Reusable Grid Component
const GRIDS = {};

class Grid {
    constructor(id, options = {}) {
        this.id = id;
        this.o = Object.assign({
            sizes: [5, 10, 15, 20, 25],
            size: 5,
            search: true,
            sortable: true,
            card: 'g760'
        }, options);
        this.cols = options.columns || [];
        this.rows = [];
        this.view = [];
        this.page = 1;
        this.size = this.o.size;
        this.sk = null;
        this.sd = 1;
        this.q = '';
        this.total = null;

        const szEl = document.getElementById(id + '_sz');
        if (szEl) {
            szEl.innerHTML = this.o.sizes.map(s => `<option ${s === this.size ? 'selected' : ''}>${s}</option>`).join('');
            szEl.onchange = () => { this.size = +szEl.value; this.page = 1; this.render(); };
        }

        const qEl = document.getElementById(id + '_q');
        if (qEl) {
            if (!this.o.search) qEl.style.display = 'none';
            else qEl.oninput = () => { this.q = qEl.value.trim().toLowerCase(); this.page = 1; this.apply(); };
        }

        const container = document.getElementById(id);
        if (container) {
            container.addEventListener('click', e => {
                const th = e.target.closest('th.sortable');
                if (!th) return;
                const k = +th.dataset.i;
                this.sd = this.sk === k ? -this.sd : 1;
                this.sk = k;
                this.apply();
            });
        }

        const footEl = document.getElementById(id + '_f');
        if (footEl) {
            footEl.addEventListener('click', e => {
                const b = e.target.closest('button[data-p]');
                if (!b || b.disabled) return;
                this.page = +b.dataset.p;
                this.render();
            });
        }

        GRIDS[id] = this;
    }

    setRows(rows, totalAll) {
        this.rows = rows || [];
        this.total = totalAll ?? this.rows.length;
        this.page = 1;
        this.apply();
    }

    apply() {
        let v = this.rows;
        if (this.q) {
            v = v.filter(r => this.cols.some(c => {
                const val = r[c.k] !== undefined ? r[c.k] : (r[c.k.charAt(0).toUpperCase() + c.k.slice(1)] ?? r[c.k.toLowerCase()]);
                return val != null && String(val).toLowerCase().includes(this.q);
            }));
        }

        if (this.sk !== null) {
            const col = this.cols[this.sk];
            const getVal = r => {
                if (col.s) return col.s(r);
                if (col.v) return col.v(r);
                return r[col.k] !== undefined ? r[col.k] : (r[col.k.charAt(0).toUpperCase() + col.k.slice(1)] ?? r[col.k.toLowerCase()]);
            };
            v = [...v].sort((a, b) => {
                const x = getVal(a);
                const y = getVal(b);
                if (typeof x === 'number' && typeof y === 'number') {
                    return (x - y) * this.sd;
                }
                const numX = Number(x);
                const numY = Number(y);
                if (!isNaN(numX) && !isNaN(numY) && x !== '' && y !== '' && x != null && y != null) {
                    return (numX - numY) * this.sd;
                }
                return String(x ?? '').localeCompare(String(y ?? '')) * this.sd;
            });
        }

        this.view = v;
        this.render();
    }

    render() {
        const o = this.o;
        const n = this.view.length;
        const pages = Math.max(1, Math.ceil(n / this.size));
        if (this.page > pages) this.page = pages;

        const s = (this.page - 1) * this.size;
        const slice = this.view.slice(s, s + this.size);
        const container = document.getElementById(this.id);
        if (!container) return;

        const isNum = c => ['money', 'amt', 'pct', 'int'].includes(c.t);

        let h = `<table class="grid ${o.card}"><thead>`;
        if (o.groups) {
            h += '<tr class="grp">' + o.groups.map(g => `<th colspan="${g[1]}">${g[0]}</th>`).join('') + '</tr>';
        }
        h += '<tr>' + this.cols.map((c, i) => {
            const sortable = o.sortable && c.t !== 'sn' && !c.nosort;
            const sortCls = this.sk === i ? (this.sd > 0 ? 'asc' : 'desc') : '';
            const thCls = [sortable ? 'sortable' : '', sortCls, c.hide ? 'h-' + c.hide : '', isNum(c) ? 'num' : ''].filter(Boolean).join(' ');
            return `<th class="${thCls}" data-i="${i}" ${c.w ? `style="width:${c.w}"` : ''}>${c.l}${sortable ? '<span class="si">&#x21C5;</span>' : ''}</th>`;
        }).join('') + '</tr></thead><tbody>';

        if (!slice.length) {
            h += `<tr class="empty-row"><td colspan="${this.cols.length}">No records match the selected filters.</td></tr>`;
        } else {
            slice.forEach((r, i) => {
                h += `<tr class="${o.rowCls ? o.rowCls(r) : ''}">` + this.cols.map(c => {
                    let formatted = '';
                    const rawVal = r[c.k] !== undefined ? r[c.k] : (r[c.k.charAt(0).toUpperCase() + c.k.slice(1)] ?? r[c.k.toLowerCase()]);
                    if (c.r) {
                        formatted = c.r(r, s + i);
                    } else if (c.t === 'sn') {
                        formatted = s + i + 1;
                    } else if (c.t === 'badge') {
                        formatted = `<span class="badge s-${slug(rawVal)}">${esc(rawVal)}</span>`;
                    } else if (c.t === 'money' || c.t === 'amt') {
                        formatted = (rawVal === 0 ? '–' : (rawVal == null || rawVal === '' ? '' : amt(rawVal)));
                    } else if (c.t === 'int') {
                        formatted = (rawVal === 0 ? '0' : (rawVal == null || rawVal === '' ? '' : int(rawVal)));
                    } else if (c.t === 'pct') {
                        formatted = pct(rawVal);
                    } else {
                        formatted = esc(rawVal ?? '');
                    }

                    const numCls = isNum(c) ? 'num' : '';
                    const fullCls = [numCls, c.cls || '', c.hide ? 'h-' + c.hide : ''].filter(Boolean).join(' ');
                    return `<td class="${fullCls}" data-l="${esc(c.l)}">${formatted}</td>`;
                }).join('') + '</tr>';
            });
        }
        h += '</tbody>';

        // Totals Footer
        if (o.totals && n) {
            h += '<tfoot><tr>' + this.cols.map((c, i) => {
                let txt = '';
                if (i === 0) txt = 'Total';
                if (c.tot) {
                    const totalVal = this.view.reduce((sum, r) => sum + (Number(r[c.k]) || 0), 0);
                    txt = amt(totalVal);
                }
                const numCls = isNum(c) ? 'num' : '';
                return `<td class="${numCls}">${txt}</td>`;
            }).join('') + '</tr></tfoot>';
        }

        h += '</table>';
        container.innerHTML = h;

        // Pager Footer
        const footEl = document.getElementById(this.id + '_f');
        if (footEl) {
            const from = n ? s + 1 : 0;
            const to = Math.min(n, s + this.size);
            let f = `<div>Showing ${int(from)} to ${int(to)} of ${int(n)} entries${this.total != null && this.total !== n ? ` (filtered from ${int(this.total)} total entries)` : ''}</div><div class="pager">`;
            const btn = (p, l, dis, on) => `<button data-p="${p}" ${dis ? 'disabled' : ''} class="${on ? 'on' : ''}" aria-label="page ${p}">${l}</button>`;
            f += btn(1, '&laquo;', this.page === 1) + btn(this.page - 1, '&lsaquo;', this.page === 1);
            const P = this.page, nums = pages <= 7
                ? [...Array(pages)].map((_, i) => i + 1)
                : P <= 4 ? [1, 2, 3, 4, 5, '…', pages]
                : P >= pages - 3 ? [1, '…', pages - 4, pages - 3, pages - 2, pages - 1, pages]
                : [1, '…', P - 1, P, P + 1, '…', pages];
            nums.forEach(x => f += x === '…' ? '<span class="dots">…</span>' : btn(x, x, false, x === P));
            f += btn(this.page + 1, '&rsaquo;', this.page === pages) + btn(pages, '&raquo;', this.page === pages) + '</div>';
            footEl.innerHTML = f;
        }
    }
}

// Global Chart Registry
const CH = {};

function mkChart(id, type, labels, datasets, opts = {}) {
    const cv = document.getElementById(id);
    if (!cv) return;
    if (CH[id]) {
        CH[id].destroy();
        delete CH[id];
    }
    const wrap = cv.parentNode;
    if (!window.Chart) {
        wrap.innerHTML = '<div class="empty">Chart library not loaded.</div>';
        return;
    }
    if (!labels || !labels.length) {
        wrap.innerHTML = '<div class="empty">No data for selected filters.</div>';
        return;
    }
    if (!wrap.contains(cv) || !wrap.querySelector('canvas')) {
        wrap.innerHTML = `<canvas id="${id}"></canvas>`;
    }

    const isPie = type === 'doughnut' || type === 'pie';
    const isHoriz = !!opts.horizontal;

    // Register centerText plugin for doughnut charts if not present
    if (window.Chart && !Chart.registry?.plugins?.get('centerText')) {
        Chart.register({
            id: 'centerText',
            afterDraw(ch, _a, opt) {
                if (!opt || !opt.text) return;
                const a = ch.chartArea;
                if (!a) return;
                const cx = (a.left + a.right) / 2;
                const cy = (a.top + a.bottom) / 2;
                const ctx = ch.ctx;
                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = cssVar('--ink') || '#1a2740';
                ctx.font = '700 16px ' + (Chart.defaults.font?.family || 'sans-serif');
                ctx.fillText(opt.text, cx, cy - (opt.sub ? 8 : 0));
                if (opt.sub) {
                    ctx.fillStyle = cssVar('--muted') || '#64748b';
                    ctx.font = '11px ' + (Chart.defaults.font?.family || 'sans-serif');
                    ctx.fillText(opt.sub, cx, cy + 11);
                }
                ctx.restore();
            }
        });
    }

    const chartConfig = {
        type: type,
        data: { labels, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 350 },
            indexAxis: isHoriz ? 'y' : 'x',
            layout: {
                padding: {
                    top: isPie ? 4 : (opts.legend !== false && !isPie ? 4 : 0),
                    right: isPie ? 12 : 12,
                    left: 4,
                    bottom: 4
                }
            },
            plugins: {
                legend: {
                    display: opts.legend !== false && (isPie || datasets.length > 1),
                    position: isPie ? 'right' : (opts.legendPosition || 'top'),
                    align: isPie ? 'center' : (opts.legendAlign || 'end'),
                    labels: {
                        usePointStyle: true,
                        pointStyle: 'circle',
                        boxWidth: 7,
                        boxHeight: 7,
                        padding: isPie ? 12 : 14,
                        font: { size: 11, family: Chart.defaults.font?.family || 'sans-serif' }
                    }
                },
                centerText: opts.center || false,
                tooltip: {
                    callbacks: {
                        title: items => {
                            if (!items || !items.length) return '';
                            const idx = items[0].dataIndex;
                            return (labels && labels[idx] !== undefined ? labels[idx] : items[0].label) || '';
                        },
                        label: ctx => {
                            const val = isHoriz ? (ctx.parsed?.x ?? ctx.raw) : (ctx.parsed?.y ?? ctx.raw);
                            if (opts.money) return ` ${ctx.dataset.label || ctx.label || 'Amount'}: ${formatMoney(val)} BDT`;
                            return ` ${ctx.dataset.label || ctx.label}: ${int(val)}`;
                        }
                    }
                }
            },
            scales: isPie ? {} : (isHoriz ? {
                y: {
                    type: 'category',
                    grid: { display: false },
                    ticks: {
                        font: { size: 11 },
                        autoSkip: false,
                        callback: function (val, idx) {
                            const label = this.getLabelForValue ? this.getLabelForValue(val) : labels[idx];
                            const str = String(label ?? '');
                            return str.length > 22 ? str.substring(0, 20) + '…' : str;
                        }
                    },
                    afterFit: function (axis) {
                        if (axis.width < 140) axis.width = 140;
                    }
                },
                x: {
                    beginAtZero: true,
                    grid: {
                        display: true,
                        color: '#eef2f8',
                        drawBorder: false
                    },
                    ticks: {
                        font: { size: 10.5 },
                        callback: v => opts.money ? compact(v) : int(v)
                    }
                }
            } : {
                x: {
                    stacked: !!opts.stacked,
                    grid: { display: false },
                    ticks: { font: { size: 10.5 } }
                },
                y: {
                    stacked: !!opts.stacked,
                    beginAtZero: true,
                    grid: { color: '#eef2f8' },
                    ticks: {
                        font: { size: 10.5 },
                        callback: v => opts.money ? compact(v) : int(v)
                    }
                }
            })
        }
    };

    if (opts.y2 && !isPie && !isHoriz) {
        chartConfig.options.scales.y2 = {
            position: 'right',
            beginAtZero: true,
            grid: { drawOnChartArea: false },
            ticks: { callback: v => v + '%' }
        };
    }

    CH[id] = new Chart(document.getElementById(id), chartConfig);

    // Show full customer name in tooltip when hovering over truncated axis labels
    if (isHoriz) {
        cv.onmousemove = e => {
            const chart = CH[id];
            if (!chart || !chart.chartArea || !chart.scales?.y) return;
            const rect = cv.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            if (x < chart.chartArea.left) {
                const yAxis = chart.scales.y;
                for (let i = 0; i < labels.length; i++) {
                    const py = yAxis.getPixelForValue(i);
                    if (Math.abs(y - py) <= 12) {
                        cv.title = labels[i] || '';
                        return;
                    }
                }
            }
            cv.title = '';
        };
    }
}

const barDs = (label, data, color, extra = {}) => Object.assign({
    label,
    data,
    backgroundColor: color,
    borderRadius: 4,
    maxBarThickness: 32
}, extra);

const pieDs = (data, colors) => ({
    data,
    backgroundColor: colors || PAL(),
    borderWidth: 2,
    borderColor: '#fff'
});

// Helper to set KPI card
function setK(id, val, sub = null) {
    const el = document.getElementById(id);
    if (!el) return;
    const vEl = el.querySelector('.kval');
    if (vEl) vEl.textContent = val;
    if (sub !== null) {
        const sEl = el.querySelector('.ksub');
        if (sEl) sEl.innerHTML = sub;
    }
}

const cur = v => (CONFIG.currency || '৳') + ' ' + compact(v);
window.cur = cur;

function getDateValue(id) {
    const el = document.getElementById(id);
    if (!el) return '';
    if (window.jQuery && $(el).data('customFpInstance')) {
        const fp = $(el).data('customFpInstance');
        if (fp.selectedDates && fp.selectedDates.length > 0) {
            const d = fp.selectedDates[0];
            const day = String(d.getDate()).padStart(2, '0');
            const month = String(d.getMonth() + 1).padStart(2, '0');
            return `${d.getFullYear()}-${month}-${day}`;
        }
    }
    const val = (el.value || '').trim();
    if (!val) return '';
    const parts = val.split('/');
    if (parts.length === 3) {
        return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
    }
    return val;
}

function setDateValue(id, dateObj) {
    const el = document.getElementById(id);
    if (!el) return;
    const day = String(dateObj.getDate()).padStart(2, '0');
    const month = String(dateObj.getMonth() + 1).padStart(2, '0');
    const year = dateObj.getFullYear();
    const dmy = `${day}/${month}/${year}`;
    if (window.jQuery && $(el).data('customFpInstance')) {
        const fp = $(el).data('customFpInstance');
        fp.setDate(dateObj, false);
    }
    el.value = dmy;
}

// Global Filter State & Dispatcher
const DashboardApp = {
    currentTab: 'overview',

    getFilterState() {
        return {
            fromDate: getDateValue('fFrom') || null,
            toDate: getDateValue('fTo') || null,
            branchCode: d$('#fBranch')?.value || '',
            shipmentMode: d$('#fMode')?.value || '',
            port: d$('#fPort')?.value || '',
            customerID: d$('#fCust')?.value || '',
            jobStatus: d$('#fStatus')?.value || ''
        };
    },

    init() {
        const hashTab = (window.location.hash || '').replace('#', '').trim();
        const storedTab = sessionStorage.getItem('dashboard_active_tab');
        const initTab = d$('#tabs')?.dataset?.initialTab;

        if (hashTab && ['overview', 'jobs', 'bills', 'gp', 'recon', 'accounts'].includes(hashTab)) {
            this.currentTab = hashTab;
        } else if (storedTab && ['overview', 'jobs', 'bills', 'gp', 'recon', 'accounts'].includes(storedTab)) {
            this.currentTab = storedTab;
        } else if (initTab) {
            this.currentTab = initTab;
        }

        this.setupTabNavigation();
        this.setupFilterEvents();
        this.setupExportMenu();

        // Initial Load of Active Tab
        this.switchTab(this.currentTab);
    },

    showLoader(panelId) {
        const p = document.getElementById(panelId);
        if (!p) return;
        p.style.position = 'relative';
        if (!p.querySelector('.dashboard-loading')) {
            const ld = document.createElement('div');
            ld.className = 'dashboard-loading';
            ld.innerHTML = '<div class="dashboard-spinner"></div>';
            p.appendChild(ld);
        }
    },

    hideLoader(panelId) {
        const p = document.getElementById(panelId);
        if (!p) return;
        const ld = p.querySelector('.dashboard-loading');
        if (ld) ld.remove();
    },

    setupTabNavigation() {
        d$$('#tabs .tab').forEach(btn => {
            btn.onclick = () => {
                const tab = btn.dataset.t;
                this.switchTab(tab);
            };
        });
    },

    switchTab(tabId) {
        this.currentTab = tabId;
        sessionStorage.setItem('dashboard_active_tab', tabId);
        if (window.history && window.history.replaceState) {
            window.history.replaceState(null, '', '#' + tabId);
        }
        d$$('#tabs .tab').forEach(b => b.classList.toggle('on', b.dataset.t === tabId));
        d$$('.panel').forEach(p => p.classList.toggle('on', p.id === 'p-' + tabId));
        this.updatePeriodDisplay();
        this.loadActiveTab();
    },

    updatePeriodDisplay() {
        const f = d$('#fFrom')?.value;
        const t = d$('#fTo')?.value;
        const txt = (f && t) ? `${f} – ${t}` : 'Active Period';
        d$$('[data-period]').forEach(sp => sp.textContent = txt);
    },

    setupFilterEvents() {
        const refresh = () => {
            this.updatePeriodDisplay();
            this.loadActiveTab();
        };

        const onDateChanged = () => {
            if (window.setSelect) {
                window.setSelect('#fRange', 'custom');
            } else if (d$('#fRange')) {
                d$('#fRange').value = 'custom';
            }
            refresh();
        };

        d$('#fFrom')?.addEventListener('change', onDateChanged);
        d$('#fTo')?.addEventListener('change', onDateChanged);

        if (window.jQuery) {
            $('#fBranch, #fMode, #fPort, #fCust, #fStatus').on('change', refresh);
            $('#fRange').on('change', function () {
                DashboardApp.applyQuickRange(this.value);
                refresh();
            });
        } else {
            d$('#fBranch')?.addEventListener('change', refresh);
            d$('#fMode')?.addEventListener('change', refresh);
            d$('#fPort')?.addEventListener('change', refresh);
            d$('#fCust')?.addEventListener('change', refresh);
            d$('#fStatus')?.addEventListener('change', refresh);

            d$('#fRange')?.addEventListener('change', e => {
                this.applyQuickRange(e.target.value);
                refresh();
            });
        }

        d$('#btnReset')?.addEventListener('click', () => {
            this.resetFilters();
            refresh();
        });
    },

    applyQuickRange(code) {
        const today = new Date();
        let from = new Date(today);
        let to = new Date(today);

        switch (code) {
            case 'today':
                break;
            case '7d':
                from.setDate(today.getDate() - 7);
                break;
            case '30d':
                from.setDate(today.getDate() - 30);
                break;
            case 'this_month':
                from = new Date(today.getFullYear(), today.getMonth(), 1);
                break;
            case 'last_month':
                from = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                to = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case '3m':
                from.setMonth(today.getMonth() - 3);
                break;
            case '6m':
                from.setMonth(today.getMonth() - 6);
                break;
            case '1y':
                from.setFullYear(today.getFullYear() - 1);
                break;
            default:
                return;
        }

        setDateValue('fFrom', from);
        setDateValue('fTo', to);
    },

    resetFilters() {
        this.applyQuickRange('3m');
        if (window.setSelect) {
            window.setSelect('#fRange', '3m');
            window.setSelect('#fBranch', '');
            window.setSelect('#fMode', '');
            window.setSelect('#fPort', '');
            window.setSelect('#fCust', '');
            window.setSelect('#fStatus', '');
        } else {
            if (d$('#fRange')) d$('#fRange').value = '3m';
            if (d$('#fBranch')) d$('#fBranch').value = '';
            if (d$('#fMode')) d$('#fMode').value = '';
            if (d$('#fPort')) d$('#fPort').value = '';
            if (d$('#fCust')) d$('#fCust').value = '';
            if (d$('#fStatus')) d$('#fStatus').value = '';
        }
    },

    setupExportMenu() {
        const menu = d$('#expMenu');
        d$('#btnExport')?.addEventListener('click', e => {
            e.stopPropagation();
            menu?.classList.toggle('open');
        });
        document.addEventListener('click', () => menu?.classList.remove('open'));

        d$('#xPreview')?.addEventListener('click', () => window.print());
        d$('#xPdf')?.addEventListener('click', () => this.exportPdf());
        d$('#xExcel')?.addEventListener('click', () => this.exportExcel());
    },

    exportPdf() {
        if (!window.jspdf) {
            alert('PDF library is loading. Please try again.');
            return;
        }
        window.print();
    },

    exportExcel() {
        if (!window.XLSX) {
            alert('Excel library not loaded.');
            return;
        }
        const activePanel = document.querySelector('.panel.on');
        const table = activePanel?.querySelector('table.grid');
        if (!table) {
            alert('No table found in the current tab to export.');
            return;
        }
        const wb = XLSX.utils.table_to_book(table, { sheet: "Dashboard" });
        XLSX.writeFile(wb, `${CONFIG.title}_${this.currentTab}.xlsx`);
    },

    loadActiveTab() {
        const filter = this.getFilterState();
        switch (this.currentTab) {
            case 'overview':
                if (window.DashboardOverview) window.DashboardOverview.load(filter);
                break;
            case 'jobs':
                if (window.DashboardJobStatus) window.DashboardJobStatus.load(filter);
                break;
            case 'bills':
                if (window.DashboardBillEntry) window.DashboardBillEntry.load(filter);
                break;
            case 'gp':
                if (window.DashboardGPAnalysis) window.DashboardGPAnalysis.load(filter);
                break;
            case 'recon':
                if (window.DashboardJobReconcile) window.DashboardJobReconcile.load(filter);
                break;
            case 'accounts':
                if (window.DashboardAccounts) window.DashboardAccounts.load(filter);
                break;
        }
    }
};

document.addEventListener('DOMContentLoaded', () => {
    DashboardApp.init();
});

