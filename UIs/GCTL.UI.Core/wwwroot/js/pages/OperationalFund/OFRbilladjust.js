//#region Ready Part
$(document).ready(function () {
    //Load Dropdown
    InitDropdown('/OperationFund/GetEmpDD', '#Bill-Customer', 'Select Employee');
    InitDropdown('/OperationFund/GetShipemtDD', '#Bill-ShipmentMode', 'Select Shipment Mode');
    //Load Requisition List For Top Grid for Bill Adjust UI
    RequisitionListForBilUI();
    ClearTmpDetail();
    // Set Entry Date
    $('#CreateDate').text(DateTimeBDFormate());
    BottomGrid();

    //Clear Event Button
    $(document).on("click", "#BillAdjust-Clear", function (e) {
        e.preventDefault();
        ClearBillAdjustForm();
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

//#region Load Requisition Entry List for Top Grid for Bill Adjust UI
function RequisitionListForBilUI() {
    $.ajax({
        url: '/OFRBillAdjust/GetAllRequisitionList',
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
                                    <input type="radio" class="form-check-input adjust-radio" name="AdjustSelect" value="${item.jobNo}">
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

//#region Load Requisition Entry List for Bottom Grid
function BottomGrid() {
    $.ajax({
        url: '/OFRBillAdjust/GetAllRequisitionForBottomGrid',
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
    RequisitionListForBilUI();
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
        RequisitionListForBilUI();
    } else {
        RequisitionListForBilUI();
    }
});


$('#Bill-PageSize').on('change', function () {
    BillPageNumber = 1;
    RequisitionListForBilUI();
});

$('#Bill-Customer, #Bill-ShipmentMode').on('change', function () {
    BillPageNumber = 1;
    RequisitionListForBilUI();
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
    RequisitionListForBilUI();
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
    BottomGrid();
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
        BottomGrid();
    } else {
        BottomGrid();
    }
});


$('#bottom-PageSize').on('change', function () {
    BottomPageNumber = 1;
    BottomGrid();
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
    BottomGrid();
});
//#endregion
//#endregion
//#endregion

//#region Tmp Grid Section

//#region Load Tmp Details Table with live totals
function TmpDetailsLoad() {
    $.get('/OFRBillAdjust/TmpDetailsBillAdjustUI', function (res) {
        const $tbody = $('#BillAdjust-Body');
        $tbody.html('');

        if (!res || res.length === 0) return;
        console.log(res);
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
                const approvalAmount = parseFloat(item.approvalAmount) || 0;
                //const diffentAmount = parseFloat(item.diffentAmount) || 0;

                //const approvalAmount = reqAmount; 
                const adjustAmount =
                    item.adjustAmount && parseFloat(item.adjustAmount) > 0
                        ? parseFloat(item.adjustAmount)
                        : approvalAmount;

                subTotalReq += reqAmount;
                subTotalApproval += approvalAmount;

                $tbody.append(`
                    <tr>
                        <td class="text-center">${item.serialNo}</td>
                        <td>${item.accountHeadName}</td>
                        <td class="text-center">${item.isReceivetable ? 'Yes' : 'No'}</td>
                        <td class="text-center">${item.serviceTypeName}</td>
                         <!-- Remarks -->
                        <td>${item.remark ?? ''}</td>
                        <!-- Req. Amount readonly -->
                        <td>
                            <input type="number"
                                   class="form-control form-control-sm text-end tmp-amount bg-light"
                                   value="${reqAmount.toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}"
                                   data-service-type="${serviceType}"
                                   readonly />
                        </td>
                        <!-- Approval Amount readonly -->
                        <td>
                            <input type="number"
                                   class="form-control form-control-sm text-end bg-light approval-amount"
                                   value="${approvalAmount.toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}"
                                   data-service-type="${serviceType}" disabled />
                        </td>
                         <!-- Adjust Amount editable -->
                        <td>
                            <input type="number"
                                   class="form-control form-control-sm text-end adjust-amount"
                                   value="${adjustAmount.toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}"
                                   data-service-type="${serviceType}" />
                        </td>
                         <!-- Differen Amount readonly -->
                        <td>
                            <input type="number"
                                   class="form-control form-control-sm text-end diff-amount bg-light"
                                   value="${(approvalAmount - adjustAmount).toFixed(2)}"
                                   data-tc="${item.ofrDetailsID}"
                                   data-service-type="${serviceType}"
                                   readonly />
                        </td>
                    </tr>
                `);
            });

            // Subtotal row
            $tbody.append(`
                                <tr class="subtotal-row" data-service-type="${serviceType}">
                                    <td colspan="5" class="text-end text-info fw-bold fs-8">Subtotal</td>
                                    <td class="subtotal-req text-end fw-bold text-info fs-8">${subTotalReq.toFixed(2)}</td>
                                    <td class="subtotal-approval text-end fw-bold text-info fs-8">${subTotalApproval.toFixed(2)}</td>
                                    <td class="subtotal-adjust text-end fw-bold text-info fs-8">0.00</td>
                                    <td class="subtotal-diff text-end fw-bold text-info fs-8">0.00</td>
                                </tr>
                                `);

        });

        // Grand Total row
        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
        const grandTotalApproval = grandTotalReq;

        $tbody.append(`
                                        <tr class="grand-total-row">
                                            <td colspan="5" class="text-end text-primary fs-8 fw-bold">Grand Total</td>
                                            <td class="grand-total-req text-end fw-bold text-primary fs-8">${grandTotalReq.toFixed(2)}</td>
                                            <td class="grand-total-approval text-end fw-bold text-primary fs-8">${grandTotalApproval.toFixed(2)}</td>
                                            <td class="grand-total-adjust text-end fw-bold text-primary fs-8">0.00</td>
                                            <td class="grand-total-diff text-end fw-bold text-primary fs-8">0.00</td>
                                        </tr>
                                        `);

        recalcAdjustAndDiffTotals();

    });
}
//#endregion

//#region Calculate SubTotal and Grand Total
function recalcAdjustAndDiffTotals() {

    let adjustTotalsByService = {};
    let diffTotalsByService = {};

    let grandAdjust = 0;
    let grandDiff = 0;

    $('#BillAdjust-Body tr').each(function () {
        const $tr = $(this);
        if ($tr.hasClass('subtotal-row') || $tr.hasClass('grand-total-row')) return;

        const serviceType = $tr.find('.adjust-amount').data('service-type');

        const approveAmt = parseFloat($tr.find('.approval-amount').val()) || 0;
        const adjustAmt = parseFloat($tr.find('.adjust-amount').val()) || 0;

        const diffAmt = approveAmt - adjustAmt;

        // set difference live
        $tr.find('.diff-amount').val(diffAmt.toFixed(2));

        // group totals
        if (!adjustTotalsByService[serviceType]) {
            adjustTotalsByService[serviceType] = 0;
            diffTotalsByService[serviceType] = 0;
        }

        adjustTotalsByService[serviceType] += adjustAmt;
        diffTotalsByService[serviceType] += diffAmt;

        grandAdjust += adjustAmt;
        grandDiff += diffAmt;
    });

    // update subtotal rows
    $('#BillAdjust-Body tr.subtotal-row').each(function () {
        const serviceType = $(this).data('service-type');

        $(this).find('.subtotal-adjust')
            .text((adjustTotalsByService[serviceType] || 0).toFixed(2));

        $(this).find('.subtotal-diff')
            .text((diffTotalsByService[serviceType] || 0).toFixed(2));
    });

    // update grand total
    $('.grand-total-adjust').text(grandAdjust.toFixed(2));
    $('.grand-total-diff').text(grandDiff.toFixed(2));
}

// Live update
$(document).on('input', '.adjust-amount', function () {
    recalcAdjustAndDiffTotals();
});


//#endregion

//#endregion

//#region Dropdown Init Section
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
//#endregion

//#region Clear Function Section

//#region Clear Tmp Details Table with filtering Current User
function ClearTmpDetail() {
    $.ajax({
        url: '/OperationFund/ClearTmpDetails/',
        type: 'DELETE',
        success: function (res) {
            console.log("Current User Related Tmp Data Deleted");
            TmpDetailsLoad();

        },
        error: function (err) {
            console.log("Current User Related Tmp Data Deleted Failed");
        }
    });
}
//#endregion

//#region Clear Function
function ClearBillAdjustForm() {
    $('.job-radio').prop('checked', false);
    $("#BillAdjust-ReqNo,#BillAdjust-JobNo,#BillAdjust-Cutomer,#BillAdjust-invoiceNo,#BillAdjust-Qty,#BillAdjust-ReqDate,#BillAdjust-Shipment,#BillAdjust-LCValue,#BillAdjust-MatDescription,#BillAdjust-Weight, #BillAdjust-Search, #Appro-SearchInput, #BillAdjust-Port, #BillAdjust-DutyChalan, #BillAdjust-Asycoda, #BillAdjust-Shipping").val("");
    ClearTmpDetail();
    RequisitionListForBilUI();
    //    BottomGrid();
}
//#endregion

//#endregion

//#region Get Requisition Info in Input field and Copied Details Data To Tmp Table
$(document).on('click', 'input[name="AdjustSelect"]', function () {

    if ($(this).prop('checked') && $(this).data('waschecked')) {
        // Radio was already checked → now unchecked
        $(this).prop('checked', false);
        $(this).data('waschecked', false);

        // Clear input fields
        ClearBillAdjustForm();

    } else {
        // Normal check
        $('input[name="AdjustSelect"]').data('waschecked', false);
        $(this).data('waschecked', true);

        // Load header data
        let jobno = $(this).val();

        $.ajax({
            url: "/OFRBillAdjust/BillAdjustUIDetails",
            type: "GET",
            data: { jobno: jobno },
            success: function (headerdata) {
                console.log(headerdata);
                if (!headerdata) {
                    toastr.warning("No job data found!");
                    ClearBillAdjustForm();
                    return;
                }
                var data = headerdata.masterData;
                $("#BillAdjust-ReqNo").val(data.ofrNo);
                $("#BillAdjust-ReqDate").val(formatDate(data.ofrDate));
                $("#BillAdjust-JobNo").val(data.jobNo);
                $('#BillAdjust-Shipment').val(data.shipmentMode);
                $("#BillAdjust-Cutomer").val(data.customerName);
                $("#BillAdjust-LCValue").val(data.lcValue);
                $("#BillAdjust-invoiceNo").val(data.invoiceNo);
                $("#BillAdjust-MatDescription").val(data.materialDescription);
                $("#BillAdjust-Qty").val(data.quantity);
                $("#BillAdjust-Weight").val(data.weight);

                TmpDetailsLoad();

            },
            error: function () {
                console.error("Error loading header data", xhr);
                ClearBillAdjustForm();
            }
        });
    }
});

//#endregion

//#region Bill Adjust Amount
var OFRBillAdjustDao = {
    saveBillAdjust: function (data) {
        return $.ajax({
            url: '/OFRBillAdjust/SaveApprovalBillAdjust',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        });
    }
};
$('#BillAdjust-SaveBtn').on('click', function () {

    let adjustList = [];

    // Get the global charges
    let portCharge = parseFloat($('#BillAdjust-Port').val()) || 0;
    let dutyChalanCharge = parseFloat($('#BillAdjust-DutyChalan').val()) || 0;
    let asycodaCharge = parseFloat($('#BillAdjust-Asycoda').val()) || 0;
    let shipingCharge = parseFloat($('#BillAdjust-Shipping').val()) || 0;

    $('#BillAdjust-Body tr').each(function () {
        let $tr = $(this);

        let $adjustamount = $tr.find('.adjust-amount');
        if ($adjustamount.length === 0) return;

        let ofrDetailsID = $adjustamount.data('tc');
        let adjustAmount = parseFloat($adjustamount.val()) || 0;
        let diffAmount = parseFloat($tr.find('.diff-amount').val()) || 0;

        adjustList.push({
            OFRNo: $('#BillAdjust-ReqNo').val(),
            OFR_DetailsID: ofrDetailsID,
            AdjustAmount: adjustAmount,
            DifferentAmount: diffAmount,
            PortCharge: portCharge,
            DutyChalanCharge: dutyChalanCharge,
            AsycodaCharge: asycodaCharge,
            ShipingCharge: shipingCharge
        });
    });

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
            ClearBillAdjustForm();
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
        url: "/OFRBillAdjust/EditButtonClicked",
        type: "GET",
        data: { jobno: jobno },
        success: function (res) {

            console.log("Edit click response:", res);

            if (!res.success) {
                toastr.warning(res.message);
                ClearBillAdjustForm();
                return;
            }

            let data = res.masterData;

            // Header fields fill
            $("#BillAdjust-ReqNo").val(data.ofrNo);
            $("#BillAdjust-ReqDate").val(formatDate(data.ofrDate));
            $("#BillAdjust-JobNo").val(data.jobNo);
            $('#BillAdjust-Shipment').val(data.shipmentMode);
            $("#BillAdjust-Cutomer").val(data.customerName);
            $("#BillAdjust-LCValue").val(data.lcValue);
            $("#BillAdjust-invoiceNo").val(data.invoiceNo);
            $("#BillAdjust-MatDescription").val(data.materialDescription);
            $("#BillAdjust-Qty").val(data.quantity);
            $("#BillAdjust-Weight").val(data.weight);
            $('#BillAdjust-Port').val(data.portCharge)
            $('#BillAdjust-DutyChalan').val(data.dutyChalanCharge)
            $('#BillAdjust-Asycoda').val(data.asycodaCharge)
            $('#BillAdjust-Shipping').val(data.shipingCharge)
            $('#CreateDate').text(formatDatewithTime(data.billAdjustApprovedDate));
            $('#UpdateDate').text(formatDatewithTime(data.billAdjustModifiedDate));


            // Load temp details grid
            TmpDetailsLoad();

            toastr.success(res.message);
        },
        error: function (xhr) {
            console.error("Error loading requisition data", xhr);
            ClearBillAdjustForm();
        }
    });
});

//#endregion

//#region Date Helper Section
//#region Date Formate
function formatDate(date) {
    if (!date) return '';
    let d = new Date(date);
    return d.toLocaleDateString('en-GB');
}
function formatDatewithTime(date) {
    if (!date) return '';

    let d = new Date(date);

    return d.toLocaleString('en-GB', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
    })
        .replace(',', '')
        .replace('am', 'AM')
        .replace('pm', 'PM');

}

//#endregion

//#region Function to get current date in Bangladesh time
function DateTimeBDFormate() {
    // BD timezone offset is +6:00 from UTC
    let now = new Date();
    let utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    let bdOffset = 6 * 60 * 60 * 1000;
    let bdTime = new Date(utc + bdOffset);

    // Format DD/MM/YYYY hh:MM AM/PM
    let day = bdTime.getDate().toString().padStart(2, '0');
    let month = (bdTime.getMonth() + 1).toString().padStart(2, '0');
    let year = bdTime.getFullYear();

    let hours = bdTime.getHours();
    let minutes = bdTime.getMinutes().toString().padStart(2, '0');

    let ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // 0 => 12

    return `${day}/${month}/${year} ${hours}:${minutes} ${ampm}`;
}

//#endregion
//#endregion