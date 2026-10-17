/**
 * Income Statement Report
 * Matching School ERP Mockup Design & Hierarchy Grid
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

    function loadReport() {
        const company = getFilterVal('fCo');
        const branch = getFilterVal('fBr');
        const fromDate = $('#fFrom').val();
        const toDate = $('#fTo').val();

        $('#isTableBody').html('<tr><td colspan="2" class="text-center py-4 text-muted"><i class="fas fa-spinner fa-spin me-2"></i>Loading Income Statement...</td></tr>');

        $.ajax({
            url: '/IncomeStatement/GetReport',
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
            error: function (xhr) {
                const msg = (xhr.responseJSON && xhr.responseJSON.message) ? xhr.responseJSON.message : 'An error occurred while loading the income statement.';
                renderEmpty(msg);
            }
        });
    }

    function renderCards(d) {
        let totalIncome = Number(d.totalIncome ?? d.TotalIncome) || 0;
        let totalExpenses = Number(d.totalExpenses ?? d.TotalExpenses) || 0;
        let netSurplus = Number(d.netSurplus ?? d.NetSurplus) || 0;
        const items = d.items ?? d.Items ?? [];

        // Fallback: If totalIncome or totalExpenses are 0, check if items contain total rows or calculate
        if (items.length > 0) {
            if (!totalIncome) {
                const incTot = items.find(x => (x.isTotal ?? x.IsTotal) && ((x.section ?? x.Section) === 'Income' || ((x.name ?? x.Name) && (x.name ?? x.Name).toLowerCase().includes('total income'))));
                if (incTot) totalIncome = Number(incTot.amount ?? incTot.Amount) || 0;
                else {
                    totalIncome = items
                        .filter(x => !(x.isTotal ?? x.IsTotal) && (x.section ?? x.Section) === 'Income' && ((x.level ?? x.Level) === 5 || ((x.level ?? x.Level) === 2 && !items.some(c => (c.level ?? c.Level) === 5 && (c.section ?? c.Section) === 'Income'))))
                        .reduce((sum, x) => sum + (Number(x.amount ?? x.Amount) || 0), 0);
                }
            }
            if (!totalExpenses) {
                const expTot = items.find(x => (x.isTotal ?? x.IsTotal) && ((x.section ?? x.Section) === 'Expense' || ((x.name ?? x.Name) && (x.name ?? x.Name).toLowerCase().includes('total expense'))));
                if (expTot) totalExpenses = Number(expTot.amount ?? expTot.Amount) || 0;
                else {
                    totalExpenses = items
                        .filter(x => !(x.isTotal ?? x.IsTotal) && (x.section ?? x.Section) === 'Expense' && ((x.level ?? x.Level) === 5 || ((x.level ?? x.Level) === 2 && !items.some(c => (c.level ?? c.Level) === 5 && (c.section ?? c.Section) === 'Expense'))))
                        .reduce((sum, x) => sum + (Number(x.amount ?? x.Amount) || 0), 0);
                }
            }
            if (!netSurplus && (totalIncome || totalExpenses)) {
                const netTot = items.find(x => (x.isTotal ?? x.IsTotal) && ((x.section ?? x.Section) === 'Net' || ((x.name ?? x.Name) && ((x.name ?? x.Name).toLowerCase().includes('net surplus') || (x.name ?? x.Name).toLowerCase().includes('net deficit')))));
                if (netTot) netSurplus = Number(netTot.amount ?? netTot.Amount) || 0;
                else netSurplus = totalIncome - totalExpenses;
            }
        }

        const isSurplus = netSurplus >= 0;
        const netLabel = isSurplus ? 'Net surplus' : 'Net deficit';
        let marginStr = d.surplusMargin ?? d.SurplusMargin;
        if (!marginStr || marginStr === '0.0%') {
            if (Math.abs(totalIncome) > 0) {
                marginStr = ((netSurplus / Math.abs(totalIncome)) * 100).toFixed(1) + '%';
            } else {
                marginStr = '0.0%';
            }
        }

        const cardsHtml =
            card('Total income', taka(Math.abs(totalIncome))) +
            card('Total expenses', taka(Math.abs(totalExpenses))) +
            card(netLabel, taka(Math.abs(netSurplus)), '', isSurplus ? 'ok' : 'bad') +
            card('Surplus margin', marginStr);

        $('#cards').html(cardsHtml);
    }

    function render(d) {
        // Subtitle and headers
        const compName = d.companyName ?? d.CompanyName;
        if (compName) {
            $('#coName').text(compName + ' · Amounts in Bangladeshi Taka (৳)');
            $('#pheadCo').text(compName);
        }
        const fromFmt = d.fromDateFormatted ?? d.FromDateFormatted ?? '';
        const toFmt = d.toDateFormatted ?? d.ToDateFormatted ?? '';
        const brName = d.branchName ?? d.BranchName ?? 'All branches';
        $('#pheadSub').text(`${fromFmt} to ${toFmt} · ${brName}`);

        // Render Dashboard KPI Cards
        renderCards(d);

        // Table Rows
        const tbody = $('#isTableBody');
        tbody.empty();

        const items = d.items ?? d.Items ?? [];
        if (items && items.length > 0) {
            items.forEach(function (row) {
                const isTotal = row.isTotal ?? row.IsTotal ?? false;
                const level = row.level ?? row.Level ?? 0;
                const name = row.name ?? row.Name ?? row.particularName ?? row.ParticularName ?? '';
                const code = row.code ?? row.Code ?? '';
                const amount = Number(row.amount ?? row.Amount) || 0;
                const section = row.section ?? row.Section ?? '';
                const displayText = row.displayText ?? row.DisplayText ?? (code ? (code + ' – ' + name) : name);

                let tr = '';
                if (isTotal) {
                    const isNet = section === 'Net' || name.toLowerCase().includes('net');
                    const totClass = isNet ? 'tot net' : 'tot';
                    tr = `<tr class="${totClass}"><td class="fw-bold">${esc(name)}</td><td class="num fw-bold">${sg(amount)}</td></tr>`;
                } else if (level === 1) {
                    tr = `<tr class="l1"><td colspan="2" class="fw-bold">${esc(name)}</td></tr>`;
                } else {
                    const rowClass = level === 2 ? 'l2' : (level === 3 ? 'l3' : (level === 4 ? 'l4' : 'lf'));
                    tr = `<tr class="${rowClass}"><td>${esc(displayText)}</td><td class="num">${sg(amount)}</td></tr>`;
                }
                tbody.append(tr);
            });
        } else {
            tbody.append(`<tr><td colspan="2" class="empty-cell">No transactions in this period.</td></tr>`);
        }
    }

    function renderEmpty(msg) {
        $('#isTableBody').html(`<tr><td colspan="2" class="empty-cell">${esc(msg)}</td></tr>`);
        const emptyCards =
            card('Total income', taka(0)) +
            card('Total expenses', taka(0)) +
            card('Net surplus', taka(0), '', 'ok') +
            card('Surplus margin', '0.0%');
        $('#cards').html(emptyCards);
    }

    function previewPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/IncomeStatement/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}&preview=true`;
        window.open(url, '_blank');
    }

    function downloadPdf() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/IncomeStatement/ExportPdf?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function exportExcel() {
        const company = getFilterVal('fCo') || '';
        const branch = getFilterVal('fBr') || '';
        const fromDate = $('#fFrom').val() || '';
        const toDate = $('#fTo').val() || '';

        const url = `/IncomeStatement/ExportExcel?CompanyCode=${encodeURIComponent(company)}&BranchCode=${encodeURIComponent(branch)}&FromDate=${encodeURIComponent(fromDate)}&ToDate=${encodeURIComponent(toDate)}`;
        window.location.href = url;
    }

    function onCompanyChange() {
        const companyId = getFilterVal('fCo');
        $.getJSON('/IncomeStatement/GetBranches', { companyCode: companyId, companyId: companyId }, function (branches) {
            const $b = $('#fBr');
            $b.empty();
            $b.append('<option value="All">All Branches</option>');
            if (branches && branches.length > 0) {
                branches.forEach(function (br) {
                    const code = br.code ?? br.Code ?? '';
                    const name = br.name ?? br.Name ?? '';
                    $b.append(`<option value="${esc(code)}">${esc(name)}</option>`);
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

        // Automatic update on any filter change
        $('#fCo').on('change', onCompanyChange);
        $('#fBr').on('change', loadReport);
        if (typeof flatpickr === 'undefined') {
            $('#fFrom, #fTo').on('change', loadReport);
        }

        // Reset Filters button
        $('#btnResetFilters').on('click', resetFilters);

        // Export Dropdown Action
        $('#isExportAction').on('change', function () {
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
