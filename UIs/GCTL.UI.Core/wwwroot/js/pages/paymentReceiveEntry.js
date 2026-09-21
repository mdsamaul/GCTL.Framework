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
            finalDeleteSelector: ".js-PaymentReceiveEntry-delete, .js-delete, .js-gc-delete-confirm, #deleteModal .btn-danger, .js-confirm-delete, .btn-confirm-delete",
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
            window.gctlRenderers.renderDueInvoiceNo = function (data, type, row) {
                var billNo = data || (row ? (row.billNo || row.BillNo || '') : '');
                var html = '<span class="fw-bold text-primary">' + (billNo || '') + '</span>';
                var info = (!isManualCheckboxMode) ? fifoAllocationMap[billNo] : null;
                if (info && info.status) {
                    if (info.status === 'Full Paid') {
                        html += ' <span class="badge bg-success-subtle text-success border border-success-subtle py-0 px-1 ms-1" style="font-size: 10px; font-weight:600;"><i class="fas fa-check-circle me-1"></i>Full Paid</span>';
                    } else if (info.status === 'Partial Paid') {
                        html += ' <span class="badge bg-warning-subtle text-dark border border-warning-subtle py-0 px-1 ms-1" style="font-size: 10px; font-weight:600;" title="Paid: ' + formatCurrency(info.paidAmount) + ' | Due: ' + formatCurrency(info.remainingDue) + '"><i class="fas fa-adjust me-1 text-warning"></i>Paid ' + formatCurrency(info.paidAmount) + '</span>';
                    }
                }
                return html;
            };

            window.gctlRenderers.renderDueColumn = function (data, type, row) {
                var billNo = row ? (row.billNo || row.BillNo || '') : '';
                var origDue = parseFloat(data != null ? data : (row ? (row.due || row.Due || 0) : 0));
                var info = (!isManualCheckboxMode) ? fifoAllocationMap[billNo] : null;
                if (info && info.status) {
                    if (info.status === 'Full Paid') {
                        return '<div class="text-end text-nowrap"><del class="text-muted small me-1">' + formatCurrency(origDue) + '</del><span class="fw-bold text-success">0.00</span></div>';
                    } else if (info.status === 'Partial Paid') {
                        return '<div class="text-end text-nowrap"><del class="text-muted small me-1">' + formatCurrency(origDue) + '</del><span class="fw-bold text-danger">' + formatCurrency(info.remainingDue) + '</span></div>';
                    }
                }
                return '<div class="text-end text-nowrap">' + formatCurrency(origDue) + '</div>';
            };
            if (!window.gctlRenderers.formatCurrency) {
                window.gctlRenderers.formatCurrency = function (data) {
                    return formatCurrency(data);
                };
            }
            window.gctlRenderers.renderBillListPopover = function (data, type, row) {
                var billsStr = data || (row ? (row.billList || row.AgainstBillList || '') : '');
                if (!billsStr || !billsStr.trim()) {
                    return '<span class="text-muted small">-</span>';
                }
                var bills = billsStr.split(/[\r\n,;]+/).map(function (s) { return s.trim(); }).filter(function (s) { return s.length > 0; });
                if (bills.length === 0) {
                    return '<span class="text-muted small">-</span>';
                }
                var rawBills = encodeURIComponent(JSON.stringify(bills));
                return '<div class="bill-list-wrapper d-inline-block">' +
                       '<button type="button" class="btn btn-xs btn-outline-secondary js-toggle-bill-list py-0 px-2" data-bills-json="' + rawBills + '" style="font-size:11px; height:22px; line-height:20px;" title="Click to view ' + bills.length + ' invoices">' +
                       '<i class="fas fa-list-ul text-secondary"></i>' +
                       '</button>' +
                       '</div>';
            };
            window.gctlRenderers.renderBillList = window.gctlRenderers.renderBillListPopover;

            if (!window.gctlRenderers.renderStatusBadge) {
                window.gctlRenderers.renderStatusBadge = function (data) {
                    return '<span class="text-success fw-semibold" style="font-size:12px;">' + (data || 'Approved') + '</span>';
                };
            }
            if (!window.gctlRenderers.renderAITChallan) {
                window.gctlRenderers.renderAITChallan = function (data, type, row) {
                    var fileUrl = data || (row ? (row.aitChallanUpload || row.AITChallanUpload) : '');
                    var challanNo = row ? (row.aitChallanNo || row.AITChallanNo || '') : '';
                    if (fileUrl) {
                        return '<a href="' + fileUrl + '" target="_blank" class="text-primary text-decoration-underline fw-semibold small" title="Challan: ' + (challanNo || 'View') + '"><i class="fas fa-file-pdf text-danger me-1"></i>' + (challanNo || 'View') + '</a>';
                    }
                    return challanNo ? ('<span class="small fw-semibold text-secondary">' + challanNo + '</span>') : '<span class="text-muted small">-</span>';
                };
            }

            // Floating Bill List Popover (appended to body to prevent table overflow clipping & z-index/stacking overlap)
            function getFloatingBillListPopover() {
                var $popover = $('#gctl-bill-list-floating-popover');
                if ($popover.length === 0) {
                    $popover = $('<div id="gctl-bill-list-floating-popover" class="shadow-lg" style="display:none; position:fixed; z-index:99999999; background:#ffffff; border:1px solid #cbd5e1; border-radius:6px; max-height:240px; overflow-y:auto; padding:6px 10px; font-size:11.5px; text-align:left; min-width:220px; box-shadow:0 10px 25px -5px rgba(0,0,0,0.3), 0 8px 10px -6px rgba(0,0,0,0.2) !important;"></div>').appendTo('body');
                }
                return $popover;
            }

            // Toggle bill list popover
            $("body").off('click', '.js-toggle-bill-list').on('click', '.js-toggle-bill-list', function (e) {
                e.stopPropagation();
                var btn = this;
                var $btn = $(btn);
                var $popover = getFloatingBillListPopover();

                // If clicking the same button that is currently open, toggle off
                if ($popover.is(':visible') && $popover.data('trigger-btn') === btn) {
                    $popover.hide().removeData('trigger-btn');
                    return;
                }

                // Extract bills
                var bills = [];
                var rawJson = $btn.attr('data-bills-json');
                if (rawJson) {
                    try {
                        bills = JSON.parse(decodeURIComponent(rawJson));
                    } catch (err) { }
                }
                if (!bills || bills.length === 0) {
                    var rawAttr = $btn.attr('data-bills');
                    if (rawAttr) {
                        bills = rawAttr.split('|||');
                    } else {
                        var $menu = $btn.siblings('.bill-list-popover-menu');
                        if ($menu.length) {
                            bills = $menu.find('div').map(function () { return $(this).text().trim(); }).get();
                        }
                    }
                }

                if (!bills || bills.length === 0) {
                    return;
                }

                var contentHtml = '<div class="d-flex justify-content-between align-items-center pb-1 mb-2 border-bottom bg-light px-2 py-1 rounded" style="margin:-6px -10px 6px -10px;">' +
                                  '<span class="fw-bold text-dark" style="font-size:11.5px;"><i class="fas fa-file-invoice text-primary me-1"></i>Invoices (' + bills.length + ')</span>' +
                                  '<span class="js-close-bill-popover text-muted ms-2" style="cursor:pointer; font-size:15px; line-height:1; font-weight:bold;" title="Close">&times;</span>' +
                                  '</div>' +
                                  '<div class="bill-list-items" style="max-height:180px; overflow-y:auto;">';

                bills.forEach(function (b) {
                    contentHtml += '<div class="py-1 border-bottom text-dark fw-semibold d-flex align-items-center" style="font-family:monospace; font-size:11.5px; white-space:nowrap;">' +
                                   '<i class="fas fa-file-alt text-secondary me-2 small"></i>' +
                                   $('<div>').text(b).html() +
                                   '</div>';
                });
                contentHtml += '</div>';

                $popover.html(contentHtml).data('trigger-btn', btn);

                // Position calculation relative to viewport
                var rect = btn.getBoundingClientRect();
                $popover.css({ display: 'block', visibility: 'hidden', top: 0, left: 0 });
                var popWidth = $popover.outerWidth() || 220;
                var popHeight = $popover.outerHeight() || 180;

                var left = rect.left + (rect.width / 2) - (popWidth / 2);
                if (left < 10) left = 10;
                if (left + popWidth > window.innerWidth - 10) {
                    left = window.innerWidth - popWidth - 10;
                }

                var top = rect.bottom + 4;
                // If it goes past bottom of viewport and fits above, flip to top
                if (top + popHeight > window.innerHeight - 10 && (rect.top - popHeight - 4) > 10) {
                    top = rect.top - popHeight - 4;
                }

                $popover.css({
                    top: Math.round(top) + 'px',
                    left: Math.round(left) + 'px',
                    visibility: 'visible'
                });
            });

            // Close popover when clicking anywhere outside
            $(document).on('click', function (e) {
                var $popover = $('#gctl-bill-list-floating-popover');
                if ($popover.is(':visible')) {
                    if (!$(e.target).closest('#gctl-bill-list-floating-popover, .js-toggle-bill-list').length) {
                        $popover.hide().removeData('trigger-btn');
                    }
                }
            });

            // Close popover on close button click
            $("body").off('click', '.js-close-bill-popover').on('click', '.js-close-bill-popover', function (e) {
                e.stopPropagation();
                $('#gctl-bill-list-floating-popover').hide().removeData('trigger-btn');
            });

            // Close popover on window scroll, resize, or table scroll
            $(window).on('scroll resize', function () {
                $('#gctl-bill-list-floating-popover').hide().removeData('trigger-btn');
            });
            $("body").on('scroll', '.dataTables_scrollBody, .table-responsive, .custom-grid-card', function () {
                $('#gctl-bill-list-floating-popover').hide().removeData('trigger-btn');
            });
            $(document).on('draw.dt', function () {
                $('#gctl-bill-list-floating-popover').hide().removeData('trigger-btn');
            });

            // Listen for DataTables error event to log safely without alerting user
            $(settings.customerGridSelector + ', ' + settings.receivedGridSelector + ', ' + settings.dueInvoicesGridSelector).on('error.dt', function (e, dtSettings, techNote, message) {
                console.warn('DataTables warning suppressed:', message);
            });

            // Initialize top CustomerList grid if not already initialized
            if ($.gctlGrid) {
                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                    var isLaptop = window.innerWidth <= 1600 || (window.screen && window.screen.availWidth <= 1600);
                    var defaultPageLen = isLaptop ? 5 : 10;
                    $.gctlGrid.initFromModel({
                        gridId: settings.customerGridId,
                        pageLength: defaultPageLen,
                        lengthMenu: [[5, 10, 25, 50, -1], [5, 10, 25, 50, 'All']]
                    });
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

                // Initialize bottom PaymentReceivedHistory grid automatically
                if (!$.fn.DataTable || !$.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    var fDate = $('#filterFromDate').val();
                    var tDate = $('#filterToDate').val();
                    var recUrl = base + `/SearchPaymentReceived?fromDate=${encodeURIComponent(fDate)}&toDate=${encodeURIComponent(tDate)}`;
                    $.gctlGrid.initFromModel({
                        gridId: settings.receivedGridId,
                        ajaxUrl: recUrl
                    });
                }
            }

            // Initialize custom-datepicker if needed (avoid altInput reinitialization)
            if (window.custom_Datepicker && window.custom_Datepicker.init) {
                $('input.custom-datepicker:not(.flatpickr-alt-input):not([type="hidden"])').each(function () {
                    window.custom_Datepicker.init(this);
                });
            }

            // Initialize custom-select-single (customDropdownSingle from custom-select-2.js)
            if (window.initCustomDropdowns) {
                window.initCustomDropdowns(settings.formContainer || document, false);
            }

            // Clear any hardcoded inline width on Against/Bill List so CSS layout takes full width
            function alignAgainstBillWidth() {
                var $input = $('#AgainstBillList');
                if (!$input.length) return;
                var el = $input[0];
                el.style.removeProperty('width');
                el.style.removeProperty('max-width');
                el.style.removeProperty('position');
                el.style.removeProperty('z-index');
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
            function formatDateForInput(dateVal) {
                if (!dateVal) return '';
                var d = (dateVal instanceof Date) ? dateVal : new Date(dateVal);
                if (isNaN(d.getTime())) return String(dateVal);
                var year = d.getFullYear();
                var month = ('0' + (d.getMonth() + 1)).slice(-2);
                var day = ('0' + d.getDate()).slice(-2);
                return `${year}-${month}-${day}`;
            }

            function setDatepickerVal(selector, val) {
                var $el = $(selector);
                if (!$el.length) return;

                if (!val) {
                    var fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
                    if (fp) {
                        fp.clear();
                    } else {
                        $el.val('');
                    }
                    return;
                }

                // Ensure flatpickr is initialized on the element
                if (!$el.data('customFpInstance') && window.custom_Datepicker && window.custom_Datepicker.init) {
                    window.custom_Datepicker.init($el[0]);
                }

                var dateObj = null;
                var dateIso = '';
                var dateDisplay = '';

                if (val instanceof Date) {
                    dateObj = val;
                } else {
                    var s = String(val).trim();
                    if (s.includes('T')) s = s.split('T')[0];
                    // Check DD/MM/YYYY
                    var dmyMatch = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
                    if (dmyMatch) {
                        dateObj = new Date(parseInt(dmyMatch[3], 10), parseInt(dmyMatch[2], 10) - 1, parseInt(dmyMatch[1], 10));
                    } else {
                        var parsed = new Date(s);
                        if (!isNaN(parsed.getTime())) {
                            dateObj = parsed;
                        }
                    }
                }

                if (dateObj && !isNaN(dateObj.getTime())) {
                    var year = dateObj.getFullYear();
                    var month = ('0' + (dateObj.getMonth() + 1)).slice(-2);
                    var day = ('0' + dateObj.getDate()).slice(-2);
                    dateIso = `${year}-${month}-${day}`;
                    dateDisplay = `${day}/${month}/${year}`;
                } else {
                    dateIso = String(val);
                    dateDisplay = String(val);
                }

                $el.val(dateIso);
                $el.attr('value', dateIso);

                var fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
                if (fp) {
                    try {
                        if (dateObj && !isNaN(dateObj.getTime())) {
                            fp.setDate(dateObj, false);
                        } else {
                            fp.setDate(dateIso, false);
                        }
                    } catch (e) { }

                    if (fp.altInput && dateDisplay) {
                        fp.altInput.value = dateDisplay;
                    }
                } else if (window.setDate) {
                    try {
                        window.setDate(selector, dateObj || dateIso);
                    } catch (e) {
                        $el.val(dateIso);
                    }
                }

                // Direct DOM update on visible input next to hidden input
                var nextInp = $el.next('input')[0];
                if (nextInp && (nextInp.classList.contains('input') || nextInp.classList.contains('flatpickr-alt-input') || nextInp.classList.contains('custom-datepicker'))) {
                    nextInp.value = dateDisplay;
                }
            }

            function ensureDefaultDates(force) {
                var isEdit = ($('#Tc').val() && parseInt($('#Tc').val()) > 0);
                var today = new Date();
                if (!isEdit || force) {
                    setDatepickerVal('#PaymentReceiveDate', today);
                    setDatepickerVal('#MrDate', today);
                }
                if ($('#filterToDate').length) {
                    setDatepickerVal('#filterToDate', today);
                }
            }

            // Ensure Money Receipt Date, Payment Receive Date & To Date default to current date
            ensureDefaultDates();
            setTimeout(ensureDefaultDates, 50);
            setTimeout(ensureDefaultDates, 150);
            setTimeout(ensureDefaultDates, 300);
            setTimeout(ensureDefaultDates, 600);

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
                if ($(e.target).is('button, a')) return;
                var $link = $(this).find('a.js-select-customer, a[data-id]');
                var clickedId = $link.length ? ($link.data('id') || $link.text().trim()) : '';
                selectCustomerFromRow($(this), clickedId);
            });

            // Maintain selected row highlight across pagination and search
            $('#CustomerList-grid').on('draw.dt', function () {
                syncCustomerGridSelection();
            });

            function syncCustomerGridSelection() {
                var currentId = $('#CustomerId').val();
                if (!currentId) {
                    $('#CustomerList-grid tbody tr').removeClass('selected-row is-selected table-active');
                    return;
                }
                $('#CustomerList-grid tbody tr').each(function () {
                    var $r = $(this);
                    var rowId = '';
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.customerGridSelector)) {
                        var d = $(settings.customerGridSelector).DataTable().row($r).data();
                        if (d) {
                            rowId = d.customerId || d.CustomerID || '';
                        }
                    }
                    if (!rowId) {
                        var $link = $r.find('a.js-select-customer, a[data-id]');
                        rowId = $link.length ? ($link.data('id') || $link.text().trim()) : '';
                    }
                    if (!rowId) {
                        rowId = $r.find('td:first-child').text().trim();
                    }
                    if (rowId && rowId === currentId) {
                        $r.addClass('selected-row is-selected table-active');
                    } else {
                        $r.removeClass('selected-row is-selected table-active');
                    }
                });
            }

            function selectCustomerFromRow($row, passedCustomerId) {
                if (!$row || $row.length === 0) return;

                // Highlight selected row
                $('#CustomerList-grid tbody tr').removeClass('selected-row is-selected table-active bg-primary-subtle');
                $row.addClass('selected-row is-selected table-active');

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
                        customerId = $cells.eq(1).text().trim() || $cells.eq(0).text().trim();
                    }
                    if (!customerName) customerName = $cells.eq(2).text().trim() || $cells.eq(1).text().trim();
                    if (!customerAddress) customerAddress = $cells.eq(3).text().trim() || $cells.eq(2).text().trim();
                }

                if (!customerId) return;

                // Populate Main Information fields
                $('#CustomerId').val(customerId);
                $('#CustomerNameDisplay').val(customerName);
                $('#BillingAddressDisplay').text(customerAddress || '').attr('title', customerAddress || '');
                $('#BillingAddressHidden').val(customerAddress || '');

                // Ensure Payment Receive Date & Money Receipt Date always default to current date
                var isEditMode = ($('#Tc').val() && parseInt($('#Tc').val()) > 0);
                if (!isEditMode) {
                    var today = new Date();
                    setDatepickerVal('#PaymentReceiveDate', today);
                    setDatepickerVal('#MrDate', today);
                }

                // Reset bill & payment selection
                $('#AgainstBillList').val('');
                $('#Amount').val('');
                $('#SelectedInvoicesJson').val('[]');

                // Load Due Invoices for this customer
                loadDueInvoices(customerId);
            }
            //#endregion

            //#region Due Invoices (CustomGrid with Server Data & Pagination)
            function loadDueInvoices(customerId, preSelectedBills, isEditMode, preSelectedAit) {
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
                                ? preSelectedBills.split(/[\r\n,;]+/).map(function (b) { return b.trim(); }).filter(Boolean)
                                : preSelectedBills;
                            isManualCheckboxMode = true;
                            list.forEach(function (item) {
                                var bNo = item.billNo || item.BillNo;
                                if (preList.includes(bNo)) {
                                    var dueVal = parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                                    selectedInvoicesMap[bNo] = {
                                        invoiceNo: bNo,
                                        amount: dueVal
                                    };
                                }
                            });
                            syncDueInvoiceCheckboxes();
                            if (!isEditMode) {
                                syncManualCheckboxState();
                            } else {
                                var selCount = Object.keys(selectedInvoicesMap).length;
                                if (selCount > 0) {
                                    $('#dueInvoicesCountBadge').text(selCount + ' of ' + count + ' Selected');
                                }
                            }
                        } else {
                            // New customer selection: start fresh
                            selectedInvoicesMap = {};
                            fifoAllocationMap = {};
                            isManualCheckboxMode = false;
                            populateAITInvoiceDropdown(currentDueInvoices, []);
                        }

                        if (isEditMode && preSelectedAit) {
                            populateAITInvoiceDropdown(currentDueInvoices, preSelectedAit);
                        }
                    });
                }
            }

            var selectedInvoicesMap = {}; // Checked invoices: { [billNo]: { invoiceNo, amount } }
            var fifoAllocationMap = {};  // FIFO allocated invoices when no checkboxes are checked
            var isManualCheckboxMode = false;

            // Safe date parser for FIFO ordering (supports DD/MM/YYYY and YYYY-MM-DD)
            function parseBillDate(dateStr) {
                if (!dateStr) return 0;
                if (dateStr instanceof Date) return dateStr.getTime();
                var s = String(dateStr).trim();
                var dmyMatch = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
                if (dmyMatch) {
                    var day = parseInt(dmyMatch[1], 10);
                    var month = parseInt(dmyMatch[2], 10) - 1;
                    var year = parseInt(dmyMatch[3], 10);
                    return new Date(year, month, day).getTime();
                }
                var ymdMatch = s.match(/^(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})/);
                if (ymdMatch) {
                    var year = parseInt(ymdMatch[1], 10);
                    var month = parseInt(ymdMatch[2], 10) - 1;
                    var day = parseInt(ymdMatch[3], 10);
                    return new Date(year, month, day).getTime();
                }
                var parsed = Date.parse(s);
                return isNaN(parsed) ? 0 : parsed;
            }

            // FIFO Engine: Used ONLY when user DOES NOT check checkboxes ("ami check kore na dile tokhon ai fifo system use hobe")
            function applyFifoFromAmount(enteredAmount) {
                var payAmount = parseFloat(enteredAmount);
                fifoAllocationMap = {};

                if (isNaN(payAmount) || payAmount <= 0 || !Array.isArray(currentDueInvoices) || currentDueInvoices.length === 0) {
                    $('#SelectedInvoicesJson').val('[]');
                    $('#AgainstBillList').val('');
                    var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                    $cbs.prop('checked', false).closest('tr').removeClass('selected-row');
                    $('#dueInvoicesCheckAll').prop('checked', false);
                    $('#dueInvoicesCountBadge').text(currentDueInvoices.length + ' Invoices');
                    updateVisibleFifoCells();
                    return;
                }

                // Sort loaded due invoices by billDate ascending (oldest first)
                var sorted = currentDueInvoices.slice().sort(function (a, b) {
                    var tA = parseBillDate(a.billDate || a.BillDate);
                    var tB = parseBillDate(b.billDate || b.BillDate);
                    if (tA !== tB) return tA - tB;
                    return String(a.billNo || a.BillNo || '').localeCompare(String(b.billNo || b.BillNo || ''));
                });

                var remainingPool = payAmount;
                var allocatedItems = [];
                var allocatedBills = [];

                for (var i = 0; i < sorted.length; i++) {
                    var item = sorted[i];
                    var bNo = item.billNo || item.BillNo;
                    if (!bNo) continue;

                    var invDue = parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                    if (remainingPool <= 0) break;

                    var pay = Math.min(remainingPool, invDue);
                    remainingPool = remainingPool - pay;
                    var remDue = Math.max(0, invDue - pay);
                    var status = (pay >= invDue) ? 'Full Paid' : 'Partial Paid';

                    fifoAllocationMap[bNo] = {
                        invoiceNo: bNo,
                        amount: parseFloat(pay.toFixed(2)),
                        paidAmount: pay,
                        due: invDue,
                        remainingDue: remDue,
                        status: status
                    };

                    allocatedBills.push(bNo);
                    allocatedItems.push({
                        InvoiceNo: bNo,
                        Amount: parseFloat(pay.toFixed(2)),
                        Due: parseFloat(invDue.toFixed(2)),
                        RemainingDue: parseFloat(remDue.toFixed(2)),
                        Status: status
                    });
                }

                // Update Against/Bill List input
                $('#AgainstBillList').val(allocatedBills.join(', '));

                // Serialize for backend
                $('#SelectedInvoicesJson').val(JSON.stringify(allocatedItems));

                // Check checkboxes for allocated invoices on the visible grid
                var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                $cbs.each(function () {
                    var bNo = $(this).val() || $(this).data('id');
                    var isAllocated = !!fifoAllocationMap[bNo];
                    $(this).prop('checked', isAllocated);
                    if (isAllocated) {
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).closest('tr').removeClass('selected-row');
                    }
                });

                $('#dueInvoicesCountBadge').text(allocatedBills.length + ' of ' + currentDueInvoices.length + ' Allocated (FIFO)');
                $('#dueInvoicesCheckAll').prop('checked', allocatedBills.length > 0 && allocatedBills.length === currentDueInvoices.length);

                updateVisibleFifoCells();
            }

            // Direct DOM update of visible table cells: ultra-fast (<1ms), zero lag
            function updateVisibleFifoCells() {
                var $table = $(settings.dueInvoicesGridSelector);
                $table.find('tbody tr').each(function () {
                    var $tr = $(this);
                    var $cb = $tr.find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                    if (!$cb.length) return;
                    var bNo = $cb.val() || $cb.data('id');
                    if (!bNo) return;

                    var item = currentDueInvoices.find(function (x) { return (x.billNo || x.BillNo) === bNo; });
                    var origDue = item ? parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0)) : 0;

                    var $invCell = $tr.find('td').eq(1); // Invoice No. column
                    var $dueCell = $tr.find('td').eq(4); // Due column

                    var fifo = fifoAllocationMap[bNo];
                    if (fifo && !isManualCheckboxMode) {
                        var badgeHtml = '';
                        if (fifo.status === 'Full Paid') {
                            badgeHtml = ' <span class="badge bg-success-subtle text-success border border-success-subtle py-0 px-1 ms-1" style="font-size: 10px; font-weight:600;"><i class="fas fa-check-circle me-1"></i>Full Paid</span>';
                        } else if (fifo.status === 'Partial Paid') {
                            badgeHtml = ' <span class="badge bg-warning-subtle text-dark border border-warning-subtle py-0 px-1 ms-1" style="font-size: 10px; font-weight:600;" title="Paid: ' + formatCurrency(fifo.paidAmount) + ' | Due: ' + formatCurrency(fifo.remainingDue) + '"><i class="fas fa-adjust me-1 text-warning"></i>Paid ' + formatCurrency(fifo.paidAmount) + '</span>';
                        }
                        $invCell.html('<span class="fw-bold text-primary">' + bNo + '</span>' + badgeHtml);

                        if (fifo.status === 'Full Paid') {
                            $dueCell.html('<div class="text-end text-nowrap"><del class="text-muted small me-1">' + formatCurrency(origDue) + '</del><span class="fw-bold text-success">0.00</span></div>');
                        } else if (fifo.status === 'Partial Paid') {
                            $dueCell.html('<div class="text-end text-nowrap"><del class="text-muted small me-1">' + formatCurrency(origDue) + '</del><span class="fw-bold text-danger">' + formatCurrency(fifo.remainingDue) + '</span></div>');
                        }
                    } else {
                        // Normal mode ("ager motoi")
                        $invCell.html('<span class="fw-bold text-primary">' + bNo + '</span>');
                        $dueCell.html('<div class="text-end text-nowrap">' + formatCurrency(origDue) + '</div>');
                    }
                });
            }

            // Sync checkboxes on currently active page with selectedInvoicesMap or fifoAllocationMap
            function syncDueInvoiceCheckboxes() {
                var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                var allChecked = $cbs.length > 0;
                $cbs.each(function () {
                    var billNo = $(this).val() || $(this).data('id');
                    var isSel = isManualCheckboxMode ? !!selectedInvoicesMap[billNo] : !!fifoAllocationMap[billNo];
                    $(this).prop('checked', isSel);
                    if (isSel) {
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        $(this).closest('tr').removeClass('selected-row');
                        allChecked = false;
                    }
                });

                $('#dueInvoicesCheckAll').prop('checked', allChecked && $cbs.length > 0);
                updateVisibleFifoCells();
            }

            // Listen for draw.dt on DueInvoices grid to maintain checkbox state across pagination
            $(settings.dueInvoicesGridSelector).on('draw.dt', function () {
                syncDueInvoiceCheckboxes();
            });

            // Manual Checkbox Click: "ami check korle dile ager motoi hobe"
            $(settings.dueInvoicesGridSelector).off('change', '.form-check-input.checkbox, input[type="checkbox"].checkbox')
                .on('change', '.form-check-input.checkbox, input[type="checkbox"].checkbox', function () {
                    isManualCheckboxMode = true;
                    fifoAllocationMap = {};

                    var isChecked = $(this).prop('checked');
                    var billNo = $(this).val() || $(this).data('id');
                    if (!billNo) return;

                    if (isChecked) {
                        var item = currentDueInvoices.find(function (x) { return (x.billNo || x.BillNo) === billNo; });
                        var dueVal = item ? parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0)) : 0;
                        selectedInvoicesMap[billNo] = {
                            invoiceNo: billNo,
                            amount: dueVal
                        };
                        $(this).closest('tr').addClass('selected-row');
                    } else {
                        delete selectedInvoicesMap[billNo];
                        $(this).closest('tr').removeClass('selected-row');
                    }

                    // If user unchecked all checkboxes, reset to allow FIFO mode
                    if (Object.keys(selectedInvoicesMap).length === 0) {
                        isManualCheckboxMode = false;
                    }

                    syncManualCheckboxState();
                });

            // Master Check All in DueInvoices grid: "ager motoi hobe"
            $("body").off('click', '#dueInvoicesCheckAll').on('click', '#dueInvoicesCheckAll', function () {
                var isChecked = $(this).prop('checked');
                isManualCheckboxMode = true;
                fifoAllocationMap = {};

                if (isChecked) {
                    currentDueInvoices.forEach(function (item) {
                        var bNo = item.billNo || item.BillNo;
                        if (bNo) {
                            var dueVal = parseFloat(item.due != null ? item.due : (item.Due != null ? item.Due : 0));
                            selectedInvoicesMap[bNo] = {
                                invoiceNo: bNo,
                                amount: dueVal
                            };
                        }
                    });
                    $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox')
                        .prop('checked', true).closest('tr').addClass('selected-row');
                } else {
                    selectedInvoicesMap = {};
                    isManualCheckboxMode = false;
                    $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox')
                        .prop('checked', false).closest('tr').removeClass('selected-row');
                }

                syncManualCheckboxState();
            });

            function syncManualCheckboxState() {
                var selectedBills = Object.keys(selectedInvoicesMap);
                var totalDueSum = 0;
                var selectedItems = [];

                selectedBills.forEach(function (billNo) {
                    var item = selectedInvoicesMap[billNo];
                    totalDueSum += (item.amount || 0);
                    selectedItems.push({
                        InvoiceNo: billNo,
                        Amount: parseFloat((item.amount || 0).toFixed(2))
                    });
                });

                // Update Against/Bill List input
                $('#AgainstBillList').val(selectedBills.join(', '));

                // Update Payment Amount with sum of checked invoices
                if (selectedBills.length > 0) {
                    $('#Amount').val(totalDueSum.toFixed(2));
                } else {
                    $('#Amount').val('');
                }

                // Store serialized JSON in hidden field
                $('#SelectedInvoicesJson').val(JSON.stringify(selectedItems));

                // Update count badge in header
                var totalCount = currentDueInvoices.length;
                var selCount = selectedBills.length;
                if (selCount > 0) {
                    $('#dueInvoicesCountBadge').text(selCount + ' of ' + totalCount + ' Selected');
                } else {
                    $('#dueInvoicesCountBadge').text(totalCount + ' Invoices');
                }

                // Update check-all checkbox state
                var $cbs = $(settings.dueInvoicesGridSelector).find('.form-check-input.checkbox, input[type="checkbox"].checkbox');
                var gridTotal = $cbs.length;
                var gridChecked = $cbs.filter(':checked').length;
                $('#dueInvoicesCheckAll').prop('checked', gridTotal > 0 && gridTotal === gridChecked);

                updateVisibleFifoCells();
            }

            // Real-time listener for Amount input:
            // "but ami check kore na dile tokhon ai fifo system use hobe"
            $("body").off('input change', '#Amount').on('input change', '#Amount', function () {
                // If user has explicitly checked checkboxes, do NOT trigger FIFO
                if (isManualCheckboxMode && Object.keys(selectedInvoicesMap).length > 0) {
                    return;
                }
                isManualCheckboxMode = false;
                applyFifoFromAmount($(this).val());
            });

            function populateAITInvoiceDropdown(invoices, selectedVals) {
                var $sel = $('#AITInvoiceNo');
                if (!$sel.length) return;

                var selectedArray = [];
                if (selectedVals !== undefined && selectedVals !== null) {
                    if (Array.isArray(selectedVals)) {
                        selectedArray = selectedVals.map(function (s) { return String(s).trim(); }).filter(Boolean);
                    } else if (typeof selectedVals === 'string' && selectedVals.trim() !== '') {
                        selectedArray = selectedVals.split(/[\r\n,;]+/).map(function (s) { return s.trim(); }).filter(Boolean);
                    }
                }

                var uniqueInvoices = [];
                var seen = {};

                function addInvoice(no) {
                    no = String(no || '').trim();
                    if (no && !seen[no]) {
                        seen[no] = true;
                        uniqueInvoices.push(no);
                    }
                }

                if (Array.isArray(invoices)) {
                    invoices.forEach(function (inv) {
                        var no = typeof inv === 'string' ? inv : (inv.billNo || inv.invoiceNo || inv.BillNo || '');
                        addInvoice(no);
                    });
                }

                // Ensure selected values are in options
                selectedArray.forEach(addInvoice);

                // Build clean options (NO empty option!)
                var optionsHtml = '';
                uniqueInvoices.forEach(function (no) {
                    var isSel = selectedArray.indexOf(no) !== -1 ? ' selected="selected"' : '';
                    optionsHtml += '<option value="' + no + '"' + isSel + '>' + no + '</option>';
                });

                $sel.html(optionsHtml);

                if (window.reinitById) {
                    window.reinitById('AITInvoiceNo');
                } else if (window.reinitCustomDropdownById) {
                    window.reinitCustomDropdownById('AITInvoiceNo');
                }

                if (window.setSelect) {
                    window.setSelect('#AITInvoiceNo', selectedArray);
                }
            }

            // Sync AIT Invoice dropdown when Against/Bill List is manually updated
            $("body").off('input change', '#AgainstBillList').on('input change', '#AgainstBillList', function () {
                var val = $(this).val() || '';
                var bills = val.split(/[\r\n,;]+/).map(function (s) { return s.trim(); }).filter(function (s) { return s.length > 0; });
                if (Object.keys(selectedInvoicesMap).length === 0 && Object.keys(fifoAllocationMap).length === 0 && bills.length > 0) {
                    populateAITInvoiceDropdown(bills, $('#AITInvoiceNo').val());
                }
            });
            //#endregion

            //#region Save / Submit
            $("body").off("click", settings.saveSelector).on("click", settings.saveSelector, function (e) {
                e.preventDefault();

                if (!validatePaymentForm()) {
                    return false;
                }

                var formEl = document.getElementById('PaymentReceiveEntry-form') || $(settings.formSelector)[0];
                var formData = new FormData(formEl);

                // Ensure multi-select AITInvoiceNo is formatted as comma-separated string
                var aitVals = $('#AITInvoiceNo').val();
                var aitStr = '';
                if (Array.isArray(aitVals)) {
                    aitStr = aitVals.join(', ');
                } else if (aitVals) {
                    aitStr = String(aitVals);
                }
                formData.set('AITInvoiceNo', aitStr);

                var fileInput = document.getElementById('AITChallanFile');
                if (fileInput && fileInput.files && fileInput.files.length > 0) {
                    formData.set('AITChallanFile', fileInput.files[0]);
                }

                var url = $(settings.formSelector).attr("action") || saveUrl;

                var $submitBtn = $(this);
                var isEdit = ($('#Tc').val() && parseInt($('#Tc').val()) > 0);
                $submitBtn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin me-1"></i> ' + (isEdit ? 'Updating...' : 'Submitting...'));

                $.ajax({
                    url: url,
                    method: "POST",
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function (response) {
                        $submitBtn.prop('disabled', false).html('<i class="far fa-save me-1 text-primary"></i> Submit');

                        if (response.isSuccess) {
                            toastr.success(response.message || (isEdit ? "Updated Successfully." : "Saved Successfully."));

                            // Reload customer list grid
                            reloadCustomerGrid();

                            // Reload bottom grid
                            reloadReceivedGrid();

                            // Reset form & generate new IDs
                            resetForm();
                        } else {
                            toastr.error(response.message || "Failed to save record.");
                            if (isEdit) {
                                $submitBtn.html('<i class="fas fa-edit me-1 text-primary"></i> Update');
                            }
                        }
                    },
                    error: function () {
                        $submitBtn.prop('disabled', false).html(isEdit ? '<i class="fas fa-edit me-1 text-primary"></i> Update' : '<i class="far fa-save me-1 text-primary"></i> Submit');
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

            //#region Bottom Filter By: Auto reload on date change
            function reloadReceivedGridWithDates() {
                var fromDate = $('#filterFromDate').val();
                var toDate = $('#filterToDate').val();
                if (!fromDate || !toDate) return;
                var newUrl = base + `/SearchPaymentReceived?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}`;
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    $(settings.receivedGridSelector).DataTable().ajax.url(newUrl).load();
                } else if ($.gctlGrid) {
                    $.gctlGrid.initFromModel({
                        gridId: settings.receivedGridId,
                        ajaxUrl: newUrl
                    });
                }
            }

            // Auto reload when date changes
            $('#filterFromDate, #filterToDate').on('change', function () {
                reloadReceivedGridWithDates();
            });

            // Also support flatpickr altInput change
            setTimeout(function () {
                $('#filterFromDate, #filterToDate').each(function () {
                    var fp = $(this).data('customFpInstance');
                    if (fp) {
                        var origChange = fp.config.onChange;
                        fp.config.onChange = function (selectedDates, dateStr, instance) {
                            if (origChange) origChange(selectedDates, dateStr, instance);
                            reloadReceivedGridWithDates();
                        };
                    }
                });
            }, 300);
            //#endregion

            //#region Edit (Load from Bottom Grid)
            $("body").off('click', '.js-PaymentReceiveEntry-edit, #PaymentReceivedHistory-grid tbody tr td:nth-child(2) a')
                .on('click', '.js-PaymentReceiveEntry-edit, #PaymentReceivedHistory-grid tbody tr td:nth-child(2) a', function (e) {
                e.preventDefault();
                var id = $(this).data('id') || $(this).attr('data-id') || $(this).text().trim();
                if (!id) {
                    var $row = $(this).closest('tr');
                    if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                        var rowData = $(settings.receivedGridSelector).DataTable().row($row).data();
                        if (rowData) {
                            id = rowData.paymentReceiveId || rowData.PaymentReceiveId || '';
                        }
                    }
                }
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
                            syncCustomerGridSelection();

                            $('#MrNo').val(data.mrNo || '');
                            if (data.mrDate) {
                                setDatepickerVal('#MrDate', formatDateForInput(data.mrDate));
                            }
                            $('#Amount').val(data.amount != null ? Number(data.amount).toFixed(2) : '');
                            $('#BankFeeOnlineCharge').val(data.bankFeeOnlineCharge != null ? Number(data.bankFeeOnlineCharge).toFixed(2) : '');
                            $('#Remarks').val(data.remarks || '');

                            // AIT fields
                            $('#AIT').val(data.ait != null ? Number(data.ait).toFixed(2) : '0.00');
                            $('#AITChallanNo').val(data.aitChallanNo || '');
                            $('#AITChallanUpload').val(data.aitChallanUpload || '');
                            if (data.aitChallanUpload) {
                                if ($('#AITChallanFileLink').length === 0) {
                                    $('#AITChallanFile').after('<a href="' + data.aitChallanUpload + '" target="_blank" class="btn btn-xs btn-outline-info text-nowrap py-1 px-2 ms-1" id="AITChallanFileLink" title="View Uploaded Challan"><i class="fas fa-file-download"></i> View</a>');
                                } else {
                                    $('#AITChallanFileLink').attr('href', data.aitChallanUpload).show();
                                }
                            } else {
                                $('#AITChallanFileLink').hide();
                            }

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

                            var savedAit = (data.aitInvoiceNo || '').split(/[\r\n,;]+/).map(function (s) { return s.trim(); }).filter(Boolean);

                            // Load due invoices and pre-select against bills without wiping amount or against bill list
                            if (data.customerId) {
                                loadDueInvoices(data.customerId, data.againstBillList, true, savedAit);
                            }

                            if (savedAit.length > 0) {
                                setTimeout(function () {
                                    populateAITInvoiceDropdown(currentDueInvoices, savedAit);
                                }, 300);
                            }

                            // Change button to Update
                            $('#btnSubmit, .js-PaymentReceiveEntry-save').html('<i class="fas fa-edit me-1 text-primary"></i> Update');

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
                syncCustomerGridSelection();
                if (window.setSelect) {
                    window.setSelect('#PaymentTypeId', '01');
                    window.setSelect('#PaymentModeCode', '001');
                    window.setSelect('#Receivedby', '');
                } else {
                    $('#PaymentTypeId').val('01').trigger('change');
                    $('#PaymentModeCode').val('001').trigger('change');
                    $('#Receivedby').val('').trigger('change');
                }
                setDatepickerVal('#PaymentReceiveDate', new Date());
                setDatepickerVal('#MrDate', new Date());
                setDatepickerVal('#filterToDate', new Date());
                $('#Amount').val('');
                $('#BankFeeOnlineCharge').val('');
                $('#AIT').val('0.00000');
                $('#AITChallanNo').val('');
                $('#AITInvoiceNo').empty();
                if (window.clearCustomDropdownById) {
                    window.clearCustomDropdownById('AITInvoiceNo');
                }
                if (window.reinitById) {
                    window.reinitById('AITInvoiceNo');
                } else if (window.reinitCustomDropdownById) {
                    window.reinitCustomDropdownById('AITInvoiceNo');
                }
                $('#AITChallanFile').val('');
                $('#AITChallanUpload').val('');
                $('#AITChallanFileLink').hide();
                $('#Remarks').val('');
                $('#SelectedInvoicesJson').val('[]');
                $('#modifyDateSpan').text('');
                alignAgainstBillWidth();

                // Clear Due Invoices Table
                selectedInvoicesMap = {};
                fifoAllocationMap = {};
                isManualCheckboxMode = false;
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
                $('#btnSubmit, .js-PaymentReceiveEntry-save').html('<i class="far fa-save me-1 text-primary"></i> Submit');
            }
            //#endregion

            //#region Delete
            function hideDeleteModal() {
                var $modal = $(settings.deleteModal);
                if ($modal.length === 0) $modal = $("#deleteModal, #delete-modal").first();
                if ($modal.length > 0) {
                    if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                        var modalInstance = bootstrap.Modal.getInstance($modal[0]);
                        if (modalInstance) {
                            modalInstance.hide();
                        } else {
                            $modal.modal("hide");
                        }
                    } else {
                        $modal.modal("hide");
                    }
                }
                setTimeout(function () {
                    $('body').removeClass('modal-open').css('overflow', '');
                    $('.modal-backdrop').remove();
                }, 350);
            }

            $("body").off("click", settings.deleteSelector).on("click", settings.deleteSelector, function (e) {
                e.preventDefault();
                selectedItemsToDelete = [];

                // 1. Check bottom grid selection (via DataTables API or DOM checkboxes)
                if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    var dt = $(settings.receivedGridSelector).DataTable();
                    dt.$('tbody input[type="checkbox"]:checked').each(function () {
                        var $chk = $(this);
                        var val = $chk.attr('data-id') || $chk.val();
                        if (!val || val === 'on') {
                            var rowData = dt.row($chk.closest('tr')).data();
                            if (rowData) {
                                val = rowData.paymentReceiveId || rowData.PaymentReceiveId;
                            }
                        }
                        if (val && val !== 'on' && !selectedItemsToDelete.includes(String(val).trim())) {
                            selectedItemsToDelete.push(String(val).trim());
                        }
                    });
                } else {
                    $(settings.receivedGridSelector).find("tbody input[type='checkbox']:checked").each(function () {
                        var $chk = $(this);
                        var val = $chk.attr('data-id') || $chk.val();
                        if (val && val !== 'on' && !selectedItemsToDelete.includes(String(val).trim())) {
                            selectedItemsToDelete.push(String(val).trim());
                        }
                    });
                }

                // 2. If nothing checked in bottom grid, check currently loaded form record
                if (selectedItemsToDelete.length === 0) {
                    var btnId = $(this).data('id') || $(this).attr('data-id');
                    var formId = $("#PaymentReceiveId").val();
                    var tc = $("#Tc").val();
                    var isUpdateMode = $('#btnSubmit, .js-PaymentReceiveEntry-save').text().toLowerCase().includes('update');

                    if (btnId) {
                        selectedItemsToDelete.push(String(btnId).trim());
                    } else if (formId && (tc || isUpdateMode)) {
                        selectedItemsToDelete.push(String(formId).trim());
                    }
                }

                if (selectedItemsToDelete.length > 0) {
                    var count = selectedItemsToDelete.length;
                    var titleText = count > 1
                        ? `Are you sure you want to delete these ${count} records? This process cannot be undone.`
                        : `Are you sure you want to delete payment receive record "${selectedItemsToDelete[0]}"? This process cannot be undone.`;

                    var $modal = $(settings.deleteModal);
                    if ($modal.length === 0) $modal = $("#deleteModal, #delete-modal").first();
                    $modal.find('.modal-title, .title, .confirmation-text, p.title').text(titleText);
                    
                    if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                        bootstrap.Modal.getOrCreateInstance($modal[0]).show();
                    } else {
                        $modal.modal("show");
                    }
                } else {
                    toastr.info("Please select a record from the grid or load a record to delete.");
                }
            });

            var deleteConfirmSelectors = "#deleteModal .btn-danger, #deleteModal .js-delete, #deleteModal .js-PaymentReceiveEntry-delete, " + settings.finalDeleteSelector;
            $("body").off("click", deleteConfirmSelectors).on("click", deleteConfirmSelectors, function (e) {
                e.stopPropagation();
                e.preventDefault();
                e.stopImmediatePropagation();

                if (!selectedItemsToDelete || selectedItemsToDelete.length === 0) {
                    hideDeleteModal();
                    return;
                }

                var $btn = $(this);
                var originalHtml = $btn.html();
                $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i> Deleting...');

                $.ajax({
                    url: deleteUrl,
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify(selectedItemsToDelete),
                    success: function (res) {
                        hideDeleteModal();
                        $btn.prop('disabled', false).html(originalHtml);

                        if (res.success) {
                            toastr.success(res.message || "Deleted Successfully.");
                            selectedItemsToDelete = [];
                            $('#PaymentReceivedHistory-check-all, ' + settings.receivedGridSelector + ' th input[type="checkbox"]').prop('checked', false);
                            reloadCustomerGrid();
                            reloadReceivedGrid();
                            resetForm();
                            $(settings.deleteSelector).data('id', '');
                        } else {
                            toastr.error(res.message || "Delete Failed.");
                        }
                    },
                    error: function (xhr) {
                        hideDeleteModal();
                        $btn.prop('disabled', false).html(originalHtml);

                        var msg = "An error occurred during deletion.";
                        try {
                            var errRes = JSON.parse(xhr.responseText);
                            if (errRes && errRes.message) msg = errRes.message;
                        } catch (e) { }
                        toastr.error(msg);
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
                var fromDate = $('#filterFromDate').val();
                var toDate = $('#filterToDate').val();
                if (fromDate && toDate && $.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    var newUrl = base + `/SearchPaymentReceived?fromDate=${encodeURIComponent(fromDate)}&toDate=${encodeURIComponent(toDate)}`;
                    $(settings.receivedGridSelector).DataTable().ajax.url(newUrl).load(null, false);
                } else if ($.gctlGrid) {
                    $.gctlGrid.reload(settings.receivedGridId);
                } else if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.receivedGridSelector)) {
                    $(settings.receivedGridSelector).DataTable().ajax.reload(null, false);
                }
            }
            //#endregion
        });
    };
})(jQuery);
