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
    return amt(n);
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
            sizes: [5, 10, 20, 50, 100, 'All'],
            size: 5,
            search: true,
            sortable: true,
            card: 'g760'
        }, options);
        this.cols = options.columns || [];
        this.rows = [];
        this.view = [];
        this.page = 1;
        this.size = (this.o.size === -1 || this.o.size === 'All') ? 999999 : (Number(this.o.size) || 10);
        this.sk = null;
        this.sd = 1;
        this.q = '';
        this.total = null;

        const szEl = document.getElementById(id + '_sz');
        if (szEl) {
            szEl.innerHTML = this.o.sizes.map(s => {
                const isAll = s === 'All' || s === -1;
                const val = isAll ? -1 : s;
                const label = isAll ? 'All' : s;
                const selected = (isAll && this.size >= 999999) || (!isAll && s === this.size) ? 'selected' : '';
                return `<option value="${val}" ${selected}>${label}</option>`;
            }).join('');
            szEl.onchange = () => {
                const v = +szEl.value;
                this.size = (v === -1 || isNaN(v)) ? 999999 : v;
                this.page = 1;
                this.render();
            };
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
                let val = undefined;
                if (typeof c.v === 'function') {
                    val = c.v(r);
                } else if (c.k) {
                    val = r[c.k] !== undefined ? r[c.k] : (r[c.k.charAt(0).toUpperCase() + c.k.slice(1)] ?? r[c.k.toLowerCase()]);
                }
                return val != null && String(val).toLowerCase().includes(this.q);
            }));
        }

        if (this.sk !== null) {
            const col = this.cols[this.sk];
            const getVal = r => {
                if (col.s) return col.s(r);
                if (col.v) return col.v(r);
                if (!col.k) return '';
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

        let h = `<table class="grid ${o.card}">`;
        if (typeof o.customHeader === 'function') {
            h += o.customHeader();
        } else {
            h += '<thead>';
            if (o.groups) {
                h += '<tr class="grp">' + o.groups.map(g => `<th colspan="${g[1]}">${g[0]}</th>`).join('') + '</tr>';
            }
            h += '<tr>' + this.cols.map((c, i) => {
                const sortable = o.sortable && c.t !== 'sn' && !c.nosort;
                const sortCls = this.sk === i ? (this.sd > 0 ? 'asc' : 'desc') : '';
                const thCls = [sortable ? 'sortable' : '', sortCls, c.hide ? 'h-' + c.hide : '', isNum(c) ? 'num' : ''].filter(Boolean).join(' ');
                return `<th class="${thCls}" data-i="${i}" ${c.w ? `style="width:${c.w}"` : ''}>${c.l}${sortable ? '<span class="si">&#x21C5;</span>' : ''}</th>`;
            }).join('') + '</tr></thead>';
        }
        h += '<tbody>';

        if (!slice.length) {
            h += `<tr class="empty-row"><td colspan="${this.cols.length}">No records match the selected filters.</td></tr>`;
        } else {
            slice.forEach((r, i) => {
                h += `<tr class="${o.rowCls ? o.rowCls(r) : ''}">` + this.cols.map(c => {
                    let formatted = '';
                    let rawVal = undefined;
                    if (typeof c.v === 'function') {
                        rawVal = c.v(r);
                    } else if (c.k) {
                        rawVal = r[c.k] !== undefined ? r[c.k] : (r[c.k.charAt(0).toUpperCase() + c.k.slice(1)] ?? r[c.k.toLowerCase()]);
                    }
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
                        formatted = typeof rawVal === 'string' && rawVal.endsWith('%') ? esc(rawVal) : pct(rawVal);
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
                if (c.totf) {
                    txt = c.totf(this.view);
                } else if (c.tot) {
                    const totalVal = this.view.reduce((sum, r) => sum + (Number(r[c.k] !== undefined ? r[c.k] : (r[c.k.charAt(0).toUpperCase() + c.k.slice(1)] ?? 0)) || 0), 0);
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

// Register centerText plugin for doughnut charts once
function ensureCenterTextPlugin() {
    if (!window.Chart || typeof Chart.register !== 'function') return;
    if (Chart.registry?.plugins?.get('centerText')) return;
    try {
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

                const mainText = String(opt.text || '');
                const subText = String(opt.sub || '');

                let fontSize = '17px';
                if (mainText.length > 16) fontSize = '12px';
                else if (mainText.length > 11) fontSize = '14.5px';

                ctx.font = `bold ${fontSize} Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`;
                ctx.fillStyle = cssVar('--ink') || '#0f172a';
                ctx.fillText(mainText, cx, subText ? cy - 9 : cy);

                if (subText) {
                    ctx.font = '500 11.5px Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
                    ctx.fillStyle = cssVar('--muted') || '#64748b';
                    ctx.fillText(subText, cx, cy + 13);
                }
                ctx.restore();
            }
        });
    } catch (e) {
        console.warn('centerText plugin registration error:', e);
    }
}
ensureCenterTextPlugin();

const CHART_CONTAINERS = {};

function mkChart(id, type, labels, datasets, opts = {}) {
    let cv = document.getElementById(id);
    let wrap = cv ? cv.parentNode : CHART_CONTAINERS[id];
    if (cv && !CHART_CONTAINERS[id]) {
        CHART_CONTAINERS[id] = cv.parentNode;
    }
    if (!wrap && id) {
        const found = document.querySelector(`.cwrap canvas#${id}`) || document.querySelector(`#${id}`);
        if (found) {
            wrap = found.parentElement;
            CHART_CONTAINERS[id] = wrap;
        }
    }
    if (!wrap) return;

    if (CH[id]) {
        CH[id].destroy();
        delete CH[id];
    }

    if (!window.Chart) {
        wrap.innerHTML = '<div class="empty">Chart library not loaded.</div>';
        return;
    }

    // Ensure canvas exists inside wrap
    if (!cv || !wrap.contains(cv)) {
        wrap.innerHTML = `<canvas id="${id}"></canvas>`;
        cv = document.getElementById(id);
    }

    // Handle empty data without destroying canvas
    const existingMsg = wrap.querySelector('.chart-empty-msg');
    if (!labels || !labels.length || !datasets || !datasets.length || datasets.every(d => !d.data || !d.data.length)) {
        if (cv) cv.style.display = 'none';
        if (!existingMsg) {
            const msg = document.createElement('div');
            msg.className = 'empty chart-empty-msg';
            msg.textContent = 'No data for selected filters.';
            wrap.appendChild(msg);
        } else {
            existingMsg.style.display = 'flex';
        }
        return;
    }

    // Has valid data: remove empty overlay and make canvas visible
    if (existingMsg) {
        existingMsg.remove();
    }
    if (cv) {
        cv.style.display = 'block';
    }

    const isPie = type === 'doughnut' || type === 'pie';
    const isHoriz = !!opts.horizontal;
    const catAxis = isHoriz ? 'y' : 'x';
    const valAxis = isHoriz ? 'x' : 'y';

    ensureCenterTextPlugin();

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
                    position: isPie ? (opts.legendPosition || 'right') : (opts.legendPosition || 'top'),
                    align: isPie ? (opts.legendAlign || 'center') : (opts.legendAlign || 'end'),
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
                            if (isPie) {
                                const ds = ctx.dataset;
                                const total = (ds.data || []).reduce((s, x) => s + (Number(x) || 0), 0);
                                const v = Number(ctx.raw) || 0;
                                const p = total ? (v / total * 100).toFixed(1) : 0;
                                return ` ${ctx.label}: ${opts.money ? formatMoney(v) : int(v)} (${p}%)`;
                            }
                            const val = isHoriz ? (ctx.parsed?.x ?? ctx.raw) : (ctx.parsed?.y ?? ctx.raw);
                            if (opts.money) return ` ${ctx.dataset.label || ctx.label || 'Amount'}: ${(CONFIG.currency || '৳')} ${formatMoney(val)}`;
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
                        callback: v => opts.money ? formatMoney(v) : int(v)
                    }
                }
            } : {
                x: {
                    stacked: !!opts.stacked,
                    grid: { display: false },
                    ticks: {
                        autoSkip: labels.length > 9,
                        maxRotation: 0,
                        font: { size: 10.5 },
                        callback: function (val) {
                            let l = String(this.getLabelForValue ? this.getLabelForValue(val) : val);
                            const m = opts.horizontal ? 26 : 18;
                            if (l.length > m) l = l.slice(0, m - 1) + '…';
                            return (!opts.horizontal && l.length > 9 && l.includes(' ')) ? l.split(' ') : l;
                        }
                    }
                },
                [valAxis]: {
                    stacked: !!opts.stacked,
                    beginAtZero: true,
                    grid: { color: '#eef2f8' },
                    ticks: {
                        font: { size: 10.5 },
                        callback: v => opts.money ? Number(v).toLocaleString('en-US', { maximumFractionDigits: 0 }) : int(v)
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

    if (isPie && opts.center) {
        chartConfig.options.cutout = opts.cutout || '68%';
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

const cur = v => (CONFIG.currency || '৳') + ' ' + amt(v);
window.cur = cur;

function getSelectVal(id) {
    const selector = id.startsWith('#') ? id : '#' + id;
    const el = document.querySelector(selector);
    if (!el) return '';
    if (window.jQuery) {
        const $el = $(el);
        const inst = $el.data('customDdInstance');
        if (inst && typeof inst.getValue === 'function') {
            const v = inst.getValue();
            if (Array.isArray(v)) return v.filter(Boolean).join(',');
            if (v !== null && v !== undefined && String(v).trim() !== '') return String(v).trim();
        }
        if (inst && inst.selected && inst.selected.length) {
            const v = inst.selected[0];
            if (v !== null && v !== undefined && String(v).trim() !== '') return String(v).trim();
        }
        const v = $el.val();
        if (Array.isArray(v)) return v.filter(Boolean).join(',');
        if (v !== null && v !== undefined) return String(v).trim();
    }
    return (el.value || '').trim();
}

function setSelectVal(selector, val) {
    if (!selector) return;
    const id = selector.startsWith('#') ? selector : '#' + selector;
    const $el = $(id);
    if (!$el.length) return;
    const inst = $el.data('customDdInstance');
    if (inst && typeof inst.setValue === 'function') {
        if (!val) {
            inst.clear();
        } else {
            inst.setValue(val);
        }
    } else {
        $el.val(val);
    }
}
window.setSelect = setSelectVal;

function getDateValue(id) {
    const el = document.getElementById(id);
    if (!el) return '';
    const fp = (window.jQuery ? $(el).data('customFpInstance') : null) || el._flatpickr;
    if (fp && fp.selectedDates && fp.selectedDates.length > 0) {
        const d = fp.selectedDates[0];
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        return `${d.getFullYear()}-${month}-${day}`;
    }
    const val = (el.value || '').trim();
    if (!val) return '';
    if (/^\d{4}-\d{2}-\d{2}$/.test(val)) return val;
    const parts = val.split('/');
    if (parts.length === 3) {
        return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
    }
    return val;
}

function getDateDisplay(id) {
    const el = document.getElementById(id);
    if (!el) return '';
    const fp = (window.jQuery ? $(el).data('customFpInstance') : null) || el._flatpickr;
    if (fp && fp.altInput && fp.altInput.value) {
        return fp.altInput.value.trim();
    }
    if (fp && fp.selectedDates && fp.selectedDates.length > 0) {
        const d = fp.selectedDates[0];
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        return `${day}/${month}/${d.getFullYear()}`;
    }
    const val = (el.value || '').trim();
    if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
        const p = val.split('-');
        return `${p[2]}/${p[1]}/${p[0]}`;
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
    const iso = `${year}-${month}-${day}`;
    const fp = (window.jQuery ? $(el).data('customFpInstance') : null) || el._flatpickr;
    if (fp) {
        fp.setDate(dateObj, false);
    } else {
        el.value = iso;
    }
}

// Global Filter State & Dispatcher
const DashboardApp = {
    currentTab: 'overview',

    getFilterState() {
        return {
            fromDate: getDateValue('fFrom') || null,
            toDate: getDateValue('fTo') || null,
            branchCode: getSelectVal('fBranch'),
            shipmentMode: getSelectVal('fMode'),
            port: getSelectVal('fPort'),
            customerID: getSelectVal('fCust'),
            jobStatus: getSelectVal('fStatus')
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
        if (tabId === 'jobs' && window.DashboardJobStatus) {
            setTimeout(() => {
                if (window.DashboardJobStatus.activeMode === 'Air' && window.DashboardJobStatus.airTable) {
                    window.DashboardJobStatus.airTable.columns.adjust();
                } else if (window.DashboardJobStatus.activeMode === 'Sea' && window.DashboardJobStatus.seaTable) {
                    window.DashboardJobStatus.seaTable.columns.adjust();
                }
            }, 100);
        }
    },

    updatePeriodDisplay() {
        const f = getDateDisplay('fFrom');
        const t = getDateDisplay('fTo');
        const txt = (f && t) ? `${f} – ${t}` : 'Active Period';
        d$$('[data-period]').forEach(sp => sp.textContent = txt);
    },

    setupFilterEvents() {
        let refreshTimer = null;
        const triggerRefresh = (delay = 0) => {
            clearTimeout(refreshTimer);
            if (delay > 0) {
                refreshTimer = setTimeout(() => {
                    this.updatePeriodDisplay();
                    this.loadActiveTab();
                }, delay);
            } else {
                this.updatePeriodDisplay();
                this.loadActiveTab();
            }
        };

        const onDateChanged = () => {
            setSelectVal('#fRange', 'custom');
            triggerRefresh(0);
        };

        const onRangeChanged = () => {
            const code = getSelectVal('fRange');
            if (code && code !== 'custom') {
                this.applyQuickRange(code);
            }
            triggerRefresh(0);
        };

        if (window.jQuery) {
            // Dropdowns change
            $(document).on('change', '#fBranch, #fMode, #fPort, #fCust, #fStatus', function () {
                triggerRefresh(0);
            });

            // Quick range change
            $(document).on('change', '#fRange', onRangeChanged);

            // Date inputs change
            $(document).on('change', '#fFrom, #fTo', onDateChanged);

            // Delegated click for custom-select-2 dropdown options and clear icons
            $(document).on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-clear-icon, .custom-dd-item', function () {
                const $wrap = $(this).closest('.custom-dd-wrapper');
                const origId = $wrap.prev('select').attr('id');
                if (origId === 'fRange') {
                    setTimeout(onRangeChanged, 30);
                } else {
                    triggerRefresh(30);
                }
            });

            // Flatpickr clear / today footer buttons
            $(document).on('click', '.custom-fp-today-btn, .custom-fp-clear-btn', function () {
                setTimeout(onDateChanged, 50);
            });

            // Manual typing in flatpickr alt-input
            $(document).on('input change blur', '.flatpickr-alt-input', function () {
                triggerRefresh(300);
            });
        }

        d$('#fFrom')?.addEventListener('change', onDateChanged);
        d$('#fTo')?.addEventListener('change', onDateChanged);
        d$('#fRange')?.addEventListener('change', onRangeChanged);
        d$('#fBranch')?.addEventListener('change', () => triggerRefresh(0));
        d$('#fMode')?.addEventListener('change', () => triggerRefresh(0));
        d$('#fPort')?.addEventListener('change', () => triggerRefresh(0));
        d$('#fCust')?.addEventListener('change', () => triggerRefresh(0));
        d$('#fStatus')?.addEventListener('change', () => triggerRefresh(0));

        d$('#btnReset')?.addEventListener('click', () => {
            this.resetFilters();
            triggerRefresh(0);
        });

        const hookFp = (id) => {
            const el = document.getElementById(id);
            if (!el) return;
            const fp = (window.jQuery ? $(el).data('customFpInstance') : null) || el._flatpickr;
            if (fp && fp.config && Array.isArray(fp.config.onChange)) {
                if (!fp.config.onChange.includes(onDateChanged)) {
                    fp.config.onChange.push(onDateChanged);
                }
            }
        };
        hookFp('fFrom');
        hookFp('fTo');
        setTimeout(() => {
            hookFp('fFrom');
            hookFp('fTo');
        }, 150);
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
        setSelectVal('#fRange', '3m');
        setSelectVal('#fBranch', '');
        setSelectVal('#fMode', '');
        setSelectVal('#fPort', '');
        setSelectVal('#fCust', '');
        setSelectVal('#fStatus', '');
    },

    getGPExportParams() {
        const filter = this.getFilterState();
        const selected = [];
        document.querySelectorAll('#g_g .gp-row-chk:checked').forEach(chk => {
            if (chk.value) selected.push(chk.value);
        });

        const params = new URLSearchParams();
        if (filter.fromDate) params.append('fromDate', filter.fromDate);
        if (filter.toDate) params.append('toDate', filter.toDate);
        if (filter.branchCode) params.append('branchCode', filter.branchCode);
        if (filter.shipmentMode) params.append('shipmentMode', filter.shipmentMode);
        if (filter.customerID) params.append('customerId', filter.customerID);
        if (selected.length > 0) params.append('selectedBillNos', selected.join(','));
        return params.toString();
    },

    setupExportMenu() {
        const menu = d$('#expMenu');
        d$('#btnExport')?.addEventListener('click', e => {
            e.stopPropagation();
            menu?.classList.toggle('open');
        });
        document.addEventListener('click', () => menu?.classList.remove('open'));

        d$('#xPreview')?.addEventListener('click', () => this.handleExportAction('Preview'));
        d$('#xPdf')?.addEventListener('click', () => this.handleExportAction('PDF'));
        d$('#xExcel')?.addEventListener('click', () => this.handleExportAction('Excel'));
    },

    handleExportAction(format) {
        if (this.currentTab === 'overview') {
            this.exportOverview(format);
            return;
        }
        if (this.currentTab === 'jobs' && window.DashboardJobStatus && typeof window.DashboardJobStatus.handleReportExport === 'function') {
            window.DashboardJobStatus.handleReportExport(format);
            return;
        }
        if (this.currentTab === 'gp') {
            const qs = this.getGPExportParams();
            if (format === 'Preview') {
                this.openReportPreviewModal('GP Analysis Report Preview', `/GPAnalysisReport/PreviewPdf?${qs}`);
            } else if (format === 'PDF') {
                window.location.href = `/GPAnalysisReport/ExportPdf?${qs}`;
            } else if (format === 'Excel') {
                window.location.href = `/GPAnalysisReport/ExportExcel?${qs}`;
            }
            return;
        }
        if (format === 'Preview') {
            this.exportPreview();
        } else if (format === 'PDF') {
            this.exportPdf();
        } else if (format === 'Excel') {
            this.exportExcel();
        }
    },

    exportOverview(format) {
        const filter = this.getFilterState();
        const params = new URLSearchParams();
        if (filter.fromDate) params.append('fromDate', filter.fromDate);
        if (filter.toDate) params.append('toDate', filter.toDate);
        if (filter.branchCode) params.append('branchCode', filter.branchCode);
        if (filter.shipmentMode) params.append('shipmentMode', filter.shipmentMode);
        if (filter.port) params.append('port', filter.port);
        if (filter.customerID) params.append('customerID', filter.customerID);
        if (filter.jobStatus) params.append('jobStatus', filter.jobStatus);

        const fmt = (format || 'preview').toLowerCase();
        if (fmt === 'preview') {
            params.append('exportFormat', 'PDF');
            params.append('download', 'false');
            const url = `/DashboardOverview/Export?${params.toString()}`;
            this.openReportPreviewModal('Executive Overview Report Preview', url);
        } else if (fmt === 'pdf') {
            params.append('exportFormat', 'PDF');
            params.append('download', 'true');
            window.location.href = `/DashboardOverview/Export?${params.toString()}`;
        } else if (fmt === 'excel') {
            params.append('exportFormat', 'Excel');
            window.location.href = `/DashboardOverview/Export?${params.toString()}`;
        }
    },

    openReportPreviewModal(title, url) {
        const $iframe = $('#reportPreviewIframe');
        const $spinner = $('#modalHeaderSpinner');
        const $title = $('#reportPreviewModalLabel');
        const $newTab = $('#btnModalOpenNewTab');

        if ($title.length) {
            $title.html('<i class="fas fa-file-pdf text-danger me-2"></i>' + this.escapeHtml(title || 'Report Preview'));
        }
        if ($newTab.length) {
            $newTab.attr('href', url);
        }
        if ($spinner.length) {
            $spinner.show();
        }

        const modalEl = document.getElementById('reportPreviewModal');
        if (modalEl && typeof bootstrap !== 'undefined') {
            const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
            modalInstance.show();
        }

        if ($iframe.length) {
            $iframe.attr('src', url);
            $iframe.off('load').on('load', function () {
                if ($spinner.length) $spinner.hide();
            });
            setTimeout(function () {
                if ($spinner.length) $spinner.hide();
            }, 1200);
        }
    },

    escapeHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    },

    exportPreview() {
        if (this.currentTab === 'gp') {
            const qs = this.getGPExportParams();
            window.open(`/GPAnalysisReport/PreviewPdf?${qs}`, '_blank');
        } else {
            window.print();
        }
    },

    exportPdf() {
        if (this.currentTab === 'gp') {
            const qs = this.getGPExportParams();
            window.location.href = `/GPAnalysisReport/ExportPdf?${qs}`;
        } else {
            if (!window.jspdf) {
                alert('PDF library is loading. Please try again.');
                return;
            }
            window.print();
        }
    },

    exportExcel() {
        if (this.currentTab === 'gp') {
            const qs = this.getGPExportParams();
            window.location.href = `/GPAnalysisReport/ExportExcel?${qs}`;
            return;
        }

        if (!window.XLSX) {
            alert('Excel library not loaded.');
            return;
        }
        const activePanel = document.querySelector('.panel.on');
        const table = activePanel?.querySelector('table.custom-grid-table:visible, table.grid, table');
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

