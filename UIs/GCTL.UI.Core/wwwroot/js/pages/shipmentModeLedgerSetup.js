(function ($) {
    $.shipmentModeLedgerSetup = function (options) {

        //#region Default options

        var settings = $.extend({
            baseUrl: "/ShipmentModeLedgerSetup",
            formSelector: "#ShipmentModeLedgerSetup-form",
            formContainer: ".js-ShipmentModeLedgerSetup-form-container",
            gridSelector: "#ShipmentModeLedgerSetup-grid",
            gridContainer: ".js-ShipmentModeLedgerSetup-grid-container",
            editSelector: ".js-ShipmentModeLedgerSetup-edit",
            saveSelector: ".js-ShipmentModeLedgerSetup-save",
            selectAllSelector: "#js-ShipmentModeLedgerSetup-checkall",
            deleteSelector: ".js-ShipmentModeLedgerSetup-delete-confirm",
            deleteModal: "#ShipmentModeLedgerSetup-delete-modal",
            finalDeleteSelector: ".js-ShipmentModeLedgerSetup-delete",
            clearSelector: ".js-ShipmentModeLedgerSetup-clear",
            topSelector: ".js-go",
            decimalSelector: ".js-ShipmentModeLedgerSetup-decimalplaces",
            maxDecimalPlace: 5,
            showNagativeFormat: false,
            availabilitySelector: ".js-ShipmentModeLedgerSetup-check-availability",
            haseFile: false,
            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",
            lastCodeSelector: '#lastCode',
            load: function () {

            }
        }, options);

        var base = (settings.baseUrl || "/ShipmentModeLedgerSetup").replace(/\/+$/, "");
        var saveUrl = base + "/Setup";
        var deleteUrl = base + "/Delete";
        var selectedItems = [];

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        //#endregion

        $(() => {
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'ShipmentModeLedgerSetup-grid' });
            }

            loadTable();
            $('#LdateModifyHide').hide();
            $('#sectionBreak').hide();

            //#region formatDateTime

            function formatDateTime(dateStr) {
                if (!dateStr) return '';
                var date = new Date(dateStr);
                if (isNaN(date.getTime())) return dateStr;
                return date.toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit',
                    hour12: true
                });
            }

            //#endregion

            //#region Dropdown Helpers

            function setDropdownValue(selector, val) {
                if (typeof setSelect === "function") {
                    setSelect(selector, val, false);
                }
                $(selector).val(val).trigger('change');
                if (typeof window.reinitCustomDropdownById === "function") {
                    var id = selector.replace('#', '');
                    window.reinitCustomDropdownById(id);
                }
            }

            function resetSelectField(selector) {
                if (typeof setSelect === "function") {
                    setSelect(selector, '', false);
                }
                $(selector).val('').trigger('change');
                if (typeof window.reinitCustomDropdownById === "function") {
                    var id = selector.replace('#', '');
                    window.reinitCustomDropdownById(id);
                }
            }

            //#endregion

            //#region GetById (Edit)

            $("body").off('click', settings.editSelector).on('click', settings.editSelector, function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                if (!id) return;

                $.ajax({
                    url: base + '/GetById',
                    type: 'GET',
                    data: { code: id },
                    success: function (data) {
                        if (data) {
                            var tcVal = (data.tc && parseInt(data.tc) > 0) ? data.tc : (data.expenseTypeId ? 1 : 0);
                            $('#Tc').val(tcVal);
                            $('#Id').val(data.id || '');
                            $('#ExpenseTypeId').val(data.expenseTypeId || '');

                            setDropdownValue('#ExpenseType', data.expenseType || '');
                            setDropdownValue('#ExpenseLedgerCodeNo', data.expenseLedgerCodeNo || '');
                            setDropdownValue('#RevenueLedgerCodeNo', data.revenueLedgerCodeNo || '');
                            setDropdownValue('#AdvanceLedgerCode', data.advanceLedgerCode || '');

                            $(settings.deleteSelector).data('id', data.expenseTypeId);

                            $('#LdateModifyHide').show();
                            $('#sectionBreak').show();

                            if (data.ldate) {
                                $('#creationDateSpan').text(formatDateTime(data.ldate));
                            } else {
                                $('#creationDateSpan').text('');
                            }

                            if (data.modifyDate) {
                                $('#modifyDateSpan').text(formatDateTime(data.modifyDate));
                            } else {
                                $('#modifyDateSpan').text('');
                            }
                        }
                    },
                    error: function () {
                        toastr.error('Failed to load shipment mode ledger setup data.');
                    }
                });
            });

            //#endregion

            //#region Save

            $("body").off('click', settings.saveSelector).on('click', settings.saveSelector, function (e) {
                e.preventDefault();

                var $valid = $(settings.formSelector).valid();
                if (!$valid) {
                    return false;
                }

                var data;
                if (settings.haseFile)
                    data = new FormData($(settings.formSelector)[0]);
                else
                    data = $(settings.formSelector).serialize();

                var url = $(settings.formSelector).attr("action") || saveUrl;

                var ajaxOptions = {
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        if (response.isSuccess) {
                            toastr.success(response.message);
                            loadTable();
                            ResetForm();
                            if (response.lastCode) {
                                $(settings.lastCodeSelector).val(response.lastCode);
                            }
                            $(settings.deleteSelector).data('id', '');
                        }
                        else {
                            toastr.error(response.message);
                            console.log(response);
                        }
                    },
                    error: function (xhr) {
                        toastr.error("An error occurred while saving.");
                        console.error(xhr);
                    }
                };

                if (settings.haseFile) {
                    ajaxOptions.processData = false;
                    ajaxOptions.contentType = false;
                }

                $.ajax(ajaxOptions);
            });

            //#endregion

            //#region selectAllSelector & deleteSelector

            $("body").off("click", `${settings.selectAllSelector}, #ShipmentModeLedgerSetup-check-all`)
                     .on("click", `${settings.selectAllSelector}, #ShipmentModeLedgerSetup-check-all`, function () {
                var isChecked = $(this).prop('checked');
                $(settings.gridSelector).find("tbody input[type='checkbox']").prop('checked', isChecked);
            });

            $("body").off("click", settings.deleteSelector)
                     .on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItems = [];

                // 1. Collect from checked checkboxes in table
                $(settings.gridSelector).find("tbody input[type='checkbox']:checked, tbody .checkBox:checked, tbody .form-check-input:checked").each(function () {
                    var val = $(this).val();
                    if (val && !selectedItems.includes(val)) {
                        selectedItems.push(val);
                    }
                });

                // 2. Active form item fallback
                if (selectedItems.length === 0) {
                    var btnId = $(this).data('id');
                    if (btnId) {
                        selectedItems.push(btnId);
                    } else {
                        var formId = $("#ExpenseTypeId").val();
                        var tcVal = parseInt($("#Tc").val() || $("#tc").val()) || 0;
                        if (formId && tcVal > 0) {
                            selectedItems.push(formId);
                        }
                    }
                }

                if (selectedItems.length > 0) {
                    var count = selectedItems.length;
                    var titleText = count > 1 
                        ? `Are you sure you want to delete these ${count} items? This process cannot be undone.`
                        : "Are you sure you want to delete this item? This process cannot be undone.";
                    $(settings.deleteModal).find('.title').text(titleText);
                    $(settings.deleteModal).modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }
            });

            //#endregion

            //#region Delete Execution (Modal)

            $("body").off("click", settings.finalDeleteSelector)
                     .on("click", settings.finalDeleteSelector, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                if (!selectedItems || selectedItems.length === 0) {
                    $(settings.deleteModal).modal("hide");
                    return;
                }

                $.ajax({
                    url: deleteUrl,
                    method: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedItems),
                    success: function (response) {
                        $(settings.deleteModal).modal("hide");
                        if (response.success) {
                            toastr.success(response.message);
                            selectedItems = [];

                            loadTable();
                            ResetForm();
                            $(`${settings.selectAllSelector}, #ShipmentModeLedgerSetup-check-all`).prop('checked', false);
                            $(settings.deleteSelector).data('id', '');
                        } else {
                            toastr.error(response.message);
                        }
                    },
                    error: function (xhr) {
                        $(settings.deleteModal).modal("hide");
                        var msg = "Failed to delete.";
                        try {
                            var res = JSON.parse(xhr.responseText);
                            if (res.message) msg = res.message;
                        } catch (ex) {}
                        toastr.error(msg);
                    }
                });
            });

            //#endregion

            //#region Clear

            $("body").off('click', settings.clearSelector).on('click', settings.clearSelector, function (e) {
                e.preventDefault();
                ResetForm();
                loadTable();
            });

            //#endregion

            //#region ResetForm

            function ResetForm() {
                $('#Tc').val(0);
                $('#Id').val('');
                $(settings.deleteSelector).data('id', '');

                resetSelectField('#ExpenseType');
                resetSelectField('#ExpenseLedgerCodeNo');
                resetSelectField('#RevenueLedgerCodeNo');
                resetSelectField('#AdvanceLedgerCode');

                $('#LdateModifyHide').hide();
                $('#sectionBreak').hide();
                $('#creationDateSpan').text('');
                $('#modifyDateSpan').text('');

                selectedItems = [];
                $(`${settings.selectAllSelector}, #ShipmentModeLedgerSetup-check-all`).prop('checked', false);
                $(settings.gridSelector).find('input[type="checkbox"]').prop('checked', false);

                getNextExpenseTypeId();
            }

            function getNextExpenseTypeId() {
                $.ajax({
                    url: base + '/GetNextId',
                    type: 'GET',
                    success: function (response) {
                        if (response) {
                            $('#ExpenseTypeId').val(response);
                            $('#lastCode').val(response);
                        }
                    },
                    error: function (xhr) {
                        console.error("Error fetching next Expense Type ID:", xhr.responseText);
                    }
                });
            }

            //#endregion

            //#region Top Selector

            $("body").off("click", settings.topSelector).on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html, body").animate({ scrollTop: 0 }, 500);
            });

            //#endregion

        });

        //#region loadTable

        function loadTable() {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload(null, false);
            }
        }

        //#endregion

    };

}(jQuery));