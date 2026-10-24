$(document).ready(function () {
    const CUT = [1, 3, 6, 11, 16];
    const CONTROLLERS = ['GroupLedger', 'ControlLedger', 'SubControlLedger', 'SubSubsidiaryLedger', 'GeneralLedger'];
    const CHILD_NAMES = ['Control Ledger', 'Sub-Control Ledger', 'Subsidiary Ledger', 'General Ledger'];

    function esc(s) {
        return $('<div>').text(s || '').html();
    }

    function seg(c) {
        if (!c) return '';
        let o = '', a = 0;
        for (let i = 0; i < 5 && a < c.length; i++) {
            o += `<i class="s${i}">${c.slice(a, CUT[i])}</i>`;
            a = CUT[i];
        }
        return `<code class="coa-seg">${o}</code>`;
    }

    function renderNode(x) {
        const k = x.level;
        const codeHtml = seg(x.code);
        const nameHtml = esc(x.name);
        const shortHtml = x.shortName ? ` <span style="color:var(--coa-mut);font-size:0.85em;">(${esc(x.shortName)})</span>` : '';
        const statusHtml = x.isActive ? '' : ' <span class="coa-bd off" style="margin-left:6px;">Archived</span>';
        const countHtml = k < 4 ? `<span class="ct">${x.glCount} GL</span>` : '';
        const editUrl = `/${CONTROLLERS[k]}?editCode=${encodeURIComponent(x.code)}`;
        const addUrl = k < 4 ? `/${CONTROLLERS[k + 1]}?parentCode=${encodeURIComponent(x.code)}` : '';

        const actions = `<span class="ac"><a href="${editUrl}">Edit</a>${k < 4 ? `<a href="${addUrl}">Add ${CHILD_NAMES[k]}</a>` : ''}</span>`;

        const t = `<span class="nm">${codeHtml} ${nameHtml}</span>${shortHtml}${statusHtml}${countHtml}${actions}`;

        if (k < 4) {
            const hasKids = x.children && x.children.length > 0;
            const kidsHtml = hasKids
                ? x.children.map(renderNode).join('')
                : '<div style="color:var(--coa-mut);font-size:0.85em;padding:4px 0 4px 16px;">No child ledgers yet</div>';
            return `<details ${k < 2 ? 'open' : ''}><summary>${t}</summary><div class="kids">${kidsHtml}</div></details>`;
        } else {
            return `<div class="leaf">${t}</div>`;
        }
    }

    function loadTree() {
        const showArchived = $('#coa-showArchived').is(':checked');
        const search = ($('#coa-treeSearch').val() || '').trim();

        $('#tree').html('<div class="coa-em">Loading chart of accounts...</div>');

        $.ajax({
            url: '/ChartOfAccounts/GetTree',
            type: 'GET',
            data: { showArchived: showArchived, search: search },
            success: function (res) {
                if (res.success && res.data && res.data.length > 0) {
                    const html = res.data.map(renderNode).join('');
                    $('#tree').html(html);
                } else {
                    $('#tree').html(`
                        // <div class="coa-em">
                        //     <p>No ledgers found.</p>
                        //     <small>Click "Seed Default" above to load the standard chart of accounts, or start by adding a Group Ledger.</small>
                        // </div>
                    `);
                }
            },
            error: function () {
                $('#tree').html('<div class="coa-em" style="color:var(--coa-bad);">Failed to load chart of accounts.</div>');
            }
        });
    }

    // Expand / Collapse
    $('#coa-expandAllBtn').on('click', function () {
        $('#tree details').prop('open', true);
    });

    $('#coa-collapseAllBtn').on('click', function () {
        $('#tree details').prop('open', false);
    });

    // Checkbox toggle
    $('#coa-showArchived').on('change', function () {
        loadTree();
    });

    // Search filter debounce
    let searchTimeout;
    $('#coa-treeSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(loadTree, 300);
    });

    // Initial load
    loadTree();
});
