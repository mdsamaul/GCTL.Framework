let selectedModalSubControlLedger = [];
let ModalcurrenSubControltPage = 1;
let ModalSubControlpageSize = 10;
let ModalSubControlcurrentSortColumn = 'GeneralLedgerCodeNo';
let ModalSubControlcurrentSortDirection = 'desc';
let ModalisEditModeSubControl = false;
console.log(`Sub-Control Page Global Called: ${selectedModalSubControlLedger}`);




//#region Group and Control Ledger Dropdown For Sub-Control Ledger UI
function ModalLoadDropdownGRLCRL() {
    $.ajax({
        url: "/group-ledger-dropdown-forsubcontrol",
        type: "GET",
        success: function (response) {
            choiceManager.populateDropdown('ModalgroupName', response.data);

            let $gldropdown = $("#ModalgroupName");

            // Group Ledger change event
            $gldropdown.off('change').on('change', function () {
                let selectedGroupCode = $(this).val();

                // Reset fields if no group selected
                if (!selectedGroupCode) {
                    $('#Modalgroupcode').val('');
                    $('#ModalgroupshortName').val('');
                    choiceManager.clearChoice('ModalConRLName');
                    $('#ModalConRLCode').val('');
                    $('#ModalCLShortName').val('');
                    $('#ModalGeneralLedgerCodeNo').val('');
                    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", "", "");
                    return;
                }

                // Load group info
                $.ajax({
                    url: '/group-ledger-info-forsubcontrol',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#Modalgroupcode').val(data.controlLedgerCodeNo);
                        $('#ModalgroupshortName').val(data.shortName || '');
                    }
                });

                // Load Control Ledger under selected Group
                $.ajax({
                    url: '/control-ledger-dropdown',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (response) {
                        console.log("Control Ledger Dropdown data For Sub-Control===", response);

                        choiceManager.populateDropdown('ModalConRLName', response.data);

                        // Auto reload Sub-Control Ledger list based on selected group
                        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", selectedGroupCode, "");
                    },
                    error: function () {
                        console.error("Failed to fetch Control Ledger list under group");
                    }
                });
            });

            // Control Ledger change event
            $("#ModalConRLName").off('change').on('change', function () {
                let selectedControlCode = $(this).val();

                if (!selectedControlCode) {
                    $('#ModalConRLCode').val('');
                    $('#ModalCLShortName').val('');
                    $('#ModalGeneralLedgerCodeNo').val('');
                    // Reload list filtered only by group
                    let selectedGroupCode = $("#ModalgroupName").val();
                    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", selectedGroupCode, "");
                    return;
                }

                // fetch control ledger details
                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode },
                    success: function (data) {
                        $('#ModalConRLCode').val(data.subControlLedgerCodeNo);
                        $('#ModalCLShortName').val(data.shortName || '');

                        if (!ModalisEditModeSubControl) {
                            $.ajax({
                                url: '/next-subcontrol-ledger-code',
                                type: 'GET',
                                data: { controlLedgercode: data.subControlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#ModalGeneralLedgerCodeNo').val(nextCode);
                                }
                            });
                        }

                        // Reload Sub-Control Ledger list filtered by both group and control
                        let selectedGroupCode = $("#ModalgroupName").val();
                        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", selectedGroupCode, selectedControlCode);
                    },
                    error: function () {
                        console.error("Failed to fetch Control Ledger info");
                    }
                });
            });
        },
        error: function (xhr, status, error) {
            console.error("Failed to load Group Ledger dropdown:", error);
        }
    });
}

//#endregion


//#region Save Or Update Sub Control Ledger
function ModalSubControlLedgerSaveOrUpdate() {
    console.log("Save Or Update Sub-Control Ledger Function Called");

    let autoID = $('#ModalSubCRLAutoID').val();
    let SubControlLedgerData = {
        autoId: autoID || 0,
        SubControlLedgerCodeNo: $("#ModalConRLCode").val(),
        GeneralLedgerCodeNo: $("#ModalGeneralLedgerCodeNo").val(),
        GeneralLedgerName: $("#ModalGeneralLedgerName").val(),
        ShortName: $("#ModalSubCRLshortName").val(),
        GRLCode: $("#ModalgroupName").val(),

    };

    //validation
    if (!SubControlLedgerData.GRLCode) {
        toastr.error("Please Select a Group Ledger.");
        let choicesWrapper = document.querySelector('#ModalgroupName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubControlLedgerData.SubControlLedgerCodeNo) {
        toastr.error("Please Select a Control Ledger.");
        let choicesWrapper = document.querySelector('#ModalConRLName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubControlLedgerData.GeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger.");
        $('#ModalGeneralLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#ModalGeneralLedgerName').removeClass('border-error');

    }


    //Check for duplicate
    $.ajax({
        url: '/subcontrol-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(SubControlLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/subcontrol-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/subcontrol-ledger/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubControlLedgerData),
                success: function (response) {
                    if (response.success) {
                        console.log("Sub-Control Ledger Save or UPdate Data : ", response);
                        toastr.success(successMessage);

                        ModalSubControlLedgerFormClear();
                        selectedModalSubControlLedger = [];

                        //Load Sub-Control Table
                        LoadSubControlLedgerAll();
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Sub-Control Ledger :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Sub-Control Ledger :', error);
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


//#region Sub Control Ledger Clear
function ModalSubControlLedgerFormClear() {
    ModalisEditModeSubControl = false;

    $("#ModalautoId").val("");

    $("#Modalgroupcode").val("");
    $("#ModalgroupshortName").val("");

    $("#ModalConRLCode").val("");
    $("#ModalCLShortName").val("");

    $("#ModalGeneralLedgerCodeNo").val("");
    $("#ModalGeneralLedgerName").val("");
    $("#ModalSubCRLshortName").val("");

    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubControldisplayLDate').text('');
    $('#ModalSubControldisplayModifyDate').text('');

    $(".ModalSubcontrol-checkbox").prop("checked", false);
    $("#ModalSubselectAll").prop("checked", false);
    $('#ModalSubsearchInput').val('');

    //Entry Date 
    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    choiceManager.clearChoice('ModalConRLName');
    choiceManager.clearChoice('ModalgroupName');

    document.querySelectorAll('.choiceDD').forEach(function (dropdown) {
        let choicesWrapper = dropdown.closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.remove('border-error');
        }
    });

    selectedModalSubControlLedger = [];
    currenSubControltPage = 1;
    const emptySearch = '';
    const emptyControlCode = '';
    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, emptySearch, emptyControlCode);
}
//#endregion


//#region Load All Sub-Control Ledger Data
function ModalLoadSubControlLedgerAll(page = 1, ModalSubControlpageSize = 10, sortColumn = ModalSubControlcurrentSortColumn, sortDirection = ModalSubControlcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "") {

    $.ajax({
        url: '/subcontrol-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: ModalSubControlpageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            controlLedgerCode: controlLedgerCode,
            groupLedgerCode: groupLedgerCode
        },
        success: function (response) {
            console.log("Sub-Control Ledger Data :", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm ModalSubcontrol-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer Modalsubcontrol-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.generalLedgerCodeNo}
                    </td>
                    <td>${item.generalLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#ModalSubControltblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#ModalSubControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#ModalSubControlpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#ModalSubControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            ModalSubControlgeneratePageButtons(currenControltPage, totalPages);
            ModalSubControlupdateCheckboxState();
            ModalSubControlupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit

$(document).on('click', '.Modalsubcontrol-ledger', function () {
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    ModalisEditModeSubControl = true;
    selectedModalSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            console.log("Sub-Control Ledger Edit Details======= : ", data);

            // Basic info
            $('#ModalSubCRLAutoID').val(id);
            $('#Modalgroupcode').val(data.grlCode || '');
            choiceManager.setChoiceValue('ModalgroupName', data.grlCode);
            $('#ModalgroupshortName').val(data.grlShortname || '');

            // Load Control Ledger dropdown for this group
            $.ajax({
                url: '/control-ledger-dropdown',
                type: 'GET',
                data: { groupcode: data.grlCode },
                success: function (controlResponse) {
                    //choiceManager.populateDropdown('ConRLName', controlResponse.data);

                    // Now set selected control ledger value
                    $('#ModalConRLCode').val(data.subControlLedgerCodeNo || '');
                    choiceManager.setChoiceValue('ModalConRLName', data.subControlLedgerCodeNo);
                    $('#ModalCLShortName').val(data.crlShortname || '');

                    //Set remaining fields
                    $('#ModalGeneralLedgerCodeNo').val(data.generalLedgerCodeNo || '');
                    $('#ModalGeneralLedgerName').val(data.generalLedgerName || '');
                    $('#ModalSubCRLshortName').val(data.shortName || '');

                    //Display Date
                    if (data.lDate) {
                        let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: 'numeric',
                            minute: '2-digit',
                            hour12: true
                        }).replace(',', '');
                        $('#ModalSubControldisplayLDate').text(entryDate.toUpperCase());
                    } else {
                        $('#ModalSubControldisplayLDate').text('');
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
                        $('#ModalSubControldisplayModifyDate').text(updateDate.toUpperCase());
                    } else {
                        $('#ModalSubControldisplayModifyDate').text('');
                    }

                    // Checkbox update
                    $('.ModalSubcontrol-checkbox').prop('checked', false);
                    $("#ModalSubselectAll").prop('checked', false);
                    $(`.ModalSubcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);
                    ModalSubControlupdateCheckboxState();
                },
                error: function () {
                    console.error("Failed to load Control Ledger dropdown in edit mode");
                }
            });
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Sub-Control Ledger Details.");
            console.error("Error Fetching Sub-Control Details:", error);
        }
    });
});


//#endregion


//#region Single & More Delete Sub-Control Ledger

function ModalSubControlLedgerDelete() {
    // Filter only valid IDs
    const ids = selectedModalSubControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    // Single URL for both single & multiple delete
    const url = '/subcontrol-ledger-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
            window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ModalSubControlLedgerFormClear();
            ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
            selectedModalSubControlLedger = [];
            $("#ModalSubselectAll").prop("checked", false);

            //Load Sub-Control Table
            LoadSubControlLedgerAll();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Sort & Pagination Helpers 
function ModalSubControlupdateSortIndicators() {
    $('.ModalSubsortable').removeClass('sort-asc sort-desc');
    $('.ModalSubsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.ModalSubsortable[data-column="${ModalSubControlcurrentSortColumn}"]`);
    header.addClass(ModalSubControlcurrentSortColumn === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalSubControlcurrentSortColumn === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalSubControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalSubControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalLoadSubControlLedgerAll(page, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, $('#ModalSubsearchInput').val().trim()));

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

function ModalSubControlupdateCheckboxState() {
    $('.ModalSubcontrol-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedModalSubControlLedger.includes(ID));
    });
    const allChecked = $('.ModalSubcontrol-checkbox:visible').length > 0 &&
        $('.ModalSubcontrol-checkbox:visible:not(:checked)').length === 0;
    $('#ModalSubselectAll').prop('checked', allChecked);
}

function ModalSubControldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ModalSubControlbindCheckboxHandlers() {
    $(document).off('click', '.ModalSubcontrol-checkbox');
    $(document).on('click', '.ModalSubcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalSubControlLedger.includes(id)) {
                selectedModalSubControlLedger.push(id);
            }
        } else {
            const index = selectedModalSubControlLedger.indexOf(id);
            if (index !== -1) {
                selectedModalSubControlLedger.splice(index, 1);
            }
        }

        const allChecked = $('.ModalSubcontrol-checkbox:visible').length > 0 &&
            $('.ModalSubcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#ModalSubselectAll').prop('checked', allChecked);

        console.log("Selected Sub Control Ledger:", selectedModalSubControlLedger);
    });
}
//#endregion


//#region Select all checkbox

$(document).on('click', '#ModalSubselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.ModalSubcontrol-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.ModalSubcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedModalSubControlLedger.includes(id)) {
                selectedModalSubControlLedger.push(id);
            }
        });
    } else {
        $('.ModalSubcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedModalSubControlLedger.indexOf(id);
            if (index !== -1) {
                selectedModalSubControlLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Sub Control Ledger:", selectedModalSubControlLedger);
});
//#endregion



//#region Ready Part

$(document).ready(function () {
    ModalLoadDropdownGRLCRL();

    ModalLoadSubControlLedgerAll();

    //Entry Date 
    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("submit", "#ModalsubcontrolForm", function (e) {
        e.preventDefault();
        ModalSubControlLedgerSaveOrUpdate();
    });

    // Handle Delete Button
    $(document).on("click", "#ModalSubcontroldeleteBtn", function (e) {
        e.preventDefault();
        ModalSubControlLedgerDelete();
    });

    // Handle Clear button
    $(document).on("click", ".ModalSubcontrolresetBtn", function () {
        console.log("Clerar Button Clicked")
        ModalSubControlLedgerFormClear();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".ModalsubfavBtn").on("click", function () {
        toggleFavorite();
    });

    //Searching Handle
    $(document).on('input', '.ModalSubsearchInput', ModalSubControldebounce(function () {
        const searchValue = $('.ModalSubsearchInput').val();
        console.log("Sub Control Search Value: ", searchValue);
        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, searchValue);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#ModalSubControlfirstPage', function () {
        currenSubControltPage = 1;
        LoadSubControlLedgerAll(currenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
    });

    //Last Page Button
    $(document).on('click', '#ModalSubControlnextPage', function () {
        currenSubControltPage++;
        ModalLoadSubControlLedgerAll(currenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
    });

    //Previous Page Button
    $(document).on('click', '#ModalSubControlprevPage', function () {
        if (currenSubControltPage > 1) {
            currenSubControltPage--;
            ModalLoadSubControlLedgerAll(currenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
        }
    });

    //Last Page Button
    $(document).on('click', '#ModalSubControllastPage', function () {
        let lastPage = $('#ModalSubControlpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenSubControltPage = lastPage;
            LoadSubControlLedgerAll(currenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
        }
    });

    //Showing Page Size
    $(document).on('change', '.ModalSubControlpageSize', function () {
        SubControlpageSize = parseInt($(this).val());
        LoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.ModalSubsortable', function () {
        const column = $(this).data('column');

        if (column === ModalSubControlcurrentSortColumn) {
            ModalSubControlcurrentSortDirection = (ModalSubControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalSubControlcurrentSortColumn = column;
            ModalSubControlcurrentSortDirection = 'desc';
        }

        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, $('#ModalSubsearchInput').val().trim());
    });

    ModalSubControlbindCheckboxHandlers();
});

//#endregion
