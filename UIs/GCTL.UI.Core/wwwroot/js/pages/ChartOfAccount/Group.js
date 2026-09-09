let selectedGroupLedger = [];
let isGroupLedgerDeletePending = false;
let groupLedgerPendingDeleteIds = [];

//#region Helper: Bangladesh Time & Date
function getBangladeshDateTime() {
    let now = new Date();
    let utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    let bdOffset = 6 * 60 * 60 * 1000;
    let bdTime = new Date(utc + bdOffset);

    let day = bdTime.getDate().toString().padStart(2, '0');
    let month = (bdTime.getMonth() + 1).toString().padStart(2, '0');
    let year = bdTime.getFullYear();

    let hours = bdTime.getHours();
    let minutes = bdTime.getMinutes().toString().padStart(2, '0');
    let ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12;

    return `${day}/${month}/${year} ${hours}:${minutes} ${ampm}`;
}
//#endregion

//#region Validation Error Border
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('border-error');
    }
});
//#endregion

//#region Group Ledger Next Code
function LoadNextGroupLedgerCode() {
    $.ajax({
        url: '/next-group-ledger-code',
        method: 'GET',
        success: function (response) {
            $('#ControlLedgerCodeNo').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed To Load Next Group Ledger Code: ", error);
        }
    });
}
//#endregion

//#region Grid Reload Helper
function reloadGroupLedgerTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#groupLedgerTable')) {
        $('#groupLedgerTable').DataTable().ajax.reload(null, false);
    }
}

// Backward-compatible wrapper
function LoadGroupLedger() {
    reloadGroupLedgerTable();
}
//#endregion

//#region Selection Helpers
function getSelectedGroupLedgerIds() {
    let ids = [...selectedGroupLedger];

    $('#groupLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    // Fallback: If no checkbox selected in grid, check if form is currently editing a record
    if (ids.length === 0) {
        const formAutoId = parseFloat($('#autoId').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateGroupLedgerCheckAllState() {
    const $checkboxes = $('#groupLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#groupledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Group Ledger
function SaveOrUpdateGroupLedger() {
    let autoID = $('#autoId').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let GroupLedgerData = {
        autoId: parsedAutoId,
        ControlLedgerCodeNo: $("#ControlLedgerCodeNo").val(),
        ControlLedgerName: ($("#ControlLedgerName").val() || '').trim(),
        ShortName: ($("#ShortName").val() || '').trim()
    };

    // Validation
    if (!GroupLedgerData.ControlLedgerName) {
        toastr.error("Please Enter Group Ledger Name.");
        $('#ControlLedgerName').addClass('border-error').focus();
        return false;
    } else {
        $('#ControlLedgerName').removeClass('border-error');
    }

    // Check for duplicate
    $.ajax({
        url: '/group-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(GroupLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message || "Data Already Exists.");
                return;
            }

            let url = '/group-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';

            if (parsedAutoId > 0) {
                url = `/group-ledger/${parsedAutoId}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(GroupLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        // Custom Event For Update Group Ledger Dropdown in Control Ledger UI
                        const event = new CustomEvent('groupLedgerUpdated', { detail: { newControlLedgerCodeNo: response.controlLedgerCodeNo } });
                        window.dispatchEvent(event);

                        ClearGroupLedgerForm();
                        selectedGroupLedger = [];
                        reloadGroupLedgerTable();

                        // Modal Reload if modal scripts loaded
                        if (typeof ModalLoadGroupLedger === 'function') {
                            ModalLoadGroupLedger(1, typeof ModalpageSize !== 'undefined' ? ModalpageSize : 10, typeof ModalcurrentSortColumn !== 'undefined' ? ModalcurrentSortColumn : 'ControlLedgerCodeNo', typeof ModalcurrentSortDirection !== 'undefined' ? ModalcurrentSortDirection : 'desc');
                        }
                        if (typeof ModalLoadNextGroupLedgerCode === 'function') {
                            ModalLoadNextGroupLedgerCode();
                        }
                        if (typeof ModalSubSidiaryForGeneralUI === 'function') {
                            ModalSubSidiaryForGeneralUI();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Group Ledger:', response);
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving Group Ledger.");
                    console.error('Error Saving Group Ledger:', error);
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

//#region Group Ledger Clear
function ClearGroupLedgerForm() {
    $("#autoId").val("");
    $("#ControlLedgerCodeNo").val("");
    $("#ControlLedgerName").val("");
    $("#ShortName").val("");
    $(".form-control").removeClass("is-invalid is-valid border-error");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#UpdateDate').text('');
    $('#EntryDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-GroupLedger-save span').text('Save');

    // Reset selections
    selectedGroupLedger = [];
    $('#groupLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#groupLedgerTable tbody tr').removeClass('selected-row');
    $('#groupledger-check-all').prop('checked', false);

    LoadNextGroupLedgerCode();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-groupledger-edit, .group-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);

    $.ajax({
        url: `/group-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (!data) {
                toastr.error("No record found.");
                return;
            }

            // Populate form fields
            $('#autoId').val(data.autoId || id);
            $('#ControlLedgerCodeNo').val(data.controlLedgerCodeNo || '');
            $('#ControlLedgerName').val(data.controlLedgerName || '');
            $('#ShortName').val(data.shortName || '');

            // Format Entry Date
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#EntryDate').text(entryDate.toUpperCase());
            } else {
                $('#EntryDate').text('');
            }

            // Format Update Date
            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#UpdateDate').text(updateDate.toUpperCase());
            } else {
                $('#UpdateDate').text('');
            }

            // Switch save button text to Update
            $('.js-GroupLedger-save span').text('Update');

            // Select edited row
            selectedGroupLedger = [id];

            $('#groupLedgerTable tbody input[type="checkbox"]').each(function () {
                const val = parseFloat($(this).val() || $(this).data('id'));
                if (val === id) {
                    $(this).prop('checked', true);
                    $(this).closest('tr').addClass('selected-row');
                } else {
                    $(this).prop('checked', false);
                    $(this).closest('tr').removeClass('selected-row');
                }
            });
            updateGroupLedgerCheckAllState();

            // Focus and scroll to form
            $('#ControlLedgerName').focus();
            if ($('#groupForm').length) {
                $('html, body').animate({ scrollTop: $('#groupForm').offset().top - 80 }, 300);
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Group Ledger details.");
            console.error("Error fetching details:", error);
        }
    });
});
//#endregion

//#region Checkbox Selection Handlers
// Individual row checkbox
$(document).on('change', '#groupLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedGroupLedger.includes(val)) {
            selectedGroupLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedGroupLedger = selectedGroupLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateGroupLedgerCheckAllState();
});

// Master Checkbox
$(document).on('change', '#groupledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#groupLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedGroupLedger.includes(val)) {
                selectedGroupLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedGroupLedger = selectedGroupLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event (Keep selections on page change/sort/search)
$(document).on('draw.dt', '#groupLedgerTable', function () {
    $('#groupLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (selectedGroupLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateGroupLedgerCheckAllState();
});
//#endregion

//#region Delete Functionality
function DeleteGroupLedger(ids) {
    if (!ids || !ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/group-ledger-all-delete',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess === false || response.success === false) {
                toastr.error(response.message || "Failed to delete.");
                return;
            }

            toastr.success(response.message || "Data Deleted Successfully.");

            // Custom Event For Update Group Ledger Dropdown in Control Ledger UI
            const event = new CustomEvent('groupLedgerUpdated', { detail: { newControlLedgerCodeNo: response.controlLedgerCodeNo } });
            window.dispatchEvent(event);

            // Reset form, selection, and reload table
            ClearGroupLedgerForm();
            selectedGroupLedger = [];
            reloadGroupLedgerTable();

            // Group Modal Table Reload if modal exists
            if (typeof ModalLoadGroupLedger === 'function') {
                ModalLoadGroupLedger(1, typeof ModalpageSize !== 'undefined' ? ModalpageSize : 10, typeof ModalcurrentSortColumn !== 'undefined' ? ModalcurrentSortColumn : 'ControlLedgerCodeNo', typeof ModalcurrentSortDirection !== 'undefined' ? ModalcurrentSortDirection : 'desc');
            }
            if (typeof ModalLoadNextGroupLedgerCode === 'function') {
                ModalLoadNextGroupLedgerCode();
            }
            if (typeof ModalLoadDropdownGRLCRL === 'function') {
                ModalLoadDropdownGRLCRL();
            }
            if (typeof ModalSubSidiaryForGeneralUI === 'function') {
                ModalSubSidiaryForGeneralUI();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", ".js-GroupLedger-delete-confirm, #deleteBtn", function (e) {
    const isGroupBtn = $(this).hasClass('js-GroupLedger-delete-confirm') ||
                       $(this).closest('#group-tab-pane, #groupForm').length > 0;
    if (!isGroupBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedGroupLedgerIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    groupLedgerPendingDeleteIds = ids;
    isGroupLedgerDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            DeleteGroupLedger(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isGroupLedgerDeletePending && groupLedgerPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...groupLedgerPendingDeleteIds];
        isGroupLedgerDeletePending = false;
        groupLedgerPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        DeleteGroupLedger(idsToDelete);
    }
});

// Reset pending on modal hide
$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isGroupLedgerDeletePending = false;
    groupLedgerPendingDeleteIds = [];
});
//#endregion

//#region Ready Part
$(document).ready(function () {
    LoadNextGroupLedgerCode();
    $('#EntryDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#groupForm", function (e) {
        e.preventDefault();
        SaveOrUpdateGroupLedger();
    });

    $(document).on("click", ".js-GroupLedger-save", function (e) {
        e.preventDefault();
        SaveOrUpdateGroupLedger();
    });

    // Clear Button Handlers
    $(document).on("click", ".js-GroupLedger-clear, .resetBtn", function (e) {
        const isGroupReset = $(this).hasClass('js-GroupLedger-clear') ||
                             $(this).closest('#group-tab-pane, #groupForm').length > 0;
        if (!isGroupReset) return;
        e.preventDefault();
        ClearGroupLedgerForm();
    });

    // Print Button
    $(document).on("click", ".printBtn", function (e) {
        if ($(this).closest('#group-tab-pane, #groupForm').length > 0) {
            e.preventDefault();
            if (typeof printForm === 'function') {
                printForm();
            } else {
                window.print();
            }
        }
    });

    // Favorite Button
    $(document).on("click", ".favBtn", function () {
        if ($(this).closest('#group-tab-pane, #groupForm').length > 0 && typeof toggleFavorite === 'function') {
            toggleFavorite();
        }
    });
});
//#endregion
