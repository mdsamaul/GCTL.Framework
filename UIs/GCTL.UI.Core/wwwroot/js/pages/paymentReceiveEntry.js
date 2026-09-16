(function ($) {
    $.paymentReceiveEntry = function (options) {

        //#region Settings & Options
        var settings = $.extend({
            baseUrl: "/PaymentReceiveEntry",
            formSelector: "#PaymentReceiveEntry-form",
            formContainer: ".js-PaymentReceiveEntry-form-container",
            customerGridId: "CustomerList-grid",
            customerGridSelector: "#CustomerList-grid",
            receivedGridId: "PaymentReceivedHistory-grid",
            receivedGridSelector: "#PaymentReceivedHistory-grid",
            saveSelector: ".js-PaymentReceiveEntry-save",
            clearSelector: ".js-PaymentReceiveEntry-clear",
            deleteSelector: ".js-PaymentReceiveEntry-delete-confirm",
            finalDeleteSelector: ".js-confirm-delete, .btn-confirm-delete",
            deleteModal: "#deleteModal",
            printSelector: ".js-PaymentReceiveEntry-print",
            load: function () { }
        }, options);

        var base = (settings.baseUrl || "/PaymentReceiveEntry").replace(/\/+$/, "");
        var saveUrl = base + "/Setup";
        var deleteUrl = base + "/Delete";
        var selectedItemsToDelete = [];
        var currentDueInvoices = [];
        //#endregion

        // Prevent DataTables intrusive alert popups
        if ($.fn && $.fn.dataTable) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        $(() => {
            if ($.fn && $.fn.dataTable) {
                $.fn.dataTable.ext.errMode = 'none';
            }

            // Ensure renderCustomerId is available globally
            window.gctlRenderers = window.gctlRenderers || {};
            if (!window.gctlRenderers.renderCustomerId) {
                window.gctlRenderers.renderCustomerId = function (data, type, row) {
                    var id = data || (row ? (row.customerId || row.CustomerID || '') : '');
                    return '<a class="custom-grid-link js-select-customer fw-bold text-primary" data-id="' + id + '" style="cursor:pointer; text-decoration:underline;">' + id + '</a>';
                };
            }

            // Listen for DataTables error event to log safely without alerting user
            $(settings.customerGridSelector + ', ' + settings.receivedGridSelector).on('error.dt', function (e, dtSettings, techNote, message) {
                console.warn('DataTables warning suppressed:', message);
            });

            // Initialize top CustomerList grid if not already initialized
            if ($.gctlGrid) {
                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                    $.gctlGrid.initFromModel({ gridId: settings.customerGridId });
                }
            }

            //#region Date Formatting Utilities
            function formatDateForInput(dateStr) {
                if (!dateStr) return '';
                var d = new Date(dateStr);
                if (isNaN(d.getTime())) return dateStr;
                var year = d.getFullYear();
                var month = ('0' + (d.getMonth() + 1)).slice(-2);
                var day = ('0' + d.getDate()).slice(-2);
                return `${year}-${month}-${day}`;
            }

            function formatCurrency(val) {
                if (val === null || val === undefined || val === '') return '0.00';
                var num = parseFloat(val);
                return isNaN(num) ? '0.00' : num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            }
            //#endregion

            //#region Top Grid: Customer Click / Selection
            // Listen for click on Customer ID link (.js-select-customer)
            $("body").off('click', '.js-select-customer').on('click', '.js-select-customer', function (e) {
                e.preventDefault();
                var clickedId = $(this).data('id') || $(this).text().trim();
                var $row = $(this).closest('tr');
                selectCustomerFromRow($row, clickedId);
            });

            // Also allow clicking anywhere on the row in CustomerList-grid
            $("body").off('click', '#CustomerList-grid tbody tr').on('click', '#CustomerList-grid tbody tr', function (e) {
                if ($(e.target).is('input, button')) return;
                var $link = $(this).find('a.js-select-customer, a[data-id]');
                var clickedId = $link.length ? ($link.data('id') || $link.text().trim()) : '';
                selectCustomerFromRow($(this), clickedId);
            });

            function selectCustomerFromRow($row, passedCustomerId) {
                if (!$row || $row.length === 0) return;

                // Highlight selected row
                $('#CustomerList-grid tbody tr').removeClass('table-active bg-primary-subtle');
                $row.addClass('table-active bg-primary-subtle');

                var rowData = null;
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                    rowData = $(settings.customerGridSelector).DataTable().row($row).data();
                }

                var customerId = passedCustomerId || '';
                var customerName = '';
                var customerAddress = '';

                if (rowData) {
                    if (!customerId) {
                        customerId = rowData.customerId || rowData.CustomerID || rowData.customerID || '';
                    }
                    customerName = rowData.customerName || rowData.CustomerName || '';
                    customerAddress = rowData.customerAddress || rowData.CustomerAddress || '';
                }

                if (!customerId) {
                    var $cells = $row.find('td');
                    var $link = $cells.find('a.js-select-customer, a[data-id]');
                    if ($link.length) {
                        customerId = $link.data('id') || $link.text().trim();
                    }
                    if (!customerId) {
                        customerId = $cells.eq(0).text().trim();
                    }
                    if (!customerName) customerName = $cells.eq(1).text().trim();
                    if (!customerAddress) customerAddress = $cells.eq(2).text().trim();
                }

                if (!customerId) return;

                // Populate Main Information fields (both Customer ID and Name)
                $('#CustomerId').val(customerId);
                $('#CustomerNameDisplay').val(customerName);
                $('#BillingAddressDisplay').val(customerAddress);

                // Reset bill & payment selection
                $('#AgainstBillList').val('');
                $('#Amount').val('');
                $('#SelectedInvoicesJson').val('[]');

                // Load Due Invoices for this customer
                loadDueInvoices(customerId);
            }
            //#endregion

            //#region Due Invoices (Middle Right Grid)
            function loadDueInvoices(customerId, preSelectedBills) {
                var $tbody = $('#dueInvoicesTableBody');
                $tbody.html('<tr><td colspan="5" class="text-center py-3 text-muted"><i class="fa fa-spinner fa-spin me-1"></i> Loading due invoices...</td></tr>');

                $.ajax({
                    url: base + '/GetDueInvoices',
                    type: 'GET',
                    data: { customerId: customerId },
                    success: function (res) {
                        var list = res && res.data ? res.data : (Array.isArray(res) ? res : []);
                        currentDueInvoices = list;
                        renderDueInvoicesTable(list, preSelectedBills);
                    },
                    error: function () {
                        $tbody.html('<tr><td colspan="5" class="text-center text-danger py-3">Failed to load due invoices.</td></tr>');
                    }
                });
            }

            function renderDueInvoicesTable(invoices, preSelectedBills) {
                var $tbody = $('#dueInvoicesTableBody');
                $tbody.empty();

                var preSelectedList = [];
                if (preSelectedBills) {
                    preSelectedList = preSelectedBills.split(',').map(b => b.trim());
                }

                if (!invoices || invoices.length === 0) {
                    $tbody.html('<tr><td colspan="5" class="text-center text-muted py-3"><em>No due invoices found for this customer.</em></td></tr>');
                    $('#dueInvoicesCountBadge').text('0 Invoices');
                    $('#dueInvoicesInfo').text('Showing 0 to 0 of 0 entries');
                    $('#dueTotalAmount').text('0.00');
                    $('#dueTotalDue').text('0.00');
                    return;
                }

                var totalAmount = 0;
                var totalDue = 0;

                invoices.forEach(function (inv, idx) {
                    var billAmount = parseFloat(inv.billAmount || 0);
                    var dueAmount = parseFloat(inv.due != null ? inv.due : (inv.Due != null ? inv.Due : 0));
                    totalAmount += billAmount;
                    totalDue += dueAmount;

                    var isChecked = preSelectedList.includes(inv.billNo);

                    var $tr = $(`
                        <tr class="align-middle">
                            <td class="text-center">
                                <div class="form-check d-flex justify-content-center m-0">
                                    <input class="form-check-input js-due-invoice-checkbox" type="checkbox"
                                           data-billno="${inv.billNo}"
                                           data-amount="${billAmount}"
                                           data-due="${dueAmount}"
                                           ${isChecked ? 'checked' : ''} />
                                </div>
                            </td>
                            <td><span class="fw-bold text-primary">${inv.billNo}</span></td>
                            <td class="text-center">${inv.billDate || ''}</td>
                            <td class="text-end">${formatCurrency(billAmount)}</td>
                            <td class="text-end">${formatCurrency(dueAmount)}</td>
                        </tr>
                    `);

                    $tbody.append($tr);
                });

                $('#dueInvoicesCountBadge').text(`${invoices.length} Invoices`);
                $('#dueInvoicesInfo').text(`Showing 1 to ${invoices.length} of ${invoices.length} entries`);
                $('#dueTotalAmount').text(formatCurrency(totalAmount));
                $('#dueTotalDue').text(formatCurrency(totalDue));

                // If any preselected, update totals
                if (preSelectedList.length > 0) {
                    updateSelectedInvoicesState();
                }
            }

            // Individual invoice checkbox click
            $("body").off('change', '.js-due-invoice-checkbox').on('change', '.js-due-invoice-checkbox', function () {
                updateSelectedInvoicesState();
            });

            // Select All checkbox click
            $("body").off('change', '#dueInvoicesSelectAll').on('change', '#dueInvoicesSelectAll', function () {
                var isChecked = $(this).prop('checked');
                $('.js-due-invoice-checkbox').prop('checked', isChecked);
                updateSelectedInvoicesState();
            });

            function updateSelectedInvoicesState() {
                var selectedBills = [];
                var selectedInvoiceItems = [];
                var totalDueSum = 0;

                $('.js-due-invoice-checkbox:checked').each(function () {
                    var billNo = $(this).data('billno');
                    var due = parseFloat($(this).data('due') || 0);

                    if (billNo) {
                        selectedBills.push(billNo);
                        selectedInvoiceItems.push({
                            invoiceNo: billNo,
                            amount: due
                        });
                        totalDueSum += due;
                    }
                });

                // Update Against/Bill List input
                $('#AgainstBillList').val(selectedBills.join(', '));

                // Auto populate Payment Amount with sum of selected due amounts
                if (selectedBills.length > 0) {
                    $('#Amount').val(totalDueSum.toFixed(2));
                }

                // Store serialized JSON in hidden field
                $('#SelectedInvoicesJson').val(JSON.stringify(selectedInvoiceItems));

                // Update select all checkbox state
                var totalCount = $('.js-due-invoice-checkbox').length;
                var checkedCount = $('.js-due-invoice-checkbox:checked').length;
                $('#dueInvoicesSelectAll').prop('checked', totalCount > 0 && totalCount === checkedCount);
            }
            //#endregion

            //#region Save / Submit
            $("body").off("click", settings.saveSelector).on("click", settings.saveSelector, function (e) {
                e.preventDefault();

                if (!validatePaymentForm()) {
                    return false;
                }

                var $form = $(settings.formSelector);
                var data = $form.serialize();
                var url = $form.attr("action") || saveUrl;

                var $submitBtn = $(this);
                $submitBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i> Submitting...');

                $.ajax({
                    url: url,
                    method: "POST",
                    data: data,
                    success: function (response) {
                        $submitBtn.prop('disabled', false).html('<i class="far fa-save me-1 text-primary"></i> Submit');

                        if (response.isSuccess) {
                            toastr.success(response.message || "Saved Successfully.");

                            // Reload customer list grid
                            reloadCustomerGrid();

                            // Reload bottom grid if open
                            if ($('#paymentReceivedGridContainer').is(':visible')) {
                                reloadReceivedGrid();
                            }

                            // Reset form & generate new IDs
                            resetForm();
                        } else {
                            toastr.error(response.message || "Failed to save record.");
                        }
                    },
                    error: function () {
                        $submitBtn.prop('disabled', false).html('<i class="far fa-save me-1 text-primary"></i> Submit');
                        toastr.error("An error occurred while saving.");
                    }
                });
            });

            function validatePaymentForm() {
                var customerId = $('#CustomerId').val();
                if (!customerId || customerId.trim() === '') {
                    toastr.warning('Please select a Customer from the top customer list.');
                    $('#CustomerList-grid').focus();
                    return false;
                }

                var receiveId = $('#PaymentReceiveId').val();
                if (!receiveId || receiveId.trim() === '') {
                    toastr.warning('Payment Receive No. is required.');
                    $('#PaymentReceiveId').focus();
                    return false;
                }

                var amount = $('#Amount').val();
                if (!amount || parseFloat(amount) <= 0 || isNaN(parseFloat(amount))) {
                    toastr.warning('Please enter a valid Payment Amount greater than 0.');
                    $('#Amount').focus();
                    return false;
                }

                return true;
            }
            //#endregion

            //#region Bottom Filter By: Search & Preview
            $("body").off('click', '#btnFilterSearch').on('click', '#btnFilterSearch', function () {
                var fromDate = $('#filterFromDate').val();
                var toDate = $('#filterToDate').val();

                $('#paymentReceivedGridContainer').slideDown();

                // If grid already initialized, reload with search params
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    var newUrl = base + `/SearchPaymentReceived?fromDate=${fromDate}&toDate=${toDate}`;
                    $(settings.receivedGridSelector).DataTable().ajax.url(newUrl).load();
                } else if ($.gctlGrid) {
                    $.gctlGrid.initFromModel({ gridId: settings.receivedGridId });
                }
            });

            $("body").off('click', '#btnCloseReceivedGrid').on('click', '#btnCloseReceivedGrid', function () {
                $('#paymentReceivedGridContainer').slideUp();
            });

            $("body").off('click', '#btnFilterPreview, .js-PaymentReceiveEntry-print').on('click', '#btnFilterPreview, .js-PaymentReceiveEntry-print', function (e) {
                e.preventDefault();
                window.print();
            });
            //#endregion

            //#region Edit (Load from Bottom Grid)
            $("body").off('click', '.js-PaymentReceiveEntry-edit').on('click', '.js-PaymentReceiveEntry-edit', function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                if (!id) return;

                $.ajax({
                    url: base + '/GetById',
                    type: 'GET',
                    data: { code: id },
                    success: function (data) {
                        if (data) {
                            $('#Tc').val(data.tc || '');
                            $('#PaymentReceiveId').val(data.paymentReceiveId || '');

                            if (data.paymentReceiveDate) {
                                $('#PaymentReceiveDate').val(formatDateForInput(data.paymentReceiveDate));
                            }

                            $('#CustomerId').val(data.customerId || '');
                            $('#CustomerNameDisplay').val(data.customerName || '');
                            $('#BillingAddressDisplay').val(data.billingAddress || '');
                            $('#AgainstBillList').val(data.againstBillList || '');

                            if (data.paymentTypeId) {
                                $('#PaymentTypeId').val(data.paymentTypeId);
                            }
                            if (data.paymentModeCode) {
                                $('#PaymentModeCode').val(data.paymentModeCode);
                            }

                            $('#MrNo').val(data.mrNo || '');
                            if (data.mrDate) {
                                $('#MrDate').val(formatDateForInput(data.mrDate));
                            }

                            $('#Amount').val(data.amount != null ? parseFloat(data.amount).toFixed(2) : '');
                            $('#BankFeeOnlineCharge').val(data.bankFeeOnlineCharge != null ? parseFloat(data.bankFeeOnlineCharge).toFixed(2) : '');
                            $('#Remarks').val(data.remarks || '');
                            $('#Receivedby').val(data.receivedby || '');

                            if (data.creationDate) {
                                $('#creationDateSpan').text(data.creationDate);
                            }
                            if (data.lastUpdateDate) {
                                $('#modifyDateSpan').text(data.lastUpdateDate);
                            }

                            // Load due invoices and pre-select against bills
                            if (data.customerId) {
                                loadDueInvoices(data.customerId, data.againstBillList);
                            }

                            // Set delete button data-id
                            $(settings.deleteSelector).data('id', data.paymentReceiveId);

                            // Scroll smoothly to Main Information
                            $('html, body').animate({
                                scrollTop: $('.main-info-fieldset').offset().top - 80
                            }, 400);
                        }
                    },
                    error: function () {
                        toastr.error('Failed to load payment receive details.');
                    }
                });
            });
            //#endregion

            //#region Clear & Reset
            $("body").off('click', settings.clearSelector).on('click', settings.clearSelector, function (e) {
                e.preventDefault();
                resetForm();
            });

            function resetForm() {
                $('#Tc').val('');
                $('#CustomerId').val('');
                $('#CustomerNameDisplay').val('');
                $('#BillingAddressDisplay').val('');
                $('#AgainstBillList').val('');
                $('#PaymentTypeId').val('01');
                $('#PaymentModeCode').val('001');
                $('#PaymentReceiveDate').val(formatDateForInput(new Date()));
                $('#MrDate').val(formatDateForInput(new Date()));
                $('#Amount').val('');
                $('#BankFeeOnlineCharge').val('');
                $('#Remarks').val('');
                $('#Receivedby').val('');
                $('#SelectedInvoicesJson').val('[]');
                $('#modifyDateSpan').text('');

                // Clear Due Invoices Table
                $('#dueInvoicesTableBody').html('<tr><td colspan="5" class="text-center text-muted py-3"><em>Select a customer from the top grid to load due invoices.</em></td></tr>');
                $('#dueInvoicesCountBadge').text('0 Invoices');
                $('#dueInvoicesInfo').text('Showing 0 to 0 of 0 entries');
                $('#dueTotalAmount').text('0.00');
                $('#dueTotalDue').text('0.00');
                $('#dueInvoicesSelectAll').prop('checked', false);

                // Fetch new sequence numbers
                $.get(base + '/GetNextNumbers', function (res) {
                    if (res) {
                        if (res.paymentReceiveId) $('#PaymentReceiveId').val(res.paymentReceiveId);
                        if (res.mrNo) $('#MrNo').val(res.mrNo);
                    }
                });

                // Clear active row highlight
                $('#CustomerList-grid tbody tr').removeClass('table-active bg-primary-subtle');
                $(settings.deleteSelector).data('id', '');
            }
            //#endregion

            //#region Delete
            $("body").off("click", settings.deleteSelector).on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItemsToDelete = [];

                // Check bottom grid selection if open
                if ($('#paymentReceivedGridContainer').is(':visible')) {
                    $(settings.receivedGridSelector).find("tbody input[type='checkbox']:checked").each(function () {
                        var val = $(this).val();
                        if (val && !selectedItemsToDelete.includes(val)) {
                            selectedItemsToDelete.push(val);
                        }
                    });
                }

                // If nothing checked in bottom grid, check current form PaymentReceiveId
                if (selectedItemsToDelete.length === 0) {
                    var btnId = $(this).data('id');
                    var formId = $("#PaymentReceiveId").val();
                    var tc = $("#Tc").val();

                    if (btnId) {
                        selectedItemsToDelete.push(btnId);
                    } else if (formId && tc) {
                        selectedItemsToDelete.push(formId);
                    }
                }

                if (selectedItemsToDelete.length > 0) {
                    var count = selectedItemsToDelete.length;
                    var titleText = count > 1
                        ? `Are you sure you want to delete these ${count} records? This process cannot be undone.`
                        : `Are you sure you want to delete this payment receive record (${selectedItemsToDelete[0]})?`;

                    var $modal = $(settings.deleteModal);
                    if ($modal.length === 0) $modal = $("#deleteModal, #delete-modal").first();
                    $modal.find('.modal-title, .title, .confirmation-text').text(titleText);
                    $modal.modal("show");
                } else {
                    toastr.info("Please select a record to delete.");
                }
            });

            $("body").off("click", settings.finalDeleteSelector).on("click", settings.finalDeleteSelector, function (e) {
                e.preventDefault();
                if (!selectedItemsToDelete || selectedItemsToDelete.length === 0) return;

                $.ajax({
                    url: deleteUrl,
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedItemsToDelete),
                    success: function (res) {
                        var $modal = $(settings.deleteModal);
                        if ($modal.length === 0) $modal = $("#deleteModal, #delete-modal").first();
                        $modal.modal("hide");

                        if (res.success) {
                            toastr.success(res.message || "Deleted Successfully.");
                            reloadCustomerGrid();
                            if ($('#paymentReceivedGridContainer').is(':visible')) {
                                reloadReceivedGrid();
                            }
                            resetForm();
                        } else {
                            toastr.error(res.message || "Delete Failed.");
                        }
                    },
                    error: function () {
                        var $modal = $(settings.deleteModal);
                        if ($modal.length === 0) $modal = $("#deleteModal, #delete-modal").first();
                        $modal.modal("hide");
                        toastr.error("An error occurred during deletion.");
                    }
                });
            });
            //#endregion

            //#region Reload Utilities
            function reloadCustomerGrid() {
                if ($.gctlGrid) {
                    $.gctlGrid.reload(settings.customerGridId);
                } else if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                    $(settings.customerGridSelector).DataTable().ajax.reload(null, false);
                }
            }

            function reloadReceivedGrid() {
                if ($.gctlGrid) {
                    $.gctlGrid.reload(settings.receivedGridId);
                } else if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    $(settings.receivedGridSelector).DataTable().ajax.reload(null, false);
                }
            }
            //#endregion
        });
    };
})(jQuery);
