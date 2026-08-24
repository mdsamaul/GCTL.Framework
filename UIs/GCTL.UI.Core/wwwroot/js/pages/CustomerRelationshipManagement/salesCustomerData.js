let currentPage = 1;
let pageSize = 10;
let editId = null;
let currentSortColumn = 'CustomerID';
let currentSortOrder = 'desc';

$(document).ready(() => {
    setupEventListeners();
    loadTableData();
    loadCUSId();
    showFooter();
    updateSortingIndicator();
    //loadCustomerList();
    LoadCountryDropdown();

    $("#btnAddContactPerson").click(function () {
        console.log("Hello");
        $("#contactPersonModal").modal("show");
    });

});

//Coutry Dropdown 
function LoadCountryDropdown() {
    $.ajax({
        url: '/SalesCustomer/GetAllCountryDropdown', 
        method: 'GET',
        success: function (countries) {
            console.log("Country Dropdown Loaded", countries);
            var $country = $('#CountryId'); 
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

function setupEventListeners() {

    //Showing Entites
    $('#salesCustomer-pageSizeDropdown').on('change', function () {
        const selectedSize = $(this).val();
        pageSize = parseInt(selectedSize, 10);

        console.log("New pageSize:", pageSize);
        currentPage = 1; 
        loadTableData();
    });

    //Sorting
    $('th.salesCustomer-sort').on('click', function () {
        const column = $(this).data('sort');
        $('.salesCustomer-sort').removeClass('sort-asc sort-desc');

        if (currentSortColumn === column) {
            currentSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortColumn = column;
            currentSortOrder = 'asc';
        }

        $(this).addClass(`sort-${currentSortOrder}`);
        loadTableData(currentSortColumn, currentSortOrder);
        updateSortingIndicator();
    });

    $("#salesCustomer-btnBulkDelete").on('click', bulkDeleteCustomers);

    $("#salesCustomer-btnDelete").on('click', confirmDeleteCustomer);

    //Searach
    const debouncedSearch = debounce(() => {
        currentPage = 1;
        loadTableData();
    }, 300);

    $("#salesCustomer-searchInput").on("input", debouncedSearch);
    
    //Previous Button
    $("#salesCustomer-prevPageBtn").on('click', () => {
        if (currentPage > 1) {
            currentPage--;
            loadTableData();
        }
    });
    
    //Next Button
    $("#salesCustomer-nextPageBtn").on('click', () => {
        if (currentPage < totalPages) {
            currentPage++;
            loadTableData();
        }
    });
    
    //Clear Button
    $("#salesCustomer-resetBtn").on('click', resetCustomerForm);

    //Save Button
    $('#salesCustomer-btnSave').on('click', saveCustomer);

    $('#salesCustomer-check-all').on('change', function () {
        $('.salesCustomer-selectItem').prop('checked', $(this).prop('checked'));
        toggleBulkActions();
    });

    $(document).on('change', '.salesCustomer-selectItem', toggleBulkActions);

    // Customer selection via dropdowns
    $("#selectAll").on('change', function () {
        $(".customer-checkbox").prop('checked', $(this).prop('checked'));
    });

    $(document).on('change', ".customer-checkbox", () => {
        const allChecked = $(".customer-checkbox:checked").length === $(".customer-checkbox").length;
        $("#selectAll").prop('checked', allChecked);
    });

    // Report buttons
    $("#previewBtn").on('click', () => generateReport('profile'));
    $("#listPreviewBtn").on('click', () => generateReport('list'));

    
}

function debounce(func, delay) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => {
            func.apply(this, args);
        }, delay);
    };
}

function updateSortingIndicator() {
    $('th.salesCustomer-sort').find('.sort-icon').remove();
    const th = $(`th.salesCustomer-sort[data-sort="${currentSortColumn}"]`);
    const icon = currentSortOrder === 'asc' ? 'fa-sort-up' : 'fa-sort-down';
    th.append(`<span class="sort-icon ms-2"><i class="fas ${icon}"></i></span>`);
}

function showFooter() {
    $("#salesCustomer-Footer").toggleClass("d-none", !editId);
}

function toggleBulkActions() {
    const anyChecked = $('.salesCustomer-selectItem:checked').length > 0;
    $('#salesCustomer-btnBulkDelete').toggleClass('d-none', !anyChecked);
    $('#salesCustomer-btnDelete').toggleClass('d-none', anyChecked);
}

function loadTableData(sortColumn = currentSortColumn, sortOrder = currentSortOrder) {
    const searchTerm = $("#salesCustomer-searchInput").val();

    $.ajax({
        url: '/SalesCustomer/GetAll',
        method: 'GET',
        data: {
            pageNumber: currentPage,
            pageSize,
            searchTerm,
            sortColumn,
            sortOrder
        },
        success: function (response) {
            renderCustomerTable(response);
            setupActionButtons();
        },
        error: function (error) {
            console.error("Error loading customer data:", error);
            $("#salesCustomer-table-body").html('<tr><td colspan="7" class="text-center">Error loading data</td></tr>');
        }
    });
}

function renderCustomerTable(response) {
    const tableBody = $("#salesCustomer-table-body");
    tableBody.empty();

    if (response.data.length > 0) {
        $(".no-data").hide();

        $.each(response.data, (_, item) => {
            tableBody.append(`
                <tr class="position-static customer-row" data-id="${item.customerId}">
                    <td style="width:5%" class="text-center text-middle align-middle">
                        <input type="checkbox" class="salesCustomer-selectItem" data-id="${item.customerId}" />
                    </td>
                    <td style="padding: 5px 10px; font-size: 10px;" class="align-middle white-space-nowrap ps-0">${item.customerId}</td>
                    <td style="padding: 5px 10px; font-size: 10px;" class="align-middle ps-0">${item.customerName}</td>
                    <td style="padding: 5px 10px; font-size: 10px;" class="align-middle white-space-nowrap ps-0">${item.shortName}</td>
                    <td style="padding: 5px 10px; font-size: 10px;" class="align-middle ps-0">${item.customerAddress}</td>
                    <td style="padding: 5px 10px; font-size: 10px;" class="align-middle text-middle white-space-nowrap ps-0">${item.openingBalance && item.openingBalance !== 0 ? item.openingBalance.toFixed(2) : ""}</td>
                </tr>
            `);
        });
    } else {
        tableBody.html('<tr><td colspan="7" class="text-center">No data available</td></tr>');
        $("#salesCustomer-paginationInfo").text('');
        $("#salesCustomer-paginationLinks").empty();
    }

    const paginationInfo = response.paginationInfo;
    $("#salesCustomer-paginationInfo").text(`Showing ${paginationInfo.startItem} to ${paginationInfo.endItem} Items of ${paginationInfo.totalItems}`);
    $("#salesCustomer-paginationLinks").text(`(${paginationInfo.totalItems})`);

    updatePagination(paginationInfo.pageNumber, paginationInfo.currentPage, paginationInfo.totalPages);
    totalPages = paginationInfo.totalPages;
}

function updatePagination(pageNumbers, currentPage, totalPages) {
    const paginationLinks = $("#salesCustomer-paginationLinks");
    paginationLinks.empty();
    const windowSize = 1;

    const createPageButton = (page) => {
        return $('<li>')
            .addClass('page-item')
            .toggleClass('active', page === currentPage)
            .append(
                $('<button>')
                    .addClass('page-link')
                    .text(page)
                    .on('click', () => goToPage(page))
            );
    };

    const addEllipsis = () => {
        return $('<li>')
            .addClass('page-item disabled')
            .append(
                $('<span>')
                    .addClass('page-link')
                    .text('...')
            );
    };

    if (currentPage > windowSize + 1) {
        paginationLinks.append(createPageButton(1), addEllipsis());
    }

    // Pages around current page
    const startPage = Math.max(1, currentPage - windowSize);
    const endPage = Math.min(totalPages, currentPage + windowSize);
    for (let i = startPage; i <= endPage; i++) {
        paginationLinks.append(createPageButton(i));
    }

    if (currentPage < totalPages - windowSize) {
        paginationLinks.append(addEllipsis(), createPageButton(totalPages));
    }

    $("#salesCustomer-prevPageBtn").prop('disabled', currentPage === 1);
    $("#salesCustomer-nextPageBtn").prop('disabled', currentPage === totalPages);
}

function goToPage(page) {
    currentPage = page;
    loadTableData();
}

function setupActionButtons() {
    $(".customer-row").on('click', function () {
        const customerId = $(this).data('id');
        if (!customerId) return;

        $.ajax({
            url: `/SalesCustomer/Details/${customerId}`,
            method: 'GET',
            success: populateCustomerForm,
            error: () => toastr.warning("Failed to fetch customer details.")
        });
    });
}

function populateCustomerForm(response) {
    editId = response.customerId;
    console.log(response);
    showFooter();
    //debugger;
    $.each(response, (key, value) => {
        const formattedKey = key.charAt(0).toUpperCase() + key.slice(1);
        const $field = $(`#${formattedKey}`);

        if ($field.length) {
            $field.val(value);
            if ($field.is('select')) {
                $field.trigger('change');
            }
        } else {
            console.warn(`No matching field for key: ${key}`);
        }
    });
    if (response.openingDate) {
        const date = new Date(response.openingDate);
        const formattedDate = date.toISOString().split('T')[0];
        $("#OpeningDate").val(formattedDate);
    }

    $("#salesCustomer-entryDate").text(`Entry Date: ${formatDate(response.lDate)}`);
    $("#salesCustomer-lastUpdateDate").text(`Last Updated: ${formatDate(response.modifyDate)}`);


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
        toastr.error('Please Enter Customer.');
        $('#customerName').focus();
        return;
    }

    const address = $('#CustomerAddress').val().trim();
    if (!address) {
        toastr.error('Please Enter Address.');
        $('#address').focus();
        return;
    }

    const selectedIds = [];
    $('.contact-person-checkbox:checked').each(function () {
        selectedIds.push($(this).val());
    });
    $('#selectedContactPersonIds').val(selectedIds.join(','));


    const formData = new FormData($('#customerForm')[0]);
    for (let pair of formData.entries()) {
        console.log(pair[0] + ': ' + pair[1]);
    }

    const url = editId ? `/SalesCustomer/Edit/${editId}` : '/SalesCustomer/Create';
    const method = editId ? 'PUT' : 'POST';

    $.ajax({
        url,
        type: method,
        data: formData,
        contentType: false,
        processData: false,
        success: function (response) {
            toastr.success(editId ? 'Data Updated Successfully' : 'Data Saved Successfully');
            editId = null;
            resetCustomerForm();
            loadTableData();
            loadCustomerDD();
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
        toastr.error('Error saving customer');
    }
    console.error("Error saving customer:", error);
}

function confirmDeleteCustomer() {
    if (!editId) {
        toastr.warning('Please select a customer.');
        return;
    }

    Swal.fire({
        title: "Are you sure?",
        text: "Once deleted, you won't be able to recover this customer!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Delete",
        cancelButtonText: "Cancel",
        confirmButtonColor: "#d33",
    }).then((result) => {
        if (result.isConfirmed) {
            deleteCustomer(editId);
            editId = null;
            toastr.success("Data deleted successfully!");
            showFooter();
        }
    });
}

function deleteCustomer(customerId) {
    $.ajax({
        url: `/SalesCustomer/Delete/${customerId}`,
        type: 'DELETE',
        contentType: 'application/json',
        success: function () {
            resetCustomerForm();
            loadTableData();
            loadCustomerDD();
        },
        error: function (xhr, status, error) {
            console.error("Error deleting customer:", error);
            toastr.error("Failed to delete customer. Please try again.");
        }
    });
}

function bulkDeleteCustomers() {
    const selectedIds = [];

    $('.salesCustomer-selectItem:checked').each(function () {
        selectedIds.push($(this).data('id'));
    });

    if (selectedIds.length === 0) {
        Swal.fire('No selection', 'Please select at least one item to delete.', 'info');
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete ${selectedIds.length} customer(s). This action cannot be undone.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete them!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/SalesCustomer/BulkDelete',
                method: 'POST',
                data: { ids: selectedIds },
                traditional: true,
                success: function (response) {
                    if (response.isSuccess) {
                        Swal.fire('Deleted!', response.message, 'success');
                        resetCustomerForm();
                        loadTableData();
                        loadCustomerDD();
                    } else {
                        Swal.fire('Failed!', response.message, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error!', 'An error occurred while deleting.', 'error');
                }
            });
        }
    });
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

    $('#lastEntryDate').val('Entry Date: --');
    $('#lastUpdateDate').val('Last Date: --');


    $('#salesCustomer-check-all').prop('checked', false);
    $('.salesCustomer-selectItem').prop('checked', false);
    $("#salesCustomer-searchInput").val('');

    editId = null;
    loadCUSId();
    toggleBulkActions();
    showFooter();
    loadTableData();

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



//function getSelectedCustomerIds() {
//    const isSelectAllChecked = $("#selectAll").prop('checked');
//    if (isSelectAllChecked) return [];

//    const selectedIds = [];
//    $(".customer-checkbox:checked").each(function () {
//        selectedIds.push($(this).val());
//    });

//    return selectedIds;
//}




//Contact Person Dropdown Start

$(document).ready(function () {
    loadContactPersonDropdown();

    var dropdownElement = document.getElementById('dropdownButton');
    var bootstrapDropdown = new bootstrap.Dropdown(dropdownElement, {
        autoClose: true,
        popperConfig: {
            placement: 'top-start'
        }
    });

    $('#dropdownButton').on('click', function (e) {
        e.stopPropagation();
        bootstrapDropdown.toggle();
    });

    $(document).on('click', function (event) {
        const $dropdown = $('#contactPersonDropdown');
        const $button = $('#dropdownButton');

        if (!$dropdown.is(event.target) && $dropdown.has(event.target).length === 0 &&
            !$button.is(event.target)) {
            bootstrapDropdown.hide();
        }
    });

    // Search/filter inside dropdown
    $('#contactSearch').on('input', function () {
        var value = $(this).val().toLowerCase();
        $('#contactPersonList tr').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Checkbox change event
    $(document).on('change', '.contact-person-checkbox', function () {
        updateSelectedContacts();
    });

    window.addEventListener('contactPersonChanged', function (e) {
        console.log("Contact Person changed, reloading dropdown:", e.detail);
        loadContactPersonDropdown();
    });

    $('#selectAllContacts').on('change', function () {
        $('.contact-person-checkbox').prop('checked', this.checked);
        updateSelectedContacts();
    });

    $(document).on('change', '.contact-person-checkbox', function () {
        const allChecked = $('.contact-person-checkbox:checked').length === $('.contact-person-checkbox').length;
        $('#selectAllContacts').prop('checked', allChecked);
        updateSelectedContacts();
    });

});

function loadContactPersonDropdown() {
    $.ajax({
        url: '/SalesCustomer/GetAllContactPerson',
        type: 'GET',
        dataType: 'json',
        success: function (persons) {
            console.log("Contact Person Data",persons);
            const $list = $('#contactPersonList');
            $list.empty();

            if (persons.length > 0) {
                $.each(persons, function (_, person) {
                    const $row = $('<tr>');
                    $row.append(
                        $('<td>').css('text-align', 'center').append(
                            $('<input>').attr({
                                type: 'checkbox',
                                class: 'contact-person-checkbox',
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
        },
        error: function (xhr) {
            console.error("Error fetching contact persons:", xhr.responseText);
            $('#contactPersonList').html('<tr><td colspan="5" class="text-center text-danger">Error loading contacts</td></tr>');
        }
    });
}

// Update selected contacts
function updateSelectedContacts() {
    const selectedContacts = [];
    const selectedIds = [];

    $('.contact-person-checkbox:checked').each(function () {
        selectedContacts.push($(this).data('name'));
        selectedIds.push($(this).val());
    });

    $('#selectedContacts').html(
        selectedContacts.map(name => `<span class="selected-contact">${name}</span>`).join('')
    );

    $('#selectedContactPersonIds').val(selectedIds.join(','));
    console.log("Selected Contact IDs:", selectedIds.join(','));
}
//End of Contact Person

//Live Phone and Email Live Validation
$(document).on("input", "#Phone, #Email", function () {
    const $this = $(this);
    const id = $this.attr("id");
    const value = $this.val().trim();

    // Remove previous errors
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


//function loadCustomerList() {
//    $.ajax({
//        url: '/SalesCustomer/GetForCustomerList',
//        type: 'GET',
//        dataType: 'json',
//        success: function (response) {
//            const $customerList = $("#customerList");
//            if (!$customerList.length) return;

//            $customerList.empty();

//            if (response.data && response.data.length > 0) {
//                $.each(response.data, (_, customer) => {
//                    $('<li>')
//                        .append(
//                            $('<label>')
//                                .addClass('dropdown-item')
//                                .append(
//                                    $('<input>')
//                                        .attr({
//                                            type: 'checkbox',
//                                            class: 'customer-checkbox',
//                                            value: customer.customerId
//                                        })
//                                )
//                                .append(` ${customer.customerName}`)
//                        )
//                        .appendTo($customerList);
//                });

//                $(".customer-checkbox").prop('checked', true);
//                $("#selectAll").prop('checked', true);
//            } else {
//                $('<li>')
//                    .append(
//                        $('<span>')
//                            .addClass('dropdown-item')
//                            .text('No customers available')
//                    )
//                    .appendTo($customerList);
//            }
//        },
//        error: function () {
//            $("#customerList").html('');
//            $('<li>')
//                .append(
//                    $('<span>')
//                        .addClass('dropdown-item text-danger')
//                        .text('Error loading customers')
//                )
//                .appendTo($("#customerList"));
//        }
//    });
//}

//#endregion