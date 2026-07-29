(function ($) {
    $.shipmentModeLedgerSetup = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#ShipmentModeLedgerSetup-form",
            formContainer: ".js-ShipmentModeLedgerSetup-form-container",
            gridSelector: "#ShipmentModeLedgerSetup-grid",
            gridContainer: ".js-ShipmentModeLedgerSetup-grid-container",
            editSelector: ".js-ShipmentModeLedgerSetup-edit",
            saveSelector: ".js-ShipmentModeLedgerSetup-save",
            selectAllSelector: "#ShipmentModeLedgerSetup-check-all",
            deleteSelector: ".js-ShipmentModeLedgerSetup-delete-confirm",
            deleteModal: "#ShipmentModeLedgerSetup-delete-modal",
            finalDeleteSelector: ".js-ShipmentModeLedgerSetup-delete",
            clearSelector: ".js-ShipmentModeLedgerSetup-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-ShipmentModeLedgerSetup-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-ShipmentModeLedgerSetup-check-availability",
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
            $(document).on('click', '.js-ShipmentModeLedgerSetup-edit', function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                //console.log(id);
                $.ajax({
                    url: '/ShipmentModeLedgerSetup/GetById',
                    type: 'GET',
                    data: { code: id },
                    success: function (data) {
                        if (data) {
                           // console.log(data);
                            
                            $('#Tc').val(data.tc);
                            $('#Id').val(data.id);
                            $('#ExpenseTypeId').val(data.expenseTypeId);
                          
                            $('#ExpenseType').val(data.expenseType).trigger('change');
                            window.reinitCustomDropdownById('ExpenseType');
                           
                            $('#RevenueLedgerCodeNo').val(data.revenueLedgerCodeNo).trigger('change');
                            window.reinitCustomDropdownById('RevenueLedgerCodeNo');

                            $('#ExpenseLedgerCodeNo').val(data.expenseLedgerCodeNo).trigger('change');
                            window.reinitCustomDropdownById('ExpenseLedgerCodeNo');
                            
                            $('#AdvanceLedgerCode').val(data.advanceLedgerCode).trigger('change');
                            window.reinitCustomDropdownById('AdvanceLedgerCode');

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
                //debugger
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
                let code = $(".js-ExpenseTypeId-code").val();
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
            $.gctlGrid.initFromModel({
                gridId: 'ShipmentModeLedgerSetup-grid'
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

                        resolve(data)
                    },
                    error: function (error) {
                        reject(error)
                    },
                })
            })
        }

        //#endregion

        //#region ResetBranchAddressForm

        $(document).on('click', '.js-ShipmentModeLedgerSetup-clear', function () {
            ResetBranchAddressForm();
            loadTable(); // if applicable
        });

        function ResetBranchAddressForm() {
            // Reset hidden/control fields
            $('#Tc').val(0);
            $('#Id').val('');
            $('#ExpenseTypeId').val('');


            setSelect('#ExpenseType', '', false);
            setSelect('#ExpenseLedgerCodeNo', '', false);
            setSelect('#RevenueLedgerCodeNo', '', false);
            setSelect('#AdvanceLedgerCode', '', false);

            // Hide sections
            $('#LdateModifyHide').hide();

            // Optional: clear checkbox selections if you have a grid
            selectedItems?.clear?.();

            $("#ShipmentModeLedgerSetup-check-all").prop("checked", false);
            $('#ShipmentModeLedgerSetup-grid input[type="checkbox"]').prop('checked', false);

            // Optional: hide additional containers
            // $('#sectionBreak').hide();
            // $('#employeeIdContainer').addClass('d-none');
        }

        //#endregion

    }

}(jQuery));