let selectedControlLedger = [];
let currenControltPage = 1;
let ControlpageSize = 10;
let ControlcurrentSortColumn = 'SubControlLedgerCodeNo';
let ControlcurrentSortDirection = 'desc';
let isEditMode = false;
let selectedGroupCodeCL = "";

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
    LoadGroupLedgerDropdown();
    $('#GroupLedgerCodeNo').val('');
    $('#GroupLedgerShortName').val('');
    $('#SubControlLedgerCodeNo').val('');
    LoadAllControlLedger();
});
//#endregion

//#region Group Ledger Dropdown Loader
function LoadGroupLedgerDropdown(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            let $select = $('#GLName');
            $select.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $select.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $select.val(selectedId);
            }

            // Standard Plugin Reinitialization
            window.reinitCustomDropdownById('GLName');

            // Dropdown Change Event
            $select.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                selectedGroupCodeCL = selectedGroupCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#GroupLedgerCodeNo').val('');
                    $('#GroupLedgerShortName').val('');
                    $('#SubControlLedgerCodeNo').val('');
                    LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", "");
                    return;
                }

                // Fetch Group Ledger Details & Next Code
                $.ajax({
                    url: '/group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#GroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
                        $('#GroupLedgerShortName').val(data.shortName || '');

                        if (!isEditMode) {
                            $.ajax({
                                url: '/next-control-ledger-code',
                                type: 'GET',
                                // data: { groupcode: data.controlLedgerCodeNo },
                                data: { groupLedgerCode: data.controlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#SubControlLedgerCodeNo').val(nextCode);
                                }
                            });
                        }

                        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", selectedGroupCodeCL);
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown.");
        }
    });
}
//#endregion

//#region Save Or Update Control Ledger
function SaveOrUpdateControlLedger() {
    let autoID = $('#autoId').val();
    let ControlLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        ControlLedgerCodeNo: $("#GroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#SubControlLedgerCodeNo").val(),
        SubControlLedgerName: $("#SubControlLedgerName").val(),
        ShortName: $("#ControlShortName").val(),
        GroupLedgerName: $("#GLName").val()
    };

    // Client Validations
    if (!ControlLedgerData.GroupLedgerName) {
        toastr.error("Please Select a Group Ledger.");
        $('#GLName').addClass('border-error');
        $('#GLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!ControlLedgerData.SubControlLedgerName) {
        toastr.error("Please Enter Control Ledger Name.");
        $('#SubControlLedgerName').addClass('border-error');
        return false;
    } else {
        $('#SubControlLedgerName').removeClass('border-error');
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

                        const event = new CustomEvent('ControlLedgerUpdated', { detail: { newControlLedgerCodeNo: response.subControlLedgerCodeNo } });
                        window.dispatchEvent(event);

                        ClearControlLedgerForm();
                        selectedControlLedger = [];

                        if (typeof ModalLoadAllControlLedger === "function") {
                            ModalLoadAllControlLedger();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("Error Saving Control Ledger.");
                    console.error('Save Error:', error);
                }
            });
        },
        error: function (xhr, status, error) {
            toastr.error("Server error occurred during duplicate check!");
            console.error('Duplicate Check Error:', error);
        }
    });
}
//#endregion

//#region Clear Form
function ClearControlLedgerForm() {
    isEditMode = false;

    $("#autoId").val("");

    // Standard Clear for custom dropdowns
    window.clearAllCustomDropdowns('#controlForm');

    $("#GroupLedgerCodeNo, #GroupLedgerShortName").val("");
    $("#SubControlLedgerCodeNo, #SubControlLedgerName, #ControlShortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ControlUpdateDate').text('');
    $('#ControlEntryDate').text(getBangladeshDateTime());

    $(".control-checkbox").prop("checked", false);
    $("#ControlselectAll").prop("checked", false);
    $('#ControlsearchInput').val('');

    selectedControlLedger = [];
    currenControltPage = 1;
    selectedGroupCodeCL = "";

    LoadGroupLedgerDropdown();
    LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, '', '');
}
//#endregion

//#region Load All Control Ledger Data
function LoadAllControlLedger(page = 1, pageSize = ControlpageSize, sortColumn = ControlcurrentSortColumn, sortDirection = ControlcurrentSortDirection, searchTerm = "", groupCode = "") {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#controlLedgerTable')) {
        $('#controlLedgerTable').DataTable().ajax.reload(null, false);
        return;
    }
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
                            <input type="checkbox" class="form-check-input form-check-input-sm control-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-link p-0 text-decoration-none control-ledger" data-autoid="${item.autoId}">
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
            $('#controlTblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#ControlpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#ControlpaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#ControltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                currenControltPage = currPage;
                ControlgeneratePageButtons(currenControltPage, totalPages);
            }

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
$(document).on('click', '.js-controlledger-edit, .control-ledger', function (e) {
    e.preventDefault();
    const id = parseInt($(this).data('id') || $(this).data('autoid'));
    if (!id) return;

    isEditMode = true;
    selectedControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#autoId').val(id);
                $('#GroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
                $('#GroupLedgerShortName').val(data.groupLedgerShortName || '');
                $('#SubControlLedgerCodeNo').val(data.subControlLedgerCodeNo || '');
                $('#SubControlLedgerName').val(data.subControlLedgerName || '');
                $('#ControlShortName').val(data.shortName || '');

                // Dropdown Sync
                $('#GLName').val(data.controlLedgerCodeNo);
                window.reinitCustomDropdownById('GLName');

                if (data.lDate) {
                    let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ControlEntryDate').text(entryDate.toUpperCase());
                } else {
                    $('#ControlEntryDate').text('');
                }

                if (data.modifyDate) {
                    let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ControlUpdateDate').text(updateDate.toUpperCase());
                } else {
                    $('#ControlUpdateDate').text('');
                }

                $('.control-checkbox').prop('checked', false);
                $("#ControlselectAll").prop('checked', false);
                $(`.control-checkbox[data-autoid="${id}"]`).prop('checked', true);

                ControlupdateCheckboxState();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});
//#endregion

//#region Delete Control Ledger
function DeleteControlLedger() {
    const ids = selectedControlLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

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
            ClearControlLedgerForm();
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);

            if (typeof ModalLoadAllControlLedger === "function") {
                ModalLoadAllControlLedger();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion

//#region Checkbox & Sorting Helpers
$(document).on('click', '#ControlselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.control-checkbox:visible').prop('checked', isChecked);

    $('.control-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedControlLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedControlLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedControlLedger.splice(index, 1);
        }
    });
});

function ControlbindCheckboxHandlers() {
    $(document).off('click', '.control-checkbox').on('click', '.control-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedControlLedger.includes(id)) selectedControlLedger.push(id);
        } else {
            const index = selectedControlLedger.indexOf(id);
            if (index !== -1) selectedControlLedger.splice(index, 1);
        }

        const allChecked = $('.control-checkbox:visible').length > 0 &&
            $('.control-checkbox:visible:not(:checked)').length === 0;
        $('#ControlselectAll').prop('checked', allChecked);
    });
}

function ControlupdateCheckboxState() {
    $('.control-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
        $(this).prop('checked', selectedControlLedger.includes(ID));
    });
    const allChecked = $('.control-checkbox:visible').length > 0 &&
        $('.control-checkbox:visible:not(:checked)').length === 0;
    $('#ControlselectAll').prop('checked', allChecked);
}

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
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadAllControlLedger(
                page,
                ControlpageSize,
                ControlcurrentSortColumn,
                ControlcurrentSortDirection,
                $('#ControlsearchInput').val().trim(),
                selectedGroupCodeCL
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

function Controldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}
//#endregion

//#region Document Ready
$(document).ready(function () {
    LoadGroupLedgerDropdown();
    LoadAllControlLedger();
    ControlbindCheckboxHandlers();

    $('#ControlEntryDate').text(getBangladeshDateTime());

    $(document).on("submit", "#controlForm", function (e) {
        debugger
        e.preventDefault();
        SaveOrUpdateControlLedger();
    });
    $(document).on("click", ".js-ControlLedger-save", function (e) {
        debugger
        e.preventDefault();
        SaveOrUpdateControlLedger();
    });

    $(document).on("click", "#ControldeleteBtn, .js-ControlLedger-delete-confirm", function (e) {
        e.preventDefault();
        DeleteControlLedger();
    });

    $(document).on("click", "#btnClear, .ControlresetBtn, .js-ControlLedger-clear", function (e) {
        e.preventDefault();
        ClearControlLedgerForm();
    });

    $(document).on('input', '.ControlsearchInput', Controldebounce(function () {
        const searchValue = $(this).val().trim();
        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, searchValue, selectedGroupCodeCL);
    }, 500));

    $(document).on('click', '#ControlfirstPage', function () {
        currenControltPage = 1;
        LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", selectedGroupCodeCL);
    });

    $(document).on('click', '#ControlnextPage', function () {
        currenControltPage++;
        LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", selectedGroupCodeCL);
    });

    $(document).on('click', '#ControlprevPage', function () {
        if (currenControltPage > 1) {
            currenControltPage--;
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", selectedGroupCodeCL);
        }
    });

    $(document).on('click', '#ControllastPage', function () {
        let lastPage = parseInt($('#ControlpageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            currenControltPage = lastPage;
            LoadAllControlLedger(currenControltPage, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, "", selectedGroupCodeCL);
        }
    });

    $(document).on('change', '.ControlListpageSize', function () {
        ControlpageSize = parseInt($(this).val());
        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection);
    });

    $(document).on('click', '.Controlsortable', function () {
        const column = $(this).data('column');
        if (column === ControlcurrentSortColumn) {
            ControlcurrentSortDirection = (ControlcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ControlcurrentSortColumn = column;
            ControlcurrentSortDirection = 'desc';
        }
        LoadAllControlLedger(1, ControlpageSize, ControlcurrentSortColumn, ControlcurrentSortDirection, $('#ControlsearchInput').val().trim(), selectedGroupCodeCL);
    });
});
//#endregion