var selectedModalControlLedger = [];
var isModalControlDeletePending = false;
var modalControlPendingDeleteIds = [];
var ModalisEditMode = false;

//#region Helper: Bangladesh Local Date & Time Formatter
window.getBangladeshDateTime = window.getBangladeshDateTime || function () {
    return new Date().toLocaleString('en-GB', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
    }).replace(',', '').toUpperCase();
};
//#endregion

//#region Error Border Cleanup
document.addEventListener('change', function (e) {
    if (e.target && $(e.target).hasClass('form-select')) {
        $(e.target).removeClass('border-error');
        $(e.target).siblings('.custom-dd-wrapper').removeClass('border-error');
    }
});
//#endregion

//#region Custom Event Listener For Group Ledger Dropdown Updated
window.addEventListener('groupLedgerUpdated', function (e) {
    ModalLoadGroupLedgerDropdown();
    $('#ModalGroupLedgerCodeNo').val('');
    $('#ModalGroupLedgerShortName').val('');
    $('#ModalSubControlLedgerCodeNo').val('');
    reloadModalControlTable();
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
                    },
                    error: function () {
                        console.error("Failed to fetch Group Ledger info For Modal Control Ledger UI");
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown for Modal Control Ledger UI");
        }
    });
}
//#endregion

//#region Table Reload Helper
function reloadModalControlTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#modalControlLedgerTable')) {
        $('#modalControlLedgerTable').DataTable().ajax.reload(null, false);
    }
}

function ModalLoadAllControlLedger() {
    reloadModalControlTable();
}
//#endregion

//#region Selection Helpers
function getSelectedModalControlIds() {
    let ids = [...selectedModalControlLedger];

    $('#modalControlLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    if (ids.length === 0) {
        const formAutoId = parseFloat($('#ModalautoId').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateModalControlCheckAllState() {
    const $checkboxes = $('#modalControlLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#modal-controlledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Modal Control Ledger
function ModalSaveOrUpdateControlLedger() {
    let autoID = $('#ModalautoId').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let ControlLedgerData = {
        autoId: parsedAutoId,
        ControlLedgerCodeNo: $("#ModalGroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#ModalSubControlLedgerCodeNo").val(),
        SubControlLedgerName: ($("#ModalSubControlLedgerName").val() || '').trim(),
        ShortName: ($("#ModalControlShortName").val() || '').trim(),
        GroupLedgerName: $("#ModalGLName").val()
    };

    // Client Validations
    if (!ControlLedgerData.GroupLedgerName) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalGLName').addClass('border-error');
        $('#ModalGLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!ControlLedgerData.SubControlLedgerName) {
        toastr.error("Please Enter Control Ledger Name.");
        $('#ModalSubControlLedgerName').addClass('border-error').focus();
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
                toastr.error(dupResponse.message || "Data Already Exists.");
                return;
            }

            let url = parsedAutoId > 0 ? `/control-ledger/${parsedAutoId}` : '/control-ledger';
            let method = parsedAutoId > 0 ? 'PUT' : 'POST';
            let successMessage = parsedAutoId > 0 ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

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

                        ModalClearControlLedgerForm();
                        selectedModalControlLedger = [];
                        reloadModalControlTable();

                        if (typeof LoadAllControlLedger === "function") {
                            LoadAllControlLedger();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving Control Ledger.");
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

//#region Clear Modal Form
function ModalClearControlLedgerForm() {
    ModalisEditMode = false;

    $("#ModalautoId").val("");
    window.clearAllCustomDropdowns('#ModalcontrolForm');

    $("#ModalGroupLedgerCodeNo, #ModalGroupLedgerShortName").val("");
    $("#ModalSubControlLedgerCodeNo, #ModalSubControlLedgerName, #ModalControlShortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalControldisplayModifyDate').text('');
    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    $('.js-ModalControl-save span').text('Save');

    selectedModalControlLedger = [];
    $('#modalControlLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#modalControlLedgerTable tbody tr').removeClass('selected-row');
    $('#modal-controlledger-check-all').prop('checked', false);

    ModalLoadGroupLedgerDropdown();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-modal-controlledger-edit, .Modalcontrol-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    ModalisEditMode = true;
    selectedModalControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#ModalautoId').val(data.autoId || id);
                $('#ModalGroupLedgerCodeNo').val(data.controlLedgerCodeNo || '');
                $('#ModalGroupLedgerShortName').val(data.groupLedgerShortName || '');
                $('#ModalSubControlLedgerCodeNo').val(data.subControlLedgerCodeNo || '');
                $('#ModalSubControlLedgerName').val(data.subControlLedgerName || '');
                $('#ModalControlShortName').val(data.shortName || '');

                // Dropdown Sync
                $('#ModalGLName').val(data.controlLedgerCodeNo);
                window.reinitCustomDropdownById('ModalGLName');

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

                $('.js-ModalControl-save span').text('Update');

                // Sync checkboxes
                $('#modalControlLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id'));
                    if (rowVal === id) {
                        $(this).prop('checked', true).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false).closest('tr').removeClass('selected-row');
                    }
                });
                updateModalControlCheckAllState();

                $('#ModalSubControlLedgerName').focus();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});
//#endregion

//#region Checkbox Selection Handlers
// Row checkbox
$(document).on('change', '#modalControlLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedModalControlLedger.includes(val)) {
            selectedModalControlLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedModalControlLedger = selectedModalControlLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateModalControlCheckAllState();
});

// Master Checkbox
$(document).on('change', '#modal-controlledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#modalControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedModalControlLedger.includes(val)) {
                selectedModalControlLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedModalControlLedger = selectedModalControlLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#modalControlLedgerTable', function () {
    $('#modalControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (selectedModalControlLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateModalControlCheckAllState();
});
//#endregion

//#region Delete Modal Control Ledger
function ModalDeleteControlLedger(ids) {
    if (!ids || !ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/control-ledger-all-delete',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess === false || response.success === false) {
                toastr.error(response.message || "Failed to delete.");
                return;
            }

            toastr.success(response.message || "Data Deleted Successfully.");

            ModalClearControlLedgerForm();
            selectedModalControlLedger = [];
            reloadModalControlTable();

            if (typeof LoadAllControlLedger === "function") {
                LoadAllControlLedger();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Delete Button in Modal Toolbar
$(document).on("click", "#ModalControldeleteBtn, .js-ModalControl-delete-confirm", function (e) {
    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedModalControlIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    modalControlPendingDeleteIds = ids;
    isModalControlDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            ModalDeleteControlLedger(ids);
        }
    }
});

// Confirm button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isModalControlDeletePending && modalControlPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...modalControlPendingDeleteIds];
        isModalControlDeletePending = false;
        modalControlPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        ModalDeleteControlLedger(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isModalControlDeletePending = false;
    modalControlPendingDeleteIds = [];
});
//#endregion

//#region Ready Part
$(document).ready(function () {
    ModalLoadGroupLedgerDropdown();
    $('#ModalControldisplayLDate').text(getBangladeshDateTime());

    // Save Handlers
    $(document).on("submit", "#ModalcontrolForm", function (e) {
        e.preventDefault();
        ModalSaveOrUpdateControlLedger();
    });

    $(document).on("click", ".js-ModalControl-save", function (e) {
        e.preventDefault();
        ModalSaveOrUpdateControlLedger();
    });

    // Clear Handler
    $(document).on("click", ".js-ModalControl-clear, .ModalControlresetBtn", function (e) {
        e.preventDefault();
        ModalClearControlLedgerForm();
    });
});
//#endregion
