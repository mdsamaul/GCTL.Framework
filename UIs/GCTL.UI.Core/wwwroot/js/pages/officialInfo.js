
(function ($) {
    $.officialInfo = function (options) {

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#officialInfo-form",
            gridSelector: "#officialInfo-grid",
            gridContainer: ".js-officialInfo-grid-container",
            editSelector: ".js-officialInfo-edit",
            saveSelector: ".js-officialInfo-save",
            selectAllSelector: "#officialInfo-check-all",
            deleteSelector: ".js-officialInfo-delete-confirm",
            deleteModal: "#officialInfo-delete-modal",
            finalDeleteSelector: ".js-officialInfo-delete",
            clearSelector: ".js-officialInfo-clear",
            topSelector: ".js-go",
            showNagativeFormat: false,
            haseFile: false,
            quickAddSelector: ".js-quick-add-officialInfo",
            quickAddModalOfficialInfo: "#quickAddModal-officialInfo",
            load: function () { }
        }, options);

        var gridUrl = settings.baseUrl + "/GetAll";
        var saveUrl = settings.baseUrl + "/setup";
        var deleteUrl = settings.baseUrl + "/Delete";

        // FLAG: prevents #EmployeeId change from firing during programmatic set
        var _suppressEmployeeChange = false;

        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(window).on("load", function () {
            $("#customLoadingOverlay").fadeOut(300);
        });

        $(() => {

            initialize();
            loadTableData(null);

            // ── #EmployeeId change: only fires on real user selection ──────────────
            //$('#EmployeeId').on('change', function () {
            //    if (_suppressEmployeeChange) return;
            //    var selectedEmployee = $(this).val();
            //    if (!selectedEmployee) return;
            //    fetchAndPopulateEmployee(selectedEmployee);
            //});
            $('#EmployeeId').on('change', function () {
                if (_suppressEmployeeChange) return;
                var selectedEmployee = $(this).val();
                if (!selectedEmployee) {
                    loadTableData(null); // employee clear হলে সব দেখাবে
                    clear();
                    return;
                }
                loadTableData(selectedEmployee); // ✅ grid filter করবে
                fetchAndPopulateEmployee(selectedEmployee);
            });

            // ── Core populate function ─────────────────────────────────────────────
            function fetchAndPopulateEmployee(employeeId) {
                $.ajax({
                    url: '/OfficialInfo/GetEmployeeDetailsByCode',
                    type: 'GET',
                    data: { code: employeeId },
                    success: function (response) {
                        if (!response) {
                            console.error('No response received.');
                            return;
                        }

                        var data = response.data || response || {};
                      //  console.log('Employee details response:', data);
                        if (data) {
                            _suppressEmployeeChange = true;

                            // Text Inputs
                            setField('#AutoId', response.autoId || '0');
                            setField('#FullName', data.employeeName || '');
                            setField('#MobileNo', data.officialPhone || '');
                            setField('#Email', data.officialEmail || '');
                            setField('#PayId', data.payId || '');
                            setField('#AttendenceId', data.attendenceId || '');
                            setField('#DesignationName', data.designationName || '');
                            setField('#DepartmentName', data.departmentName || '');
                            setField('#ProbationPeriod', data.probationPeriod || '');
                            setField('#AppointmentLetterNo', data.appointmentLetterNo || '');
                            setField('#ConfirmationRefNo', data.confirmationRefNo || '');
                            setField('#JoiningSalary', data.joiningSalary || '');
                           
                            if (data.clientId) {
                                setSelect('#ClientId', data.clientId);
                            }
                            
                            // Select2 Dropdowns
                            setSelect('#EmployeeId', data.employeeID);
                            setSelect('#OfficialInfoCompanyCode', data.companyCode);
                            setSelect('#OfficialInfoBranchCode', data.branchCode);
                            setSelect('#DepartmentCode', data.departmentCode);
                            setSelect('#DesignationCode', data.designationCode);
                            setSelect('#EmpTypeCode', data.employeeTypeCode);
                            setSelect('#EmploymentNatureId', data.employmentNatureId);
                            setSelect('#GradeCode', data.gradeCode);
                            setSelect('#ShiftCode', data.shiftCode);
                            setSelect('#ReportingTo', data.immediateSupervisorID);
                            setSelect('#Hod', data.headOfDepartmentID);
                            setSelect('#EmployeeStatus', data.employeeStatusId);
                            
                            setSelect('#IsLunchBilEligible', data.isLunchBilEligible);
                            setSelect('#IsOverTimeEligible', data.isOverTimeEligible);
                            setSelect('#IsExtraDutyEligible', data.isExtraDutyEligible);
                            setSelect('#IsGovtHolidayEligible', data.isGovtHolidayEligible);
                            setSelect('#IsAttendanceBonusEligible', data.isAttendanceBonusEligible);

                            // Dates
                            setDate('#JoiningDate', data.joiningDate);
                            setDate('#ConfirmeDate', data.confirmationDate);
                            setDate('#AppointmentLetterDate', data.appointmentLetterDate);
                            setDate('#ContractEndDate', data.contractEndDate);

                            // Update Save button label
                            var autoId = parseInt(response.autoId) || 0;
                            if (autoId > 0) {
                                $(settings.saveSelector).html('<i class="fas fa-save"></i> Update');
                            } else {
                                $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
                            }
                            _suppressEmployeeChange = false;
                        }
                    },
                    error: function () {
                        _suppressEmployeeChange = false;
                        console.error('Failed to fetch employee details.');
                        toastr.error('Failed to load employee details.');
                    }
                });
            }

            // ── Helpers ────────────────────────────────────────────────────────────
            
            function setField(selector, value, isHtml = false) {
                if (isHtml) {
                    $(selector).html(value);
                } else {
                    $(selector).val(value);
                }
            }

            function setSelect(selector, value) {
                if (value === null || value === undefined) return;
                var $el = $(selector);
                if ($el.length === 0) return;
                
                if ($el.hasClass('select2-hidden-accessible')) {
                    $el.val(value).trigger('change.select2');
                } else {
                    $el.val(value).trigger('change');
                }
            }

            function setDate(selector, dateStr) {
                if (!dateStr) return;
                var el = document.querySelector(selector);
                if (!el) return;
                if (el._flatpickr) {
                    el._flatpickr.setDate(toFlatpickrFormat(dateStr), true);
                } else {
                    $(selector).val(toFlatpickrFormat(dateStr));
                }
            }

            function toFlatpickrFormat(dateStr) {
                if (!dateStr) return '';
                var parts = dateStr.split('/');
                if (parts.length === 3) {
                    return parts[1] + '-' + parts[0] + '-' + parts[2]; // DD/MM/YYYY -> MM-DD-YYYY
                }
                return dateStr;
            }

            // ── EDIT button click ──────────────────────────────────────────────────
            $(document).on('click', '.js-officialInfo-Edit-confirm', function () {
                var employeeId = $(this).attr('data-id');
                console.log('Edit clicked, employeeId:', employeeId);
                if (!employeeId) return;

                var $employeeDD = $('#EmployeeId');

                _suppressEmployeeChange = true;

                if ($employeeDD.find('option[value="' + employeeId + '"]').length > 0) {
                    $employeeDD.val(employeeId).trigger('change.select2');
                } else {
                    var newOption = new Option(employeeId, employeeId, true, true);
                    $employeeDD.append(newOption).trigger('change.select2');
                }

                _suppressEmployeeChange = false;

                // Directly call populate — do NOT trigger 'change'
                fetchAndPopulateEmployee(employeeId);
                  loadTableData(employeeId); 
                // Scroll to form top
                $('html, body').animate({
                    scrollTop: $(settings.formSelector).offset().top - 20
                }, 400);
            });

            // ── Save button ────────────────────────────────────────────────────────
            $('body').on('click', settings.saveSelector, function () {
                if (!validation()) return;

                validateEmployeeDD();
                validateCompanyCodeDD();
                validateDepartmentCodeDD();
                validateDesignationCodeDD();

                var autoId = parseInt($('#AutoId').val()) || 0;
                if (autoId === 0) {
                    checkExistsBeforeSave();
                } else {
                    $(settings.formSelector).submit();
                }
            });

            function checkExistsBeforeSave() {
                var code = $('#EmployeeId').val();
                $.ajax({
                    method: 'GET',
                    data: { code: code },
                    url: '/OfficialInfo/IsExistsByCode',
                    success: function (response) {
                        if (response.isSuccess) {
                            toastr.info(response.message);
                        } else {
                            $(settings.formSelector).submit();
                        }
                    },
                    error: function () {
                        $(settings.formSelector).submit();
                    }
                });
            }

            // ── Form submit ────────────────────────────────────────────────────────
            $('body').on('submit', settings.formSelector, function (e) {
                e.preventDefault();

                var form = $(this)[0];
                var formData = new FormData(form);
                var actionUrl = $(this).attr('action');

                $.ajax({
                    type: 'POST',
                    url: actionUrl,
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function (result) {
                        if (result.noSavePermission || result.noUpdatePermission || result.isDuplicate) {
                            toastr.info(result.message);
                        } else if (result.isSuccess) {
                            toastr.success(result.message, 'Success');
                            setTimeout(function () {
                                loadTableData(result.lastCode);
                            }, 1000);
                        } else {
                            $(settings.formSelector).html(result);
                        }
                    },
                    error: function () {
                        toastr.error('Failed Insert.');
                        loadTableData(null);
                    }
                });
            });

            // ── Validation ─────────────────────────────────────────────────────────
            function validation() {
                var employee = $('#EmployeeId').val();
                var company = $('#OfficialInfoCompanyCode').val();
                var department = $('#DepartmentCode').val();
                var designation = $('#DesignationCode').val();

                if (!employee) {
                    toastr.info('Select Employee');
                    $('#EmployeeId').select2('open');
                    return false;
                }
                if (!company) {
                    toastr.info('Select Company');
                    $('#OfficialInfoCompanyCode').select2('open');
                    return false;
                }
                if (!department) {
                    toastr.info('Select Department');
                    $('#DepartmentCode').select2('open');
                    return false;
                }
                if (!designation) {
                    toastr.info('Select Designation');
                    $('#DesignationCode').select2('open');
                    return false;
                }
                return true;
            }

            $('#EmployeeId').on('change blur', function () { validateEmployeeDD(); });
            $('#OfficialInfoCompanyCode').on('change blur', function () { validateCompanyCodeDD(); });
            $('#DepartmentCode').on('change blur', function () { validateDepartmentCodeDD(); });
            $('#DesignationCode').on('change blur', function () { validateDesignationCodeDD(); });

            function validateEmployeeDD() {
                var val = $('#EmployeeId').val();
                $('#EmployeeId').next('.select2-container')
                    .css('border', (!val || val.trim() === '') ? '1px solid red' : '');
            }
            function validateCompanyCodeDD() {
                var val = $('#OfficialInfoCompanyCode').val();
                $('#OfficialInfoCompanyCode').next('.select2-container')
                    .css('border', (!val || val.trim() === '') ? '1px solid red' : '');
            }
            function validateDepartmentCodeDD() {
                var val = $('#DepartmentCode').val();
                $('#DepartmentCode').next('.select2-container')
                    .css('border', (!val || val.trim() === '') ? '1px solid red' : '');
            }
            function validateDesignationCodeDD() {
                var val = $('#DesignationCode').val();
                $('#DesignationCode').next('.select2-container')
                    .css('border', (!val || val.trim() === '') ? '1px solid red' : '');
            }

            // ── Select All ─────────────────────────────────────────────────────────
            $("body").on('click', settings.selectAllSelector, function () {
                $('.checkbox').prop('checked', $(this).prop('checked'));
            });

            // ── Delete confirmation ────────────────────────────────────────────────
            function getDeleteModal() {
                var $m = $(settings.deleteModal);
                if ($m.length) return $m;
                $m = $("#deleteModal");
                if ($m.length) return $m;
                return $("#gcDeleteModal");
            }

            $("body").on("click", settings.deleteSelector + ", .js-grid-delete", function (e) {
                e.preventDefault();
                selectedItems = [];
                if ($(this).attr('data-id')) {
                    selectedItems.push($(this).attr('data-id'));
                } else {
                    $('.checkbox:checked').each(function () {
                        var val = $(this).val();
                        if (val) selectedItems.push(val);
                    });
                }

                if (selectedItems.length > 0) {
                    var $modal = getDeleteModal();
                    $modal.find('.title').html("Are you sure you want to delete these item(s)? This process cannot be undone.");
                    $modal.modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }
            });

            // ── Final delete ───────────────────────────────────────────────────────
            $("body").on("click", `${settings.finalDeleteSelector}, .js-delete, .js-gc-delete-confirm`, function (e) {
                if (!selectedItems || selectedItems.length === 0) return;
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                var $modal = getDeleteModal();
                $.ajax({
                    type: 'POST',
                    url: '/OfficialInfo/Delete',
                    contentType: 'application/json',
                    data: JSON.stringify(selectedItems),
                    success: function (result) {
                        $modal.modal("hide");
                        if (result.isSuccess || result.success) {
                            toastr.success(result.message);
                            selectedItems = [];
                            $('.checkbox').prop('checked', false);
                            var employeeId = $("#EmployeeId").val();
                            loadTableData(employeeId || null);
                        } else {
                            toastr.error(result.message, 'Error');
                        }
                    },
                    error: function (xhr) {
                        $modal.modal("hide");
                        toastr.error(xhr.responseJSON?.message || "An error occurred.");
                    }
                });
            });

            // ── Clear ──────────────────────────────────────────────────────────────
            $('.js-officialInfo-clear').on('click', function () {
                clear();
            });

            function clear() {
                $('#officialInfo-form')[0].reset();
                $('#AutoId').val('0');
                $('#ClientId').val('');
                if ($.fn.multiselect) {
                    $('#ClientId').multiselect('refresh');
                }

                _suppressEmployeeChange = true;
                $(".selectpickerOfficialInfo").val(null).trigger("change");
                _suppressEmployeeChange = false;

                $('.officialInfoDatepicker').val('');
                $('#LdateModifyDate').hide();
                $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
                $('.text-danger').text('');
                $('.selectpickerOfficialInfo').next('.select2-container').css('border', '');

                history.replaceState(null, '', '/OfficialInfo/Setup');

                _suppressEmployeeChange = true;
                var companyOptions = $('#OfficialInfoCompanyCode option');
                if (companyOptions.length === 2) {
                    $('#OfficialInfoCompanyCode').val(companyOptions.eq(1).val()).trigger("change");
                } else {
                    $('#OfficialInfoCompanyCode').val('001').trigger("change");
                }
                _suppressEmployeeChange = false;
                loadTableData(null);
            }

            // ── Quick Add modal ────────────────────────────────────────────────────

            let loadUrl, target, reloadUrl, title, lastCode;

            $("body").on("click", settings.quickAddSelector, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                loadUrl = $(this).data("url");
                target = $(this).data("target");
                reloadUrl = $(this).data("reload-url");
                title = $(this).data("title");

                $(settings.quickAddModalOfficialInfo + " .modal-title").html(title);
                $(settings.quickAddModalOfficialInfo + " .modal-body").empty();
                $(settings.quickAddModalOfficialInfo + " .modal-body").load(loadUrl, function () {
                    $(settings.quickAddModalOfficialInfo).modal("show");
                    $("#header").hide();
                    $(settings.quickAddModalOfficialInfo + " .modal-body #header").hide();
                    $("#left_menu").hide();
                    $(settings.quickAddModalOfficialInfo + " .modal-body #left_menu").hide();
                    $("#main-content").toggleClass("collapse-main");
                    $(settings.quickAddModalOfficialInfo + " .modal-body #main-content").toggleClass("collapse-main");
                    $("body").removeClass("sidebar-mini");
                });
            });

            $("body").on("click", ".js-modal-dismiss-officialInfo", function () {
                $("body").removeClass("sidebar-mini").addClass("sidebar-mini");
                $("#header").show();
                $(settings.quickAddModalOfficialInfo + " .modal-body #header").show();
                $("#left_menu").show();
                $(settings.quickAddModalOfficialInfo + " .modal-body #left_menu").show();
                $("#main-content").toggleClass("collapse-main");
                $(settings.quickAddModalOfficialInfo + " .modal-body #main-content").toggleClass("collapse-main");

                lastCode = $(settings.quickAddModalOfficialInfo + " #lastCode").val();
                $(settings.quickAddModalOfficialInfo + " .modal-body").empty();
                $(settings.quickAddModalOfficialInfo).modal("hide");

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

            // Initialize Custom DataTable from View Model
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'officialInfoTable' });
            }

        }); 

        // ── Initialize Select2 ─────────────────────────────────────────────────────
        function initialize() {
            $(document).ready(async function () {
                AllDatePicker();             

            });
        }

        // ── Flatpickr init ─────────────────────────────────────────────────────────

        function AllDatePicker() {
            // flatpickr(
            //     document.querySelectorAll(
            //         "#JoiningDate,            #ConfirmeDate, " +
            //         "#AppointmentLetterDate,  #ContractEndDate"
            //     ),
            //     CalendarService.createConfig({ dateFormat: "m-d-Y" })
            // );
        }

        // ── Load grid ──────────────────────────────────────────────────────────────
        function loadTableData(employeeId) {
            var url = settings.baseUrl + '/GetTableData';
            if (employeeId) {
                url += '?employeeId=' + employeeId;
            }
            if ($.gctlGrid) {
                var table = $.gctlGrid.getTable('officialInfoTable');
                if (table) {
                    table.ajax.url(url).load();
                } else {
                    $.gctlGrid.initFromModel({ gridId: 'officialInfoTable' });
                }
            }
        }

    };

}(jQuery));

