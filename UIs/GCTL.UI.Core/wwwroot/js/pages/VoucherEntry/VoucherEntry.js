
window.gctlRenderers = window.gctlRenderers || {};
window.gctlRenderers.renderVoucherApprovalStatus = function (data, type, row) {
    var val = (data || '').toString().trim();
    var isApproved = (val === "Y" || val === "Yes" || val === "1" || val.toLowerCase() === "approved" || val.toLowerCase() === "yes");
    if (type === 'display') {
        return isApproved 
            ? '<span class="badge bg-success px-2 py-1" style="font-size: 11px; font-weight: 600;">Yes</span>' 
            : '<span class="badge bg-secondary px-2 py-1" style="font-size: 11px; font-weight: 600;">No</span>';
    }
    return isApproved ? 'Yes' : 'No';
};

// Pagination & Table State
let VoucherEntrycurrentPage = 1;
let VoucherEntrypageSize = parseInt($('#VoucherEntrypageSize').val()) || 5;
let VoucherEntrySortColumn = 'VoucherDate'; 
let VoucherEntrySortOrder = 'desc';
let VoucherEntrySearchTerm = '';
let isEditModeVoucher = false; 
let isInitialVoucherLoad = false; // Fast time false
let selectedVoucher = []; // Selected voucher IDs

//#region Ready Part

$(document).ready(function () {
    setDate("#VoucherDate", new Date());
    setDate("#StartDate", new Date());
    setDate("#EndDate", new Date());
    
    // Set default dates for the grid filter (Date From: 1st of current month, Date To: Current Date)
    var now = new Date();
    var firstDayOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

    var formatYYYYMMDD = function(date) {
        var d = new Date(date),
            month = '' + (d.getMonth() + 1),
            day = '' + d.getDate(),
            year = d.getFullYear();
        if (month.length < 2) month = '0' + month;
        if (day.length < 2) day = '0' + day;
        return [year, month, day].join('-');
    };

    if (typeof setDate === "function") {
        if (!$('#dateFrom').val()) {
            setDate("#dateFrom", firstDayOfMonth);
        }
        if (!$('#dateTo').val()) {
            setDate("#dateTo", now);
        }
    } else {
        if (!$('#dateFrom').val()) $('#dateFrom').val(formatYYYYMMDD(firstDayOfMonth));
        if (!$('#dateTo').val()) $('#dateTo').val(formatYYYYMMDD(now));
    }

    // Reload grid on date change (debounced)
    var filterDateTimer = null;
    function triggerGridDateReload() {
        if (filterDateTimer) clearTimeout(filterDateTimer);
        filterDateTimer = setTimeout(function () {
            LoadVoucherEntriesTable();
        }, 150);
    }

    $('#dateFrom, #dateTo, #filterVoucherType').on('change', function () {
        triggerGridDateReload();
    });

    // Also listen to flatpickr onChange
    setTimeout(function () {
        $('#dateFrom, #dateTo').each(function () {
            var fp = $(this).data('customFpInstance') || this._flatpickr;
            if (fp) {
                var origChange = fp.config.onChange;
                fp.config.onChange = function (selectedDates, dateStr, instance) {
                    if (origChange) origChange(selectedDates, dateStr, instance);
                    triggerGridDateReload();
                };
            }
        });
    }, 300);

    LoadVoucherTypeDropdown();
    LoadAccountHeadDropdown();
    VoucherEntryCompanyDropdown();
    VoucherEntryBranchDropdown();

    LoadVoucherEntriesTable();
    DeleteWithUser();

    // Set Entry Date
    $('#EntryDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("click", ".js-VoucherEntry-save", function (e) {
        e.preventDefault();
        SaveVoucherEntry();
    });



    //// Handle Clear button
    $(document).on("click", ".js-VoucherEntry-clear", function () {
        console.log("Clear Button Clicked")
        ClearVoucherEntryForm();
        DeleteWithUser();

    });

    $('#SearchwithDate').on('click', function () {
        LoadVoucherEntriesTable();
    });

    // Select all checkbox
    $(document).on('click', '#VoucherEntrySelectAll', function () {
        const isChecked = $(this).prop('checked');
        $('.voucher-checkbox:visible').prop('checked', isChecked);

        if (isChecked) {
            $('.voucher-checkbox:visible').each(function () {
                const id = parseInt($(this).data('autoid'));
                if (!selectedVoucher.includes(id)) selectedVoucher.push(id);
            });
        } else {
            $('.voucher-checkbox:visible').each(function () {
                const id = parseInt($(this).data('autoid'));
                const index = selectedVoucher.indexOf(id);
                if (index !== -1) selectedVoucher.splice(index, 1);
            });
        }
        console.log('Selected Vouchers:', selectedVoucher);
    });

    //Pagination Button
    $(document).on('click', '#paginationbutton .page-link', function (e) {
        e.preventDefault();
        const page = parseInt($(this).data('page'));
        if (!isNaN(page) && page > 0) {
            VoucherEntrycurrentPage = page;
            LoadVoucherEntriesTable(page);
        }
    });

    // PageSize Change Handler
    $(document).on('change', '#VoucherEntrypageSize', function () {
        VoucherEntrypageSize = parseInt($(this).val()) || 5;
        VoucherEntrycurrentPage = 1;
        LoadVoucherEntriesTable();
    });

    // Search Input Handler
    $(document).on('keyup', '#table-search', debounce(function () {
        VoucherEntrySearchTerm = $(this).val().trim();
        VoucherEntrycurrentPage = 1;
        LoadVoucherEntriesTable();
    }, 500));

    // Debounce function
    function debounce(func, wait) {
        let timeout;
        return function () {
            const context = this, args = arguments;
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(context, args), wait);
        };
    }

    enableEnterNavigation("#VoucherEntryForm");
    enableSelect2ArrowNavigation("#VoucherTypeCode");

});

//#endregion


//#region EnterKeyNavigation

function enableEnterNavigation(formSelector) {
    const form = document.querySelector(formSelector);
    if (!form) return;

    function getFocusable() {
        return Array.from(form.querySelectorAll("input, select, textarea"))
            .filter(el => !el.disabled && !el.readOnly && el.type !== "hidden" && $(el).is(":visible"));
    }

    // Select2 select → focus next field on select
    $(formSelector + " select").on("select2:select", function () {
        setTimeout(() => focusNext(this), 50);
    });

    $(formSelector).on("keydown", "input, select, textarea", function (e) {
        const isSelect2 = $(this).hasClass("select2-hidden-accessible") && $(this).data('select2');
        const select2Open = $('.select2-container--open').length > 0;

        // Allow arrow keys & Enter inside Select2 dropdown
        if (isSelect2 && select2Open) {
            if (["ArrowUp", "ArrowDown", "Enter"].includes(e.key)) return;
        }

        if (e.key === "Enter") {
            e.preventDefault();
            e.stopPropagation();
            focusNext(this);
        }
    });

    function focusNext(current) {
        let focusable = getFocusable();
        let index = focusable.indexOf(current);
        if (index === -1) return;

        // Find next visible & enabled element
        let next;
        for (let i = index + 1; i < focusable.length; i++) {
            if ($(focusable[i]).is(':visible') && !focusable[i].disabled) {
                next = focusable[i];
                break;
            }
        }
        if (!next) return;

        // Flatpickr → focus + open calendar
        if ($(next).hasClass("flatpickr-input") && next._flatpickr) {
            next.focus();
            setTimeout(() => next._flatpickr.open(), 50);
        }
        // Select2 → focus + open dropdown
        else if ($(next).hasClass("select2-hidden-accessible")) {
            next.focus();
            setTimeout(() => $(next).select2("open"), 50);
        }
        // Normal input → just focus
        else {
            next.focus();
        }
    }
}

function enableSelect2ArrowNavigation(selector) {
    const $select = $(selector);
    $select.on('select2:open', function () {
        const $results = $('.select2-results__options');
        let index = 0;

        $(document).off('keydown.select2Nav').on('keydown.select2Nav', function (e) {
            const items = $results.find('li[role="option"]:visible');
            if (!items.length) return;

            if (e.key === "ArrowDown") {
                e.preventDefault();
                index = (index + 1) % items.length;
                $(items[index]).trigger('mouseenter');
            } else if (e.key === "ArrowUp") {
                e.preventDefault();
                index = (index - 1 + items.length) % items.length;
                $(items[index]).trigger('mouseenter');
            } else if (e.key === "Enter") {
                e.preventDefault();
                $(items[index]).trigger('mouseup');
                // After selection, move focus to next field
                const currentSelect = $select.get(0);
                setTimeout(() => {
                    const form = currentSelect.closest("form");
                    if (form) {
                        const allFocusable = Array.from(form.querySelectorAll("input, select, textarea"))
                            .filter(el => !el.disabled && !el.readOnly && el.type !== "hidden" && $(el).is(":visible"));
                        const currentIndex = allFocusable.indexOf(currentSelect);
                        if (currentIndex !== -1 && currentIndex < allFocusable.length - 1) {
                            const next = allFocusable[currentIndex + 1];
                            if ($(next).hasClass("select2-hidden-accessible")) {
                                next.focus();
                                setTimeout(() => $(next).select2("open"), 50);
                            } else if ($(next).hasClass("flatpickr-input") && next._flatpickr) {
                                next.focus();
                                setTimeout(() => next._flatpickr.open(), 50);
                            } else {
                                next.focus();
                            }
                        }
                    }
                }, 100);
            }
        });
    });

    $select.on('select2:close', function () {
        $(document).off('keydown.select2Nav');
    });
}



// #endregion


//#region Voucher Type Dropdown
function LoadVoucherTypeDropdown() {
    $.ajax({
        url: '/voucher-Type-dropdown',
        method: 'GET',
        success: function (response) {
            console.log("Voucher Type Dropdown Loaded", response.data);

            var $general = $('#VoucherTypeCode');
            $general.empty().append('<option value="">Select Voucher Type</option>');

            var $filter = $('#filterVoucherType');
            if ($filter.length) {
                var currentFilterVal = $filter.val() || '';
                $filter.empty().append('<option value="">All</option>');
            }

            $.each(response.data, function (index, general) {
                $general.append(
                    '<option value="' + general.id + '">' + general.name + '</option>'
                );
                if ($filter.length) {
                    $filter.append(
                        '<option value="' + general.id + '">' + general.name + '</option>'
                    );
                }
            });

            if ($filter.length && currentFilterVal) {
                $filter.val(currentFilterVal);
            }

            // Initialize Select2 to make it searchable
            // $general.select2({
            //     placeholder: "Voucher Type",
            //     width: '100%',
            //     allowClear: true,
            //     dropdownPosition: 'below'
            // });
            window.reinitCustomDropdownById('VoucherTypeCode');
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('filterVoucherType');
            }

            $general.next('.select2-container').find('.select2-selection__placeholder')
                .html('Voucher Type <span style="color:red">*</span>');
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Voucher Type:', error);
        }
    });
}
//#endregion


//#region Company Dropdown
function VoucherEntryCompanyDropdown() {
    $.ajax({
        url: '/voucerentry-company-dropdown',
        method: 'GET',
        success: function (response) {
            console.log("Company Dropdown Loaded", response.data);

            var $company = $('#CompanyCode');
            $company.empty().append('<option value=""></option>');

            $.each(response.data, function (index, company) {
                $company.append(
                    '<option value="' + company.id + '">' + company.name + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            // $company.select2({
            //     width: '100%',
            //     allowClear: true,
            // });
            if (response.data.length > 0) {
                $company.val(response.data[0].id);
            }
            
            $company.prop("disabled", true);
            window.reinitCustomDropdownById('CompanyCode');
            
            if (response.data.length > 0) {
                $company.trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Company:', error);
        }
    });
}
//#endregion


//#region Company Dropdown On change Event

$("#CompanyCode").on('change', function (e) {
    e.preventDefault();
    const code = $(this).val() || "";
    console.log("Selected Company Code:", code); 
    VoucherEntryBranchDropdown(code);
});

//#endregion


//#region Branch Dropdown
function VoucherEntryBranchDropdown(companyCode, callback) {
    $.ajax({
        url: '/voucerentry-branch-dropdown',
        method: 'GET',
        data: { companycode: companyCode },
        success: function (response) {
            console.log("Branch Dropdown Loaded", response.data);

            var $branch = $('#BranchCode');
            $branch.empty().append('<option value=""></option>');

            $.each(response.data, function (index, branch) {
                $branch.append('<option value="' + branch.id + '">' + branch.name + '</option>');
            });

            // Initialize Select2
            // $branch.select2({
            //     placeholder: "Select Branch",
            //     width: '100%',
            //     allowClear: true
            // });
            window.reinitCustomDropdownById('BranchCode');

            $branch.next('.select2-container').find('.select2-selection__placeholder')
                .html('Select Branch <span style="color:red">*</span>');



            if (callback) callback();

        },
        error: function (xhr, status, error) {
            console.error('Failed to load Branch:', error);
        }
    });
}


//#endregion


//#region Voucher Type Current Date

// function initVoucherDatePicker() {
//     return flatpickr("#VoucherDate", {
//         dateFormat: "d-m-Y",
//         defaultDate: "today",
//         allowInput: true,
//         altInput: true,
//         altFormat: "d-m-Y",
//         locale: {
//             firstDayOfWeek: 5
//         }
//     });
// }


//#endregion


//#region Table Search Start Date

// setDate("#StartDate", new Date()); // initialized in document.ready

//#endregion


//#region  Table Search End Date

// setDate("#EndDate", new Date()); // initialized in document.ready

//#endregion


//#region  When Voucher Type changes

$('#VoucherTypeCode').on('change', function (e, isFromLoad) {
    // If this comes from the initial load (from fillMainVoucherInfo), then I won't do anything
    if (isInitialVoucherLoad || isFromLoad) {
        return;
    }

    const voucherTypeCode = $(this).val();
    //if (!voucherTypeCode) return;

    if (!voucherTypeCode) {
        $('#VoucherNo').val(''); 
        return;
    }

    //The user has now changed it as desired => a new voucher will be generated
    $.ajax({
        url: '/generate-voucher-no/' + voucherTypeCode,
        type: 'GET',
        success: function (response) {
            if (response.voucherNo) {
                $('#VoucherNo').val(response.voucherNo);
            }
        },
        error: function (xhr) {
            console.error('Error generating voucher no:', xhr);
            toastr.error('Failed to generate new voucher number!');
        }
    });
});

//#endregion


//#region Validation Error Border(Common input handler)

// Common handler for all inputs
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('is-invalid');
    }
});
// All Select2 dropdown 
$('#BranchCode').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger'); // error remove
    }
});
$('#VoucherTypeCode').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger'); // error remove
    }
});

//#endregion


//#region  Main Information Validation

function validateSelect2(selector, value, message) {
    const $selection = $(selector).next('.select2-container').find('.select2-selection');
    if (!value) {
        toastr.error(message);
        $selection.addClass('border-danger'); // safer than is-invalid
        return false;
    } else {
        $selection.removeClass('border-danger');
        return true;
    }
}
function MainInfoValidation(mainData) {

    //Branch validation
    if (!validateSelect2('#BranchCode', mainData.BranchCode, "Please Select a Branch")) return false;

    //Voucher Type validation 
    if (!validateSelect2('#VoucherTypeCode', mainData.VoucherType_Code, "Please Select a Voucher Type")) return false;

    //Voucher Date Validation
    if (!mainData.VoucherDate || mainData.VoucherDate.trim() === "") {
        toastr.error("Please enter Voucher Date.");
        $('#VoucherDate').addClass('is-invalid');
        return false;
    } else {
        $('#VoucherDate').removeClass('is-invalid');
    }


    //Voucher No validation
    if (!mainData.VoucherNo) {
        toastr.error("Please Enter Voucher No.");
        $('#VoucherNo').addClass('is-invalid');
        return false;
    } else {
        $('#VoucherNo').removeClass('is-invalid');
    }

    //Naration validation
    if (!mainData.Narration) {
        toastr.error("Please Enter Narration.");
        $('#Narration').addClass('is-invalid');
        return false;
    } else {
        $('#Narration').removeClass('is-invalid');
    }

    return true; // all valid
}

//#endregion


//#region  Main Information Save Or Update

function SaveVoucherEntry() {
    let autoID = parseInt($('#VoucherEntryAutoID').val(), 10) || 0;

    const mainData = {
        autoId: autoID || 0,
        CompanyCode: $('#CompanyCode').val(),
        BranchCode: $('#BranchCode').val(),
        VoucherType_Code: $('#VoucherTypeCode').val(),
        VoucherDate: $('#VoucherDate').val(),
        Narration: $('#Narration').val(),
        VoucherNo: $('#VoucherNo').val(),
        Reference: $('#Reference').val()
    };

    // Frontend Validation
    if (!MainInfoValidation(mainData)) return false;

    // Debit/Credit Validation
    $.ajax({
        url: `/check-debit-credit`,
        type: 'GET',
        success: function (validationResponse) {

            let ajaxUrl = '/voucher-entry';
            let ajaxType = 'POST';
            let failMessage = "Insertion Failed";

            if (isEditModeVoucher && autoID) {
                ajaxUrl = `/voucher-entry/${autoID}`;
                ajaxType = 'PUT';
                failMessage = "Update Failed";
            }

            $.ajax({
                url: ajaxUrl,
                type: ajaxType,
                contentType: 'application/json',
                data: JSON.stringify(mainData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message || (ajaxType === 'POST' ? "Data Saved Successfully!" : "Data Updated Successfully!"));
                        ClearVoucherEntryForm();
                        if (window.GctlGrid) {
                            window.GctlGrid.reload('VoucherEntryTempGrid');
                        }
                        LoadVoucherEntriesTable();
                    } else {
                        toastr.error(response.message || failMessage);
                    }
                },
                error: function (xhr) {
                    toastr.error(failMessage);
                }
            });

        },
        error: function () {
            toastr.error("Debit & Credit Amount must be equal.");
        }
    });
}

//#endregion


//#region  Main Information Section Clear
function ClearVoucherEntryForm() {
    
    isEditModeVoucher = false; 

    $("#VoucherEntryAutoID").val("");
    // $('#CompanyCode').val('').trigger('change');
    // $('#BranchCode').val('').trigger('change');
    // $('#VoucherTypeCode').val('').trigger('change');
    setSelect('#BranchCode', "", false);
    setSelect('#VoucherTypeCode', "", false);
    $('#Narration').val(''); 
    $('#VoucherNo').val('');
    $('#Reference').val('');
    $('#EntryDate').text('');
    $('#UpdateDate').text(''); 
    $(".voucher-checkbox").prop("checked", false);
    $("#VoucherEntrySelectAll").prop("checked", false);
    $('#table-search').val('');

    VoucherEntryCompanyDropdown();
    setDate("#VoucherDate", new Date());
    // Set Entry Date
    $('#EntryDate').text(getBangladeshDateTime());

    $('#VoucherTypeCode').next('.select2-container').find('.select2-selection__placeholder')
        .html('Voucher Type <span style="color:red">*</span>');


    selectedVoucher = [];

    //    DeleteWithUser();

    $("#TrType").val("").trigger('change');
    $("#AccCode").val("").trigger('change');

    $(".form-control").removeClass("is-invalid");

    // Hide all ledger sections
    $('#GroupLedgerValue').closest('.col-md-6').hide();
    $('#ControlLedgerValue').closest('.col-md-6').hide();
    $('#SubControlLedgerValue').closest('.col-md-6').hide();
    $('#SubSidiaryLedgerValue').closest('.col-md-6').hide();

    // Clear their values
    $('#GroupLedgerValue, #ControlLedgerValue, #SubControlLedgerValue, #SubSidiaryLedgerValue').text('');

    $("#Description, #ChequeNo, #Amount").val('');

    // Clear Flatpickr date
    if (typeof setDate === "function") {
        setDate("#ChequeDate", "");
    } else if (document.querySelector("#ChequeDate")) {
        if ($("#ChequeDate")[0]._flatpickr) {
            $("#ChequeDate")[0]._flatpickr.clear();
        } else {
            $('#ChequeDate').val('');
        }
    }

    $('#AccCode').next('.select2-container').find('.select2-selection__placeholder')
        .html('Account Head <span style="color:red">*</span>');

    $('#TrType').next('.select2-container').find('.select2-selection__placeholder')
        .html('Tr. Type <span style="color:red">*</span>');

    $('#TrType').next('.select2-container').find('.select2-selection')
        .removeClass('border-danger');

    $('#AccCode').next('.select2-container').find('.select2-selection')
        .removeClass('border-danger');
}
//#endregion


function LoadVoucherEntriesTable() {
    if ($.fn.DataTable.isDataTable('#voucherEntryTable')) {
        if (window.GctlGrid) {
            window.GctlGrid.reload('voucherEntryTable');
        }
    } else {
        if (window.GctlGrid) {
            window.GctlGrid.initFromModel({ 
                gridId: 'voucherEntryTable',
                ajaxData: function (d) {
                    d.fromDate = $('#dateFrom').val();
                    d.toDate = $('#dateTo').val();
                    d.voucherTypeCode = $('#filterVoucherType').val() || '';
                }
            });
        }
    }
}
//#endregion 





//#region Click Handler for Edit Information

$(document).on("click", ".js-VoucherEntry-delete-confirm", function (e) {
    e.preventDefault();
    
    let ids = [];
    var $checked = $('#voucherEntryTable tbody input[type="checkbox"]:checked');
    if ($checked.length > 0) {
        ids = $checked.map(function() { return $(this).val(); }).get();
    }
    if (!ids || ids.length === 0) {
        ids = selectedVoucher;
    }
    ids = ids.map(x => parseInt(x)).filter(x => !isNaN(x));

    if (ids.length === 0) {
        toastr.warning("No voucher selected to delete!");
        return;
    }

    // Open Modal
    var $modal = $("#deleteModal");
    if ($modal.length) {
        $modal.modal('show');
    }
});

// Confirm Delete Click
$(document).on("click", "#deleteModal .js-delete, #deleteModal .js-gc-delete-confirm", function (e) {
    e.preventDefault();
    
    // Ensure we only try to delete if we actually have selection
    let ids = [];
    var $checked = $('#voucherEntryTable tbody input[type="checkbox"]:checked');
    if ($checked.length > 0) {
        ids = $checked.map(function() { return $(this).val(); }).get();
    }
    if (!ids || ids.length === 0) {
        ids = selectedVoucher;
    }
    ids = ids.map(x => parseInt(x)).filter(x => !isNaN(x));
    
    if (ids.length > 0) {
        BulkDeleteSelectedVouchers();
        DeleteWithUser();
        $("#deleteModal").modal('hide');
    }
});

$(document).on('click', '.voucher-no-btn, .js-VoucherEntry-edit', function (e) {
    e.preventDefault();
    const autoId = $(this).data('autoid') || $(this).data('id');
    const voucherCode = $(this).attr('data-voucher-entry-code-no') || $(this).data('voucher-entry-code-no') || $(this).data('voucherentrycodeno');
    const targetIdentifier = voucherCode || autoId;
    
    DetailsCopiedToTmptable(targetIdentifier);

    $.ajax({
        url: '/voucher-entry-details/' + targetIdentifier,
        type: 'GET',
        success: function (response) {
            console.log("Full Response:", response);

            if (response.success && response.data) {
                const main = response.data?.main;
                //const details = response.data?.details;

                if (!main) {
                    toastr.error("Main voucher data not found");
                    return;
                }

                // Call functions
                fillMainVoucherInfo(main);
                //LoadTmpVoucherDetailsTable();   // Load Tmp table Data
                //renderVoucherDetailsTable(details);

                const currentAutoId = main.autoId || autoId;
                selectedVoucher = [currentAutoId];
                $('.voucher-checkbox').prop('checked', false);
                $(`.voucher-checkbox[data-autoid="${currentAutoId}"]`).prop('checked', true);

                VoucherupdateCheckboxState();
             


            } else {
                toastr.error(response.message || "Failed to load voucher details");
            }
        },
        error: function (xhr) {
            console.error("Error fetching voucher details:", xhr);
        }
    });
});

//#endregion


//#region Get Main Informaiton
function fillMainVoucherInfo(main) {
    isEditModeVoucher = true;
    isInitialVoucherLoad = true; //Now we will set the data in edit mode

    //I am setting the Voucher Type but I don't want to fire the event
    $('#VoucherEntryAutoID').val(main.autoId || 0);
    $('#VoucherEntryCodeNo').val(main.voucherEntryCodeNo || '');

    if (typeof setSelect === "function") {
        setSelect('#VoucherTypeCode', main.voucherType_Code, false);
        setSelect('#BranchCode', main.branchCode, false);
    } else {
        $('#VoucherTypeCode').val(main.voucherType_Code).trigger('change.select2', [true]); 
        $('#BranchCode').val(main.branchCode).trigger('change.select2');
    }
    
    $('#VoucherNo').val(main.voucherNo);
    $('#Reference').val(main.reference || '');
    $('#Narration').val(main.narration);

    // datepicker handle
    if (main.voucherDate) {
        if (typeof setDate === "function") {
            setDate("#VoucherDate", new Date(main.voucherDate));
        } else {
            const fp = document.querySelector("#VoucherDate")._flatpickr;
            if (fp) fp.setDate(new Date(main.voucherDate), true);
        }
    } else {
        if (typeof setDate === "function") {
            setDate("#VoucherDate", "");
        } else {
            const fp = document.querySelector("#VoucherDate")._flatpickr;
            if (fp) fp.clear();
        }
    }

    if (main.ldate) {
        let entryDate = new Date(main.ldate).toLocaleString('en-GB', {
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

    if (main.modifyDate) {
        let updateDate = new Date(main.modifyDate).toLocaleString('en-GB', {
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

    //The load is finished now, a new voucher will be generated if there is a change later.
    setTimeout(() => { isInitialVoucherLoad = false; }, 500);
}

//#endregion


//#region Master & Details Table Data Delete
function BulkDeleteSelectedVouchers() {
    // Collect selected row IDs
    let ids = [];
    var $checked = $('#voucherEntryTable tbody input[type="checkbox"]:checked');
    if ($checked.length > 0) {
        ids = $checked.map(function() { return $(this).val(); }).get();
    }
    
    if (!ids || ids.length === 0) {
        ids = selectedVoucher;
    }
    
    ids = ids.map(x => parseInt(x)).filter(x => !isNaN(x));


    if (ids.length === 0) {
        toastr.warning("No voucher selected to delete!");
        return;
    }

    // AJAX DELETE
    $.ajax({
        url: '/voucher-master-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess) {
                toastr.success(response.message || "Data Deleted Successfully!");

                // Clear form, reload table, reset selection
                ClearVoucherEntryForm();
                selectedVoucher = []; 
                $("#VoucherEntrySelectAll").prop("checked", false);

                if (window.GctlGrid) {
                    window.GctlGrid.reload('voucherEntryTable');
                } else {
                    LoadVoucherEntriesTable(VoucherEntrycurrentPage); // Refresh table
                }
            } else {
                toastr.error(response.message || "Failed to delete selected vouchers!");
            }
        },
        error: function (xhr, status, error) {
            console.error("Error deleting vouchers:", error);
            toastr.error("Server error occurred while deleting vouchers!");
        }
    });
}
//#endregion


//#region Checkbox handler
function VoucherbindCheckboxHandlers() {
    $(document).off('click', '.voucher-checkbox');
    $(document).on('click', '.voucher-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        if ($(this).prop('checked')) {
            if (!selectedVoucher.includes(id)) selectedVoucher.push(id);
        } else {
            const index = selectedVoucher.indexOf(id);
            if (index !== -1) selectedVoucher.splice(index, 1);
        }

        const allChecked = $('.voucher-checkbox:visible').length > 0 &&
            $('.voucher-checkbox:visible:not(:checked)').length === 0;
        $('#VoucherEntrySelectAll').prop('checked', allChecked);

        console.log('Selected Vouchers:', selectedVoucher);
    });
}

//#endregion


//#region Update checkbox state after render
function VoucherupdateCheckboxState() {
    $('.voucher-checkbox').each(function () {
        const id = $(this).data('autoid');
        $(this).prop('checked', selectedVoucher.includes(id));
    });

    const allChecked = $('.voucher-checkbox:visible').length > 0 &&
        $('.voucher-checkbox:visible:not(:checked)').length === 0;
    $('#VoucherEntrySelectAll').prop('checked', allChecked);
}

//#endregion


//#region Report of Voucher Pdf View & Excel Download

$('#PreviewBtn').on('click', function () {
    // checked voucher IDs
    let selectedVoucherIds = [];
    $('.voucher-checkbox:checked').each(function () {
        selectedVoucherIds.push($(this).data('autoid'));
    });

    if (selectedVoucherIds.length === 0) {
        alert("Please select at least one voucher.");
        return;
    }

    // create preview URL
    let url = '/PdfPreview?ids=' + selectedVoucherIds.join(',');

    // open a new tab
    let previewWindow = window.open('', '_blank');

    // write HTML content into new tab
    previewWindow.document.write(`
        <html>
        <head>
            <title>Voucher Preview</title>
                <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css" />
            <style>
                body { margin: 0; padding: 0; font-family: Arial, sans-serif; }
                .top-bar { background: #f2f2f2; padding: 10px; display: flex; gap: 10px; align-items: center; }
                iframe { width: 100%; height: calc(100vh - 50px); border: none; }
            </style>
        </head>
        <body>
         <div style="text-align: center; margin-bottom: 10px;">
                <!-- PDF button -->
                <button onclick="window.location.href='${url.replace('PdfPreview', 'PdfDownload')}'" style="margin-right: 10px; padding: 5px 15px; font-size: 14px;">
                    <i class="fas fa-file-pdf"></i> PDF
                </button>

                <!-- Excel button -->
                <button onclick="window.location.href='${url.replace('PdfPreview', 'ExcelDownload')}'" style="padding: 5px 15px; font-size: 14px;">
                    <i class="fas fa-file-excel"></i> Excel
                </button>
        </div>
            <iframe src="${url}"></iframe>
        </body>
        </html>
    `);

    previewWindow.document.close();
});

//#endregion


//#region Details Data Copied to Tmp Table

function DetailsCopiedToTmptable(voucherId) {
    $.ajax({
        url: `/details-copied-tmptable/${voucherId}`,
        type: 'POST',
        success: function (response) {
            if (response.success) {
                console.log("Copied Succesfully");
                if (window.GctlGrid) {
                    window.GctlGrid.reload('VoucherEntryTempGrid');
                }
            }
            else {
                toastr.error(response.message || "Failed to copy details to temp table");
            }
        },
        error: function (xhr, status, error) {
            console.error("Error copying details to temp table:", xhr.responseText, error);        }
    });
}


function DeleteWithUser() {
    $.ajax({
        url: `/delete-with-user`,
        type: 'DELETE',
        success: function (response) {
            console.log("Data Delete with User ID");
            //LoadTmpVoucherDetailsTable();
        },
        error: function (xhr, status, error) {
            console.log("Failed Deleted With USerID",error);
        }
    })
}

//#endregion


//#region Function to get current date in Bangladesh time

function getBangladeshDateTime() {
    // BD timezone offset is +6:00 from UTC
    let now = new Date();
    let utc = now.getTime() + (now.getTimezoneOffset() * 60000);
    let bdOffset = 6 * 60 * 60 * 1000;
    let bdTime = new Date(utc + bdOffset);

    // Format DD/MM/YYYY hh:MM AM/PM
    let day = bdTime.getDate().toString().padStart(2, '0');
    let month = (bdTime.getMonth() + 1).toString().padStart(2, '0');
    let year = bdTime.getFullYear();

    let hours = bdTime.getHours();
    let minutes = bdTime.getMinutes().toString().padStart(2, '0');

    let ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // 0 => 12

    return `${day}/${month}/${year} ${hours}:${minutes} ${ampm}`;
}

//#endregion