let selectedSubControlLedger = [];
let currenSubControltPage = 1;
let SubControlpageSize = 10;
let SubControlcurrentSortColumn = 'GeneralLedgerCodeNo';
let SubControlcurrentSortDirection = 'desc';
let isEditModeSubControl = false;

let selectedGroupLedgerCodeForSubControl = "";
let selectedControlLedgerCodeForSubControl = "";

// Helper: Bangladesh Local Date & Time Formatter
function getBangladeshDateTime() {
    return new Date().toLocaleString('en-GB', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
    }).replace(',', '').toUpperCase();
}

//#region Error Border Cleanup (Native / Custom Dropdown)
document.addEventListener('change', function (e) {
    if (e.target && $(e.target).hasClass('form-select')) {
        $(e.target).removeClass('border-error');
        $(e.target).siblings('.custom-dd-wrapper').removeClass('border-error');
    }
});
//#endregion

//#region Custom Event Listener For Group Ledger Dropdown Updated
window.addEventListener('groupLedgerUpdated', function (e) {
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

// 1. Group Ledger Dropdown
function LoadDropdownGRLCRL(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown-forsubcontrol",
        type: "GET",
        success: function (response) {
            let $gldropdown = $("#groupName");
            $gldropdown.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $gldropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $gldropdown.val(selectedId);
            }

            // Plugin Reinitialization
            window.reinitCustomDropdownById('groupName');

            // Group Ledger change event
            $gldropdown.off('change').on('change', function () {
                let selectedGroupCode = $(this).val();
                selectedGroupLedgerCodeForSubControl = selectedGroupCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                // Reset fields if no group selected
                if (!selectedGroupCode) {
                    $('#groupcode, #groupshortName').val('');

                    // Clear Control Dropdown via Plugin Helper
                    window.clearCustomDropdownById('ConRLName');
                    $('#ConRLCode, #CLShortName, #GeneralLedgerCodeNo').val('');

                    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", "", "");
                    return;
                }

                // Load group info
                $.ajax({
                    url: '/group-ledger-info-forsubcontrol',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#groupcode').val(data.controlLedgerCodeNo || '');
                        $('#groupshortName').val(data.shortName || '');
                    }
                });

                // Load Control Ledger under selected Group
                LoadControlLedgerDropdown(selectedGroupCode);
            });
        },
        error: function (xhr, status, error) {
            console.error("Failed to load Group Ledger dropdown:", error);
        }
    });
}

// 2. Control Ledger Dropdown
function LoadControlLedgerDropdown(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/control-ledger-dropdown',
        type: 'GET',
        data: { groupcode: selectedGroupCode },
        success: function (response) {
            let $crldropdown = $('#ConRLName');
            $crldropdown.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $crldropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $crldropdown.val(selectedId);
            }

            // Plugin Reinitialization
            window.reinitCustomDropdownById('ConRLName');

            // Auto reload Sub-Control Ledger list based on selected group
            LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, "");

            // Control Ledger change event
            $crldropdown.off('change').on('change', function () {
                let selectedControlCode = $(this).val();
                selectedControlLedgerCodeForSubControl = selectedControlCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#ConRLCode, #CLShortName, #GeneralLedgerCodeNo').val('');
                    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, "");
                    return;
                }

                // Fetch control ledger details
                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode },
                    success: function (data) {
                        $('#ConRLCode').val(data.subControlLedgerCodeNo || '');
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

                        // Reload list filtered by both group and control
                        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
                    },
                    error: function () {
                        console.error("Failed to fetch Control Ledger info");
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to fetch Control Ledger list under group");
        }
    });
}

//#endregion

//#region Save Or Update Sub Control Ledger
function SubControlLedgerSaveOrUpdate() {
    let autoID = $('#SubCRLAutoID').val();
    let SubControlLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        SubControlLedgerCodeNo: $("#ConRLCode").val(),
        GeneralLedgerCodeNo: $("#GeneralLedgerCodeNo").val(),
        GeneralLedgerName: $("#GeneralLedgerName").val(),
        ShortName: $("#SubCRLshortName").val(),
        GRLCode: $("#groupName").val()
    };

    // Validations
    if (!SubControlLedgerData.GRLCode) {
        toastr.error("Please Select a Group Ledger.");
        $('#groupName').addClass('border-error');
        $('#groupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlLedgerData.SubControlLedgerCodeNo) {
        toastr.error("Please Select a Control Ledger.");
        $('#ConRLName').addClass('border-error');
        $('#ConRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlLedgerData.GeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger.");
        $('#GeneralLedgerName').addClass('border-error');
        return false;
    } else {
        $('#GeneralLedgerName').removeClass('border-error');
    }

    // Check for duplicate
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

            let url = autoID ? `/subcontrol-ledger/${autoID}` : '/subcontrol-ledger';
            let method = autoID ? 'PUT' : 'POST';
            let successMessage = autoID ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubControlLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
                        window.dispatchEvent(event);

                        SubControlLedgerFormClear();
                        selectedSubControlLedger = [];

                        if (typeof ModalLoadSubControlLedgerAll === "function") {
                            ModalLoadSubControlLedgerAll();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
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

    $("#SubCRLAutoID").val("");

    // Clear all custom dropdowns
    window.clearAllCustomDropdowns('#subcontrolForm');

    $("#groupcode, #groupshortName").val("");
    $("#ConRLCode, #CLShortName").val("");
    $("#GeneralLedgerCodeNo, #GeneralLedgerName, #SubCRLshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#SubControldisplayModifyDate').text('');
    $('#SubControldisplayLDate').text(getBangladeshDateTime());

    $(".Subcontrol-checkbox").prop("checked", false);
    $("#SubselectAll").prop("checked", false);
    $('#SubsearchInput').val('');

    selectedSubControlLedger = [];
    currenSubControltPage = 1;
    selectedGroupLedgerCodeForSubControl = "";
    selectedControlLedgerCodeForSubControl = "";

    LoadDropdownGRLCRL();
    LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", "", "");
}
//#endregion

//#region Load All Sub-Control Ledger Data
function LoadSubControlLedgerAll(page = 1, pageSize = SubControlpageSize, sortColumn = SubControlcurrentSortColumn, sortDirection = SubControlcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "") {
    $.ajax({
        url: '/subcontrol-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            controlLedgerCode: controlLedgerCode,
            groupLedgerCode: groupLedgerCode
        },
        success: function (response) {
            let rows = '';
            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center">
                            <input type="checkbox" class="form-check-input form-check-input-sm Subcontrol-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-transfer subcontrol-ledger" data-autoid="${item.autoId}">
                                ${item.generalLedgerCodeNo}
                            </button>
                        </td>
                        <td>${item.generalLedgerName || ''}</td>
                        <td>${item.shortName || ''}</td>
                     </tr>`;
                });
            } else {
                rows = `<tr><td colspan="4" class="text-center text-muted">No records found</td></tr>`;
            }
            $('#SubControltblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#SubControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#SubControlpaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#SubControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                currenSubControltPage = currPage;
                SubControlgeneratePageButtons(currenSubControltPage, totalPages);
            }

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
$(document).on('click', '.subcontrol-ledger', function (e) {
    e.preventDefault();
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    isEditModeSubControl = true;
    selectedSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#SubCRLAutoID').val(id);
                $('#groupcode').val(data.grlCode || '');
                $('#groupshortName').val(data.grlShortname || '');

                // Group Ledger
                $('#groupName').val(data.grlCode);
                window.reinitCustomDropdownById('groupName');

                // Control Ledger Dropdown
                if (data.grlCode) {
                    LoadControlLedgerDropdown(data.grlCode, data.subControlLedgerCodeNo);
                }

                $('#ConRLCode').val(data.subControlLedgerCodeNo || '');
                $('#CLShortName').val(data.crlShortname || '');

                $('#GeneralLedgerCodeNo').val(data.generalLedgerCodeNo || '');
                $('#GeneralLedgerName').val(data.generalLedgerName || '');
                $('#SubCRLshortName').val(data.shortName || '');

                // Dates
                if (data.lDate) {
                    let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#SubControldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#SubControldisplayLDate').text('');
                }

                if (data.modifyDate) {
                    let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#SubControldisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#SubControldisplayModifyDate').text('');
                }

                $('.Subcontrol-checkbox').prop('checked', false);
                $("#SubselectAll").prop('checked', false);
                $(`.Subcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);
                SubControlupdateCheckboxState();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Sub-Control Ledger Details.");
            console.error("Error Fetching Sub-Control Details:", error);
        }
    });
});
//#endregion

//#region Single & Multiple Delete Sub-Control Ledger
function SubControlLedgerDelete() {
    const ids = selectedSubControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/subcontrol-ledger-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
            window.dispatchEvent(event);

            SubControlLedgerFormClear();
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection);
            selectedSubControlLedger = [];
            $("#SubselectAll").prop("checked", false);

            if (typeof ModalLoadSubControlLedgerAll === "function") {
                ModalLoadSubControlLedgerAll();
            }
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
    header.addClass(SubControlcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(SubControlcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function SubControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#SubControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadSubControlLedgerAll(page, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, $('#SubsearchInput').val().trim(), selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl));

        if (page === currentPage) {
            btn.addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    if (totalPages <= 1) return;

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

    navigationDiv.append(createButton(totalPages));
}

function SubControlupdateCheckboxState() {
    $('.Subcontrol-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
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
    $(document).off('click', '.Subcontrol-checkbox').on('click', '.Subcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedSubControlLedger.includes(id)) selectedSubControlLedger.push(id);
        } else {
            const index = selectedSubControlLedger.indexOf(id);
            if (index !== -1) selectedSubControlLedger.splice(index, 1);
        }

        const allChecked = $('.Subcontrol-checkbox:visible').length > 0 &&
            $('.Subcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#SubselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#SubselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Subcontrol-checkbox:visible').prop('checked', isChecked);

    $('.Subcontrol-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedSubControlLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedSubControlLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedSubControlLedger.splice(index, 1);
        }
    });
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    LoadDropdownGRLCRL();
    LoadSubControlLedgerAll();
    SubControlbindCheckboxHandlers();

    $('#SubControldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#subcontrolForm", function (e) {
        e.preventDefault();
        SubControlLedgerSaveOrUpdate();
    });

    $(document).on("click", "#SubcontroldeleteBtn, .js-SubControl-delete-confirm", function (e) {
        e.preventDefault();
        SubControlLedgerDelete();
    });

    $(document).on("click", ".SubcontrolresetBtn, .js-SubControl-clear", function () {
        SubControlLedgerFormClear();
    });

    $(document).on('input', '.SubsearchInput', SubControldebounce(function () {
        const searchValue = $(this).val().trim();
        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, searchValue, selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    }, 500));

    $(document).on('click', '#SubControlfirstPage', function () {
        currenSubControltPage = 1;
        LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    $(document).on('click', '#SubControlnextPage', function () {
        currenSubControltPage++;
        LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
    });

    $(document).on('click', '#SubControlprevPage', function () {
        if (currenSubControltPage > 1) {
            currenSubControltPage--;
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
        }
    });

    $(document).on('click', '#SubControllastPage', function () {
        let lastPage = parseInt($('#SubControlpageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            currenSubControltPage = lastPage;
            LoadSubControlLedgerAll(currenSubControltPage, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection, "", selectedGroupLedgerCodeForSubControl, selectedControlLedgerCodeForSubControl);
        }
    });

    $(document).on('change', '.SubControlpageSize', function () {
        SubControlpageSize = parseInt($(this).val());
        LoadSubControlLedgerAll(1, SubControlpageSize, SubControlcurrentSortColumn, SubControlcurrentSortDirection);
    });

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
});
//#endregion