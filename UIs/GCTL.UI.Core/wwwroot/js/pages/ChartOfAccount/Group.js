let selectedGroupLedger = [];
let currentPage = 1;
let pageSize = 10;
let currentSortColumn = 'ControlLedgerCodeNo';
let currentSortDirection = 'desc';
console.log(`Global Call: ${selectedGroupLedger}`);

//#region Validation Error Border(Common input handler)

$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('border-error');
    }
});

//#endregion


//#region Group Ledger Next Code
function LoadNextGroupLedgerCode() {
    $.ajax({
        url: '/next-group-ledger-code',
        method: 'GET',
        success: function (response) {
            console.log('Next Group Ledger Code Is :', response);
            $('#ControlLedgerCodeNo').val(response);

        },
        error: function (xhr, status, error) {
            console.error("Failed To Load Next Group Ledger Code: ", error);
        }
    });
}
//#endregion


//#region Save Or Update Group Ledger
function SaveOrUpdateGroupLedger() {
    console.log("Save Or Update Ledger Function Called");

    let autoID = $('#autoId').val();
    let GroupLedgerData = {
        autoId: autoID || 0,
        ControlLedgerCodeNo: $("#ControlLedgerCodeNo").val(),
        ControlLedgerName: $("#ControlLedgerName").val(),
        ShortName: $("#ShortName").val()
    };

    //validation
    if (!GroupLedgerData.ControlLedgerName) {
        toastr.error("Please Enter Group Ledger.");
        $('#ControlLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#ControlLedgerName').removeClass('border-error');
    }


    //Check for duplicate
    $.ajax({
        url: '/group-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(GroupLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/group-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/group-ledger/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(GroupLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        //Custom Event For Update Group Ledger Dropdown in Control Ledger UI
                        const event = new CustomEvent('groupLedgerUpdated', { detail: { newControlLedgerCodeNo: response.controlLedgerCodeNo } });
                        window.dispatchEvent(event); 

                        ClearGroupLedgerForm();
                        selectedGroupLedger = [];
                        LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection);

                        //Group Modal  Reload
                        ModalLoadGroupLedger(1, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
                        ModalLoadNextGroupLedgerCode();
                        ModalSubSidiaryForGeneralUI();
                        ModalLoadDropdownGRLCRL();
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Group Ledger :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Group Ledger :', error);
                }
            });
        },
        error: function (xhr, status, error) {
            toastr.error("Server error occurred during duplicate check!");
            console.error('Error checking duplicate:', error);
        }
    });
}
//#endregion


//#region Group Ledger Clear
function ClearGroupLedgerForm() {
    $("#autoId").val("");
    $("#ControlLedgerCodeNo").val("");
    $("#ControlLedgerName").val("");
    $("#ShortName").val("");
    $(".form-control").removeClass("is-invalid is-valid");
    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#UpdateDate').text(''); 
    $('#EntryDate').text('');
    $(".row-checkbox").prop("checked", false);
    $("#selectAll").prop("checked", false);
    $('#searchInput').val('');

    // Set Entry Date
    $('#EntryDate').text(getBangladeshDateTime());

    LoadNextGroupLedgerCode();
    selectedGroupLedger = [];
    currentPage = 1;
    const emptySearch = '';
    LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection, emptySearch);
}
//#endregion


//#region Load All Group Ledger Data
function LoadGroupLedger(page = 1, pageSize = 10, sortColumn = currentSortColumn, sortDirection = currentSortDirection, searchTerm = "") {

    $.ajax({
        url: '/group-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection
        },
        success: function (response) {
            console.log("Group Ledger Data:", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm row-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer group-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.controlLedgerCodeNo}
                    </td>
                    <td>${item.controlLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#groupTblBody').html(rows); 

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#paginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#paginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#totalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currentPage = page;
            generatePageButtons(currentPage, totalPages);
            updateCheckboxState();
            updateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit
$(document).on('click', '.group-ledger', function () {
    const id = $(this).data('autoid');
    console.log("Edited Group Ledger Code:", id);

    if (!id) return;

    $.ajax({
        url: `/group-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            // Populate form fields
            $('#autoId').val(id);
            $('#ControlLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            $('#ControlLedgerName').val(data.controlLedgerName || '');
            $('#ShortName').val(data.shortName || '');

            // Entry Date
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#EntryDate').text(entryDate.toUpperCase());
            } else {
                $('#EntryDate').text('');
            }

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#UpdateDate').text(updateDate.toUpperCase());
            } else {
                $('#UpdateDate').text('');
            }

            // Optionally clear previous selections
            selectedGroupLedger = [parseInt(id)];

            // Uncheck all row checkboxes to prevent conflict
            $('.row-checkbox').prop('checked', false);
            $("#selectAll").prop('checked', false);

            $(`.row-checkbox[data-autoid="${id}"]`).prop('checked', true);

            updateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Group Ledger details.");
            console.error("Error fetching details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Group Ledger
function DeleteGroupLedger() {
    // Filter only valid IDs
    const ids = selectedGroupLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("Group IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    //URL for both single & multiple delete
    const url = '/group-ledger-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            //Custom Event For Update Group Ledger Dropdown in Control Ledger UI
            const event = new CustomEvent('groupLedgerUpdated', { detail: { newControlLedgerCodeNo: response.controlLedgerCodeNo } });
            window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ClearGroupLedgerForm();
            LoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
            selectedGroupLedger = [];
            $("#selectAll").prop("checked", false);

            //Group Modal Table Reload
            ModalLoadGroupLedger(1, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
            ModalLoadNextGroupLedgerCode();
            ModalLoadDropdownGRLCRL();
            ModalSubSidiaryForGeneralUI();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion


//#region Select all checkbox
$(document).on('click', '#selectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.row-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.row-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedGroupLedger.includes(id)) {
                selectedGroupLedger.push(id);
            }
        });
    } else {
        $('.row-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedGroupLedger.indexOf(id);
            if (index !== -1) {
                selectedGroupLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Group Ledger:", selectedGroupLedger);
});
//#endregion


//#region Sort & Pagination Helpers
function updateSortIndicators() {
    $('.sortable').removeClass('sort-asc sort-desc');
    $('.sortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.sortable[data-column="${currentSortColumn}"]`);
    header.addClass(currentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(currentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function generatePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#pageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadGroupLedger(page, pageSize, currentSortColumn, currentSortDirection, $('#searchInput').val().trim()));

        if (page === currentPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    // Always show first page
    navigationDiv.append(createButton(1));

    if (currentPage > 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (currentPage > 1 && currentPage < totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

function updateCheckboxState() {
    $('.row-checkbox').each(function () {
        const bankID = $(this).data('autoid');
        $(this).prop('checked', selectedGroupLedger.includes(bankID));
    });
    const allChecked = $('.row-checkbox:visible').length > 0 &&
        $('.row-checkbox:visible:not(:checked)').length === 0;
    $('#selectAll').prop('checked', allChecked);
}

function debounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function bindCheckboxHandlers() {
    $(document).off('click', '.row-checkbox');
    $(document).on('click', '.row-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedGroupLedger.includes(id)) {
                selectedGroupLedger.push(id);
            }
        } else {
            const index = selectedGroupLedger.indexOf(id);
            if (index !== -1) {
                selectedGroupLedger.splice(index, 1);
            }
        }

        const allChecked = $('.row-checkbox:visible').length > 0 &&
            $('.row-checkbox:visible:not(:checked)').length === 0;
        $('#selectAll').prop('checked', allChecked);

        console.log("Selected Group Ledger:", selectedGroupLedger);
    });
}
//#endregion


// Function to get current date in Bangladesh time

function getBangladeshDateTime() {
    // BD timezone offset is +6:00 from UTC
    let now = new Date();
    let utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    let bdOffset = 6 * 60 * 60 * 1000;
    let bdTime = new Date(utc + bdOffset);

    // Format DD/MM/YYYY hh:MM AM/PM
    let day = bdTime.getDate().toString().padStart(2, '0');
    let month = (bdTime.getMonth() + 1).toString().padStart(2, '0');
    let year = bdTime.getFullYear();

    let hours = bdTime.getHours();
    let minutes = bdTime.getMinutes().toString().padStart(2, '0');

    let ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // 0 => 12

    return `${day}/${month}/${year} ${hours}:${minutes} ${ampm}`;
}




//#region Ready Part

$(document).ready(function () {
    console.log("Group Ledger Page Loaded");
     LoadNextGroupLedgerCode();
     LoadGroupLedger();


    // Set Entry Date
    $('#EntryDate').text(getBangladeshDateTime());


    //Handle Save Or Update Button Handle
    $(document).on("submit", "#groupForm", function (e) {
          e.preventDefault();
          SaveOrUpdateGroupLedger();
     });

    // Handle Delete Button
    $(document).on("click", ".deleteBtn", function (e) {
         e.preventDefault();
        DeleteGroupLedger();
    }); 

     // Handle Clear button
    $(document).on("click", ".resetBtn", function () {
        console.log("Clerar Button Clicked")
        ClearGroupLedgerForm();
    });

        // Handle print button
    $(".printBtn").on("click", function (e) {
          e.preventDefault();
          printForm();
    });

        // Handle favorite button
    $(".favBtn").on("click", function () {
         toggleFavorite();
    });

    //Pagination Handle
        $(document).on('input', '.searchInput', debounce(function () {
            const searchValue = $('.searchInput').val();
            LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection, searchValue);
        }, 500));

        $(document).on('click', '#firstPage', function () {
            currentPage = 1;
            LoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
        });

        $(document).on('click', '#prevPage', function () {
            console.log("previous page called on bank page ");
            if (currentPage > 1) {
                currentPage--;
                LoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
            }
        });

        $(document).on('click', '#nextPage', function () {
            currentPage++;
            LoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
        });

        $(document).on('click', '#lastPage', function () {
            let lastPage = $('#pageNavigation button').last().text();
            lastPage = parseInt(lastPage);
            if (!isNaN(lastPage)) {
                currentPage = lastPage;
                LoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
            }
        });

        $(document).on('change', '.pageSize', function () {
            pageSize = parseInt($(this).val());
            LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection);
        });

        $(document).on('click', '.sortable', function () {
            const column = $(this).data('column');

            if (column === currentSortColumn) {
                currentSortDirection = (currentSortDirection === 'desc') ? 'asc' : 'desc';
            } else {
                currentSortColumn = column;
                currentSortDirection = 'desc';
            }

            LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection, $('#searchInput').val().trim());
        });

        bindCheckboxHandlers();
});

//#endregion
