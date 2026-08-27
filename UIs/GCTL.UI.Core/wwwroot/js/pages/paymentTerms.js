(function ($) {
    $.paymentTerms = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/PaymentTerms",
            formSelector: "#paymentterms-form",
            formContainer: ".js-paymentterms-form-container",
            gridSelector: "#paymentTermsTable",
            gridContainer: ".js-paymentterms-grid-container",
            editSelector: ".js-paymentterms-edit, .js-grid-edit",
            saveSelector: ".js-paymentterms-save",
            selectAllSelector: "#paymentterms-check-all",
            deleteSelector: ".js-paymentterms-delete-confirm",
            deleteModal: "#paymentterms-delete-modal",
            finalDeleteSelector: ".js-paymentterms-delete",
            clearSelector: ".js-paymentterms-clear",
            topSelector: ".js-go",
            availabilitySelector: ".js-paymentterms-check-availability",
            haseFile: false,
            lastCodeSelector: "#lastCode",
            Percentise: "#Percentise",
            CreditDays: "#CreditDays",
            PaymentTermsName: "#PaymentTermsName",
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
            select2DD();
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'paymentTermsTable' });
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
                                        select2DD();
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
                            select2DD();
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
                    $(settings.gridSelector).find('tbody input[type="checkbox"]:checked').each(function () {
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

            $("body").on("click", `${settings.finalDeleteSelector}, .js-paymentterms-delete, .js-delete, .js-gc-delete-confirm`, function (e) {
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

        // PaymentTermsName Calculation Logic
        $(document).on('input', settings.Percentise, function () {
            let per = parseFloat($(this).val());
            if (per === 100) {
                $(settings.CreditDays).val("");
                $(settings.CreditDays).prop("disabled", true);
                $(settings.PaymentTermsName).val("100 % adv.");
                $(settings.Percentise).removeClass('border-danger');
            } else if (per > 100) {
                $(settings.PaymentTermsName).val('');
                toastr.info("Percentage must be 100 or less.");
                $(settings.CreditDays).prop("disabled", true);
                $(settings.Percentise).addClass('border-danger');
            } else if (per >= 0 && per <= 100) {
                $(settings.CreditDays).prop("disabled", false);
                $(settings.Percentise).removeClass('border-danger');
                let day = $(settings.CreditDays).val();
                if (day != "" && !isNaN(day)) {
                    $(settings.PaymentTermsName).val(`${per}% adv. + ${100 - per}%Cr. ( ${day} Days)`);
                } else {
                    $(settings.PaymentTermsName).val(`${per}% adv.`);
                }
            } else if (per < 0 || isNaN(per)) {
                $(settings.CreditDays).prop("disabled", true);
                if (isNaN(per)) $(settings.Percentise).removeClass('border-danger');
                else $(settings.Percentise).addClass('border-danger');
                $(settings.PaymentTermsName).val('');
            }
        });

        $(document).on('input', settings.CreditDays, function () {
            let day = $(this).val();
            let per = parseFloat($(settings.Percentise).val());
            if (!isNaN(per)) {
                if (day != "") {
                    $(settings.PaymentTermsName).val(`${per}% adv. + ${100 - per}%Cr. ( ${day} Days)`);
                } else {
                    $(settings.PaymentTermsName).val(`${per}% adv.`);
                }
            }
        });

        function loadTable() {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload();
            } else if (window.loadGridPage) {
                window.loadGridPage('paymentTermsGridContainer', settings.baseUrl + "/GetTableData", 1, 10);
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

                        select2DD();
                        resolve(data);
                    },
                    error: function (error) {
                        reject(error);
                    },
                });
            });
        }

        function validation() {
            var buy = $('#Type').val();
            var sty = $('#Percentise').val();

            if (!buy) {
                toastr.info('Select Type');
                $('#Type').select2 ? $('#Type').select2('open') : $('#Type').focus();
                return false;
            }

            if (!sty) {
                toastr.info('Enter Percentage');
                $('#Percentise').trigger('focus');
                return false;
            }

            return true;
        }

        function select2DD() {
            if ($.fn.select2) {
                $('.selectpickers').select2({
                    language: {
                        noResults: function () { return "No results found"; }
                    },
                    escapeMarkup: function (markup) { return markup; }
                });
            }
        }
    };

}(jQuery));