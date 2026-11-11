$(document).ready(function () {
    const CUT = [1, 3, 6, 11, 16];

    function esc(s) {
        return $('<div>').text(s || '').html();
    }

    // Segmented Code Renderer with s0-s4 Background Colors
    function seg(c) {
        if (!c) return '';
        let o = '', a = 0;
        for (let i = 0; i < 5 && a < c.length; i++) {
            o += `<i class="s${i}">${c.slice(a, CUT[i])}</i>`;
            a = CUT[i];
        }
        return `<code class="coa-seg">${o}</code>`;
    }

    function renderList(flatList) {
        const validList = (flatList || []).filter(item => item && item.generalLedgerCode && item.generalLedgerCode.trim() !== '');

        $('#coa-listCount').text(`${validList.length} General Ledgers`);

        if (!validList || validList.length === 0) {
            $('#listViewContainer').html(`
                <div class="coa-em py-5">
                    <i class="fas fa-folder-open fa-2x mb-2 text-muted"></i>
                    <p class="mb-0">No general ledgers found matching criteria.</p>
                </div>
            `);
            return;
        }

        let rows = '';
        validList.forEach(function (item) {
            const shortHtml = item.shortName ? ` <small class="text-muted">(${esc(item.shortName)})</small>` : '';
            const statusHtml = item.isActive !== false ? '' : ' <span class="coar-badge-archived ms-1">Archived</span>';

            rows += `
                <tr>
                    <td>${seg(item.groupCode)} <span class="fw-semibold">${esc(item.groupName)}</span></td>
                    <td>${seg(item.controlCode)} <span>${esc(item.controlName)}</span></td>
                    <td>${seg(item.subControlCode)} <span>${esc(item.subControlName)}</span></td>
                    <td>${seg(item.subSubsidiaryCode)} <span>${esc(item.subSubsidiaryName)}</span></td>
                    <td><strong>${seg(item.generalLedgerCode)}</strong></td>
                    <td>
                        <span class="fw-bold text-dark">${esc(item.generalLedgerName)}</span>${shortHtml}${statusHtml}
                    </td>
                </tr>
            `;
        });

        const tableHtml = `
            <table class="coar-table">
                <thead>
                    <tr>
                        <th style="width: 15%;">Group Ledger (1st Layer)</th>
                        <th style="width: 17%;">Control Ledger (2nd Layer)</th>
                        <th style="width: 18%;">Sub-Control Ledger (3rd Layer)</th>
                        <th style="width: 20%;">Sub-Sidiary Ledger (4th Layer)</th>
                        <th style="width: 12%;">Account Code</th>
                        <th style="width: 18%;">Account Head (5th Layer)</th>
                    </tr>
                </thead>
                <tbody>
                    ${rows}
                </tbody>
            </table>
        `;

        $('#listViewContainer').html(tableHtml);
    }

    function loadList() {
        const showArchived = $('#coa-showArchived').is(':checked');
        const search = ($('#coa-listSearch').val() || '').trim();

        $('#listViewContainer').html(`
            <div class="coa-em py-5">
                <div class="spinner-border spinner-border-sm text-success me-2"></div>
                Loading chart of accounts list...
            </div>
        `);

        $.ajax({
            url: '/ChartOfAccounts/GetListData',
            type: 'GET',
            data: { search: search, showArchived: showArchived },
            success: function (res) {
                if (res && res.success && res.data) {
                    renderList(res.data);
                } else {
                    $('#listViewContainer').html(`
                        <div class="coa-em text-danger py-4">
                            <i class="fas fa-exclamation-circle me-1"></i> Failed to load chart of accounts list.
                        </div>
                    `);
                }
            },
            error: function () {
                $('#listViewContainer').html(`
                    <div class="coa-em text-danger py-4">
                        <i class="fas fa-exclamation-triangle me-1"></i> Network error loading list data.
                    </div>
                `);
            }
        });
    }

    // Archived Checkbox Toggle
    $('#coa-showArchived').on('change', function () {
        loadList();
    });

    // Debounced Search Filter
    let searchTimeout;
    $('#coa-listSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(loadList, 300);
    });

    // Initial Load
    loadList();
});

