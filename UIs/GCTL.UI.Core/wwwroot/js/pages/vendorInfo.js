(function ($) {
    $.vendorInfo = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#VendorInfo-form",
            formContainer: ".js-VendorInfo-form-container",
            gridSelector: "#VendorInfo-grid",
            gridContainer: ".js-VendorInfo-grid-container",
            editSelector: ".js-VendorInfo-edit",
            saveSelector: ".js-VendorInfo-save",
            selectAllSelector: "#VendorInfo-check-all",
            deleteSelector: ".js-VendorInfo-delete-confirm",
            deleteModal: "#VendorInfo-delete-modal",
            finalDeleteSelector: ".js-VendorInfo-delete",
            clearSelector: ".js-VendorInfo-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-VendorInfo-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-VendorInfo-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            lastCodeSelector: '#lastCode',
            load: function () {

            }
        }, options);

        var gridUrl = settings.baseUrl + "/grid";
        var saveUrl = settings.baseUrl + "/setup";
        var deleteUrl = settings.baseUrl + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        //#endregion

        $(() => {

            loadTable();
            $('#LdateModifyHide').hide();
            $('#sectionBreak').hide();

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

            $(document).on('click', '.js-VendorInfo-edit', function (e) {
                
                e.preventDefault();
                var id = $(this).data('id');
                $.ajax({
                    url: '/VendorInfo/GetById',
                    type: 'GET',
                    data: { code: id },
                    success: function (data) {
                        if (data) {
                            console.log(data)
                            $('#Tc').val(data.tc);
                            $('#ID').val(data.id);
                            $('#VendorId').val(data.vendorId);
                            $('#VendorPrefix').val(data.vendorPrefix).trigger('change');
                            window.reinitCustomDropdownById('VendorPrefix');
                            $('#VendorCode').val(data.vendorCode);
                            $('#Name').val(data.name);
                            $('#Address').val(data.address);
                            $('#CountryId').val(data.countryId).trigger('change');
                            window.reinitCustomDropdownById('CountryId');
                            $('#City').val(data.city);
                            $('#StateOrProvince').val(data.stateOrProvince);
                            $('#ZipCode').val(data.zipCode);
                            $('#VatRegNo').val(data.vatRegNo);
                            $('#Phone').val(data.phone);
                            $('#Fax').val(data.fax);
                            $('#Email').val(data.email);
                            $('#Website').val(data.website);
                            $('#Cpid').val(data.cpid).trigger('change');
                            window.reinitCustomDropdownById('Cpid');
                            $('#Remarks').val(data.remarks);

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
                debugger
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
                                    ResetVendorInfoForm();
                                    $(settings.lastCodeSelector).val(response.lastCode);
                                    // alert("The last code has been updated to: " + response.lastCode);
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
                $(".checkBox").prop('checked',
                    $(this).prop('checked'));
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

                // Extract value from data-* attributes
                var title = source.data("title");
                title = "Are you sure to delete?";
                var modal = $(this);
                $(modal).find('.title').html(title);

                $("body").on("click", settings.finalDeleteSelector, function (e) {
                    e.stopPropagation();
                    e.preventDefault();
                    e.stopImmediatePropagation();

                    // Delete
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
                                ResetVendorInfoForm();
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
                let code = $(".js-VendorId-code").val();
                let name = self.val();
                // check
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
            $("#VendorInfo-grid tbody").empty();
            return $.gctlGrid.init({
                gridSelector: '#VendorInfo-grid',
                ajaxUrl: settings.baseUrl + "/GetTableData",
                checkAllSelector: "#js-VendorInfo-checkall",
                checkBoxClass: "checkBox",
                columns: [
                    $.gctlGrid.checkboxColumn("vendorId", "checkBox"),
                    $.gctlGrid.editLinkColumn("vendorId", "js-VendorInfo-edit"),
                    { data: "name", className: " text-middle" },
                    { data: "address", className: " text-middle" },
                    { data: "phone", className: "text-center text-middle" },
                    { data: "website", className: "text-center text-middle" },
                    { data: "email", className: "text-center text-middle" }
                ]
            });
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

                        resolve(data)
                    },
                    error: function (error) {
                        reject(error)
                    },
                })
            })
        }

        //#endregion

        //#region ResetVendorInfoForm

        $(document).on('click', '.js-VendorInfo-clear', function () {
            ResetVendorInfoForm();
            loadTable(); // if applicable
        });

        function ResetVendorInfoForm() {
            // Reset hidden/control fields
            $('#Tc').val(0);
            $('#Id').val('');
            $('#VendorId').val('');

            // Reset text inputs
            $('#VendorCode').val('');
            $('#Name').val('');
            $('#Address').val('');
            $('#City').val('');
            $('#StateOrProvince').val('');
            $('#ZipCode').val('');
            $('#VatRegNo').val('');
            $('#Phone').val('');
            $('#Fax').val('');
            $('#Email').val('');
            $('#Website').val('');
            $('#Remarks').val('');

            setSelect('#VendorPrefix', '', false);
            setSelect('#CountryId', '', false);
            setSelect('#Cpid', '', false);

            // Hide sections
            $('#LdateModifyHide').hide();

            // Optional: clear checkbox selections if you have a grid
            selectedItems?.clear?.();

            $("#VendorInfo-check-all").prop("checked", false);
            $('#VendorInfo-grid input[type="checkbox"]').prop('checked', false);

            // Optional: hide additional containers
            // $('#sectionBreak').hide();
            // $('#employeeIdContainer').addClass('d-none');
        }

        //#endregion

        //  #region validation

        function validation() {

            var code = $('#VendorPrefix').val();
            var sty = $('#Name').val();

            if (!code) {
                toastr.info('select Vendor Prefix');
                $('#VendorPrefix').select2('open');
                return false;
            }


            if (!sty) {
                toastr.info('Enter Vendor Name');
                $('#Name').trigger('focus');
                return false;
            }

            return true;
        }

        //#endregion

    }

}(jQuery));