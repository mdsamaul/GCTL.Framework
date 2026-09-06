var selectedModalSubControlLedger = [];
var ModalcurrenSubControltPage = 1;
var ModalSubControlpageSize = 10;
var ModalSubControlcurrentSortColumn = 'ModalGeneralLedgerCodeNo';
var ModalSubControlcurrentSortDirection = 'desc';
var ModalisEditModeSubControl = false;

var ModalselectedGroupLedgerCodeForSubControl = "";
var ModalselectedControlLedgerCodeForSubControl = "";

// Helper: Bangladesh Local Date & Time Formatter
window.getBangladeshDateTime = window.getBangladeshDateTime || function() {
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
    ModalLoadDropdownGRLCRL();
    $('#Modalgroupcode').val('');
    $('#ModalgroupshortName').val('');
    $('#ModalConRLCode').val('');
    $('#ModalCLShortName').val('');
    $('#ModalGeneralLedgerCodeNo').val('');
    ModalLoadSubControlLedgerAll();
});
//#endregion

//#region Group and Control Ledger Dropdown For Sub-Control Ledger UI

// 1. Group Ledger Dropdown
function ModalLoadDropdownGRLCRL(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown-forsubcontrol",
        type: "GET",
        success: function (response) {
            let $gldropdown = $("#ModalgroupName");
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
            window.reinitCustomDropdownById('ModalgroupName');

            // Group Ledger change event
            $gldropdown.off('change').on('change', function () {
                let selectedGroupCode = $(this).val();
                ModalselectedGroupLedgerCodeForSubControl = selectedGroupCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                // Reset fields if no group selected
                if (!selectedGroupCode) {
                    $('#Modalgroupcode, #ModalgroupshortName').val('');

                    // Clear Control Dropdown via Plugin Helper
                    window.clearCustomDropdownById('ModalConRLName');
                    $('#ModalConRLCode, #ModalCLShortName, #ModalGeneralLedgerCodeNo').val('');

                    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", "", "");
                    return;
                }

                // Load group info
                $.ajax({
                    url: '/group-ledger-info-forsubcontrol',
                    type: 'GET',
                    data: { Modalgroupcode: selectedGroupCode },
                    success: function (data) {
                        $('#Modalgroupcode').val(data.controlLedgerCodeNo || '');
                        $('#ModalgroupshortName').val(data.shortName || '');
                    }
                });

                // Load Control Ledger under selected Group
                ModalLoadControlLedgerDropdown(selectedGroupCode);
            });
        },
        error: function (xhr, status, error) {
            console.error("Failed to load Group Ledger dropdown:", error);
        }
    });
}

// 2. Control Ledger Dropdown
function ModalLoadControlLedgerDropdown(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/control-ledger-dropdown',
        type: 'GET',
        data: { Modalgroupcode: selectedGroupCode },
        success: function (response) {
            let $crldropdown = $('#ModalConRLName');
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
            window.reinitCustomDropdownById('ModalConRLName');

            // Auto reload Sub-Control Ledger list based on selected group
            ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, "");

            // Control Ledger change event
            $crldropdown.off('change').on('change', function () {
                let selectedControlCode = $(this).val();
                ModalselectedControlLedgerCodeForSubControl = selectedControlCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#ModalConRLCode, #ModalCLShortName, #ModalGeneralLedgerCodeNo').val('');
                    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, "");
                    return;
                }

                // Fetch control ledger details
                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode },
                    success: function (data) {
                        $('#ModalConRLCode').val(data.subControlLedgerCodeNo || '');
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

                        // Reload list filtered by both group and control
                        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
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
function ModalSubControlLedgerSaveOrUpdate() {
    let autoID = $('#ModalSubCRLAutoID').val();
    let SubControlLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        SubControlLedgerCodeNo: $("#ModalConRLCode").val(),
        ModalGeneralLedgerCodeNo: $("#ModalGeneralLedgerCodeNo").val(),
        ModalGeneralLedgerName: $("#ModalGeneralLedgerName").val(),
        ShortName: $("#ModalSubCRLshortName").val(),
        GRLCode: $("#ModalgroupName").val()
    };

    // Validations
    if (!SubControlLedgerData.GRLCode) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalgroupName').addClass('border-error');
        $('#ModalgroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlLedgerData.SubControlLedgerCodeNo) {
        toastr.error("Please Select a Control Ledger.");
        $('#ModalConRLName').addClass('border-error');
        $('#ModalConRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlLedgerData.ModalGeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger.");
        $('#ModalGeneralLedgerName').addClass('border-error');
        return false;
    } else {
        $('#ModalGeneralLedgerName').removeClass('border-error');
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

                        if ($.fn.DataTable.isDataTable('#modalSubControlLedgerTable')) {
                            $('#modalSubControlLedgerTable').DataTable().ajax.reload(null, false);
                        }
                        if ($.fn.DataTable.isDataTable('#subControlLedgerTable')) {
                            $('#subControlLedgerTable').DataTable().ajax.reload(null, false);
                        }

                        const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
                        window.dispatchEvent(event);

                        ModalSubControlLedgerFormClear();
                        selectedModalSubControlLedger = [];

                        if (typeof ModalModalLoadSubControlLedgerAll === "function") {
                            ModalModalLoadSubControlLedgerAll();
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
function ModalSubControlLedgerFormClear() {
    ModalisEditModeSubControl = false;

    $("#ModalSubCRLAutoID").val("");

    // Clear all custom dropdowns
    window.clearAllCustomDropdowns('#ModalsubcontrolForm');

    $("#Modalgroupcode, #ModalgroupshortName").val("");
    $("#ModalConRLCode, #ModalCLShortName").val("");
    $("#ModalGeneralLedgerCodeNo, #ModalGeneralLedgerName, #ModalSubCRLshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubControldisplayModifyDate').text('');
    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    $(".ModalSubcontrol-checkbox").prop("checked", false);
    $("#ModalSubControlselectAll").prop("checked", false);
    $('#ModalSubControlsearchInput').val('');

    selectedModalSubControlLedger = [];
    ModalcurrenSubControltPage = 1;
    ModalselectedGroupLedgerCodeForSubControl = "";
    ModalselectedControlLedgerCodeForSubControl = "";

    ModalLoadDropdownGRLCRL();
    ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", "", "");
}
//#endregion

//#region Load All Sub-Control Ledger Data
function ModalLoadSubControlLedgerAll(page = 1, pageSize = ModalSubControlpageSize, sortColumn = ModalSubControlcurrentSortColumn, sortDirection = ModalSubControlcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "") {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#subControlLedgerTable')) {
        $('#subControlLedgerTable').DataTable().ajax.reload(null, false);
        return;
    }
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
                            <input type="checkbox" class="form-check-input form-check-input-sm ModalSubcontrol-checkbox"
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
            $('#ModalSubControltblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#ModalSubControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#ModalSubControlpaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#ModalSubControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                ModalcurrenSubControltPage = currPage;
                ModalSubControlgeneratePageButtons(ModalcurrenSubControltPage, totalPages);
            }

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
$(document).on('click', '.js-subcontrolledger-edit, .subcontrol-ledger', function (e) {
    e.preventDefault();
    const id = parseInt($(this).data('id') || $(this).data('autoid'));
    if (!id) return;

    ModalisEditModeSubControl = true;
    selectedModalSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#ModalSubCRLAutoID').val(id);
                $('#Modalgroupcode').val(data.grlCode || '');
                $('#ModalgroupshortName').val(data.grlShortname || '');

                // Group Ledger
                $('#ModalgroupName').val(data.grlCode);
                window.reinitCustomDropdownById('ModalgroupName');

                // Control Ledger Dropdown
                if (data.grlCode) {
                    ModalLoadControlLedgerDropdown(data.grlCode, data.subControlLedgerCodeNo);
                }

                $('#ModalConRLCode').val(data.subControlLedgerCodeNo || '');
                $('#ModalCLShortName').val(data.crlShortname || '');

                $('#ModalGeneralLedgerCodeNo').val(data.generalLedgerCodeNo || '');
                $('#ModalGeneralLedgerName').val(data.generalLedgerName || '');
                $('#ModalSubCRLshortName').val(data.shortName || '');

                // Dates
                if (data.lDate) {
                    let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubControldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#ModalSubControldisplayLDate').text('');
                }

                if (data.modifyDate) {
                    let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubControldisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#ModalSubControldisplayModifyDate').text('');
                }

                $('.ModalSubcontrol-checkbox').prop('checked', false);
                $("#ModalSubControlselectAll").prop('checked', false);
                $(`.ModalSubcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);
                ModalSubControlupdateCheckboxState();
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
function ModalSubControlLedgerDelete() {
    const ids = selectedModalSubControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

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

            ModalSubControlLedgerFormClear();
            ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
            selectedModalSubControlLedger = [];
            $("#ModalSubControlselectAll").prop("checked", false);

            if (typeof ModalModalLoadSubControlLedgerAll === "function") {
                ModalModalLoadSubControlLedgerAll();
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
function ModalSubControlupdateSortIndicators() {
    $('.ModalSubControlsortable').removeClass('sort-asc sort-desc');
    $('.ModalSubControlsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.ModalSubControlsortable[data-column="${ModalSubControlcurrentSortColumn}"]`);
    header.addClass(ModalSubControlcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalSubControlcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalSubControlgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalSubControlpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalLoadSubControlLedgerAll(page, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, $('#ModalSubControlsearchInput').val().trim(), ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl));

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

function ModalSubControlupdateCheckboxState() {
    $('.ModalSubcontrol-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
        $(this).prop('checked', selectedModalSubControlLedger.includes(ID));
    });
    const allChecked = $('.ModalSubcontrol-checkbox:visible').length > 0 &&
        $('.ModalSubcontrol-checkbox:visible:not(:checked)').length === 0;
    $('#ModalSubControlselectAll').prop('checked', allChecked);
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
    $(document).off('click', '.ModalSubcontrol-checkbox').on('click', '.ModalSubcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalSubControlLedger.includes(id)) selectedModalSubControlLedger.push(id);
        } else {
            const index = selectedModalSubControlLedger.indexOf(id);
            if (index !== -1) selectedModalSubControlLedger.splice(index, 1);
        }

        const allChecked = $('.ModalSubcontrol-checkbox:visible').length > 0 &&
            $('.ModalSubcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#ModalSubControlselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#ModalSubControlselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.ModalSubcontrol-checkbox:visible').prop('checked', isChecked);

    $('.ModalSubcontrol-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedModalSubControlLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedModalSubControlLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedModalSubControlLedger.splice(index, 1);
        }
    });
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ModalLoadDropdownGRLCRL();
    ModalLoadSubControlLedgerAll();
    SubControlbindCheckboxHandlers();

    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#ModalsubcontrolForm", function (e) {
        e.preventDefault();
        ModalSubControlLedgerSaveOrUpdate();
    });
    $(document).on("click", ".js-SubControl-save", function (e) {
        e.preventDefault();
        ModalSubControlLedgerSaveOrUpdate();
    });

    $(document).on("click", "#ModalSubcontroldeleteBtn, .js-SubControl-delete-confirm", function (e) {
        e.preventDefault();
        ModalSubControlLedgerDelete();
    });

    $(document).on("click", ".SubcontrolresetBtn, .js-SubControl-clear", function () {
        ModalSubControlLedgerFormClear();
    });

    $(document).on('input', '.ModalSubControlsearchInput', SubControldebounce(function () {
        const searchValue = $(this).val().trim();
        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, searchValue, ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
    }, 500));

    $(document).on('click', '#ModalSubControlfirstPage', function () {
        ModalcurrenSubControltPage = 1;
        ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
    });

    $(document).on('click', '#ModalSubControlnextPage', function () {
        ModalcurrenSubControltPage++;
        ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
    });

    $(document).on('click', '#ModalSubControlprevPage', function () {
        if (ModalcurrenSubControltPage > 1) {
            ModalcurrenSubControltPage--;
            ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
        }
    });

    $(document).on('click', '#ModalSubControllastPage', function () {
        let lastPage = parseInt($('#ModalSubControlpageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            ModalcurrenSubControltPage = lastPage;
            ModalLoadSubControlLedgerAll(ModalcurrenSubControltPage, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, "", ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
        }
    });

    $(document).on('change', '.ModalSubControlpageSize', function () {
        ModalSubControlpageSize = parseInt($(this).val());
        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection);
    });

    $(document).on('click', '.ModalSubControlsortable', function () {
        const column = $(this).data('column');
        if (column === ModalSubControlcurrentSortColumn) {
            ModalSubControlcurrentSortDirection = (ModalSubControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalSubControlcurrentSortColumn = column;
            ModalSubControlcurrentSortDirection = 'desc';
        }
        ModalLoadSubControlLedgerAll(1, ModalSubControlpageSize, ModalSubControlcurrentSortColumn, ModalSubControlcurrentSortDirection, $('#ModalSubControlsearchInput').val().trim(), ModalselectedGroupLedgerCodeForSubControl, ModalselectedControlLedgerCodeForSubControl);
    });
});
//#endregion
