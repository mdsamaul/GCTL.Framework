(function ($) {
    $.companies = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#company-form",
            formContainer: ".js-company-form-container",
            gridSelector: "#company-grid",
            gridContainer: ".js-company-grid-container",
            editSelector: ".js-company-edit",
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


        var gridUrl = settings.baseUrl + "/grid";
        var saveUrl = settings.baseUrl + "/setup";
        var deleteUrl = settings.baseUrl + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(() => {
            //settings.load(settings.baseUrl, settings.gridSelector);
            loadCompanies(settings.baseUrl, settings.gridSelector);
            initialize();
            $("body").on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                let url = saveUrl + "/" + $(this).data("id") ?? "";
                loadForm(url);

                // Setting id on the delete selector for delete
                var id = $(this).data('id');
                $(settings.deleteSelector).data('id', id);

                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            //Report Xls , Pdf
            $(document).on('click', '.js-export-excel', function () {
                //;
                $("#loadingIndicator").show();

                $.ajax({
                    url: '/Companies/ExportToExcel',
                    method: 'GET',
                    success: function (response) {
                        window.location.href = '/Companies/ExportToExcel';
                    },
                    error: function () {
                        toastr.error('Error to Export Excel')
                    },
                    complete: function () {
                        $('#loadingIndicator').hide();
                    }
                });
            });
           
            $(document).on('click', '.js-export-pdf', function () {
               // ;
                $("#loadingIndicator").show();
                $.ajax({
                    url: '/Companies/ExportToPdf',
                    type: 'Get',

                    success: function (response) {
                        window.location.href = '/Companies/ExportToPdf'
                    },
                    error: function () {
                        toastr.error('An error occurred while exporting the PDF.');
                    },
                    complete: function () {
                        $('#loadingIndicator').hide();
                    }

                });
            });

            //

            // Save
            // $("body").on("click", settings.saveSelector, function () {
            //     
            //     if (!validation()) return false;

            //     var $valid = $(settings.formSelector).valid();
            //     if (!$valid) {
            //         return false;
            //     }

            //     var data;
            //     if (settings.haseFile)
            //         data = new FormData($(settings.formSelector)[0]);
            //     else
            //         data = $(settings.formSelector).serialize();

            //     var url = $(settings.formSelector).attr("action");

            //     var options = {
            //         url: url,
            //         method: "POST",
            //         data: data,
            //         success: function (response) {
            //             if (response.isSuccess) {
            //                 loadForm(saveUrl)
            //                     .then((data) => {
            //                         loadCompanies(settings.baseUrl, settings.gridSelector);
            //                         $(settings.lastCodeSelector).val(response.lastCode);
            //                         //alert($(settings.lastCodeSelector).val());
            //                         
            //                         GcModal.markSuccess($(this), response.lastCode);
            //                         GcModal.close($(this)); 
            //                     })
            //                     .catch((error) => {
            //                         console.log(error)
            //                     })

            //                 toastr.success(response.message);
            //             }
            //             else {
            //                 toastr.warning(response.message);
            //                 console.log(response);
            //             }
            //         }
            //     }
            //     if (settings.haseFile) {
            //         options.processData = false;
            //         options.contentType = false;
            //     }
            //     $.ajax(options);
            // });

            $('body').on('click', settings.saveSelector, function () {

                if (!validation()) return false;
                var validForm = $(settings.formSelector).valid();
                if (!validForm) return false;

                var data = $(settings.formSelector).serialize();
                var url = $(settings.formSelector).attr("action");
                var self = this;

                $.ajax({
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
                                    loadCompanies(settings.baseUrl, settings.gridSelector);
                                });
                            }
                        } else {
                            toastr.error(response.message);
                        }
                    }
                });
            });

            $("body").on("click", settings.selectAllSelector, function () {
                $(".checkBox").prop('checked',
                    $(this).prop('checked'));
            });


            // $("body").on("click", settings.deleteSelector, function (e) {
            //     
            //     e.preventDefault();
            //     if ($(this).data('id')) {
            //         selectedItems.push($(this).data('id'));
            //     } else {
            //         $('input:checkbox.checkBox').each(function () {
            //             if ($(this).prop('checked')) {
            //                 if (!selectedItems.includes($(this).val())) {
            //                     selectedItems.push($(this).val());
            //                 }
            //             }
            //         });
            //     }

            //     if (selectedItems.length > 0) {
            //         $(settings.deleteModal).modal("show");
            //     } else {
            //         toastr.info("Please select at least one item.");
            //     }
            // });

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
                    GcDelete.confirm("Are you sure to delete?", function () {
                        $.ajax({
                            url: deleteUrl + "/" + selectedItems,
                            method: "POST",
                            success: function (response) {
                                
                                if (response.success) {
                                    toastr.success(response.message);
                                    selectedItems = [];
                                    loadCompanies(settings.baseUrl, settings.gridSelector);
                                    loadForm(saveUrl);
                                } else {
                                    if (response.refSuccess) {
                                        toastr.warning(response.message, 'Warning');
                                    }
                                    toastr.error(response.message, 'Error');
                                }
                            }
                        });
                    });
                } else {
                    toastr.info("Please select at least one item.");
                }
            });


            $("body").on('show.bs.modal', settings.deleteModal, function (event) {
                
                //event.preventDefault();
                // Get button that triggered the modal
                var source = $(event.relatedTarget);
                var id = source.data("id");

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
                        url: deleteUrl + "/" + selectedItems,
                        method: "POST",
                        success: function (response) {
                            console.log(response);
                            $(modal).modal("hide");
                            if (response.success) {
                                toastr.success(response.message);
                                selectedItems = [];
                                /*settings.load(settings.baseUrl);*/
                                loadCompanies(settings.baseUrl, settings.gridSelector);
                                loadForm(saveUrl);
                            }
                            else {
                                if (response.refSuccess) {
                                    toastr.warning(response.message, 'Warning');
                                }

                                if (!response.success) {
                                    toastr.error(response.message, 'Error');
                                }
                                //toastr.warning(response.message);
                                console.log(response);
                            }
                        }
                    });
                });

            }).on('hide.bs.modal', function () {
                $("body").off("click", settings.finalDeleteSelector);
            });

            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });


            $("body").on("keyup", settings.decimalSelector, function () {
                var self = $(this);
                showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
            });

            //$("body").on("keyup", settings.availabilitySelector, function () {
            //    var self = $(this);
            //    let code = $(".js-code").val();
            //    let name = self.val();

            //    // check
            //    $.ajax({
            //        url: settings.baseUrl + "/CheckAvailability",
            //        method: "POST",
            //        data: { code: code, name: name },
            //        success: function (response) {
            //            console.log(response);
            //            if (response.isSuccess) {
            //                toastr.error(response.message);
            //            }
            //        }
            //    });
            //});
        });

        function loadCompanies(baseUrl, gridSelector) {
            var dataTable = $(gridSelector).DataTable({
                ajax: {
                    url: baseUrl + "/grid",
                    type: "GET",
                    datatype: "json"
                },
                columnDefs: [
                    { targets: [0], orderable: false },
                ],
                columns: [
                    {
                        "data": "companyCode",
                        "className": "text-center",
                        "render": function (data) {
                            return `<input type="checkbox" class="checkBox" value="${data}" />`;
                        },
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:8%');
                        }
                    },
                    {
                        "data": "companyCode",
                        "render": function (data) {
                            return `<a class='btn js-company-edit' data-id='${data}'><i>${data}</i></a>`;
                        },
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:15%');
                        }
                    },
                    {
                        "data": "companyName",
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:20%');
                        }
                    },
                    {
                        "data": "address1",
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:26%');
                        }
                    },
                    {
                        "data": "phone1",
                        "className": "text-center",
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:15%');
                        }
                    },
                    {
                        "data": "email",
                        "className": "text-center",
                        "createdCell": function (td) {
                            $(td).attr('style', 'width:16%');
                        }
                    }
                ],
                lengthChange: true,
                pageLength: 10,
                lengthMenu: [
                    [10, 25, 50, -1],
                    [10, 25, 50, 'All'],
                ],
                order: [[1, "asc"]],
                destroy: true
            });
        }

        function loadForm(url) {
            return new Promise((resolve, reject) => {
                $.ajax({
                    url: url,
                    type: 'GET',
                    success: function (data) {
                        $(settings.formContainer).empty();
                        $(settings.formContainer).html(data);
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
            $('.selectpicker88').select2({
                language: {
                    noResults: function () {
                        //return 'Not found <a class="add_new_item" href="javascript:void(0)">Add New</a>';
                    }
                },
                escapeMarkup: function (markup) {
                    return markup;
                }
            });

            $(".phone-validation").AppNumericOnly();

            showDecimalPlaces($(settings.decimalSelector).val(), $(settings.decimalSelector).parent().find(".input-group-text"));
        }

        function showDecimalPlaces(selector, target) {
            let places = parseInt(selector);
            let length = places > settings.maxDecimalPlace ? settings.maxDecimalPlace : places, numberOfDecimals = "";
            if (places > 0) {
                numberOfDecimals = "1.";
                for (var i = 1; i <= length; i++) {
                    numberOfDecimals += i;
                }
            } else {
                if (places == 0)
                    numberOfDecimals = "1";
                else
                    numberOfDecimals = "";
            }

            $(target).text(numberOfDecimals);

            if (settings.showNagativeFormat && numberOfDecimals.length > 0) {
                var symbol = $(".symbol").val();
                var options = [];
                options.push(`<option>-${numberOfDecimals} ${symbol}</option>`);
                options.push(`<option>(${numberOfDecimals}) ${symbol}</opttion>`);

                $(".negative-format").empty();
                $(".negative-format").append(options);
            }
        }

        function validation() {
         
            var companyName = $('#CompanyName').val();
            var add = $('#Address1').val();

            if (!companyName) {
                toastr.info('Select Company Name');
                $('#CompanyName').trigger('focus');
                return false;
            }          
            if (!add) {
                toastr.info('Enter Address 1');
                $('#Address1').trigger('focus');
                return false;
            }

            return true;

        }
    }

}(jQuery));

