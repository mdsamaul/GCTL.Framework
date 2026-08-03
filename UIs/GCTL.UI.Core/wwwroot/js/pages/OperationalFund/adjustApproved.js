//#region Ready Part
$(document).ready(function () {
    //Load Dropdown
    InitDropdown('/OperationFund/GetEmpDD', '#Bill-Customer', 'Select Employee');
    InitDropdown('/OperationFund/GetShipemtDD', '#Bill-ShipmentMode', 'Select Shipment Mode');
    //Load Top and Bottom Grid
    LoadTopGrid();
    LoadBottomGrid();
    ClearTmpDetail();


    ////Clear Event Button
    $(document).on("click", "#adjustApprove-Clear", function (e) {
        e.preventDefault();
        ClearForm();
    });
});
//#endregion

//#region Grid Section

//#region Global Veriable
let BillPageNumber = 1;
let BillSortColumn = "JobNo";
let BillSortOrder = "desc";
//For Bottom Grid
let BottomPageNumber = 1;
let BottomSortColumn = "JobNo";
let BottomSortOrder = "desc";
//#endregion

//#region Load  Top Grid 
function LoadTopGrid() {
    $.ajax({
        url: '/OFRAdjustApproval/GetAllRequisitionTopGrid',
        type: 'GET',
        data: {
            pageNumber: BillPageNumber,
            pageSize: $('#Bill-PageSize').val(),
            searchTerm: $('#Bill-SearchInput').val(),
            sortColumn: BillSortColumn,
            sortOrder: BillSortOrder,
            customerid: $('#Bill-Customer').val(),
            shipmentmodeid: $('#Bill-ShipmentMode').val()
        },
        success: function (res) {
            console.log("Bill Adjust UI Top Grid Data :", res);
            let rows = '';

            if (!res.data || res.data.length === 0) {
                rows = `
                        <tr>
                            <td colspan="6" class="text-center text-muted py-3">
                                No data found
                            </td>
                        </tr>`;
            } else {

                $.each(res.data, function (i, item) {
                    rows += `
                            <tr>
                                <td class="text-center">
                                    <input type="radio" class="form-check-input adjust-radio" name="ApproveChecked" value="${item.jobNo}">
                                </td>
                                <td>${item.jobNo ?? ''}</td>
                                <td>${item.customerName ?? ''}</td>
                                <td class="text-center">${item.shipmentMode ?? ''}</td>
                            </tr>`;
                });
            }

            $('#Bill-tbody').html(rows);

            buildJobPagination(res.totalCount);
            updateJobEntryInfo(res.totalCount);
        }
    });
}
//#endregion

//#region Bottom Grid Select All Checkbox
$(document).on('change', '#bottom-selectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.bottom-checkbox').prop('checked', isChecked);
});

// Individual checkbox change - update select all
$(document).on('change', '.bottom-checkbox', function () {
    const totalCheckboxes = $('.bottom-checkbox').length;
    const checkedCheckboxes = $('.bottom-checkbox:checked').length;

    $('#bottom-selectAll').prop('checked', totalCheckboxes === checkedCheckboxes);
});
//#endregion

//#region Load  Bottom Grid
function LoadBottomGrid() {
    $.ajax({
        url: '/OFRAdjustApproval/GetAllRequisitionForBottomGrid',
        type: 'GET',
        data: {
            pageNumber: BottomPageNumber,
            pageSize: $('#bottom-PageSize').val(),
            searchTerm: $('#bottom-Search').val(),
            sortColumn: BottomSortColumn,
            sortOrder: BottomSortOrder,
        },
        success: function (res) {
            console.log("Approval UI Bottom Grid Data :", res);
            let rows = '';

            if (!res.data || res.data.length === 0) {
                rows = `
                        <tr>
                            <td colspan="6" class="text-center text-muted py-3">
                                No data found
                            </td>
                        </tr>`;
            } else {

                $.each(res.data, function (i, item) {
                    rows += `
                            <tr>
                                <td class="text-center">
                                    <input type="checkbox" class="form-check-input bottom-checkbox" name="bottom-checkbox" value="${item.jobNo}">
                                </td>
                                <td class="text-center fw-bold text-dark">
                                    <span class="job-no" style="cursor:pointer; text-decoration:underline;">
                                        ${item.jobNo ?? ''}
                                    </span>
                                </td>
                                <td class="text-center text-dark text-muted fs-9 fw-bold">${item.customerName ?? ''}</td>
                                <td class="text-center text-dark text-muted fs-9 fw-bold">${item.shipmentMode ?? ''}</td>
                            </tr>`;
                });
            }

            $('#bottom-body').html(rows);
            // Reset select all checkbox
            $('#bottom-selectAll').prop('checked', false);
            BottomPagination(res.totalCount);
            updateBottomEntryInfo(res.totalCount);
        }
    });
}
//#endregion

//#region Top Grid Pagination Section
//#region Job Pagination
function buildJobPagination(totalCount) {

    let pageSize = parseInt($('#Bill-PageSize').val());
    let pagination = '';

    if (pageSize === -1 || totalCount === 0) {
        $('#Bill-Pagination').html('');
        return;
    }

    let totalPages = Math.ceil(totalCount / pageSize);
    let current = BillPageNumber;

    // Previous
    pagination += `
        <li class="page-item ${current === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changeJobPage(${current - 1})">Previous</a>
        </li>`;

    // Always show first 2 pages
    for (let i = 1; i <= Math.min(2, totalPages); i++) {
        pagination += pageItem(i, current);
    }

    // Dots before middle
    if (current > 4) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Middle pages (current ±1)
    let start = Math.max(3, current - 1);
    let end = Math.min(totalPages - 2, current + 1);

    for (let i = start; i <= end; i++) {
        pagination += pageItem(i, current);
    }

    // Dots after middle
    if (current < totalPages - 3) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Always show last 2 pages
    for (let i = Math.max(totalPages - 1, 3); i <= totalPages; i++) {
        pagination += pageItem(i, current);
    }

    // Next
    pagination += `
        <li class="page-item ${current === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changeJobPage(${current + 1})">Next</a>
        </li>`;

    $('#Bill-Pagination').html(pagination);
}

// helper
function pageItem(page, current) {
    return `
        <li class="page-item ${page === current ? 'active' : ''}">
            <a class="page-link" href="#" onclick="changeJobPage(${page})">${page}</a>
        </li>`;
}


function changeJobPage(page) {
    BillPageNumber = page;
    LoadTopGrid();
}
//#endregion

//#region Update Text
function updateJobEntryInfo(totalCount) {

    let pageSize = $('#Bill-PageSize').val();

    if (pageSize == -1) {
        $('.Bill-Pagination-message').text(`Showing 1 to ${totalCount} of ${totalCount} entries`);
        return;
    }

    let start = ((BillPageNumber - 1) * pageSize) + 1;
    let end = Math.min(BillPageNumber * pageSize, totalCount);

    $('.Bill-Pagination-message').text(`Showing ${start} to ${end} of ${totalCount} entries`);
}
//#endregion

//#region Search, Filter and PageSize

$('#Bill-SearchInput').on('input', function () {
    BillPageNumber = 1;
    if ($(this).val() === '') {
        LoadTopGrid();
    } else {
        LoadTopGrid();
    }
});


$('#Bill-PageSize').on('change', function () {
    BillPageNumber = 1;
    LoadTopGrid();
});

$('#Bill-Customer, #Bill-ShipmentMode').on('change', function () {
    BillPageNumber = 1;
    LoadTopGrid();
});
//#endregion

//#region Sorting
$('.table thead th[data-column]').on('click', function () {

    let column = $(this).data('column');

    if (BillSortColumn === column) {
        BillSortOrder = BillSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        BillSortColumn = column;
        BillSortOrder = 'asc';
    }

    BillPageNumber = 1;
    LoadTopGrid();
});
//#endregion
//#endregion

//#region Bottom Grid Pagination
//#region Job Pagination
function BottomPagination(totalCount) {

    let pageSize = parseInt($('#bottom-PageSize').val());
    let pagination = '';

    if (pageSize === -1 || totalCount === 0) {
        $('#bottom-Pagination').html('');
        return;
    }

    let totalPages = Math.ceil(totalCount / pageSize);
    let current = BottomPageNumber;

    // Previous
    pagination += `
        <li class="page-item ${current === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changeBottomPage(${current - 1})">Previous</a>
        </li>`;

    // Always show first 2 pages
    for (let i = 1; i <= Math.min(2, totalPages); i++) {
        pagination += BottompageItem(i, current);
    }

    // Dots before middle
    if (current > 4) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Middle pages (current ±1)
    let start = Math.max(3, current - 1);
    let end = Math.min(totalPages - 2, current + 1);

    for (let i = start; i <= end; i++) {
        pagination += BottompageItem(i, current);
    }

    // Dots after middle
    if (current < totalPages - 3) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Always show last 2 pages
    for (let i = Math.max(totalPages - 1, 3); i <= totalPages; i++) {
        pagination += BottompageItem(i, current);
    }

    // Next
    pagination += `
        <li class="page-item ${current === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changeBottomPage(${current + 1})">Next</a>
        </li>`;

    $('#bottom-Pagination').html(pagination);
}

// helper
function BottompageItem(page, current) {
    return `
        <li class="page-item ${page === current ? 'active' : ''}">
            <a class="page-link" href="#" onclick="changeBottomPage(${page})">${page}</a>
        </li>`;
}


function changeBottomPage(page) {
    BottomPageNumber = page;
    LoadBottomGrid();
}
//#endregion

//#region Update Text
function updateBottomEntryInfo(totalCount) {

    let pageSize = $('#bottom-PageSize').val();

    if (pageSize == -1) {
        $('.bottom-message').text(`Showing 1 to ${totalCount} of ${totalCount} entries`);
        return;
    }

    let start = ((BottomPageNumber - 1) * pageSize) + 1;
    let end = Math.min(BottomPageNumber * pageSize, totalCount);

    $('.bottom-message').text(`Showing ${start} to ${end} of ${totalCount} entries`);
}
//#endregion

//#region Search, Filter and PageSize

$('#bottom-Search').on('input', function () {
    BottomPageNumber = 1;
    if ($(this).val() === '') {
        LoadBottomGrid();
    } else {
        LoadBottomGrid();
    }
});


$('#bottom-PageSize').on('change', function () {
    BottomPageNumber = 1;
    LoadBottomGrid();
});


//#endregion

//#region Sorting
$('#bottomGrid thead th[data-column]').on('click', function () {

    let column = $(this).data('column');

    if (BottomSortColumn === column) {
        BottomSortOrder = BottomSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        BottomSortColumn = column;
        BottomSortOrder = 'asc';
    }

    BottomPageNumber = 1;
    LoadBottomGrid();
});
//#endregion
//#endregion
//#endregion

//#region Dropdown Init with Choice.js
function InitDropdown(url, selector, placeholder) {
    $.ajax({
        url: url,
        type: 'GET',
        success: function (response) {

            let select = $(selector);
            select.empty().append(`<option value="">${placeholder}</option>`);

            response.forEach(function (item) {
                select.append(`<option value="${item.value}">${item.text}</option>`);
            });

            // already initialized
            if (!select.data('choices')) {

                let choiceInstance = new Choices(selector, {
                    removeItemButton: true,
                    shouldSort: false,
                    dropdownAutoWidth: false,
                    placeholderValue: placeholder
                });

                // store instance in element
                select.data('choices', choiceInstance);

            }
        }
    });
}
//#endregion

//#region Get Requisition Info in Input field and Copied Details Data To Tmp Table
$(document).on('click', 'input[name="ApproveChecked"]', function () {

    if ($(this).prop('checked') && $(this).data('waschecked')) {
        // Radio was already checked → now unchecked
        $(this).prop('checked', false);
        $(this).data('waschecked', false);

        // Clear input fields
        ClearForm();

    } else {
        // Normal check
        $('input[name="ApproveChecked"]').data('waschecked', false);
        $(this).data('waschecked', true);

        // Load header data
        let jobno = $(this).val();

        $.ajax({
            url: "/OFRAdjustApproval/JobDetails",
            type: "GET",
            data: { jobno: jobno },
            success: function (headerdata) {
                console.log(headerdata);
                if (!headerdata) {
                    toastr.warning("No job data found!");
                    ClearForm();
                    return;
                }
                var data = headerdata.masterData;
                $("#Adjust-ReqNo").val(data.ofrNo);
                $("#Adjust-ReqDate").val(formatDate(data.ofrDate));
                $("#Adjust-JobNo").val(data.jobNo);
                $('#Adjust-Shipment').val(data.shipmentMode);
                $("#Adjust-Cutomer").val(data.customerName);
                $("#Adjust-LCValue").val(data.lcValue);
                $("#Adjust-invoiceNo").val(data.invoiceNo);
                $("#Adjust-MatDescription").val(data.materialDescription);
                $("#Adjust-Qty").val(data.quantity);
                $("#Adjust-Weight").val(data.weight);

                LoadTmpGrid(jobno);

            },
            error: function () {
                console.error("Error loading header data", xhr);
                ClearForm();
            }
        });
    }
});

//#endregion

//#region Clear Function Section

//#region Clear Tmp Details Table with filtering Current User
function ClearTmpDetail() {
    $.ajax({
        url: '/OperationFund/ClearTmpDetails/',
        type: 'DELETE',
        success: function (res) {
            console.log("Current User Related Tmp Data Deleted");
            LoadTmpGrid();

        },
        error: function (err) {
            console.log("Current User Related Tmp Data Deleted Failed");
        }
    });
}
//#endregion

//#region Clear Function
function ClearForm() {
    $('.job-radio').prop('checked', false);
    $("#Adjust-ReqNo,#Adjust-JobNo,#Adjust-Cutomer,#Adjust-invoiceNo,#Adjust-Qty,#Adjust-ReqDate,#Adjust-Shipment,#Adjust-LCValue,#Adjust-MatDescription,#Adjust-Weight, #Adjust-Search, #Adjust-SearchInput").val("");
    ClearTmpDetail();
    LoadTopGrid();
    LoadBottomGrid();
}
//#endregion

//#endregion

//#region Date Formate
function formatDate(date) {
    if (!date) return '';
    let d = new Date(date);
    return d.toLocaleDateString('en-GB');
}

//#endregion

//#region Tmp Grid Section

//#region Load Tmp Details Table with live totals
function LoadTmpGrid(jobNo) {
    $.get('/OFRAdjustApproval/LoadTmpTable', { jobNo: jobNo }, function (res) {
        const $tbody = $('#Adjust-Body');
        $tbody.html('');

        if (!res || res.length === 0) return;
        console.log("Tmp Grid Data:", res);

        let groupedData = {};

        // Group data by serviceType
        res.forEach(item => {
            const service = item.serviceTypeName || 'Other';
            if (!groupedData[service]) groupedData[service] = [];
            groupedData[service].push(item);
        });

        Object.keys(groupedData).forEach(serviceType => {
            let subTotalReq = 0;
            let subTotalApproval = 0;

            groupedData[serviceType].forEach(item => {
                const reqAmount = parseFloat(item.amount) || 0;
                const approvalAmount = parseFloat(item.approvalAmount) || 0;
                const adjustAmount =
                    item.adjustAmount && parseFloat(item.adjustAmount) > 0
                        ? parseFloat(item.adjustAmount)
                        : approvalAmount;

                subTotalReq += reqAmount;
                subTotalApproval += approvalAmount;

                // Create row
                const $row = $(`
                    <tr>
                        <td class="text-center">${item.serialNo}</td>
                        <td>${item.accountHeadName}</td>
                        <td class="text-center">${item.isReceivetable ? 'Yes' : 'No'}</td>

                        <td>
                            <input type="number" class="form-control form-control-sm text-end tmp-amount bg-light"
                                   value="${reqAmount.toFixed(2)}" data-tc="${item.ofrDetailsID}" data-service-type="${serviceType}" readonly />
                        </td>

                        <td>
                            <input type="number" class="form-control form-control-sm text-end bg-light approval-amount"
                                   value="${approvalAmount.toFixed(2)}" data-tc="${item.ofrDetailsID}" data-service-type="${serviceType}" disabled />
                        </td>

                        <td>
                            <input type="number" class="form-control form-control-sm text-end adjust-amount"
                                   value="${adjustAmount.toFixed(2)}" data-tc="${item.ofrDetailsID}" data-service-type="${serviceType}" disabled />
                        </td>

                        <td>
                            <input type="number" class="form-control form-control-sm text-end confirm-amount"
                                   value="${adjustAmount.toFixed(2)}" data-service-type="${serviceType}" data-tc="${item.ofrDetailsID}" />
                        </td>

                        <td>
                            <input type="number" class="form-control form-control-sm text-end diff-amount bg-light"
                                   value="0.00" data-service-type="${serviceType}" readonly />
                        </td>

                        <td>
                            <select class="form-select form-select-sm cashbank-select" data-tc="${item.ofrDetailsID}">
                                <option value="">Select...</option>
                                <option value="10200400001">Cash</option>
                                <option value="10200400003">Bank</option>
                                <option value="Employee">Employee</option>
                            </select>
                        </td>

                        <td>
                            <select class="form-select form-select-sm bankaccount-select" data-tc="${item.ofrDetailsID}" disabled>
                                <option value="">Select Bank Account</option>
                            </select>
                        </td>
                    </tr>
                `);

                // Append row
                $tbody.append($row);

                //// Cash/Bank value set
                $row.find('.cashbank-select').val(item.cashBank || '');

                //Load bank accounts and auto-select Bank
                loadBankAccounts($row.find('.cashbank-select'), item.bankAccount);

            });

            // Subtotal row
            $tbody.append(`
                <tr class="subtotal-row" data-service-type="${serviceType}">
                    <td colspan="3" class="text-end text-info fw-bold fs-8">Subtotal</td>
                    <td class="subtotal-req text-end fw-bold text-info fs-8">${subTotalReq.toFixed(2)}</td>
                    <td class="subtotal-approval text-end fw-bold text-info fs-8">${subTotalApproval.toFixed(2)}</td>
                    <td class="subtotal-adjust text-end fw-bold text-info fs-8">0.00</td>
                    <td class="subtotal-confirm text-end fw-bold text-info fs-8">0.00</td>
                    <td class="subtotal-diff text-end fw-bold text-info fs-8">0.00</td>
                    <td></td>
                    <td></td>
                </tr>
            `);
        });

        // Grand Total
        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
        const grandTotalApproval = grandTotalReq;

        $tbody.append(`
            <tr class="grand-total-row">
                <td colspan="3" class="text-end text-primary fs-8 fw-bold">Grand Total</td>
                <td class="grand-total-req text-end fw-bold text-primary fs-8">${grandTotalReq.toFixed(2)}</td>
                <td class="grand-total-approval text-end fw-bold text-primary fs-8">${grandTotalApproval.toFixed(2)}</td>
                <td class="grand-total-adjust text-end fw-bold text-primary fs-8">—</td>
                <td class="grand-total-confirm text-end fw-bold text-primary fs-8">0.00</td>
                <td class="grand-total-diff text-end fw-bold text-primary fs-8">0.00</td>
                <td></td>
                <td></td>
            </tr>
        `);

        // Recalculate totals for Adjust / Confirm / Diff
        recalcAdjustAndDiffTotals();

        // Initialize Choices.js for Cash/Bank dropdown
        initCashBankChoices();
    });
}
//#endregion

//#region Calculate SubTotal and Grand Total
function recalcAdjustAndDiffTotals() {
    let adjustTotalsByService = {};
    let confirmTotalsByService = {};
    let diffTotalsByService = {};

    let grandAdjust = 0;
    let grandConfirm = 0;
    let grandDiff = 0;

    $('#Adjust-Body tr').each(function () {
        const $tr = $(this);
        if ($tr.hasClass('subtotal-row') || $tr.hasClass('grand-total-row')) return;

        const serviceType = $tr.find('.confirm-amount').data('service-type');

        const adjustAmt = parseFloat($tr.find('.adjust-amount').val()) || 0;
        const confirmAmt = parseFloat($tr.find('.confirm-amount').val()) || 0;
        const diffAmt = adjustAmt - confirmAmt;

        // Show difference in row
        $tr.find('.diff-amount').val(diffAmt.toFixed(2));

        // Initialize service bucket if needed
        if (!adjustTotalsByService[serviceType]) {
            adjustTotalsByService[serviceType] = 0;
            confirmTotalsByService[serviceType] = 0;
            diffTotalsByService[serviceType] = 0;
        }

        // Accumulate totals
        adjustTotalsByService[serviceType] += adjustAmt;
        confirmTotalsByService[serviceType] += confirmAmt;
        diffTotalsByService[serviceType] += diffAmt;

        grandAdjust += adjustAmt;
        grandConfirm += confirmAmt;
        grandDiff += diffAmt;
    });

    // Update subtotal rows
    $('#Adjust-Body tr.subtotal-row').each(function () {
        const serviceType = $(this).data('service-type');

        $(this).find('.subtotal-adjust')
            .text((adjustTotalsByService[serviceType] || 0).toFixed(2));


        $(this).find('.subtotal-confirm')
            .text((confirmTotalsByService[serviceType] || 0).toFixed(2));

        $(this).find('.subtotal-diff')
            .text((diffTotalsByService[serviceType] || 0).toFixed(2));
    });

    // Update grand total row
    $('.grand-total-adjust').text(grandAdjust.toFixed(2));
    $('.grand-total-confirm').text(grandConfirm.toFixed(2));
    $('.grand-total-diff').text(grandDiff.toFixed(2));
}

// Live update
$(document).on('input', '.confirm-amount', function () {
    recalcAdjustAndDiffTotals();
});
//#endregion

//#region Load Cash Bank Dropdown with js
function initCashBankChoices() {
    $('.cashbank-select').each(function () {

        if (!$(this).data('choices')) {
            let choice = new Choices(this, {
                searchEnabled: false,
                shouldSort: false,
                placeholderValue: 'Select...',
                removeItemButton: true
            });

            $(this).data('choices', choice);
        }
    });
}
//#endregion

//#region When Click on Cash/Bank Dropdown
//$(document).on('change', '.cashbank-select', function () {
//    let choiceInstance = $(this).data('choices');
//    let value = choiceInstance ? choiceInstance.getValue(true) : $(this).val();
//    let selectedType = $(this).find('option:selected').data('type'); // Cash / Bank / Employee
//    let tc = $(this).data('tc');

//    let $bankSelect = $('.bankaccount-select[data-tc="' + tc + '"]');

//    // Destroy old Choices instance first
//    if ($bankSelect.data('choices')) {
//        $bankSelect.data('choices').destroy();
//        $bankSelect.removeData('choices');
//    }

//    // Clear dropdown
//    $bankSelect.empty().append('<option value="">Select Bank Account</option>');

//    // Disable if nothing selected or Cash selected
//    if (!value) {
//        $bankSelect.prop('disabled', true);
//        return; // skip AJAX for Cash
//    }

//    // Enable for Bank / Employee
//    $bankSelect.prop('disabled', false);

//    // Load bank account data via AJAX
//    $.ajax({
//        url: '/OFRApproval/BankAccountDD',
//        type: 'GET',
//        data: { value: value },
//        success: function (res) {
//            res.forEach(item => {
//                $bankSelect.append(`<option value="${item.value}">${item.text}</option>`);
//            });

//            // Re-initialize Choices.js
//            let bankChoice = new Choices($bankSelect[0], {
//                searchEnabled: true,
//                shouldSort: false,
//                placeholderValue: 'Select Bank Account'
//            });

//            $bankSelect.data('choices', bankChoice);
//        }
//    });
//});
function loadBankAccounts($cashBankSelect, selectedBank = null) {
    let choiceInstance = $cashBankSelect.data('choices');
    let value = choiceInstance ? choiceInstance.getValue(true) : $cashBankSelect.val();
    let tc = $cashBankSelect.data('tc');

    let $bankSelect = $('.bankaccount-select[data-tc="' + tc + '"]');

    // Destroy old Choices instance first
    if ($bankSelect.data('choices')) {
        $bankSelect.data('choices').destroy();
        $bankSelect.removeData('choices');
    }

    // Clear dropdown
    $bankSelect.empty().append('<option value="">Select Bank Account</option>');

    $bankSelect.prop('disabled', false);

    // Load bank accounts via AJAX
    $.ajax({
        url: '/OFRApproval/BankAccountDD',
        type: 'GET',
        data: { value: value },
        success: function (res) {
            res.forEach(item => {
                $bankSelect.append(`<option value="${item.value}">${item.text}</option>`);
            });

            // Auto-select Bank Account if provided
            if (selectedBank) {
                $bankSelect.val(selectedBank);
            }
            else if (value === '10200400001' && res.length > 0) {
                $bankSelect.val(res[0].value);
            }
            //else if (res.length > 0) {
            //    $bankSelect.val(res[0].value); 
            //}
            let bankChoice = new Choices($bankSelect[0], {
                searchEnabled: true,
                shouldSort: false,
                placeholderValue: 'Select Bank Account',
                removeItemButton: true
            });

            $bankSelect.data('choices', bankChoice);
        }
    });
}

$(document).on('change', '.cashbank-select', function () {
    loadBankAccounts($(this));
});
//#endregion
//#endregion

//#region Bill Adjust Amount
var OFRBillAdjustDao = {
    saveBillAdjust: function (data) {
        return $.ajax({
            url: '/OFRAdjustApproval/SaveAdjustApproval',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        });
    }
};
$('#adjustApprove-SaveBtn').on('click', function () {

    let adjustList = [];

    $('#Adjust-Body tr').each(function () {
        let $tr = $(this);

        let $confirmamount = $tr.find('.confirm-amount');
        if ($confirmamount.length === 0) return;

        let ofrDetailsID = $confirmamount.data('tc');
        let confirmamount = parseFloat($confirmamount.val()) || 0;

        let diffAmount = parseFloat($tr.find('.diff-amount').val()) || 0;
        let cashBank = $tr.find('.cashbank-select').val();
        let bankAccount = $tr.find('.bankaccount-select').val();

        adjustList.push({
            OFRNo: $('#Adjust-ReqNo').val(),
            OFR_DetailsID: ofrDetailsID,
            ConfirmAmount: confirmamount,
            DifferentAmount: diffAmount,
            CashBank: cashBank,
            BankAcount: bankAccount

        });
    });
    console.log(adjustList);
    if (adjustList.length === 0) {
        toastr.warning("No Bill Adjust data found");
        return;
    }

    OFRBillAdjustDao.saveBillAdjust(adjustList)
        .done(function (res) {
            if (res.success) {
                toastr.success(res.message);
            } else {
                toastr.error(res.message);
            }
            ClearForm();
        })
        .fail(function () {
            console.error("Server error while saving Bill Adjust");
        });
});
//#endregion

//#region When User Click Bottom Grid job no Edit Button
$(document).on('click', '.job-no', function () {

    let jobno = $(this).text().trim();

    if (!jobno) {
        toastr.warning("Invalid Job No");
        return;
    }

    $.ajax({
        url: "/OFRAdjustApproval/EditDetails",
        type: "GET",
        data: { jobno: jobno },
        success: function (res) {

            console.log("Edit click response:", res);

            if (!res.success) {
                toastr.warning(res.message);
                ClearForm();
                return;
            }

            let data = res.masterData;

            // Header fields fill
            $("#Adjust-ReqNo").val(data.ofrNo);
            $("#Adjust-ReqDate").val(formatDate(data.ofrDate));
            $("#Adjust-JobNo").val(data.jobNo);
            $('#Adjust-Shipment').val(data.shipmentMode);
            $("#Adjust-Cutomer").val(data.customerName);
            $("#Adjust-LCValue").val(data.lcValue);
            $("#Adjust-invoiceNo").val(data.invoiceNo);
            $("#Adjust-MatDescription").val(data.materialDescription);
            $("#Adjust-Qty").val(data.quantity);
            $("#Adjust-Weight").val(data.weight);

            // Load temp details grid
            LoadTmpGrid(jobno);
            toastr.success(res.message);
        },
        error: function (xhr) {
            console.error("Error loading requisition data", xhr);
            ClearForm();
        }
    });
});

//#endregion