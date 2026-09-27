//#region Ready Part
$(document).ready(function () {
    //Load Dropdown
    InitDropdown('/OperationFund/GetEmpDD', '#Bill-Customer', 'Select Customer');
    InitDropdown('/OperationFund/GetShipemtDD', '#Bill-ShipmentMode', 'Select Shipment Mode');

    $('#Bill-Customer, #Bill-ShipmentMode').on('change', function () {
        if (jobTable) jobTable.ajax.reload();
    });

    //Load Top and Bottom Grid
    LoadTopGrid();
    LoadBottomGrid();
    ClearTmpDetail();

    //Clear Event Button
    $(document).on("click", "#adjustApprove-Clear", function (e) {
        e.preventDefault();
        ClearForm();
    });
});
//#endregion

//#region Grid Section
let jobTable = null;
let bottomTable = null;
window.gctlRenderers = window.gctlRenderers || {};
window.gctlRenderers.formatDate = function (data) { return formatDate(data); };

//#region Load Top Grid
function LoadTopGrid() {
    jobTable = $.gctlGrid.initFromModel({
        gridId: 'Bill-Table',
        ajaxData: function (d) {
            d.customerid = $('#Bill-Customer').val();
            d.shipmentmodeid = $('#Bill-ShipmentMode').val();
        }
    });
}
//#endregion

//#region Load Bottom Grid
function LoadBottomGrid() {
    if (bottomTable) {
        bottomTable.ajax.reload();
        return;
    }
    bottomTable = $.gctlGrid.initFromModel({
        gridId: 'bottomGrid'
    });
}
//#endregion
//#endregion

//#region Dropdown Init
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

//#region Get Requisition Info in Input field and Copied Details Data To Tmp Table
$(document).on('click', 'input[name="ApproveChecked"]', function () {
    if ($(this).prop('checked') && $(this).data('waschecked')) {
        // Radio was already checked -> now unchecked
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
                if (!headerdata || !headerdata.masterData) {
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
            error: function (xhr) {
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
            $('#Adjust-Body').html('');
        },
        error: function (err) {
            console.log("Current User Related Tmp Data Deleted Failed");
        }
    });
}
//#endregion

//#region Clear Function
function ClearForm() {
    $('input[name="ApproveChecked"]').prop('checked', false).data('waschecked', false);
    $("#Adjust-ReqNo,#Adjust-JobNo,#Adjust-Cutomer,#Adjust-invoiceNo,#Adjust-Qty,#Adjust-ReqDate,#Adjust-Shipment,#Adjust-LCValue,#Adjust-MatDescription,#Adjust-Weight").val("");
    ClearTmpDetail();
    if (jobTable) jobTable.ajax.reload();
    if (bottomTable) bottomTable.ajax.reload();
}
//#endregion

//#endregion

//#region Date Format
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
                        <td class="text-center">${item.serialNo ?? ''}</td>
                        <td>${item.accountHeadName ?? ''}</td>
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
                                <option value="Cash">Cash</option>
                                <option value="Bank">Bank</option>
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

                // Cash/Bank value set (handles "Bank", "10200400003", "Cash", "10200400001", "Employee")
                let cbVal = item.cashBank || '';
                if (cbVal === '10200400003') cbVal = 'Bank';
                else if (cbVal === '10200400001') cbVal = 'Cash';
                $row.find('.cashbank-select').val(cbVal);

                // Load bank accounts and auto-select Bank
                loadBankAccounts($row.find('.cashbank-select'), item.bankAccount);
            });

            // Subtotal row
            $tbody.append(`
                <tr class="subtotal-row" data-service-type="${serviceType}">
                    <td colspan="3" class="text-end fw-bold fs-8">Subtotal</td>
                    <td class="subtotal-req text-end fw-bold fs-8">${subTotalReq.toFixed(2)}</td>
                    <td class="subtotal-approval text-end fw-bold fs-8">${subTotalApproval.toFixed(2)}</td>
                    <td class="subtotal-adjust text-end fw-bold fs-8">0.00</td>
                    <td class="subtotal-confirm text-end fw-bold fs-8">0.00</td>
                    <td class="subtotal-diff text-end fw-bold fs-8">0.00</td>
                    <td colspan="2"></td>
                </tr>
            `);
        });

        // Grand Total
        const grandTotalReq = res.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0);
        const grandTotalApproval = grandTotalReq;

        $tbody.append(`
            <tr class="grand-total-row">
                <td colspan="3" class="text-end fs-8 fw-bold">Grand Total</td>
                <td class="grand-total-req text-end fw-bold fs-8">${grandTotalReq.toFixed(2)}</td>
                <td class="grand-total-approval text-end fw-bold fs-8">${grandTotalApproval.toFixed(2)}</td>
                <td class="grand-total-adjust text-end fw-bold fs-8">—</td>
                <td class="grand-total-confirm text-end fw-bold fs-8">0.00</td>
                <td class="grand-total-diff text-end fw-bold fs-8">0.00</td>
                <td colspan="2"></td>
            </tr>
        `);

        // Recalculate totals for Adjust / Confirm / Diff
        recalcAdjustAndDiffTotals();
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

//#region Load Bank Accounts Dropdown
function loadBankAccounts($cashBankSelect, selectedBank = null) {
    let value = $cashBankSelect.val();
    let tc = $cashBankSelect.data('tc');
    let $bankSelect = $('.bankaccount-select[data-tc="' + tc + '"]');

    // Clear dropdown
    $bankSelect.empty().append('<option value="">Select Bank Account</option>');

    if (!value || value === 'Cash') {
        $bankSelect.prop('disabled', true);
        return;
    }

    $bankSelect.prop('disabled', false);

    // Load bank accounts via AJAX
    $.get('/OFRAdjustApproval/BankAccountDD', { value: value }, function (res) {
        if (res && res.length > 0) {
            res.forEach(item => {
                $bankSelect.append(`<option value="${item.value}">${item.text}</option>`);
            });
        }

        // Auto-select Bank Account if provided
        if (selectedBank) {
            $bankSelect.val(selectedBank);
        } else if (value === '10200400001' && res && res.length > 0) {
            $bankSelect.val(res[0].value);
        }
    });
}

$(document).on('change', '.cashbank-select', function () {
    loadBankAccounts($(this));
});
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
    let ofrNo = $(this).data('id') || $(this).text().trim();

    if (!ofrNo) {
        toastr.warning("Invalid OFR No");
        return;
    }

    $.ajax({
        url: "/OFRAdjustApproval/EditDetails",
        type: "GET",
        data: { ofrNo: ofrNo },
        success: function (res) {
            console.log("Edit click response:", res);

            if (!res.success || !res.masterData) {
                toastr.warning(res.message || "Failed to load details");
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
            LoadTmpGrid(data.jobNo);
            toastr.success(res.message);
        },
        error: function (xhr) {
            console.error("Error loading requisition data", xhr);
            ClearForm();
        }
    });
});
//#endregion