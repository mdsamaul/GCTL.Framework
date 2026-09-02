//#region Ready Part

$(document).ready(function () {

    LoadAccountHeadDropdown();
    
    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({
            gridId: 'VoucherEntryTempGrid',
            onLoaded: function (json) {
                let totalDr = 0;
                let totalCr = 0;
                let records = json.data ? json.data : (Array.isArray(json) ? json : []);
                
                records.forEach(item => {
                    totalDr += (item.debitAmount || 0);
                    totalCr += (item.creditAmount || 0);
                });
                
                $('#TotalDrValue').text(totalDr.toFixed(2));
                $('#TotalCrValue').text(totalCr.toFixed(2));
            }
        });
    }
    
    // Initialize ChequeDate
    if (document.querySelector("#ChequeDate")) {
        setDate("#ChequeDate", new Date());
    }


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
// Handled by _DynamicFieldColumn and global scripts
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

            if (window.reinitCustomDropdownById) {
                window.reinitCustomDropdownById('AccCode');
            }
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
// Handled by dynamic field column
//#endregion


//#region Validation Error Border(Common input handler)

// Common handler for all inputs
$(document).on('input', '.form-control', function () {
    if ($(this).val().trim() !== '') {
        $(this).removeClass('is-invalid');
    }
});


//#endregion


//#region Tmp Voucher Entry Details Validation
function validateSelect2(selector, value, message) {
    if (!value) {
        toastr.error(message);
        return false;
    } 
    return true;
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

    $("#TrType").val("");
    if (window.reinitCustomDropdownById) window.reinitCustomDropdownById("TrType");

    $("#AccCode").val("");
    if (window.reinitCustomDropdownById) window.reinitCustomDropdownById("AccCode");

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
        ChequeDate: (function() {
            let val = $('#ChequeDate').val();
            if (!val) return null;
            
            let fp = document.querySelector("#ChequeDate")?._flatpickr;
            if (fp && fp.selectedDates && fp.selectedDates.length > 0) {
                let d = fp.selectedDates[0];
                return new Date(d.getTime() - (d.getTimezoneOffset() * 60000)).toISOString();
            }
            
            if (val.match(/^\d{4}-\d{2}-\d{2}$/)) {
                return new Date(val).toISOString();
            }
            
            if (val.match(/^\d{2}-\d{2}-\d{4}$/)) {
                let parts = val.split('-');
                let parsedDate = new Date(`${parts[2]}-${parts[1]}-${parts[0]}`);
                if (!isNaN(parsedDate.getTime())) return parsedDate.toISOString();
            }
            
            return null;
        })(),
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

    console.log("Request URL:", url);
    console.log("Request Method:", method);
    console.log("Data to send:", TmpData);

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
                if (window.GctlGrid) {
                    window.GctlGrid.reload('VoucherEntryTempGrid');
                }
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





//#region tmp-details button click event
function fillFormFields(data) {
    if (!data) return;

    $('#TmpDetailsAutoID').val(data.autoId);
    $('#TrType').val(data.trType);
    if (window.reinitCustomDropdownById) window.reinitCustomDropdownById("TrType");
    
    $('#AccCode').val(data.accCode);
    if (window.reinitCustomDropdownById) window.reinitCustomDropdownById("AccCode");
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
            $('#ChequeDate').val(isoDate);
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

$(document).on('click', '.js-grid-edit', function (e) {
    e.preventDefault();
    const id = $(this).data('id');
    if (!id) return;

    $.get(`/tmp-voucher/details/${id}`, function (data) {
        fillFormFields(data);
    }).fail(function (xhr) {
        console.error("Error loading voucher details:", xhr);
        toastr.error("Failed to load voucher details!");
    });
});

//#endregion


//#region Single & Multiple Delete Tmp Voucher
$(document).on('click', '.js-grid-delete', function (e) {
    e.preventDefault();
    const id = parseInt($(this).data('id'));
    if (!id || isNaN(id)) return;

    DeleteTmpDetails([id]);
});

function DeleteSelectedTmpDetails() {
    let ids = [];
    $('#VoucherEntryTempGrid tbody input[type="checkbox"]:checked').each(function() {
        let idVal = $(this).val() || $(this).data('id');
        let id = parseInt(idVal);
        if (id && !isNaN(id)) ids.push(id);
    });

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }
    DeleteTmpDetails(ids);
}

function DeleteTmpDetails(ids) {
    console.log("Tmp Voucher IDs to delete:", ids);
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
                // Reload Details Table
                if (window.GctlGrid) {
                    window.GctlGrid.reload('VoucherEntryTempGrid');
                }
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



