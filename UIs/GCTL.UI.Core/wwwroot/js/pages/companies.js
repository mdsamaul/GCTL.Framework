(function ($) {
    $.companies = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#company-form",
            formContainer: ".js-company-form-container",
            gridSelector: "#companyTable",
            gridContainer: ".js-company-grid-container",
            editSelector: ".js-company-edit, .js-grid-edit",
            saveSelector: ".js-company-save",
            selectAllSelector: "#company-check-all",
            deleteSelector: ".js-company-delete-confirm",
            deleteModal: "#company-delete-modal",
            finalDeleteSelector: ".js-company-delete",
            clearSelector: ".js-company-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-company-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-company-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            lastCodeSelector: '#lastCode',
            load: function () {

            }
        }, options);

        var gridUrl = settings.baseUrl + "/GetAll";
        var saveUrl = settings.baseUrl + "/setup";
        var deleteUrl = settings.baseUrl + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(() => {
            // Initialize Custom DataTable from View Model
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'companyTable' });
            }

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

            // Export Excel / Pdf
            $(document).on('click', '.js-export-excel', function () {
                window.location.href = settings.baseUrl + '/ExportToExcel';
            });

            $(document).on('click', '.js-export-pdf', function () {
                window.location.href = settings.baseUrl + '/ExportToPdf';
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

                var options = {
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        if (response.isSuccess) {
                            var self = this;
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
                    options.processData = false;
                    options.contentType = false;
                }
                $.ajax(options);
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
                    $('#companyTable tbody input[type="checkbox"]:checked').each(function () {
                        var val = $(this).val();
                        if (val) selectedItems.push(val);
                    });
                }

                if (selectedItems.length > 0) {
                    var $modal = getDeleteModal();
                    $modal.find('.title').html("Are you sure you want to delete these item(s)?");
                    $modal.modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }
            });

            $("body").on("click", `${settings.finalDeleteSelector}, .js-company-delete, .js-delete, .js-gc-delete-confirm`, function (e) {
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

            $("body").on("keyup", settings.decimalSelector, function () {
                var self = $(this);
                showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
            });

        });

        // Load / Reload Table via CustomDataTable
        function loadTable() {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload(null, false);
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

                        if (window.initCustomDropdowns) {
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

        function validation() {
            var companyName = $('#CompanyName').val();
            var address = $('#Address1').val();

            if (!companyName) {
                toastr.info('Enter Company Name');
                $('#CompanyName').trigger('focus');
                return false;
            }
            if (!address) {
                toastr.info('Enter Address');
                $('#Address1').trigger('focus');
                return false;
            }
            return true;
        }
    };

}(jQuery));


