let selectedModalControlLedger = [];
let ModalcurrenControltPage = 1;
let ModalControlpageSize = 10;
let ModalControlcurrentSortColumn = 'SubControlLedgerCodeNo';
let ModalControlcurrentSortDirection = 'desc';
let ModalisEditMode = false;

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
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    ModalLoadGroupLedgerDropdown();
    $('#ModalGroupLedgerCodeNo').val('');
    $('#ModalGroupLedgerShortName').val('');
    $('#ModalSubControlLedgerCodeNo').val('');
    ModalLoadAllControlLedger();
});
//#endregion

//#region Group Ledger Dropdown For Modal Control Ledger UI
function ModalLoadGroupLedgerDropdown(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            let $dropdown = $('#ModalGLName');
            $dropdown.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalGLName');

            $dropdown.off('change').on('change', function () {
                let selectedCode = $(this).val();
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

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
                        $('#ModalGroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
                        $('#ModalGroupLedgerShortName').val(data.shortName || '');

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
                        console.error("Failed to fetch Group Ledger info For Modal Control Ledger UI");
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown For Modal Control Ledger UI");
        }
    });
}
//#endregion

//#region Save Or Update Control Ledger
function ModalSaveOrUpdateControlLedger() {
    let autoID = $('#ModalautoId').val();
    let ControlLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        ControlLedgerCodeNo: $("#ModalGroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#ModalSubControlLedgerCodeNo").val(),
        SubControlLedgerName: $("#ModalSubControlLedgerName").val(),
        ShortName: $("#ModalControlShortName").val(),
        GroupLedgerName: $("#ModalGLName").val()
    };

    // Validations
    if (!ControlLedgerData.GroupLedgerName) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalGLName').addClass('border-error');
        $('#ModalGLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!ControlLedgerData.SubControlLedgerName) {
        toastr.error("Please Enter Control Ledger.");
        $('#ModalSubControlLedgerName').addClass('border-error');
        return false;
    } else {
        $('#ModalSubControlLedgerName').removeClass('border-error');
    }

    // Check duplicate
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

            let url = autoID ? `/control-ledger/${autoID}` : '/control-ledger';
            let method = autoID ? 'PUT' : 'POST';
            let successMessage = autoID ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(ControlLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        ModalClearControlLedgerForm();
                        selectedModalControlLedger = [];

                        // Reload parent/main control table if exists
                        if (typeof LoadAllControlLedger === "function") {
                            LoadAllControlLedger();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("Error Saving Control Ledger.");
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
    $("#ModalGroupLedgerCodeNo, #ModalGroupLedgerShortName").val("");
    $("#ModalSubControlLedgerCodeNo, #ModalSubControlLedgerName, #ModalControlShortName").val("");

    window.clearAllCustomDropdowns('#ModalcontrolForm');

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalControldisplayModifyDate').text('');
    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    $(".Modalcontrol-checkbox").prop("checked", false);
    $("#ModalControlselectAll").prop("checked", false);
    $('#ModalControlsearchInput').val('');

    selectedModalControlLedger = [];
    ModalcurrenControltPage = 1;

    ModalLoadGroupLedgerDropdown();
    ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, "", "");
}
//#endregion

//#region Load All Control Ledger Data
function ModalLoadAllControlLedger(page = 1, pageSize = ModalControlpageSize, sortColumn = ModalControlcurrentSortColumn, sortDirection = ModalControlcurrentSortDirection, searchTerm = "", groupCode = "") {
    $.ajax({
        url: '/control-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupCode
        },
        success: function (response) {
            let rows = '';
            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center">
                            <input type="checkbox" class="form-check-input form-check-input-sm Modalcontrol-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-link p-0 text-decoration-none Modalcontrol-ledger" data-autoid="${item.autoId}">
                                ${item.subControlLedgerCodeNo}
                            </button>
                        </td>
                        <td>${item.subControlLedgerName || ''}</td>
                        <td>${item.shortName || ''}</td>
                    </tr>`;
                });
            } else {
                rows = `<tr><td colspan="4" class="text-center text-muted">No records found</td></tr>`;
            }
            $('#ModalcontrolTblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#ModalControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#ModalControlpaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#ModalControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                ModalcurrenControltPage = currPage;
                ModalControlgeneratePageButtons(ModalcurrenControltPage, totalPages);
            }

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
    selectedModalControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            $('#ModalautoId').val(id);
            $('#ModalGroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            $('#ModalGroupLedgerShortName').val(data.groupLedgerShortName || '');
            $('#ModalSubControlLedgerCodeNo').val(data.subControlLedgerCodeNo || '');
            $('#ModalSubControlLedgerName').val(data.subControlLedgerName || '');
            $('#ModalControlShortName').val(data.shortName || '');

            // Group Ledger Dropdown
            $('#ModalGLName').val(data.controlLedgerCodeNo);
            window.reinitCustomDropdownById('ModalGLName');

            // Dates
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: 'numeric', minute: '2-digit', hour12: true
                }).replace(',', '');
                $('#ModalControldisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#ModalControldisplayLDate').text('');
            }

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: 'numeric', minute: '2-digit', hour12: true
                }).replace(',', '');
                $('#ModalControldisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#ModalControldisplayModifyDate').text('');
            }

            $('.Modalcontrol-checkbox').prop('checked', false);
            $(`.Modalcontrol-checkbox[data-autoid="${id}"]`).prop('checked', true);
            $("#ModalControlselectAll").prop('checked', false);

            ModalControlupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});
//#endregion

//#region Delete Control Ledger
function ModalDeleteControlLedger() {
    const ids = selectedModalControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/control-ledger-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            ModalClearControlLedgerForm();
            ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
            selectedModalControlLedger = [];
            $("#ModalControlselectAll").prop("checked", false);

            if (typeof LoadAllControlLedger === "function") {
                LoadAllControlLedger();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion

//#region Pagination & Checkbox Helpers
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
        const btn = $('<button type="button">')
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

function ModalControlupdateCheckboxState() {
    $('.Modalcontrol-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
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
    $(document).off('click', '.Modalcontrol-checkbox').on('click', '.Modalcontrol-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalControlLedger.includes(id)) selectedModalControlLedger.push(id);
        } else {
            const index = selectedModalControlLedger.indexOf(id);
            if (index !== -1) selectedModalControlLedger.splice(index, 1);
        }

        const allChecked = $('.Modalcontrol-checkbox:visible').length > 0 &&
            $('.Modalcontrol-checkbox:visible:not(:checked)').length === 0;
        $('#ModalControlselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#ModalControlselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Modalcontrol-checkbox:visible').prop('checked', isChecked);

    $('.Modalcontrol-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedModalControlLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedModalControlLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedModalControlLedger.splice(index, 1);
        }
    });
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ModalLoadGroupLedgerDropdown();
    ModalLoadAllControlLedger();
    ModalControlbindCheckboxHandlers();

    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#ModalcontrolForm", function (e) {
        e.preventDefault();
        ModalSaveOrUpdateControlLedger();
    });

    $(document).on("click", "#ModalControldeleteBtn, .js-ModalControl-delete-confirm", function (e) {
        e.preventDefault();
        ModalDeleteControlLedger();
    });

    $(document).on("click", ".ModalControlresetBtn, .js-ModalControl-clear", function () {
        ModalClearControlLedgerForm();
    });

    $(document).on('input', '.ModalControlsearchInput', ModalControldebounce(function () {
        const searchValue = $(this).val().trim();
        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection, searchValue);
    }, 500));

    $(document).on('click', '#ModalControlfirstPage', function () {
        ModalcurrenControltPage = 1;
        ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

    $(document).on('click', '#ModalControlnextPage', function () {
        ModalcurrenControltPage++;
        ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

    $(document).on('click', '#ModalControlprevPage', function () {
        if (ModalcurrenControltPage > 1) {
            ModalcurrenControltPage--;
            ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
        }
    });

    $(document).on('click', '#ModalControllastPage', function () {
        let lastPage = parseInt($('#ModalControlpageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            ModalcurrenControltPage = lastPage;
            ModalLoadAllControlLedger(ModalcurrenControltPage, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
        }
    });

    $(document).on('change', '.ModalControlListpageSize', function () {
        ModalControlpageSize = parseInt($(this).val());
        ModalLoadAllControlLedger(1, ModalControlpageSize, ModalControlcurrentSortColumn, ModalControlcurrentSortDirection);
    });

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
});
//#endregion