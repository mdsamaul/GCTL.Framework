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
    $('#statusModifyDateContainer').hide();
    $('#statusLDateContainer').hide();
    $('#shipmentStatusForm input[name="autoId"], #autoId').val(0);
    $('#selectAllShipmentStatus').prop('checked', false);
    $('.js-shipmentstatus-check').prop('checked', false);

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

function deleteSelectedShipmentStatuses() {
    let selectedIds = [];
    $("#shipmentStatusTable .js-shipmentstatus-check:checked").each(function () {
        let val = parseInt($(this).val());
        if (val) selectedIds.push(val);
    });

    if (selectedIds.length === 0) {
        let currentAutoId = parseInt($('#shipmentStatusForm input[name="autoId"]').val()) || parseInt($('#autoId').val()) || 0;
        if (currentAutoId > 0) {
            selectedIds.push(currentAutoId);
        }
    }

    if (selectedIds.length === 0) {
        toastr.warning("Please select at least one item to delete.");
        return;
    }

    if (!confirm(`Are you sure you want to delete ${selectedIds.length} record(s)?`)) {
        return;
    }

    $.ajax({
        url: '/shipment-status-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedIds),
        success: function (response) {
            $('#selectAllShipmentStatus').prop('checked', false);
            clearShipmentStatusForm();
            if ($.gctlGrid) {
                $.gctlGrid.reload('shipmentStatusTable');
            }

            if (response.isSuccess) {
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function (xhr) {
            let msg = "An error occurred while deleting shipment statuses!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment statuses:', xhr);
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

    let lDateVal = data.ldate || data.lDate || data.Ldate;
    if (lDateVal) {
        const formattedLDate = new Date(lDateVal).toLocaleDateString('en-GB');
        $('#statusDisplayLDate').text(formattedLDate);
        $('#statusLDateContainer').show();
    } else {
        $('#statusLDateContainer').hide();
    }

    let modDateVal = data.modifyDate || data.ModifyDate;
    if (modDateVal) {
        const formattedModifyDate = new Date(modDateVal).toLocaleDateString('en-GB');
        $('#statusDisplayModifyDate').text(formattedModifyDate);
        $('#statusModifyDateContainer').show();
    } else {
        $('#statusModifyDateContainer').hide();
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

    // Toolbar buttons
    $(document).on("click", ".saveShipmentStatusBtn", function (e) {
        e.preventDefault();
        saveOrUpdateShipmentStatus();
    });

    $(document).on("click", ".deleteShipmentStatusBtn", function (e) {
        e.preventDefault();
        deleteSelectedShipmentStatuses();
    });

    $(document).on("click", ".clearShipmentStatusBtn", function (e) {
        e.preventDefault();
        clearShipmentStatusForm();
    });
});