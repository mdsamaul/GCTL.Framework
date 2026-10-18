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

    /* ===== Flatpickr Footer Helper (Today & Clear Buttons) matching VoucherEntry ===== */
    function addCalendarTodayClearButtons(fpInstance) {
        if (!fpInstance || !fpInstance.calendarContainer) return;
        if (fpInstance.calendarContainer.querySelector('.custom-fp-footer')) return;

        const footer = document.createElement('div');
        footer.className = 'custom-fp-footer';

        const todayBtn = document.createElement('button');
        todayBtn.type = 'button';
        todayBtn.className = 'custom-fp-today-btn';
        todayBtn.textContent = 'Today';
        todayBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            fpInstance.setDate(new Date(), true);
            $(fpInstance.input).trigger('change');
            if (fpInstance.altInput) $(fpInstance.altInput).trigger('change');
            fpInstance.close();
        });

        const clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.className = 'custom-fp-clear-btn';
        clearBtn.textContent = 'Clear';
        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            fpInstance.clear();
            $(fpInstance.input).val('').trigger('change');
            if (fpInstance.altInput) $(fpInstance.altInput).val('').trigger('change');
            fpInstance.close();
        });

        footer.appendChild(todayBtn);
        footer.appendChild(clearBtn);
        fpInstance.calendarContainer.appendChild(footer);
    }

    function initDatePickers() {
        ['fFrom', 'fTo', 'modalOpeningDate'].forEach(function (id) {
            const el = document.getElementById(id);
            if (!el || typeof flatpickr === 'undefined') return;
            const existingFp = el._flatpickr || $(el).data('customFpInstance');
            if (existingFp && typeof existingFp.destroy === 'function') {
                existingFp.destroy();
            }

            const fp = flatpickr(el, {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                allowInput: true,
                defaultDate: id === 'modalOpeningDate' ? new Date() : null,
                onReady: function (selectedDates, dateStr, instance) {
                    instance.input.placeholder = "dd/mm/yyyy";
                    addCalendarTodayClearButtons(instance);
                },
                onOpen: function (selectedDates, dateStr, instance) {
                    addCalendarTodayClearButtons(instance);
                }
            });
            $(el).data('customFpInstance', fp);
        });

        function triggerFilterReload() {
            reloadGrid(true);
        }

        ['fFrom', 'fTo'].forEach(function (id) {
            const el = document.getElementById(id);
            if (el && el._flatpickr) {
                el._flatpickr.config.onChange.push(triggerFilterReload);
                if (el._flatpickr.altInput) {
                    $(el._flatpickr.altInput).on('change', triggerFilterReload);
                    $(el._flatpickr.altInput).on('keydown', function (e) {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            triggerFilterReload();
                        }
                    });
                }
            }
            $(`#${id}`).on('change', triggerFilterReload);
        });
    }

    function setDatePickerValue(id, val, triggerChange = false) {
        const rawId = id.replace('#', '');
        const el = document.getElementById(rawId);
        if (!el) return;
        const fp = el._flatpickr || $(el).data('customFpInstance') || (el.nextElementSibling ? el.nextElementSibling._flatpickr : null);
        if (!val) {
            if (fp) {
                fp.clear();
                $(el).val('');
                if (fp.altInput) $(fp.altInput).val('');
            } else {
                $(`#${rawId}`).val('');
            }
            return;
        }

        const dateObj = parseDateToJsDate(val);
        if (fp) {
            if (dateObj && !isNaN(dateObj.getTime())) {
                fp.setDate(dateObj, triggerChange);
            } else {
                fp.setDate(val, triggerChange);
            }
        } else {
            if (dateObj && !isNaN(dateObj.getTime())) {
                $(`#${rawId}`).val(formatDateToDMY(dateObj));
            } else {
                $(`#${rawId}`).val(val || '');
            }
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

    function updateHeaderFyBadge() {
        let fyText = $('#fFinancialYear option:selected').text().trim();
        if (!fyText || fyText === 'All Years' || fyText.toLowerCase().includes('select')) {
            const val = $('#fFinancialYear').val();
            if (val) fyText = val;
            else fyText = '';
        }

        // 1. Breadcrumb title badge (.page-breadcrumb-title)
        const $breadcrumbTitle = $('.page-breadcrumb-title');
        if ($breadcrumbTitle.length) {
            let $badge = $('#pageHeaderFy');
            if (!$badge.length) {
                $breadcrumbTitle.append(' <span id="pageHeaderFy" class="badge bg-primary-subtle text-primary border border-primary-subtle fw-semibold ms-2" style="font-size: 0.825rem; vertical-align: middle; padding: 0.25rem 0.55rem; letter-spacing: normal;"></span>');
                $badge = $('#pageHeaderFy');
            }
            if (fyText) {
                $badge.text(fyText).removeClass('d-none');
            } else {
                $badge.addClass('d-none');
            }
        }

        // 2. Also update page-level selectedFyDisplay element if present
        const $pageBadge = $('#selectedFyDisplay');
        if ($pageBadge.length) {
            if (fyText) {
                $pageBadge.text(fyText).removeClass('d-none');
            } else {
                $pageBadge.addClass('d-none');
            }
        }
    }

    // ==========================================
    // 4. DATE FILTER LOGIC
    // ==========================================
    function initializeDateFiltersOnLoad() {
        let $opt = $('#fFinancialYear').find('option[data-current="1"]');
        if (!$opt.length || !$opt.val()) {
            $opt = $('#fFinancialYear').find('option:selected');
        }
        if (!$opt.length || !$opt.val()) {
            $opt = $('#fFinancialYear').find('option[value!=""]').last();
        }

        if ($opt && $opt.length && $opt.val()) {
            const val = $opt.val();
            const text = $opt.text().trim();
            setSelectValue('fFinancialYear', val, text);

            const startDate = $opt.attr('data-start') || $opt.data('start');
            const endDate = $opt.attr('data-end') || $opt.data('end');
            if (startDate) setDatePickerValue('fFrom', startDate);
            if (endDate) setDatePickerValue('fTo', endDate);
        }

        updateHeaderFyBadge();
    }

    // ==========================================
    // 5. DOCUMENT READY & EVENT ATTACHMENTS
    // ==========================================
    $(document).ready(function () {
        // Initialize datepickers matching VoucherEntry
        initDatePickers();

        // Set initial date filter values if a financial year is preselected
        initializeDateFiltersOnLoad();
        updateHeaderFyBadge();

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

            updateHeaderFyBadge();
            reloadGrid(true);
        });

        // When dates or account head changes: Reload grid
        $(document).on('change', '#fFrom, #fTo, #fAccountHead', function () {
            reloadGrid(true);
        });

        // Reset Filters Button: Restore default filters (Current Financial Year) and reload grid
        $('#btnResetFilters').on('click', function (e) {
            e.preventDefault();
            clearSelectValue('fAccountHead');
            initializeDateFiltersOnLoad();
            updateHeaderFyBadge();
            reloadGrid(true);
        });


        // Ensure Account Head dropdown panel never overflows screen horizontally
        $(document).on('click focus', '#modalAccountHead + .custom-dd-wrapper, #fAccountHead + .custom-dd-wrapper', function () {
            const $wrapper = $(this);
            const $panel = $wrapper.find('.custom-dd-panel');
            if ($panel.length) {
                setTimeout(function () {
                    if (!$wrapper.hasClass('open')) return;
                    $panel.css({ 'left': '0', 'right': 'auto' });
                    const rect = $panel[0].getBoundingClientRect();
                    const winWidth = window.innerWidth || document.documentElement.clientWidth;
                    if (rect.right > winWidth - 15) {
                        $panel.css({ 'left': 'auto', 'right': '0' });
                    }
                }, 10);
            }
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

        // Delete from Toolbar (Deletes checked records or loaded form record)
        $(document).on('click', '#btnDelete, .js-ob-delete-confirm', function (e) {
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
        $(document).on('click', '#btnExportPrint', function (e) {
            e.preventDefault();
            window.print();
        });

        // Export PDF
        $(document).on('click', '#btnPrintPdfBtn, #btnExportPdf', function (e) {
            e.preventDefault();
            exportToPdf();
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

        $('#OpeningBalance-saveBtn, #btnSave').html('<i class="fas fa-save"></i> <span>Save</span>');

        // Select Current FY in form
        let $modalOpt = $('#modalFinancialYear').find('option[data-current="1"]');
        if (!$modalOpt.length || !$modalOpt.val()) {
            $modalOpt = $('#modalFinancialYear').find('option:selected');
        }
        if (!$modalOpt.length || !$modalOpt.val()) {
            $modalOpt = $('#modalFinancialYear').find('option[value!=""]').last();
        }
        const activeFyCode = $modalOpt.val() || '';
        const activeFyText = $modalOpt.text().trim() || '';
        setSelectValue('modalFinancialYear', activeFyCode, activeFyText);

        setDatePickerValue('modalOpeningDate', new Date());

        const $compOpt = $('#modalCompany').find('option:selected');
        if ($compOpt.length && $compOpt.val()) {
            setSelectValue('modalCompany', $compOpt.val(), $compOpt.text().trim());
        } else {
            setSelectValue('modalCompany', '001', 'Formosa Logistics Limited');
        }

        const $brOpt = $('#modalBranch').find('option:selected');
        if ($brOpt.length && $brOpt.val()) {
            setSelectValue('modalBranch', $brOpt.val(), $brOpt.text().trim());
        } else {
            setSelectValue('modalBranch', '01', 'Head Office (Dhaka)');
        }

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

                const data = (res && res.data) ? res.data : res;
                if (!data || res.success === false) {
                    toastr.error(res.message || 'Record not found.');
                    return;
                }

                $('#OpeningBalance-isEdit').val('1');
                $('#OpeningBalance-autoId').val(data.autoId || '');
                $('#modalObCode').val(data.comOpeningBalanceCode || code);

                // Company: populate name and code
                const compVal = data.companyCodeRaw || (data.main_CompanyCode ? String(data.main_CompanyCode).padStart(3, '0') : '001');
                const compText = data.companyName || 'Formosa Logistics Limited';
                setSelectValue('modalCompany', compVal, compText);

                // Branch: populate branch name instead of numeric code 1
                const branchVal = data.branchCodeRaw || (data.branchCode ? String(data.branchCode).padStart(2, '0') : '01');
                const branchText = data.branchName || 'Head Office (Dhaka)';
                setSelectValue('modalBranch', branchVal, branchText);

                // Financial Year
                let fyVal = data.financialCodeNo || '';
                let fyText = '';
                if (fyVal) {
                    let $fyMatch = $('#modalFinancialYear').find(`option[value="${fyVal}"]`);
                    if (!$fyMatch.length) {
                        $fyMatch = $('#modalFinancialYear').find('option').filter(function () {
                            return $(this).text().trim() === String(fyVal).trim();
                        });
                        if ($fyMatch.length) {
                            fyVal = $fyMatch.val();
                        }
                    }
                    if ($fyMatch.length) {
                        fyText = $fyMatch.text().trim();
                    }
                    setSelectValue('modalFinancialYear', fyVal, fyText);
                }

                const dateVal = data.openingDateFormatted || data.openingDateIso || (data.openingDate ? data.openingDate.slice(0, 10) : '');
                setDatePickerValue('modalOpeningDate', dateVal);

                // Account Head: populate with Account Head Name e.g. "2030020000100002 - Bay Footwear Ltd (Inter company loan)"
                const glVal = data.generalLedgerCodeNo || '';
                const glName = data.generalLedgerName || '';
                const glText = glName ? `${glVal} - ${glName}` : (data.accountHeadDisplay || glVal);
                if (glVal) {
                    setSelectValue('modalAccountHead', glVal, glText);
                }

                $('#modalAmount').val(data.openingBalance !== null && data.openingBalance !== undefined ? data.openingBalance : '');

                const trVal = data.trType || '';
                const trText = trVal === 'Dr' ? 'Dr (Debit)' : (trVal === 'Cr' ? 'Cr (Credit)' : trVal);
                if (trVal) {
                    setSelectValue('modalTrType', trVal, trText);
                }

                $('#modalReference').val(data.reference === '-' ? '' : (data.reference || ''));
                $('#modalDescription').val(data.description === '-' ? '' : (data.description || ''));

                $('#OpeningBalance-saveBtn, #btnSave').html('<i class="fas fa-save"></i> <span>Update</span>');
                toastr.info(`Editing Opening Balance: ${data.comOpeningBalanceCode || code}`);

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
            CompanyCodeRaw: ($('#modalCompany').val() || '001').trim(),
            BranchCode: $('#modalBranch').val() ? parseInt($('#modalBranch').val(), 10) : null,
            BranchCodeRaw: ($('#modalBranch').val() || '01').trim(),
            FinancialCodeNo: ($('#modalFinancialYear').val() || '').trim(),
            GeneralLedgerCodeNo: ($('#modalAccountHead').val() || '').trim(),
            OpeningBalance: parseFloat($('#modalAmount').val()),
            TrType: ($('#modalTrType').val() || '').trim(),
            OpeningDateRaw: (function () {
                const el = document.getElementById('modalOpeningDate');
                if (el && el._flatpickr && el._flatpickr.selectedDates && el._flatpickr.selectedDates.length > 0) {
                    return formatDateToDMY(el._flatpickr.selectedDates[0]);
                }
                return ($('#modalOpeningDate').val() || '').trim();
            })(),
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
    }

    function resetBulkSelection() {
        $('.js-ob-check').prop('checked', false);
        $('#selectAllOpeningBalance').prop('checked', false);
        selectedCodes = [];
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
            toastr.warning('Please select at least one record to delete, or load a record to edit.');
            return;
        }

        pendingDeleteTarget = selectedCodes;
        isDeletingBulk = true;

        $('#obConfirmTitle').text('Confirm Delete');
        if (selectedCodes.length === 1) {
            $('#obConfirmMessage').html(`Are you sure you want to delete Opening Balance <strong>${escapeHtml(selectedCodes[0])}</strong>?<br/><small class="text-danger mt-1 d-block">This action cannot be undone.</small>`);
        } else {
            $('#obConfirmMessage').html(`Are you sure you want to delete <strong>${selectedCodes.length}</strong> selected opening balance record(s)?<br/><small class="text-danger mt-1 d-block">This action cannot be undone.</small>`);
        }

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

    function exportToPdf() {
        const filter = {
            FinancialCodeNo: $('#fFinancialYear').val() || null,
            GeneralLedgerCodeNo: $('#fAccountHead').val() || null,
            FromDate: $('#fFrom').val() || null,
            ToDate: $('#fTo').val() || null
        };

        toastr.info('Generating PDF report...');

        fetch('/OpeningBalance/ExportPdf', {
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
            a.download = `Opening_Balance_${new Date().toISOString().slice(0, 10)}.pdf`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
            toastr.success('PDF exported successfully.');
        })
        .catch(() => {
            toastr.error('Failed to export PDF.');
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
