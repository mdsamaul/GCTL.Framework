(function ($) {
    'use strict';

    let selectedCodes = [];
    let pendingDeleteTarget = null; // single code string or array of codes
    let isDeletingBulk = false;

    // ==========================================
    // 1. GLOBAL RENDERERS FOR _CustomGrid
    // ==========================================
    window.gctlRenderers = window.gctlRenderers || {};

    window.gctlRenderers.renderTrTypeBadge = function (data, type, row) {
        if (!data) return '-';
        const tr = String(data).trim();
        const cls = tr.toLowerCase() === 'dr' ? 'ob-badge-dr' : 'ob-badge-cr';
        return `<span class="${cls}">${escapeHtml(tr)}</span>`;
    };

    window.gctlRenderers.renderFyBadge = function (data, type, row) {
        if (!data || data === '-') return '-';
        return `<span class="ob-badge-fy">${escapeHtml(data)}</span>`;
    };

    window.gctlRenderers.formatOpeningBalanceCurrency = function (data, type, row) {
        if (data === null || data === undefined || data === '') return '৳ 0.00';
        const num = parseFloat(data);
        if (isNaN(num)) return '৳ 0.00';
        return '৳ ' + num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    window.gctlRenderers.renderAccountHead = function (data, type, row) {
        const text = data || row?.accountHeadDisplay || row?.generalLedgerName || row?.generalLedgerCodeNo || '-';
        return `<span class="fw-semibold text-body-emphasis">${escapeHtml(text)}</span>`;
    };

    window.gctlRenderers.renderNullDash = function (data, type, row) {
        return (data && String(data).trim() !== '' && data !== 'null') ? escapeHtml(String(data)) : '-';
    };

    // ==========================================
    // 2. HELPER FUNCTIONS
    // ==========================================
    function parseDateToJsDate(val) {
        if (!val) return null;
        if (val instanceof Date) return val;
        const str = String(val).trim();
        // Match dd/MM/yyyy or dd-MM-yyyy or dd.MM.yyyy
        const dmyMatch = str.match(/^(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{4})$/);
        if (dmyMatch) {
            const day = parseInt(dmyMatch[1], 10);
            const month = parseInt(dmyMatch[2], 10) - 1; // 0-based
            const year = parseInt(dmyMatch[3], 10);
            return new Date(year, month, day);
        }
        // Match yyyy-MM-dd
        const ymdMatch = str.match(/^(\d{4})[\/\-\.](\d{1,2})[\/\-\.](\d{1,2})$/);
        if (ymdMatch) {
            const year = parseInt(ymdMatch[1], 10);
            const month = parseInt(ymdMatch[2], 10) - 1;
            const day = parseInt(ymdMatch[3], 10);
            return new Date(year, month, day);
        }
        const parsed = new Date(str);
        return isNaN(parsed.getTime()) ? null : parsed;
    }

    function formatDateToDMY(date) {
        if (!date) return '';
        const d = date instanceof Date ? date : parseDateToJsDate(date);
        if (!d) return '';
        const dd = String(d.getDate()).padStart(2, '0');
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const yyyy = d.getFullYear();
        return `${dd}/${mm}/${yyyy}`;
    }

    function setDatePickerValue(id, val) {
        const rawId = id.replace('#', '');
        const el = document.getElementById(rawId);
        if (el && el._flatpickr) {
            if (val) {
                const dateObj = parseDateToJsDate(val);
                if (dateObj) {
                    el._flatpickr.setDate(dateObj, false);
                } else {
                    el._flatpickr.setDate(val, false);
                }
            } else {
                el._flatpickr.clear();
            }
        } else {
            $(`#${rawId}`).val(val ? formatDateToDMY(val) : '');
        }
    }

    function setSelectValue(id, val, text) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        if ((!text || text.includes('\n')) && val) {
            const $match = $el.find(`option[value="${val}"]`).first();
            if ($match.length) {
                text = $match.text().trim();
            }
        }
        if (typeof window.setCustomDropdownValue === 'function') {
            window.setCustomDropdownValue(rawId, val, text);
        }
        if (window.choiceManager && typeof window.choiceManager.setChoiceValue === 'function') {
            window.choiceManager.setChoiceValue(rawId, val || '');
        }
        $el.val(val || '');
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

    function getConfirmModal() {
        const el = document.getElementById('obConfirmModal');
        return el && typeof bootstrap !== 'undefined' ? bootstrap.Modal.getOrCreateInstance(el) : null;
    }

    function updateSummaryCards(summary) {
        if (!summary) return;
        const dr = parseFloat(summary.totalDebit || summary.TotalDebit) || 0;
        const cr = parseFloat(summary.totalCredit || summary.TotalCredit) || 0;
        const diff = dr - cr;

        $('#lblTotalDebit').text('৳ ' + dr.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        $('#lblTotalCredit').text('৳ ' + cr.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        $('#lblDifference').text('৳ ' + Math.abs(diff).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));

        const $card = $('#cardDifference');
        const $badge = $('#badgeDiffStatus');

        if (Math.abs(diff) < 0.01) {
            $card.css('--stat-color', '#16a34a');
            $('#lblDifference').removeClass('text-danger').addClass('text-success');
            $badge.removeClass('badge-subtle-danger').addClass('badge-subtle-success')
                .html('<i class="fas fa-check-circle me-1"></i>Balanced');
        } else {
            $card.css('--stat-color', '#dc2626');
            $('#lblDifference').removeClass('text-success').addClass('text-danger');
            $badge.removeClass('badge-subtle-success').addClass('badge-subtle-danger')
                .html('<i class="fas fa-exclamation-circle me-1"></i>Unbalanced');
        }
    }

    // ==========================================
    // 3. GRID INITIALIZATION & RELOAD
    // ==========================================
    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            $.gctlGrid.initFromModel({
                gridId: 'OpeningBalance-table',
                ajaxData: function (d) {
                    d.financialCodeNo = $('#fFinancialYear').val() || '';
                    d.generalLedgerCodeNo = $('#fAccountHead').val() || '';

                    const fromEl = document.getElementById('fFrom');
                    const toEl = document.getElementById('fTo');

                    if (fromEl && fromEl._flatpickr && fromEl._flatpickr.selectedDates.length > 0) {
                        d.fromDate = formatDateToDMY(fromEl._flatpickr.selectedDates[0]);
                    } else {
                        d.fromDate = $('#fFrom').val() || '';
                    }

                    if (toEl && toEl._flatpickr && toEl._flatpickr.selectedDates.length > 0) {
                        d.toDate = formatDateToDMY(toEl._flatpickr.selectedDates[0]);
                    } else {
                        d.toDate = $('#fTo').val() || '';
                    }
                },
                onLoaded: function (json) {
                    if (json && json.summary) {
                        updateSummaryCards(json.summary);
                    }
                    syncSelectedCheckboxes();
                }
            });
        }
    }

    function reloadGrid(resetPaging) {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('OpeningBalance-table', !!resetPaging);
        } else {
            const table = $('#OpeningBalance-table').DataTable();
            if (table) {
                table.ajax.reload(null, !resetPaging);
            }
        }
    }

    // ==========================================
    // 4. DATE FILTER LOGIC
    // ==========================================
    function initializeDateFiltersOnLoad() {
        const fyVal = $('#fFinancialYear').val();
        if (fyVal) {
            const $opt = $('#fFinancialYear').find(`option[value="${fyVal}"]`);
            if ($opt.length) {
                const startDate = $opt.attr('data-start') || $opt.data('start');
                const endDate = $opt.attr('data-end') || $opt.data('end');
                if (startDate) setDatePickerValue('fFrom', startDate);
                if (endDate) setDatePickerValue('fTo', endDate);
            }
        }
    }

    // ==========================================
    // 5. DOCUMENT READY & EVENT ATTACHMENTS
    // ==========================================
    $(document).ready(function () {
        // Initialize datepickers
        if (typeof initializeDatepickerDMY === 'function') {
            initializeDatepickerDMY("fFrom, fTo, modalOpeningDate");
        }

        // Set initial date filter values if a financial year is preselected
        initializeDateFiltersOnLoad();

        // Initialize DataTable
        initGrid();

        // Initialize Form
        resetForm();

        // ------------------------------------------
        // Filter Events
        // ------------------------------------------
        // When Financial Year changes: From = start date of FY, To = end date of FY
        $(document).on('change', '#fFinancialYear', function () {
            const fyVal = $(this).val();
            let startDate = '';
            let endDate = '';

            if (fyVal) {
                const $opt = $(this).find(`option[value="${fyVal}"]`);
                if ($opt.length) {
                    startDate = $opt.attr('data-start') || $opt.data('start') || '';
                    endDate = $opt.attr('data-end') || $opt.data('end') || '';
                }
            }

            if (fyVal && (startDate || endDate)) {
                if (startDate) setDatePickerValue('fFrom', startDate);
                if (endDate) setDatePickerValue('fTo', endDate);
            } else {
                setDatePickerValue('fFrom', '');
                setDatePickerValue('fTo', '');
            }

            reloadGrid(true);
        });

        // When dates or account head changes: Reload grid
        $(document).on('change', '#fFrom, #fTo, #fAccountHead', function () {
            reloadGrid(true);
        });

        // Reset Filters Button: Clear all filters and reload grid
        $('#btnResetFilters').on('click', function (e) {
            e.preventDefault();
            clearSelectValue('fAccountHead');
            clearSelectValue('fFinancialYear');
            setDatePickerValue('fFrom', '');
            setDatePickerValue('fTo', '');
            reloadGrid(true);
        });

        // ------------------------------------------
        // Toolbar and Form Action Buttons
        // ------------------------------------------
        // Save
        $(document).on('click', '#btnSave, .js-ob-save, #OpeningBalance-saveBtn', function (e) {
            e.preventDefault();
            saveOpeningBalance();
        });

        // Clear
        $(document).on('click', '#btnClear, .js-ob-clear, #OpeningBalance-resetBtn', function (e) {
            e.preventDefault();
            resetForm();
        });

        // Delete from Toolbar
        $(document).on('click', '#btnDelete, .js-ob-delete-confirm', function (e) {
            e.preventDefault();
            openDeleteConfirmation();
        });

        // Bulk Delete Button in Filter Bar
        $(document).on('click', '#OpeningBalance-delSel', function (e) {
            e.preventDefault();
            openDeleteConfirmation();
        });

        // Form Submit
        $('#OpeningBalance-form').on('submit', function (e) {
            e.preventDefault();
            saveOpeningBalance();
        });

        // ------------------------------------------
        // Row Actions (Edit & Delete)
        // ------------------------------------------
        // Edit click on OB Code or Action pencil
        $(document).on('click', '.js-ob-edit, .js-grid-edit', function (e) {
            e.preventDefault();
            const code = $(this).data('id') || $(this).text().trim();
            if (code) {
                loadOpeningBalanceDetails(code);
            }
        });

        // Delete click on row trash icon
        $(document).on('click', '.js-grid-delete', function (e) {
            e.preventDefault();
            const code = $(this).data('id');
            if (code) {
                confirmDeleteSingle(code);
            }
        });

        // Confirm Delete Modal submit
        $('#obConfirmSubmitBtn').on('click', function (e) {
            e.preventDefault();
            executePendingDelete();
        });

        // ------------------------------------------
        // Checkboxes & Bulk Actions
        // ------------------------------------------
        $(document).on('change', '.js-ob-check', function () {
            syncSelectedCheckboxes();
        });

        $(document).on('change', '#selectAllOpeningBalance', function () {
            $('.js-ob-check').prop('checked', $(this).prop('checked'));
            syncSelectedCheckboxes();
        });

        // ------------------------------------------
        // Form Change Listeners
        // ------------------------------------------
        // When modal Financial Year changes, generate next OB Code
        $(document).on('change', '#modalFinancialYear', function () {
            const fy = $(this).val();
            const isEdit = $('#OpeningBalance-isEdit').val() === '1';
            if (fy && !isEdit) {
                fetchNextCode(fy);
            }
        });

        // When Account Head changes, automatically select default Dr / Cr
        $(document).on('change', '#modalAccountHead', function () {
            const selectedOpt = $(this).find('option:selected');
            const defType = selectedOpt.data('deftype');
            const currentTr = $('#modalTrType').val();
            if (defType && !currentTr) {
                setSelectValue('modalTrType', defType);
            }
        });

        // ------------------------------------------
        // Excel CSV Upload & Exports
        // ------------------------------------------
        $('#OpeningBalance-formUploadCsv').on('submit', function (e) {
            e.preventDefault();
            uploadExcel();
        });

        // Print
        $(document).on('click', '#btnPrint, #btnExportPrint', function (e) {
            e.preventDefault();
            window.print();
        });

        // Export PDF
        $(document).on('click', '#btnPrintPdfBtn, #btnExportPdf', function (e) {
            e.preventDefault();
            window.print();
        });

        // Export Excel
        $(document).on('click', '#btnPrintExcelBtn, #btnExportCSV', function (e) {
            e.preventDefault();
            exportToExcel();
        });
    });

    // ==========================================
    // 6. FORM LOGIC & DETAILS
    // ==========================================
    function resetForm() {
        $('#OpeningBalance-isEdit').val('0');
        $('#OpeningBalance-autoId').val('');
        $('#OpeningBalance-modalAlertError').addClass('d-none').html('');
        $('#modalObCode').val('');
        $('#modalAmount').val('');
        $('#modalReference').val('');
        $('#modalDescription').val('');

        const $modalOpt = $('#modalFinancialYear').find('option[value!=""]').first();
        const activeFyCode = $modalOpt.val() || '';
        const activeFyText = $modalOpt.text().trim() || '';
        setSelectValue('modalFinancialYear', activeFyCode, activeFyText);

        setDatePickerValue('modalOpeningDate', new Date());

        clearSelectValue('modalAccountHead');
        clearSelectValue('modalTrType');

        if (activeFyCode) {
            fetchNextCode(activeFyCode);
        }
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

    function loadOpeningBalanceDetails(code) {
        if (!code) return;
        $.ajax({
            url: `/OpeningBalance/GetById?id=${encodeURIComponent(code)}`,
            type: 'GET',
            success: function (res) {
                if (!res) {
                    toastr.error('Record not found.');
                    return;
                }

                $('#OpeningBalance-isEdit').val('1');
                $('#OpeningBalance-autoId').val(res.autoId || '');
                $('#modalObCode').val(res.comOpeningBalanceCode || code);

                if (res.main_CompanyCode) setSelectValue('modalCompany', res.main_CompanyCode);
                if (res.branchCode) setSelectValue('modalBranch', res.branchCode);
                if (res.financialCodeNo) setSelectValue('modalFinancialYear', res.financialCodeNo);

                const dateVal = res.openingDateFormatted || res.openingDateIso || (res.openingDate ? res.openingDate.slice(0, 10) : '');
                setDatePickerValue('modalOpeningDate', dateVal);

                if (res.generalLedgerCodeNo) setSelectValue('modalAccountHead', res.generalLedgerCodeNo);
                $('#modalAmount').val(res.openingBalance !== null ? res.openingBalance : '');
                if (res.trType) setSelectValue('modalTrType', res.trType);

                $('#modalReference').val(res.reference === '-' ? '' : (res.reference || ''));
                $('#modalDescription').val(res.description === '-' ? '' : (res.description || ''));

                $('html, body').animate({ scrollTop: $('#OpeningBalance-form').offset().top - 80 }, 300);
            },
            error: function () {
                toastr.error('Failed to load opening balance details.');
            }
        });
    }

    function saveOpeningBalance() {
        const isEdit = $('#OpeningBalance-isEdit').val() === '1';

        const model = {
            AutoId: $('#OpeningBalance-autoId').val() ? parseFloat($('#OpeningBalance-autoId').val()) : null,
            ComOpeningBalanceCode: ($('#modalObCode').val() || '').trim(),
            Main_CompanyCode: $('#modalCompany').val() ? parseInt($('#modalCompany').val(), 10) : null,
            BranchCode: $('#modalBranch').val() ? parseInt($('#modalBranch').val(), 10) : null,
            FinancialCodeNo: ($('#modalFinancialYear').val() || '').trim(),
            GeneralLedgerCodeNo: ($('#modalAccountHead').val() || '').trim(),
            OpeningBalance: parseFloat($('#modalAmount').val()),
            TrType: ($('#modalTrType').val() || '').trim(),
            OpeningDateRaw: ($('#modalOpeningDate').val() || '').trim(),
            Reference: ($('#modalReference').val() || '').trim(),
            Description: ($('#modalDescription').val() || '').trim()
        };

        if (!model.FinancialCodeNo) {
            toastr.warning('Please select a Financial Year.');
            $('#modalFinancialYear').focus();
            return;
        }
        if (!model.OpeningDateRaw) {
            toastr.warning('Please enter the Opening Date.');
            $('#modalOpeningDate').focus();
            return;
        }
        if (!model.GeneralLedgerCodeNo) {
            toastr.warning('Please select an Account Head.');
            $('#modalAccountHead').focus();
            return;
        }
        if (isNaN(model.OpeningBalance) || model.OpeningBalance < 0) {
            toastr.warning('Please enter a valid Opening Balance amount.');
            $('#modalAmount').focus();
            return;
        }
        if (!model.TrType) {
            toastr.warning('Please select Transaction Type (Dr / Cr).');
            $('#modalTrType').focus();
            return;
        }

        const $btn = $('.js-ob-save, #OpeningBalance-saveBtn, #btnSave');
        $btn.prop('disabled', true);

        $.ajax({
            url: '/OpeningBalance/Save',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                $btn.prop('disabled', false);
                if (res.success) {
                    toastr.success(res.message || (isEdit ? 'Opening Balance updated successfully.' : 'Opening Balance created successfully.'));
                    resetForm();
                    reloadGrid(false);
                } else {
                    toastr.error(res.message || 'Failed to save opening balance.');
                }
            },
            error: function (xhr) {
                $btn.prop('disabled', false);
                const msg = xhr.responseJSON?.message || 'An unexpected server error occurred.';
                toastr.error(msg);
            }
        });
    }

    // ==========================================
    // 7. DELETION LOGIC
    // ==========================================
    function syncSelectedCheckboxes() {
        selectedCodes = [];
        $('.js-ob-check:checked').each(function () {
            const val = $(this).val();
            if (val) selectedCodes.push(val);
        });

        if (selectedCodes.length > 0) {
            $('#OpeningBalance-delSel').removeClass('d-none');
        } else {
            $('#OpeningBalance-delSel').addClass('d-none');
        }
    }

    function resetBulkSelection() {
        $('.js-ob-check').prop('checked', false);
        $('#selectAllOpeningBalance').prop('checked', false);
        selectedCodes = [];
        $('#OpeningBalance-delSel').addClass('d-none');
    }

    function openDeleteConfirmation() {
        if (!selectedCodes.length) {
            const currentCode = ($('#modalObCode').val() || '').trim();
            const isEdit = $('#OpeningBalance-isEdit').val() === '1';
            if (isEdit && currentCode) {
                selectedCodes = [currentCode];
            }
        }

        if (!selectedCodes.length) {
            toastr.warning('Please select at least one record to delete.');
            return;
        }

        pendingDeleteTarget = selectedCodes;
        isDeletingBulk = true;

        $('#obConfirmTitle').text('Confirm Delete');
        $('#obConfirmMessage').html(`Are you sure you want to delete <strong>${selectedCodes.length}</strong> selected opening balance record(s)?<br/><small class="text-danger mt-1 d-block">This action cannot be undone.</small>`);

        const modal = getConfirmModal();
        if (modal) modal.show();
    }

    function confirmDeleteSingle(code) {
        if (!code) return;
        pendingDeleteTarget = code;
        isDeletingBulk = false;

        $('#obConfirmTitle').text('Confirm Delete');
        $('#obConfirmMessage').html(`Are you sure you want to delete Opening Balance <strong>${escapeHtml(code)}</strong>?<br/><small class="text-danger mt-1 d-block">This action cannot be undone.</small>`);

        const modal = getConfirmModal();
        if (modal) modal.show();
    }

    function executePendingDelete() {
        const modal = getConfirmModal();
        if (modal) modal.hide();

        if (!pendingDeleteTarget) return;

        if (isDeletingBulk || Array.isArray(pendingDeleteTarget)) {
            const ids = Array.isArray(pendingDeleteTarget) ? pendingDeleteTarget : [pendingDeleteTarget];
            $.ajax({
                url: '/OpeningBalance/BulkDelete',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(ids),
                success: function (res) {
                    if (res.success) {
                        toastr.success(res.message || 'Records deleted successfully.');
                        resetForm();
                        resetBulkSelection();
                        reloadGrid(false);
                    } else {
                        toastr.error(res.message || 'Failed to delete records.');
                    }
                },
                error: function () {
                    toastr.error('Error occurred while deleting records.');
                }
            });
        } else {
            $.ajax({
                url: `/OpeningBalance/Delete?id=${encodeURIComponent(pendingDeleteTarget)}`,
                type: 'POST',
                success: function (res) {
                    if (res.success) {
                        toastr.success(res.message || 'Record deleted successfully.');
                        resetForm();
                        resetBulkSelection();
                        reloadGrid(false);
                    } else {
                        toastr.error(res.message || 'Failed to delete record.');
                    }
                },
                error: function () {
                    toastr.error('Error occurred while deleting record.');
                }
            });
        }
    }

    // ==========================================
    // 8. EXCEL UPLOAD & EXPORT
    // ==========================================
    function uploadExcel() {
        const fileInput = document.getElementById('fileCsv');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            toastr.warning('Please select an Excel file (.xlsx or .xls) to upload.');
            return;
        }

        const file = fileInput.files[0];
        if (!file.name.match(/\.(xlsx|xls|csv)$/i)) {
            toastr.warning('Please select a valid Excel file (.xlsx, .xls, or .csv).');
            return;
        }

        const formData = new FormData();
        formData.append('file', file);

        const $btn = $('#btnUploadCsv');
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Uploading...');

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
                    toastr.success(res.message || 'File imported successfully.');
                    reloadGrid(false);
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
        const filter = {
            FinancialCodeNo: $('#fFinancialYear').val() || null,
            GeneralLedgerCodeNo: $('#fAccountHead').val() || null,
            FromDate: $('#fFrom').val() || null,
            ToDate: $('#fTo').val() || null
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
        .catch(() => {
            toastr.error('Failed to export Excel.');
        });
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

})(jQuery);
