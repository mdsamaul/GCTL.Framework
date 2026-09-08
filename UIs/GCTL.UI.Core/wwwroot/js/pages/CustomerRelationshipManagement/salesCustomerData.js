let editId = null;

$(document).ready(function () {
    if ($.gctlGrid) {
        $.gctlGrid.initFromModel({ gridId: 'salesCustomerTable' });
    }

    setupEventListeners();
    loadCUSId();
    showFooter();

    $("#btnAddContactPerson").click(function () {
        $("#contactPersonModal").modal("show");
    });
});

function setupEventListeners() {
    // Clear Button
    $("#salesCustomer-resetBtn").on('click', resetCustomerForm);

    // Save Button
    $('#salesCustomer-btnSave, #salesCustomer-btnSave-bottom, .js-salescustomer-save').on('click', saveCustomer);

    // Delete Button (Triggers modal)
    $('#salesCustomer-btnDelete, #salesCustomer-btnBulkDelete').on('click', function (e) {
        e.preventDefault();
        var selectedIds = getSelectedCustomerIds();
        if (selectedIds.length === 0) {
            toastr.warning('Please select at least one customer to delete.');
            return;
        }
        $('#salescustomer-delete-modal').modal('show');
    });

    // Confirm Delete inside Modal
    $(document).on('click', '.js-salescustomer-delete', function (e) {
        e.preventDefault();
        var selectedIds = getSelectedCustomerIds();
        if (selectedIds.length === 0) {
            toastr.warning('No customer selected to delete.');
            $('#salescustomer-delete-modal').modal('hide');
            return;
        }

        $.ajax({
            url: '/SalesCustomer/Delete',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(selectedIds),
            success: function (response) {
                $('#salescustomer-delete-modal').modal('hide');
                if (response.isSuccess !== false && response.success !== false) {
                    toastr.success(response.message || 'Data deleted successfully!');
                    resetCustomerForm();
                    refreshCustomerGrid();
                } else {
                    toastr.error(response.message || 'Failed to delete customer.');
                }
            },
            error: function (xhr, status, error) {
                $('#salescustomer-delete-modal').modal('hide');
                console.error("Error deleting customer:", error);
                toastr.error("Failed to delete customer. Please try again.");
            }
        });
    });

    // Edit row selection from _CustomGrid
    $(document).on('click', '.js-salescustomer-edit', function (e) {
        e.preventDefault();
        var customerId = $(this).data('id');
        if (!customerId) return;

        $.ajax({
            url: `/SalesCustomer/Details/${customerId}`,
            method: 'GET',
            success: populateCustomerForm,
            error: function () {
                toastr.warning("Failed to fetch customer details.");
            }
        });
    });

    // Report buttons
    $("#previewBtn").on('click', function () { generateReport('profile'); });
    $("#listPreviewBtn").on('click', function () { generateReport('list'); });
}

function getSelectedCustomerIds() {
    var selectedIds = [];
    $('.js-salescustomer-check:checked').each(function () {
        var val = $(this).val();
        if (val) selectedIds.push(val);
    });
    if (selectedIds.length === 0 && editId) {
        selectedIds.push(editId);
    }
    return selectedIds;
}

function refreshCustomerGrid() {
    if ($.gctlGrid) {
        $.gctlGrid.reload('salesCustomerTable');
    }
}

function showFooter() {
    if (editId) {
        $("#salesCustomerLDateContainer").show();
        $("#salesCustomerModifyDateContainer").show();
    } else {
        $("#salesCustomerLDateContainer").hide();
        $("#salesCustomerModifyDateContainer").hide();
    }
}

function populateCustomerForm(response) {
    editId = response.customerId;
    showFooter();

    $.each(response, (key, value) => {
        const formattedKey = key.charAt(0).toUpperCase() + key.slice(1);
        const $field = $(`#${formattedKey}`);

        if ($field.length) {
            $field.val(value);
            if ($field.is('select')) {
                $field.trigger('change');
            }
        }
    });

    if (response.openingDate) {
        const date = new Date(response.openingDate);
        const formattedDate = date.toISOString().split('T')[0];
        $("#OpeningDate").val(formattedDate);
    }

    $("#displayLDate").val(formatDate(response.lDate));
    $("#displayModifyDate").val(formatDate(response.modifyDate));

    if (response.contactPerson) {
        const selectedContactPersons = response.contactPerson.split(',');
        $.each(selectedContactPersons, (_, contactId) => {
            $(`#contactPersonList input[type="checkbox"][value="${contactId.trim()}"]`).prop('checked', true);
        });
        $("#selectedContactPersonIds").val(selectedContactPersons.join(','));
    }
}

function saveCustomer(e) {
    e.preventDefault();

    const customerName = $('#CustomerName').val().trim();
    if (!customerName) {
        toastr.error('Please Enter Customer Name.');
        $('#CustomerName').focus();
        return;
    }

    const address = $('#CustomerAddress').val().trim();
    if (!address) {
        toastr.error('Please Enter Address.');
        $('#CustomerAddress').focus();
        return;
    }

    const selectedIds = [];
    $('.contact-person-checkbox:checked').each(function () {
        selectedIds.push($(this).val());
    });
    $('#selectedContactPersonIds').val(selectedIds.join(','));

    const formData = new FormData($('#customerForm')[0]);

    const url = editId ? `/SalesCustomer/Edit/${editId}` : '/SalesCustomer/Create';
    const method = editId ? 'PUT' : 'POST';

    $.ajax({
        url: url,
        type: method,
        data: formData,
        contentType: false,
        processData: false,
        success: function (response) {
            if (response.isSuccess !== false) {
                toastr.success(editId ? 'Data Updated Successfully' : 'Data Saved Successfully');
                resetCustomerForm();
                refreshCustomerGrid();
            } else {
                toastr.error(response.message || 'Error saving customer');
            }
        },
        error: function (error) {
            handleSaveError(error);
        }
    });
}

function handleSaveError(error) {
    if (error.responseJSON && error.responseJSON.errors) {
        let errorMessages = [];
        $.each(error.responseJSON.errors, (_, messages) => {
            $.each(messages, (_, message) => {
                errorMessages.push(`<li>${message}</li>`);
            });
        });
        toastr.error('Validation Error:\n<ul>' + errorMessages.join('') + '</ul>');
    } else {
        toastr.error(error.responseJSON?.message || 'Error saving customer');
    }
    console.error("Error saving customer:", error);
}

function loadCUSId() {
    $.ajax({
        type: "GET",
        url: `/SalesCustomer/GenerateNewCustomerId`,
        success: function (response) {
            if (response) {
                $('#CustomerId').val(response);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error fetching CustomerId:", error);
        }
    });
}

function resetCustomerForm() {
    $('#customerForm')[0].reset();
    $('#CustomerId').val('');
    $('#CountryId, #TransactionType, #Category, #SalesPerson, #ContactPerson')
        .val('').trigger('change');

    $("#displayLDate").val('');
    $("#displayModifyDate").val('');

    editId = null;
    loadCUSId();
    showFooter();

    $('#Phone, #Email').removeClass('error-border');
    $('#PhoneError, #EmailError').remove();
}

function formatDate(dateStr) {
    if (!dateStr) return "--";
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
        year: 'numeric', month: 'short', day: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

// Live Phone and Email Validation
$(document).on("input", "#Phone, #Email", function () {
    const $this = $(this);
    const id = $this.attr("id");
    const value = $this.val().trim();

    $this.removeClass("error-border");
    $(`#${id}Error`).remove();

    if (!value) return; 

    if (id === "Phone") {
        if (!/^\d+$/.test(value)) {
            $this.addClass("error-border");
            $this.after(`<span id="PhoneError" class="text-danger">Phone Must Be Number.</span>`);
        }
    }

    if (id === "Email") {
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
            $this.addClass("error-border");
            $this.after(`<span id="EmailError" class="text-danger">Please Enter Valid Email.</span>`);
        }
    }
});

//#region Customer Report
function generateReport(viewType) {
    const url = '/SalesCustomer/Preview';

    const $form = $('<form>')
        .attr({
            action: url,
            method: 'POST',
            target: '_blank' 
        });

    $('<input>')
        .attr({
            type: 'hidden',
            name: 'viewType',
            value: viewType
        })
        .appendTo($form);

    $form.appendTo('body').submit().remove();
}
//#endregion