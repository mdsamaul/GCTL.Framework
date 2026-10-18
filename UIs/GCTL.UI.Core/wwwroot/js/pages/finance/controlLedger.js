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

    function setChoiceDisabled(id, disabled) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        $el.prop('disabled', disabled);
        const inst = $el.data('customDdInstance');
        if (inst && typeof inst.setDisabled === 'function') {
            inst.setDisabled(disabled);
        }
        if (window.choiceManager && typeof window.choiceManager.disableChoice === 'function') {
            if (disabled) window.choiceManager.disableChoice(rawId);
            else window.choiceManager.enableChoice(rawId);
        }
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.prop('disabled', disabled).trigger('change.select2');
        }
    }

    function setChoiceVal(id, val, text) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        if (typeof window.setCustomDropdownValue === 'function') {
            window.setCustomDropdownValue(rawId, val, text);
        }
        $el.val(val || '');
        if (window.choiceManager && typeof window.choiceManager.setChoiceValue === 'function') {
            window.choiceManager.setChoiceValue(rawId, val || '');
        }
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(val || null).trigger('change');
        }
    }

    function clearChoice(id) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        if (typeof window.clearCustomDropdownById === 'function') {
            window.clearCustomDropdownById(rawId);
        }
        $el.val('');
        if (window.choiceManager && typeof window.choiceManager.clearChoice === 'function') {
            window.choiceManager.clearChoice(rawId);
        }
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(null).trigger('change');
        }
    }

    function openDropdown(id) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        const inst = $el.data('customDdInstance');
        if (inst && typeof inst._openPanelFull === 'function') {
            inst._openPanelFull();
            return;
        }
        if (window.choiceManager && typeof window.choiceManager.openChoice === 'function') {
            window.choiceManager.openChoice(rawId);
            return;
        }
        if ($el.hasClass('select2-hidden-accessible') || (typeof $el.select2 === 'function' && $el.data('select2'))) {
            try {
                $el.select2('open');
                return;
            } catch (e) {}
        }
        $el.focus();
    }

    // Top Dropdown Selection -> Generates code AND filters the grid!
    $('#GroupLedgerCodeNo').on('change', function () {
        const groupCode = $(this).val();

        // 1. Fetch next code if not editing
        if ($('#ControlLedger-isEdit').val() !== '1') {
            if (!groupCode) {
                $('#ControlLedgerCodeNo').val('');
                $('#ControlLedgerCodePreview').html('<span class="text-muted">Select Group first</span>');
            } else {
                $.get(`/ControlLedger/GetNextCode?groupCode=${encodeURIComponent(groupCode)}`, function (res) {
                    if (res && res.success) {
                        $('#ControlLedgerCodeNo').val(res.code);
                        $('#ControlLedgerCodePreview').html(seg(res.code));
                    }
                });
            }
        }

        // 2. Automatically filter the grid below
        currentPage = 1;
        loadTableData();
    });

    // Reset Form
    function resetForm() {
        $('#ControlLedger-form')[0].reset();
        $('#ControlLedger-isEdit').val('0');
        $('#IsActive').prop('checked', true);
        setChoiceDisabled('GroupLedgerCodeNo', false);
        if (window.choiceManager && typeof window.choiceManager.resetChoice === 'function') {
            window.choiceManager.resetChoice('GroupLedgerCodeNo');
        } else {
            clearChoice('GroupLedgerCodeNo');
        }
        $('#ControlLedgerCodeNo').val('');
        $('#ControlLedgerCodePreview').html('<span class="text-muted">Select Group first</span>');
        $('#ControlLedger-formTitle').text('Control Ledger: new entry');
        $('#ControlLedger-saveBtn').text('Save');
        $('#ControlLedger-form').find('.text-danger').text('');
        currentPage = 1;
        loadTableData();
        setTimeout(function () {
            $('#ControlLedgerName').focus();
        }, 150);
    }

    $('#ControlLedger-resetBtn').on('click', resetForm);

    // Form Submit
    $('#ControlLedger-form').submit(function (e) {
        e.preventDefault();

        const form = $(this);
        const isEdit = $('#ControlLedger-isEdit').val() === '1';
        const url = isEdit ? '/ControlLedger/Update' : '/ControlLedger/Create';
        const $saveButton = $('#ControlLedger-saveBtn');

        // Validation: Required Dropdown
        const groupCode = $('#GroupLedgerCodeNo').val();
        if (!groupCode) {
            form.find('[data-valmsg-for="GroupLedgerCodeNo"]').text('Please select Group Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Group Ledger');
            openDropdown('GroupLedgerCodeNo');
            return false;
        }

        // Validation: Required Name
        const nameVal = $('#ControlLedgerName').val() ? $('#ControlLedgerName').val().trim() : '';
        if (!nameVal) {
            form.find('[data-valmsg-for="ControlLedgerName"]').text('Please enter Control Ledger Name');
            if (typeof toastr !== 'undefined') toastr.warning('Please enter Control Ledger Name');
            $('#ControlLedgerName').focus();
            return false;
        }

        $('#GroupLedgerCodeNo').prop('disabled', false);

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
                    if (typeof window.loadCoaGlobalStats === 'function') window.loadCoaGlobalStats();
                } else {
                    if (typeof toastr !== 'undefined') toastr.error(res.message);
                    if (isEdit) setChoiceDisabled('GroupLedgerCodeNo', true);
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
                if (isEdit) setChoiceDisabled('GroupLedgerCodeNo', true);
            },
            complete: function () {
                $saveButton.prop('disabled', false);
            }
        });
    });

    // Edit
    $(document).on('click', '.ControlLedgerEditBtn', async function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        try {
            const res = await $.get(`/ControlLedger/GetById?code=${encodeURIComponent(code)}`);
            if (res.success && res.data) {
                const item = res.data;
                const grpCode = item.groupLedgerCodeNo || item.GroupLedgerCodeNo;
                const ctrlCode = item.controlLedgerCodeNo || item.ControlLedgerCodeNo;
                const ctrlName = item.controlLedgerName || item.ControlLedgerName;
                const shortName = item.shortName || item.ShortName || '';
                const isActive = item.isActive ?? item.IsActive ?? true;

                $('#ControlLedger-isEdit').val('1');
                setChoiceVal('GroupLedgerCodeNo', grpCode);
                setChoiceDisabled('GroupLedgerCodeNo', true);
                $('#ControlLedgerCodeNo').val(ctrlCode);
                $('#ControlLedgerCodePreview').html(seg(ctrlCode));
                $('#ControlLedgerName').val(ctrlName);
                $('#ShortName').val(shortName);
                $('#IsActive').prop('checked', isActive);

                $('#ControlLedger-formTitle').text(`Control Ledger: editing ${ctrlCode}`);
                $('#ControlLedger-saveBtn').text('Update');
                $('html, body').animate({ scrollTop: $('#ControlLedger-form').offset().top - 80 }, 'fast');
            } else {
                if (typeof toastr !== 'undefined') toastr.warning(res.message || "Data not found.");
            }
        } catch (error) {
            if (typeof toastr !== 'undefined') toastr.error("Failed to fetch control ledger details.");
        }
    });

    // Archive / Restore
    $(document).on('click', '.ControlLedgerArchiveBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        const isArchived = $(this).data('archived') === true || $(this).data('archived') === 'true';

        $.ajax({
            url: '/ControlLedger/ToggleArchive',
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
    $(document).on('click', '.ControlLedgerDeleteBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        if (!confirm(`Delete ledger ${code}?`)) return;

        $.ajax({
            url: '/ControlLedger/SoftDelete',
            type: 'POST',
            data: { Codes: [code] },
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

    // Table controls
    $('#ControlLedger-pageSizeSelect').on('change', function () {
        pageSize = parseInt($(this).val(), 10);
        currentPage = 1;
        loadTableData();
    });

    let searchTimeout;
    $("#ControlLedger-searchInput").on("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            currentPage = 1;
            loadTableData();
        }, 300);
    });

    $('#ControlLedger-showArchived').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    function renderPagination(p) {
        const container = $("#ControlLedger-paginationContainer");
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

    $(document).on('click', '#ControlLedger-paginationContainer .coa-page-btn:not(.disabled)', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'), 10);
        if (page && page !== currentPage) {
            currentPage = page;
            loadTableData();
        }
    });

    function loadTableData() {
        const searchTerm = $("#ControlLedger-searchInput").val();
        const groupCode = $('#GroupLedgerCodeNo').val(); // Filtered directly from top form dropdown!
        const showArchived = $('#ControlLedger-showArchived').is(':checked');

        $.ajax({
            url: '/ControlLedger/GetAll',
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchTerm,
                groupCode: groupCode,
                showArchived: showArchived
            },
            success: function (response) {
                const tableBody = $("#ControlLedger-body");
                tableBody.empty();

                if (response.data && response.data.length > 0) {
                    response.data.forEach(function (item) {
                        const codeHtml = seg(item.controlLedgerCodeNo);
                        const isAr = !item.isActive;
                        const rowClass = isAr ? 'ar' : '';
                        const statusBadge = `<span class="coa-bd ${isAr ? 'off' : 'on'}">${isAr ? 'Archived' : 'Active'}</span>`;
                        const archiveActionText = isAr ? 'Restore' : 'Archive';

                        tableBody.append(`
                            <tr class="${rowClass}">
                                <td class="text-center">${codeHtml}</td>
                                <td>${esc(item.controlLedgerName)}</td>
                                <td class="text-center">${esc(item.shortName || '-')}</td>
                                <td><span class="coa-pa">${esc(item.hierarchyPath)}</span></td>
                                <td class="text-center">${statusBadge}</td>
                                <td class="coa-ac text-center">
                                    <a href="#" class="ControlLedgerEditBtn" data-code="${esc(item.controlLedgerCodeNo)}">Edit</a>
                                    <a href="#" class="ControlLedgerArchiveBtn" data-code="${esc(item.controlLedgerCodeNo)}" data-archived="${isAr}">${archiveActionText}</a>
                                    <a href="#" class="ControlLedgerDeleteBtn dl" data-code="${esc(item.controlLedgerCodeNo)}">Delete</a>
                                </td>
                            </tr>
                        `);
                    });
                } else {
                    tableBody.append('<tr><td colspan="6" class="coa-em">No ledgers found. Change the search or add one above.</td></tr>');
                }

                if (response.paginationInfo) {
                    const p = response.paginationInfo;
                    $("#ControlLedger-paginationInfo").text(`Showing ${p.startItem} to ${p.endItem} of ${p.totalItems} entries`);
                    renderPagination(p);
                }
            }
        });
    }

    // Check query params
    const urlParams = new URLSearchParams(window.location.search);
    const parentCodeParam = urlParams.get('parentCode') || urlParams.get('groupCode');
    const editCodeParam = urlParams.get('editCode');

    if (parentCodeParam) {
        $('#GroupLedgerCodeNo').val(parentCodeParam).trigger('change');
    }

    if (editCodeParam) {
        setTimeout(() => {
            $(`.ControlLedgerEditBtn[data-code="${editCodeParam}"]`).trigger('click');
        }, 300);
    }

    // Init
    loadTableData();
    setTimeout(() => {
        $('#ControlLedgerName').focus();
    }, 250);
});
