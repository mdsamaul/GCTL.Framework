(function ($) {
    $.exporterInfo = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/ExporterInfo",
            formSelector: "#ExporterInfo-form",
            formContainer: ".js-ExporterInfo-form-container",
            gridSelector: "#ExporterInfo-grid",
            gridContainer: ".js-ExporterInfo-grid-container",
            editSelector: ".js-ExporterInfo-edit",
            saveSelector: ".js-ExporterInfo-save",
            selectAllSelector: "#ExporterInfo-check-all",
            deleteSelector: ".js-ExporterInfo-delete-confirm",
            deleteModal: "#ExporterInfo-delete-modal",
            finalDeleteSelector: ".js-ExporterInfo-delete",
            clearSelector: ".js-ExporterInfo-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-ExporterInfo-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-ExporterInfo-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            lastCodeSelector: '#lastCode',
            load: function () {

            }
        }, options);

        var base = (settings.baseUrl || "/ExporterInfo").replace(/\/+$/, "");
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
                $.gctlGrid.initFromModel({ gridId: 'ExporterInfo-grid' });
            }

            loadTable();
            loadSNCId();

            //#region editSelector & clearSelector

            $("body").off("click", `${settings.editSelector},${settings.clearSelector}`)
                     .on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                var editId = $(this).data("id");
                let url = saveUrl + (editId ? "/" + editId : "");

                loadForm(url).then((data) => {
                    console.info("Form Loaded Successfully", data);
                    if (!editId) {
                        loadSNCId();
                    }
                }).catch((error) => {
                    console.error("Failed to load form", error);
                });

                if (!editId) {
                    $(`${settings.selectAllSelector}, #js-ExporterInfo-checkall`).prop('checked', false);
                    $(settings.gridSelector).find("tbody input[type='checkbox']").prop('checked', false);
                    selectedItems = [];
                }

                // Setting id on the delete selector for delete
                $(settings.deleteSelector).data('id', editId || '');
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            //#endregion

            //#region Save

            $("body").off("click", settings.saveSelector)
                     .on("click", settings.saveSelector, function () {
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
                                    $(settings.lastCodeSelector).val(response.lastCode);
                                    loadSNCId();
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

            $("body").off("click", `${settings.selectAllSelector}, #js-ExporterInfo-checkall`)
                     .on("click", `${settings.selectAllSelector}, #js-ExporterInfo-checkall`, function () {
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
                        var formId = $("#ExporterId").val();
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
                            loadForm(saveUrl).then(() => {
                                loadSNCId();
                            });
                            $(`${settings.selectAllSelector}, #js-ExporterInfo-checkall`).prop('checked', false);
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
                let code = $(".js-ExporterId-code").val();
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

        //#region GenerateNewId

        function loadSNCId() {
            $.ajax({
                url: base + "/GenerateNewId",
                type: "GET",
                dataType: "json",

                success: function (data) {
                    if (data) {
                        $('#ExporterId').val(data);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error fetching ExporterId:", error);
                }
            });
        }

        //#endregion

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

                        resolve(data);
                    },
                    error: function (error) {
                        reject(error);
                    },
                });
            });
        }

        //#endregion

        //#region validation

        function validation() {

            var sty = $('#ExporterName').val();

            if (!sty || !sty.trim()) {
                toastr.info('Enter Exporter Name');
                $('#ExporterName').trigger('focus');
                return false;
            }

            return true;
        }

        //#endregion

    };

}(jQuery));