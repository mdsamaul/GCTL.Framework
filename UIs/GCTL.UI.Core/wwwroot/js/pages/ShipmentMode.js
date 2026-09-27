function getNextExpenseTypeId() {
    $.ajax({
        url: '/next-expense-type-id',
        type: 'GET',
        success: function (response) {
            $('#expenseTypeId, #shipmentForm input[name="expenseTypeId"]').val(response);
        },
        error: function (xhr) {
            console.error("Error fetching next Expense Type ID:", xhr.responseText);
        }
    });
}

function clearShipmentForm() {
    if ($('#shipmentForm').length) {
        $('#shipmentForm')[0].reset();
    }
    $('#expenseType').removeClass('error-border');
    $('#sectionBreak').hide();
    $('#LdateModifyHide').hide();
    $('#shipmentForm input[name="tcId"], #tcId').val(0);
    $('#selectAllShipmentMode').prop('checked', false);
    $('.js-shipmentmode-check').prop('checked', false);

    if (typeof setSelect === "function") {
        setSelect('#transportModeTypeId', '', false);
    } else {
        $('#transportModeTypeId').val('').trigger('change');
    }

    getNextExpenseTypeId();

    if ($.gctlGrid) {
        $.gctlGrid.reload('shipmentModeTable');
    }
}

function saveOrUpdateShipmentMode() {
    let tcId = parseInt($('#shipmentForm input[name="tcId"]').val()) || parseInt($('#tcId').val()) || 0;
    const transportModeTypeId = $('#transportModeTypeId').val();
    const expenseTypeId = $('#shipmentForm input[name="expenseTypeId"]').val() || $('#expenseTypeId').val();
    const expenseType = ($('#shipmentForm input[name="expenseType"]').val() || $('#expenseType').val() || '').trim();
    const shortName = ($('#shipmentForm input[name="shortName"]').val() || $('#shortName').val() || '').trim();

    if (!expenseType) {
        toastr.warning("Shipment Mode is required.");
        $('#expenseType').addClass('error-border').focus();
        return;
    }

    let model = {
        TC: tcId,
        transportModeTypeId: transportModeTypeId,
        ExpenseTypeID: expenseTypeId,
        ExpenseType: expenseType,
        ShortName: shortName
    };

    $.ajax({
        url: '/shipment-mode/is-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {
            if (response.isDuplicate) {
                toastr.warning(response.message || "Data already exists!");
                return;
            }

            if (tcId === 0) {
                $.ajax({
                    url: '/shipment-mode',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data saved successfully.");
                        clearShipmentForm();
                        if ($.gctlGrid) {
                            $.gctlGrid.reload('shipmentModeTable');
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Error saving data.");
                    }
                });
            } else {
                $.ajax({
                    url: `/shipment-mode/${tcId}`,
                    type: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data updated successfully.");
                        clearShipmentForm();
                        if ($.gctlGrid) {
                            $.gctlGrid.reload('shipmentModeTable');
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Update Failed.");
                    }
                });
            }
        },
        error: function () {
            toastr.error("Error checking duplicate.");
        }
    });
}

var selectedShipmentModeIds = [];

function promptDeleteShipmentModes() {
    selectedShipmentModeIds = [];
    $("#shipmentModeTable .js-shipmentmode-check:checked").each(function () {
        let val = parseInt($(this).val());
        if (val) selectedShipmentModeIds.push(val);
    });

    if (selectedShipmentModeIds.length === 0) {
        let currentTc = parseInt($('#shipmentForm input[name="tcId"]').val()) || parseInt($('#tcId').val()) || 0;
        if (currentTc > 0) {
            selectedShipmentModeIds.push(currentTc);
        }
    }

    if (selectedShipmentModeIds.length === 0) {
        toastr.info("Please select at least one item to delete.");
        return;
    }

    $('#ShipmentMode-delete-modal').modal('show');
}

function executeDeleteShipmentModes() {
    if (!selectedShipmentModeIds || selectedShipmentModeIds.length === 0) {
        $('#ShipmentMode-delete-modal').modal('hide');
        return;
    }

    $.ajax({
        url: '/shipment-mode-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedShipmentModeIds),
        success: function (response) {
            $('#ShipmentMode-delete-modal').modal('hide');
            $('#selectAllShipmentMode').prop('checked', false);
            clearShipmentForm();
            if ($.gctlGrid) {
                $.gctlGrid.reload('shipmentModeTable');
            }

            if (response.isSuccess) {
                toastr.success(response.message || "Data Deleted Successfully.");
            } else {
                toastr.error(response.message || "Failed to delete.");
            }
            selectedShipmentModeIds = [];
        },
        error: function (xhr) {
            $('#ShipmentMode-delete-modal').modal('hide');
            let msg = "An error occurred while deleting shipment modes!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment modes:', xhr);
            selectedShipmentModeIds = [];
        }
    });
}

function populateExpenseTypeForm(data) {
    if (!data) return;
    $('#shipmentForm input[name="expenseTypeId"], #expenseTypeId').val(data.expenseTypeID || data.ExpenseTypeID);
    $('#shipmentForm input[name="expenseType"], #expenseType').val(data.expenseType || data.ExpenseType);
    $('#shipmentForm input[name="shortName"], #shortName').val(data.shortName || data.ShortName || "");
    $('#shipmentForm input[name="tcId"], #tcId').val(data.tc || data.TC);

    let transportModeVal = (data.transportModeTypeId || data.TransportModeTypeId || "").trim();
    if (typeof setSelect === "function") {
        setSelect('#transportModeTypeId', transportModeVal, false);
    } else {
        $('#transportModeTypeId').val(transportModeVal).trigger('change');
    }

    let hasDate = false;
    let lDateVal = data.ldate || data.lDate || data.Ldate;
    if (lDateVal) {
        const formattedLDate = new Date(lDateVal).toLocaleString('en-GB', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit', hour12: true
        });
        $('#displayLDate').text(formattedLDate);
        hasDate = true;
    } else {
        $('#displayLDate').text('');
    }

    let modDateVal = data.modifyDate || data.ModifyDate;
    if (modDateVal) {
        const formattedModifyDate = new Date(modDateVal).toLocaleString('en-GB', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit', hour12: true
        });
        $('#displayModifyDate').text(formattedModifyDate);
        hasDate = true;
    } else {
        $('#displayModifyDate').text('');
    }

    if (hasDate) {
        $('#sectionBreak').show();
        $('#LdateModifyHide').show();
    } else {
        $('#sectionBreak').hide();
        $('#LdateModifyHide').hide();
    }
}

$(document).ready(function () {
    // Custom grid initialization
    if ($.gctlGrid && $('#shipmentModeTable').length) {
        $.gctlGrid.initFromModel({ gridId: 'shipmentModeTable' });
    }

    // Edit link click in custom datatable
    $(document).on('click', '.js-shipmentmode-edit', function (e) {
        e.preventDefault();
        let tc = $(this).data('id') || $(this).closest('tr').find('.js-shipmentmode-check').val();
        if (!tc) return;

        $.ajax({
            url: `/shipment-mode/get-by-id/${tc}`,
            type: 'GET',
            success: function (res) {
                if (res.data) {
                    populateExpenseTypeForm(res.data);
                }
            },
            error: function () {
                toastr.error("Failed to fetch shipment mode details.");
            }
        });
    });

    // Save
    $(document).on("click", ".saveBtn, .js-shipmentmode-save", function (e) {
        e.preventDefault();
        saveOrUpdateShipmentMode();
    });

    // Delete prompt
    $(document).on("click", ".deleteBtn, .js-shipmentmode-delete-confirm", function (e) {
        e.preventDefault();
        promptDeleteShipmentModes();
    });

    // Delete execute from modal
    $(document).on("click", ".js-shipmentmode-delete", function (e) {
        e.preventDefault();
        executeDeleteShipmentModes();
    });

    // Clear
    $(document).on("click", ".clearBtn, .js-shipmentmode-clear", function (e) {
        e.preventDefault();
        clearShipmentForm();
    });
});