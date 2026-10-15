(function ($) {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentSortColumn = 'ComOpeningBalanceCode';
    let currentSortOrder = 'asc';
    let currentTableData = [];
    let searchTimer = null;
    let pendingDeleteCode = null;

    function getFormModal() {
        const el = document.getElementById('openingBalanceModal');
        return el && typeof bootstrap !== 'undefined' ? bootstrap.Modal.getOrCreateInstance(el) : null;
    }

    function getConfirmModal() {
        const el = document.getElementById('obConfirmModal');
        return el && typeof bootstrap !== 'undefined' ? bootstrap.Modal.getOrCreateInstance(el) : null;
    }

    function setDatePickerValue(id, val) {
        const el = document.getElementById(id);
        if (el && el._flatpickr) {
            if (val) {
                el._flatpickr.setDate(val, true);
            } else {
                el._flatpickr.clear();
            }
        } else {
            $(`#${id}`).val(val || '');
        }
    }

    function setSelectValue(id, val, text) {
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

    function clearSelectValue(id) {
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

    $(document).ready(function () {
        initEvents();
        loadTableData();
        if ($('#OpeningBalance-form').length && !$('#openingBalanceModal').length) {
            resetForm();
        }
    });

    function initEvents() {
        // Search input with debounce
        $('#OpeningBalance-searchInput').on('input', function () {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                currentPage = 1;
                loadTableData();
            }, 300);
        });

        // Header search input (for ComIndex and header search box)
        $('#OpeningBalance-headerSearch').on('input', function () {
            const val = $(this).val();
            $('#OpeningBalance-searchInput').val(val);
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                currentPage = 1;
                loadTableData();
            }, 300);
        });

        // Page size select change
        $('#OpeningBalance-pageSizeSelect').on('change', function () {
            pageSize = parseInt($(this).val(), 10) || 10;
            currentPage = 1;
            loadTableData();
        });

        /* ===== Datepickers ===== */
        if (typeof initializeDatepickerDMY === 'function') {
            initializeDatepickerDMY("fFrom, fTo, modalOpeningDate");
        }

    // Filter elements change
    $('#fFinancialYear, #fFrom, #fTo, #fAccountHead').on('change', function () {
        currentPage = 1;
        loadTableData();
    });

    // Reset filters
    $('#btnResetFilters').on('click', function () {
        clearSelectValue('fFinancialYear');
        clearSelectValue('fAccountHead');
        setDatePickerValue('fFrom', '');
        setDatePickerValue('fTo', '');
        $('#OpeningBalance-searchInput').val('');
        $('#OpeningBalance-headerSearch').val('');
        currentPage = 1;
        loadTableData();
    });

        // Sorting on sortable table headers
        $(document).on('click', '#OpeningBalance-table th.sort', function () {
            const col = $(this).data('sort');
            if (!col) return;

            if (currentSortColumn === col) {
                currentSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
            } else {
                currentSortColumn = col;
                currentSortOrder = 'asc';
            }

            updateSortIcons();
            loadTableData();
        });

        // Pagination Prev / Next / Page clicks
        $('#OpeningBalance-prevPageBtn').on('click', function () {
            if (currentPage > 1) {
                currentPage--;
                loadTableData();
            }
        });

        $('#OpeningBalance-nextPageBtn').on('click', function () {
            currentPage++;
            loadTableData();
        });

        $(document).on('click', '.page-btn', function (e) {
            e.preventDefault();
            const page = parseInt($(this).data('page'), 10);
            if (page && page !== currentPage) {
                currentPage = page;
                loadTableData();
            }
        });

        // Bulk Selection Management matching StudentAdmission
        $(document).on('change', '#OpeningBalance-check-all', function () {
            const isChecked = $(this).is(':checked');
            $('.OpeningBalance-selectItem').prop('checked', isChecked);
            toggleBulkActions();
        });

        $(document).on('change', '.OpeningBalance-selectItem', function () {
            toggleBulkActions();
        });

        $('#OpeningBalance-delSel').on('click', function () {
            const selectedItems = $('.OpeningBalance-selectItem:checked');
            const selectedIds = [];
            selectedItems.each(function () {
                selectedIds.push($(this).data('id'));
            });

            if (selectedIds.length > 0) {
                showDeleteConfirm(selectedIds);
            } else {
                toastr.info('Please select at least one item to delete.');
            }
        });

        // Open Add Modal
        $('#btnOpenNewModal').on('click', function () {
            openAddModal();
        });

        // Modal Financial Year change -> fetch next OB Code
        $(document).on('change', '#modalFinancialYear', function () {
            const fy = $(this).val();
            const isEdit = $('#OpeningBalance-isEdit').val() === '1';
            if (fy && !isEdit) {
                fetchNextCode(fy);
            }
        });

        // Modal Account Head change -> auto-set Dr / Cr default
        $(document).on('change', '#modalAccountHead', function () {
            const selectedOpt = $(this).find('option:selected');
            const defType = selectedOpt.data('deftype');
            const currentTr = window.choiceManager ? choiceManager.getChoiceValue('modalTrType') : $('#modalTrType').val();
            if (defType && !currentTr) {
                if (window.choiceManager) {
                    choiceManager.setChoiceValue('modalTrType', defType);
                } else {
                    $('#modalTrType').val(defType);
                }
            }
        });

        // Modal Form Submit
        $('#OpeningBalance-saveBtn').on('click', function () {
            saveOpeningBalance();
        });

        $('#OpeningBalance-form').on('submit', function (e) {
            e.preventDefault();
            saveOpeningBalance();
        });

        // Embedded Form Reset / Cancel
        $('#OpeningBalance-resetBtn').on('click', function () {
            resetForm();
        });

        // Row Edit Button click
        $(document).on('click', '.btn-edit-row', function () {
            const code = $(this).data('code');
            if (code) {
                openEditModal(code);
            }
        });

        // Row Delete Button click
        $(document).on('click', '.btn-delete-row', function () {
            const code = $(this).data('code');
            if (code) {
                showDeleteConfirm(code);
            }
        });

        // Confirm Delete Submit
        $('#obConfirmSubmitBtn').on('click', function () {
            if (pendingDeleteCode) {
                executeDelete(pendingDeleteCode);
            }
        });

        // Excel CSV Upload Form Submit
        $('#OpeningBalance-formUploadCsv').on('submit', function (e) {
            e.preventDefault();
            uploadCsv();
        });

        // Export Print / PDF
        $('#btnExportPrint, #btnExportPdf').on('click', function () {
            window.print();
        });

        // Export Excel (.xlsx)
        $('#btnExportCSV, #btnExportExcel').on('click', function () {
            exportToExcel();
        });
    }

    function toggleBulkActions() {
        const allItems = $('.OpeningBalance-selectItem');
        const checkedItems = $('.OpeningBalance-selectItem:checked');
        const allChecked = allItems.length > 0 && allItems.length === checkedItems.length;
        const someChecked = checkedItems.length > 0 && !allChecked;

        $('#OpeningBalance-check-all').prop('checked', allChecked);
        $('#OpeningBalance-check-all').prop('indeterminate', someChecked);

        if (checkedItems.length > 0) {
            $('#OpeningBalance-bulkSelectActions').removeClass('d-none');
            $('#OpeningBalance-searchBox').addClass('d-none');
        } else {
            $('#OpeningBalance-bulkSelectActions').addClass('d-none');
            $('#OpeningBalance-searchBox').removeClass('d-none');
        }
    }

    function resetBulkSelection() {
        $('.OpeningBalance-selectItem').prop('checked', false);
        $('#OpeningBalance-check-all').prop('checked', false).prop('indeterminate', false);
        $('#OpeningBalance-bulkSelectActions').addClass('d-none');
        $('#OpeningBalance-searchBox').removeClass('d-none');
    }

    function loadTableData() {
        const fyVal = window.choiceManager ? (choiceManager.getChoiceValue('fFinancialYear') || $('#fFinancialYear').val()) : $('#fFinancialYear').val();
        const headVal = window.choiceManager ? (choiceManager.getChoiceValue('fAccountHead') || $('#fAccountHead').val()) : $('#fAccountHead').val();

        const filter = {
            PageNumber: currentPage,
            PageSize: pageSize,
            SearchTerm: $('#OpeningBalance-searchInput').val()?.trim() || '',
            FinancialCodeNo: (fyVal || '').trim() || null,
            GeneralLedgerCodeNo: (headVal || '').trim() || null,
            FromDate: $('#fFrom').val() || null,
            ToDate: $('#fTo').val() || null,
            SortColumn: currentSortColumn,
            SortOrder: currentSortOrder
        };

        $('#OpeningBalance-body').html(`
            <tr>
                <td colspan="10" class="text-center py-5 text-muted">
                    <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
                    Loading opening balance records...
                </td>
            </tr>
        `);

        $.ajax({
            url: '/OpeningBalance/GetAll',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filter),
            success: function (res) {
                if (res.success && res.data) {
                    currentTableData = res.data.items || [];
                    renderTable(currentTableData, res.data.totalCount);
                    renderSummary(res.data.summary);
                    updatePagination(res.data.totalCount, res.data.pageNumber, res.data.pageSize);
                } else {
                    $('#OpeningBalance-body').html('<tr><td colspan="10" class="text-center text-danger py-4">Failed to load data.</td></tr>');
                }
            },
            error: function () {
                $('#OpeningBalance-body').html('<tr><td colspan="10" class="text-center text-danger py-4">An error occurred while fetching data.</td></tr>');
            }
        });
    }

    function renderTable(items, totalCount) {
        resetBulkSelection();

        if (!items || items.length === 0) {
            $('#OpeningBalance-body').html(`
                <tr>
                    <td colspan="10" class="text-center py-4 text-muted">
                        No data available
                    </td>
                </tr>
            `);
            $('#OpeningBalance-paginationInfo').text('Showing 0 to 0 of 0 entries');
            return;
        }

        let html = '';
        items.forEach(function (r) {
            const trBadge = r.trType === 'Dr'
                ? '<span class="ob-badge-dr"><i class="fas fa-arrow-down me-1"></i>Dr</span>'
                : '<span class="ob-badge-cr"><i class="fas fa-arrow-up me-1"></i>Cr</span>';

            const fyBadge = r.financialCodeNo ? `<span class="ob-badge-fy">${escapeHtml(r.financialCodeNo)}</span>` : '';

            const accHeadDisplay = r.generalLedgerCodeNo && r.generalLedgerName
                ? `${escapeHtml(r.generalLedgerCodeNo)} - ${escapeHtml(r.generalLedgerName)}`
                : escapeHtml(r.generalLedgerName || r.generalLedgerCodeNo || '');

            html += `
                <tr class="hover-actions-trigger btn-reveal-trigger position-static">
                    <td class="fs-9 align-middle text-center py-2">
                        <div class="form-check mb-0 fs-8 d-inline-block">
                            <input class="form-check-input OpeningBalance-selectItem" data-id="${r.comOpeningBalanceCode}" type="checkbox" />
                        </div>
                    </td>
                    <td class="align-middle text-center white-space-nowrap text-body py-2">
                        ${escapeHtml(r.comOpeningBalanceCode)}
                    </td>
                    <td class="align-middle py-2 ps-3 text-start fw-semibold text-body">
                        ${accHeadDisplay}
                    </td>
                    <td class="align-middle py-2 text-end fw-bold font-monospace text-body-emphasis">${formatCurrency(r.openingBalance)}</td>
                    <td class="align-middle py-2 text-center text-nowrap">${trBadge}</td>
                    <td class="align-middle py-2 text-center text-nowrap text-body">${escapeHtml(r.openingDateFormatted || '')}</td>
                    <td class="align-middle py-2 text-center text-nowrap">${fyBadge}</td>
                    <td class="align-middle py-2 text-start text-body">${escapeHtml(r.reference || '')}</td>
                    <td class="align-middle py-2 text-start text-body text-truncate" style="max-width: 180px;" title="${escapeHtml(r.description || '')}">${escapeHtml(r.description || '')}</td>
                    <td class="align-middle py-2 text-center text-nowrap" style="width: 150px; min-width: 140px;">
                        <div class="adms-act justify-content-center">
                            <button type="button" class="a-p btn-edit-row" data-code="${r.comOpeningBalanceCode}" title="Edit"><i class="fas fa-edit me-1"></i>Edit</button>
                            <button type="button" class="a-no btn-delete-row" data-code="${r.comOpeningBalanceCode}" title="Delete"><i class="fas fa-trash-alt me-1"></i>Delete</button>
                        </div>
                    </td>
                </tr>
            `;
        });

        $('#OpeningBalance-body').html(html);

        const start = ((currentPage - 1) * pageSize) + 1;
        const end = Math.min(currentPage * pageSize, totalCount);
        $('#OpeningBalance-paginationInfo').text(`Showing ${start} to ${end} of ${totalCount} entries`);
    }

    function renderSummary(summary) {
        if (!summary) return;
        const d = summary.totalDebit || 0;
        const c = summary.totalCredit || 0;
        const diff = Math.abs(d - c);
        const isBalanced = diff < 0.005;

        $('#lblTotalDebit').text(formatCurrency(d));
        $('#lblTotalCredit').text(formatCurrency(c));

        if (isBalanced) {
            $('#lblDifference').text(formatCurrency(diff)).removeClass('text-danger').addClass('text-success');
            $('#badgeDiffStatus')
                .removeClass('badge-subtle-danger')
                .addClass('badge-subtle-success')
                .html('<i class="fas fa-check-circle me-1"></i>Balanced');
            $('#cardDifference').css('--stat-color', '#16a34a');
        } else {
            const status = d > c ? 'Dr excess' : 'Cr excess';
            $('#lblDifference').text(formatCurrency(diff)).removeClass('text-success').addClass('text-danger');
            $('#badgeDiffStatus')
                .removeClass('badge-subtle-success')
                .addClass('badge-subtle-danger')
                .html(`<i class="fas fa-exclamation-triangle me-1"></i>${status}`);
            $('#cardDifference').css('--stat-color', '#dc2626');
        }
    }

    function updatePagination(totalCount, page, pSize) {
        const totalPages = Math.ceil(totalCount / pSize) || 1;
        const paginationLinks = $('#OpeningBalance-paginationLinks');
        paginationLinks.empty();
        const windowSize = 1;

        const createPageButton = (p) => `
            <li class="page-item ${p === page ? 'active' : ''}">
                <button type="button" class="page-link page-btn" data-page="${p}">${p}</button>
            </li>
        `;
        const addEllipsis = () => '<li class="page-item disabled"><span class="page-link">...</span></li>';

        if (page > windowSize + 1) {
            paginationLinks.append(createPageButton(1), addEllipsis());
        }

        const startPage = Math.max(1, page - windowSize);
        const endPage = Math.min(totalPages, page + windowSize);
        for (let i = startPage; i <= endPage; i++) {
            paginationLinks.append(createPageButton(i));
        }

        if (page < totalPages - windowSize) {
            paginationLinks.append(addEllipsis(), createPageButton(totalPages));
        }

        $('#OpeningBalance-prevPageBtn').prop('disabled', page <= 1);
        $('#OpeningBalance-nextPageBtn').prop('disabled', page >= totalPages || totalPages === 0);
    }

    function updateSortIcons() {
        $('#OpeningBalance-table th.sort').each(function () {
            const col = $(this).data('sort');
            const icon = $(this).find('.sort-icon');
            if (col === currentSortColumn) {
                icon.removeClass('fa-sort fa-sort-up fa-sort-down')
                    .addClass(currentSortOrder === 'asc' ? 'fa-sort-up text-primary' : 'fa-sort-down text-primary');
            } else {
                icon.removeClass('fa-sort-up fa-sort-down text-primary').addClass('fa-sort text-muted');
            }
        });
    }

    function openAddModal() {
        $('#OpeningBalance-isEdit').val('0');
        $('#openingBalanceModalLabel').html('<i class="fas fa-plus-circle me-2 text-primary"></i>Add Opening Balance');
        $('#OpeningBalance-modalAlertError').addClass('d-none').text('');

        // Reset inputs
        $('#modalObCode').val('');
        $('#modalAmount').val('');
        $('#modalReference').val('');
        $('#modalDescription').val('');

        // Defaults
        const activeFy = $('#fFinancialYear').val() || $('#modalFinancialYear option:eq(1)').val() || '2026-2027';
        const currentYear = activeFy.length >= 4 ? activeFy.substring(0, 4) : '2026';
        setDatePickerValue('modalOpeningDate', `${currentYear}-07-01`);

        setSelectValue('modalFinancialYear', activeFy || '');
        clearSelectValue('modalAccountHead');
        clearSelectValue('modalTrType');

        fetchNextCode(activeFy);
        const modal = getFormModal();
        if (modal) modal.show();
    }

    function openEditModal(code) {
        $('#OpeningBalance-isEdit').val('1');
        $('#openingBalanceModalLabel, #formTitle').html('<i class="fas fa-edit me-2 text-primary"></i>Edit Opening Balance');
        $('#OpeningBalance-saveBtn').html('<i class="fas fa-save me-1"></i>Update Opening Balance');
        $('#OpeningBalance-modalAlertError').addClass('d-none').text('');

        $.ajax({
            url: `/OpeningBalance/GetById?id=${encodeURIComponent(code)}`,
            type: 'GET',
            success: function (res) {
                if (res.success && res.data) {
                    const d = res.data;
                    $('#modalObCode').val(d.comOpeningBalanceCode || '');
                    $('#modalAmount').val(d.openingBalance != null ? d.openingBalance : '');
                    setDatePickerValue('modalOpeningDate', d.openingDateIso || d.openingDateFormatted || '');
                    $('#modalReference').val(d.reference || '');
                    $('#modalDescription').val(d.description || '');

                    if (d.main_CompanyCode) setSelectValue('modalCompany', String(d.main_CompanyCode));
                    if (d.branchCode) setSelectValue('modalBranch', String(d.branchCode));
                    if (d.financialCodeNo) setSelectValue('modalFinancialYear', d.financialCodeNo);
                    if (d.generalLedgerCodeNo) setSelectValue('modalAccountHead', d.generalLedgerCodeNo);
                    if (d.trType) setSelectValue('modalTrType', d.trType);

                    const modal = getFormModal();
                    if (modal && $('#openingBalanceModal').length) {
                        modal.show();
                    } else if ($('#OpeningBalance-form').length) {
                        $('html, body').animate({ scrollTop: $('#OpeningBalance-form').offset().top - 80 }, 200);
                    }
                } else {
                    toastr.warning(res.message || 'Unable to load opening balance data.');
                }
            },
            error: function () {
                toastr.error('Error retrieving opening balance details.');
            }
        });
    }

    function resetForm() {
        $('#OpeningBalance-isEdit').val('0');
        $('#openingBalanceModalLabel, #formTitle').html('<i class="fas fa-plus-circle me-2 text-primary"></i>Opening Balance');
        $('#OpeningBalance-saveBtn').html('<i class="fas fa-save me-1"></i>Save');
        $('#OpeningBalance-modalAlertError').addClass('d-none').text('');

        $('#modalAmount').val('');
        $('#modalReference').val('');
        $('#modalDescription').val('');

        const activeFy = $('#fFinancialYear').val() || $('#modalFinancialYear option:eq(1)').val() || '2026-2027';
        const currentYear = activeFy.length >= 4 ? activeFy.substring(0, 4) : '2026';
        setDatePickerValue('modalOpeningDate', `${currentYear}-07-01`);

        setSelectValue('modalFinancialYear', activeFy || '');
        clearSelectValue('modalAccountHead');
        clearSelectValue('modalTrType');

        fetchNextCode(activeFy);
    }

    function fetchNextCode(financialYear) {
        if (!financialYear) return;
        $.ajax({
            url: `/OpeningBalance/GetNextCode?financialYear=${encodeURIComponent(financialYear)}`,
            type: 'GET',
            success: function (res) {
                if (res.success && res.code) {
                    $('#modalObCode').val(res.code);
                }
            }
        });
    }

    function saveOpeningBalance() {
        const isEdit = $('#OpeningBalance-isEdit').val() === '1';

        const companyVal = window.choiceManager ? (choiceManager.getChoiceValue('modalCompany') || $('#modalCompany').val()) : $('#modalCompany').val();
        const branchVal = window.choiceManager ? (choiceManager.getChoiceValue('modalBranch') || $('#modalBranch').val()) : $('#modalBranch').val();
        const fyVal = window.choiceManager ? (choiceManager.getChoiceValue('modalFinancialYear') || $('#modalFinancialYear').val()) : $('#modalFinancialYear').val();
        const headVal = window.choiceManager ? (choiceManager.getChoiceValue('modalAccountHead') || $('#modalAccountHead').val()) : $('#modalAccountHead').val();
        const trTypeVal = window.choiceManager ? (choiceManager.getChoiceValue('modalTrType') || $('#modalTrType').val()) : $('#modalTrType').val();

        const model = {
            ComOpeningBalanceCode: $('#modalObCode').val().trim(),
            Main_CompanyCode: parseInt(companyVal, 10) || null,
            BranchCode: parseInt(branchVal, 10) || null,
            FinancialCodeNo: (fyVal || '').trim(),
            GeneralLedgerCodeNo: (headVal || '').trim(),
            OpeningBalance: parseFloat($('#modalAmount').val()),
            TrType: (trTypeVal || '').trim(),
            OpeningDate: $('#modalOpeningDate').val() || null,
            Reference: $('#modalReference').val().trim(),
            Description: $('#modalDescription').val().trim()
        };

        if (!model.FinancialCodeNo) {
            showModalError('Please select a Financial Year.');
            return;
        }
        if (!model.GeneralLedgerCodeNo) {
            showModalError('Please select an Account Head.');
            return;
        }
        if (isNaN(model.OpeningBalance) || model.OpeningBalance < 0) {
            showModalError('Please enter a valid Opening Balance amount.');
            return;
        }
        if (!model.TrType) {
            showModalError('Please select Transaction Type (Dr / Cr).');
            return;
        }
        if (!model.OpeningDate) {
            showModalError('Please select the Opening Balance Date.');
            return;
        }

        const $btn = $('#OpeningBalance-saveBtn');
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Saving...');

        $.ajax({
            url: '/OpeningBalance/Save',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                $btn.prop('disabled', false).html('<i class="fas fa-save me-1"></i>Save');
                if (res.success) {
                    toastr.success(res.message || (isEdit ? 'Opening Balance updated successfully.' : 'Opening Balance created successfully.'));
                    const modal = getFormModal();
                    if (modal && $('#openingBalanceModal').length) {
                        modal.hide();
                    } else {
                        resetForm();
                    }
                    loadTableData();
                } else {
                    showModalError(res.message || 'Failed to save opening balance.');
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('<i class="fas fa-save me-1"></i>Save');
                showModalError('An unexpected server error occurred.');
            }
        });
    }

    function showDeleteConfirm(codeOrCodes) {
        pendingDeleteCode = codeOrCodes;
        const isMultiple = Array.isArray(codeOrCodes);
        const desc = isMultiple
            ? `<strong>${codeOrCodes.length}</strong> selected opening balance record(s)`
            : `opening balance <strong>${escapeHtml(codeOrCodes)}</strong>`;

        $('#obConfirmMessage').html(`Are you sure you want to delete ${desc}?<br><small class="text-danger mt-1 d-block">This action cannot be undone.</small>`);
        const confirmModal = getConfirmModal();
        if (confirmModal) confirmModal.show();
    }

    function executeDelete(codes) {
        const confirmModal = getConfirmModal();
        if (confirmModal) confirmModal.hide();

        const codeList = Array.isArray(codes) ? codes : [codes];
        if (codeList.length === 0) return;

        let completed = 0;
        let errors = 0;

        codeList.forEach(function (c) {
            $.ajax({
                url: `/OpeningBalance/Delete?id=${encodeURIComponent(c)}`,
                type: 'POST',
                success: function (res) {
                    if (!res.success) errors++;
                },
                error: function () {
                    errors++;
                },
                complete: function () {
                    completed++;
                    if (completed === codeList.length) {
                        if (errors === 0) {
                            toastr.success(codeList.length > 1 ? `${codeList.length} records deleted.` : 'Opening balance record deleted.');
                        } else {
                            toastr.warning(`Deleted ${completed - errors} record(s). ${errors} failed.`);
                        }
                        loadTableData();
                    }
                }
            });
        });
    }

    function uploadCsv() {
        const fileInput = document.getElementById('fileCsv');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            toastr.warning('Please select an Excel file (.xlsx or .xls) to upload.');
            return;
        }

        const file = fileInput.files[0];
        if (!file.name.match(/\.(xlsx|xls|csv)$/i)) {
            toastr.warning('Please select a valid Excel file (.xlsx or .xls).');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        const $btn = $('#btnUploadCsv');
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Saving...');

        $.ajax({
            url: '/OpeningBalance/UploadExcel',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                $btn.prop('disabled', false).html('<i class="fas fa-save me-1"></i>File save');
                fileInput.value = '';
                if (res.success) {
                    toastr.success(res.message || 'Excel file imported successfully.');
                    loadTableData();
                } else {
                    toastr.error(res.message || 'Excel upload failed.');
                }
            },
            error: function () {
                $btn.prop('disabled', false).html('<i class="fas fa-save me-1"></i>File save');
                toastr.error('An error occurred during file upload.');
            }
        });
    }

    function exportToExcel() {
        const fyVal = window.choiceManager ? (choiceManager.getChoiceValue('fFinancialYear') || $('#fFinancialYear').val()) : $('#fFinancialYear').val();
        const headVal = window.choiceManager ? (choiceManager.getChoiceValue('fAccountHead') || $('#fAccountHead').val()) : $('#fAccountHead').val();

        const filter = {
            SearchTerm: $('#OpeningBalance-searchInput').val()?.trim() || '',
            FinancialCodeNo: (fyVal || '').trim() || null,
            GeneralLedgerCodeNo: (headVal || '').trim() || null,
            FromDate: $('#fFrom').val() || null,
            ToDate: $('#fTo').val() || null,
            SortColumn: currentSortColumn,
            SortOrder: currentSortOrder
        };

        toastr.info('Generating Excel (.xlsx) file...');

        fetch('/OpeningBalance/ExportExcel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(filter)
        })
        .then(response => {
            if (!response.ok) throw new Error('Export failed');
            return response.blob();
        })
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Opening_Balance_${new Date().toISOString().slice(0, 10)}.xlsx`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
            toastr.success('Excel exported successfully.');
        })
        .catch(err => {
            toastr.error('Failed to export Excel.');
        });
    }

    function showModalError(msg) {
        $('#OpeningBalance-modalAlertError').removeClass('d-none').html(msg);
    }

    function formatCurrency(val) {
        const num = parseFloat(val) || 0;
        return '৳ ' + num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function escapeCsv(str) {
        if (!str) return '""';
        return '"' + String(str).replace(/"/g, '""') + '"';
    }

    function escapeCsvText(str) {
        if (!str) return '""';
        return '="' + String(str).replace(/"/g, '""') + '"';
    }

})(jQuery);
