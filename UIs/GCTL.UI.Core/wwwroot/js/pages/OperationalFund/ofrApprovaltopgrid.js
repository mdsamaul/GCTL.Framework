//#region Ready Part
$(document).ready(function () {
    //Load Dropdown
    InitDropdown('/OperationFund/GetEmpDD', '#Customer', 'Select Employee');
    InitDropdown('/OperationFund/GetShipemtDD', '#Appro-ShipmentMode', 'Select Shipment Mode');
    loadRequisitionList();

    $('#Customer, #Appro-ShipmentMode').on('change', function () {
        if (jobTable) jobTable.ajax.reload();
    });
    //Load Requisition List For Top Grid
    BottomGrid();
    // ClearTmpDetail();
    // Clear Event Button
    $(document).on("click", "#Approve-Clear", function (e) {
        e.preventDefault();
        ClearApprovedForm();
    });
});
//#endregion

//#region Global Veriable
//For Top Grid

//#endregion

//#region Load Requisition Entry List for Top Grid
let jobTable = null;
window.gctlRenderers = window.gctlRenderers || {};
window.gctlRenderers.formatDate = function (data) { return formatDate(data); };

function loadRequisitionList() {
    jobTable = $.gctlGrid.initFromModel({
        gridId: 'Appro-Table',
        ajaxData: function (d) {
            d.customerid = $('#Customer').val();
            d.shipmentmodeid = $('#Appro-ShipmentMode').val();
        }
    });
}
// function loadRequisitionList() {

//     $.ajax({
//         url: '/OFRApproval/GetAllRequisitionList',
//         type: 'GET',
//         data: {
//             pageNumber: jobPageNumber,
//             pageSize: $('#Appro-PageSize').val(),
//             searchTerm: $('#Appro-SearchInput').val(),
//             sortColumn: jobSortColumn,
//             sortOrder: jobSortOrder,
//             customerid: $('#Customer').val(),
//             shipmentmodeid: $('#Appro-ShipmentMode').val()
//         },
//         success: function (res) {
//             console.log("Approval UI Top Grid Data :", res);
//             let rows = '';

//             if (!res.data || res.data.length === 0) {
//                 rows = `
//                         <tr>
//                             <td colspan="6" class="text-center text-muted py-3">
//                                 No data found
//                             </td>
//                         </tr>`;
//             } else {

//                 $.each(res.data, function (i, item) {
//                     rows += `
//                             <tr>
//                                 <td class="text-center">
//                                     <input type="radio" class="form-check-input job-radio" name="jobSelect" value="${item.jobNo}">
//                                 </td>
//                                 <td>${item.jobNo ?? ''}</td>
//                                 <td>${item.customerName ?? ''}</td>
//                                 <td class="text-center">${item.shipmentMode ?? ''}</td>
//                             </tr>`;
//                 });
//             }

//             $('#Appro-tbody').html(rows);

//             buildJobPagination(res.totalCount);
//             updateJobEntryInfo(res.filterCount, res.totalCount);
//         }
//     });
// }
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

//#region //#region Load Requisition Entry List for Bottom Grid
let bottomTable = null;
function BottomGrid() {
    if (bottomTable) {
        bottomTable.ajax.reload();
        return;
    }
    bottomTable = $.gctlGrid.initFromModel({
        gridId: 'bottomGrid',
    });
}
//#endregion

//#region Get Requisition Info in Input field and Copied Details Data To Tmp Table
$(document).on('click', 'input[name="jobSelect"]', function () {

    if ($(this).prop('checked') && $(this).data('waschecked')) {
        // Radio was already checked → now unchecked
        $(this).prop('checked', false);
        $(this).data('waschecked', false);

        // Clear input fields
        ClearApprovedForm();

    } else {
        // Normal check
        $('input[name="jobSelect"]').data('waschecked', false);
        $(this).data('waschecked', true);

        // Load header data
        let jobno = $(this).val();

        $.ajax({
            url: "/OFRApproval/GetRequisitionDataWithDetails",
            type: "GET",
            data: { jobno: jobno },
            success: function (headerdata) {
                console.log(headerdata);
                if (!headerdata) {
                    toastr.warning("No job data found!");
                    ClearApprovedForm();
                    return;
                }
                var data = headerdata.masterData;
                $("#Appro-ReqNo").val(data.ofrNo);
                $("#Appro-ReqDate").val(formatDate(data.ofrDate));
                //$("#Appro-ReqDate").val(headerdata.ofrDate);
                $("#Appro-JobNo").val(data.jobNo);
                $('#Appro-Shipment').val(data.shipmentMode);
                $("#Appro-Cutomer").val(data.customerName);
                $("#Appro-LCValue").val(data.lcValue);
                $("#Appro-invoiceNo").val(data.invoiceNo);
                $("#Appro-InvoiceValue").val(data.invoiceValue);
                $("#Appro-MatDescription").val(data.materialDescription);
                $("#Appro-Qty").val(data.quantity);
                $("#Appro-Weight").val(data.weight);
                //$("#ShipmentModeID").val(headerdata.shipmentModeID);
                //$("#CustomerID").val(headerdata.customerID);
                loadTmpDetails(jobno);

            },
            error: function () {
                console.error("Error loading header data", xhr);
                ClearApprovedForm();
            }
        });
    }
});

//#endregion

//#region Load Bank Accounts
function loadBankAccounts($cashBankSelect, selectedBank = null) {
    let value = $cashBankSelect.val();
    let tc = $cashBankSelect.data('tc');
    let $bankSelect = $('.bankaccount-select[data-tc="' + tc + '"]');

    // Clear dropdown
    $bankSelect.empty().append('<option value="">Select Bank Account</option>');

    if (!value || value === 'Cash' || value === 'Employee') {
        $bankSelect.prop('disabled', true);
        return;
    }

    $bankSelect.prop('disabled', false);

    // Load bank accounts via AJAX
    $.get('/OFRApproval/BankAccountDD', { value: value }, function (res) {
        if (res && res.length > 0) {
            res.forEach(item => {
                $bankSelect.append(`<option value="${item.value}">${item.text}</option>`);
            });
        }

        // Auto-select bank account
        if (selectedBank) {
            $bankSelect.val(selectedBank);
        }
        else if (value === '10200400001' && res && res.length > 0) {
            $bankSelect.val(res[0].value);
        }
    });
}

// On Cash/Bank change event
$(document).on('change', '.cashbank-select', function () {
    loadBankAccounts($(this));
});
//#endregion

//#region Load Tmp Details Table with live totals
function loadTmpDetails(jobNo) {
    $.get('/OFRApproval/GetAllTmpDetails', { jobNo: jobNo }, function (res) {
        const $tbody = $('#Tmp-TableBody');

        console.log('Temp table Data : ', res);

        $tbody.html('');

        if (!res || res.length === 0) return;

        let groupedData = {};

        // Group by serviceType
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
                const approvalAmount = parseFloat(item.approvalAmount) || reqAmount;

                subTotalReq += reqAmount;
                subTotalApproval += approvalAmount;

                const $row = $(`
                    <tr>
                        <td class="text-center">${item.serialNo}</td>
                        <td>${item.accountHeadName}</td>
                        <td class="text-center">${item.isReceivetable ? 'Yes' : 'No'}</td>
                        <td class="text-center">${item.serviceTypeName}</td>
                        <td>${item.remark ?? ''}</td>
                        <td>
                            <input type="number" class="form-control form-control-sm text-end tmp-amount bg-light"
                                   value="${reqAmount.toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}" data-service-type="${serviceType}" readonly />
                        </td>
                        <td>
                            <input type="number" class="form-control form-control-sm text-end approval-amount"
                                   value="${approvalAmount.toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}" data-service-type="${serviceType}" />
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

                $tbody.append($row);

                //Set Cash/Bank from API
                $row.find('.cashbank-select').val(item.cashBank || '');


                //Load Bank Accounts and auto-select if any
                loadBankAccounts($row.find('.cashbank-select'), item.bankAccount);
            });

            // Subtotal row
            $tbody.append(`
                <tr class="subtotal-row" data-service-type="${serviceType}">
                    <td colspan="5" class="text-end fw-bold fs-8">Subtotal</td>
                    <td class="subtotal-req text-end fw-bold fs-8">${subTotalReq.toFixed(2)}</td>
                    <td class="subtotal-approval text-end fw-bold fs-8">${subTotalApproval.toFixed(2)}</td>
                    <td colspan="2"></td>
                </tr>
            `);
        });

        // Grand Total row
        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
        const grandTotalApproval = res.reduce((sum, item) => sum + (parseFloat(item.approvalAmount) || parseFloat(item.amount) || 0), 0);
        $tbody.append(`
            <tr class="grand-total-row">
                <td colspan="5" class="text-end fw-bold fs-8">Grand Total</td>
                <td class="grand-total-req text-end fw-bold fs-8">${grandTotalReq.toFixed(2)}</td>
                <td class="grand-total-approval text-end fw-bold fs-8">${grandTotalApproval.toFixed(2)}</td>
                <td colspan="2"></td>
            </tr>
        `);
    });
}

//function loadTmpDetails() {
//    $.get('/OFRApproval/GetAllTmpDetails', function (res) {
//        const $tbody = $('#Tmp-TableBody');
//        $tbody.html('');

//        if (!res || res.length === 0) return;
//        console.log(res);
//        let groupedData = {};

//        // Group by serviceType
//        res.forEach(item => {
//            const service = item.serviceTypeName || 'Other';
//            if (!groupedData[service]) groupedData[service] = [];
//            groupedData[service].push(item);
//        });

//        Object.keys(groupedData).forEach(serviceType => {
//            let subTotalReq = 0;
//            let subTotalApproval = 0;

//            groupedData[serviceType].forEach(item => {
//                const reqAmount = parseFloat(item.amount) || 0;

//                //const approvalAmount = reqAmount;
//                const approvalAmount =
//                    item.approvalAmount && parseFloat(item.approvalAmount) > 0
//                        ? parseFloat(item.approvalAmount)
//                        : reqAmount;

//                subTotalReq += reqAmount;
//                subTotalApproval += approvalAmount;

//                $tbody.append(`
//                    <tr>
//                        <td class="text-center">${item.serialNo}</td>
//                        <td>${item.accountHeadName}</td>
//                        <td class="text-center">${item.isReceivetable ? 'Yes' : 'No'}</td>
//                        <td class="text-center">${item.serviceTypeName}</td>
//                         <!-- Remarks -->
//                        <td>${item.remark ?? ''}</td>
//                        <!-- Req. Amount readonly -->
//                        <td>
//                            <input type="number"
//                                   class="form-control form-control-sm text-end tmp-amount bg-light"
//                                   value="${reqAmount.toFixed(2)}"
//                                   data-tc="${item.ofrDetailsID}"
//                                   data-service-type="${serviceType}"
//                                   readonly />
//                        </td>
//                        <!-- Approval Amount editable -->
//                        <td>
//                            <input type="number"
//                                   class="form-control form-control-sm text-end approval-amount"
//                                   value="${approvalAmount.toFixed(2)}"
//                                   data-tc="${item.ofrDetailsID}"
//                                   data-service-type="${serviceType}" />

//                        </td>

//                        <!-- Cash/Bank -->
//                        <td>
//                            <select class="form-select form-select-sm cashbank-select" data-tc="${item.ofrDetailsID}">
//                                <option value="">Select...</option>
//                                <option value="10200400001">Cash</option>
//                                <option value="10200400003">Bank</option>
//                                <option value="Employee">Employee</option>
//                            </select>
//                        </td>

//                        <!-- Bank Account -->
//                        <td>
//                            <select class="form-select form-select-sm bankaccount-select" data-tc="${item.ofrDetailsID}" disabled>
//                                <option value="">Select Bank Account</option>
//                            </select>
//                        </td>
//                    </tr>
//                `);
//            });

//            // Subtotal row
//            $tbody.append(`
//                <tr class="subtotal-row" data-service-type="${serviceType}">
//                    <td colspan="5" class="text-end text-info fw-bold fs-8">Subtotal</td>
//                    <td class="subtotal-req text-end text-info fw-bold fs-8">${subTotalReq.toFixed(2)}</td>
//                    <td class="subtotal-approval text-end text-info fw-bold fs-8">${subTotalApproval.toFixed(2)}</td>
//                    <td colspan="2"></td>
//                </tr>
//            `);
//        });

//        // Grand Total row
//        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
//        const grandTotalApproval = grandTotalReq;

//        $tbody.append(`
//            <tr class="grand-total-row">
//                <td colspan="5" class="text-end text-primary fw-bold fs-8">Grand Total</td>
//                <td class="grand-total-req text-end text-primary fw-bold fs-8">${grandTotalReq.toFixed(2)}</td>
//                <td class="grand-total-approval text-end text-primary fw-bold fs-8">${grandTotalApproval.toFixed(2)}</td>
//                <td colspan="2"></td>
//            </tr>
//        `);
//        initCashBankChoices();

//    });
//}

function recalcTotals() {
    let groupedApprovalTotals = {};
    let grandTotalApproval = 0;

    $('#Tmp-TableBody tr').each(function () {
        const $tr = $(this);
        if ($tr.hasClass('subtotal-row') || $tr.hasClass('grand-total-row')) return;

        const serviceType = $tr.find('input.approval-amount').data('service-type');
        const approvalAmount = parseFloat($tr.find('input.approval-amount').val()) || 0;

        if (!groupedApprovalTotals[serviceType]) groupedApprovalTotals[serviceType] = 0;
        groupedApprovalTotals[serviceType] += approvalAmount;

        grandTotalApproval += approvalAmount;
    });

    // Update subtotal rows
    $('#Tmp-TableBody tr.subtotal-row').each(function () {
        const serviceType = $(this).data('service-type');
        const subtotalApproval = groupedApprovalTotals[serviceType] || 0;
        $(this).find('.subtotal-approval').text(subtotalApproval.toFixed(2));
    });

    // Update grand total row
    $('.grand-total-approval').text(grandTotalApproval.toFixed(2));
}

// Live update
$(document).on('input', '.approval-amount', recalcTotals);

//#endregion

//#region Date Formate
function formatDate(date) {
    if (!date) return '';
    let d = new Date(date);
    return d.toLocaleDateString('en-GB');
}
//#endregion

//#region Dropdown Init with Choice.js
function InitDropdown(url, selector, placeholder, callback) {
    $.ajax({
        url: url,
        type: 'GET',
        success: function (response) {
            let select = $(selector);
            select.empty().append(`<option value="">${placeholder}</option>`);

            response.forEach(function (item) {
                select.append(`<option value="${item.value}">${item.text}</option>`);
            });

            let id = select.attr('id');
            if (id && window.reinitCustomDropdownById) {
                window.reinitCustomDropdownById(id);
            }

            if (callback) callback();
        }
    });
}
//#endregion

//#region Approved Amount
var OFRApprovalDao = {
    saveApproval: function (data) {
        return $.ajax({
            url: '/OFRApproval/SaveApproval',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        });
    }
};
$('#Approve-SaveBtn').on('click', function () {

    let approvalList = [];

    $('#Tmp-TableBody tr').each(function () {
        let $tr = $(this);

        let $amount = $tr.find('.approval-amount');
        if ($amount.length === 0) return;

        let tc = $amount.data('tc');
        let actualAmount = parseFloat($amount.val()) || 0;

        let cashBank = $tr.find('.cashbank-select').val();
        let bankAccount = $tr.find('.bankaccount-select').val();

        approvalList.push({
            ActualAmount: actualAmount,
            OFRNo: $('#Appro-ReqNo').val(),
            OFR_DetailsID: tc,
            DetailsCashBank: cashBank,
            DetailsBankAccount: bankAccount
        });
    });
    console.log(approvalList);

    if (approvalList.length === 0) {
        toastr.warning("No approval data found");
        return;
    }

    OFRApprovalDao.saveApproval(approvalList)
        .done(function (res) {
            if (res.success) {
                toastr.success(res.message);
            } else {
                toastr.error(res.message);
            }
            ClearApprovedForm();
        })
        .fail(function () {
            console.error("Server error while saving approval");
        });
});
//#endregion

//#region Clear Function
function ClearApprovedForm() {
    $('.job-radio').prop('checked', false);
    $('input[name="jobSelect"]').data('waschecked', false);
    $("#Appro-ReqNo,#Appro-JobNo,#Appro-Cutomer,#Appro-invoiceNo,#Appro-Qty,#Appro-ReqDate,#Appro-Shipment,#Appro-LCValue,#Appro-MatDescription,#Appro-Weight, #bottom-Search, #Appro-SearchInput").val("");
    
    const customSelectors = ['Customer', 'Appro-ShipmentMode'];
    customSelectors.forEach(id => {
        if (window.clearCustomDropdownById) window.clearCustomDropdownById(id);
    });

    $('#Tmp-TableBody').empty();

    ClearTmpDetail();
    if (jobTable) jobTable.ajax.reload();
    BottomGrid();
}
//#endregion

//#region Clear Tmp Details Table with filtering Current User
function ClearTmpDetail() {
    $.ajax({
        url: '/OperationFund/ClearTmpDetails/',
        type: 'DELETE',
        success: function (res) {
            console.log("Current User Related Tmp Data Deleted");
            loadTmpDetails();
        },
        error: function (err) {
            console.log("Current User Related Tmp Data Deleted Failed");
        }
    });
}
//#endregion

//#region Bottom Grid Pagination
//#region Job Pagination
// function BottomPagination(totalCount) {

//     let pageSize = parseInt($('#bottom-PageSize').val());
//     let pagination = '';

//     if (pageSize === -1 || totalCount === 0) {
//         $('#bottom-Pagination').html('');
//         return;
//     }

//     let totalPages = Math.ceil(totalCount / pageSize);
//     let current = BottomPageNumber;

//     // Previous
//     pagination += `
//         <li class="page-item ${current === 1 ? 'disabled' : ''}">
//             <a class="page-link" href="#" onclick="changeBottomPage(${current - 1})">Previous</a>
//         </li>`;

//     // Always show first 2 pages
//     for (let i = 1; i <= Math.min(2, totalPages); i++) {
//         pagination += BottompageItem(i, current);
//     }

//     // Dots before middle
//     if (current > 4) {
//         pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
//     }

//     // Middle pages (current ±1)
//     let start = Math.max(3, current - 1);
//     let end = Math.min(totalPages - 2, current + 1);

//     for (let i = start; i <= end; i++) {
//         pagination += BottompageItem(i, current);
//     }

//     // Dots after middle
//     if (current < totalPages - 3) {
//         pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
//     }

//     // Always show last 2 pages
//     for (let i = Math.max(totalPages - 1, 3); i <= totalPages; i++) {
//         pagination += BottompageItem(i, current);
//     }

//     // Next
//     pagination += `
//         <li class="page-item ${current === totalPages ? 'disabled' : ''}">
//             <a class="page-link" href="#" onclick="changeBottomPage(${current + 1})">Next</a>
//         </li>`;

//     $('#bottom-Pagination').html(pagination);
// }

// helper
// function BottompageItem(page, current) {
//     return `
//         <li class="page-item ${page === current ? 'active' : ''}">
//             <a class="page-link" href="#" onclick="changeBottomPage(${page})">${page}</a>
//         </li>`;
// }


// function changeBottomPage(page) {
//     BottomPageNumber = page;
//     BottomGrid();
// }
//#endregion

//#region Update Text
// function updateBottomEntryInfo(filterCount, totalCount = null) {
//     let pageSize = $('#bottom-PageSize').val();
//     let searchTerm = $('#bottom-Search').val();

//     let text;
//     if (pageSize == -1) {
//         text = `Showing 1 to ${filterCount} of ${filterCount} entries`;
//     } else {
//         let start = ((jobPageNumber - 1) * pageSize) + 1;
//         let end = Math.min(jobPageNumber * pageSize, filterCount);
//         text = `Showing ${start} to ${end} of ${filterCount} entries`;
//     }

//     if (searchTerm && totalCount && totalCount !== filterCount) {
//         text += ` (filtered from ${totalCount} total entries)`;
//     }

//     $('.bottom-message').text(text);
// }
//#endregion

//#region Search, Filter and PageSize

// $('#bottom-Search').on('input', function () {
//     BottomPageNumber = 1;
//     if ($(this).val() === '') {
//         BottomGrid();
//     } else {
//         BottomGrid();
//     }
// });


// $('#bottom-PageSize').on('change', function () {
//     BottomPageNumber = 1;
//     BottomGrid();
// });


//#endregion

//#region Sorting
// $('#bottomGrid thead th[data-column]').on('click', function () {

//     let column = $(this).data('column');

//     if (BottomSortColumn === column) {
//         BottomSortOrder = BottomSortOrder === 'asc' ? 'desc' : 'asc';
//     } else {
//         BottomSortColumn = column;
//         BottomSortOrder = 'asc';
//     }

//     BottomPageNumber = 1;
//     BottomGrid();
// });
//#endregion
//#endregion

// //#region Top Grid Pagination
// //#region Job Pagination
// // function buildJobPagination(totalCount) {

// //     let pageSize = parseInt($('#Appro-PageSize').val());
// //     let pagination = '';

// //     if (pageSize === -1 || totalCount === 0) {
// //         $('#Appro-Pagination').html('');
// //         return;
// //     }

// //     let totalPages = Math.ceil(totalCount / pageSize);
// //     let current = jobPageNumber;

// //     // Previous
// //     pagination += `
// //         <li class="page-item ${current === 1 ? 'disabled' : ''}">
// //             <a class="page-link" href="#" onclick="changeJobPage(${current - 1})">Previous</a>
// //         </li>`;

// //     // Always show first 2 pages
// //     for (let i = 1; i <= Math.min(2, totalPages); i++) {
// //         pagination += pageItem(i, current);
// //     }

// //     // Dots before middle
// //     if (current > 4) {
// //         pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
// //     }

// //     // Middle pages (current ±1)
// //     let start = Math.max(3, current - 1);
// //     let end = Math.min(totalPages - 2, current + 1);

// //     for (let i = start; i <= end; i++) {
// //         pagination += pageItem(i, current);
// //     }

// //     // Dots after middle
// //     if (current < totalPages - 3) {
// //         pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
// //     }

// //     // Always show last 2 pages
// //     for (let i = Math.max(totalPages - 1, 3); i <= totalPages; i++) {
// //         pagination += pageItem(i, current);
// //     }

// //     // Next
// //     pagination += `
// //         <li class="page-item ${current === totalPages ? 'disabled' : ''}">
// //             <a class="page-link" href="#" onclick="changeJobPage(${current + 1})">Next</a>
// //         </li>`;

// //     $('#Appro-Pagination').html(pagination);
// // }

// // helper
// // function pageItem(page, current) {
// //     return `
// //         <li class="page-item ${page === current ? 'active' : ''}">
// //             <a class="page-link" href="#" onclick="changeJobPage(${page})">${page}</a>
// //         </li>`;
// // }


// // function changeJobPage(page) {
// //     jobPageNumber = page;
// //     loadRequisitionList();
// // }
// //#endregion

// //#region Update Text
// function updateJobEntryInfo(filterCount, totalCount = null) {

//     let pageSize = $('#Appro-PageSize').val();
//     let searchTerm = $('#Appro-SearchInput').val();

//     let text;
//     if (pageSize == -1) {
//         text = `Showing 1 to ${filterCount} of ${filterCount} entries`;
//         //return;
//     } else {
//         let start = ((jobPageNumber - 1) * pageSize) + 1;
//         let end = Math.min(jobPageNumber * pageSize, filterCount);
//         text = `Showing ${start} to ${end} of ${filterCount} entries`;
//     }

//     if (filterCount && totalCount && totalCount !== filterCount) {
//         text += ` (filtered from ${totalCount} total entries)`;
//     }

//     $('.Appro-Pagination-message').text(text);
// }
// //#endregion

// //#region Search, Filter and PageSize

// $('#Appro-SearchInput').on('input', function () {
//     jobPageNumber = 1;
//     if ($(this).val() === '') {
//         loadRequisitionList();
//     } else {
//         loadRequisitionList();
//     }
// });


// $('#Appro-PageSize').on('change', function () {
//     jobPageNumber = 1;
//     loadRequisitionList();
// });

// $('#Customer, #Appro-ShipmentMode').on('change', function () {
//     jobPageNumber = 1;
//     loadRequisitionList();
// });
// //#endregion

// //#region Sorting
// $('.table thead th[data-column]').on('click', function () {

//     let column = $(this).data('column');

//     if (jobSortColumn === column) {
//         jobSortOrder = jobSortOrder === 'asc' ? 'desc' : 'asc';
//     } else {
//         jobSortColumn = column;
//         jobSortOrder = 'asc';
//     }

//     jobPageNumber = 1;
//     loadRequisitionList();
// });
// //#endregion
// //#endregion

//#region When User Click Bottom Grid job no Edit Button
$(document).on('click', '.job-no', function () {

    let jobno = $(this).data('id') || $(this).text().trim();

    if (!jobno) {
        toastr.warning("Invalid Job No");
        return;
    }

    $.ajax({
        url: "/OFRApproval/EditButtonClicked",
        type: "GET",
        data: { jobno: jobno },
        success: function (res) {

            console.log("Edit click response:", res);

            if (!res.success) {
                toastr.warning(res.message);
                ClearApprovedForm();
                return;
            }

            let data = res.masterData;

            // Header fields fill
            $("#Appro-ReqNo").val(data.ofrNo);
            $("#Appro-ReqDate").val(formatDate(data.ofrDate));
            $("#Appro-JobNo").val(data.jobNo);
            $('#Appro-Shipment').val(data.shipmentMode);
            $("#Appro-Cutomer").val(data.customerName);
            $("#Appro-LCValue").val(data.lcValue);
            $("#Appro-invoiceNo").val(data.invoiceNo);
            $("#Appro-InvoiceValue").val(data.invoiceValue);
            $("#Appro-MatDescription").val(data.materialDescription);
            $("#Appro-Qty").val(data.quantity);
            $("#Appro-Weight").val(data.weight);

            // Load temp details grid
            loadTmpDetails(jobno);

            toastr.success(res.message);
        },
        error: function (xhr) {
            console.error("Error loading requisition data", xhr);
            ClearApprovedForm();
        }
    });
});

//#endregion