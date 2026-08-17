let selectedModalSubSidiaryLedger = [];
let ModalcurrenSubSidiarytPage = 1;
let ModalSubSidiarypageSize = 10;
let ModalSubSidiarycurrentSortColumn = 'SusidiaryLedgerCodeNo';
let ModalSubSidiarycurrentSortDirection = 'desc';
console.log(`Sub-Sidiary Page Global Called: ${selectedModalSubSidiaryLedger}`);
let ModalisEditModeSubSidiary = false;
const $ModalgroupDropdown = $("#ModalforsidiarygroupName");
const $ModalcontrolDropdown = $("#ModalforsidiaryControlName");
const $ModalsubControlDropdown = $("#ModalforsidiaryGeneralLedgerName");
const ModalselectedGroupCode = $('#ModalforsidiarygroupName').val() || "";
const ModalselectedControlCode = $('#ModalforsidiaryControlName').val() || "";
const ModalselectedSubControlCode = $('#ModalforsidiaryGeneralLedgerName').val() || "";




//#region Load All Sub-Sidiary Ledger Data
function ModalSubSididiarLedgerAll(page = 1, ModalSubSidiarypageSize = 10, sortColumn = ModalSubSidiarycurrentSortColumn, sortDirection = ModalSubSidiarycurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "", subcontrolLedgerCode = "") {

    $.ajax({
        url: '/subsidiary-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: ModalSubSidiarypageSize,
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
                        <input type="checkbox" class="form-check-input form-check-input-sm ModalSubsidiary-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer Modalsubsidiary-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.susidiaryLedgerCodeNo}
                    </td>
                    <td>${item.subsidiaryLedgerName || ''}</td>
                    <td>${item.shortName || ''}</td>
                 </tr>`;
            });
            $('#ModalSubSidiarytblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#ModalSubSidiarypaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#ModalSubSidiarypaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#ModalSubSidiarytotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            ModalSubSidiarygeneratePageButtons(currenControltPage, totalPages);
            ModalSubSidiaryupdateCheckboxState();
            ModalSubSidiaryupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Sort & Pagination Helpers 
function ModalSubSidiaryupdateSortIndicators() {
    $('.ModalSubSidiarysortable').removeClass('sort-asc sort-desc');
    $('.ModalSubSidiarysortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.ModalSubSidiarysortable[data-column="${ModalSubSidiarycurrentSortColumn}"]`);
    header.addClass(ModalSubSidiarycurrentSortColumn === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalSubSidiarycurrentSortColumn === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalSubSidiarygeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalSubSidiarypageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalSubSididiarLedgerAll(page, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, $('#ModalSubSidiarysearchInput').val().trim()));

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

function ModalSubSidiaryupdateCheckboxState() {
    $('.ModalSubsidiary-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedModalSubSidiaryLedger.includes(ID));
    });
    const allChecked = $('.ModalSubsidiary-checkbox:visible').length > 0 &&
        $('.ModalSubsidiary-checkbox:visible:not(:checked)').length === 0;
    $('#ModalSubSidiaryselectAll').prop('checked', allChecked);
}

function ModalSubSidiarydebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ModalSubSidiarybindCheckboxHandlers() {
    $(document).off('click', '.ModalSubsidiary-checkbox');
    $(document).on('click', '.ModalSubsidiary-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalSubSidiaryLedger.includes(id)) {
                selectedModalSubSidiaryLedger.push(id);
            }
        } else {
            const index = selectedModalSubSidiaryLedger.indexOf(id);
            if (index !== -1) {
                selectedModalSubSidiaryLedger.splice(index, 1);
            }
        }

        const allChecked = $('.ModalSubsidiary-checkbox:visible').length > 0 &&
            $('.ModalSubsidiary-checkbox:visible:not(:checked)').length === 0;
        $('#ModalSubSidiaryselectAll').prop('checked', allChecked);

        console.log("Selected Sub Sidiary Ledger:", selectedModalSubSidiaryLedger);
    });
}
//#endregion


//#region Select all checkbox

$(document).on('click', '#ModalSubSidiaryselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.ModalSubsidiary-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.ModalSubsidiary-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedModalSubSidiaryLedger.includes(id)) {
                selectedModalSubSidiaryLedger.push(id);
            }
        });
    } else {
        $('.ModalSubsidiary-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedModalSubSidiaryLedger.indexOf(id);
            if (index !== -1) {
                selectedModalSubSidiaryLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected Sub Control Ledger:", selectedModalSubSidiaryLedger);
});
//#endregion


//#region Save Or Update Sub-Sidiary Ledger
function ModalSubSidiaryLedgerSaveOrUpdate() {
    console.log("Save Or Update Sub-Sidiary Ledger Function Called");

    let autoID = $('#ModalforsidiaryAutoID').val();
    let SubSidiaryLedgerData = {
        autoId: autoID || 0,
        GeneralLedgerCodeNo: $("#ModalforsidiaryGeneralLedgerCodeNo").val(),
        SusidiaryLedgerCodeNo: $("#ModalSusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: $("#ModalSubsidiaryLedgerName").val(),
        ShortName: $("#ModalforsidiaryshortName").val(),
        CRLCode: $("#ModalforsidiaryControlName").val(),
        GLCode: $("#ModalforsidiarygroupName").val(),
    };

    //validation
    if (!SubSidiaryLedgerData.GLCode) {
        toastr.error("Please Select a Group Ledger.");
        let choicesWrapper = document.querySelector('#ModalforsidiarygroupName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.CRLCode) {
        toastr.error("Please Select a Control Ledger.");
        let choicesWrapper = document.querySelector('#ModalforsidiaryControlName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.GeneralLedgerCodeNo) {
        toastr.error("Please Select a Sub-Control Ledger.");
        let choicesWrapper = document.querySelector('#ModalforsidiaryGeneralLedgerName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger.");
        $('#ModalSubsidiaryLedgerName').addClass('border-error');
        return false;
    }
    else {
        $('#ModalSubsidiaryLedgerName').removeClass('border-error');
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

                        ModalSubSidiaryLedgerFormClear();
                        selectedModalSubSidiaryLedger = [];

                        //Load Sub-Sidiary Table
                        SubSididiarLedgerAll();
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
function ModalSubSidiaryLedgerFormClear() {

    ModalisEditModeSubSidiary = false;

    $("#ModalforsidiaryAutoID").val("");
    choiceManager.clearChoice('ModalforsidiarygroupName');
    choiceManager.clearChoice('ModalforsidiaryControlName');
    choiceManager.clearChoice('ModalforsidiaryGeneralLedgerName');


    $("#Modalforsidiarygroupcode").val("");
    $("#ModalforsidiarygroupshortName").val("");

    $("#ModalforsidiaryControlCode").val("");
    $("#ModalforsidiaryCLShortName").val("");

    $("#ModalforsidiaryGeneralLedgerCodeNo").val("");
    $("#ModalforsidiarySubCRLshortName").val("");

    $("#ModalSusidiaryLedgerCodeNo").val("");
    $("#ModalSubsidiaryLedgerName").val("");
    $("#ModalforsidiaryshortName").val("");

    $(".form-control").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubSidiarydisplayModifyDate').text('');
    $('#ModalSubSidiaryldisplayLDate').text('');

    $(".ModalSubsidiary-checkbox").prop("checked", false);
    $("#ModalSubSidiaryselectAll").prop("checked", false);
    $('#ModalSubSidiarysearchInput').val('');

    // Set Entry Date
    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    document.querySelectorAll('.choiceDD').forEach(function (dropdown) {
        let choicesWrapper = dropdown.closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.remove('border-error');
        }
    });

    selectedModalSubSidiaryLedger = [];
    currenSubSidiarytPage = 1;
    const emptySearch = '';
    const emptyGroupCode = '';
    const emptyControlCode = '';
    const emptySubControlCode = '';
    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, emptySearch, emptyGroupCode, emptyControlCode, emptySubControlCode);
}
//#endregion


//#region Single & More Delete Sub-Sidiary Ledger

function ModalSubSidiaryLedgerDelete() {
    // Filter only valid IDs
    const ids = selectedModalSubSidiaryLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
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

            //const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
            //window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            ModalSubSidiaryLedgerFormClear();
            ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
            selectedModalSubSidiaryLedger = [];
            $("#ModalSubSidiaryselectAll").prop("checked", false);

            //Load Sub-Sidiary Table
            SubSididiarLedgerAll();
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion

function ModalSubSidiaryForGeneralUI() {
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            choiceManager.populateDropdown('ModalforsidiarygroupName', groupResponse.data);

            let isClearingGroupDropdown = false;
            $ModalgroupDropdown.off('change').on('change', function () {
                const ModalselectedGroupCode = $(this).val();

                if (isClearingGroupDropdown) return;

                if (!ModalselectedGroupCode) {
                    isClearingGroupDropdown = true;

                    choiceManager.clearChoice('ModalforsidiarygroupName');
                    $('#Modalforsidiarygroupcode').val('');
                    $('#ModalforsidiarygroupshortName').val('');

                    // Control Ledger clear
                    $ModalcontrolDropdown.off('change');
                    choiceManager.clearChoice('ModalforsidiaryControlName');
                    $('#ModalforsidiaryControlCode').val('');
                    $('#ModalforsidiaryCLShortName').val('');

                    // Sub-Control Ledger clear
                    $ModalsubControlDropdown.off('change');
                    choiceManager.clearChoice('ModalforsidiaryGeneralLedgerName');
                    $('#ModalforsidiaryGeneralLedgerCodeNo').val('');
                    $('#ModalforsidiarySubCRLshortName').val('');
                    $('#ModalSusidiaryLedgerCodeNo').val('');

                    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", "", "", "");
                    isClearingGroupDropdown = false;
                }



                // Load Group info
                if (ModalselectedGroupCode) {
                    $.ajax({
                        url: '/forsubsidiary-group-ledger-info',
                        type: 'GET',
                        data: { groupcode: ModalselectedGroupCode },
                        success: function (data) {
                            $('#Modalforsidiarygroupcode').val(data.controlLedgerCodeNo);
                            $('#ModalforsidiarygroupshortName').val(data.shortName || '');
                            ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCode, "", "");
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

//#region Ready Part

$(document).ready(function () {

    //Group Ledger Dropdown
    ModalSubSidiaryForGeneralUI();

    // Set Entry Date
    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $('#ModalforsidiarygroupName').on('change', function (e) {
        e.preventDefault();

        var id = $(this).val();

        ModalgetControlLedger(id);
    })


    function ModalgetControlLedger(ModalselectedGroupCode) {
        $.ajax({
            url: '/forSubsidiary-control-ledger-dropdown',
            type: 'GET',
            data: { groupcode: ModalselectedGroupCode },
            success: function (controlResponse) {
                choiceManager.populateDropdown('ModalforsidiaryControlName', controlResponse.data);

                // If in edit mode, set the value after populate
                if (ModalisEditModeSubSidiary) {
                    const currentControlCode = $('#ModalforsidiaryControlCode').val();
                    choiceManager.setChoiceValue('ModalforsidiaryControlName', currentControlCode);
                }
                let isClearingControlDropdown = false;

                $ModalcontrolDropdown.off('change').on('change', function () {
                    if (isClearingControlDropdown) return;

                    const ModalselectedControlCode = $(this).val();

                    if (!ModalselectedControlCode) {
                        isClearingControlDropdown = true;

                        choiceManager.clearChoice('ModalforsidiaryControlName');
                        $('#ModalforsidiaryControlCode').val('');
                        $('#ModalforsidiaryCLShortName').val('');

                        choiceManager.clearChoice('ModalforsidiaryGeneralLedgerName');
                        $('#ModalforsidiaryGeneralLedgerCodeNo').val('');
                        $('#ModalforsidiarySubCRLshortName').val('');
                        $('#ModalSusidiaryLedgerCodeNo').val('');

                        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCode, "", "");

                        isClearingControlDropdown = false;
                        return;
                    }

                    // Load Control Ledger info
                    ModalGetControlLedgerInfo(ModalselectedControlCode)

                });
            }
        });
    }

    function ModalGetControlLedgerInfo(ModalselectedControlCode) {
        $.ajax({
            url: '/forSubsidiary-control-ledger-info',
            type: 'GET',
            data: { contrlcode: ModalselectedControlCode },
            success: function (controlInfo) {
                $('#ModalforsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo);
                $('#ModalforsidiaryCLShortName').val(controlInfo.shortName || '');
                ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCode, ModalselectedControlCode, "");
            }
        });
    }


    $('#ModalforsidiaryControlName').on('change', function (e) {
        e.preventDefault();

        var id = $(this).val();

        ModalgetSubControllerLedger(id);
    });

    function ModalgetSubControllerLedger(ModalselectedControlCode) {
        // Load Sub-Control Ledger dropdown under selected control
        $.ajax({
            url: '/subcontrol-ledger-dropdown',
            type: 'GET',
            data: { controlcode: ModalselectedControlCode },
            success: function (subControlResponse) {
                console.log("Sub Control Info Infor ====", subControlResponse);

                choiceManager.populateDropdown('ModalforsidiaryGeneralLedgerName', subControlResponse.data);

                if (ModalisEditModeSubSidiary) {
                    const currentSubControlCode = $('#ModalforsidiaryGeneralLedgerCodeNo').val();
                    choiceManager.setChoiceValue('ModalforsidiaryGeneralLedgerName', currentSubControlCode);
                }

                let isClearingSubControlDropdown = false;

                $ModalsubControlDropdown.off('change').on('change', function () {
                    if (isClearingSubControlDropdown) return;

                    const ModalselectedSubControlCode = $(this).val();

                    if (!ModalselectedSubControlCode) {
                        isClearingSubControlDropdown = true;
                        //choiceManager.clearChoice('forsidiaryGeneralLedgerName');
                        $('#ModalforsidiaryGeneralLedgerName').destroy();
                        $('#ModalforsidiaryGeneralLedgerCodeNo').val('');
                        $('#ModalforsidiarySubCRLshortName').val('');
                        $('#ModalSusidiaryLedgerCodeNo').val('');

                        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCode, ModalselectedControlCode, "");

                        isClearingSubControlDropdown = false;
                        return;
                    }

                    // Fetch Sub-Control info
                    ModalGetSubcontrolInfo(ModalselectedSubControlCode);

                });
            }
        });

    }

    function ModalGetSubcontrolInfo(ModalselectedSubControlCode) {
        $.ajax({
            url: '/forsudsidiary-subcontrol-ledger-info',
            type: 'GET',
            data: { subcontrlcode: ModalselectedSubControlCode },
            success: function (subControlInfo) {
                $('#ModalforsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo);
                $('#ModalforsidiarySubCRLshortName').val(subControlInfo.shortName || '');

                if (!ModalisEditModeSubSidiary) {
                    $.ajax({
                        url: '/next-subsidiary-ledger-code',
                        type: 'GET',
                        data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                        success: function (nextCode) {
                            $('#ModalSusidiaryLedgerCodeNo').val(nextCode);
                        }
                    });
                }

                ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCode, ModalselectedControlCode, ModalselectedSubControlCode);
            }
        });
    }

    ModalSubSididiarLedgerAll();


    $(document).on('click', '.Modalsubsidiary-ledger', function () {
        const autoId = parseInt($(this).data('autoid'));
        if (!autoId) return;

        ModalisEditModeSubSidiary = true;
        //selectedSubSidiaryLedger = [autoId]; 



        $.ajax({
            url: `/subsidiary-ledger/details/${autoId}`,
            type: 'GET',
            success: function (response) {
                if (response) {
                    console.log("Sub-Sidiary Ledger Edit Details======= : ", response);

                    $('#ModalforsidiaryAutoID').val(response.autoId);
                    $('#ModalforsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                    $('#ModalSusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                    $('#ModalSubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                    $('#ModalforsidiaryshortName').val(response.shortName || '');

                    $('#Modalforsidiarygroupcode').val(response.glCode || '');
                    choiceManager.setChoiceValue('ModalforsidiarygroupName', response.glCode);
                    $('#ModalforsidiarygroupshortName').val(response.glShortName || '');

                    $('#ModalforsidiaryControlCode').val(response.crlCode || '');
                    choiceManager.setChoiceValue('ModalforsidiaryControlName', response.crlCode || '');
                    $('#ModalforsidiaryCLShortName').val(response.crLShortName || '');

                    $('#ModalforsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                    choiceManager.setChoiceValue('ModalforsidiaryControlName', response.generalLedgerCodeNo || '');
                    $('#ModalforsidiarySubCRLshortName').val(response.subCRLShortName || '');

                    if (response.lDate) {
                        let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric',
                            hour: 'numeric',
                            minute: '2-digit',
                            hour12: true
                        }).replace(',', '');
                        $('#ModalSubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                    } else {
                        $('#ModalSubSidiaryldisplayLDate').text('');
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
                        $('#ModalSubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                    } else {
                        $('#ModalSubSidiarydisplayModifyDate').text('');
                    }
  
                    $('.ModalSubsidiary-checkbox').prop('checked', false);

                    selectedModalSubSidiaryLedger = [autoId];

                    $(`.ModalSubsidiary-checkbox[data-autoid="${autoId}"]`).prop('checked', true);

                    $("#ModalSubSidiaryselectAll").prop('checked', false);

                    ModalSubSidiaryupdateCheckboxState();

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
    $(document).on("submit", "#ModalsubSidiaryForm", function (e) {
        e.preventDefault();
        ModalSubSidiaryLedgerSaveOrUpdate();
    });

    // Handle Delete Button
    $(document).on("click", "#ModalSubSidiarydeleteBtn", function (e) {
        e.preventDefault();
        ModalSubSidiaryLedgerDelete();
    });

    // Handle Clear button
    $(document).on("click", ".ModalSubSidiaryresetBtn", function () {
        console.log("Clerar Button Clicked")
        ModalSubSidiaryLedgerFormClear();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".ModalsubSidiaryfavBtn").on("click", function () {
        toggleFavorite();
    });


    //Searching Handle
    $(document).on('input', '.ModalSubSidiarysearchInput', ModalSubSidiarydebounce(function () {
        const searchValue = $('.ModalSubSidiarysearchInput').val();
        console.log("Sub Sidiary Search Value: ", searchValue);
        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, searchValue);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#ModalSubSidiaryfirstPage', function () {
        currenSubSidiarytPage = 1;
        ModalSubSididiarLedgerAll(currenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
    });

    //Last Page Button
    $(document).on('click', '#ModalSubSidiarynextPage', function () {
        currenSubSidiarytPage++;
        ModalSubSididiarLedgerAll(currenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
    });

    //Previous Page Button
    $(document).on('click', '#ModalSubSidiaryprevPage', function () {
        if (currenSubSidiarytPage > 1) {
            currenSubSidiarytPage--;
            ModalSubSididiarLedgerAll(currenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
        }
    });

    //Last Page Button
    $(document).on('click', '#ModalSubSidiarylastPage', function () {
        let lastPage = $('#ModalSubSidiarypageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenSubSidiarytPage = lastPage;
            ModalSubSididiarLedgerAll(currenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
        }
    });

    //Showing Page Size
    $(document).on('change', '.ModalSubSidiarypageSize', function () {
        SubSidiarypageSize = parseInt($(this).val());
        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.ModalSubSidiarysortable', function () {
        const column = $(this).data('column');

        if (column === ModalSubSidiarycurrentSortColumn) {
            ModalSubSidiarycurrentSortDirection = (ModalSubSidiarycurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalSubSidiarycurrentSortColumn = column;
            ModalSubSidiarycurrentSortDirection = 'desc';
        }

        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, $('#ModalSubsearchInput').val().trim());
    });

    ModalSubSidiarybindCheckboxHandlers();

});

//#endregion
