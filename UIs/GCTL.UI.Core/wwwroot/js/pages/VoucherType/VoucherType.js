let selectedVoucherType = [];
let VouchercurrentPage = 1;
let VoucherpageSize = 5;
let VouchercurrentSortColumn = 'VoucherType_Code';
let VouchercurrentSortDirection = 'desc';
console.log(`Global Call on Voucher Page: ${selectedVoucherType}`);

//#region Validation Error Border(Common input handler)

// Common handler for all inputs
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('is-invalid');
    }
});


//#endregion


//#region Reset Duration Type Dropdown
function LoadResetDurationDropdown() {
    $.ajax({
        url: '/reset-duration-dropdown',
        method: 'GET',
        success: function (response) {
            console.log("Duration Type Loaded", response.data);

            var $duration = $('#resetDurationTypeCode');
            $duration.empty().append('<option value="">Select Duration Type</option>');

            $.each(response.data, function (index, duration) {
                $duration.append(
                    '<option value="' + duration.id + '">' + duration.name + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            $duration.select2({
                placeholder: "Duration Type",
                width: '100%',
                allowClear: true, 
            });
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Duration Type:', error);
        }
    });
}
//#endregion


//#region Voucher Type Validation
function ValidateVoucherType(VoucherTypeData) {
    // Voucher Type Name validation
    if (!VoucherTypeData.Voucher_TypeName) {
        toastr.error("Please Enter Voucher Type.");
        $('#VoucherTypeName').addClass('is-invalid');
        return false;
    } else {
        $('#VoucherTypeName').removeClass('is-invalid');
    }

    // Description validation
    if (!VoucherTypeData.Description) {
        toastr.error("Please Enter Description.");
        $('#Description').addClass('is-invalid');
        return false;
    } else {
        $('#Description').removeClass('is-invalid');
    }

    // Numbering Method validation
    if (!VoucherTypeData.numberingMethod) {
        toastr.error("Please Select Numbering Method.");
        $('#numberingMethod').addClass('is-invalid');
        return false;
    } else {
        $('#numberingMethod').removeClass('is-invalid');
    }

    // Reset Duration validation
    if (!VoucherTypeData.resetDuration) {
        toastr.error("Please Enter Reset Duration.");
        $('#resetDuration').addClass('is-invalid');
        return false;
    } else {
        $('#resetDuration').removeClass('is-invalid');
    }

    // Prefix validation
    if (!VoucherTypeData.prefix) {
        toastr.error("Please Enter Prefix.");
        $('#prefix').addClass('is-invalid');
        return false;
    } else {
        $('#prefix').removeClass('is-invalid');
    }

    // Padding validation
    if (!VoucherTypeData.padding) {
        toastr.error("Please Enter Padding.");
        $('#padding').addClass('is-invalid');
        return false;
    } else {
        $('#padding').removeClass('is-invalid');
    }

    return true; // all valid
}

//#endregion


//#region Save Or Update Voucher Type
function SaveOrUpdateVoucherType() {
    console.log("Save Or Update Voucher Function Called");

    let autoID = $('#VoucherautoId').val();
    let VoucherTypeData = {
        autoId: autoID || 0,
        VoucherType_Code: "", 
        Voucher_TypeName: $("#VoucherTypeName").val(),
        Description: $("#Description").val(),
        numberingMethod: $("#numberingMethod").val(),
        startingNumber: $("#startingNumber").val() ? parseFloat($("#startingNumber").val()) : 0,
        resetDuration: $("#resetDuration").val() ? parseFloat($("#resetDuration").val()) : 0,
        resetDurationType_Code: $("#resetDurationTypeCode").val(),
        prefix: $("#prefix").val(),
        suffix: $("#suffix").val(),
        padding: $("#padding").val() ? parseFloat($("#padding").val()) : 0
    };

    //validation
    if (!ValidateVoucherType(VoucherTypeData)) {
        console.log("Validation Failed!");
        return false; // stop if validation fails
    }


    //Check for duplicate
    $.ajax({
        url: '/voucher-type/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(VoucherTypeData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/voucher-type';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/voucher-type/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(VoucherTypeData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        ClearVoucherForm();
                        selectedVoucherType = [];
                        LoadAllVoucher(1, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);

                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error Voucher Type :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Voucher Type :', error);
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


//#region Voucher Form Clear
function ClearVoucherForm() {
    $("#VoucherautoId").val("");
    $("#VoucherTypeName").val("");
    $("#Description").val("");
    $("#startingNumber").val("");
    $("#resetDuration").val("");
    $("#resetDurationTypeCode").val("").trigger('change');
    $("#prefix").val("");
    $("#suffix").val("");
    $("#padding").val("");
    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#VoucherdisplayModifyDate').text('');
    $('#VoucherdisplayLDate').text('');
    $(".voucher-checkbox").prop("checked", false);
    $("#VoucherselectAll").prop("checked", false);
    $('#VouchersearchInput').val('');

    // Set Entry Date
    $('#VoucherdisplayLDate').text(getBangladeshDateTime());

    selectedVoucherType = [];
    VouchercurrentPage = 1;
    const emptySearch = '';
    LoadAllVoucher(1, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection, emptySearch);
}
//#endregion


//#region Load All Voucher Data
function LoadAllVoucher(page = 1, VoucherpageSize = 5, sortColumn = VouchercurrentSortColumn, sortDirection = VouchercurrentSortDirection, searchTerm = "") {

    $.ajax({
        url: '/voucher-type-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: VoucherpageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection
        },
        success: function (response) {
            console.log("Voucher Type Data:", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm voucher-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer voucher-type"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.voucherType_Code}
                    </td>
                    <td>${item.voucher_TypeName || ''}</td>
                    <td>${item.description || ''}</td>
                    <td>${item.numberingMethod || ''}</td>
                    <td class="text-center">${item.startingNumber || ''}</td>
                    <td class="text-center">${item.resetDuration || ''}</td>
                    <td>${item.durationTypeName || ''}</td>
                    <td>${item.prefix || ''}</td>
                    <td>${item.suffix || ''}</td>
                 </tr>`;
            });
            $('#VouchertblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#VoucherpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#VoucherpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#VouchertotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            VouchercurrentPage = page;
            VouchergeneratePageButtons(VouchercurrentPage, totalPages);
            VoucherupdateCheckboxState();
            VoucherupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit
$(document).on('click', '.voucher-type', function () {
    const id = $(this).data('autoid');
    console.log("Edited Voucher Type Code:", id);

    if (!id) return;

    $.ajax({
        url: `/voucher-type/details/${id}`,
        type: 'GET',
        success: function (data) {
            // Populate form fields
            $('#VoucherautoId').val(id);
            $('#VoucherTypeName').val(data.voucher_TypeName || '');
            $('#Description').val(data.description || '');
            $('#numberingMethod').val(data.numberingMethod || '');
            $('#startingNumber').val(data.startingNumber || '');
            $('#resetDuration').val(data.resetDuration || '');
            $('#resetDurationTypeCode').val(data.resetDurationTypeCode || '');
            $('#prefix').val(data.prefix || '');
            $('#suffix').val(data.suffix || '');
            $('#padding').val(data.padding || '');

            // Display LDate if available

            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#VoucherdisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#VoucherdisplayLDate').text('');
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
                $('#VoucherdisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#VoucherdisplayModifyDate').text('');
            }

            // Optionally clear previous selections
            selectedVoucherType = [parseInt(id)];

            // Uncheck all row checkboxes to prevent conflict
            $('.voucher-checkbox').prop('checked', false);
            $("#VoucherselectAll").prop('checked', false); 

            $(`.voucher-checkbox[data-autoid="${id}"]`).prop('checked', true);

            VoucherupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Voucher Type details.");
            console.error("Error fetching details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Voucher
function DeleteVoucherType() {
    // Filter only valid IDs
    const ids = selectedVoucherType.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("Voucher IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    //URL for both single & multiple delete
    const url = '/voucher-type-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            // Clear form, reload table, reset selection
            ClearVoucherForm();
            LoadAllVoucher(VouchercurrentPage, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
            selectedVoucherType = [];
            $("#VoucherselectAll").prop("checked", false);


        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion


//#region Select all checkbox
$(document).on('click', '#VoucherselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.voucher-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.voucher-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedVoucherType.includes(id)) {
                selectedVoucherType.push(id);
            }
        });
    } else {
        $('.voucher-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedVoucherType.indexOf(id);
            if (index !== -1) {
                selectedVoucherType.splice(index, 1);
            }
        });
    }
    console.log("Selected Voucher Type:", selectedVoucherType);
});
//#endregion


//#region Sort & Pagination Helpers
function VoucherupdateSortIndicators() {
    $('.Vouchersortable').removeClass('sort-asc sort-desc');
    $('.Vouchersortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Vouchersortable[data-column="${VouchercurrentSortColumn}"]`);
    header.addClass(VouchercurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(VouchercurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function VouchergeneratePageButtons(VouchercurrentPage, totalPages) {
    const navigationDiv = $('#VoucherpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadAllVoucher(page, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection, $('#VouchersearchInput').val().trim()));

        if (page === VouchercurrentPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    // Always show first page
    navigationDiv.append(createButton(1));

    if (VouchercurrentPage > 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (VouchercurrentPage > 1 && VouchercurrentPage < totalPages) {
        navigationDiv.append(createButton(VouchercurrentPage));
    }

    if (VouchercurrentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

function VoucherupdateCheckboxState() {
    $('.voucher-checkbox').each(function () {
        const bankID = $(this).data('autoid');
        $(this).prop('checked', selectedVoucherType.includes(bankID));
    });
    const allChecked = $('.voucher-checkbox:visible').length > 0 &&
        $('.voucher-checkbox:visible:not(:checked)').length === 0;
    $('#VoucherselectAll').prop('checked', allChecked);
}

function debounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function VoucherbindCheckboxHandlers() {
    $(document).off('click', '.voucher-checkbox');
    $(document).on('click', '.voucher-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedVoucherType.includes(id)) {
                selectedVoucherType.push(id);
            }
        } else {
            const index = selectedVoucherType.indexOf(id);
            if (index !== -1) {
                selectedVoucherType.splice(index, 1);
            }
        }

        const allChecked = $('.voucher-checkbox:visible').length > 0 &&
            $('.voucher-checkbox:visible:not(:checked)').length === 0;
        $('#VoucherselectAll').prop('checked', allChecked);

        console.log("Selected Voucher Type:", selectedVoucherType);
    });
}
//#endregion


//#region Ready Part

$(document).ready(function () {
    console.log("Voucher Type Page Loaded");

    LoadAllVoucher();
    LoadResetDurationDropdown();

    // Set Entry Date
    $('#VoucherdisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("submit", "#VoucherTypeForm", function (e) {
        e.preventDefault();
        SaveOrUpdateVoucherType();
    });

    // Handle Delete Button
    $(document).on("click", ".VoucherdeleteBtn", function (e) {
        e.preventDefault();
        DeleteVoucherType();
    });

    // Handle Clear button
    $(document).on("click", ".VoucherresetBtn", function () {
        console.log("Clerar Button Clicked")
        ClearVoucherForm();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".favBtn").on("click", function () {
        toggleFavorite();
    });

    //Pagination Handle
    $(document).on('input', '.VouchersearchInput', debounce(function () {
        const searchValue = $('.VouchersearchInput').val();
        LoadAllVoucher(1, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection, searchValue);
    }, 500));

    $(document).on('click', '#VoucherfirstPage', function () {
        VouchercurrentPage = 1;
        LoadAllVoucher(VouchercurrentPage, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
    });

    $(document).on('click', '#VoucherprevPage', function () {
        console.log("previous page called ");
        if (VouchercurrentPage > 1) {
            VouchercurrentPage--;
            LoadAllVoucher(VouchercurrentPage, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
        }
    });

    $(document).on('click', '#VouchernextPage', function () {
        VouchercurrentPage++;
        LoadAllVoucher(VouchercurrentPage, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
    });

    $(document).on('click', '#VoucherlastPage', function () {
        let lastPage = $('#VoucherpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            VouchercurrentPage = lastPage;
            LoadAllVoucher(VouchercurrentPage, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
        }
    });

    $(document).on('change', '.VoucherpageSize', function () {
        VoucherpageSize = parseInt($(this).val());
        LoadAllVoucher(1, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection);
    });

    $(document).on('click', '.Vouchersortable', function () {
        const column = $(this).data('column');

        if (column === VouchercurrentSortColumn) {
            VouchercurrentSortDirection = (VouchercurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            VouchercurrentSortColumn = column;
            VouchercurrentSortDirection = 'desc';
        }

        LoadAllVoucher(1, VoucherpageSize, VouchercurrentSortColumn, VouchercurrentSortDirection, $('#VouchersearchInput').val().trim());
    });

    VoucherbindCheckboxHandlers();
});

//#endregion

//#region Bangladesh Date and Time Formate 
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