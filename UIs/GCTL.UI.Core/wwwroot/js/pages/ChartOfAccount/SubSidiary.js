let selectedSubSidiaryLedger = [];
let isSubSidiaryLedgerDeletePending = false;
let subSidiaryLedgerPendingDeleteIds = [];
let isEditModeSubSidiary = false;

// Selected dropdown values
let selectedGroupCodeSSL = "";
let selectedControlCodeSSL = "";
let selectedSubControlCodeSSL = "";

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
    ForSubSidiGRLDropdown();
    $('#forsidiarygroupcode, #forsidiarygroupshortName').val('');
    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');
    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
    $('#SusidiaryLedgerCodeNo').val('');
    reloadSubSidiaryLedgerTable();
});

window.addEventListener('ControlLedgerUpdated', function (e) {
    const currentGroupCode = $('#forsidiarygroupName').val();
    if (currentGroupCode) {
        getControlLedger(currentGroupCode);
    }
    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');
    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
    $('#SusidiaryLedgerCodeNo').val('');
    reloadSubSidiaryLedgerTable();
});

window.addEventListener('SubcontrolLedgerUpdated', function (e) {
    const currentControlCode = $('#forsidiaryControlName').val();
    if (currentControlCode) {
        getSubControllerLedger(currentControlCode);
    }
    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
    $('#SusidiaryLedgerCodeNo').val('');
    reloadSubSidiaryLedgerTable();
});
//#endregion

//#region Load Cascading Dropdowns

// 1. Group Ledger Dropdown
function ForSubSidiGRLDropdown(selectedId = null) {
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            let $dropdown = $('#forsidiarygroupName');
            $dropdown.find('option:not(:first)').remove();

            if (groupResponse && groupResponse.data) {
                $.each(groupResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('forsidiarygroupName');

            $dropdown.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                selectedGroupCodeSSL = selectedGroupCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#forsidiarygroupcode, #forsidiarygroupshortName').val('');
                    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
                    $('#SusidiaryLedgerCodeNo').val('');

                    let $ctrl = $('#forsidiaryControlName');
                    $ctrl.find('option:not(:first)').remove();
                    $ctrl.val('');
                    window.reinitCustomDropdownById('forsidiaryControlName');

                    let $subctrl = $('#forsidiaryGeneralLedgerName');
                    $subctrl.find('option:not(:first)').remove();
                    $subctrl.val('');
                    window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');
                    return;
                }

                // Clear downstream fields
                $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');
                $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
                $('#SusidiaryLedgerCodeNo').val('');

                let $subctrl = $('#forsidiaryGeneralLedgerName');
                $subctrl.find('option:not(:first)').remove();
                $subctrl.val('');
                window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');

                // Load Control Ledger Dropdown
                getControlLedger(selectedGroupCode);

                // Fetch Group Info
                $.ajax({
                    url: '/forsubsidiary-group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#forsidiarygroupcode').val(data.controlLedgerCodeNo || '');
                        $('#forsidiarygroupshortName').val(data.shortName || '');
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to fetch Group Ledger dropdown for Sub-Sidiary.");
        }
    });
}

// 2. Control Ledger Dropdown
function getControlLedger(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-dropdown',
        type: 'GET',
        data: { groupcode: selectedGroupCode },
        success: function (controlResponse) {
            let $dropdown = $('#forsidiaryControlName');
            $dropdown.find('option:not(:first)').remove();

            if (controlResponse && controlResponse.data) {
                $.each(controlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            $dropdown.off('change').on('change', function () {
                const selectedControlCode = $(this).val();
                selectedControlCodeSSL = selectedControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
                    $('#SusidiaryLedgerCodeNo').val('');

                    let $subctrl = $('#forsidiaryGeneralLedgerName');
                    $subctrl.find('option:not(:first)').remove();
                    $subctrl.val('');
                    window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');
                    return;
                }

                // Clear downstream fields
                $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName').val('');
                $('#SusidiaryLedgerCodeNo').val('');

                // Load Sub-Control Dropdown
                getSubControllerLedger(selectedControlCode);

                // Fetch Control Info
                GetControlLedgerInfo(selectedControlCode);
            });

            window.reinitCustomDropdownById('forsidiaryControlName');
        },
        error: function () {
            console.error("Failed to fetch Control Ledger dropdown for Sub-Sidiary.");
        }
    });
}

function GetControlLedgerInfo(selectedControlCode) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-info',
        type: 'GET',
        data: { contrlcode: selectedControlCode, controlcode: selectedControlCode },
        success: function (controlInfo) {
            $('#forsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo || '');
            $('#forsidiaryCLShortName').val(controlInfo.shortName || '');
        }
    });
}

// 3. Sub-Control Ledger Dropdown
function getSubControllerLedger(selectedControlCode, selectedId = null) {
    $.ajax({
        url: '/subcontrol-ledger-dropdown',
        type: 'GET',
        data: { controlcode: selectedControlCode },
        success: function (subControlResponse) {
            let $dropdown = $('#forsidiaryGeneralLedgerName');
            $dropdown.find('option:not(:first)').remove();

            if (subControlResponse && subControlResponse.data) {
                $.each(subControlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            $dropdown.off('change').on('change', function () {
                const selectedSubControlCode = $(this).val();
                selectedSubControlCodeSSL = selectedSubControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSubControlCode) {
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName, #SusidiaryLedgerCodeNo').val('');
                    return;
                }

                // Fetch Sub-Control Info & Next Code
                GetSubcontrolInfo(selectedSubControlCode);
            });

            window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');
        },
        error: function () {
            console.error("Failed to fetch Sub-Control Ledger dropdown for Sub-Sidiary.");
        }
    });
}

function GetSubcontrolInfo(selectedSubControlCode) {
    $.ajax({
        url: '/forsudsidiary-subcontrol-ledger-info',
        type: 'GET',
        data: { subcontrlcode: selectedSubControlCode, subcontrolcode: selectedSubControlCode },
        success: function (subControlInfo) {
            $('#forsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo || '');
            $('#forsidiarySubCRLshortName').val(subControlInfo.shortName || '');

            if (!isEditModeSubSidiary) {
                $.ajax({
                    url: '/next-subsidiary-ledger-code',
                    type: 'GET',
                    data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                    success: function (nextCode) {
                        $('#SusidiaryLedgerCodeNo').val(nextCode);
                    }
                });
            }
        }
    });
}
//#endregion

//#region Table Reload Helper
function reloadSubSidiaryLedgerTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#subSidiaryLedgerTable')) {
        $('#subSidiaryLedgerTable').DataTable().ajax.reload(null, false);
    }
}

function SubSididiarLedgerAll() {
    reloadSubSidiaryLedgerTable();
}
//#endregion

//#region Selection Helpers
function getSelectedSubSidiaryLedgerIds() {
    let ids = [...selectedSubSidiaryLedger];

    $('#subSidiaryLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id') || $(this).data('autoid');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    if (ids.length === 0) {
        const formAutoId = parseFloat($('#forsidiaryAutoID').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateSubSidiaryCheckAllState() {
    const $checkboxes = $('#subSidiaryLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#subsidiaryledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Sub-Sidiary Ledger
function SubSidiaryLedgerSaveOrUpdate() {
    let autoID = $('#forsidiaryAutoID').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let SubSidiaryLedgerData = {
        autoId: parsedAutoId,
        GLCode: $("#forsidiarygroupcode").val() || $("#forsidiarygroupName").val(),
        CRLCode: $("#forsidiaryControlCode").val() || $("#forsidiaryControlName").val(),
        GeneralLedgerCodeNo: $("#forsidiaryGeneralLedgerCodeNo").val() || $("#forsidiaryGeneralLedgerName").val(),
        SusidiaryLedgerCodeNo: $("#SusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: ($("#SubsidiaryLedgerName").val() || '').trim(),
        ShortName: ($("#forsidiaryshortName").val() || '').trim()
    };

    // Validations
    if (!$("#forsidiarygroupName").val()) {
        toastr.error("Please Select a Group Ledger.");
        $('#forsidiarygroupName').addClass('border-error');
        $('#forsidiarygroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#forsidiaryControlName").val()) {
        toastr.error("Please Select a Control Ledger.");
        $('#forsidiaryControlName').addClass('border-error');
        $('#forsidiaryControlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#forsidiaryGeneralLedgerName").val()) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#forsidiaryGeneralLedgerName').addClass('border-error');
        $('#forsidiaryGeneralLedgerName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger Name.");
        $('#SubsidiaryLedgerName').addClass('border-error').focus();
        return false;
    } else {
        $('#SubsidiaryLedgerName').removeClass('border-error');
    }

    // Check duplicate
    $.ajax({
        url: '/subsidiary-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(SubSidiaryLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message || "Data Already Exists.");
                return;
            }

            let url = parsedAutoId > 0 ? `/subsidiary-ledger/${parsedAutoId}` : '/subsidiary-ledger';
            let method = parsedAutoId > 0 ? 'PUT' : 'POST';
            let successMessage = parsedAutoId > 0 ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubSidiaryLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
                        window.dispatchEvent(event);

                        SubSidiaryLedgerFormClear();
                        selectedSubSidiaryLedger = [];
                        reloadSubSidiaryLedgerTable();

                        if (typeof ModalSubSididiarLedgerAll === "function") {
                            ModalSubSididiarLedgerAll();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving Sub-Sidiary Ledger.");
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

//#region Clear Form
function SubSidiaryLedgerFormClear() {
    isEditModeSubSidiary = false;

    $("#forsidiaryAutoID").val("");

    window.clearAllCustomDropdowns('#subSidiaryForm');

    $("#forsidiarygroupcode, #forsidiarygroupshortName").val("");
    $("#forsidiaryControlCode, #forsidiaryCLShortName").val("");
    $("#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName").val("");
    $("#SusidiaryLedgerCodeNo, #SubsidiaryLedgerName, #forsidiaryshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#UpdateDate, #SubSidiarydisplayModifyDate').text('');
    $('#EntryDate, #SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-SubSidiary-save span').text('Save');

    // Reset selections
    selectedSubSidiaryLedger = [];
    $('#subSidiaryLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#subSidiaryLedgerTable tbody tr').removeClass('selected-row');
    $('#subsidiaryledger-check-all').prop('checked', false);

    selectedGroupCodeSSL = "";
    selectedControlCodeSSL = "";
    selectedSubControlCodeSSL = "";

    let $ctrlSelect = $('#forsidiaryControlName');
    $ctrlSelect.find('option:not(:first)').remove();
    $ctrlSelect.val('');
    window.reinitCustomDropdownById('forsidiaryControlName');

    let $subctrlSelect = $('#forsidiaryGeneralLedgerName');
    $subctrlSelect.find('option:not(:first)').remove();
    $subctrlSelect.val('');
    window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');

    ForSubSidiGRLDropdown();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-subsidiaryledger-edit, .subsidiary-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    isEditModeSubSidiary = true;
    selectedSubSidiaryLedger = [id];

    $.ajax({
        url: `/subsidiary-ledger/details/${id}`,
        type: 'GET',
        success: function (response) {
            if (response) {
                $('#forsidiaryAutoID').val(response.autoId || id);
                $('#SusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                $('#SubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                $('#forsidiaryshortName').val(response.shortName || '');

                // Group Ledger
                $('#forsidiarygroupcode').val(response.glCode || '');
                $('#forsidiarygroupshortName').val(response.glShortName || '');
                $('#forsidiarygroupName').val(response.glCode);
                window.reinitCustomDropdownById('forsidiarygroupName');

                // Control Ledger
                if (response.glCode) {
                    getControlLedger(response.glCode, response.crlCode);
                }
                $('#forsidiaryControlCode').val(response.crlCode || '');
                $('#forsidiaryCLShortName').val(response.crLShortName || '');

                // Sub-Control Ledger
                if (response.crlCode) {
                    getSubControllerLedger(response.crlCode, response.generalLedgerCodeNo);
                }
                $('#forsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                $('#forsidiarySubCRLshortName').val(response.subCRLShortName || '');

                // Dates
                if (response.lDate) {
                    let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#EntryDate, #SubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#EntryDate, #SubSidiaryldisplayLDate').text('');
                }

                if (response.modifyDate) {
                    let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#UpdateDate, #SubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#UpdateDate, #SubSidiarydisplayModifyDate').text('');
                }

                // Switch save button to Update
                $('.js-SubSidiary-save span').text('Update');

                // Sync checkboxes in grid
                $('#subSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
                    if (rowVal === id) {
                        $(this).prop('checked', true);
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false);
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });
                updateSubSidiaryCheckAllState();

                // Focus and scroll
                $('#SubsidiaryLedgerName').focus();
                if ($('#subSidiaryForm').length) {
                    $('html, body').animate({ scrollTop: $('#subSidiaryForm').offset().top - 80 }, 300);
                }
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
//#endregion

//#region Checkbox Selection Handlers
// Individual row checkbox
$(document).on('change', '#subSidiaryLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedSubSidiaryLedger.includes(val)) {
            selectedSubSidiaryLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedSubSidiaryLedger = selectedSubSidiaryLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateSubSidiaryCheckAllState();
});

// Master Checkbox
$(document).on('change', '#subsidiaryledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#subSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedSubSidiaryLedger.includes(val)) {
                selectedSubSidiaryLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedSubSidiaryLedger = selectedSubSidiaryLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#subSidiaryLedgerTable', function () {
    $('#subSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (selectedSubSidiaryLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateSubSidiaryCheckAllState();
});
//#endregion

//#region Delete Sub-Sidiary Ledger
function SubSidiaryLedgerDelete(ids) {
    if (!ids || !ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/subsidiary-ledger-all-delete',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess === false || response.success === false) {
                toastr.error(response.message || "Failed to delete.");
                return;
            }

            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
            window.dispatchEvent(event);

            SubSidiaryLedgerFormClear();
            selectedSubSidiaryLedger = [];
            reloadSubSidiaryLedgerTable();

            if (typeof ModalSubSididiarLedgerAll === "function") {
                ModalSubSididiarLedgerAll();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#SubSidiarydeleteBtn, .js-SubSidiary-delete-confirm", function (e) {
    const isSubSidiaryBtn = $(this).hasClass('js-SubSidiary-delete-confirm') ||
                            $(this).closest('#subsidiary-tab-pane, #subSidiaryForm').length > 0;
    if (!isSubSidiaryBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedSubSidiaryLedgerIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    subSidiaryLedgerPendingDeleteIds = ids;
    isSubSidiaryLedgerDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            SubSidiaryLedgerDelete(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isSubSidiaryLedgerDeletePending && subSidiaryLedgerPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...subSidiaryLedgerPendingDeleteIds];
        isSubSidiaryLedgerDeletePending = false;
        subSidiaryLedgerPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        SubSidiaryLedgerDelete(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isSubSidiaryLedgerDeletePending = false;
    subSidiaryLedgerPendingDeleteIds = [];
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ForSubSidiGRLDropdown();
    $('#EntryDate, #SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#subSidiaryForm", function (e) {
        e.preventDefault();
        SubSidiaryLedgerSaveOrUpdate();
    });

    $(document).on("click", ".js-SubSidiary-save", function (e) {
        e.preventDefault();
        SubSidiaryLedgerSaveOrUpdate();
    });

    // Clear Button Handler
    $(document).on("click", ".SubSidiaryresetBtn, .js-SubSidiary-clear", function (e) {
        const isSubSidiaryReset = $(this).hasClass('js-SubSidiary-clear') ||
                                  $(this).closest('#subsidiary-tab-pane, #subSidiaryForm').length > 0;
        if (!isSubSidiaryReset) return;
        e.preventDefault();
        SubSidiaryLedgerFormClear();
    });

    // Print Button
    $(document).on("click", ".printBtn", function (e) {
        if ($(this).closest('#subsidiary-tab-pane, #subSidiaryForm').length > 0) {
            e.preventDefault();
            if (typeof printForm === 'function') {
                printForm();
            } else {
                window.print();
            }
        }
    });

    // Favorite Button
    $(document).on("click", ".subSidiaryfavBtn", function () {
        if ($(this).closest('#subsidiary-tab-pane, #subSidiaryForm').length > 0 && typeof toggleFavorite === 'function') {
            toggleFavorite();
        }
    });
});
//#endregion