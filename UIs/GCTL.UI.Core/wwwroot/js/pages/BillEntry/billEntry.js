
function getTodayDateStr() {
    var today = new Date();
    var y = today.getFullYear();
    var m = String(today.getMonth() + 1).padStart(2, '0');
    var d = String(today.getDate()).padStart(2, '0');
    return y + '-' + m + '-' + d;
}

// Example usage: Populate with sample data
$(document).ready(function () {

    showDev('loaded')

    if (typeof setDate === 'function') {
        setDate('#billDate', getTodayDateStr());
    } else {
        $('#billDate').val(getTodayDateStr());
    }

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

    // Customer change: load customer address and related delivery addresses
    $(document).on('change', '#customerId', function () {
        if (window._isPopulatingBillForm) {
            return;
        }
        var customerId = $(this).val();
        loadCustomerInfo(customerId);
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

    // Click on Bill No link in BillHistoryTable to populate the form
    $(document).on('click', '.bill-detail-link', function (e) {
        e.preventDefault();
        const billNo = $(this).data('id') || $(this).text().trim();
        if (!billNo) return;

        $.ajax({
            url: '/JobBillEntry/GetBillDetail',
            type: 'GET',
            data: { billNo: billNo },
            success: function (response) {
                if (response && response.success === false) {
                    toastr.warning(response.message || "Bill not found.");
                    return;
                }
                clearForm();
                populateForm(response);

                if (response.id) {
                    PopulateExpDetailsTable(response.id);
                    populateChargeTblFunc(response.id);
                }
                toastr.info("Loaded Bill: " + billNo);
            },
            error: function () {
                toastr.error("Error loading bill details.");
            }
        });
    });

    // Delete selected bills using top toolbar delete button
    $('#deleteBillEntryBtn').on('click', function (e) {
        e.preventDefault();

        var selectedBills = [];
        $('#BillHistoryTable tbody input[type="checkbox"]:checked').each(function () {
            var val = $(this).val() || $(this).data('id');
            if (val) {
                selectedBills.push(String(val));
            }
        });

        if (selectedBills.length === 0) {
            toastr.warning("Please select at least one bill to delete.");
            return;
        }

        if (!confirm("Are you sure you want to delete the selected " + selectedBills.length + " bill(s)?")) {
            return;
        }

        $.ajax({
            url: '/JobBillEntry/DeleteBills',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(selectedBills),
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message || "Selected bill(s) deleted successfully.");

                    var currentBillNo = $('#billNo').val();
                    if (currentBillNo && selectedBills.indexOf(currentBillNo) !== -1) {
                        clearForm();
                    }

                    if (billHistoryGrid) {
                        billHistoryGrid.ajax.reload();
                    }
                    if (jobGrid) {
                        jobGrid.ajax.reload();
                    }
                } else {
                    toastr.error(response.message || "Failed to delete bills.");
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                toastr.error("An error occurred while deleting bills.");
            }
        });
    });
    //#endregion

    //#region Submit
    $('#saveBillEntryBtn').on('click', function (e) {
        e.preventDefault();

        var jobNo = $('#jobNo').val() || $('#jobNoHidden').val();
        if (!jobNo) {
            toastr.warning("Please select a job first.");
            return;
        }

        var chcItems = [];
        $('#chargeTableBody tr').each(function () {
            var $row = $(this);
            var isChecked = $row.find('.includeChk').is(':checked');
            if (isChecked) {
                var chcVal = $row.find('.billAmtInput').length ? parseFloat($row.find('.billAmtInput').val()) : parseFloat($row.data('billamt'));
                if (isNaN(chcVal)) chcVal = 0;
                chcItems.push({
                    chcid: String($row.data('chcid') || ''),
                    billHead: String($row.data('billhead') || ''),
                    description: String($row.data('desc') || ''),
                    billAmount: chcVal,
                    isReceiptable: String($row.data('isrec') || '')
                });
            }
        });

        var expenseItems = [];
        $('#expenceDetailTableBody tr').each(function () {
            var $row = $(this);
            var isChecked = $row.find('.includeExpenseChk').is(':checked');
            if (isChecked) {
                var expBillAmt = parseFloat($row.find('.expBillAmtInput').val());
                if (isNaN(expBillAmt)) expBillAmt = 0;
                var confAmt = parseFloat($row.data('confirmamt'));
                if (isNaN(confAmt)) confAmt = 0;

                expenseItems.push({
                    expenseHeadId: String($row.data('headid') || ''),
                    billHead: String($row.data('head') || ''),
                    confirmAmt: confAmt,
                    billAmt: expBillAmt
                });
            }
        });

        var requestData = {
            billNo: $('#billNo').val() || '',
            billDate: $('#billDate').val() || '',
            jobNo: jobNo,
            customerId: $('#customerId').val() || '',
            destinationId: $('#destinationId').val() || '',
            remarks: $('#remarks').val() || '',
            agComm: parseFloat($('#agComm').val()) || null,
            onInvVal: parseFloat($('#onInvVal').val()) || null,
            amount: parseFloat($('#amount').val()) || null,
            specialDiscount: parseFloat($('#disc').val()) || null,
            chcItems: chcItems,
            expenseItems: expenseItems
        };

        var $btn = $(this);
        $btn.prop('disabled', true);

        $.ajax({
            url: '/JobBillEntry/SaveBill',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(requestData),
            success: function (response) {
                $btn.prop('disabled', false);
                if (response.success) {
                    toastr.success(response.message || "Bill saved successfully!");
                    if (response.data) {
                        $('#billNo').val(response.data);
                    }
                    if (jobGrid) {
                        jobGrid.ajax.reload();
                    }
                    if (window.BillHistoryTable) {
                        window.BillHistoryTable.ajax.reload();
                    } else if ($.fn.DataTable && $.fn.DataTable.isDataTable('#BillHistoryTable')) {
                        $('#BillHistoryTable').DataTable().ajax.reload();
                    }
                } else {
                    toastr.error(response.message || "Failed to save bill.");
                }
            },
            error: function (xhr, status, error) {
                $btn.prop('disabled', false);
                console.error(error);
                toastr.error("An error occurred while saving the bill.");
            }
        });
    });

    function calculateTotalBillAmount() {
        var total = 0;

        // Sum included custom handling charges
        $('#chargeTableBody tr').each(function () {
            var $row = $(this);
            if ($row.find('.includeChk').is(':checked')) {
                var amt = $row.find('.billAmtInput').length ? parseFloat($row.find('.billAmtInput').val()) : parseFloat($row.data('billamt'));
                if (!isNaN(amt)) {
                    total += amt;
                }
            }
        });

        // Sum included expense details
        $('#expenceDetailTableBody tr').each(function () {
            var $row = $(this);
            if ($row.find('.includeExpenseChk').is(':checked')) {
                var amt = parseFloat($row.find('.expBillAmtInput').val());
                if (isNaN(amt)) {
                    amt = parseFloat($row.data('billamt')) || 0;
                }
                if (!isNaN(amt)) {
                    total += amt;
                }
            }
        });

        // Add Agency Commission Amount if populated
        var agComm = parseFloat($('#agComm').val());
        // If amount was not manually entered, update #amount with the calculated total
        $('#amount').val(total.toFixed(2));
    }

    $(document).on('change', '#chargeTableBody .includeChk', calculateTotalBillAmount);
    $(document).on('change', '#expenceDetailTableBody .includeExpenseChk', calculateTotalBillAmount);
    $(document).on('input change', '#expenceDetailTableBody .expBillAmtInput', calculateTotalBillAmount);
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

async function loadCustomerInfo(customerId, selectedDeliveryLocation) {
    if (!customerId) {
        $('#customerAddress').val('');
        if (typeof setSelect === 'function') {
            setSelect('#customerDeliveryAddress', '', true, [], false, 'Select Customer Delivery Address');
        } else {
            $('#customerDeliveryAddress').empty().append('<option value="">Select Customer Delivery Address</option>').val('').prop('disabled', true).trigger('change');
        }
        return;
    }

    try {
        var response = await $.ajax({
            url: '/JobBillEntry/CustomerInfo',
            type: 'GET',
            data: { customerId: customerId }
        });

        if (response && response.success && response.data) {
            var customer = response.data;
            $('#customerAddress').val(customer.customerAddress || '');

            var deliveryItems = (customer.delivaryAddress || []).map(function (d) {
                return {
                    id: d.delivaryId,
                    text: d.delivaryAddress
                };
            });

            var targetLocation = selectedDeliveryLocation;
            if (!targetLocation && deliveryItems.length > 0) {
                targetLocation = deliveryItems[0].id;
            }

            if (typeof setSelect === 'function') {
                setSelect(
                    '#customerDeliveryAddress',
                    targetLocation || '',
                    true,
                    deliveryItems,
                    false,
                    'Select Customer Delivery Address'
                );
            } else {
                var $delSelect = $('#customerDeliveryAddress').empty().append('<option value="">Select Customer Delivery Address</option>');
                deliveryItems.forEach(function (item) {
                    $delSelect.append($('<option></option>').val(item.id).text(item.text));
                });
                $delSelect.val(targetLocation || '').prop('disabled', true).trigger('change');
            }
        }
    } catch (e) {
        console.error('Error loading customer info:', e);
    }
}


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
    $('#creationDate').text('');
    $('#lastUpdateDate').text('');
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
    $('#disc').val('');

    // Date fields (using setDate)
    if (typeof setDate === 'function') {
        setDate('#billDate', getTodayDateStr(), false);
        setDate('#jobDate', '', true);
        setDate('#docsReceivedDate', '', true);
        setDate('#expDate', '', true);
        setDate('#lcDate', '', true);
        setDate('#invoiceDate', '', true);
        setDate('#ipDate', '', true);
        setDate('#arrivalDate', '', true);
        setDate('#blDate', '', true);
        setDate('#beDate', '', true);
        setDate('#efrDate', '', true);
        setDate('#etaDeliveryDate', '', true);
        setDate('#actualDeliveryDate', '', true);
        setDate('#unstuffingDate', '', true);
        setDate('#etdDate', '', true);
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
    console.log(data);

    showDev(data);

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
    $('#disc').val(data.specialDiscount || '');
    $('#headId').val(data.headId || '').trigger('change');

    // Dates using setDate
    var targetBillDate = data.billDate || getTodayDateStr();
    if (typeof setDate === 'function') {
        setDate('#billDate', targetBillDate, false);
        setDate('#jobDate', data.jobDate || '', true);
        setDate('#docsReceivedDate', data.docsReceivedDate || data.docReceivedDate || '', true);
        setDate('#expDate', data.expDate || '', true);
        setDate('#lcDate', data.lcDate || '', true);
        setDate('#invoiceDate', data.invoiceDate || '', true);
        setDate('#ipDate', data.ipDate || '', true);
        setDate('#blDate', data.blDate || '', true);
        setDate('#beDate', data.beDate || '', true);
        setDate('#dischargeDate', data.dischargeDate || '', true);
    } else {
        $('#billDate').val(targetBillDate);
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

    window._isPopulatingBillForm = true;
    $('#shipmentModeId').val(data.shipmentModeId || '').trigger('change');
    $('#isCustomJobNo').prop('checked', data.isCustomJobNo || false);
    $('#jobNo').val(data.jobNo || '');
    $('#jobNoHidden').val(data.jobNo || '');
    $('#customerId').val(data.customerId || '').trigger('change');
    if (data.customerId) {
        loadCustomerInfo(data.customerId, data.customerDeliveryAddress);
    } else {
        $('#customerDeliveryAddress').val(data.customerDeliveryAddress || '').trigger('change');
    }
    window._isPopulatingBillForm = false;
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
    $('#creationDate').text(data.createdAt ? String(data.createdAt).split('T')[0] : '');
    $('#lastUpdateDate').text(data.updatedAt ? String(data.updatedAt).split('T')[0] : '');
    if (window.initCustomDropdowns) {
        window.initCustomDropdowns(document, true);
    }
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
        var isIncluded = (item.include !== false);
        var row = `
                <tr data-tc="${item.tc || ''}" data-chcid="${item.chcid || ''}" data-billhead="${item.chc || ''}" data-desc="${item.description || ''}" data-billamt="${item.billAmt || 0}" data-isrec="${item.isRec || ''}">
                    <td class="text-center">${item.serialNo || (index + 1)}</td>
                    <td class="text-center">${item.isRec || ''}</td>
                    <td class="text-start ps-2">${item.chc || ''}</td>
                    <td class="text-start ps-2">${item.description || ''}</td>
                    <td class="text-end pe-2">${item.billAmt || 0}</td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input m-0 shadow-none includeChk" ${isIncluded ? "checked" : ""}>
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

// Populate table
function populateExpenseTable(data) {
    var tbody = $("#expenceDetailTableBody");
    tbody.empty(); // clear old rows

    $.each(data, function (i, item) {
        var isIncluded = (item.include !== false);
        var row = `
                <tr data-headid="${item.expenseHeadId || ''}" data-head="${item.head || ''}" data-confirmamt="${item.confirmAmt || 0}" data-billamt="${item.billAmt || 0}">
                    <td class="text-center">${item.serialNo || (i + 1)}</td>
                    <td class="text-start ps-2">${item.head || ''}</td>
                    <td class="text-center">${item.isRec || ''}</td>
                    <td class="text-center">${item.serviceType || ''}</td>
                    <td class="text-end pe-2">${item.confirmAmt || 0}</td>
                    <td class="text-end pe-2">
                        <input type="number" class="form-control form-control-sm text-end expBillAmtInput"
                               value="${item.billAmt || 0}" />
                    </td>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input m-0 shadow-none includeExpenseChk" ${isIncluded ? "checked" : ""} />
                    </td>
                </tr>
            `;
        tbody.append(row);
    });
}


//#endregion


//#region popilate tables

function populateChargeTblFunc(jobId) {


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
            showDev(data);
            populateForm(data);




        },
        error: function () {
            alert('Error loading job details');
        }
    });
}

                    //#endregion