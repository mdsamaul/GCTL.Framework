(function ($) {
    $.transportType = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#TransportType-form",
            formContainer: ".js-TransportType-form-container",
            gridSelector: "#TransportType-grid",
            gridContainer: ".js-TransportType-grid-container",
            editSelector: ".js-TransportType-edit",
            saveSelector: ".js-TransportType-save",
            selectAllSelector: "#TransportType-check-all",
            deleteSelector: ".js-TransportType-delete-confirm",
            deleteModal: "#TransportType-delete-modal",
            finalDeleteSelector: ".js-TransportType-delete",
            clearSelector: ".js-TransportType-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-TransportType-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-TransportType-check-availability",
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

        function initGrid() {
            if ($.gctlGrid) {
                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.gridSelector)) {
                    $.gctlGrid.initFromModel({ gridId: 'TransportType-grid' });
                }
            }
        }

        function adjustGridColumns() {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                var dt = $(settings.gridSelector).DataTable();
                dt.columns.adjust();
                if (dt.responsive) {
                    dt.responsive.recalc();
                }
            }
        }

        // Initialize grid immediately
        initGrid();

        // Also adjust columns when inside a modal once shown
        var $modalAncestor = $(settings.gridSelector).closest(".modal");
        if ($modalAncestor.length) {
            $modalAncestor.off("shown.bs.modal.transportType gc-modal:loaded.transportType")
                          .on("shown.bs.modal.transportType gc-modal:loaded.transportType", function () {
                initGrid();
                setTimeout(adjustGridColumns, 50);
            });
        }

        $(() => {
            initGrid();

            //#region editSelector & clearSelector

            $("body").off("click", `${settings.editSelector},${settings.clearSelector}`)
                     .on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                let url = saveUrl + ($(this).data("id") ? "/" + $(this).data("id") : "");

                loadForm(url).then((data) => {
                    console.info("Form Loaded Successfully", data);
                }).catch((error) => {
                    console.error("Failed to load form", error);
                });

                // Setting id on the delete selector for delete
                var id = $(this).data('id');
                         $(settings.deleteSelector).data('data-id', id);
                $("html, body").animate({ scrollTop: 0 }, 500);

            });

            //#endregion

            //#region Save

            $("body").off("click", settings.saveSelector)
                     .on("click", settings.saveSelector, function () {

                var self = this;
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
                            toastr.success(response.message);


                            var $modalAncestor = $(self).closest(".modal");
                            if ($modalAncestor.length && window.GcModal) {
                                GcModal.markSuccess($modalAncestor, response.lastCode);
                                GcModal.close($modalAncestor);
                            } else {
                                // normal page এ থাকলে আগের মতোই form reload করো
                                loadForm(saveUrl).then(() => {
                                    loadTable();
                                });
                            }
                        } else {
                            toastr.error(response.message);
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

            $("body").off("click", `${settings.selectAllSelector}, #js-TransportType-checkall`)
                     .on("click", `${settings.selectAllSelector}, #js-TransportType-checkall`, function () {
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
                        var formId = $("#VehicleTypeId").val();
                        var tcVal = parseInt($("#Tc").val() || $("#tc").val()) || 0;
                        if (formId && tcVal > 0) {
                            selectedItems.push(formId);
                        }
                    }
                }

                if (selectedItems.length > 0) {
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
                            loadForm(saveUrl);
                            $(`${settings.selectAllSelector}, #js-TransportType-checkall`).prop('checked', false);
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

            //#region topSelector & decimalSelector

            $("body").off("click", settings.topSelector).on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });


            $("body").off("keyup", settings.decimalSelector).on("keyup", settings.decimalSelector, function () {
                var self = $(this);
                showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
            });

            //#endregion

            //#region availabilitySelector

            $("body").off("keyup", settings.availabilitySelector).on("keyup", settings.availabilitySelector, function () {
                var self = $(this);
                let code = $(".js-VehicleTypeId-code").val();
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
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload(null, false);
            } else {
                initGrid();
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

                        resolve(data)
                    },
                    error: function (error) {
                        reject(error)
                    },
                })
            })
        }

        //#endregion

        //#region validation

        function validation() {

            var col = $('#VehicleType').val();

            if (!col) {
                toastr.info('Enter Transport Type');
                $('#VehicleType').trigger('focus');
                return false;
            }
            return true;
        }

        //#endregion

    }

}(jQuery));