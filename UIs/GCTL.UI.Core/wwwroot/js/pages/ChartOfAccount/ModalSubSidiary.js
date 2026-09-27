var selectedModalSubSidiaryLedger = [];
var isModalSubSidiaryDeletePending = false;
var modalSubSidiaryPendingDeleteIds = [];
var ModalisEditModeSubSidiary = false;

// Selected dropdown values
var ModalselectedGroupCodeSSL = "";
var ModalselectedControlCodeSSL = "";
var ModalselectedSubControlCodeSSL = "";

//#region Helper: Bangladesh Local Date & Time Formatter
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
    ModalSubSidiaryForGeneralUI();
    $("#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName").val("");
    $("#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName").val("");
    $("#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName").val("");
    $("#ModalSusidiaryLedgerCodeNo").val("");
    reloadModalSubSidiaryTable();
});

window.addEventListener('ControlLedgerUpdated', function (e) {
    const currentGroupCode = $('#ModalforsidiarygroupName').val();
    if (currentGroupCode) {
        ModalgetControlLedger(currentGroupCode);
    }
    $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');
    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
    $('#ModalSusidiaryLedgerCodeNo').val('');
    reloadModalSubSidiaryTable();
});

window.addEventListener('SubcontrolLedgerUpdated', function (e) {
    const currentControlCode = $('#ModalforsidiaryControlName').val();
    if (currentControlCode) {
        ModalgetSubControllerLedger(currentControlCode);
    }
    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
    $('#ModalSusidiaryLedgerCodeNo').val('');
    reloadModalSubSidiaryTable();
});
//#endregion

//#region Load Cascading Dropdowns

// 1. Group Ledger Dropdown
function ModalSubSidiaryForGeneralUI(selectedId = null) {
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            let $dropdown = $('#ModalforsidiarygroupName');
            $dropdown.find('option:not(:first)').remove();

            if (groupResponse && groupResponse.data) {
                $.each(groupResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalforsidiarygroupName');

            $dropdown.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                ModalselectedGroupCodeSSL = selectedGroupCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName').val('');
                    $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
                    $('#ModalSusidiaryLedgerCodeNo').val('');

                    let $ctrl = $('#ModalforsidiaryControlName');
                    $ctrl.find('option:not(:first)').remove();
                    $ctrl.val('');
                    window.reinitCustomDropdownById('ModalforsidiaryControlName');

                    let $subctrl = $('#ModalforsidiaryGeneralLedgerName');
                    $subctrl.find('option:not(:first)').remove();
                    $subctrl.val('');
                    window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');
                    return;
                }

                // Clear downstream fields
                $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');
                $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
                $('#ModalSusidiaryLedgerCodeNo').val('');

                let $subctrl = $('#ModalforsidiaryGeneralLedgerName');
                $subctrl.find('option:not(:first)').remove();
                $subctrl.val('');
                window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');

                // Load Control Ledger Dropdown
                ModalgetControlLedger(selectedGroupCode);

                // Fetch Group Info
                $.ajax({
                    url: '/forsubsidiary-group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#Modalforsidiarygroupcode').val(data.controlLedgerCodeNo || '');
                        $('#ModalforsidiarygroupshortName').val(data.shortName || '');
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to fetch Group Ledger dropdown for Modal Sub-Sidiary.");
        }
    });
}

// 2. Control Ledger Dropdown
function ModalgetControlLedger(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-dropdown',
        type: 'GET',
        data: { groupcode: selectedGroupCode },
        success: function (controlResponse) {
            let $dropdown = $('#ModalforsidiaryControlName');
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
                ModalselectedControlCodeSSL = selectedControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
                    $('#ModalSusidiaryLedgerCodeNo').val('');

                    let $subctrl = $('#ModalforsidiaryGeneralLedgerName');
                    $subctrl.find('option:not(:first)').remove();
                    $subctrl.val('');
                    window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');
                    return;
                }

                // Clear downstream fields
                $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName').val('');
                $('#ModalSusidiaryLedgerCodeNo').val('');

                // Load Sub-Control Dropdown
                ModalgetSubControllerLedger(selectedControlCode);

                // Fetch Control Info
                ModalGetControlLedgerInfo(selectedControlCode);
            });

            window.reinitCustomDropdownById('ModalforsidiaryControlName');
        },
        error: function () {
            console.error("Failed to fetch Control Ledger dropdown for Modal Sub-Sidiary.");
        }
    });
}

function ModalGetControlLedgerInfo(selectedControlCode) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-info',
        type: 'GET',
        data: { contrlcode: selectedControlCode, controlcode: selectedControlCode },
        success: function (controlInfo) {
            $('#ModalforsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo || '');
            $('#ModalforsidiaryCLShortName').val(controlInfo.shortName || '');
        }
    });
}

// 3. Sub-Control Ledger Dropdown
function ModalgetSubControllerLedger(selectedControlCode, selectedId = null) {
    $.ajax({
        url: '/subcontrol-ledger-dropdown',
        type: 'GET',
        data: { controlcode: selectedControlCode },
        success: function (subControlResponse) {
            let $dropdown = $('#ModalforsidiaryGeneralLedgerName');
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
                ModalselectedSubControlCodeSSL = selectedSubControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSubControlCode) {
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName, #ModalSusidiaryLedgerCodeNo').val('');
                    return;
                }

                // Fetch Sub-Control Info & Next Code
                ModalGetSubcontrolInfo(selectedSubControlCode);
            });

            window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');
        },
        error: function () {
            console.error("Failed to fetch Sub-Control Ledger dropdown for Modal Sub-Sidiary.");
        }
    });
}

function ModalGetSubcontrolInfo(selectedSubControlCode) {
    $.ajax({
        url: '/forsudsidiary-subcontrol-ledger-info',
        type: 'GET',
        data: { subcontrlcode: selectedSubControlCode, subcontrolcode: selectedSubControlCode },
        success: function (subControlInfo) {
            $('#ModalforsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo || '');
            $('#ModalforsidiarySubCRLshortName').val(subControlInfo.shortName || '');

            if (!ModalisEditModeSubSidiary) {
                $.ajax({
                    url: '/next-subsidiary-ledger-code',
                    type: 'GET',
                    data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                    success: function (nextCode) {
                        $('#ModalSusidiaryLedgerCodeNo').val(nextCode);
                    }
                });
            }
        }
    });
}
//#endregion

//#region Table Reload Helper
function reloadModalSubSidiaryTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#modalSubSidiaryLedgerTable')) {
        $('#modalSubSidiaryLedgerTable').DataTable().ajax.reload(null, false);
    }
}

function ModalSubSididiarLedgerAll() {
    reloadModalSubSidiaryTable();
}
//#endregion

//#region Selection Helpers
function getSelectedModalSubSidiaryIds() {
    let ids = [...selectedModalSubSidiaryLedger];

    $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const rawVal = $(this).val() || $(this).data('id') || $(this).data('autoid');
        const numVal = parseFloat(rawVal);
        if (!isNaN(numVal) && !ids.includes(numVal)) {
            ids.push(numVal);
        }
    });

    if (ids.length === 0) {
        const formAutoId = parseFloat($('#ModalforsidiaryAutoID').val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            ids.push(formAutoId);
        }
    }

    return ids;
}

function updateModalSubSidiaryCheckAllState() {
    const $checkboxes = $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]');
    const total = $checkboxes.length;
    const checked = $checkboxes.filter(':checked').length;
    $('#modal-subsidiaryledger-check-all').prop('checked', total > 0 && total === checked);
}
//#endregion

//#region Save Or Update Modal Sub-Sidiary Ledger
function ModalSubSidiaryLedgerSaveOrUpdate() {
    let autoID = $('#ModalforsidiaryAutoID').val();
    let parsedAutoId = autoID ? parseFloat(autoID) : 0;

    let SubSidiaryLedgerData = {
        autoId: parsedAutoId,
        GLCode: $("#Modalforsidiarygroupcode").val() || $("#ModalforsidiarygroupName").val(),
        CRLCode: $("#ModalforsidiaryControlCode").val() || $("#ModalforsidiaryControlName").val(),
        GeneralLedgerCodeNo: $("#ModalforsidiaryGeneralLedgerCodeNo").val() || $("#ModalforsidiaryGeneralLedgerName").val(),
        SusidiaryLedgerCodeNo: $("#ModalSusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: ($("#ModalSubsidiaryLedgerName").val() || '').trim(),
        ShortName: ($("#ModalforsidiaryshortName").val() || '').trim()
    };

    // Validations
    if (!$("#ModalforsidiarygroupName").val()) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalforsidiarygroupName').addClass('border-error');
        $('#ModalforsidiarygroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#ModalforsidiaryControlName").val()) {
        toastr.error("Please Select a Control Ledger.");
        $('#ModalforsidiaryControlName').addClass('border-error');
        $('#ModalforsidiaryControlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!$("#ModalforsidiaryGeneralLedgerName").val()) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#ModalforsidiaryGeneralLedgerName').addClass('border-error');
        $('#ModalforsidiaryGeneralLedgerName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger Name.");
        $('#ModalSubsidiaryLedgerName').addClass('border-error').focus();
        return false;
    } else {
        $('#ModalSubsidiaryLedgerName').removeClass('border-error');
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

                        ModalSubSidiaryLedgerFormClear();
                        selectedModalSubSidiaryLedger = [];
                        reloadModalSubSidiaryTable();

                        if (typeof reloadSubSidiaryLedgerTable === "function") {
                            reloadSubSidiaryLedgerTable();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving Sub-Sidiary Ledger.");
                    console.error('Error Saving Modal Sub-Sidiary Ledger :', error);
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

//#region Clear Modal Form
function ModalSubSidiaryLedgerFormClear() {
    ModalisEditModeSubSidiary = false;

    $("#ModalforsidiaryAutoID").val("");

    window.clearAllCustomDropdowns('#ModalsubSidiaryForm');

    $("#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName").val("");
    $("#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName").val("");
    $("#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName").val("");
    $("#ModalSusidiaryLedgerCodeNo, #ModalSubsidiaryLedgerName, #ModalforsidiaryshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubSidiarydisplayModifyDate').text('');
    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-ModalSubSidiary-save span, .ModalsaveBtn').text('Save');

    // Reset selections
    selectedModalSubSidiaryLedger = [];
    $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#modalSubSidiaryLedgerTable tbody tr').removeClass('selected-row');
    $('#modal-subsidiaryledger-check-all').prop('checked', false);

    ModalselectedGroupCodeSSL = "";
    ModalselectedControlCodeSSL = "";
    ModalselectedSubControlCodeSSL = "";

    let $ctrlSelect = $('#ModalforsidiaryControlName');
    $ctrlSelect.find('option:not(:first)').remove();
    $ctrlSelect.val('');
    window.reinitCustomDropdownById('ModalforsidiaryControlName');

    let $subctrlSelect = $('#ModalforsidiaryGeneralLedgerName');
    $subctrlSelect.find('option:not(:first)').remove();
    $subctrlSelect.val('');
    window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');

    ModalSubSidiaryForGeneralUI();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-modal-subsidiaryledger-edit, .Modalsubsidiary-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    ModalisEditModeSubSidiary = true;
    selectedModalSubSidiaryLedger = [id];

    $.ajax({
        url: `/subsidiary-ledger/details/${id}`,
        type: 'GET',
        success: function (response) {
            if (response) {
                $('#ModalforsidiaryAutoID').val(response.autoId || id);
                $('#ModalSusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                $('#ModalSubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                $('#ModalforsidiaryshortName').val(response.shortName || '');

                // Group Ledger
                $('#Modalforsidiarygroupcode').val(response.glCode || '');
                $('#ModalforsidiarygroupshortName').val(response.glShortName || '');
                $('#ModalforsidiarygroupName').val(response.glCode);
                window.reinitCustomDropdownById('ModalforsidiarygroupName');

                // Control Ledger
                if (response.glCode) {
                    ModalgetControlLedger(response.glCode, response.crlCode);
                }
                $('#ModalforsidiaryControlCode').val(response.crlCode || '');
                $('#ModalforsidiaryCLShortName').val(response.crLShortName || '');

                // Sub-Control Ledger
                if (response.crlCode) {
                    ModalgetSubControllerLedger(response.crlCode, response.generalLedgerCodeNo);
                }
                $('#ModalforsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                $('#ModalforsidiarySubCRLshortName').val(response.subCRLShortName || '');

                // Dates
                if (response.lDate) {
                    let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#ModalSubSidiaryldisplayLDate').text('');
                }

                if (response.modifyDate) {
                    let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#ModalSubSidiarydisplayModifyDate').text('');
                }

                // Switch save button to Update
                $('.js-ModalSubSidiary-save span, .ModalsaveBtn').text('Update');

                // Sync checkboxes in grid
                $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
                    if (rowVal === id) {
                        $(this).prop('checked', true);
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false);
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });
                updateModalSubSidiaryCheckAllState();

                // Focus and scroll
                $('#ModalSubsidiaryLedgerName').focus();
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
$(document).on('change', '#modalSubSidiaryLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedModalSubSidiaryLedger.includes(val)) {
            selectedModalSubSidiaryLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedModalSubSidiaryLedger = selectedModalSubSidiaryLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateModalSubSidiaryCheckAllState();
});

// Master Checkbox
$(document).on('change', '#modal-subsidiaryledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedModalSubSidiaryLedger.includes(val)) {
                selectedModalSubSidiaryLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedModalSubSidiaryLedger = selectedModalSubSidiaryLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#modalSubSidiaryLedgerTable', function () {
    $('#modalSubSidiaryLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (selectedModalSubSidiaryLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateModalSubSidiaryCheckAllState();
});
//#endregion

//#region Delete Modal Sub-Sidiary Ledger
function ModalSubSidiaryLedgerDelete(ids) {
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

            ModalSubSidiaryLedgerFormClear();
            selectedModalSubSidiaryLedger = [];
            reloadModalSubSidiaryTable();

            if (typeof reloadSubSidiaryLedgerTable === "function") {
                reloadSubSidiaryLedgerTable();
            }
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#ModalSubSidiarydeleteBtn, .js-ModalSubSidiary-delete-confirm", function (e) {
    const isModalBtn = $(this).hasClass('js-ModalSubSidiary-delete-confirm') ||
                       $(this).closest('#SSLModal, #ModalsubSidiaryForm').length > 0;
    if (!isModalBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedModalSubSidiaryIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    modalSubSidiaryPendingDeleteIds = ids;
    isModalSubSidiaryDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            ModalSubSidiaryLedgerDelete(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isModalSubSidiaryDeletePending && modalSubSidiaryPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...modalSubSidiaryPendingDeleteIds];
        isModalSubSidiaryDeletePending = false;
        modalSubSidiaryPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        ModalSubSidiaryLedgerDelete(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isModalSubSidiaryDeletePending = false;
    modalSubSidiaryPendingDeleteIds = [];
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ModalSubSidiaryForGeneralUI();
    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#ModalsubSidiaryForm", function (e) {
        e.preventDefault();
        ModalSubSidiaryLedgerSaveOrUpdate();
    });

    $(document).on("click", ".js-ModalSubSidiary-save, .ModalsaveBtn", function (e) {
        if ($(this).closest('#SSLModal, #ModalsubSidiaryForm').length > 0) {
            e.preventDefault();
            ModalSubSidiaryLedgerSaveOrUpdate();
        }
    });

    // Clear Button Handler
    $(document).on("click", ".ModalSubSidiaryresetBtn, .js-ModalSubSidiary-clear", function (e) {
        const isModalReset = $(this).hasClass('js-ModalSubSidiary-clear') ||
                             $(this).closest('#SSLModal, #ModalsubSidiaryForm').length > 0;
        if (!isModalReset) return;
        e.preventDefault();
        ModalSubSidiaryLedgerFormClear();
    });
});
//#endregion
