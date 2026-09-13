(function ($) {
    // Global renderers for Custom Grid
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.formatDate = function (data, type, row) {
        if (!data) return '';
        const date = new Date(data);
        return `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
    };
    window.gctlRenderers.renderPriority = function (data, type, row) {
        return (data === 1 || data === "1") ? '<span style="color: #b22222; font-weight: bold;">Important</span>' : 'Normal';
    };
    window.gctlRenderers.renderActiveStatus = function (data, type, row) {
        return (data === "1" || data === 1) ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Hold</span>';
    };
    window.gctlShowFileModal = function (fileUrl, fileName) {
        let $modal = $('#gctlFileViewerModal');
        if (!$modal.length) {
            const modalHtml = `
            <div class="modal fade" id="gctlFileViewerModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-xl modal-dialog-centered">
                    <div class="modal-content shadow-lg border-0">
                        <div class="modal-header bg-light border-bottom-0 py-2">
                            <h5 class="modal-title fs-6 fw-semibold text-secondary" id="gctlFileViewerModalTitle">File Viewer</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body p-0 text-center" style="height: 85vh; background: #e9ecef;">
                            <div id="gctlFileViewerContent" style="width: 100%; height: 100%; display: flex; align-items: center; justify-content: center;"></div>
                        </div>
                    </div>
                </div>
            </div>`;
            $('body').append(modalHtml);
            $modal = $('#gctlFileViewerModal');
            
            // Cleanup iframe on hide to stop audio/video if any
            $modal.on('hidden.bs.modal', function () {
                $('#gctlFileViewerContent').empty();
            });
        }
        
        $('#gctlFileViewerModalTitle').text(fileName);
        const $content = $('#gctlFileViewerContent');
        
        // Show loading state briefly
        $content.html('<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>');
        
        const ext = fileName.split('.').pop().toLowerCase();
        const imgExts = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'];
        
        setTimeout(() => {
            if (imgExts.includes(ext)) {
                $content.html(`<img src="${fileUrl}" style="max-width: 100%; max-height: 100%; object-fit: contain;" alt="${fileName}" />`);
            } else {
                $content.html(`<iframe src="${fileUrl}" style="width: 100%; height: 100%; border: none;"></iframe>`);
            }
        }, 150);
        
        const modalInstance = new bootstrap.Modal($modal[0]);
        modalInstance.show();
    };

    window.gctlRenderers.renderNoticeFile = function (data, type, row) {
        if (!data?.trim()) return '<span class="text-muted small">No file</span>';
        const fileName = data.split(/[/\\]/).pop();
        const fileUrl = `/NoticeDocument/${data}`;
        return `<a href="javascript:void(0)" onclick="window.gctlShowFileModal('${fileUrl}', '${fileName}')" class="file-link text-primary fs-5" title="Click to view ${fileName}"><i class="bi bi-eye-fill"></i></a>`;
    };

    $.noticeManagement = function (options) {
        // Configuration with destructuring and defaults
        const {
            baseUrl = "/",
            noticeForm = "#notice-form",
            tcField = "#Tc",
            noticeIdField = "#NoticeId",
            noticeTitleField = "#NoticeTitle",
            noticeDescField = "#NoticeDesc",
            statusField = "#Status",
            entryDateField = "#EntryDate",
            priorityLevelField = "#PriorityLevel",
            fileUploadField = "#fileUpload",
            companySelect = "#companySelect",
            branchSelect = "#branchSelect",
            departmentSelect = "#departmentSelect",
            designationSelect = "#designationSelect",
            employeeSelect = "#employeeSelect",
            activityStatusSelect = "#activityStatusSelect",
            employeeGrid = "#employee-filter-grid",
            employeeGridBody = "#employee-filter-grid tbody",
            employeeCheckAll = "#employee-check-all",
            noticeGrid = "#notice-grid",
            noticeGridBody = "#notice-grid tbody",
            noticeCheckAll = "#notice-check-all",
            customSearch = "#custom-search",
            saveBtn = ".js-notice-save, .js-notice-dec-save, .toolbar-btn-save",
            deleteBtn = ".js-notice-delete-confirm, .js-notice-dec-delete-confirm, .toolbar-btn-delete",
            clearBtn = ".js-notice-clear, .js-notice-dec-clear, .toolbar-btn-clear",
            sendMailBtn = "#sendMail",
            load = () => console.log("Loading notice management..."),
            onSave = () => console.log("Notice saved successfully"),
            onDelete = () => console.log("Notice deleted successfully"),
            onEmailSent = () => console.log("Email sent successfully")
        } = options || {};

        // API endpoints
        const API = {
            filterEmp: `${baseUrl}/getFilterEmp`,
            generateId: `${baseUrl}/GenerateNewId`,
            saveNotice: `${baseUrl}/SaveNotice`,
            getById: `${baseUrl}/GetById`,
            bulkDelete: `${baseUrl}/BulkDelete`,
            sendEmail: `${baseUrl}/SendNoticeToEmployee`,
            paginatedData: `${baseUrl}/GetPaginatedData`,
            getFile: `${baseUrl}/GetNoticeFile`
        };

        // State management
        const state = {
            empDataTable: null,
            filterChangeBound: false,
            originalEmpId: null,
            selectedEmpIds: new Set(),
            selectedNoticeIds: new Set(),
            isEditMode: false,
            isProgrammaticChange: false
        };

        // Utility functions
        const utils = {
            toArray: val => val ? (Array.isArray(val) ? val : [val]) : [],

            showLoading: () => $("#loadingOverlay").fadeIn(200),
            hideLoading: () => $("#loadingOverlay").fadeOut(200),

            showToast: (type, message) => {
                if (typeof toastr !== 'undefined') {
                    const title = { success: 'Success', error: 'Error' }[type] || 'Warning';
                    toastr[type](message, title);
                } else {
                    alert(message);
                }
            },

            showConfirmation: (message, title, callback) => {
                if (typeof Swal === 'undefined') {
                    if (confirm(`${message}`)) {
                        callback();
                    }
                    return;
                }

                Swal.fire({
                    title: title,
                    text: message,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Yes, proceed',
                    cancelButtonText: 'No, cancel'
                }).then(function (result) {
                    if (result.value === true || result.isConfirmed) {
                        callback();
                    }
                });
            },

            formatDate: dateStr => {
                if (!dateStr) return '';
                const date = new Date(dateStr);
                return `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
            },

            getFilterValue: () => {
                const getArray = (val) => {
                    if (!val) return [];
                    return Array.isArray(val) ? val : [val];
                };
                return {
                    companyCodes: getArray($(companySelect).val()),
                    branchCodes: getArray($(branchSelect).val()),
                    departmentCodes: getArray($(departmentSelect).val()),
                    designationCodes: getArray($(designationSelect).val()),
                    employeeCodes: getArray($(employeeSelect).val()),
                    employeeStatus: $(activityStatusSelect).val() || ['01']
                };
            }
        };

        // Loading overlay setup
        const setupLoadingOverlay = () => {
            if ($("#loadingOverlay").length === 0) {
                $("body").append(`
                    <div id="loadingOverlay" style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:9999;justify-content:center;align-items:center;">
                        <div style="background:white;padding:20px;border-radius:5px;box-shadow:0 0 10px rgba(0,0,0,0.3);text-align:center;">
                            <div class="spinner-border text-primary" role="status"></div>
                        </div>
                    </div>
                `);
            }
        };

        // Multiselect configuration
        const initializeMultiselects = (configs) => {
            Object.entries(configs).forEach(([selector, text]) => {
                if ($(selector).length) {
                    $(selector).multiselect({
                        enableFiltering: true,
                        includeSelectAllOption: true,
                        selectAllText: 'Select All',
                        nonSelectedText: `Select ${text}`,
                        nSelectedText: 'Selected',
                        allSelectedText: 'All Selected',
                        filterPlaceholder: 'Search...',
                        buttonWidth: '100%',
                        maxHeight: 350,
                        filterBehavior: 'text',
                        enableCaseInsensitiveFiltering: true,
                        buttonText: (options) => {
                            if (options.length === 0) return `Select ${text}`;
                            return options.length > 1 ? `${options.length} Selected` : $(options[0]).text();
                        }
                    });
                }
            });
        };


        // Dropdown management
        const dropdownManager = {
            populate: (selector, dataList) => {
                const $select = $(selector);
                
                // Format data for setSelect (expects {id/value, text})
                const mappedData = dataList.map(item => ({
                    id: item.code,
                    text: item.name
                }));
                
                let instance = $select.data('customDdInstance');
                // Read value from instance if possible, because the underlying <select> might be empty right now
                let val = instance && instance.selected && instance.selected.length > 0 ? instance.selected[0] : $select.val();
                let hasRealValue = val && (typeof val === 'string' ? val.trim() !== '' : val.length > 0);
                
                let hasData = instance && instance.data && instance.data.length > 0;
                
                // If the dropdown already has a user-selected value AND it already has its options loaded, do NOT rebuild it!
                // This completely stops the "reload/flicker" effect when selecting an item, 
                // while ensuring it still loads properly on the first page load!
                if (hasRealValue && hasData) {
                    return;
                }
                
                // Ensure the underlying DOM <select> gets the new options!
                // This is critical so that .val(val) succeeds and form submission works.
                $select.empty();
                dataList.forEach(item => {
                    $select.append(`<option value="${item.code}">${item.name}</option>`);
                });
                if (val) $select.val(val);

                state.isProgrammaticChange = true;
                if (typeof setSelect === "function") {
                    setSelect(selector, val, false, mappedData);
                } else {
                    $select.val(val);
                }
                setTimeout(() => state.isProgrammaticChange = false, 50);
            },

            clearDependent: selectors => {
                state.isProgrammaticChange = true;
                selectors.forEach(selector => {
                    const $select = $(selector);
                    $select.empty(); // Physically remove old options to prevent auto-selecting the first option
                    
                    if (typeof setSelect === "function") {
                        setSelect(selector, '', false, []);
                        const instance = $select.data('customDdInstance');
                        if (instance) {
                            instance.data = [];
                            instance.selected = [];
                            instance.$input.val(''); // Make sure the text input is cleared so the placeholder shows!
                            instance._renderList([]);
                        }
                    }
                });
                setTimeout(() => state.isProgrammaticChange = false, 50);
            },

            setupCascading: () => {
                const clearMap = {
                    [companySelect]: [branchSelect, departmentSelect, designationSelect, employeeSelect],
                    [branchSelect]: [departmentSelect, designationSelect, employeeSelect],
                    [departmentSelect]: [designationSelect, employeeSelect],
                    [designationSelect]: [employeeSelect]
                };

                Object.entries(clearMap).forEach(([parent, children]) => {
                    $(parent).off("change.clearDropdowns").on("change.clearDropdowns", () => {
                        dropdownManager.clearDependent(children);
                    });
                });
            }
        };

        // Employee grid management
        const employeeGridManager = {
            load: employees => {
                let dt = $.gctlGrid.getTable('employee-filter-grid');
                if (!dt) {
                    dt = employeeGridManager.initDataTable();
                }

                if (dt) {
                    // Update the state array before re-drawing if needed, though checkboxes will bind via delegate
                    dt.clear();
                    dt.rows.add(employees);
                    dt.draw();
                }

                if (state.isEditMode) $(employeeCheckAll).prop('disabled', true);
            },

            initDataTable: () => {
                return $.gctlGrid.initFromModel({ 
                    gridId: 'employee-filter-grid',
                    extraOptions: {
                        createdRow: function (row, data, dataIndex) {
                            if (state.isEditMode && String(data.employeeId) === String(state.originalEmpId)) {
                                $(row).find('.empSelect').prop('checked', true);
                            }
                            if (state.isEditMode) {
                                $(row).find('.empSelect').prop('disabled', true);
                            }
                        },
                        drawCallback: () => {
                            $(`${employeeGridBody} input[type="checkbox"]`).each(function () {
                                const empId = $(this).data('id') || $(this).val();
                                $(this).prop('checked', state.selectedEmpIds.has(empId));
                            });
                            employeeGridManager.updateSelectAllState();
                        }
                    }
                });
            },

            updateSelectAllState: () => {
                const total = $(`${employeeGridBody} input[type="checkbox"]`).not(':disabled').length;
                const checked = $(`${employeeGridBody} input[type="checkbox"]`).not(':disabled').filter(':checked').length;
                $(employeeCheckAll).prop('checked', total > 0 && total === checked);
            },

            updateSelectedIds: () => {
                $(`${employeeGridBody} input[type="checkbox"]`).each(function () {
                    const empId = $(this).data('id') || $(this).val();
                    state.selectedEmpIds[$(this).is(':checked') ? 'add' : 'delete'](empId);
                });
            }
        };

        // Notice grid management
        const noticeGridManager = {
            display: () => {
                $.gctlGrid.initFromModel({ 
                    gridId: 'notice-grid',
                    extraOptions: {
                        drawCallback: () => {
                            $(`${noticeGridBody} input[type="checkbox"]`).each(function () {
                                const id = $(this).data('id') || $(this).val();
                                $(this).prop('checked', state.selectedNoticeIds.has(id));
                            });
                            const total = $(`${noticeGridBody} input[type="checkbox"]`).length;
                            const checked = $(`${noticeGridBody} input[type="checkbox"]:checked`).length;
                            $(noticeCheckAll).prop('checked', total > 0 && total === checked);
                        }
                    }
                });
            },

            updateSelectedIds: () => {
                $(`${noticeGridBody} input[type="checkbox"]`).each(function () {
                    const id = $(this).data('id');
                    state.selectedNoticeIds[$(this).is(':checked') ? 'add' : 'delete'](id);
                });
            }
        };

        // API handlers
        const api = {
            loadFilterEmp: () => {
                //utils.showLoading();
                $.ajax({
                    url: API.filterEmp,
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(utils.getFilterValue()),
                    success: res => {
                        console.log("Filtered employees loaded successfully:", res);
                        const { lookupData, employees } = res;
                        employeeGridManager.load(employees);

                        if (lookupData) {
                            if (lookupData.companies) dropdownManager.populate(companySelect, lookupData.companies);
                            if (lookupData.departments) dropdownManager.populate(departmentSelect, lookupData.departments);
                            if (lookupData.designations) dropdownManager.populate(designationSelect, lookupData.designations);
                            if (lookupData.employees) dropdownManager.populate(employeeSelect, lookupData.employees);
                            if (lookupData.branches) dropdownManager.populate(branchSelect, lookupData.branches);
                        }

                        dropdownManager.setupCascading();
                        api.bindFilterEvents();
                    },
                    error: (xhr, status, error) => {
                        console.error("Error loading filtered employees:", error);
                        utils.showToast('error', 'Failed to load employee data');
                    },
                    complete: utils.hideLoading
                });
            },

            bindFilterEvents: () => {
                if (!state.filterChangeBound) {
                    // Bind change events to reload filter
                    const filterSelectors = [companySelect, branchSelect, departmentSelect, designationSelect, employeeSelect, activityStatusSelect].join(',');
                    $(filterSelectors).off("change.loadFilter").on("change.loadFilter", function () {
                        // Prevent redundant calls on empty values during clearing
                        if (state.isProgrammaticChange) return;

                        if (!state.filterChangeBound) {
                            state.filterChangeBound = true;
                            setTimeout(() => {
                                api.loadFilterEmp();
                                state.filterChangeBound = false;
                            }, 100);
                        }
                    });
                }
            },

            loadNoticeId: () => {
                $.ajax({
                    url: API.generateId,
                    type: "GET",
                    dataType: "json",
                    success: data => data && $(noticeIdField).val(data),
                    error: (xhr, status, error) => {
                        console.error("Error fetching Notice Id: ", error);
                        utils.showToast('error', 'Failed to generate notice ID');
                    }
                });
            },

            saveNotice: () => {
                if (!forms.validate()) return;

                const formData = new FormData();
                formData.append('Tc', $(tcField).val().trim() || 0);
                formData.append('NoticeId', $(noticeIdField).val());
                formData.append('NoticeTitle', $(noticeTitleField).val());
                formData.append('NoticeDesc', $(noticeDescField).val());
                formData.append('Status', $(statusField).is(':checked') ? '1' : '0');
                formData.append('EntryDate', $(entryDateField).val());
                formData.append('PriorityLevel', $(priorityLevelField).val());

                const fileInput = $(fileUploadField)[0];
                if (fileInput?.files.length > 0) {
                    formData.append('formFile', fileInput.files[0]);
                }

                utils.showLoading();
                $.ajax({
                    url: API.saveNotice,
                    type: "POST",
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: () => {
                        utils.showToast("success", "Data Saved Successfully");
                        api.loadNoticeData();
                        forms.clear();
                        api.loadNoticeId();
                        onSave();
                    },
                    error: (xhr, status, error) => {
                        utils.showToast("error", "Error saving data");
                        console.error("Error saving data:", error);
                    },
                    complete: utils.hideLoading
                });
            },

            sendEmail: () => {
                if (!forms.validateEmail()) return;

                const data = {
                    EmployeeIds: Array.from(state.selectedEmpIds),
                    Tcs: Array.from(state.selectedNoticeIds)
                };

                utils.showLoading();
                $.ajax({
                    url: API.sendEmail,
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(data),
                    success: () => {
                        utils.showToast("success", "Email Sent Successfully");
                        forms.clear();
                        api.loadNoticeId();
                        onEmailSent();
                    },
                    error: (xhr, status, error) => {
                        utils.showToast("error", "Error sending email");
                        console.error("Error sending email:", error);
                    },
                    complete: utils.hideLoading
                });
            },

            bulkDelete: () => {
                const selectedIds = Array.from(state.selectedNoticeIds);
                if (selectedIds.length === 0) {
                    utils.showToast("warning", "Please select records to delete");
                    return;
                }

                //if (!confirm(`Are you sure you want to delete ${selectedIds.length} selected notice(s)?`)) return;
                utils.showConfirmation(
                    `Are you sure you want to delete ${selectedIds.length} selected record(s)?`,
                    'Confirmation',
                    function () {
                        utils.showLoading();
                        $.ajax({
                            url: API.bulkDelete,
                            type: "POST",
                            contentType: "application/json",
                            data: JSON.stringify({ Tcs: selectedIds }),
                            success: response => {
                                state.selectedNoticeIds.clear();
                                utils.showToast("success", response.message || "Successfully deleted");
                                api.loadNoticeData();
                                api.loadNoticeId();
                                forms.clear();
                                onDelete();
                            },
                            error: (xhr, status, error) => {
                                console.error("Error details:", xhr.responseText);
                                utils.showToast("error", "Error deleting notice records");
                            },
                            complete: utils.hideLoading
                        });
                    }
                );
            },

            populateForm: id => {
                $.ajax({
                    url: `${API.getById}/${id}`,
                    type: "GET",
                    success: res => {
                        if (!res?.data) {
                            utils.showToast("error", "Invalid data format received");
                            return;
                        }

                        const { data } = res;
                        forms.clear();
                        state.isEditMode = true;

                        $(tcField).val(data.tc);
                        $(noticeIdField).val(data.noticeId);
                        $(noticeTitleField).val(data.noticeTitle);
                        $(noticeDescField).val(data.noticeDesc);
                        $(statusField).prop('checked', data.status === '1');
                        $(priorityLevelField).val(data.priorityLevel);

                        $("#DateInfo").show();

                        $("#LDate").text(data.ldate ? moment(data.ldate).format('DD/MM/YYYY') : '');
                        $("#ModifyDate").text(data.modifyDate ? moment(data.modifyDate).format('DD/MM/YYYY') : '');
                    },
                    error: (xhr, status, error) => {
                        utils.showToast("error", "Error loading notice data");
                        console.error("Error loading notice:", error);
                    }
                });
            },

            loadNoticeData: () => {
                // utils.showLoading();
                noticeGridManager.display();
                // utils.hideLoading();
            }
        };

        // Form management
        const forms = {
            validate: () => {
                if (!$(noticeTitleField).val()) {
                    utils.showToast("warning", "Please enter Notice Title");
                    return false;
                }

                const fileInput = $(fileUploadField)[0];
                if (fileInput?.files.length > 0) {
                    const file = fileInput.files[0];
                    const maxSize = 5 * 1024 * 1024;
                    const allowTypes = ['image/jpeg', 'image/png', 'image/gif', 'application/pdf'];

                    if (file.size > maxSize) {
                        utils.showToast("warning", "File size should not exceed 5MB");
                        return false;
                    }

                    if (!allowTypes.includes(file.type)) {
                        utils.showToast("warning", "Only JPG, PNG, GIF, PDF files are allowed");
                        return false;
                    }
                }
                return true;
            },

            validateEmail: () => {
                if (state.selectedEmpIds.size === 0) {
                    utils.showToast("warning", "Please select at least one employee");
                    $(employeeGridBody).focus();
                    return false;
                }

                if (state.selectedNoticeIds.size === 0) {
                    utils.showToast("warning", "Please select a notice to send");
                    $(noticeGrid).focus();
                    return false;
                }
                return true;
            },

            clear: () => {
                state.isEditMode = false;
                const fields = [fileUploadField, tcField, noticeIdField, noticeTitleField, noticeDescField, entryDateField, priorityLevelField];
                fields.forEach(field => $(field).val(''));

                $(statusField).prop('checked', false);

                const checkboxes = [employeeGridBody, noticeGridBody];
                checkboxes.forEach(grid => {
                    $(`${grid} input[type="checkbox"]`).prop('checked', false).prop('disabled', false);
                });

                const checkAllBoxes = [employeeCheckAll, noticeCheckAll];
                checkAllBoxes.forEach(checkbox => {
                    $(checkbox).prop('checked', false).prop('disabled', false);
                });

                $("#DateInfo").hide();

                $("#LDate").text('');
                $("#ModifyDate").text('');

                state.selectedNoticeIds.clear();
                state.selectedEmpIds.clear();
            },

            setupEnterKeyNavigation: () => {
                const $form = $(noticeForm);
                if (!$form.length) return;

                $form.on('keydown', 'input, select, textarea, button, [tabindex]:not([tabindex="-1"])', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        const $focusable = $form.find('input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button, [href], [tabindex]:not([tabindex="-1"])').filter(':visible');
                        const index = $focusable.index(this);
                        if (index > -1) {
                            const $next = $focusable.eq(index + 1).length ? $focusable.eq(index + 1) : $focusable.eq(0);
                            $next.focus();
                        }
                    }
                });
            }
        };

        // Event binding
        const bindEvents = () => {
            // Form events
            $(saveBtn).on('click', api.saveNotice);
            $(deleteBtn).on('click', api.bulkDelete);
            $(sendMailBtn).on('click', api.sendEmail);
            $(clearBtn).on('click', () => {
                forms.clear();
                api.loadNoticeId();
            });

            // Grid events
            $(document).on("click", ".notice-id-link", function () {
                const id = $(this).data("id");
                if (id) api.populateForm(id);
            });

            // Checkbox events
            $(noticeCheckAll).on('change', function () {
                const isChecked = $(this).is(':checked');
                $(`${noticeGridBody} input[type="checkbox"]`).prop('checked', isChecked);
                noticeGridManager.updateSelectedIds();
            });

            $(noticeGridBody).on('change', 'input[type="checkbox"]', function () {
                const total = $(noticeGridBody).find('input[type="checkbox"]').length;
                const checked = $(noticeGridBody).find('input[type="checkbox"]:checked').length;
                $(noticeCheckAll).prop('checked', total === checked);
                noticeGridManager.updateSelectedIds();
            });

            $(employeeCheckAll).on('change', function () {
                const isChecked = $(this).is(':checked');
                $(`${employeeGridBody} input[type="checkbox"]`).not(':disabled').prop('checked', isChecked);
                employeeGridManager.updateSelectedIds();
            });

            $(employeeGridBody).on('change', 'input[type="checkbox"]', function () {
                employeeGridManager.updateSelectedIds();
                employeeGridManager.updateSelectAllState();
            });

            // Filter events
            $(`${companySelect}, ${departmentSelect}, ${designationSelect}`).on("change", api.loadFilterEmp);
        };


        // Main initialization
        const init = () => {
            load();
            setupLoadingOverlay();

            if (typeof $.fn.multiselect !== 'undefined') {
                initializeMultiselects({
                    '#companySelect': 'Select Company',
                    '#branchSelect': 'Select Branch',
                    '#employeeSelect': 'Select Employee',
                    '#departmentSelect': 'Select Department',
                    '#designationSelect': 'Select Designation'
                });
            } else {
                console.error('Bootstrap Multiselect not loaded!');
            }

            bindEvents();
            api.loadFilterEmp();
            api.loadNoticeId();
            api.loadNoticeData();
            forms.setupEnterKeyNavigation();

            $(window).on('load', () => {
                $(noticeForm)[0]?.scrollIntoView({ behavior: 'smooth', block: 'start' });
            });
        };

        init();
    };
})(jQuery);