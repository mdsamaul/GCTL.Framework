let currentPageDL = 1;
let pageSizeDL = 10;
let totalPageDL = 0;
let editdlId;
let selectedCustomer;

let currentSortColumnDL = 'DeliveryLocationCode';
let currentSortOrderDL = 'desc';
console.log("Delivery Page Worked");

$(document).ready(() => {
    loadDeliveryLocationsPaginated();
    loadCustomerDD();
    setupDLEventListeners();
    loadDLId();
    showFooterDl();
    updatePaginationDl();
    updateSortingIndicatorDl();
    setupDLActionButtons();
    
    LoadCountryDropdownDL();

    $("#CustomerIdForDelivery").change(function () {
        var customerId = $(this).val();
        selectedCustomer = customerId || ""; 
        currentPageDL = 1; 
        loadDeliveryLocationsPaginated(selectedCustomer);
    }); 
})

function setupDLEventListeners() {


    $('#pageSizeDropdown').on('change', function () {
        var selectedSize = $(this).val(); 
        if (!selectedSize) return;

        pageSizeDL = parseInt(selectedSize, 10);
        console.log("New pageSizeDL:", pageSizeDL);

        loadDeliveryLocationsPaginated();
    });


    $('th.sortDL').on('click', function () {
        const column = $(this).data('sort');
        $('.sortDL').removeClass('sort-asc sort-desc');

        if (currentSortColumnDL === column) {
            currentSortOrderDL = currentSortOrderDL === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortColumnDL = column;
            currentSortOrderDL = 'asc';
        }

        $(this).addClass('sort-' + currentSortOrderDL);

        loadDeliveryLocationsPaginated(selectedCustomer, currentSortColumnDL, currentSortOrderDL);
        updateSortingIndicator(column, currentSortOrderDL);
    });

    $("#btnBulkDeleteDL").on('click', bulkDeleteDeliveryLocation);

    $("#btnDeleteDL").on('click', confirmDelete);

    const debouncedSearch = debounce(() => {
        currentPageDL = 1;
        loadDeliveryLocationsPaginated(selectedCustomer);
    }, 300);

    $("#searchInput-Dl").on("input", debouncedSearch);

    $("#prevPageBtn-Dl").on('click', function () {
        if (currentPageDL > 1) {
            currentPageDL--;
            loadDeliveryLocationsPaginated(selectedCustomer);
        }
    });

    $("#nextPageBtn-Dl").on('click', function () {
        if (currentPageDL < totalPagesDL) {
            currentPageDL++;
            loadDeliveryLocationsPaginated(selectedCustomer);
        }
    });

    $('#viewBank-Dl-check-all').on('change', function () {
        $('.selectItem-Dl').prop('checked', $(this).prop('checked'));
        toggleBulkActionsDl();
    })

    $(document).on('change', '.selectItem-Dl', toggleBulkActionsDl);

    //Clear Button handler
    //$("#btnResetDL").on('click', resetDLForm);
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

function debounce(func, delay) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => {
            func.apply(this, args);
        }, delay);
    };
}

function updateSortingIndicatorDl() {
    $('th.sortDL').find('.sort-icon').remove();

    const th = $(`th.sortDL[data-sort="${currentSortColumnDL}"]`);
    const icon = currentSortOrderDL === 'asc' ? 'fa-sort-up' : 'fa-sort-down';

    th.append(`<span class="sort-icon ms-2"><i class="fas ${icon}"></i></span>`);
}

function updatePaginationDl(pageNumbers, currentPage, totalPages) {
    const paginationLinks = $("#paginationLinks-Dl");
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
                    .on('click', () => goToPageDl(page))
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

    const startPage = Math.max(1, currentPage - windowSize);
    const endPage = Math.min(totalPages, currentPage + windowSize);
    for (let i = startPage; i <= endPage; i++) {
        paginationLinks.append(createPageButton(i));
    }

    if (currentPage < totalPages - windowSize) {
        paginationLinks.append(addEllipsis(), createPageButton(totalPages));
    }

    $("#prevPageBtn-Dl").prop('disabled', currentPage === 1);
    $("#nextPageBtn-Dl").prop('disabled', currentPage === totalPages);
}

function goToPageDl(page) {
    currentPageDL = page;
    loadDeliveryLocationsPaginated(selectedCustomer);
}

function showFooterDl() {
    $("#Dlfooter").toggleClass("d-none", !editId);
}

function toggleBulkActionsDl() {
    const anyChecked = $('.selectItem-Dl:checked').length > 0;
    $('#btnBulkDeleteDL').toggleClass('d-none',!anyChecked);
    $('#btnDeleteDL').toggleClass('d-none',anyChecked);
}

function loadDeliveryLocationsPaginated(customerId = "", sortColumn = 'DeliveryLocationCode', sortOrder = 'desc') {
    //resetDLWOcusDD();
    //if (!editdlId) {
    //    resetDLWOcusDD();
    //}
    const searchTermDl = $("#searchInput-Dl").val();

    $.ajax({
        url: '/SalesDeliveryLocation/GetAllPaginated',
        method: 'GET',
        data: {
            cusId: customerId, 
            pageNumber: currentPageDL,
            pageSizeDl: pageSizeDL,
            searchTerm: searchTermDl,
            sortColumn: sortColumn,
            sortOrder: sortOrder
        },
        success: function (response) {
            console.log("Delivery Data Loaded", response);

            if (response && response.paginationInfo) {
                totalPagesDL = response.paginationInfo.totalPages; 
            }

            renderDeliveryLocationTable(response);
            setupDLActionButtons();
        },
        error: function (error) {
            console.error("Error loading data: ", error);
            $("#deliveryTableBody").html('<tr><td colspan="6" class="text-center">No Data Found</td></tr>');
        }
    });
}

function renderDeliveryLocationTable(response) {
    const tableBody = $("#deliveryTableBody");
    tableBody.empty();

    if (response.data.length > 0) {
        $("no-data").hide();
        console.log(response.data);
        $.each(response.data, (_, item) => {
            tableBody.append(`
                        <tr class="position-static delivery-row" data-id="${item.deliveryLocationCode}">
                            <td style="width:5%" class="text-center text-middle align-middle">
                                <input type="checkbox" class="selectItem-Dl" data-id="${item.deliveryLocationCode}" />
                            </td>
                            <td style="padding: 5px 10px; font-size: 10px;" class="align-middle white-space-nowrap ps-0">${item.deliveryLocationCode}</td>
                            <td style="padding: 5px 10px; font-size: 10px;" class="align-middle ps-0">${item.customerName}</td>
                            <td style="padding: 5px 10px; font-size: 10px;" class="align-middle ps-0">${item.locationAddress}</td>
                            <td style="padding: 5px 10px; font-size: 10px;" class="align-middle ps-0">${item.contactPersonName}</td>
                        </tr>
            `)
        });
    } else {
        tableBody.html('<tr><td colspan="6" class="text-center">No data available</td></tr>');
        $("#paginationInfo-Dl").text('');
        $("#paginationLinks-Dl").empty();
    }

    const paginationInfo = response.paginationInfo;
    $("#paginationInfo-Dl").text(`Showing ${paginationInfo.startItem} to ${paginationInfo.endItem} Items of ${paginationInfo.totalItems}`);
    $("#paginationLinks-Dl").text(`(${paginationInfo.totalItems})`);

    updatePaginationDl(paginationInfo.pageNumber, paginationInfo.currentPage, paginationInfo.totalPages);
    totalPages = paginationInfo.totalPages;
}

function setupDLActionButtons() {

    $(".delivery-row").find("select, input").click(e => e.stopPropagation());

    $(".delivery-row").on("click", function () {
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
            loadDeliveryLocationsPaginated(selectedCustomer);
        },
        error: function (error) {
            handleDLSaveError(error);
        }
    });
}

function handleDLSaveError(error){
    if (error.responseJSON && error.responseJSON.errors) {
        let errorMessage = [];

        $.each(error.responseJSON.error, (_, messages) => {
            $.each(message, (_, message) => {
                errorMessage.push(`<li>${message}</li>`);
            });
        });

        errorMessage += "</ul>";
        toastr.error('Validation Error:\n<ul>' + errorMessage.join('') + '</ul>');;
    } else {
        toastr.error('Error saving customer');
    }
    console.error("Error saving customer:", error);
}

function confirmDelete() {
    if (!editdlId) {
        toastr.warning("Please select a customer.");
        return;
    }

    Swal.fire({
        title: "Are you sure?",
        text: "Once deleted, you won't be able to recover this customer!",
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
            //resetDLWOcusDD();
            resetDLForm();
            loadDLId();
            loadDeliveryLocationsPaginated(selectedCustomer);
        },
        error: function (xhr, status, error) {
            console.error("Error deleting customer:", error);
            toastr.error("Failed to delete customer. Please try again.");
        }
    });
};


function bulkDeleteDeliveryLocation() {
    const selectedIds = [];

    $(".selectItem-Dl:checked").each(function () {
        selectedIds.push($(this).data('id'));
    });

    if (selectedIds.length === 0) {
        Swal.fire('No selection', 'Please select at least one item to delete', 'info');
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete ${selectedIds.length} DeliveryLocation(s) (This action cannot be undone).`,
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

                    // Reset form in "new entry" mode
                    resetDLForm({ resetCustomer: false, keepEdit: false });
                    editdlId = null;

                    // Reload table for current customer
                    loadDeliveryLocationsPaginated(selectedCustomer);
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

    toggleBulkActionsDl();
    showFooterDl();

    $('.selectItem-Dl').prop('checked', false);
    $('#viewBank-Dl-check-all').prop('checked', false);

    // Reset search
    const searchInput = $("#searchInput-Dl");
    searchInput.val('');
    if (searchInput.val() === '') {
        currentPageDL = 1;
        loadDeliveryLocationsPaginated(selectedCustomer);
    }
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


