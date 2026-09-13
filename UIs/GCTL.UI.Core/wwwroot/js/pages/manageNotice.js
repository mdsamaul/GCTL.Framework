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
    window.gctlRenderers.renderNoticeFile = function (data, type, row) {
        if (!data?.trim()) return '<span class="text-muted small">No file</span>';
        const fileName = data.split(/[/\\]/).pop();
        return `<a href="/NoticeDocument/${data}" target="_blank" class="file-link" title="Click to open ${fileName}"><i class="fas fa-paperclip me-1"></i>${fileName}</a>`;
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

            getFilterValue: () => ({
                CompanyCodes: utils.toArray($(companySelect).val()),
                BranchCodes: utils.toArray($(branchSelect).val()),
                DepartmentCodes: utils.toArray($(departmentSelect).val()),
                DesignationCodes: utils.toArray($(designationSelect).val()),
                EmployeeCodes: utils.toArray($(employeeSelect).val())
            })
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

                if (selector == companySelect && !hasRealValue) {
                    val = '001';
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
                if ($.fn.DataTable.isDataTable(employeeGrid) && state.empDataTable) {
                    state.empDataTable.destroy();
                    state.empDataTable = null;
                }

                const tableBody = $(employeeGridBody);
                tableBody.empty();

                employees.forEach(emp => {
                    const isOriginal = state.isEditMode && String(emp.employeeId) === String(state.originalEmpId);
                    const checkboxAttrs = state.isEditMode ? 'disabled' : '';
                    const checkedAttr = isOriginal ? 'checked' : '';

                    const row = $(`
                        <tr>
                            <td class="text-center"><input type="checkbox" class="empSelect" ${checkboxAttrs} ${checkedAttr} /></td>
                            <td class="p-1 text-center">${emp.employeeId}</td>
                            <td class="p-1 text-start">${emp.employeeName}</td>
                            <td class="p-1 text-start">${emp.designationName}</td>
                            <td class="p-1 text-center">${emp.departmentName}</td>
                            <td class="p-1 text-center">${emp.branchName}</td>
                            <td class="p-1 text-center">${emp.employeeTypeName}</td>
                            <td class="p-1 text-center">${emp.employmentNature}</td>
                            <td class="p-1 text-center">${emp.joiningDate}</td>
                            <td class="p-1 text-center">${emp.employeeStatus}</td>
                        </tr>
                    `);
                    tableBody.append(row);
                });

                employeeGridManager.initDataTable();
                if (state.isEditMode) $(employeeCheckAll).prop('disabled', true);
            },

            initDataTable: () => {
                state.empDataTable = $(employeeGrid).DataTable({
                    paging: true,
                    pageLength: 10,
                    lengthMenu: [[10, 25, 50, 100, 1000, -1], [10, 25, 50, 100, 1000, "All"]],
                    lengthChange: true,
                    info: true,
                    autoWidth: false,
                    responsive: true,
                    language: {
                        search: "🔍 Search:",
                        lengthMenu: "Show _MENU_ entries",
                        info: "Showing _START_ to _END_ of _TOTAL_ entries",
                        paginate: {
                            first: "First",
                            previous: "Prev",
                            next: "Next",
                            last: "Last"
                        },
                        emptyTable: "No data available",
                        processing: "Loading data..."
                    },
                    columnDefs: [
                        {
                            targets: 0, orderable: false, className: 'no-sort',
                            createdCell: function (td) {
                                $(td).html(`
                                    <div class="d-flex align-items-center justify-content-center h-100">
                                        ${$(td).html()}
                                    </div>
                                `);
                            }
                        },
                        {
                            targets: 8,
                            render: function (data, type, row) {

                                if (type === "sort" || type === "type") {
                                    // Convert to timestamp for proper sorting
                                    const parts = data.split('/');
                                    return new Date(parts[2], parts[1] - 1, parts[0]).getTime();
                                }

                                return data; // display normally
                            }
                        }
                    ],
                    initComplete: function () {
                        utils.hideLoading();
                        $(employeeGrid).closest('.dataTables_wrapper')
                            .find(customSearch).on('keyup', function () {
                                state.empDataTable.search(this.value).draw();
                            });

                    },
                    drawCallback: () => {
                        $(`${employeeGridBody} input[type="checkbox"]`).each(function () {
                            const empId = $(this).closest('tr').find('td:nth-child(2)').text().trim();
                            $(this).prop('checked', state.selectedEmpIds.has(empId));
                        });
                        employeeGridManager.updateSelectAllState();
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
                    const empId = $(this).closest('tr').find('td:nth-child(2)').text().trim();
                    state.selectedEmpIds[$(this).is(':checked') ? 'add' : 'delete'](empId);
                });
            }
        };

        // Notice grid management
        const noticeGridManager = {
            display: () => {
                if ($.fn.DataTable.isDataTable(noticeGrid)) {
                    $(noticeGrid).DataTable().clear().destroy();
                }

                $(noticeGrid).DataTable({
                    processing: true,
                    serverSide: true,
                    ajax: { url: API.paginatedData, type: 'POST' },
                    columns: [
                        {
                            data: null, orderable: false, className: 'text-center',
                            render: (data, type, row) => `<input type="checkbox" data-id="${row.tc}"/>`
                        },
                        {
                            data: 'noticeId', className: 'text-center',
                            render: (data, type, row) => `<a href="#notice-form" class="notice-id-link" data-id="${row.tc}">${data}</a>`
                        },
                        { data: 'noticeTitle', className: 'text-left' },
                        { data: 'noticeDesc', className: 'text-left' },
                        {
                            data: 'entryDate', className: 'text-center',
                            render: data => utils.formatDate(data)
                        },
                        {
                            data: 'priorityLevel', className: 'text-center',
                            render: data => data === 1 ? '<span style="color: #b22222; font-weight: bold;">Important</span>' : 'Normal'
                        },
                        {
                            data: 'status', className: 'text-center',
                            render: data => data === "1" ? "Active" : 'Hold'
                        },
                        {
                            data: 'filePath', className: 'text-center',
                            render: data => {
                                if (!data?.trim()) return '<span class="text-muted">No file</span>';
                                const fileName = data.split(/[/\\]/).pop();
                                return `<a href="/NoticeDocument/${data}" target="_blank" class="file-link" title="Click to open ${fileName}">${fileName}</a>`;
                            }
                        }
                    ],
                    autoWidth: false, info: true, lengthChange: true,
                    lengthMenu: [[10, 25, 50, 100, 1000, -1], [10, 25, 50, 100, 1000, "All"]],
                    ordering: true, pageLength: 10, paging: true, responsive: true, order: [[1, 'desc']],
                    scrollCollapse: true, scrollX: true, searching: true,
                    columnDefs: [{
                        targets: 0, orderable: false, className: 'no-sort',
                        createdCell: function (td) {
                            $(td).html(`
                                 <div class="d-flex align-items-center justify-content-center h-100">
                                    ${$(td).html()}
                                </div>
                            `);
                        }
                    }],
                    language: {
                        search: "🔍 Search:", lengthMenu: "Show _MENU_ entries",
                        info: "Showing _START_ to _END_ of _TOTAL_ entries",
                        paginate: { first: "First", previous: "Prev", next: "Next", last: "Last" },
                        emptyTable: "No data available", processing: "Loading data..."
                    },
                    initComplete: function () {
                        const api = this.api();
                        let debounceTimeout;
                        $('.dataTables_filter input').off().on('input', function () {
                            clearTimeout(debounceTimeout);
                            const searchTerm = this.value;
                            debounceTimeout = setTimeout(() => {
                                api.search(searchTerm).page('first').draw('page');
                            }, 500);
                        });
                    },
                    drawCallback: () => {
                        $(`${noticeGridBody} input[type="checkbox"]`).each(function () {
                            const id = $(this).data('id');
                            $(this).prop('checked', state.selectedNoticeIds.has(id));
                        });
                        const total = $(`${noticeGridBody} input[type="checkbox"]`).length;
                        const checked = $(`${noticeGridBody} input[type="checkbox"]:checked`).length;
                        $(noticeCheckAll).prop('checked', total > 0 && total === checked);
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

        // Initialize employee grid
        const initEmployeeGrid = () => {
            //utils.showLoading();
            setTimeout(() => {
                if (state.empDataTable) state.empDataTable.destroy();
                employeeGridManager.initDataTable();
            }, 100);
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
            initEmployeeGrid();
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