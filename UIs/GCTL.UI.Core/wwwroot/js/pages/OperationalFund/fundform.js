//#region Global Veriable
let jobPageNumber = 1;
let jobSortColumn = "JobNo";
let jobSortOrder = "desc";
//let currencyChoice;
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

            // Initialize Choices if not already
            // if (!select.data('choices')) {
            //     let choiceInstance = new Choices(selector, {
            //         removeItemButton: true,
            //         shouldSort: false,
            //         dropdownAutoWidth: false,
            //         placeholderValue: placeholder
            //     });
            //     select.data('choices', choiceInstance);

            //     // Store instance globally for later use
            //     if (selector === '#ServiceType') serviceTypeChoice = choiceInstance;
            //     else if (selector === '#AccountHead') accountHeadChoice = choiceInstance;
            //     else if (selector === '#Customer') customerChoice = choiceInstance;
            //     else if (selector === '#Job-ShipmentMode') shipmentModeChoice = choiceInstance;
            //     else if (selector === '#Job-Currencies') currencyChoice = choiceInstance;
            // }

            if (callback) callback();
        }
    });
}

//#endregion

//#region Load AccountHead based on ServiceType
function loadAccountHeads($serviceTypeSelect, selectedHead = null) {

    let serviceTypeId = $serviceTypeSelect.val();
    let $accountHeadSelect = $('#AccountHead');

    // Destroy old Choices instance
    // if ($accountHeadSelect.data('choices')) {
    //     $accountHeadSelect.data('choices').destroy();
    //     $accountHeadSelect.removeData('choices');
    // }

    // Clear dropdown
    $accountHeadSelect.empty().append('<option value="">Select Accounts Head</option>').prop('disabled', false);

    if (!serviceTypeId) return;

    // Load Account Heads via AJAX
    $.get('/OperationFund/GetAccountsHeadDD', { serviceTypeId: serviceTypeId }, function (res) {

        res.forEach(item => {
            $accountHeadSelect.append(
                `<option value="${item.value}">${item.text}</option>`
            );
        });

        // Auto select
        if (selectedHead) {
            $accountHeadSelect.val(selectedHead);
        }
        //else if (res.length > 0) {
        //    $accountHeadSelect.val(res[0].value);
        //}

        // Initialize Choices.js
        // $accountHeadSelect.data('choices', new Choices($accountHeadSelect[0], {
        //     searchEnabled: true,
        //     shouldSort: false,
        //     placeholderValue: 'Select Accounts Head',
        //     removeItemButton: true
        // }));
    });
}
$(document).on('change', '#ServiceType', function () {
    loadAccountHeads($(this));
});


//#endregion

//#region Load Job Entry List
function loadJobEntryList() {
    console.log(jobPageNumber);
    $.ajax({
        url: '/OperationFund/GetAllJobList',
        type: 'GET',
        data: {
            pageNumber: jobPageNumber,
            pageSize: $('#Job-PageSize').val(),
            searchTerm: $('#Job-SearchInput').val(),
            sortColumn: jobSortColumn,
            sortOrder: jobSortOrder,
            customerid: $('#Customer').val(),
            shipmentmodeid: $('#Job-ShipmentMode').val()
        },
        success: function (res) {
            console.log("Job List :", res);
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
                                    <input type="radio" class="form-check-input job-radio" name="jobSelect" value="${item.jobNo}">
                                </td>
                                <td>${item.jobNo ?? ''}</td>
                                <td class="text-center">${formatDate(item.jobDate)}</td>
                                <td class="text-center">${item.shipmentMode ?? ''}</td>
                                <td>${item.customer ?? ''}</td>
                                <td class="text-center">${formatDate(item.docReceivedDate)}</td>
                            </tr>`;
                });
            }

            $('#Job-tbody').html(rows);

            buildJobPagination(res.totalCount);
            updateJobEntryInfo(res.filterCount, res.totalCount);
        }
    });
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
        // Radio was already checked → now unchecked
        $(this).prop('checked', false);
        $(this).data('waschecked', false);

        // Clear input fields
        clearJobHeaderFields();
        //toastr.info("Job unselected");

    } else {
        // Normal check
        $('input[name="jobSelect"]').data('waschecked', false);
        $(this).data('waschecked', true);

        // Load header data
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
        //    currencyChoice.removeActiveItems();
        currencyChoice.setChoiceByValue("");
    }
}

//#endregion


//#region Job Pagination
function buildJobPagination(totalCount) {

    let pageSize = parseInt($('#Job-PageSize').val());
    let pagination = '';

    if (pageSize === -1 || totalCount === 0) {
        $('#Job-Pagination').html('');
        return;
    }

    let totalPages = Math.ceil(totalCount / pageSize);
    let current = jobPageNumber;

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

    $('#Job-Pagination').html(pagination);
}

// helper
function pageItem(page, current) {
    return `
        <li class="page-item ${page === current ? 'active' : ''}">
            <a class="page-link" href="#" onclick="changeJobPage(${page})">${page}</a>
        </li>`;
}


function changeJobPage(page) {
    jobPageNumber = page;
    loadJobEntryList();
}
//#endregion


//#region Update Text
function updateJobEntryInfo(filterCount, totalCount = null) {
    let pageSize = $('#Job-PageSize').val();
    let searchTerm = $('#Job-SearchInput').val();

    let text;
    if (pageSize == -1) {
        text = `Showing 1 to ${filterCount} of ${filterCount} entries`;
    } else {
        let start = ((jobPageNumber - 1) * pageSize) + 1;
        let end = Math.min(jobPageNumber * pageSize, filterCount);
        text = `Showing ${start} to ${end} of ${filterCount} entries`;
    }

    if (searchTerm && totalCount && totalCount !== filterCount) {
        text += ` (filtered from ${totalCount} total entries)`;
    }

    $('.Job-Pagination-message').text(text);
}
//#endregion


//#region Search, Filter and PageSize

$('#Job-SearchInput').on('input', function () {
    jobPageNumber = 1;
    if ($(this).val() === '') {
        loadJobEntryList();
    } else {
        loadJobEntryList();
    }
});


$('#Job-PageSize').on('change', function () {
    jobPageNumber = 1;
    loadJobEntryList();
});

$('#Customer, #Job-ShipmentMode').on('change', function () {
    jobPageNumber = 1;
    loadJobEntryList();
});
//#endregion


//#region Sorting
$('#JobTable thead th[data-column]').on('click', function () {

    let column = $(this).data('column');
    console.log(column);
    if (jobSortColumn === column) {
        jobSortOrder = jobSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        jobSortColumn = column;
        jobSortOrder = 'asc';
    }

    jobPageNumber = 1;
    loadJobEntryList();
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
