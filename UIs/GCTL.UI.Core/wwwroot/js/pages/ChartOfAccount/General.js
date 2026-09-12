let selectedGeneralLedger = [];
let isGeneralLedgerDeletePending = false;
let generalLedgerPendingDeleteIds = [];
let isEditModeGeneral = false;

// Selected dropdown values
let selectedGroupCodeforGL = "";
let selectedControlCodeforGL = "";
let selectedSubControlCodeforGL = "";
let selectedSubsidiaryCodeforGL = "";

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

//#region Validation Error Border Cleanup
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
window.addEventListener('groupLedgerUpdated', function () {
    loadGeneralLedgerData();
});

window.addEventListener('ControlLedgerUpdated', function () {
    const currentGroupCode = $('#GeneralgrlName').val();
    if (currentGroupCode) {
        loadControlLedgerDropdown(currentGroupCode);
    }
});

window.addEventListener('SubcontrolLedgerUpdated', function () {
    const currentControlCode = $('#GeneralCRLName').val();
    if (currentControlCode) {
        loadSubControlLedgerDropdown(currentControlCode);
    }
});

window.addEventListener('SubSidiaryLedgerUpdated', function () {
    const currentSubControlCode = $('#GeneralSCRLName').val();
    if (currentSubControlCode) {
        loadSubSidiaryLedgerDropdown(currentSubControlCode);
    }
});
//#endregion

//#region General Ledger UI: Cascading Dropdowns and Data Loading

// 1. Group Ledger Dropdown
function loadGeneralLedgerData(selectedId = null) {
    return $.ajax({
        url: "/generalUi-grl-dropdown",
        type: "GET",
        success: function (groupResponse) {
            let $dropdown = $('#GeneralgrlName');
            $dropdown.find('option:not(:first)').remove();

            if (groupResponse && groupResponse.data) {
                $.each(groupResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('GeneralgrlName');

            // Bind Change Event
            $dropdown.off('change').on('change', async function () {
                const selectedGroupCode = $(this).val();
                selectedGroupCodeforGL = selectedGroupCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    resetGroupLedgerFields();
                    return;
                }

                // Reset downstream fields
                resetControlLedgerFields();

                // Load Control Ledger Dropdown
                await loadControlLedgerDropdown(selectedGroupCode);

                // Fetch Group Ledger Details
                $.ajax({
                    url: '/generalUi-grl-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (groupInfo) {
                        $('#Generalgrlcode').val(groupInfo.controlLedgerCodeNo || '');
                        $('#GeneralgrlshortName').val(groupInfo.shortName || '');
                    },
                    error: function (err) {
                        console.error("Error fetching group info:", err);
                    }
                });
            });
        },
        error: function (error) {
            console.error("Error loading group ledger dropdown:", error);
        }
    });
}

// 2. Control Ledger Dropdown
function loadControlLedgerDropdown(groupCode, selectedId = null) {
    return $.ajax({
        url: '/generalUi-crl-dropdown',
        type: 'GET',
        data: { groupcode: groupCode },
        success: function (controlResponse) {
            let $dropdown = $("#GeneralCRLName");
            $dropdown.find('option:not(:first)').remove();

            if (controlResponse && controlResponse.data) {
                $.each(controlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('GeneralCRLName');

            $dropdown.off('change').on('change', async function () {
                const selectedControlCode = $(this).val();
                selectedControlCodeforGL = selectedControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    resetControlLedgerFields();
                    return;
                }

                // Reset downstream fields
                resetSubControlLedgerFields();

                // Load Sub-Control Dropdown
                await loadSubControlLedgerDropdown(selectedControlCode);

                // Fetch Control Details
                $.ajax({
                    url: '/generalUi-crl-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode },
                    success: function (controlInfo) {
                        $('#GeneralCRLCode').val(controlInfo.subControlLedgerCodeNo || '');
                        $('#GeneralCRLShortName').val(controlInfo.shortName || '');
                    },
                    error: function (err) {
                        console.error("Error loading control info:", err);
                    }
                });
            });
        },
        error: function (error) {
            console.error("Error loading control ledger:", error);
        }
    });
}

// 3. Sub-Control Ledger Dropdown
function loadSubControlLedgerDropdown(controlCode, selectedId = null) {
    return $.ajax({
        url: '/generalUi-subcrl-dropdown',
        type: 'GET',
        data: { controlcode: controlCode },
        success: function (subControlResponse) {
            let $dropdown = $("#GeneralSCRLName");
            $dropdown.find('option:not(:first)').remove();

            if (subControlResponse && subControlResponse.data) {
                $.each(subControlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('GeneralSCRLName');

            $dropdown.off('change').on('change', async function () {
                const selectedSubControlCode = $(this).val();
                selectedSubControlCodeforGL = selectedSubControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSubControlCode) {
                    resetSubControlLedgerFields();
                    return;
                }

                // Reset downstream fields
                resetSubSidiaryLedgerFields();

                // Load Sub-Sidiary Dropdown
                await loadSubSidiaryLedgerDropdown(selectedSubControlCode);

                // Fetch Sub-Control Details
                $.ajax({
                    url: '/generalUi-subcrl-info',
                    type: 'GET',
                    data: { subcontrlcode: selectedSubControlCode },
                    success: function (subControlInfo) {
                        $('#GeneralSCRLCode').val(subControlInfo.generalLedgerCodeNo || '');
                        $('#GeneralSCRLShortname').val(subControlInfo.shortName || '');
                    },
                    error: function (err) {
                        console.error("Error loading sub-control info:", err);
                    }
                });
            });
        },
        error: function (error) {
            console.error("Error loading sub-control ledger:", error);
        }
    });
}

// 4. Sub-Sidiary Ledger Dropdown
function loadSubSidiaryLedgerDropdown(subControlCode, selectedId = null) {
    return $.ajax({
        url: '/generalUi-ssl-dropdown',
        type: 'GET',
        data: { subcontrolcode: subControlCode },
        success: function (sslResponse) {
            let $dropdown = $("#GeneralSSLName");
            $dropdown.find('option:not(:first)').remove();

            if (sslResponse && sslResponse.data) {
                $.each(sslResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('GeneralSSLName');

            $dropdown.off('change').on('change', function () {
                const selectedSSLCode = $(this).val();
                selectedSubsidiaryCodeforGL = selectedSSLCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSSLCode) {
                    resetSubSidiaryLedgerFields();
                    return;
                }

                // Fetch Sub-Sidiary Info
                $.ajax({
                    url: '/generalUi-ssl-info',
                    type: 'GET',
                    data: { subsidiarycode: selectedSSLCode },
                    success: function (sslInfo) {
                        $('#GeneralSSLCode').val(sslInfo.susidiaryLedgerCodeNo || '');
                        $('#GeneralSSLShortName').val(sslInfo.shortName || '');

                        if (!isEditModeGeneral) {
                            $.ajax({
                                url: '/next-general-ledger-code',
                                type: 'GET',
                                data: { subsidiaryLedgercode: selectedSSLCode },
                                success: function (nextCode) {
                                    $('#GeneralLedgerCode').val(nextCode);
                                },
                                error: function (xhr, status, error) {
                                    console.error("Error fetching next General Ledger code:", error);
                                }
                            });
                        }
                    },
                    error: function (err) {
                        console.error("Error loading subsidiary info:", err);
                    }
                });
            });
        },
        error: function (error) {
            console.error("Error loading subsidiary ledger:", error);
        }
    });
}

// 5. Cash Flow Dropdown
function CashFlowDropdown(selectedId = null) {
    return $.ajax({
        url: "/cash-flow-dropdown",
        type: "GET",
        success: function (cashdata) {
            let $dropdown = $('#CashFlowTypeID');
            $dropdown.find('option:not(:first)').remove();

            if (cashdata && cashdata.data) {
                $.each(cashdata.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('CashFlowTypeID');
        },
        error: function () {
            console.error("Failed Cash Flow Dropdown");
        }
    });
}
//#endregion

//#region Reset Cascading Helpers
function resetGroupLedgerFields() {
    $('#Generalgrlcode, #GeneralgrlshortName').val('');
    resetControlLedgerFields();
}

function resetControlLedgerFields() {
    $('#GeneralCRLCode, #GeneralCRLShortName').val('');
    let $ctrl = $('#GeneralCRLName');
    $ctrl.find('option:not(:first)').remove();
    $ctrl.val('');
    window.reinitCustomDropdownById('GeneralCRLName');
    resetSubControlLedgerFields();
}

function resetSubControlLedgerFields() {
    $('#GeneralSCRLCode, #GeneralSCRLShortname').val('');
    let $subctrl = $('#GeneralSCRLName');
    $subctrl.find('option:not(:first)').remove();
    $subctrl.val('');
    window.reinitCustomDropdownById('GeneralSCRLName');
    resetSubSidiaryLedgerFields();
}

function resetSubSidiaryLedgerFields() {
    $('#GeneralSSLCode, #GeneralSSLShortName').val('');
    let $ssl = $('#GeneralSSLName');
    $ssl.find('option:not(:first)').remove();
    $ssl.val('');
    window.reinitCustomDropdownById('GeneralSSLName');
    $('#GeneralLedgerCode').val('');
}
//#endregion

//#region Table Reload Helper
function reloadGeneralLedgerTable() {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#generalLedgerTable')) {
        $('#generalLedgerTable').DataTable().ajax.reload(null, false);
    }
}
//#endregion

//#region Save Or Update General Ledger
function GeneralLedgerSaveOrUpdate() {
    let autoID = $('#GeneralAutoID').val();
    let isActive = $("#IsActive").is(':checked') ? 'Y' : 'N';

    let GeneralLedgerData = {
        autoId: autoID ? parseFloat(autoID) : 0,
        SubsidiaryLedgerCodeNo: ($("#GeneralSSLCode").val() || "").trim(),
        SubSusidiaryLedgerCodeNo: ($("#GeneralLedgerCode").val() || "").trim(),
        SubSubsidiaryLedgerName: ($("#GeneralLName").val() || "").trim(),
        ShortName: ($("#GeneralShortName").val() || "").trim(),
        CashFlowTypeID: $("#CashFlowTypeID").val() || null,
        IsActive: isActive,
        GeneralgrlName: $("#GeneralgrlName").val() || "",
        GeneralCRLName: $("#GeneralCRLName").val() || "",
        GeneralSCRLName: $("#GeneralSCRLName").val() || "",
        GeneralSSLName: $("#GeneralSSLName").val() || ""
    };

    // Validations
    if (!GeneralLedgerData.GeneralgrlName) {
        toastr.error("Please Select a Group Ledger.");
        $('#GeneralgrlName').addClass('border-error');
        $('#GeneralgrlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralCRLName) {
        toastr.error("Please Select a Control Ledger.");
        $('#GeneralCRLName').addClass('border-error');
        $('#GeneralCRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralSCRLName) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#GeneralSCRLName').addClass('border-error');
        $('#GeneralSCRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralSSLName) {
        toastr.error("Please Select a Sub-Sidiary Ledger.");
        $('#GeneralSSLName').addClass('border-error');
        $('#GeneralSSLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.SubSusidiaryLedgerCodeNo) {
        toastr.error("Please Enter General Ledger Code.");
        $('#GeneralLedgerCode').addClass('border-error');
        return false;
    } else {
        $('#GeneralLedgerCode').removeClass('border-error');
    }

    if (!GeneralLedgerData.SubSubsidiaryLedgerName) {
        toastr.error("Please Enter General Ledger Name.");
        $('#GeneralLName').addClass('border-error');
        return false;
    } else {
        $('#GeneralLName').removeClass('border-error');
    }

    // Check duplicate
    $.ajax({
        url: '/general-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(GeneralLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            let url = autoID ? `/general-ledger/${autoID}` : '/general-ledger';
            let method = autoID ? 'PUT' : 'POST';
            let successMessage = autoID ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(GeneralLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);
                        GeneralLedgerFormClear();
                        selectedGeneralLedger = [];
                        reloadGeneralLedgerTable();
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error Saving General Ledger.");
                    console.error('Error Saving General Ledger :', error);
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
function GeneralLedgerFormClear() {
    isEditModeGeneral = false;

    $("#GeneralAutoID").val("");

    window.clearAllCustomDropdowns('#generalLedgerForm');

    $("#Generalgrlcode, #GeneralgrlshortName").val("");
    $("#GeneralCRLCode, #GeneralCRLShortName").val("");
    $("#GeneralSCRLCode, #GeneralSCRLShortname").val("");
    $("#GeneralSSLCode, #GeneralSSLShortName").val("");
    $("#GeneralLedgerCode, #GeneralLName, #GeneralShortName").val("");
    $("#CashFlowTypeID").val("");
    $("#IsActive").prop('checked', false);

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#GeneraldisplayModifyDate').text('');
    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    // Reset button text
    $('.js-GeneralLedger-save span, #btnSave span').text('Save');

    // Reset selections
    selectedGeneralLedger = [];
    $('#generalLedgerTable tbody input[type="checkbox"]').prop('checked', false);
    $('#generalLedgerTable tbody tr').removeClass('selected-row');
    $('#generalledger-check-all').prop('checked', false);

    selectedGroupCodeforGL = "";
    selectedControlCodeforGL = "";
    selectedSubControlCodeforGL = "";
    selectedSubsidiaryCodeforGL = "";

    // Clear dependent dropdown options
    let $ctrlSelect = $('#GeneralCRLName');
    $ctrlSelect.find('option:not(:first)').remove();
    $ctrlSelect.val('');
    window.reinitCustomDropdownById('GeneralCRLName');

    let $subctrlSelect = $('#GeneralSCRLName');
    $subctrlSelect.find('option:not(:first)').remove();
    $subctrlSelect.val('');
    window.reinitCustomDropdownById('GeneralSCRLName');

    let $sslSelect = $('#GeneralSSLName');
    $sslSelect.find('option:not(:first)').remove();
    $sslSelect.val('');
    window.reinitCustomDropdownById('GeneralSSLName');

    loadGeneralLedgerData();
    CashFlowDropdown();
}
//#endregion

//#region Populate Data For Edit
$(document).on('click', '.js-generalledger-edit, .general-ledger', function (e) {
    e.preventDefault();
    const rawId = $(this).data('id') || $(this).data('autoid');
    if (!rawId) return;

    const id = parseFloat(rawId);
    isEditModeGeneral = true;
    selectedGeneralLedger = [id];

    $.ajax({
        url: `/general-ledger/details/${id}`,
        type: 'GET',
        success: async function (response) {
            if (response) {
                $('#GeneralAutoID').val(response.autoId || id);

                // Group Ledger
                const groupCode = response.generalgrlcode || '';
                $('#Generalgrlcode').val(groupCode);
                $('#GeneralgrlshortName').val(response.generalgrlshortName || '');
                $('#GeneralgrlName').val(groupCode);
                window.reinitCustomDropdownById('GeneralgrlName');

                // Control Ledger
                const controlCode = response.generalCRLCode || '';
                if (groupCode) {
                    await loadControlLedgerDropdown(groupCode, controlCode);
                }
                $('#GeneralCRLCode').val(controlCode);
                $('#GeneralCRLShortName').val(response.generalCRLShortName || '');

                // Sub-Control Ledger
                const subControlCode = response.generalSCRLCode || '';
                if (controlCode) {
                    await loadSubControlLedgerDropdown(controlCode, subControlCode);
                }
                $('#GeneralSCRLCode').val(subControlCode);
                $('#GeneralSCRLShortname').val(response.generalSCRLShortname || '');

                // Sub-Sidiary Ledger
                const subSididaryLedger = response.subsidiaryLedgerCodeNo || '';
                if (subControlCode) {
                    await loadSubSidiaryLedgerDropdown(subControlCode, subSididaryLedger);
                }
                $('#GeneralSSLCode').val(subSididaryLedger);
                $('#GeneralSSLShortName').val(response.generalSSLShortName || '');

                // General Ledger details
                $('#GeneralLedgerCode').val(response.subSusidiaryLedgerCodeNo || '');
                $('#GeneralLName').val(response.subSubsidiaryLedgerName || '');
                $('#GeneralShortName').val(response.shortName || '');

                // Cash Flow
                const cashFlowTypeID = response.cashFlowTypeID || '';
                $('#CashFlowTypeID').val(cashFlowTypeID);
                window.reinitCustomDropdownById('CashFlowTypeID');

                // IsActive
                $('#IsActive').prop('checked', response.isActive === 'Y' || response.isActive === 'Yes');

                // Dates
                if (response.lDate) {
                    let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#GeneralldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#GeneralldisplayLDate').text('');
                }

                if (response.modifyDate) {
                    let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#GeneraldisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#GeneraldisplayModifyDate').text('');
                }

                // Switch save button to Update
                $('.js-GeneralLedger-save span, #btnSave span').text('Update');

                // Sync checkboxes in grid
                $('#generalLedgerTable tbody input[type="checkbox"]').each(function () {
                    const rowVal = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
                    if (rowVal === id) {
                        $(this).prop('checked', true);
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).prop('checked', false);
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });
                updateGeneralLedgerCheckAllState();

                // Focus and scroll
                $('#GeneralLName').focus();
                if ($('#generalLedgerForm').length) {
                    $('html, body').animate({ scrollTop: $('#generalLedgerForm').offset().top - 80 }, 300);
                }
            } else {
                toastr.error("General Ledger not found!");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error fetching General Ledger details!");
            console.error("Error fetching details:", error);
        }
    });
});
//#endregion

//#region Checkbox Selection Handlers
function updateGeneralLedgerCheckAllState() {
    const $visibleCheckboxes = $('#generalLedgerTable tbody input[type="checkbox"]');
    if ($visibleCheckboxes.length === 0) {
        $('#generalledger-check-all').prop('checked', false);
        return;
    }
    const allChecked = $visibleCheckboxes.length === $visibleCheckboxes.filter(':checked').length;
    $('#generalledger-check-all').prop('checked', allChecked);
}

function getSelectedGeneralLedgerIds() {
    let ids = [...selectedGeneralLedger];

    $('#generalLedgerTable tbody input[type="checkbox"]:checked').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (!isNaN(val) && !ids.includes(val)) {
            ids.push(val);
        }
    });

    // Fallback: If no checkbox selected, but an item is loaded in the form for edit
    if (ids.length === 0) {
        const currentAutoId = parseFloat($('#GeneralAutoID').val());
        if (!isNaN(currentAutoId) && currentAutoId > 0) {
            ids.push(currentAutoId);
        }
    }

    return ids;
}

// Individual row checkbox
$(document).on('change', '#generalLedgerTable tbody input[type="checkbox"]', function () {
    const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
    if (isNaN(val)) return;

    if ($(this).prop('checked')) {
        if (!selectedGeneralLedger.includes(val)) {
            selectedGeneralLedger.push(val);
        }
        $(this).closest('tr').addClass('selected-row');
    } else {
        selectedGeneralLedger = selectedGeneralLedger.filter(x => x !== val);
        $(this).closest('tr').removeClass('selected-row');
    }

    updateGeneralLedgerCheckAllState();
});

// Master Checkbox
$(document).on('change', '#generalledger-check-all', function () {
    const isChecked = $(this).prop('checked');
    $('#generalLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (isNaN(val)) return;

        if (isChecked) {
            if (!selectedGeneralLedger.includes(val)) {
                selectedGeneralLedger.push(val);
            }
            $(this).prop('checked', true).closest('tr').addClass('selected-row');
        } else {
            selectedGeneralLedger = selectedGeneralLedger.filter(x => x !== val);
            $(this).prop('checked', false).closest('tr').removeClass('selected-row');
        }
    });
});

// DataTables Draw Event
$(document).on('draw.dt', '#generalLedgerTable', function () {
    $('#generalLedgerTable tbody input[type="checkbox"]').each(function () {
        const val = parseFloat($(this).val() || $(this).data('id') || $(this).data('autoid'));
        if (selectedGeneralLedger.includes(val)) {
            $(this).prop('checked', true);
            $(this).closest('tr').addClass('selected-row');
        }
    });
    updateGeneralLedgerCheckAllState();
});
//#endregion

//#region Delete General Ledger
function GeneralLedgerDelete(ids) {
    if (!ids || !ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/general-ledger-all-delete',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess === false || response.success === false) {
                toastr.error(response.message || "Failed to delete.");
                return;
            }

            toastr.success(response.message || "Data Deleted Successfully.");

            GeneralLedgerFormClear();
            selectedGeneralLedger = [];
            reloadGeneralLedgerTable();
        },
        error: function (xhr, status, error) {
            toastr.error(xhr.responseJSON?.message || "Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Toolbar Delete Button
$(document).on("click", "#GeneraldeleteBtn, .js-GeneralLedger-delete-confirm", function (e) {
    const isGeneralBtn = $(this).hasClass('js-GeneralLedger-delete-confirm') ||
                         $(this).closest('#general-tab-pane, #generalLedgerForm').length > 0;
    if (!isGeneralBtn) return;

    e.preventDefault();
    e.stopPropagation();

    const ids = getSelectedGeneralLedgerIds();
    if (!ids.length) {
        toastr.warning("Please select at least one record to delete.");
        return;
    }

    generalLedgerPendingDeleteIds = ids;
    isGeneralLedgerDeletePending = true;

    if ($('#gcDeleteModal').length) {
        const itemText = ids.length > 1 ? `these ${ids.length} items` : 'this item';
        $('#gcDeleteModal .title').text(`Are you sure you want to delete ${itemText}? This process cannot be undone.`);
        $('#gcDeleteModal').modal('show');
    } else {
        if (confirm(`Are you sure you want to delete ${ids.length > 1 ? 'these ' + ids.length + ' items' : 'this item'}?`)) {
            GeneralLedgerDelete(ids);
        }
    }
});

// Confirmation Button inside #gcDeleteModal
$(document).on("click", "#gcDeleteModal .js-gc-delete-confirm, #gcDeleteModal .js-delete", function (e) {
    if (isGeneralLedgerDeletePending && generalLedgerPendingDeleteIds.length > 0) {
        e.preventDefault();
        e.stopImmediatePropagation();
        const idsToDelete = [...generalLedgerPendingDeleteIds];
        isGeneralLedgerDeletePending = false;
        generalLedgerPendingDeleteIds = [];
        $('#gcDeleteModal').modal('hide');
        GeneralLedgerDelete(idsToDelete);
    }
});

$(document).on('hidden.bs.modal', '#gcDeleteModal', function () {
    isGeneralLedgerDeletePending = false;
    generalLedgerPendingDeleteIds = [];
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    loadGeneralLedgerData();
    CashFlowDropdown();
    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    // Save Button Handlers
    $(document).on("submit", "#generalLedgerForm", function (e) {
        e.preventDefault();
        GeneralLedgerSaveOrUpdate();
    });

    $(document).on("click", ".js-GeneralLedger-save", function (e) {
        e.preventDefault();
        GeneralLedgerSaveOrUpdate();
    });

    // Clear Button Handler
    $(document).on("click", "#btnClear, .GeneralresetBtn, .js-GeneralLedger-clear", function (e) {
        const isGeneralReset = $(this).hasClass('js-GeneralLedger-clear') ||
                               $(this).closest('#general-tab-pane, #generalLedgerForm').length > 0;
        if (!isGeneralReset) return;
        e.preventDefault();
        GeneralLedgerFormClear();
    });

    // Print Button
    $(document).on("click", ".printBtn", function (e) {
        if ($(this).closest('#general-tab-pane, #generalLedgerForm').length > 0) {
            e.preventDefault();
            if (typeof printForm === 'function') {
                printForm();
            } else {
                window.print();
            }
        }
    });

    // Favorite Button
    $(document).on("click", ".GeneralfavBtn", function () {
        if ($(this).closest('#general-tab-pane, #generalLedgerForm').length > 0 && typeof toggleFavorite === 'function') {
            toggleFavorite();
        }
    });
});
//#endregion