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

    // Load Controls
    function loadControls(groupCode, targetSelectId, selectedVal) {
        const rawId = targetSelectId.replace('#', '');
        const select = $(targetSelectId);

        if (!groupCode) {
            select.empty().append('<option value="">-- Select Control Ledger --</option>');
            if (window.choiceManager) {
                window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Control Ledger --' });
            }
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById(rawId);
            }
            setChoiceDisabled(rawId, true);
            return Promise.resolve();
        }

        return $.get(`/ControlLedger/GetDropdown?groupCode=${encodeURIComponent(groupCode)}`, function (items) {
            select.empty().append('<option value="">-- Select Control Ledger --</option>');
            if (items && items.length > 0) {
                items.forEach(function (c) {
                    select.append(`<option value="${esc(c.code)}">${esc(c.displayText)}</option>`);
                });
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, items, {
                        labelKey: 'displayText',
                        valueKey: 'code',
                        placeholder: '-- Select Control Ledger --'
                    });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, false);
                if (selectedVal) setChoiceVal(rawId, selectedVal);
            } else {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Control Ledger --' });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, true);
            }
        });
    }

    // Load Sub-Controls
    function loadSubControls(controlCode, targetSelectId, selectedVal) {
        const rawId = targetSelectId.replace('#', '');
        const select = $(targetSelectId);

        if (!controlCode) {
            select.empty().append('<option value="">-- Select Sub-Control --</option>');
            if (window.choiceManager) {
                window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Control --' });
            }
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById(rawId);
            }
            setChoiceDisabled(rawId, true);
            return Promise.resolve();
        }

        return $.get(`/SubControlLedger/GetDropdown?controlCode=${encodeURIComponent(controlCode)}`, function (items) {
            select.empty().append('<option value="">-- Select Sub-Control --</option>');
            if (items && items.length > 0) {
                items.forEach(function (c) {
                    select.append(`<option value="${esc(c.code)}">${esc(c.displayText)}</option>`);
                });
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, items, {
                        labelKey: 'displayText',
                        valueKey: 'code',
                        placeholder: '-- Select Sub-Control --'
                    });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, false);
                if (selectedVal) setChoiceVal(rawId, selectedVal);
            } else {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Control --' });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, true);
            }
        });
    }

    // Load Sub-Subsidiaries
    function loadSubSubsidiaries(subControlCode, targetSelectId, selectedVal) {
        const rawId = targetSelectId.replace('#', '');
        const select = $(targetSelectId);

        if (!subControlCode) {
            select.empty().append('<option value="">-- Select Sub-Subsidiary --</option>');
            if (window.choiceManager) {
                window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Subsidiary --' });
            }
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById(rawId);
            }
            setChoiceDisabled(rawId, true);
            return Promise.resolve();
        }

        return $.get(`/SubSubsidiaryLedger/GetDropdown?subControlCode=${encodeURIComponent(subControlCode)}`, function (items) {
            select.empty().append('<option value="">-- Select Sub-Subsidiary --</option>');
            if (items && items.length > 0) {
                items.forEach(function (c) {
                    select.append(`<option value="${esc(c.code)}">${esc(c.displayText)}</option>`);
                });
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, items, {
                        labelKey: 'displayText',
                        valueKey: 'code',
                        placeholder: '-- Select Sub-Subsidiary --'
                    });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, false);
                if (selectedVal) setChoiceVal(rawId, selectedVal);
            } else {
                if (window.choiceManager) {
                    window.choiceManager.populateDropdown(rawId, [], { placeholder: '-- Select Sub-Subsidiary --' });
                }
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById(rawId);
                }
                setChoiceDisabled(rawId, true);
            }
        });
    }

    // Cascades & Filter Grid!
    $('#GeneralLedger-GroupSelect').on('change', function () {
        const groupCode = $(this).val();
        $('#GeneralLedgerCodeNo').val('');
        $('#GeneralLedgerCodePreview').html('<span class="text-muted">Select Sub-Subsidiary first</span>');
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('GeneralLedger-SubControlSelect', [], { placeholder: '-- Select Sub-Control --' });
            window.choiceManager.populateDropdown('GeneralLedger-SubSubsidiarySelect', [], { placeholder: '-- Select Sub-Subsidiary --' });
        } else {
            $('#GeneralLedger-SubControlSelect').empty().append('<option value="">-- Select Sub-Control --</option>');
            $('#GeneralLedger-SubSubsidiarySelect').empty().append('<option value="">-- Select Sub-Subsidiary --</option>');
        }
        setChoiceDisabled('GeneralLedger-SubControlSelect', true);
        setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);

        loadControls(groupCode, '#GeneralLedger-ControlSelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#GeneralLedger-ControlSelect').on('change', function () {
        const controlCode = $(this).val();
        $('#GeneralLedgerCodeNo').val('');
        $('#GeneralLedgerCodePreview').html('<span class="text-muted">Select Sub-Subsidiary first</span>');
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('GeneralLedger-SubSubsidiarySelect', [], { placeholder: '-- Select Sub-Subsidiary --' });
        } else {
            $('#GeneralLedger-SubSubsidiarySelect').empty().append('<option value="">-- Select Sub-Subsidiary --</option>');
        }
        setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);

        loadSubControls(controlCode, '#GeneralLedger-SubControlSelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#GeneralLedger-SubControlSelect').on('change', function () {
        const subControlCode = $(this).val();
        $('#GeneralLedgerCodeNo').val('');
        $('#GeneralLedgerCodePreview').html('<span class="text-muted">Select Sub-Subsidiary first</span>');

        loadSubSubsidiaries(subControlCode, '#GeneralLedger-SubSubsidiarySelect').then(() => {
            currentPage = 1;
            loadTableData();
        });
    });

    $('#GeneralLedger-SubSubsidiarySelect').on('change', function () {
        const subSubCode = $(this).val();

        if ($('#GeneralLedger-isEdit').val() !== '1') {
            if (!subSubCode) {
                $('#GeneralLedgerCodeNo').val('');
                $('#GeneralLedgerCodePreview').html('<span class="text-muted">Select Sub-Subsidiary first</span>');
            } else {
                $.get(`/GeneralLedger/GetNextCode?subSubsidiaryCode=${encodeURIComponent(subSubCode)}`, function (res) {
                    if (res && res.success) {
                        $('#GeneralLedgerCodeNo').val(res.code);
                        $('#GeneralLedgerCodePreview').html(seg(res.code));
                    }
                });
            }
        }

        currentPage = 1;
        loadTableData();
    });

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

    // Reset Form
    function resetForm() {
        $('#GeneralLedger-form')[0].reset();
        $('#GeneralLedger-isEdit').val('0');
        $('#IsActive').prop('checked', true);
        setChoiceDisabled('GeneralLedger-GroupSelect', false);
        if (window.choiceManager && typeof window.choiceManager.resetChoice === 'function') {
            window.choiceManager.resetChoice('GeneralLedger-GroupSelect');
        } else {
            clearChoice('GeneralLedger-GroupSelect');
        }
        if (window.choiceManager) {
            window.choiceManager.populateDropdown('GeneralLedger-ControlSelect', [], { placeholder: '-- Select Control Ledger --' });
            window.choiceManager.populateDropdown('GeneralLedger-SubControlSelect', [], { placeholder: '-- Select Sub-Control --' });
            window.choiceManager.populateDropdown('GeneralLedger-SubSubsidiarySelect', [], { placeholder: '-- Select Sub-Subsidiary --' });
        } else {
            $('#GeneralLedger-ControlSelect').empty().append('<option value="">-- Select Control Ledger --</option>');
            $('#GeneralLedger-SubControlSelect').empty().append('<option value="">-- Select Sub-Control --</option>');
            $('#GeneralLedger-SubSubsidiarySelect').empty().append('<option value="">-- Select Sub-Subsidiary --</option>');
        }
        if (typeof window.reinitCustomDropdownById === 'function') {
            window.reinitCustomDropdownById('GeneralLedger-ControlSelect');
            window.reinitCustomDropdownById('GeneralLedger-SubControlSelect');
            window.reinitCustomDropdownById('GeneralLedger-SubSubsidiarySelect');
        }
        setChoiceDisabled('GeneralLedger-ControlSelect', true);
        setChoiceDisabled('GeneralLedger-SubControlSelect', true);
        setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);
        $('#GeneralLedgerCodeNo').val('');
        $('#GeneralLedgerCodePreview').html('<span class="text-muted">Select Sub-Subsidiary first</span>');
        $('#GeneralLedger-formTitle').text('General Ledger: new entry');
        $('#GeneralLedger-saveBtn').text('Save');
        $('#GeneralLedger-form').find('.text-danger').text('');
        currentPage = 1;
        loadTableData();
        setTimeout(function () {
            $('#GeneralLedgerName').focus();
        }, 150);
    }

    $('#GeneralLedger-resetBtn').on('click', resetForm);

    // Form Submit
    $('#GeneralLedger-form').submit(function (e) {
        e.preventDefault();

        const form = $(this);
        const isEdit = $('#GeneralLedger-isEdit').val() === '1';
        const url = isEdit ? '/GeneralLedger/Update' : '/GeneralLedger/Create';
        const $saveButton = $('#GeneralLedger-saveBtn');

        // Validation: Required Group Dropdown
        const groupCode = $('#GeneralLedger-GroupSelect').val();
        if (!groupCode) {
            form.find('[data-valmsg-for="GroupLedgerCodeNo"]').text('Please select Group Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Group Ledger');
            openDropdown('GeneralLedger-GroupSelect');
            return false;
        }

        // Validation: Required Control Dropdown
        const ctrlCode = $('#GeneralLedger-ControlSelect').val();
        if (!ctrlCode) {
            form.find('[data-valmsg-for="ControlLedgerCodeNo"]').text('Please select Control Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Control Ledger');
            openDropdown('GeneralLedger-ControlSelect');
            return false;
        }

        // Validation: Required Sub-Control Dropdown
        const subCtrlCode = $('#GeneralLedger-SubControlSelect').val();
        if (!subCtrlCode) {
            form.find('[data-valmsg-for="SubControlLedgerCodeNo"]').text('Please select Sub-Control Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Sub-Control Ledger');
            openDropdown('GeneralLedger-SubControlSelect');
            return false;
        }

        // Validation: Required Sub-Subsidiary Dropdown
        const subSubCode = $('#GeneralLedger-SubSubsidiarySelect').val();
        if (!subSubCode) {
            form.find('[data-valmsg-for="SubSubsidiaryLedgerCodeNo"]').text('Please select Sub-Subsidiary Ledger');
            if (typeof toastr !== 'undefined') toastr.warning('Please select Sub-Subsidiary Ledger');
            openDropdown('GeneralLedger-SubSubsidiarySelect');
            return false;
        }

        // Validation: Required Name
        const nameVal = $('#GeneralLedgerName').val() ? $('#GeneralLedgerName').val().trim() : '';
        if (!nameVal) {
            form.find('[data-valmsg-for="GeneralLedgerName"]').text('Please enter General Ledger Name');
            if (typeof toastr !== 'undefined') toastr.warning('Please enter General Ledger Name');
            $('#GeneralLedgerName').focus();
            return false;
        }

        $('#GeneralLedger-GroupSelect, #GeneralLedger-ControlSelect, #GeneralLedger-SubControlSelect, #GeneralLedger-SubSubsidiarySelect').prop('disabled', false);

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
                        setChoiceDisabled('GeneralLedger-GroupSelect', true);
                        setChoiceDisabled('GeneralLedger-ControlSelect', true);
                        setChoiceDisabled('GeneralLedger-SubControlSelect', true);
                        setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);
                    }
                }
            },
            error: function (err) {
                if (typeof toastr !== 'undefined') toastr.error("Server error: " + err.responseText);
                if (isEdit) {
                    setChoiceDisabled('GeneralLedger-GroupSelect', true);
                    setChoiceDisabled('GeneralLedger-ControlSelect', true);
                    setChoiceDisabled('GeneralLedger-SubControlSelect', true);
                    setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);
                }
            },
            complete: function () {
                $saveButton.prop('disabled', false);
            }
        });
    });

    // Edit
    $(document).on('click', '.GeneralLedgerEditBtn', async function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        try {
            const res = await $.get(`/GeneralLedger/GetById?code=${encodeURIComponent(code)}`);
            if (res.success && res.data) {
                const item = res.data;
                const grpCode = item.groupLedgerCodeNo || item.GroupLedgerCodeNo;
                const ctrlCode = item.controlLedgerCodeNo || item.ControlLedgerCodeNo;
                const subCtrlCode = item.subControlLedgerCodeNo || item.SubControlLedgerCodeNo;
                const sslCode = item.subSubsidiaryLedgerCodeNo || item.SubSubsidiaryLedgerCodeNo;
                const glCode = item.generalLedgerCodeNo || item.GeneralLedgerCodeNo;
                const glName = item.generalLedgerName || item.GeneralLedgerName;
                const shortName = item.shortName || item.ShortName || '';
                const isActive = item.isActive ?? item.IsActive ?? true;

                $('#GeneralLedger-isEdit').val('1');

                setChoiceVal('GeneralLedger-GroupSelect', grpCode);
                setChoiceDisabled('GeneralLedger-GroupSelect', true);
                await loadControls(grpCode, '#GeneralLedger-ControlSelect', ctrlCode);
                setChoiceDisabled('GeneralLedger-ControlSelect', true);

                await loadSubControls(ctrlCode, '#GeneralLedger-SubControlSelect', subCtrlCode);
                setChoiceDisabled('GeneralLedger-SubControlSelect', true);

                await loadSubSubsidiaries(subCtrlCode, '#GeneralLedger-SubSubsidiarySelect', sslCode);
                setChoiceDisabled('GeneralLedger-SubSubsidiarySelect', true);

                $('#GeneralLedgerCodeNo').val(glCode);
                $('#GeneralLedgerCodePreview').html(seg(glCode));
                $('#GeneralLedgerName').val(glName);
                $('#ShortName').val(shortName);
                $('#IsActive').prop('checked', isActive);

                $('#GeneralLedger-formTitle').text(`General Ledger: editing ${glCode}`);
                $('#GeneralLedger-saveBtn').text('Update');
                $('html, body').animate({ scrollTop: $('#GeneralLedger-form').offset().top - 80 }, 'fast');
            } else {
                if (typeof toastr !== 'undefined') toastr.warning(res.message || "Data not found.");
            }
        } catch (error) {
            if (typeof toastr !== 'undefined') toastr.error("Failed to fetch GL details.");
        }
    });

    // Archive / Restore
    $(document).on('click', '.GeneralLedgerArchiveBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        const isArchived = $(this).data('archived') === true || $(this).data('archived') === 'true';

        $.ajax({
            url: '/GeneralLedger/ToggleArchive',
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
    $(document).on('click', '.GeneralLedgerDeleteBtn', function (e) {
        e.preventDefault();
        const code = $(this).data('code');
        if (!code) return;

        if (!confirm(`Delete ledger ${code}?`)) return;

        $.ajax({
            url: '/GeneralLedger/SoftDelete',
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
    $('#GeneralLedger-pageSizeSelect').on('change', function () {
        pageSize = parseInt($(this).val(), 10);
        currentPage = 1;
        loadTableData();
    });

    let searchTimeout;
    $("#GeneralLedger-searchInput").on("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            currentPage = 1;
            loadTableData();
        }, 300);
    });

    $('#GeneralLedger-showArchived').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    function renderPagination(p) {
        const container = $("#GeneralLedger-paginationContainer");
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

    $(document).on('click', '#GeneralLedger-paginationContainer .coa-page-btn:not(.disabled)', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'), 10);
        if (page && page !== currentPage) {
            currentPage = page;
            loadTableData();
        }
    });

    function loadTableData() {
        const searchTerm = $("#GeneralLedger-searchInput").val();
        const groupCode = $('#GeneralLedger-GroupSelect').val();
        const controlCode = $('#GeneralLedger-ControlSelect').val();
        const subControlCode = $('#GeneralLedger-SubControlSelect').val();
        const subSubsidiaryCode = $('#GeneralLedger-SubSubsidiarySelect').val(); // Filtered from top dropdown!
        const showArchived = $('#GeneralLedger-showArchived').is(':checked');

        $.ajax({
            url: '/GeneralLedger/GetAll',
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchTerm,
                subSubsidiaryCode: subSubsidiaryCode,
                subControlCode: subControlCode,
                controlCode: controlCode,
                groupCode: groupCode,
                showArchived: showArchived
            },
            success: function (response) {
                const tableBody = $("#GeneralLedger-body");
                tableBody.empty();

                if (response.data && response.data.length > 0) {
                    response.data.forEach(function (item) {
                        const codeHtml = seg(item.generalLedgerCodeNo);
                        const isAr = !item.isActive;
                        const rowClass = isAr ? 'ar' : '';
                        const statusBadge = `<span class="coa-bd ${isAr ? 'off' : 'on'}">${isAr ? 'Archived' : 'Active'}</span>`;
                        const archiveActionText = isAr ? 'Restore' : 'Archive';

                        tableBody.append(`
                            <tr class="${rowClass}">
                                <td class="text-center">${codeHtml}</td>
                                <td>${esc(item.generalLedgerName)}</td>
                                <td class="text-center">${esc(item.shortName || '-')}</td>
                                <td><span class="coa-pa">${esc(item.hierarchyPath)}</span></td>
                                <td class="text-center">${statusBadge}</td>
                                <td class="coa-ac text-center">
                                    <a href="#" class="GeneralLedgerEditBtn" data-code="${esc(item.generalLedgerCodeNo)}">Edit</a>
                                    <a href="#" class="GeneralLedgerArchiveBtn" data-code="${esc(item.generalLedgerCodeNo)}" data-archived="${isAr}">${archiveActionText}</a>
                                    <a href="#" class="GeneralLedgerDeleteBtn dl" data-code="${esc(item.generalLedgerCodeNo)}">Delete</a>
                                </td>
                            </tr>
                        `);
                    });
                } else {
                    tableBody.append('<tr><td colspan="6" class="coa-em">No ledgers found. Change the search or add one above.</td></tr>');
                }

                if (response.paginationInfo) {
                    const p = response.paginationInfo;
                    $("#GeneralLedger-paginationInfo").text(`Showing ${p.startItem} to ${p.endItem} of ${p.totalItems} entries`);
                    renderPagination(p);
                }
            }
        });
    }

    // Check query params
    const urlParams = new URLSearchParams(window.location.search);
    const parentCodeParam = urlParams.get('parentCode') || urlParams.get('subSubsidiaryCode');
    const editCodeParam = urlParams.get('editCode');

    if (parentCodeParam) {
        const groupCode = parentCodeParam.substring(0, 1);
        const controlCode = parentCodeParam.substring(0, 3);
        const subControlCode = parentCodeParam.substring(0, 6);

        $('#GeneralLedger-GroupSelect').val(groupCode);
        loadControls(groupCode, '#GeneralLedger-ControlSelect', controlCode).then(() => {
            loadSubControls(controlCode, '#GeneralLedger-SubControlSelect', subControlCode).then(() => {
                loadSubSubsidiaries(subControlCode, '#GeneralLedger-SubSubsidiarySelect', parentCodeParam).then(() => {
                    $('#GeneralLedger-SubSubsidiarySelect').trigger('change');
                });
            });
        });
    }

    if (editCodeParam) {
        setTimeout(() => {
            $(`.GeneralLedgerEditBtn[data-code="${editCodeParam}"]`).trigger('click');
        }, 300);
    }

    // Init
    loadTableData();
    setTimeout(() => {
        $('#GeneralLedgerName').focus();
    }, 250);
});
