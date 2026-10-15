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
        if (window.choiceManager && typeof window.choiceManager.disableChoice === 'function') {
            if (disabled) window.choiceManager.disableChoice(id);
            else window.choiceManager.enableChoice(id);
        }
        const $el = $('#' + id);
        $el.prop('disabled', disabled);
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.prop('disabled', disabled).trigger('change.select2');
        }
    }

    function setChoiceVal(id, val) {
        if (window.choiceManager && typeof window.choiceManager.setChoiceValue === 'function') {
            window.choiceManager.setChoiceValue(id, val || '');
        } else {
            $('#' + id).val(val || '').trigger('change');
        }
        const $el = $('#' + id);
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(val || null).trigger('change');
        }
    }

    function clearChoice(id) {
        if (window.choiceManager && typeof window.choiceManager.clearChoice === 'function') {
            window.choiceManager.clearChoice(id);
        } else {
            $('#' + id).val('').trigger('change');
        }
        const $el = $('#' + id);
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(null).trigger('change');
        }
    }

    function openDropdown(id) {
        if (window.choiceManager && typeof window.choiceManager.openChoice === 'function') {
            window.choiceManager.openChoice(id);
            return;
        }
        const $el = $('#' + id);
        if ($el.hasClass('select2-hidden-accessible') || (typeof $el.select2 === 'function' && $el.data('select2'))) {
            try {
                $el.select2('open');
                return;
            } catch (e) {}
        }
        const container = document.getElementById(id)?.closest('.choices');
        if (container) {
            const inner = container.querySelector('.choices__inner') || container;
            inner.click();
        } else {
            $el.focus();
        }
    }

    // Load Controls for Group
    function loadControls(groupCode, targetSelectId, selectedVal) {
        const rawId = targetSelectId.replace('#', '');

        if (!groupCode) {
            if (window.choiceManager) {
                window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Control Ledger --' });
            } else {
                $(targetSelectId).empty().append('<option value="">-- Select Control Ledger --</option>');
            }
            setChoiceDisabled(rawId, true);
            return Promise.resolve();
        }

        return $.get(`/ControlLedger/GetDropdown?groupCode=${encodeURIComponent(groupCode)}`, function (items) {
            if (items && items.length > 0) {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, items, {
                        labelKey: 'displayText',
                        valueKey: 'code',
                        placeholder: '-- Select Control Ledger --'
                    });
                } else {
                    const select = $(targetSelectId);
                    select.empty().append('<option value="">-- Select Control Ledger --</option>');
                    items.forEach(function (c) {
                        select.append(`<option value="${esc(c.code)}">${esc(c.displayText)}</option>`);
                    });
                }
                setChoiceDisabled(rawId, false);
                if (selectedVal) {
                    setChoiceVal(rawId, selectedVal);
                }
            } else {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Control Ledger --' });
                } else {
                    $(targetSelectId).empty().append('<option value="">-- Select Control Ledger --</option>');
                }
                setChoiceDisabled(rawId, true);
            }
        });
    }

    // Top Dropdown Selection -> Generates code AND filters the grid!
    $('#SubControlLedger-GroupSelect').on('change', function () {
        const groupCode = $(this).val();
        $('#SubControlLedgerCodeNo').val('');
        $('#SubControlLedgerCodePreview').html('<span class="text-muted">Select Control first</span>');
        loadControls(groupCode, '#SubControlLedger-ControlSelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#SubControlLedger-ControlSelect').on('change', function () {
        const controlCode = $(this).val();

        if ($('#SubControlLedger-isEdit').val() !== '1') {
            if (!controlCode) {
                $('#SubControlLedgerCodeNo').val('');
                $('#SubControlLedgerCodePreview').html('<span class="text-muted">Select Control first</span>');
            } else {
                $.get(`/SubControlLedger/GetNextCode?controlCode=${encodeURIComponent(controlCode)}`, function (res) {
                    if (res && res.success) {
                        $('#SubControlLedgerCodeNo').val(res.code);
                        $('#SubControlLedgerCodePreview').html(seg(res.code));
                    }
                });
            }
        }

        currentPage = 1;
        loadTableData();
    });

    // Reset Form
    function resetForm() {
        $('#SubControlLedger-form')[0].reset();
        $('#SubControlLedger-isEdit').val('0');
        $('#IsActive').prop('checked', true);
        setChoiceDisabled('SubControlLedger-GroupSelect', false);
        if (window.choiceManager && typeof window.choiceManager.resetChoice === 'function') {
            window.choiceManager.resetChoice('SubControlLedger-GroupSelect');
        } else {
            clearChoice('SubControlLedger-GroupSelect');
        }
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('SubControlLedger-ControlSelect', [], { placeholder: '-- Select Control Ledger --' });
        } else {
            $('#SubControlLedger-ControlSelect').empty().append('<option value="">-- Select Control Ledger --</option>');
        }
        setChoiceDisabled('SubControlLedger-ControlSelect', true);
        $('#SubControlLedgerCodeNo').val('');
        $('#SubControlLedgerCodePreview').html('<span class="text-muted">Select Control first</span>');
        $('#SubControlLedger-formTitle').text('Sub-Control Ledger: new entry');
        $('#SubControlLedger-saveBtn').text('Save');
        $('#SubControlLedger-form').find('.text-danger').text('');
        currentPage = 1;
        loadTableData();
        setTimeout(function () {
            $('#SubControlLedgerName').focus();
        }, 150);
    }

    $('#SubControlLedger-resetBtn').on('click', resetForm);

    // Form Submit
    $('#SubControlLedger-form').submit(function (e) {
        e.preventDefault();

        const form = $(this);
        const isEdit = $('#SubControlLedger-isEdit').val() === '1';
        const url = isEdit ? '/SubControlLedger/Update' : '/SubControlLedger/Create';
        const $saveButton = $('#SubControlLedger-saveBtn');

        // Validation: Required Group Dropdown
        const groupCode = $('#SubControlLedger-GroupSelect').val();
        if (!groupCode) {
            form.find('[data-valmsg-for="GroupLedgerCodeNo"]').text('Please select Group Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Group Ledger');
            openDropdown('SubControlLedger-GroupSelect');
            return false;
        }

        // Validation: Required Control Dropdown
        const ctrlCode = $('#SubControlLedger-ControlSelect').val();
        if (!ctrlCode) {
            form.find('[data-valmsg-for="ControlLedgerCodeNo"]').text('Please select Control Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Control Ledger');
            openDropdown('SubControlLedger-ControlSelect');
            return false;
        }

        // Validation: Required Name
        const nameVal = $('#SubControlLedgerName').val() ? $('#SubControlLedgerName').val().trim() : '';
        if (!nameVal) {
            form.find('[data-valmsg-for="SubControlLedgerName"]').text('Please enter Sub-Control Name');
            if (typeof toastr !== 'undefined') toastr.warning('Please enter Sub-Control Name');
            $('#SubControlLedgerName').focus();
            return false;
        }

        $('#SubControlLedger-GroupSelect, #SubControlLedger-ControlSelect').prop('disabled', false);

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
                    if (isEdit) {
                        setChoiceDisabled('SubControlLedger-GroupSelect', true);
                        setChoiceDisabled('SubControlLedger-ControlSelect', true);
                    }
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
                if (isEdit) {
                    setChoiceDisabled('SubControlLedger-GroupSelect', true);
                    setChoiceDisabled('SubControlLedger-ControlSelect', true);
                }
            },
            complete: function () {
                $saveButton.prop('disabled', false);
            }
        });
    });

    // Edit
    $(document).on('click', '.SubControlLedgerEditBtn', async function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        try {
            const res = await $.get(`/SubControlLedger/GetById?code=${encodeURIComponent(code)}`);
            if (res.success && res.data) {
                const item = res.data;
                const grpCode = item.groupLedgerCodeNo || item.GroupLedgerCodeNo;
                const ctrlCode = item.controlLedgerCodeNo || item.ControlLedgerCodeNo;
                const subCtrlCode = item.subControlLedgerCodeNo || item.SubControlLedgerCodeNo;
                const subCtrlName = item.subControlLedgerName || item.SubControlLedgerName;
                const shortName = item.shortName || item.ShortName || '';
                const isActive = item.isActive ?? item.IsActive ?? true;

                $('#SubControlLedger-isEdit').val('1');
                setChoiceVal('SubControlLedger-GroupSelect', grpCode);
                setChoiceDisabled('SubControlLedger-GroupSelect', true);
                await loadControls(grpCode, '#SubControlLedger-ControlSelect', ctrlCode);
                setChoiceDisabled('SubControlLedger-ControlSelect', true);

                $('#SubControlLedgerCodeNo').val(subCtrlCode);
                $('#SubControlLedgerCodePreview').html(seg(subCtrlCode));
                $('#SubControlLedgerName').val(subCtrlName);
                $('#ShortName').val(shortName);
                $('#IsActive').prop('checked', isActive);

                $('#SubControlLedger-formTitle').text(`Sub-Control Ledger: editing ${subCtrlCode}`);
                $('#SubControlLedger-saveBtn').text('Update');
                $('html, body').animate({ scrollTop: $('#SubControlLedger-form').offset().top - 80 }, 'fast');
            } else {
                if (typeof toastr !== 'undefined') toastr.warning(res.message || "Data not found.");
            }
        } catch (error) {
            if (typeof toastr !== 'undefined') toastr.error("Failed to fetch sub-control details.");
        }
    });

    // Archive / Restore
    $(document).on('click', '.SubControlLedgerArchiveBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        const isArchived = $(this).data('archived') === true || $(this).data('archived') === 'true';

        $.ajax({
            url: '/SubControlLedger/ToggleArchive',
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
    $(document).on('click', '.SubControlLedgerDeleteBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        if (!confirm(`Delete ledger ${code}?`)) return;

        $.ajax({
            url: '/SubControlLedger/SoftDelete',
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
    $('#SubControlLedger-pageSizeSelect').on('change', function () {
        pageSize = parseInt($(this).val(), 10);
        currentPage = 1;
        loadTableData();
    });

    let searchTimeout;
    $("#SubControlLedger-searchInput").on("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            currentPage = 1;
            loadTableData();
        }, 300);
    });

    $('#SubControlLedger-showArchived').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    function renderPagination(p) {
        const container = $("#SubControlLedger-paginationContainer");
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

    $(document).on('click', '#SubControlLedger-paginationContainer .coa-page-btn:not(.disabled)', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'), 10);
        if (page && page !== currentPage) {
            currentPage = page;
            loadTableData();
        }
    });

    function loadTableData() {
        const searchTerm = $("#SubControlLedger-searchInput").val();
        const groupCode = $('#SubControlLedger-GroupSelect').val();
        const controlCode = $('#SubControlLedger-ControlSelect').val(); // Filtered from top dropdown!
        const showArchived = $('#SubControlLedger-showArchived').is(':checked');

        $.ajax({
            url: '/SubControlLedger/GetAll',
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchTerm,
                controlCode: controlCode,
                groupCode: groupCode,
                showArchived: showArchived
            },
            success: function (response) {
                const tableBody = $("#SubControlLedger-body");
                tableBody.empty();

                if (response.data && response.data.length > 0) {
                    response.data.forEach(function (item) {
                        const codeHtml = seg(item.subControlLedgerCodeNo);
                        const isAr = !item.isActive;
                        const rowClass = isAr ? 'ar' : '';
                        const statusBadge = `<span class="coa-bd ${isAr ? 'off' : 'on'}">${isAr ? 'Archived' : 'Active'}</span>`;
                        const archiveActionText = isAr ? 'Restore' : 'Archive';

                        tableBody.append(`
                            <tr class="${rowClass}">
                                <td class="text-center">${codeHtml}</td>
                                <td>${esc(item.subControlLedgerName)}</td>
                                <td class="text-center">${esc(item.shortName || '-')}</td>
                                <td><span class="coa-pa">${esc(item.hierarchyPath)}</span></td>
                                <td class="text-center">${statusBadge}</td>
                                <td class="coa-ac text-center">
                                    <a href="#" class="SubControlLedgerEditBtn" data-code="${esc(item.subControlLedgerCodeNo)}">Edit</a>
                                    <a href="#" class="SubControlLedgerArchiveBtn" data-code="${esc(item.subControlLedgerCodeNo)}" data-archived="${isAr}">${archiveActionText}</a>
                                    <a href="#" class="SubControlLedgerDeleteBtn dl" data-code="${esc(item.subControlLedgerCodeNo)}">Delete</a>
                                </td>
                            </tr>
                        `);
                    });
                } else {
                    tableBody.append('<tr><td colspan="6" class="coa-em">No ledgers found. Change the search or add one above.</td></tr>');
                }

                if (response.paginationInfo) {
                    const p = response.paginationInfo;
                    $("#SubControlLedger-paginationInfo").text(`Showing ${p.startItem} to ${p.endItem} of ${p.totalItems} entries`);
                    renderPagination(p);
                }
            }
        });
    }

    // Check query params
    const urlParams = new URLSearchParams(window.location.search);
    const parentCodeParam = urlParams.get('parentCode') || urlParams.get('controlCode');
    const editCodeParam = urlParams.get('editCode');

    if (parentCodeParam) {
        const groupCode = parentCodeParam.substring(0, 1);
        $('#SubControlLedger-GroupSelect').val(groupCode);
        loadControls(groupCode, '#SubControlLedger-ControlSelect', parentCodeParam).then(() => {
            $('#SubControlLedger-ControlSelect').trigger('change');
        });
    }

    if (editCodeParam) {
        setTimeout(() => {
            $(`.SubControlLedgerEditBtn[data-code="${editCodeParam}"]`).trigger('click');
        }, 300);
    }

    // Init
    loadTableData();
    setTimeout(() => {
        $('#SubControlLedgerName').focus();
    }, 250);
});
