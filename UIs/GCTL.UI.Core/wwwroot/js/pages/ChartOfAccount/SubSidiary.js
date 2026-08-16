let selectedSubSidiaryLedger = [];
let currenSubSidiarytPage = 1;
let SubSidiarypageSize = 10;
let SubSidiarycurrentSortColumn = 'SusidiaryLedgerCodeNo';
let SubSidiarycurrentSortDirection = 'desc';
console.log(`Sub-Sidiary Page Global Called: ${selectedSubSidiaryLedger}`);
let isEditModeSubSidiary = false; 
const $groupDropdown = $("#forsidiarygroupName");
const $controlDropdown = $("#forsidiaryControlName");
const $subControlDropdown = $("#forsidiaryGeneralLedgerName");
//const selectedGroupCode = $('#forsidiarygroupName').val() || "";
//const selectedControlCode = $('#forsidiaryControlName').val() || "";
//const selectedSubControlCode = $('#forsidiaryGeneralLedgerName').val() || "";
//Selected dropdown values (used for reload, pagination, sorting)
let selectedGroupCodeSSL = "";
let selectedControlCodeSSL = "";
let selectedSubControlCodeSSL = "";



//#region Custom Event Listener For Group Ledger Dropdown Updated

window.addEventListener('groupLedgerUpdated', function (e) {
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    ForSubSidiGRLDropdown();
    $("#forsidiarygroupcode").val("");
    $("#forsidiarygroupshortName").val("");

    $("#forsidiaryControlCode").val("");
    $("#forsidiaryCLShortName").val("");

    $("#forsidiaryGeneralLedgerCodeNo").val("");
    $("#forsidiarySubCRLshortName").val("");

    $("#SusidiaryLedgerCodeNo").val("");
    SubSididiarLedgerAll();

});

//#endregion


//#region Load All Sub-Sidiary Ledger Data
function SubSididiarLedgerAll(page = 1, SubSidiarypageSize = 10, sortColumn = SubSidiarycurrentSortColumn, sortDirection = SubSidiarycurrentSortDirection, searchTerm = "",  groupLedgerCode = "",  controlLedgerCode = "",  subcontrolLedgerCode = "") {

    $.ajax({
        url: '/subsidiary-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: SubSidiarypageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupLedgerCode,
            controlLedgerCode: controlLedgerCode,
            subcontrolLedgerCode: subcontrolLedgerCode
        },
        success: function (response) {
            console.log("Sub-Sidiary Ledger Data :", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm Subsidiary-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer subsidiary-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.susidiaryLedgerCodeNo}
                    </td>
                    <td>${item.subsidiaryLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#SubSidiarytblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#SubSidiarypaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#SubSidiarypaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#SubSidiarytotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            SubSidiarygeneratePageButtons(currenControltPage, totalPages);
            SubSidiaryupdateCheckboxState();
            SubSidiaryupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion

 
//#region Sort & Pagination Helpers 
function SubSidiaryupdateSortIndicators() {
    $('.SubSidiarysortable').removeClass('sort-asc sort-desc');
    $('.SubSidiarysortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.SubSidiarysortable[data-column="${SubSidiarycurrentSortColumn}"]`);
    header.addClass(SubSidiarycurrentSortColumn === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(SubSidiarycurrentSortColumn === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function SubSidiarygeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#SubSidiarypageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => SubSididiarLedgerAll(page, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, $('#SubSidiarysearchInput').val().trim(), selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL));

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

function SubSidiaryupdateCheckboxState() {
    $('.Subsidiary-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedSubSidiaryLedger.includes(ID));
    });
    const allChecked = $('.Subsidiary-checkbox:visible').length > 0 &&
        $('.Subsidiary-checkbox:visible:not(:checked)').length === 0;
    $('#SubSidiaryselectAll').prop('checked', allChecked);
}

function SubSidiarydebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function SubSidiarybindCheckboxHandlers() {
    $(document).off('click', '.Subsidiary-checkbox');
    $(document).on('click', '.Subsidiary-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedSubSidiaryLedger.includes(id)) {
                selectedSubSidiaryLedger.push(id);
            }
        } else {
            const index = selectedSubSidiaryLedger.indexOf(id);
            if (index !== -1) {
                selectedSubSidiaryLedger.splice(index, 1);
            }
        }

        const allChecked = $('.Subsidiary-checkbox:visible').length > 0 &&
            $('.Subsidiary-checkbox:visible:not(:checked)').length === 0;
        $('#SubSidiaryselectAll').prop('checked', allChecked);

        console.log("Selected Sub Sidiary Ledger:", selectedSubSidiaryLedger);
    });
}
//#endregion


//#region Select all checkbox

$(document).on('click', '#SubSidiaryselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Subsidiary-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.Subsidiary-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedSubSidiaryLedger.includes(id)) {
                selectedSubSidiaryLedger.push(id);
            }
        });
    } else {
        $('.Subsidiary-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedSubSidiaryLedger.indexOf(id);
            if (index !== -1) {
                selectedSubSidiaryLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Sub Control Ledger:", selectedSubSidiaryLedger);
});
//#endregion


//#region Save Or Update Sub-Sidiary Ledger
function SubSidiaryLedgerSaveOrUpdate() {
    console.log("Save Or Update Sub-Sidiary Ledger Function Called");

    let autoID = $('#forsidiaryAutoID').val();
    let SubSidiaryLedgerData = {
        autoId: autoID || 0,
        GeneralLedgerCodeNo: $("#forsidiaryGeneralLedgerCodeNo").val(),
        SusidiaryLedgerCodeNo: $("#SusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: $("#SubsidiaryLedgerName").val(),
        ShortName: $("#forsidiaryshortName").val(), 
        CRLCode: $("#forsidiaryControlName").val(),
        GLCode: $("#forsidiarygroupName").val(),
    };

    //validation
    if (!SubSidiaryLedgerData.GLCode) {
        toastr.error("Please Select a Group Ledger.");
        let choicesWrapper = document.querySelector('#forsidiarygroupName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.CRLCode) {
        toastr.error("Please Select a Control Ledger.");
        let choicesWrapper = document.querySelector('#forsidiaryControlName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.GeneralLedgerCodeNo) {
        toastr.error("Please Select a Sub-Control Ledger.");
        let choicesWrapper = document.querySelector('#forsidiaryGeneralLedgerName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger.");
        $('#SubsidiaryLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#SubsidiaryLedgerName').removeClass('border-error');
    }


    //Check for duplicate
    $.ajax({
        url: '/subsidiary-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(SubSidiaryLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/subsidiary-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/subsidiary-ledger/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubSidiaryLedgerData),
                success: function (response) {
                    if (response.success) {
                        console.log("Sub-Sidiary Ledger Save or UPdate Data : ", response);
                        toastr.success(successMessage);

                        //Custom Event For Update Control Ledger Dropdown in Control Ledger UI
                        const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
                        window.dispatchEvent(event);

                        SubSidiaryLedgerFormClear();
                        selectedSubSidiaryLedger = [];

                        //Load Sub-Sidiary Modal Table
                        ModalSubSididiarLedgerAll();
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Sub-Sidiary Ledger :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Sub-Sidiary Ledger :', error);
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


//#region Sub Sidiary Ledger Clear
function SubSidiaryLedgerFormClear() {

    isEditModeSubSidiary = false;

    $("#forsidiaryAutoID").val("");
    choiceManager.clearChoice('forsidiarygroupName');
    choiceManager.clearChoice('forsidiaryControlName');
    choiceManager.clearChoice('forsidiaryGeneralLedgerName');


    $("#forsidiarygroupcode").val("");
    $("#forsidiarygroupshortName").val("");

    $("#forsidiaryControlCode").val("");
    $("#forsidiaryCLShortName").val("");

    $("#forsidiaryGeneralLedgerCodeNo").val("");
    $("#forsidiarySubCRLshortName").val("");
    
    $("#SusidiaryLedgerCodeNo").val("");
    $("#SubsidiaryLedgerName").val("");
    $("#forsidiaryshortName").val("");

    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#SubSidiarydisplayModifyDate').text('');
    $('#SubSidiaryldisplayLDate').text('');

    $(".Subsidiary-checkbox").prop("checked", false);
    $("#SubSidiaryselectAll").prop("checked", false);
    $('#SubSidiarysearchInput').val('');


    document.querySelectorAll('.choiceDD').forEach(function (dropdown) {
        //choiceManager.clearChoice(dropdown.id);
        let choicesWrapper = dropdown.closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.remove('border-error');
        }
    });

    // Set Entry Date
    $('#SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    selectedSubSidiaryLedger = [];
    currenSubSidiarytPage = 1;
    const emptySearch = '';
    const emptyGroupCode = '';
    const emptyControlCode = '';
    const emptySubControlCode = '';
    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, emptySearch, emptyGroupCode, emptyControlCode, emptySubControlCode);
}
//#endregion


//#region Single & More Delete Sub-Sidiary Ledger

function SubSidiaryLedgerDelete() {
    // Filter only valid IDs
    const ids = selectedSubSidiaryLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    // Single URL for both single & multiple delete
    const url = '/subsidiary-ledger-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
            window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            SubSidiaryLedgerFormClear();
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection);
            selectedSubSidiaryLedger = [];
            $("#SubSidiaryselectAll").prop("checked", false);

            //Load Sub-Sidiary Modal Table
            ModalSubSididiarLedgerAll();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Load Group Ledger Dropdown and With Related Data 
function ForSubSidiGRLDropdown() {
    //Group Ledger Dropdown
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            choiceManager.populateDropdown('forsidiarygroupName', groupResponse.data);

            let isClearingGroupDropdown = false;
            $groupDropdown.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();

                if (isClearingGroupDropdown) return;

                if (!selectedGroupCode) {
                    isClearingGroupDropdown = true;

                    choiceManager.clearChoice('forsidiarygroupName');
                    $('#forsidiarygroupcode').val('');
                    $('#forsidiarygroupshortName').val('');

                    // Control Ledger clear
                    $controlDropdown.off('change');
                    choiceManager.clearChoice('forsidiaryControlName');
                    $('#forsidiaryControlCode').val('');
                    $('#forsidiaryCLShortName').val('');

                    // Sub-Control Ledger clear
                    $subControlDropdown.off('change');
                    choiceManager.clearChoice('forsidiaryGeneralLedgerName');
                    $('#forsidiaryGeneralLedgerCodeNo').val('');
                    $('#forsidiarySubCRLshortName').val('');
                    $('#SusidiaryLedgerCodeNo').val('');

                    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", "", "", "");
                    isClearingGroupDropdown = false;
                }



                // Load Group info
                if (selectedGroupCode) {
                    $.ajax({
                        url: '/forsubsidiary-group-ledger-info',
                        type: 'GET',
                        data: { groupcode: selectedGroupCode },
                        success: function (data) {
                            $('#forsidiarygroupcode').val(data.controlLedgerCodeNo);
                            $('#forsidiarygroupshortName').val(data.shortName || '');
                            SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCode, "", "");
                        }
                    });
                }

                // Load Control Ledger under selected Group
            });
        },
        error: function () {
            console.error("Failed to fetch Group Ledger dropdown");
        }
    });
}

//#endregion 


//#region Ready Part

$(document).ready(function () {

    ForSubSidiGRLDropdown();


    // Set Entry Date
    $('#SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $('#forsidiarygroupName').on('change', function (e) {
        e.preventDefault();
        selectedGroupCodeSSL = $(this).val() || "";
        getControlLedger(selectedGroupCodeSSL);

        //var id = $(this).val();
        //getControlLedger(id);
    })

    function getControlLedger(selectedGroupCode) {
        $.ajax({
            url: '/forSubsidiary-control-ledger-dropdown',
            type: 'GET',
            data: { groupcode: selectedGroupCode },
            success: function (controlResponse) {
                choiceManager.populateDropdown('forsidiaryControlName', controlResponse.data);

                // If in edit mode, set the value after populate
                if (isEditModeSubSidiary) {
                    const currentControlCode = $('#forsidiaryControlCode').val();
                    choiceManager.setChoiceValue('forsidiaryControlName', currentControlCode);
                }
                let isClearingControlDropdown = false;

                $controlDropdown.off('change').on('change', function () {
                    if (isClearingControlDropdown) return;

                    const selectedControlCode = $(this).val();

                    if (!selectedControlCode) {
                        isClearingControlDropdown = true;

                        choiceManager.clearChoice('forsidiaryControlName');
                        $('#forsidiaryControlCode').val('');
                        $('#forsidiaryCLShortName').val('');

                        choiceManager.clearChoice('forsidiaryGeneralLedgerName');
                        $('#forsidiaryGeneralLedgerCodeNo').val('');
                        $('#forsidiarySubCRLshortName').val('');
                        $('#SusidiaryLedgerCodeNo').val('');

                        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, "", "");

                        isClearingControlDropdown = false;
                        return;
                    }

                    // Load Control Ledger info
                    GetControlLedgerInfo(selectedControlCode)

                });
            }
        });
    }

    function GetControlLedgerInfo(selectedControlCode) {
        $.ajax({
            url: '/forSubsidiary-control-ledger-info',
            type: 'GET',
            data: { contrlcode: selectedControlCode },
            success: function (controlInfo) {
                $('#forsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo);
                $('#forsidiaryCLShortName').val(controlInfo.shortName || '');
                SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, "");
            }
        });
    }


    $('#forsidiaryControlName').on('change', function (e) {
        e.preventDefault();
        selectedControlCodeSSL = $(this).val() || "";
        getSubControllerLedger(selectedControlCodeSSL);


       // var id = $(this).val();
       //getSubControllerLedger(id);
    });

    $(document).on('change', '#forsidiaryGeneralLedgerName', function () {
        selectedSubControlCodeSSL = $(this).val() || "";
    });


    function getSubControllerLedger(selectedControlCode) {
                // Load Sub-Control Ledger dropdown under selected control
                $.ajax({
                    url: '/subcontrol-ledger-dropdown',
                    type: 'GET',
                    data: { controlcode: selectedControlCode },
                    success: function (subControlResponse) {
                        console.log("Sub Control Info Infor ====", subControlResponse);

                        choiceManager.populateDropdown('forsidiaryGeneralLedgerName', subControlResponse.data);

                        if (isEditModeSubSidiary) {
                            const currentSubControlCode = $('#forsidiaryGeneralLedgerCodeNo').val();
                            choiceManager.setChoiceValue('forsidiaryGeneralLedgerName', currentSubControlCode);
                        }

                        let isClearingSubControlDropdown = false;

                        $subControlDropdown.off('change').on('change', function () {
                            if (isClearingSubControlDropdown) return;

                            const selectedSubControlCode = $(this).val();

                            if (!selectedSubControlCode) {
                                //isClearingSubControlDropdown = true;
                                choiceManager.clearChoice('forsidiaryGeneralLedgerName');
                                //$('#forsidiaryGeneralLedgerName').destroy();
                                $('#forsidiaryGeneralLedgerCodeNo').val('');
                                $('#forsidiarySubCRLshortName').val('');
                                $('#SusidiaryLedgerCodeNo').val(''); 

                                SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, "");

                                //isClearingSubControlDropdown = false;
                                return;
                            }

                            // Fetch Sub-Control info
                            GetSubcontrolInfo(selectedSubControlCode);
                            
                        });
                    }
                });
     
    }

    function GetSubcontrolInfo(selectedSubControlCode) {
        $.ajax({
            url: '/forsudsidiary-subcontrol-ledger-info',
            type: 'GET',
            data: { subcontrlcode: selectedSubControlCode },
            success: function (subControlInfo) {
                $('#forsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo);
                $('#forsidiarySubCRLshortName').val(subControlInfo.shortName || '');

                if (!isEditModeSubSidiary) {
                    $.ajax({
                        url: '/next-subsidiary-ledger-code',
                        type: 'GET',
                        data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                        success: function (nextCode) {
                            $('#SusidiaryLedgerCodeNo').val(nextCode);
                        }
                    });
                }

                SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
            }
        });
    }

    SubSididiarLedgerAll();


    $(document).on('click', '.subsidiary-ledger', function () {
        const autoId = parseInt($(this).data('autoid')); 
        if (!autoId) return; 

        isEditModeSubSidiary = true;
        //selectedSubSidiaryLedger = [autoId]; 



        $.ajax({
            url: `/subsidiary-ledger/details/${autoId}`,
            type: 'GET',
            success: function (response) {
                if (response) {
                    console.log("Sub-Sidiary Ledger Edit Details======= : ", response);

                    $('#forsidiaryAutoID').val(response.autoId);
                    $('#forsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                    $('#SusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                    $('#SubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                    $('#forsidiaryshortName').val(response.shortName || '');

                    $('#forsidiarygroupcode').val(response.glCode || '');
                    choiceManager.setChoiceValue('forsidiarygroupName', response.glCode);
                    $('#forsidiarygroupshortName').val(response.glShortName || '');

                    $('#forsidiaryControlCode').val(response.crlCode || '');
                    choiceManager.setChoiceValue('forsidiaryControlName', response.crlCode || '');
                    $('#forsidiaryCLShortName').val(response.crLShortName || '');

                    $('#forsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                    choiceManager.setChoiceValue('forsidiaryControlName', response.generalLedgerCodeNo || '');
                    $('#forsidiarySubCRLshortName').val(response.subCRLShortName || '');

                    if (response.lDate) {
                        let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: 'numeric',
                            minute: '2-digit',
                            hour12: true
                        }).replace(',', '');
                        $('#SubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                    } else {
                        $('#SubSidiaryldisplayLDate').text('');
                    }

                    if (response.modifyDate) {
                        let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: 'numeric',
                            minute: '2-digit',
                            hour12: true
                        }).replace(',', '');
                        $('#SubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                    } else {
                        $('#SubSidiarydisplayModifyDate').text('');
                    }

                    //SubSidiaryupdateCheckboxState(); 
                    // 1. Clear all selected checkboxes
                    $('.Subsidiary-checkbox').prop('checked', false);

                    // 2. Update selectedSubSidiaryLedger array
                    selectedSubSidiaryLedger = [autoId];

                    // 3. Check the checkbox of the clicked row
                    $(`.Subsidiary-checkbox[data-autoid="${autoId}"]`).prop('checked', true);

                    // 4. Make sure 'Select All' is unchecked
                    $("#SubSidiaryselectAll").prop('checked', false);

                    // 5. Update any internal state (optional)
                    SubSidiaryupdateCheckboxState();

                } else {
                    toastr.error("Sub-Sidiary Ledger not found!");
                }
            },
            error: function (xhr, status, error) {
                toastr.error("Error fetching Sub-Sidiary Ledger details!");
                console.error("Error fetching details:", error);
            }
        });
    });


    //Handle Save Or Update Button Handle
    $(document).on("submit", "#subSidiaryForm", function (e) {
        e.preventDefault();
        SubSidiaryLedgerSaveOrUpdate();
    });

    // Handle Delete Button
    $(document).on("click", "#SubSidiarydeleteBtn", function (e) {
        e.preventDefault();
        SubSidiaryLedgerDelete();
    });

    // Handle Clear button
    $(document).on("click", ".SubSidiaryresetBtn", function () {
        console.log("Clerar Button Clicked")
        SubSidiaryLedgerFormClear();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".subSidiaryfavBtn").on("click", function () {
        toggleFavorite();
    });


    //Searching Handle
    $(document).on('input', '.SubSidiarysearchInput', SubSidiarydebounce(function () {
        const searchValue = $('.SubSidiarysearchInput').val();
        console.log("Sub Sidiary Search Value: ", searchValue);
        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, searchValue, selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#SubSidiaryfirstPage', function () {
        currenSubSidiarytPage = 1;
        SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });

    //Next Page Button
    $(document).on('click', '#SubSidiarynextPage', function () {
        currenSubSidiarytPage++;
        SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });

    //Previous Page Button
    $(document).on('click', '#SubSidiaryprevPage', function () {
        if (currenSubSidiarytPage > 1) {
            currenSubSidiarytPage--;
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
        }
    });

    //Last Page Button
    $(document).on('click', '#SubSidiarylastPage', function () {
        let lastPage = $('#SubSidiarypageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenSubSidiarytPage = lastPage;
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "" ,selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
        }
    });

    //Showing Page Size
    $(document).on('change', '.SubSidiarypageSize', function () {
        SubSidiarypageSize = parseInt($(this).val());
        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.SubSidiarysortable', function () {
        const column = $(this).data('column');

        if (column === SubSidiarycurrentSortColumn) {
            SubSidiarycurrentSortDirection = (SubSidiarycurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            SubSidiarycurrentSortColumn = column;
            SubSidiarycurrentSortDirection = 'desc';
        }

        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, $('#SubsearchInput').val().trim(), selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });

    SubSidiarybindCheckboxHandlers();

});

//#endregion
