/**
 * Trial Balance Report matching School ERP Accounting Design
 * Integrated with Choices.js (choiceDD / choiceManager) and Flatpickr
 */

let reportData = null;
let fpFrom = null;
let fpTo = null;
let defaultCompany = '';
let defaultFrom = '';
let defaultTo = '';

const fmt = value => {
    value = Math.round((+value + Number.EPSILON) * 100) / 100;
    if (!value) value = 0;
    return value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};
const taka = value => '৳ ' + fmt(value);
const esc = value => String(value ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const dmy = date => date ? date.split('-').reverse().join('/') : '';

function getFilterVal(id) {
    return $('#' + id).val();
}

function setFilterVal(id, val) {
    $('#' + id).val(val);
    if (typeof window.reinitCustomDropdownById === 'function') {
        window.reinitCustomDropdownById(id);
    }
}

$(document).ready(function () {
    if (typeof window.initCustomDropdowns === 'function') {
        window.initCustomDropdowns('#rf', false);
    }

    defaultCompany = $('#companyFilter').val() || '';
    defaultFrom = $('#from').val() || '';
    defaultTo = $('#to').val() || '';

    initFlatpickr();
    initFilterEvents();
    initExportDropdown();

    $('#btnResetFilters').on('click', resetFilters);

    fetchReportData();
});

function initFlatpickr() {
    if (typeof flatpickr !== 'undefined') {
        const initialFrom = $('#from').val();
        const initialTo = $('#to').val();

        fpFrom = flatpickr('#from', {
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            defaultDate: initialFrom || null,
            allowInput: true,
            onChange: function (selectedDates) {
                if (selectedDates.length > 0 && fpTo) {
                    fpTo.set('minDate', selectedDates[0]);
                }
                fetchReportData();
            }
        });

        fpTo = flatpickr('#to', {
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            defaultDate: initialTo || null,
            allowInput: true,
            onChange: function () {
                fetchReportData();
            }
        });
    }
}

function initFilterEvents() {
    $('#companyFilter').on('change', function () {
        const companyCode = getFilterVal('companyFilter');
        loadBranches(companyCode).then(() => {
            fetchReportData();
        });
    });

    $('#branch').on('change', function () {
        fetchReportData();
    });

    $('#level').on('change', function () {
        renderTrialBalance();
    });
}

function initExportDropdown() {
    $('#exportFormatSelect').on('change', function () {
        const format = $(this).val();
        if (format) {
            exportReport(format);
            // Reset dropdown back to placeholder so user can select again
            $(this).val('');
        }
    });
}

async function loadBranches(companyCode) {
    try {
        const response = await fetch(`/TrialBalance/GetBranches?companyCode=${encodeURIComponent(companyCode || '')}`);
        if (!response.ok) return;
        const branches = await response.json();
        const $branch = $('#branch');
        if ($branch.length) {
            $branch.empty();
            $branch.append('<option value="All">All Branches</option>');
            if (Array.isArray(branches)) {
                branches.forEach(b => {
                    $branch.append(`<option value="${esc(b.code || b.id)}">${esc(b.name)}</option>`);
                });
            }

            $branch.val('All');
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('branch');
            }
        }
    } catch (err) {
        console.error('Error fetching branches:', err);
    }
}

async function fetchReportData() {
    const $report = $('#report');
    if ($report.length && !reportData) {
        $report.html(`
            <div class="text-center py-4">
                <div class="spinner-border text-success" role="status"></div>
                <p class="mt-2 text-muted">Loading Trial Balance report...</p>
            </div>
        `);
    }

    const toIso = val => {
        if (!val) return null;
        if (val.indexOf('/') !== -1) {
            const parts = val.split('/');
            if (parts.length === 3) return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
        }
        return val;
    };

    const filter = {
        Main_CompanyCode: getFilterVal('companyFilter') || '',
        BranchCode: getFilterVal('branch') || 'All',
        FromDate: toIso($('#from').val()),
        ToDate: toIso($('#to').val()),
        Level: parseInt(getFilterVal('level') || '5') || 5
    };

    try {
        const response = await fetch('/TrialBalance/GetReportData', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(filter)
        });

        const res = await response.json();
        if (res.success && res.data) {
            reportData = res.data;
            if (reportData.companyName && $('#companySubtitle').length) {
                $('#companySubtitle').text(reportData.companyName + ' · Amounts in Bangladeshi Taka (৳)');
            }
            renderTrialBalance();
        } else {
            if ($report.length) {
                $report.html(`<div class="alert alert-warning py-3 text-center my-2">${res.message || 'No data returned for Trial Balance.'}</div>`);
            }
        }
    } catch (err) {
        console.error('Error loading Trial Balance data:', err);
        if ($report.length) {
            $report.html(`<div class="alert alert-danger py-2 my-2">Failed to load Trial Balance data. Please check connection and try again.</div>`);
        }
    }
}

function renderTrialBalance() {
    if (!reportData || !reportData.items) return;

    const level = parseInt(getFilterVal('level') || '5') || 5;
    const items = (reportData.items || []).filter(x => (x.level ?? x.Level ?? 0) <= level);
    const branchText = $('#branch option:selected').text() || 'All branches';
    const from = $('#from').val() || '';
    const to = $('#to').val() || '';
    const company = reportData.companyName || reportData.CompanyName || 'School Management ERP';

    // Calculate Summary totals from Level 1 items or use reportData.summary
    const l1Items = (reportData.items || []).filter(x => (x.level ?? x.Level ?? 0) === 1);
    const sum = reportData.summary || reportData.Summary || {};
    const totalOpDr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.openingDebit ?? x.OpeningDebit ?? 0), 0) : (sum.totalOpeningDebit ?? sum.TotalOpeningDebit ?? 0);
    const totalOpCr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.openingCredit ?? x.OpeningCredit ?? 0), 0) : (sum.totalOpeningCredit ?? sum.TotalOpeningCredit ?? 0);
    const totalPDr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.periodDebit ?? x.PeriodDebit ?? 0), 0) : (sum.totalPeriodDebit ?? sum.TotalPeriodDebit ?? 0);
    const totalPCr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.periodCredit ?? x.PeriodCredit ?? 0), 0) : (sum.totalPeriodCredit ?? sum.TotalPeriodCredit ?? 0);
    const totalClDr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.closingDebit ?? x.ClosingDebit ?? 0), 0) : (sum.totalClosingDebit ?? sum.TotalClosingDebit ?? 0);
    const totalClCr = l1Items.length > 0 ? l1Items.reduce((acc, x) => acc + (x.closingCredit ?? x.ClosingCredit ?? 0), 0) : (sum.totalClosingCredit ?? sum.TotalClosingCredit ?? 0);

    const diff = Math.abs(totalClDr - totalClCr);
    const balanced = diff < 0.01;

    // Render 4 Summary Cards with Bangladeshi Taka (৳)
    const card = (label, value, detail = '', tone = '') =>
        `<div class="card-box ${tone}"><span>${label}</span><div class="big">${value}</div>${detail ? `<small>${detail}</small>` : ''}</div>`;

    const cardsEl = $('#cards');
    if (cardsEl.length) {
        cardsEl.html(
            card('Closing debit', taka(totalClDr)) +
            card('Closing credit', taka(totalClCr)) +
            card('Period debit / credit', taka(totalPDr), 'Cr ' + taka(totalPCr)) +
            card('Trial balance', balanced ? '✓ Balanced' : '⚠ Difference', balanced ? 'Debit = Credit' : taka(diff), balanced ? 'ok' : 'bad')
        );
    }

    // Render Table Rows (Clear accounting hierarchy layout matching School ERP)
    let rowsHtml = '';
    if (items.length === 0) {
        rowsHtml = `<tr><td colspan="7" class="text-center py-4 text-muted">No transactions or ledger records found for the selected criteria.</td></tr>`;
    } else {
        items.forEach(item => {
            const lvl = item.level ?? item.Level ?? 5;
            const code = item.code ?? item.Code ?? '';
            const name = item.name ?? item.Name ?? '';
            const opDr = item.openingDebit ?? item.OpeningDebit ?? 0;
            const opCr = item.openingCredit ?? item.OpeningCredit ?? 0;
            const perDr = item.periodDebit ?? item.PeriodDebit ?? 0;
            const perCr = item.periodCredit ?? item.PeriodCredit ?? 0;
            const clDr = item.closingDebit ?? item.ClosingDebit ?? 0;
            const clCr = item.closingCredit ?? item.ClosingCredit ?? 0;

            const rowClass = lvl <= 2 ? 'l' + lvl : lvl === 3 ? 'l3' : lvl === 4 ? 'l4' : '';
            const padLeft = 8 + (lvl - 1) * 16;
            const fontStyle = lvl <= 2 ? 'font-weight: 700;' : lvl === 3 ? 'font-weight: 600;' : lvl === 4 ? 'font-weight: 500;' : '';

            rowsHtml += `
                <tr class="${rowClass} tb-item-row" data-code="${esc(code)}" data-level="${lvl}">
                    <td style="padding-left: ${padLeft}px; ${fontStyle}">
                        <span class="tb-title">${esc(code ? code + ' - ' : '')}${esc(name)}</span>
                    </td>
                    <td class="num group-start">${fmt(opDr)}</td>
                    <td class="num">${fmt(opCr)}</td>
                    <td class="num group-start">${fmt(perDr)}</td>
                    <td class="num">${fmt(perCr)}</td>
                    <td class="num group-start">${fmt(clDr)}</td>
                    <td class="num">${fmt(clCr)}</td>
                </tr>
            `;
        });
    }

    const dateRangeLabel = (from || to) ? `${dmy(from)} to ${dmy(to)}` : 'All Dates';

    const reportEl = $('#report');
    if (reportEl.length) {
        reportEl.html(`
            <div class="report-head-print">
                <b>${esc(company)}</b>
                <h2>Trial Balance</h2>
                <div>${dateRangeLabel}</div>
            </div>
            <table class="report-table">
                <colgroup>
                    <col style="width:34%">
                    <col span="6">
                </colgroup>
                <thead>
                    <tr>
                        <th rowspan="2">Particulars</th>
                        <th colspan="2" class="group-header">Opening Balance</th>
                        <th colspan="2" class="group-header">Transactions</th>
                        <th colspan="2" class="group-header">Closing Balance</th>
                    </tr>
                    <tr>
                        <th class="num group-start">Debit</th><th class="num">Credit</th>
                        <th class="num group-start">Debit</th><th class="num">Credit</th>
                        <th class="num group-start">Debit</th><th class="num">Credit</th>
                    </tr>
                </thead>
                <tbody>
                    ${rowsHtml}
                    <tr class="total">
                        <td>Grand Total</td>
                        <td class="num group-start">${fmt(totalOpDr)}</td>
                        <td class="num">${fmt(totalOpCr)}</td>
                        <td class="num group-start">${fmt(totalPDr)}</td>
                        <td class="num">${fmt(totalPCr)}</td>
                        <td class="num group-start">${fmt(totalClDr)}</td>
                        <td class="num">${fmt(totalClCr)}</td>
                    </tr>
                </tbody>
            </table>
        `);
    }
}

function resetFilters() {
    if (defaultCompany) {
        setFilterVal('companyFilter', defaultCompany);
    }

    loadBranches(defaultCompany).then(() => {
        setFilterVal('branch', 'All');
    });

    setFilterVal('level', '5');

    if (fpFrom) {
        fpFrom.setDate(defaultFrom || null, false);
        if (fpTo) fpTo.set('minDate', null);
    } else {
        $('#from').val(defaultFrom);
    }

    if (fpTo) {
        fpTo.setDate(defaultTo || null, false);
    } else {
        $('#to').val(defaultTo);
    }

    fetchReportData();
}

function exportReport(kind) {
    const company = getFilterVal('companyFilter') || '';
    const branch = getFilterVal('branch') || 'All';
    const from = $('#from').val() || '';
    const to = $('#to').val() || '';
    const level = getFilterVal('level') || '5';

    const baseUrl = `/TrialBalance/ExportPdf?Main_CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(from)}&ToDate=${encodeURIComponent(to)}&Level=${encodeURIComponent(level)}`;

    if (kind === 'print' || kind === 'preview') {
        window.open(baseUrl, '_blank');
    } else if (kind === 'pdf') {
        const url = baseUrl + '&download=true';
        const link = document.createElement('a');
        link.href = url;
        link.target = '_blank';
        document.body.appendChild(link);
        link.click();
        link.remove();
    } else if (kind === 'excel') {
        const reportContent = document.getElementById('report');
        if (!reportContent) return;

        const clone = reportContent.cloneNode(true);
        let contentHtml = clone.innerHTML;
        contentHtml = contentHtml.replace(/<colgroup>[\s\S]*?<\/colgroup>/i, `
            <colgroup>
                <col width="370" style="width: 370pt;">
                <col width="100" style="width: 92pt;">
                <col width="100" style="width: 92pt;">
                <col width="100" style="width: 92pt;">
                <col width="100" style="width: 92pt;">
                <col width="100" style="width: 92pt;">
                <col width="100" style="width: 92pt;">
            </colgroup>
        `);

        const html = `
            <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40">
            <head>
                <meta charset="utf-8">
                <!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>TrialBalance</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]-->
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; }
                    .report-head-print { text-align: center; margin-bottom: 12px; }
                    .report-head-print b { font-size: 13pt; color: #18262b; display: block; }
                    .report-head-print h2 { font-size: 11pt; margin: 2px 0; color: #18262b; }
                    .report-head-print div { font-size: 9pt; color: #5d6d68; }
                    table { border-collapse: collapse; }
                    td, th { border: 0.5pt solid #dcdfe3; padding: 4px 6px; font-family: 'Segoe UI', Arial, sans-serif; font-size: 9.5pt; color: #18262b; }
                    th { background: #f7f8f9 !important; color: #18262b !important; font-weight: bold; text-align: center; }
                    th.group { background: #f7f8f9 !important; }
                    .num { text-align: right; mso-number-format: "\\#\\,\\#\\#0\\.00"; white-space: nowrap; width: 72pt; }
                    tr.l1 td { font-weight: bold; background: #f2f4f5 !important; }
                    tr.l2 td { font-weight: bold; background: #ffffff !important; }
                    tr.l3 td { background: #ffffff !important; }
                    tr.total td { font-weight: bold; background: #f7f8f9 !important; border-top: 1.5pt solid #222222 !important; border-bottom: 2.5pt double #222222 !important; }
                </style>
            </head>
            <body>
                ${contentHtml}
            </body>
            </html>
        `;

        const link = document.createElement('a');
        link.href = URL.createObjectURL(new Blob(['\ufeff' + html], { type: 'application/vnd.ms-excel;charset=utf-8' }));
        link.download = `Trial_Balance_${new Date().toISOString().slice(0, 10)}.xls`;
        document.body.appendChild(link);
        link.click();
        link.remove();
    }
}
