//(function ($) {
//    $.hrmEmployee2 = function (options) {

//        var settings = $.extend({
//            baseUrl: "/",
//            formSelector: "#HrmEmployee2-form",
//            formContainer: ".js-HrmEmployee2-form-container",
//            gridSelector: "#HrmEmployee2-grid",
//            gridContainer: ".js-HrmEmployee2-grid-container",
//            editSelector: ".js-HrmEmployee2-edit",
//            saveSelector: ".js-HrmEmployee2-save",
//            selectAllSelector: "#HrmEmployee2-check-all",
//            deleteSelector: ".js-HrmEmployee2-delete-confirm",
//            deleteModal: "#HrmEmployee2-delete-modal",
//            finalDeleteSelector: ".js-HrmEmployee2-delete",
//            clearSelector: ".js-HrmEmployee2-clear",
//            topSelector: ".js-go",
//            decimalSelector: ".js-HrmEmployee2-decimalplaces",
//            maxDecimalPlace: 5,
//            showNagativeFormat: false,
//            availabilitySelector: ".js-HrmEmployee2-check-availability",
//            haseFile: false,
//            quickAddSelector: ".js-quick-add",
//            quickAddModal: "#quickAddModal",
//            load: function () {

//            }

//        }, options);

//        var baseControllerNameUrl = "/HrmEmployee2";
//        var testSave = baseControllerNameUrl + "/Setup";
//        var nextCodeULR = baseControllerNameUrl + "/GenerateNextCode";
//        var loadTableURL = baseControllerNameUrl + "/GetTableData";
//        var deleteURL = baseControllerNameUrl + "/Delete";
//        var uploadURL = baseControllerNameUrl + "/UploadExcel";
//        var getById = baseControllerNameUrl + "/Setup";
//        var duplicateCheckURL = baseControllerNameUrl + "/CheckAvailability";
//        var selectedItems = [];

//        $(document).ready(function () {
//            $('[data-toggle="tooltip"]').tooltip();
//        });

//        $(window).on("load", function () {
//            $("#customLoadingOverlay").fadeOut(300);
//        });

//        $(() => {

//            initialize();

//            // Edit leave type

//            $("body").on('click', settings.editSelector, function () {

//                var id = $(this).data('id');

//                $.get(getById, { id: id }, function (result) {

//                    $(settings.formSelector).html($(result).find(settings.formSelector).html());

//                    $(settings.formSelector).attr('action', testSave);

//                }).fail(function () {
//                    toastr.error('Failed Update.');
//                });
//            });

//            // Save button click event
//            $("body").on('click', settings.saveSelector, function () {

//                if (!validation()) { return; }
//                $(settings.formSelector).submit();
//            });

//            // Form submission event
//            $("body").on('submit', settings.formSelector, function (e) {
//                e.preventDefault();
//                var form = $(this)[0];
//                var formData = new FormData(form);
//                var actionUrl = $(this).attr('action');

//                $.ajax({
//                    type: 'POST',
//                    url: actionUrl,
//                    data: formData,
//                    contentType: false,
//                    processData: false,
//                    success: function (result) {

//                        if (result.noSavePermission || result.noUpdatePermission || result.isDuplicate) {
//                            toastr.error(result.message);
//                        } else if (result.isSuccess) {
//                            $(settings.gridContainer).html(result.html);
//                            initialize();

//                            toastr.success(result.message);
//                            setTimeout(function () {
//                                window.location.href = result.redirectUrl;
//                            }, 1000);

//                        } else {
//                            $(settings.formSelector).html(result);
//                            initialize();

//                        }
//                    },
//                    error: function () {
//                        toastr.error('Failed Insert.');
//                    }
//                });
//            });

//            //Duplicate

//            $("body").on("keyup change", "#FirstName, #FatherName, #DateOfBirthOrginalHidden,#EmployeeId", function () {

//                let code = $("#EmployeeId").val();
//                let firstName = $("#FirstName").val();
//                let fathersName = $("#FatherName").val();
//                let dateOfBirthOriginal = $("#DateOfBirthOrginalHidden").val();

//                $.ajax({
//                    url: duplicateCheckURL,
//                    method: "POST",
//                    data: { code: code, firstName: firstName, fathersName: fathersName, dateOfBirthOriginal: dateOfBirthOriginal },
//                    success: function (response) {
//                        console.log(response);
//                        if (response.isSuccess) {
//                            toastr.error(response.message);
//                        }
//                    }
//                });

//            });

//            //
//            $('#CompanyCode').change(function () {
//                const companyCode = $(this).val();
//                console.log("Selected company code:", companyCode); // Debugging the selected company code

//                $.getJSON('/HrmEmployee2/GetBranchesByCompanyCode', { companyCode: companyCode }, function (data) {
//                    console.log("Branches data:", data); // Debugging the response data
//                    const branchDropdown = $('#BranchCode');
//                    branchDropdown.empty();
//                    if (data.length > 0) {
//                        branchDropdown.append('<option value="">--Select Branch--</option>');
//                        $.each(data, function (index, item) {
//                            branchDropdown.append(`<option value="${item.value}">${item.text}</option>`);
//                        });
//                    }
//                    else {
//                        branchDropdown.append('<option value="">No branches available</option>');
//                    }
//                });
//            });

//            //

//            $("body").on("click", settings.topSelector, function (e) {
//                e.preventDefault();
//                $("html, body").animate({ scrollTop: 0 }, 500);
//            });

//            let loadUrl,
//                target,
//                reloadUrl,
//                title,
//                lastCode;
//            // Quick add
//            $("body").on("click", settings.quickAddSelector, function (e) {
//                e.stopPropagation();
//                e.preventDefault();
//                e.stopImmediatePropagation();

//                loadUrl = $(this).data("url");
//                target = $(this).data("target");
//                reloadUrl = $(this).data("reload-url");
//                title = $(this).data("title");

//                $(settings.quickAddModal + " .modal-title").html(title);
//                $(settings.quickAddModal + " .modal-body").empty();

//                $(settings.quickAddModal + " .modal-body").load(loadUrl, function () {
//                    $(settings.quickAddModal).modal("show");
//                    $("#header").hide();
//                    $(settings.quickAddModal + " .modal-body #header").hide()

//                    $("#left_menu").hide();
//                    $(settings.quickAddModal + " .modal-body #left_menu").hide()

//                    $("#main-content").toggleClass("collapse-main");
//                    $(settings.quickAddModal + " .modal-body #main-content").toggleClass("collapse-main")

//                    $("body").removeClass("sidebar-mini");
//                })
//            });

//            $("body").on("click", ".js-modal-dismiss", function () {
//                $("body").removeClass("sidebar-mini").addClass("sidebar-mini");

//                $("#header").show();
//                $(settings.quickAddModal + " .modal-body #header").show()

//                $("#left_menu").show();

//                $(settings.quickAddModal + " .modal-body #left_menu").show()

//                $("#main-content").toggleClass("collapse-main");
//                $(settings.quickAddModal + " .modal-body #main-content").toggleClass("collapse-main")

//                lastCode = $(settings.quickAddModal + " #lastCode").val();

//                $(settings.quickAddModal + " .modal-body").empty();
//                $(settings.quickAddModal).modal("hide");

//                $(target).empty("");
//                $(target).append($('<option>', {
//                    value: '',
//                    text: `--Select ${title}--`
//                }));
//                $.ajax({
//                    url: reloadUrl,
//                    method: "GET",
//                    success: function (response) {
//                        console.log(response);
//                        $.each(response, function (i, item) {
//                            $(target).append($('<option>', {
//                                value: item.code,
//                                text: item.name
//                            }));
//                        });
//                        console.log("Test", lastCode);
//                        $(target).val(lastCode);
//                    }
//                });
//            });

//            // Select all checkboxes
//            $("body").on('click', settings.selectAllSelector, function () {
//                $('.checkBox').prop('checked', $(this).prop('checked'));
//            });


//            // Step 1: Collect IDs and show modal
//            $("body").on('click', settings.deleteSelector, function (e) {
//                e.preventDefault();

//                var selectedIds = [];

//                $('.checkBox:checked').each(function () {
//                    selectedIds.push($(this).val());
//                });

//                if (selectedIds.length === 0) {
//                    // ✅ Use .val() instead of data('id') to match checkbox value
//                    var singleId = $(this).closest('tr').find('.checkBox').val();
//                    if (singleId) {
//                        selectedIds.push(singleId);
//                    } else {
//                        toastr.error('Please select an employee to delete.');
//                        return;
//                    }
//                }

//                $(settings.deleteModal + ' ' + settings.finalDeleteSelector).data('ids', selectedIds);
//                $(settings.deleteModal).modal('show');
//            });

//            // Step 2: Confirm and send delete request
//            $("body").on('click', settings.finalDeleteSelector, function (e) {
//                e.preventDefault();

//                var selectedIds = $(this).data('ids');

//                if (!selectedIds || selectedIds.length === 0) {
//                    toastr.error('No employee selected.');
//                    $(settings.deleteModal).modal('hide');
//                    return;
//                }

//                $.ajax({
//                    type: 'POST',
//                    url: deleteURL,
//                    contentType: 'application/json',
//                    data: JSON.stringify(selectedIds),
//                    success: function (result) {
//                        if (result.isSuccess) {
//                            loadTableData();
//                            toastr.success(result.message);
//                            $('.checkBox').prop('checked', false);
//                        } else if (result.refError) {
//                            // ✅ Fix 3: Match refError from controller
//                            toastr.warning(result.message);
//                        } else {
//                            toastr.error(result.message, 'Error');
//                        }
//                        $(settings.deleteModal).modal('hide');
//                    },
//                    error: function (xhr) {
//                        toastr.error('An error occurred while deleting.');
//                        $(settings.deleteModal).modal('hide');
//                    }
//                });
//            });

//            $("body").on("keyup", settings.availabilitySelector, function () {
//                var self = $(this);
//                let code = $(".js-code").val();
//                let name = self.val();

//                // check
//                $.ajax({
//                    url: settings.baseUrl + "/CheckAvailability",
//                    method: "POST",
//                    data: { code: code, name: name },
//                    success: function (response) {
//                        console.log(response);
//                        if (response.isSuccess) {
//                            toastr.error(response.message);
//                        }
//                    }
//                });
//            });

//            $("body").on("click", ".js-file-chooser", function (e) {
//                e.preventDefault();
//                var target = $(this).data("target");
//                $(target).trigger("click");
//            })

//            $("body").on("change", ".js-file", function (e) {
//                e.preventDefault();
//                var target = $(this).data("target");
//                showImagePreview($(this), target);
//            })

//            $("body").on("click", ".js-clear-file", function (e) {
//                e.preventDefault();
//                var file = $(this).data("file");
//                var tag = $(this).data("tag");
//                clearImage(file, tag);
//            })
//        });

//        function showImagePreview(input, target) {

//            if (input[0].files && input[0].files[0]) {
//                var reader = new FileReader();
//                reader.onload = function (e) {
//                    $(target).prop('src', e.target.result);
//                };
//                reader.readAsDataURL(input[0].files[0]);
//            }
//        }

//        function clearImage(file, tag) {

//            console.log(file);
//            console.log(tag);
//            $(file).removeAttr("src");
//            $(tag).val(true);
//        }

//        function initialize() {
//            loadTableData();
//            //GenerateNextCode();
//            AllDatePicker();
//            ConvertToBangla();

//            var empId = $('#EmployeeId').val();
//            if (empId) {
//                $("#EmployeeId").prop("readonly", true).addClass("bg-white");
//            } else {
//                $("#EmployeeId").prop("readonly", false);

//            }

//            //ResetForm();
//        }
//        //
//        function ResetForm() {

//            $(settings.formSelector)[0].reset();

//            $('#Id').val('');
//            $('#AutoId').val('');
//            $('#EmployeeId').val('');
//            $('#FirstName').val('');
//            $('#LastName').val('');
//            $('#FatherName').val('');
//            $('#MotherName').val('');
//            $('#DateOfBirthCertificate').val('');
//            $('#DateOfBirthOrginal').val('');
//            $('#DateOfBirthOrginalHidden').val('');
//            $('#BirthCertificateNo').val('');
//            $('#PlaceOfBirth').val('');
//            $('#MarriageDate').val('');
//            $('#NoOfSon').val('');
//            $('#NoOfDaughters').val('');
//            $('#CardNo').val('');
//            $('#UserInfoEmployeeId').val('');
//            $('#PersonalEmail').val('');
//            $('#NationalIdno').val('');
//            $('#TinNo').val('');
//            $('#Telephone').val('');
//            $('#ExtraCurriActivities').val('');
//            $('#Remarks').val('');
//            $('#FirstNameBangla').val('');
//            $('#LastNameBangla').val('');
//            $('#FatherOccupation').val('');
//            $('#MotherOccupation').val('');
//            $('#Spouse')
//            $('#BloodGroupCode').val(null).trigger('change');
//            //$('#CompanyCode').val(null).trigger('change');
//            $('#NationalityCode').val(null).trigger('change');
//            $('#SexCode').val(null).trigger('change');
//            $('#BranchCode').val(null).trigger('change');
//            $('#ReligionCode').val(null).trigger('change');
//            $('#MaritalStatusCode').val(null).trigger('change');
//            $('#LdateModifyHide').hide();
//            $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
//            $('.text-danger').text('');
//            $(settings.formSelector).attr('action', testSave);
//            $("#EmployeeId").prop("readonly", false);

//            // GenerateNextCode();
//        }

//        $("body").on('click', settings.clearSelector, function () {
//            ResetForm();
//        });
//        //
//        // Validation function
//        function validation() {

//            var company = $("#CompanyCode").val();
//            var employeeId = $("#EmployeeId").val();
//            var fName = $("#FirstName").val();
//           // var dob = $("#DateOfBirthOrginal").val();
//            var gender = $("#SexCode").val();

//            if (!company) {
//                toastr.info('Select Company');
//                $('#CompanyCode').select2('open');
//                return false;
//            }

//            if (!employeeId) {
//                toastr.info('Select EmployeeId');
//                $('#EmployeeId').trigger('focus');
//                return false;
//            }

//            if (!fName) {
//                toastr.info('Enter  First Name');
//                $('#FirstName').trigger('focus');
//                return false;
//            }

//            //if (!dob) {
//            //    toastr.info('Enter Date Of Birth');
//            //    $("#DateOfBirthOrginal")[0]._flatpickr.open();
//            //    return false;
//            //}

//            if (!gender) {
//                toastr.info('Select Gender');
//                $('#SexCode').select2('open');
//                return false;
//            }
//            return true;
//        }

//        //Load  list

//        function loadTableData() {
//            $.ajax({
//                type: 'GET',
//                url: loadTableURL,
//                success: function (data) {
//                    var table = $(settings.gridSelector).DataTable();
//                    table.destroy();

//                    var tbody = $('#HrmEmployee2-grid tbody');
//                    tbody.empty();

//                    if (data && data.length > 0) {
//                        // C# already sorted by AutoId DESC — no JS sort needed
//                        $.each(data, function (index, item) {
//                            var fullName = (item.firstName || '') + ' ' + (item.lastName || '');
//                            var fullNameBn = (item.firstNameBangla || '') + ' ' + (item.lastNameBangla || '');
//                            var photoHtml = item.photoUrl
//                                ? `<img src="${item.photoUrl}" class="rounded-circle" height="50" width="50" />`
//                                : `<div style="height:50px;width:50px;"></div>`;

//                            var row = `
//                        <tr>
//                            <td class="text-center text-middle">
//                                <input type="checkbox" class="checkBox"
//                                    data-id="${item.employeeId}"
//                                    value="${item.employeeId}" />
//                            </td>
//                            <td class="text-center text-middle">
//                                <a title="Edit ${fullName}"
//                                   style="text-decoration:underline;"
//                                   href="/HrmEmployee2/Setup?id=${item.employeeId}">
//                                    ${item.employeeId}
//                                </a>
//                            </td>
//                            <td class="text-left">${fullName}</td>
//                            <td class="text-left">${fullNameBn}</td>
//                            <td class="text-center text-middle">${item.sexCode || ''}</td>
//                            <td class="text-center text-middle">${item.nationalityCode || ''}</td>
//                            <td class="text-center text-middle">${item.nationalIdno || ''}</td>
//                            <td class="text-center text-middle">${item.telephone || ''}</td>
//                            <td class="text-center text-middle">${photoHtml}</td>
//                            <td class="text-center text-middle">
//                                <a title="Edit ${fullName}"
//                                   class="btn btn-sm"
//                                   href="/HrmEmployee2/Setup?id=${item.employeeId}">
//                                    <i class="fas fa-pencil-alt"></i>
//                                </a>
//                                <button type="button"
//                                        title="Delete ${fullName}"
//                                        class="btn btn-sm js-HrmEmployee2-delete-confirm"
//                                        data-id="${item.employeeId}">
//                                    <i class="fas fa-trash fa-sm"></i>
//                                </button>
//                            </td>
//                        </tr>`;
//                            tbody.append(row);
//                        });

//                        dataTable();

//                    } else {
//                        tbody.html(`
//                    <tr>
//                        <td colspan="10" class="text-center text-middle">
//                            No Employee Information Available.
//                        </td>
//                    </tr>`);
//                    }
//                },
//                error: function () {
//                    toastr.error('Failed to Load Data');
//                }
//            });
//        }

//        function dataTable() {
//            $(settings.gridSelector).DataTable({
//                responsive: true,
//                pageLength: 10,
//                destroy: true,
//                lengthMenu: [10, 25, 50, 100],
//                order: [], // ← DataTable নিজে sort করবে না, server order রাখবে
//            });
//        }

//        function AllDatePicker() {
//            flatpickr($("#DateOfBirthOrginal, #DateOfBirthCertificate, #MarriageDate"), CalendarService.createConfig(
//                {

//                }
//            ));
//        }

//        function ConvertToBangla() {
//            let engFInput = document.getElementById("FirstName");
//            let bnFInput = document.getElementById("FirstNameBangla");

//            if (!engFInput || !bnFInput) return;

//            // লম্বা key আগে রাখো (order গুরুত্বপূর্ণ)
//            const prefixMap = [
//                ["MD.", "মো:"],
//                ["Md.", "মো:"],
//                ["md.", "মো:"],
//                ["MD", "মো:"],
//                ["Md", "মো:"],
//                ["md", "মো:"],
//                ["MR.", "মিস্টার"],
//                ["Mr.", "মিস্টার"],
//                ["mr.", "মিস্টার"],
//                ["MR", "মিস্টার"],
//                ["Mr", "মিস্টার"],
//                ["mr", "মিস্টার"],
//                ["MS.", "মিসেস"],
//                ["Ms.", "মিসেস"],
//                ["ms.", "মিসেস"],
//                ["MS", "মিসেস"],
//                ["Ms", "মিসেস"],
//                ["ms", "মিসেস"],
//            ];

//            if (engFInput != null || engFInput.value.trim() !== "") {


//                engFInput.addEventListener("input", function () {
//                    let text = this.value;

//                    if (text.trim() === "") {
//                        bnFInput.value = "";
//                        return;
//                    }

//                    // Prefix আলাদা করো
//                    let banglaPrefix = "";
//                    let remainingText = text;

//                    for (let [key, value] of prefixMap) {
//                        if (text.startsWith(key + " ") || text.startsWith(key)) {
//                            banglaPrefix = value;
//                            remainingText = text.slice(key.length).trim();
//                            break;
//                        }
//                    }

//                    // Remaining text নেই মানে শুধু prefix
//                    if (remainingText === "") {
//                        bnFInput.value = banglaPrefix;
//                        return;
//                    }

//                    fetch("https://inputtools.google.com/request?text=" + encodeURIComponent(remainingText) + "&itc=bn-t-i0-und&num=1")
//                        .then(res => res.json())
//                        .then(data => {
//                            if (data[0] === "SUCCESS") {
//                                let banglaText = data[1][0][1][0];
//                                bnFInput.value = banglaPrefix ? banglaPrefix + " " + banglaText : banglaText;
//                            }
//                        })
//                        .catch(err => {
//                            console.error(err);
//                        });
//                });
//            }

//            let engLInput = document.getElementById("LastName");
//            let bnLInput = document.getElementById("LastNameBangla");

//            if (engLInput != null || engLInput.value.trim() !== "") {

//                engLInput.addEventListener("input", function () {

//                    let text = this.value;

//                    // If empty, clear Bangla field
//                    if (text.trim() === "") {
//                        bnLInput.value = "";
//                        return;
//                    }

//                    fetch("https://inputtools.google.com/request?text=" + encodeURIComponent(text) + "&itc=bn-t-i0-und&num=1")
//                        .then(res => res.json())
//                        .then(data => {
//                            if (data[0] === "SUCCESS") {
//                                bnLInput.value = data[1][0][1][0];
//                            }
//                        })
//                        .catch(err => {
//                            console.error(err);
//                        });

//                });
//            }
//        }

//        // ── File input: show selected filename & validate extension ──────────────
//        $('#excelFileInput').on('change', function () {
//            const file = this.files[0];
//            const warningEl = $('#checkExcelFileFormate');
//            const chooseText = $('#choosefileText');

//            if (!file) {
//                chooseText.text('Choose File');
//                warningEl.hide();
//                return;
//            }

//            const ext = file.name.split('.').pop().toLowerCase();
//            if (ext !== 'xlsx' && ext !== 'xls') {
//                warningEl.show();
//                chooseText.text('Choose File');
//                $(this).val('');          // clear the invalid file
//                return;
//            }

//            warningEl.hide();
//            chooseText.text(file.name);
//        });

//        // ── Form submit: send file via AJAX ──────────────────────────────────────
//        $(document).on('click', '#excelUploadForm button[type="submit"]', function (e) {

//            e.preventDefault();

//            const fileInput = $('#excelFileInput')[0];
//            const warningEl = $('#checkExcelFileFormate');

//            if (!fileInput.files || fileInput.files.length === 0) {
//                toastr.warning('Please choose an Excel file first.');
//                return;
//            }

//            const file = fileInput.files[0];
//            const ext = file.name.split('.').pop().toLowerCase();

//            if (ext !== 'xlsx' && ext !== 'xls') {
//                warningEl.show();
//                return;
//            }
//            warningEl.hide();

//            const formData = new FormData();
//            formData.append('excelFile', file);

//            // Add antiforgery token if present
//            const token = $('input[name="__RequestVerificationToken"]').val();
//            if (token) formData.append('__RequestVerificationToken', token);

//            // Show loading state
//            const $uploadBtn = $(this);
//            const originalHtml = $uploadBtn.html();
//            $uploadBtn.html('<span class="spinner-border spinner-border-sm me-1"></span> Uploading...').prop('disabled', true);

//            $.ajax({
//                type: 'POST',
//                url: uploadURL,
//                data: formData,
//                contentType: false,
//                processData: false,
//                success: function (response) {
//                    $uploadBtn.html(originalHtml).prop('disabled', false);

//                    if (response.isFormatError) {
//                        warningEl.show();
//                        return;
//                    }

//                    if (response.isSuccess) {

//                        let message = response.message;

//                        if (response.failCount > 0) {
//                            message += `<br/>${response.failCount} row(s) failed.`;

//                            if (response.errors && response.errors.length > 0) {
//                                response.errors.forEach(function (err) {
//                                    message += `<br/>• ${err}`;
//                                });
//                            }
//                        }

//                        toastr.success(message);

//                        // Reset file input
//                        fileInput.value = '';
//                        $('#choosefileText').text('Choose File');

//                    } else {
//                        toastr.error(response.message);
//                    }
//                },
//                error: function (xhr) {

//                    $uploadBtn.html(originalHtml).prop('disabled', false);

//                    const msg = xhr.responseJSON?.message || 'An unexpected error occurred.';

//                    toastr.error(msg);
//                }
//            });
//        });

//    }

//}(jQuery));


// ── hrmEmployee2.js  (server-side DataTables version) ────────────────────────
// Replace the existing loadTableData() and dataTable() functions with this file.
// Everything else in your original hrmEmployee2.js stays exactly the same.

(function ($) {
    $.hrmEmployee2 = function (options) {

        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#HrmEmployee2-form",
            formContainer: ".js-HrmEmployee2-form-container",
            gridSelector: "#HrmEmployee2-grid",
            gridContainer: ".js-HrmEmployee2-grid-container",
            editSelector: ".js-HrmEmployee2-edit",
            saveSelector: ".js-HrmEmployee2-save",
            selectAllSelector: "#HrmEmployee2-check-all",
            deleteSelector: ".js-HrmEmployee2-delete-confirm",
            deleteModal: "#HrmEmployee2-delete-modal",
            finalDeleteSelector: ".js-HrmEmployee2-delete",
            clearSelector: ".js-HrmEmployee2-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-HrmEmployee2-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-HrmEmployee2-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            load: function () { }
        }, options);

        var baseControllerNameUrl = "/HrmEmployee2";
        var loadTableURL = baseControllerNameUrl + "/GetTableData";  // now POST
        var deleteURL = baseControllerNameUrl + "/Delete";
        var getById = baseControllerNameUrl + "/Setup";
        var testSave = baseControllerNameUrl + "/Setup";
        var duplicateCheckURL = baseControllerNameUrl + "/CheckAvailability";
        var uploadURL = baseControllerNameUrl + "/UploadExcel";

        // DataTable instance reference
        var dataTableInstance = null;

        // ── DOM ready ────────────────────────────────────────────────────────
        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(window).on("load", function () {
            $("#customLoadingOverlay").fadeOut(300);
        });

        $(() => {
            initialize();

            // ── Edit ─────────────────────────────────────────────────────────
            $("body").on('click', settings.editSelector, function () {
                var id = $(this).data('id');
                $.get(getById, { id: id }, function (result) {
                    $(settings.formSelector).html($(result).find(settings.formSelector).html());
                    $(settings.formSelector).attr('action', testSave);
                }).fail(function () { toastr.error('Failed Update.'); });
            });

            // ── Save ─────────────────────────────────────────────────────────
            $("body").on('click', settings.saveSelector, function () {
                if (!validation()) { return; }
                $(settings.formSelector).submit();
            });

            // ── Form submit ──────────────────────────────────────────────────
            $("body").on('submit', settings.formSelector, function (e) {
                e.preventDefault();
                var formData = new FormData(this);
                var actionUrl = $(this).attr('action');

                $.ajax({
                    type: 'POST', url: actionUrl,
                    data: formData, contentType: false, processData: false,
                    success: function (result) {
                        if (result.noSavePermission || result.noUpdatePermission || result.isDuplicate) {
                            toastr.error(result.message);
                        } else if (result.isSuccess) {
                            // Reload the DataTable (server-side – just draw again)
                            if (dataTableInstance) dataTableInstance.ajax.reload(null, false);
                            toastr.success(result.message);
                            setTimeout(function () {
                                window.location.href = result.redirectUrl;
                            }, 1000);
                        } else {
                            $(settings.formSelector).html(result);
                            initialize();
                        }
                    },
                    error: function () { toastr.error('Failed Insert.'); }
                });
            });

            // ── Duplicate check ──────────────────────────────────────────────
            $("body").on("keyup change", "#FirstName, #FatherName, #DateOfBirthOrginalHidden, #EmployeeId", function () {
                $.ajax({
                    url: duplicateCheckURL, method: "POST",
                    data: {
                        code: $("#EmployeeId").val(),
                        firstName: $("#FirstName").val(),
                        fathersName: $("#FatherName").val(),
                        dateOfBirthOriginal: $("#DateOfBirthOrginalHidden").val()
                    },
                    success: function (response) {
                        if (response.isSuccess) { toastr.error(response.message); }
                    }
                });
            });

            // ── Company → Branch cascade ─────────────────────────────────────
            $('#CompanyCode').change(function () {
                const companyCode = $(this).val();
                $.getJSON('/HrmEmployee2/GetBranchesByCompanyCode', { companyCode }, function (data) {
                    const branchDropdown = $('#BranchCode');
                    branchDropdown.empty();
                    branchDropdown.append(data.length > 0
                        ? '<option value="">--Select Branch--</option>'
                        : '<option value="">No branches available</option>');
                    $.each(data, function (i, item) {
                        branchDropdown.append(`<option value="${item.value}">${item.text}</option>`);
                    });
                });
            });

            // ── Scroll to top ────────────────────────────────────────────────
            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            // ── Quick add modal ──────────────────────────────────────────────
            let loadUrl, target, reloadUrl, title, lastCode;
            $("body").on("click", settings.quickAddSelector, function (e) {
                e.stopPropagation(); e.preventDefault(); e.stopImmediatePropagation();
                loadUrl = $(this).data("url");
                target = $(this).data("target");
                reloadUrl = $(this).data("reload-url");
                title = $(this).data("title");

                $(settings.quickAddModal + " .modal-title").html(title);
                $(settings.quickAddModal + " .modal-body").empty();
                $(settings.quickAddModal + " .modal-body").load(loadUrl, function () {
                    $(settings.quickAddModal).modal("show");
                    $("#header, #left_menu").hide();
                    $("#main-content").toggleClass("collapse-main");
                    $("body").removeClass("sidebar-mini");
                });
            });

            $("body").on("click", ".js-modal-dismiss", function () {
                $("body").removeClass("sidebar-mini").addClass("sidebar-mini");
                $("#header, #left_menu").show();
                $("#main-content").toggleClass("collapse-main");
                lastCode = $(settings.quickAddModal + " #lastCode").val();
                $(settings.quickAddModal + " .modal-body").empty();
                $(settings.quickAddModal).modal("hide");

                $(target).empty("").append($('<option>', { value: '', text: `--Select ${title}--` }));
                $.ajax({
                    url: reloadUrl, method: "GET",
                    success: function (response) {
                        $.each(response, function (i, item) {
                            $(target).append($('<option>', { value: item.code, text: item.name }));
                        });
                        $(target).val(lastCode);
                    }
                });
            });

            // ── Select all checkboxes ────────────────────────────────────────
            $("body").on('click', settings.selectAllSelector, function () {
                $('.checkBox').prop('checked', $(this).prop('checked'));
            });

            // ── Delete: collect IDs → show modal ────────────────────────────
            $("body").on('click', settings.deleteSelector, function (e) {
                e.preventDefault();
                var selectedIds = [];
                $('.checkBox:checked').each(function () { selectedIds.push($(this).val()); });

                if (selectedIds.length === 0) {
                    var singleId = $(this).closest('tr').find('.checkBox').val();
                    if (singleId) { selectedIds.push(singleId); }
                    else { toastr.error('Please select an employee to delete.'); return; }
                }
                $(settings.deleteModal + ' ' + settings.finalDeleteSelector).data('ids', selectedIds);
                $(settings.deleteModal).modal('show');
            });

            // ── Delete: confirm ──────────────────────────────────────────────
            $("body").on('click', settings.finalDeleteSelector, function (e) {
                e.preventDefault();
                var selectedIds = $(this).data('ids');
                if (!selectedIds || selectedIds.length === 0) {
                    toastr.error('No employee selected.');
                    $(settings.deleteModal).modal('hide');
                    return;
                }
                $.ajax({
                    type: 'POST', url: deleteURL,
                    contentType: 'application/json',
                    data: JSON.stringify(selectedIds),
                    success: function (result) {
                        if (result.isSuccess) {
                            // Server-side: just reload current page
                            if (dataTableInstance) dataTableInstance.ajax.reload(null, false);
                            toastr.success(result.message);
                            $('.checkBox').prop('checked', false);
                        } else if (result.refError) {
                            toastr.warning(result.message);
                        } else {
                            toastr.error(result.message, 'Error');
                        }
                        $(settings.deleteModal).modal('hide');
                    },
                    error: function () {
                        toastr.error('An error occurred while deleting.');
                        $(settings.deleteModal).modal('hide');
                    }
                });
            });

            // ── Image helpers ────────────────────────────────────────────────
            $("body").on("click", ".js-file-chooser", function (e) {
                e.preventDefault();
                $($(this).data("target")).trigger("click");
            });
            $("body").on("change", ".js-file", function (e) {
                e.preventDefault();
                showImagePreview($(this), $(this).data("target"));
            });
            $("body").on("click", ".js-clear-file", function (e) {
                e.preventDefault();
                clearImage($(this).data("file"), $(this).data("tag"));
            });
        });

        // ── Helpers ──────────────────────────────────────────────────────────
        function showImagePreview(input, target) {
            if (input[0].files && input[0].files[0]) {
                var reader = new FileReader();
                reader.onload = e => $(target).prop('src', e.target.result);
                reader.readAsDataURL(input[0].files[0]);
            }
        }
        function clearImage(file, tag) {
            var $img = $(file);
            var defaultSrc = $img.data("default");   // default image path

            if (defaultSrc) {
                $img.attr("src", defaultSrc);
            } else {
                $img.removeAttr("src");
            }

            $(tag).val(true);
        }

        // ── Initialize ───────────────────────────────────────────────────────
        function initialize() {
            initDataTable();
            AllDatePicker();
            ConvertToBangla();

            var empId = $('#EmployeeId').val();
            $('#EmployeeId').prop("readonly", !!empId).toggleClass("bg-white", !!empId);
        }

        // ── SERVER-SIDE DataTable ─────────────────────────────────────────────
        function initDataTable() {
            var $grid = $(settings.gridSelector);
            if ($grid.length && $grid.attr('data-grid-config')) {
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                    dataTableInstance = $grid.DataTable();
                    dataTableInstance.ajax.reload(null, false);
                    return;
                }
                if ($.gctlGrid) {
                    dataTableInstance = $.gctlGrid.initFromModel({ gridId: 'HrmEmployee2-grid' });
                    return;
                }
            }

            // Destroy previous instance cleanly
            if (dataTableInstance) {
                dataTableInstance.destroy();
                dataTableInstance = null;
            }

            dataTableInstance = $(settings.gridSelector).DataTable({
                serverSide: true,          // ← key flag
                processing: true,          // shows "Processing…" overlay
                responsive: true,
                destroy: true,
                pageLength: 10,
                lengthMenu: [10, 25, 50, 100],

                // ── AJAX config ───────────────────────────────────────────────
                ajax: {
                    url: loadTableURL,
                    type: 'POST',           // controller action is [HttpPost]
                    // Pass anti-forgery token if your app requires it
                    headers: {
                        'RequestVerificationToken':
                            $('input[name="__RequestVerificationToken"]').val() || ''
                    }
                },

                // ── Column definitions (must match controller columnNames[]) ──
                columns: [
                    {
                        // 0 – checkbox
                        data: null,
                        orderable: false,
                        searchable: false, className: 'text-center',
                        name: '',
                        render: function (data, type, row) {
                            return `<input type="checkbox" class="checkBox"
                                        data-id="${row.employeeId}"
                                        value="${row.employeeId}" />`;
                        }
                    },
                    {
                        // 1 – Employee ID (link)
                        data: 'employeeId',
                        name: 'EmployeeId',
                        className: 'text-center',
                        render: function (data, type, row) {
                            var fullName = (row.firstName || '') + ' ' + (row.lastName || '');
                            return `<a title="Edit ${fullName}"
                                       style="text-decoration:underline;"
                                       href="/HrmEmployee2/Setup?id=${data}">${data}</a>`;
                        }
                    },
                    {
                        // 2 – Full name
                        data: null,
                        name: 'FirstName',
                        render: (d, t, row) =>
                            (row.firstName || '') + ' ' + (row.lastName || '')
                    },
                    {
                        // 3 – Name in Bangla
                        data: null,
                        name: 'FirstName',
                        orderable: false,
                        render: (d, t, row) =>
                            (row.firstNameBangla || '') + ' ' + (row.lastNameBangla || '')
                    },
                    { data: 'sexCode', name: 'SexCode', className: 'text-center' },
                    { data: 'nationalityCode', name: 'NationalityCode', className: 'text-center' },
                    { data: 'nationalIdno', name: 'NationalIdno', className: 'text-center' },
                    { data: 'telephone', name: 'Telephone', className: 'text-center' },
                    {
                        // 8 – Photo
                        data: 'photoUrl',
                        orderable: false,
                        searchable: false,
                        className: 'text-center',
                        render: function (data) {
                            return data
                                ? `<img src="${data}" class="rounded-circle"
                                        height="50" width="50" />`
                                : `<div style="height:50px;width:50px;"></div>`;
                        }
                    },
                    {
                        // 9 – Actions
                        data: null,
                        orderable: false,
                        searchable: false,
                        className: 'text-center',
                        render: function (data, type, row) {
                            var fullName = (row.firstName || '') + ' ' + (row.lastName || '');
                            return `
                                <a title="Edit ${fullName}"
                                   class="btn btn-sm"
                                   href="/HrmEmployee2/Setup?id=${row.employeeId}">
                                    <i class="fas fa-pencil-alt"></i>
                                </a>
                                <button type="button"
                                        title="Delete ${fullName}"
                                        class="btn btn-sm js-HrmEmployee2-delete-confirm"
                                        data-id="${row.employeeId}">
                                    <i class="fas fa-trash fa-sm"></i>
                                </button>`;
                        }
                    }
                ],

                // Default sort: AutoId DESC (column 0 maps to AutoId in SP)
                order: [[1, 'desc']],

                // ── Language overrides ────────────────────────────────────────
                language: {
                    processing: '<i class="fas fa-spinner fa-spin"></i> Loading...',
                    zeroRecords: 'No Employee Information Available.',
                    emptyTable: 'No Employee Information Available.'
                }
            });
        }

        // ── Date pickers ─────────────────────────────────────────────────────
        function AllDatePicker() {
            // flatpickr(
            //     $("#DateOfBirthOrginal, #DateOfBirthCertificate, #MarriageDate"),
            //     CalendarService.createConfig({})
            // );
        }

        // ── English → Bangla transliteration ─────────────────────────────────
        function ConvertToBangla() {
            let engFInput = document.getElementById("FirstName");
            let bnFInput = document.getElementById("FirstNameBangla");
            if (!engFInput || !bnFInput) return;

            const prefixMap = [
                ["MD.", "মো:"], ["Md.", "মো:"], ["md.", "মো:"],
                ["MD", "মো:"], ["Md", "মো:"], ["md", "মো:"],
                ["MR.", "মিস্টার"], ["Mr.", "মিস্টার"], ["mr.", "মিস্টার"],
                ["MR", "মিস্টার"], ["Mr", "মিস্টার"], ["mr", "মিস্টার"],
                ["MS.", "মিসেস"], ["Ms.", "মিসেস"], ["ms.", "মিসেস"],
                ["MS", "মিসেস"], ["Ms", "মিসেস"], ["ms", "মিসেস"],
            ];

            engFInput.addEventListener("input", function () {
                let text = this.value;
                if (text.trim() === "") { bnFInput.value = ""; return; }

                let banglaPrefix = "";
                let remainingText = text;
                for (let [key, value] of prefixMap) {
                    if (text.startsWith(key + " ") || text.startsWith(key)) {
                        banglaPrefix = value;
                        remainingText = text.slice(key.length).trim();
                        break;
                    }
                }
                if (remainingText === "") { bnFInput.value = banglaPrefix; return; }

                fetch("https://inputtools.google.com/request?text=" +
                    encodeURIComponent(remainingText) + "&itc=bn-t-i0-und&num=1")
                    .then(r => r.json())
                    .then(data => {
                        if (data[0] === "SUCCESS") {
                            let bt = data[1][0][1][0];
                            bnFInput.value = banglaPrefix ? banglaPrefix + " " + bt : bt;
                        }
                    }).catch(console.error);
            });

            let engLInput = document.getElementById("LastName");
            let bnLInput = document.getElementById("LastNameBangla");
            if (!engLInput || !bnLInput) return;

            engLInput.addEventListener("input", function () {
                let text = this.value;
                if (text.trim() === "") { bnLInput.value = ""; return; }
                fetch("https://inputtools.google.com/request?text=" +
                    encodeURIComponent(text) + "&itc=bn-t-i0-und&num=1")
                    .then(r => r.json())
                    .then(data => {
                        if (data[0] === "SUCCESS") bnLInput.value = data[1][0][1][0];
                    }).catch(console.error);
            });
        }

        // ── Validation ───────────────────────────────────────────────────────
        function validation() {
            var company = $("#CompanyCode").val();
            var employeeId = $("#EmployeeId").val();
            var fName = $("#FirstName").val();
            var gender = $("#SexCode").val();

            if (!company) { toastr.info('Select Company'); $('#CompanyCode').select2('open'); return false; }
            if (!employeeId) { toastr.info('Select EmployeeId'); $('#EmployeeId').trigger('focus'); return false; }
            if (!fName) { toastr.info('Enter First Name'); $('#FirstName').trigger('focus'); return false; }
            if (!gender) { toastr.info('Select Gender'); $('#SexCode').select2('open'); return false; }
            return true;
        }

        // ── ResetForm ────────────────────────────────────────────────────────
        function ResetForm() {
            $(settings.formSelector)[0].reset();
            ['#Id', '#AutoId', '#EmployeeId', '#FirstName', '#LastName', '#FatherName', '#MotherName',
                '#DateOfBirthCertificate', '#DateOfBirthOrginal', '#DateOfBirthOrginalHidden',
                '#BirthCertificateNo', '#PlaceOfBirth', '#MarriageDate', '#NoOfSon', '#NoOfDaughters',
                '#CardNo', '#UserInfoEmployeeId', '#PersonalEmail', '#NationalIdno', '#TinNo',
                '#Telephone', '#ExtraCurriActivities', '#Remarks', '#FirstNameBangla', '#LastNameBangla',
                '#FatherOccupation', '#MotherOccupation'
            ].forEach(id => $(id).val(''));

            ['#BloodGroupCode', '#NationalityCode', '#SexCode', '#BranchCode',
                '#ReligionCode', '#MaritalStatusCode'
            ].forEach(id => $(id).val(null).trigger('change'));

            $('#LdateModifyHide').hide();
            $(settings.saveSelector).html('<i class="fas fa-save"></i> Save');
            $('.text-danger').text('');
            $(settings.formSelector).attr('action', testSave);
            $("#EmployeeId").prop("readonly", false);
        }

        $("body").on('click', settings.clearSelector, function () { ResetForm(); });

        // ── Excel upload (unchanged) ──────────────────────────────────────────
        $('#excelFileInput').on('change', function () {
            const file = this.files[0];
            const warningEl = $('#checkExcelFileFormate');
            const chooseText = $('#choosefileText');
            if (!file) { chooseText.text('Choose File'); warningEl.hide(); return; }
            const ext = file.name.split('.').pop().toLowerCase();
            if (ext !== 'xlsx' && ext !== 'xls') {
                warningEl.show(); chooseText.text('Choose File'); $(this).val(''); return;
            }
            warningEl.hide(); chooseText.text(file.name);
        });

        $(document).on('click', '#excelUploadForm button[type="submit"]', function (e) {
            e.preventDefault();
            const fileInput = $('#excelFileInput')[0];
            const warningEl = $('#checkExcelFileFormate');
            if (!fileInput.files || fileInput.files.length === 0) {
                toastr.warning('Please choose an Excel file first.'); return;
            }
            const ext = fileInput.files[0].name.split('.').pop().toLowerCase();
            if (ext !== 'xlsx' && ext !== 'xls') { warningEl.show(); return; }
            warningEl.hide();

            const formData = new FormData();
            formData.append('excelFile', fileInput.files[0]);
            const token = $('input[name="__RequestVerificationToken"]').val();
            if (token) formData.append('__RequestVerificationToken', token);

            const $btn = $(this);
            const origHtml = $btn.html();
            $btn.html('<span class="spinner-border spinner-border-sm me-1"></span> Uploading...').prop('disabled', true);

            $.ajax({
                type: 'POST', url: uploadURL,
                data: formData, contentType: false, processData: false,
                success: function (response) {
                    $btn.html(origHtml).prop('disabled', false);
                    if (response.isFormatError) { warningEl.show(); return; }
                    if (response.isSuccess) {
                        let msg = response.message;
                        if (response.failCount > 0) {
                            msg += `<br/>${response.failCount} row(s) failed.`;
                            (response.errors || []).forEach(err => msg += `<br/>• ${err}`);
                        }
                        toastr.success(msg);
                        fileInput.value = '';
                        $('#choosefileText').text('Choose File');
                        // Reload the server-side table
                        if (dataTableInstance) dataTableInstance.ajax.reload(null, false);
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function (xhr) {
                    $btn.html(origHtml).prop('disabled', false);
                    toastr.error(xhr.responseJSON?.message || 'An unexpected error occurred.');
                }
            });
        });

    }; // end $.hrmEmployee2

}(jQuery));