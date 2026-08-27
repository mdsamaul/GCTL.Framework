(function ($) {
    $.bankAccountInfo = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#bankaccountinfo-form",
            formContainer: ".js-bankaccountinfo-form-container",
            gridSelector: "#bankAccountInfoTable",
            gridContainer: ".js-bankaccountinfo-grid-container",
            editSelector: ".js-bankaccountinfo-edit, .js-grid-edit",
            saveSelector: ".js-bankaccountinfo-save",
            selectAllSelector: "#bankaccountinfo-check-all",
            deleteSelector: ".js-bankaccountinfo-delete-confirm",
            deleteModal: "#bankaccountinfo-delete-modal",
            finalDeleteSelector: ".js-bankaccountinfo-delete",
            clearSelector: ".js-bankaccountinfo-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-bankaccountinfo-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-bankaccountinfo-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            lastCodeSelector: "#lastCode",
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

        $(() => {
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'bankAccountInfoTable' });
            }

            // Cascading Branch filter when Bank changes
            $("body").on("change", "#BankId", function () {
                var bankId = $(this).val();
                var branchDropdown = $('#BranchId');
                branchDropdown.empty().append('<option value="">-- Select Branch --</option>');

                if (bankId) {
                    $.ajax({
                        type: 'GET',
                        url: '/BankAccountInfo/GetBranchesByBankId',
                        data: { bankId: bankId },
                        success: function (data) {
                            if (data && data.length > 0) {
                                $.each(data, function (index, item) {
                                    branchDropdown.append('<option value="' + item.value + '">' + item.text + '</option>');
                                });
                            }
                        },
                        error: function () {
                            toastr.error('Failed to load branches.');
                        }
                    });
                }
            });

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

            $("body").on("click", `${settings.finalDeleteSelector}, .js-bankaccountinfo-delete, .js-delete, .js-gc-delete-confirm`, function (e) {
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
                        if (response.success || response.isSuccess) {
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
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload();
            } else if (window.loadGridPage) {
                window.loadGridPage('bankAccountInfoGridContainer', settings.baseUrl + "/GetTableData", 1, 10);
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
            var bankId = $('#BankId').val();
            var branchId = $('#BranchId').val();
            var accountName = $('#AccountName').val();
            var accountNo = $('#AccountNo').val();

            if (!bankId) {
                toastr.info('Select Bank');
                $('#BankId').trigger('focus');
                return false;
            }
            if (!branchId) {
                toastr.info('Select Branch');
                $('#BranchId').trigger('focus');
                return false;
            }
            if (!accountName) {
                toastr.info('Enter Account Name');
                $('#AccountName').trigger('focus');
                return false;
            }
            if (!accountNo) {
                toastr.info('Enter Account No.');
                $('#AccountNo').trigger('focus');
                return false;
            }
            return true;
        }
    };

}(jQuery));
