(function ($) {
    $.vendorInfo = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/VendorInfo",
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

        var base = (settings.baseUrl || "/VendorInfo").replace(/\/+$/, "");
        var gridUrl = base + "/GetTableData";
        var saveUrl = base + "/Setup";
        var deleteUrl = base + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        //#endregion

        $(() => {
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'VendorInfo-grid' });
            }

            loadTable();
            $('#LdateModifyHide').hide();
            $('#sectionBreak').hide();

            //#region formatDateTime
            loadContactPersonsTable();

            function formatDateTime(dateStr) {
                if (!dateStr) return '';
                var date = new Date(dateStr);
                if (isNaN(date.getTime())) return dateStr;
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

            //#region GetById (Edit)

            $("body").off('click', settings.editSelector).on('click', settings.editSelector, function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                if (!id) return;

                $.ajax({
                    url: base + '/GetById',
                    type: 'GET',
                    data: { code: id },
                    success: function (data) {
                        if (data) {
                            console.log(data);
                            $('#Tc').val(data.tc);
                            $('#Id').val(data.id || '');
                            $('#ID').val(data.id || '');
                            $('#VendorId').val(data.vendorId);
                            $('#VendorPrefix').val(data.vendorPrefix).trigger('change');
                            if (typeof window.reinitCustomDropdownById === "function") {
                                window.reinitCustomDropdownById('VendorPrefix');
                            }
                            $('#VendorCode').val(data.vendorCode);
                            $('#Name').val(data.name);
                            $('#Address').val(data.address);
                            $('#CountryId').val(data.countryId).trigger('change');
                            if (typeof window.reinitCustomDropdownById === "function") {
                                window.reinitCustomDropdownById('CountryId');
                            }
                            $('#City').val(data.city);
                            $('#StateOrProvince').val(data.stateOrProvince);
                            $('#ZipCode').val(data.zipCode);
                            $('#VatRegNo').val(data.vatRegNo);
                            $('#Phone').val(data.phone);
                            $('#Fax').val(data.fax);
                            $('#Email').val(data.email);
                            $('#Website').val(data.website);
                            $('#Remarks').val(data.remarks);

                            loadContactPersonsTableForEdit(data.cpid);

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

                            // Store id on delete button
                            $(settings.deleteSelector).data('id', data.vendorId);
                            $("html, body").animate({ scrollTop: 0 }, 500);
                        }
                    },
                    error: function () {
                        toastr.error('Failed to load vendor information data.');
                    }
                });
            });

            //#endregion

            //#region Save

            $("body").off("click", settings.saveSelector).on("click", settings.saveSelector, function () {
                if (!validation()) {
                    return false;
                }
                var $valid = $(settings.formSelector).valid();
                if (!$valid) {
                    return false;
                }

                var data;
                if (settings.haseFile)
                    data = new FormData($(settings.formSelector)[0]);
                else
                    data = $(settings.formSelector).serialize();

                var url = $(settings.formSelector).attr("action") || saveUrl;

                var options = {
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        if (response.isSuccess) {
                            toastr.success(response.message);
                            loadForm(saveUrl)
                                .then((data) => {
                                    loadTable();
                                    ResetVendorInfoForm();
                                    $(settings.lastCodeSelector).val(response.lastCode);
                                    $.get(base + '/GetNewVendorId', function (res) {
                                        if (res && res.vendorId) {
                                            $('#VendorId').val(res.vendorId);
                                        }
                                    });
                                    $(settings.deleteSelector).data('id', '');
                                })
                                .catch((error) => {
                                    console.log(error);
                                });
                        }
                        else {
                            toastr.error(response.message);
                            console.log(response);
                        }
                    },
                    error: function (xhr) {
                        toastr.error("An error occurred while saving.");
                    }
                };
                if (settings.haseFile) {
                    options.processData = false;
                    options.contentType = false;
                }
                $.ajax(options);
            });

            //#endregion

            //#region selectAllSelector & deleteSelector

            $("body").off("click", `${settings.selectAllSelector}, #js-VendorInfo-checkall`)
                     .on("click", `${settings.selectAllSelector}, #js-VendorInfo-checkall`, function () {
                var isChecked = $(this).prop('checked');
                $(settings.gridSelector).find("tbody input[type='checkbox']").prop('checked', isChecked);
            });

            $("body").off("click", settings.deleteSelector)
                     .on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItems = [];

                // 1. Collect from checked checkboxes in table
                $(settings.gridSelector).find("tbody input[type='checkbox']:checked, tbody .checkbox:checked, tbody .form-check-input:checked").each(function () {
                    var val = $(this).val();
                    if (val && !selectedItems.includes(val)) {
                        selectedItems.push(val);
                    }
                });

                // 2. Active form item fallback
                if (selectedItems.length === 0) {
                    var btnId = $(this).data('id');
                    if (btnId) {
                        selectedItems.push(btnId);
                    } else {
                        var formId = $("#VendorId").val();
                        var tcVal = parseInt($("#Tc").val() || $("#tc").val()) || 0;
                        if (formId && tcVal > 0) {
                            selectedItems.push(formId);
                        }
                    }
                }

                if (selectedItems.length > 0) {
                    var count = selectedItems.length;
                    var titleText = count > 1 
                        ? `Are you sure you want to delete these ${count} items? This process cannot be undone.`
                        : "Are you sure you want to delete this item? This process cannot be undone.";
                    $(settings.deleteModal).find('.title').text(titleText);
                    $(settings.deleteModal).modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }
            });

            //#endregion

            //#region Delete & deleteModal

            $("body").off("click", settings.finalDeleteSelector)
                     .on("click", settings.finalDeleteSelector, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                if (!selectedItems || selectedItems.length === 0) {
                    $(settings.deleteModal).modal("hide");
                    return;
                }

                // Delete AJAX
                $.ajax({
                    url: deleteUrl,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedItems),
                    success: function (response) {
                        $(settings.deleteModal).modal("hide");
                        if (response.success) {
                            toastr.success(response.message);
                            selectedItems = [];

                            loadTable();
                            ResetVendorInfoForm();
                            loadForm(saveUrl);
                            $(`${settings.selectAllSelector}, #js-VendorInfo-checkall`).prop('checked', false);
                            $(settings.deleteSelector).data('id', '');
                        } else {
                            toastr.error(response.message);
                        }
                    },
                    error: function (xhr) {
                        $(settings.deleteModal).modal("hide");
                        var msg = "Failed to delete.";
                        try {
                            var res = JSON.parse(xhr.responseText);
                            if (res.message) msg = res.message;
                        } catch (ex) {}
                        toastr.error(msg);
                    }
                });
            });

            //#endregion

            $('body').on('click', '.load-branch-btn', function () {
                $.ajax({
                    url: '/BranchAddress/Index',
                    type: 'GET',
                    success: function (res) {
                        $('#branchAddressContent').html(res);
                        // Switch to tab
                        new bootstrap.Tab(document.querySelector('#vendorTabs a[href="#branchAddress"]')).show();
                    },
                    error: function () {
                        alert('Error loading branch address');
                    }
                });
            });

            //#region topSelector & decimalSelector

            $("body").off("click", settings.topSelector).on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            $("body").off("keyup", settings.decimalSelector).on("keyup", settings.decimalSelector, function () {
                var self = $(this);
                if (typeof showDecimalPlaces === "function") {
                    showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
                }
            });

            //#endregion

            //#region availabilitySelector

            $("body").off("keyup", settings.availabilitySelector).on("keyup", settings.availabilitySelector, function () {
                var self = $(this);
                let code = $(".js-VendorId-code").val();
                let name = self.val();
                // check
                $.ajax({
                    url: base + "/CheckAvailability",
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
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload(null, false);
            }
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
                        $(settings.formContainer).empty();
                        $(settings.formContainer).html(data);
                        $.validator.unobtrusive.parse($(settings.formSelector));

                        if (typeof window.initCustomDropdowns === "function") {
                            window.initCustomDropdowns(settings.formContainer, true);
                        }

                        loadContactPersonsTable();

                        resolve(data);
                    },
                    error: function (error) {
                        reject(error);
                    },
                });
            });
        }

        //#endregion

        //#region ResetVendorInfoForm

        $("body").off('click', `${settings.clearSelector}, .js-VendorInfo-clear`)
                 .on('click', `${settings.clearSelector}, .js-VendorInfo-clear`, function (e) {
            e.preventDefault();
            ResetVendorInfoForm();

            $.get(base + '/GetNewVendorId', function (res) {
                if (res && res.vendorId) {
                    $('#VendorId').val(res.vendorId);
                }
            });
        });

        function ResetVendorInfoForm() {
            $('#Tc').val(0);
            $('#Id').val('');
            $('#ID').val('');

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

            if (typeof setSelect === "function") {
                setSelect('#VendorPrefix', '', false);
                setSelect('#CountryId', '', false);
            } else {
                $('#VendorPrefix').val('').trigger('change');
                $('#CountryId').val('').trigger('change');
            }
            loadContactPersonsTable();

            $('#LdateModifyHide').hide();
            $('#sectionBreak').hide();

            selectedItems = [];

            $(`${settings.selectAllSelector}, #js-VendorInfo-checkall`).prop("checked", false);
            $(settings.gridSelector).find('tbody input[type="checkbox"]').prop('checked', false);
            $(settings.deleteSelector).data('id', '');
        }

        //#endregion

        //#region validation

        function validation() {

            var code = $('#VendorPrefix').val();
            var sty = $('#Name').val();

            if (!code) {
                toastr.info('Select Vendor Prefix');
                $('#VendorPrefix').select2('open');
                return false;
            }

            if (!sty || !sty.trim()) {
                toastr.info('Enter Vendor Name');
                $('#Name').trigger('focus');
                return false;
            }

            return true;
        }

        //#endregion

        function loadContactPersonsTableForEdit(selectedCpid) {
            $.ajax({
                url: base + '/GetContactPersonsForTable',
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
                        if (selectedCpid) {
                            selectedValues = Array.isArray(selectedCpid)
                                ? selectedCpid
                                : selectedCpid.toString().split(',').map(x => x.trim()).filter(x => x);
                        }

                        if (typeof setSelect === "function") {
                            setSelect(
                                '#Cpid',
                                selectedValues,
                                false,
                                contactItems,
                                true,
                                'Select Contact Person(s)'
                            );
                        }
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Error loading contact persons');
                }
            });
        }

        function loadContactPersonsTable() {
            $.ajax({
                url: base + '/GetContactPersonsForTable',
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

                        if (typeof setSelect === "function") {
                            setSelect(
                                '#Cpid',
                                [],
                                false,
                                contactItems,
                                true,
                                'Select Contact Person(s)'
                            );
                        }
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Error loading contact persons');
                }
            });
        }

    };

}(jQuery));