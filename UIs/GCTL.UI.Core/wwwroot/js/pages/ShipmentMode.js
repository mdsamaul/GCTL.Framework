let currentPage = 1;
let pageSize = 10;
let selectedExpenseTypes = [];
let currentSortColumn = 'ExpenseTypeID';
let currentSortDirection = 'desc';
let totalPages = 1;
let currentSearchTerm = "";


console.log(`Global Call: ${selectedExpenseTypes}`);
function getNextExpenseTypeId() {
    $.ajax({
        url: '/next-expense-type-id', 
        type: 'GET',
        success: function (response) {
            $('#expenseTypeId').val(response);
        },
        error: function (xhr) {
            console.error("Error fetching next Expense Type ID:", xhr.responseText);
        }
    });
}
function clearShipmentForm() {
    $('#shipmentForm')[0].reset();
    $('#expenseType').removeClass('error-border');
    $('#modifyDateContainer').hide();
    $('#lDateContainer').hide();
    $("#searchInput").val('');
    $('#shipmentForm input[name="tcId"]').val(0);

    selectedExpenseTypes = []; 
    $('.row-checkbox').prop('checked', false);
    $('#selectAll').prop('checked', false);

    currentPage = 1;
    const emptySearch = '';
    loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection, emptySearch);
    getNextExpenseTypeId();

}
function saveOrUpdateShipmentMode() {
    let tcId = parseInt($('#shipmentForm input[name="tcId"]').val()) || 0; // force number
    const expenseTypeId = $('#shipmentForm input[name="expenseTypeId"]').val();
    const expenseType = $('#shipmentForm input[name="expenseType"]').val().trim();
    const shortName = $('#shipmentForm input[name="shortName"]').val().trim();

    if (!expenseType) {
        toastr.warning("Expense Type is required.");
        $('#expenseType').addClass('error-border').focus();
        return;
    }

    let model = {
        TC: tcId,
        ExpenseTypeID: expenseTypeId,
        ExpenseType: expenseType,
        ShortName: shortName
    };

    $.ajax({
        url: '/shipment-mode/is-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {
            if (response.isDuplicate) {
                toastr.warning(response.message || "Data already exists!");
                return;
            }

            if (tcId === 0) {   
                $.ajax({
                    url: '/shipment-mode',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data saved successfully.");
                        clearShipmentForm();
                        getNextExpenseTypeId();
                        loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection);
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Error saving data.");
                    }
                });
            } else {   
                $.ajax({
                    url: `/shipment-mode/${tcId}`,
                    type: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data updated successfully.");
                        clearShipmentForm();
                        loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection);
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Update Failed.");
                    }
                });
            }
        },
        error: function () {
            toastr.error("Error checking duplicate.");
        }
    });
}

function deleteSelectedShipmentModes() {
    let selectedIds = [...selectedExpenseTypes];
    if (selectedIds.length === 0) {
        toastr.warning("Please select at least one item to delete.");
        return;
    }


    $.ajax({
        url: '/shipment-mode-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedExpenseTypes), 
        success: function (response) {
            selectedExpenseTypes = [];
            $('#selectAll').prop('checked', false);
            $("#searchInput").val('');
            clearShipmentForm();
            loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection);

            if (response.isSuccess) {
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function (xhr) {
            let msg = "An error occurred while deleting shipment modes!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment modes:', xhr);
        }
    });
}

function loadExpenseTypes(page = 1, pageSize = 10, sortColumn = currentSortColumn, sortDirection = currentSortDirection, searchTerm = "") {
    currentPage = page;

    $.ajax({
        url: '/expense-types',
        type: 'GET',
        data: {
            page: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            currentSortColumn: sortColumn,
            sortDirection: sortDirection
        },

        success: function (response) {
            console.log('Expense Table:', response);
            let rows = '';

            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                <td class="text-center align-middle">
                    <input type="checkbox" class="form-check-input row-checkbox"
                        data-expensetypeid="${item.tc}">
                </td>
                <td class="text-center align-middle">
                    <button class="btn btn-transfer"
                        data-tc="${item.tc}"
                        data-expensetypeid="${item.expenseTypeID}"
                        data-expensetype="${item.expenseType}"
                        data-shortname="${item.shortName}"
                        data-ldate="${item.lDate}"
                        data-modifydate="${item.modifyDate}">
                        ${item.expenseTypeID}
                    </button>
                </td>
                <td>${item.expenseType || ''}</td>
                <td>${item.shortName || ''}</td>
            </tr>`;
                });


                $('#tblBody').html(rows);

                const totalRecords = response.totalCount || 0;
                const pagination = response.paginationInfo || {};
                totalPages = pagination.totalPages || 1;

                const startIndex = pagination.startItem || 0;
                const endIndex = pagination.endItem || 0;

                $('#paginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#paginationInfo').text(`Page ${currentPage} of ${totalPages}`);
                $('#totalRecordsInfo').text(`Total Records: ${totalRecords}`);

                generatePageButtons(currentPage, totalPages);
                $('#selectAll').prop('checked', false);
                updateCheckboxState();
                updateSortIndicators();
            } else {
                $('#tblBody').html(`<tr class="no-data"><td colspan="4" class="text-center">No Data Available</td></tr>`);
                $('#paginationSummary').text('');
                $('#paginationInfo').text('');
                $('#totalRecordsInfo').text('');
                $('#pageNavigation').html('');
                $('#selectAll').prop('checked', false);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading data: ", error);
            $('#tblBody').html(`<tr class="no-data"><td colspan="4" class="text-center">Failed to load data.</td></tr>`);
            $('#paginationSummary').text('');
            $('#paginationInfo').text('');
            $('#totalRecordsInfo').text('');
            $('#pageNavigation').html('');
            $('#selectAll').prop('checked', false);
        }
    });
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return dateString;
    return date.toLocaleDateString();
}

function updateSortIndicators() {
    $('.sortable').removeClass('sort-asc sort-desc');
    $('.sortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    $(`.sortable[data-column="${currentSortColumn}"]`).addClass(`sort-${currentSortDirection}`);

    if (currentSortDirection === 'asc') {
        $(`.sortable[data-column="${currentSortColumn}"] .sort-icon`)
            .removeClass('fa-sort fa-sort-down')
            .addClass('fa-sort-up');
    } else {
        $(`.sortable[data-column="${currentSortColumn}"] .sort-icon`)
            .removeClass('fa-sort fa-sort-up')
            .addClass('fa-sort-down');
    }
}

function updateCheckboxState() {
    $('.row-checkbox').each(function () {
        const expenseTypeID = $(this).data('expensetypeid');
        $(this).prop('checked', selectedExpenseTypes.includes(expenseTypeID));
    });
    const allChecked = $('.row-checkbox:visible').length > 0 &&
        $('.row-checkbox:visible:not(:checked)').length === 0;
    $('#selectAll').prop('checked', allChecked);
}

function generatePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#pageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        return $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .toggleClass('btn-primary', page === currentPage)
            .toggleClass('btn-outline-primary', page !== currentPage)
            .click(() => loadExpenseTypes(page, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm));
    }

    if (totalPages <= 1) {
        navigationDiv.append(createButton(1));
        return;
    }

    navigationDiv.append(createButton(1));

    if (currentPage > 3) navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    for (let p = Math.max(2, currentPage - 1); p <= Math.min(totalPages - 1, currentPage + 1); p++) {
        navigationDiv.append(createButton(p));
    }
    if (currentPage < totalPages - 2) navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));

    if (totalPages > 1) navigationDiv.append(createButton(totalPages));
}


$(document).on('click', '.sortable', function () {
    const column = $(this).data('column');

    if (column === currentSortColumn) {
        currentSortDirection = (currentSortDirection === 'asc') ? 'desc' : 'asc';
    } else {
        currentSortColumn = column;
        currentSortDirection = 'asc';
    }

    loadExpenseTypes(1, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
});

function debounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}


$(document).on('click', '.btn-transfer', function () {
    const data = {
        tc: $(this).data('tc'),
        expenseTypeID: $(this).data('expensetypeid'),
        expenseType: $(this).data('expensetype'),
        shortName: $(this).data('shortname'),
        lDate: $(this).data('ldate'),
        modifyDate: $(this).data('modifydate')
    };
    populateExpenseTypeForm(data);

});

function populateExpenseTypeForm(data) {
    $('#shipmentForm input[name="expenseTypeId"]').val(data.expenseTypeID);
    $('#shipmentForm input[name="expenseType"]').val(data.expenseType);
    $('#shipmentForm input[name="shortName"]').val(data.shortName);
    $('#shipmentForm input[name="tcId"]').val(data.tc);

    if (data.lDate) {
        const formattedLDate = new Date(data.lDate).toLocaleDateString('en-GB');
        $('#displayLDate').text(formattedLDate);
        $('#lDateContainer').show();
    } else {
        $('#lDateContainer').hide();
    }

    if (data.modifyDate) {
        const formattedModifyDate = new Date(data.modifyDate).toLocaleDateString('en-GB');
        $('#displayModifyDate').text(formattedModifyDate);
        $('#modifyDateContainer').show();
    } else {
        $('#modifyDateContainer').hide();
    }
    selectedExpenseTypes = [parseInt(data.tc)];
    console.log("Selected Shipment Mode after populate:", selectedExpenseTypes);
}

$(document).on('click', '#selectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.row-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.row-checkbox:visible').each(function () {
            const expenseTypeID = $(this).data('expensetypeid');
            if (!selectedExpenseTypes.includes(expenseTypeID)) {
                selectedExpenseTypes.push(expenseTypeID);
            }
        });
    } else {
        $('.row-checkbox:visible').each(function () {
            const expenseTypeID = $(this).data('expensetypeid');
            const index = selectedExpenseTypes.indexOf(expenseTypeID);
            if (index !== -1) {
                selectedExpenseTypes.splice(index, 1);
            }
        });
    }
});


$(document).on('click', '.row-checkbox', function () {
    const expenseTypeID = $(this).data('expensetypeid');
    const isChecked = $(this).prop('checked');
    if (isChecked) {
        if (!selectedExpenseTypes.includes(expenseTypeID)) {
            selectedExpenseTypes.push(expenseTypeID);
        }
    } else {
        const index = selectedExpenseTypes.indexOf(expenseTypeID);
        if (index !== -1) {
            selectedExpenseTypes.splice(index, 1);
        }
    }
    const allChecked = $('.row-checkbox:visible').length > 0 &&
        $('.row-checkbox:visible:not(:checked)').length === 0;
    $('#selectAll').prop('checked', allChecked);
});

$(document).ready(function () {
    $('#shipmentFormContainer').load('/ShipmentMode/ShipmentForm', function () {
        getNextExpenseTypeId(); 
    });
    $('#shipmentList').load('/ShipmentMode/ShipmentList', function () {
        loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection);

    });

    $(document).on("submit", "#shipmentForm", function (e) {
        e.preventDefault();
        saveOrUpdateShipmentMode();
    });

    // Handle pagination button clicks
    $(document).on('click', '#firstPage', function () {
        loadExpenseTypes(1, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    });

    $(document).on('click', '#prevPage', function () {
        if (currentPage > 1)
            loadExpenseTypes(currentPage - 1, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    });

    $(document).on('click', '#nextPage', function () {
        if (currentPage < totalPages)
            loadExpenseTypes(currentPage + 1, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    });

    $(document).on('click', '#lastPage', function () {
        loadExpenseTypes(totalPages, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    });

    $(document).on('change', '#pageSize', function () {
        pageSize = parseInt($(this).val());
        currentPage = 1;
        loadExpenseTypes(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    });

    $(document).on('click', 'resetBtn', function (e) {
        console.log("Clear button called");
        clearShipmentForm();
    })
  
    $(document).on("click", ".deleteBtn", function (e) {
        e.preventDefault();
        console.log("Delete button Called");
        deleteSelectedShipmentModes(); 
    });

    $(document).on("click", ".resetBtn", function (e) {
        console.log("Clear button Called");
        clearShipmentForm(); 
    });

    $(document).on('input', '#searchInput', debounce(function () {
        currentSearchTerm = $(this).val().trim();
        loadExpenseTypes(1, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
    }, 500));

});
