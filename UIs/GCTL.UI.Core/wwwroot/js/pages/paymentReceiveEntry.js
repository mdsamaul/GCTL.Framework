(function ($) {
    $.paymentReceiveEntry = function (options) {

        //#region Settings & Options
        var settings = $.extend({
            baseUrl: "/PaymentReceiveEntry",
            formSelector: "#PaymentReceiveEntry-form",
            formContainer: ".js-PaymentReceiveEntry-form-container",
            customerGridId: "CustomerList-grid",
            customerGridSelector: "#CustomerList-grid",
            dueInvoicesGridId: "DueInvoices-grid",
            dueInvoicesGridSelector: "#DueInvoices-grid",
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
        var selectedInvoicesMap = {};
        //#endregion

        // Prevent DataTables intrusive alert popups
        if ($.fn && $.fn.dataTable) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        $(() => {
            if ($.fn && $.fn.dataTable) {
                $.fn.dataTable.ext.errMode = 'none';
            }

            // Ensure renderers are available globally
            window.gctlRenderers = window.gctlRenderers || {};
            if (!window.gctlRenderers.renderCustomerId) {
                window.gctlRenderers.renderCustomerId = function (data, type, row) {
                    var id = data || (row ? (row.customerId || row.CustomerID || '') : '');
                    return '<a class="custom-grid-link js-select-customer fw-bold text-primary" data-id="' + id + '" style="cursor:pointer; text-decoration:underline;">' + id + '</a>';
                };
            }
            if (!window.gctlRenderers.renderDueInvoiceNo) {
                window.gctlRenderers.renderDueInvoiceNo = function (data, type, row) {
                    var billNo = data || (row ? row.billNo : '');
                    return '<span class="fw-bold text-primary">' + (billNo || '') + '</span>';
                };
            }
            if (!window.gctlRenderers.formatCurrency) {
                window.gctlRenderers.formatCurrency = function (data) {
                    return formatCurrency(data);
                };
            }

            // Listen for DataTables error event to log safely without alerting user
            $(settings.customerGridSelector + ', ' + settings.receivedGridSelector + ', ' + settings.dueInvoicesGridSelector).on('error.dt', function (e, dtSettings, techNote, message) {
                console.warn('DataTables warning suppressed:', message);
            });

            // Initialize top CustomerList grid if not already initialized
            if ($.gctlGrid) {
                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                    $.gctlGrid.initFromModel({ gridId: settings.customerGridId });
                }

                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.dueInvoicesGridSelector)) {
                    $.gctlGrid.initFromModel({
                        gridId: settings.dueInvoicesGridId,
                        ajaxData: function (d) {
                            d.customerId = $('#CustomerId').val() || '';
                        },
                        onLoaded: function (json) {
                            var list = (json && json.data) ? json.data : (Array.isArray(json) ? json : []);
                            currentDueInvoices = list;
                            var count = list.length;
                            $('#dueInvoicesCountBadge').text(count + ' Invoices');

                            var totalAmount = 0;
                            var totalDue = 0;
                            list.forEach(function (item) {
                                totalAmount += parseFloat(item.billAmount || 0);
                                totalDue += parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                            });
                            $('#dueTotalAmount').text(formatCurrency(totalAmount));
                            $('#dueTotalDue').text(formatCurrency(totalDue));
                        }
                    });
                }
            }

            // Initialize custom-datepicker if needed
            if (window.custom_Datepicker && window.custom_Datepicker.init) {
                $('.custom-datepicker').each(function () {
                    window.custom_Datepicker.init(this);
                });
            }

            // Initialize custom-select-single (customDropdownSingle from custom-select-2.js)
            if (window.initCustomDropdowns) {
                window.initCustomDropdowns(settings.formContainer || document, false);
            }

            // Align Against/Bill List input width to match the end of right-side inputs
            function alignAgainstBillWidth() {
                var $input = $('#AgainstBillList');
                if (!$input.length) return;

                if (window.innerWidth >= 768) {
                    var inputEl = $input[0];

                    // Determine exact left edge of the input (anchored to inner left of its .col-sm-7 parent)
                    var leftEdge = 0;
                    var col7 = inputEl.closest('.col-sm-7');
                    if (col7 && col7.offsetWidth > 0) {
                        var pl = parseFloat(window.getComputedStyle(col7).paddingLeft) || 2;
                        leftEdge = col7.getBoundingClientRect().left + pl;
                    } else {
                        var $topInput = $('#PaymentReceiveId');
                        if ($topInput.length && $topInput.is(':visible') && $topInput[0].offsetWidth > 0) {
                            leftEdge = $topInput[0].getBoundingClientRect().left;
                        } else {
                            leftEdge = inputEl.getBoundingClientRect().left;
                        }
                    }

                    // Determine exact right edge of the right-side inputs
                    var rightEdge = 0;
                    // 1. BankFeeOnlineCharge (standard input in row 6, never hidden by plugins)
                    var bankInput = document.getElementById('BankFeeOnlineCharge');
                    if (bankInput && bankInput.offsetWidth > 0) {
                        rightEdge = bankInput.getBoundingClientRect().right;
                    }
                    // 2. PaymentReceiveDate (check flatpickr altInput or the input itself or its .col-sm-7 container)
                    if (!rightEdge || rightEdge <= leftEdge) {
                        var prDate = document.getElementById('PaymentReceiveDate');
                        if (prDate) {
                            var fp = $(prDate).data('customFpInstance');
                            if (fp && fp.altInput && fp.altInput.offsetWidth > 0) {
                                rightEdge = fp.altInput.getBoundingClientRect().right;
                            } else {
                                var nextInput = $(prDate).next('input:visible')[0];
                                if (nextInput && nextInput.offsetWidth > 0) {
                                    rightEdge = nextInput.getBoundingClientRect().right;
                                } else if (prDate.offsetWidth > 0) {
                                    rightEdge = prDate.getBoundingClientRect().right;
                                } else {
                                    var rCol7 = prDate.closest('.col-sm-7');
                                    if (rCol7 && rCol7.offsetWidth > 0) {
                                        var pr = parseFloat(window.getComputedStyle(rCol7).paddingRight) || 2;
                                        rightEdge = rCol7.getBoundingClientRect().right - pr;
                                    }
                                }
                            }
                        }
                    }
                    // 3. Fallback to right col-md-6 inner edge
                    if (!rightEdge || rightEdge <= leftEdge) {
                        var rightCol = document.querySelector('#PaymentReceiveEntry-form .row.g-2 > .col-md-6:nth-child(2)');
                        if (rightCol && rightCol.offsetWidth > 0) {
                            var rcRect = rightCol.getBoundingClientRect();
                            var prCol = parseFloat(window.getComputedStyle(rightCol).paddingRight) || 4;
                            rightEdge = rcRect.right - prCol - 2;
                        }
                    }

                    var exactW = Math.round(rightEdge - leftEdge);
                    if (exactW > 50) {
                        inputEl.style.setProperty('width', exactW + 'px', 'important');
                        inputEl.style.setProperty('max-width', 'none', 'important');
                        inputEl.style.setProperty('position', 'relative', 'important');
                        inputEl.style.setProperty('z-index', '2', 'important');
                    }
                } else {
                    var el = $input[0];
                    el.style.removeProperty('width');
                    el.style.removeProperty('max-width');
                    el.style.removeProperty('position');
                    el.style.removeProperty('z-index');
                }
            }

            alignAgainstBillWidth();
            setTimeout(alignAgainstBillWidth, 50);
            setTimeout(alignAgainstBillWidth, 150);
            setTimeout(alignAgainstBillWidth, 300);
            setTimeout(alignAgainstBillWidth, 600);
            $(window).on('resize orientationchange', alignAgainstBillWidth);

            if (window.ResizeObserver) {
                var formEl = document.querySelector('.js-PaymentReceiveEntry-form-container') || document.querySelector('#PaymentReceiveEntry-form');
                if (formEl) {
                    var ro = new ResizeObserver(function () {
                        alignAgainstBillWidth();
                    });
                    ro.observe(formEl);
                }
            }

            //#region Date Formatting Utilities
            function setDatepickerVal(selector, val) {
                if (window.setDate) {
                    window.setDate(selector, val);
                } else {
                    $(selector).val(val);
                }
            }

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

                // Populate Main Information fields
                $('#CustomerId').val(customerId);
                $('#CustomerNameDisplay').val(customerName);
                $('#BillingAddressDisplay').text(customerAddress || '').attr('title', customerAddress || '');
                $('#BillingAddressHidden').val(customerAddress || '');

                // Reset bill & payment selection
                $('#AgainstBillList').val('');
                $('#Amount').val('');
                $('#SelectedInvoicesJson').val('[]');

                // Load Due Invoices for this customer
                loadDueInvoices(customerId);
            }
            //#endregion

            //#region Due Invoices (CustomGrid with Server Data & Pagination)
            function loadDueInvoices(customerId, preSelectedBills) {
                selectedInvoicesMap = {};
                $('#dueInvoicesCheckAll').prop('checked', false);

                if (!customerId) {
                    currentDueInvoices = [];
                    $('#dueInvoicesCountBadge').text('0 Invoices');
                    $('#dueTotalAmount').text('0.00');
                    $('#dueTotalDue').text('0.00');
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.dueInvoicesGridSelector)) {
                        $(settings.dueInvoicesGridSelector).DataTable().clear().draw();
                    }
                    return;
                }

                var dtUrl = base + '/GetDueInvoices?customerId=' + encodeURIComponent(customerId);
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.dueInvoicesGridSelector)) {
                    var dt = $(settings.dueInvoicesGridSelector).DataTable();
                    dt.ajax.url(dtUrl).load(function (json) {
                        var list = (json && json.data) ? json.data : (Array.isArray(json) ? json : []);
                        currentDueInvoices = list;
                        var count = list.length;
                        $('#dueInvoicesCountBadge').text(count + ' Invoices');

                        var totalAmount = 0;
                        var totalDue = 0;
                        list.forEach(function (item) {
                            totalAmount += parseFloat(item.billAmount || 0);
                            totalDue += parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                        });
                        $('#dueTotalAmount').text(formatCurrency(totalAmount));
                        $('#dueTotalDue').text(formatCurrency(totalDue));

                        if (preSelectedBills) {
                            var preList = typeof preSelectedBills === 'string'
                                ? preSelectedBills.split(',').map(function (b) { return b.trim(); })
                                : preSelectedBills;
                            list.forEach(function (item) {
                                if (preList.includes(item.billNo)) {
                                    var dueVal = parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                                    selectedInvoicesMap[item.billNo] = {
                                        invoiceNo: item.billNo,
                                        amount: dueVal
                                    };
                                }
                            });
                            syncDueInvoiceCheckboxes();
                            updateSelectedInvoicesState();
                        }
                    });
                }
            }

            // Sync checkboxes on currently active page with selectedInvoicesMap
            function syncDueInvoiceCheckboxes() {
                var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                var allChecked = $cbs.length > 0;
                $cbs.each(function () {
                    var billNo = $(this).val() || $(this).data('id');
                    var isSel = !!selectedInvoicesMap[billNo];
                    $(this).prop('checked', isSel);
                    if (isSel) {
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).closest('tr').removeClass('selected-row');
                        allChecked = false;
                    }
                });

                $('#dueInvoicesCheckAll').prop('checked', allChecked && $cbs.length > 0);
            }

            // Listen for draw.dt on DueInvoices grid to maintain checkbox state across pagination
            $(settings.dueInvoicesGridSelector).on('draw.dt', function () {
                syncDueInvoiceCheckboxes();
            });

            // Individual invoice checkbox click
            $(settings.dueInvoicesGridSelector).off('change', '.form-check-input.checkbox, input[type="checkbox"].checkbox')
                .on('change', '.form-check-input.checkbox, input[type="checkbox"].checkbox', function () {
                    var isChecked = $(this).prop('checked');
                    var billNo = $(this).val() || $(this).data('id');
                    if (!billNo) return;

                    if (isChecked) {
                        var item = currentDueInvoices.find(function (x) { return x.billNo === billNo; });
                        var dueVal = item ? parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0)) : 0;
                        selectedInvoicesMap[billNo] = {
                            invoiceNo: billNo,
                            amount: dueVal
                        };
                    } else {
                        delete selectedInvoicesMap[billNo];
                    }

                    syncDueInvoiceCheckboxes();
                    updateSelectedInvoicesState();
                });

            // Master Check All in DueInvoices grid
            $("body").off('click', '#dueInvoicesCheckAll').on('click', '#dueInvoicesCheckAll', function () {
                var isChecked = $(this).prop('checked');
                if (isChecked) {
                    currentDueInvoices.forEach(function (item) {
                        if (item.billNo) {
                            var dueVal = parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                            selectedInvoicesMap[item.billNo] = {
                                invoiceNo: item.billNo,
                                amount: dueVal
                            };
                        }
                    });
                } else {
                    selectedInvoicesMap = {};
                }

                syncDueInvoiceCheckboxes();
                updateSelectedInvoicesState();
            });

            function updateSelectedInvoicesState() {
                var selectedList = Object.values(selectedInvoicesMap);
                var selectedBills = selectedList.map(function (x) { return x.invoiceNo; });
                var totalDueSum = 0;
                selectedList.forEach(function (x) {
                    totalDueSum += (x.amount || 0);
                });

                // Update Against/Bill List input
                $('#AgainstBillList').val(selectedBills.join(', '));

                // Auto populate Payment Amount with sum of selected due amounts
                if (selectedBills.length > 0) {
                    $('#Amount').val(totalDueSum.toFixed(2));
                } else {
                    $('#Amount').val('');
                }

                // Store serialized JSON in hidden field
                $('#SelectedInvoicesJson').val(JSON.stringify(selectedList));

                // Update check-all checkbox state
                var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                var totalCount = $cbs.length;
                var checkedCount = $cbs.filter(':checked').length;
                $('#dueInvoicesCheckAll').prop('checked', totalCount > 0 && totalCount === checkedCount);
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
                                setDatepickerVal('#PaymentReceiveDate', formatDateForInput(data.paymentReceiveDate));
                            }

                            $('#CustomerId').val(data.customerId || '');
                            $('#CustomerNameDisplay').val(data.customerName || '');
                            $('#BillingAddressDisplay').text(data.billingAddress || '').attr('title', data.billingAddress || '');
                            $('#BillingAddressHidden').val(data.billingAddress || '');
                            $('#AgainstBillList').val(data.againstBillList || '');
                            alignAgainstBillWidth();

                            if (window.setSelect) {
                                if (data.paymentTypeId) window.setSelect('#PaymentTypeId', data.paymentTypeId);
                                if (data.paymentModeCode) window.setSelect('#PaymentModeCode', data.paymentModeCode);
                                window.setSelect('#Receivedby', data.receivedby || '');
                            } else {
                                if (data.paymentTypeId) $('#PaymentTypeId').val(data.paymentTypeId).trigger('change');
                                if (data.paymentModeCode) $('#PaymentModeCode').val(data.paymentModeCode).trigger('change');
                                $('#Receivedby').val(data.receivedby || '').trigger('change');
                            }

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
                $('#BillingAddressDisplay').text('').attr('title', '');
                $('#BillingAddressHidden').val('');
                $('#AgainstBillList').val('');
                if (window.setSelect) {
                    window.setSelect('#PaymentTypeId', '01');
                    window.setSelect('#PaymentModeCode', '001');
                    window.setSelect('#Receivedby', '');
                } else {
                    $('#PaymentTypeId').val('01').trigger('change');
                    $('#PaymentModeCode').val('001').trigger('change');
                    $('#Receivedby').val('').trigger('change');
                }
                setDatepickerVal('#PaymentReceiveDate', formatDateForInput(new Date()));
                setDatepickerVal('#MrDate', formatDateForInput(new Date()));
                $('#Amount').val('');
                $('#BankFeeOnlineCharge').val('');
                $('#Remarks').val('');
                $('#SelectedInvoicesJson').val('[]');
                $('#modifyDateSpan').text('');
                alignAgainstBillWidth();

                // Clear Due Invoices Table
                selectedInvoicesMap = {};
                currentDueInvoices = [];
                $('#dueInvoicesCountBadge').text('0 Invoices');
                $('#dueTotalAmount').text('0.00');
                $('#dueTotalDue').text('0.00');
                $('#dueInvoicesCheckAll').prop('checked', false);
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.dueInvoicesGridSelector)) {
                    $(settings.dueInvoicesGridSelector).DataTable().clear().draw();
                }

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
