$(document).ready(function () {
    let currentPage = 1;
    let pageSize = 10;
    const CUT = [1, 3, 6, 11, 16];

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

    // Fetch Next Code
    function fetchNextCode() {
        if ($('#GroupLedger-isEdit').val() === '1') return;
        $.get('/GroupLedger/GetNextCode', function (res) {
            if (res && res.success) {
                $('#GroupLedgerCodeNo').val(res.code);
                $('#GroupLedgerCodePreview').html(seg(res.code));
            }
        });
    }

    // Reset Form
    function resetForm() {
        $('#GroupLedger-form')[0].reset();
        $('#GroupLedger-isEdit').val('0');
        $('#IsActive').prop('checked', true);
        $('#GroupLedger-formTitle').text('Group Ledger: new entry');
        $('#GroupLedger-saveBtn').text('Save');
        $('#GroupLedger-form').find('.text-danger').text('');
        fetchNextCode();
        setTimeout(function () {
            $('#GroupLedgerName').focus();
        }, 150);
    }

    $('#GroupLedger-resetBtn').on('click', resetForm);

    // Form Submit
    $('#GroupLedger-form').submit(function (e) {
        e.preventDefault();

        const form = $(this);
        const isEdit = $('#GroupLedger-isEdit').val() === '1';
        const url = isEdit ? '/GroupLedger/Update' : '/GroupLedger/Create';
        const $saveButton = $('#GroupLedger-saveBtn');

        // Validation: Required Name
        const nameVal = $('#GroupLedgerName').val() ? $('#GroupLedgerName').val().trim() : '';
        if (!nameVal) {
            form.find('[data-valmsg-for="GroupLedgerName"]').text('Please enter Group Ledger Name');
            if (typeof toastr !== 'undefined') toastr.warning('Please enter Group Ledger Name');
            $('#GroupLedgerName').focus();
            return false;
        }

        $.ajax({
            url: url,
            type: 'POST',
            data: form.serialize(),
            beforeSend: function () {
                $saveButton.prop('disabled', true);
            },
            success: function (res) {
                if (res.success) {
                    if (typeof toastr !== 'undefined') toastr.success(res.message);
                    resetForm();
                    loadTableData();
                    if (typeof window.loadCoaGlobalStats === 'function') window.loadCoaGlobalStats();
                } else {
                    if (typeof toastr !== 'undefined') toastr.error(res.message);
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
            },
            complete: function () {
                $saveButton.prop('disabled', false);
            }
        });
    });

    // Edit
    $(document).on('click', '.GroupLedgerEditBtn', async function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        try {
            const res = await $.get(`/GroupLedger/GetById?code=${encodeURIComponent(code)}`);
            if (res.success && res.data) {
                const item = res.data;
                const codeVal = item.groupLedgerCodeNo || item.GroupLedgerCodeNo;
                const nameVal = item.groupLedgerName || item.GroupLedgerName;
                const shortNameVal = item.shortName || item.ShortName || '';
                const activeVal = item.isActive ?? item.IsActive ?? true;

                $('#GroupLedger-isEdit').val('1');
                $('#GroupLedgerCodeNo').val(codeVal);
                $('#GroupLedgerCodePreview').html(seg(codeVal));
                $('#GroupLedgerName').val(nameVal);
                $('#ShortName').val(shortNameVal);
                $('#IsActive').prop('checked', activeVal);

                $('#GroupLedger-formTitle').text(`Group Ledger: editing ${codeVal}`);
                $('#GroupLedger-saveBtn').text('Update');
                $('html, body').animate({ scrollTop: $('#GroupLedger-form').offset().top - 80 }, 'fast');
            } else {
                if (typeof toastr !== 'undefined') toastr.warning(res.message || "Data not found.");
            }
        } catch (error) {
            if (typeof toastr !== 'undefined') toastr.error("Failed to fetch group ledger details.");
        }
    });

    // Archive / Restore
    $(document).on('click', '.GroupLedgerArchiveBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        const isArchived = $(this).data('archived') === true || $(this).data('archived') === 'true';

        $.ajax({
            url: '/GroupLedger/ToggleArchive',
            type: 'POST',
            data: { code: code },
            success: function (res) {
                if (res.success) {
                    if (typeof toastr !== 'undefined') toastr.success(res.message);
                    loadTableData();
                    if (typeof window.loadCoaGlobalStats === 'function') window.loadCoaGlobalStats();
                } else {
                    if (typeof toastr !== 'undefined') toastr.error(res.message);
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
            }
        });
    });

    // Delete single
    $(document).on('click', '.GroupLedgerDeleteBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        if (!confirm(`Delete ledger ${code}?`)) return;

        $.ajax({
            url: '/GroupLedger/SoftDelete',
            type: 'POST',
            data: { Codes: [code] },
            success: function (res) {
                if (res.success) {
                    if (typeof toastr !== 'undefined') toastr.success(res.message);
                    loadTableData();
                    fetchNextCode();
                    if (typeof window.loadCoaGlobalStats === 'function') window.loadCoaGlobalStats();
                } else {
                    if (typeof toastr !== 'undefined') toastr.error(res.message);
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
            }
        });
    });

    // Table controls
    $('#GroupLedger-pageSizeSelect').on('change', function () {
        pageSize = parseInt($(this).val(), 10);
        currentPage = 1;
        loadTableData();
    });

    let searchTimeout;
    $("#GroupLedger-searchInput").on("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            currentPage = 1;
            loadTableData();
        }, 300);
    });

    $('#GroupLedger-showArchived').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    function renderPagination(p) {
        const container = $("#GroupLedger-paginationContainer");
        container.empty();

        if (!p || p.totalPages <= 0) return;

        const current = p.currentPage;
        const total = p.totalPages;

        // First & Previous
        container.append(`<button type="button" class="coa-btn coa-page-btn ${current <= 1 ? 'disabled' : ''}" data-page="1" ${current <= 1 ? 'disabled' : ''}>First</button>`);
        container.append(`<button type="button" class="coa-btn coa-page-btn ${current <= 1 ? 'disabled' : ''}" data-page="${current - 1}" ${current <= 1 ? 'disabled' : ''}>Previous</button>`);

        // Page numbers with ellipsis
        const pages = [];
        const delta = 2;

        for (let i = 1; i <= total; i++) {
            if (i === 1 || i === total || (i >= current - delta && i <= current + delta)) {
                pages.push(i);
            } else if (pages[pages.length - 1] !== '...') {
                pages.push('...');
            }
        }

        pages.forEach(item => {
            if (item === '...') {
                container.append(`<span class="coa-page-dots">...</span>`);
            } else {
                const isActive = item === current;
                container.append(`<button type="button" class="coa-btn coa-page-btn ${isActive ? 'pri' : ''}" data-page="${item}" ${isActive ? 'disabled' : ''}>${item}</button>`);
            }
        });

        // Next & Last
        container.append(`<button type="button" class="coa-btn coa-page-btn ${current >= total ? 'disabled' : ''}" data-page="${current + 1}" ${current >= total ? 'disabled' : ''}>Next</button>`);
        container.append(`<button type="button" class="coa-btn coa-page-btn ${current >= total ? 'disabled' : ''}" data-page="${total}" ${current >= total ? 'disabled' : ''}>Last</button>`);
    }

    $(document).on('click', '#GroupLedger-paginationContainer .coa-page-btn:not(.disabled)', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'), 10);
        if (page && page !== currentPage) {
            currentPage = page;
            loadTableData();
        }
    });

    function loadTableData() {
        const searchTerm = $("#GroupLedger-searchInput").val();
        const showArchived = $('#GroupLedger-showArchived').is(':checked');

        $.ajax({
            url: '/GroupLedger/GetAll',
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchTerm,
                showArchived: showArchived
            },
            success: function (response) {
                const tableBody = $("#GroupLedger-body");
                tableBody.empty();

                if (response.data && response.data.length > 0) {
                    response.data.forEach(function (item) {
                        const codeHtml = seg(item.groupLedgerCodeNo);
                        const isAr = !item.isActive;
                        const rowClass = isAr ? 'ar' : '';
                        const statusBadge = `<span class="coa-bd ${isAr ? 'off' : 'on'}">${isAr ? 'Archived' : 'Active'}</span>`;
                        const archiveActionText = isAr ? 'Restore' : 'Archive';

                        tableBody.append(`
                            <tr class="${rowClass}">
                                <td class="text-center">${codeHtml}</td>
                                <td>${esc(item.groupLedgerName)}</td>
                                <td class="text-center">${esc(item.shortName || '-')}</td>
                                <td class="text-center">${statusBadge}</td>
                                <td class="coa-ac text-center">
                                    <a href="#" class="GroupLedgerEditBtn" data-code="${esc(item.groupLedgerCodeNo)}">Edit</a>
                                    <a href="#" class="GroupLedgerArchiveBtn" data-code="${esc(item.groupLedgerCodeNo)}" data-archived="${isAr}">${archiveActionText}</a>
                                    <a href="#" class="GroupLedgerDeleteBtn dl" data-code="${esc(item.groupLedgerCodeNo)}">Delete</a>
                                </td>
                            </tr>
                        `);
                    });
                } else {
                    tableBody.append('<tr><td colspan="5" class="coa-em">No ledgers found. Change the search or add one above.</td></tr>');
                }

                if (response.paginationInfo) {
                    const p = response.paginationInfo;
                    $("#GroupLedger-paginationInfo").text(`Showing ${p.startItem} to ${p.endItem} of ${p.totalItems} entries`);
                    renderPagination(p);
                }
            }
        });
    }

    // Check if query string has editCode
    const urlParams = new URLSearchParams(window.location.search);
    const editCodeParam = urlParams.get('editCode');
    if (editCodeParam) {
        setTimeout(() => {
            $(`.GroupLedgerEditBtn[data-code="${editCodeParam}"]`).trigger('click');
        }, 300);
    }

    // Init
    fetchNextCode();
    loadTableData();
    setTimeout(() => {
        $('#GroupLedgerName').focus();
    }, 250);
});
