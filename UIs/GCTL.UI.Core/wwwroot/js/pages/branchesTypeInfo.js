(function ($) {
    $.branchesTypeInfo = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#branch-form",
            formContainer: ".js-branch-form-container",
            gridSelector: "#branch-grid",
            gridContainer: ".js-branch-grid-container",
            editSelector: ".js-branch-edit",
            saveSelector: ".js-branch-save",
            selectAllSelector: "#branch-check-all",
            deleteSelector: ".js-branch-delete-confirm",
            clearSelector: ".js-branch-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-branch-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-branch-check-availability",
            haseFile: false,
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
            initGrid();
            initialize();

            $('body').on('click', `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                let url = saveUrl + "/" + ($(this).data("id") || "");
                loadForm(url);

                var id = $(this).data('id');
                $(settings.deleteSelector).data('id', id);

                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            $('body').on('click', settings.saveSelector, function () {

                if (!validation()) return false;
                var CompanyCode = $("#CompanyCode").val();
                var validForm = $(settings.formSelector).valid();
                if (!validForm) {
                    return false;
                }

                var data;
                if (settings.haseFile) {
                    data = new FormData($(settings.formSelector)[0]);
                } else {
                    data = $(settings.formSelector).serialize();
                }

                var url = $(settings.formSelector).attr("action");

                var options = {
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        if (response.isSuccess) {
                            loadForm(saveUrl)
                                .then((data) => {
                                    $("#CompanyCode").val(CompanyCode);
                                    reloadGrid();
                                })
                                .catch((error) => {
                                    console.log(error);
                                });

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

            $('body').on('click', settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItems = [];
                if ($(this).data('id')) {
                    selectedItems.push($(this).data('id'));
                } else {
                    $('#branch-grid tbody input[type="checkbox"]:checked').each(function () {
                        var val = $(this).val();
                        if (val) selectedItems.push(val);
                    });
                }

                if (selectedItems.length > 0) {
                    GcDelete.confirm("Are you sure to delete?", function () {
                        $.ajax({
                            url: deleteUrl + "/" + selectedItems.join(','),
                            method: "POST",
                            success: function (response) {
                                if (response.success) {
                                    toastr.success(response.message);
                                    selectedItems = [];
                                    reloadGrid();
                                    loadForm(saveUrl);
                                }
                                else {
                                    if (response.refSuccess) {
                                        toastr.warning(response.message, 'Warning');
                                    }
                                    if (!response.success) {
                                        toastr.error(response.message, 'Error');
                                    }
                                }
                            }
                        });
                    });
                } else {
                    toastr.info("Please select at least one item.", 'Warning');
                }
            });

            $("body").on('click', settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            $("body").on('keyup', settings.decimalSelector, function () {
                var self = $(this);
                showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
            });

            $('body').on('keyup', settings.availabilitySelector, function () {
                var self = $(this);
                let code = $(".js-code").val();
                let name = self.val();

                $.ajax({
                    url: settings.baseUrl + "/CheckAvailability",
                    method: "POST",
                    data: { code: code, name: name },
                    success: function (response) {
                        if (response.isSuccess) {
                            toastr.error(response.message);
                        }
                    }
                });
            });
        });

        $("body").on("change", "#CompanyCode", function () {            
            reloadGrid();
        });

        function initGrid() {
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({
                    gridId: 'branch-grid',
                    ajaxData: function (d) {
                        d.CompanyCode = $("#CompanyCode").val() || "";
                    }
                });
            }
        }

        function reloadGrid() {
            if ($.gctlGrid) {
                $.gctlGrid.reload('branch-grid');
            }
        }

        function loadForm(url) {
            return new Promise((resolve, reject) => {
                const currentCompanyCode = $("#CompanyCode").val();

                $.ajax({
                    url: url,
                    type: 'GET',
                    success: function (data) {
                        $(settings.formContainer).empty();
                        $(settings.formContainer).html(data);
                        $("#CompanyCode").val(currentCompanyCode);
                        $.validator.unobtrusive.parse($(settings.formSelector));

                        initialize();
                        resolve(data)
                    },
                    error: function (error) {
                        reject(error)
                    },
                })
            })
        }

        function initialize() {
            var $select = $('#CompanyCode');
            var items = $select.find('option').map(function () {
                return { id: $(this).val(), text: $(this).text(), selected: $(this).is(':selected') };
            }).get().filter(function (i) { return i.id !== ""; });

            new customDropdownSingle('#CompanyCode', {
                placeholder: 'Select Company',
                items: items
            }).init();
        }
        function validation() {
            var compName = $('#CompanyCode').val();
            var baName = $('#BranchName').val();
            var add = $('#Address').val();

            if (!compName) {
                toastr.info('Select Company');
                $('#CompanyCode').select2('open');
                return false;
            }
            if (!baName) {
                toastr.info('Enter Branch');
                $('#BranchName').trigger('focus');
                return false;
            }
            if (!add) {
                toastr.info('Enter Address');
                $('#Address').trigger('focus');
                return false;
            }

            return true;
        }
    }

}(jQuery));