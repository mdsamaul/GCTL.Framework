(function ($) {
    $.userAccesses = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#useraccess-form",
            formContainer: ".js-useraccess-form-container",
            gridSelector: "#useraccess-grid",
            gridContainer: ".js-useraccess-grid-container",
            editSelector: ".js-useraccess-edit",
            saveSelector: ".js-useraccess-save",
            selectAllSelector: "#useraccess-check-all",
            deleteSelector: ".js-useraccess-delete-confirm",
            deleteModal: "#useraccess-delete-modal",
            finalDeleteSelector: ".js-useraccess-delete",
            clearSelector: ".js-useraccess-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-useraccess-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-useraccess-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            load: function () {

            }
        }, options);


        var gridUrl = settings.baseUrl + "/grid";
        var saveUrl = settings.baseUrl + "/setup";
        var deleteUrl = settings.baseUrl + "/Delete";
        var selectedItems = [];
        $(() => {

            //settings.load(settings.baseUrl, settings.gridSelector);
            loadUserAccesses(settings.baseUrl, settings.gridSelector);
            initialize();
            toggleEmployeeRequired();
            $("body").on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                let url = saveUrl + "/" + $(this).data("id") ?? "";
                loadForm(url);

                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            // Save
            $("body").on("click", settings.saveSelector, function () {
                var $btn = $(this);

                var isValid = validation();
                if (!isValid) return false;

                // var $valid = $(settings.formSelector).valid();
                // console.log($valid);
                // if (!$valid) {
                //     return false;
                // }

                if ($btn.data("submitting")) return false;
                $btn.data("submitting", true).prop("disabled", true);

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
                                    loadUserAccesses(settings.baseUrl, settings.gridSelector);
                                    /*settings.load(settings.baseUrl, settings.gridSelector);*/
                                })
                                .catch((error) => {
                                    console.log(error)
                                })

                            toastr.success(response.success, 'Success');
                        }
                        else {
                            toastr.error(response.message, 'Error');
                            console.log(response);
                        }
                    },
                    complete: function () {
                        $btn.data("submitting", false).prop("disabled", false);
                    }
                }
                if (settings.haseFile) {
                    options.processData = false;
                    options.contentType = false;
                }
                $.ajax(options);
            });

            $("body").on("click", settings.selectAllSelector, function () {
                $(".checkBox").prop('checked',
                    $(this).prop('checked'));
            });


            $("body").on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItems = [];
                $('input:checkbox.checkBox').each(function () {
                    if ($(this).prop('checked')) {
                        selectedItems.push($(this).val());
                    }
                });


                if (selectedItems.length > 0) {
                    $(settings.deleteModal).modal("show");
                } else {
                    toastr.info("Please select at least one item.", 'Warning');
                }
            });


            $("body").on('show.bs.modal', settings.deleteModal, function (event) {
                var source = $(event.relatedTarget);
                var id = source.data("id");

                var title = source.data("title");
                title = "Are you sure want to delete these items?";
                var modal = $(this);
                $(modal).find('.title').html(title);

                $("body").on("click", settings.finalDeleteSelector, function (e) {
                    e.stopPropagation();
                    e.preventDefault();
                    e.stopImmediatePropagation();

                    $(this).prop("disabled", true);
                    // Delete
                    $.ajax({
                        url: deleteUrl + "/" + selectedItems,
                        method: "POST",
                        success: function (response) {
                            console.log(response);
                            $(modal).modal("hide");
                            if (response.success) {
                                toastr.success(response.message, 'Success');
                                selectedItems = [];
                                /*settings.load(settings.baseUrl);*/
                                loadUserAccesses(settings.baseUrl, settings.gridSelector);
                                loadForm(saveUrl);
                                /*$('#EmployeeId').trigger("change");*/
                            }
                            else {
                                loadUserAccesses(settings.baseUrl, settings.gridSelector);
                                toastr.error(response.message, 'Error');
                                console.log(response);
                            }
                        }
                    });
                });

            }).on('hide.bs.modal', function () {
                $("body").off("click", settings.finalDeleteSelector);
                selectedItems = [];
            });

            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });


            $("body").on("keyup", settings.decimalSelector, function () {
                var self = $(this);
                showDecimalPlaces(self.val(), self.parent().find(".input-group-text"));
            });

            $("body").on("keyup", settings.availabilitySelector, function () {
                var self = $(this);
                let code = $(".js-code").val();
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

            $("body").on("change.getInfo", '#EmployeeId', function () {
                console.log($(this).val());
                console.log($(this).val().length);
                if ($(this).val().length > 0)
                    getEmployeeInfo($(this).val());
                else
                    getEmployeeInfo();
            });

            $("body").on("click.getInfo", '#searchBtn', function () {

                if ($('#Username').val().length > 0)
                    getEmployeeByUsername($('#Username').val());
            });

            $("body").on("click", "#isRequired", function () {
                toggleEmployeeRequired();
            });

            $('#UserType').on('change', function () {
                console.log('test');
                toggleUserTypeSections();
            });
            toggleUserTypeSections();
        });
        function toggleUserTypeSections() {
            var userType = $('#UserType').val();

            if (userType === 'Employee') {
                $('#employeeSection').show();
                $('#customerSection').hide();

                // re-enable employee inputs so they post back / validate
                $('#employeeSection').find('input, select').prop('disabled', false);
                $('#customerSection').find('input, select').prop('disabled', true);
            }
            else if (userType === 'Customer') {
                $('#employeeSection').hide();
                $('#customerSection').show();

                $('#customerSection').find('input, select').prop('disabled', false);
                $('#employeeSection').find('input, select').prop('disabled', true);
            }
            else {
                // nothing selected yet
                $('#employeeSection').hide();
                $('#customerSection').hide();

                $('#employeeSection').find('input, select').prop('disabled', true);
                $('#customerSection').find('input, select').prop('disabled', true);
            }
        }

        // run on load (handles edit mode where UserType is already set)


        // Select2 fires a native 'change' event on the underlying <select>,
        // so this also fires when the user picks a value from the select2 dropdown.
        
        function getEmployeeInfo(id) {

            $.ajax({
                type: "GET",
                url: settings.baseUrl + "/GetEmployee/",
                data: { userId: id },
                success: function (data) {
                    console.log(data);
                    $(settings.formContainer).empty();
                    $(settings.formContainer).html(data);
                    $.validator.unobtrusive.parse($(settings.formSelector));


                    initialize();
                }
            });
        };
        function validation() {
            var isRequired = $("#isRequired").is(":checked");
            var employeeId = $("#EmployeeId").val();

            if (isRequired && !employeeId) {
                toastr.warning("Employee is required!", 'Warning');
                $("EmployeeId").focus();
                return false;
            }

            var username = $("#Username").val().trim();
            if (!username) {
                toastr.warning("Username is required!", 'Warning');
                $("#Username").focus();
                return false;
            }

            var userpassword = $("#UserPassword").val().trim();
            if (!userpassword) {
                toastr.warning("Password is required!", 'Warning');
                $("#UserPassword").focus();
                return false;
            }

            var role = $("#Role").val().trim();
            if (!role) {
                toastr.warning("Role is required!", 'Warning');
                $("#Role").focus();
                return false;
            }

            var accessCode = $("#AccessCode").val().trim();
            if (!accessCode) {
                toastr.warning("Access code is required!", 'warning');
                $("#AccessCode").focus();
                return false;
            }

            return true;
        }
        function getEmployeeByUsername(id) {
            $.ajax({
                type: "GET",
                url: settings.baseUrl + "/GetEmployeeByUser/",
                data: { username: id },
                success: function (data) {
                    console.log(data);
                    $(settings.formContainer).empty();
                    $(settings.formContainer).html(data);
                    $.validator.unobtrusive.parse($(settings.formSelector));


                    initialize();
                }
            });
        };

        function loadUserAccesses(baseUrl, gridSelector) {
            var dataTable = $(gridSelector).DataTable({
                ajax: {
                    url: baseUrl + "/grid",
                    type: "GET",
                    datatype: "json"
                },

                columnDefs: [
                    {
                        targets: [0], orderable: false,
                        createdCell: function (td) {
                            $(td).html(`
                        <div class="d-flex align-items-center justify-content-center px-0">
                            ${$(td).html()}
                        </div>
                    `);
                        }
                    },
                ],
                columns: [
                    {
                        data: "id",
                        className: "text-center align-middle px-0",
                        render: function (data) {
                            return `
                                <div class="form-check d-inline-block m-0">
                                    <input type="checkbox" class="checkBox form-check-input" value="${data}">
                                </div>
                            `;
                        }
                    },
                    {
                        data: "employeeId",
                        className: "align-middle",
                        render: function (data, type, row) {
                            return `<a class="btn js-useraccess-edit px-0" data-id="${row.id}"><i>${data}</i></a>`;
                        }
                    },
                    { data: "employeeName", className: "align-middle" },
                    { data: "username", className: "align-middle" },
                    { data: "role", className: "align-middle" },
                    { data: "accessCode", className: "align-middle" }
                ],
                lengthChange: true,
                pageLength: 10,
                lengthMenu: [[10, 25, 50, 100, 1000, -1], [10, 25, 50, 100, 1000, "All"]],
                autowidth: false,
                responsive: true,
                fixedHeader: false,
                order: [[1, "Desc"]],
                sScrollY: "100%",
                scrollX: true,
                sScrollX: "100%",
                bDestroy: true
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
                        console.log(error);
                        reject(error)
                    },
                })
            })
        }

        function initialize() {

            showDecimalPlaces($(settings.decimalSelector).val(), $(settings.decimalSelector).parent().find(".input-group-text"));

            // Password toggle init
            const passwordInput = document.getElementById('UserPassword');
            const toggleBtn = document.getElementById('toggleBtn');
            const eyeIcon = document.getElementById('eyeIcon');

            if (passwordInput && toggleBtn) {
                function togglePassword() {
                    const cursorPos = passwordInput.selectionStart;
                    if (passwordInput.type === 'password') {
                        passwordInput.type = 'text';
                        eyeIcon.classList.replace('fa-eye', 'fa-eye-slash');
                    } else {
                        passwordInput.type = 'password';
                        eyeIcon.classList.replace('fa-eye-slash', 'fa-eye');
                    }
                    passwordInput.setSelectionRange(cursorPos, cursorPos);
                    passwordInput.focus();
                }

                toggleBtn.addEventListener('click', togglePassword);
                toggleBtn.addEventListener('mousedown', (e) => e.preventDefault());
            }

            // initInlineSearchSelect2('#EmployeeId');
            // initInlineSearchSelect2('#Role');
            // initInlineSearchSelect2('#AccessCode');
            // initInlineSearchSelect2('#SingleSession');

            if (typeof $.fn.multiselect !== 'undefined') {
                // bsms_InitializeMultiselects({
                //     '#AccessPermissionCompanyCode': 'Select Company',
                //     '#AccessPermissionBranchCode': 'Select Branch'
                // });
            } else {
                console.error('Bootstrap Multiselect not loaded!');
            }
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

        function toggleEmployeeRequired() {
            const $checkbox = $("#isRequired");
            const $employee = $("#EmployeeId");

            if ($checkbox.is(":checked")) {
                $employee.prop("disabled", false);
                $employee.rules("add", {
                    required: true,
                    messages: { required: "Employee is required." }
                });
            } else {
                let userId = $("input[name='UserId']").val() ?? 0;

                loadForm(settings.baseUrl +"/Setup/0")
                    .then(() => {
                        $("#isRequired").prop("checked", false);
                        $("#EmployeeId").prop("disabled", true);
                        $("#EmployeeId").rules("remove", "required");
                    });
            }
        }

        //function toggleEmployeeRequired() {
        //    const $employee = $("#EmployeeId");

        //    if ($("#isRequired").is(":checked")) {
        //        $employee.prop("disabled", false).trigger("change");
        //        $employee.rules("add", {
        //            required: true,
        //            messages: { required: "Employee is required." }
        //        });
        //    } else {
        //        loadForm(saveUrl + "/" + 0);
        //    }
        //}
    }


    function gcDestroySelect2($el) {
        try {
            if ($el.data('select2')) $el.select2('destroy');
        } catch (e) { }

        $el.siblings('.select2-container').remove();
        $el.nextAll('.select2-container').remove();

        $el.removeData('gcSelect2Init');
    }

    function initInlineSearchSelect2(selectorOrElements) {

        const $targets = (selectorOrElements instanceof jQuery)
            ? selectorOrElements
            : $(selectorOrElements || '.gc-select2');

        $targets.each(function () {

            const $el = $(this);

            if ($el.data('gcSelect2Init') === true && $el.data('select2')) return;

            if ($el.data('select2') || $el.siblings('.select2-container').length || $el.nextAll('.select2-container').length) {
                gcDestroySelect2($el);
            }

            const placeholder = $el.data('placeholder') || $el.attr('data-placeholder') || 'Search...';

            const $wrap =
                $el.closest('.dropdown-wrapper-select2').length ? $el.closest('.dropdown-wrapper-select2')
                    : $el.closest('.gc-inline-select2').length ? $el.closest('.gc-inline-select2')
                        : $el.closest('.input-group').length ? $el.closest('.input-group')
                            : $(document.body);

            $el.select2({
                width: '100%',
                dropdownParent: $wrap,
                allowClear: true,
                minimumResultsForSearch: 0,
                placeholder: { id: '', text: placeholder }
            });

            $el.data('gcSelect2Init', true);

            $el.off('.gcInline');

            $el.on('select2:open.gcInline', function () {

                const s2 = $el.data('select2');
                if (!s2) return;

                const $container = s2.$container;
                const $dropdown = s2.$dropdown;
                if (!$container || !$dropdown) return;

                const $selection = $container.find('.select2-selection--single');
                const $searchWrap = $dropdown.find('.select2-search--dropdown');
                const $searchField = $searchWrap.find('.select2-search__field');

                $selection.addClass('gc-inline-search-active');

                $searchWrap
                    .addClass('gc-inline-search')
                    .appendTo($selection);


                $searchField.attr('placeholder', placeholder);
                //New from here
                $searchField.off('keydown.gcSpace').on('keydown.gcSpace', function (e) {
                    if (e.key === ' ' || e.keyCode === 32) {
                        e.stopPropagation(); // prevent Select2 from eating it
                        // manually insert a space at cursor position
                        var el = this;
                        var start = el.selectionStart;
                        var end = el.selectionEnd;
                        var val = el.value;
                        el.value = val.substring(0, start) + ' ' + val.substring(end);
                        el.selectionStart = el.selectionEnd = start + 1;
                        $(el).trigger('input');
                        e.preventDefault();
                    }
                });
                //
                $searchField.val('');
                $searchField[0].style.Width = '100%';

                setTimeout(function () {
                    $searchField.trigger('focus');
                }, 0);
            });

            $el.on('select2:close.gcInline', function () {

                const s2 = $el.data('select2');
                if (!s2) return;

                const $container = s2.$container;
                const $dropdown = s2.$dropdown;
                if (!$container || !$dropdown) return;

                const $selection = $container.find('.select2-selection--single');
                const $searchWrap = $selection.find('.select2-search--dropdown.gc-inline-search');

                $selection.removeClass('gc-inline-search-active');

                if ($searchWrap.length) {
                    $searchWrap
                        .removeClass('gc-inline-search')
                        .prependTo($dropdown);
                }
            });
        });
    }


}(jQuery));
