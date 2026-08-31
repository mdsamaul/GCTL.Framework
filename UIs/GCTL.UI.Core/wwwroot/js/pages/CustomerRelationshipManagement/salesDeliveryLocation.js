let editdlId;
let selectedCustomer;

console.log("Delivery Page Worked");

$(document).ready(() => {
    loadCustomerDD();
    setupDLEventListeners();
    loadDLId();
    showFooterDl();
    setupDLActionButtons();

    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({
            gridId: 'salesDeliveryLocationTable',
            ajaxData: function (d) {
                d.cusId = selectedCustomer || "";
            }
        });
    }

    $("#CustomerIdForDelivery").change(function () {
        var customerId = $(this).val();
        selectedCustomer = customerId || "";
        reloadDeliveryGrid(true);
    });

    $('button#delivery-address-tab').on('shown.bs.tab', function () {
        var dt = $.gctlGrid.getTable('salesDeliveryLocationTable');
        if (dt) {
            dt.columns.adjust().draw(false);
        }
        if (typeof window.initCustomDropdowns === "function") {
            window.initCustomDropdowns('#delivery-form-container', false);
        }
        if (typeof window.initCustomTableDropdowns === "function") {
            window.initCustomTableDropdowns('#delivery-form-container', false);
        }
    });
});

function setupDLEventListeners() {
    $("#btnBulkDeleteDL").on('click', bulkDeleteDeliveryLocation);
    $("#btnDeleteDL").on('click', confirmDelete);

    $(document).on('change', '.js-salesdl-check', toggleBulkActionsDl);
    $(document).on('change', '#selectAllSalesDeliveryLocation', function () {
        $('.js-salesdl-check').prop('checked', $(this).prop('checked'));
        toggleBulkActionsDl();
    });

    // Clear Button handler
    $("#btnResetDL").on('click', function () {
        resetDLForm({ resetCustomer: true, keepEdit: false });
    });

    $('#btnSaveDL').on('click', saveDeliveryLocation);
}

function showFooterDl() {
    $("#Dlfooter").toggleClass("d-none", !editdlId);
}

function toggleBulkActionsDl() {
    const anyChecked = $('.js-salesdl-check:checked').length > 0;
    $('#btnBulkDeleteDL').toggleClass('d-none', !anyChecked);
    $('#btnDeleteDL').toggleClass('d-none', anyChecked);
}

function reloadDeliveryGrid(resetPaging) {
    if ($.gctlGrid) {
        $.gctlGrid.reload('salesDeliveryLocationTable', resetPaging === true);
    }
}

function setupDLActionButtons() {
    $(document).on("click", ".js-salesdl-edit", function (e) {
        e.preventDefault();
        const deliveryId = $(this).data("id");
        if (!deliveryId) return;

        $.ajax({
            url: `/SalesDeliveryLocation/Details/${deliveryId}`,
            method: 'GET',
            success: populateDeliveryLocationForm,
            error: () => toastr.error("Failed to load delivery location details.")
        });
    });
}

function populateDeliveryLocationForm(response) {
    console.log("populate DeliveryLocationForm response:", response);
    editdlId = response.deliveryLocationCode;
    showFooterDl();

    // Populate fields
    $('#DeliveryLocationCode').val(response.deliveryLocationCode);
    $('#CustomerIdForDelivery').val(response.customerId);
    $('#DeliveryCountryId').val(response.countryId);
    $('#LocationAddress').val(response.locationAddress);
    $('#DeliveryPhone').val(response.phone);
    $('#DeliveryEmail').val(response.email);
    $('#DeliveryCity').val(response.city);
    $('#DeliveryStateOrProvince').val(response.stateOrProvince);
    $('#DeliveryZipCode').val(response.zipCode);
    $('#DeliveryRemarks').val(response.remarks);

    // MultiSelect Contact Person
    const cpidInstance = $('#DLCpid').data('customTableDdInstance');
    if (response.contactPerson) {
        const ids = response.contactPerson.split(',').map(s => s.trim()).filter(Boolean);
        if (cpidInstance && typeof cpidInstance.setValue === 'function') {
            cpidInstance.setValue(ids);
        } else {
            $('#DLCpid').val(ids);
        }
    } else {
        if (cpidInstance && typeof cpidInstance.setValue === 'function') {
            cpidInstance.setValue([]);
        } else {
            $('#DLCpid').val([]);
        }
    }

    if (typeof window.initCustomDropdowns === "function") {
        window.initCustomDropdowns('#delivery-form-container', true);
    }
    if (typeof window.initCustomTableDropdowns === "function") {
        window.initCustomTableDropdowns('#delivery-form-container', false);
    }
}

function saveDeliveryLocation(e) {
    e.preventDefault();

    const customerId = $('#CustomerIdForDelivery').val();
    if (!customerId) {
        toastr.warning('Please Select a Customer.');
        return;
    }

    const locationaddress = $('#LocationAddress').val();
    if (!locationaddress) {
        toastr.warning('Please Enter an Address.');
        return;
    }

    const formData = new FormData($('#deliveryForm')[0]);
    formData.set("CustomerId", customerId);
    formData.set("CountryId", $('#DeliveryCountryId').val() || "");

    const selectedContactIds = $('#DLCpid').val();
    if (Array.isArray(selectedContactIds) && selectedContactIds.length > 0) {
        formData.set("ContactPerson", selectedContactIds.join(','));
        formData.set("ContactPersonIds", selectedContactIds.join(','));
    } else {
        formData.set("ContactPerson", "");
        formData.set("ContactPersonIds", "");
    }

    const url = editdlId ? `/SalesDeliveryLocation/Edit/${editdlId}` : '/SalesDeliveryLocation/Create';
    const method = editdlId ? 'PUT' : 'POST';

    $.ajax({
        url,
        type: method,
        data: formData,
        contentType: false,
        processData: false,
        success: function (response) {
            toastr.success(editdlId ? 'Data Updated Successfully' : 'Data Saved Successfully');

            editdlId = null;
            resetDLForm({ resetCustomer: false, keepEdit: false }); 
            reloadDeliveryGrid(false);
        },
        error: function (error) {
            handleDLSaveError(error);
        }
    });
}

function handleDLSaveError(error) {
    if (error.responseJSON && error.responseJSON.errors) {
        let errorMessage = [];

        $.each(error.responseJSON.errors, (_, messages) => {
            $.each(messages, (_, message) => {
                errorMessage.push(`<li>${message}</li>`);
            });
        });

        toastr.error('Validation Error:\n<ul>' + errorMessage.join('') + '</ul>');
    } else {
        toastr.error(error.responseJSON?.message || 'Error saving delivery location');
    }
    console.error("Error saving delivery location:", error);
}

function confirmDelete() {
    if (!editdlId) {
        toastr.warning("Please select a delivery location.");
        return;
    }

    Swal.fire({
        title: "Are you sure?",
        text: "Once deleted, you won't be able to recover this delivery location!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Delete",
        cancelButtonText: "Cancel",
        confirmButtonColor: "#d33"
    }).then((result) => {
        if (result.isConfirmed) {
            deleteDeliveryLocation(editdlId);
            editdlId = null;
        }
    });
}

function deleteDeliveryLocation(dlCode) {
    $.ajax({
        url: `/SalesDeliveryLocation/Delete/${dlCode}`,
        type: 'DELETE',
        success: function (response) {
            if (response.success) {
                toastr.success(response.message);
            }
            resetDLForm();
            loadDLId();
            reloadDeliveryGrid(false);
        },
        error: function (xhr, status, error) {
            console.error("Error deleting delivery location:", error);
            toastr.error("Failed to delete delivery location. Please try again.");
        }
    });
}

function bulkDeleteDeliveryLocation() {
    const selectedIds = [];

    $(".js-salesdl-check:checked").each(function () {
        var val = $(this).val();
        if (val) selectedIds.push(val);
    });

    if (selectedIds.length === 0) {
        Swal.fire('No selection', 'Please select at least one item to delete', 'info');
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete ${selectedIds.length} Delivery Location(s) (This action cannot be undone).`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete them!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/SalesDeliveryLocation/BulkDelete',
                method: 'POST',
                data: { ids: selectedIds },
                traditional: true,
                success: function (response) {
                    Swal.fire('Deleted!', response.message, 'success');

                    resetDLForm({ resetCustomer: false, keepEdit: false });
                    editdlId = null;

                    reloadDeliveryGrid(false);
                },
                error: function () {
                    Swal.fire('Error!', 'An error occurred while deleting.', 'error');
                }
            });
        }
    });
}

function loadDLId() {
    $.ajax({
        type: "GET",
        url: `/SalesDeliveryLocation/GenerateNewSalesDeliveryLocationCode`,
        success: function (response) {
            if (response) {
                console.log("New DeliveryLocationCode:", response);
                $('#DeliveryLocationCode').val(response);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error fetching CustomerId:", error);
        }
    });
}

function loadCustomerDD(selectedId = null) {
    $.ajax({
        url: "/SalesCustomer/SearchCustomers",
        type: "GET",
        dataType: "json",
        success: function (data) {
            var dropdown = $("#CustomerIdForDelivery");
            dropdown.empty();
            dropdown.append('<option value="">Select Customer</option>');

            $.each(data, function (index, customer) {
                var isSelected = selectedId == customer.customerId ? 'selected' : '';
                dropdown.append(
                    '<option value="' + customer.customerId + '" ' + isSelected + '>' + customer.customerName + '</option>'
                );
            });

            if (typeof window.initCustomDropdowns === "function") {
                window.initCustomDropdowns('#delivery-form-container', true);
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.error("Error loading customers:", textStatus, errorThrown);
        }
    });
}

function resetDLForm(options = { resetCustomer: true, keepEdit: false }) {
    const form = document.getElementById('deliveryForm');
    if (form) form.reset();

    if (options.resetCustomer) {
        $('#CustomerIdForDelivery').val('');
        selectedCustomer = "";
    }

    $('#DeliveryCountryId').val('');
    const cpidInst = $('#DLCpid').data('customTableDdInstance');
    if (cpidInst && typeof cpidInst.clear === 'function') {
        cpidInst.clear();
    } else {
        $('#DLCpid').val([]);
    }
    $('#DeliveryPhone, #DeliveryEmail').removeClass('error-border');
    $('#DeliveryPhoneError, #DeliveryEmailError').remove();

    if (typeof window.initCustomDropdowns === "function") {
        window.initCustomDropdowns('#delivery-form-container', true);
    }
    if (typeof window.initCustomTableDropdowns === "function") {
        window.initCustomTableDropdowns('#delivery-form-container', false);
    }

    // Edit mode
    editdlId = options.keepEdit ? editdlId : null;

    // Generate new DeliveryLocationCode
    if (!editdlId) loadDLId();

    $('.js-salesdl-check').prop('checked', false);
    $('#selectAllSalesDeliveryLocation').prop('checked', false);
    toggleBulkActionsDl();
    showFooterDl();
}

// Live Validation for Email And Phone
$(document).on("input", "#DeliveryPhone, #DeliveryEmail", function () {
    const id = $(this).attr("id");
    const value = $(this).val().trim();
    $(this).removeClass("error-border");
    $(`#${id}Error`).remove();

    if (!value) return; 

    if (id === "DeliveryPhone") {
        if (!/^\d+$/.test(value)) {
            $(this).addClass("error-border");
            $(this).after(`<span id="DeliveryPhoneError" class="text-danger">Mobile Number Must Be Number.</span>`);
        }
    }

    if (id === "DeliveryEmail") {
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
            $(this).addClass("error-border");
            $(this).after(`<span id="DeliveryEmailError" class="text-danger">Please Enter Valid Email.</span>`);
        }
    }
});


