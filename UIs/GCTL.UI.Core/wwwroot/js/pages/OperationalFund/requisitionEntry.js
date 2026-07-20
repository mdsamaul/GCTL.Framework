//#region Global Veriable
let ReqPageNumber = 1;
let ReqSortColumn = "OFRNo";
let ReqSortOrder = "desc";
//#endregion

//#region Load Requisition List
function LoadRequisitionList() {

    $.ajax({
        url: '/OperationFund/GetAllRequisition',
        type: 'GET',
        data: {
            pageNumber: ReqPageNumber,
            pageSize: $('#Req-PageSize').val(),
            searchTerm: $('#Req-Search').val(),
            sortColumn: ReqSortColumn,
            sortOrder: ReqSortOrder,

        },
        success: function (res) {
            console.log("Req List :", res);
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
                                    <input type="checkbox" class="form-check-input " name="ReqSelect" value="${item.ofrNo}">
                                </td>

                                <td class="text-center fw-bold text-dark">
                                    <span class="ofr-no-link" style="cursor:pointer; text-decoration:underline;">
                                        ${item.ofrNo ?? ''}
                                    </span>
                                </td>

                                <td class="text-center text-dark text-muted fs-9 fw-bold">${formatDate(item.ofrDate)}</td>
                                <td class="text-center text-dark text-muted fs-9 fw-bold">${item.jobNo ?? ''}</td>
                                <td class="text-center text-dark text-muted fs-9 fw-bold">${item.customerName ?? ''}</td>
                                <td class="text-left text-dark text-muted fs-9 fw-bold">${item.billingAddress ?? ''}</td>
                                <td class="text-center text-dark text-muted fs-9 fw-bold">${item.shipmentMode ?? ''}</td>
                            </tr>`;
                });
            }

            $('#Requisition-body').html(rows);

            ReqPagination(res.totalCount);
            ReqUpdatePagi(res.filterCount, res.totalCount);
        }
    });
}
//#endregion

//#region Job Pagination
function ReqPagination(totalCount) {

    let pageSize = parseInt($('#Req-PageSize').val());
    let pagination = '';

    if (pageSize === -1 || totalCount === 0) {
        $('#Req-Pagination').html('');
        return;
    }

    let totalPages = Math.ceil(totalCount / pageSize);
    let current = ReqPageNumber;

    // Previous
    pagination += `
        <li class="page-item ${current === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="ReqchangePage(${current - 1})">Previous</a>
        </li>`;

    // Always show first 2 pages
    for (let i = 1; i <= Math.min(2, totalPages); i++) {
        pagination += billPageItem(i, current);
    }

    // Dots before middle
    if (current > 4) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Middle pages (current ±1)
    let start = Math.max(3, current - 1);
    let end = Math.min(totalPages - 2, current + 1);

    for (let i = start; i <= end; i++) {
        pagination += billPageItem(i, current);
    }

    // Dots after middle
    if (current < totalPages - 3) {
        pagination += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }

    // Always show last 2 pages
    for (let i = Math.max(totalPages - 1, 3); i <= totalPages; i++) {
        pagination += billPageItem(i, current);
    }

    // Next
    pagination += `
        <li class="page-item ${current === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="ReqchangePage(${current + 1})">Next</a>
        </li>`;

    $('#Req-Pagination').html(pagination);
}

// helper
function billPageItem(page, current) {
    return `
        <li class="page-item ${page === current ? 'active' : ''}">
            <a class="page-link" href="#" onclick="ReqchangePage(${page})">${page}</a>
        </li>`;
}


function ReqchangePage(page) {
    ReqPageNumber = page;
    LoadRequisitionList();
}
//#endregion

//#region Update Text

function ReqUpdatePagi(filterCount, totalCount = null) {
    let pageSize = $('#Req-PageSize').val();
    let searchTerm = $('#Req-Search').val();

    let text;
    if (pageSize == -1) {
        text = `Showing 1 to ${filterCount} of ${filterCount} entries`;
    } else {
        let start = ((ReqPageNumber - 1) * pageSize) + 1;
        let end = Math.min(ReqPageNumber * pageSize, filterCount);

        text = (`Showing ${start} to ${end} of ${filterCount} entries`);
    }

    if (searchTerm && totalCount && totalCount !== filterCount) {
        text += ` (filtered from ${totalCount} total entries)`;
    }

    $('.Req-Pagination-message').text(text);
}
//#endregion

//#region Search and PageSize

$('#Req-Search').on('input', function () {
    ReqPageNumber = 1;
    if ($(this).val() === '') {
        LoadRequisitionList();
    } else {
        LoadRequisitionList();
    }
});


$('#Req-PageSize').on('change', function () {
    ReqPageNumber = 1;
    LoadRequisitionList();
});

//#endregion

//#region Sorting
$('#FundTable thead th[data-column]').on('click', function () {

    let column = $(this).data('column');

    if (ReqSortColumn === column) {
        ReqSortOrder = ReqSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        ReqSortColumn = column;
        ReqSortOrder = 'asc';
    }

    ReqPageNumber = 1;
    LoadRequisitionList();
});
//#endregion

//#region Select Checkbox single and Multi
// Select All
$('#Req-selectAll').on('change', function () {
    $('input[name="ReqSelect"]').prop('checked', this.checked);
});

// Single checkbox change → SelectAll sync
$(document).on('change', 'input[name="ReqSelect"]', function () {
    let total = $('input[name="ReqSelect"]').length;
    let checked = $('input[name="ReqSelect"]:checked').length;

    $('#Req-selectAll').prop('checked', total === checked);
});

//#endregion

//#region Delete Single & Multi Master & Details
$('#Req-DeleteBtn').on('click', function () {
    let ofrNos = [];

    $('input[name="ReqSelect"]:checked').each(function () {
        ofrNos.push($(this).val());
    });

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
            // reset
            $('#Req-selectAll').prop('checked', false);
            ReqPageNumber = 1;

            LoadRequisitionList();
            loadJobEntryList();
            ClearAfterClikButton();
            ClearTmpDetail()
        },
        error: function () {
            toastr.error('Delete Failed!');
        }
    });
});

//#endregion


//#region Load Requisition Details
// Function to set date from API
function SetRequisitionDate(dateStr) {
    if (reqDatePicker) {
        // Convert ISO date string to JS Date
        let jsDate = dateStr ? new Date(dateStr) : new Date();
        reqDatePicker.setDate(jsDate, true, "d-m-Y"); // true = trigger change
    }
}

// Copy to Tmp table for current user
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
            console.log("Requisition Details Are :", res);
            if (!res) {
                toastr.warning('No data found');
                return;
            }

            //Fill master/form fields
            $('#Job-JoBNO').val(res.jobNo ?? '');
            $('#Job-ShipmentModeInput').val(res.shipmentMode ?? '');
            $('#Job-CustomerName').val(res.customerName ?? '');
            $('#Job-LCValue').val(res.lcValue ?? '');
            $('#Job-InvoiceNo').val(res.invoiceNo ?? '');
            $('#Job-InvoiceValue').val(res.invoiceValue ?? '');
            $('#Job-RequisitionNo').val(res.reqNo ?? '');
            $('#CustomerID').val(res.customerID ?? '');
            $('#ShipmentModeID').val(res.shipmentModeID ?? '');

            // Fill Requisition Date
            SetRequisitionDate(res.reqDate);
            //$('#Remark').val(res.remark ?? '');

            // Set Choices dropdown values using instances
            //if (serviceTypeChoice) serviceTypeChoice.setChoiceByValue(res.serviceTypeID);
            // Load Account Head AFTER service type is set
            //loadAccountHeads($('#ServiceType'), res.accountHeadID);
            if (currencyChoice) currencyChoice.setChoiceByValue(res.currencyID);

            // Documentation fields
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

//#region Ready Part
$(document).ready(function () {
    //Load list
    LoadRequisitionList();

    $(document).on('click', '.ofr-no-link', function () {
        let ofrNo = $(this).text().trim();
        //    LoadRequisitionDetails(ofrNo);
        copyToTmp(ofrNo).done(function () {
            //load requisition details
            LoadRequisitionDetails(ofrNo);
            loadTmpDetails();
        }).fail(function () {
            toastr.error('Failed to load Expenses in Details!');
        });
    });
});
//#endregion
