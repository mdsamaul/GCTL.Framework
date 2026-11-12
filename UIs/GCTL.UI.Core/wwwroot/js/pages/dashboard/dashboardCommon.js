/**
 * C&F Management Dashboard - Common Core Module
 * Handles global state, filters, tab switching, reusable Grid component, Chart.js wrappers, and export routines.
 */
'use strict';

const CONFIG = {
    title: 'C&F Management Dashboard',
    company: 'Formosa Logistics Limited',
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

// Date formatting helper DD/MM/YYYY
const dmy = v => {
    if (!v) return '';
    if (typeof v === 'string') {
        const trimmed = v.trim();
        if (/^\d{2}\/\d{2}\/\d{4}/.test(trimmed)) return trimmed.slice(0, 10);
        if (/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
            const p = trimmed.slice(0, 10).split('-');
            return `${p[2]}/${p[1]}/${p[0]}`;
        }
    }
    const d = new Date(v);
    if (isNaN(d.getTime())) return String(v);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    return `${day}/${month}/${d.getFullYear()}`;
};
window.dmy = dmy;

const isNum = c => ['money', 'amt', 'pct', 'int'].includes(c?.t);
const plain = s => String(s ?? '').replace(/<[^>]+>/g, '').replace(/&amp;/g, '&');
function fmtCell(c, r, i, forExport) {
    if (!c) return '';
    if (c.x) return c.x(r, i);
    if (c.t === 'sn') return String(i + 1);
    const v = c.v ? c.v(r) : (r[c.k] !== undefined ? r[c.k] : (r[c.k?.charAt(0).toUpperCase() + c.k?.slice(1)] ?? r[c.k?.toLowerCase()]));
    if (c.r && !['money', 'amt', 'int', 'pct', 'date'].includes(c.t)) {
        return plain(c.r(r, i));
    }
    switch (c.t) {
        case 'int': return int(v);
        case 'money': return (v || v === 0) ? (v === 0 && !forExport ? '–' : amt(v)) : '';
        case 'amt': return (v === 0 && !forExport ? '–' : amt(v));
        case 'pct': return pct(v, 2);
        case 'date': return dmy(v);
        default: return String(v ?? '');
    }
}
const cellTxt = (c, r, i) => c.t === 'badge' ? String(r[c.k] ?? '') : fmtCell(c, r, i, true);
const getCompanyName = () => document.querySelector('.sidebar-brand-text')?.textContent?.trim() || CONFIG.company || 'Formosa Logistics Limited';

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
window.GRIDS = GRIDS;

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
    const vEl = el.querySelector('.kval') || el.querySelector('.mval') || el;
    if (vEl) vEl.textContent = val;
    if (sub !== null) {
        const sEl = el.querySelector('.ksub') || el.querySelector('.msub');
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
    if (fp && typeof fp.setDate === 'function') {
        fp.setDate(dateObj, false);
    }
    el.value = dmy;
    if (el.nextElementSibling && el.nextElementSibling.classList.contains('flatpickr-alt-input')) {
        el.nextElementSibling.value = dmy;
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
        const rawHash = (window.location.hash || '').replace('#', '').trim();
        let hashTab = rawHash;
        let hashSubTab = null;

        if (rawHash.includes('/')) {
            const parts = rawHash.split('/');
            hashTab = parts[0];
            hashSubTab = parts[1];
        } else if (rawHash.startsWith('accounts-') || rawHash.startsWith('accounts_')) {
            hashTab = 'accounts';
            hashSubTab = rawHash.substring(9);
        }

        const storedTab = sessionStorage.getItem('dashboard_active_tab');
        const initTab = d$('#tabs')?.dataset?.initialTab;

        const validTabs = ['overview', 'jobs', 'bills', 'gp', 'recon', 'accounts'];
        if (hashTab && validTabs.includes(hashTab)) {
            this.currentTab = hashTab;
            if (hashTab === 'accounts' && hashSubTab) {
                const normalizedSub = (hashSubTab === 'summary') ? 'cb' : hashSubTab;
                sessionStorage.setItem('dashboard_accounts_subtab', normalizedSub);
            }
        } else if (storedTab && validTabs.includes(storedTab)) {
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
        
        let hashUrl = '#' + tabId;
        if (tabId === 'accounts') {
            const sub = sessionStorage.getItem('dashboard_accounts_subtab') || 'cb';
            hashUrl = '#accounts/' + sub;
        }

        if (window.history && window.history.replaceState) {
            window.history.replaceState(null, '', hashUrl);
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

    activePanel() {
        if (this.currentTab === 'accounts') {
            return document.querySelector('#p-accounts .sub.on') || document.querySelector('#p-accounts');
        }
        return document.querySelector('#p-' + this.currentTab) || document.querySelector('.panel.on') || document.body;
    },

    reportTitle() {
        if (this.currentTab === 'accounts') {
            const sub = document.querySelector('#subnav button.on')?.textContent?.trim() || 'Cash & Bank Summary';
            return 'Accounts – ' + sub;
        }
        const tabBtn = document.querySelector('.tabs .tab.on');
        return tabBtn ? tabBtn.textContent.trim() : 'Dashboard';
    },

    filterLines() {
        const fFrom = getDateValue('fFrom') || '';
        const fTo = getDateValue('fTo') || '';
        const period = (fFrom && fTo) ? `${fFrom} – ${fTo}` : (fTo ? `As on ${fTo}` : 'All time');

        const bSel = document.getElementById('fBranch');
        const branch = (bSel && bSel.value) ? (bSel.options[bSel.selectedIndex]?.text || 'All Branches') : 'All Branches';

        const L = [
            ['Period', period],
            ['Branch', branch]
        ];

        const mSel = document.getElementById('fMode');
        const pSel = document.getElementById('fPort');
        const cSel = document.getElementById('fCust');
        const sSel = document.getElementById('fStatus');

        if (this.currentTab === 'recon') {
            return [
                ['Statement', 'Job number wise reconcile (Actual)'],
                ['Branch', branch],
                ['Shipment Mode', (mSel && mSel.value) ? mSel.options[mSel.selectedIndex]?.text : 'All Modes'],
                ['Port', (pSel && pSel.value) ? pSel.options[pSel.selectedIndex]?.text : 'All Ports'],
                ['Customer', (cSel && cSel.value) ? cSel.options[cSel.selectedIndex]?.text : 'All Customers']
            ];
        }

        if (this.currentTab !== 'accounts') {
            L.push(
                ['Shipment Mode', (mSel && mSel.value) ? mSel.options[mSel.selectedIndex]?.text : 'All Modes'],
                ['Port', (pSel && pSel.value) ? pSel.options[pSel.selectedIndex]?.text : 'All Ports'],
                ['Customer', (cSel && cSel.value) ? cSel.options[cSel.selectedIndex]?.text : 'All Customers'],
                ['Job Status', (sSel && sSel.value) ? sSel.options[sSel.selectedIndex]?.text : 'All Status']
            );
        }
        return L;
    },

    collectExport() {
        const p = this.activePanel();
        if (!p) {
            return {
                title: this.reportTitle(),
                company: getCompanyName(),
                kpis: [],
                charts: [],
                tables: [],
                filters: this.filterLines()
            };
        }

        // 1. KPIs & Mini cards
        const kpis = [...p.querySelectorAll('.kpi, .mini')].map(e => {
            const lbl = e.querySelector('.klabel, .mlbl')?.textContent?.trim() || '';
            const val = e.querySelector('.kval, .mval')?.textContent?.trim() || '';
            const sub = e.querySelector('.ksub, .msub')?.textContent?.trim() || '';
            return [lbl, val, sub];
        }).filter(x => x[0] || x[1]);

        // 2. Charts
        const charts = [...p.querySelectorAll('canvas')].map(c => {
            let img = null;
            try {
                if (CH[c.id] && typeof CH[c.id].toBase64Image === 'function') {
                    img = CH[c.id].toBase64Image('image/png', 1);
                } else if (typeof c.toDataURL === 'function') {
                    img = c.toDataURL('image/png');
                }
            } catch (err) {
                console.warn('Canvas to image failed for', c.id, err);
            }
            if (!img) return null;
            const card = c.closest('.card');
            const title = card?.querySelector('h3')?.textContent?.trim() || 'Chart';
            return {
                title,
                img,
                w: c.width || 400,
                h: c.height || 200
            };
        }).filter(Boolean);

        // 3. Tables (GRIDS first, DOM table fallback)
        let tables = [...p.querySelectorAll('.gbody > div[id]')].filter(d => GRIDS[d.id]).map(d => {
            const g = GRIDS[d.id];
            const card = d.closest('.card');
            const name = card?.querySelector('h3')?.textContent?.replace(/\s+/g, ' ').trim() || 'Data Table';
            const rows = g.q ? g.view : (g.rows && g.rows.length ? g.rows : g.view);
            return {
                name,
                g,
                cols: g.cols,
                rows: rows,
                xcols: g.o?.xcols || g.cols
            };
        });

        if (!tables.length) {
            const domTables = [...p.querySelectorAll('table')].filter(t => t.offsetParent !== null || t.rows.length > 0);
            tables = domTables.map((t, idx) => {
                const card = t.closest('.card') || t.closest('.panel') || p;
                const name = card?.querySelector('h3')?.textContent?.replace(/\s+/g, ' ').trim() || `Table ${idx + 1}`;
                const ths = [...t.querySelectorAll('thead th')];
                const cols = ths.length ? ths.map((th, ci) => ({
                    l: th.textContent.trim(),
                    k: `col_${ci}`,
                    t: th.classList.contains('num') ? 'amt' : 'text'
                })) : [];
                const trs = [...t.querySelectorAll('tbody tr')].filter(tr => !tr.classList.contains('empty-row'));
                const rows = trs.map((tr, ri) => {
                    const cells = [...tr.querySelectorAll('td')];
                    const obj = {};
                    cells.forEach((td, ci) => {
                        const key = cols[ci] ? cols[ci].k : `col_${ci}`;
                        obj[key] = td.textContent.trim();
                    });
                    return obj;
                });
                return {
                    name,
                    g: { o: {} },
                    cols: cols,
                    rows: rows,
                    xcols: cols
                };
            });
        }

        return {
            title: this.reportTitle(),
            company: getCompanyName(),
            kpis: kpis,
            charts: charts,
            tables: tables,
            filters: this.filterLines()
        };
    },

    exportPreview() {
        const X = this.collectExport();
        const w = window.open('', '_blank');
        if (!w) {
            alert('Please allow pop-ups to open the print preview.');
            return;
        }

        const kp = X.kpis.map(k => `
            <div class="k">
                <small>${esc(k[0])}</small>
                <b>${esc(k[1])}</b>
                <span>${esc(k[2])}</span>
            </div>
        `).join('');

        const ch = X.charts.map(c => `
            <figure>
                <figcaption>${esc(c.title)}</figcaption>
                <img src="${c.img}">
            </figure>
        `).join('');

        const tb = X.tables.map(t => {
            let tfootHtml = '';
            if (t.g?.o?.totals && t.rows.length) {
                tfootHtml = '<tfoot><tr class="total">' + t.cols.map((c, i) => {
                    let txt = '';
                    if (i === 0) txt = 'Total';
                    if (c.totf) {
                        txt = c.totf(t.rows);
                    } else if (c.tot) {
                        const sum = t.rows.reduce((acc, r) => {
                            const raw = r[c.k] !== undefined ? r[c.k] : (r[c.k?.charAt(0).toUpperCase() + c.k?.slice(1)] ?? 0);
                            return acc + (Number(raw) || 0);
                        }, 0);
                        txt = amt(sum);
                    }
                    return `<td class="${isNum(c) ? 'n' : ''}">${esc(txt)}</td>`;
                }).join('') + '</tr></tfoot>';
            }

            return `
                <h3>${esc(t.name)} <small>(${int(t.rows.length)} rows)</small></h3>
                <table>
                    <thead>
                        <tr>${t.cols.map(c => `<th>${c.l}</th>`).join('')}</tr>
                    </thead>
                    <tbody>
                        ${t.rows.map((r, i) => `
                            <tr class="${t.g?.o?.rowCls ? t.g.o.rowCls(r) : ''}">
                                ${t.cols.map(c => `<td class="${isNum(c) ? 'n' : ''}">${esc(cellTxt(c, r, i))}</td>`).join('')}
                            </tr>
                        `).join('')}
                    </tbody>
                    ${tfootHtml}
                </table>
            `;
        }).join('');

        const htmlContent = `<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>${esc(X.title)} - ${esc(X.company)}</title>
    <style>
        @page { size: A4 landscape; margin: 10mm; }
        body { font: 11px/1.4 "Segoe UI", Arial, sans-serif; color: #1a2740; margin: 0; padding: 16px; background: #fff; }
        .bar { position: sticky; top: 0; background: #0a2f6c; color: #fff; padding: 8px 14px; margin: -16px -16px 14px; display: flex; justify-content: space-between; align-items: center; z-index: 1000; box-shadow: 0 2px 6px rgba(0,0,0,0.2); }
        .bar button { background: #fff; color: #0a2f6c; border: 0; border-radius: 6px; padding: 7px 18px; font-weight: 700; cursor: pointer; font-size: 12px; }
        .bar button:hover { background: #eef3fb; }
        h1 { font-size: 18px; margin: 0; color: #0a2f6c; }
        h2 { font-size: 12px; margin: 2px 0 8px; color: #546580; font-weight: 500; }
        h3 { font-size: 12px; margin: 14px 0 6px; color: #0a2f6c; font-weight: 600; }
        h3 small { color: #6a7a91; font-weight: 400; }
        .f { display: flex; flex-wrap: wrap; gap: 4px 18px; margin-bottom: 12px; font-size: 10.5px; background: #f8fafc; padding: 6px 10px; border-radius: 6px; border: 1px solid #e2e8f0; }
        .f span b { color: #0a2f6c; }
        .ks { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 8px; margin-bottom: 12px; }
        .k { border: 1px solid #dfe6f1; border-left: 4px solid #0b57d0; border-radius: 6px; padding: 6px 8px; background: #fff; }
        .k small { display: block; text-transform: uppercase; font-size: 8.5px; color: #6a7a91; letter-spacing: .4px; font-weight: 600; }
        .k b { font-size: 13px; color: #1a2740; display: block; margin: 2px 0; }
        .k span { display: block; font-size: 9px; color: #6a7a91; }
        .cs { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-bottom: 10px; }
        figure { margin: 0; border: 1px solid #dfe6f1; border-radius: 6px; padding: 6px; break-inside: avoid; background: #fff; }
        figcaption { font-weight: 700; font-size: 10px; margin-bottom: 3px; color: #33445f; }
        img { width: 100%; height: auto; display: block; }
        table { width: 100%; border-collapse: collapse; font-size: 9.5px; margin-bottom: 10px; }
        th { background: #eef3fb; border: 1px solid #d5deec; padding: 5px 6px; text-align: center; font-weight: 650; color: #0a2f6c; }
        td { border: 1px solid #e4eaf3; padding: 4px 6px; }
        td.n { text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
        tr { break-inside: avoid; }
        thead { display: table-header-group; }
        tfoot tr.total td { font-weight: 700; background: #eef3fb; border-top: 2px solid #b7c9e6; }
        tr.l1 td, tr.total td { font-weight: 700; background: #eef3fb; }
        tr.l2 td, tr.sub td, tr.head td { font-weight: 700; background: #f6f8fc; }
        @media print {
            .bar { display: none !important; }
            body { padding: 0 !important; }
        }
    </style>
</head>
<body>
    <div class="bar">
        <span>Print preview – use Ctrl+P / “Save as PDF”</span>
        <button onclick="window.print()">Print</button>
    </div>
    <h1>${esc(X.company)}</h1>
    <h2>${esc(X.title)} · generated ${new Date().toLocaleString('en-GB')}</h2>
    <div class="f">
        ${X.filters.map(f => `<span>${esc(f[0])}: <b>${esc(f[1])}</b></span>`).join('')}
    </div>
    <div class="ks">${kp}</div>
    <div class="cs">${ch}</div>
    ${tb}
</body>
</html>`;
        w.document.open();
        w.document.write(htmlContent);
        w.document.close();
    },

    exportPdf() {
        if (!window.jspdf || !window.jspdf.jsPDF) {
            alert('PDF library not loaded – check your internet connection.');
            return;
        }
        const X = this.collectExport();
        const doc = new window.jspdf.jsPDF({ orientation: 'landscape', unit: 'pt', format: 'a4' });
        const W = doc.internal.pageSize.getWidth();
        const H = doc.internal.pageSize.getHeight();
        const M = 28;

        const bd = s => String(s ?? '').replace(/৳/g, 'BDT ').replace(/[^\x00-\xFF–—]/g, '');

        // Top Navy Bar
        doc.setFillColor(10, 47, 108);
        doc.rect(0, 0, W, 46, 'F');
        doc.setTextColor(255);
        doc.setFontSize(15);
        doc.setFont(undefined, 'bold');
        doc.text(bd(X.company), M, 21);

        doc.setFontSize(10);
        doc.setFont(undefined, 'normal');
        doc.text(bd(X.title) + '   |   Generated ' + new Date().toLocaleString('en-GB'), M, 37);

        // Filters
        doc.setTextColor(40);
        doc.setFontSize(8.5);
        doc.text(bd(X.filters.map(f => f[0] + ': ' + f[1]).join('    ')), M, 62);

        let y = 72;
        // KPIs Grid (4 per row)
        if (X.kpis.length > 0) {
            const per = 4;
            const rows = [];
            for (let i = 0; i < X.kpis.length; i += per) {
                rows.push(X.kpis.slice(i, i + per));
            }
            doc.autoTable({
                startY: y,
                theme: 'grid',
                styles: { fontSize: 8, cellPadding: 4 },
                body: rows.map(r => {
                    const a = [];
                    r.forEach(k => {
                        a.push({
                            content: bd(k[0]),
                            styles: { fontStyle: 'bold', fillColor: [238, 243, 251], textColor: [60, 75, 100] }
                        }, bd(k[1]));
                    });
                    return a;
                }),
                margin: { left: M, right: M }
            });
            y = doc.lastAutoTable.finalY + 10;
        }

        // Charts (up to 3 per row)
        if (X.charts.length > 0) {
            const cw = (W - 2 * M - 20) / 3;
            let x = M, rowH = 0;
            X.charts.forEach(c => {
                const h = Math.min(cw * c.h / c.w, 140);
                if (x + cw > W - M + 1) {
                    x = M;
                    y += rowH + 8;
                    rowH = 0;
                }
                if (y + h + 14 > H - M) {
                    doc.addPage();
                    y = M;
                    x = M;
                    rowH = 0;
                }
                doc.setFontSize(8);
                doc.setFont(undefined, 'bold');
                doc.setTextColor(10, 47, 108);
                doc.text(bd(c.title), x, y + 8);
                try {
                    doc.addImage(c.img, 'PNG', x, y + 11, cw, h);
                } catch (err) {
                    console.warn('PDF addImage error:', err);
                }
                rowH = Math.max(rowH, h + 14);
                x += cw + 10;
            });
            y += rowH + 10;
        }

        // Tables
        X.tables.forEach(t => {
            const head = [t.cols.map(c => bd(plain(c.l)))];
            const body = t.rows.map((r, i) => t.cols.map(c => bd(cellTxt(c, r, i))));

            let foot = undefined;
            if (t.g?.o?.totals && t.rows.length) {
                const footRow = t.cols.map((c, i) => {
                    if (i === 0) return 'Total';
                    if (c.totf) return bd(c.totf(t.rows));
                    if (c.tot) {
                        const sum = t.rows.reduce((acc, r) => {
                            const raw = r[c.k] !== undefined ? r[c.k] : (r[c.k?.charAt(0).toUpperCase() + c.k?.slice(1)] ?? 0);
                            return acc + (Number(raw) || 0);
                        }, 0);
                        return bd(amt(sum));
                    }
                    return '';
                });
                foot = [footRow];
            }

            if (y > H - 80) {
                doc.addPage();
                y = M;
            }
            doc.setFontSize(10);
            doc.setFont(undefined, 'bold');
            doc.setTextColor(10, 47, 108);
            doc.text(bd(t.name) + ' (' + t.rows.length + ' rows)', M, y + 8);

            doc.autoTable({
                startY: y + 14,
                head: head,
                body: body,
                foot: foot,
                theme: 'grid',
                styles: { fontSize: 6.8, cellPadding: 2.2, overflow: 'linebreak' },
                headStyles: { fillColor: [10, 47, 108], halign: 'center' },
                footStyles: { fillColor: [223, 234, 249], textColor: [10, 47, 108], fontStyle: 'bold' },
                margin: { left: M, right: M },
                columnStyles: Object.fromEntries(t.cols.map((c, i) => [i, { halign: isNum(c) ? 'right' : 'left' }])),
                didParseCell: d => {
                    if (d.section === 'body' && t.g?.o?.rowCls) {
                        const cl = t.g.o.rowCls(t.rows[d.row.index]);
                        if (/l1|l2|total|head|sub/.test(cl)) d.cell.styles.fontStyle = 'bold';
                        if (/l1|total/.test(cl)) d.cell.styles.fillColor = [223, 234, 249];
                    }
                }
            });
            y = doc.lastAutoTable.finalY + 16;
        });

        // Page Numbers
        const totalPages = doc.internal.getNumberOfPages();
        for (let i = 1; i <= totalPages; i++) {
            doc.setPage(i);
            doc.setFontSize(7.5);
            doc.setTextColor(120);
            doc.text('Page ' + i + ' of ' + totalPages, W - M, H - 12, { align: 'right' });
            doc.text(bd(X.company), M, H - 12);
        }

        const toDate = (document.getElementById('fTo')?.value || new Date().toISOString().slice(0, 10)).replace(/[^\w]+/g, '-');
        doc.save(X.title.replace(/[^\w]+/g, '_') + '_' + toDate + '.pdf');
    },

    exportExcel() {
        if (!window.XLSX) {
            alert('Excel library not loaded – check your internet connection.');
            return;
        }
        const X = this.collectExport();
        const wb = XLSX.utils.book_new();

        // Sheet 1: Summary
        const sm = [
            [X.company],
            [X.title],
            ['Generated', new Date().toLocaleString('en-GB')],
            [],
            ...X.filters.map(f => [f[0], f[1]]),
            [],
            ['Indicator', 'Value', 'Note'],
            ...X.kpis.map(k => [k[0], k[1], k[2]])
        ];
        const ws0 = XLSX.utils.aoa_to_sheet(sm);
        ws0['!cols'] = [{ wch: 34 }, { wch: 28 }, { wch: 46 }];
        XLSX.utils.book_append_sheet(wb, ws0, 'Summary');

        // Data Sheets
        X.tables.forEach(t => {
            t = { ...t, cols: t.xcols || t.cols };
            const aoa = [t.cols.map(c => plain(c.l))];

            t.rows.forEach((r, i) => {
                aoa.push(t.cols.map(c => {
                    if (c.t === 'sn') return i + 1;
                    if (c.x && !isNum(c)) return c.x(r, i);
                    const rawVal = r[c.k] !== undefined ? r[c.k] : (r[c.k?.charAt(0).toUpperCase() + c.k?.slice(1)] ?? r[c.k?.toLowerCase()]);
                    if (c.t === 'badge') return rawVal ?? '';
                    const v = c.v ? c.v(r) : rawVal;
                    if (c.r && c.x && c.t !== 'money' && c.t !== 'pct') return c.x(r, i);
                    if (c.t === 'date') return dmy(v);
                    if (['money', 'amt', 'int'].includes(c.t)) return (v == null || v === '') ? null : +v;
                    if (c.t === 'pct') return +(+v).toFixed(2);
                    if (c.x) return c.x(r, i);
                    return v != null ? v : '';
                }));
            });

            if (t.g?.o?.totals && t.rows.length) {
                const totRow = t.cols.map((c, ci) => {
                    if (ci === 0) return 'Total';
                    if (c.tot) {
                        const sum = t.rows.reduce((acc, r) => {
                            const raw = r[c.k] !== undefined ? r[c.k] : (r[c.k?.charAt(0).toUpperCase() + c.k?.slice(1)] ?? 0);
                            return acc + (Number(raw) || 0);
                        }, 0);
                        return sum;
                    }
                    return '';
                });
                aoa.push(totRow);
            }

            const ws = XLSX.utils.aoa_to_sheet(aoa);
            ws['!cols'] = t.cols.map(c => ({
                wch: Math.max(10, Math.min(42, plain(c.l).length + 6, c.t === 'text' || !c.t ? 40 : 16))
            }));

            for (let R = 1; R < aoa.length; R++) {
                t.cols.forEach((c, ci) => {
                    const a = XLSX.utils.encode_cell({ r: R, c: ci });
                    if (ws[a] && ws[a].t === 'n') {
                        ws[a].z = c.t === 'int' ? '#,##0' : c.t === 'pct' ? '0.00' : '#,##0.00';
                    }
                });
            }

            const sheetTitle = t.name.replace(/[\\\/?*\[\]:]/g, ' ').slice(0, 31);
            XLSX.utils.book_append_sheet(wb, ws, sheetTitle);
        });

        const toDate = (document.getElementById('fTo')?.value || new Date().toISOString().slice(0, 10)).replace(/[^\w]+/g, '-');
        XLSX.writeFile(wb, X.title.replace(/[^\w]+/g, '_') + '_' + toDate + '.xlsx');
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

window.DashboardApp = DashboardApp;
window.exportPreview = () => DashboardApp.exportPreview();
window.exportPdf = () => DashboardApp.exportPdf();
window.exportExcel = () => DashboardApp.exportExcel();
window.collectExport = () => DashboardApp.collectExport();

document.addEventListener('DOMContentLoaded', () => {
    DashboardApp.init();
});

