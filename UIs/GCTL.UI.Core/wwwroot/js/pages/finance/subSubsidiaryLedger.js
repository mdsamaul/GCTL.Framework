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

    // Load Controls
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
                if (selectedVal) setChoiceVal(rawId, selectedVal);
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

    // Load Sub-Controls
    function loadSubControls(controlCode, targetSelectId, selectedVal) {
        const rawId = targetSelectId.replace('#', '');

        if (!controlCode) {
            if (window.choiceManager) {
                window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Control --' });
            } else {
                $(targetSelectId).empty().append('<option value="">-- Select Sub-Control --</option>');
            }
            setChoiceDisabled(rawId, true);
            return Promise.resolve();
        }

        return $.get(`/SubControlLedger/GetDropdown?controlCode=${encodeURIComponent(controlCode)}`, function (items) {
            if (items && items.length > 0) {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, items, {
                        labelKey: 'displayText',
                        valueKey: 'code',
                        placeholder: '-- Select Sub-Control --'
                    });
                } else {
                    const select = $(targetSelectId);
                    select.empty().append('<option value="">-- Select Sub-Control --</option>');
                    items.forEach(function (c) {
                        select.append(`<option value="${esc(c.code)}">${esc(c.displayText)}</option>`);
                    });
                }
                setChoiceDisabled(rawId, false);
                if (selectedVal) setChoiceVal(rawId, selectedVal);
            } else {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Control --' });
                } else {
                    $(targetSelectId).empty().append('<option value="">-- Select Sub-Control --</option>');
                }
                setChoiceDisabled(rawId, true);
            }
        });
    }

    // Top Dropdowns Cascade & Filter Grid!
    $('#SubSubsidiaryLedger-GroupSelect').on('change', function () {
        const groupCode = $(this).val();
        $('#SubSubsidiaryLedgerCodeNo').val('');
        $('#SubSubsidiaryLedgerCodePreview').html('<span class="text-muted">Select Sub-Control first</span>');
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('SubSubsidiaryLedger-SubControlSelect', [], { placeholder: '-- Select Sub-Control --' });
        } else {
            $('#SubSubsidiaryLedger-SubControlSelect').empty().append('<option value="">-- Select Sub-Control --</option>');
        }
        setChoiceDisabled('SubSubsidiaryLedger-SubControlSelect', true);

        loadControls(groupCode, '#SubSubsidiaryLedger-ControlSelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#SubSubsidiaryLedger-ControlSelect').on('change', function () {
        const controlCode = $(this).val();
        $('#SubSubsidiaryLedgerCodeNo').val('');
        $('#SubSubsidiaryLedgerCodePreview').html('<span class="text-muted">Select Sub-Control first</span>');

        loadSubControls(controlCode, '#SubSubsidiaryLedger-SubControlSelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#SubSubsidiaryLedger-SubControlSelect').on('change', function () {
        const subControlCode = $(this).val();

        if ($('#SubSubsidiaryLedger-isEdit').val() !== '1') {
            if (!subControlCode) {
                $('#SubSubsidiaryLedgerCodeNo').val('');
                $('#SubSubsidiaryLedgerCodePreview').html('<span class="text-muted">Select Sub-Control first</span>');
            } else {
                $.get(`/SubSubsidiaryLedger/GetNextCode?subControlCode=${encodeURIComponent(subControlCode)}`, function (res) {
                    if (res && res.success) {
                        $('#SubSubsidiaryLedgerCodeNo').val(res.code);
                        $('#SubSubsidiaryLedgerCodePreview').html(seg(res.code));
                    }
                });
            }
        }

        currentPage = 1;
        loadTableData();
    });

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

    // Reset Form
    function resetForm() {
        $('#SubSubsidiaryLedger-form')[0].reset();
        $('#SubSubsidiaryLedger-isEdit').val('0');
        $('#IsActive').prop('checked', true);
        setChoiceDisabled('SubSubsidiaryLedger-GroupSelect', false);
        if (window.choiceManager && typeof window.choiceManager.resetChoice === 'function') {
            window.choiceManager.resetChoice('SubSubsidiaryLedger-GroupSelect');
        } else {
            clearChoice('SubSubsidiaryLedger-GroupSelect');
        }
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('SubSubsidiaryLedger-ControlSelect', [], { placeholder: '-- Select Control Ledger --' });
            window.choiceManager.populateDropdown('SubSubsidiaryLedger-SubControlSelect', [], { placeholder: '-- Select Sub-Control --' });
        } else {
            $('#SubSubsidiaryLedger-ControlSelect').empty().append('<option value="">-- Select Control Ledger --</option>');
            $('#SubSubsidiaryLedger-SubControlSelect').empty().append('<option value="">-- Select Sub-Control --</option>');
        }
        setChoiceDisabled('SubSubsidiaryLedger-ControlSelect', true);
        setChoiceDisabled('SubSubsidiaryLedger-SubControlSelect', true);
        $('#SubSubsidiaryLedgerCodeNo').val('');
        $('#SubSubsidiaryLedgerCodePreview').html('<span class="text-muted">Select Sub-Control first</span>');
        $('#SubSubsidiaryLedger-formTitle').text('Sub-Subsidiary Ledger: new entry');
        $('#SubSubsidiaryLedger-saveBtn').text('Save');
        $('#SubSubsidiaryLedger-form').find('.text-danger').text('');
        currentPage = 1;
        loadTableData();
        setTimeout(function () {
            $('#SubSubsidiaryLedgerName').focus();
        }, 150);
    }

    $('#SubSubsidiaryLedger-resetBtn').on('click', resetForm);

    // Form Submit
    $('#SubSubsidiaryLedger-form').submit(function (e) {
        e.preventDefault();

        const form = $(this);
        const isEdit = $('#SubSubsidiaryLedger-isEdit').val() === '1';
        const url = isEdit ? '/SubSubsidiaryLedger/Update' : '/SubSubsidiaryLedger/Create';
        const $saveButton = $('#SubSubsidiaryLedger-saveBtn');

        // Validation: Required Group Dropdown
        const groupCode = $('#SubSubsidiaryLedger-GroupSelect').val();
        if (!groupCode) {
            form.find('[data-valmsg-for="GroupLedgerCodeNo"]').text('Please select Group Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Group Ledger');
            openDropdown('SubSubsidiaryLedger-GroupSelect');
            return false;
        }

        // Validation: Required Control Dropdown
        const ctrlCode = $('#SubSubsidiaryLedger-ControlSelect').val();
        if (!ctrlCode) {
            form.find('[data-valmsg-for="ControlLedgerCodeNo"]').text('Please select Control Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Control Ledger');
            openDropdown('SubSubsidiaryLedger-ControlSelect');
            return false;
        }

        // Validation: Required Sub-Control Dropdown
        const subCtrlCode = $('#SubSubsidiaryLedger-SubControlSelect').val();
        if (!subCtrlCode) {
            form.find('[data-valmsg-for="SubControlLedgerCodeNo"]').text('Please select Sub-Control Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Sub-Control Ledger');
            openDropdown('SubSubsidiaryLedger-SubControlSelect');
            return false;
        }

        // Validation: Required Name
        const nameVal = $('#SubSubsidiaryLedgerName').val() ? $('#SubSubsidiaryLedgerName').val().trim() : '';
        if (!nameVal) {
            form.find('[data-valmsg-for="SubSubsidiaryLedgerName"]').text('Please enter Sub-Subsidiary Name');
            if (typeof toastr !== 'undefined') toastr.warning('Please enter Sub-Subsidiary Name');
            $('#SubSubsidiaryLedgerName').focus();
            return false;
        }

        $('#SubSubsidiaryLedger-GroupSelect, #SubSubsidiaryLedger-ControlSelect, #SubSubsidiaryLedger-SubControlSelect').prop('disabled', false);

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
                        setChoiceDisabled('SubSubsidiaryLedger-GroupSelect', true);
                        setChoiceDisabled('SubSubsidiaryLedger-ControlSelect', true);
                        setChoiceDisabled('SubSubsidiaryLedger-SubControlSelect', true);
                    }
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
                if (isEdit) {
                    setChoiceDisabled('SubSubsidiaryLedger-GroupSelect', true);
                    setChoiceDisabled('SubSubsidiaryLedger-ControlSelect', true);
                    setChoiceDisabled('SubSubsidiaryLedger-SubControlSelect', true);
                }
            },
            complete: function () {
                $saveButton.prop('disabled', false);
            }
        });
    });

    // Edit
    $(document).on('click', '.SubSubsidiaryLedgerEditBtn', async function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        try {
            const res = await $.get(`/SubSubsidiaryLedger/GetById?code=${encodeURIComponent(code)}`);
            if (res.success && res.data) {
                const item = res.data;
                const grpCode = item.groupLedgerCodeNo || item.GroupLedgerCodeNo;
                const ctrlCode = item.controlLedgerCodeNo || item.ControlLedgerCodeNo;
                const subCtrlCode = item.subControlLedgerCodeNo || item.SubControlLedgerCodeNo;
                const sslCode = item.subSubsidiaryLedgerCodeNo || item.SubSubsidiaryLedgerCodeNo;
                const sslName = item.subSubsidiaryLedgerName || item.SubSubsidiaryLedgerName;
                const shortName = item.shortName || item.ShortName || '';
                const isActive = item.isActive ?? item.IsActive ?? true;

                $('#SubSubsidiaryLedger-isEdit').val('1');

                setChoiceVal('SubSubsidiaryLedger-GroupSelect', grpCode);
                setChoiceDisabled('SubSubsidiaryLedger-GroupSelect', true);
                await loadControls(grpCode, '#SubSubsidiaryLedger-ControlSelect', ctrlCode);
                setChoiceDisabled('SubSubsidiaryLedger-ControlSelect', true);

                await loadSubControls(ctrlCode, '#SubSubsidiaryLedger-SubControlSelect', subCtrlCode);
                setChoiceDisabled('SubSubsidiaryLedger-SubControlSelect', true);

                $('#SubSubsidiaryLedgerCodeNo').val(sslCode);
                $('#SubSubsidiaryLedgerCodePreview').html(seg(sslCode));
                $('#SubSubsidiaryLedgerName').val(sslName);
                $('#ShortName').val(shortName);
                $('#IsActive').prop('checked', isActive);

                $('#SubSubsidiaryLedger-formTitle').text(`Sub-Subsidiary Ledger: editing ${sslCode}`);
                $('#SubSubsidiaryLedger-saveBtn').text('Update');
                $('html, body').animate({ scrollTop: $('#SubSubsidiaryLedger-form').offset().top - 80 }, 'fast');
            } else {
                if (typeof toastr !== 'undefined') toastr.warning(res.message || "Data not found.");
            }
        } catch (error) {
            if (typeof toastr !== 'undefined') toastr.error("Failed to fetch sub-subsidiary details.");
        }
    });

    // Archive / Restore
    $(document).on('click', '.SubSubsidiaryLedgerArchiveBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        const isArchived = $(this).data('archived') === true || $(this).data('archived') === 'true';

        $.ajax({
            url: '/SubSubsidiaryLedger/ToggleArchive',
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
    $(document).on('click', '.SubSubsidiaryLedgerDeleteBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        if (!confirm(`Delete ledger ${code}?`)) return;

        $.ajax({
            url: '/SubSubsidiaryLedger/SoftDelete',
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
    $('#SubSubsidiaryLedger-pageSizeSelect').on('change', function () {
        pageSize = parseInt($(this).val(), 10);
        currentPage = 1;
        loadTableData();
    });

    let searchTimeout;
    $("#SubSubsidiaryLedger-searchInput").on("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            currentPage = 1;
            loadTableData();
        }, 300);
    });

    $('#SubSubsidiaryLedger-showArchived').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    function renderPagination(p) {
        const container = $("#SubSubsidiaryLedger-paginationContainer");
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

    $(document).on('click', '#SubSubsidiaryLedger-paginationContainer .coa-page-btn:not(.disabled)', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'), 10);
        if (page && page !== currentPage) {
            currentPage = page;
            loadTableData();
        }
    });

    function loadTableData() {
        const searchTerm = $("#SubSubsidiaryLedger-searchInput").val();
        const groupCode = $('#SubSubsidiaryLedger-GroupSelect').val();
        const controlCode = $('#SubSubsidiaryLedger-ControlSelect').val();
        const subControlCode = $('#SubSubsidiaryLedger-SubControlSelect').val(); // Filtered from top dropdown!
        const showArchived = $('#SubSubsidiaryLedger-showArchived').is(':checked');

        $.ajax({
            url: '/SubSubsidiaryLedger/GetAll',
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchTerm,
                subControlCode: subControlCode,
                controlCode: controlCode,
                groupCode: groupCode,
                showArchived: showArchived
            },
            success: function (response) {
                const tableBody = $("#SubSubsidiaryLedger-body");
                tableBody.empty();

                if (response.data && response.data.length > 0) {
                    response.data.forEach(function (item) {
                        const codeHtml = seg(item.subSubsidiaryLedgerCodeNo);
                        const isAr = !item.isActive;
                        const rowClass = isAr ? 'ar' : '';
                        const statusBadge = `<span class="coa-bd ${isAr ? 'off' : 'on'}">${isAr ? 'Archived' : 'Active'}</span>`;
                        const archiveActionText = isAr ? 'Restore' : 'Archive';

                        tableBody.append(`
                            <tr class="${rowClass}">
                                <td class="text-center">${codeHtml}</td>
                                <td>${esc(item.subSubsidiaryLedgerName)}</td>
                                <td class="text-center">${esc(item.shortName || '-')}</td>
                                <td><span class="coa-pa">${esc(item.hierarchyPath)}</span></td>
                                <td class="text-center">${statusBadge}</td>
                                <td class="coa-ac text-center">
                                    <a href="#" class="SubSubsidiaryLedgerEditBtn" data-code="${esc(item.subSubsidiaryLedgerCodeNo)}">Edit</a>
                                    <a href="#" class="SubSubsidiaryLedgerArchiveBtn" data-code="${esc(item.subSubsidiaryLedgerCodeNo)}" data-archived="${isAr}">${archiveActionText}</a>
                                    <a href="#" class="SubSubsidiaryLedgerDeleteBtn dl" data-code="${esc(item.subSubsidiaryLedgerCodeNo)}">Delete</a>
                                </td>
                            </tr>
                        `);
                    });
                } else {
                    tableBody.append('<tr><td colspan="6" class="coa-em">No ledgers found. Change the search or add one above.</td></tr>');
                }

                if (response.paginationInfo) {
                    const p = response.paginationInfo;
                    $("#SubSubsidiaryLedger-paginationInfo").text(`Showing ${p.startItem} to ${p.endItem} of ${p.totalItems} entries`);
                    renderPagination(p);
                }
            }
        });
    }

    // Check query params
    const urlParams = new URLSearchParams(window.location.search);
    const parentCodeParam = urlParams.get('parentCode') || urlParams.get('subControlCode');
    const editCodeParam = urlParams.get('editCode');

    if (parentCodeParam) {
        const groupCode = parentCodeParam.substring(0, 1);
        const controlCode = parentCodeParam.substring(0, 3);
        $('#SubSubsidiaryLedger-GroupSelect').val(groupCode);
        loadControls(groupCode, '#SubSubsidiaryLedger-ControlSelect', controlCode).then(() => {
            loadSubControls(controlCode, '#SubSubsidiaryLedger-SubControlSelect', parentCodeParam).then(() => {
                $('#SubSubsidiaryLedger-SubControlSelect').trigger('change');
            });
        });
    }

    if (editCodeParam) {
        setTimeout(() => {
            $(`.SubSubsidiaryLedgerEditBtn[data-code="${editCodeParam}"]`).trigger('click');
        }, 300);
    }

    // Init
    loadTableData();
    setTimeout(() => {
        $('#SubSubsidiaryLedgerName').focus();
    }, 250);
});
