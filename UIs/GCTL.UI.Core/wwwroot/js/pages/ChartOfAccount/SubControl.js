let selectedSubControlLedger = [];
let isSubControlLedgerDeletePending = false;
let subControlLedgerPendingDeleteIds = [];
let isEditModeSubControl = false;
let selectedGroupLedgerCodeForSubControl = "";
let selectedControlLedgerCodeForSubControl = "";

//#region Helper: Bangladesh Time & Date Formatter
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

document.addEventListener('change', function (e) {
    if (e.target && $(e.target).hasClass('form-select')) {
        $(e.target).removeClass('border-error');
        $(e.target).siblings('.custom-dd-wrapper').removeClass('border-error');
    }
});
//#endregion

//#region Custom Event Listeners
window.addEventListener('groupLedgerUpdated', function (e) {
    LoadDropdownGRLCRL();
    $('#groupcode, #groupshortName').val('');
    $('#ConRLCode, #CLShortName').val('');
    $('#GeneralLedgerCodeNo').val('');
    reloadSubControlLedgerTable();
});

window.addEventListener('ControlLedgerUpdated', function (e) {
    const currentGroupCode = $('#groupName').val();
    if (currentGroupCode) {
        LoadControlLedgerDropdown(currentGroupCode);
    }
    $('#ConRLCode, #CLShortName').val('');
    $('#GeneralLedgerCodeNo').val('');
    reloadSubControlLedgerTable();
});
//#endregion

//#region Dropdown Loaders
function LoadDropdownGRLCRL(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            let $select = $('#groupName');
            $select.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $select.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $select.val(selectedId);
            }

            window.reinitCustomDropdownById('groupName');

            $select.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                selectedGroupLedgerCodeForSubControl = selectedGroupCode || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#groupcode, #groupshortName').val('');
                    $('#ConRLCode, #CLShortName').val('');
                    $('#GeneralLedgerCodeNo').val('');
                    let $controlSelect = $('#ConRLName');
                    $controlSelect.find('option:not(:first)').remove();
                    $controlSelect.val('');
                    window.reinitCustomDropdownById('ConRLName');
                    return;
                }

                // Fetch Group Ledger details
                $.ajax({
                    url: '/group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#groupcode').val(data.controlLedgerCodeNo || '');
                        $('#groupshortName').val(data.shortName || '');
                        LoadControlLedgerDropdown(selectedGroupCode);
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown.");
        }
    });
}

function LoadControlLedgerDropdown(groupCode, selectedControlCode = null) {
    $.ajax({
        url: "/control-ledger-dropdown",
        type: "GET",
        data: { groupcode: groupCode },
        success: function (response) {
            let $select = $('#ConRLName');
            $select.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $select.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedControlCode) {
                $select.val(selectedControlCode);
            }

            $select.off('change').on('change', function () {
                const selectedControl = $(this).val();
                selectedControlLedgerCodeForSubControl = selectedControl || "";

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControl) {
                    $('#ConRLCode, #CLShortName').val('');
                    $('#GeneralLedgerCodeNo').val('');
                    return;
                }

                // Fetch Control Ledger details & next code
                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControl, controlcode: selectedControl },
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
                    }
                });
            });

            window.reinitCustomDropdownById('ConRLName');
        },
        error: function () {
            console.error("Failed to load Control Ledger dropdown.");
        }
    });
}
//#endregion

//#region Table Reload Helper
function reloadSubControlLedgerTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#subControlLedgerTable')) {
        $('#subControlLedgerTable').DataTable().ajax.reload(null, false);
    }
}

function LoadSubControlLedgerAll() {
    reloadSubControlLedgerTable();
}
//#endregion

//#region Selection Helpers
function getSelectedSubControlLedgerIds() {
    let ids = [...selectedSubControlLedger];

    $('#subControlLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    if (ids.length === 0) {
        const formAutoId = parseFloat($('#SubCRLAutoID').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateSubControlCheckAllState() {
    const $checkboxes = $('#subControlLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#subcontrolledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Sub-Control Ledger
function SubControlLedgerSaveOrUpdate() {
    let autoID = $('#SubCRLAutoID').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let SubControlData = {
        autoId: parsedAutoId,
        GRLCode: $("#groupcode").val(),
        SubControlLedgerCodeNo: $("#ConRLCode").val(),
        GeneralLedgerCodeNo: $("#GeneralLedgerCodeNo").val(),
        GeneralLedgerName: ($("#GeneralLedgerName").val() || '').trim(),
        ShortName: ($("#SubCRLshortName").val() || '').trim()
    };

    // Client Validations
    if (!$("#groupName").val()) {
        toastr.error("Please Select a Group Ledger.");
        $('#groupName').addClass('border-error');
        $('#groupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#ConRLName").val()) {
        toastr.error("Please Select a Control Ledger.");
        $('#ConRLName').addClass('border-error');
        $('#ConRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlData.GeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger Name.");
        $('#GeneralLedgerName').addClass('border-error').focus();
        return false;
    } else {
        $('#GeneralLedgerName').removeClass('border-error');
    }

    // Check duplicate
    $.ajax({
        url: '/subcontrol-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(SubControlData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message || "Data Already Exists.");
                return;
            }

            let url = parsedAutoId > 0 ? `/subcontrol-ledger/${parsedAutoId}` : '/subcontrol-ledger';
            let method = parsedAutoId > 0 ? 'PUT' : 'POST';
            let successMessage = parsedAutoId > 0 ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubControlData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
                        window.dispatchEvent(event);

                        SubControlLedgerFormClear();
                        selectedSubControlLedger = [];
                        reloadSubControlLedgerTable();

                        if (typeof ModalLoadSubControlLedgerAll === "function") {
                            ModalLoadSubControlLedgerAll();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving Sub-Control Ledger.");
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
function SubControlLedgerFormClear() {
    isEditModeSubControl = false;

    $("#SubCRLAutoID").val("");

    window.clearAllCustomDropdowns('#subcontrolForm');

    $("#groupcode, #groupshortName").val("");
    $("#ConRLCode, #CLShortName").val("");
    $("#GeneralLedgerCodeNo, #GeneralLedgerName, #SubCRLshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#SubControldisplayModifyDate').text('');
    $('#SubControldisplayLDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-SubControl-save span').text('Save');

    // Reset selections
    selectedSubControlLedger = [];
    $('#subControlLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#subControlLedgerTable tbody tr').removeClass('selected-row');
    $('#subcontrolledger-check-all').prop('checked', false);

    selectedGroupLedgerCodeForSubControl = "";
    selectedControlLedgerCodeForSubControl = "";

    let $conSelect = $('#ConRLName');
    $conSelect.find('option:not(:first)').remove();
    $conSelect.val('');
    window.reinitCustomDropdownById('ConRLName');

    LoadDropdownGRLCRL();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-subcontrolledger-edit, .subcontrol-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    isEditModeSubControl = true;
    selectedSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#SubCRLAutoID').val(data.autoId || id);
                $('#groupcode').val(data.grlCode || '');
                $('#groupshortName').val(data.grlShortname || '');

                // Group Ledger dropdown
                $('#groupName').val(data.grlCode);
                window.reinitCustomDropdownById('groupName');

                // Control Ledger dropdown
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

                // Switch save button to Update
                $('.js-SubControl-save span').text('Update');

                // Sync checkboxes in grid
                $('#subControlLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id'));
                    if (rowVal === id) {
                        $(this).prop('checked', true);
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false);
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });
                updateSubControlCheckAllState();

                // Focus and scroll
                $('#GeneralLedgerName').focus();
                if ($('#subcontrolForm').length) {
                    $('html, body').animate({ scrollTop: $('#subcontrolForm').offset().top - 80 }, 300);
                }
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Sub-Control Ledger Details.");
            console.error("Error Fetching Sub-Control Details:", error);
        }
    });
});
//#endregion

//#region Checkbox Selection Handlers
// Individual row checkbox
$(document).on('change', '#subControlLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedSubControlLedger.includes(val)) {
            selectedSubControlLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedSubControlLedger = selectedSubControlLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateSubControlCheckAllState();
});

// Master Checkbox
$(document).on('change', '#subcontrolledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#subControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedSubControlLedger.includes(val)) {
                selectedSubControlLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedSubControlLedger = selectedSubControlLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#subControlLedgerTable', function () {
    $('#subControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (selectedSubControlLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateSubControlCheckAllState();
});
//#endregion

//#region Delete Sub-Control Ledger
function SubControlLedgerDelete(ids) {
    if (!ids || !ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/subcontrol-ledger-all-delete',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess === false || response.success === false) {
                toastr.error(response.message || "Failed to delete.");
                return;
            }

            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubcontrolLedgerUpdated', { detail: { newSubCRLlLedgerCodeNo: response.generalLedgerCodeNo } });
            window.dispatchEvent(event);

            SubControlLedgerFormClear();
            selectedSubControlLedger = [];
            reloadSubControlLedgerTable();

            if (typeof ModalLoadSubControlLedgerAll === "function") {
                ModalLoadSubControlLedgerAll();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#SubcontroldeleteBtn, .js-SubControl-delete-confirm", function (e) {
    const isSubControlBtn = $(this).hasClass('js-SubControl-delete-confirm') ||
                            $(this).closest('#sub-control-tab-pane, #subcontrolForm').length > 0;
    if (!isSubControlBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedSubControlLedgerIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    subControlLedgerPendingDeleteIds = ids;
    isSubControlLedgerDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            SubControlLedgerDelete(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isSubControlLedgerDeletePending && subControlLedgerPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...subControlLedgerPendingDeleteIds];
        isSubControlLedgerDeletePending = false;
        subControlLedgerPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        SubControlLedgerDelete(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isSubControlLedgerDeletePending = false;
    subControlLedgerPendingDeleteIds = [];
});
//#endregion

//#region Ready Part
$(document).ready(function () {
    LoadDropdownGRLCRL();
    $('#SubControldisplayLDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#subcontrolForm", function (e) {
        e.preventDefault();
        SubControlLedgerSaveOrUpdate();
    });

    $(document).on("click", ".js-SubControl-save", function (e) {
        e.preventDefault();
        SubControlLedgerSaveOrUpdate();
    });

    // Clear Button Handler
    $(document).on("click", ".SubcontrolresetBtn, .js-SubControl-clear", function (e) {
        const isSubControlReset = $(this).hasClass('js-SubControl-clear') ||
                                  $(this).closest('#sub-control-tab-pane, #subcontrolForm').length > 0;
        if (!isSubControlReset) return;
        e.preventDefault();
        SubControlLedgerFormClear();
    });

    // Print Button
    $(document).on("click", ".printBtn", function (e) {
        if ($(this).closest('#sub-control-tab-pane, #subcontrolForm').length > 0) {
            e.preventDefault();
            if (typeof printForm === 'function') {
                printForm();
            } else {
                window.print();
            }
        }
    });

    // Favorite Button
    $(document).on("click", ".subfavBtn", function () {
        if ($(this).closest('#sub-control-tab-pane, #subcontrolForm').length > 0 && typeof toggleFavorite === 'function') {
            toggleFavorite();
        }
    });
});
//#endregion