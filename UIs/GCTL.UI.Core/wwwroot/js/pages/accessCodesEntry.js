(function ($) {
    $.accessCodeEntries = function (options) {
        const settings = $.extend({

            onSaved: function (id) {

            },
            onDelete: function () {

            },
            load: function () {

            }
        }, options);

        // Selectors
        const selectors = {
            grid: '#access-code-grid',
            form: '#access-code-form',
            checkAll: '#access-code-check-all',
            btnSave: '.js-access-code-save',
            btnDelete: '.js-access-code-delete-confirm',
            btnClear: '.js-access-code-clear',
            inputCode: '.js-access-code',
            inputName: '.js-access-code-name',
            hiddenLastCode: 'input[name="lastCode"]',
            quickAddModal: "#quickAddModal",
        };

        let dataTable;

        const initialize = () => {
            initGrid();
            bindEvents();
        };

        const initGrid = () => {
            dataTable = $(selectors.grid).DataTable({
                ajax: {
                    url: '/menuTab/GetAccessListTable',
                    type: 'GET',
                    dataSrc: function (json) {
                        return json.data;
                    }
                },
                dom: '<"d-flex justify-content-between align-items-center mb-2"lf>rt<"d-flex justify-content-between align-items-center mt-2"ip>',
                columns: [
                    {
                        data: 'accessCodeId',
                        orderable: false,
                        className: 'text-center px-0 no-sort',
                        width: "40px",
                        render: function (data) {
                            return `<input type="checkbox" class="js-grid-checkbox" value="${data}" />`;
                        }
                    },
                    {
                        data: 'accessCodeId', className: "text-center",
                        render: function (data, type, row) {
                            return `<button type="button" class="btn btn-link p-0 access-code-id-btn" 
                                data-id="${data}">
                                ${data}
                            </button>`;
                        }
                    },
                    { data: 'accessCodeName' }
                ],
                columnDefs: [
                    {
                        targets: [0], orderable: false,
                        createdCell: function (td) {
                            $(td).html(`<div class="d-flex align-items-center justify-content-center px-0">${$(td).html()}</div>`);
                        }
                    }
                ],
                lengthChange: true,
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],
                autoWidth: false,
                responsive: true,
                fixedHeader: true,
                order: [[1, "desc"]],
                bDestroy: true,
            });
        };

        const bindEvents = () => {
            // Save / Update
            $(selectors.btnSave).on('click', function () {
                saveAccessCode();
            });

            // Multi-Delete
            $(selectors.btnDelete).on('click', function () {
                deleteSelected();
            });

            // Clear Form
            $(selectors.btnClear).on('click', function () {
                clearForm();
            });

            // Select All Checkbox
            $(selectors.checkAll).on('change', function () {
                $('.js-grid-checkbox').prop('checked', $(this).prop('checked'));
            });

            // Row Click (Edit)
            $(document).on('click', ".access-code-id-btn", function () {
                const row = $(this).closest('tr');
                const data = dataTable.row(row).data();
                if (data) {
                    fillForm(data);
                }
            });
        };

        const saveAccessCode = () => {
            const $form = $(selectors.form);

            if (!$form.valid()) return;

            // serialize() naturally includes the Anti-Forgery Token
            const formData = $form.serialize();

            $.ajax({
                url: '/MenuTab/AccessCodeSetup',
                type: 'POST',
                data: formData,
                success: function (response) {
                    if (response.isSuccess) {
                        alert(response.message || "Saved successfully");
                        if (settings.isModal && typeof settings.onSaved === 'function') {
                            var savedId = response && response.lastCode ? response.lastCode : null;
                            settings.onSaved(savedId);
                            return;
                        }
                        clearForm();
                        initGrid();
                    } else {
                        alert("Error: " + response.message);
                    }
                },
                error: function (xhr) {
                    alert("An error occurred on the server.");
                }
            });
        };

        const deleteSelected = () => {
            const selectedIds = [];
            $('.js-grid-checkbox:checked').each(function () {
                selectedIds.push($(this).val());
            });

            if (selectedIds.length === 0) {
                alert('Please select at least one item to delete.');
                return;
            }

            if (confirm(`Are you sure you want to delete ${selectedIds.length} items?`)) {
                $.ajax({
                    url: '/menuTab/DeleteAccessCodes',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(selectedIds),
                    success: function (response) {
                        if (settings.isModal && typeof settings.onDelete === 'function') {
                            settings.onDelete();
                            return;
                        }
                        alert('Deleted successfully!');
                        $(selectors.checkAll).prop('checked', false);
                        dataTable.ajax.reload();
                        clearForm();
                    },
                    error: function (xhr) {
                        alert('Error occurred while deleting.');
                    }
                });
            }
        };

        const fillForm = (data) => {
            $(selectors.inputCode).val(data.accessCodeId);
            $(selectors.inputName).val(data.accessCodeName);
            $(selectors.hiddenLastCode).val(data.accessCodeId);
        };

        const clearForm = () => {
            $(selectors.form)[0].reset();
            $(selectors.hiddenLastCode).val('');
            $(selectors.inputCode).prop('readonly', false);
            // Reset validation messages
            $(selectors.form).validate().resetForm();
        };

        initialize();
    };
})(jQuery);