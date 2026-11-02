/**

 * EXPENSE HEAD SETUP DASHBOARD (Formosa Logistics Limited / C&F Management)

 * Millisecond In-Memory Execution + Select2 + Flatpickr + Stored Procedure Sync

 */



(function ($) {

    "use strict";



    /* ================= CONFIG ================= */

    const CONFIG = {

        company: "Formosa Logistics Limited",

        address: "21, Hazaribagh, Dhaka-1209",

        reportTitle: "Expense Head Setup Report",

        defaultPageSize: 15

    };



    /* ================= STATE ================= */

    const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));



    let rows = [];

    let shipmentModes = [];

    let serviceProviders = [];

    let editing = null;

    let manualSerialEntry = false;

    let fpFrom = null;

    let fpTo = null;



    let S = {

        mode: "",

        svc: "",

        rec: "",

        sta: "",

        fromDate: null,

        toDate: null,

        q: "",

        sort: "",

        dir: 1,

        page: 1,

        size: CONFIG.defaultPageSize,

        sel: new Set()

    };



    /* ================= TOAST NOTIFICATION ================= */

    function toast(m) {

        const $t = $("#toast");

        if (!$t.length) return;

        $t.text(m).addClass("show");

        clearTimeout(toast.t);

        toast.t = setTimeout(() => $t.removeClass("show"), 2600);

    }



    /* ================= FLATPICKR INITIALIZATION ================= */

    function initDatePickers() {

        if (typeof flatpickr === "undefined") return;



        const fpConfig = {

            dateFormat: "d/m/Y",

            allowInput: true

        };



        if ($("#fFromDate").length) {
            fpFrom = flatpickr("#fFromDate", Object.assign({}, fpConfig, {
                onChange: function (dates) {
                    if (isProgrammaticFilterChange) return;
                    S.fromDate = dates[0] || null;
                    if (dates.length > 0 && fpTo) {
                        fpTo.set("minDate", dates[0]);
                    } else if (fpTo) {
                        fpTo.set("minDate", null);
                    }
                    S.page = 1;
                    render();
                }
            }));
        }

        if ($("#fToDate").length) {
            fpTo = flatpickr("#fToDate", Object.assign({}, fpConfig, {
                onChange: function (dates) {
                    if (isProgrammaticFilterChange) return;
                    S.toDate = dates[0] || null;
                    S.page = 1;
                    render();
                }
            }));
        }
    }

        /* ================= CUSTOM DROPDOWN (custom-select-single) ================= */

    let isProgrammaticFilterChange = false;

    function initCustomDropdownEvents() {

        $("#fMode").off("change.eh").on("change.eh", function () {

            if (isProgrammaticFilterChange) return;

            const val = ($(this).val() || "").toString().trim();

            if (S.mode !== val) {

                S.mode = val;

                S.page = 1;

                render();

            }

        });



        $("#fSvc").off("change.eh").on("change.eh", function () {

            if (isProgrammaticFilterChange) return;

            const val = ($(this).val() || "").toString().trim();

            if (S.svc !== val) {

                S.svc = val;

                S.page = 1;

                render();

            }

        });



        $("#fRec").off("change.eh").on("change.eh", function () {

            if (isProgrammaticFilterChange) return;

            const val = ($(this).val() || "").toString().trim();

            if (S.rec !== val) {

                S.rec = val;

                S.page = 1;

                render();

            }

        });



        $("#fSta").off("change.eh").on("change.eh", function () {

            if (isProgrammaticFilterChange) return;

            const val = ($(this).val() || "").toString().trim();

            if (S.sta !== val) {

                S.sta = val;

                S.page = 1;

                render();

            }

        });



        $("#eMode, #eSvc").off("change.ehSerial").on("change.ehSerial", function () {

            if (!manualSerialEntry) suggestSerialNo();

        });

    }



    /* ================= ROW DATA PARSER ================= */

    function parseAndSetRows(data) {

        if (!Array.isArray(data)) return;

        rows = data.map(r => {

            const tc = r.tc || r.TC || 0;

            const id = String(r.expenseHeadId || r.expenseHeadID || r.ExpenseHeadId || r.ExpenseHeadID || r.id || r.Id || "").trim();

            const name = String(r.expenseHead || r.ExpenseHead || r.name || r.Name || "").trim();

            const short = String(r.shortName || r.ShortName || r.short || r.Short || "").trim();

            const serial = String(r.serialNo || r.SerialNo || r.serial || r.Serial || "").trim();

            const isRec = (r.rec === 1 || r.Rec === 1 || r.isReceiptable === "Y" || r.isReceiptable === "Yes" || r.IsReceiptable === "Y" || r.IsReceiptable === "Yes");

            const svc = String(r.serviceTypeName || r.ServiceTypeName || r.svc || r.Svc || "").trim();

            const serviceTypeId = String(r.serviceTypeId || r.serviceTypeID || r.ServiceTypeId || r.ServiceTypeID || "").trim();

            const mode = String(r.expenseType || r.ExpenseType || r.mode || r.Mode || "").trim();

            const expenseTypeId = String(r.expenseTypeId || r.expenseTypeID || r.ExpenseTypeId || r.ExpenseTypeID || "").trim();

            const amt = (r.amount !== null && r.amount !== undefined && r.amount !== "") ? Number(r.amount) : ((r.amt !== null && r.amt !== undefined && r.amt !== "") ? Number(r.amt) : "");

            const status = String(r.status || r.Status || r.entryType || r.EntryType || "Active").trim() || "Active";



            return {

                tc,

                id,

                name,

                short,

                serial,

                rec: isRec ? 1 : 0,

                svc,

                serviceTypeId,

                mode,

                expenseTypeId,

                amt,

                status,

                ldate: r.ldate || r.LDate || r.Ldate || null,

                modifyDate: r.modifyDate || r.ModifyDate || null

            };

        });

    }



    /* ================= DATA LOADING ================= */

    function loadData() {

        const startTime = performance.now();

        if (rows.length === 0) {

            $("#cnt").text("Loading...");

        }



        $.ajax({

            url: "/ExpenseHead/GetAllData",

            type: "GET",

            dataType: "json",

            success: function (res) {

                const elapsed = (performance.now() - startTime).toFixed(1);

                if (res && res.success && Array.isArray(res.data)) {

                    if (Array.isArray(res.shipmentModes) && res.shipmentModes.length) shipmentModes = res.shipmentModes;

                    if (Array.isArray(res.serviceTypes) && res.serviceTypes.length) serviceProviders = res.serviceTypes;

                    parseAndSetRows(res.data);

                    fillSelects();

                    render();

                    toast(`Loaded ${rows.length} expense heads (${elapsed} ms)`);

                } else if (rows.length === 0) {

                    $("#tb").html('<tr><td colspan="11" class="eh-empty">No expense heads found.</td></tr>');

                    $("#cnt").text("0 records");

                    $("#info").text("No records available");

                }

            },

            error: function (xhr, status, err) {

                console.error("Failed to load expense heads:", xhr.responseText || err);

                if (rows.length === 0) {

                    $("#tb").html('<tr><td colspan="11" class="eh-empty" style="color:var(--danger)">Failed to load data from server. Please refresh or check connection.</td></tr>');

                    $("#cnt").text("Error");

                    $("#info").text("Error loading data");

                }

            }

        });

    }



    /* ================= FILTER / SORT ================= */

    function parseItemDate(item) {

        if (!item.modifyDate && !item.ldate) return null;

        const d = new Date(item.modifyDate || item.ldate);

        return isNaN(d.getTime()) ? null : d;

    }



    function filtered() {

        const q = S.q.trim().toLowerCase();

        let r = rows.filter(x => {

            if (S.mode && x.mode !== S.mode) return false;

            if (S.svc && x.svc !== S.svc) return false;

            if (S.rec !== "" && String(x.rec) !== S.rec) return false;

            if (S.sta && x.status.toLowerCase() !== S.sta.toLowerCase()) return false;



            // Date range filter

            if (S.fromDate || S.toDate) {

                const itemDate = parseItemDate(x);

                if (itemDate) {

                    if (S.fromDate) {

                        const f = new Date(S.fromDate);

                        f.setHours(0, 0, 0, 0);

                        if (itemDate < f) return false;

                    }

                    if (S.toDate) {

                        const t = new Date(S.toDate);

                        t.setHours(23, 59, 59, 999);

                        if (itemDate > t) return false;

                    }

                } else {

                    return false;

                }

            }



            if (q) {

                const combined = (x.id + " " + x.name + " " + x.short + " " + x.mode + " " + x.svc).toLowerCase();

                if (!combined.includes(q)) return false;

            }



            return true;

        });



        if (S.sort) {

            const k = S.sort;

            const d = S.dir;

            r = r.slice().sort((a, b) => {

                let A = a[k];

                let B = b[k];

                if (["serial", "rec"].includes(k) || k === "amt") {

                    A = +A || 0;

                    B = +B || 0;

                    return (A - B) * d;

                }

                return String(A).localeCompare(String(B), undefined, { numeric: true, sensitivity: "base" }) * d;

            });

        }

        return r;

    }



    const uniq = k => [...new Set(rows.map(r => r[k]).filter(Boolean))].sort((a, b) => a.localeCompare(b));



    function fillSelects() {

        const opt = (arr, ph) => `<option value="">${ph}</option>` + arr.map(v => `<option value="${esc(v)}">${esc(v)}</option>`).join("");

        const modes = (shipmentModes.length > 0) ? shipmentModes.map(m => (m.name || m.ExpenseType || "").trim()).filter(Boolean) : uniq("mode");

        const svcs = (serviceProviders.length > 0) ? serviceProviders.map(s => (s.name || s.ServiceTypeName || "").trim()).filter(Boolean) : uniq("svc");

        const keepMode = S.mode;

        const keepSvc = S.svc;

        $("#fMode").html(opt(modes, "All shipment modes")).val(keepMode);

        $("#fSvc").html(opt(svcs, "All service providers")).val(keepSvc);

        const modeModalOpts = `<option value="">Select Shipment Mode</option>` + shipmentModes.map(m => {

            const code = m.id || m.code || m.name;

            const name = (m.name || m.ExpenseType || code).trim();

            return `<option value="${esc(code)}" data-name="${esc(name)}">${esc(name)}</option>`;

        }).join("");

        const svcModalOpts = `<option value="">Select Service Provider</option>` + serviceProviders.map(s => {

            const code = s.id || s.code || s.name;

            const name = (s.name || s.ServiceTypeName || code).trim();

            return `<option value="${esc(code)}" data-name="${esc(name)}">${esc(name)}</option>`;

        }).join("");

        $("#eMode").html(modeModalOpts);

        $("#eSvc").html(svcModalOpts);

        if (typeof window.reinitCustomDropdownById === "function") {

            window.reinitCustomDropdownById("fMode");

            window.reinitCustomDropdownById("fSvc");

            window.reinitCustomDropdownById("eMode");

            window.reinitCustomDropdownById("eSvc");

            if (keepMode) window.setCustomDropdownValue("fMode", keepMode, keepMode);

            if (keepSvc) window.setCustomDropdownValue("fSvc", keepSvc, keepSvc);

        }

        initCustomDropdownEvents();

    }



    /* ================= RENDER ================= */

    function render() {

        const all = filtered();

        const pages = Math.max(1, Math.ceil(all.length / S.size));

        if (S.page > pages) S.page = pages;

        const start = (S.page - 1) * S.size;

        const view = all.slice(start, start + S.size);



        // 1. KPIs

        const rec = all.filter(r => r.rec).length;

        const act = all.filter(r => r.status === "Active").length;

        const kp = [

            ["Total Expense Heads", all.length, "of " + rows.length + " overall", "var(--brand2)"],

            ["Receiptable", rec, pct(rec, all.length) + " of selection", "var(--ok)"],

            ["Non-Receiptable", all.length - rec, pct(all.length - rec, all.length) + " of selection", "var(--amber)"],

            ["Active", act, (all.length - act) + " inactive", "var(--accent)"],

            ["Shipment Modes", new Set(all.map(r => r.mode)).size, new Set(all.map(r => r.svc)).size + " service providers", "#8a5cd0"]

        ];

        $("#kpis").html(kp.map(k => `<div class="eh-card eh-kpi" style="--c:${k[3]}"><div class="l">${k[0]}</div><div class="v">${k[1]}</div><div class="s">${k[2]}</div></div>`).join(""));



        // 2. Charts

        const cm = {};

        all.forEach(r => cm[r.mode] = (cm[r.mode] || 0) + 1);

        const ms = Object.entries(cm).sort((a, b) => b[1] - a[1]);

        const mx = ms.length ? ms[0][1] : 1;

        $("#chMode").html(ms.slice(0, 8).map(([n, c]) => `

            <div class="eh-bar ${S.mode === n ? "on" : ""}" data-mode="${esc(n)}">

                <span class="n" title="${esc(n)}">${esc(n)}</span>

                <span class="t"><i style="width:${(c / mx * 100).toFixed(1)}%"></i></span>

                <span class="c">${c}</span>

            </div>

        `).join("") || '<div class="eh-empty">No data</div>');



        const cs = {};

        all.forEach(r => cs[r.svc] = (cs[r.svc] || 0) + 1);

        donut("#dnSvc", "#lgSvc", Object.entries(cs).sort((a, b) => b[1] - a[1]), ["#1d4a8f", "#0e9f8e", "#e8a317", "#8a5cd0", "#d64550"], "svc", all.length);

        donut("#dnRec", "#lgRec", [["Yes", rec], ["No", all.length - rec]], ["#1e9e5a", "#b8c2d6"], "rec", all.length, { Yes: "1", No: "0" });



        // 3. Table Rows

        $("#tb").html(view.map(r => {
            const idStr = String(r.id || "").trim();
            const isChecked = S.sel.has(idStr);
            return `
            <tr class="${isChecked ? "sel" : ""}">

                <td class="eh-ctr eh-chk0"><input type="checkbox" data-sel="${esc(idStr)}" ${isChecked ? "checked" : ""}></td>

                <td class="eh-id" data-l="ID"><a href="javascript:void(0)" class="eh-edit-btn" data-edit="${esc(idStr)}" data-tc="${r.tc}">${esc(idStr)}</a></td>

                <td data-l="Expense Head" class="eh-nm">${esc(r.name)}</td>

                <td data-l="Short Name" class="eh-hide-md">${esc(r.short) || '<span style="color:var(--muted)">—</span>'}</td>

                <td data-l="Serial" class="eh-num">${esc(r.serial)}</td>

                <td data-l="Receiptable" class="eh-ctr"><span class="eh-badge ${r.rec ? "b-yes" : "b-no"}">${r.rec ? "Yes" : "No"}</span></td>

                <td data-l="Service By">${esc(r.svc)}</td>

                <td data-l="Shipment Mode">${esc(r.mode)}</td>

                <td data-l="Amount" class="eh-num eh-hide-sm">${r.amt !== "" && r.amt !== null && !isNaN(r.amt) ? Number(r.amt).toLocaleString(undefined, { minimumFractionDigits: 2 }) : '<span style="color:var(--muted)">—</span>'}</td>

                <td data-l="Status" class="eh-ctr eh-hide-sm"><span class="eh-badge ${r.status === "Active" ? "b-act" : "b-ina"}">${r.status}</span></td>

                <td class="eh-ctr">

                    <div class="eh-acts">

                        <button type="button" class="eh-btn eh-sm eh-edit-btn" data-edit="${esc(idStr)}" data-tc="${r.tc}" title="Edit">✎</button>

                        <button type="button" class="eh-btn eh-sm eh-danger" data-del="${esc(idStr)}" data-tc="${r.tc}" title="Delete">🗑</button>

                    </div>

                </td>

            </tr>

        `;
        }).join("") || `<tr><td colspan="11" class="eh-empty">No expense heads match the current filters.</td></tr>`);



        $("#cnt").text(all.length + " records");

        $("#info").text(all.length ? `Showing ${start + 1} to ${start + view.length} of ${all.length} entries` + (all.length !== rows.length ? ` (filtered from ${rows.length})` : "") : "No entries");

        $("#selN").text(S.sel.size);

        $("#bulkDel").prop("disabled", !S.sel.size);

        $("#selAll").prop("checked", view.length > 0 && view.every(r => S.sel.has(String(r.id || "").trim())));



        $("th[data-k]").each(function () {

            const k = $(this).data("k");

            $(this).toggleClass("sorted", k === S.sort);

            $(this).find(".eh-ar").text(k === S.sort ? (S.dir > 0 ? "▲" : "▼") : "⇅");

        });



        pager(pages);

    }



    const pct = (a, b) => b ? Math.round(a / b * 100) + "%" : "0%";



    function donut(dn, lg, data, cols, key, total, map) {

        let a = 0, g = [];

        data.slice(0, 5).forEach(([n, c], i) => {

            const s = a;

            a += c / (total || 1) * 360;

            g.push(`${cols[i]} ${s}deg ${a}deg`);

        });

        const $dn = $(dn);

        if ($dn.length) {

            $dn.css("background", total ? `conic-gradient(${g.join(",")})` : "var(--surface2)");

            $dn.attr("data-c", total);

        }

        $(lg).html(data.slice(0, 5).map(([n, c], i) => `

            <div data-${key}="${esc(map ? map[n] : n)}">

                <i style="background:${cols[i]}"></i>${esc(n)}<b>${c}</b>

            </div>

        `).join(""));

    }



    function pager(pages) {

        const p = S.page, b = [];

        const add = (t, pg, on, dis) => b.push(`<button data-pg="${pg}" class="${on ? "on" : ""}" ${dis ? "disabled" : ""}>${t}</button>`);

        add("«", 1, 0, p === 1);

        add("‹", p - 1, 0, p === 1);

        let s = Math.max(1, p - 2), e = Math.min(pages, s + 4);

        s = Math.max(1, e - 4);

        for (let i = s; i <= e; i++) add(i, i, i === p);

        add("›", p + 1, 0, p === pages);

        add("»", pages, 0, p === pages);

        $("#pager").html(b.join(""));

    }



    /* ================= EVENTS ================= */

    function setF(k, v) {

        v = (v === null || v === undefined) ? "" : String(v).trim();

        if (S[k] === v) return;

        S[k] = v;

        S.page = 1;



        isProgrammaticFilterChange = true;

        try {

            if (k === "mode") {

                if (!v) {

                    if (typeof window.clearCustomDropdownById === "function") window.clearCustomDropdownById("fMode");

                    else $("#fMode").val("");

                } else {

                    if (typeof window.setCustomDropdownValue === "function") window.setCustomDropdownValue("fMode", v, v);

                    else $("#fMode").val(v);

                }

            } else if (k === "svc") {

                if (!v) {

                    if (typeof window.clearCustomDropdownById === "function") window.clearCustomDropdownById("fSvc");

                    else $("#fSvc").val("");

                } else {

                    if (typeof window.setCustomDropdownValue === "function") window.setCustomDropdownValue("fSvc", v, v);

                    else $("#fSvc").val(v);

                }

            } else if (k === "rec") {

                if (!v) {

                    if (typeof window.clearCustomDropdownById === "function") window.clearCustomDropdownById("fRec");

                    else $("#fRec").val("");

                } else {

                    const label = v === "1" ? "Yes" : (v === "0" ? "No" : "");

                    if (typeof window.setCustomDropdownValue === "function") window.setCustomDropdownValue("fRec", v, label);

                    else $("#fRec").val(v);

                }

            } else if (k === "sta") {

                if (!v) {

                    if (typeof window.clearCustomDropdownById === "function") window.clearCustomDropdownById("fSta");

                    else $("#fSta").val("");

                } else {

                    if (typeof window.setCustomDropdownValue === "function") window.setCustomDropdownValue("fSta", v, v);

                    else $("#fSta").val(v);

                }

            } else if (k === "q") {

                $("#fQ").val(v);

            }

        } finally {

            isProgrammaticFilterChange = false;

        }



        render();

    }



    $("#fQ").on("input", function () {

        const val = $(this).val() || "";

        if (S.q !== val) {

            S.q = val;

            S.page = 1;

            render();

        }

    });



    $("#pgSize").on("change", function () {

        S.size = +$(this).val();

        S.page = 1;

        render();

    });



    $("#resetBtn").on("click", function () {
        Object.assign(S, {
            mode: "",
            svc: "",
            rec: "",
            sta: "",
            fromDate: null,
            toDate: null,
            q: "",
            page: 1,
            sort: "",
            dir: 1
        });
        S.sel.clear();

        isProgrammaticFilterChange = true;
        try {
            ["fMode", "fSvc", "fRec", "fSta"].forEach(id => {
                if (typeof window.clearCustomDropdownById === "function") {
                    window.clearCustomDropdownById(id);
                } else {
                    $("#" + id).val("");
                }
            });
            if (fpFrom) fpFrom.clear(false);
            if (fpTo) {
                fpTo.clear(false);
                fpTo.set("minDate", null);
            }
            $("#fFromDate, #fToDate").val("");
            $("#fQ").val("");
        } finally {
            isProgrammaticFilterChange = false;
        }

        render();
        toast("Filters reset");
    });

        // Chart Click Filtering

    $("#chMode").on("click", "[data-mode]", function () {

        const mode = $(this).data("mode");

        setF("mode", S.mode === mode ? "" : mode);

    });



    $("#lgSvc").on("click", "[data-svc]", function () {

        const svc = $(this).data("svc");

        setF("svc", S.svc === svc ? "" : svc);

    });



    $("#lgRec").on("click", "[data-rec]", function () {

        const rec = String($(this).data("rec"));

        setF("rec", S.rec === rec ? "" : rec);

    });



    // Pagination

    $("#pager").on("click", "[data-pg]", function () {

        S.page = +$(this).data("pg");

        render();

    });



    // Sorting

    $("thead").on("click", "th[data-k]", function () {

        const k = $(this).data("k");

        S.dir = S.sort === k ? -S.dir : 1;

        S.sort = k;

        render();

    });



    // Row Actions: Edit & Delete
    $("#tb").on("click", "[data-edit]", function (e) {
        e.preventDefault();
        const id = $(this).attr("data-edit");
        openForm(id);
    });

    $("#tb").on("click", "[data-del]", function (e) {
        e.preventDefault();
        const id = $(this).attr("data-del") || $(this).data("del");
        if (id) del([id]);
    });

    $("#tb").on("change", "[data-sel]", function () {
        const id = String($(this).attr("data-sel") || $(this).data("sel") || "").trim();
        if (!id) return;
        if (this.checked) S.sel.add(id);
        else S.sel.delete(id);
        render();
    });

    $("#selAll").on("change", function () {
        const all = filtered().slice((S.page - 1) * S.size, S.page * S.size);
        const chk = this.checked;
        all.forEach(r => {
            const id = String(r.id || "").trim();
            if (id) {
                if (chk) S.sel.add(id);
                else S.sel.delete(id);
            }
        });
        render();
    });

    $("#bulkDel").on("click", function (e) {
        e.preventDefault();
        const ids = [...S.sel];
        if (!ids.length) {
            toast("Please select at least one item to delete.");
            return;
        }
        del(ids);
    });

    let pendingDeleteIds = [];

    function del(ids) {
        if (!ids || !ids.length) {
            toast("Please select at least one item to delete.");
            return;
        }

        pendingDeleteIds = ids;
        const count = ids.length;
        let msg = "Are you sure you want to delete these item(s)? This process cannot be undone.";
        if (count === 1) {
            const singleId = String(ids[0]).trim();
            const r = rows.find(x => String(x.id).trim() === singleId || String(x.tc).trim() === singleId);
            if (r && r.name) {
                msg = `Are you sure you want to delete "${r.name}"? This process cannot be undone.`;
            } else {
                msg = "Are you sure you want to delete this expense head? This process cannot be undone.";
            }
        } else {
            msg = `Are you sure you want to delete these ${count} expense heads? This process cannot be undone.`;
        }

        const modalEl = document.getElementById("ExpenseHead-delete-modal");
        const $modal = $("#ExpenseHead-delete-modal");
        if ($modal.length && modalEl) {
            $modal.find(".title").text(msg);
            if (window.bootstrap && window.bootstrap.Modal) {
                const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
                bsModal.show();
            } else if ($.fn.modal) {
                $modal.modal("show");
            }
        } else {
            if (confirm(msg)) {
                executeDelete(ids);
            }
        }
    }

    function closeDeleteModal() {
        const modalEl = document.getElementById("ExpenseHead-delete-modal");
        if (modalEl && window.bootstrap && window.bootstrap.Modal) {
            const bsModal = bootstrap.Modal.getInstance(modalEl);
            if (bsModal) bsModal.hide();
        } else if ($.fn.modal) {
            $("#ExpenseHead-delete-modal").modal("hide");
        }
        pendingDeleteIds = [];
    }

    $(document).on("click", "#ExpenseHead-delete-modal .js-expensehead-delete", function (e) {
        e.preventDefault();
        if (!pendingDeleteIds || !pendingDeleteIds.length) {
            closeDeleteModal();
            return;
        }

        const idsToDelete = [...pendingDeleteIds];
        const $btn = $(this);
        const origHtml = $btn.html();
        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Deleting...');

        executeDelete(idsToDelete, function () {
            $btn.prop("disabled", false).html(origHtml);
            closeDeleteModal();
        });
    });

    $(document).on("click", "#ExpenseHead-delete-modal [data-bs-dismiss='modal'], #ExpenseHead-delete-modal [data-dismiss='modal']", function () {
        closeDeleteModal();
    });

    $("#ExpenseHead-delete-modal").on("hidden.bs.modal", function () {
        pendingDeleteIds = [];
    });

    function executeDelete(ids, callback) {
        const numericIds = ids.map(id => {
            const r = rows.find(x => String(x.id).trim() === String(id).trim() || String(x.tc).trim() === String(id).trim());
            return (r && r.tc) ? r.tc : parseInt(id, 10);
        }).filter(n => !isNaN(n) && n > 0);

        const deletedSet = new Set(ids.map(i => String(i).trim()));
        rows = rows.filter(r => !deletedSet.has(String(r.id).trim()) && !deletedSet.has(String(r.tc).trim()));
        ids.forEach(i => S.sel.delete(i));

        fillSelects();
        render();

        $.ajax({
            url: "/ExpenseHead/BulkDeleteExpenseHead",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(numericIds)
        }).done(function (res) {
            if (res && res.isSuccess === false) {
                toast(res.message || "Failed to delete from database");
                loadData();
                return;
            }
            const count = ids.length;
            toast(count === 1 ? "1 expense head deleted successfully" : `${count} expense heads deleted successfully`);
        }).fail(function (xhr) {
            console.warn("Bulk delete server sync warning:", xhr.responseText);
            toast(ids.length + " removed from list");
        }).always(function () {
            if (typeof callback === "function") callback();
        });
    }



    $("[data-close]").on("click", function () {

        const dlg = $(this).closest("dialog")[0];

        if (dlg) dlg.close();

    });



    $("#favBtn").on("click", function () {

        $(this).toggleClass("on");

        toast($(this).hasClass("on") ? "Added to favorites" : "Removed from favorites");

    });



    /* ================= ADD / EDIT FORM ================= */

    const nextId = () => String(rows.reduce((m, r) => Math.max(m, parseInt(r.id, 10) || 0), 0) + 1).padStart(4, "0");



    function openForm(id) {

        const idStr = (id !== null && id !== undefined) ? String(id).trim() : "";

        editing = idStr ? rows.find(r => String(r.id).trim() === idStr || String(r.tc).trim() === idStr) : null;

        manualSerialEntry = false;

        $("#dlgT").text(editing ? "Edit Expense Head" : "Expense Head Info");

        $("#eDel").toggle(!!editing);

        if (editing) {

            fillForm(editing);

        } else {

            fillForm({

                tc: 0,

                id: nextId(),

                name: "",

                short: "",

                serial: "",

                rec: 0,

                svc: S.svc || "",

                serviceTypeId: "",

                mode: S.mode || "",

                expenseTypeId: "",

                amt: "",

                status: "Active"

            });

            // Dynamic Next ID from DB

            $.get("/next-expense-head-id", function (res) {

                if (res && !editing) $("#eId").val(res);

            });

            suggestSerialNo();

        }

        const dlg = document.getElementById("dlg");

        if (dlg) {

            dlg.showModal();

            setTimeout(() => $("#eName").trigger("focus"), 100);

        }

    }

    function suggestSerialNo() {

        if (editing) return;

        const rawMode = ($("#eMode").val() || "").toString().trim();

        const rawSvc = ($("#eSvc").val() || "").toString().trim();

        const rec = $("#eRec").is(":checked") ? "Y" : "N";

        if (!rawMode || !rawSvc) return;

        let expenseTypeId = rawMode;

        const foundMode = shipmentModes.find(m =>

            (m.id && String(m.id).trim() === rawMode) ||

            (m.code && String(m.code).trim() === rawMode) ||

            (m.name && m.name.trim() === rawMode)

        );

        if (foundMode) expenseTypeId = foundMode.id || foundMode.code || rawMode;

        let serviceTypeId = rawSvc;

        const foundSvc = serviceProviders.find(s =>

            (s.id && String(s.id).trim() === rawSvc) ||

            (s.code && String(s.code).trim() === rawSvc) ||

            (s.name && s.name.trim() === rawSvc)

        );

        if (foundSvc) serviceTypeId = foundSvc.id || foundSvc.code || rawSvc;

        $.get("/serial-no", { expenseType: expenseTypeId, serviceType: serviceTypeId, isReceiptable: rec }, function (res) {

            if (res && res.serialNo && !manualSerialEntry) {

                $("#eSerial").val(res.serialNo);

            }

        }).fail(function () {

            let relevant = rows.filter(r => (r.expenseTypeId === expenseTypeId || r.mode === rawMode) && (r.serviceTypeId === serviceTypeId || r.svc === rawSvc));

            let maxSerial = 0;

            relevant.forEach(r => {

                const s = parseInt(r.serial, 10);

                if (!isNaN(s) && s > maxSerial) maxSerial = s;

            });

            if (!manualSerialEntry) $("#eSerial").val(maxSerial + 1);

        });

    }

    $("#eSerial").on("input", function () {

        manualSerialEntry = true;

    });

    $("#eRec").on("change", function () {

        if (!manualSerialEntry) suggestSerialNo();

    });

    function fillForm(r) {

        $("#eTc").val(r.tc || 0);

        $("#eId").val(r.id || "");

        $("#eName").val(r.name || "");

        $("#eShort").val(r.short || "");

        $("#eSerial").val(r.serial || "");

        $("#eAmt").val(r.amt !== null && r.amt !== undefined ? r.amt : "");

        $("#eRec").prop("checked", !!r.rec);

        // Resolve code and name for shipment mode

        let modeCode = r.expenseTypeId || "";

        let modeName = (r.mode || "").trim();

        if (!modeCode && modeName) {

            const found = shipmentModes.find(m => (m.name && m.name.trim() === modeName) || (m.ExpenseType && m.ExpenseType.trim() === modeName));

            if (found) modeCode = found.id || found.code || "";

        }

        if (!modeName && modeCode) {

            const found = shipmentModes.find(m => (m.id && String(m.id).trim() === String(modeCode).trim()) || (m.code && String(m.code).trim() === String(modeCode).trim()));

            if (found) modeName = (found.name || found.ExpenseType || "").trim();

        }

        // Resolve code and name for service provider

        let svcCode = r.serviceTypeId || "";

        let svcName = (r.svc || "").trim();

        if (!svcCode && svcName) {

            const found = serviceProviders.find(s => (s.name && s.name.trim() === svcName) || (s.ServiceTypeName && s.ServiceTypeName.trim() === svcName));

            if (found) svcCode = found.id || found.code || "";

        }

        if (!svcName && svcCode) {

            const found = serviceProviders.find(s => (s.id && String(s.id).trim() === String(svcCode).trim()) || (s.code && String(s.code).trim() === String(svcCode).trim()));

            if (found) svcName = (found.name || found.ServiceTypeName || "").trim();

        }

        const staKey = r.status || "Active";

        if (typeof window.setCustomDropdownValue === "function") {

            if (modeCode || modeName) {

                window.setCustomDropdownValue("eMode", modeCode || modeName, modeName || modeCode);

            } else {

                window.clearCustomDropdownById("eMode");

            }

            if (svcCode || svcName) {

                window.setCustomDropdownValue("eSvc", svcCode || svcName, svcName || svcCode);

            } else {

                window.clearCustomDropdownById("eSvc");

            }

            window.setCustomDropdownValue("eSta", staKey, staKey);

        } else {

            $("#eMode").val(modeCode || modeName);

            $("#eSvc").val(svcCode || svcName);

            $("#eSta").val(staKey);

        }

        if (r.ldate) {

            $("#lDateContainer").show();

            $("#eLdate").val(new Date(r.ldate).toLocaleDateString());

        } else {

            $("#lDateContainer").hide();

        }

        if (r.modifyDate) {

            $("#modifyDateContainer").show();

            $("#eModifyDate").val(new Date(r.modifyDate).toLocaleDateString());

        } else {

            $("#modifyDateContainer").hide();

        }

        $("#frm .eh-in").removeClass("err");

        $("#frm .custom-dd-box").removeClass("err");

    }



    $("#newBtn").on("click", () => openForm());



    $("#eClear").on("click", () => {

        fillForm({

            tc: 0,

            id: $("#eId").val(),

            name: "",

            short: "",

            serial: "",

            rec: 0,

            svc: "",

            mode: "",

            amt: "",

            status: "Active"

        });

        suggestSerialNo();

    });



    $("#eDel").on("click", () => {

        const dlg = document.getElementById("dlg");

        if (dlg) dlg.close();

        if (editing) del([editing.id || editing.tc]);

    });



    $("#eSave").on("click", function () {

        let ok = true;

        const reqFields = ["#eMode", "#eName", "#eSvc", "#eSerial"];

        reqFields.forEach(s => {

            const val = $(s).val();

            const bad = !val || !val.toString().trim();

            $(s).closest(".eh-fld").find(".custom-dd-box, .eh-in").toggleClass("err", bad);

            if (bad) ok = false;

        });



        if (!ok) return toast("Please fill all required (*) fields");



        const rawMode = ($("#eMode").val() || "").toString().trim();

        const rawSvc = ($("#eSvc").val() || "").toString().trim();

        const nameVal = $("#eName").val().trim();

        const shortVal = $("#eShort").val().trim();

        const serialVal = parseInt($("#eSerial").val(), 10) || 1;

        const recVal = $("#eRec").is(":checked") ? 1 : 0;

        const amtVal = $("#eAmt").val() !== "" ? parseFloat($("#eAmt").val()) : null;

        const statusVal = $("#eSta").val() || "Active";

        const tcVal = parseInt($("#eTc").val(), 10) || 0;

        const idVal = $("#eId").val();



        // Resolve Shipment Mode Code & Name

        let expenseTypeId = "";

        let modeVal = "";

        const foundMode = shipmentModes.find(m =>

            (m.id && String(m.id).trim() === rawMode) ||

            (m.code && String(m.code).trim() === rawMode) ||

            (m.name && m.name.trim() === rawMode) ||

            (m.ExpenseType && m.ExpenseType.trim() === rawMode)

        );

        if (foundMode) {

            expenseTypeId = foundMode.id || foundMode.code || rawMode;

            modeVal = (foundMode.name || foundMode.ExpenseType || rawMode).trim();

        } else {

            expenseTypeId = rawMode;

            modeVal = $("#eMode option:selected").text().trim() || rawMode;

        }



        // Resolve Service Provider Code & Name

        let serviceTypeId = "";

        let svcVal = "";

        const foundSvc = serviceProviders.find(s =>

            (s.id && String(s.id).trim() === rawSvc) ||

            (s.code && String(s.code).trim() === rawSvc) ||

            (s.name && s.name.trim() === rawSvc) ||

            (s.ServiceTypeName && s.ServiceTypeName.trim() === rawSvc)

        );

        if (foundSvc) {

            serviceTypeId = foundSvc.id || foundSvc.code || rawSvc;

            svcVal = (foundSvc.name || foundSvc.ServiceTypeName || rawSvc).trim();

        } else {

            serviceTypeId = rawSvc;

            svcVal = $("#eSvc option:selected").text().trim() || rawSvc;

        }



        // Check duplicate name under same shipment mode

        const isDup = rows.some(r => r !== editing && (r.expenseTypeId === expenseTypeId || r.mode === modeVal) && r.name.toLowerCase() === nameVal.toLowerCase());

        if (isDup) {

            return toast("This expense head already exists for the selected shipment mode");

        }



        const payload = {

            TC: tcVal,

            ExpenseHeadID: idVal,

            ExpenseHead: nameVal,

            ShortName: shortVal,

            SerialNo: String(serialVal),

            IsReceiptable: recVal ? "Y" : "N",

            Rec: recVal,

            Amount: amtVal,

            ServiceTypeID: serviceTypeId,

            ServiceTypeName: svcVal,

            ExpenseTypeID: expenseTypeId,

            ExpenseType: modeVal,

            Status: statusVal,

            EntryType: statusVal

        };



        const $saveBtn = $("#eSave");

        const origBtnText = $saveBtn.text();

        $saveBtn.prop("disabled", true).text("Saving...");



        $.ajax({

            url: "/ExpenseHead/FastSave",

            type: "POST",

            contentType: "application/json",

            data: JSON.stringify(payload),

            success: function (res) {

                $saveBtn.prop("disabled", false).text(origBtnText);

                if (res && res.success) {

                    const savedItem = res.data || {};

                    const newTc = savedItem.tc || savedItem.TC || tcVal;

                    const newId = String(savedItem.id || savedItem.Id || savedItem.expenseHeadId || idVal || "").trim();



                    const updatedRecord = {

                        tc: newTc,

                        id: newId,

                        name: nameVal,

                        short: shortVal,

                        serial: String(serialVal),

                        rec: recVal,

                        svc: svcVal,

                        serviceTypeId: serviceTypeId,

                        mode: modeVal,

                        expenseTypeId: expenseTypeId,

                        amt: amtVal,

                        status: statusVal,

                        ldate: savedItem.ldate || savedItem.LDate || (editing ? editing.ldate : new Date().toISOString()),

                        modifyDate: savedItem.modifyDate || savedItem.ModifyDate || new Date().toISOString()

                    };



                    if (editing) {

                        Object.assign(editing, updatedRecord);

                    } else {

                        rows.unshift(updatedRecord);

                    }



                    fillSelects();

                    const dlg = document.getElementById("dlg");

                    if (dlg) dlg.close();

                    editing = null;

                    render();

                    toast("Saved successfully: " + nameVal);

                } else {

                    toast(res && res.message ? res.message : "Save failed.");

                }

            },

            error: function (xhr) {

                $saveBtn.prop("disabled", false).text(origBtnText);

                let msg = "Error saving to database.";

                try {

                    const errObj = JSON.parse(xhr.responseText);

                    msg = errObj.message || errObj.error || msg;

                } catch (e) {

                    if (xhr.statusText) msg += " (" + xhr.statusText + ")";

                }

                toast(msg);

                console.error("Save error:", xhr.responseText);

            }

        });

    });



    /* ================= REPORT EXPORT & PREVIEW ================= */
    $("#expBtn").on("click", function (e) {
        e.stopPropagation();
        $("#expMenu").toggleClass("open");
    });

    $(document).on("click", function () {
        $("#expMenu").removeClass("open");
    });

    $("#expMenu").on("click", "[data-x]", function () {
        const action = $(this).data("x");
        $("#expMenu").removeClass("open");
        if (action === "preview") openPreview();
        else if (action === "pdf") downloadPdf();
        else if (action === "excel") exportExcel();
    });

    function filterText() {
        const p = [];
        if (S.mode) p.push("Shipment Mode: " + S.mode);
        if (S.svc) p.push("Service By: " + S.svc);
        if (S.rec !== "") p.push("Receiptable: " + (S.rec === "1" ? "Yes" : "No"));
        if (S.sta) p.push("Status: " + S.sta);
        if (S.q) p.push("Search: " + S.q);
        if (S.fromDate) p.push("From: " + S.fromDate.toLocaleDateString("en-GB"));
        if (S.toDate) p.push("To: " + S.toDate.toLocaleDateString("en-GB"));
        return p.join(" | ") || "All records";
    }

    function reportTable(data) {
        return `<table style="width:100%;border-collapse:collapse;margin-top:6px;font-size:11px">` +
            `<thead>` +
                `<tr style="height:28px">` +
                    `<th style="padding:6px;text-align:center;border:1px solid #333;width:75px;color:#000;font-weight:bold;background:#fff">Expense Head ID</th>` +
                    `<th style="padding:6px;text-align:left;border:1px solid #333;color:#000;font-weight:bold;background:#fff">Expense Head</th>` +
                    `<th style="padding:6px;text-align:left;border:1px solid #333;color:#000;font-weight:bold;background:#fff">Short Name</th>` +
                    `<th style="padding:6px;text-align:center;border:1px solid #333;width:50px;color:#000;font-weight:bold;background:#fff">Serial No</th>` +
                    `<th style="padding:6px;text-align:center;border:1px solid #333;width:75px;color:#000;font-weight:bold;background:#fff">Is Receiptable?</th>` +
                    `<th style="padding:6px;text-align:left;border:1px solid #333;color:#000;font-weight:bold;background:#fff">Service Provided By</th>` +
                    `<th style="padding:6px;text-align:left;border:1px solid #333;color:#000;font-weight:bold;background:#fff">Shipment Mode</th>` +
                    `<th style="padding:6px;text-align:right;border:1px solid #333;width:75px;color:#000;font-weight:bold;background:#fff">Amount</th>` +
                    `<th style="padding:6px;text-align:center;border:1px solid #333;width:60px;color:#000;font-weight:bold;background:#fff">Status</th>` +
                `</tr>` +
            `</thead>` +
            `<tbody>` +
            data.map((r) => `
                <tr style="background:#fff">` +
                    `<td style="padding:4px 6px;text-align:center;border:1px solid #ccc;color:#000">${esc(r.id)}</td>` +
                    `<td style="padding:4px 6px;text-align:left;border:1px solid #ccc;color:#000">${esc(r.name)}</td>` +
                    `<td style="padding:4px 6px;text-align:left;border:1px solid #ccc;color:#000">${esc(r.short)}</td>` +
                    `<td style="padding:4px 6px;text-align:center;border:1px solid #ccc;color:#000">${esc(r.serial)}</td>` +
                    `<td style="padding:4px 6px;text-align:center;border:1px solid #ccc;color:#000">${r.rec ? "Yes" : "No"}</td>` +
                    `<td style="padding:4px 6px;text-align:left;border:1px solid #ccc;color:#000">${esc(r.svc)}</td>` +
                    `<td style="padding:4px 6px;text-align:left;border:1px solid #ccc;color:#000">${esc(r.mode)}</td>` +
                    `<td style="padding:4px 6px;text-align:right;border:1px solid #ccc;color:#000">${r.amt !== "" && r.amt !== null ? Number(r.amt).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : ""}</td>` +
                    `<td style="padding:4px 6px;text-align:center;border:1px solid #ccc;color:#000">${esc(r.status)}</td>` +
                `</tr>`
            ).join("") +
            `</tbody></table>`;
    }

    function reportHTML() {
        const d = filtered();
        const printDateStr = new Date().toLocaleDateString("en-GB") + " " + new Date().toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit", hour12: true });
        return `
            <div class="rpt-container" style="font-family:Arial,sans-serif;color:#111;padding:4px">` +
                `<div class="rpt-header" style="display:flex;align-items:center;justify-content:space-between;padding-bottom:10px;border-bottom:1.5px solid #333;margin-bottom:10px">` +
                    `<div style="width:130px;flex-shrink:0;text-align:left">` +
                        `<img src="/images/fermosa-wX200.png" style="max-height:40px;max-width:130px;object-fit:contain" alt="Formosa Logo" onerror="this.style.display='none'" />` +
                    `</div>` +
                    `<div style="flex:1;text-align:center;padding:0 8px">` +
                        `<h2 style="margin:0;font-size:19px;font-weight:700;color:#000;letter-spacing:0.3px">${esc(CONFIG.company)}</h2>` +
                        `<p style="margin:2px 0 0;font-size:11.5px;color:#555">${esc(CONFIG.address)}</p>` +
                        `<h3 style="margin:4px 0 0;font-size:13.5px;font-weight:700;color:#222;text-transform:uppercase;letter-spacing:0.5px">${esc(CONFIG.reportTitle)}</h3>` +
                        `<p style="margin:3px 0 0;font-size:10.5px;color:#666">${esc(filterText())} &nbsp;•&nbsp; <b>${d.length}</b> record(s)</p>` +
                    `</div>` +
                    `<div style="width:130px;flex-shrink:0"></div>` +
                `</div>` +
                reportTable(d) +
                `<div class="rpt-footer" style="display:flex;align-items:center;justify-content:space-between;margin-top:14px;padding-top:6px;border-top:1px solid #ccc;font-size:10px;color:#666">` +
                    `<div style="text-align:left">Printed: ${printDateStr}</div>` +
                    `<div style="text-align:center">Formosa Logistics Limited - C&F Management System</div>` +
                    `<div style="text-align:right">Page 1 of 1</div>` +
                `</div>` +
            `</div>`;
    }

    function openPreview() {
        $("#pvBody").html(reportHTML());
        const pv = document.getElementById("pv");
        if (pv) pv.showModal();
    }

    function printFrame() {
        const f = document.createElement("iframe");
        f.style.cssText = "position:fixed;right:0;bottom:0;width:0;height:0;border:0";
        document.body.appendChild(f);
        f.srcdoc = `<!DOCTYPE html><html><head><title>${esc(CONFIG.reportTitle)}</title><style>@page{size:A4 landscape;margin:12mm 10mm 14mm 10mm}body{font-family:Arial,sans-serif;font-size:10px;color:#111;margin:0;padding:0;padding-bottom:25px}table{width:100%;border-collapse:collapse}thead{display:table-header-group}tr{page-break-inside:avoid}th{background:#fff !important;color:#000 !important;padding:5px 6px;font-size:10px;border:1px solid #333}td{padding:4px 6px;font-size:9.5px;border:1px solid #ccc}.rpt-footer{position:fixed;bottom:0;left:0;right:0;display:flex;justify-content:space-between;font-size:9px;color:#666;border-top:1px solid #ccc;padding-top:4px;background:#fff}</style></head><body>${reportHTML()}</body></html>`;
        f.onload = () => {
            f.contentWindow.focus();
            f.contentWindow.print();
            setTimeout(() => f.remove(), 2500);
        };
    }

    function downloadPdf() {
        const recVal = S.rec !== "" ? (S.rec === "1" ? "Yes" : "No") : "";
        const params = new URLSearchParams({
            format: "pdf",
            expenseTypeId: S.mode || "",
            serviceType: S.svc || "",
            isReceiptable: recVal,
            status: S.sta || "",
            search: S.q || ""
        });
        toast("Downloading PDF report...");
        window.location.href = "/expense-head/report?" + params.toString();
    }

    $("#pvPrint").on("click", printFrame);
    $("#pvPdf").on("click", downloadPdf);

    function exportExcel() {
        const d = filtered();
        const printDateStr = new Date().toLocaleDateString("en-GB") + " " + new Date().toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit", hour12: true });
        const html = `<html xmlns:x="urn:schemas-microsoft-com:office:excel"><head><meta charset="utf-8"></head><body><table border="1" style="border-collapse:collapse">` +
            `<tr><td colspan="9" align="center" valign="middle" style="font-size:18pt;font-weight:bold;height:38px;text-align:center;vertical-align:middle;color:#000;mso-number-format:'\\@'">${esc(CONFIG.company)}</td></tr>` +
            `<tr><td colspan="9" align="center" valign="middle" style="font-size:11pt;color:#555;height:24px;text-align:center;vertical-align:middle;mso-number-format:'\\@'">${esc(CONFIG.address)}</td></tr>` +
            `<tr><td colspan="9" align="center" valign="middle" style="font-size:14pt;font-weight:bold;height:30px;text-align:center;vertical-align:middle;color:#222;mso-number-format:'\\@'">${esc(CONFIG.reportTitle)}</td></tr>` +
            `<tr><td colspan="9" align="center" valign="middle" style="font-size:10pt;color:#666;height:22px;text-align:center;vertical-align:middle;mso-number-format:'\\@'">${esc(filterText())} &nbsp;•&nbsp; ${d.length} record(s) &nbsp;•&nbsp; Printed: ${printDateStr}</td></tr>` +
            `<tr><td colspan="9" style="height:12px;border:none"></td></tr>` +
            `<tr style="font-weight:bold;height:28px"><th style="color:#000;text-align:center;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Expense Head ID</th><th style="color:#000;text-align:left;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Expense Head</th><th style="color:#000;text-align:left;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Short Name</th><th style="color:#000;text-align:center;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Serial No</th><th style="color:#000;text-align:center;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Is Receiptable?</th><th style="color:#000;text-align:left;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Service Provided By</th><th style="color:#000;text-align:left;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Shipment Mode</th><th style="color:#000;text-align:right;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Amount</th><th style="color:#000;text-align:center;vertical-align:middle;padding:6px;border:1px solid #333;background:#fff">Status</th></tr>` +
            d.map(r => `
                <tr>` +
                    `<td align="center" valign="middle" style="mso-number-format:'\\@';text-align:center;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.id)}</td>` +
                    `<td align="left" valign="middle" style="text-align:left;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.name)}</td>` +
                    `<td align="left" valign="middle" style="text-align:left;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.short)}</td>` +
                    `<td align="center" valign="middle" style="text-align:center;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.serial)}</td>` +
                    `<td align="center" valign="middle" style="text-align:center;vertical-align:middle;border:1px solid #ccc;color:#000">${r.rec ? "Yes" : "No"}</td>` +
                    `<td align="left" valign="middle" style="text-align:left;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.svc)}</td>` +
                    `<td align="left" valign="middle" style="text-align:left;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.mode)}</td>` +
                    `<td align="right" valign="middle" style="text-align:right;vertical-align:middle;border:1px solid #ccc;color:#000">${r.amt !== "" && r.amt !== null ? Number(r.amt).toFixed(2) : ""}</td>` +
                    `<td align="center" valign="middle" style="text-align:center;vertical-align:middle;border:1px solid #ccc;color:#000">${esc(r.status)}</td>` +
                `</tr>`
            ).join("") +
            `</table></body></html>`;

        const blob = new Blob(["\ufeff" + html], { type: "application/vnd.ms-excel" });
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "ExpenseHeadSetupReport_" + new Date().toISOString().slice(0, 10) + ".xls";
        document.body.appendChild(a);
        a.click();
        a.remove();
        toast("Excel exported (" + d.length + " rows)");
    }

        /* ================= INITIALIZATION ================= */

    $(function () {

        initDatePickers();

        initCustomDropdownEvents();



        // 1. Preload DB Dropdown datasets if provided by Stored Procedure

        if (window.INITIAL_DROPDOWNS) {

            if (Array.isArray(window.INITIAL_DROPDOWNS.shipmentModes) && window.INITIAL_DROPDOWNS.shipmentModes.length) {

                shipmentModes = window.INITIAL_DROPDOWNS.shipmentModes.map(m => ({ id: m.code, name: m.name }));

            }

            if (Array.isArray(window.INITIAL_DROPDOWNS.serviceProviders) && window.INITIAL_DROPDOWNS.serviceProviders.length) {

                serviceProviders = window.INITIAL_DROPDOWNS.serviceProviders.map(s => ({ id: s.code, name: s.name }));

            }

        }



        // 2. Instant rendering from preloaded Stored Procedure data (0ms latency!)

        if (window.INITIAL_EXPENSE_HEADS && Array.isArray(window.INITIAL_EXPENSE_HEADS) && window.INITIAL_EXPENSE_HEADS.length > 0) {

            parseAndSetRows(window.INITIAL_EXPENSE_HEADS);

            fillSelects();

            render();

        }



        // 3. Initialize custom-select-single for all dropdowns

        if (typeof window.initCustomDropdowns === "function") {

            window.initCustomDropdowns(".expense-head-scope", true);

        }

        initCustomDropdownEvents();



        // 4. Fresh background fetch from Stored Procedure

        loadData();

    });



})(jQuery);

