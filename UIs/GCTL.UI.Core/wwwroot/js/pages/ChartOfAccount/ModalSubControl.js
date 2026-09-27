var selectedModalSubControlLedger = [];
var isModalSubControlDeletePending = false;
var modalSubControlPendingDeleteIds = [];
var ModalisEditModeSubControl = false;

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
    ModalLoadDropdownGRLCRL();
    $('#Modalgroupcode, #ModalgroupshortName').val('');
    $('#ModalConRLCode, #ModalCLShortName').val('');
    $('#ModalGeneralLedgerCodeNo').val('');
    reloadModalSubControlTable();
});

window.addEventListener('ControlLedgerUpdated', function (e) {
    const currentGroupCode = $('#ModalgroupName').val();
    if (currentGroupCode) {
        ModalLoadControlLedgerDropdown(currentGroupCode);
    }
    $('#ModalConRLCode, #ModalCLShortName').val('');
    $('#ModalGeneralLedgerCodeNo').val('');
    reloadModalSubControlTable();
});
//#endregion

//#region Dropdown Loaders
function ModalLoadDropdownGRLCRL(selectedId = null) {
    $.ajax({
        url: "/group-ledger-dropdown",
        type: "GET",
        success: function (response) {
            let $select = $('#ModalgroupName');
            $select.find('option:not(:first)').remove();

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    $select.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $select.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalgroupName');

            $select.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#Modalgroupcode, #ModalgroupshortName').val('');
                    $('#ModalConRLCode, #ModalCLShortName').val('');
                    $('#ModalGeneralLedgerCodeNo').val('');
                    let $controlSelect = $('#ModalConRLName');
                    $controlSelect.find('option:not(:first)').remove();
                    $controlSelect.val('');
                    window.reinitCustomDropdownById('ModalConRLName');
                    return;
                }

                $.ajax({
                    url: '/group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#Modalgroupcode').val(data.controlLedgerCodeNo || '');
                        $('#ModalgroupshortName').val(data.shortName || '');
                        ModalLoadControlLedgerDropdown(selectedGroupCode);
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to load Group Ledger dropdown for Modal Sub-Control.");
        }
    });
}

function ModalLoadControlLedgerDropdown(groupCode, selectedControlCode = null) {
    $.ajax({
        url: "/control-ledger-dropdown",
        type: "GET",
        data: { groupcode: groupCode },
        success: function (response) {
            let $select = $('#ModalConRLName');
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

                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControl) {
                    $('#ModalConRLCode, #ModalCLShortName').val('');
                    $('#ModalGeneralLedgerCodeNo').val('');
                    return;
                }

                $.ajax({
                    url: '/control-ledger-info',
                    type: 'GET',
                    data: { contrlcode: selectedControl, controlcode: selectedControl },
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
                    }
                });
            });

            window.reinitCustomDropdownById('ModalConRLName');
        },
        error: function () {
            console.error("Failed to load Control Ledger dropdown for Modal Sub-Control.");
        }
    });
}
//#endregion

//#region Table Reload Helper
function reloadModalSubControlTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#modalSubControlLedgerTable')) {
        $('#modalSubControlLedgerTable').DataTable().ajax.reload(null, false);
    }
}

function ModalLoadSubControlLedgerAll() {
    reloadModalSubControlTable();
}
//#endregion

//#region Selection Helpers
function getSelectedModalSubControlIds() {
    let ids = [...selectedModalSubControlLedger];

    $('#modalSubControlLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    if (ids.length === 0) {
        const formAutoId = parseFloat($('#ModalSubCRLAutoID').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateModalSubControlCheckAllState() {
    const $checkboxes = $('#modalSubControlLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#modal-subcontrolledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Modal Sub-Control Ledger
function ModalSubControlLedgerSaveOrUpdate() {
    let autoID = $('#ModalSubCRLAutoID').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let SubControlData = {
        autoId: parsedAutoId,
        GRLCode: $("#Modalgroupcode").val(),
        SubControlLedgerCodeNo: $("#ModalConRLCode").val(),
        GeneralLedgerCodeNo: $("#ModalGeneralLedgerCodeNo").val(),
        GeneralLedgerName: ($("#ModalGeneralLedgerName").val() || '').trim(),
        ShortName: ($("#ModalSubCRLshortName").val() || '').trim()
    };

    // Client Validations
    if (!$("#ModalgroupName").val()) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalgroupName').addClass('border-error');
        $('#ModalgroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#ModalConRLName").val()) {
        toastr.error("Please Select a Control Ledger.");
        $('#ModalConRLName').addClass('border-error');
        $('#ModalConRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubControlData.GeneralLedgerName) {
        toastr.error("Please Enter Sub-Control Ledger Name.");
        $('#ModalGeneralLedgerName').addClass('border-error').focus();
        return false;
    } else {
        $('#ModalGeneralLedgerName').removeClass('border-error');
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

                        ModalSubControlLedgerFormClear();
                        selectedModalSubControlLedger = [];
                        reloadModalSubControlTable();

                        if (typeof LoadSubControlLedgerAll === "function") {
                            LoadSubControlLedgerAll();
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

//#region Clear Modal Form
function ModalSubControlLedgerFormClear() {
    ModalisEditModeSubControl = false;

    $("#ModalSubCRLAutoID").val("");

    window.clearAllCustomDropdowns('#ModalsubcontrolForm');

    $("#Modalgroupcode, #ModalgroupshortName").val("");
    $("#ModalConRLCode, #ModalCLShortName").val("");
    $("#ModalGeneralLedgerCodeNo, #ModalGeneralLedgerName, #ModalSubCRLshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubControldisplayModifyDate').text('');
    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    $('.js-ModalSubControl-save span').text('Save');

    selectedModalSubControlLedger = [];
    $('#modalSubControlLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#modalSubControlLedgerTable tbody tr').removeClass('selected-row');
    $('#modal-subcontrolledger-check-all').prop('checked', false);

    let $conSelect = $('#ModalConRLName');
    $conSelect.find('option:not(:first)').remove();
    $conSelect.val('');
    window.reinitCustomDropdownById('ModalConRLName');

    ModalLoadDropdownGRLCRL();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-modal-subcontrolledger-edit', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    ModalisEditModeSubControl = true;
    selectedModalSubControlLedger = [id];

    $.ajax({
        url: `/subcontrol-ledger/details/${id}`,
        type: 'GET',
        success: function (data) {
            if (data) {
                $('#ModalSubCRLAutoID').val(data.autoId || id);
                $('#Modalgroupcode').val(data.grlCode || '');
                $('#ModalgroupshortName').val(data.grlShortname || '');

                $('#ModalgroupName').val(data.grlCode);
                window.reinitCustomDropdownById('ModalgroupName');

                if (data.grlCode) {
                    ModalLoadControlLedgerDropdown(data.grlCode, data.subControlLedgerCodeNo);
                }

                $('#ModalConRLCode').val(data.subControlLedgerCodeNo || '');
                $('#ModalCLShortName').val(data.crlShortname || '');

                $('#ModalGeneralLedgerCodeNo').val(data.generalLedgerCodeNo || '');
                $('#ModalGeneralLedgerName').val(data.generalLedgerName || '');
                $('#ModalSubCRLshortName').val(data.shortName || '');

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

                $('.js-ModalSubControl-save span').text('Update');

                $('#modalSubControlLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id'));
                    if (rowVal === id) {
                        $(this).prop('checked', true).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false).closest('tr').removeClass('selected-row');
                    }
                });
                updateModalSubControlCheckAllState();

                $('#ModalGeneralLedgerName').focus();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch Sub-Control Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});
//#endregion

//#region Checkbox Selection Handlers
// Individual row checkbox
$(document).on('change', '#modalSubControlLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedModalSubControlLedger.includes(val)) {
            selectedModalSubControlLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedModalSubControlLedger = selectedModalSubControlLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateModalSubControlCheckAllState();
});

// Master Checkbox
$(document).on('change', '#modal-subcontrolledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#modalSubControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedModalSubControlLedger.includes(val)) {
                selectedModalSubControlLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedModalSubControlLedger = selectedModalSubControlLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#modalSubControlLedgerTable', function () {
    $('#modalSubControlLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id'));
        if (selectedModalSubControlLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateModalSubControlCheckAllState();
});
//#endregion

//#region Delete Modal Sub-Control Ledger
function ModalSubControlLedgerDelete(ids) {
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

            ModalSubControlLedgerFormClear();
            selectedModalSubControlLedger = [];
            reloadModalSubControlTable();

            if (typeof LoadSubControlLedgerAll === "function") {
                LoadSubControlLedgerAll();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#ModalSubcontroldeleteBtn, .js-ModalSubControl-delete-confirm", function (e) {
    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedModalSubControlIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    modalSubControlPendingDeleteIds = ids;
    isModalSubControlDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            ModalSubControlLedgerDelete(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isModalSubControlDeletePending && modalSubControlPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...modalSubControlPendingDeleteIds];
        isModalSubControlDeletePending = false;
        modalSubControlPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        ModalSubControlLedgerDelete(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isModalSubControlDeletePending = false;
    modalSubControlPendingDeleteIds = [];
});
//#endregion

//#region Ready Part
$(document).ready(function () {
    ModalLoadDropdownGRLCRL();
    $('#ModalSubControldisplayLDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#ModalsubcontrolForm", function (e) {
        e.preventDefault();
        ModalSubControlLedgerSaveOrUpdate();
    });

    $(document).on("click", ".js-ModalSubControl-save", function (e) {
        e.preventDefault();
        ModalSubControlLedgerSaveOrUpdate();
    });

    // Clear Button Handler
    $(document).on("click", ".js-ModalSubControl-clear, .ModalSubcontrolresetBtn", function (e) {
        e.preventDefault();
        ModalSubControlLedgerFormClear();
    });
});
//#endregion
