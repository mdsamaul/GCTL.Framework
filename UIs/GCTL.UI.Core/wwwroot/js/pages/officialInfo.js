
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
                    loadTableData(null); 
                    clear();
                    return;
                }
                loadTableData(selectedEmployee); 
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
                           // console.error('No response received.');
                            return;
                        }

                        var data = response.data || response || {};
                        
                        if (data) {
                            populateFormFromData(data, response.autoId);
                        }
                    },
                    error: function () {
                        _suppressEmployeeChange = false;
                        console.error('Failed to fetch employee details.');
                        toastr.error('Failed to load employee details.');
                    }
                });
            }

            // ── Extract populate logic to a reusable function ──────────────────────
            function populateFormFromData(data, autoIdParam) {
                
                function getProp(obj, propName) {
                    if (!obj) return undefined;
                    var key = Object.keys(obj).find(k => k.toLowerCase() === propName.toLowerCase());
                    return key ? obj[key] : undefined;
                }

                _suppressEmployeeChange = true;
               // console.log(data);
                // Text Inputs
                var autoId = autoIdParam || getProp(data, 'autoId') || '0';
                window.setField('#AutoId', autoId);
                
                var firstName = getProp(data, 'firstName') || '';
                var lastName = getProp(data, 'lastName') || '';
                var fallbackName = (firstName + ' ' + lastName).trim();
                window.setField('#FullName', getProp(data, 'employeeName') || getProp(data, 'name') || getProp(data, 'fullName') || fallbackName || '');
                
                window.setField('#MobileNo', getProp(data, 'officialPhone') || getProp(data, 'mobileNo') || '');
                window.setField('#Email', getProp(data, 'officialEmail') || getProp(data, 'email') || '');
                window.setField('#PayId', getProp(data, 'payId') || '');
                window.setField('#AttendenceId', getProp(data, 'attendenceId') || '');
                window.setField('#DesignationName', getProp(data, 'designationName') || '');
                window.setField('#DepartmentName', getProp(data, 'departmentName') || '');
                window.setField('#ProbationPeriod', getProp(data, 'probationPeriod') || '');
                window.setField('#AppointmentLetterNo', getProp(data, 'appointmentLetterNo') || '');
                window.setField('#ConfirmationRefNo', getProp(data, 'confirmationRefNo') || '');
                window.setField('#JoiningSalary', getProp(data, 'joiningSalary') || '');
                
                if (getProp(data, 'clientId')) {
                    window.setField('#ClientId', getProp(data, 'clientId'));
                }
                
                // Select2 Dropdowns
                window.setField('#EmployeeId', getProp(data, 'employeeId') || getProp(data, 'employeeID'));
                window.setField('#OfficialInfoCompanyCode', getProp(data, 'officialInfoCompanyCode') || getProp(data, 'companyCode'));
                window.setField('#OfficialInfoBranchCode', getProp(data, 'officialInfoBranchCode') || getProp(data, 'branchCode'));
                window.setField('#DepartmentCode', getProp(data, 'departmentCode'));
                window.setField('#DesignationCode', getProp(data, 'designationCode'));
                window.setField('#EmpTypeCode', getProp(data, 'empTypeCode') || getProp(data, 'employeeTypeCode'));
                window.setField('#EmploymentNatureId', getProp(data, 'employmentNatureId'));
                window.setField('#GradeCode', getProp(data, 'gradeCode'));
                window.setField('#ShiftCode', getProp(data, 'shiftCode'));
                window.setField('#ReportingTo', getProp(data, 'reportingTo') || getProp(data, 'immediateSupervisorID') || getProp(data, 'immediateSupervisorId'));
                window.setField('#Hod', getProp(data, 'hod') || getProp(data, 'headOfDepartmentID') || getProp(data, 'headOfDepartmentId'));
                window.setField('#EmployeeStatus', getProp(data, 'employeeStatus') || getProp(data, 'employeeStatusId'));
                
                window.setField('#IsLunchBilEligible', getProp(data, 'isLunchBilEligible'));
                window.setField('#IsOverTimeEligible', getProp(data, 'isOverTimeEligible'));
                window.setField('#IsExtraDutyEligible', getProp(data, 'isExtraDutyEligible'));
                window.setField('#IsGovtHolidayEligible', getProp(data, 'isGovtHolidayEligible'));
                window.setField('#IsAttendanceBonusEligible', getProp(data, 'isAttendanceBonusEligible'));

                // Dates
                flatpickrHelper.setDate('#JoiningDate', getProp(data, 'joiningDate'));
                flatpickrHelper.setDate('#ConfirmeDate', getProp(data, 'confirmeDate') || getProp(data, 'confirmationDate'));
                flatpickrHelper.setDate('#AppointmentLetterDate', getProp(data, 'appointmentLetterDate'));
                flatpickrHelper.setDate('#ContractEndDate', getProp(data, 'contractEndDate'));

                // Update Save button label
                autoId = parseInt(autoId) || 0;
                if (autoId > 0) {
                    $(settings.saveSelector).html('<i class="fas fa-save"></i> Update');
                } else {
                    $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
                }
                _suppressEmployeeChange = false;
            }

            // ── Fetch Full Official Info ───────────────────────────────────────────
            // (Removed fetchAndPopulateOfficialInfo because it only fetched partial grid data.
            // Using fetchAndPopulateEmployee instead, which fetches the full JSON details).

            // ── Helpers ────────────────────────────────────────────────────────────

            // ── EDIT button click ──────────────────────────────────────────────────
            $(document).on('click', '.js-officialInfo-Edit-confirm, .js-grid-edit', function () {
                var employeeId = $(this).attr('data-id');
               // console.log('Edit clicked, employeeId:', employeeId);
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

                _suppressEmployeeChange = false;

                // Directly call fetchAndPopulateEmployee to get full details (instead of partial grid data)
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
                    toastr.warning('Please select an Employee');
                    validateEmployeeDD();
                    return false;
                }
                if (!company) {
                    toastr.warning('Please select a Company');
                    validateCompanyCodeDD();
                    return false;
                }
                if (!department) {
                    toastr.warning('Please select a Department');
                    validateDepartmentCodeDD();
                    return false;
                }
                if (!designation) {
                    toastr.warning('Please select a Designation');
                    validateDesignationCodeDD();
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
                    if (typeof getSelectedGridIds !== 'undefined') {
                        selectedItems = getSelectedGridIds('officialInfoTable');
                    } else {
                        $('.grid-row-checkbox:checked, .checkbox:checked').each(function () {
                            var val = $(this).val();
                            if (val) selectedItems.push(val);
                        });
                    }
                }

                if (selectedItems.length > 0) {
                    var $modal = getDeleteModal();
                    $modal.find('.title').html("Are you sure to delete");
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
                            $('.grid-row-checkbox, .checkbox, .grid-select-all').prop('checked', false);
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
                       // console.log(response);
                        $.each(response, function (i, item) {
                            $(target).append($('<option>', { value: item.code, text: item.name }));
                        });
                        $(target).val(lastCode);
                       // console.log("Test", lastCode);
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

