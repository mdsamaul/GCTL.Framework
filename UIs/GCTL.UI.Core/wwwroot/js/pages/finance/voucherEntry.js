(function ($) {
    'use strict';

    let currentPage = 1;
    let pageSize = 10;
    let currentSortColumn = 'voucherdate';
    let currentSortOrder = 'desc';
    let searchTimer = null;
    let editingVoucherCode = null;
    let pendingAction = null; // { type: 'delete'|'post', code: string }
    let lines = [];
    let isResettingForm = false;
    let isResettingFilters = false;

    const VOUCHER_TYPES = {
        '1': 'Bank Receipt Voucher',
        '2': 'Bank Payment Voucher',
        '3': 'Cash Receipt Voucher',
        '4': 'Cash Payment Voucher',
        '5': 'Journal Voucher',
        '6': 'Contra Voucher',
        BRV: 'Bank Receipt Voucher',
        BPV: 'Bank Payment Voucher',
        CRV: 'Cash Receipt Voucher',
        CPV: 'Cash Payment Voucher',
        JV: 'Journal Voucher',
        CV: 'Contra Voucher'
    };

    const TYPE_NAMES = {
        '1': 'BRV',
        '2': 'BPV',
        '3': 'CRV',
        '4': 'CPV',
        '5': 'JV',
        '6': 'CV',
        BRV: 'BRV',
        BPV: 'BPV',
        CRV: 'CRV',
        CPV: 'CPV',
        JV: 'JV',
        CV: 'CV'
    };

    function getAccounts() {
        return (window.VOUCHER_DROPDOWNS && window.VOUCHER_DROPDOWNS.accounts) || [];
    }

    function getCashAccountCode() {
        const accs = getAccounts();
        const found = accs.find(a => a.IsCash || (a.Name && a.Name.toLowerCase().includes('cash')));
        return found ? found.Code : (accs[0] ? accs[0].Code : '');
    }

    function getBankAccountCode() {
        const accs = getAccounts();
        const found = accs.find(a => a.IsBank || (a.Name && (a.Name.toLowerCase().includes('bank') || a.Name.toLowerCase().includes('dbbl'))));
        return found ? found.Code : (accs[0] ? accs[0].Code : '');
    }

    function formatCurrency(n) {
        n = Math.round((+n + Number.EPSILON) * 100) / 100;
        if (!n) n = 0;
        return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function showToast(message, type) {
        if (typeof toastr !== 'undefined') {
            if (type === 'error' || type === 'bad') toastr.error(message);
            else if (type === 'warning') toastr.warning(message);
            else toastr.success(message);
        } else {
            alert(message);
        }
    }

    /* ===== Flatpickr Footer Helper (Today & Clear Buttons) ===== */
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
            fpInstance.close();
        });

        footer.appendChild(todayBtn);
        footer.appendChild(clearBtn);
        fpInstance.calendarContainer.appendChild(footer);
    }

    function setDatePicker(id, val, triggerChange = false) {
        const el = document.getElementById(id);
        if (!val) {
            if (el && el._flatpickr) el._flatpickr.clear();
            else $(`#${id}`).val('');
            return;
        }

        let dateObj = null;
        if (val instanceof Date) {
            dateObj = val;
        } else if (typeof val === 'string') {
            if (val.includes('/')) {
                // dd/mm/yyyy
                const parts = val.split('/');
                if (parts.length === 3) {
                    dateObj = new Date(parseInt(parts[2], 10), parseInt(parts[1], 10) - 1, parseInt(parts[0], 10));
                }
            } else if (val.includes('-')) {
                // yyyy-mm-dd
                const parts = val.split('T')[0].split('-');
                if (parts.length === 3) {
                    dateObj = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
                }
            }
        }

        if (el && el._flatpickr) {
            if (dateObj && !isNaN(dateObj.getTime())) {
                el._flatpickr.setDate(dateObj, triggerChange);
            } else {
                el._flatpickr.setDate(val, triggerChange);
            }
        } else {
            if (dateObj && !isNaN(dateObj.getTime())) {
                const dd = String(dateObj.getDate()).padStart(2, '0');
                const mm = String(dateObj.getMonth() + 1).padStart(2, '0');
                const yyyy = dateObj.getFullYear();
                $(`#${id}`).val(`${dd}/${mm}/${yyyy}`);
            } else {
                $(`#${id}`).val(val || '');
            }
        }
    }

    function getDateInputValue(id) {
        const el = document.getElementById(id);
        if (el && el._flatpickr) {
            if (el._flatpickr.selectedDates && el._flatpickr.selectedDates.length > 0) {
                const d = el._flatpickr.selectedDates[0];
                const y = d.getFullYear();
                const m = String(d.getMonth() + 1).padStart(2, '0');
                const day = String(d.getDate()).padStart(2, '0');
                return `${y}-${m}-${day}`;
            }
            if (el._flatpickr.altInput && el._flatpickr.altInput.value) {
                const altVal = el._flatpickr.altInput.value.trim();
                if (altVal.includes('/')) {
                    const p = altVal.split('/');
                    if (p.length === 3 && p[2].length === 4) {
                        return `${p[2]}-${p[1].padStart(2, '0')}-${p[0].padStart(2, '0')}`;
                    }
                } else if (altVal.includes('-')) {
                    const p = altVal.split('T')[0].split('-');
                    if (p.length === 3) {
                        if (p[0].length === 4) return p.join('-');
                        if (p[2].length === 4) return `${p[2]}-${p[1].padStart(2, '0')}-${p[0].padStart(2, '0')}`;
                    }
                }
            }
        }
        const val = ($(`#${id}`).val() || '').trim();
        if (!val) return null;
        if (val.includes('/')) {
            const p = val.split('/');
            if (p.length === 3 && p[2].length === 4) return `${p[2]}-${p[1].padStart(2, '0')}-${p[0].padStart(2, '0')}`;
        }
        if (val.includes('-')) {
            const p = val.split('T')[0].split('-');
            if (p.length === 3) {
                if (p[0].length === 4) return p.join('-');
                if (p[2].length === 4) return `${p[2]}-${p[1].padStart(2, '0')}-${p[0].padStart(2, '0')}`;
            }
            return val.split('T')[0];
        }
        return val;
    }

    // Number to words converter for Bangladeshi Taka
    function amountInWords(amount) {
        const cents = Math.round((Math.abs(amount) + Number.EPSILON) * 100);
        const whole = Math.floor(cents / 100);
        const paisa = cents % 100;
        const units = [[10000000, 'Crore'], [100000, 'Lakh'], [1000, 'Thousand'], [1, '']];
        let n = whole, out = '';

        for (const [value, label] of units) {
            if (n >= value) {
                const part = Math.floor(n / value);
                out += (out ? ' ' : '') + amountInWordsGroup(part);
                if (label) out += ' ' + label;
                n %= value;
            }
        }
        if (!out) out = 'Zero';
        return `${out} Taka${paisa ? ` and ${amountInWordsGroup(paisa)} Paisa` : ''} Only`;
    }

    function amountInWordsGroup(n) {
        const small = ['Zero', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
        const tens = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];
        let out = '';
        if (n >= 100) {
            out = small[Math.floor(n / 100)] + ' Hundred';
            n %= 100;
            if (n) out += ' ';
        }
        if (n >= 20) {
            out += tens[Math.floor(n / 10)];
            n %= 10;
            if (n) out += '-' + small[n];
        } else if (n) {
            out += small[n];
        }
        return out;
    }

    /* ===== DOM Ready ===== */
    $(document).ready(function () {
        initDatePickers();
        initSelect2Dropdowns();
        initDefaultForm();
        initEvents();
        updateSortingIndicator();
        setTimeout(updateSortingIndicator, 100);
        loadTableData();
    });

    /* ===== Custom Dropdown & Select2 Helpers ===== */
    function setDropdownValue(id, val, text) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        if (typeof window.setCustomDropdownValue === 'function') {
            window.setCustomDropdownValue(rawId, val, text);
        }
        $el.val(val || '');
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(val || null).trigger('change');
        } else {
            $el.trigger('change');
        }
    }

    function clearDropdownValue(id) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        if (typeof window.clearCustomDropdownById === 'function') {
            window.clearCustomDropdownById(rawId);
        }
        $el.val('');
        if ($el.hasClass('select2-hidden-accessible')) {
            $el.val(null).trigger('change');
        } else {
            $el.trigger('change');
        }
    }

    function setDropdownDisabled(id, disabled) {
        const rawId = id.replace('#', '');
        const $el = $('#' + rawId);
        $el.prop('disabled', !!disabled);
        const inst = $el.data('customDdInstance');
        if (inst && typeof inst.setDisabled === 'function') {
            inst.setDisabled(disabled);
        }
    }

    function initSelect2Dropdowns() {
        if (typeof window.initCustomDropdowns === 'function') {
            window.initCustomDropdowns('#vchFormCard, .vch-filter-toolbar', false);
        }

        if (typeof $.fn.select2 === 'undefined') return;

        if (!$('#vCompany').hasClass('custom-select-single')) {
            $('#vCompany').select2({
                width: '100%',
                dropdownAutoWidth: true,
                placeholder: 'Select Company',
                allowClear: true
            });
        }

        if (!$('#vBranch').hasClass('custom-select-single')) {
            $('#vBranch').select2({
                width: '100%',
                dropdownAutoWidth: true,
                placeholder: 'Select Branch',
                allowClear: true
            });
        }

        if (!$('#vType').hasClass('custom-select-single')) {
            $('#vType').select2({
                width: '100%',
                dropdownAutoWidth: true,
                placeholder: 'Select Voucher Type',
                allowClear: true
            });
        }

        if (!$('#vfType').hasClass('custom-select-single')) {
            $('#vfType').select2({
                width: '100%',
                dropdownAutoWidth: true,
                placeholder: 'All types',
                allowClear: true
            });
        }

        if (!$('#vfStat').hasClass('custom-select-single')) {
            $('#vfStat').select2({
                width: '100%',
                dropdownAutoWidth: true,
                placeholder: 'All statuses',
                allowClear: true
            });
        }

        // Dynamic opened dropdown width: at least the dropdown box width, or as wide as the longest item on 1 line
        $(document).off('select2:open.vchWidth').on('select2:open.vchWidth', function (e) {
            const $select = $(e.target);
            const $inFlowContainer = $select.next('.select2-container');
            const boxWidth = $inFlowContainer.outerWidth() || 200;
            const $openContainer = $('body > .select2-container--open');
            const $dropdown = $openContainer.find('.select2-dropdown');
            
            if ($dropdown.length) {
                $dropdown.css({
                    'min-width': boxWidth + 'px',
                    'width': 'max-content'
                });

                setTimeout(function () {
                    const offset = $openContainer.offset();
                    const dropWidth = $dropdown.outerWidth() || 0;
                    const winWidth = $(window).width();
                    if (offset && offset.left + dropWidth > winWidth - 10) {
                        const adjustedLeft = Math.max(10, winWidth - dropWidth - 15);
                        $openContainer.css('left', adjustedLeft + 'px');
                    }
                }, 0);
            }
        });
    }

    // Financial year in Bangladesh: July 1st to June 30th
    // If month >= 7 (July..Dec): current fiscal year start is 01/07/{currentYear}
    // If month < 7 (Jan..June): current fiscal year start is 01/07/{currentYear - 1}
    function getFiscalYearStartDate() {
        const today = new Date();
        const curYear = today.getFullYear();
        const curMonth = today.getMonth() + 1; // 1-12
        const fyStartYear = curMonth >= 7 ? curYear : curYear - 1;
        return new Date(fyStartYear, 6, 1); // Month index 6 = July (0-indexed)
    }

    function getTodayDate() {
        return new Date();
    }

    function initDatePickers() {
        ['vDate', 'vfFrom', 'vfTo'].forEach(function (id) {
            const el = document.getElementById(id);
            if (!el || typeof flatpickr === 'undefined') return;
            if (el._flatpickr) el._flatpickr.destroy();

            flatpickr(el, {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                allowInput: true,
                onReady: function (selectedDates, dateStr, instance) {
                    instance.input.placeholder = "dd/mm/yyyy";
                    addCalendarTodayClearButtons(instance);
                },
                onOpen: function (selectedDates, dateStr, instance) {
                    addCalendarTodayClearButtons(instance);
                }
            });
        });

        setDatePicker('vDate', getTodayDate(), false);
        setDatePicker('vfFrom', getFiscalYearStartDate(), false);
        setDatePicker('vfTo', getTodayDate(), false);

        function triggerFilterReload() {
            if (!isResettingFilters) {
                currentPage = 1;
                loadTableData();
            }
        }

        // Attach flatpickr onChange listeners and altInput change/enter so calendar clicks and typing both reload table
        const fromEl = document.getElementById('vfFrom');
        if (fromEl && fromEl._flatpickr) {
            fromEl._flatpickr.config.onChange.push(triggerFilterReload);
            if (fromEl._flatpickr.altInput) {
                $(fromEl._flatpickr.altInput).on('change', triggerFilterReload);
                $(fromEl._flatpickr.altInput).on('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        triggerFilterReload();
                    }
                });
            }
        }
        $('#vfFrom').on('change', triggerFilterReload);

        const toEl = document.getElementById('vfTo');
        if (toEl && toEl._flatpickr) {
            toEl._flatpickr.config.onChange.push(triggerFilterReload);
            if (toEl._flatpickr.altInput) {
                $(toEl._flatpickr.altInput).on('change', triggerFilterReload);
                $(toEl._flatpickr.altInput).on('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        triggerFilterReload();
                    }
                });
            }
        }
        $('#vfTo').on('change', triggerFilterReload);
    }

    function initDefaultForm() {
        isResettingForm = true;
        editingVoucherCode = null;
        $('#vCodeNo').val('');
        $('#formVoucherTitle').html('<i class="fas fa-edit me-1 text-primary"></i>' + (window.SmartLocalize ? window.SmartLocalize('voucher Entry') : 'voucher Entry'));
        $('#editVoucherBadge').hide();
        $('#vBtnSaveText').text('Save voucher');
        setDropdownDisabled('vType', false);

        // Auto-select Company by default
        const defaultCompany = $('#vCompany option[value!=""]:first').val() || '1';
        setDropdownValue('vCompany', defaultCompany);

        // Branch and Voucher Type remain unselected
        clearDropdownValue('vBranch');
        clearDropdownValue('vType');
        $('#vNo').val('');
        $('#vRef').val('');
        $('#vNarr').val('');
        $('#vStat').val('Pending Approval');
        setDatePicker('vDate', getTodayDate(), false);

        isResettingForm = false;

        // Clean initial 2 blank rows until user selects a voucher type
        lines = [
            { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: '' },
            { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: '' }
        ];
        renderLines();
    }

    function generateNextVoucherNo(type) {
        $.ajax({
            url: '/VoucherEntry/GetNextVoucherNo',
            type: 'GET',
            data: { type: type },
            success: function (res) {
                if (res && res.success) {
                    $('#vNo').val(res.voucherNo);
                }
            }
        });
    }

    function getActiveVoucherType() {
        const val = ($('#vType').val() || '').toString();
        const $sel = $('#vType option:selected');
        const dt = ($sel.data('typename') || '').toString().toUpperCase();
        if (dt) return dt;
        if (TYPE_NAMES[val]) return TYPE_NAMES[val];
        return val.toUpperCase();
    }

    function resetLinesForType(type) {
        const cash = getCashAccountCode();
        const bank = getBankAccountCode();
        const t = (type || '').toString().toUpperCase();

        if (t === 'BRV' || t === '1') {
            lines = [
                { accCode: bank, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: true, autoSide: 'Dr' },
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: 'Cr' }
            ];
        } else if (t === 'BPV' || t === '2') {
            lines = [
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: 'Dr' },
                { accCode: bank, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: true, autoSide: 'Cr' }
            ];
        } else if (t === 'CRV' || t === '3') {
            lines = [
                { accCode: cash, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: true, autoSide: 'Dr' },
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: 'Cr' }
            ];
        } else if (t === 'CPV' || t === '4') {
            lines = [
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: 'Dr' },
                { accCode: cash, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: true, autoSide: 'Cr' }
            ];
        } else if (t === 'CV' || t === '6') {
            lines = [
                { accCode: cash, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false },
                { accCode: bank, desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false }
            ];
        } else {
            lines = [
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false },
                { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false }
            ];
        }
        renderLines();
    }

    function applyAutoSummation() {
        const t = getActiveVoucherType();

        // Receipt Voucher: Bank/Cash is auto DEBIT, user enters CREDIT entries
        if (t === 'BRV' || t === 'CRV' || t === '1' || t === '3') {
            let autoIdx = lines.findIndex(l => l.isAuto && l.autoSide === 'Dr');
            if (autoIdx < 0) return; // Do not guess or overwrite if no auto line defined!

            let totalCredit = 0;
            lines.forEach(function (ln, i) {
                if (i !== autoIdx) {
                    totalCredit += parseFloat(ln.cr) || 0;
                }
            });
            totalCredit = Math.round((totalCredit + Number.EPSILON) * 100) / 100;

            if (lines[autoIdx]) {
                lines[autoIdx].dr = totalCredit;
                lines[autoIdx].cr = 0;
                const $row = $(`.vch-ln-row[data-index="${autoIdx}"]`);
                $row.find('.ln-dr').val(totalCredit > 0 ? totalCredit : '');
                $row.find('.ln-cr').val('');
            }
        }
        // Payment Voucher: Bank/Cash is auto CREDIT, user enters DEBIT entries
        else if (t === 'BPV' || t === 'CPV' || t === '2' || t === '4') {
            let autoIdx = lines.findIndex(l => l.isAuto && l.autoSide === 'Cr');
            if (autoIdx < 0) return; // Do not guess or overwrite if no auto line defined!

            let totalDebit = 0;
            lines.forEach(function (ln, i) {
                if (i !== autoIdx) {
                    totalDebit += parseFloat(ln.dr) || 0;
                }
            });
            totalDebit = Math.round((totalDebit + Number.EPSILON) * 100) / 100;

            if (lines[autoIdx]) {
                lines[autoIdx].cr = totalDebit;
                lines[autoIdx].dr = 0;
                const $row = $(`.vch-ln-row[data-index="${autoIdx}"]`);
                $row.find('.ln-cr').val(totalDebit > 0 ? totalDebit : '');
                $row.find('.ln-dr').val('');
            }
        }
        // Contra Voucher: 2 lines mirror
        else if (t === 'CV' || t === '6') {
            if (lines.length === 2) {
                const dr0 = parseFloat(lines[0].dr) || 0;
                const cr0 = parseFloat(lines[0].cr) || 0;
                if (dr0 > 0) {
                    lines[1].cr = dr0;
                    lines[1].dr = 0;
                    $(`.vch-ln-row[data-index="1"] .ln-cr`).val(dr0 > 0 ? dr0 : '');
                    $(`.vch-ln-row[data-index="1"] .ln-dr`).val('');
                } else if (cr0 > 0) {
                    lines[1].dr = cr0;
                    lines[1].cr = 0;
                    $(`.vch-ln-row[data-index="1"] .ln-dr`).val(cr0 > 0 ? cr0 : '');
                    $(`.vch-ln-row[data-index="1"] .ln-cr`).val('');
                }
            }
        }
    }

    function syncLinesFromDom() {
        $('#voucherLinesContainer .vch-ln-row').each(function () {
            const idx = parseInt($(this).data('index'), 10);
            if (lines[idx]) {
                const accVal = $(this).find('.ln-acc').val();
                lines[idx].accCode = accVal || '';
                lines[idx].desc = $(this).find('.ln-desc').val() || '';
                lines[idx].chq = $(this).find('.ln-chq').val() || '';
                lines[idx].chqd = $(this).find('.ln-chqd').val() || '';
                lines[idx].dr = parseFloat($(this).find('.ln-dr').val()) || 0;
                lines[idx].cr = parseFloat($(this).find('.ln-cr').val()) || 0;
            }
        });
    }

    function renderLines(isEditPopulation) {
        const accounts = getAccounts();
        const container = $('#voucherLinesContainer');

        // Destroy existing custom dropdown and Select2 instances on lines
        container.find('.ln-acc').each(function () {
            const inst = $(this).data('customDdInstance');
            if (inst && typeof inst.destroy === 'function') {
                inst.destroy();
            }
            if ($(this).data('select2')) {
                $(this).select2('destroy');
            }
        });
        container.empty();

        const t = getActiveVoucherType();

        lines.forEach(function (ln, i) {
            let optionsHtml = '<option value="">Search code or name…</option>';

            // Fallback option if current account head is not in active accounts
            if (ln.accCode && !accounts.some(a => a.Code === ln.accCode)) {
                let fallbackTitle = (ln.accName && ln.accName !== ln.accCode) ? ln.accName : '';
                if (!fallbackTitle && ln.desc) fallbackTitle = ln.desc;
                const optText = fallbackTitle ? `${ln.accCode} - ${escapeHtml(fallbackTitle)}` : escapeHtml(ln.accCode);
                optionsHtml += `<option value="${escapeHtml(ln.accCode)}" selected>${optText}</option>`;
            }

            let grouped = {};
            accounts.forEach(function (acc) {
                const grp = acc.GroupName || 'Accounts';
                if (!grouped[grp]) grouped[grp] = [];
                grouped[grp].push(acc);
            });

            for (const grpName in grouped) {
                optionsHtml += `<optgroup label="${escapeHtml(grpName)}">`;
                grouped[grpName].forEach(function (acc) {
                    const sel = acc.Code === ln.accCode ? 'selected' : '';
                    optionsHtml += `<option value="${acc.Code}" ${sel}>${acc.Code} - ${escapeHtml(acc.Name)}</option>`;
                });
                optionsHtml += `</optgroup>`;
            }

            const isAutoDr = ln.isAuto && ln.autoSide === 'Dr';
            const isAutoCr = ln.isAuto && ln.autoSide === 'Cr';

            // Debit Input Attributes (Editable, NOT readonly, NOT disabled)
            const drClass = 'form-control form-control-sm text-end ln-dr' + (isAutoDr ? ' bg-light fw-bold text-primary' : '');
            const drTitle = isAutoDr ? 'Auto-summed from Credit entries' : '';

            // Credit Input Attributes (Editable, NOT readonly, NOT disabled)
            const crClass = 'form-control form-control-sm text-end ln-cr' + (isAutoCr ? ' bg-light fw-bold text-primary' : '');
            const crTitle = isAutoCr ? 'Auto-summed from Debit entries' : '';

            const deleteBtn = lines.length <= 2 ?
                `<button type="button" class="btn btn-sm p-1 lh-1 text-muted" disabled title="A voucher requires at least two lines"><i class="fas fa-lock fs-11"></i></button>` :
                `<button type="button" class="btn btn-phoenix-danger btn-sm p-1 lh-1 btn-rm-ln" data-index="${i}" title="Remove Line"><i class="fas fa-times"></i></button>`;

            const rowHtml = `
                <div class="vch-ln-row" data-index="${i}">
                    <div>
                        <select class="form-select form-select-sm ln-acc custom-select-single" id="lnAcc_${i}" data-index="${i}" data-page-size="100" data-placeholder="Search code or name…">
                            ${optionsHtml}
                        </select>
                    </div>
                    <div>
                        <input type="text" class="form-control form-control-sm ln-desc" data-index="${i}" placeholder="Description" value="${escapeHtml(ln.desc || '')}" />
                    </div>
                    <div>
                        <input type="text" class="form-control form-control-sm ln-chq" data-index="${i}" placeholder="Cheque No" value="${escapeHtml(ln.chq || '')}" />
                    </div>
                    <div>
                        <input type="text" class="form-control form-control-sm ln-chqd" data-index="${i}" placeholder="dd/mm/yyyy" value="${escapeHtml(ln.chqd || '')}" />
                    </div>
                    <div>
                        <input type="number" step="0.01" min="0" class="${drClass}" data-index="${i}" placeholder="0.00" value="${ln.dr ? ln.dr : ''}" title="${drTitle}" />
                    </div>
                    <div>
                        <input type="number" step="0.01" min="0" class="${crClass}" data-index="${i}" placeholder="0.00" value="${ln.cr ? ln.cr : ''}" title="${crTitle}" />
                    </div>
                    <div class="text-center">
                        <button type="button" class="btn btn-phoenix-primary btn-sm p-1 lh-1 btn-add-ln" data-index="${i}" title="Add Line">
                            <i class="fas fa-plus"></i>
                        </button>
                    </div>
                    <div class="text-center">
                        ${deleteBtn}
                    </div>
                </div>
            `;
            container.append(rowHtml);
        });

        // Initialize custom-select-single on each row's account head dropdown
        if (typeof window.initCustomDropdowns === 'function') {
            window.initCustomDropdowns(container, true);
        }

        // Initialize datepickers on cheque dates with Today & Clear buttons
        container.find('.ln-chqd').each(function () {
            const inputEl = this;
            const idx = parseInt($(inputEl).data('index'), 10);
            const curVal = lines[idx] ? lines[idx].chqd : '';
            if (typeof flatpickr !== 'undefined') {
                if (inputEl._flatpickr) inputEl._flatpickr.destroy();
                flatpickr(inputEl, {
                    dateFormat: "d/m/Y",
                    altInput: true,
                    altFormat: "d/m/Y",
                    allowInput: true,
                    defaultDate: curVal || undefined,
                    onReady: function (selectedDates, dateStr, instance) {
                        instance.input.placeholder = "dd/mm/yyyy";
                        addCalendarTodayClearButtons(instance);
                    },
                    onOpen: function (selectedDates, dateStr, instance) {
                        addCalendarTodayClearButtons(instance);
                    },
                    onChange: function (selectedDates, dateStr) {
                        if (!isNaN(idx) && lines[idx]) {
                            lines[idx].chqd = dateStr;
                        }
                    }
                });
            }
        });

        if (!isEditPopulation) {
            applyAutoSummation();
        }
        recalculateTotals();
    }

    function recalculateTotals() {
        let totalDr = 0;
        let totalCr = 0;

        lines.forEach(function (ln) {
            totalDr += parseFloat(ln.dr) || 0;
            totalCr += parseFloat(ln.cr) || 0;
        });

        totalDr = Math.round((totalDr + Number.EPSILON) * 100) / 100;
        totalCr = Math.round((totalCr + Number.EPSILON) * 100) / 100;
        const diff = Math.round(Math.abs(totalDr - totalCr) * 100) / 100;

        $('#vTotalDebit').text('৳ ' + formatCurrency(totalDr));
        $('#vTotalCredit').text('৳ ' + formatCurrency(totalCr));
        $('#vDifference').text('৳ ' + formatCurrency(diff));

        const isBalanced = (diff === 0 && totalDr > 0);
        const badge = $('#vBalanceBadge');
        if (isBalanced) {
            badge.removeClass('bg-danger').addClass('bg-success').html('<i class="fas fa-check me-1"></i>Balanced');
        } else {
            badge.removeClass('bg-success').addClass('bg-danger').html('<i class="fas fa-times me-1"></i>Not balanced');
        }
    }

    function escapeHtml(s) {
        return String(s || '').replace(/[&<>"']/g, function (m) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m];
        });
    }

    /* ===== Events Setup ===== */
    function initEvents() {
        // Voucher type change
        $('#vType').on('change', function () {
            if (!editingVoucherCode && !isResettingForm) {
                const t = $(this).val();
                if (t) {
                    const typeName = $(this).find('option:selected').data('typename') || t;
                    generateNextVoucherNo(t);
                    resetLinesForType(typeName || t);
                } else {
                    $('#vNo').val('');
                    lines = [
                        { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: '' },
                        { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: '' }
                    ];
                    renderLines();
                }
            }
        });

        // Line inputs event delegation & active row elevation
        $(document).on('focus', '.vch-ln-row input, .vch-ln-row select, .vch-ln-row .custom-dd-input', function () {
            $('.vch-ln-row').removeClass('has-open-dd');
            $(this).closest('.vch-ln-row').addClass('has-open-dd');
        });

        $(document).on('blur', '.vch-ln-row input, .vch-ln-row select, .vch-ln-row .custom-dd-input', function () {
            const $row = $(this).closest('.vch-ln-row');
            setTimeout(function () {
                if (!$row.find('.custom-dd-wrapper.open').length && !$row.find(':focus').length) {
                    $row.removeClass('has-open-dd');
                }
            }, 200);
        });

        $(document).on('change', '.ln-acc', function () {
            const idx = parseInt($(this).data('index'), 10);
            if (lines[idx]) {
                lines[idx].accCode = $(this).val();
            }
        });

        $(document).on('input', '.ln-desc', function () {
            const idx = parseInt($(this).data('index'), 10);
            if (lines[idx]) {
                lines[idx].desc = $(this).val();
            }
        });

        $(document).on('input', '.ln-chq', function () {
            const idx = parseInt($(this).data('index'), 10);
            if (lines[idx]) {
                lines[idx].chq = $(this).val();
            }
        });

        $(document).on('change', '.ln-chqd', function () {
            const idx = parseInt($(this).data('index'), 10);
            if (lines[idx]) {
                lines[idx].chqd = $(this).val();
            }
        });

        $(document).on('input', '.ln-dr', function () {
            const idx = parseInt($(this).data('index'), 10);
            const val = parseFloat($(this).val()) || 0;
            if (lines[idx]) {
                lines[idx].dr = val;
                if (val > 0) {
                    lines[idx].cr = 0;
                    $(this).closest('.vch-ln-row').find('.ln-cr').val('');
                }
            }
            const t = getActiveVoucherType();
            const isRec = (t === 'BRV' || t === 'CRV' || t === '1' || t === '3');
            const isPay = (t === 'BPV' || t === 'CPV' || t === '2' || t === '4');

            if (isPay) {
                applyAutoSummation();
            } else if (isRec) {
                if (!lines[idx] || !lines[idx].isAuto) {
                    applyAutoSummation();
                }
            } else if (t === 'CV' || t === '6') {
                if (idx === 0) applyAutoSummation();
            }
            recalculateTotals();
        });

        $(document).on('input', '.ln-cr', function () {
            const idx = parseInt($(this).data('index'), 10);
            const val = parseFloat($(this).val()) || 0;
            if (lines[idx]) {
                lines[idx].cr = val;
                if (val > 0) {
                    lines[idx].dr = 0;
                    $(this).closest('.vch-ln-row').find('.ln-dr').val('');
                }
            }
            const t = getActiveVoucherType();
            const isRec = (t === 'BRV' || t === 'CRV' || t === '1' || t === '3');
            const isPay = (t === 'BPV' || t === 'CPV' || t === '2' || t === '4');

            if (isRec) {
                applyAutoSummation();
            } else if (isPay) {
                if (!lines[idx] || !lines[idx].isAuto) {
                    applyAutoSummation();
                }
            } else if (t === 'CV' || t === '6') {
                if (idx === 0) applyAutoSummation();
            }
            recalculateTotals();
        });

        $(document).on('click', '.btn-add-ln', function () {
            syncLinesFromDom();
            const idx = parseInt($(this).data('index'), 10);
            const t = getActiveVoucherType();
            let newSide = '';
            if (t === 'BRV' || t === 'CRV' || t === '1' || t === '3') newSide = 'Cr';
            else if (t === 'BPV' || t === 'CPV' || t === '2' || t === '4') newSide = 'Dr';

            lines.splice(idx + 1, 0, { accCode: '', desc: '', chq: '', chqd: '', dr: 0, cr: 0, isAuto: false, autoSide: newSide });
            renderLines();
        });

        $(document).on('click', '.btn-rm-ln', function () {
            syncLinesFromDom();
            const idx = parseInt($(this).data('index'), 10);
            if (lines.length <= 2) {
                showToast('A voucher needs at least two lines.', 'error');
                return;
            }
            lines.splice(idx, 1);
            applyAutoSummation();
            renderLines();
            recalculateTotals();
        });

        // Clear Form Button
        $('#vBtnClear').on('click', function () {
            $('#vchForm')[0].reset();
            initDefaultForm();
        });

        // Save Form Button
        $('#vBtnSave').on('click', function () {
            saveVoucher();
        });

        // Filter Inputs
        $('#vfType, #vfStat').on('change', function () {
            if (isResettingFilters) return;
            currentPage = 1;
            loadTableData();
        });

        $('#vfType, #vfStat').on('select2:clear', function () {
            if (isResettingFilters) return;
            $(this).val(null);
            setTimeout(function () {
                currentPage = 1;
                loadTableData();
            }, 50);
        });

        $('#vfFrom, #vfTo').on('change', function () {
            if (isResettingFilters) return;
            currentPage = 1;
            loadTableData();
        });

        $('#VoucherEntry-searchInput, #vfQ').on('input', function () {
            if (isResettingFilters) return;
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () {
                currentPage = 1;
                loadTableData();
            }, 300);
        });

        $('#VoucherEntry-searchInput, #vfQ').on('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                clearTimeout(searchTimer);
                currentPage = 1;
                loadTableData();
            }
        });

        $('#VoucherEntry-searchBtn').on('click', function () {
            clearTimeout(searchTimer);
            currentPage = 1;
            loadTableData();
        });

        // Click KPI Breakdown Cards to filter by Voucher Type
        $(document).on('click', '#vchTypeRow .vch-type-card', function () {
            const cardClass = $(this).attr('class') || '';
            let targetType = '';
            if (cardClass.includes('type-BRV')) targetType = '1';
            else if (cardClass.includes('type-BPV')) targetType = '2';
            else if (cardClass.includes('type-CRV')) targetType = '3';
            else if (cardClass.includes('type-CPV')) targetType = '4';
            else if (cardClass.includes('type-JV')) targetType = '5';
            else if (cardClass.includes('type-CV')) targetType = '6';

            if (targetType) {
                if ($('#vfType').val() === targetType) {
                    clearDropdownValue('vfType');
                } else {
                    const optText = $('#vfType option[value="' + targetType + '"]').text() || targetType;
                    setDropdownValue('vfType', targetType, optText);
                }
            }
        });

        $('#btnResetVFilters').on('click', function () {
            isResettingFilters = true;

            // Reset dropdowns so displayed selection clears back to placeholder
            clearDropdownValue('vfType');
            clearDropdownValue('vfStat');

            // Reset search inputs
            $('#vfQ').val('');
            $('#VoucherEntry-searchInput').val('');

            // Reset datepickers to Fiscal Year Start and Today (without triggering cascade)
            setDatePicker('vfFrom', getFiscalYearStartDate(), false);
            setDatePicker('vfTo', getTodayDate(), false);

            isResettingFilters = false;

            currentSortColumn = 'voucherdate';
            currentSortOrder = 'desc';
            updateSortingIndicator();

            currentPage = 1;
            loadTableData();
        });

        $('#VoucherEntry-pageSizeSelect').on('change', function () {
            pageSize = parseInt($(this).val(), 10) || 10;
            currentPage = 1;
            loadTableData();
        });

        // Table Header Sorting (Only ONE single arrow on active column: up when asc, down when desc)
        $(document).on('click', '#VoucherEntry-table th.vch-sort, #VoucherEntry-table th.sort, #VoucherEntry-table th[data-sort]', function () {
            const col = ($(this).data('sort') || '').toLowerCase();
            if (!col) return;
            if (currentSortColumn.toLowerCase() === col) {
                currentSortOrder = (currentSortOrder.toLowerCase() === 'asc') ? 'desc' : 'asc';
            } else {
                currentSortColumn = col;
                currentSortOrder = 'asc';
            }
            updateSortingIndicator();
            currentPage = 1;
            loadTableData();
        });

        // Export Selector
        $('#vfExport').on('change', function () {
            const val = $(this).val();
            $(this).val('');
            if (val === 'p') {
                printVoucherList();
            } else if (val === 'x') {
                exportExcel();
            } else if (val === 'pdf') {
                exportPdf();
            }
        });

        // Print modal button (direct in-place print without Edge popup crash)
        $('#btnPrintVoucherDoc').on('click', function () {
            printModalVoucher();
        });

        // Action confirmation modal click
        $('#btnConfirmAction').on('click', function () {
            if (!pendingAction) return;

            const modalEl = document.getElementById('voucherConfirmModal');
            const modal = bootstrap.Modal.getInstance(modalEl);
            if (modal) modal.hide();

            if (pendingAction.type === 'delete') {
                executeDelete(pendingAction.code);
            } else if (pendingAction.type === 'post') {
                executePost(pendingAction.code);
            }
            pendingAction = null;
        });
    }

    /* ===== Save Voucher ===== */
    function saveVoucher() {
        syncLinesFromDom();

        const type = $('#vType').val();
        const dateIso = getDateInputValue('vDate');
        const no = $('#vNo').val();
        const narr = $('#vNarr').val().trim();
        const ref = $('#vRef').val().trim();
        // Initial / saving status is always Pending Approval unless updating an already posted voucher
        let stat = 'Pending Approval';
        if (editingVoucherCode) {
            const currentStat = ($('#vStat').val() || '').trim();
            if (currentStat === 'Posted' || currentStat === 'Approved' || currentStat === 'Y') {
                stat = 'Posted';
            }
        }
        const company = $('#vCompany').val();
        const branch = $('#vBranch').val();

        if (!branch) {
            showToast('Please select a branch.', 'error');
            return;
        }
        if (!type) {
            showToast('Please select a voucher type.', 'error');
            return;
        }
        if (!dateIso) {
            showToast('Please select a voucher date.', 'error');
            return;
        }
        if (!narr) {
            showToast('Please enter a narration.', 'error');
            return;
        }

        const validLines = lines.filter(x => x.accCode && ((parseFloat(x.dr) || 0) > 0 || (parseFloat(x.cr) || 0) > 0));
        if (validLines.length < 2) {
            showToast('A voucher requires at least two transaction lines.', 'error');
            return;
        }

        let totalDr = 0, totalCr = 0;
        validLines.forEach(l => {
            totalDr += parseFloat(l.dr) || 0;
            totalCr += parseFloat(l.cr) || 0;
        });

        totalDr = Math.round((totalDr + Number.EPSILON) * 100) / 100;
        totalCr = Math.round((totalCr + Number.EPSILON) * 100) / 100;

        if (Math.abs(totalDr - totalCr) > 0.01) {
            showToast(`Total Debit (৳${formatCurrency(totalDr)}) and Total Credit (৳${formatCurrency(totalCr)}) must be equal.`, 'error');
            return;
        }

        const currentBtnText = editingVoucherCode ? 'Update voucher' : 'Save voucher';

        const model = {
            VoucherEntryCodeNo: $('#vCodeNo').val(),
            VoucherNo: no,
            VoucherType_Code: type,
            VoucherDate: `${dateIso}T00:00:00`,
            CompanyCode: company,
            Main_CompanyCode: company,
            BranchCode: branch,
            Reference: ref,
            InvoiceNo: ref,
            Narration: narr,
            IsApproved: stat,
            Details: validLines.map(l => {
                let chqDateStr = null;
                if (l.chqd) {
                    if (l.chqd.includes('-')) {
                        chqDateStr = `${l.chqd.split('T')[0]}T00:00:00`;
                    } else if (l.chqd.includes('/')) {
                        const cParts = l.chqd.split('/');
                        if (cParts.length === 3) {
                            chqDateStr = `${cParts[2]}-${cParts[1].padStart(2, '0')}-${cParts[0].padStart(2, '0')}T00:00:00`;
                        }
                    }
                }
                return {
                    AccCode: l.accCode,
                    TrType: (parseFloat(l.dr) || 0) > 0 ? 'Dr' : 'Cr',
                    Description: l.desc,
                    DebitAmount: parseFloat(l.dr) || 0,
                    CreditAmount: parseFloat(l.cr) || 0,
                    ChequeNo: l.chq,
                    ChequeDate: chqDateStr
                };
            })
        };

        $('#vBtnSave').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Saving...');

        $.ajax({
            url: '/VoucherEntry/Save',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                $('#vBtnSave').prop('disabled', false).html('<i class="fas fa-save me-1"></i><span id="vBtnSaveText">' + currentBtnText + '</span>');
                if (res && res.success) {
                    showToast(res.message || 'Voucher saved successfully.');
                    initDefaultForm();
                    loadTableData();
                } else {
                    showToast(res ? res.message : 'Error saving voucher.', 'error');
                }
            },
            error: function () {
                $('#vBtnSave').prop('disabled', false).html('<i class="fas fa-save me-1"></i><span id="vBtnSaveText">' + currentBtnText + '</span>');
                showToast('Failed to connect to server.', 'error');
            }
        });
    }

    let activeTableRequest = null;

    /* ===== Load Table Data & KPI Cards ===== */
    function loadTableData() {
        const filter = {
            PageNumber: currentPage,
            PageSize: pageSize,
            SortColumn: currentSortColumn,
            SortOrder: currentSortOrder,
            VoucherType: $('#vfType').val() || null,
            Status: $('#vfStat').val() || null,
            SearchTerm: ($('#VoucherEntry-searchInput').val() || $('#vfQ').val() || '').trim() || null,
            FromDate: getDateInputValue('vfFrom') || null,
            ToDate: getDateInputValue('vfTo') || null
        };

        $('#VoucherEntry-body').html('<tr><td colspan="8" class="text-center py-4 text-muted"><i class="fas fa-spinner fa-spin me-1"></i>Loading vouchers...</td></tr>');

        if (activeTableRequest && activeTableRequest.readyState !== 4) {
            activeTableRequest.abort();
        }

        activeTableRequest = $.ajax({
            url: '/VoucherEntry/GetAll',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filter),
            success: function (res) {
                if (res && res.success && res.data) {
                    updateKpiCards(res.data.summary);
                    renderTableRows(res.data.items);
                    renderPagination(res.data.totalCount, res.data.pageNumber, res.data.pageSize);
                    updateSortingIndicator();
                } else {
                    $('#VoucherEntry-body').html('<tr><td colspan="8" class="text-center py-3 text-danger">Failed to load vouchers.</td></tr>');
                }
            },
            error: function (xhr, status) {
                if (status === 'abort') return;
                $('#VoucherEntry-body').html('<tr><td colspan="8" class="text-center py-3 text-danger">Server communication error.</td></tr>');
            }
        });
    }

    function updateKpiCards(summary) {
        if (!summary) return;

        $('#kpiTotalVouchers').text(summary.totalVouchers || 0);
        $('#kpiVouchersToday').text(summary.vouchersToday || 0);
        $('#kpiVouchersMonth').text(summary.vouchersThisMonth || 0);
        $('#kpiTotalAmount').text('৳ ' + formatCurrency(summary.totalDebit) + ' ✓');
        $('#kpiCashBalance').text('৳ ' + formatCurrency(summary.cashBalance));
        $('#kpiBankBalance').text('৳ ' + formatCurrency(summary.bankBalance));

        // Update table footer total row
        $('#vchGridTotalAmount').text('৳ ' + formatCurrency(summary.totalDebit));

        $('#cardBrvCount').text(summary.brvCount || 0);
        $('#cardBrvAmount').text('৳ ' + formatCurrency(summary.brvAmount));

        $('#cardBpvCount').text(summary.bpvCount || 0);
        $('#cardBpvAmount').text('৳ ' + formatCurrency(summary.bpvAmount));

        $('#cardCrvCount').text(summary.crvCount || 0);
        $('#cardCrvAmount').text('৳ ' + formatCurrency(summary.crvAmount));

        $('#cardCpvCount').text(summary.cpvCount || 0);
        $('#cardCpvAmount').text('৳ ' + formatCurrency(summary.cpvAmount));

        $('#cardJvCount').text(summary.jvCount || 0);
        $('#cardJvAmount').text('৳ ' + formatCurrency(summary.jvAmount));

        $('#cardCvCount').text(summary.cvCount || 0);
        $('#cardCvAmount').text('৳ ' + formatCurrency(summary.cvAmount));
    }

    function renderTableRows(items) {
        const tbody = $('#VoucherEntry-body');
        tbody.empty();

        if (!items || items.length === 0) {
            tbody.html('<tr><td colspan="8" class="text-center py-4 text-muted">No vouchers match the filters.</td></tr>');
            return;
        }

        items.forEach(function (v) {
            const isApproved = (v.isApproved === 'Posted' || v.isApproved === 'Approved' || v.isApproved === 'Y');
            const statClass = isApproved ? 'st-Approved' : 'st-Pending';
            const statText = isApproved ? 'Approved' : 'Pending';

            const typeDisplay = TYPE_NAMES[v.voucherType_Code] || v.voucherType_Code || v.voucherTypeName || '';
            // Post button hidden as requested
            const postBtn = '';

            let dateDisplay = v.voucherDateFormatted || v.VoucherDateFormatted || '';
            if (!dateDisplay && (v.voucherDate || v.VoucherDate)) {
                const rawDate = v.voucherDate || v.VoucherDate;
                if (rawDate instanceof Date) {
                    const dd = String(rawDate.getDate()).padStart(2, '0');
                    const mm = String(rawDate.getMonth() + 1).padStart(2, '0');
                    const yyyy = rawDate.getFullYear();
                    dateDisplay = `${dd}/${mm}/${yyyy}`;
                } else if (typeof rawDate === 'string') {
                    const clean = rawDate.split('T')[0];
                    if (clean.includes('-')) {
                        const dp = clean.split('-');
                        if (dp.length === 3 && dp[0].length === 4) {
                            dateDisplay = `${dp[2]}/${dp[1]}/${dp[0]}`;
                        } else {
                            dateDisplay = clean;
                        }
                    } else if (clean.includes('/')) {
                        dateDisplay = clean;
                    }
                }
            }

            const row = `
                <tr>
                    <td class="font-monospace fw-bold text-primary">
                        <a href="javascript:void(0);" onclick="window.voucherManager.viewVoucher('${escapeHtml(v.voucherNo)}')">${escapeHtml(v.voucherNo)}</a>
                    </td>
                    <td class="text-center">${escapeHtml(dateDisplay)}</td>
                    <td class="text-center"><span class="vch-chip t-${typeDisplay}">${typeDisplay}</span></td>
                    <td>${escapeHtml(v.branchName || 'Head Office')}</td>
                    <td class="text-truncate" style="max-width: 260px;" title="${escapeHtml(v.narration || '')}">${escapeHtml(v.narration || '')}</td>
                    <td class="text-end font-monospace fw-semibold">${formatCurrency(v.totalAmount)}</td>
                    <td class="text-center"><span class="vch-chip ${statClass}">${statText}</span></td>
                    <td class="text-center nox" style="white-space: nowrap;">
                        <button type="button" class="vch-btn-action vch-btn-view" onclick="window.voucherManager.viewVoucher('${escapeHtml(v.voucherNo)}')">View</button>
                        <button type="button" class="vch-btn-action vch-btn-edit" onclick="window.voucherManager.editVoucher('${escapeHtml(v.voucherNo)}')">Edit</button>
                        ${postBtn}
                        <button type="button" class="vch-btn-action vch-btn-del" onclick="window.voucherManager.confirmDelete('${escapeHtml(v.voucherNo)}')">Delete</button>
                    </td>
                </tr>
            `;
            tbody.append(row);
        });
    }

    function renderPagination(total, page, size) {
        const totalPages = Math.max(1, Math.ceil(total / size));
        const info = `Showing ${total ? (page - 1) * size + 1 : 0} to ${Math.min(page * size, total)} of ${total} entries`;
        $('#VoucherEntry-paginationInfo').text(info);

        const container = $('#VoucherEntry-paginationContainer');
        container.empty();

        if (totalPages <= 1) return;

        let html = `<button type="button" class="btn btn-phoenix-secondary btn-sm py-1 px-2" ${page <= 1 ? 'disabled' : ''} onclick="window.voucherManager.changePage(${page - 1})">Prev</button>`;

        let start = Math.max(1, page - 2);
        let end = Math.min(totalPages, page + 2);

        for (let i = start; i <= end; i++) {
            const activeClass = i === page ? 'btn-primary' : 'btn-phoenix-secondary';
            html += `<button type="button" class="btn ${activeClass} btn-sm py-1 px-2" onclick="window.voucherManager.changePage(${i})">${i}</button>`;
        }

        html += `<button type="button" class="btn btn-phoenix-secondary btn-sm py-1 px-2" ${page >= totalPages ? 'disabled' : ''} onclick="window.voucherManager.changePage(${page + 1})">Next</button>`;
        container.html(html);
    }

    // Dynamic Single-Arrow Sorting Indicator for all columns (only ONE single arrow on active column, never double arrows)
    function updateSortingIndicator() {
        $('#VoucherEntry-table th[data-sort]').each(function () {
            const $th = $(this);
            const column = ($th.data('sort') || '').toLowerCase();
            
            // Remove any existing sort icons, layout.js injected icons, or stray fa-sort elements
            $th.find('.sort-icon, .vch-sort-icon, i.fa-sort, i.fa-sort-up, i.fa-sort-down, i.fas.fa-sort, i[class*="fa-sort"]').remove();

            if (column === currentSortColumn.toLowerCase()) {
                const isAsc = currentSortOrder.toLowerCase() === 'asc';
                const iconClass = isAsc ? 'fa-arrow-up' : 'fa-arrow-down';
                const titleText = isAsc ? 'Ascending (click to sort descending)' : 'Descending (click to sort ascending)';
                $th.append(`<span class="vch-sort-icon ms-1 text-primary" title="${titleText}"><i class="fas ${iconClass} fs-10"></i></span>`);
            } else {
                $th.append(`<span class="vch-sort-icon ms-1"></span>`);
            }
        });
    }

    /* ===== Global Action Handlers ===== */
    window.voucherManager = {
        changePage: function (p) {
            currentPage = p;
            loadTableData();
        },

        viewVoucher: function (code) {
            $.ajax({
                url: '/VoucherEntry/GetPrint',
                type: 'GET',
                data: { id: code },
                success: function (res) {
                    if (res && res.success && res.data) {
                        const v = res.data;
                        $('#printOrgName').text(v.companyName || 'FORMOSA LOGISTICS LIMITED');
                        $('#printCompanySub').text(v.companyAddress || (v.branchName ? `${v.branchName}` : '21, Hazaribagh, Dhaka-1209 Phone: Email:'));
                        $('#printVoucherKind').text((VOUCHER_TYPES[v.voucherType_Code] || v.voucherTypeName || v.voucherType_Code).toUpperCase());
                        $('#printVoucherNo').text(v.voucherNo || '');
                        let vDate = v.voucherDateFormatted || '';
                        if (!vDate && v.voucherDate) {
                            try {
                                const d = new Date(v.voucherDate);
                                if (!isNaN(d.getTime())) {
                                    const day = String(d.getDate()).padStart(2, '0');
                                    const month = String(d.getMonth() + 1).padStart(2, '0');
                                    const year = d.getFullYear();
                                    vDate = `${day}/${month}/${year}`;
                                }
                            } catch (e) {
                                vDate = (v.voucherDate || '').substring(0, 10);
                            }
                        }
                        $('#printVoucherDate').text(vDate);

                        let viewerName = (v.preparedBy || '').trim();
                        if (!viewerName || viewerName === 'Guest User' || viewerName.indexOf('@') !== -1) {
                            const domName = ($('.dropdown-profile h6').text() || $('#navbarDropdownUser').text() || '').trim();
                            if (domName && domName.indexOf('@') === -1 && domName !== 'Guest User') {
                                viewerName = domName;
                            } else {
                                viewerName = 'Md. Moklesur Rahman';
                            }
                        }
                        $('#printPreparedByName').text(viewerName);

                        const tbody = $('#printLinesBody');
                        tbody.empty();

                        (v.details || []).forEach(function (d) {
                            let lineDesc = (d.description || d.Description || '').trim();
                            const narr = (v.narration || '').trim();
                            if (lineDesc && narr && (lineDesc.toLowerCase() === narr.toLowerCase() || narr.toLowerCase().startsWith(lineDesc.toLowerCase()))) {
                                lineDesc = '';
                            }
                            const desc = lineDesc ? `${d.accName} - ${lineDesc}` : (d.accName || d.accCode);
                            const drVal = (d.debitAmount !== null && d.debitAmount !== undefined && d.debitAmount > 0) ? formatCurrency(d.debitAmount) : '0.00';
                            const crVal = (d.creditAmount !== null && d.creditAmount !== undefined && d.creditAmount > 0) ? formatCurrency(d.creditAmount) : '0.00';
                            tbody.append(`
                                <tr>
                                    <td style="border: 1px solid #888 !important; border-left: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: center; vertical-align: middle; color: #000; background: transparent !important;">${escapeHtml(d.accCode)}</td>
                                    <td style="border: 1px solid #888 !important; border-right: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: left; vertical-align: middle; color: #000; background: transparent !important;">${escapeHtml(desc)}</td>
                                    <td style="border: 1px solid #888 !important; border-left: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: right; vertical-align: middle; color: #000; background: transparent !important;">${drVal}</td>
                                    <td style="border: 1px solid #888 !important; border-right: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: right; vertical-align: middle; color: #000; background: transparent !important;">${crVal}</td>
                                </tr>
                            `);
                        });

                        $('#printTotalDebit').text(formatCurrency(v.totalDebit));
                        $('#printTotalCredit').text(formatCurrency(v.totalCredit));
                        $('#printAmountInWords').text(v.amountInWords || amountInWords(v.totalAmount));
                        $('#printNarration').text(v.narration || '');

                        const modalEl = document.getElementById('voucherPrintModal');
                        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                        modal.show();
                    } else {
                        showToast(res ? res.message : 'Error fetching voucher details.', 'error');
                    }
                }
            });
        },

        editVoucher: function (code) {
            $.ajax({
                url: '/VoucherEntry/GetById',
                type: 'GET',
                data: { id: code },
                success: function (res) {
                    if (res && res.success && res.data) {
                        const v = res.data;
                        editingVoucherCode = v.voucherEntryCodeNo;
                        isResettingForm = true;

                        $('#vCodeNo').val(v.voucherEntryCodeNo);
                        $('#formVoucherTitle').html(`<i class="fas fa-edit me-1 text-primary"></i>Edit voucher ${escapeHtml(v.voucherNo)}`);
                        $('#editVoucherBadge').show();
                        $('#vBtnSaveText').text('Update voucher');

                        let targetType = v.voucherType_Code;
                        if ($('#vType option[value="' + targetType + '"]').length === 0) {
                            const opt = $('#vType option').filter(function () {
                                return $(this).data('typename') === targetType;
                            });
                            if (opt.length > 0) {
                                targetType = opt.val();
                            }
                        }
                        setDropdownValue('vType', targetType);
                        setDropdownDisabled('vType', true);

                        const compVal = v.companyCode || v.main_CompanyCode || '1';
                        let compName = v.companyName;
                        if (!compName || compName === compVal) {
                            const match = $('#vCompany option').filter(function () {
                                return $(this).val() == compVal || $(this).data('code') == compVal || parseInt($(this).val(), 10) == parseInt(compVal, 10);
                            });
                            if (match.length) compName = match.text();
                        }
                        setDropdownValue('vCompany', compVal, compName);

                        const branchVal = v.branchCode || '1';
                        let brName = v.branchName;
                        if (!brName || brName === branchVal) {
                            const match = $('#vBranch option').filter(function () {
                                return $(this).val() == branchVal || $(this).data('code') == branchVal || parseInt($(this).val(), 10) == parseInt(branchVal, 10);
                            });
                            if (match.length) brName = match.text();
                        }
                        setDropdownValue('vBranch', branchVal, brName);

                        setDatePicker('vDate', v.voucherDateFormatted || v.voucherDate);
                        $('#vNo').val(v.voucherNo);
                        $('#vRef').val(v.invoiceNo || v.reference || '');
                        const statVal = (v.isApproved === 'Posted' || v.isApproved === 'Approved' || v.isApproved === 'Y') ? 'Posted' : 'Pending Approval';
                        $('#vStat').val(statVal);
                        $('#vNarr').val(v.narration || '');

                        isResettingForm = false;

                        const vTypeStr = (v.voucherType_Code || '').toString().toUpperCase();
                        const isRec = (vTypeStr === 'BRV' || vTypeStr === 'CRV' || vTypeStr === '1' || vTypeStr === '3');
                        const isPay = (vTypeStr === 'BPV' || vTypeStr === 'CPV' || vTypeStr === '2' || vTypeStr === '4');

                        lines = (v.details || []).map(function (d) {
                            let chqDateStr = '';
                            if (d.chequeDateFormatted && !d.chequeDateFormatted.includes('1900')) {
                                chqDateStr = d.chequeDateFormatted;
                            } else if (d.chequeDate) {
                                const cd = new Date(d.chequeDate);
                                if (!isNaN(cd.getTime()) && cd.getFullYear() > 1950) {
                                    const dd = String(cd.getDate()).padStart(2, '0');
                                    const mm = String(cd.getMonth() + 1).padStart(2, '0');
                                    chqDateStr = `${dd}/${mm}/${cd.getFullYear()}`;
                                }
                            }

                            const drVal = parseFloat(d.debitAmount != null ? d.debitAmount : d.DebitAmount) || 0;
                            const crVal = parseFloat(d.creditAmount != null ? d.creditAmount : d.CreditAmount) || 0;

                            let isAuto = false;
                            let autoSide = '';
                            if (isRec && (d.isCash || d.isBank || drVal > 0) && drVal > 0) {
                                isAuto = true;
                                autoSide = 'Dr';
                            } else if (isPay && (d.isCash || d.isBank || crVal > 0) && crVal > 0) {
                                isAuto = true;
                                autoSide = 'Cr';
                            }

                            let lineDesc = (d.description || d.Description || '').trim();
                            const narr = (v.narration || '').trim();
                            if (lineDesc && narr && (lineDesc.toLowerCase() === narr.toLowerCase() || narr.toLowerCase().startsWith(lineDesc.toLowerCase()))) {
                                lineDesc = '';
                            }

                            let accTitle = (d.accName || d.AccName || '').trim();
                            if (!accTitle || accTitle === d.accCode) {
                                if (lineDesc) accTitle = lineDesc;
                            }

                            return {
                                accCode: (d.accCode || d.AccCode || '').trim(),
                                accName: accTitle,
                                desc: lineDesc,
                                chq: d.chequeNo || d.ChequeNo || '',
                                chqd: chqDateStr,
                                dr: drVal,
                                cr: crVal,
                                isAuto: isAuto,
                                autoSide: autoSide
                            };
                        });

                        renderLines(true);

                        $('html, body').animate({
                            scrollTop: $('#vchFormCard').offset().top - 80
                        }, 400);
                    } else {
                        showToast(res ? res.message : 'Error fetching voucher for edit.', 'error');
                    }
                }
            });
        },

        confirmDelete: function (code) {
            pendingAction = { type: 'delete', code: code };
            $('#voucherConfirmTitle').text('Confirm Delete');
            $('#voucherConfirmMessage').html(`Are you sure you want to delete voucher <b>${escapeHtml(code)}</b>?`);
            $('#btnConfirmAction').removeClass('btn-success').addClass('btn-danger').text('Yes, Delete');

            const modalEl = document.getElementById('voucherConfirmModal');
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        },

        confirmPost: function (code) {
            pendingAction = { type: 'post', code: code };
            $('#voucherConfirmTitle').text('Confirm Approval');
            $('#voucherConfirmMessage').html(`Are you sure you want to approve and post voucher <b>${escapeHtml(code)}</b>?`);
            $('#btnConfirmAction').removeClass('btn-danger').addClass('btn-success').text('Yes, Approve');

            const modalEl = document.getElementById('voucherConfirmModal');
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.show();
        },

        printVoucher: function (code) {
            if (!code) return;
            const currentModalNo = $('#printVoucherNo').text().trim();
            const modalEl = document.getElementById('voucherPrintModal');
            const isModalOpen = modalEl && modalEl.classList.contains('show');

            if (isModalOpen && currentModalNo === code) {
                printModalVoucher();
            } else {
                window.voucherManager.viewVoucher(code);
                setTimeout(function () {
                    printModalVoucher();
                }, 500);
            }
        }
    };

    function getFormattedCurrentDateTime() {
        const now = new Date();
        const day = String(now.getDate()).padStart(2, '0');
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const year = now.getFullYear();
        let hours = now.getHours();
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        hours = hours ? hours : 12;
        const hoursStr = String(hours).padStart(2, '0');
        return `${day}/${month}/${year} ${hoursStr}:${minutes} ${ampm}`;
    }

    function printModalVoucher() {
        const paper = document.getElementById('voucherPaperContent');
        if (!paper) return;

        let iframe = document.getElementById('voucherPrintIframe');
        if (!iframe) {
            iframe = document.createElement('iframe');
            iframe.id = 'voucherPrintIframe';
            // Use real geometry off-screen so Chromium/Edge initializes media_size and destination capabilities without crashing
            iframe.style.position = 'fixed';
            iframe.style.left = '-9999px';
            iframe.style.top = '0';
            iframe.style.width = '1024px';
            iframe.style.height = '768px';
            iframe.style.border = '0';
            iframe.style.opacity = '0';
            iframe.style.pointerEvents = 'none';
            document.body.appendChild(iframe);
        } else {
            iframe.style.position = 'fixed';
            iframe.style.left = '-9999px';
            iframe.style.top = '0';
            iframe.style.width = '1024px';
            iframe.style.height = '768px';
            iframe.style.border = '0';
            iframe.style.opacity = '0';
            iframe.style.pointerEvents = 'none';
        }

        const doc = iframe.contentWindow.document;
        doc.open();
        doc.write(`<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title></title>
    <style>
        * {
            box-sizing: border-box !important;
            margin: 0;
            padding: 0;
        }
        @page {
            size: A4 portrait;
            margin: 0 !important; /* Suppresses Edge/Chrome default title and date/time headers */
        }
        html, body {
            background: #ffffff !important;
            color: #000000 !important;
            font-family: 'Times New Roman', Times, serif, Arial;
            margin: 0 !important;
            padding: 0 !important;
            width: 210mm !important;
            height: auto !important;
            min-height: auto !important;
            box-sizing: border-box !important;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        .print-sheet {
            position: relative !important;
            width: 210mm !important;
            max-width: 210mm !important;
            min-height: auto !important;
            height: auto !important;
            margin: 0 auto !important;
            padding: 16mm 20mm 20mm 20mm !important;
            box-sizing: border-box !important;
            background: #ffffff !important;
        }
        .voucher-content-top {
            width: 100% !important;
            box-sizing: border-box !important;
        }
        .voucher-bottom-block {
            position: static !important;
            width: 100% !important;
            margin-top: 130px !important;
            padding: 0 !important;
            box-sizing: border-box !important;
            page-break-inside: avoid !important;
            break-inside: avoid !important;
        }
        .voucher-signatures-bottom {
            width: 100% !important;
        }
        .voucher-meta-table,
        .voucher-meta-table tr,
        .voucher-meta-table td {
            border: none !important;
            border-style: none !important;
            outline: none !important;
            box-shadow: none !important;
            background: transparent !important;
        }
        #printLinesTable {
            border-collapse: collapse !important;
            table-layout: fixed !important;
            width: 100% !important;
            border: 1.5px solid #888 !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
            box-sizing: border-box !important;
        }
        #printLinesTable th,
        #printLinesTable td {
            border: 1px solid #888 !important;
            background: transparent !important;
            background-color: transparent !important;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        #printLinesTable thead th {
            border-bottom: 1.5px solid #888 !important;
        }
        #printLinesTable tbody tr:first-child td {
            border-top: 1.5px solid #888 !important;
        }
        #printLinesTable th:first-child,
        #printLinesTable tbody td:first-child {
            border-left: 1.5px solid #888 !important;
        }
        #printLinesTable th:nth-child(2),
        #printLinesTable tbody td:nth-child(2) {
            border-right: 1.5px solid #888 !important;
        }
        #printLinesTable th:nth-child(3),
        #printLinesTable tbody td:nth-child(3) {
            border-left: 1.5px solid #888 !important;
        }
        #printLinesTable th:last-child,
        #printLinesTable tbody td:last-child {
            border-right: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td {
            border-top: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:first-child {
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:nth-child(2) {
            border-left: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:last-child {
            border-right: 1.5px solid #888 !important;
        }
        .voucher-attached-box,
        div[style*="border"] {
            border: 1.5px solid #888 !important;
            border-top: none !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
            border-bottom: 1.5px solid #888 !important;
        }
    </style>
</head>
<body>
    <div class="print-sheet">
        ${paper.innerHTML}
    </div>
</body>
</html>`);
        doc.close();

        setTimeout(function () {
            try {
                iframe.contentWindow.focus();
                iframe.contentWindow.print();
            } catch (e) {
                console.error('Print iframe error:', e);
                window.print();
            }
        }, 350);
    }

    function executeDelete(code) {
        $.ajax({
            url: '/VoucherEntry/Delete?id=' + encodeURIComponent(code),
            type: 'POST',
            data: { id: code },
            success: function (res) {
                if (res && res.success) {
                    showToast(res.message || 'Voucher deleted successfully.');
                    loadTableData();
                } else {
                    showToast(res ? res.message : 'Failed to delete voucher.', 'error');
                }
            },
            error: function (xhr) {
                const msg = (xhr.responseJSON && xhr.responseJSON.message) || xhr.statusText || 'Failed to connect to server.';
                showToast(msg, 'error');
            }
        });
    }

    function executePost(code) {
        $.ajax({
            url: '/VoucherEntry/Post?id=' + encodeURIComponent(code),
            type: 'POST',
            data: { id: code },
            success: function (res) {
                if (res && res.success) {
                    showToast(res.message || 'Voucher approved and posted.');
                    loadTableData();
                } else {
                    showToast(res ? res.message : 'Failed to post voucher.', 'error');
                }
            },
            error: function (xhr) {
                const msg = (xhr.responseJSON && xhr.responseJSON.message) || xhr.statusText || 'Failed to connect to server.';
                showToast(msg, 'error');
            }
        });
    }

    function getExportParams() {
        const params = new URLSearchParams({
            voucherType: $('#vfType').val() || '',
            status: $('#vfStat').val() || '',
            searchTerm: ($('#VoucherEntry-searchInput').val() || $('#vfQ').val() || '').trim()
        });

        const fromVal = getDateInputValue('vfFrom');
        if (fromVal) params.append('fromDate', fromVal);

        const toVal = getDateInputValue('vfTo');
        if (toVal) params.append('toDate', toVal);

        return params;
    }

    function exportExcel() {
        const params = getExportParams();
        window.location.href = `/VoucherEntry/ExportExcel?${params.toString()}`;
    }

    function exportPdf() {
        const params = getExportParams();
        window.location.href = `/VoucherEntry/ExportPdf?${params.toString()}`;
    }

    function printVoucherList() {
        const params = getExportParams();
        params.append('preview', 'true');
        window.open(`/VoucherEntry/ExportPdf?${params.toString()}`, '_blank');
    }

})(jQuery);
