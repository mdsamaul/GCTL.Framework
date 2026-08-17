let currentContactPage = 1;
let ContactpageSize = 10;
let ContactcurrentSortColumn = "CPID";
let currentSortDirection = "desc";
let searchTerm = "";
let totalPages = 0;
console.log("Contact Person Page Js Worked");

//Load Next CPID
function loadNextCPID() {
    $.ajax({
        url: '/next-CPID-id',
        method: 'GET',
        success: function (response) {
            console.log("Next CPID:", response);
            $('#cpid').val(response);
        },
        error: function (xhr, status, error) {
            console.error("Failed to load next CPID:", error);
        }
    });
}

// Load Designation into the dropdown
function LoadDesignationDropdown() {
    $.ajax({
        url: '/designation-dropdown',
        method: 'GET',
        success: function (designations) {
            console.log("Designation Dropdown Worked", designations);
            var $designation = $('#designationCode');
            $designation.empty().append('<option value=""></option>');

            $.each(designations, function (index, designation) {
                $designation.append(
                    '<option value="' + designation.designationCode + '">' + designation.designationName + '</option>'
                );
            });

            $designation.select2({
                placeholder: 'Designation',
                //escapeMarkup: function (markup) { return markup; },
                width: '100%',
                dropdownParent: $('#contactPersonModal')
            });
        },
        error: function (xhr, status, error) {
            console.error('Failed to load Designation:', error);
        }
    });
}

//Load currencies into the dropdown
function LoadCompanyDropdown() {
    $.ajax({
        url: '/core-company-dropdown',
        method: 'GET',
        success: function (companies) {
            console.log("Company Dropdown Worked");
            var $company = $('#companyCode');
            $company.empty().append('<option value=""></option>');

            $.each(companies, function (index, company) {
                $company.append(
                    '<option value="' + company.companyCode + '">' + company.companyName + '</option>'
                );
            });

            // Initialize Select2 to make it searchable
            $company.select2({
                placeholder: "Company",
                width: '100%',
                dropdownParent: $('#contactPersonModal')
            });
        },
        error: function (xhr, status, error) {
            console.error('Failed to load currencies:', error);
        }
    });
}

//Start Validation
function validateCompanyForm() {
    let isValid = true;

   
    $("#contactPersonName, #contactPersonMobile").removeClass("error-border");

    // Contact Person Name Checking Empty
    const contactpersonName = $("#contactPersonName").val().trim();
    if (!contactpersonName) {
        toastr.error("Please Enter Contact Person.");
        $("#contactPersonName").addClass("error-border").focus();
        return false;
    }

    // Empty Mobile Cheking
    const mobile = $("#contactPersonMobile").val().trim();
    if (!mobile) {
        toastr.error("Please Enter Mobile Number.");
        $("#contactPersonMobile").addClass("error-border").focus();
        return false;
    }

    return isValid;
}

// Live validation for Mobile and Email
$(document).on("input", "#contactPersonMobile, #contactPersonEmail", function () {
    const id = $(this).attr("id");

    // Mobile Validation
    if (id === "contactPersonMobile") {
        const mobile = $(this).val().trim();
        $(this).removeClass("error-border");
        $("#mobileError").remove();


        if (!/^\d+$/.test(mobile)) {
            $(this).addClass("error-border");
            $(this).after('<span id="mobileError" class="text-danger">Mobile Number Must Be Number.</span>');
            return;
        }
    }

    // Email Validation
    if (id === "contactPersonEmail") {
        const email = $(this).val().trim();
        $(this).removeClass("error-border");
        $("#emailError").remove();

        if (!email) return;

        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(email)) {
            $(this).addClass("error-border");
            $(this).after('<span id="emailError" class="text-danger">Please Enter Valid Email.</span>');
        }
    }
});
//End of Validation

//Save Or Update Contact Person

async function saveOrUpdateContactPerson() {
    console.log("SaveOrUpdate ContactPerson function Worked");

    //Call Validate Function
    if (!validateCompanyForm()) return;

    const contactData = {
        AutoId: parseInt($("#autoId").val()) || 0,
        Cpid: $("#cpid").val() || "",
        ContactPersonName: ($("#contactPersonName").val() || "").trim(),
        DesignationCode: ($("#designationCode").val() || "").trim(),
        ContactPersonMobile: ($("#contactPersonMobile").val() || "").trim(),
        ContactPersonEmail: ($("#contactPersonEmail").val() || "").trim(),
        CompanyCode: ($("#companyCode").val() || "").trim(),
        EmployeeId: ($("#employeeID").val() || "").trim()
    };


    console.log("Contact Data:", contactData);

    try {
        //Duplicate check
        //const dupResponse = await $.ajax({
        //    url: '/contact/check-duplicate',
        //    type: 'POST',
        //    contentType: 'application/json',
        //    data: JSON.stringify(contactData)
        //});

        //if (dupResponse.isDuplicate) {
        //    toastr.error(dupResponse.message || "Data already exists!");
        //    return;
        //}

        //Save or Update
        const isUpdate = contactData.AutoId > 0;
        const saveResponse = await $.ajax({
            url: isUpdate ? `/contact-person/${contactData.AutoId}` : '/contact-person',
            type: isUpdate ? 'PUT' : 'POST',
            contentType: 'application/json',
            data: JSON.stringify(contactData)
        });
        console.log("Contact response:", contactData);
        //if (saveResponse.success) {

        //    clearContactForm();
        //    LoadDesignationDropdown();
        //    LoadCompanyDropdown();
        //    LoadAllContactPerson(currentContactPage, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
        //    toastr.success(isUpdate ? "Data Updated Successfully" : "Data Saved Successfully");
        //} else {
        //    toastr.error(saveResponse.message || (isUpdate ? "Update Failed" : "Insertion Failed"));
        //}
        if (saveResponse.success) {
                clearContactForm();
                LoadDesignationDropdown();
                LoadCompanyDropdown();
                LoadAllContactPerson(currentContactPage, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
            toastr.success(isUpdate ? "Data Updated Successfully" : "Data Saved Successfully");

            //Trigger event for main page dropdown refresh
            const event = new CustomEvent('contactPersonChanged', {
                detail: { message: 'Contact person updated' }
            });
            window.dispatchEvent(event);        } else {
            toastr.error(saveResponse.message || (isUpdate ? "Update Failed" : "Insertion Failed"));
        }

    } catch (err) {
        console.error(err);
        toastr.error(contactData.AutoId ? "Error updating company" : "Error saving company");
    }
}

//Loading All Contact Person Data
function LoadAllContactPerson(page = 1, pageSizeVal = ContactpageSize, sortColumn = ContactcurrentSortColumn, sortDirection = currentSortDirection, search = searchTerm) {
    currentContactPage = page;
    ContactpageSize = pageSizeVal;
    ContactcurrentSortColumn = sortColumn;
    currentSortDirection = sortDirection;
    searchTerm = search;

    $.ajax({
        url: '/contact-person-list',
        type: 'GET',
        data: {
            pageNumber: currentContactPage,
            pageSize: pageSizeVal,
            searchTerm: searchTerm,
            sortColumn: ContactcurrentSortColumn,
            sortOrder: currentSortDirection
        },
        success: function (response) {
            console.log("Contact Person Response:", response);

            let rows = '';
            if (!response.data || response.data.length === 0) {
                rows = `<tr class="no-data">
                            <td colspan="6" class="text-center">No Data Available</td>
                        </tr>`;
            } else {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center align-middle">
                            <input type="checkbox" class="row-checkbox" data-id="${item.autoId}">
                        </td>
                        <td class="text-center align-middle">
                          <button type="button" class="btn btn-link edit-contact px-0" data-id="${item.autoId}">
                            ${item.cpid}
                           </button>

                        </td>
                        <td class="align-middle">${item.contactPersonName || ''}</td>
                        <td class="align-middle">${item.designationName || ''}</td>
                        <td class="align-middle">${item.contactPersonMobile || ''}</td>
                        <td class="align-middle">${item.contactPersonEmail || ''}</td>
                    </tr>`;
                });
            }

            $('#tblBody').html(rows);

            // Pagination info
            totalPages = Math.ceil((response.paginationInfo?.totalItems || 0) / ContactpageSize);
            $('#paginationInfo').text(`Page ${currentContactPage} of ${totalPages}`);
            $('#paginationSummary').text(`Showing ${response.paginationInfo?.startItem || 0} to ${response.paginationInfo?.endItem || 0} of ${response.paginationInfo?.totalItems || 0} entries`);
            $('#totalRecordsInfo').text(`Total Records: ${response.paginationInfo?.totalItems || 0}`);

            generateCompanyPageButtons(currentContactPage, totalPages);
            $('#selectAll').prop('checked', false);
            updateCheckboxState();
            updateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error loading company data: ", error);
            $('#tblBody').html(`<tr class="no-data"><td colspan="6" class="text-center">Failed to load data</td></tr>`);
        }
    });
}

//Generating Pagination Page Number Button
function generateCompanyPageButtons(currentContactPage, totalPages) {
    const navigationDiv = $('#pageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => LoadAllContactPerson(page, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, $('#searchInput').val().trim()));

        if (page === currentContactPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    if (totalPages === 0) return;

    navigationDiv.append(createButton(1));

    if (currentContactPage > 3) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (currentContactPage !== 1 && currentContactPage !== totalPages) {
        navigationDiv.append(createButton(currentContactPage));
    }

    if (currentContactPage < totalPages - 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

//Checkbox
function updateCheckboxState() {
    const $allCheckboxes = $('.row-checkbox');
    const $selectAll = $('#selectAll');

    if ($allCheckboxes.length === 0) {
        $selectAll.prop('checked', false);
        $selectAll.prop('indeterminate', false);
        return;
    }

    const checkedCount = $allCheckboxes.filter(':checked').length;

    if (checkedCount === 0) {
        $selectAll.prop('checked', false);
        $selectAll.prop('indeterminate', false);
    } else if (checkedCount === $allCheckboxes.length) {
        $selectAll.prop('checked', true);
        $selectAll.prop('indeterminate', false);
    } else {
        $selectAll.prop('checked', false);
        $selectAll.prop('indeterminate', true);
    }
}

// Call this when user clicks the "Select All" checkbox
$(document).on('change', '#selectAll', function () {
    const isChecked = $(this).is(':checked');
    $('.row-checkbox').prop('checked', isChecked);
});

// Update sort indicators on table headers
function updateSortIndicators() {
    $('.sortable').each(function () {
        const column = $(this).data('column');
        const $icon = $(this).find('.sort-icon');

        $icon.removeClass('fa-sort-up fa-sort-down').addClass('fa-sort'); 

        if (column === ContactcurrentSortColumn) {
            if (currentSortDirection === 'asc') {
                $icon.removeClass('fa-sort').addClass('fa-sort-up');
            } else if (currentSortDirection === 'desc') {
                $icon.removeClass('fa-sort').addClass('fa-sort-down');
            }
        }
    });
}

// Change sort direction when header is clicked
$(document).on('click', '.sortable', function () {
    const column = $(this).data('column');

    if (ContactcurrentSortColumn === column) {
        currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        ContactcurrentSortColumn = column;
        currentSortDirection = 'asc';
    }

    LoadAllContactPerson(currentContactPage, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, $('#searchInput').val().trim());
});

//Populate Data 
function populateContactForm(data) {
    $('#autoId').val(data.autoId || 0);
    $('#cpid').val(data.cpid || "");
    $('#contactPersonName').val(data.contactPersonName || "");
    $('#designationCode').val(data.designationCode || "").trigger("change");
    $('#contactPersonMobile').val(data.contactPersonMobile || "");
    $('#contactPersonEmail').val(data.contactPersonEmail || "");
    $('#companyCode').val(data.companyCode || "").trigger("change");
    $('#employeeID').val(data.employeeId || "");

    if (data.ldate) {
        console.log("Contact Person LDate:", data.lDate);
        const lDate = new Date(data.ldate).toLocaleDateString('en-GB');
        $('#displayLDate').text(lDate);
        $('#lDateContainer').show();
    } else {
        $('#lDateContainer').hide();
    }

    if (data.modifyDate) {
        const modifyDate = new Date(data.modifyDate).toLocaleDateString('en-GB');
        $('#displayModifyDate').text(modifyDate);
        $('#modifyDateContainer').show();
    } else {
        $('#modifyDateContainer').hide();
    }
}


//Populate data with clicked edit button

$(document).on('click', '.edit-contact', function () {
    const id = $(this).data('id');

    $.ajax({
        url: `/contact-person-details?id=${id}`,
        type: 'GET',
        success: function (data) {
            console.log("Fetched Contact person details:", data);
            populateContactForm(data);
        },
        error: function (err) {
            console.error('Failed to fetch Contact person details:', err);
        }
    });
});

//Reset form function
function clearContactForm() {
    $("#contactForm")[0].reset();
    $("#autoId").val(0);
    $("#designationCode, #employeeID, #companyCode").val("").trigger("change");
    loadNextCPID();
    $('#lDateContainer').hide();
    $('#modifyDateContainer').hide();
    $(".row-checkbox").prop("checked", false);
    $("#selectAll").prop("checked", false);
    $('#searchInput').val('');

    $('#contactPersonMobile, #contactPersonEmail').removeClass('error-border');
    $('#mobileError, #emailError').remove();

    LoadAllContactPerson(1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, '');
}


//Select/Deselect all checkboxes
$(document).on('change', '#selectAll', function () {
    const isChecked = $(this).is(':checked');
    $('.row-checkbox').prop('checked', isChecked);
});

//Delete Single Company and Bulk Delete

$(document).on("click", "#deleteBtn", function () {
    const id = $("#autoId").val() ? parseFloat($("#autoId").val()) : 0.0;
    const selectedIds = $(".row-checkbox:checked")
        .map(function () { return parseInt($(this).data("id")); })
        .get();

    if (id && id > 0) {
        //Single Delete 

        $.ajax({
            url: `/single-contact-person/${id}`,
            type: 'DELETE',
            success: function (response) {
                toastr.success(response.message || "Data Deleted Successfully");
                clearContactForm();
                LoadAllContactPerson();
                if (response.isSuccess) {
                    console.log("Single event called");
                    const event = new CustomEvent('contactPersonChanged', {
                        detail: { message: 'Contact person deleted' }
                    });
                    window.dispatchEvent(event);
                }
            //    loadNextCPID();
            },
            error: function () {
                toastr.error("Error deleting contact");
            }
        });

    }
    else if (selectedIds.length > 0) {

        $.ajax({
            url: '/contact-person-list',
            type: 'DELETE',
            contentType: 'application/json',
            data: JSON.stringify(selectedIds),
            success: function (response) {
                if (response.isSuccess) {
                    toastr.success(response.message);
                    clearContactForm();
                    LoadAllContactPerson();
                    $("#selectAll").prop("checked", false);
                    //Custom event for 
                    const event = new CustomEvent('contactPersonChanged', {
                        detail: { message: 'Contact person(s) deleted' }
                    });
                    window.dispatchEvent(event);
                } else {
                    toastr.error(response.message || "Delete Failed");
                }
            },
            error: function () {
                toastr.error("Error deleting companies");
            }
        });

    } else {
        toastr.warning("Please select a data to delete");
    }
});

// Search on typing
$(document).on("input", "#searchInput", function () {
    const searchValue = $(this).val().trim();
    LoadAllContactPerson(1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchValue);
});


$(document).ready(function () {
    LoadAllContactPerson();
    loadNextCPID();
    LoadDesignationDropdown();
    LoadCompanyDropdown();

    $(document).on("submit", "#contactForm", function (e) {
        e.preventDefault();
        saveOrUpdateContactPerson(e);
    });

    $(document).on("click", ".resetBtn", function () {
        console.log("Clear Button Clicked");
        clearContactForm();
    });

    $('#firstPage').off('click').on('click', function () {
        if (currentContactPage > 1) LoadAllContactPerson(1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
    });

    $('#prevPage').off('click').on('click', function () {
        if (currentContactPage > 1) LoadAllContactPerson(currentContactPage - 1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
    });

    $('#nextPage').off('click').on('click', function () {
        if (currentContactPage < totalPages) LoadAllContactPerson(currentContactPage + 1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
    });

    $('#lastPage').off('click').on('click', function () {
        if (currentContactPage < totalPages) LoadAllContactPerson(totalPages, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, searchTerm);
    });

    $('#pageSize').on('change', function () {
        let newSize = parseInt($(this).val());
        ContactpageSize = newSize;
        LoadAllContactPerson(1, ContactpageSize, ContactcurrentSortColumn, currentSortDirection, $('#searchInput').val().trim());
    });

});
