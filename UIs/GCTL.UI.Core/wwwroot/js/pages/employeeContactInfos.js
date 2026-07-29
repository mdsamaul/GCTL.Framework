
(function ($) {
    $.employeeContactInfos = function (options) {

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#EmployeeContactInfos-form",
            formContainer: ".js-EmployeeContactInfos-form-container",
            gridSelector: "#EmployeeContactInfos-grid",
            gridContainer: ".js-EmployeeContactInfos-grid-container",
            editSelector: ".js-EmployeeContactInfos-edit",
            saveSelector: ".js-EmployeeContactInfos-save",
            selectAllSelector: "#EmployeeContactInfos-check-all",
            deleteSelector: ".js-EmployeeContactInfos-delete-confirm",
            deleteModal: "#EmployeeContactInfos-delete-modal",
            finalDeleteSelector: ".js-EmployeeContactInfos-delete",
            clearSelector: ".js-EmployeeContactInfos-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-EmployeeContactInfos-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-EmployeeContactInfos-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            load: function () { }
        }, options);

        var baseControllerNameUrl = "/EmployeeContactInfos";
        var testSave = baseControllerNameUrl + "/Setup";
        var nextCodeULR = baseControllerNameUrl + "/GenerateNextCode";
        var loadTableURL = baseControllerNameUrl + "/GetTableData";
        var deleteURL = baseControllerNameUrl + "/Delete";
        var getById = baseControllerNameUrl + "/Setup";

        // ── State flags ──────────────────────────────────────────────────────────
        var _pendingEmployeeRestore = null;
        var _suppressEmployeeChange = false;

        // ── Tooltip / overlay ────────────────────────────────────────────────────
        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });
        $(window).on("load", function () {
            $("#customLoadingOverlay").fadeOut(300);
        });

        $(() => {

            initialize();

            // ── Edit Button ──────────────────────────────────────────────────────          
            $("body").on('click', settings.editSelector, function () {
                var id = $(this).data('id');
                var employeeId = $(this).data('employee-id');

                $.ajax({
                    url: getById,
                    type: "GET",
                    data: { id: id },
                    success: function (result) {
                        var $form = $(settings.formSelector);
                        $form.html($(result).find(settings.formSelector).html());
                        $form.attr('action', testSave);

                        // ── HTML load হওয়ার পর hidden field থেকে value নাও ──
                        var branchId = $('#SelectedBranchCode').val() || "";
                        var branchText = $('#SelectedBranchName').val() || "";
                        var empId = $('#EmployeeId').val() || employeeId || "";
                        var empText = $('#EmployeeId option:selected').text() || "";

                        _pendingEmployeeRestore = { empId, empText, branchId, branchText };

                        // Select2 init
                        s2_InitSingle("#EmployeeId", "/GcFilters/employee", "Select Employee", null);
                        s2_InitSingle("#BranchCode", "/GcFilters/branch", "Select Branch", "branch");

                        // BranchCode set
                        if (branchId) {
                            if ($('#BranchCode').find('option[value="' + branchId + '"]').length === 0) {
                                $('#BranchCode').append(new Option(branchText, branchId, true, true));
                            }
                            $('#BranchCode').val(branchId).trigger('change.select2');
                            setTimeout(function () { _restoreEmployee(); }, 300);
                        } else {
                            _restoreEmployee();
                        }

                        loadTableData(empId || employeeId);
                        initializeMultiselects && initializeMultiselects('.gc-multiselect');
                        initInlineSearchSelect2 && initInlineSearchSelect2('.gc-select2');
                        ConvertToBangla();
                    },
                    error: function () { toastr.error('Failed Update.'); }
                });
            });

            // ── BranchCode Change (user-initiated only) ──────────────────────────
            $('body').on('change', '#BranchCode', function () {
                if (_suppressEmployeeChange) return;
                if (_pendingEmployeeRestore) {
                    var restore = _pendingEmployeeRestore;
                    _pendingEmployeeRestore = null;
                    setTimeout(function () {
                        if (restore.empId) {
                            if ($('#EmployeeId').find('option[value="' + restore.empId + '"]').length === 0) {
                                $('#EmployeeId').append(new Option(restore.empText, restore.empId, true, true));
                            }
                            $('#EmployeeId').val(restore.empId).trigger('change.select2');
                        }
                    }, 300);
                }
            });

            // ── Restore Employee helper (edit flow) ──────────────────────────────
            function _restoreEmployee() {
                if (!_pendingEmployeeRestore) return;
                var restore = _pendingEmployeeRestore;
                _pendingEmployeeRestore = null;
                if (restore.empId) {
                    if ($('#EmployeeId').find('option[value="' + restore.empId + '"]').length === 0) {
                        $('#EmployeeId').append(new Option(restore.empText, restore.empId, true, true));
                    }
                    $('#EmployeeId').val(restore.empId).trigger('change.select2');
                }
            }

            // ── Save Button ──────────────────────────────────────────────────────
            $("body").on('click', settings.saveSelector, function () {
                if (!validation()) return;
                $(settings.formSelector).submit();
            });

            // ── Form Submit ──────────────────────────────────────────────────────
            $("body").on('submit', settings.formSelector, function (e) {
                e.preventDefault();
                var formData = new FormData($(this)[0]);
                var actionUrl = $(this).attr('action');

                $.ajax({
                    type: 'POST',
                    url: actionUrl,
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function (result) {
                        if (result.noSavePermission || result.noUpdatePermission || result.isDuplicate) {
                            toastr.error(result.message);
                        } else if (result.isSuccess) {
                            $(settings.gridContainer).html(result.html);
                            initialize();
                            toastr.success(result.message, 'Success');
                            ResetForm();
                        } else {
                            $(settings.formSelector).html(result);
                            initialize();
                        }
                    },
                    error: function () { toastr.error('Failed Insert.'); }
                });
            });

            // ── EmployeeId Change ────────────────────────────────────────────────
            $('body').on('change', '#EmployeeId', function () {
                if (_suppressEmployeeChange) return;

                var selectedEmployee = $(this).val();
                loadTableData(selectedEmployee || null);

                if (!selectedEmployee) {
                    clearAllFields();
                    return;
                }

                $.ajax({
                    url: '/EmployeeContactInfos/GetContactInfoByEmployeeId',
                    type: 'GET',
                    data: { employeeId: selectedEmployee },
                    success: function (response) {
                        // Controller: return Json(new { success = true, data = result });
                        if (!response || !response.success) {
                            clearAllFields();
                            return;
                        }

                        var data = response.data || {};

                        // Suppress all change events while filling form
                        _suppressEmployeeChange = true;

                        // ── Display-only spans ───────────────────────────────────
                        $('#FullName').text(data.employeeName || '');
                        $('#DesignationName').text(data.designationName || '');
                        $('#DepartmentName').text(data.departmentName || '');

                        // ── Hidden fields ────────────────────────────────────────
                        $('#AutoId').val(data.autoId > 0 ? data.autoId : '0');
                        $('#EmpContactId').val(data.empContactId || '');

                        // ── Select2: EmployeeId (UI sync only) ───────────────────
                        s2SetValue('EmployeeId', data.employeeId, data.employeeName);

                        // ── Select2: BranchCode (UI sync only) ───────────────────
                        s2SetValue('BranchCode', data.branchCode, data.coreBranchName);

                        // ── Permanent Address ────────────────────────────────────
                        $('#ParmanentAddress').val(data.parmanentAddress || '');
                        $('#ParmanentAddressBangla').val(data.parmanentAddressBangla || '');
                        $('#ParmanentPostOffice').val(data.parmanentPostOffice || '');
                        $('#ParmanentThana').val(data.parmanentThana || '');
                        $('#ParmanentPostCode').val(data.parmanentPostCode || '');
                        $('#ParmanentPhone').val(data.parmanentPhone || '');
                        s2SetValue('ParmanentDistrict', data.parmanentDistrict, data.parmanentDistrict);

                        // ── Present Address ──────────────────────────────────────
                        $('#PresentAddress').val(data.presentAddress || '');
                        $('#PresentAddressBangla').val(data.presentAddressBangla || '');
                        $('#PresentPostOffice').val(data.presentPostOffice || '');
                        $('#PresentThana').val(data.presentThana || '');
                        $('#PresentPostCode').val(data.presentPostCode || '');
                        $('#PresentMobile').val(data.presentMobile || '');
                        $('#PresentPhone').val(data.presentPhone || '');
                        $('#PresentFax').val(data.presentFax || '');
                        $('#PresentEmail').val(data.presentEmail || '');
                        s2SetValue('PresentDistrict', data.presentDistrict, data.presentDistrict);

                        // ── Emergency Contact 1 ──────────────────────────────────
                        $('#EmContactName1').val(data.emContactName1 || '');
                        $('#EmContactAddress1').val(data.emContactAddress1 || '');
                        $('#EmContactMobile1').val(data.emContactMobile1 || '');
                        $('#EmContactPhone1').val(data.emContactPhone1 || '');
                        $('#EmContactFax1').val(data.emContactFax1 || '');
                        $('#EmContactEmail').val(data.emContactEmail || '');
                        s2SetValue('EmContactRelation1', data.emContactRelation1, data.emContactRelation1);

                        // ── Emergency Contact 2 ──────────────────────────────────
                        $('#EmContactName2').val(data.emContactName2 || '');
                        $('#EmContactAddress2').val(data.emContactAddress2 || '');
                        $('#EmContactMobile2').val(data.emContactMobile2 || '');
                        $('#EmContactPhone2').val(data.emContactPhone2 || '');
                        $('#EmContactFax2').val(data.emContactFax2 || '');
                        $('#EmContactEmai2').val(data.emContactEmai2 || '');
                        s2SetValue('EmContactRelation2', data.emContactRelation2, data.emContactRelation2);

                        // ── Show/hide creation/modify date row ───────────────────
                        if (data.autoId > 0) {
                            $('#LdateModifyHide').show();
                        } else {
                            $('#LdateModifyHide').hide();
                        }

                        _suppressEmployeeChange = false;
                    },
                    error: function () {
                        _suppressEmployeeChange = false;
                        toastr.error('Failed to load employee contact info.');
                    }
                });
            });

            // ── Clear All Fields ─────────────────────────────────────────────────
            function clearAllFields() {
                
                $('#FullName').text('');
                $('#DesignationName').text('');
                $('#DepartmentName').text('');
                $('#AutoId').val('0');
                $('#EmpContactId').val('');
                $('#LdateModifyHide').hide();

                s2Clear('BranchCode');

                $('#ParmanentAddress, #ParmanentAddressBangla, #ParmanentPostOffice').val('');
                $('#ParmanentThana, #ParmanentPostCode, #ParmanentPhone').val('');
                s2Clear('ParmanentDistrict');

                $('#PresentAddress, #PresentAddressBangla, #PresentPostOffice').val('');
                $('#PresentThana, #PresentPostCode, #PresentMobile').val('');
                $('#PresentPhone, #PresentFax, #PresentEmail').val('');
                s2Clear('PresentDistrict');

                $('#EmContactName1, #EmContactAddress1, #EmContactMobile1').val('');
                $('#EmContactPhone1, #EmContactFax1, #EmContactEmail').val('');
                s2Clear('EmContactRelation1');

                $('#EmContactName2, #EmContactAddress2, #EmContactMobile2').val('');
                $('#EmContactPhone2, #EmContactFax2, #EmContactEmai2').val('');
                s2Clear('EmContactRelation2');
               
            }

            // ── Select2 Helpers ──────────────────────────────────────────────────

            /**
             * s2SetValue — set value + sync select2 UI only (no 'change' event cascade).
             * If value is empty/null → clears the field instead.
             */
            function s2SetValue(id, value, label) {
                var $el = $('#' + id);
                if (!value) {
                    $el.val(null).trigger('change.select2');
                    return;
                }
                var displayLabel = label || value;
                if ($el.find('option[value="' + value + '"]').length === 0) {
                    $el.append(new Option(displayLabel, value, true, true));
                }
                $el.val(value).trigger('change.select2'); 
            }

            /**
             * s2Clear — clears a select2 field (UI sync only).
             */
            function s2Clear(id) {
                $('#' + id).val(null).trigger('change.select2');
            }

            // ── Scroll Top ───────────────────────────────────────────────────────
            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            let loadUrl, target, reloadUrl, title, lastCode;

            // ── Quick Add ────────────────────────────────────────────────────────
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

            // ── Select All ───────────────────────────────────────────────────────
            $("body").on('click', settings.selectAllSelector, function () {
                $('.checkBox').prop('checked', $(this).prop('checked'));
            });

            // ── Delete Confirm ───────────────────────────────────────────────────
            $("body").on('click', settings.deleteSelector, function (e) {
                e.preventDefault();
                var selectedIds = [];
                $('.checkBox:checked').each(function () { selectedIds.push($(this).val()); });

                if (selectedIds.length === 0) {
                    var deleteId = $(this).closest('tr').find('.checkBox').data('id') + "";
                    if (deleteId) {
                        selectedIds.push(deleteId);
                    } else {
                        toastr.error('Please select employee to delete.');
                        return;
                    }
                }
                $(settings.deleteModal + ' ' + settings.finalDeleteSelector).data('ids', selectedIds);
                $(settings.deleteModal).modal('show');
            });

            // ── Final Delete ─────────────────────────────────────────────────────
            $("body").on('click', settings.finalDeleteSelector, function (e) {
                e.preventDefault();
                var selectedIds = $(this).data('ids');

                $.ajax({
                    type: 'POST',
                    url: deleteURL,
                    contentType: 'application/json',
                    data: JSON.stringify(selectedIds),
                    success: function (result) {
                        if (result.success) {
                            loadTableData();
                            GenerateNextCode();
                            toastr.success(result.message);
                            $('.checkBox').prop('checked', false);
                        } else {
                            toastr.error(result.message);
                        }
                        $(settings.deleteModal).modal('hide');
                    },
                    error: function () {
                        toastr.error('An error occurred.');
                        $(settings.deleteModal).modal('hide');
                    }
                });
            });

            // ── Availability Check ───────────────────────────────────────────────
            $("body").on("keyup", settings.availabilitySelector, function () {
                let code = $(".js-code").val();
                let name = $(this).val();
                $.ajax({
                    url: settings.baseUrl + "/CheckAvailability",
                    method: "POST",
                    data: { code: code, name: name },
                    success: function (response) {
                        if (response.isSuccess) toastr.error(response.message);
                    }
                });
            });

        }); // end $()


        // ── Initialize ───────────────────────────────────────────────────────────
        function initialize() {
            $(document).ready(async function () {

                s2_InitSingle("#EmployeeId", "/GcFilters/employee", "Select Employee", null);

                s2_InitSingle(
                    "#CompanyCode",
                    "/GcFilters/company",
                    "Select Company",
                    "company",
                    ["#BranchCode", "#DivisionCode", "#DepartmentCode", "#DesignationCode"]
                );

                s2_InitSingle("#BranchCode", "/GcFilters/branch", "Select Branch", "branch");

                await s2_LoadNext("#CompanyCode", "/GcFilters/company");

                var $comp = $("#CompanyCode");
                var defaultCode = "001";
                var defaultName = $comp.find('option[value="' + defaultCode + '"]').text();
                await s2_SetDefault("#CompanyCode", defaultCode, defaultName, true);
            });

            loadTableData();
            ConvertToBangla();
        }

        // ── Reset Form ────────────────────────────────────────────────────────────
        function ResetForm() {
            _pendingEmployeeRestore = null;
            _suppressEmployeeChange = false;

            $(settings.formSelector)[0].reset();

            $('#AutoId').val('0');
            $('#EmpContactId').val('');
            $('#LdateModifyHide').hide();

            $('#ParmanentAddress, #ParmanentAddressBangla, #ParmanentPostOffice').val('');
            $('#ParmanentThana, #ParmanentPostCode, #ParmanentPhone').val('');

            $('#PresentAddress, #PresentAddressBangla, #PresentPostOffice').val('');
            $('#PresentThana, #PresentPostCode, #PresentMobile').val('');
            $('#PresentPhone, #PresentFax, #PresentEmail').val('');

            $('#EmContactName1, #EmContactAddress1, #EmContactPhone1').val('');
            $('#EmContactMobile1, #EmContactFax1, #EmContactEmail').val('');

            $('#EmContactName2, #EmContactAddress2, #EmContactPhone2').val('');
            $('#EmContactMobile2, #EmContactFax2, #EmContactEmai2').val('');

            // UI-only clear — no cascade
            $('#BranchCode').val(null).trigger('change.select2');
            $('#EmployeeId').val(null).trigger('change.select2');
            $('#ParmanentDistrict').val(null).trigger('change.select2');
            $('#PresentDistrict').val(null).trigger('change.select2');
            $('#EmContactRelation1').val(null).trigger('change.select2');
            $('#EmContactRelation2').val(null).trigger('change.select2');

            $('#FullName').text('');
            $('#DesignationName').text('');
            $('#DepartmentName').text('');

            $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
            $('.text-danger').text('');
            $(settings.formSelector).attr('action', testSave);
            GenerateNextCode();
        }

        $("body").on('click', settings.clearSelector, function () {
            ResetForm();
            loadTableData();

        });

        // ── Validation ────────────────────────────────────────────────────────────
        function validation() {
            var company = $("#CompanyCode").val();
            var employee = $('#EmployeeId').val();

            if (!company) {
                toastr.info('Select Company');
                $('#CompanyCode').select2('open');
                return false;
            }
            if (!employee) {
                toastr.info('Select Employee');
                $('#EmployeeId').select2('open');
                return false;
            }
            return true;
        }

        // ── Load Table ────────────────────────────────────────────────────────────
        function loadTableData(id) {
            $.ajax({
                type: 'GET',
                url: loadTableURL,
                data: { employeeId: id },
                success: function (data) {
                    $(settings.gridContainer).html(data);
                    dataTable();
                },
                error: function () { toastr.error('Failed Load Data'); }
            });
        }

        function dataTable() {
            $(settings.gridSelector).DataTable({
                responsive: true,
                pageLength: 10,
                destroy: true,
                lengthMenu: [10, 25, 50, 100]
            });
        }

        // ── ConvertToBangla ───────────────────────────────────────────────────────
        function ConvertToBangla() {
            const prefixMap = [
                ["MD.", "মো:"], ["Md.", "মো:"], ["md.", "মো:"],
                ["MD", "মো:"], ["Md", "মো:"], ["md", "মো:"],
                ["MR.", "মিস্টার"], ["Mr.", "মিস্টার"], ["mr.", "মিস্টার"],
                ["MR", "মিস্টার"], ["Mr", "মিস্টার"], ["mr", "মিস্টার"],
                ["MS.", "মিসেস"], ["Ms.", "মিসেস"], ["ms.", "মিসেস"],
                ["MS", "মিসেস"], ["Ms", "মিসেস"], ["ms", "মিসেস"]
            ];

            async function transliterate(text) {
                if (!text || text.trim() === "") return "";
                let banglaPrefix = "";
                let remainingText = text;

                for (let [key, value] of prefixMap) {
                    if (text.startsWith(key + " ") || text.startsWith(key)) {
                        banglaPrefix = value;
                        remainingText = text.slice(key.length).trim();
                        break;
                    }
                }

                if (remainingText === "") return banglaPrefix;

                let results = await Promise.all(
                    remainingText.split(",").map(async part => {
                        let cleanPart = part.trim();
                        if (!cleanPart) return "";
                        try {
                            let res = await fetch("https://inputtools.google.com/request?text=" +
                                encodeURIComponent(cleanPart) + "&itc=bn-t-i0-und&num=1");
                            let data = await res.json();
                            return data[0] === "SUCCESS" ? data[1][0][1][0] : cleanPart;
                        } catch { return cleanPart; }
                    })
                );

                let banglaText = results.join(", ");
                return banglaPrefix ? banglaPrefix + " " + banglaText : banglaText;
            }

            function bindConvert(engId, bnId) {
                let engInput = document.getElementById(engId);
                let bnInput = document.getElementById(bnId);
                if (!engInput || !bnInput) return;
                engInput.addEventListener("input", async function () {
                    if (this.value.trim() === "") { bnInput.value = ""; return; }
                    bnInput.value = await transliterate(this.value);
                });
            }

            bindConvert("ParmanentAddress", "ParmanentAddressBangla");
            bindConvert("PresentAddress", "PresentAddressBangla");
        }

        // ── GenerateNextCode ──────────────────────────────────────────────────────
        function GenerateNextCode() {
            $('#EmpContactId').val('');
            $.ajax({
                type: 'GET',
                url: nextCodeULR,
                success: function (result) { $('#EmpContactId').val(result); },
                error: function () { toastr.error('Failed Next Code'); }
            });
        }

    };
}(jQuery));