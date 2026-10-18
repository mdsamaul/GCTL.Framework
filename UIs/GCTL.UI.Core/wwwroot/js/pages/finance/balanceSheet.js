/**
 * Balance Sheet Report
 * Matching School ERP Mockup Design & Two-Column Grid
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
    const sg = n => n < 0 ? '(' + fmt(Math.abs(n)) + ')' : fmt(n);
    const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
    const card = (l, v, s, c) => `<div class="kpi-card ${c || ''}"><span>${l}</span><div class="big">${v}</div>${s ? `<small>${s}</small>` : ''}</div>`;

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

    function loadReport() {
        const company = getFilterVal('fCo');
        const branch = getFilterVal('fBr');
        const fromDate = $('#fFrom').val();
        const toDate = $('#fTo').val();

        $.ajax({
            url: '/BalanceSheet/GetReport',
            type: 'GET',
            data: {
                CompanyCode: company,
                BranchCode: branch,
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
                renderEmpty('An error occurred while loading the balance sheet.');
            }
        });
    }

    function render(d) {
        // Subtitle and headers
        if (d.companyName) {
            $('#coName').text(d.companyName + ' · Amounts in Bangladeshi Taka (৳)');
            $('#pheadCo').text(d.companyName);
        }
        $('#pheadSub').text(`As at ${d.toDateFormatted} · ${d.branchName || 'All branches'}`);

        // 4 KPI Summary Cards matching Mockup
        const cardsHtml =
            card('Total assets', taka(d.totalAssets)) +
            card('Total liabilities', taka(d.totalLiabilities)) +
            card('Equity (incl. surplus)', taka(d.totalEquity)) +
            card('Balance check', d.balanceCheckLabel, d.balanceCheckSub, d.balanceCheckClass);

        $('#c-bs').html(cardsHtml);

        // Assets Table Body
        const assetsTbody = $('#bsAssetsTableBody');
        assetsTbody.empty();

        if (d.assetsItems && d.assetsItems.length > 0) {
            d.assetsItems.forEach(function (row) {
                const rowClass = row.level === 2 ? 'l2' : (row.level === 3 ? 'l3' : 'lf');
                const text = row.displayText || row.name;
                assetsTbody.append(`<tr class="${rowClass}"><td>${esc(text)}</td><td class="num">${sg(row.amount)}</td></tr>`);
            });
            assetsTbody.append(`<tr class="tot"><td>Total Assets</td><td class="num">${sg(d.totalAssetsAmount)}</td></tr>`);
        } else {
            assetsTbody.append(`<tr><td colspan="2" class="empty-state">No asset accounts found.</td></tr>`);
        }

        // Equities & Liabilities Table Body
        const liabTbody = $('#bsLiabilitiesTableBody');
        liabTbody.empty();

        if (d.equitiesAndLiabilitiesItems && d.equitiesAndLiabilitiesItems.length > 0) {
            d.equitiesAndLiabilitiesItems.forEach(function (row) {
                let rowClass = row.level === 2 ? 'l2' : (row.level === 3 ? 'l3' : 'lf');
                if (row.isHighlighted) rowClass += ' highlighted';
                const text = row.displayText || row.name;
                liabTbody.append(`<tr class="${rowClass}"><td>${esc(text)}</td><td class="num">${sg(row.amount)}</td></tr>`);
            });
            liabTbody.append(`<tr class="tot"><td>Total Equities & Liabilities</td><td class="num">${sg(d.totalEquitiesAndLiabilitiesAmount)}</td></tr>`);
        } else {
            liabTbody.append(`<tr><td colspan="2" class="empty-state">No liability or equity accounts found.</td></tr>`);
        }
    }

    function renderEmpty(msg) {
        $('#c-bs').html(
            card('Total assets', '৳ 0.00') +
            card('Total liabilities', '৳ 0.00') +
            card('Equity (incl. surplus)', '৳ 0.00') +
            card('Balance check', '✔ Balanced', 'Assets = Equities & Liabilities', 'ok')
        );
        $('#bsAssetsTableBody').html(`<tr><td colspan="2" class="empty-state">${esc(msg)}</td></tr>`);
        $('#bsLiabilitiesTableBody').html(`<tr><td colspan="2" class="empty-state">${esc(msg)}</td></tr>`);
    }

    function previewPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/BalanceSheet/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}&preview=true`;
        window.open(url, '_blank');
    }

    function downloadPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/BalanceSheet/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function exportExcel() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/BalanceSheet/ExportExcel?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function onCompanyChange() {
        const companyId = getFilterVal('fCo');
        $.getJSON('/BalanceSheet/GetBranches', { companyId: companyId }, function (branches) {
            const $b = $('#fBr');
            $b.empty();
            $b.append('<option value="All">All Branches</option>');
            if (branches && branches.length > 0) {
                branches.forEach(function (br) {
                    $b.append(`<option value="${esc(br.code)}">${esc(br.name)}</option>`);
                });
            }
            $b.val('All');
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('fBr');
            }
            loadReport();
        });
    }

    function resetFilters() {
        $('#fCo').prop('selectedIndex', 0);
        $('#fBr').val('All');

        if (typeof window.reinitCustomDropdownById === 'function') {
            window.reinitCustomDropdownById('fCo');
            window.reinitCustomDropdownById('fBr');
        }

        const today = new Date();
        const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
        const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);

        if (fpFrom) fpFrom.setDate(firstDay, false);
        if (fpTo) fpTo.setDate(lastDay, false);

        loadReport();
    }

    $(document).ready(function () {
        if (typeof window.initCustomDropdowns === 'function') {
            window.initCustomDropdowns('#rf', false);
        }

        initDatePickers();

        $('#fCo').on('change', onCompanyChange);
        $('#fBr').on('change', loadReport);
        $('#btnResetFilters').on('click', resetFilters);

        $('#bsExportAction').on('change', function () {
            const val = $(this).val();
            if (val === 'p') {
                previewPdf();
            } else if (val === 'pdf') {
                downloadPdf();
            } else if (val === 'x') {
                exportExcel();
            }
            $(this).val('');
        });

        loadReport();
    });

})();
