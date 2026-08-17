
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

    LoadVoucherTypeDropdown();
    LoadAccountHeadDropdown();
    VoucherEntryCompanyDropdown();
    VoucherEntryBranchDropdown();

    LoadVoucherEntriesTable();
    initVoucherDatePicker();
    DeleteWithUser();

    // Set Entry Date
    $('#EntryDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("click", ".VoucherEntrysaveBtn", function (e) {
        e.preventDefault();
        SaveVoucherEntry();
    });

    //// Handle Delete Button
    $(document).on("click", ".VoucherEntrydeleteBtn", function (e) {
        e.preventDefault();
        BulkDeleteSelectedVouchers();
        DeleteWithUser();

    });

    //// Handle Clear button
    $(document).on("click", ".VoucherEntryresetBtn", function () {
        console.log("Clerar Button Clicked")
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

            $.each(response.data, function (index, general) {
                $general.append(
                    '<option value="' + general.id + '">' + general.name + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            $general.select2({
                placeholder: "Voucher Type",
                width: '100%',
                allowClear: true,
                dropdownPosition: 'below'
            });

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
            $company.select2({
                width: '100%',
                allowClear: true,
            });
            if (response.data.length === 1) {
                $company.val(response.data[0].id).trigger('change');
            }
            $company.prop("disabled", true);
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
            $branch.select2({
                placeholder: "Select Branch",
                width: '100%',
                allowClear: true
            });

            $branch.next('.select2-container').find('.select2-selection__placeholder')
                .html('Select Branch <span style="color:red">*</span>');



            // trigger callback if provided
            if (callback) callback();

           // enableSelect2ArrowNavigation("#BranchCode");

            //$branch.select2('open');
            $branch.select2('focus');

        },
        error: function (xhr, status, error) {
            console.error('Failed to load Branch:', error);
        }
    });
}


//#endregion


//#region Voucher Type Current Date

function initVoucherDatePicker() {
    return flatpickr("#VoucherDate", {
        dateFormat: "d-m-Y",
        defaultDate: "today",
        allowInput: true,
        altInput: true,
        altFormat: "d-m-Y",
        locale: {
            firstDayOfWeek: 5
        }
    });
}


//#endregion


//#region Table Search Start Date

flatpickr("#StartDate", {
    dateFormat: "d-m-Y",
    allowInput: true,
    altInput: true,
    altFormat: "d-m-Y",
    locale: {
        firstDayOfWeek: 5
    }
});

//#endregion


//#region  Table Search End Date

flatpickr("#EndDate", {
    dateFormat: "d-m-Y",
    allowInput: true,
    altInput: true,
    altFormat: "d-m-Y",
    locale: {
        firstDayOfWeek: 5
    }
});

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
        VoucherDate: new Date($('#VoucherDate').val().split('-').reverse().join('-')).toISOString(),
        Narration: $('#Narration').val(),
        VoucherNo: $('#VoucherNo').val()
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
                        LoadTmpVoucherDetailsTable();
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
    $('#CompanyCode').val('').trigger('change');
    $('#BranchCode').val('').trigger('change');
    $('#VoucherTypeCode').val('').trigger('change');
    $('#Narration').val(''); 
    $('#VoucherNo').val('');
    $('#EntryDate').text('');
    $('#UpdateDate').text(''); 
    $(".voucher-checkbox").prop("checked", false);
    $("#VoucherEntrySelectAll").prop("checked", false);
    $('#table-search').val('');

    VoucherEntryCompanyDropdown();
    initVoucherDatePicker();

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
    if (document.querySelector("#ChequeDate") && $("#ChequeDate")[0]._flatpickr) {
        $("#ChequeDate")[0]._flatpickr.clear();
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


//#region Load Voucher Entries Table
function LoadVoucherEntriesTable(page = 1, pageSize = VoucherEntrypageSize, sortColumn = VoucherEntrySortColumn, sortOrder = VoucherEntrySortOrder, searchTerm = VoucherEntrySearchTerm)
{
    const startDateVal = $('#StartDate').val();
    const endDateVal = $('#EndDate').val();

    // Convert from "dd-mm-yyyy" → "yyyy-MM-dd"
    const startDate = startDateVal ? startDateVal.split('-').reverse().join('-') : null;
    const endDate = endDateVal ? endDateVal.split('-').reverse().join('-') : null;

    $.ajax({
        url: '/entry-voucher-list',
        type: 'GET',
        data: { pageNumber: page, pageSize, sortColumn, sortOrder, searchTerm, startDate, endDate },
        success: function (response) {
            const data = response.Data || response.data || [];
            const pagination = response.PaginationInfo || response.paginationInfo || {};
            const totalRecords = response.TotalCount || response.totalCount || data.length;

            renderVoucherTable(data);
            renderPagination(pagination, totalRecords);

            // Update global page and checkboxes
            VoucherEntrycurrentPage = pagination.CurrentPage ?? pagination.currentPage ?? 1;

        },
        error: function (xhr, status, error) {
            console.error('Error fetching voucher entries:', error);
            $('#VoucherEntrytable').html(`<tr><td colspan="7" class="text-center">Failed to load data</td></tr>`);
        }
    });
}
//#endregion 


//#region Render Table Rows

function renderVoucherTable(data) {
    if (!data || data.length === 0) {
        $('#VoucherEntrytable').html(`<tr><td colspan="7" class="text-center">No Voucher Found</td></tr>`);
        return;
    }

    const rows = data.map(item => `
        <tr>
            <td class="text-center">
                <input type="checkbox" class="form-check-input voucher-checkbox" data-autoid="${item.autoId}" style="width:12px; height:12px;">
            </td>
            <td class="text-center">
                <button type="button" class="voucher-no-btn btn btn-transfer tooltip-btn" data-autoid="${item.autoId}" data-tooltip="Edit">
                    <i class="fas fa-edit me-2"></i> ${item.voucherNo}
                </button>
            </td>
            <td class="text-center">
                ${item.voucherDate ? new Date(item.voucherDate).toLocaleDateString('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' }) : ''}
            </td>
            <td>${item.narration || ''}</td>
            <td>${item.branchName || ''}</td>
            <td class="text-end">${item.amount ?? 0}</td>
            <td>${item.invoiceNo || ''}</td>
        </tr>
    `).join('');

    $('#VoucherEntrytable').html(rows);

    VoucherbindCheckboxHandlers();
    VoucherupdateCheckboxState();
}
//#endregion


//#region Render Pagination

function renderPagination(pagination, totalRecords) {
    const currentPage = pagination.CurrentPage ?? pagination.currentPage ?? 1;
    const totalPages = pagination.TotalPages ?? pagination.totalPages ?? 1;
    const startItem = pagination.StartItem ?? pagination.startItem ?? 1;
    const endItem = pagination.EndItem ?? pagination.endItem ?? 1;

    $('#Entrypaginationtext').text(`Showing ${startItem} to ${endItem} of ${totalRecords} entries`);

    let buttons = '';
    buttons += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <a class="page-link" href="#" data-page="${currentPage - 1}">Previous</a>
                </li>`;

    for (let i = 1; i <= totalPages; i++) {
        buttons += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${i}">${i}</a>
                    </li>`;
    }

    buttons += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <a class="page-link" href="#" data-page="${currentPage + 1}">Next</a>
                </li>`;

    $('#paginationbutton').html(buttons);
}

//#endregion


//#region Sorting Handler
$(document).on('click', '.VoucherEntrySortable', function () {
    const column = $(this).data('column');

    if (VoucherEntrySortColumn === column) {
        VoucherEntrySortOrder = (VoucherEntrySortOrder === 'asc') ? 'desc' : 'asc';
    } else {
        VoucherEntrySortColumn = column;
        VoucherEntrySortOrder = 'asc';
    }

    // active column indication
    $('.VoucherEntrysortable').removeClass('sorting-asc sorting-desc');
    $(this).addClass(VoucherEntrySortOrder === 'asc' ? 'sorting-asc' : 'sorting-desc');

    // reload table
    LoadVoucherEntriesTable(VoucherEntrycurrentPage, VoucherEntrypageSize, VoucherEntrySortColumn, VoucherEntrySortOrder, VoucherEntrySearchTerm);
});
//#endregion


//#region Click Handler for Edit Information

$(document).on('click', '.voucher-no-btn', function () {

    const autoId = $(this).data('autoid');

    DetailsCopiedToTmptable(autoId);

    $.ajax({
        url: '/voucher-entry-details/' + autoId,
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
                LoadTmpVoucherDetailsTable();   // Load Tmp table Data
                //renderVoucherDetailsTable(details);

                selectedVoucher = [autoId];
                $('.voucher-checkbox').prop('checked', false);
                $(`.voucher-checkbox[data-autoid="${autoId}"]`).prop('checked', true);

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
    $('#VoucherTypeCode') .val(main.voucherType_Code).trigger('change.select2', [true]); 
    $('#VoucherNo').val(main.voucherNo);
    $('#Narration').val(main.narration);
    $('#BranchCode').val(main.branchCode).trigger('change.select2');

    // datepicker handle
    const fp = flatpickr("#VoucherDate", { dateFormat: "d-m-Y", allowInput: true });
    if (main.voucherDate) {
        fp.setDate(new Date(main.voucherDate), true);
    } else {
        fp.clear();
    }

    if (main.lDate) {
        let entryDate = new Date(main.lDate).toLocaleString('en-GB', {
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
    const ids = selectedVoucher.map(x => parseInt(x)).filter(x => !isNaN(x));


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

                LoadVoucherEntriesTable(VoucherEntrycurrentPage); // Refresh table
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
            LoadTmpVoucherDetailsTable();
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