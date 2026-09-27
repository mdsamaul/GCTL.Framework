let manualSerialEntry = false;

// Loading Next Expense Head ID
function loadNextExpenseHeadId() {
    $.ajax({
        url: '/next-expense-head-id',
        method: 'GET',
        success: function (response) {
            $('#expenseHeadId').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed to load next Expense Head ID:", error);
        }
    });
}

// Loading ExpenseType into Dropdown
function loadExpenseTypes(selectedId = null) {
    $.ajax({
        url: '/expense-type-dropdown',
        type: 'GET',
        success: function (data) {
            var select = $("#expenseType");
            select.empty();
            select.append('<option value="">Select Shipment Mode</option>');
            if (data && data.length > 0) {
                $.each(data, function (index, item) {
                    var id = item.expenseTypeID || item.expenseTypeId;
                    var name = item.expenseType || "";
                    var isSelected = selectedId == id ? 'selected' : '';
                    select.append('<option value="' + id + '" ' + isSelected + '>' + name + '</option>');
                });
            }
            if (typeof window.initCustomDropdowns === "function") {
                window.initCustomDropdowns('#expenseForm', true);
            }
        },
        error: function (xhr, status, error) {
            console.error("Failed to load Expense Types:", error);
        }
    });
}

// Loading Core_ServiceType
function loadServiceProviderDropdown(selectedId = null) {
    $.ajax({
        url: '/core-service-dropdown',
        method: 'GET',
        success: function (response) {
            let $dropdown = $('#serviceProvider');
            $dropdown.empty();
            $dropdown.append(`<option value="">Select Service Provider</option>`);
            if (response && response.length > 0) {
                response.forEach(item => {
                    let id = item.serviceTypeId || item.serviceTypeID;
                    let name = item.serviceTypeName || "";
                    let isSelected = selectedId == id ? 'selected' : '';
                    $dropdown.append(`<option value="${id}" ${isSelected}>${name}</option>`);
                });
            }
            if (typeof window.initCustomDropdowns === "function") {
                window.initCustomDropdowns('#expenseForm', true);
            }
        },
        error: function (xhr) {
            console.error("Failed to load Service Provider dropdown", xhr);
        }
    });
}

// Load Next Serial Number
function LoadNextSerialNo() {
    let expenseType = $("#expenseType").val();
    let serviceType = $("#serviceProvider").val();
    let isReceiptable = $("#isReceiptable").is(":checked") ? "Y" : "N";
    //let requestedSerial = manualSerialEntry ? parseInt($("#serialNo").val()) : null;

    if (!expenseType) {
        $("#serialNo").val("");
        return;
    }

    $.ajax({
        url: '/serial-no',
        method: 'GET',
        data: {
            expenseType: expenseType,
            serviceType: serviceType,
            isReceiptable: isReceiptable,
            //requestedSerial: requestedSerial
        },
        success: function (res) {
            if (!manualSerialEntry) {
                $("#serialNo").val(res.serialNo);
            }
        },
        error: function (xhr) {
            toastr.error(xhr.responseJSON?.message || "Failed to load serial number.");
            if (!manualSerialEntry) $("#serialNo").val("");
        }
    });
}

// Save or Update With Duplicate Checking
function SaveOrUpdateExpenseHead() {
    let hiddenID = parseInt($("#tc").val()) || 0;
    let isEdit = hiddenID > 0;
    let expenseHeadData = {
        TC: hiddenID,
        ExpenseHeadID: $("#expenseHeadId").val()?.trim(),
        ExpenseHead: $("#expenseHead").val()?.trim(),
        ShortName: $("#shortName").val()?.trim(),
        Amount: parseFloat($("#amount").val()) || 0,
        SerialNo: $("#serialNo").val()?.trim(),
        IsReceiptable: $("#isReceiptable").is(":checked") ? "Y" : "N",
        ServiceTypeID: $("#serviceProvider").val(),
        ExpenseTypeID: $("#expenseType").val(),
        EntryType: ""
    };

    // Validation
    if (!expenseHeadData.ExpenseTypeID) {
        toastr.error("Please select Shipment Mode name.");
        $("#expenseType").addClass('error-border').focus();
        return;
    } else {
        $("#expenseType").removeClass('error-border');
    }
    if (!expenseHeadData.ExpenseHead) {
        toastr.error("Please enter Expense Head name.");
        $("#expenseHead").addClass('error-border').focus();
        return;
    } else {
        $("#expenseHead").removeClass('error-border');
    }
    if (!expenseHeadData.SerialNo) {
        toastr.error("Please enter Serial No.");
        $("#serialNo").addClass('error-border').focus();
        return;
    } else {
        $("#serialNo").removeClass('error-border');
    }
    if (!expenseHeadData.ServiceTypeID) {
        toastr.error("Please enter Service Provider Name.");
        $("#serviceProvider").addClass('error-border').focus();
        return;
    } else {
        $("#serviceProvider").removeClass('error-border');
    }

    // Checking duplicate
    $.ajax({
        url: '/expense-head/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(expenseHeadData),
        success: function (dupRes) {
            if (dupRes.isDuplicate) {
                toastr.error(dupRes.message);
                return;
            } else {
                let url = isEdit ? `/expense-head/${hiddenID}` : '/expense-head';
                let type = isEdit ? 'PUT' : 'POST';

                $.ajax({
                    url: url,
                    type: type,
                    contentType: 'application/json',
                    data: JSON.stringify(expenseHeadData),
                    success: function (res) {
                        if (res.success) {
                            toastr.success(res.message);
                            ResetExpenseHeadForm();
                            reloadDataTable();
                        } else {
                            toastr.error(res.message);
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || (isEdit ? "Update Failed." : "Insertion Failed."));
                    }
                });
            }
        },
        error: function () {
            toastr.error("Failed to check duplicate.");
        }
    });
}

// Reload Grid Helper
function reloadDataTable() {
    if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
        $.gctlGrid.reload('expenseHeadTable');
    } else if ($.fn.DataTable && $.fn.DataTable.isDataTable('#expenseHeadTable')) {
        $('#expenseHeadTable').DataTable().ajax.reload(null, false);
    }
}

// Populate Form from Item Data
function populateForm(item) {
    if (!item) return;

    $("#tc").val(item.tc || 0);
    $("#expenseHeadId").val(item.expenseHeadID || "");
    $("#expenseHead").val(item.expenseHead || "");
    $("#shortName").val(item.shortName || "");
    $("#amount").val(item.amount && item.amount != 0 ? item.amount : "");
    $("#serialNo").val(item.serialNo || "");
    $("#isReceiptable").prop("checked", item.isReceiptable === "Y" || item.isReceiptable === "Yes" || item.isReceiptable === "YES");

    let serviceTypeVal = (item.serviceTypeID || item.serviceTypeId || "").trim();
    let expenseTypeVal = (item.expenseTypeID || item.expenseTypeId || "").trim();

    if (typeof setSelect === "function") {
        setSelect('#serviceProvider', serviceTypeVal, false);
        setSelect('#expenseType', expenseTypeVal, false);
    } else {
        $("#serviceProvider").val(serviceTypeVal).trigger('change');
        $("#expenseType").val(expenseTypeVal).trigger('change');
    }

    if (item.ldate || item.lDate) {
        const dateVal = item.ldate || item.lDate;
        const lDate = new Date(dateVal).toLocaleDateString('en-GB');
        $("#lDateContainer").show();
        $("#displayLDate").text(lDate);
    } else {
        $("#lDateContainer").hide();
    }
    if (item.modifyDate) {
        const modifyDate = new Date(item.modifyDate).toLocaleDateString('en-GB');
        $("#modifyDateContainer").show();
        $("#displayModifyDate").text(modifyDate);
    } else {
        $("#modifyDateContainer").hide();
    }
}

// Clear Form Function
function ResetExpenseHeadForm() {
    $("#tc").val("");
    $("#expenseHeadId").val("");
    $("#expenseHead").val("");
    $("#shortName").val("");
    $("#amount").val("");
    $("#serialNo").val("");
    $("#isReceiptable").prop("checked", false);

    if (typeof setSelect === "function") {
        setSelect('#serviceProvider', '', false);
        setSelect('#expenseType', '', false);
    } else {
        $("#serviceProvider").val('');
        $("#expenseType").val('');
    }

    $("#displayLDate").text('');
    $("#lDateContainer").hide();
    $("#displayModifyDate").text('');
    $("#modifyDateContainer").hide();
    $('#selectAllExpense').prop('checked', false);
    $('.js-expensehead-check').prop('checked', false);

    loadNextExpenseHeadId();
    reloadDataTable();
}

// Bulk Delete with Modal
var selectedExpenseHeadIds = [];

function PromptDeleteExpenseHead() {
    selectedExpenseHeadIds = [];
    $("#expenseHeadTable .js-expensehead-check:checked").each(function () {
        let val = parseInt($(this).val());
        if (val) selectedExpenseHeadIds.push(val);
    });

    if (selectedExpenseHeadIds.length === 0) {
        let currentTc = parseInt($("#tc").val());
        if (currentTc > 0) {
            selectedExpenseHeadIds.push(currentTc);
        }
    }

    if (selectedExpenseHeadIds.length === 0) {
        toastr.info("Please select at least one item to delete.");
        return;
    }

    $('#ExpenseHead-delete-modal').modal('show');
}

function ExecuteDeleteExpenseHead() {
    if (!selectedExpenseHeadIds || selectedExpenseHeadIds.length === 0) {
        $('#ExpenseHead-delete-modal').modal('hide');
        return;
    }

    $.ajax({
        url: '/expense-head-bulk',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedExpenseHeadIds),
        success: function (res) {
            $('#ExpenseHead-delete-modal').modal('hide');
            if (res.isSuccess) {
                toastr.success(res.message || "Data Deleted Successfully.");
                ResetExpenseHeadForm();
                reloadDataTable();
                $('#selectAllExpense').prop('checked', false);
            } else {
                toastr.error(res.message || "Failed to delete.");
            }
            selectedExpenseHeadIds = [];
        },
        error: function (xhr) {
            $('#ExpenseHead-delete-modal').modal('hide');
            toastr.error(xhr.responseJSON?.message || "Failed to delete selected records.");
            selectedExpenseHeadIds = [];
        }
    });
}

function BulkDeleteExpenseHead() {
    PromptDeleteExpenseHead();
}

// Export Report
function ExportExpenseHeadReport(format) {
    let expenseTypeId = $('#expenseType').val() || '';
    let url = `/expense-head/report?expenseTypeId=${expenseTypeId}&format=${format}`;

    $.ajax({
        url: url,
        type: 'GET',
        xhrFields: {
            responseType: 'blob'
        },
        success: function (data, status, xhr) {
            let filename = "";
            const disposition = xhr.getResponseHeader('Content-Disposition');
            if (disposition && disposition.indexOf('attachment') !== -1) {
                const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
                const matches = filenameRegex.exec(disposition);
                if (matches != null && matches[1]) filename = matches[1].replace(/['"]/g, '');
            }
            if (!filename) {
                filename = format === 'excel' ? 'ExpenseHeadReport.xlsx' : 'ExpenseHeadReport.pdf';
            }

            const link = document.createElement('a');
            const blob = new Blob([data], { type: xhr.getResponseHeader('Content-Type') });
            link.href = window.URL.createObjectURL(blob);
            link.download = filename;
            link.click();
            window.URL.revokeObjectURL(link.href);
        },
        error: function (xhr, status, error) {
            console.error("Export failed:", error);
            toastr.error("Failed to generate report.");
        }
    });
}

// Document Ready Initializations
$(document).ready(function () {

    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({
            gridId: 'expenseHeadTable',
            ajaxData: function (d) {
                d.expenseType = $('#expenseType').val() || '';
                d.serviceType = $('#serviceProvider').val() || '';
                d.isReceiptable = $('#isReceiptable').is(':checked') ? 'Y' : '';
            }
        });
    }

    loadNextExpenseHeadId();
    loadExpenseTypes();
    loadServiceProviderDropdown();

    // Edit link click in custom datatable
    $(document).on("click", ".js-expensehead-edit", function (e) {
        e.preventDefault();
        let tc = $(this).data("id") || $(this).closest("tr").find(".js-expensehead-check").val();
        if (!tc) return;

        $.ajax({
            url: `/expense-head/get-by-id/${tc}`,
            type: 'GET',
            success: function (res) {
                if (res.data) {
                    populateForm(res.data);
                }
            },
            error: function () {
                toastr.error("Failed to fetch expense head details.");
            }
        });
    });

    // Serial No manual entry handling
    $("#serialNo").on("input", function () {
        let value = $(this).val().trim();
        if (value && (!/^\d+$/.test(value) || parseInt(value) <= 0)) {
            toastr.error("Serial No must be a positive integer.");
            $(this).val("");
            manualSerialEntry = false;
        } else {
            manualSerialEntry = value !== "";
        }
    });

    // Reset manual flag, load serial, and filter grid on dropdown/checkbox change
    $(document).on("change", "#expenseType, #serviceProvider, #isReceiptable", function () {
        manualSerialEntry = false;
        LoadNextSerialNo();
        reloadDataTable();
    });

    // Handle form submission
    $("#expenseForm").on("submit", function (e) {
        e.preventDefault();
        SaveOrUpdateExpenseHead();
    });

    $(".saveBtn").on("click", function () {
        if ($(this).attr("type") === "button") {
            SaveOrUpdateExpenseHead();
        }
    });

    // Handle delete button click (trigger modal)
    $(document).on("click", ".deleteBtn, #deleteBtn, .js-expensehead-delete-confirm", function (e) {
        e.preventDefault();
        PromptDeleteExpenseHead();
    });

    // Handle confirm delete from modal
    $(document).on("click", ".js-expensehead-delete", function (e) {
        e.preventDefault();
        ExecuteDeleteExpenseHead();
    });

    // Handle clear button
    $(".resetBtn").on("click", function () {
        ResetExpenseHeadForm();
    });

    // PDF Export
    $(document).on('click', '.export-pdf', function (e) {
        e.preventDefault();
        ExportExpenseHeadReport('pdf');
    });

    // Excel Export
    $(document).on('click', '.export-excel', function (e) {
        e.preventDefault();
        ExportExpenseHeadReport('excel');
    });
});
