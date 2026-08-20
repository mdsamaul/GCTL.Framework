//#region Ready Part

$(document).ready(function () {

    LoadAccountHeadDropdown();
    LoadTmpVoucherDetailsTable();


    ////Handle Save Or Update Button Handle
    $(document).on('click', '#AddDetails', function (e) {
        e.preventDefault(); 
        SaveOrUpdateTmpDetails(); 
    });


    //// Handle Delete Button
    $(document).on("click", "#CancelDetails", function (e) {
        e.preventDefault();
        DeleteSelectedTmpDetails();
    });

    //// Handle Clear button
    $(document).on("click", "#RefreshDetails", function () {
        console.log("Clerar Button Clicked")
        DetailsSectionClear();
    });

});

//#endregion


//#region Tr. Type Dropdown

$('#TrType').select2({
    placeholder: "Tr. Type",
    width: '100%',
    allowClear: true,
    dropdownPosition: 'below'
});

$('#TrType').next('.select2-container').find('.select2-selection__placeholder')
    .html('Tr. Type <span style="color:red">*</span>');

//#endregion


//#region Account Head Dropdown
function LoadAccountHeadDropdown() {
    $.ajax({
        url: '/Account-Head-dropdown',
        method: 'GET',
        success: function (response) {
            console.log("Account Head Dropdown Loaded", response.data);

            var $general = $('#AccCode');
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


//#region Account Head Change Event → Load Ledger Details

$(document).on('change', '#AccCode', function () {
    const id = $(this).val(); // selected AccountHead id
    if (!id) return;

    $.ajax({
        url: `/Ledger-details/${id}`,
        method: 'GET',
        success: function (response) {
            console.log("Account Head with other Ledger Data :", response);
            if (response) {
                // Show ledger sections
                $('.ledger-row').parent().show();

                // Populate values
                $('#GroupLedgerValue').text(response.groupLedgerName || '');
                $('#ControlLedgerValue').text(response.controlLedgerName || '');
                $('#SubControlLedgerValue').text(response.subControlLedgerName || '');
                $('#SubSidiaryLedgerValue').text(response.subSidiaryLedgerName || '');
                $('#Description').val(response.generalLedgerName || '');
            } else {
                clearLedgerFields();
            }
        },
        error: function (xhr) {
            console.error('Failed to load Ledger details:', xhr);
            clearLedgerFields();
        }
    });
});

function clearLedgerFields() {
    $('.ledger-row').parent().hide();
    $('#GroupLedgerValue, #ControlLedgerValue, #SubControlLedgerValue, #SubSidiaryLedgerValue, #Description').text('');
}

//#endregion


//#region Cheque Date

flatpickr("#ChequeDate", {
    dateFormat: "d-m-Y",
    allowInput: true,
    altInput: true,
    altFormat: "d-m-Y",
    locale: {
        firstDayOfWeek: 5
    }
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
$('#AccCode').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger'); // error remove
    }
});
$('#TrType').on('change', function () {
    const $selection = $(this).next('.select2-container').find('.select2-selection');
    if ($(this).val()) {
        $selection.removeClass('border-danger'); // error remove
    }
});

//#endregion


//#region Tmp Voucher Entry Details Validation
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

function TmpVoucherDetailsValidate(TmpData) {
    // Tr Type validation
    if (!validateSelect2('#TrType', TmpData.TrType, "Please Select a Tr. Type")) return false;

    // Account Head validation 
    if (!validateSelect2('#AccCode', TmpData.AccCode, "Please Select a Account Head")) return false;

    // Amount validation
    if ((TmpData.TrType === "Dr" && TmpData.DebitAmount <= 0) ||
        (TmpData.TrType === "Cr" && TmpData.CreditAmount <= 0)) {
        toastr.error("Please Enter Amount.");
        $('#Amount').addClass('is-invalid');
        return false;
    } else {
        $('#Amount').removeClass('is-invalid');
    }

    return true; // all valid
}

//#endregion


//#region Tmp Voucher Details Clear Function
function DetailsSectionClear() {
    $("#TmpDetailsAutoID").val("");

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


//#region Save Or Update Tmp Voucher Entry Details
function SaveOrUpdateTmpDetails() {
    console.log("Save Or Update Tmp Voucher Details Function Called");

    let autoID = $("#TmpDetailsAutoID").val();
    let amountValue = parseFloat($("#Amount").val()) || 0;

    let TmpData = {
        autoId: autoID || 0,
        VoucherEntryDetailsCodeNo: "",
        TrType: $("#TrType").val(),
        AccCode: $("#AccCode").val(),
        Description: $("#Description").val(),
        ChequeNo: $("#ChequeNo").val(),
        ChequeDate: $('#ChequeDate').val()
            ? new Date($('#ChequeDate').val().split('-').reverse().join('-')).toISOString()
            : null,
        DebitAmount: $("#TrType").val() === "Dr" ? amountValue : 0,
        CreditAmount: $("#TrType").val() === "Cr" ? amountValue : 0,
    };

    // Validation
    if (!TmpVoucherDetailsValidate(TmpData)) {
        console.log("Validation Failed!");
        return false;
    }

    // Decide if POST or PUT
    let url = '/tmp-voucher-details';
    let method = 'POST';
    let successMessage = 'Data Saved Successfully.';
    let failMessage = 'Insertion Failed.';

    if (autoID) {
        url = `/tmp-voucher-details/${autoID}`;
        method = 'PUT';
        successMessage = 'Data Updated Successfully.';
        failMessage = 'Update Failed.';
    }

    // Save or Update
    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        data: JSON.stringify(TmpData),
        success: function (response) {
            if (response.success) {
                toastr.success(successMessage);

                // Clear details form
                DetailsSectionClear();

                // Reload Details Table
                LoadTmpVoucherDetailsTable();
            } else {
                toastr.error(response.message || failMessage);
                console.error('Error Tmp Voucher Entry Details :', response);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error Saving Tmp Voucher Entry Details :', error);
            toastr.error(failMessage);
        }
    });
}

//function SaveOrUpdateTmpDetails() {
//    console.log("Save/Update Tmp Voucher Details Called");

//    let autoID = $('#VoucherEntryAutoID').val();
//    let amountValue = parseFloat($("#Amount").val()) || 0;

//    let TmpData = {
//        autoId: autoID || 0,
//        VoucherEntryDetailsCodeNo: "",
//        TrType: $("#TrType").val(),
//        AccCode: $("#AccCode").val(),
//        Description: $("#Description").val(),
//        ChequeNo: $("#ChequeNo").val(),
//        ChequeDate: $('#ChequeDate').val()
//            ? new Date($('#ChequeDate').val().split('-').reverse().join('-')).toISOString()
//            : null,
//        DebitAmount: $("#TrType").val() === "Dr" ? amountValue : 0,
//        CreditAmount: $("#TrType").val() === "Cr" ? amountValue : 0,
//    };

//    // Validation
//    if (!TmpVoucherDetailsValidate(TmpData)) {
//        console.log("Validation Failed!");
//        return false;
//    }

//    //Debit/Credit Equal Check
//    $.ajax({
//        url: '/tmp-voucher-details/check-equal',
//        type: 'POST',
//        contentType: 'application/json',
//        data: JSON.stringify(TmpData),
//        success: function (checkResponse) {
//            if (!checkResponse.success) {
//                toastr.error(checkResponse.message);
//                return;
//            }

//            //if Equal Save বা Update
//            let url = '/tmp-voucher-details';
//            let method = 'POST';
//            let successMessage = 'Data Saved Successfully.';
//            let failMessage = 'Insertion Failed.';

//            if (autoID) {
//                url = `/tmp-voucher-details/${autoID}`;
//                method = 'PUT';
//                successMessage = 'Data Updated Successfully.';
//                failMessage = 'Update Failed.';
//            }

//            $.ajax({
//                url: url,
//                type: method,
//                contentType: 'application/json',
//                data: JSON.stringify(TmpData),
//                success: function (response) {
//                    if (response.success) {
//                        toastr.success(successMessage);
//                        DetailsSectionClear();
//                        LoadTmpVoucherDetailsTable();
//                    } else {
//                        toastr.error(response.message || failMessage);
//                    }
//                },
//                error: function (xhr, status, error) {
//                    console.error('Error Saving/Updating Tmp Voucher Entry:', error);
//                    toastr.error(failMessage);
//                }
//            });

//        },
//        error: function (xhr) {
//            console.error("Error checking debit/credit:", xhr);
//            toastr.error("Error checking debit/credit.");
//        }
//    });
//}


//#endregion


//#region Load All Tmp Voucher Details
function LoadTmpVoucherDetailsTable() {
    $.ajax({
        url: '/all-tmp-voucher-details',
        type: 'GET',
        success: function (data) {
            const $tbody = $('.voucher-entry-table tbody');
            $tbody.empty(); // Clear previous rows

            let totalDr = 0, totalCr = 0;

            if (data && Array.isArray(data) && data.length > 0) {
                // Records found
                $.each(data, function (index, item) {
                    const row = `
                        <tr>
                            <td class="text-center" style="vertical-align: middle;">
                                <input type="checkbox" class="row-select" data-id="${item.autoId}" />
                            </td>
                            <td style="text-align:center">
                               <button type="button" class="btn btn-transfer tmp-details tooltip-btn" data-autoid="${item.autoId}" data-tooltip="Edit">
                                    <i class="fas fa-edit me-2"></i> ${item.accCode}
                                </button>
                            </td>
                            <td style="vertical-align: middle;">${item.accountHead || ''}</td>
                            <td style="vertical-align: middle;">${item.description || ''}</td>
                            <td class="text-end" style="vertical-align: middle;">${item.debitAmount?.toFixed(2) || '0.00'}</td>
                            <td class="text-end" style="vertical-align: middle;">${item.creditAmount?.toFixed(2) || '0.00'}</td>
                        </tr>`;
                    $tbody.append(row);

                    totalDr += item.debitAmount || 0;
                    totalCr += item.creditAmount || 0;
                });
            } else {
                // Empty row with all tds
                $tbody.append(`
                    <tr>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                        <td></td>
                    </tr>
                `);
            }

            // Always append total row
            $tbody.append(`
                <tr>
                    <td colspan="4" class="text-end fw-bold">Total</td>
                    <td class="fw-bold text-end">${totalDr.toFixed(2)}</td>
                    <td class="fw-bold text-end">${totalCr.toFixed(2)}</td>
                </tr>
            `);

            // Summary text
            const from = data && data.length > 0 ? 1 : 0;
            const to = data && data.length > 0 ? data.length : 0;
            $('.voucher-entry-table + small').text(`Showing ${from} to ${to} of ${to} entries`);
        },
        error: function (xhr, status, error) {
            console.error("Error loading tmp voucher details:", xhr);
            toastr.error("Failed to load tmp voucher details due to server/network error!");

            const $tbody = $('.voucher-entry-table tbody');
            $tbody.empty();

            // Empty row with all tds
            $tbody.append(`
                <tr>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                </tr>
            `);

            // Total row 0
            $tbody.append(`
                <tr>
                    <td colspan="4" class="text-end fw-bold">Total</td>
                    <td class="fw-bold text-end">0.00</td>
                    <td class="fw-bold text-end">0.00</td>
                </tr>
            `);

            $('.voucher-entry-table + small').text(`Showing 0 to 0 of 0 entries`);
        }
    });
}

//#region Load All Tmp Voucher Details
//function LoadTmpVoucherDetailsTable(luser) {
//    $.ajax({
//        url: '/all-tmp-voucher-details',
//        type: 'GET',
//        data: { luser: luser }, 
//        success: function (data) {
//            const $tbody = $('.voucher-entry-table tbody');
//            $tbody.empty(); // Clear previous rows

//            let totalDr = 0, totalCr = 0;

//            if (data && Array.isArray(data) && data.length > 0) {
//                $.each(data, function (index, item) {
//                    const row = `
//                        <tr>
//                            <td class="text-center" style="vertical-align: middle;">
//                                <input type="checkbox" class="row-select" data-id="${item.autoId}" />
//                            </td>
//                            <td style="text-align:center">
//                               <button type="button" class="btn btn-transfer tmp-details" data-autoid="${item.autoId}">
//                                    <i class="fas fa-edit me-2"></i> ${item.accCode}
//                                </button>
//                            </td>
//                            <td style="vertical-align: middle;">${item.accountHead || ''}</td>
//                            <td style="vertical-align: middle;">${item.description || ''}</td>
//                            <td class="text-end" style="vertical-align: middle;">${item.debitAmount?.toFixed(2) || '0.00'}</td>
//                            <td class="text-end" style="vertical-align: middle;">${item.creditAmount?.toFixed(2) || '0.00'}</td>
//                        </tr>`;
//                    $tbody.append(row);

//                    totalDr += item.debitAmount || 0;
//                    totalCr += item.creditAmount || 0;
//                });
//            } else {
//                $tbody.append(`
//                    <tr>
//                        <td colspan="6" class="text-center">No Records Found</td>
//                    </tr>
//                `);
//            }

//            // Always append total row
//            $tbody.append(`
//                <tr>
//                    <td colspan="4" class="text-end fw-bold">Total</td>
//                    <td class="fw-bold text-end">${totalDr.toFixed(2)}</td>
//                    <td class="fw-bold text-end">${totalCr.toFixed(2)}</td>
//                </tr>
//            `);

//            // Summary text
//            const from = data && data.length > 0 ? 1 : 0;
//            const to = data && data.length > 0 ? data.length : 0;
//            $('.voucher-entry-table + small').text(`Showing ${from} to ${to} of ${to} entries`);
//        },
//        error: function (xhr, status, error) {
//            console.error("Error loading tmp voucher details:", xhr);
//            toastr.error("Failed to load tmp voucher details due to server/network error!");
//        }
//    });
//}
//#endregion

//#endregion


//#region tmp-details button click event
function fillFormFields(data) {
    if (!data) return;

    $('#TmpDetailsAutoID').val(data.autoId);
    $('#TrType').val(data.trType).trigger('change');
    $('#AccCode').val(data.accCode).trigger('change');
    $('#Description').val(data.description || '');

    // Amount
    const amount = data.trType === "Dr" ? data.debitAmount : data.creditAmount;
    $('#Amount').val(amount || 0);

    // Cheque Date
    if (data.chequeDate) {
        const isoDate = data.chequeDate.split('T')[0]; // yyyy-mm-dd
        const parts = isoDate.split('-');
        const bdDate = `${parts[2]}-${parts[1]}-${parts[0]}`; // dd-mm-yyyy
        if ($("#ChequeDate")[0]._flatpickr) {
            $("#ChequeDate")[0]._flatpickr.setDate(bdDate, true);
        } else {
            $('#ChequeDate').val(bdDate);
        }
    } else {
        if ($("#ChequeDate")[0]._flatpickr) {
            $("#ChequeDate")[0]._flatpickr.clear();
        } else {
            $('#ChequeDate').val('');
        }
    }

    // Ledger info
    if (data.groupLedgerName) $('#GroupLedgerValue').text(data.groupLedgerName).closest('.ledger-row').show();
    if (data.controlLedgerName) $('#ControlLedgerValue').text(data.controlLedgerName).closest('.ledger-row').show();
    if (data.subControlLedgerName) $('#SubControlLedgerValue').text(data.subControlLedgerName).closest('.ledger-row').show();
    if (data.subSidiaryLedgerName) $('#SubSidiaryLedgerValue').text(data.subSidiaryLedgerName).closest('.ledger-row').show();
}

$(document).on('click', '.tmp-details', function (e) {
    e.preventDefault();
    const id = $(this).data('autoid');
    if (!id) return;

    $.get(`/tmp-voucher/details/${id}`, function (data) {
        fillFormFields(data);
    }).fail(function (xhr) {
        console.error("Error loading voucher details:", xhr);
        toastr.error("Failed to load voucher details!");
    });
});

//#endregion


//#region Checkbox Update

let selectedTmpDetails = []; // global variable

$(document).on('change', '.voucher-entry-table tbody input.row-select', function () {
    const id = $(this).data('id');
    if ($(this).is(':checked')) {
        if (!selectedTmpDetails.includes(id)) selectedTmpDetails.push(id);
    } else {
        selectedTmpDetails = selectedTmpDetails.filter(x => x !== id);
    }
});

//#endregion


//#region Single & Multiple Delete Tmp Voucher
function DeleteSelectedTmpDetails() {
    // Filter only valid IDs
    const ids = selectedTmpDetails.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("Tmp Voucher IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    // URL for delete
    const url = '/tmp-voucher-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            if (response.isSuccess) {
                toastr.success(response.message || "Data Deleted Successfully.");
                // Reload Tmp Voucher Table
                LoadTmpVoucherDetailsTable();
                // Clear selection
                selectedTmpDetails = [];
                $('.voucher-entry-table tbody input.row-select').prop('checked', false);
            } else {
                toastr.error(response.message || "Delete Failed.");
            }
        },
        error: function (xhr, status, error) {
            console.error("Error deleting tmp voucher details:", error);
            toastr.error("Server error occurred while deleting data!");
        }
    });
}
//#endregion



