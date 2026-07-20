//#region Validation Function for Tmp Details
function validateTmpDetails(model) {
    if (!$("#Job-RequisitionNo").val()) {
        toastr.warning("Please Enter Requisition No.");
        return false;
    }

    if (!$("#RequisitionDate").val()) {
        toastr.warning("Please Enter Requisition Date.");
        return false;
    }

    if (!$("#ServiceType").val()) {
        toastr.warning("Please Select Service Type.");
        return false;
    }

    //if (!$("#CustomerID").val()) {
    //    toastr.warning("Please Enter Customer Name.");
    //    return false;
    //}

    if (!$("#ShipmentModeID").val()) {
        toastr.warning("Please Enter Shipment Mode.");
        return false;
    }

    return true;
}
//#endregion

//#region Validation Function for Master 
function MasterValidation(masterdata) {
    if (!$("#Job-RequisitionNo").val()) {
        toastr.warning("Please Enter Requisition No.");
        return false;
    }

    if (!$("#RequisitionDate").val()) {
        toastr.warning("Please Enter Requisition Date.");
        return false;
    }

    //if (!$("#ServiceType").val()) {
    //    toastr.warning("Please Select Service Type.");
    //    return false;
    //}


    if (!$("#ShipmentModeID").val()) {
        toastr.warning("Please Enter Shipment Mode.");
        return false;
    }

    return true;
}
//#endregion

//#region Save TMp Details
function saveTmpDetails() {
    const reqDateVal = $('#RequisitionDate').val();

    const model = {
        Tc: null,
        CustomerNameID: $("#CustomerID").val(),
        ShipmentModeID: $("#ShipmentModeID").val(),
        ReqNo: $("#Job-RequisitionNo").val(),
        RequDate: reqDateVal ? new Date(reqDateVal.split('-').reverse().join('-')).toISOString() : null,
        ServiceTypeID: $("#ServiceType").val(),
        AccountHeadID: $("#AccountHead").val(),
        Amount: $("#Amount").val(),
        Remark: $("#Remark").val(),
        EmployeeID: null,
        JobNo: $("#Job-JoBNO").val()
    };

    if (!validateTmpDetails(model)) return;

    $.ajax({
        url: "/OperationFund/CreateTmpDetails",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(model),
        success: function (res) {
            toastr.success(res.message || "Data Saved Successfully");
            clearTmpForm();
            loadTmpDetails();
        },
        error: function (err) {
            toastr.error(err.responseJSON?.message || "Insertion Failed");
        }
    });
}

//#endregion

//#region Clear Function
function ClearAfterClikButton() {
    //uncheck radion button
    $('.job-radio').prop('checked', false);
    $("#Job-JoBNO,#Job-ShipmentModeInput,#Job-CustomerName,#Job-LCValue,#Job-InvoiceNo,#Job-InvoiceValue,#Job-HAWB,#Job-MatDescription,#Job-Qty,#Job-Weight,#Job-RequisitionNo,#Amount,#Remark").val("");
    $("#Job-SearchInput,#Req-Search").val("");
    // Choices dropdowns reset
    const choicesSelectors = ['#ServiceType', '#AccountHead', '#Customer', '#Job-ShipmentMode', '#Job-Currencies'];
    choicesSelectors.forEach(sel => {
        let choiceInstance = $(sel).data('choices');
        if (choiceInstance) {
            choiceInstance.setChoiceByValue('');
        }
    });
    //Uncheck Select All checkbox
    $('#Req-selectAll').prop('checked', false);

    //Uncheck all row checkboxes
    $('input[name="ReqSelect"]').prop('checked', false);
    LoadRequisitionList();
    loadJobEntryList();
}
function ClearFullForm() {
    $('.job-radio').prop('checked', false);
    $("#Job-JoBNO,#Job-ShipmentModeInput,#Job-CustomerName,#Job-LCValue,#Job-InvoiceNo,#Job-InvoiceValue,#Job-HAWB,#Job-MatDescription,#Job-Qty,#Job-Weight,#Job-RequisitionNo,#Amount,#Remark").val("");
    // Choices dropdowns reset
    const choicesSelectors = ['#ServiceType', '#AccountHead', '#Customer', '#Job-ShipmentMode', '#Job-Currencies'];
    choicesSelectors.forEach(sel => {
        let choiceInstance = $(sel).data('choices');
        if (choiceInstance) {
            choiceInstance.setChoiceByValue('');
        }
    });

}
function clearTmpForm() {
    $("#Amount,#Remark").val("");

    // Choices dropdowns reset
    const choicesSelectors = ['#ServiceType', '#AccountHead'];
    choicesSelectors.forEach(sel => {
        let choiceInstance = $(sel).data('choices');
        if (choiceInstance) {
            choiceInstance.setChoiceByValue('');
        }
    });
}

//#endregion

//#region Delete Tmp Details Data with CurrentUser ID
function deleteTmpDetail(tc) {
    console.log("Deleting TC:", tc);

    $.ajax({
        url: '/OperationFund/DeleteTmpDetail/' + tc,
        type: 'DELETE',
        success: function (res) {
            toastr.success(res.message || "Deleted successfully");
            loadTmpDetails();
        },
        error: function (err) {
            toastr.error(err.responseJSON?.message || "Delete Failed");
        }
    });
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

//#region Save Master & Details
function MasterAndDetailSaved() {
    const reqDate = $('#RequisitionDate').val();

    const masterdata = {
        JobNo: $("#Job-JoBNO").val(),
        OFFRDate: reqDate ? new Date(reqDate.split('-').reverse().join('-')).toISOString() : null,
        ExpenseTypeID: $("#ShipmentModeID").val(),
        ServiceTypeID: $("#ServiceType").val(),
        Details: []
    };
    // Collect all tmp amounts from input fields
    $('.tmp-amount').each(function () {
        const tc = $(this).data('tc'); // row id
        const amount = parseFloat($(this).val()) || 0;

        masterdata.Details.push({
            Tc: tc,
            Amount: amount
        });
    });
    console.log("Master Data,", masterdata);
    if (!MasterValidation(masterdata)) return;

    $.ajax({
        url: "/OperationFund/CreateMasterDetails",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(masterdata),
        success: function (res) {
            toastr.success(res.message || "Data Saved Successfully");
            ClearFullForm();
            LoadRequisitionList();
            loadJobEntryList();
            loadTmpDetails();
        },
        error: function (err) {
            toastr.error(err.responseJSON?.message || "Insertion Failed");
        }
    });
}
//#endregion

//#region Ready part
$(document).ready(function () {
    //Clear tmp table
    ClearTmpDetail();
    //Load Tmp Table
    loadTmpDetails();
    //Click Tmp Save Button
    $(document).on("click", "#TmpSaveBtn", function (e) {
        e.preventDefault();
        saveTmpDetails();
    });
    //Click Master & Details Save Button
    $(document).on("click", "#Req-SaveBtn", function (e) {
        e.preventDefault();
        MasterAndDetailSaved();
    });

    //Click Master & Details Clear Button
    $(document).on("click", "#Req-Clear", function (e) {
        e.preventDefault();
        ClearAfterClikButton();
        ClearTmpDetail();
    });
});
//#endregion


//#region Load Tmp Details Table with live totals
function loadTmpDetails() {
    $.get('/OperationFund/GetAllTmpVoucherDetails', function (res) {
        const $tbody = $('#expenseTableBody');
        $tbody.html('');

        if (!res || res.length === 0) return;

        let groupedData = {};

        // Group by serviceType
        res.forEach(item => {
            const service = item.serviceTypeName || 'Other';
            if (!groupedData[service]) groupedData[service] = [];
            groupedData[service].push(item);
        });

        // Render grouped rows + subtotal rows
        Object.keys(groupedData).forEach(serviceType => {
            let subTotal = 0;

            groupedData[serviceType].forEach(item => {
                const amount = parseFloat(item.amount) || 0;
                subTotal += amount;

                $tbody.append(`
                    <tr>
                        <td class="text-center">${item.serialNo}</td>
                        <td>${item.accountHeadName}</td>
                        <td class="text-center">${item.isReceivetable ? 'Yes' : 'No'}</td>
                        <td class="text-center">${item.serviceTypeName}</td>
                        <td class="text-end">
                            <input type="number"
                                   class="form-control form-control-sm text-end tmp-amount"
                                   value="${amount.toFixed(2)}"
                                   data-tc="${item.tc}"
                                   data-service-type="${serviceType}" />
                        </td>
                        <td>${item.remark ?? ''}</td>
                        <td class="text-center">
                            <button class="btn btn-sm btn-outline-danger border-0" onclick="deleteTmpDetail(${item.tc})">
                                <i class="bi bi-trash3-fill"></i> Delete
                            </button>
                        </td>
                    </tr>
                `);
            });

            // Subtotal row
            $tbody.append(`
                <tr class="subtotal-row" data-service-type="${serviceType}">
                    <td colspan="4" class="text-end text-info fs-8 fw-bold">
                        Subtotal
                    </td>
                    <td class="subtotal-amount text-end text-info fs-8 fw-bold">${subTotal.toFixed(2)}</td>
                    <td colspan="2"></td>
                </tr>
            `);
        });

        // Grand Total row
        let grandTotal = Object.values(groupedData).reduce((gt, items) => {
            return gt + items.reduce((st, item) => st + (parseFloat(item.amount) || 0), 0);
        }, 0);

        // Grand Total row
        $tbody.append(`
            <tr class="grand-total-row">
                <td colspan="4" class="text-end text-primary fs-8 fw-bold">Grand Total</td>
                <td class="grand-total-amount text-end text-primary fs-8 fw-bold">${grandTotal.toFixed(2)}</td>
                <td colspan="2"></td>
            </tr>
        `);

    });
}

function recalcTotals() {
    let groupedTotals = {};
    let grandTotal = 0;

    // subtotal per service type
    $('#expenseTableBody tr').each(function () {
        const $tr = $(this);
        if ($tr.hasClass('subtotal-row') || $tr.hasClass('grand-total-row')) return;

        const serviceType = $tr.find('input.tmp-amount').data('service-type');
        const amount = parseFloat($tr.find('input.tmp-amount').val()) || 0;

        if (!groupedTotals[serviceType]) groupedTotals[serviceType] = 0;
        groupedTotals[serviceType] += amount;
    });

    // Update subtotal rows & sum grand total
    $('#expenseTableBody tr.subtotal-row').each(function () {
        const serviceType = $(this).data('service-type');
        const subtotal = groupedTotals[serviceType] || 0;
        $(this).find('.subtotal-amount').text(subtotal.toFixed(2));
        grandTotal += subtotal;
    });

    // Update Grand Total
    $('.grand-total-amount').text(grandTotal.toFixed(2));
}

// Live update
$(document).on('input', '.tmp-amount', recalcTotals);
//#endregion




