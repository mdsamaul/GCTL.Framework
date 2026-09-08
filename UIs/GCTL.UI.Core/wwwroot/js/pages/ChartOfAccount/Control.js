let selectedControlLedger = [];
let isControlLedgerDeletePending = false;
let controlLedgerPendingDeleteIds = [];
let isEditMode = false;
let selectedGroupCodeCL = "";

//#region Helper: Bangladesh Local Date & Time Formatter
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
//#endregion

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
    reloadControlLedgerTable();
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

            window.reinitCustomDropdownById('GLName');

            $select.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                selectedGroupCodeCL = selectedGroupCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#GroupLedgerCodeNo').val('');
                    $('#GroupLedgerShortName').val('');
                    $('#SubControlLedgerCodeNo').val('');
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
                                data: { groupledgercode: data.controlLedgerCodeNo },
                                success: function (nextCode) {
                                    $('#SubControlLedgerCodeNo').val(nextCode);
                                }
                            });
                        }
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

//#region Table Reload Helper
function reloadControlLedgerTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#controlLedgerTable')) {
        $('#controlLedgerTable').DataTable().ajax.reload(null, false);
    }
}

// Backward-compatible wrapper
function LoadAllControlLedger() {
    reloadControlLedgerTable();
}
//#endregion

//#region Selection Helpers
function getSelectedControlLedgerIds() {
    let ids = [...selectedControlLedger];

    $('#controlLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    // Fallback: If no checkbox selected in grid, check if form is currently editing a record
    if (ids.length === 0) {
        const formAutoId = parseFloat($('#controlAutoId').val() || $('#controlForm [name="autoId"]').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateControlLedgerCheckAllState() {
    const $checkboxes = $('#controlLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#controlledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Control Ledger
function SaveOrUpdateControlLedger() {
    let autoID = $('#controlAutoId').val() || $('#controlForm [name="autoId"]').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let ControlLedgerData = {
        autoId: parsedAutoId,
        ControlLedgerCodeNo: $("#GroupLedgerCodeNo").val(),
        SubControlLedgerCodeNo: $("#SubControlLedgerCodeNo").val(),
        SubControlLedgerName: ($("#SubControlLedgerName").val() || '').trim(),
        ShortName: ($("#ControlShortName").val() || '').trim(),
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
        $('#SubControlLedgerName').addClass('border-error').focus();
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

                        ClearControlLedgerForm();
                        selectedControlLedger = [];
                        reloadControlLedgerTable();

                        if (typeof ModalLoadAllControlLedger === "function") {
                            ModalLoadAllControlLedger();
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

//#region Clear Form
function ClearControlLedgerForm() {
    isEditMode = false;

    $("#controlAutoId, #controlForm [name='autoId']").val("");

    window.clearAllCustomDropdowns('#controlForm');

    $("#GroupLedgerCodeNo, #GroupLedgerShortName").val("");
    $("#SubControlLedgerCodeNo, #SubControlLedgerName, #ControlShortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ControlUpdateDate').text('');
    $('#ControlEntryDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-ControlLedger-save span').text('Save');

    // Reset selections
    selectedControlLedger = [];
    $('#controlLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#controlLedgerTable tbody tr').removeClass('selected-row');
    $('#controlledger-check-all').prop('checked', false);

    selectedGroupCodeCL = "";
    LoadGroupLedgerDropdown();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-controlledger-edit, .control-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    isEditMode = true;
    selectedControlLedger = [id];

    $.ajax({
        url: `/control-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#controlAutoId').val(data.autoId || id);
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

                // Switch save button text to Update
                $('.js-ControlLedger-save span').text('Update');

                // Sync checkboxes in grid
                $('#controlLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id'));
                    if (rowVal === id) {
                        $(this).prop('checked', true);
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false);
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });
                updateControlLedgerCheckAllState();

                // Focus and scroll
                $('#SubControlLedgerName').focus();
                if ($('#controlForm').length) {
                    $('html, body').animate({ scrollTop: $('#controlForm').offset().top - 80 }, 300);
                }
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
// Individual row checkbox
$(document).on('change', '#controlLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedControlLedger.includes(val)) {
            selectedControlLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedControlLedger = selectedControlLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateControlLedgerCheckAllState();
});

// Master Checkbox
$(document).on('change', '#controlledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#controlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedControlLedger.includes(val)) {
                selectedControlLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedControlLedger = selectedControlLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#controlLedgerTable', function () {
    $('#controlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (selectedControlLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateControlLedgerCheckAllState();
});
//#endregion

//#region Delete Control Ledger
function DeleteControlLedger(ids) {
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

            ClearControlLedgerForm();
            selectedControlLedger = [];
            reloadControlLedgerTable();

            if (typeof ModalLoadAllControlLedger === "function") {
                ModalLoadAllControlLedger();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#ControldeleteBtn, .js-ControlLedger-delete-confirm", function (e) {
    const isControlBtn = $(this).hasClass('js-ControlLedger-delete-confirm') ||
                         $(this).closest('#control-tab-pane, #controlForm').length > 0;
    if (!isControlBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedControlLedgerIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    controlLedgerPendingDeleteIds = ids;
    isControlLedgerDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            DeleteControlLedger(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isControlLedgerDeletePending && controlLedgerPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...controlLedgerPendingDeleteIds];
        isControlLedgerDeletePending = false;
        controlLedgerPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        DeleteControlLedger(idsToDelete);
    }
});

// Reset pending on modal hide
$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isControlLedgerDeletePending = false;
    controlLedgerPendingDeleteIds = [];
});
//#endregion

//#region Ready Part
$(document).ready(function () {
    LoadGroupLedgerDropdown();
    $('#ControlEntryDate').text(getBangladeshDateTime());

    // Form submit / Save button
    $(document).on("submit", "#controlForm", function (e) {
        e.preventDefault();
        SaveOrUpdateControlLedger();
    });

    $(document).on("click", ".js-ControlLedger-save", function (e) {
        e.preventDefault();
        SaveOrUpdateControlLedger();
    });

    // Clear button
    $(document).on("click", "#btnClear, .ControlresetBtn, .js-ControlLedger-clear", function (e) {
        const isControlReset = $(this).hasClass('js-ControlLedger-clear') ||
                               $(this).closest('#control-tab-pane, #controlForm').length > 0;
        if (!isControlReset) return;
        e.preventDefault();
        ClearControlLedgerForm();
    });

    // Print button
    $(document).on("click", ".printBtn", function (e) {
        if ($(this).closest('#control-tab-pane, #controlForm').length > 0) {
            e.preventDefault();
            if (typeof printForm === 'function') {
                printForm();
            } else {
                window.print();
            }
        }
    });

    // Favorite button
    $(document).on("click", ".confavBtn", function () {
        if ($(this).closest('#control-tab-pane, #controlForm').length > 0 && typeof toggleFavorite === 'function') {
            toggleFavorite();
        }
    });
});
//#endregion