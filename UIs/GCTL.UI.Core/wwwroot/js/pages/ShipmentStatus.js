function getNextShipmentStatusId() {
    $.ajax({
        url: '/next-shipment-status-id',
        type: 'GET',
        success: function (response) {
            $('#shipmentStatusId, #shipmentStatusForm input[name="shipmentStatusId"]').val(response);
        },
        error: function (xhr) {
            console.error("Error fetching next Shipment Status ID:", xhr.responseText);
        }
    });
}

function clearShipmentStatusForm() {
    if ($('#shipmentStatusForm').length) {
        $('#shipmentStatusForm')[0].reset();
    }
    $('#shipmentStatus').removeClass('error-border');
    $('#statusSectionBreak').hide();
    $('#statusLdateModifyHide').hide();
    $('#shipmentStatusForm input[name="autoId"], #autoId').val(0);
    $('#selectAllShipmentStatus').prop('checked', false);
    $('.js-shipmentstatus-check').prop('checked', false);

    if (typeof setSelect === "function") {
        setSelect('#statusTransportModeTypeId', '', false);
    } else {
        $('#statusTransportModeTypeId').val('').trigger('change');
    }

    getNextShipmentStatusId();

    if ($.gctlGrid) {
        $.gctlGrid.reload('shipmentStatusTable');
    }
}

function saveOrUpdateShipmentStatus() {
    let autoId = parseInt($('#shipmentStatusForm input[name="autoId"]').val()) || parseInt($('#autoId').val()) || 0;
    const transportModeTypeId = $('#statusTransportModeTypeId').val();
    const shipmentStatusId = $('#shipmentStatusForm input[name="shipmentStatusId"]').val() || $('#shipmentStatusId').val();
    const shipmentStatus = ($('#shipmentStatusForm input[name="shipmentStatus"]').val() || $('#shipmentStatus').val() || '').trim();
    const shortName = ($('#shipmentStatusForm input[name="statusShortName"]').val() || $('#statusShortName').val() || '').trim();

    if (!shipmentStatus) {
        toastr.warning("Shipment Status is required.");
        $('#shipmentStatus').addClass('error-border').focus();
        return;
    }

    let model = {
        AutoId: autoId,
        TransportModeTypeId: transportModeTypeId,
        ShipmentStatusId: shipmentStatusId,
        ShipmentStatus: shipmentStatus,
        ShortName: shortName
    };

    $.ajax({
        url: '/shipment-status/is-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {
            if (response.isDuplicate) {
                toastr.warning(response.message || "Data already exists!");
                return;
            }

            if (autoId === 0) {
                $.ajax({
                    url: '/shipment-status',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data saved successfully.");
                        clearShipmentStatusForm();
                        if ($.gctlGrid) {
                            $.gctlGrid.reload('shipmentStatusTable');
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Error saving data.");
                    }
                });
            } else {
                $.ajax({
                    url: `/shipment-status/${autoId}`,
                    type: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data updated successfully.");
                        clearShipmentStatusForm();
                        if ($.gctlGrid) {
                            $.gctlGrid.reload('shipmentStatusTable');
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

var selectedShipmentStatusIds = [];

function promptDeleteShipmentStatuses() {
    selectedShipmentStatusIds = [];
    $("#shipmentStatusTable .js-shipmentstatus-check:checked").each(function () {
        let val = parseInt($(this).val());
        if (val) selectedShipmentStatusIds.push(val);
    });

    if (selectedShipmentStatusIds.length === 0) {
        let currentAutoId = parseInt($('#shipmentStatusForm input[name="autoId"]').val()) || parseInt($('#autoId').val()) || 0;
        if (currentAutoId > 0) {
            selectedShipmentStatusIds.push(currentAutoId);
        }
    }

    if (selectedShipmentStatusIds.length === 0) {
        toastr.info("Please select at least one item to delete.");
        return;
    }

    $('#ShipmentStatus-delete-modal').modal('show');
}

function executeDeleteShipmentStatuses() {
    if (!selectedShipmentStatusIds || selectedShipmentStatusIds.length === 0) {
        $('#ShipmentStatus-delete-modal').modal('hide');
        return;
    }

    $.ajax({
        url: '/shipment-status-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedShipmentStatusIds),
        success: function (response) {
            $('#ShipmentStatus-delete-modal').modal('hide');
            $('#selectAllShipmentStatus').prop('checked', false);
            clearShipmentStatusForm();
            if ($.gctlGrid) {
                $.gctlGrid.reload('shipmentStatusTable');
            }

            if (response.isSuccess) {
                toastr.success(response.message || "Data Deleted Successfully.");
            } else {
                toastr.error(response.message || "Failed to delete.");
            }
            selectedShipmentStatusIds = [];
        },
        error: function (xhr) {
            $('#ShipmentStatus-delete-modal').modal('hide');
            let msg = "An error occurred while deleting shipment statuses!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment statuses:', xhr);
            selectedShipmentStatusIds = [];
        }
    });
}

function populateShipmentStatusForm(data) {
    if (!data) return;
    $('#shipmentStatusForm input[name="autoId"], #autoId').val(data.autoId || data.AutoId);
    $('#shipmentStatusForm input[name="shipmentStatusId"], #shipmentStatusId').val(data.shipmentStatusId || data.ShipmentStatusId);
    $('#shipmentStatusForm input[name="shipmentStatus"], #shipmentStatus').val(data.shipmentStatus || data.ShipmentStatus);
    $('#shipmentStatusForm input[name="statusShortName"], #statusShortName').val(data.shortName || data.ShortName || "");

    let transportModeVal = (data.transportModeTypeId || data.TransportModeTypeId || "").trim();
    if (typeof setSelect === "function") {
        setSelect('#statusTransportModeTypeId', transportModeVal, false);
    } else {
        $('#statusTransportModeTypeId').val(transportModeVal).trigger('change');
    }

    let hasDate = false;
    let lDateVal = data.ldate || data.lDate || data.Ldate;
    if (lDateVal) {
        const formattedLDate = new Date(lDateVal).toLocaleString('en-GB', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit', hour12: true
        });
        $('#statusDisplayLDate').text(formattedLDate);
        hasDate = true;
    } else {
        $('#statusDisplayLDate').text('');
    }

    let modDateVal = data.modifyDate || data.ModifyDate;
    if (modDateVal) {
        const formattedModifyDate = new Date(modDateVal).toLocaleString('en-GB', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit', hour12: true
        });
        $('#statusDisplayModifyDate').text(formattedModifyDate);
        hasDate = true;
    } else {
        $('#statusDisplayModifyDate').text('');
    }

    if (hasDate) {
        $('#statusSectionBreak').show();
        $('#statusLdateModifyHide').show();
    } else {
        $('#statusSectionBreak').hide();
        $('#statusLdateModifyHide').hide();
    }
}

$(document).ready(function () {
    // Edit link click in custom datatable
    $(document).on('click', '.js-shipmentstatus-edit', function (e) {
        e.preventDefault();
        let autoId = $(this).data('id') || $(this).closest('tr').find('.js-shipmentstatus-check').val();
        if (!autoId) return;

        $.ajax({
            url: `/shipment-status/get-by-id/${autoId}`,
            type: 'GET',
            success: function (res) {
                if (res.data) {
                    populateShipmentStatusForm(res.data);
                }
            },
            error: function () {
                toastr.error("Failed to fetch shipment status details.");
            }
        });
    });

    // Save
    $(document).on("click", ".saveShipmentStatusBtn, .js-shipmentstatus-save", function (e) {
        e.preventDefault();
        saveOrUpdateShipmentStatus();
    });

    // Delete prompt
    $(document).on("click", ".deleteShipmentStatusBtn, .js-shipmentstatus-delete-confirm", function (e) {
        e.preventDefault();
        promptDeleteShipmentStatuses();
    });

    // Delete execute from modal
    $(document).on("click", ".js-shipmentstatus-delete", function (e) {
        e.preventDefault();
        executeDeleteShipmentStatuses();
    });

    // Clear
    $(document).on("click", ".clearShipmentStatusBtn, .js-shipmentstatus-clear", function (e) {
        e.preventDefault();
        clearShipmentStatusForm();
    });
});