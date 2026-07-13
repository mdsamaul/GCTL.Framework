let currentPage = 1;
let pageSize = 10;
let currentSortColumn = 'ExpenseHeadID';
let currentSortingDirection = 'desc';
let totalPages = 1;
let filters = {
    expenseType: "",
    isReceiptable: "",
    serviceType: ""
};
let selectedExpenseHead = [];

// Loading Next Expense Head ID
function loadNextExpenseHeadId() {
    $.ajax({
        url: '/next-expense-head-id',
        method: 'GET',
        success: function (response) {
            $('#expenseHeadId').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed to load next Expense Head ID:", error);
        }
    });
}

// Loading ExpenseType into Dropdown
function loadExpenseTypes(selectedId = null) {
    $.ajax({
        url: '/expense-type-dropdown',
        type: 'GET',
        success: function (data) {
            var select = $("#expenseType");
            select.empty();
            select.append('<option value=""></option>');
            $.each(data, function (index, item) {
                var isSelected = selectedId == item.expenseTypeID ? 'selected' : '';
                select.append('<option value="' + item.expenseTypeID + '" ' + isSelected + '>' + item.expenseType + '</option>');
            });
        },
        error: function (xhr, status, error) {
            console.error("Failed to load Expense Types:", error);
        }
    });
}

// Loading Core_ServiceType
function loadServiceProviderDropdown() {
    $.ajax({
        url: '/core-service-dropdown',
        method: 'GET',
        success: function (response) {
            let $dropdown = $('#serviceProvider');
            $dropdown.empty();
            $dropdown.append(`<option value=""> </option>`);
            if (response && response.length > 0) {
                response.forEach(item => {
                    $dropdown.append(`<option value="${item.serviceTypeID}">${item.serviceTypeName}</option>`);
                });
            }
        },
        error: function (xhr) {
            console.error("Failed to load Service Provider dropdown", xhr);
        }
    });
}

// Save or Update With Duplicate Checking
function SaveOrUpdateExpenseHead() {
    let hiddenID = parseInt($("#tc").val()) || 0;
    let isEdit = hiddenID > 0;
    let expenseHeadData = {
        TC: hiddenID,
        ExpenseHeadID: $("#expenseHeadId").val()?.trim(),
        ExpenseHead: $("#expenseHead").val()?.trim(),
        ShortName: $("#shortName").val()?.trim(),
        Amount: parseFloat($("#amount").val()) || 0,
        SerialNo: $("#serialNo").val()?.trim(),
        IsReceiptable: $("#isReceiptable").is(":checked") ? "Yes" : "No",
        ServiceTypeID: $("#serviceProvider").val(),
        ExpenseTypeID: $("#expenseType").val(),
        EntryType: ""
    };

    // Validation
    if (!expenseHeadData.ExpenseTypeID) {
        toastr.error("Please select Expense Type name.");
        $("#expenseType").addClass('error-border').focus();
        return;
    } else {
        $("#expenseType").removeClass('error-border');
    }
    if (!expenseHeadData.ExpenseHead) {
        toastr.error("Please enter Expense Head name.");
        $("#expenseHead").addClass('error-border').focus();
        return;
    } else {
        $("#expenseHead").removeClass('error-border');
    }
    if (!expenseHeadData.SerialNo) {
        toastr.error("Please enter Serial No.");
        $("#serialNo").addClass('error-border').focus();
        return;
    } else {
        $("#serialNo").removeClass('error-border');
    }
    if (!expenseHeadData.ServiceTypeID) {
        toastr.error("Please enter Service Provider Name.");
        $("#serviceProvider").addClass('error-border').focus();
        return;
    } else {
        $("#serviceProvider").removeClass('error-border');
    }

    // Checking duplicate
    $.ajax({
        url: '/expense-head/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(expenseHeadData),
        success: function (dupRes) {
            if (dupRes.isDuplicate) {
                toastr.error(dupRes.message);
                return;
            } else {
                let url = isEdit ? `/expense-head/${hiddenID}` : '/expense-head';
                let type = isEdit ? 'PUT' : 'POST';

                $.ajax({
                    url: url,
                    type: type,
                    contentType: 'application/json',
                    data: JSON.stringify(expenseHeadData),
                    success: function (res) {
                        if (res.success) {
                            toastr.success(res.message);
                            ResetExpenseHeadForm();
                            LoadExpenseHeadList(currentPage, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim());
                        } else {
                            toastr.error(res.message);
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || (isEdit ? "Update Failed." : "Insertion Failed."));
                    }
                });
            }
        },
        error: function () {
            toastr.error("Failed to check duplicate.");
        }
    });
}

// Edit Handle
$(document).on("click", ".edit-expense-head", function () {
    let item = $(this).data("item");

    if (typeof item === "string") item = JSON.parse(item);

    $("#tc").val(item.tc || 0);
    $("#expenseHeadId").val(item.expenseHeadID || "");
    $("#expenseHead").val(item.expenseHead || "");
    $("#shortName").val(item.shortName || "");
    $("#amount").val(item.amount && item.amount != 0 ? item.amount : "");
    $("#serialNo").val(item.serialNo || "");
    $("#isReceiptable").prop("checked", item.isReceiptable === "Yes");
    $("#serviceProvider").val(item.serviceTypeID || "");
    $("#expenseType").val(item.expenseTypeID || "");

    // Update global filters
    filters.expenseType = item.expenseTypeID || "";
    filters.isReceiptable = item.isReceiptable || "";
    filters.serviceType = item.serviceTypeID || "";

    if (item.lDate) {
        const lDate = new Date(item.lDate).toLocaleDateString('en-GB');
        $("#lDateContainer").show();
        $("#displayLDate").text(lDate);
    } else {
        $("#lDateContainer").hide();
    }
    if (item.modifyDate) {
        const modifyDate = new Date(item.modifyDate).toLocaleDateString('en-GB');
        $("#modifyDateContainer").show();
        $("#displayModifyDate").text(modifyDate);
    } else {
        $("#modifyDateContainer").hide();
    }
    selectedExpenseHead = [parseInt(item.tc)];
    console.log("Selected Expense populate", selectedExpenseHead);
    LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, "", filters);
});

// Bulk Delete
function BulkDeleteExpenseHead() {
    let selectedIds = [...selectedExpenseHead];
    $(".row-checkbox:checked").each(function () {
        selectedIds.push(parseInt($(this).data("id")));
    });

    if (selectedIds.length === 0) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    $.ajax({
        url: '/expense-head-bulk',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedIds),
        success: function (res) {
            if (res.isSuccess) {
                toastr.success(res.message);
                ResetExpenseHeadForm();
                LoadExpenseHeadList(currentPage, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim());
                $('#selectAll').prop('checked', false);
            } else {
                toastr.error(res.message);
            }
        },
        error: function (xhr) {
            toastr.error(xhr.responseJSON?.message || "Failed to delete selected records.");
        }
    });
}

// Update Checkbox State
function updateCheckboxState() {
    $(".row-checkbox").off("change").on("change", function () {
        $("#selectAll").prop("checked", $(".row-checkbox:checked").length === $(".row-checkbox").length);
    });

    $("#selectAll").off("change").on("change", function () {
        $(".row-checkbox").prop("checked", this.checked);
    });
}

// Sorting Indicator
function updateSortIndicators() {
    $(".sortable").each(function () {
        let column = $(this).data("column");
        let icon = $(this).find(".sort-icon");

        icon.removeClass("fa-sort-up fa-sort-down").addClass("fa-sort");
        if (column === currentSortColumn) {
            icon.removeClass("fa-sort");
            icon.addClass(currentSortingDirection === "asc" ? "fa-sort-up" : "fa-sort-down");
        }
    });
}

// Click event for sorting
$(document).on('click', '.sortable', function () {
    let column = $(this).data('column');

    if (currentSortColumn === column) {
        currentSortingDirection = currentSortingDirection === 'asc' ? 'desc' : 'asc';
    } else {
        currentSortColumn = column;
        currentSortingDirection = 'asc';
    }

    LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
});

// Pagination
function generatePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#pageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm')
            .text(page)
            .click(() => LoadExpenseHeadList(page, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters));

        if (page === currentPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    if (totalPages > 0) {
        navigationDiv.append(createButton(1));
    }

    if (currentPage > 2) {
        navigationDiv.append($('<span>').text('..').css('padding', '0 2px'));
    }

    if (currentPage !== 1 && currentPage !== totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('..').css('padding', '0 2px'));
    }

    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

// Clear Form Function
function ResetExpenseHeadForm() {
    $("#tc").val("");
    $("#searchInput").val('');
    $("#expenseHeadId").val("");
    $("#expenseHead").val("");
    $("#shortName").val("");
    $("#amount").val("");
    $("#serialNo").val("");
    $("#isReceiptable").prop("checked", false);
    $("#serviceProvider").val("");
    $("#expenseType").val("");
    $("#displayLDate").text('');
    $("#lDateContainer").hide();
    $("#displayModifyDate").text('');
    $("#modifyDateContainer").hide();
    $('#selectAll').prop('checked', false);
    $(".row-checkbox").prop('checked', false);

    // Reset filters
    filters = {
        expenseType: "",
        isReceiptable: "",
        serviceType: ""
    };

    loadNextExpenseHeadId();
    LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, '');
}

// Load Next Serial Number
let manualSerialEntry = false;

$("#serialNo").on("input", function () {
    let value = $(this).val().trim();
    if (value && (!/^\d+$/.test(value) || parseInt(value) <= 0)) {
        toastr.error("Serial No must be a positive integer.");
        $(this).val("");
        manualSerialEntry = false;
    } else {
        manualSerialEntry = value !== "";
        console.log("Manual Serial Entry:", manualSerialEntry, "Value:", value); 
        if (manualSerialEntry) {
            LoadNextSerialNo(); // Trigger serial check on valid input
        }
    }
});

// Load next serial
function LoadNextSerialNo() {
    let expenseType = $("#expenseType").val();
    let serviceType = $("#serviceProvider").val();
    let isReceiptable = $("#isReceiptable").is(":checked") ? "Yes" : "No";
    let requestedSerial = manualSerialEntry ? parseInt($("#serialNo").val()) : null;

    if (!expenseType) {
        $("#serialNo").val("");
        return;
    }

    console.log("Sending AJAX request with:", { expenseType, serviceType, isReceiptable, requestedSerial }); // Debug log

    $.ajax({
        url: '/serial-no',
        method: 'GET',
        data: {
            expenseType: expenseType,
            serviceType: serviceType,
            isReceiptable: isReceiptable,
            requestedSerial: requestedSerial
        },
        success: function (res) {
            console.log("Received serial number:", res.serialNo); 
            if (!manualSerialEntry) {
                $("#serialNo").val(res.serialNo);
            }
        },
        error: function (xhr) {
            console.error("Failed to load serial number:", xhr.responseJSON?.message); // Debug log
            toastr.error(xhr.responseJSON?.message || "Failed to load serial number.");
            if (!manualSerialEntry) $("#serialNo").val("");
        }
    });
}

// Reset manual flag on dropdown change
$("#expenseType, #serviceProvider, #isReceiptable").on("change", function () {
    manualSerialEntry = false;
    console.log("Dropdown changed, resetting manualSerialEntry and loading next serial"); // Debug log
    LoadNextSerialNo();
});


// Load Expense Head List
function LoadExpenseHeadList(page = 1, pageSizeVal = pageSize, sortColumn = currentSortColumn, sortDirection = currentSortingDirection, searchTerm = "", filters = {}) {
    currentPage = page;
    pageSize = pageSizeVal;
    currentSortColumn = sortColumn;
    currentSortingDirection = sortDirection;

    console.log("Loading ExpenseHead List:", { currentPage, pageSize, filters });

    $.ajax({
        url: '/expense-head-list',
        method: "GET",
        data: {
            pageNumber: page,
            pageSize: pageSizeVal,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            expenseType: filters.expenseType || "",
            isReceiptable: filters.isReceiptable || "",
            serviceType: filters.serviceType || ""
        },
        success: function (response) {
            console.log("AJAX Response:", response);

            let rows = '';
            if (!response.data || response.data.length === 0) {
                rows = `<tr class="no-data"><td colspan="7" class="text-center">No Data Available</td></tr>`;
            } else {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center align-middle" style="width: 3px;">
                            <input type="checkbox"  class="row-checkbox" data-id="${item.tc}">
                        </td>
                        <td class="text-center align-middle">
                            <button type="button" class="btn btn-link edit-expense-head p-0 m-0"
                                data-item='${JSON.stringify(item)}'>
                                ${item.expenseHeadID}
                            </button>
                        </td>
                        <td class="align-middle">${item.expenseHead || ''}</td>
                        <td class="text-center align-middle">${item.serialNo || ''}</td>
                        <td class="text-center align-middle">${item.isReceiptable || ''}</td>
                        <td class="align-middle">${item.serviceTypeName || ''}</td>
                        <td class="align-middle">${item.expenseType || ''}</td>
                    </tr>`;
                });
            }
            $('#tblBody').html(rows);

            const startIndex = response.paginationInfo?.startItem || 0;
            const endIndex = response.paginationInfo?.endItem || 0;
            const totalRecords = response.paginationInfo?.totalItems || 0;
            totalPages = response.paginationInfo?.totalPages || Math.ceil(totalRecords / pageSizeVal);

            console.log("Pagination Info:", { startIndex, endIndex, totalRecords, totalPages });

            $('#paginationInfo').text(`Page ${currentPage} of ${totalPages}`);
            $('#paginationSummary').text(`Showing ${startIndex} to ${endIndex} Items of ${totalRecords} entries`);
            $('#totalRecordsInfo').text(`Total Records: ${totalRecords}`);

            generatePageButtons(currentPage, totalPages);
            $('#selectAll').prop('checked', false);
            updateCheckboxState();
            updateSortIndicators();
        },
        error: function (xhr) {
            console.error("Failed to load ExpenseHead List:", xhr);
            $('#tblBody').html(`<tr class="no-data"><td colspan="7" class="text-center">Failed to load data</td></tr>`);
        }
    });
}

// Filter event
$('#expenseType, #isReceiptable, #serviceProvider').on('change', function () {
    filters.expenseType = $('#expenseType').val();
    filters.isReceiptable = $('#isReceiptable').is(':checked') ? "Yes" : "No";
    filters.serviceType = $('#serviceProvider').val();
    LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
});

// Get Current Filters
function getCurrentFilters() {
    return {
        expenseType: $('#expenseType').val() || filters.expenseType,
        isReceiptable: filters.isReceiptable,
        serviceType: $('#serviceProvider').val() || filters.serviceType
    };
}

$(document).ready(function () {
    loadNextExpenseHeadId();
    loadExpenseTypes();
    loadServiceProviderDropdown();
    LoadExpenseHeadList();
    LoadNextSerialNo();

    // Handle form submission
    $("#expenseForm").on("submit", function (e) {
        e.preventDefault();
        SaveOrUpdateExpenseHead();
    });
    $(".saveBtn").on("click", function () {
        if ($(this).attr("type") === "button") {
            SaveOrUpdateExpenseHead();
        }
    });

    // Handle delete button click
    $(".deleteBtn").on("click", function (e) {
        e.preventDefault();
        BulkDeleteExpenseHead();
    });

    // Handle clear button
    $(".resetBtn").on("click", function () {
        ResetExpenseHeadForm();
        loadNextExpenseHeadId();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".favBtn").on("click", function () {
        //toggleFavorite();
    });

    // Pagination Button Clicks
    $('#firstPage').on('click', () => {
        if (currentPage > 1) LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
        console.log("First Clicked | totalPages:", totalPages);
    });

    $('#lastPage').on('click', () => {
        console.log("Last page clicked", { currentPage, totalPages });
        if (currentPage < totalPages) LoadExpenseHeadList(totalPages, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
    });

    $('#prevPage').on('click', () => {
        if (currentPage > 1) LoadExpenseHeadList(currentPage - 1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
        console.log("Prev Clicked | currentPage:", currentPage);
    });

    $('#nextPage').on('click', () => {
        if (currentPage < totalPages) LoadExpenseHeadList(currentPage + 1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
        console.log("Next Clicked | totalPages:", totalPages);
    });

    $('#pageSize').on('change', function () {
        pageSize = parseInt($(this).val());
        LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, $('#searchInput').val().trim(), filters);
    });

    $('#searchInput').on('keyup', function () {
        let searchTerm = $(this).val().trim();
        LoadExpenseHeadList(1, pageSize, currentSortColumn, currentSortingDirection, searchTerm, filters);
    });


    //Report section

    // PDF Export
    $(document).on('click', '.export-pdf', function (e) {
        e.preventDefault();
        ExportExpenseHeadReport('pdf');
    });

    // Excel Export
    $(document).on('click', '.export-excel', function (e) {
        e.preventDefault();
        ExportExpenseHeadReport('excel');
    });
});



// ------------------------- Report Export -------------------------
function ExportExpenseHeadReport(format) {
    let expenseTypeId = $('#expenseType').val() || '';

    let url = `/expense-head/report?expenseTypeId=${expenseTypeId}&format=${format}`;


    $.ajax({
        url: url,
        type: 'GET',
        xhrFields: {
            responseType: 'blob' // important to receive file as blob
        },
        success: function (data, status, xhr) {
            // Get filename from response header if exists
            let filename = "";
            const disposition = xhr.getResponseHeader('Content-Disposition');
            if (disposition && disposition.indexOf('attachment') !== -1) {
                const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                const matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) filename = matches[1].replace(/['"]/g, '');
            }
            if (!filename) {
                filename = format === 'excel' ? 'ExpenseHeadReport.xlsx' : 'ExpenseHeadReport.pdf';
            }

            // Create temporary link and download
            const link = document.createElement('a');
            const blob = new Blob([data], { type: xhr.getResponseHeader('Content-Type') });
            link.href = window.URL.createObjectURL(blob);
            link.download = filename;
            link.click();
            window.URL.revokeObjectURL(link.href);
        },
        error: function (xhr, status, error) {
            console.error("Export failed:", error);
            toastr.error("Failed to generate report.");
        }
    });
}




