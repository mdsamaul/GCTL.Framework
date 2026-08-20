let selectedSubControlLedger = [];
let currenSubControltPage = 1;
let SubControlpageSize = 10;
let SubControlcurrentSortColumn = 'GeneralLedgerCodeNo';
let SubControlcurrentSortDirection = 'desc';
let isEditModeSubControl = false;
console.log(`Sub-Control Page Global Called: ${selectedSubControlLedger}`);

let selectedGroupLedgerCodeForSubControl = "";
let selectedControlLedgerCodeForSubControl = "";



//#region Custom Event Listener For Group Ledger Dropdown Updated

window.addEventListener('groupLedgerUpdated', function (e) {
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    LoadDropdownGRLCRL();
    $('#groupcode').val('');
    $('#groupshortName').val('');
    $('#ConRLCode').val('');
    $('#CLShortName').val('');
    $('#GeneralLedgerCodeNo').val('');
    LoadSubControlLedgerAll();

});

//#endregion


//#region Group and Control Ledger Dropdown For Sub-Control Ledger UI
function LoadDropdownGRLCRL() {
    $.ajax({
        url: "/group-ledger-dropdown-forsubcontrol",
        type: "GET",
        success: function (response) {
            // Populate Group Ledger dropdown manually
            let $gldropdown = $("#groupName");
             $gldropdown.empty();
            $gldropdown.append('<option value=""></option>');

            $.each(response.data, function (index, item) {
                $gldropdown.append($('<option></option>').val(item.id).text(item.name));
            });

            // Group Ledger change event
            $gldropdown.off('change').on('change', function () {
                let selectedGroupCode = $(this).val();
                selectedGroupLedgerCodeForSubControl = selectedGroupCode;

                $(this).removeClass('border-error');

                // Reset fields if no group selected
                if (!selectedGroupCode) {
                    $('#groupcode').val('');
                    $('#groupshortName').val('');
                    $('#ConRLName').val('').trigger('change');
                    $('#ConRLCode').val('');
                    $('#CLShortName').val('');
                    $('#GeneralLedgerCodeNo').val('');
                    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", "", "");
                    return;
                }

                // Load group info
                $.ajax({
                    url: '/group-ledger-info-forsubcontrol',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#groupcode').val(data.controlLedgerCodeNo);
                        $('#groupshortName').val(data.shortName || '');
                    }
                });

                // Load Control Ledger under selected Group
                $.ajax({
                    url: '/control-ledger-dropdown',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (response) {
                        console.log("Control Ledger Dropdown data For Sub-Control===", response);

                        // Populate Control Ledger dropdown manually
                        let $crldropdown = $('#ConRLName');
                         $crldropdown.empty();
                        $crldropdown.append('<option value=""></option>');
                        $.each(response.data, function (index, item) {
                            $crldropdown.append($('<option></option>').val(item.id).text(item.name));
                        });

                        // Auto reload Sub-Control Ledger list based on selected group
                        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, "");
                    },
                    error: function () {
                        console.error("Failed to fetch Control Ledger list under group");
                    }
                });
            });

            // Control Ledger change event
            $("#ConRLName").off('change').on('change', function () {
                let selectedControlCode = $(this).val();
                selectedControlLedgerCodeForSubControl = selectedControlCode;

                $(this).removeClass('border-error');

                if (!selectedControlCode) {
                    $('#ConRLCode').val('');
                    $('#CLShortName').val('');
                    $('#GeneralLedgerCodeNo').val('');
                    // Reload list filtered only by group
                    let selectedGroupCode = $("#groupName").val();
                    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, "");
                    return;
                }

                // fetch control ledger details
                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode },
                    success: function (data) {
                        $('#ConRLCode').val(data.subControlLedgerCodeNo);
                        $('#CLShortName').val(data.shortName || '');

                        if (!isEditModeSubControl) {
                            $.ajax({
                                url: '/next-subcontrol-ledger-code',
                                type: 'GET',
                                data: { controlLedgercode: data.subControlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#GeneralLedgerCodeNo').val(nextCode);
                                }
                            });
                        }

                        // Reload Sub-Control Ledger list filtered by both group and control
                        let selectedGroupCode = $("#groupName").val();
                        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
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
function SubControlLedgerSaveOrUpdate() {
    console.log("Save Or Update Sub-Control Ledger Function Called");

    let autoID = $('#SubCRLAutoID').val();
    let SubControlLedgerData = {
        autoId: autoID || 0,
        SubControlLedgerCodeNo: $("#ConRLCode").val(),
        GeneralLedgerCodeNo: $("#GeneralLedgerCodeNo").val(),
        GeneralLedgerName: $("#GeneralLedgerName").val(),
        ShortName: $("#SubCRLshortName").val(),
        GRLCode: $("#groupName").val(),
    };

    //Group Ledger
    if (!SubControlLedgerData.GRLCode) {
        toastr.error("Please Select a Group Ledger.");
        $('#groupName').addClass('border-error');
        return false;
    } else {
        $('#groupName').removeClass('border-error');
    }

    //Control Ledger
    if (!SubControlLedgerData.SubControlLedgerCodeNo) {
        toastr.error("Please Select a Control Ledger.");
        $('#ConRLName').addClass('border-error');
        return false;
    } else {
        $('#ConRLName').removeClass('border-error');
    }

    //Sub-Control Ledger
    if (!SubControlLedgerData.GeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger.");
        $('#GeneralLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#GeneralLedgerName').removeClass('border-error');
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

                        //Custom Event For Update Control Ledger Dropdown in Control Ledger UI
                        const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
                        window.dispatchEvent(event);

                        SubControlLedgerFormClear();
                        selectedSubControlLedger = [];

                        //Load Sub-Control Modal
                        ModalLoadSubControlLedgerAll();
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
function SubControlLedgerFormClear() {
    isEditModeSubControl = false;

    $("#autoId").val("");

    $("#groupcode").val("");
    $("#groupshortName").val("");

    $("#ConRLCode").val("");
    $("#CLShortName").val("");

    $("#GeneralLedgerCodeNo").val("");
    $("#GeneralLedgerName").val("");
    $("#SubCRLshortName").val("");

    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#SubControldisplayModifyDate').text('');
    $('#SubControldisplayLDate').text('');

    $(".Subcontrol-checkbox").prop("checked", false);
    $("#SubselectAll").prop("checked", false);
    $('#SubsearchInput').val('');

    // Clear Group and Control Ledger dropdowns
    $('#groupName').val('').trigger('change');
    $('#ConRLName').val('').trigger('change');
    $('#groupName').removeClass('border-error');
    $('#ConRLName').removeClass('border-error');

    $('#SubControldisplayLDate').text(getBangladeshDateTime());

    selectedSubControlLedger = [];
    currenSubControltPage = 1;
    const emptySearch = '';
    const emptyControlCode = '';
    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, emptySearch, emptyControlCode);
    LoadDropdownGRLCRL();
}
//#endregion


//#region Load All Sub-Control Ledger Data
function LoadSubControlLedgerAll(page = 1, SubControlpageSize = 10, sortColumn = SubControlcurrentSortColumn, sortDirection = SubControlcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "") {

    $.ajax({
        url: '/subcontrol-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: SubControlpageSize,
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
                        <input type="checkbox" class="form-check-input form-check-input-sm Subcontrol-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer subcontrol-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.generalLedgerCodeNo}
                    </td>
                    <td>${item.generalLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#SubControltblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#SubControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#SubControlpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#SubControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            SubControlgeneratePageButtons(currenControltPage, totalPages);
            SubControlupdateCheckboxState();
            SubControlupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit

$(document).on('click', '.subcontrol-ledger', function () {
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    isEditModeSubControl = true;
    selectedSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            console.log("Sub-Control Ledger Edit Details======= : ", data);

            //Basic info
            $('#SubCRLAutoID').val(id);
            $('#groupcode').val(data.grlCode || '');
            $('#groupName').val(data.grlCode).trigger('change');
            $('#groupshortName').val(data.grlShortname || '');

            // Load Control Ledger dropdown for this group
            $.ajax({
                url: '/control-ledger-dropdown',
                type: 'GET',
                data: { groupcode: data.grlCode },
                success: function (controlResponse) {
                    // Populate Control Ledger dropdown manually
                    let $crldropdown = $('#ConRLName');
                    $crldropdown.empty();
                    $crldropdown.append('<option value=""></option>'); 
                    $.each(controlResponse.data, function (index, item) {
                        $crldropdown.append($('<option></option>').val(item.id).text(item.name));
                    });

                    // Now set selected control ledger value
                    $('#ConRLCode').val(data.subControlLedgerCodeNo || '');
                    $('#ConRLName').val(data.subControlLedgerCodeNo).trigger('change');
                    $('#CLShortName').val(data.crlShortname || '');

                    //Set remaining fields
                    $('#GeneralLedgerCodeNo').val(data.generalLedgerCodeNo || '');
                    $('#GeneralLedgerName').val(data.generalLedgerName || '');
                    $('#SubCRLshortName').val(data.shortName || '');
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
                        $('#SubControldisplayLDate').text(entryDate.toUpperCase());
                    } else {
                        $('#SubControldisplayLDate').text('');
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
                        $('#SubControldisplayModifyDate').text(updateDate.toUpperCase());
                    } else {
                        $('#SubControldisplayModifyDate').text('');
                    }

                    // Checkbox update
                    $('.Subcontrol-checkbox').prop('checked', false);
                    $("#SubselectAll").prop('checked', false);
                    $(`.Subcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);
                    SubControlupdateCheckboxState();
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

function SubControlLedgerDelete() {
    // Filter only valid IDs
    const ids = selectedSubControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
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
            SubControlLedgerFormClear();
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection);
            selectedSubControlLedger = [];
            $("#SubselectAll").prop("checked", false);

            //Load Sub-Control Modal
            ModalLoadSubControlLedgerAll();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Sort & Pagination Helpers 
function SubControlupdateSortIndicators() {
    $('.Subsortable').removeClass('sort-asc sort-desc');
    $('.Subsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Subsortable[data-column="${SubControlcurrentSortColumn}"]`);
    header.addClass(SubControlcurrentSortColumn === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(SubControlcurrentSortColumn === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function SubControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#SubControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadSubControlLedgerAll(page, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, $('#SubsearchInput').val().trim()));

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

function SubControlupdateCheckboxState() {
    $('.Subcontrol-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedSubControlLedger.includes(ID));
    });
    const allChecked = $('.Subcontrol-checkbox:visible').length > 0 &&
        $('.Subcontrol-checkbox:visible:not(:checked)').length === 0;
    $('#SubselectAll').prop('checked', allChecked);
}

function SubControldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function SubControlbindCheckboxHandlers() {
    $(document).off('click', '.Subcontrol-checkbox');
    $(document).on('click', '.Subcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedSubControlLedger.includes(id)) {
                selectedSubControlLedger.push(id);
            }
        } else {
            const index = selectedSubControlLedger.indexOf(id);
            if (index !== -1) {
                selectedSubControlLedger.splice(index, 1);
            }
        }

        const allChecked = $('.Subcontrol-checkbox:visible').length > 0 &&
            $('.Subcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#SubselectAll').prop('checked', allChecked);

        console.log("Selected Sub Control Ledger:", selectedSubControlLedger);
    });
}
//#endregion


//#region Select all checkbox

$(document).on('click', '#SubselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Subcontrol-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.Subcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedSubControlLedger.includes(id)) {
                selectedSubControlLedger.push(id);
            }
        });
    } else {
        $('.Subcontrol-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedSubControlLedger.indexOf(id);
            if (index !== -1) {
                selectedSubControlLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Sub Control Ledger:", selectedSubControlLedger);
});
//#endregion


//#region Ready Part

$(document).ready(function () {
    LoadDropdownGRLCRL();
    LoadSubControlLedgerAll();

    $('#SubControldisplayLDate').text(getBangladeshDateTime());


    //Handle Save Or Update Button Handle
    $(document).on("submit", "#subcontrolForm", function (e) {
        e.preventDefault();
        SubControlLedgerSaveOrUpdate();
    });

    // Handle Delete Button
    $(document).on("click", "#SubcontroldeleteBtn", function (e) {
        e.preventDefault();
        SubControlLedgerDelete();
    });

    // Handle Clear button
    $(document).on("click", ".SubcontrolresetBtn", function () {
        console.log("Clerar Button Clicked")
        SubControlLedgerFormClear();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".subfavBtn").on("click", function () {
        toggleFavorite();
    });

    //Searching Handle
    $(document).on('input', '.SubsearchInput', SubControldebounce(function () {
        const searchValue = $('.SubsearchInput').val();
        console.log("Sub Control Search Value: ", searchValue);
        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, searchValue, selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#SubControlfirstPage', function () {
        currenSubControltPage = 1;
        LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    //Next Page Button
    $(document).on('click', '#SubControlnextPage', function () {
        currenSubControltPage++;
        LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    //Previous Page Button
    $(document).on('click', '#SubControlprevPage', function () {
        if (currenSubControltPage > 1) {
            currenSubControltPage--;
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
        }
    });

    //Last Page Button
    $(document).on('click', '#SubControllastPage', function () {
        let lastPage = $('#SubControlpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenSubControltPage = lastPage;
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
        }
    });

    //Showing Page Size
    $(document).on('change', '.SubControlpageSize', function () {
        SubControlpageSize = parseInt($(this).val());
        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    // Sorting
    $(document).on('click', '.Subsortable', function () {
        const column = $(this).data('column');

        if (column === SubControlcurrentSortColumn) {
            SubControlcurrentSortDirection = (SubControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            SubControlcurrentSortColumn = column;
            SubControlcurrentSortDirection = 'desc';
        }

        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, $('#SubsearchInput').val().trim(), selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    SubControlbindCheckboxHandlers();
});

//#endregion