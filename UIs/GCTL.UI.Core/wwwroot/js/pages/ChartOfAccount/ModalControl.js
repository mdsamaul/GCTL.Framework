let selectedModalControlLedger = [];
let ModalcurrenControltPage = 1;
let ModalControlpageSize = 10;
let ModalControlcurrentSortColumn = 'SubControlLedgerCodeNo';
let ModalControlcurrentSortDirection = 'desc';
let ModalisEditMode = false; // Edit mode flag
console.log(`Control Page Global Called: ${selectedModalControlLedger}`);

//#region Custom Event Listener For Group Ledger Dropdown Updated

window.addEventListener('groupLedgerUpdated', function (e) {
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    ModalLoadGroupLedgerDropdown();
    $('#ModalGroupLedgerCodeNo').val('');
    $('#ModalGroupLedgerShortName').val('');
    $('#ModalSubControlLedgerCodeNo').val('');
    ModalLoadAllControlLedger();

});

//#endregion


//#region Group Ledger Dropdown For Control Ledger UI
function ModalLoadGroupLedgerDropdown() {

    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            // Populate dropdown using choiceManager
            choiceManager.populateDropdown('ModalGLName', response.data);

            // On change, fetch full ledger info and update inputs
            $("#ModalGLName").off('change').on('change', function () {
                let selectedCode = $(this).val();
                console.log("Selected Group Code :", selectedCode);

                if (!selectedCode) {
                    $('#ModalGroupLedgerCodeNo').val('');
                    $('#ModalGroupLedgerShortName').val('');
                    $('#ModalSubControlLedgerCodeNo').val('');
                    ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, "", "");
                    return;
                }

                // Fetch full ledger info
                $.ajax({
                    url: '/group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedCode },
                    success: function (data) {
                        $('#ModalGroupLedgerCodeNo').val(data.controlLedgerCodeNo);
                        $('#ModalGroupLedgerShortName').val(data.shortName || '');

                        // Only fetch next code if not editing
                        if (!ModalisEditMode) {
                            $.ajax({
                                url: '/next-control-ledger-code',
                                type: 'GET',
                                data: { groupledgercode: data.controlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#ModalSubControlLedgerCodeNo').val(nextCode);
                                }
                            });
                        }

                        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, "", data.controlLedgerCodeNo);
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
function ModalSaveOrUpdateControlLedger() {
    console.log("Save Or Update Control Ledger Function Called");

    let autoID = $('#ModalautoId').val();
    let ControlLedgerData = {
        autoId: autoID || 0,
        ControlLedgerCodeNo: $("#ModalGroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#ModalSubControlLedgerCodeNo").val(),
        SubControlLedgerName: $("#ModalSubControlLedgerName").val(),
        ShortName: $("#ModalControlShortName").val(),
        GroupLedgerName: $("#ModalGLName").val(),
    };

    //validation
    if (!ControlLedgerData.GroupLedgerName) {
        toastr.error("Please Select a Group Ledger.");
        let choicesWrapper = document.querySelector('#ModalGLName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!ControlLedgerData.SubControlLedgerName) {
        toastr.error("Please Enter Control Ledger.");
        $('#ModalSubControlLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#ModalSubControlLedgerName').removeClass('border-error');
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

                        ModalClearControlLedgerForm();
                        selectedModalControlLedger = [];

                        //Load Control Table
                        LoadAllControlLedger();
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
function ModalClearControlLedgerForm() {
    ModalisEditMode = false; 

    $("#ModalautoId").val("");
    $("#ModalGroupLedgerCodeNo").val("");
    $("#ModalGroupLedgerShortName").val("");
    $("#ModalSubControlLedgerCodeNo").val("");
    $("#ModalSubControlLedgerName").val("");
    $("#ModalControlShortName").val("");
    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#ModalControldisplayModifyDate').text('');
    $('#ModalControldisplayLDate').text('');
    $(".Modalcontrol-checkbox").prop("checked", false);
    $("#ModalControlselectAll").prop("checked", false);
    $('#ModalControlsearchInput').val('');

    choiceManager.clearChoice('ModalGLName');

    document.querySelectorAll('.choiceDD').forEach(function (dropdown) {
        let choicesWrapper = dropdown.closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.remove('border-error');
        }
    });
    // Set Entry Date
    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    selectedModalControlLedger = [];
    ModalcurrenControltPage = 1;
    const emptySearch = '';
    const emptyGroupCode = '';
    ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, emptySearch, emptyGroupCode);
}
//#endregion


//#region Load All Control Ledger Data
function ModalLoadAllControlLedger(page = 1, ModalControlpageSize = 10, sortColumn = ModalControlcurrentSortColumn, sortDirection = ModalControlcurrentSortDirection, searchTerm = "", groupCode = "") {

    $.ajax({
        url: '/control-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: ModalControlpageSize,
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
                        <input type="checkbox" class="form-check-input form-check-input-sm Modalcontrol-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer Modalcontrol-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.subControlLedgerCodeNo}
                    </td>
                    <td>${item.subControlLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#ModalcontrolTblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#ModalControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#ModalControlpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#ModalControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            ModalControlgeneratePageButtons(currenControltPage, totalPages);
            ModalControlupdateCheckboxState();
            ModalControlupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit

$(document).on('click', '.Modalcontrol-ledger', function () {
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    ModalisEditMode = true;
    // Reset selectedControlLedger to only this record
    selectedModalControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            $('#ModalautoId').val(id);
            $('#ModalGroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            choiceManager.setChoiceValue('ModalGLName', data.controlLedgerCodeNo);
            $('#ModalGroupLedgerShortName').val(data.groupLedgerShortName || '');
            $('#ModalSubControlLedgerCodeNo').val(data.subControlLedgerCodeNo || '');
            $('#ModalSubControlLedgerName').val(data.subControlLedgerName || '');
            $('#ModalControlShortName').val(data.shortName || '');

            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#ModalControldisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#ModalControldisplayLDate').text('');
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
                $('#ModalControldisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#ModalControldisplayModifyDate').text('');
            }

            // Uncheck all row checkboxes to prevent conflict
            $('.Modalcontrol-checkbox').prop('checked', false);
            $("#ModalControlselectAll").prop('checked', false);

            $(`.Modalcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);

            ModalControlupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Control Ledger

function ModalDeleteControlLedger() {
    // Filter only valid IDs
    const ids = selectedModalControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
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

            //const event = new CustomEvent('controlLedgerUpdated', { detail: { newSubControlLedgerCodeNo: response.subControlLedgerCodeNo } });
            //window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ModalClearControlLedgerForm();
            ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
            selectedModalControlLedger = [];
            $("#ModalControlselectAll").prop("checked", false);

            //Load Control Table
            LoadAllControlLedger();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Select all checkbox
$(document).on('click', '#ModalControlselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Modalcontrol-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.Modalcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedModalControlLedger.includes(id)) {
                selectedModalControlLedger.push(id);
            }
        });
    } else {
        $('.Modalcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedModalControlLedger.indexOf(id);
            if (index !== -1) {
                selectedModalControlLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Control Ledger:", selectedModalControlLedger);
});
//#endregion


//#region Sort & Pagination Helpers 
function ModalControlupdateSortIndicators() {
    $('.ModalControlsortable').removeClass('sort-asc sort-desc');
    $('.ModalControlsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.ModalControlsortable[data-column="${ModalControlcurrentSortColumn}"]`);
    header.addClass(ModalControlcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalControlcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalLoadAllControlLedger(
                page,
                ModalControlpageSize,
                ModalControlcurrentSortColumn,
                ModalControlcurrentSortDirection,
                $('#ModalControlsearchInput').val().trim()
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

function ModalControlupdateCheckboxState() {
    $('.Modalcontrol-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedModalControlLedger.includes(ID));
    });
    const allChecked = $('.Modalcontrol-checkbox:visible').length > 0 &&
        $('.Modalcontrol-checkbox:visible:not(:checked)').length === 0;
    $('#ModalControlselectAll').prop('checked', allChecked);
}

function ModalControldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ModalControlbindCheckboxHandlers() {
    $(document).off('click', '.Modalcontrol-checkbox');
    $(document).on('click', '.Modalcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalControlLedger.includes(id)) {
                selectedModalControlLedger.push(id);
            }
        } else {
            const index = selectedModalControlLedger.indexOf(id);
            if (index !== -1) {
                selectedModalControlLedger.splice(index, 1);
            }
        }

        const allChecked = $('.Modalcontrol-checkbox:visible').length > 0 &&
            $('.Modalcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#ModalControlselectAll').prop('checked', allChecked);

        console.log("Selected Control Ledger:", selectedModalControlLedger);
    });
}
//#endregion


//#region Ready Part

$(document).ready(function () {

    ModalLoadGroupLedgerDropdown();
    ModalLoadAllControlLedger();

    // Set Entry Date
    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button 
    $(document).on("submit", "#ModalcontrolForm", function (e) {
        e.preventDefault();
        ModalSaveOrUpdateControlLedger();
    });

    // Handle Delete Button
    $(document).on("click", "#ModalControldeleteBtn", function (e) {
        e.preventDefault();
        ModalDeleteControlLedger();
    });

    // Handle Clear button
    $(document).on("click", ".ModalControlresetBtn", function () {
        console.log("Clerar Button Clicked")
        ModalClearControlLedgerForm();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".ModalconfavBtn").on("click", function () {
        toggleFavorite();
    });

    //Searching Handle
    $(document).on('input', '.ModalControlsearchInput', ModalControldebounce(function () {
        const searchValue = $('.ModalControlsearchInput').val();
        console.log("Control Search Value: ", searchValue);
        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, searchValue);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#ModalControlfirstPage', function () {
        currenControltPage = 1;
        ModalLoadAllControlLedger(currenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

    //Last Page Button
    $(document).on('click', '#ModalControlnextPage', function () {
        currenControltPage++;
        ModalLoadAllControlLedger(currenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

    //Previous Page Button
    $(document).on('click', '#ModalControlprevPage', function () {
        if (currenControltPage > 1) {
            currenControltPage--;
            ModalLoadAllControlLedger(currenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
        }
    });

    //Last Page Button
    $(document).on('click', '#ModalControllastPage', function () {
        let lastPage = $('#ModalControlpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenControltPage = lastPage;
            ModalLoadAllControlLedger(currenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
        }
    });

    //Showing Page Size
    $(document).on('change', '.ModalControlListpageSize', function () {
        ModalControlpageSize = parseInt($(this).val());
        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.ModalControlsortable', function () {
        const column = $(this).data('column');

        if (column === ModalControlcurrentSortColumn) {
            ModalControlcurrentSortDirection = (ModalControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalControlcurrentSortColumn = column;
            ModalControlcurrentSortDirection = 'desc';
        }

        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, $('#ModalControlsearchInput').val().trim());
    });

    ModalControlbindCheckboxHandlers();
});

//#endregion

