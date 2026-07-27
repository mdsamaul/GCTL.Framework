$(() => {

    loadTable();
    $('#LdateModifyHide').hide();
    $('#sectionBreak').hide();
    loadContactPersonsTable();

    //#region formatDateTime

    function formatDateTime(dateStr) {
        var date = new Date(dateStr);
        return date.toLocaleString('en-GB', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
    }

    //#endregion

    //#region GetById
    $(document).on('click', '.js-BranchAddress-edit', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        console.log(id);
        $.ajax({
            url: '/BranchAddress/GetById',
            type: 'GET',
            data: { code: id },
            success: function (data) {
                if (data) {
                    $('#Tc').val(data.tc);
                    $('#Id').val(data.id);
                    $('#VendorBranchId').val(data.vendorBranchId);
                    $('#VendorId').val(data.vendorId).trigger('change');
                    window.reinitCustomDropdownById('VendorId');
                    $('#LocationAddress').val(data.locationAddress);
                    $('#Name').val(data.name);
                    $('#Address').val(data.address);
                    $('#CountryId').val(data.countryId).trigger('change');
                    window.reinitCustomDropdownById('CountryId');
                    $('#City').val(data.city);
                    $('#StateOrProvince').val(data.stateOrProvince);
                    $('#ZipCode').val(data.zipCode);
                    $('#Phone').val(data.phone);
                    $('#Email').val(data.email);
                    $('#Remarks').val(data.remarks);

                    loadContactPersonsTableForEdit(data.contactPerson);

                    $('#LdateModifyHide').show();
                    $('#sectionBreak').show();

                    if (data.ldate) {
                        $('#creationDateSpan').text(formatDateTime(data.ldate));
                    } else {
                        $('#creationDateSpan').text('');
                    }

                    if (data.modifyDate) {
                        $('#modifyDateSpan').text(formatDateTime(data.modifyDate));
                    } else {
                        $('#modifyDateSpan').text('');
                    }
                }
            },
            error: function () {
                alert('Failed to load vendor information data.');
            }
        });
    });

    //#endregion

    //#region Save

    $("body").on("click", settings.saveSelector, function () {
        validation();
        var $valid = $(settings.formSelector).valid();
        if (!$valid) {
            return false;
        }

        var data;
        if (settings.haseFile)
            data = new FormData($(settings.formSelector)[0]);
        else
            data = $(settings.formSelector).serialize();

        var url = $(settings.formSelector).attr("action");

        var options = {
            url: url,
            method: "POST",
            data: data,
            success: function (response) {
                if (response.isSuccess) {
                    loadForm(saveUrl)
                        .then((data) => {
                            loadTable();
                            ResetBranchAddressForm();
                            $(settings.lastCodeSelector).val(response.lastCode);
                        })
                        .catch((error) => {
                            console.log(error)
                        })

                    toastr.success(response.message);
                }
                else {
                    toastr.error(response.message);
                    console.log(response);
                }
            }
        }
        if (settings.haseFile) {
            options.processData = false;
            options.contentType = false;
        }
        $.ajax(options);
    });

    //#endregion

    //#region selectAllSelector & deleteSelector

    $("body").on("click", settings.selectAllSelector, function () {
        $(".checkBox").prop('checked', $(this).prop('checked'));
    });

    $("body").on("click", settings.deleteSelector, function (e) {
        e.preventDefault();

        if ($(this).data('id')) {
            selectedItems.push($(this).data('id'));
        } else {
            $('input:checkbox.checkBox').each(function () {
                if ($(this).prop('checked')) {
                    if (!selectedItems.includes($(this).val())) {
                        selectedItems.push($(this).val());
                    }
                }
            });
        }

        if (selectedItems.length > 0) {
            $(settings.deleteModal).modal("show");
        } else {
            toastr.info("Please select at least one item.");
        }
    });

    //#endregion

    //#region Delete & deleteModal

    $("body").on('show.bs.modal', settings.deleteModal, function (event) {

        var source = $(event.relatedTarget);
        var id = source.data("ids");
        var title = source.data("title");
        title = "Are you sure to delete?";
        var modal = $(this);
        $(modal).find('.title').html(title);

        $("body").on("click", settings.finalDeleteSelector, function (e) {
            e.stopPropagation();
            e.preventDefault();
            e.stopImmediatePropagation();

            $.ajax({
                url: deleteUrl,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(selectedItems),
                success: function (response) {
                    console.log(response);
                    $(modal).modal("hide");
                    if (response.success) {
                        toastr.success(response.message);
                        selectedItems = [];

                        loadTable();
                        ResetBranchAddressForm();
                        loadForm(saveUrl);
                    }
                    else {
                        toastr.error(response.message);
                        console.log(response);
                    }
                }
            });
        });

    }).on('hide.bs.modal', function () {
        $("body").off("click", settings.finalDeleteSelector);
    });

    //#endregion

    //#region topSelector & decimalSelector

    $("body").on("click", settings.topSelector, function (e) {
        e.preventDefault();
        $("html, body").animate({ scrollTop: 0 }, 500);
    });

    $("body").on("keyup", settings.decimalSelector, function () {
        var self = $(this);
        showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
    });

    //#endregion

    //#region availabilitySelector

    $("body").on("keyup", settings.availabilitySelector, function () {
        var self = $(this);
        let code = $(".js-VendorBranchId-code").val();
        let name = self.val();
        $.ajax({
            url: settings.baseUrl + "/CheckAvailability",
            method: "POST",
            data: { code: code, name: name },
            success: function (response) {
                console.log(response);
                if (response.isSuccess) {
                    toastr.error(response.message);
                }
            }
        });
    });

    //#endregion

});

//#region loadTable

function loadTable() {
    $.gctlGrid.initFromModel({
        gridId: 'BranchAddress-grid'
    })
}

//#endregion

//#region loadForm

function loadForm(url) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: url,
            type: 'GET',
            cache: false,
            success: function (data) {
                console.log(data);
                $(settings.formContainer).empty();
                $(settings.formContainer).html(data);
                $.validator.unobtrusive.parse($(settings.formSelector));

                window.initCustomDropdowns(settings.formContainer, true);
                loadContactPersonsTable();

                resolve(data)
            },
            error: function (error) {
                reject(error)
            },
        })
    })
}

//#endregion

//#region Contact Person Table Dropdown

function loadContactPersonsTable() {
    $.ajax({
        url: '/BranchAddress/GetContactPersonsForTable',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                var contactItems = response.data.map(function (c) {
                    return {
                        id: c.id,
                        text: c.name,
                        designation: c.designation,
                        phone: c.phone,
                        email: c.email
                    };
                });

                setSelect(
                    '#ContactPerson',
                    [],
                    false,
                    contactItems,
                    true,
                    'Select Contact Person(s)'
                );
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error('Error loading contact persons');
        }
    });
}

function loadContactPersonsTableForEdit(selectedContactPerson) {
    $.ajax({
        url: '/BranchAddress/GetContactPersonsForTable',
        type: 'GET',
        success: function (response) {
            if (response.success) {
                var contactItems = response.data.map(function (c) {
                    return {
                        id: c.id,
                        text: c.name,
                        designation: c.designation,
                        phone: c.phone,
                        email: c.email
                    };
                });

                var selectedValues = [];
                if (selectedContactPerson) {
                    selectedValues = Array.isArray(selectedContactPerson)
                        ? selectedContactPerson
                        : selectedContactPerson.toString().split(',').map(x => x.trim()).filter(x => x);
                }

                setSelect(
                    '#ContactPerson',
                    selectedValues,
                    false,
                    contactItems,
                    true,
                    'Select Contact Person(s)'
                );
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error('Error loading contact persons');
        }
    });
}

//#endregion

//#region ResetBranchAddressForm

$(document).on('click', '.js-BranchAddress-clear', function () {
    ResetBranchAddressForm();
    loadTable();
});

function ResetBranchAddressForm() {
    $('#Tc').val(0);
    $('#Id').val('');
    $('#VendorId').val('');

    $('#VendorCode').val('');
    $('#LocationAddress').val('');
    $('#Address').val('');
    $('#City').val('');
    $('#StateOrProvince').val('');
    $('#ZipCode').val('');
    $('#Phone').val('');
    $('#Fax').val('');
    $('#Email').val('');
    $('#Remarks').val('');

    setSelect('#VendorId', '', false);
    setSelect('#CountryId', '', false);
    loadContactPersonsTable();

    $('#LdateModifyHide').hide();

    selectedItems?.clear?.();

    $("#BranchAddress-check-all").prop("checked", false);
    $('#BranchAddress-grid input[type="checkbox"]').prop('checked', false);
}

//#endregion

//  #region validation

function validation() {

    var code = $('#VendorId').val();
    var sty = $('#LocationAddress').val();

    if (!code) {
        toastr.info('select Vendor Name');
        $('#VendorId').select2('open');
        return false;
    }

    if (!sty) {
        toastr.info('Enter Address');
        $('#LocationAddress').trigger('focus');
        return false;
    }

    return true;
}

//#endregion