/**
 * General Ledger Report matching School ERP Mockup Design & Grid
 * With custom-select-single and Flatpickr plugins integration
 */

(function () {
    'use strict';

    let fpFrom = null;
    let fpTo = null;

    const fmt = n => {
        n = Math.round((+n + Number.EPSILON) * 100) / 100;
        if (!n) n = 0;
        return n.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    const taka = n => '৳ ' + fmt(n);
    const bl = n => Math.abs(n) < .005 ? '0.00' : fmt(Math.abs(n)) + (n > 0 ? ' Dr' : ' Cr');
    const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
    const card = (l, v, s, c) => `<div class="card-box ${c || ''}"><span>${l}</span><div class="big">${v}</div>${s ? `<small>${s}</small>` : ''}</div>`;

    function toast(m, c) {
        const t = document.getElementById('toast');
        if (!t) return;
        t.textContent = m;
        t.className = c || '';
        t.style.display = 'block';
        clearTimeout(t._t);
        t._t = setTimeout(() => t.style.display = 'none', 3200);
    }

    function getFilterVal(id) {
        return $('#' + id).val();
    }

    function initDatePickers() {
        if (typeof flatpickr !== 'undefined') {
            const initialFrom = $('#fFrom').val();
            const initialTo = $('#fTo').val();

            fpFrom = flatpickr('#fFrom', {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                defaultDate: initialFrom,
                allowInput: true,
                onChange: function (selectedDates) {
                    if (selectedDates.length > 0 && fpTo) {
                        fpTo.set('minDate', selectedDates[0]);
                    }
                    loadReport();
                }
            });

            fpTo = flatpickr('#fTo', {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                defaultDate: initialTo,
                allowInput: true,
                onChange: function () {
                    loadReport();
                }
            });
        }
    }

    function toIso(val) {
        if (!val) return null;
        if (val.indexOf('/') !== -1) {
            const parts = val.split('/');
            if (parts.length === 3) return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
        }
        return val;
    }

    function loadReport() {
        const company = getFilterVal('fCo');
        const branch = getFilterVal('fBr');
        const accountHead = getFilterVal('fAcc');
        const fromDate = toIso($('#fFrom').val());
        const toDate = toIso($('#fTo').val());

        $.ajax({
            url: '/GeneralLedgerReport/GetReport',
            type: 'GET',
            data: {
                CompanyCode: company,
                BranchCode: branch,
                GeneralLedgerCodeNo: accountHead,
                FromDate: fromDate,
                ToDate: toDate
            },
            success: function (res) {
                if (res && res.success && res.data) {
                    render(res.data);
                } else {
                    renderEmpty(res ? res.message : 'No data found.');
                }
            },
            error: function () {
                renderEmpty('An error occurred while loading the ledger.');
            }
        });
    }

    function render(d) {
        // Synchronize account head selection if backend selected default
        if (d.accountCode && getFilterVal('fAcc') !== d.accountCode) {
            if (typeof window.setCustomDropdownValue === 'function') {
                window.setCustomDropdownValue('fAcc', d.accountCode, d.accountName);
            } else {
                $('#fAcc').val(d.accountCode);
            }
        }

        // Subtitle and headers
        if (d.companyName) {
            $('#coName').text(d.companyName + ' · Amounts in Bangladeshi Taka (৳)');
            $('#pheadCo').text(d.companyName);
        }
        $('#pheadSub').text(`${d.accountCode} – ${d.accountName} · ${d.fromDateFormatted} to ${d.toDateFormatted} · ${d.branchName || 'All branches'}`);

        // Subtitle and Breadcrumb Path
        $('#glSub').text(`${d.accountCode} – ${d.accountName} · ${d.fromDateFormatted} to ${d.toDateFormatted}`);
        $('#glPath').text(d.hierarchyPath || '');

        // 4 KPI Summary Cards matching Mockup
        const cardsHtml =
            card('Opening balance', bl(d.openingBalance), d.fromDateFormatted) +
            card('Total debit', taka(d.totalDebit)) +
            card('Total credit', taka(d.totalCredit)) +
            card('Closing balance', bl(d.closingBalance), d.accountName, 'ok');

        $('#c-gl').html(cardsHtml);

        // Table Rows matching Mockup table.g
        const tbody = $('#glTableBody');
        tbody.empty();

        const rows = [];
        // 1. Opening balance row: tr.l2
        rows.push(`<tr class="l2"><td colspan="6">Opening balance</td><td class="num">${bl(d.openingBalance)}</td></tr>`);

        // 2. Transaction Rows
        if (d.items && d.items.length > 0) {
            d.items.forEach(function (tx) {
                rows.push(`<tr>` +
                    `<td>${esc(tx.dateFormatted)}</td>` +
                    `<td class="fw-bold" style="color: var(--gl-pri);">${esc(tx.voucherNo)}</td>` +
                    `<td style="color: var(--gl-mut);">${esc(tx.invoiceNo || '–')}</td>` +
                    `<td>${esc(tx.narration || '')}</td>` +
                    `<td class="num">${tx.debit > 0 ? fmt(tx.debit) : ''}</td>` +
                    `<td class="num">${tx.credit > 0 ? fmt(tx.credit) : ''}</td>` +
                    `<td class="num fw-semibold">${bl(tx.runningBalance)}</td>` +
                    `</tr>`);
            });
        } else {
            rows.push(`<tr><td colspan="7" class="empty-cell">No transactions in this period.</td></tr>`);
        }

        // 3. Total / Closing row: tr.tot
        rows.push(`<tr class="tot"><td colspan="4">Total / Closing</td><td class="num">${fmt(d.totalDebit)}</td><td class="num">${fmt(d.totalCredit)}</td><td class="num">${bl(d.closingBalance)}</td></tr>`);
        tbody.html(rows.join(''));
    }

    function renderEmpty(msg) {
        $('#c-gl').html(
            card('Opening balance', '0.00', '') +
            card('Total debit', '৳ 0.00') +
            card('Total credit', '৳ 0.00') +
            card('Closing balance', '0.00', '', 'bad')
        );
        $('#glSub').text('');
        $('#glPath').text('');
        $('#glTableBody').html(`<tr><td colspan="7" class="empty-cell">${esc(msg)}</td></tr>`);
    }

    function exportExcel() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const accountHead = getFilterVal('fAcc') || '';
        const fromDate = toIso($('#fFrom').val()) || '';
        const toDate = toIso($('#fTo').val()) || '';

        const url = `/GeneralLedgerReport/ExportExcel?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&GeneralLedgerCodeNo=${encodeURIComponent(accountHead)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function previewPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const accountHead = getFilterVal('fAcc') || '';
        const fromDate = toIso($('#fFrom').val()) || '';
        const toDate = toIso($('#fTo').val()) || '';

        const url = `/GeneralLedgerReport/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&GeneralLedgerCodeNo=${encodeURIComponent(accountHead)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}&preview=true`;
        window.open(url, '_blank');
    }

    function downloadPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const accountHead = getFilterVal('fAcc') || '';
        const fromDate = toIso($('#fFrom').val()) || '';
        const toDate = toIso($('#fTo').val()) || '';

        const url = `/GeneralLedgerReport/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&GeneralLedgerCodeNo=${encodeURIComponent(accountHead)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function onCompanyChange() {
        const companyCode = getFilterVal('fCo');
        $.getJSON('/GeneralLedgerReport/GetBranches', { companyCode: companyCode }, function (branches) {
            const $b = $('#fBr');
            $b.empty();
            $b.append('<option value="All">All Branches</option>');
            if (branches && branches.length > 0) {
                branches.forEach(function (br) {
                    $b.append(`<option value="${esc(br.code)}">${esc(br.name)}</option>`);
                });
            }
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('fBr');
            }
            loadReport();
        });
    }

    function resetFilters() {
        const firstCo = $('#fCo option:first').val();
        if (firstCo) $('#fCo').val(firstCo);
        $('#fBr').val('All');
        const defaultAcc = $('#fAcc option[value="1020040000100001"]').length ? '1020040000100001' : $('#fAcc option:first').val();
        if (defaultAcc) $('#fAcc').val(defaultAcc);

        if (typeof window.reinitCustomDropdownById === 'function') {
            window.reinitCustomDropdownById('fCo');
            window.reinitCustomDropdownById('fBr');
            window.reinitCustomDropdownById('fAcc');
        }

        const now = new Date();
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const firstDay = `${year}-${month}-01`;
        const lastDate = new Date(year, now.getMonth() + 1, 0).getDate();
        const lastDay = `${year}-${month}-${String(lastDate).padStart(2, '0')}`;

        if (fpFrom) {
            fpFrom.setDate(firstDay, false);
        } else {
            $('#fFrom').val(firstDay);
        }

        if (fpTo) {
            fpTo.setDate(lastDay, false);
        } else {
            $('#fTo').val(lastDay);
        }

        loadReport();
    }

    window.resetFilters = resetFilters;

    $(document).ready(function () {
        if (typeof window.initCustomDropdowns === 'function') {
            window.initCustomDropdowns('#rf', false);
        }

        initDatePickers();

        // Ensure default account head is 1020040000100001 (Cash In Hand) if available
        if ($('#fAcc option[value="1020040000100001"]').length) {
            if (typeof window.setCustomDropdownValue === 'function') {
                window.setCustomDropdownValue('fAcc', '1020040000100001');
            } else {
                $('#fAcc').val('1020040000100001');
            }
        }

        // Automatic update on any filter change
        $('#fCo').on('change', onCompanyChange);
        $('#fBr, #fAcc').on('change', loadReport);
        $('#fFrom, #fTo').on('change', loadReport);

        // Reset Filters button
        $('#btnResetFilters').on('click', resetFilters);

        // Export Dropdown Action
        $('#glExportAction').on('change', function () {
            const act = $(this).val();
            $(this).val(''); // Reset selection for next use

            if (act === 'p') {
                previewPdf();
            } else if (act === 'pdf') {
                downloadPdf();
            } else if (act === 'x') {
                exportExcel();
            }
        });

        // Initial Load
        loadReport();
    });

})();
