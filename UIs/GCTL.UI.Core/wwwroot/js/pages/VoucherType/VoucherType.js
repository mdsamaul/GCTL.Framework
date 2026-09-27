let selectedVoucherType = [];

$(document).ready(function () {
    console.log("Voucher Type Page Loaded");

    // Initialize _CustomGrid DataTable
    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({
            gridId: 'voucherTypeTable'
        });
    }

    loadNextVoucherTypeCode();
    loadNumberingMethodDropdown();
    loadResetDurationDropdown();

    // Save Button
    $(document).on("click", ".VoucherSaveBtn, .js-vouchertype-save", function (e) {
        e.preventDefault();
        saveOrUpdateVoucherType();
    });

    $(document).on("submit", "#VoucherTypeForm", function (e) {
        e.preventDefault();
        saveOrUpdateVoucherType();
    });

    // Delete Button (Toolbar)
    $(document).on("click", ".VoucherdeleteBtn, .js-vouchertype-delete-confirm", function (e) {
        e.preventDefault();
        openDeleteModal();
    });

    // Clear Button
    $(document).on("click", ".VoucherresetBtn, .js-vouchertype-clear", function (e) {
        e.preventDefault();
        clearVoucherForm();
    });

    // Row Edit Click Handler
    $(document).on("click", ".js-vouchertype-edit", function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        if (id) {
            loadVoucherTypeDetails(id);
        }
    });

    // Checkbox synchronization for bulk actions
    $(document).on('change', '.js-vouchertype-check', function () {
        syncSelectedCheckboxes();
    });

    $(document).on('change', '#selectAllVoucherType', function () {
        $('.js-vouchertype-check').prop('checked', $(this).prop('checked'));
        syncSelectedCheckboxes();
    });

    // Confirm Delete Click
    $(document).on("click", "#deleteModal .js-delete, #deleteModal .js-gc-delete-confirm", function (e) {
        e.preventDefault();
        confirmDeleteVoucherType();
    });
});

function syncSelectedCheckboxes() {
    selectedVoucherType = [];
    $('.js-vouchertype-check:checked').each(function () {
        var val = parseFloat($(this).val());
        if (!isNaN(val)) selectedVoucherType.push(val);
    });
}

function reloadVoucherTypeGrid(resetPaging = false) {
    if ($.gctlGrid) {
        $.gctlGrid.reload('voucherTypeTable', resetPaging);
    }
}

// Load Next Voucher Type Code
function loadNextVoucherTypeCode() {
    $.ajax({
        url: '/next-voucher-type-code',
        method: 'GET',
        success: function (response) {
            $('#VoucherTypeCode').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed to load next Voucher Type Code:", error);
        }
    });
}

// Load Numbering Method Dropdown
function loadNumberingMethodDropdown(selectedVal = null) {
    var $dropdown = $('#numberingMethod');
    $dropdown.empty().append('<option value="">Select Numbering Method</option>');
    var methods = [
        { id: 'Manual', name: 'Manual' },
        { id: 'Auto', name: 'Auto' },
        { id: 'Date Wise Auto', name: 'Date Wise Auto' }
    ];

    $.each(methods, function (i, item) {
        var selected = (selectedVal && selectedVal.toLowerCase() === item.id.toLowerCase()) ? 'selected' : '';
        $dropdown.append(`<option value="${item.id}" ${selected}>${item.name}</option>`);
    });

    if (typeof window.initCustomDropdowns === "function") {
        window.initCustomDropdowns('#VoucherFormView', true);
    }
}

// Load Duration Type Dropdown
function loadResetDurationDropdown(selectedId = null) {
    $.ajax({
        url: '/reset-duration-dropdown',
        method: 'GET',
        success: function (response) {
            var $duration = $('#resetDurationTypeCode');
            $duration.empty().append('<option value="">Select Duration Type</option>');

            if (response && response.data) {
                $.each(response.data, function (index, item) {
                    var selected = (selectedId && String(selectedId) === String(item.id)) ? 'selected' : '';
                    $duration.append(`<option value="${item.id}" ${selected}>${item.name}</option>`);
                });
            }

            if (typeof window.initCustomDropdowns === "function") {
                window.initCustomDropdowns('#VoucherFormView', true);
            }
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Duration Type:', error);
        }
    });
}

// Validation
function validateVoucherType(model) {
    if (!model.Voucher_TypeName) {
        toastr.warning("Please enter Voucher Type.");
        $('#VoucherTypeName').focus();
        return false;
    }

    if (!model.Description) {
        toastr.warning("Please enter Description.");
        $('#Description').focus();
        return false;
    }

    if (!model.numberingMethod) {
        toastr.warning("Please select Numbering Method.");
        $('#numberingMethod').focus();
        return false;
    }

    if (!model.resetDuration && model.resetDuration !== 0) {
        toastr.warning("Please enter Reset Duration.");
        $('#resetDuration').focus();
        return false;
    }

    if (!model.resetDurationType_Code) {
        toastr.warning("Please select Duration Type.");
        $('#resetDurationTypeCode').focus();
        return false;
    }

    if (!model.prefix) {
        toastr.warning("Please enter Prefix.");
        $('#prefix').focus();
        return false;
    }

    return true;
}

// Save or Update
function saveOrUpdateVoucherType() {
    let autoId = $('#VoucherautoId').val();
    let model = {
        autoId: autoId ? parseFloat(autoId) : 0,
        VoucherType_Code: $('#VoucherTypeCode').val() || "",
        Voucher_TypeName: ($("#VoucherTypeName").val() || "").trim(),
        Description: ($("#Description").val() || "").trim(),
        numberingMethod: $("#numberingMethod").val() || "",
        startingNumber: $("#startingNumber").val() ? parseFloat($("#startingNumber").val()) : 0,
        resetDuration: $("#resetDuration").val() ? parseFloat($("#resetDuration").val()) : 0,
        resetDurationType_Code: $("#resetDurationTypeCode").val() || "",
        prefix: ($("#prefix").val() || "").trim(),
        suffix: ($("#suffix").val() || "").trim(),
        padding: $("#padding").val() ? parseFloat($("#padding").val()) : 0
    };

    if (!validateVoucherType(model)) {
        return false;
    }

    $.ajax({
        url: '/voucher-type/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message || "Voucher Type already exists.");
                return;
            }

            let isEdit = autoId && parseFloat(autoId) > 0;
            let url = isEdit ? `/voucher-type/${autoId}` : '/voucher-type';
            let method = isEdit ? 'PUT' : 'POST';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(model),
                success: function (response) {
                    if (response.success || response.isSuccess) {
                        toastr.success(response.message || (isEdit ? 'Data Updated Successfully.' : 'Data Saved Successfully.'));
                        clearVoucherForm();
                        reloadVoucherTypeGrid(false);
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(xhr.responseJSON?.message || "Error saving Voucher Type.");
                    console.error('Error saving:', error);
                }
            });
        },
        error: function (xhr, status, error) {
            toastr.error("Server error occurred during duplicate check!");
            console.error('Error checking duplicate:', error);
        }
    });
}

// Load Details for Edit
function loadVoucherTypeDetails(id) {
    if (!id) return;

    $.ajax({
        url: `/voucher-type/details/${id}`,
        type: 'GET',
        success: function (data) {
            $('#VoucherautoId').val(data.autoId);
            $('#VoucherTypeCode').val(data.voucherType_Code || '');
            $('#VoucherTypeName').val(data.voucher_TypeName || '');
            $('#Description').val(data.description || '');
            $('#startingNumber').val(data.startingNumber != null ? data.startingNumber : '');
            $('#resetDuration').val(data.resetDuration != null ? data.resetDuration : '');
            $('#prefix').val(data.prefix || '');
            $('#suffix').val(data.suffix || '');
            $('#padding').val(data.padding != null ? data.padding : '');

            loadNumberingMethodDropdown(data.numberingMethod);
            loadResetDurationDropdown(data.resetDurationType_Code);

            // Audit Dates
            if (data.lDate || data.modifyDate) {
                $('#entryUpdateDateSection').show();
                $('#VoucherdisplayLDate').text(data.lDate ? formatDate(data.lDate) : '');
                $('#VoucherdisplayModifyDate').text(data.modifyDate ? formatDate(data.modifyDate) : '');
            } else {
                $('#entryUpdateDateSection').hide();
            }

            $("html, body").animate({ scrollTop: 0 }, 300);
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to load details.");
            console.error("Error loading details:", error);
        }
    });
}

// Open Delete Modal
function openDeleteModal() {
    if (!selectedVoucherType.length) {
        const formAutoId = parseFloat($("#VoucherautoId").val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            selectedVoucherType = [formAutoId];
        }
    }

    const ids = selectedVoucherType.map(x => parseFloat(x)).filter(x => !isNaN(x));

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    var $modal = $("#deleteModal");
    if ($modal.length) {
        $modal.find('.title').html(`Are you sure you want to delete ${ids.length} record(s)?`);
        $modal.modal("show");
    }
}

// Confirm Delete
function confirmDeleteVoucherType() {
    const ids = selectedVoucherType.map(x => parseFloat(x)).filter(x => !isNaN(x));
    if (!ids.length) return;

    var $modal = $("#deleteModal");
    $.ajax({
        url: '/voucher-type-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            $modal.modal("hide");
            if (response.isSuccess || response.success) {
                toastr.success(response.message || "Data Deleted Successfully.");
                clearVoucherForm();
                selectedVoucherType = [];
                reloadVoucherTypeGrid(false);
            } else {
                toastr.error(response.message || "Failed to delete.");
            }
        },
        error: function (xhr, status, error) {
            $modal.modal("hide");
            toastr.error("Error occurred during deletion.");
            console.error("Delete Error:", error);
        }
    });
}

// Clear Form
function clearVoucherForm() {
    $("#VoucherautoId").val("");
    $("#VoucherTypeName").val("");
    $("#Description").val("");
    $("#startingNumber").val("");
    $("#resetDuration").val("");
    $("#prefix").val("");
    $("#suffix").val("");
    $("#padding").val("");

    $('#VoucherdisplayModifyDate').text('');
    $('#VoucherdisplayLDate').text('');
    $('#entryUpdateDateSection').hide();

    $(".js-vouchertype-check").prop("checked", false);
    $("#selectAllVoucherType").prop("checked", false);

    selectedVoucherType = [];

    loadNextVoucherTypeCode();
    loadNumberingMethodDropdown();
    loadResetDurationDropdown();

    if (typeof window.clearAllCustomDropdowns === 'function') {
        window.clearAllCustomDropdowns('#VoucherFormView');
    }
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    var d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    var day = String(d.getDate()).padStart(2, '0');
    var month = String(d.getMonth() + 1).padStart(2, '0');
    var year = d.getFullYear();
    return day + '/' + month + '/' + year;
}