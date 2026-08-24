(function ($) {
    $.paymentType = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/PaymentType",
            formSelector: "#paymenttype-form",
            formContainer: ".js-paymenttype-form-container",
            gridSelector: "#paymentTypeGridContainer",
            gridContainer: ".js-paymenttype-grid-container",
            editSelector: ".js-paymenttype-edit, .js-grid-edit",
            saveSelector: ".js-paymenttype-save",
            selectAllSelector: "#paymenttype-check-all",
            deleteSelector: ".js-paymenttype-delete-confirm",
            deleteModal: "#paymenttype-delete-modal",
            finalDeleteSelector: ".js-paymenttype-delete",
            clearSelector: ".js-paymenttype-clear",
            topSelector: ".js-go",
            availabilitySelector: ".js-paymenttype-check-availability",
            haseFile: false,
            lastCodeSelector: "#lastCode",
            load: function () {

            }
        }, options);

        var saveUrl = settings.baseUrl + "/Setup";
        var deleteUrl = settings.baseUrl + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(() => {
            loadTable();

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

                var id = $(this).data('id');
                $(settings.deleteSelector).data('id', id);

                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            // Save
            $("body").on("click", settings.saveSelector, function () {
                if (!validation()) return false;
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

                var ajaxOptions = {
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        if (response.isSuccess) {
                            var $modalAncestor = $(settings.saveSelector).closest(".modal");
                            if ($modalAncestor.length && window.GcModal) {
                                GcModal.markSuccess($modalAncestor, response.lastCode);
                                GcModal.close($modalAncestor);
                            } else {
                                loadForm(saveUrl)
                                    .then((data) => {
                                        loadTable();
                                        $(settings.lastCodeSelector).val(response.lastCode);
                                    })
                                    .catch((error) => {
                                        console.log(error);
                                    });
                            }
                            toastr.success(response.message);
                        }
                        else {
                            toastr.error(response.message);
                            console.log(response);
                        }
                    }
                };
                if (settings.haseFile) {
                    ajaxOptions.processData = false;
                    ajaxOptions.contentType = false;
                }
                $.ajax(ajaxOptions);
            });

            $("body").on("click", settings.selectAllSelector, function () {
                $(".checkBox").prop('checked', $(this).prop('checked'));
            });

            function getDeleteModal() {
                var $m = $(settings.deleteModal);
                if ($m.length) return $m;
                $m = $("#deleteModal");
                if ($m.length) return $m;
                return $("#gcDeleteModal");
            }

            $("body").on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItems = [];
                if ($(this).data('id')) {
                    selectedItems.push($(this).data('id'));
                } else {
                    selectedItems = window.getSelectedGridIds ? window.getSelectedGridIds('paymentTypeGridContainer') : [];
                }

                if (selectedItems.length > 0) {
                    var $modal = getDeleteModal();
                    $modal.find('.title').html("Are you sure you want to delete these item(s)?");
                    $modal.modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }
            });

            $("body").on("click", `${settings.finalDeleteSelector}, .js-paymenttype-delete, .js-delete, .js-gc-delete-confirm`, function (e) {
                if (!selectedItems || selectedItems.length === 0) return;
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                var $modal = getDeleteModal();
                $.ajax({
                    url: deleteUrl,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedItems),
                    success: function (response) {
                        $modal.modal("hide");
                        if (response.isSuccess || response.success) {
                            toastr.success(response.message);
                            selectedItems = [];
                            loadTable();
                            loadForm(saveUrl);
                        }
                        else {
                            toastr.error(response.message);
                            console.log(response);
                        }
                    },
                    error: function (xhr) {
                        $modal.modal("hide");
                        toastr.error(xhr.responseJSON?.message || "Error deleting records.");
                    }
                });
            });

            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

        });

        function loadTable() {
            if (window.loadGridPage) {
                window.loadGridPage('paymentTypeGridContainer', settings.baseUrl + "/GetTableData", 1, 10);
            }
        }

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

                        resolve(data);
                    },
                    error: function (error) {
                        reject(error);
                    },
                });
            });
        }

        function validation() {
            var name = $('#PaymentType').val();
            if (!name) {
                toastr.info('Enter Payment Type');
                $('#PaymentType').trigger('focus');
                return false;
            }
            return true;
        }
    };

}(jQuery));