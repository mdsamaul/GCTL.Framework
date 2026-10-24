$(document).ready(function () {
    let currentView = 'tree';
    let cachedData = null;
    let searchTimeout = null;

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

    function getChipClass(type) {
        if (!type) return 'chip-Asset';
        if (type.toLowerCase().includes('asset')) return 'chip-Asset';
        if (type.toLowerCase().includes('liabilit') || type.toLowerCase().includes('equity')) return 'chip-Liability';
        if (type.toLowerCase().includes('income') || type.toLowerCase().includes('revenue')) return 'chip-Income';
        if (type.toLowerCase().includes('expense')) return 'chip-Expense';
        return 'chip-Asset';
    }

    // Recursive Tree Node Renderer matching /ChartOfAccounts/Index
    function renderTreeNode(node) {
        const k = node.level;
        const codeHtml = seg(node.code);
        const nameHtml = esc(node.name);
        const shortHtml = node.shortName ? ` <span class="text-muted fs-9 ms-1">(${esc(node.shortName)})</span>` : '';
        const statusHtml = node.isActive ? '' : ' <span class="coar-badge-archived ms-1">Archived</span>';
        const countHtml = k < 4 ? `<span class="ct">${node.glCount || 0} GL</span>` : '';
        const typeChip = k === 4 ? `<span class="coar-chip ${getChipClass(node.accountType)} ms-auto">${esc(node.accountType)}</span>` : '';

        const t = `<span class="nm">${codeHtml} ${nameHtml}</span>${shortHtml}${statusHtml}${countHtml}${typeChip}`;

        if (k < 4) {
            const hasKids = node.children && node.children.length > 0;
            const kidsHtml = hasKids
                ? node.children.map(renderTreeNode).join('')
                : '<div class="text-muted fs-9 py-1 ps-3">No child ledgers yet</div>';
            return `<details ${k < 2 ? 'open' : ''} data-code="${esc(node.code)}"><summary>${t}</summary><div class="kids">${kidsHtml}</div></details>`;
        } else {
            return `<div class="leaf" data-code="${esc(node.code)}">${t}</div>`;
        }
    }

    function renderTree(treeList) {
        if (!treeList || treeList.length === 0) {
            $('#treeViewContainer').html('<div class="empty-state"><i class="fas fa-folder-open fa-2x mb-2 text-muted"></i><p>No chart of accounts records found.</p></div>');
            return;
        }

        const html = treeList.map(renderTreeNode).join('');
        $('#treeViewContainer').html(html);
    }

    function renderList(flatList) {
        const validList = (flatList || []).filter(item => item && item.generalLedgerCode && item.generalLedgerCode.trim() !== '');
        if (!validList || validList.length === 0) {
            $('#listViewContainer').html('<div class="empty-state"><i class="fas fa-list-alt fa-2x mb-2 text-muted"></i><p>No chart of accounts records found.</p></div>');
            return;
        }

        let rows = '';
        validList.forEach(function (item) {
            rows += `
                <tr>
                    <td>${seg(item.groupCode)} <span class="fw-semibold">${esc(item.groupName)}</span></td>
                    <td>${seg(item.controlCode)} <span>${esc(item.controlName)}</span></td>
                    <td>${seg(item.subControlCode)} <span>${esc(item.subControlName)}</span></td>
                    <td>${seg(item.subSubsidiaryCode)} <span>${esc(item.subSubsidiaryName)}</span></td>
                    <td><strong>${seg(item.generalLedgerCode)}</strong></td>
                    <td>
                        <span class="fw-bold text-dark">${esc(item.generalLedgerName)}</span>
                    </td>
                </tr>
            `;
        });

        const tableHtml = `
            <div class="coar-table-responsive">
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
            </div>
        `;

        $('#listViewContainer').html(tableHtml);
    }

    function updateStats(stats) {
        if (!stats) return;
        $('#statTotal').text(stats.totalGeneralLedgers ?? 0);
        $('#statAssets').text(stats.assetsGLCount ?? 0);
        $('#statEquity').text(stats.equityGLCount ?? 0);
        $('#statIncome').text(stats.incomeGLCount ?? 0);
        $('#statExpenses').text(stats.expenseGLCount ?? 0);
    }

    function loadReportData() {
        const searchTerm = ($('#coaSearch').val() || '').trim();
        const showArchived = $('#coaShowArchived').is(':checked');

        $('#treeViewContainer').html('<div class="empty-state"><div class="spinner-border spinner-border-sm text-success me-2"></div> Loading chart of accounts...</div>');
        $('#listViewContainer').html('<div class="empty-state"><div class="spinner-border spinner-border-sm text-success me-2"></div> Loading chart of accounts...</div>');

        $.ajax({
            url: '/ChartOfAccountsReports/GetData',
            type: 'GET',
            data: { searchTerm: searchTerm, showArchived: showArchived },
            success: function (res) {
                if (res && res.success && res.data) {
                    cachedData = res.data;
                    updateStats(cachedData.stats);

                    if (cachedData.companyName) {
                        $('#companySubtitle').text(cachedData.companyName + ' · Amounts in Bangladeshi Taka (৳)');
                        $('#pheadCo').text(cachedData.companyName);
                    }

                    renderTree(cachedData.tree);
                    renderList(cachedData.list);
                } else {
                    $('#treeViewContainer').html('<div class="empty-state text-danger">Failed to load chart of accounts report.</div>');
                    $('#listViewContainer').html('<div class="empty-state text-danger">Failed to load chart of accounts report.</div>');
                }
            },
            error: function () {
                $('#treeViewContainer').html('<div class="empty-state text-danger">Network error loading report data.</div>');
                $('#listViewContainer').html('<div class="empty-state text-danger">Network error loading report data.</div>');
            }
        });
    }

    // Toggle between Tree View and List View
    $('#btnTreeView').on('click', function () {
        currentView = 'tree';
        $('#btnTreeView').addClass('active');
        $('#btnListView').removeClass('active');
        $('#treeViewContainer').removeClass('d-none');
        $('#listViewContainer').addClass('d-none');
        $('#btnExpandAll, #btnCollapseAll').removeClass('d-none');
    });

    $('#btnListView').on('click', function () {
        currentView = 'list';
        $('#btnListView').addClass('active');
        $('#btnTreeView').removeClass('active');
        $('#listViewContainer').removeClass('d-none');
        $('#treeViewContainer').addClass('d-none');
        $('#btnExpandAll, #btnCollapseAll').addClass('d-none');
    });

    // Expand / Collapse All
    $('#btnExpandAll').on('click', function () {
        $('#treeViewContainer details').prop('open', true);
    });

    $('#btnCollapseAll').on('click', function () {
        $('#treeViewContainer details').prop('open', false);
    });

    // Debounced Search Filter
    $('#coaSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(loadReportData, 350);
    });

    // Show Archived Toggle
    $('#coaShowArchived').on('change', function () {
        loadReportData();
    });

    // Extract currently visible / expanded tree nodes based on UI collapse state
    function getVisibleTreeNodes() {
        if (!cachedData || !cachedData.tree) return [];

        const visible = [];
        function traverse(nodes, isParentOpen) {
            if (!nodes || !isParentOpen) return;
            for (let i = 0; i < nodes.length; i++) {
                const node = nodes[i];
                const detailsEl = $(`#treeViewContainer details[data-code="${node.code}"]`);
                const isOpen = detailsEl.length > 0 ? detailsEl.prop('open') : false;

                visible.push({
                    code: node.code || '',
                    name: node.name || '',
                    shortName: node.shortName || '',
                    level: node.level,
                    accountType: node.accountType || 'Asset',
                    glCount: node.glCount || 0,
                    isActive: node.isActive !== false,
                    isOpen: isOpen,
                    hasChildren: !!(node.children && node.children.length > 0)
                });

                if (node.level < 4 && isOpen && node.children && node.children.length > 0) {
                    traverse(node.children, true);
                }
            }
        }

        traverse(cachedData.tree, true);
        return visible;
    }

    function submitExportForm(url, params, newTab) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = url;
        if (newTab) {
            form.target = '_blank';
        }

        for (const key in params) {
            if (Object.prototype.hasOwnProperty.call(params, key)) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = key;
                input.value = params[key];
                form.appendChild(input);
            }
        }

        document.body.appendChild(form);
        form.submit();
        setTimeout(function () {
            if (form.parentNode) {
                form.parentNode.removeChild(form);
            }
        }, 1500);
    }

    // Export Handlers
    window.exportCoaReport = function (action) {
        const searchTerm = ($('#coaSearch').val() || '').trim();
        const showArchived = $('#coaShowArchived').is(':checked');
        const viewMode = currentView || 'tree';

        const params = {
            viewMode: viewMode,
            searchTerm: searchTerm,
            showArchived: showArchived
        };

        if (viewMode === 'tree') {
            const visibleNodes = getVisibleTreeNodes();
            params.visibleTreeJson = JSON.stringify(visibleNodes);
        }

        if (action === 'preview' || action === 'print') {
            submitExportForm('/ChartOfAccountsReports/ExportPdf', params, true);
        } else if (action === 'pdf') {
            params.download = 'true';
            submitExportForm('/ChartOfAccountsReports/ExportPdf', params, false);
        } else if (action === 'excel') {
            submitExportForm('/ChartOfAccountsReports/ExportExcel', params, false);
        } else if (action === 'csv') {
            submitExportForm('/ChartOfAccountsReports/ExportCsv', params, false);
        }
    };

    // Export Dropdown Action (Direct trigger on selection)
    $('#coaExportFormat').on('change', function () {
        const act = ($(this).val() || 'pdf').trim();
        $(this).val(''); // Reset selection for next use
        if (act) {
            exportCoaReport(act);
        }
    });

    // Initial Load
    loadReportData();
});
