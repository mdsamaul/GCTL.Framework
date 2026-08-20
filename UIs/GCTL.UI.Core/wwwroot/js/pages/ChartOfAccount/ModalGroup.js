let selectedModalGroupLedger = [];
let ModalcurrentPage = 1;
let ModalpageSize = 10;
let ModalcurrentSortColumn = 'ControlLedgerCodeNo';
let ModalcurrentSortDirection = 'desc';
console.log(`Global Call: ${selectedModalGroupLedger}`);




//#region Modal Group Ledger Next Code
function ModalLoadNextGroupLedgerCode() {
    $.ajax({
        url: '/next-group-ledger-code',
        method: 'GET',
        success: function (response) {
            console.log('Next Group Ledger Code Is :', response);
            $('#ModalControlLedgerCodeNo').val(response);

        },
        error: function (xhr, status, error) {
            console.error("Failed To Load Next Group Ledger Code For Group Modal: ", error);
        }
    });
}
//#endregion


//#region Save Or Update Modal Group Ledger
function ModalSaveOrUpdateGroupLedger() {
    console.log("Save Or Update Ledger Function Called");

    let autoID = $('#ModalautoId').val();
    let GroupLedgerData = {
        autoId: autoID || 0,
        ControlLedgerCodeNo: $("#ModalControlLedgerCodeNo").val(),
        ControlLedgerName: $("#ModalControlLedgerName").val(),
        ShortName: $("#ModalShortName").val()
    };

    //validation
    if (!GroupLedgerData.ControlLedgerName) {
        toastr.error("Please Enter Group Ledger.");
        $('#ModalControlLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#ModalControlLedgerName').removeClass('border-error');
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
                        //const event = new CustomEvent('ModalgroupLedgerUpdated', {
                        //    detail: {
                        //        savedId: response.savedId,
                        //        savedName: response.savedName  
                        //    }
                        //});
                        //window.dispatchEvent(event);

                        // Active tab dropdown update + auto-select
                        updateGroupDropdown(response.savedId, response.savedName);
                        // All other tab dropdown update
                        updateAllGroupDropdowns(response.savedId, response.savedName);


                        ModalClearGroupLedgerForm();
                        selectedModalGroupLedger = [];
                        ModalLoadGroupLedger(1, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
                        //Group Ledger
                        LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection);
                        LoadNextGroupLedgerCode();
                        LoadDropdownGRLCRL();
                        ForSubSidiGRLDropdown();
                        ModalLoadDropdownGRLCRL();
                        loadGeneralLedgerData();
                        ModalSubSidiaryForGeneralUI();
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


//#region Modal Group Ledger Clear
function ModalClearGroupLedgerForm() {
    $("#ModalautoId").val("");
    $("#ModalControlLedgerCodeNo").val("");
    $("#ModalControlLedgerName").val("");
    $("#ModalShortName").val("");
    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $(".form-control").removeClass("border-error");
    $('#ModaldisplayModifyDate').text('');
    $('#ModaldisplayLDate').text('');
    $(".Modalrow-checkbox").prop("checked", false);
    $("#ModalselectAll").prop("checked", false);
    $('#ModalsearchInput').val('');
    ModalLoadNextGroupLedgerCode();

    // Set Entry Date
    $('#ModaldisplayLDate').text(getBangladeshDateTime());

    selectedModalGroupLedger = [];
    ModalcurrentPage = 1;
    const ModalemptySearch = '';
    ModalLoadGroupLedger(1, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection, ModalemptySearch);
}
//#endregion


//#region Load All Modal Group Ledger Data
function ModalLoadGroupLedger(page = 1, ModalpageSize = 10, sortColumn = ModalcurrentSortColumn, sortDirection = ModalcurrentSortDirection, searchTerm = "") {

    $.ajax({
        url: '/group-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: ModalpageSize,
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
                        <input type="checkbox" class="form-check-input form-check-input-sm Modalrow-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer Modalgroup-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.controlLedgerCodeNo}
                    </td>
                    <td>${item.controlLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#ModalgroupTblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#ModalpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#ModalpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#ModaltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currentPage = page;
            ModalgeneratePageButtons(currentPage, totalPages);
            ModalupdateCheckboxState();
            ModalupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit
$(document).on('click', '.Modalgroup-ledger', function () {
    const id = $(this).data('autoid');
    console.log("Edited Group Ledger Code:", id);

    if (!id) return;

    $.ajax({
        url: `/group-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            // Populate form fields
            $('#ModalautoId').val(id);
            $('#ModalControlLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            $('#ModalControlLedgerName').val(data.controlLedgerName || '');
            $('#ModalShortName').val(data.shortName || '');

            // Display LDate 
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#ModaldisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#ModaldisplayLDate').text('');
            }
            // Display ModifyDate if available

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#ModaldisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#ModaldisplayModifyDate').text('');
            }

            // Optionally clear previous selections
            selectedModalGroupLedger = [parseInt(id)];

            // Uncheck all row checkboxes to prevent conflict
            $('.Modalrow-checkbox').prop('checked', false);
            $("#ModalselectAll").prop('checked', false);

            $(`.Modalrow-checkbox[data-autoid="${id}"]`).prop('checked', true);

            ModalupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Group Ledger details.");
            console.error("Error fetching details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Modal Group Ledger
function ModalDeleteGroupLedger() {
    // Filter only valid IDs
    const ids = selectedModalGroupLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
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
            //const event = new CustomEvent('groupLedgerUpdated', { detail: { newControlLedgerCodeNo: response.controlLedgerCodeNo } });
            //window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ModalClearGroupLedgerForm();
            ModalLoadGroupLedger(currentPage, pageSize, currentSortColumn, currentSortDirection);
            selectedModalGroupLedger = [];
            $("#ModalselectAll").prop("checked", false);

            ModalLoadGroupLedgerDropdown();
            LoadDropdownGRLCRL();
            ForSubSidiGRLDropdown();
            ModalLoadDropdownGRLCRL();
            loadGeneralLedgerData();
            ModalSubSidiaryForGeneralUI();
            LoadGroupLedgerDropdown();
            LoadGroupLedger(1, pageSize, currentSortColumn, currentSortDirection, "");
            ModalLoadNextGroupLedgerCode();
            LoadNextGroupLedgerCode();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion


//#region Select all checkbox
$(document).on('click', '#ModalselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Modalrow-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.Modalrow-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedModalGroupLedger.includes(id)) {
                selectedModalGroupLedger.push(id);
            }
        });
    } else {
        $('.Modalrow-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedModalGroupLedger.indexOf(id);
            if (index !== -1) {
                selectedModalGroupLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Group Ledger:", selectedModalGroupLedger);
});
//#endregion


//#region Sort & Pagination Helpers
function ModalupdateSortIndicators() {
    $('.Modalsortable').removeClass('sort-asc sort-desc');
    $('.Modalsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.sortable[data-column="${ModalcurrentSortColumn}"]`);
    header.addClass(ModalcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalLoadGroupLedger(page, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection, $('#ModalsearchInput').val().trim()));

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

function ModalupdateCheckboxState() {
    $('.Modalrow-checkbox').each(function () {
        const bankID = $(this).data('autoid');
        $(this).prop('checked', selectedModalGroupLedger.includes(bankID));
    });
    const allChecked = $('.Modalrow-checkbox:visible').length > 0 &&
        $('.Modalrow-checkbox:visible:not(:checked)').length === 0;
    $('#ModalselectAll').prop('checked', allChecked);
}

function Modaldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ModalbindCheckboxHandlers() {
    $(document).off('click', '.Modalrow-checkbox');
    $(document).on('click', '.Modalrow-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalGroupLedger.includes(id)) {
                selectedModalGroupLedger.push(id);
            }
        } else {
            const index = selectedModalGroupLedger.indexOf(id);
            if (index !== -1) {
                selectedModalGroupLedger.splice(index, 1);
            }
        }

        const allChecked = $('.Modalrow-checkbox:visible').length > 0 &&
            $('.Modalrow-checkbox:visible:not(:checked)').length === 0;
        $('#ModalselectAll').prop('checked', allChecked);

        console.log("Selected Group Ledger:", selectedModalGroupLedger);
    });
}
//#endregion


//#region Ready Part

$(document).ready(function () {
    console.log("Group Ledger Page Loaded");
    ModalLoadNextGroupLedgerCode();
    ModalLoadGroupLedger();

    // Set Entry Date
    $('#ModaldisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("submit", "#ModalgroupForm", function (e) {
        e.preventDefault();
        ModalSaveOrUpdateGroupLedger();
    });

    // Handle Delete Button
    $(document).on("click", ".ModaldeleteBtn", function (e) {
        e.preventDefault();
        ModalDeleteGroupLedger();
    });

    // Handle Clear button
    $(document).on("click", ".ModalresetBtn", function () {
        console.log("Clerar Button Clicked")
        ModalClearGroupLedgerForm();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".ModalfavBtn").on("click", function () {
        toggleFavorite();
    });

    //Pagination Handle
    $(document).on('input', '.ModalsearchInput', Modaldebounce(function () {
        const searchValue = $('.searchInput').val();
        ModalLoadGroupLedger(1, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection, searchValue);
    }, 500));

    $(document).on('click', '#ModalfirstPage', function () {
        currentPage = 1;
        ModalLoadGroupLedger(currentPage, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
    });

    $(document).on('click', '#ModalprevPage', function () {
        console.log("previous page called on bank page ");
        if (currentPage > 1) {
            currentPage--;
            ModalLoadGroupLedger(currentPage, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
        }
    });

    $(document).on('click', '#ModalnextPage', function () {
        currentPage++;
        ModalLoadGroupLedger(currentPage, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
    });

    $(document).on('click', '#ModallastPage', function () {
        let lastPage = $('#ModalpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currentPage = lastPage;
            ModalLoadGroupLedger(currentPage, ModalpageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
        }
    });

    $(document).on('change', '.ModalpageSize', function () {
        pageSize = parseInt($(this).val());
        ModalLoadGroupLedger(1, pageSize, ModalcurrentSortColumn, ModalcurrentSortDirection);
    });

    $(document).on('click', '.Modalsortable', function () {
        const column = $(this).data('column');

        if (column === ModalcurrentSortColumn) {
            ModalcurrentSortDirection = (ModalcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalcurrentSortColumn = column;
            ModalcurrentSortDirection = 'desc';
        }

        ModalLoadGroupLedger(1, pageSize, ModalcurrentSortColumn, ModalcurrentSortDirection, $('#ModalsearchInput').val().trim());
    });

    ModalbindCheckboxHandlers();
});

//#endregion
