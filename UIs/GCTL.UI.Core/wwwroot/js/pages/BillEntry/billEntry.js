
// Example usage: Populate with sample data
$(document).ready(function () {

    showDev('loaded')

    //#region Select 2

    const initializeSelect = () => {
        // $('.searchableSelect').select2({
        //     width: '100%',
        //     allowClear: true,
        //     placeholder: 'Select an option',
        //     language: { noResults: () => 'No results found' },
        //     escapeMarkup: markup => markup
        // });
    };

    initializeSelect();

    //#endregion

    //#region Populate TopTable (_CustomGrid)

    // Flatpickr for Date Filter
    if (typeof flatpickr !== "undefined") {
        // flatpickr("#dateFrom", {
        //     dateFormat: "d-m-Y",
        //     allowInput: true,
        //     onChange: function () {
        //         if (jobGrid) jobGrid.ajax.reload();
        //     }
        // });

        // flatpickr("#dateTo", {
        //     dateFormat: "d-m-Y",
        //     allowInput: true,
        //     onChange: function () {
        //         if (jobGrid) jobGrid.ajax.reload();
        //     }
        // });
    }

    var jobGrid = null;
    if ($.gctlGrid) {
        jobGrid = $.gctlGrid.initFromModel({
            gridId: 'JobSettingsTable',
            pageLength: 5,
            ajaxData: function (d) {
                d.customerName = $('#customerFilter').val();
                d.shipmentMode = $('#shipmentModeFilter').val();
                d.dateFrom = $('#dateFrom').val();
                d.dateTo = $('#dateTo').val();
            }
        });
    }

    // Reload grid on filter change
    $('#customerFilter, #shipmentModeFilter')
        .on('change', function () {
            if (jobGrid) {
                jobGrid.ajax.reload();
            }
        });

    $('#dateFrom, #dateTo').on('change keyup', function () {
        if (jobGrid) {
            jobGrid.ajax.reload();
        }
    });

    //#endregion

    //#region On click / radio select populate Form

    // Radio selection or row click in JobSettingsTable
    $(document).on('change', 'input[name="jobSelect"]', function () {
        const jobId = $(this).val();
        if (jobId) {
            PopulateTheForm(jobId);
            PopulateExpDetailsTable(jobId);
            populateChargeTblFunc(jobId);
        }
    });

    // Click on Job No link or View button to load details
    $(document).on('click', '.job-detail-link, .btn-view-job', function (e) {
        e.preventDefault();
        const jobId = $(this).data('id');

        PopulateTheForm(jobId);
        PopulateExpDetailsTable(jobId);
        populateChargeTblFunc(jobId);
    });





    //#endregion

    //#region nicher table (_CustomGrid: BillHistoryTable)
    var billHistoryGrid = null;
    if ($.gctlGrid) {
        billHistoryGrid = $.gctlGrid.initFromModel({
            gridId: 'BillHistoryTable',
            pageLength: 10
        });
    }

    // Radio selection on BillHistoryTable
    $(document).on('change', 'input[name="billSelect"]', function () {
        const billNo = $(this).val();
        console.log('Selected bill:', billNo);
    });
    //#endregion

    //#region Submit
    $('#saveShipmentBtn').on('click', function (e) {
        e.preventDefault();

        // ফর্ম ডেটা সংগ্রহ
        var formData = $('#shipmentForm').serialize();

        $.ajax({
            url: $('#shipmentForm').attr('action'),   // asp-action="Create" অনুযায়ী URL নেবে
            type: $('#shipmentForm').attr('method'),  // method="post"
            data: formData,
            success: function (response) {
                if (response.success) {
                    clearForm();
                    loadJobStatuses();
                    toastr.success(response.message || "Shipment saved successfully!");
                }
                else {
                    toastr.warning(response.message || "Shipment not saved!");
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                toastr.warning("Error occurred while saving shipment.");
            }
        });
    });





    //#endregion

    //#region SampleData

    var sampleData = {
        shipmentModeId: '1', // Assuming '1' is a valid option value
        isCustomJobNo: true,
        jobNo: 'JOB12345',
        jobDate: '2023-10-01',
        customerId: '2',
        customerDeliveryAddress: '123 Main St, City, Country',
        portId: '3',
        docsReceivedDate: '2023-09-15',
        lcExpNo: 'LC123456',
        lcValue: '100000',
        ipEpNo: 'IP789',
        ipDate: '2023-09-20',
        importerId: '4',
        invoiceNo: 'INV001',
        invoiceDate: '2023-09-25',
        blNo: 'BL987654',
        blDate: '2023-10-05',
        beNo: 'BE111222',
        beDate: '2023-10-10',
        containerNo: 'CONT123',
        containerSize: '20FT',
        lcaNo: 'LCA456',
        dischargeDate: '2023-10-15',
        materialDescription: 'Sample materials for import',
        quantity: 100,
        quantityUnitId: '5', // Assuming '5' is PKG unit
        weight: 5000,
        weightUnitId: '6', // Assuming '6' is KG unit
        forwarderId: '7',
        vesselRottNo: 'VESSEL001',
        remarks: 'Sample remarks'
    };

    //populateForm(sampleData);

    //#endregion

    //#region btnAdd Head

    $("#btnAddCost").click(function (e) {
        e.preventDefault();
        AddCostHead();

    });



    //#endregion

    $("#btnClear").click(function () {
        clearForm();
    });


    $(document).on("click", ".deleteChrgBtn", function () {
        var tc = $(this).data("tc");

        DeleteCharge(tc)


    });


});


//#region delete head

function DeleteCharge(tc) {
    $.ajax({
        url: '/JobBillEntry/DeleteCharge',   // adjust to your controller/action
        type: 'POST',
        data: { tc: tc },
        success: function (response) {
            if (response.success) {
                toastr.success("Deleted successfully!");
                // Optionally refresh table here
                var tc = $('#sdId').val();

                populateChargeTblFunc(tc);

            } else {
                toastr.error("Delete failed: " + (response.error || ""));
            }
        },
        error: function (xhr, status, error) {
            alert("Error: " + error);
        }
    });
}

//#endregion



//#region add cost head

function AddCostHead() {
    // Collect values from form
    var headId = $("#headId").val();
    var cost = $("#cost").val();
    var jobNo = $("#jobNo").val();
    var customerId = $("#customerId").val();
    var tc = $('#sdId').val();

    // Send to controller via AJAX
    $.ajax({
        url: '/JobBillEntry/AddCost',   // <-- adjust controller/action name
        type: 'POST',
        data: {
            headId: headId,
            cost: cost,
            jobNo: jobNo,
            customerId: customerId,
            tc: tc
        },
        success: function (response) {
            showDev(response)
            if (response.success) {
                toastr.success("Cost added successfully!");
                // Optionally refresh table or clear inputs

                populateChargeTblFunc(response.sepTC)
                //callHelp(response.sepTC)
            } else {
                toastr.error("Failed to add cost: " + (response.error || ""));
            }
        },
        error: function (xhr, status, error) {
            alert("Error: " + error);
        }
    });
}

function callHelp(jobId) {
    showDev(jobId);

}

//#endregion


//#region Clr

function clearForm() {
    // Text inputs
    $('#sdId').val('');

    $('#jobNo').val('');
    $('#jobNoHidden').val('');
    $('#customerDeliveryAddress').val('');
    $('#lcExpNo').val('');
    $('#lcValue').val('');
    $('#ipEpNo').val('');
    $('#invoiceNo').val('');
    $('#blNo').val('');
    $('#beNo').val('');
    $('#containerNo').val('');
    $('#containerSize').val('');
    $('#lcaNo').val('');
    $('#materialDescription').val('');
    $('#quantity').val('');
    $('#weight').val('');
    $('#vesselRottNo').val('');
    $('#remarks').val('');
    $('#creationDate').val('');
    $('#updatedDate').val('');
    $('#billNo').val('');
    $('#customerAddress').val('');
    $('#lcNo').val('');
    $('#bdtRate').val('');
    $('#bdtTotal').val('');
    $('#invoiceValue').val('');
    $('#invBdtRate').val('');
    $('#invBdtTotal').val('');
    $('#noTransport').val('');
    $('#demarageDay').val('');
    $('#undertakingNo').val('');
    $('#efrNo').val('');
    $('#rotNo').val('');
    $('#hawb').val('');
    $('#packQty').val('');
    $('#forwarderId').val('');
    $('#consoleNo').val('');
    $('#agComm').val('');
    $('#onInvVal').val('');
    $('#amount').val('');
    $('#cost').val('');

    // Date fields (using setDate)
    if (typeof setDate === 'function') {
        setDate('#billDate', '');
        setDate('#jobDate', '');
        setDate('#docsReceivedDate', '');
        setDate('#expDate', '');
        setDate('#lcDate', '');
        setDate('#invoiceDate', '');
        setDate('#ipDate', '');
        setDate('#arrivalDate', '');
        setDate('#blDate', '');
        setDate('#beDate', '');
        setDate('#efrDate', '');
        setDate('#etaDeliveryDate', '');
        setDate('#actualDeliveryDate', '');
        setDate('#unstuffingDate', '');
        setDate('#etdDate', '');
    } else {
        $('#billDate, #jobDate, #docsReceivedDate, #expDate, #lcDate, #invoiceDate, #ipDate, #arrivalDate, #blDate, #beDate, #efrDate, #etaDeliveryDate, #actualDeliveryDate, #unstuffingDate, #etdDate').val('');
    }

    // Dropdowns
    $('#shipmentModeId, #customerId, #customerDeliveryAddress, #portId, #importerId, #quantityUnitId, #weightUnitId, #packQtyUnitId, #forwarderId, #placeOfLoadingId, #shedYardId, #freightChargeId, #lcValCurrencyId, #invValCurrencyId, #destinationId, #headId').val('').trigger('change');
    if (window.initCustomDropdowns) {
        window.initCustomDropdowns(document, true);
    }

    // Textarea
    $('#shipmentStatus').val('');
    $('#autoId').val('');

    // Checkboxes
    $('#isCustomJobNo').prop('checked', false);

    var tbody1 = $("#chargeTableBody");
    tbody1.empty(); // clear old rows

    var tbody2 = $("#expenceDetailTableBody");
    tbody2.empty(); // clear old rows

}

//#endregion

//#region Function to populate the form with data
function populateBottomForm(data) {

    showDev(data);

    // Date fields
    if (typeof setDate === 'function') {
        if (data.etaDeliveryDate) setDate('#etaDeliveryDate', data.etaDeliveryDate);
        if (data.actualDeliveryDate) setDate('#actualDeliveryDate', data.actualDeliveryDate);
        if (data.unstuffingDate) setDate('#unstuffingDate', data.unstuffingDate);
        if (data.etdDate) setDate('#etdDate', data.etdDate);
    } else {
        if (data.etaDeliveryDate) $('#etaDeliveryDate').val(data.etaDeliveryDate.split('T')[0]);
        if (data.actualDeliveryDate) $('#actualDeliveryDate').val(data.actualDeliveryDate.split('T')[0]);
        if (data.unstuffingDate) $('#unstuffingDate').val(data.unstuffingDate.split('T')[0]);
        if (data.etdDate) $('#etdDate').val(data.etdDate.split('T')[0]);
    }

    // Dropdowns
    if (data.placeOfLoadingID) {
        $('#placeOfLoadingId').val(data.placeOfLoadingID).trigger('change');
    }
    if (data.shedYardID) {
        $('#shedYardId').val(data.shedYardID).trigger('change');
    }
    if (data.freightChargeID) {
        $('#freightChargeId').val(data.freightChargeID).trigger('change');
    }

    // Textarea
    if (data.shipmentStatus) {
        $('#shipmentStatus').val(data.shipmentStatus);
    }

    if (data.autoId) {
        $('#autoId').val(data.autoId);
    }
}


function populateForm(data) {

    $('#sdId').val(data.id || '');

    // New ones
    $('#billNo').val(data.billNo || '').prop('readonly', true);
    $('#customerAddress').val(data.customerAddress || '');
    $('#lcNo').val(data.lcNo || '');
    $('#lcValCurrencyId').val(data.lcValCurrencyId || '').trigger('change');
    $('#bdtRate').val(data.bdtRate || '');
    $('#bdtTotal').val(data.bdtTotal || '');
    $('#invoiceValue').val(data.invoiceValue || '');
    $('#invValCurrencyId').val(data.invValCurrencyId || '').trigger('change');
    $('#consoleNo').val(data.consoleNo || '');
    $('#destinationId').val(data.destinationId || '').trigger('change');
    $('#agComm').val(data.agComm || '');
    $('#onInvVal').val(data.onInvVal || '');
    $('#amount').val(data.amount || '');
    $('#headId').val(data.headId || '').trigger('change');

    // Dates using setDate
    if (typeof setDate === 'function') {
        setDate('#billDate', data.billDate || '');
        setDate('#jobDate', data.jobDate || '');
        setDate('#docsReceivedDate', data.docsReceivedDate || data.docReceivedDate || '');
        setDate('#expDate', data.expDate || '');
        setDate('#lcDate', data.lcDate || '');
        setDate('#invoiceDate', data.invoiceDate || '');
        setDate('#ipDate', data.ipDate || '');
        setDate('#blDate', data.blDate || '');
        setDate('#beDate', data.beDate || '');
        setDate('#dischargeDate', data.dischargeDate || '');
    } else {
        $('#billDate').val(data.billDate || '');
        $('#jobDate').val(data.jobDate || '');
        $('#docsReceivedDate').val(data.docsReceivedDate || data.docReceivedDate || '');
        $('#expDate').val(data.expDate || '');
        $('#lcDate').val(data.lcDate || '');
        $('#invoiceDate').val(data.invoiceDate || '');
        $('#ipDate').val(data.ipDate || '');
        $('#blDate').val(data.blDate || '');
        $('#beDate').val(data.beDate || '');
        $('#dischargeDate').val(data.dischargeDate || '');
    }

    $('#shipmentModeId').val(data.shipmentModeId || '').trigger('change');
    $('#isCustomJobNo').prop('checked', data.isCustomJobNo || false);
    $('#jobNo').val(data.jobNo || '');
    $('#jobNoHidden').val(data.jobNo || '');
    $('#customerId').val(data.customerId || '').trigger('change');
    $('#customerDeliveryAddress').val(data.customerDeliveryAddress || '').trigger('change');
    $('#portId').val(data.portId || '').trigger('change');
    $('#lcExpNo').val(data.lcExpNo || '');
    $('#lcValue').val(data.lcValue || '');
    $('#ipEpNo').val(data.ipEpNo || '');
    $('#importerId').val(data.importerId || '').trigger('change');
    $('#invoiceNo').val(data.invoiceNo || '');
    $('#blNo').val(data.blNo || '');
    $('#beNo').val(data.beNo || '');
    $('#containerNo').val(data.containerNo || '');
    $('#containerSize').val(data.containerSize || '');
    $('#lcaNo').val(data.lcaNo || '');
    $('#materialDescription').val(data.materialDescription || '');
    $('#quantity').val(data.quantity || '');
    $('#quantityUnitId').val(data.quantityUnitId || '').trigger('change');
    $('#weight').val(data.weight || '');
    $('#weightUnitId').val(data.weightUnitId || '').trigger('change');
    $('#forwarderId').val(data.forwarderId || '').trigger('change');
    $('#vesselRottNo').val(data.vesselRottNo || '');
    $('#remarks').val(data.remarks || '');
    $('#creationDate').val(data.createdAt || '');
    $('#updatedDate').val(data.updatedAt || '');
    if (window.initCustomDropdowns) {
        window.initCustomDropdowns(document, true);
    }

    populateExpenseTable(expenseData);
}

//#endregion

//#region Popultae helper table


// Example JSON data
var jsonChargeData = [
    { slNo: 1, isRec: "Yes", chc: "Head A", description: "Service Charge", billAmt: 500, include: true },
    { slNo: 2, isRec: "No", chc: "Head B", description: "Maintenance", billAmt: 300, include: false },
    { slNo: 3, isRec: "Yes", chc: "Head C", description: "Transport", billAmt: 200, include: true }
];

// Function to populate table
function populateChargeTable(data) {
    var tbody = $("#chargeTableBody");
    tbody.empty(); // clear old rows

    $.each(data, function (index, item) {
        var row = `
                <tr>
                    <td class="text-center">${index + 1}</td>
                    <td class="text-center">${item.isRec}</td>
                    <td class="text-start ps-2">${item.chc}</td>
                    <td class="text-start ps-2">${item.description}</td>
                    <td class="text-end pe-2">${item.billAmt}</td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input m-0 shadow-none includeChk" ${item.include ? "checked" : ""}>
                    </td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-danger border-0 deleteChrgBtn" data-tc="${item.tc}" title="Delete">
                            <i class="bi bi-trash3-fill"></i>
                        </button>
                    </td>
                </tr>
            `;
        tbody.append(row);
    });
}

var expenseData = [
    {
        sl: 3,
        head: "Transport",
        isRec: true,
        serviceType: "Logistics",
        confirmAmt: 500,
        billAmt: 500,
        isInclude: true
    },
    {
        sl: 5,
        head: "Food",
        isRec: false,
        serviceType: "Catering",
        confirmAmt: 300,
        billAmt: 300,
        isInclude: false
    }
];

// Populate table
function populateExpenseTable(data) {
    var tbody = $("#expenceDetailTableBody");
    tbody.empty(); // clear old rows

    $.each(data, function (i, item) {
        var row = `
                <tr>
                    <td class="text-center">${i + 1}</td>
                    <td class="text-start ps-2">${item.head}</td>
                    <td class="text-center">${item.isRec}</td>
                    <td class="text-center">${item.serviceType}</td>
                    <td class="text-end pe-2">${item.confirmAmt}</td>
                    <td class="text-end pe-2">
                        <input type="number" class="form-control form-control-sm text-end"
                               value="${item.billAmt}" />
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input m-0 shadow-none" ${item.isInclude ? "checked" : ""} />
                    </td>
                </tr>
            `;
        tbody.append(row);
    });
}


//#endregion


//#region popilate tables

function populateChargeTblFunc(jobId) {

    debugger

    $.ajax({
        url: '/JobBillEntry/GetChrgTblData',
        type: 'GET',
        data: { id: jobId },
        success: function (data) {

            showDev(data, 'chrg tbl')
            if (data.success) {
                populateChargeTable(data.data);
            }





        },
        error: function () {
            alert('Error loading job details');
        }
    });
}

function PopulateExpDetailsTable(jobId) {
    $.ajax({
        url: '/JobBillEntry/GetExpDetailTbl',
        type: 'GET',
        data: { id: jobId },
        success: function (data) {


            if (data.success) {
                populateExpenseTable(data.data)
            }





        },
        error: function () {
            alert('Error loading job details');
        }
    });
}
function PopulateTheForm(jobId) {
    $.ajax({
        url: '/JobBillEntry/GetJobDetail',
        type: 'GET',
        data: { id: jobId },
        success: function (data) {
            clearForm();
            populateForm(data);




        },
        error: function () {
            alert('Error loading job details');
        }
    });
}

                    //#endregion