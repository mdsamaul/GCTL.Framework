let editdlId;
let selectedCustomer;

console.log("Delivery Page Worked");

$(document).ready(() => {
    loadCustomerDD();
    setupDLEventListeners();
    loadDLId();
    showFooterDl();
    setupDLActionButtons();
    LoadCountryDropdownDL();

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

    $('#CustomerIdForDelivery').select2({
        placeholder: "Customer",
        allowClear: true,
        width: '100%'
    });
}

function showFooterDl() {
    $("#Dlfooter").toggleClass("d-none", !editId);
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

    //populate field
    $('#DeliveryLocationCode').val(response.deliveryLocationCode);

    $('#CustomerIdForDelivery').val(response.customerId).trigger('change.select2');
    $('#DeliveryCountryId').val(response.countryId).trigger('change.select2');
    $('#LocationAddress').val(response.locationAddress);
    $('#DeliveryPhone').val(response.phone);
    $('#DeliveryEmail').val(response.email);
    $('#DeliveryCity').val(response.city);
    $('#DeliveryZipCode').val(response.zipCode);
    $('#DeliveryRemarks').val(response.remarks);

    //Contact Person auto-select
    loadContactPersonDropdownDL(function () {
        if (response.contactPerson) {
            const ids = response.contactPerson.split(',');
            $('.contact-person-checkboxDL').prop('checked', false);
            ids.forEach(id => {
                $(`.contact-person-checkboxDL[value='${id}']`).prop('checked', true);
            });
            updateSelectedContactsDL(); 
        }
    });
}

function saveDeliveryLocation(e) {
    e.preventDefault();

    const formData = new FormData($('#deliveryForm')[0]);
    const customerId = formData.get("CustomerId");
    if (!customerId) {
        toastr.warning('Please Select a Customer.');
        return;
    }

    const locationaddress = formData.get("LocationAddress");
    if (!locationaddress) {
        toastr.warning('Please Enter a Address.');
        return;
    }

    const selectedContacts = $('#DLselectedContactPersonIds').val();
    if (selectedContacts) {
        formData.append('ContactPersonIds', selectedContacts); // only append if selected
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

function handleDLSaveError(error){
    if (error.responseJSON && error.responseJSON.errors) {
        let errorMessage = [];

        $.each(error.responseJSON.errors, (_, messages) => {
            $.each(messages, (_, message) => {
                errorMessage.push(`<li>${message}</li>`);
            });
        });

        toastr.error('Validation Error:\n<ul>' + errorMessage.join('') + '</ul>');
    } else {
        toastr.error('Error saving delivery location');
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

function loadCustomerDD() {
    $.ajax({
        url: "/SalesCustomer/SearchCustomers",
        type: "GET",
        dataType: "json",
        success: function (data) {
            var dropdown = $("#CustomerIdForDelivery");

            dropdown.empty();
            dropdown.append('<option value="">--Select Customer--</option>');

            $.each(data, function (index, customer) {
                dropdown.append(
                    '<option value="' + customer.customerId + '">' + customer.customerName + '</option>'
                );
            });

            //dropdown.trigger('change');
        },
        error: function (jqXHR, textStatus, errorThrown) {
            console.error("Error loading customers:", textStatus, errorThrown);
        }
    });
}

//Coutry dropdown
function LoadCountryDropdownDL() {
    $.ajax({
        url: '/SalesDeliveryLocation/GetCountryDropdown',
        method: 'GET',
        success: function (countries) {
            console.log("Country Dropdown Loaded", countries);
            var $country = $('#DeliveryCountryId');
            $country.empty().append('<option value=""></option>');

            $.each(countries, function (index, country) {
                $country.append(
                    '<option value="' + country.countryID + '">' + country.countryName + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            $country.select2({
                placeholder: "Country",
                width: '100%',

            });
        },
        error: function (xhr, status, error) {
            console.error('Failed to load countries:', error);
        }
    });
}

//Contact Person Dropdown

$(document).ready(function () {
    loadContactPersonDropdownDL();

    var dropdownElement = document.getElementById('DLdropdownButton');
    if (dropdownElement) {
        var bootstrapDropdown = new bootstrap.Dropdown(dropdownElement, {
            autoClose: true,
            popperConfig: {
                placement: 'top-start'
            }
        });

        $('#DLdropdownButton').on('click', function (e) {
            e.stopPropagation();
            bootstrapDropdown.toggle();
        });

        $(document).on('click', function (event) {
            if (!$(event.target).closest('#DLcontactPersonDropdown, #DLdropdownButton').length) {
                bootstrapDropdown.hide();
            }
        });
    }

    // Search/filter inside dropdown
    $('#DLcontactSearch').on('input', function () {
        var value = $(this).val().toLowerCase();
        $('#DLcontactPersonList tr').each(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });


    $(document).on('change', '.contact-person-checkboxDL', updateSelectedContactsDL);

    $('#DLselectAllContacts').on('change', function () {
        $('.contact-person-checkboxDL').prop('checked', this.checked);
        updateSelectedContactsDL();
    });

    window.addEventListener('contactPersonChanged', function (e) {
        console.log("Contact Person changed, reloading dropdown:", e.detail);
        loadContactPersonDropdownDL();
    });
});

function loadContactPersonDropdownDL(callback) {
    $.ajax({
        url: '/SalesDeliveryLocation/GetContactPerson',
        type: 'GET',
        dataType: 'json',
        success: function (persons) {
            console.log("Contact Person Data for Delivery Page", persons);
            const $list = $('#DLcontactPersonList');
            $list.empty();

            if (persons.length > 0) {
                $.each(persons, function (_, person) {
                    const $row = $('<tr>');
                    $row.append(
                        $('<td>').css('text-align', 'center').append(
                            $('<input>').attr({
                                type: 'checkbox',
                                class: 'contact-person-checkboxDL',
                                value: person.cpid
                            }).data({
                                name: person.contactPersonName,
                                phone: person.contactPersonMobile || '',
                                designation: person.designationName || '',
                                email: person.contactPersonEmail || ''
                            })
                        )
                    );
                    $row.append($('<td>').text(person.contactPersonName));
                    $row.append($('<td>').text(person.designationName || ''));
                    $row.append($('<td>').text(person.contactPersonMobile || ''));
                    $row.append($('<td>').text(person.contactPersonEmail || ''));
                    $list.append($row);
                });
            } else {
                $list.append('<tr><td colspan="5" class="text-center">No Data Available</td></tr>');
            }

            if (callback) callback();
        },
        error: function (xhr) {
            console.error("Error fetching contact persons:", xhr.responseText);
            $('#DLcontactPersonList').html('<tr><td colspan="5" class="text-center text-danger">Error loading contacts</td></tr>');
        }
    });
}

//Update selected contacts properly
function updateSelectedContactsDL() {
    let selectedDLIds = [];
    $('.contact-person-checkboxDL:checked').each(function () {
        selectedDLIds.push($(this).val());
    });

    selectedDLIds = [...new Set(selectedDLIds)];
    $('#DLselectedContactPersonIds').val(selectedDLIds.join(','));
}

function resetDLForm(options = { resetCustomer: true, keepEdit: false }) {
    const form = document.getElementById('deliveryForm');
    if (form) form.reset();

    if (options.resetCustomer) {
        $('#CustomerIdForDelivery').val('').trigger('change.select2');
        selectedCustomer = "";
    }

    // Clear contact person selection
    $('#DLselectedContactPersonIds').val('');
    $('.contact-person-checkboxDL').prop('checked', false);

    // Clear country & other fields
    $('#DeliveryCountryId').val('').trigger('change');
    $('#DeliveryContactPerson').val('').trigger('change');
    $('#DeliveryPhone, #DeliveryEmail').removeClass('error-border');
    $('#DeliveryPhoneError, #DeliveryEmailError').remove();

    // Edit mode
    editdlId = options.keepEdit ? editdlId : null;

    // Generate new DeliveryLocationCode
    if (!editdlId) loadDLId();

    $('.js-salesdl-check').prop('checked', false);
    $('#selectAllSalesDeliveryLocation').prop('checked', false);
    toggleBulkActionsDl();
    showFooterDl();
}

//Live Validation for Email And Phone
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


