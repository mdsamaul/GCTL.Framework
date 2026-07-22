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
                console.log(headerdata);
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
