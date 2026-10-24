/**
 * GCTL ERP - Freight Forwarding & C&F Module
 * Bill Adjustment Entry Page Controller
 */
(function ($) {
    'use strict';

    // State Variables
    let selectedBillNo = '';
    let isSaving = false;
    let billsDataTable = null;

    // DOM Elements Cache
    const DOM = {
        // Form & Header
        form: $('#formBillAdjustment'),
        csrfToken: $('input[name="__RequestVerificationToken"]'),
        
        // 1. Adjustment & Master Information
        txtAdjustmentNo: $('#txtAdjustmentNo'),
        ddlAdjustmentType: $('#ddlAdjustmentType'),
        txtAdjustmentDate: $('#txtAdjustmentDate'),
        txtBillNoPrefix: $('#txtBillNoPrefix'),
        txtBillNo: $('#txtBillNo'),
        txtBillDate: $('#txtBillDate'),
        txtJobNo: $('#txtJobNo'),
        txtJobDate: $('#txtJobDate'),
        txtBranch: $('#txtBranch'),
        txtServiceType: $('#txtServiceType'),

        hdnCustomerId: $('#hdnCustomerId'),
        txtCustomerName: $('#txtCustomerName'),
        txtCustomerAddress: $('#txtCustomerAddress'),
        txtCustomerDeliveryAddress: $('#txtCustomerDeliveryAddress'),
        txtSupplierName: $('#txtSupplierName'),
        txtPortName: $('#txtPortName'),

        txtDocRecvDate: $('#txtDocRecvDate'),
        txtLcNo: $('#txtLcNo'),
        txtLcValue: $('#txtLcValue'),
        txtLcCurrency: $('#txtLcCurrency'),
        txtLcValueLeft: $('#txtLcValueLeft'),
        txtBdtRate: $('#txtBdtRate'),
        txtNumberOfTransport: $('#txtNumberOfTransport'),
        txtDemerageDays: $('#txtDemerageDays'),
        txtContainerNo: $('#txtContainerNo'),

        txtIpNo: $('#txtIpNo'),
        txtIpDate: $('#txtIpDate'),
        txtUndertakingNo: $('#txtUndertakingNo'),
        txtArrivalDate: $('#txtArrivalDate'),
        txtInvNo: $('#txtInvNo'),
        txtInvDate: $('#txtInvDate'),
        txtBlNo: $('#txtBlNo'),
        txtBlDate: $('#txtBlDate'),
        txtBeNo: $('#txtBeNo'),
        txtBeDate: $('#txtBeDate'),
        txtEfrNo: $('#txtEfrNo'),
        txtEfrDate: $('#txtEfrDate'),
        txtRotNo: $('#txtRotNo'),
        txtPoNo: $('#txtPoNo'),

        // 2. Cargo & Transit Information
        txtMaterialDescription: $('#txtMaterialDescription'),
        txtQuantity: $('#txtQuantity'),
        txtQUnit: $('#txtQUnit'),
        txtWeight: $('#txtWeight'),
        txtWUnit: $('#txtWUnit'),
        txtFwdr: $('#txtFwdr'),
        txtVesselNo: $('#txtVesselNo'),
        txtEtaDate: $('#txtEtaDate'),
        txtAtaDate: $('#txtAtaDate'),
        txtEtbDate: $('#txtEtbDate'),
        txtAtbDate: $('#txtAtbDate'),
        txtUnStuffingDate: $('#txtUnStuffingDate'),
        txtDateOfClearance: $('#txtDateOfClearance'),
        txtConsoleNo: $('#txtConsoleNo'),
        txtDestination: $('#txtDestination'),
        txtRemarks: $('#txtRemarks'),

        // 3. Financial, VAT & Commission
        txtAdvanceAmount: $('#txtAdvanceAmount'),
        txtItc: $('#txtItc'),
        txtCv: $('#txtCv'),
        txtVatTransport: $('#txtVatTransport'),
        txtVatCf: $('#txtVatCf'),
        txtVatTransportAdjust: $('#txtVatTransportAdjust'),
        txtVatCfAdjust: $('#txtVatCfAdjust'),
        txtGrossAmount: $('#txtGrossAmount'),
        txtMinAgComm: $('#txtMinAgComm'),
        txtOnInvoiceValue: $('#txtOnInvoiceValue'),
        txtAgCommAmount: $('#txtAgCommAmount'),
        txtTotalCfBillAmount: $('#txtTotalCfBillAmount'),
        txtNetAmount: $('#txtNetAmount'),

        // 4. Notes & Dates
        txtNotesReasonForCredit: $('#txtNotesReasonForCredit'),
        lblCreationDate: $('#lblCreationDate'),
        lblLastUpdateDate: $('#lblLastUpdateDate'),

        // 5. CHC Head Grid & Quick Add
        ddlChcHead: $('#ddlChcHead'),
        txtChcAmount: $('#txtChcAmount'),
        btnAddChc: $('#btnAddChc'),
        tblChcBody: $('#tblChcGrid tbody'),
        lblTotalChcAmount: $('#lblTotalChcAmount'),

        // 6. Expenses in Detail Grid
        tblExpenseBody: $('#tblExpenseGrid tbody'),
        lblTotalEstimated: $('#lblTotalEstimated'),
        lblTotalActual: $('#lblTotalActual'),
        lblTotalDifference: $('#lblTotalDifference'),

        // 7. Bottom Grid & Filters
        tblApprovedBills: $('#tblApprovedBills'),
        tblApprovedBillsBody: $('#tblApprovedBills tbody'),
        dtpFromDate: $('#dtpFromDate'),
        dtpToDate: $('#dtpToDate'),
        txtBillSearch: $('#txtBillSearch'),

        // 8. Action Buttons
        btnSave: $('#btnSave'),
        btnDelete: $('#btnDelete'),
        btnClear: $('#btnClear'),
        btnPrintBill: $('#btnPrintBill'),
        btnPrintJournal: $('#btnPrintJournal'),
        btnFav: $('#btnFav'),
        btnAdjustBottom: $('#btnAdjustBottom'),
        btnBillBottom: $('#btnBillBottom'),
        btnJournalBottom: $('#btnJournalBottom'),
        btnClearBottom: $('#btnClearBottom')
    };

    // =========================================================================
    // 1. Initialization & Date Helpers
    // =========================================================================
    $(document).ready(function () {
        initDatePickers();
        fetchNextAdjustmentNo(DOM.ddlAdjustmentType.val() || 'Credit Note');
        loadChcHeadDropdown();
        loadApprovedBills();
        bindEvents();
    });

    // Financial year in Bangladesh: July 1st to June 30th
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

    // Flatpickr Footer Helper (Today & Clear Buttons inside calendar popup)
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

    function initDatePickers() {
        if (!window.flatpickr) return;

        // A. Header Adjustment Date
        if (DOM.txtAdjustmentDate.length) {
            if (DOM.txtAdjustmentDate[0]._flatpickr) DOM.txtAdjustmentDate[0]._flatpickr.destroy();
            flatpickr(DOM.txtAdjustmentDate[0], {
                dateFormat: 'Y-m-d',
                altInput: true,
                altFormat: 'd/m/Y',
                allowInput: true,
                defaultDate: getTodayDate(),
                onReady: function (selectedDates, dateStr, instance) {
                    if (instance.altInput) {
                        instance.altInput.placeholder = 'dd/mm/yyyy';
                        instance.altInput.classList.add('vch-date-picker');
                    }
                    addCalendarTodayClearButtons(instance);
                },
                onOpen: function (selectedDates, dateStr, instance) {
                    addCalendarTodayClearButtons(instance);
                }
            });
        }

        // B. From Date (Financial Year Start: July 1st)
        if (DOM.dtpFromDate.length) {
            if (DOM.dtpFromDate[0]._flatpickr) DOM.dtpFromDate[0]._flatpickr.destroy();
            const fpFrom = flatpickr(DOM.dtpFromDate[0], {
                dateFormat: 'Y-m-d',
                altInput: true,
                altFormat: 'd/m/Y',
                allowInput: true,
                defaultDate: getFiscalYearStartDate(),
                onReady: function (selectedDates, dateStr, instance) {
                    if (instance.altInput) {
                        instance.altInput.placeholder = 'dd/mm/yyyy';
                        instance.altInput.classList.add('vch-date-picker');
                    }
                    addCalendarTodayClearButtons(instance);
                },
                onOpen: function (selectedDates, dateStr, instance) {
                    addCalendarTodayClearButtons(instance);
                },
                onChange: function () {
                    loadApprovedBills();
                }
            });

            if (fpFrom && fpFrom.altInput) {
                $(fpFrom.altInput).off('change keydown').on('change', function () {
                    loadApprovedBills();
                }).on('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        loadApprovedBills();
                    }
                });
            }
        }

        // C. To Date (Current Date: Today)
        if (DOM.dtpToDate.length) {
            if (DOM.dtpToDate[0]._flatpickr) DOM.dtpToDate[0]._flatpickr.destroy();
            const fpTo = flatpickr(DOM.dtpToDate[0], {
                dateFormat: 'Y-m-d',
                altInput: true,
                altFormat: 'd/m/Y',
                allowInput: true,
                defaultDate: getTodayDate(),
                onReady: function (selectedDates, dateStr, instance) {
                    if (instance.altInput) {
                        instance.altInput.placeholder = 'dd/mm/yyyy';
                        instance.altInput.classList.add('vch-date-picker');
                    }
                    addCalendarTodayClearButtons(instance);
                },
                onOpen: function (selectedDates, dateStr, instance) {
                    addCalendarTodayClearButtons(instance);
                },
                onChange: function () {
                    loadApprovedBills();
                }
            });

            if (fpTo && fpTo.altInput) {
                $(fpTo.altInput).off('change keydown').on('change', function () {
                    loadApprovedBills();
                }).on('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        loadApprovedBills();
                    }
                });
            }
        }
    }

    // Next Adjustment No Auto-generator
    function fetchNextAdjustmentNo(adjType) {
        $.ajax({
            url: '/BillAdjustment/GetNextAdjustmentNo',
            type: 'GET',
            data: { adjustmentType: adjType },
            success: function (res) {
                if (res && res.success && res.adjustmentNo) {
                    DOM.txtAdjustmentNo.val(res.adjustmentNo);
                }
            }
        });
    }

    // Load CHC Head dropdown from database
    function loadChcHeadDropdown() {
        $.ajax({
            url: '/BillAdjustment/GetChcHeads',
            type: 'GET',
            success: function (res) {
                if (res && res.success && Array.isArray(res.data)) {
                    DOM.ddlChcHead.empty().append('<option value="">Select Head</option>');
                    res.data.forEach(function (item) {
                        DOM.ddlChcHead.append(
                            $('<option></option>')
                                .val(item.chcId)
                                .text(item.chcHead)
                                .attr('data-desc', item.description || '')
                                .attr('data-rcpt', item.isReceiptable ? 'true' : 'false')
                        );
                    });

                    // Reinitialize custom-select-single for dynamic options
                    if (typeof window.reinitCustomDropdownById === 'function') {
                        window.reinitCustomDropdownById('ddlChcHead');
                    } else if (typeof window.initCustomDropdowns === 'function') {
                        window.initCustomDropdowns(DOM.ddlChcHead.parent(), true);
                    }
                }
            },
            error: function () {
                console.error('Failed to load CHC Heads.');
            }
        });
    }

    function bindEvents() {
        // A. Adjustment Type change updates Adjustment No automatically
        DOM.ddlAdjustmentType.on('change', function () {
            const adjType = $(this).val();
            fetchNextAdjustmentNo(adjType);
        });

        // B. Bottom Grid Filters: Auto reload on date change
        DOM.dtpFromDate.on('change', function () {
            loadApprovedBills();
        });

        DOM.dtpToDate.on('change', function () {
            loadApprovedBills();
        });

        // Quick Search: instant client filter when DataTable is active, or server reload as fallback
        DOM.txtBillSearch.on('input keyup', function () {
            const term = $(this).val();
            if (billsDataTable) {
                billsDataTable.search(term).draw();
            }
        });

        // C. Row Selection from Bottom Grid (works across all pages of DataTable)
        $(document).on('click', '#tblApprovedBills tbody tr', function (e) {
            if ($(e.target).is('button, a, input[type="checkbox"]')) {
                // If checkbox was directly clicked, don't interfere
            }
            const billNo = $(this).data('billno') || $(this).find('td:nth-child(2)').text().trim();
            if (!billNo || billNo.includes('No approved') || billNo.includes('Loading')) return;

            $('#tblApprovedBills tbody tr').removeClass('table-active');
            $('#tblApprovedBills tbody input[type="checkbox"]').prop('checked', false);

            $(this).addClass('table-active');
            $(this).find('input[type="checkbox"]').prop('checked', true);

            loadBillDetails(billNo);
        });

        // D. Dynamic Calculation in Grid 1 (Custom Handling Charges)
        DOM.tblChcBody.on('input change', '.chc-amount', function () {
            calculateChcTotals();
        });

        // E. Quick Add CHC Head Row
        DOM.btnAddChc.on('click', function () {
            const headVal = DOM.ddlChcHead.val();
            if (!headVal) {
                showToast('Please select a CHC Head.', 'warning');
                return;
            }

            const $selectedOpt = DOM.ddlChcHead.find(`option[value="${headVal}"]`);
            const headName = $selectedOpt.text() || DOM.ddlChcHead.find('option:selected').text();
            const defaultDesc = $selectedOpt.attr('data-desc') || '';
            const isRcpt = $selectedOpt.attr('data-rcpt') !== 'false';
            const amount = parseFloat(DOM.txtChcAmount.val()) || 0;

            if (amount <= 0) {
                showToast('Please enter a valid amount.', 'warning');
                return;
            }

            const rowCount = DOM.tblChcBody.find('tr[data-chcid]').length + 1;
            const newRow = `
                <tr data-chcid="${escapeHtml(headVal)}">
                    <td class="text-center align-middle chc-sl">${rowCount}</td>
                    <td class="text-center align-middle"><input type="text" class="form-control form-control-sm chc-id bg-light text-center" value="${escapeHtml(headVal)}" readonly /></td>
                    <td class="align-middle"><input type="text" class="form-control form-control-sm chc-head bg-light text-start" value="${escapeHtml(headName)}" readonly /></td>
                    <td class="align-middle"><input type="text" class="form-control form-control-sm chc-desc text-start" value="${escapeHtml(defaultDesc)}" placeholder="Enter Description" /></td>
                    <td class="text-center align-middle">
                        <input type="number" step="0.01" min="0" class="form-control form-control-sm text-end chc-amount font-weight-bold" value="${amount}" />
                    </td>
                    <td class="text-center align-middle">
                        <input type="checkbox" class="chc-receiptable" ${isRcpt ? 'checked' : ''} />
                    </td>
                    <td class="text-center align-middle">
                        <button type="button" class="btn btn-outline-danger btn-sm btn-delete-chc py-0 px-2 d-inline-flex align-items-center justify-content-center gap-1" title="Delete Row" style="font-size: 11px; height: 26px;">
                            <i class="fas fa-trash-alt"></i>
                        </button>
                    </td>
                </tr>`;

            if (DOM.tblChcBody.find('tr[data-chcid]').length === 0) {
                DOM.tblChcBody.html(newRow);
            } else {
                DOM.tblChcBody.append(newRow);
            }

            calculateChcTotals();
            DOM.txtChcAmount.val('');

            if (typeof window.clearCustomDropdownById === 'function') {
                window.clearCustomDropdownById('ddlChcHead');
            } else {
                DOM.ddlChcHead.val('').trigger('change');
            }
            showToast(`Added charge '${headName}'`, 'info');
        });

        // E.2 Delete CHC Head Row
        DOM.tblChcBody.on('click', '.btn-delete-chc', function (e) {
            e.preventDefault();
            const $row = $(this).closest('tr');
            const headName = $row.find('.chc-head').val() || $row.attr('data-chcid');
            $row.fadeOut(150, function () {
                $(this).remove();
                reindexChcRows();
                calculateChcTotals();
                showToast(`Removed charge '${headName}'`, 'info');
            });
        });

        // F. Dynamic Calculation in Grid 2 (Expenses in Detail)
        DOM.tblExpenseBody.on('input change', '.exp-actual', function () {
            const $row = $(this).closest('tr');
            const estimated = parseFloat($row.find('.exp-estimated').val()) || 0;
            const actual = parseFloat($(this).val()) || 0;
            const diff = actual - estimated;

            const $diffCell = $row.find('.exp-diff');
            $diffCell.text(formatNumber(diff));

            if (diff > 0) {
                $diffCell.removeClass('text-success').addClass('text-danger font-weight-bold');
            } else if (diff < 0) {
                $diffCell.removeClass('text-danger').addClass('text-success font-weight-bold');
            } else {
                $diffCell.removeClass('text-danger text-success font-weight-normal');
            }

            calculateExpenseTotals();
        });

        // G. Form Submission (Save & Adjust)
        DOM.btnSave.add(DOM.btnAdjustBottom).on('click', function (e) {
            e.preventDefault();
            handleSaveAdjustment();
        });

        // H. Delete Button
        DOM.btnDelete.on('click', function (e) {
            e.preventDefault();
            const adjNo = DOM.txtAdjustmentNo.val().trim();
            if (!adjNo) {
                showToast('No adjustment record selected to delete.', 'warning');
                return;
            }
            if (confirm(`Are you sure you want to delete adjustment: ${adjNo}?`)) {
                showToast(`Delete request for ${adjNo} noted.`, 'info');
            }
        });

        // I. Clear Form
        DOM.btnClear.add(DOM.btnClearBottom).on('click', function (e) {
            e.preventDefault();
            resetForm();
        });

        // J. Bill Report Button
        DOM.btnPrintBill.add(DOM.btnBillBottom).on('click', function (e) {
            e.preventDefault();
            const billNo = DOM.txtBillNo.val().trim();
            if (!billNo) {
                showToast('Please select a bill first to view Bill report.', 'warning');
                return;
            }
            showToast(`Opening Bill Report for ${billNo}...`, 'info');
        });

        // K. Journal Report Button
        DOM.btnPrintJournal.add(DOM.btnJournalBottom).on('click', function (e) {
            e.preventDefault();
            const adjNo = DOM.txtAdjustmentNo.val().trim();
            if (!adjNo) {
                showToast('No adjustment number generated yet.', 'warning');
                return;
            }
            showToast(`Opening Journal Voucher for ${adjNo}...`, 'info');
        });

        // L. Favorite Button Toggle
        DOM.btnFav.on('click', function (e) {
            e.preventDefault();
            $(this).toggleClass('active');
            showToast('Page added to favorites.', 'info');
        });
    }

    // =========================================================================
    // 2. Load Bottom Approved Bills Grid with DataTable Pagination
    // =========================================================================
    function loadApprovedBills() {
        const fromDate = DOM.dtpFromDate.val();
        const toDate = DOM.dtpToDate.val();
        const search = DOM.txtBillSearch.val();

        if (billsDataTable) {
            billsDataTable.destroy();
            billsDataTable = null;
        }

        DOM.tblApprovedBillsBody.html(`<tr><td colspan="11" class="text-center py-3"><span class="spinner-border spinner-border-sm me-1 text-primary"></span> Loading approved bills...</td></tr>`);

        $.ajax({
            url: '/BillAdjustment/GetApprovedBills',
            type: 'GET',
            data: { dateFrom: fromDate, dateTo: toDate, searchTerm: search },
            success: function (res) {
                if (!res.success) {
                    showToast(res.message || 'Error loading bills', 'error');
                    DOM.tblApprovedBillsBody.html(`<tr><td colspan="11" class="text-center text-danger py-2">Failed to load data.</td></tr>`);
                    return;
                }

                const bills = res.data || [];
                if (bills.length === 0) {
                    DOM.tblApprovedBillsBody.html(`<tr><td colspan="11" class="text-center text-muted py-2">No approved bills found.</td></tr>`);
                    return;
                }

                let html = '';
                bills.forEach(function (b) {
                    const isSelected = (b.billNo === selectedBillNo) ? 'table-active' : '';
                    const checked = (b.billNo === selectedBillNo) ? 'checked' : '';

                    // 1. Dynamic Voucher No
                    const voucherDisplay = b.voucherNo 
                        ? `<span class="badge bg-light text-primary border font-monospace">${escapeHtml(b.voucherNo)}</span>` 
                        : `<span class="text-muted">-</span>`;

                    // 2. Dynamic Adjustment Type
                    let adjTypeBadge = `<span class="text-muted">-</span>`;
                    if (b.adjustmentType) {
                        const isDebit = b.adjustmentType.toLowerCase().includes('debit');
                        const badgeClass = isDebit ? 'badge bg-warning-subtle text-dark border' : 'badge bg-info-subtle text-dark border';
                        adjTypeBadge = `<span class="${badgeClass}">${escapeHtml(b.adjustmentType)}</span>`;
                    }

                    // 3. Dynamic Adjustment No
                    const adjNoDisplay = b.adjustmentNo 
                        ? `<span class="fw-bold text-dark font-monospace">${escapeHtml(b.adjustmentNo)}</span>` 
                        : `<span class="text-muted">-</span>`;

                    // 4. Dynamic Adjustment Date
                    const adjDateDisplay = b.adjustmentDate 
                        ? formatDate(b.adjustmentDate) 
                        : `<span class="text-muted">-</span>`;

                    html += `
                        <tr class="clickable-row ${isSelected}" data-billno="${escapeHtml(b.billNo)}" style="cursor: pointer;">
                            <td class="text-center align-middle"><input type="checkbox" class="form-check-input" ${checked} /></td>
                            <td class="text-center align-middle font-weight-bold text-primary">${escapeHtml(b.billNo)}</td>
                            <td class="text-center align-middle">${formatDate(b.billDate)}</td>
                            <td class="text-center align-middle">${escapeHtml(b.jobNo || '-')}</td>
                            <td class="text-center align-middle"><span class="badge bg-light text-dark border">${escapeHtml(b.serviceType || 'Sea Export (CTG)')}</span></td>
                            <td class="text-center align-middle">${escapeHtml(b.customerName || '-')}</td>
                            <td class="text-center align-middle">${formatDate(b.deliveryDate)}</td>
                            <td class="text-center align-middle">${voucherDisplay}</td>
                            <td class="text-center align-middle">${adjTypeBadge}</td>
                            <td class="text-center align-middle">${adjNoDisplay}</td>
                            <td class="text-center align-middle">${adjDateDisplay}</td>
                        </tr>`;
                });

                DOM.tblApprovedBillsBody.html(html);

                // Initialize DataTable with Pagination & Features matching Image 3
                if ($.fn.DataTable) {
                    billsDataTable = DOM.tblApprovedBills.DataTable({
                        pageLength: 10,
                        lengthMenu: [5, 10, 25, 50, 100],
                        order: [[2, 'desc']], // Default order by Bill Date desc
                        columnDefs: [
                            { orderable: false, targets: 0 } // Sel checkbox non-orderable
                        ],
                        language: {
                            search: "",
                            searchPlaceholder: "Search bills...",
                            lengthMenu: "Show _MENU_ entries",
                            info: "Showing _START_ to _END_ of _TOTAL_ entries",
                            infoEmpty: "Showing 0 to 0 of 0 entries",
                            zeroRecords: "No matching bills found",
                            paginate: {
                                first: 'First',
                                previous: 'Previous',
                                next: 'Next',
                                last: 'Last'
                            }
                        },
                        autoWidth: false,
                        destroy: true
                    });
                }
            },
            error: function () {
                DOM.tblApprovedBillsBody.html(`<tr><td colspan="11" class="text-center text-danger py-2">Error contacting server.</td></tr>`);
            }
        });
    }

    // =========================================================================
    // 3. Load Bill Details (Header + 2 Grids) by BillNo
    // =========================================================================
    function loadBillDetails(billNo) {
        selectedBillNo = billNo;
        toggleScreenLoading(true);

        $.ajax({
            url: '/BillAdjustment/GetBillDetails',
            type: 'GET',
            data: { billNo: billNo },
            success: function (res) {
                toggleScreenLoading(false);

                if (!res.success || !res.data) {
                    showToast(res.message || 'Unable to retrieve bill details.', 'error');
                    return;
                }

                const data = res.data;
                populateHeader(data.header);
                populateChcGrid(data.chcCharges);
                populateExpenseGrid(data.expenseDetails);

                showToast(`Loaded details for Bill: ${billNo}`, 'info');
            },
            error: function () {
                toggleScreenLoading(false);
                showToast('Failed to load bill details due to network error.', 'error');
            }
        });
    }

    // Populate all fields in Read-Only Mode
    function populateHeader(h) {
        if (!h) return;

        // Bill & Identification
        DOM.txtBillNo.val(h.billNo || '');
        DOM.txtBillNoPrefix.val(h.billNoPrefix || 'SLSL');
        DOM.txtBillDate.val(formatDate(h.billDate));
        DOM.txtJobNo.val(h.jobNo || '');
        DOM.txtJobDate.val(formatDate(h.jobDate || h.billDate));
        DOM.txtBranch.val(h.branch || 'Main Branch');
        DOM.txtServiceType.val(h.serviceType || 'C&F');

        // Dynamic Adjustment Info
        if (h.adjustmentNo) {
            DOM.txtAdjustmentNo.val(h.adjustmentNo);
        } else {
            fetchNextAdjustmentNo(DOM.ddlAdjustmentType.val() || 'Credit Note');
        }
        if (h.adjustmentType) {
            DOM.ddlAdjustmentType.val(h.adjustmentType);
        }
        if (h.adjustmentDate) {
            const fpAdj = DOM.txtAdjustmentDate[0]?._flatpickr;
            if (fpAdj) {
                fpAdj.setDate(h.adjustmentDate, true);
            } else {
                DOM.txtAdjustmentDate.val(formatDate(h.adjustmentDate));
            }
        }

        // Customer & Port
        DOM.hdnCustomerId.val(h.customerId || '');
        DOM.txtCustomerName.val(h.customerName || '');
        DOM.txtCustomerAddress.val(h.customerAddress || '');
        DOM.txtCustomerDeliveryAddress.val(h.customerDeliveryAddress || h.customerAddress || '');
        DOM.txtSupplierName.val(h.supplierName || h.customerName || '');
        DOM.txtPortName.val(h.portName || 'Chittagong Port');

        // Documentation & L/C
        DOM.txtDocRecvDate.val(formatDate(h.docRecvDate));
        DOM.txtLcNo.val(h.lcNo || '');
        DOM.txtLcValue.val(formatNumber(h.lcValue));
        DOM.txtLcValueLeft.val(formatNumber(h.lcValue));
        DOM.txtLcCurrency.val(h.lcCurrency || 'USD');
        DOM.txtBdtRate.val(formatNumber(h.bdtRate));
        DOM.txtNumberOfTransport.val(h.numberOfTransport || 1);
        DOM.txtDemerageDays.val(h.numberOfDemerageDay || 0);
        DOM.txtContainerNo.val(h.containerNo || '');

        DOM.txtIpNo.val(h.ipNo || '');
        DOM.txtIpDate.val(formatDate(h.ipDate));
        DOM.txtUndertakingNo.val(h.undertakingNo || '');
        DOM.txtArrivalDate.val(formatDate(h.arrivalDate));
        DOM.txtInvNo.val(h.invNo || '');
        DOM.txtInvDate.val(formatDate(h.invDate));
        DOM.txtBlNo.val(h.blNo || '');
        DOM.txtBlDate.val(formatDate(h.blDate));
        DOM.txtBeNo.val(h.beNo || '');
        DOM.txtBeDate.val(formatDate(h.beDate));
        DOM.txtEfrNo.val(h.efrNo || '');
        DOM.txtEfrDate.val(formatDate(h.efrDate));
        DOM.txtRotNo.val(h.rotNo || '');
        DOM.txtPoNo.val(h.poNo || '');

        // Cargo & Transit
        DOM.txtMaterialDescription.val(h.materialDescription || '');
        DOM.txtQuantity.val(formatNumber(h.quantity));
        DOM.txtQUnit.val(h.qUnitType || 'PCS');
        DOM.txtWeight.val(formatNumber(h.docWeight));
        DOM.txtWUnit.val(h.wUnitType || 'KG');
        DOM.txtFwdr.val(h.fwdr || '');
        DOM.txtVesselNo.val(h.vesselNo || '');
        DOM.txtEtaDate.val(formatDate(h.etaDate || h.arrivalDate));
        DOM.txtAtaDate.val(formatDate(h.ataDate || h.arrivalDate));
        DOM.txtEtbDate.val(formatDate(h.etbDate || h.arrivalDate));
        DOM.txtAtbDate.val(formatDate(h.atbDate || h.arrivalDate));
        DOM.txtUnStuffingDate.val(formatDate(h.unStuffingDate || h.deliveryDate));
        DOM.txtDateOfClearance.val(formatDate(h.dateOfClearance || h.deliveryDate));
        DOM.txtConsoleNo.val(h.consoleNo || '');
        DOM.txtDestination.val(h.destination || 'Chittagong');
        DOM.txtRemarks.val(h.remarks || '');

        // Financial & VAT
        DOM.txtAdvanceAmount.val(formatNumber(h.advanceAmount));
        DOM.txtItc.val(formatNumber(h.itc));
        DOM.txtCv.val(formatNumber(h.cv));
        DOM.txtVatTransport.val(formatNumber(h.vatCurrAccTransport));
        DOM.txtVatCf.val(formatNumber(h.vatCurrAccCf));
        DOM.txtVatTransportAdjust.val(formatNumber(h.vatCurrAccTransportAdjust));
        DOM.txtVatCfAdjust.val(formatNumber(h.vatCurrAccCfAdjust));
        DOM.txtGrossAmount.val(formatNumber(h.grossAmount));

        // Agency Commission
        DOM.txtMinAgComm.val(formatNumber(h.minimumAgencyCommission));
        DOM.txtOnInvoiceValue.val(formatNumber(h.onInvoiceValue));
        DOM.txtAgCommAmount.val(formatNumber(h.agencyCommissionAmount));

        DOM.txtTotalCfBillAmount.val(formatNumber(h.totalCfBillAmount || h.grossAmount));
        DOM.txtNetAmount.val(formatNumber(h.netAmount));

        // Notes / Reason
        DOM.txtNotesReasonForCredit.val(h.notesReasonForCredit || h.remarks || '');
        DOM.lblCreationDate.text(formatDate(h.creationDate));
        DOM.lblLastUpdateDate.text(formatDate(h.lastUpdateDate));
    }

    function reindexChcRows() {
        const $rows = DOM.tblChcBody.find('tr[data-chcid]');
        if ($rows.length === 0) {
            DOM.tblChcBody.html('<tr><td colspan="7" class="text-center text-muted py-3">Select a Bill to populate charges.</td></tr>');
        } else {
            $rows.each(function (idx) {
                $(this).find('.chc-sl').text(idx + 1);
            });
        }
    }

    function populateChcGrid(chcList) {
        DOM.tblChcBody.empty();
        chcList = chcList || [];

        if (chcList.length === 0) {
            DOM.tblChcBody.html(`<tr><td colspan="7" class="text-center text-muted py-3">No custom handling charges recorded.</td></tr>`);
            calculateChcTotals();
            return;
        }

        let rows = '';
        chcList.forEach(function (item, idx) {
            const checked = item.isReceiptable ? 'checked' : '';
            rows += `
                <tr data-chcid="${escapeHtml(item.chcId)}">
                    <td class="text-center align-middle chc-sl">${idx + 1}</td>
                    <td class="text-center align-middle"><input type="text" class="form-control form-control-sm chc-id bg-light text-center" value="${escapeHtml(item.chcId)}" readonly /></td>
                    <td class="align-middle"><input type="text" class="form-control form-control-sm chc-head bg-light text-start" value="${escapeHtml(item.billHead || '')}" readonly /></td>
                    <td class="align-middle"><input type="text" class="form-control form-control-sm chc-desc text-start" value="${escapeHtml(item.chcHeadDescription || '')}" placeholder="Enter Description" /></td>
                    <td class="text-center align-middle">
                        <input type="number" step="0.01" min="0" class="form-control form-control-sm text-end chc-amount font-weight-bold" value="${item.amount || 0}" />
                    </td>
                    <td class="text-center align-middle">
                        <input type="checkbox" class="chc-receiptable" ${checked} />
                    </td>
                    <td class="text-center align-middle">
                        <button type="button" class="btn btn-outline-danger btn-sm btn-delete-chc py-0 px-2 d-inline-flex align-items-center justify-content-center gap-1" title="Delete Row" style="font-size: 11px; height: 26px;">
                            <i class="fas fa-trash-alt"></i>
                        </button>
                    </td>
                </tr>`;
        });

        DOM.tblChcBody.html(rows);
        calculateChcTotals();
    }

    function populateExpenseGrid(expList) {
        DOM.tblExpenseBody.empty();
        expList = expList || [];

        if (expList.length === 0) {
            DOM.tblExpenseBody.html(`<tr><td colspan="6" class="text-center text-muted">No expense details found.</td></tr>`);
            calculateExpenseTotals();
            return;
        }

        let rows = '';
        expList.forEach(function (item, idx) {
            const estimated = item.estimatedAmount || 0;
            const actual = item.actualAmount || 0;
            const diff = actual - estimated;
            const checked = item.isReceiptable ? 'checked' : '';
            const headName = item.expenseHead || item.expenseHeadId || '-';

            rows += `
                <tr data-detailid="${escapeHtml(item.billDetailsId || '')}" data-headid="${escapeHtml(item.expenseHeadId || '')}">
                    <td class="text-center align-middle">${idx + 1}</td>
                    <td class="text-center align-middle">
                        <span class="exp-head-name font-weight-bold text-dark">${escapeHtml(headName)}</span>
                    </td>
                    <td class="text-center align-middle">
                        <input type="hidden" class="exp-estimated" value="${estimated}" />
                        ${formatNumber(estimated)}
                    </td>
                    <td class="text-center align-middle">
                        <input type="number" step="0.01" min="0" class="form-control form-control-sm text-center exp-actual font-weight-bold" value="${actual}" />
                    </td>
                    <td class="text-center align-middle">
                        <span class="exp-diff font-weight-bold">${formatNumber(diff)}</span>
                    </td>
                    <td class="text-center align-middle">
                        <input type="checkbox" class="exp-receiptable" ${checked} />
                    </td>
                </tr>`;
        });

        DOM.tblExpenseBody.html(rows);
        calculateExpenseTotals();
    }

    // =========================================================================
    // 4. Calculations
    // =========================================================================
    function calculateChcTotals() {
        let total = 0;
        DOM.tblChcBody.find('.chc-amount').each(function () {
            total += parseFloat($(this).val()) || 0;
        });
        DOM.lblTotalChcAmount.text(formatNumber(total));
    }

    function calculateExpenseTotals() {
        let totalEst = 0;
        let totalAct = 0;

        DOM.tblExpenseBody.find('tr').each(function () {
            const est = parseFloat($(this).find('.exp-estimated').val()) || 0;
            const act = parseFloat($(this).find('.exp-actual').val()) || 0;
            totalEst += est;
            totalAct += act;
        });

        const totalDiff = totalAct - totalEst;
        DOM.lblTotalEstimated.text(formatNumber(totalEst));
        DOM.lblTotalActual.text(formatNumber(totalAct));
        DOM.lblTotalDifference.text(formatNumber(totalDiff));
    }

    // =========================================================================
    // 5. Validation & Save Orchestration
    // =========================================================================
    function validateForm() {
        let isValid = true;
        const errors = [];

        const billNo = DOM.txtBillNo.val().trim();
        if (!billNo) {
            errors.push('Please select a Bill from the list below.');
            DOM.txtBillNo.addClass('is-invalid');
            isValid = false;
        } else {
            DOM.txtBillNo.removeClass('is-invalid');
        }

        const adjType = DOM.ddlAdjustmentType.val();
        if (!adjType) {
            errors.push('Adjustment Type (Credit Note / Debit Note) is required.');
            DOM.ddlAdjustmentType.addClass('is-invalid');
            isValid = false;
        } else {
            DOM.ddlAdjustmentType.removeClass('is-invalid');
        }

        const adjDate = DOM.txtAdjustmentDate.val();
        if (!adjDate) {
            errors.push('Adjustment Date is required.');
            DOM.txtAdjustmentDate.addClass('is-invalid');
            isValid = false;
        } else {
            DOM.txtAdjustmentDate.removeClass('is-invalid');
        }

        const chcRows = DOM.tblChcBody.find('tr[data-chcid]');
        const expRows = DOM.tblExpenseBody.find('tr[data-detailid]');
        if (chcRows.length === 0 && expRows.length === 0) {
            errors.push('At least one Custom Handling Charge or Expense row must exist.');
            isValid = false;
        }

        if (!isValid) {
            showToast(errors[0], 'warning');
        }
        return isValid;
    }

    function handleSaveAdjustment() {
        if (isSaving) return;
        if (!validateForm()) return;

        const chcDetails = [];
        DOM.tblChcBody.find('tr[data-chcid]').each(function () {
            const $row = $(this);
            const rawId = $row.attr('data-chcid') !== undefined ? $row.attr('data-chcid') : $row.data('chcid');
            const chcId = String(rawId || '').trim();
            if (!chcId) return;

            const billHead = String($row.find('.chc-head').val() || $row.find('.chc-head').text() || '').trim();
            const desc = String($row.find('.chc-desc').val() || '').trim();
            const amount = parseFloat($row.find('.chc-amount').val()) || 0;
            const isReceiptable = $row.find('.chc-receiptable').is(':checked');

            chcDetails.push({
                chcId: chcId,
                billHead: billHead,
                chcHeadDescription: desc,
                amount: amount,
                isReceiptable: isReceiptable
            });
        });

        const expenseDetails = [];
        DOM.tblExpenseBody.find('tr[data-detailid]').each(function () {
            const $row = $(this);
            const rawDetId = $row.attr('data-detailid') !== undefined ? $row.attr('data-detailid') : $row.data('detailid');
            const rawHeadId = $row.attr('data-headid') !== undefined ? $row.attr('data-headid') : $row.data('headid');
            const detId = String(rawDetId || '').trim();
            const headId = String(rawHeadId || '').trim();
            const headName = String($row.find('.exp-head-name').text() || '').trim();
            const estimated = parseFloat($row.find('.exp-estimated').val()) || 0;
            const actual = parseFloat($row.find('.exp-actual').val()) || 0;
            const diff = actual - estimated;
            const isReceiptable = $row.find('.exp-receiptable').is(':checked');

            expenseDetails.push({
                billDetailsId: detId,
                expenseHeadId: headId,
                expenseHead: headName,
                estimatedAmount: estimated,
                actualAmount: actual,
                differentAmount: diff,
                isReceiptable: isReceiptable
            });
        });

        let adjDate = null;
        const fp = DOM.txtAdjustmentDate[0]?._flatpickr;
        if (fp && fp.selectedDates && fp.selectedDates.length > 0) {
            const d = fp.selectedDates[0];
            const y = d.getFullYear();
            const m = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            adjDate = `${y}-${m}-${day}`;
        } else {
            const raw = (DOM.txtAdjustmentDate.val() || '').trim();
            if (raw.includes('/')) {
                const p = raw.split('/');
                if (p.length === 3 && p[2].length === 4) {
                    adjDate = `${p[2]}-${p[1].padStart(2, '0')}-${p[0].padStart(2, '0')}`;
                }
            } else if (raw.includes('-')) {
                adjDate = raw.split('T')[0];
            } else {
                const now = new Date();
                const y = now.getFullYear();
                const m = String(now.getMonth() + 1).padStart(2, '0');
                const day = String(now.getDate()).padStart(2, '0');
                adjDate = `${y}-${m}-${day}`;
            }
        }

        const payload = {
            adjustmentNo: String(DOM.txtAdjustmentNo.val() || '').trim(),
            adjustmentType: String(DOM.ddlAdjustmentType.val() || 'Credit Note').trim(),
            adjustmentDate: adjDate,
            billNo: String(DOM.txtBillNo.val() || '').trim(),
            remarks: String(DOM.txtRemarks.val() || '').trim(),
            notesReasonForCredit: String(DOM.txtNotesReasonForCredit.val() || '').trim(),
            chcDetails: chcDetails,
            expenseDetails: expenseDetails
        };

        toggleSaveButtonLoading(true);

        $.ajax({
            url: '/BillAdjustment/SaveAdjustment',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (res) {
                toggleSaveButtonLoading(false);

                if (res.success) {
                    showToast(res.message || 'Adjustment saved successfully.', 'success');

                    if (res.data && res.data.adjustmentNo) {
                        DOM.txtAdjustmentNo.val(res.data.adjustmentNo);
                    }

                    loadApprovedBills();
                } else {
                    showToast(res.message || 'Validation or database error occurred.', 'error');
                }
            },
            error: function (xhr) {
                toggleSaveButtonLoading(false);
                let errMsg = 'An unexpected server error occurred.';
                try {
                    const parsed = JSON.parse(xhr.responseText);
                    if (parsed && parsed.message) errMsg = parsed.message;
                } catch (e) { }

                showToast(errMsg, 'error');
            }
        });
    }

    // =========================================================================
    // 6. Helpers & UI Utilities
    // =========================================================================
    function resetForm() {
        DOM.form[0].reset();
        DOM.txtBillNo.val('');
        DOM.hdnCustomerId.val('');
        selectedBillNo = '';
        DOM.tblChcBody.html('<tr><td colspan="7" class="text-center text-muted py-3">Select a Bill to populate charges.</td></tr>');
        DOM.tblExpenseBody.html('<tr><td colspan="6" class="text-center text-muted py-3">Select a Bill to populate expense details.</td></tr>');
        DOM.lblTotalChcAmount.text('0.00');
        DOM.lblTotalEstimated.text('0.00');
        DOM.lblTotalActual.text('0.00');
        DOM.lblTotalDifference.text('0.00');
        DOM.lblCreationDate.text('-');
        DOM.lblLastUpdateDate.text('-');
        $('.is-invalid').removeClass('is-invalid');
        $('#tblApprovedBills tbody tr').removeClass('table-active');
        $('#tblApprovedBills tbody input[type="checkbox"]').prop('checked', false);
        if (typeof window.clearCustomDropdownById === 'function') {
            window.clearCustomDropdownById('ddlChcHead');
        } else {
            DOM.ddlChcHead.val('').trigger('change');
        }
        DOM.btnSave.add(DOM.btnAdjustBottom).html('<i class="fas fa-save me-1"></i> <span>Save</span>');
        initDatePickers();
        fetchNextAdjustmentNo(DOM.ddlAdjustmentType.val() || 'Credit Note');
    }

    function toggleSaveButtonLoading(loading) {
        isSaving = loading;
        if (loading) {
            DOM.btnSave.add(DOM.btnAdjustBottom).prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> <span>Saving...</span>');
        } else {
            DOM.btnSave.add(DOM.btnAdjustBottom).prop('disabled', false).html('<i class="fas fa-save me-1"></i> <span>Save</span>');
        }
    }

    function toggleScreenLoading(loading) {
        if (loading) {
            DOM.form.css('opacity', '0.6').css('pointer-events', 'none');
        } else {
            DOM.form.css('opacity', '1').css('pointer-events', 'auto');
        }
    }

    function formatNumber(num) {
        num = parseFloat(num) || 0;
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatDate(dateStr) {
        if (!dateStr) return '-';
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        const day = String(d.getDate()).padStart(2, '0');
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const year = d.getFullYear();
        return `${day}/${month}/${year}`;
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

    function debounce(func, wait) {
        let timeout;
        return function () {
            const context = this, args = arguments;
            clearTimeout(timeout);
            timeout = setTimeout(function () {
                func.apply(context, args);
            }, wait);
        };
    }

    function showToast(msg, type) {
        if (typeof toastr !== 'undefined') {
            toastr.options = { closeButton: true, progressBar: true, positionClass: 'toast-top-right' };
            if (type === 'error') toastr.error(msg);
            else if (type === 'warning') toastr.warning(msg);
            else if (type === 'info') toastr.info(msg);
            else toastr.success(msg);
        } else {
            alert(msg);
        }
    }

})(jQuery);
