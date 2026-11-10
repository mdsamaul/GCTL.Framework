//#region Ready Part
$(document).ready(function () {
    //Load Dropdown
    InitDropdown('/OperationFund/GetEmpDD', '#Bill-Customer', 'Select Customer');
    InitDropdown('/OperationFund/GetShipemtDD', '#Bill-ShipmentMode', 'Select Shipment Mode');

    $('#Bill-Customer, #Bill-ShipmentMode').on('change', function () {
        if (jobTable) jobTable.ajax.reload();
    });

    $('#dateFrom, #dateTo').on('change keyup', function () {
        if (jobTable) jobTable.ajax.reload();
    });

    //Load Requisition List For Top Grid for Bill Adjust UI
    loadRequisitionList();
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
let jobTable = null;
let bottomTable = null;
window.gctlRenderers = window.gctlRenderers || {};
window.gctlRenderers.formatDate = function (data) { return formatDate(data); };

function loadRequisitionList() {
    jobTable = $.gctlGrid.initFromModel({
        gridId: 'Bill-Table',
        ajaxData: function (d) {
            d.customerid = $('#Bill-Customer').val();
            d.shipmentmodeid = $('#Bill-ShipmentMode').val();
            d.dateFrom = $('#dateFrom').val();
            d.dateTo = $('#dateTo').val();
        }
    });
}

function BottomGrid() {
    if (bottomTable) {
        bottomTable.ajax.reload();
        return;
    }
    bottomTable = $.gctlGrid.initFromModel({
        gridId: 'bottomGrid'
    });
}
//#endregion

//#region Tmp Grid Section

//#region Load Tmp Details Table with live totals
function TmpDetailsLoad(jobNo) {
    if (!jobNo) jobNo = $('#BillAdjust-JobNo').val();
    $.get('/OFRBillAdjust/TmpDetailsBillAdjustUI', { jobNo: jobNo }, function (res) {
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
                    <td colspan="5" class="text-end fw-bold fs-8">Subtotal</td>
                    <td class="subtotal-req text-end fw-bold fs-8">${subTotalReq.toFixed(2)}</td>
                    <td class="subtotal-approval text-end fw-bold fs-8">${subTotalApproval.toFixed(2)}</td>
                    <td class="subtotal-adjust text-end fw-bold fs-8">0.00</td>
                    <td class="subtotal-diff text-end fw-bold fs-8">0.00</td>
                </tr>
            `);

        });

        // Grand Total row
        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
        const grandTotalApproval = grandTotalReq;

        $tbody.append(`
            <tr class="grand-total-row">
                <td colspan="5" class="text-end fs-8 fw-bold">Grand Total</td>
                <td class="grand-total-req text-end fw-bold fs-8">${grandTotalReq.toFixed(2)}</td>
                <td class="grand-total-approval text-end fw-bold fs-8">${grandTotalApproval.toFixed(2)}</td>
                <td class="grand-total-adjust text-end fw-bold fs-8">0.00</td>
                <td class="grand-total-diff text-end fw-bold fs-8">0.00</td>
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
function InitDropdown(url, selector, placeholder, callback) {
    $.ajax({
        url: url,
        type: 'GET',
        success: function (response) {

            let select = $(selector);
            select.empty().append(`<option value="">${placeholder}</option>`);

            if (response && Array.isArray(response)) {
                response.forEach(function (item) {
                    select.append(`<option value="${item.value}">${item.text}</option>`);
                });
            }

            let id = select.attr('id');
            if (id && window.reinitCustomDropdownById) {
                window.reinitCustomDropdownById(id);
            }

            if (callback) callback();
        }
    });
}
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
    $('input[name="AdjustSelect"]').data('waschecked', false);
    $("#BillAdjust-ReqNo,#BillAdjust-JobNo,#BillAdjust-Cutomer,#BillAdjust-invoiceNo,#BillAdjust-Qty,#BillAdjust-ReqDate,#BillAdjust-Shipment,#BillAdjust-LCValue,#BillAdjust-MatDescription,#BillAdjust-Weight, #BillAdjust-Search, #Appro-SearchInput, #BillAdjust-Port, #BillAdjust-DutyChalan, #BillAdjust-Asycoda, #BillAdjust-Shipping").val("");
    ClearTmpDetail();
    if (jobTable) jobTable.ajax.reload();
    if (bottomTable) bottomTable.ajax.reload();
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

                TmpDetailsLoad(jobno);

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

    let ofrNo = $(this).data('id') || $(this).text().trim();

    if (!ofrNo) {
        toastr.warning("Invalid OFR No");
        return;
    }

    $.ajax({
        url: "/OFRBillAdjust/EditButtonClicked",
        type: "GET",
        data: { ofrNo: ofrNo },
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
            TmpDetailsLoad(data.jobNo);

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