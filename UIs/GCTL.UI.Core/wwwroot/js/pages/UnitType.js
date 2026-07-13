let selectedUnits = []; 
console.log("unit page");
console.log(`Global Call: ${selectedUnits}`);

// Show decimal format
function truncateNumber(number, decimalPlaces) {
    let factor = Math.pow(10, decimalPlaces);
    return Math.floor(number * factor) / factor;
}
function showDecimalFormat(decimalPlaceValue) {
    let number = 1.123456789; 
    let decimalLimit = Math.min(decimalPlaceValue, 5); 

    let truncatedNumber = truncateNumber(number, decimalLimit);

    let formattedNumber = truncatedNumber.toFixed(decimalLimit);

    $('#showFormat').val(formattedNumber);
    $('#negativeFormat').val("-" + formattedNumber);
}

// Attach input event
$(document).on('input', '#decimalPlaces', function () {
    let decimalValue = parseInt($(this).val());

    if (isNaN(decimalValue)) {
        $('#showFormat').val("");
        $('#negativeFormat').val("");
        return;
    }

    showDecimalFormat(decimalValue);
});

//unitIDs Getting
function loadNextUnitTypeId() {
    $.ajax({
        url: '/next-unit-id',
        method: 'GET',
        success: function (response) {
            $('#unitID').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed to load next unit ID:", error);
        }
    });
}

//Details population
$(document).on('click', '.btn-transfer', function () {
    const data = {
        tc: $(this).data('tcidbtn'),  
        unitTypeID: $(this).data('unitid'),
        unitTypeName: $(this).data('unitname'),
        shortName: $(this).data('shortname'),
        decimalPlaces: $(this).data('decimalplaces'),
        lDate: $(this).data('ldate'),
        modifyDate: $(this).data('modifydate')
    };
    console.log("Populated Data .......:", data);
    populateUnitForm(data);
    showDecimalFormat(parseInt(data.decimalPlaces || 0));
    selectedUnits = [];
    populateUnitIDs(data.tc); 
});

// Select all checkbox
$(document).on('click', '#selectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.row-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.row-checkbox:visible').each(function () {
            const id = parseInt($(this).data('tcidcheck'));
            if (!selectedUnits.includes(id)) {
                selectedUnits.push(id);
            }
        });
    } else {
        $('.row-checkbox:visible').each(function () {
            const id = parseInt($(this).data('tcidcheck'));
            const index = selectedUnits.indexOf(id);
            if (index !== -1) {
                selectedUnits.splice(index, 1);
            }
        });
    }
    console.log("Selected units:", selectedUnits);
});

// Save Units
function saveUnitType() {
    console.log("Save UnitType called");

    let unitName = $("#unitName").val().trim();
    if (!unitName) {
        $("#unitName").addClass("is-invalid");
        toastr.error("Unit Type Name is required.");
        return;
    } else {
        $("#unitName").removeClass("is-invalid");
    }

    let tc = $('#tcID').val();
    let url = '/unit';
    let method = 'POST';
    let successMessage = 'Data Saved Successfully';

    if (tc) {
        url = `/unit/${tc}`;
        method = 'PUT';
        successMessage = 'Data Updated Successfully';
    }

    // Get form data
    let unitData = {
        TC: tc ? parseInt(tc) : 0,
        unitTypID: $("#unitID").val(),
        unitTypeName: $("#unitName").val().trim(),
        decimalPlaces: parseInt($("#decimalPlaces").val()),
        shortName: $("#unitShortName").val().trim()
    };

    // Step 1: Duplicate check
    $.ajax({
        url: '/UnitType/CheckDuplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(unitData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message || "Duplicate data found.");
                return; // Stop here
            }

            // Step 2: Save data (no duplicate found)
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(unitData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);
                        selectedUnits = [];
                        clearForm();
                        currentPage = 1; // reset to first page after save
                        loadUnits(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
                    } else {
                        toastr.error(response.message || "Failed to save unit type!");
                    }
                },

                error: function (xhr) {
                    let msg = "An error occurred while saving unit type.";
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        msg = xhr.responseJSON.message;
                    }
                    toastr.error(msg);
                }
            });
        },
        error: function (xhr) {
            toastr.error("An error occurred while checking duplicates.");
        }
    });
}

// Clear form fields and reset states
function clearForm() {
    $("#tcID").val("");
    $("#unitID").val("");
    $("#unitName").val("");
    $("#decimalPlaces").val("");
    $("#showFormat").val("");
    $("#unitShortName").val("");
    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#modifyDateContainer').hide();
    $('#lDateContainer').hide();
    $("#searchInput").val('');
    currentSearchTerm = "";  
    $("#selectAll").prop('checked', false);
    selectedUnits = [];
    loadNextUnitTypeId();

    console.log("Form cleared");
}

let currentPage = 1;
let pageSize = 10;
let totalPages = 1;
let currentSortColumn = 'UnitTypID';
let currentSortDirection = 'desc';
let currentSearchTerm = "";  

function loadUnits(page = 1, pageSizeParam = pageSize, sortColumn = currentSortColumn, sortDirection = currentSortDirection, searchTerm = currentSearchTerm, filterID = null) {
    currentPage = page;

    let requestData = {
        page: page,
        pageSize: pageSizeParam,
        searchTerm: searchTerm,
        currentSortColumn: sortColumn,
        sortOrder: sortDirection
    };

    if (filterID !== null) {
        requestData.filterID = filterID;
    }

    $.ajax({
        url: '/unit-list',
        type: 'GET',
        data: requestData,
        success: function (response) {
            console.log('Unit type Table', response);

            if (response.data && response.data.length > 0) {
                let rows = '';

                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center">
                            <input type="checkbox" class="form-check-input form-check-input-sm row-checkbox" style="width: 12px; height: 12px;" data-tcidcheck="${item.tc}">
                        </td>
                        <td>
                            <button class="btn btn-transfer"
                                data-tcidbtn="${item.tc}"
                                data-unitid="${item.unitTypID}"
                                data-unitname="${item.unitTypeName}"
                                data-shortname="${item.shortName}"
                                data-decimalplaces="${item.decimalPlaces}"
                                data-ldate="${item.lDate}"
                                data-modifydate="${item.modifyDate}">
                                ${item.unitTypID}
                            </button>
                        </td>
                        <td>${item.unitTypeName || ''}</td>
                        <td>${item.shortName || ''}</td>
                        <td>${item.decimalPlaces || ''}</td>
                    </tr>`;
                });

                $('#tblBody').html(rows);
                bindCheckboxHandlers(); 

                // paginationInfo from response
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

            }
            else {
                $('#tblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">No Data Available</td></tr>`);
                $('#paginationSummary').text('');
                $('#paginationInfo').text('');
                $('#totalRecordsInfo').text('');
                $('#pageNavigation').html('');
                $('#selectAll').prop('checked', false);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading data: ", error);
            $('#tblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">Failed to load data.</td></tr>`);
            $('#paginationSummary').text('');
            $('#paginationInfo').text('');
            $('#totalRecordsInfo').text('');
            $('#pageNavigation').html('');
            $('#selectAll').prop('checked', false);
        }
    });
}

// Update sort indicators in the table header
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
function generatePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#pageNavigation');
    navigationDiv.empty();

    const createButton = (page, text = null) => {
        const button = $('<button>')
            .addClass('btn mx-1 btn-sm')
            .text(text || page)
            .click(function () {
                loadUnits(page, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
            });

        if (page === currentPage) {
            button.removeClass('btn-secondary').addClass('btn-primary');
        } else {
            button.addClass('btn-secondary');
        }

        return button;
    };

    if (totalPages <= 1) {
        navigationDiv.append(createButton(1));
        return;
    }

    // First page
    navigationDiv.append(createButton(1));

    // Ellipsis before middle page
    if (currentPage > 2) {
        navigationDiv.append($('<span>').text('...').addClass('mx-1'));
    }

    // Show middle page if it's not first or last
    if (currentPage !== 1 && currentPage !== totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    // Ellipsis after middle page
    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').addClass('mx-1'));
    }

    // Last page
    navigationDiv.append(createButton(totalPages));
}

// Update checkbox state based on selectedUnits
function updateCheckboxState() {
    $('.row-checkbox').each(function () {
        const unitID = parseInt($(this).data('tcidcheck'));
        $(this).prop('checked', selectedUnits.includes(unitID));
    });

    const allChecked = $('.row-checkbox:visible').length > 0 &&
        $('.row-checkbox:visible:not(:checked)').length === 0;
    $('#selectAll').prop('checked', allChecked);
}

// Debounce function
function debounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function populateUnitIDs(IDs) {
    let IDvalue = parseInt(IDs, 10);
    if (!selectedUnits.includes(IDvalue)) {
        selectedUnits.push(IDvalue);
    }
    console.log(`IDvalue: ${IDvalue}`);
    console.log(`Selected units now: ${selectedUnits}`);
}

function populateUnitForm(data) {
    console.log("From Unit Form Population Function, data are:", data);
    $('#unitForm input[name="tcID"]').val(data.tc);
    $('#unitForm input[name="unitID"]').val(data.unitTypeID);
    $('#unitForm input[name="unitName"]').val(data.unitTypeName);
    $('#unitForm input[name="unitShortName"]').val(data.shortName);
    $('#unitForm input[name="decimalPlaces"]').val(data.decimalPlaces);

    // Handle Last Date
    if (data.lDate) {
        const formattedLDate = new Date(data.lDate).toLocaleDateString('en-GB');
        $('#displayLDate').text(formattedLDate);
        $('#lDateContainer').show();
    } else {
        $('#lDateContainer').hide();
    }

    // Handle Modify Date
    if (data.modifyDate) {
        const formattedModifyDate = new Date(data.modifyDate).toLocaleDateString('en-GB');
        $('#displayModifyDate').text(formattedModifyDate);
        $('#modifyDateContainer').show();
    } else {
        $('#modifyDateContainer').hide();
    }
}

// Checkbox click handler
function bindCheckboxHandlers() {
    $(document).off('click', '.row-checkbox'); // Unbind previous handlers
    $(document).on('click', '.row-checkbox', function () {
        const id = parseInt($(this).data('tcidcheck'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedUnits.includes(id)) {
                selectedUnits.push(id);
            }
        } else {
            const index = selectedUnits.indexOf(id);
            if (index !== -1) {
                selectedUnits.splice(index, 1);
            }
        }

        const allChecked = $('.row-checkbox:visible').length > 0 &&
            $('.row-checkbox:visible:not(:checked)').length === 0;
        $('#selectAll').prop('checked', allChecked);

        console.log("Selected units:", selectedUnits);
    });
}

function deleteUnitList() {
    let idsToDelete = [...selectedUnits];
    let ids = JSON.stringify(idsToDelete);
    console.log(`IDs to be deleted: ${ids}`);
    $.ajax({
        url: '/unit-list',
        type: 'DELETE',
        data: ids,
        contentType: 'application/json',
        success: function (response) {
            clearForm();
            currentPage = 1; // reset page after deletion
            loadUnits(currentPage);
            selectedUnits = [];
            if (response.isSuccess) {
                toastr.success("Data Deleted successfully!");
            } else {
                toastr.error(response.message || "Delete failed!");
                console.error('Delete error:', response.message);
            }
            console.log(`Call from delete method, selectedUnits cleared: ${selectedUnits}`);
        },
        error: function (error) {
            let errorMessage = "An error occurred while deleting unit!";
            if (error.responseJSON && error.responseJSON.message) {
                errorMessage = error.responseJSON.message;
            }
            console.error('Error deleting Units:', error);
            toastr.error(errorMessage); 
        }
    });
}

$(document).ready(function () {
    // Delete button click
    $(document).on("click", ".deleteBtn", function (e) {
        e.preventDefault();
        deleteUnitList();
    });

    // Clear button
    $(document).on("click", ".resetBtn", function () {
        console.log("Clear Button Clicked");
        clearForm();
        currentPage = 1;
        loadUnits(currentPage);
    });

    // Print button 
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        // printForm(); 
    });

    // Favorite button 
    $(".favBtn").on("click", function () {
         //toggleFavorite(); 
    });

    loadNextUnitTypeId();

    // Form submit
    $(document).on("submit", "#unitForm", function (e) {
        e.preventDefault();
        saveUnitType();
    });

    loadUnits(currentPage);

    // Search input with debounce
    $(document).on('input', '.searchInput', debounce(function () {
        currentSearchTerm = $('.searchInput').val();
        currentPage = 1;
        loadUnits(currentPage);
    }, 500));

    // Pagination controls
    $(document).on('click', '#firstPage', function () {
        console.log("First Page Called")
        if (currentPage > 1) {
            currentPage = 1;
            loadUnits(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
        }
    });

    $(document).on('click', '#prevPage', function () {
        console.log("Previous Page Called")
        if (currentPage > 1) {
            currentPage--;
            loadUnits(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
        }
    });

    $(document).on('click', '#nextPage', function () {
        console.log("Next Page Called")
        if (currentPage < totalPages) {
            currentPage++;
            loadUnits(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
        }
        console.log("Next clicked, currentPage:", currentPage, "totalPages:", totalPages);

    });

    $(document).on('click', '#lastPage', function () {
        console.log("Last Page Called")
        if (currentPage < totalPages) {
            currentPage = totalPages;
            loadUnits(currentPage, pageSize, currentSortColumn, currentSortDirection, currentSearchTerm);
        }
    });

    // Page size change
    $(document).on('change', '.pageSize', function () {
        pageSize = parseInt($(this).val());
        currentPage = 1;
        loadUnits(currentPage);
    });

    // Sortable columns click handler
    $(document).on('click', '.sortable', function () {
        const column = $(this).data('column');

        if (column === currentSortColumn) {
            currentSortDirection = (currentSortDirection === 'asc') ? 'desc' : 'asc';
        } else {
            currentSortColumn = column;
            currentSortDirection = 'asc';
        }

        currentPage = 1;
        loadUnits(currentPage);
    });

    // Initial binding of checkbox handlers
    bindCheckboxHandlers();
  
});
