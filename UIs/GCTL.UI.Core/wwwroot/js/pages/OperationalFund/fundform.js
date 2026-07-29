//#region Global Veriable
let serviceTypeChoice = null;
let accountHeadChoice = null;
let customerChoice = null;
let shipmentModeChoice = null;
let currencyChoice = null;
//#endregion

//#region Requisition Current Date
let reqDatePicker = null; // global

function RequisitionDate() {
    reqDatePicker = flatpickr("#RequisitionDate", {
        dateFormat: "d-m-Y",
        allowInput: true,
        altInput: true,
        altFormat: "d-m-Y",
        locale: {
            firstDayOfWeek: 5
        },
        defaultDate: new Date() // today
    });
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

            if (callback) callback();
        }
    });
}
//#endregion

//#region Load AccountHead based on ServiceType
function loadAccountHeads($serviceTypeSelect, selectedHead = null) {

    let serviceTypeId = $serviceTypeSelect.val();
    let $accountHeadSelect = $('#AccountHead');

    // Clear dropdown
    $accountHeadSelect.empty().append('<option value="">Select Accounts Head</option>').prop('disabled', false);

    if (!serviceTypeId) return;

    $.get('/OperationFund/GetAccountsHeadDD', { serviceTypeId: serviceTypeId }, function (res) {
        res.forEach(item => {
            $accountHeadSelect.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

        if (selectedHead) {
            $accountHeadSelect.val(selectedHead);
        }
    });
}
$(document).on('change', '#ServiceType', function () {
    loadAccountHeads($(this));
});


//#endregion

//#region Load Job Entry List
let jobTable = null;
window.gctlRenderers.formatDate = function (data) { return formatDate(data); };

function loadJobEntryList() {
    jobTable = $.gctlGrid.initFromModel({
        gridId: 'JobTable',
        ajaxData: function (d) {
            d.customerid = $('#Customer').val();
            d.shipmentmodeid = $('#Job-ShipmentMode').val();
        }
    });
    // jobTable = $.gctlGrid.init({
    //     gridSelector: '#JobTable',
    //     ajaxUrl: '/OperationFund/GetAllJobList',
    //     serverSide: true,
    //     ajaxData: function (d) {
    //         d.customerid = $('#Customer').val();
    //         d.shipmentmodeid = $('#Job-ShipmentMode').val();
    //     },
    //     order: [[1, "desc"]],
    //     columnDefs: [{ targets: [0], orderable: false }],
    //     columns: [
    //         {
    //             "data": null,
    //             "className": "text-center",
    //             "orderable": false,
    //             "render": function (data, type, row) {
    //                 return `<input type="radio" class="form-check-input job-radio" name="jobSelect" value="${row.jobNo ?? ''}">`;
    //             }
    //         },
    //         { "data": "jobNo", "className": "text-left" },
    //         {
    //             "data": "jobDate",
    //             "className": "text-center",
    //             "render": function (data) { return formatDate(data); }
    //         },
    //         { "data": "shipmentMode", "className": "text-center" },
    //         { "data": "customer" },
    //         {
    //             "data": "docReceivedDate",
    //             "className": "text-center",
    //             "render": function (data) { return formatDate(data); }
    //         }
    //     ]
    // });
}
//#endregion

//#region Generate Requisition No function
function generateRequisitionNo(jobNo) {
    return $.ajax({
        url: "/OperationFund/GenerateRequisitionNo",
        type: "GET",
        data: { jobNo: jobNo }
    });
}
//#endregion

//#region Get Requisition Header Info
$(document).on('click', 'input[name="jobSelect"]', function () {

    if ($(this).prop('checked') && $(this).data('waschecked')) {
        $(this).prop('checked', false);
        $(this).data('waschecked', false);

        clearJobHeaderFields();
    } else {
        $('input[name="jobSelect"]').data('waschecked', false);
        $(this).data('waschecked', true);

        let jobno = $(this).val();

        $.ajax({
            url: "/OperationFund/GetHeaderData",
            type: "GET",
            data: { jobno: jobno },
            success: function (headerdata) {
                if (!headerdata) {
                    toastr.warning("No job data found!");
                    clearJobHeaderFields();
                    return;
                }

                $("#Job-JoBNO").val(headerdata.jobNo);
                $("#Job-ShipmentModeInput").val(headerdata.shipmentMode);
                $("#Job-CustomerName").val(headerdata.customerName);
                $("#Job-LCValue").val(headerdata.lcValue);
                $("#Job-InvoiceNo").val(headerdata.invoiceNo);
                $("#Job-InvoiceValue").val(headerdata.invoiceValue);
                $("#Job-HAWB").val(headerdata.hawb);
                $("#Job-MatDescription").val(headerdata.materialDescription);
                $("#Job-Qty").val(headerdata.qty);
                $("#Job-Weight").val(headerdata.weight);
                $("#ShipmentModeID").val(headerdata.shipmentModeID);     // ID
                $("#CustomerID").val(headerdata.customerID);     // ID

                if (headerdata.currencyID && currencyChoice) {
                    currencyChoice.setChoiceByValue(headerdata.currencyID);
                };
                // Generate Requisition No
                generateRequisitionNo(jobno).done(function (res) {
                    if (res && res.requisitionNo) {
                        $("#Job-RequisitionNo").val(res.requisitionNo);
                    } else {
                        $("#Job-RequisitionNo").val('');
                        toastr.warning("Failed to generate Requisition No");
                    }
                }).fail(function () {
                    $("#Job-RequisitionNo").val('');
                    toastr.error("Error generating Requisition No");
                });
                ClearTmpDetail();
            },
            error: function () {
                console.error("Error loading header data", xhr);
                clearJobHeaderFields();
            }
        });
    }
});
//#endregion

//#region Clear Job Header Fileds
function clearJobHeaderFields() {
    $("#Job-JoBNO,#Job-ShipmentModeInput,#Job-CustomerName,#Job-LCValue,#Job-InvoiceNo,#Job-InvoiceValue,#Job-HAWB,#Job-MatDescription,#Job-Qty,#Job-Weight,#Job-RequisitionNo").val("");
    if (currencyChoice) {
        currencyChoice.setChoiceByValue("");
    }
}
//#endregion

//#region Search, Filter and PageSize

$('#Customer, #Job-ShipmentMode').on('change', function () {
    if (jobTable) jobTable.ajax.reload();
});
//#endregion

//#region Date Formate
function formatDate(date) {
    if (!date) return '';
    let d = new Date(date);
    return d.toLocaleDateString('en-GB');
}
//#endregion

//#region Ready Part
$(document).ready(function () {
    //Requisition Date
    RequisitionDate();
    //Dropdown
    //InitDropdown('/OperationFund/GetAccountsHeadDD', '#AccountHead', 'Select Accounts Head');
    InitDropdown('/OperationFund/GetServiceTypeDD', '#ServiceType', 'Select Service Type');
    InitDropdown('/OperationFund/GetEmpDD', '#Customer', 'Select Employee');
    InitDropdown('/OperationFund/GetShipemtDD', '#Job-ShipmentMode', 'Select Shipment Mode');
    InitDropdown('/OperationFund/GetCurenciesDD', '#Job-Currencies', 'Select C..');

    //Load list
    loadJobEntryList();

});
//#endregion
















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
            if (currencyChoice) currencyChoice.setChoiceByValue(res.currencyID);
            $('#Job-HAWB').val(res.hawb ?? '');
            $('#Job-MatDescription').val(res.materialDescription ?? '');
            $('#Job-Qty').val(res.qty ?? '');
            $('#Job-Weight').val(res.weight ?? '');
            $('#Job-Currencies').val(res.currencyID ?? '');
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