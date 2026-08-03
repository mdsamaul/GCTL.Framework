//#region Renderers
// window.gctlRenderers.ofrLink = function (data, type, row) {
//     return `<span class="ofr-no-link" style="cursor:pointer; text-decoration:underline;">${data ?? ''}</span>`;
// };
// formatDate assumed already registered globally (used elsewhere, e.g. Job Entry grid)
//#endregion

//#region Grid Init
let requisitionTable = null;

function LoadRequisitionList() {
    if (requisitionTable) {
        requisitionTable.ajax.reload();
        return;
    }
    requisitionTable = $.gctlGrid.initFromModel({ gridId: 'FundTable' });
}
//#endregion

//#region Delete Single & Multi
$('#Req-DeleteBtn').on('click', function () {
    let ofrNos = $('#FundTable input.form-check-input:checked')
        .map(function () { return $(this).val(); }).get();

    if (ofrNos.length === 0) {
        toastr.warning('Please select at least one record');
        return;
    }

    $.ajax({
        url: '/OperationFund/DeleteRequisition',
        type: 'POST',
        data: JSON.stringify(ofrNos),
        contentType: 'application/json',
        success: function () {
            toastr.success('Deleted successfully');
            requisitionTable.ajax.reload();
            loadJobEntryList();
            ClearAfterClikButton();
            ClearTmpDetail();
        },
        error: function () {
            toastr.error('Delete Failed!');
        }
    });
});
//#endregion

//#region Requisition Details (unchanged)
function SetRequisitionDate(dateStr) {
    if (reqDatePicker) {
        let jsDate = dateStr ? new Date(dateStr) : new Date();
        reqDatePicker.setDate(jsDate, true, "d-m-Y");
    }
}

function copyToTmp(offrNo) {
    return $.ajax({
        url: '/OperationFund/CopyToTmp',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(offrNo)
    });
}

function LoadRequisitionDetails(offrNo) {
    $.ajax({
        url: '/OperationFund/GetRequisitionDetails',
        type: 'GET',
        data: { offrNo: offrNo },
        success: function (res) {
            console.log(res);
            if (!res) {
                toastr.warning('No data found');
                return;
            }
            $('#Job-JoBNO').val(res.jobNo ?? '');
            $('#Job-ShipmentModeInput').val(res.shipmentMode ?? '');
            $('#Job-CustomerName').val(res.customerName ?? '');
            $('#Job-LCValue').val(res.lcValue ?? '');
            $('#Job-InvoiceNo').val(res.invoiceNo ?? '');
            $('#Job-InvoiceValue').val(res.invoiceValue ?? '');
            $('#Job-RequisitionNo').val(res.reqNo ?? '');
            $('#CustomerID').val(res.customerID ?? '');
            $('#ShipmentModeID').val(res.shipmentModeID ?? '');
            SetRequisitionDate(res.reqDate);
            // if (currencyChoice) currencyChoice.setChoiceByValue(res.currencyID);
            $('#Job-HAWB').val(res.hawb ?? '');
            $('#Job-MatDescription').val(res.materialDescription ?? '');
            $('#Job-Qty').val(res.qty ?? '');
            $('#Job-Weight').val(res.weight ?? '');
            setCurrencyValue('#Job-Currencies', res.currencyID ?? '', true);
        },
        error: function () {
            toastr.error('Failed to load requisition details!');
        }
    });
}
//#endregion

//#region Ready
$(document).ready(function () {
    LoadRequisitionList();

    $(document).on('click', '.ofr-no-link', function () {
        let ofrNo = $(this).text().trim();
        copyToTmp(ofrNo).done(function () {
            LoadRequisitionDetails(ofrNo);
            loadTmpDetails();
        }).fail(function () {
            toastr.error('Failed to load Expenses in Details!');
        });
    });
});
//#endregion