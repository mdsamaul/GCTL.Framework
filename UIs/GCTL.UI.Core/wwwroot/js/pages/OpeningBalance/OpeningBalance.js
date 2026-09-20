let selectedBalance = [];

// Global Renderers for _CustomGrid
window.gctlRenderers = window.gctlRenderers || {};
window.gctlRenderers.formatDate = function (data) {
    if (!data) return '';
    if (typeof data === 'string' && data.includes('/')) return data;
    try {
        var d = new Date(data);
        if (isNaN(d.getTime())) return data;
        var day = ('0' + d.getDate()).slice(-2);
        var month = ('0' + (d.getMonth() + 1)).slice(-2);
        var year = d.getFullYear();
        return day + '/' + month + '/' + year;
    } catch (e) {
        return data;
    }
};
window.gctlRenderers.formatCurrency = window.gctlRenderers.formatCurrency || function (data, type) {
    if (data === null || data === undefined || data === '' || isNaN(data)) {
        return (type === 'sort' || type === 'type') ? 0 : '0.00';
    }
    var num = parseFloat(data);
    if (type === 'sort' || type === 'type') {
        return num;
    }
    return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

$(document).ready(function () {
    console.log("Opening Balance Page Loaded");

    // Initialize _CustomGrid DataTable
    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({
            gridId: 'openingBalanceTable'
        });
    }

    LoadCompanyDropdown();
    LoadGeneralLedgerDropdown();

    // Save Button
    $(document).on("click", ".BlanceSaveBtn", function (e) {
        e.preventDefault();
        SaveOrUpdateOpeningBalance();
    });

    $(document).on("submit", "#BlanceTypeForm", function (e) {
        e.preventDefault();
        SaveOrUpdateOpeningBalance();
    });

    // Delete Button
    $(document).on("click", ".BlancedeleteBtn", function (e) {
        e.preventDefault();
        DeleteOpeningBalance();
    });

    // Clear Button
    $(document).on("click", ".BlanceresetBtn", function (e) {
        e.preventDefault();
        ClearOpeningBalanceForm();
    });

    // Checkbox synchronization for bulk actions
    $(document).on('change', '.js-openingbalance-check', function () {
        syncSelectedCheckboxes();
    });

    $(document).on('change', '#selectAllOpeningBalance', function () {
        $('.js-openingbalance-check').prop('checked', $(this).prop('checked'));
        syncSelectedCheckboxes();
    });
});

function syncSelectedCheckboxes() {
    selectedBalance = [];
    $('.js-openingbalance-check:checked').each(function () {
        var val = parseFloat($(this).val());
        if (!isNaN(val)) selectedBalance.push(val);
    });
}

function reloadOpeningBalanceGrid(resetPaging = false) {
    if ($.gctlGrid) {
        $.gctlGrid.reload('openingBalanceTable', resetPaging);
    }
}

// Validation Error Border(Common input handler)
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('is-invalid');
    }
});

$(document).on('input change', '#OpeningDate, input[name="OpeningDate"]', function () {
    if ($('#OpeningDate').val()) {
        $('#OpeningDate').removeClass('is-invalid');
        $('#OpeningDate').next('.flatpickr-input').removeClass('is-invalid');
        $('#OpeningDate-error').text('');
    }
});

$('#CompanyCode, #BranchCode, #SubSusidiaryLedgerCodeNo').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger');
    }
});

$('#TrType').on('change', function () {
    if ($(this).val()) {
        $(this).removeClass('is-invalid');
    }
});

// Account Head Dropdown
function LoadGeneralLedgerDropdown(selectedValue) {
    $.ajax({
        url: '/openingUI-General-Ledger-dropdown',
        method: 'GET',
        success: function (response) {
            var items = response.data || response || [];
            var $general = $('#SubSusidiaryLedgerCodeNo');
            $general.empty().append('<option value="">-- Select Account Head --</option>');

            $.each(items, function (index, general) {
                var id = general.id !== undefined ? general.id : general.Id;
                var name = general.name !== undefined ? general.name : general.Name;
                $general.append(
                    '<option value="' + id + '">' + name + '</option>'
                );
            });

            if (selectedValue) {
                $general.val(selectedValue).trigger('change');
            }

            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('SubSusidiaryLedgerCodeNo');
            }
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Account Head:', error);
        }
    });
}

// Company Dropdown
function LoadCompanyDropdown(selectedCompanyId) {
    $.ajax({
        url: '/openingUI-company-dropdown',
        method: 'GET',
        success: function (response) {
            var items = response.data || response || [];
            var $company = $('#CompanyCode');
            $company.empty().append('<option value="">-- Select Company --</option>');

            $.each(items, function (index, company) {
                var id = company.id !== undefined ? company.id : company.Id;
                var name = company.name !== undefined ? company.name : company.Name;
                $company.append(
                    '<option value="' + id + '">' + name + '</option>'
                );
            });

            if (selectedCompanyId) {
                $company.val(selectedCompanyId).trigger('change');
            } else if (items && items.length > 0) {
                // Auto-select first company since company is readonly
                var firstId = items[0].id !== undefined ? items[0].id : items[0].Id;
                $company.val(firstId).trigger('change');
            }

            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('CompanyCode');
            }
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Company:', error);
        }
    });
}

// Company Dropdown On change Event
$("#CompanyCode").on('change', function (e) {
    const code = $(this).val() || "";
    LoadBranchDropdown(code);
});

// Branch Dropdown
function LoadBranchDropdown(companyCode, callback) {
    $.ajax({
        url: '/openingUI-branch-dropdown',
        method: 'GET',
        data: { companycode: companyCode },
        success: function (response) {
            var items = response.data || response || [];
            var $branch = $('#BranchCode');
            $branch.empty().append('<option value="">-- Select Branch --</option>');

            $.each(items, function (index, branch) {
                var id = branch.id !== undefined ? branch.id : branch.Id;
                var name = branch.name !== undefined ? branch.name : branch.Name;
                $branch.append('<option value="' + id + '">' + name + '</option>');
            });

            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('BranchCode');
            }

            if (callback) callback();
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Branch:', error);
        }
    });
}

function OpeningBalanceValidate(BalanceData) {
    if (!BalanceData.CompanyCode) {
        toastr.error("Please Select a Company.");
        return false;
    }
    if (!BalanceData.BranchCode) {
        toastr.error("Please Select a Branch.");
        return false;
    }
    if (!BalanceData.SubSusidiaryLedgerCodeNo) {
        toastr.error("Please Select an Account Head.");
        return false;
    }
    if (!BalanceData.OpeningBalance && BalanceData.OpeningBalance !== 0) {
        toastr.error("Please Enter Opening Balance.");
        $('#OpeningBalance').addClass('is-invalid');
        return false;
    }
    if (!BalanceData.TrType) {
        toastr.error("Please Select a Tr Type.");
        $('#TrType').addClass('is-invalid');
        return false;
    }
    if (!BalanceData.OpeningDate) {
        toastr.error("Please Select Opening Date.");
        $('#OpeningDate').addClass('is-invalid');
        $('#OpeningDate').next('.flatpickr-input').addClass('is-invalid');
        return false;
    }
    return true;
}

// Save Or Update Opening Balance
function SaveOrUpdateOpeningBalance() {
    let autoID = $('#BlanceautoId').val();
    let BalanceData = {
        autoId: autoID ? parseFloat(autoID) : 0,
        ComOpeningBalanceCode: "",
        CompanyCode: $("#CompanyCode").val(),
        BranchCode: $("#BranchCode").val(),
        SubSusidiaryLedgerCodeNo: $("#SubSusidiaryLedgerCodeNo").val(),
        OpeningBalance: $("#OpeningBalance").val() ? parseFloat($("#OpeningBalance").val()) : 0,
        TrType: $("#TrType").val(),
        OpeningDate: $("#OpeningDate").val() || null
    };

    if (!OpeningBalanceValidate(BalanceData)) {
        return false;
    }

    let url = '/opening-balance';
    let method = 'POST';
    let successMessage = 'Data Saved Successfully.';
    if (autoID) {
        url = `/opening-balance/${autoID}`;
        method = 'PUT';
        successMessage = 'Data Updated Successfully.';
    }

    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        data: JSON.stringify(BalanceData),
        success: function (response) {
            if (response.success) {
                toastr.success(successMessage);
                ClearOpeningBalanceForm();
                reloadOpeningBalanceGrid(false);
            } else {
                toastr.error(response.message || "Operation Failed!");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error Saving Opening Balance.");
            console.error('Error Saving Opening Balance :', error);
        }
    });
}

// Populate Data For Edit
$(document).on('click', '.js-openingbalance-edit', function (e) {
    e.preventDefault();
    const id = $(this).data('id');
    if (!id) return;

    $.ajax({
        url: `/opening-balance/details/${id}`,
        type: 'GET',
        success: function (data) {
            $('#BlanceautoId').val(id);
            $('#CompanyCode').val(data.companyCode || '').trigger('change');
            LoadBranchDropdown(data.companyCode, function () {
                $('#BranchCode').val(data.branchCode || '').trigger('change');
                if (typeof window.reinitCustomDropdownById === 'function') {
                    window.reinitCustomDropdownById('BranchCode');
                }
            });
            $('#SubSusidiaryLedgerCodeNo').val(data.subSusidiaryLedgerCodeNo || '').trigger('change');
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('SubSusidiaryLedgerCodeNo');
                window.reinitCustomDropdownById('CompanyCode');
            }

            let trTypeVal = data.trType || '';
            if (trTypeVal.toLowerCase() === 'debit') trTypeVal = 'Dr';
            if (trTypeVal.toLowerCase() === 'credit') trTypeVal = 'Cr';
            $('#TrType').val(trTypeVal);
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('TrType');
            }
            $('#OpeningBalance').val(data.openingBalance || '');

            if (data.openingDate) {
                let dateIso = data.openingDate.split('T')[0];
                if (window.custom_Datepicker && typeof window.custom_Datepicker.setValue === 'function') {
                    window.custom_Datepicker.setValue('#OpeningDate', dateIso);
                } else {
                    $('#OpeningDate').val(dateIso);
                }
            } else {
                if (window.custom_Datepicker && typeof window.custom_Datepicker.setValue === 'function') {
                    window.custom_Datepicker.setValue('#OpeningDate', '');
                } else {
                    $('#OpeningDate').val('');
                }
            }

            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#BlancedisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#BlancedisplayLDate').text('');
            }

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#BlancedisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#BlancedisplayModifyDate').text('');
            }

            // Show full date row (including hr and labels) only in Edit mode
            $('#entryUpdateDateSection').removeClass('d-none').show();

            selectedBalance = [parseFloat(id)];
            $('.js-openingbalance-check').prop('checked', false);
            $(`.js-openingbalance-check[value="${id}"]`).prop('checked', true);
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Opening Balance details.");
            console.error("Error fetching details:", error);
        }
    });
});

// Single & Bulk Delete Opening Balance
function DeleteOpeningBalance() {
    // If nothing selected from table checkboxes, check if form has active record ID
    if (!selectedBalance.length) {
        const formAutoId = parseFloat($("#BlanceautoId").val());
        if (!isNaN(formAutoId) && formAutoId > 0) {
            selectedBalance = [formAutoId];
        }
    }

    const ids = selectedBalance.map(x => parseFloat(x)).filter(x => !isNaN(x));

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

// Delete Confirmation Click Handler for Bootstrap Modal
$(document).on("click", "#deleteModal .js-delete, #deleteModal .js-gc-delete-confirm", function (e) {
    e.preventDefault();
    const ids = selectedBalance.map(x => parseFloat(x)).filter(x => !isNaN(x));
    if (!ids.length) return;

    var $modal = $("#deleteModal");
    $.ajax({
        url: '/opening-balance-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            $modal.modal("hide");
            toastr.success(response.message || "Data Deleted Successfully.");
            ClearOpeningBalanceForm();
            selectedBalance = [];
            reloadOpeningBalanceGrid(false);
        },
        error: function (xhr, status, error) {
            $modal.modal("hide");
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
});

// Clear Form
function ClearOpeningBalanceForm() {
    $("#BlanceautoId").val("");

    var defaultCompany = $("#CompanyCode option:not([value='']):first").val();
    if (defaultCompany) {
        $("#CompanyCode").val(defaultCompany).trigger('change');
    } else {
        LoadCompanyDropdown();
    }
    if (typeof window.reinitCustomDropdownById === 'function') {
        window.reinitCustomDropdownById('CompanyCode');
    }

    $("#BranchCode").val("").trigger('change');
    $("#SubSusidiaryLedgerCodeNo").val("").trigger('change');
    $("#OpeningBalance").val("");
    $("#TrType").val("").trigger('change');

    if (window.custom_Datepicker && typeof window.custom_Datepicker.setValue === 'function') {
        window.custom_Datepicker.setValue('#OpeningDate', '');
    } else {
        $("#OpeningDate").val("");
    }

    if (typeof window.reinitCustomDropdownById === 'function') {
        window.reinitCustomDropdownById('BranchCode');
        window.reinitCustomDropdownById('SubSusidiaryLedgerCodeNo');
        window.reinitCustomDropdownById('TrType');
    }

    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#OpeningDate').next('.flatpickr-input').removeClass('is-invalid');
    $('#BlancedisplayModifyDate').text('');    
    $('#BlancedisplayLDate').text('');  
    $('#entryUpdateDateSection').addClass('d-none').hide(); // Hide entire row including hr and labels
    $(".js-openingbalance-check").prop("checked", false);
    $("#selectAllOpeningBalance").prop("checked", false);

    selectedBalance = [];
}

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