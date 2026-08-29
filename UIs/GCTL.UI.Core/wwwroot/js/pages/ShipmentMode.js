$(function () {
    function showShipmentMode() {
        $("#toolbarEntry").addClass("show");
        $("#toolbarUpload").removeClass("show");
    }

    function showShipmentStatus() {
        $("#toolbarUpload").addClass("show");
        $("#toolbarEntry").removeClass("show");
    }

    // Default
    showShipmentMode();

    // Tab Switches
    $('#shipment-type-tab').on('shown.bs.tab', function () {
        showShipmentMode();
    });

    $('#shipment-status-tab').on('shown.bs.tab', function () {
        showShipmentStatus();
    });
});

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
    $('#modifyDateContainer').hide();
    $('#lDateContainer').hide();
    $('#shipmentForm input[name="tcId"]').val(0);
    $('#selectAllShipmentMode').prop('checked', false);
    $('.js-shipmentmode-check').prop('checked', false);

    getNextExpenseTypeId();

    if ($.gctlGrid) {
        $.gctlGrid.reload('shipmentModeTable');
    }
}

function saveOrUpdateShipmentMode() {
    let tcId = parseInt($('#shipmentForm input[name="tcId"]').val()) || 0;
    const transportModeTypeId = $('#transportModeTypeId').val();
    const expenseTypeId = $('#shipmentForm input[name="expenseTypeId"]').val();
    const expenseType = $('#shipmentForm input[name="expenseType"]').val().trim();
    const shortName = $('#shipmentForm input[name="shortName"]').val().trim();

    if (!expenseType) {
        toastr.warning("Expense Type is required.");
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

function deleteSelectedShipmentModes() {
    let selectedIds = [];
    $("#shipmentModeTable .js-shipmentmode-check:checked").each(function () {
        let val = parseInt($(this).val());
        if (val) selectedIds.push(val);
    });

    if (selectedIds.length === 0) {
        let currentTc = parseInt($('#shipmentForm input[name="tcId"]').val()) || 0;
        if (currentTc > 0) {
            selectedIds.push(currentTc);
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
        url: '/shipment-mode-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedIds),
        success: function (response) {
            $('#selectAllShipmentMode').prop('checked', false);
            clearShipmentForm();
            if ($.gctlGrid) {
                $.gctlGrid.reload('shipmentModeTable');
            }

            if (response.isSuccess) {
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function (xhr) {
            let msg = "An error occurred while deleting shipment modes!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment modes:', xhr);
        }
    });
}

function populateExpenseTypeForm(data) {
    if (!data) return;
    $('#shipmentForm input[name="expenseTypeId"]').val(data.expenseTypeID || data.ExpenseTypeID);
    $('#shipmentForm input[name="expenseType"]').val(data.expenseType || data.ExpenseType);
    $('#shipmentForm input[name="shortName"]').val(data.shortName || data.ShortName || "");
    $('#shipmentForm input[name="tcId"]').val(data.tc || data.TC);
    
    let transportModeVal = (data.transportModeTypeId || data.TransportModeTypeId || "").trim();
    if (typeof setSelect === "function") {
        setSelect('#transportModeTypeId', transportModeVal, false);
    } else {
        $('#transportModeTypeId').val(transportModeVal).trigger('change');
    }

    let lDateVal = data.ldate || data.lDate || data.Ldate;
    if (lDateVal) {
        const formattedLDate = new Date(lDateVal).toLocaleDateString('en-GB');
        $('#displayLDate').text(formattedLDate);
        $('#lDateContainer').show();
    } else {
        $('#lDateContainer').hide();
    }

    let modDateVal = data.modifyDate || data.ModifyDate;
    if (modDateVal) {
        const formattedModifyDate = new Date(modDateVal).toLocaleDateString('en-GB');
        $('#displayModifyDate').text(formattedModifyDate);
        $('#modifyDateContainer').show();
    } else {
        $('#modifyDateContainer').hide();
    }
}

$(document).ready(function () {
    // Load Form
    $.ajax({
        url: '/ShipmentMode/ShipmentForm',
        type: 'GET',
        success: function (data) {
            $('#shipmentFormContainer').html(data);
            if (typeof window.initCustomDropdowns === "function") {
                window.initCustomDropdowns('#shipmentFormContainer', true);
            }
            getNextExpenseTypeId();
        },
        error: function (xhr, status, error) {
            console.error("Failed to load ShipmentForm:", error);
        }
    });

    // Load List & initialize CustomGrid
    $.ajax({
        url: '/ShipmentMode/ShipmentList',
        type: 'GET',
        success: function (data) {
            $('#shipmentList').html(data);
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'shipmentModeTable' });
            }
        },
        error: function (xhr, status, error) {
            console.error("Failed to load ShipmentList:", error);
        }
    });

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

    // Toolbar buttons
    $(document).on("click", ".saveBtn", function (e) {
        e.preventDefault();
        saveOrUpdateShipmentMode();
    });

    $(document).on("click", ".deleteBtn", function (e) {
        e.preventDefault();
        deleteSelectedShipmentModes();
    });

    $(document).on("click", ".clearBtn", function (e) {
        e.preventDefault();
        clearShipmentForm();
    });
});