(function ($) {
    $.portType = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#PortType-form",
            formContainer: ".js-PortType-form-container",
            gridSelector: "#PortType-grid",
            gridContainer: ".js-PortType-grid-container",
            editSelector: ".js-PortType-edit",
            saveSelector: ".js-PortType-save",
            selectAllSelector: "#PortType-check-all",
            deleteSelector: ".js-PortType-delete-confirm",
            deleteModal: "#PortType-delete-modal",
            finalDeleteSelector: ".js-PortType-delete",
            clearSelector: ".js-PortType-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-PortType-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-PortType-check-availability",
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
                title = "Are you sure want to delete these items?";
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
                let code = $(".js-PortId-code").val();
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

            //#region Modal & quickAddSelector

            $("body").on("click", settings.quickAddSelector, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                loadUrl = $(this).data("url");
                target = $(this).data("target");
                reloadUrl = $(this).data("reload-url");
                title = $(this).data("title");

                $(settings.quickAddModal + " .modal-title").html(title);
                $(settings.quickAddModal + " .modal-body").empty();
                $(settings.quickAddModal + " .modal-body").load(loadUrl, function () {
                    $(settings.quickAddModal).modal("show");
                    $("#header").hide();
                    $(settings.quickAddModal + " .modal-body #header").hide();
                    $("#left_menu").hide();
                    $(settings.quickAddModal + " .modal-body #left_menu").hide();
                    $("#main-content").toggleClass("collapse-main");
                    $(settings.quickAddModal + " .modal-body #main-content").toggleClass("collapse-main");
                    $("body").removeClass("sidebar-mini");
                });
            });

            $("body").on("click", ".js-modal-dismiss", function () {
                $("body").removeClass("sidebar-mini").addClass("sidebar-mini");
                $("#header").show();
                $(settings.quickAddModal + " .modal-body #header").show();
                $("#left_menu").show();
                $(settings.quickAddModal + " .modal-body #left_menu").show();
                $("#main-content").toggleClass("collapse-main");
                $(settings.quickAddModal + " .modal-body #main-content").toggleClass("collapse-main");

                lastCode = $(settings.quickAddModal + " #lastCode").val();
                $(settings.quickAddModal + " .modal-body").empty();
                $(settings.quickAddModal).modal("hide");

                $(target).empty("");
                $(target).append($('<option>', { value: '', text: `--Select ${title}--` }));
                $.ajax({
                    url: reloadUrl,
                    method: "GET",
                    success: function (response) {
                        console.log(response);
                        $.each(response, function (i, item) {
                            $(target).append($('<option>', { value: item.code, text: item.name }));
                        });
                        $(target).val(lastCode);
                        console.log("Testttt", lastCode);
                    }
                });
            });

            //#endregion

        });

        //#region loadTable
        function loadTable() {
            $.get(settings.baseUrl + "/GetTableData")
                .done(html => {
                    $(settings.gridContainer).html(html);

                    if ($.fn.DataTable.isDataTable(settings.gridSelector)) {
                        $(settings.gridSelector).DataTable().destroy();
                    }

                    $(settings.gridSelector).DataTable({
                        lengthChange: true,
                        pageLength: 10,
                        lengthMenu: [
                            [10, 25, 50, -1],
                            [10, 25, 50, 'All'],
                        ],
                        order: [[1, "desc"]],
                        destroy: true,
                        paging: true,
                        searching: true,
                        responsive: true,
                    });

                })
                .fail(() => toastr.error("Failed to load table data."));
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

            var col = $('#PortType').val();

            if (!col) {
                toastr.info('Enter Port Type');
                $('#PortType').trigger('focus');
                return false;
            }
            return true;
        }

        //#endregion

    }

}(jQuery));