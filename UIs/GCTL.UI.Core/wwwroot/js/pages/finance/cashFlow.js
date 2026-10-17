/**
 * Cash Flow Statement (Direct Method) Script
 * Matching School ERP Accounting Design
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
const sg = value => value < 0 ? `(${fmt(-value)})` : fmt(value);
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
}

function initExportDropdown() {
    $('#exportFormatSelect').on('change', function () {
        const format = $(this).val();
        $(this).val(''); // Reset dropdown selection

        if (!format) return;

        if (format === 'preview') {
            exportPdfPreview();
        } else if (format === 'pdf') {
            exportPdf();
        } else if (format === 'excel') {
            exportExcel();
        }
    });
}

function getFilterPayload() {
    const toIso = val => {
        if (!val) return null;
        if (val.indexOf('/') !== -1) {
            const parts = val.split('/');
            if (parts.length === 3) return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
        }
        return val;
    };

    return {
        Main_CompanyCode: getFilterVal('companyFilter'),
        BranchCode: getFilterVal('branch'),
        FromDate: toIso($('#from').val()),
        ToDate: toIso($('#to').val())
    };
}

function fetchReportData() {
    const payload = getFilterPayload();

    $('#report').html(`
        <div class="text-center py-5">
            <div class="spinner-border text-success" role="status"></div>
            <p class="mt-2 text-muted">Loading Cash Flow report...</p>
        </div>
    `);

    $.ajax({
        url: '/CashFlow/GetReportData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (res) {
            if (res.success && res.data) {
                reportData = res.data;
                renderSummaryCards(reportData.summary);
                renderCashFlowTable(reportData);
                updatePrintHeader(reportData);
            } else {
                $('#report').html(`<div class="alert alert-danger m-3">${esc(res.message || 'Failed to load report data.')}</div>`);
            }
        },
        error: function (xhr, status, error) {
            $('#report').html(`<div class="alert alert-danger m-3">An error occurred while fetching report data: ${esc(error)}</div>`);
        }
    });
}

function renderSummaryCards(summary) {
    if (!summary) return;

    const opNet = Number(summary.operatingCashFlow ?? summary.OperatingCashFlow) || 0;
    const invNet = Number(summary.investingCashFlow ?? summary.InvestingCashFlow) || 0;
    const finNet = Number(summary.financingCashFlow ?? summary.FinancingCashFlow) || 0;
    const closing = Number(summary.closingCashAndBank ?? summary.ClosingCashAndBank) || 0;
    const isReconciled = summary.isReconciled ?? summary.IsReconciled ?? false;
    const diff = Number(summary.difference ?? summary.Difference) || 0;

    const cardsHtml = `
        <div class="card-box">
            <span>Operating</span>
            <div class="big">${taka(opNet)}</div>
            <small>Net cash from operations</small>
        </div>
        <div class="card-box">
            <span>Investing</span>
            <div class="big">${taka(invNet)}</div>
            <small>Net cash from investments</small>
        </div>
        <div class="card-box">
            <span>Financing</span>
            <div class="big">${taka(finNet)}</div>
            <small>Net cash from financing</small>
        </div>
        <div class="card-box ${isReconciled ? 'ok' : 'bad'}">
            <span>Closing cash & bank</span>
            <div class="big">${taka(closing)}</div>
            <small>${isReconciled ? '✔ Reconciled' : '✖ Diff: ' + taka(Math.abs(diff))}</small>
        </div>
    `;

    $('#cards').html(cardsHtml);
}

function renderCashFlowTable(data) {
    const activities = data.activities ?? data.Activities ?? [];
    const summary = data.summary ?? data.Summary ?? {};

    let html = `
        <table class="report-table">
            <thead>
                <tr>
                    <th style="width: 50%;">Particulars</th>
                    <th class="num" style="width: 16%;">Receipts (৳)</th>
                    <th class="num" style="width: 16%;">Payments (৳)</th>
                    <th class="num" style="width: 18%;">Net Cash (৳)</th>
                </tr>
            </thead>
            <tbody>
    `;

    if (activities.length === 0) {
        html += `
            <tr>
                <td colspan="4" class="empty-state">No cash flow activity found for the selected criteria.</td>
            </tr>
        `;
    } else {
        activities.forEach(group => {
            const groupTitle = group.title ?? group.Title ?? '';
            const activityType = group.activityType ?? group.ActivityType ?? '';
            const items = group.items ?? group.Items ?? [];
            const totalReceipts = Number(group.totalReceipts ?? group.TotalReceipts) || 0;
            const totalPayments = Number(group.totalPayments ?? group.TotalPayments) || 0;
            const netCashFlow = Number(group.netCashFlow ?? group.NetCashFlow) || 0;

            html += `
                <tr class="act-group">
                    <td colspan="4">${esc(groupTitle)}</td>
                </tr>
            `;

            if (items.length === 0) {
                html += `
                    <tr class="act-item">
                        <td colspan="4" style="color: var(--mut); font-style: italic; padding-left: 28px;">No activity in this period</td>
                    </tr>
                `;
            } else {
                items.forEach(item => {
                    const rcptVal = Number(item.receipts ?? item.Receipts) || 0;
                    const pmtVal = Number(item.payments ?? item.Payments) || 0;
                    const netVal = Number(item.netAmount ?? item.NetAmount) || 0;
                    const displayLabel = item.displayLabel ?? item.DisplayLabel ?? item.particularName ?? item.ParticularName ?? '';

                    const rcpt = rcptVal > 0 ? fmt(rcptVal) : '-';
                    const pmt = pmtVal > 0 ? fmt(pmtVal) : '-';
                    const net = sg(netVal);

                    html += `
                        <tr class="act-item">
                            <td>${esc(displayLabel)}</td>
                            <td class="num">${rcpt}</td>
                            <td class="num">${pmt}</td>
                            <td class="num">${net}</td>
                        </tr>
                    `;
                });
            }

            // Subtotal row for group
            html += `
                <tr class="act-subtotal">
                    <td>Net cash from ${esc(activityType.toLowerCase())} activities</td>
                    <td class="num">${fmt(totalReceipts)}</td>
                    <td class="num">${fmt(totalPayments)}</td>
                    <td class="num">${sg(netCashFlow)}</td>
                </tr>
            `;
        });

        // Summary reconciliation rows
        const netChange = Number(summary.netCashFlow ?? summary.NetCashFlow) || 0;
        const opCash = Number(summary.openingCashAndBank ?? summary.OpeningCashAndBank) || 0;
        const clCash = Number(summary.closingCashAndBank ?? summary.ClosingCashAndBank) || 0;

        html += `
            <tr class="summary-row">
                <td colspan="3">Net increase / (decrease) in cash & bank</td>
                <td class="num">${sg(netChange)}</td>
            </tr>
            <tr class="act-subtotal">
                <td colspan="3">Cash & bank at beginning of period</td>
                <td class="num">${fmt(opCash)}</td>
            </tr>
            <tr class="grand-total">
                <td colspan="3">Cash & bank at end of period</td>
                <td class="num">${fmt(clCash)}</td>
            </tr>
        `;
    }

    html += `
            </tbody>
        </table>
    `;

    $('#report').html(html);
}

function updatePrintHeader(data) {
    const fromStr = $('#from').val() ? dmy($('#from').val()) : 'Start';
    const toStr = $('#to').val() ? dmy($('#to').val()) : 'End';
    $('#printSubTitle').text(`${fromStr} to ${toStr}`);
}

function loadBranches(companyCode) {
    return new Promise((resolve) => {
        if (!companyCode) {
            resolve();
            return;
        }

        $.ajax({
            url: `/CashFlow/GetBranches?companyCode=${encodeURIComponent(companyCode)}`,
            type: 'GET',
            success: function (branches) {
                let options = '<option value="All">All Branches</option>';
                if (Array.isArray(branches)) {
                    branches.forEach(b => {
                        const code = b.code ?? b.Code ?? '';
                        const name = b.name ?? b.Name ?? '';
                        options += `<option value="${code}">${esc(name)}</option>`;
                    });
                }
                $('#branch').html(options);
                $('#branch').val('All');

                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById('branch');
                }
                resolve();
            },
            error: function () {
                resolve();
            }
        });
    });
}

function resetFilters() {
    if (defaultCompany) {
        $('#companyFilter').val(defaultCompany);
    } else {
        $('#companyFilter').prop('selectedIndex', 0);
    }

    loadBranches(defaultCompany).then(() => {
        $('#branch').val('All');
        if (typeof window.reinitCustomDropdownById === 'function') {
            window.reinitCustomDropdownById('branch');
        }
    });

    if (typeof window.reinitCustomDropdownById === 'function') {
        window.reinitCustomDropdownById('companyFilter');
    }

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

window.resetFilters = resetFilters;

function exportPdfPreview() {
    const payload = getFilterPayload();
    const query = $.param({
        Main_CompanyCode: payload.Main_CompanyCode || '',
        BranchCode: payload.BranchCode || '',
        FromDate: payload.FromDate || '',
        ToDate: payload.ToDate || '',
        download: false
    });
    window.open(`/CashFlow/ExportPdf?${query}`, '_blank');
}

function exportPdf() {
    const payload = getFilterPayload();
    const query = $.param({
        Main_CompanyCode: payload.Main_CompanyCode || '',
        BranchCode: payload.BranchCode || '',
        FromDate: payload.FromDate || '',
        ToDate: payload.ToDate || '',
        download: true
    });
    window.location.href = `/CashFlow/ExportPdf?${query}`;
}

function exportExcel() {
    const payload = getFilterPayload();
    const query = $.param({
        Main_CompanyCode: payload.Main_CompanyCode || '',
        BranchCode: payload.BranchCode || '',
        FromDate: payload.FromDate || '',
        ToDate: payload.ToDate || ''
    });
    window.location.href = `/CashFlow/ExportExcel?${query}`;
}
