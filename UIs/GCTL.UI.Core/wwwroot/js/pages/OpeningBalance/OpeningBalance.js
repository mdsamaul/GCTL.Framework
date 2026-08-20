let selectedBalance = []; 
let BalancecurrentPage = 1;
let BalancepageSize = 5;
let BalancecurrentSortColumn = 'ComOpeningBalanceCode';
let BalancecurrentSortDirection = 'desc';
console.log(`Global Call on Opening Balance Page: ${selectedBalance}`);

//#region Bangladeshi Date Formate

// Reusable function to format date as DD/MM/YYYY
function formatBangladeshDate(dateValue) {
    if (!dateValue) return '';
    const date = new Date(dateValue);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0'); // Month 0-based
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}


//#endregion


//#region Validation Error Border(Common input handler)

// Common handler for all inputs
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('is-invalid');
    }
});
// All Select2 dropdown 
$('#CompanyCode, #BranchCode, #SubSusidiaryLedgerCodeNo').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger'); // error remove
    }
});
// TrType validation remove on change
$('#TrType').on('change', function () {
    if ($(this).val()) {
        $(this).removeClass('is-invalid');
    }
});


//#endregion


//#region Account Head Dropdown
function LoadGeneralLedgerDropdown() {
    $.ajax({
        url: '/openingUI-General-Ledger-dropdown',
        method: 'GET',
        success: function (response) {
            console.log("General Ledger Dropdown Loaded", response.data);

            var $general = $('#SubSusidiaryLedgerCodeNo');
            $general.empty().append('<option value="">Select Account Head</option>');

            $.each(response.data, function (index, general) {
                $general.append(
                    '<option value="' + general.id + '">' + general.name + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            $general.select2({
                placeholder: "Account Head",
                width: '100%',
                allowClear: true,
                dropdownPosition: 'below'
            });

            $general.next('.select2-container').find('.select2-selection__placeholder')
                .html('Account Head <span style="color:red">*</span>');
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Account Head:', error);
        }
    });
}
//#endregion


//#region Company Dropdown
function LoadCompanyDropdown() {
    $.ajax({
        url: '/openingUI-company-dropdown',
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
    console.log("Selected Company Code:", code); // Debug
    LoadBranchDropdown(code);
});
//#endregion


//#region Branch Dropdown
function LoadBranchDropdown(companyCode, callback) {
    $.ajax({
        url: '/openingUI-branch-dropdown',
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
                placeholder:"Select Branch",
                width: '100%',
                allowClear: true
            });

            $branch.next('.select2-container').find('.select2-selection__placeholder')
                .html('Select Branch <span style="color:red">*</span>');

            // trigger callback if provided
            if (callback) callback();
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Branch:', error);
        }
    });
}
//#endregion


//#region Opening Balance Validation
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

function OpeningBalanceValidate(BalanceData) {
    // Company Name validation 
    if (!validateSelect2('#CompanyCode', BalanceData.CompanyCode, "Please Select a Company")) return false;

    // Branch validation 
    if (!validateSelect2('#BranchCode', BalanceData.BranchCode, "Please Select a Branch")) return false;

    // Account head validation
    if (!validateSelect2('#SubSusidiaryLedgerCodeNo', BalanceData.SubSusidiaryLedgerCodeNo, "Please Select an Account Head")) return false;

    // Opening Balance validation 
    if (!BalanceData.OpeningBalance) {
        toastr.error("Please Enter Opening Balance.");
        $('#OpeningBalance').addClass('is-invalid');
        return false;
    } else {
        $('#OpeningBalance').removeClass('is-invalid');
    }

    // Tr Type validation
    if (!BalanceData.TrType) {
        toastr.error("Please Select a Tr Type.");
        $('#TrType').addClass('is-invalid');
        return false;
    } else {
        $('#TrType').removeClass('is-invalid');
    }

    return true; // all valid
}

//#endregion


//#region Save Or Update Opening Balance
//function SaveOrUpdateVoucherType() {
//    console.log("Save Or Update OpeningBalance Function Called");

//    let autoID = $('#BlanceautoId').val();
//    let BalanceData = {
//        autoId: autoID || 0,
//        ComOpeningBalanceCode: "",
//        CompanyCode: $("#CompanyCode").val(),
//        BranchCode: $("#BranchCode").val(),
//        SubSusidiaryLedgerCodeNo: $("#SubSusidiaryLedgerCodeNo").val(),
//        OpeningBalance: $("#OpeningBalance").val() ? parseFloat($("#OpeningBalance").val()) : 0,
//        TrType: $("#TrType").val(),

//    };

//    //validation
//    if (!OpeningBalanceValidate(BalanceData)) {
//        console.log("Validation Failed!");
//        return false; // stop if validation fails
//    }


//    //Check for duplicate
//    $.ajax({
//        url: '/voucher-type/check-duplicate',
//        type: 'POST',
//        contentType: 'application/json',
//        data: JSON.stringify(VoucherTypeData),
//        success: function (dupResponse) {
//            if (dupResponse.isDuplicate) {
//                toastr.error(dupResponse.message);
//                return;
//            }

//            //Decide if POST or PUT
//            let url = '/opening-balance';
//            let method = 'POST';
//            let successMessage = 'Data Saved Successfully.';
//            if (autoID) {
//                url = `/opening-balance/${autoID}`;
//                method = 'PUT';
//                successMessage = 'Data Updated Successfully.';
//            }

//            //Save or Update
//            $.ajax({
//                url: url,
//                type: method,
//                contentType: 'application/json',
//                data: JSON.stringify(BalanceData),
//                success: function (response) {
//                    if (response.success) {
//                        toastr.success(successMessage);

//                        ClearOpeningBalanceForm();
//                        selectedBalance = [];
//                        LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);

//                    } else {
//                        toastr.error(response.message || "Operation Failed!");
//                        console.error('Error Opening Balance :', response);
//                    }
//                },
//                error: function (xhr, status, error) {
//                    console.error('Error Saving Opening Balance :', error);
//                }
//            });
//        },
//        error: function (xhr, status, error) {
//            toastr.error("Server error occurred during duplicate check!");
//            console.error('Error checking duplicate:', error);
//        }
//    });
//}
function SaveOrUpdateOpeningBalance() {
    console.log("Save Or Update OpeningBalance Function Called");

    let autoID = $('#BlanceautoId').val();
    let BalanceData = {
        autoId: autoID || 0,
        ComOpeningBalanceCode: "",
        CompanyCode: $("#CompanyCode").val(),
        BranchCode: $("#BranchCode").val(),
        SubSusidiaryLedgerCodeNo: $("#SubSusidiaryLedgerCodeNo").val(),
        OpeningBalance: $("#OpeningBalance").val() ? parseFloat($("#OpeningBalance").val()) : 0,
        TrType: $("#TrType").val(),
    };

    // Validation
    if (!OpeningBalanceValidate(BalanceData)) {
        console.log("Validation Failed!");
        return false; // stop if validation fails
    }

    // Decide if POST or PUT
    let url = '/opening-balance';
    let method = 'POST';
    let successMessage = 'Data Saved Successfully.';
    if (autoID) {
        url = `/opening-balance/${autoID}`;
        method = 'PUT';
        successMessage = 'Data Updated Successfully.';
    }

    // Save or Update
    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        data: JSON.stringify(BalanceData),
        success: function (response) {
            if (response.success) {
                toastr.success(successMessage);
                ClearOpeningBalanceForm();
                selectedBalance = [];
                LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
            } else {
                toastr.error(response.message || "Operation Failed!");
                console.error('Error Opening Balance :', response);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error Saving Opening Balance :', error);
        }
    });
}
//#endregion


//#region Opening Balance Form Clear
function ClearOpeningBalanceForm() {
    $("#BlanceautoId").val("");
    $("#CompanyCode").val("").trigger('change');
    $("#BranchCode").val("").trigger('change');
    $("#SubSusidiaryLedgerCodeNo").val("").trigger('change');
    $("#OpeningBalance").val("");
    $("#TrType").val("").trigger('change');

    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $('#BlancedisplayModifyDate').text('');    
    $('#BlancedisplayLDate').text('');  
    $(".balance-checkbox").prop("checked", false);
    $("#BlanceselectAll").prop("checked", false);
    $('#BlancesearchInput').val('');

    // Set Entry Date
    $('#BlancedisplayLDate').text(getBangladeshDateTime());

    selectedBalance = [];
    BalancecurrentPage = 1;
    const emptySearch = '';
    LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection, emptySearch);
    LoadCompanyDropdown();

    $('#SubSusidiaryLedgerCodeNo').next('.select2-container').find('.select2-selection__placeholder')
        .html('Account Head <span style="color:red">*</span>');
    $('#BranchCode').next('.select2-container').find('.select2-selection__placeholder')
        .html('Select Branch <span style="color:red">*</span>');
}
//#endregion


//#region Load All Opening Balance Data
function LoadAllOpeningBalanceData(page = 1, BalancepageSize = 5, sortColumn = BalancecurrentSortColumn, sortDirection = BalancecurrentSortDirection, searchTerm = "") {

    $.ajax({
        url: '/opening-balance-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: BalancepageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection
        },
        success: function (response) {
            console.log("Opening Balance Data:", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm balance-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td style="text-align:center">
                            <button class="btn btn-transfer opening-balance"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.comOpeningBalanceCode}
                    </td>
                    <td>${item.accountHead || ''}</td>
                    <td style="text-align:center">${item.openingBalance || ''}</td>
                    <td>${item.trType || ''}</td>
                    <td>${item.companyName || ''}</td>
                    <td>${item.branchName || ''}</td>
                 </tr>`;
            });
            $('#BlancetblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#BlancepaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#BlancepaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#BlancetotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            BalancecurrentPage = page;
            BalancegeneratePageButtons(BalancecurrentPage, totalPages);
            BalanceupdateCheckboxState(); 
            BalanceupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Populate Data For Edit
$(document).on('click', '.opening-balance', function () {
    const id = $(this).data('autoid');
    console.log("Edited Opening Balance Code:", id);

    if (!id) return;

    $.ajax({
        url: `/opening-balance/details/${id}`,
        type: 'GET',
        success: function (data) {
            // Populate form fields
            $('#BlanceautoId').val(id);
            $('#CompanyCode').val(data.companyCode || '').trigger('change');
            LoadBranchDropdown(data.companyCode, function () {
                // callback executed after branch options loaded
                $('#BranchCode').val(data.branchCode || '').trigger('change');
            });
            $('#SubSusidiaryLedgerCodeNo').val(data.subSusidiaryLedgerCodeNo || '').trigger('change');
            $('#TrType').val(data.trType || '');
            $('#OpeningBalance').val(data.openingBalance || '');

            //const banglaModifyDate = formatBangladeshDate(data.modifyDate);
            //    $('#BlancedisplayModifyDate').text(banglaModifyDate);
            // Entry Date
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


            // Optionally clear previous selections
            selectedBalance = [parseInt(id)];

            // Uncheck all row checkboxes to prevent conflict
            $('.balance-checkbox').prop('checked', false);
            $("#BlanceselectAll").prop('checked', false);

            $(`.balance-checkbox[data-autoid="${id}"]`).prop('checked', true);

            BalanceupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to fetch Opening Balance details.");
            console.error("Error fetching details:", error);
        }
    });
});

//#endregion


//#region Single & More Delete Voucher
function DeleteOpeningBalance() {
    // Filter only valid IDs
    const ids = selectedBalance.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("Opening Balance IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    //URL for both single & multiple delete
    const url = '/opening-balance-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            // Clear form, reload table, reset selection
            ClearOpeningBalanceForm();
            LoadAllOpeningBalanceData(BalancecurrentPage, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
            selectedBalance = [];
            $("#BlanceselectAll").prop("checked", false);


        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion


//#region Select all checkbox
$(document).on('click', '#BlanceselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.balance-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.balance-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedBalance.includes(id)) {
                selectedBalance.push(id);
            }
        });
    } else {
        $('.balance-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedBalance.indexOf(id);
            if (index !== -1) {
                selectedBalance.splice(index, 1);
            }
        });
    }
    console.log("Selected Opening Balance:", selectedBalance);
});
//#endregion


//#region Entry Date Up to date
//function SetCurrentEntryDate() {
//    const now = new Date();

//    // Bangladesh Format: DD-MM-YYYY
//    const day = String(now.getDate()).padStart(2, '0');
//    const month = String(now.getMonth() + 1).padStart(2, '0'); // Month starts from 0
//    const year = now.getFullYear();

//    const formattedDate = `${day}/${month}/${year}`;

//    const entryDateDiv = document.getElementById("BlancedisplayLDate");
//    if (entryDateDiv) {
//        entryDateDiv.textContent = formattedDate;
//        document.getElementById("BlancelDateContainer").style.display = "block"; // Ensure container visible
//    }
//}
//#endregion


//#region Sort & Pagination Helpers
function BalanceupdateSortIndicators() {
    $('.Blancesortable').removeClass('sort-asc sort-desc');
    $('.Blancesortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Blancesortable[data-column="${BalancecurrentSortColumn}"]`);
    header.addClass(BalancecurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(BalancecurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function BalancegeneratePageButtons(BalancecurrentPage, totalPages) {
    const navigationDiv = $('#BlancepageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadAllOpeningBalanceData(page, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection, $('#BlancesearchInput').val().trim()));

        if (page === BalancecurrentPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    // Always show first page
    navigationDiv.append(createButton(1));

    if (BalancecurrentPage > 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (BalancecurrentPage > 1 && BalancecurrentPage < totalPages) {
        navigationDiv.append(createButton(BalancecurrentPage));
    }

    if (BalancecurrentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

function BalanceupdateCheckboxState() {
    $('.balance-checkbox').each(function () {
        const bankID = $(this).data('autoid');
        $(this).prop('checked', selectedBalance.includes(bankID));
    });
    const allChecked = $('.balance-checkbox:visible').length > 0 &&
        $('.balance-checkbox:visible:not(:checked)').length === 0;
    $('#BlanceselectAll').prop('checked', allChecked);
}

function debounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function BalancebindCheckboxHandlers() {
    $(document).off('click', '.balance-checkbox');
    $(document).on('click', '.balance-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedBalance.includes(id)) {
                selectedBalance.push(id);
            }
        } else {
            const index = selectedBalance.indexOf(id);
            if (index !== -1) {
                selectedBalance.splice(index, 1);
            }
        }

        const allChecked = $('.balance-checkbox:visible').length > 0 &&
            $('.balance-checkbox:visible:not(:checked)').length === 0;
        $('#BlanceselectAll').prop('checked', allChecked);

        console.log("Selected Opening Balance:", selectedBalance);
    });
}
//#endregion


//#region Ready Part

$(document).ready(function () {
    console.log("Voucher Type Page Loaded");

    LoadAllOpeningBalanceData();
    LoadCompanyDropdown();
    LoadGeneralLedgerDropdown();
    //setInterval(SetCurrentEntryDate, 1000);

    // Set Entry Date
    $('#BlancedisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("submit", "#BlanceTypeForm", function (e) {
        e.preventDefault();
        SaveOrUpdateOpeningBalance();
    });

    // Handle Delete Button
    $(document).on("click", ".BlancedeleteBtn", function (e) {
        e.preventDefault();
        DeleteOpeningBalance();
    });

    // Handle Clear button
    $(document).on("click", ".BlanceresetBtn", function () {
        console.log("Clerar Button Clicked")
        ClearOpeningBalanceForm();
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
    $(document).on('input', '.BlancesearchInput', debounce(function () {
        const searchValue = $('.BlancesearchInput').val();
        LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection, searchValue);
    }, 500));

    $(document).on('click', '#BlancefirstPage', function () {
        BalancecurrentPage = 1;
        LoadAllOpeningBalanceData(BalancecurrentPage, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
    });

    $(document).on('click', '#BlanceprevPage', function () {
        console.log("previous page called ");
        if (BalancecurrentPage > 1) {
            BalancecurrentPage--;
            LoadAllOpeningBalanceData(BalancecurrentPage, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
        }
    });

    $(document).on('click', '#BlancenextPage', function () {
        BalancecurrentPage++;
        LoadAllOpeningBalanceData(BalancecurrentPage, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
    });

    $(document).on('click', '#BlancelastPage', function () {
        let lastPage = $('#BlancepageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            BalancecurrentPage = lastPage;
            LoadAllOpeningBalanceData(BalancecurrentPage, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
        }
    });

    $(document).on('change', '.BlancepageSize', function () {
        BalancepageSize = parseInt($(this).val());
        LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection);
    });

    $(document).on('click', '.Blancesortable', function () {
        const column = $(this).data('column');

        if (column === BalancecurrentSortColumn) {
            BalancecurrentSortDirection = (BalancecurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            BalancecurrentSortColumn = column;
            BalancecurrentSortDirection = 'desc';
        }

        LoadAllOpeningBalanceData(1, BalancepageSize, BalancecurrentSortColumn, BalancecurrentSortDirection, $('#BlancesearchInput').val().trim());
    });

    BalancebindCheckboxHandlers();
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