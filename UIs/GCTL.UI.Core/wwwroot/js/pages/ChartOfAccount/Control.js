let selectedControlLedger = [];
let currenControltPage = 1;
let ControlpageSize = 10;
let ControlcurrentSortColumn = 'SubControlLedgerCodeNo';
let ControlcurrentSortDirection = 'desc';
let isEditMode = false; // Edit mode flag
console.log(`Control Page Global Called: ${selectedControlLedger}`);


//#region  When Use Select value form dropdown then remove error border(Common Js for All Dropdown)

document.addEventListener('change', function (e) {
    if (e.target && e.target.matches(' #GLName')) {
        $(e.target).removeClass('border-error');
    }
});

//#endregion


//#region Custom Event Listener For Group Ledger Dropdown Updated

window.addEventListener('groupLedgerUpdated', function (e) {
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    LoadGroupLedgerDropdown();
    $('#GroupLedgerCodeNo').val('');
    $('#GroupLedgerShortName').val('');
    $('#SubControlLedgerCodeNo').val('');
    LoadAllControlLedger();

});

//#endregion


//#region Group Ledger Dropdown For Control Ledger UI

function LoadGroupLedgerDropdown(selectedId = null) {

    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            console.log("Group Ledger Dropdown Data", response);
            // Populate dropdown manually
            
            let $dropdown = $('#GLName');

            $.each(response.data, function (index, item) {
                $dropdown.append($('<option></option>').val(item.id).text(item.name));
            });

            // Auto select new/updated Ledger
            if (selectedId) {
                $dropdown.val(selectedId).trigger('change');
            }

            // On change, fetch full ledger info and update inputs
            $("#GLName").off('change').on('change', function () {
                let selectedCode = $(this).val();
                console.log("Selected Group Code :", selectedCode);

                $(this).removeClass('border-error');

                if (!selectedCode) {
                    $('#GroupLedgerCodeNo').val('');
                    $('#GroupLedgerShortName').val('');
                    $('#SubControlLedgerCodeNo').val('');
                    LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", "");
                    return;
                }

                // Fetch full ledger info
                $.ajax({
                    url: '/group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedCode },
                    success: function (data) {
                        $('#GroupLedgerCodeNo').val(data.controlLedgerCodeNo);
                        $('#GroupLedgerShortName').val(data.shortName || '');

                        // Only fetch next code if not editing
                        if (!isEditMode) {
                            $.ajax({
                                url: '/next-control-ledger-code',
                                type: 'GET',
                                data: { groupledgercode: data.controlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#SubControlLedgerCodeNo').val(nextCode);
                                }
                            });
                        }

                        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", data.controlLedgerCodeNo);
                    },
                    error: function () {
                        console.error("Failed to fetch Group Ledger info For Control Ledger UI");
                    }
                });

            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown For Control Ledger UI ");
        }
    });
}

//#endregion


//#region Save Or Update Control Ledger
function SaveOrUpdateControlLedger() {
    console.log("Save Or Update Control Ledger Function Called");

    let autoID = $('#autoId').val();
    let ControlLedgerData = {
        autoId: autoID || 0,
        ControlLedgerCodeNo: $("#GroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#SubControlLedgerCodeNo").val(),
        SubControlLedgerName: $("#SubControlLedgerName").val(),
        ShortName: $("#ControlShortName").val(),
        GroupLedgerName: $("#GLName").val(),
    };

    //validation
    if (!ControlLedgerData.GroupLedgerName) {
        toastr.error("Please Select a Group Ledger.");
        $('#GLName').addClass('border-error');
        return false;
    } else {
        $('#GLName').removeClass('border-error');
    }



    if (!ControlLedgerData.SubControlLedgerName) {
        toastr.error("Please Enter Control Ledger.");
        $('#SubControlLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#SubControlLedgerName').removeClass('border-error');
    }


    //Check for duplicate
    $.ajax({
        url: '/control-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ControlLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/control-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/control-ledger/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(ControlLedgerData),
                success: function (response) {
                    if (response.success) {
                        console.log("Control Ledger Save or UPdate Data : ", response);
                        toastr.success(successMessage);

                        //Custom Event For Update Control Ledger Dropdown in Control Ledger UI
                        //const event = new CustomEvent('controlLedgerUpdated', { detail: { newSubControlLedgerCodeNo: response.subControlLedgerCodeNo } });
                        //window.dispatchEvent(event);                      

                        ClearControlLedgerForm();
                        selectedControlLedger = [];

                        //Control Modal Table Load
                        ModalLoadAllControlLedger();
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Control Ledger :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Control Ledger :', error);
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


//#region Control Ledger Clear
function ClearControlLedgerForm() {
    isEditMode = false; // Reset edit mode

    $("#autoId").val("");
    $("#GroupLedgerCodeNo").val("");
    $('#GLName').val('').trigger('change');
    $("#GroupLedgerShortName").val("");
    $("#SubControlLedgerCodeNo").val("");
    $("#SubControlLedgerName").val("");
    $("#ControlShortName").val("");
    $(".form-control").removeClass("border-error");
    $('#GLName').removeClass('border-error');
    // $(".choiceDD").removeClass('border-error');
    $(".invalid-feedback, .valid-feedback").remove();
    $('#ControlEntryDate').text('');
    $('#ControlUpdateDate').text('');
    $(".control-checkbox").prop("checked", false);
    $("#ControlselectAll").prop("checked", false);
    $('#ControlsearchInput').val('');

    // Set Entry Date
    $('#ControlEntryDate').text(getBangladeshDateTime());

    selectedControlLedger = [];
    currenControltPage = 1;
    const emptySearch = '';
    const emptyGroupCode = '';
    LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, emptySearch, emptyGroupCode);
}
//#endregion


//#region Load All Control Ledger Data
function LoadAllControlLedger(page = 1, ControlpageSize = 10, sortColumn = ControlcurrentSortColumn, sortDirection = ControlcurrentSortDirection, searchTerm = "", groupCode = "") {

    $.ajax({
        url: '/control-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: ControlpageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupCode
        },
        success: function (response) {
            console.log("Control Ledger Data :", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm control-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer control-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.subControlLedgerCodeNo}
                    </td>
                    <td>${item.subControlLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#controlTblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#ControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#ControlpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#ControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            ControlgeneratePageButtons(currenControltPage, totalPages);
            ControlupdateCheckboxState();
            ControlupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit

$(document).on('click', '.control-ledger', function () {
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    isEditMode = true;
    // Reset selectedControlLedger to only this record
    selectedControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            $('#autoId').val(id);
            $('#GroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            $('#GLName').val(data.controlLedgerCodeNo).trigger('change');
            $('#GroupLedgerShortName').val(data.groupLedgerShortName || '');
            $('#SubControlLedgerCodeNo').val(data.subControlLedgerCodeNo || '');
            $('#SubControlLedgerName').val(data.subControlLedgerName || '');
            $('#ControlShortName').val(data.shortName || '');

            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#ControlEntryDate').text(entryDate.toUpperCase());
            } else {
                $('#ControlEntryDate').text('');
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
                $('#ControlUpdateDate').text(updateDate.toUpperCase());
            } else {
                $('#ControlUpdateDate').text('');
            }

            // Uncheck all row checkboxes to prevent conflict
            $('.control-checkbox').prop('checked', false);
            $("#ControlselectAll").prop('checked', false);

            $(`.control-checkbox[data-autoid="${id}"]`).prop('checked', true);

            ControlupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Control Ledger

function DeleteControlLedger() {
    // Filter only valid IDs
    const ids = selectedControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    // Single URL for both single & multiple delete
    const url = '/control-ledger-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('controlLedgerUpdated', { detail: { newSubControlLedgerCodeNo: response.subControlLedgerCodeNo } });
            window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ClearControlLedgerForm();
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
            selectedControlLedger = [];
            $("#ControlselectAll").prop("checked", false);

            //Control Modal Table Load
            ModalLoadAllControlLedger();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Select all checkbox
$(document).on('click', '#ControlselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.control-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.control-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedControlLedger.includes(id)) {
                selectedControlLedger.push(id);
            }
        });
    } else {
        $('.control-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedControlLedger.indexOf(id);
            if (index !== -1) {
                selectedControlLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Control Ledger:", selectedControlLedger);
});
//#endregion


//#region Sort & Pagination Helpers 
function ControlupdateSortIndicators() {
    $('.Controlsortable').removeClass('sort-asc sort-desc');
    $('.Controlsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Controlsortable[data-column="${ControlcurrentSortColumn}"]`);
    header.addClass(ControlcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ControlcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadAllControlLedger(
                page,
                ControlpageSize,
                ControlcurrentSortColumn,
                ControlcurrentSortDirection,
                $('#ControlsearchInput').val().trim()
            ));

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

    // Show current page if it's not first or last
    if (currentPage > 1 && currentPage < totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    // Always show last page if more than one
    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}



function ControlupdateCheckboxState() {
    $('.control-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedControlLedger.includes(ID));
    });
    const allChecked = $('.control-checkbox:visible').length > 0 &&
        $('.control-checkbox:visible:not(:checked)').length === 0;
    $('#ControlselectAll').prop('checked', allChecked);
}

function Controldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ControlbindCheckboxHandlers() {
    $(document).off('click', '.control-checkbox');
    $(document).on('click', '.control-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedControlLedger.includes(id)) {
                selectedControlLedger.push(id);
            }
        } else {
            const index = selectedControlLedger.indexOf(id);
            if (index !== -1) {
                selectedControlLedger.splice(index, 1);
            }
        }

        const allChecked = $('.control-checkbox:visible').length > 0 &&
            $('.control-checkbox:visible:not(:checked)').length === 0;
        $('#ControlselectAll').prop('checked', allChecked);

        console.log("Selected Control Ledger:", selectedControlLedger);
    });
}
//#endregion


//#region Ready Part

$(document).ready(function () {

    LoadGroupLedgerDropdown();
    LoadAllControlLedger();


    // Set Entry Date
    $('#ControlEntryDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button 
    $(document).on("submit", "#controlForm", function (e) {
        e.preventDefault();
        SaveOrUpdateControlLedger();
    });

    // Handle Delete Button
    $(document).on("click", "#ControldeleteBtn", function (e) {
        e.preventDefault();
        DeleteControlLedger();
    });

    // Handle Clear button
    $(document).on("click", ".ControlresetBtn", function () {
        console.log("Clerar Button Clicked")
        ClearControlLedgerForm();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".confavBtn").on("click", function () {
        toggleFavorite();
    });

    //Searching Handle
    $(document).on('input', '.ControlsearchInput', Controldebounce(function () {
        const searchValue = $('.ControlsearchInput').val();
        console.log("Control Search Value: ", searchValue);
        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, searchValue);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#ControlfirstPage', function () {
        currenControltPage = 1;
        LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
    });

    //Last Page Button
    $(document).on('click', '#ControlnextPage', function () {
        currenControltPage++;
        LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
    });

    //Previous Page Button
    $(document).on('click', '#ControlprevPage', function () {
        if (currenControltPage > 1) {
            currenControltPage--;
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
        }
    });

    //Last Page Button
    $(document).on('click', '#ControllastPage', function () {
        let lastPage = $('#ControlpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenControltPage = lastPage;
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
        }
    });

    //Showing Page Size
    $(document).on('change', '.ControlListpageSize', function () {
        ControlpageSize = parseInt($(this).val());
        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.Controlsortable', function () {
        const column = $(this).data('column');

        if (column === ControlcurrentSortColumn) {
            ControlcurrentSortDirection = (ControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ControlcurrentSortColumn = column;
            ControlcurrentSortDirection = 'desc';
        }

        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, $('#ControlsearchInput').val().trim());
    });

    ControlbindCheckboxHandlers();
});

//#endregion