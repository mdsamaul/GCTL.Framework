(function ($) {
    $.importerInfo = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#ImporterInfo-form",
            formContainer: ".js-ImporterInfo-form-container",
            gridSelector: "#ImporterInfo-grid",
            gridContainer: ".js-ImporterInfo-grid-container",
            editSelector: ".js-ImporterInfo-edit",
            saveSelector: ".js-ImporterInfo-save",
            selectAllSelector: "#ImporterInfo-check-all",
            deleteSelector: ".js-ImporterInfo-delete-confirm",
            deleteModal: "#ImporterInfo-delete-modal",
            finalDeleteSelector: ".js-ImporterInfo-delete",
            clearSelector: ".js-ImporterInfo-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-ImporterInfo-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-ImporterInfo-check-availability",
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
            loadSNCId();

            //#region editSelector & clearSelector

            $("body").on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
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
                $(settings.deleteSelector).data('id', id);
                $("html, body").animate({ scrollTop: 0 }, 500);

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
                let code = $(".js-ImporterId-code").val();
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

        //#region GenerateNewId

        function loadSNCId() {
            $.ajax({
                url: "/ImporterInfo/GenerateNewId",
                type: "GET",
                dataType: "json",

                success: function (data) {
                    if (data) {
                        $('#ImporterId').val(data);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error fetching ImporterId:", error);
                }
            })
        }

        //#endregion

        //#region loadTable
        function loadTable() {
            $("#ImporterInfo-grid tbody").empty();
            return $.gctlGrid.init({
                gridSelector: '#ImporterInfo-grid',
                ajaxUrl: settings.baseUrl + "/GetTableData",
                checkAllSelector: "#js-ImporterInfo-checkall",
                checkBoxClass: "checkBox",
                columns: [
                    $.gctlGrid.checkboxColumn("importerId", "checkBox"),
                    $.gctlGrid.editLinkColumn("importerId", "js-ImporterInfo-edit"),
                    { data: "importerName", className: " text-middle" },
                    { data: "importerAddress", className: " text-middle" },
                    { data: "phone", className: "text-center text-middle" },
                    { data: "website", className: "text-center text-middle" },
                    { data: "email1", className: "text-center text-middle" }
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
                        $(settings.formContainer).empty();
                        $(settings.formContainer).html(data);
                        $.validator.unobtrusive.parse($(settings.formSelector));

                        window.initCustomDropdowns(settings.formContainer, true);
                        // loadSNCId();
                        resolve(data)
                    },
                    error: function (error) {
                        reject(error)
                    },
                })
            })
        }

        //#endregion

        //  #region validation

        function validation() {

            var sty = $('#ImporterName').val();

            if (!sty) {
                toastr.info('Enter Importer Name');
                $('#ImporterName').trigger('focus');
                return false;
            }

            return true;
        }

        //#endregion

    }

}(jQuery));