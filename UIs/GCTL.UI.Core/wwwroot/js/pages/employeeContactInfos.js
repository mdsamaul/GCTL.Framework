(function ($) {
    $.employeeContactInfos = function (options) {
        var settings = $.extend({
            baseUrl: "/",
            formSelector: "#EmployeeContactInfos-form",
            formContainer: ".js-EmployeeContactInfos-form-container",
            gridSelector: "#EmployeeContactInfosTable",
            editSelector: ".js-EmployeeContactInfos-edit",
            saveSelector: ".js-EmployeeContactInfos-save",
            selectAllSelector: "#EmployeeContactInfos-check-all",
            deleteSelector: ".js-EmployeeContactInfos-delete-confirm",
            clearSelector: ".js-EmployeeContactInfos-clear",
            load: function () { }
        }, options);

        var baseControllerNameUrl = "/EmployeeContactInfos";
        var saveUrl = baseControllerNameUrl + "/Setup";
        var deleteURL = baseControllerNameUrl + "/Delete";

        $(function () {
            initialize();

            // ── Grid Initialization ──────────────────────────────────────────────
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({
                    gridId: settings.gridSelector.substring(1),
                    deleteUrl: deleteURL,
                    onDeleteSuccess: function () {
                        toastr.success("Deleted successfully");
                        loadTable();
                    }
                });
            }

            // ── Edit & Clear Button ──────────────────────────────────────────────
            $("body").on("click", `${settings.editSelector},${settings.clearSelector}`, function (e) {
                e.preventDefault();
                let url = saveUrl + ($(this).data("id") ? "/" + $(this).data("id") : "");
                loadForm(url).then((data) => {
                    console.info("Form Loaded Successfully");
                    // Rebind ConvertToBangla after form loads
                    ConvertToBangla();
                }).catch((error) => {
                    console.error("Failed to load form", error);
                });
            });

            // ── Save Button ──────────────────────────────────────────────────────
            $("body").on("click", settings.saveSelector, function () {
                var $form = $(settings.formSelector);
                if ($form.valid && !$form.valid()) {
                    return false;
                }
                $form.submit();
            });

            // ── Form Submit ──────────────────────────────────────────────────────
            $("body").on("submit", settings.formSelector, function (e) {
                e.preventDefault();
                var $form = $(this);
                var formData = new FormData($form[0]);
                var actionUrl = $form.attr("action");

                $.ajax({
                    type: "POST",
                    url: actionUrl,
                    data: formData,
                    contentType: false,
                    processData: false,
                    success: function (result) {
                        if (result.isSuccess === false) {
                            toastr.error(result.message || "Failed to save data.");
                        } else if (result.isSuccess === true) {
                            toastr.success(result.message || "Saved Successfully", "Success");
                            loadForm(saveUrl).then(() => {
                                ConvertToBangla();
                            });
                            loadTable();
                        } else {
                            // If it's returning HTML
                            $(settings.formContainer).html(result);
                            ConvertToBangla();
                        }
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Failed to save.");
                    }
                });
            });

            // ── Delete Button Logic ──────────────────────────────────────────────
            function getDeleteModal() {
                var $m = $("#deleteModal");
                if ($m.length) return $m;
                return $("#gcDeleteModal");
            }

            var selectedItems = [];

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

            $("body").on("click", ".js-gc-delete-confirm, .js-delete", function (e) {
                if (!selectedItems || selectedItems.length === 0) return;
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                var $modal = getDeleteModal();
                $.ajax({
                    url: deleteURL,
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
                        } else {
                            toastr.error(response.message);
                        }
                    },
                    error: function (xhr) {
                        $modal.modal("hide");
                        toastr.error(xhr.responseJSON?.message || "Error deleting records.");
                    }
                });
            });

            // ── Fetch Employee Details ───────────────────────────────────────────
            var _suppressEmployeeChange = false;

            $("body").on('change', '#EmployeeId', function () {
                if (_suppressEmployeeChange) return;
                var selectedEmployee = $(this).val();
                if (!selectedEmployee) {
                    if (window.setField) {
                        window.setField('#FullName', '');
                        window.setField('#DesignationName', '');
                        window.setField('#DepartmentName', '');
                    }
                    return;
                }

                $.ajax({
                    url: '/OfficialInfo/GetEmployeeDetailsByCode',
                    type: 'GET',
                    data: { code: selectedEmployee },
                    success: function (response) {
                        if (response && response.data) {
                            var data = response.data;
                            if (window.setField) {
                                window.setField('#FullName', data.employeeName || '');
                                window.setField('#DesignationName', data.designationName || '');
                                window.setField('#DepartmentName', data.departmentName || '');
                            } else {
                                $('#FullName').text(data.employeeName || '');
                                $('#DesignationName').text(data.designationName || '');
                                $('#DepartmentName').text(data.departmentName || '');
                            }
                        }
                    },
                    error: function () {
                        console.error('Failed to fetch employee details.');
                    }
                });
            });

        });

        // ── Load Form ────────────────────────────────────────────────────────────
        function loadForm(url) {
            return new Promise((resolve, reject) => {
                $.ajax({
                    url: url,
                    type: "GET",
                    success: function (result) {
                        $(settings.formContainer).html(result);
                        $(settings.formSelector).attr("action", saveUrl);

                        if (typeof window.initCustomDropdowns === "function") {
                            window.initCustomDropdowns(settings.formContainer, true);
                        }

                        if ($.validator && $.validator.unobtrusive) {
                            $.validator.unobtrusive.parse($(settings.formSelector));
                        }
                        resolve(result);
                    },
                    error: function (error) {
                        toastr.error("Failed to load form.");
                        reject(error);
                    }
                });
            });
        }

        function loadTable() {
            if ($.gctlGrid) {
                $.gctlGrid.reload(settings.gridSelector.substring(1));
            }
        }

        function initialize() {
            ConvertToBangla();
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
                        } catch {
                            return cleanPart;
                        }
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
                    if (this.value.trim() === "") {
                        bnInput.value = "";
                        return;
                    }
                    bnInput.value = await transliterate(this.value);
                });
            }

            bindConvert("ParmanentAddress", "ParmanentAddressBangla");
            bindConvert("PresentAddress", "PresentAddressBangla");
        }
    };

    $(document).ready(function () {
        $.employeeContactInfos();
    });

}(jQuery));