(function ($) {
    'use strict';

    // Global Renderers
    window.gctlRenderers = window.gctlRenderers || {};

    window.gctlRenderers.formatCurrency = function (data, type) {
        if (data === null || data === undefined || data === '' || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '0.00';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') {
            return num;
        }
        if (Math.abs(num) < 0.00001) {
            return '0.00';
        }
        return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    // Invoice list renderer: if > 1 invoice, show first + hover badge with list
    window.gctlRenderers.renderInvoiceList = function (data, type, row) {
        if (!data || typeof data !== 'string') return '';
        if (type === 'export') return data;

        var rawList = data.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        if (rawList.length <= 1) {
            return rawList[0] || '';
        }

        var firstInv = rawList[0];
        var remainingCount = rawList.length - 1;
        var popoverHtml = '<ul class="invoice-popover-list">' +
            rawList.map(function (inv) { return '<li><i class="fas fa-file-invoice text-primary me-1"></i>' + inv + '</li>'; }).join('') +
            '</ul>';

        var encodedHtml = encodeURIComponent(popoverHtml);

        return '<span class="text-nowrap">' + firstInv +
            ' <span class="inv-more-badge" role="button" tabindex="0" data-bs-html="true" data-bs-title="All Invoices (' + rawList.length + ')" data-bs-content-html="' + encodedHtml + '">+' + remainingCount + ' More</span>' +
            '</span>';
    };

    let gridTable = null;
    let filterDebounceTimer = null;
    let isInitialized = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        initDefaultDates();
        initGrid();
        bindEvents();

        setTimeout(function () {
            isInitialized = true;
        }, 500);
    });

    function initDefaultDates() {
        var now = new Date();
        var currentMonthFirstDay = new Date(now.getFullYear(), now.getMonth(), 1);

        var formatDate = function (d) {
            var day = String(d.getDate()).padStart(2, '0');
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var year = d.getFullYear();
            return day + '/' + month + '/' + year;
        };

        var fromStr = formatDate(currentMonthFirstDay);
        var toStr = formatDate(now);

        var $from = $('#filterFromDate');
        var $to = $('#filterToDate');

        if (!$from.val()) {
            $from.val(fromStr);
        }
        if (!$to.val()) {
            $to.val(toStr);
        }

        var syncFlatpickr = function () {
            var fpFrom = $from.data('customFpInstance') || ($from[0] ? $from[0]._flatpickr : null);
            if (fpFrom) {
                if (!fpFrom.selectedDates || !fpFrom.selectedDates.length) {
                    fpFrom.setDate(currentMonthFirstDay, false);
                }
                if (fpFrom.altInput && !fpFrom.altInput.value) {
                    fpFrom.altInput.value = fromStr;
                }
            } else if (window.setDate) {
                window.setDate('#filterFromDate', fromStr);
            }

            var fpTo = $to.data('customFpInstance') || ($to[0] ? $to[0]._flatpickr : null);
            if (fpTo) {
                if (!fpTo.selectedDates || !fpTo.selectedDates.length) {
                    fpTo.setDate(now, false);
                }
                if (fpTo.altInput && !fpTo.altInput.value) {
                    fpTo.altInput.value = toStr;
                }
            } else if (window.setDate) {
                window.setDate('#filterToDate', toStr);
            }

            bindFlatpickrChangeEvents();
        };

        syncFlatpickr();
        setTimeout(syncFlatpickr, 100);
        setTimeout(syncFlatpickr, 300);
    }

    var flatpickrEventsBound = false;
    function bindFlatpickrChangeEvents() {
        if (flatpickrEventsBound) return;
        var fromEl = document.getElementById('filterFromDate');
        var toEl = document.getElementById('filterToDate');

        var fpFrom = (fromEl && fromEl._flatpickr) || $('#filterFromDate').data('customFpInstance');
        var fpTo = (toEl && toEl._flatpickr) || $('#filterToDate').data('customFpInstance');

        if (!fpFrom || !fpTo) return;
        flatpickrEventsBound = true;

        if (fpFrom.config && Array.isArray(fpFrom.config.onChange)) {
            fpFrom.config.onChange.push(function () {
                onFilterChanged();
            });
        }

        if (fpTo.config && Array.isArray(fpTo.config.onChange)) {
            fpTo.config.onChange.push(function () {
                onFilterChanged();
            });
        }
    }

    function getDateValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
        if (fp) {
            if (fp.selectedDates && fp.selectedDates.length > 0) {
                var d = fp.selectedDates[0];
                var day = String(d.getDate()).padStart(2, '0');
                var month = String(d.getMonth() + 1).padStart(2, '0');
                var year = d.getFullYear();
                return day + '/' + month + '/' + year;
            }
            if (fp.altInput && fp.altInput.value) {
                return fp.altInput.value.trim();
            }
        }

        var $alt = $el.siblings('.flatpickr-alt-input');
        if ($alt.length && $alt.val()) {
            return $alt.val().trim();
        }

        var rawVal = ($el.val() || '').trim();
        if (/^\d{4}-\d{2}-\d{2}$/.test(rawVal)) {
            var parts = rawVal.split('-');
            return parts[2] + '/' + parts[1] + '/' + parts[0];
        }

        return rawVal;
    }

    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst) {
            if (Array.isArray(inst.selected)) {
                return inst.selected
                    .filter(function (v) { return v !== null && v !== undefined && String(v).trim() !== '' && String(v).indexOf('Select') === -1; })
                    .join(',');
            }
            if (inst.selected !== undefined && inst.selected !== null) {
                var s = String(inst.selected).trim();
                return s.indexOf('Select') === -1 ? s : '';
            }
        }

        var val = $el.val();
        if (Array.isArray(val)) {
            return val
                .filter(function (v) { return v !== null && v !== undefined && String(v).trim() !== '' && String(v).indexOf('Select') === -1; })
                .join(',');
        }

        var textVal = (val || '').trim();
        return textVal.indexOf('Select') === -1 ? textVal : '';
    }

    function getSelectedPaymentReceiveNos() {
        var selected = [];
        $('#paymentReceiveReportTable tbody .chk-payment-item:checked').each(function () {
            var val = $(this).val() || $(this).data('id');
            if (val) {
                selected.push(String(val).trim());
            }
        });
        return selected.join(',');
    }

    function getFilterParams() {
        return {
            fromDate: getDateValue('filterFromDate'),
            toDate: getDateValue('filterToDate'),
            customerTypeId: getDropdownValue('filterCustomerTypeId'),
            district: getDropdownValue('filterDistrict'),
            employeeId: getDropdownValue('filterEmployeeId'),
            paymentTypeId: getDropdownValue('filterPaymentTypeId'),
            paymentModeId: getDropdownValue('filterPaymentModeId'),
            paymentStatus: getDropdownValue('filterPaymentStatus'),
            selectedPaymentReceiveNos: getSelectedPaymentReceiveNos()
        };
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'paymentReceiveReportTable',
                extraOptions: {
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ],
                    drawCallback: function () {
                        var $table = $('#paymentReceiveReportTable');
                        $table.removeClass('gctl-table-loading');
                        $table.find('tbody').css('min-height', '');
                        $table.find('tbody tr.skeleton-row').remove();

                        initInvoicePopovers();
                    }
                },
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.fromDate = filters.fromDate;
                    d.toDate = filters.toDate;
                    d.customerTypeId = filters.customerTypeId;
                    d.district = filters.district;
                    d.employeeId = filters.employeeId;
                    d.paymentTypeId = filters.paymentTypeId;
                    d.paymentModeId = filters.paymentModeId;
                    d.paymentStatus = filters.paymentStatus;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    var $table = $('#paymentReceiveReportTable');
                    $table.removeClass('gctl-table-loading');
                    $table.find('tbody').css('min-height', '');
                    $table.find('tbody tr.skeleton-row').remove();

                    if (json && json.summary) {
                        updateKpiCards(json.summary);
                    } else {
                        updateKpiCards({ totalReceivedAmount: 0, regularAmount: 0, advanceAmount: 0, totalTransactions: 0 });
                    }
                    initInvoicePopovers();
                }
            });

            $('#paymentReceiveReportTable').on('xhr.dt', function (e, settings, json) {
                var $table = $('#paymentReceiveReportTable');
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').css('min-height', '');
                $table.find('tbody tr.skeleton-row').remove();
                if (json && json.summary) {
                    updateKpiCards(json.summary);
                }
            });

            $('#paymentReceiveReportTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error in PaymentReceiveReport:', techNote, message);
                var $table = $('#paymentReceiveReportTable');
                $table.removeClass('gctl-table-loading');
                $table.find('tbody').html('<tr><td colspan="17" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function initInvoicePopovers() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Popover) return;

        $('.inv-more-badge').each(function () {
            var existing = bootstrap.Popover.getInstance(this);
            if (existing) {
                existing.dispose();
            }
        });

        $('.inv-more-badge').each(function () {
            var badgeEl = this;
            var encodedContent = badgeEl.getAttribute('data-bs-content-html');
            var content = encodedContent ? decodeURIComponent(encodedContent) : '';

            var popover = new bootstrap.Popover(badgeEl, {
                html: true,
                trigger: 'manual',
                customClass: 'invoice-custom-popover',
                content: content,
                container: 'body',
                placement: 'auto'
            });

            var isPinned = false;
            var hideTimeout = null;

            function getTip() {
                if (popover.tip) return popover.tip;
                if (typeof popover.getTipElement === 'function') {
                    try { return popover.getTipElement(); } catch (e) { }
                }
                var popId = badgeEl.getAttribute('aria-describedby');
                if (popId) {
                    var el = document.getElementById(popId);
                    if (el) return el;
                }
                return document.querySelector('.popover.invoice-custom-popover');
            }

            function showPopover() {
                clearTimeout(hideTimeout);

                // Hide any other open popovers
                $('.inv-more-badge').not(badgeEl).each(function () {
                    var other = bootstrap.Popover.getInstance(this);
                    if (other) {
                        other.hide();
                    }
                    $(this).removeClass('active-popover');
                });

                popover.show();
                $(badgeEl).addClass('active-popover');

                // Bind hover events to popover container so mouse over popover keeps it visible
                setTimeout(function () {
                    var tip = getTip();
                    if (tip) {
                        $(tip).off('mouseenter.invPop mouseleave.invPop')
                            .on('mouseenter.invPop', function () {
                                clearTimeout(hideTimeout);
                            })
                            .on('mouseleave.invPop', function () {
                                if (!isPinned) {
                                    scheduleHide();
                                }
                            });
                    }
                }, 50);
            }

            function scheduleHide() {
                if (isPinned) return;
                clearTimeout(hideTimeout);
                hideTimeout = setTimeout(function () {
                    popover.hide();
                    $(badgeEl).removeClass('active-popover');
                }, 350);
            }

            $(badgeEl).off('mouseenter mouseleave click')
                .on('mouseenter', function () {
                    if (!isPinned) {
                        showPopover();
                    }
                })
                .on('mouseleave', function () {
                    scheduleHide();
                })
                .on('click', function (e) {
                    e.stopPropagation();
                    isPinned = !isPinned;
                    if (isPinned) {
                        showPopover();
                    } else {
                        popover.hide();
                        $(badgeEl).removeClass('active-popover');
                    }
                });
        });

        // Close when clicking outside of any badge or popover
        $(document).off('click.invoicePopover').on('click.invoicePopover', function (e) {
            if (!$(e.target).closest('.inv-more-badge, .popover').length) {
                $('.inv-more-badge').each(function () {
                    var p = bootstrap.Popover.getInstance(this);
                    if (p) {
                        p.hide();
                    }
                    $(this).removeClass('active-popover');
                });
            }
        });
    }

    function updateKpiCards(summary) {
        if (!summary) {
            summary = { totalReceivedAmount: 0, regularAmount: 0, advanceAmount: 0, totalTransactions: 0 };
        }
        $('#stat_TotalReceived').text(formatNumber(summary.totalReceivedAmount));
        $('#stat_RegularAmount').text(formatNumber(summary.regularAmount));
        $('#stat_AdvanceAmount').text(formatNumber(summary.advanceAmount));
        $('#stat_TotalTransactions').text(summary.totalTransactions || 0);
    }

    function formatNumber(val) {
        if (val === null || val === undefined || isNaN(val)) return '0.00';
        return parseFloat(val).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function onFilterChanged() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 120);
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('paymentReceiveReportTable', true);
        } else if (gridTable && gridTable.ajax) {
            gridTable.ajax.reload(null, true);
        } else {
            var $table = $('#paymentReceiveReportTable');
            if ($.fn.DataTable && $.fn.DataTable.isDataTable($table)) {
                $table.DataTable().ajax.reload(null, true);
            }
        }
    }

    function openPreviewModal() {
        var filters = getFilterParams();
        var queryParams = $.param(filters);
        var previewUrl = '/PaymentReceiveReport/PreviewPdf?' + queryParams;

        var $modal = $('#pdfPreviewModal');
        if (!$modal.length) {
            $('body').append(
                '<div class="modal fade" id="pdfPreviewModal" tabindex="-1" aria-hidden="true">' +
                '  <div class="modal-dialog modal-xl modal-dialog-centered" style="max-width: 95vw; height: 92vh;">' +
                '    <div class="modal-content h-100">' +
                '      <div class="modal-header py-2 px-3 border-bottom">' +
                '        <h6 class="modal-title font-weight-bold"><i class="fas fa-file-pdf text-danger me-2"></i> Payment Receive Report - Preview</h6>' +
                '        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                '      </div>' +
                '      <div class="modal-body p-0 h-100" style="background: #525659;">' +
                '        <iframe id="pdfPreviewIframe" src="" style="width: 100%; height: 100%; border: none;"></iframe>' +
                '      </div>' +
                '    </div>' +
                '  </div>' +
                '</div>'
            );
            $modal = $('#pdfPreviewModal');
        }

        $('#pdfPreviewIframe').attr('src', previewUrl);
        var modalInstance = bootstrap.Modal.getOrCreateInstance($modal[0]);
        modalInstance.show();
    }

    function exportReport(format) {
        var filters = getFilterParams();
        filters.format = format;
        var queryParams = $.param(filters);
        window.location.href = '/PaymentReceiveReport/Export?' + queryParams;
    }

    function handleExportAction(action) {
        if (!action) return;
        var act = String(action).trim().toUpperCase();

        if (act === 'PREVIEW' || act.indexOf('PREVIEW') !== -1) {
            openPreviewModal();
        } else if (act === 'PDF') {
            exportReport('PDF');
        } else if (act === 'EXCEL' || act === 'XLSX') {
            exportReport('EXCEL');
        }

        setTimeout(function () {
            if (typeof window.clearCustomDropdownById === 'function') {
                window.clearCustomDropdownById('filterExportFormat');
            } else {
                var $el = $('#filterExportFormat');
                var inst = $el.data('customDdInstance');
                if (inst && typeof inst.clear === 'function') {
                    inst.clear();
                } else {
                    $el.val('');
                }
            }
        }, 350);
    }

    function bindEvents() {
        // Flatpickr & standard date change (handles both hidden and visible inputs)
        $(document).on('change', '#filterFromDate, #filterToDate, input.flatpickr-alt-input', function () {
            onFilterChanged();
        });

        // Native select changes
        $('#filterCustomerTypeId, #filterDistrict, #filterEmployeeId, #filterPaymentTypeId, #filterPaymentModeId, #filterPaymentStatus').on('change', function () {
            onFilterChanged();
        });

        // Custom Dropdown click, option select, clear or remove (from custom-select-2 widget)
        $('.filter-section').on('click', '.custom-dd-option, .custom-dd-clear, .custom-dd-tag-remove, .custom-dd-item', function () {
            var $wrapper = $(this).closest('.filter-group');
            if ($wrapper.hasClass('filter-group-export') || $wrapper.find('#filterExportFormat').length > 0) {
                return;
            }
            setTimeout(function () {
                onFilterChanged();
            }, 30);
        });

        // Export Dropdown
        $('#filterExportFormat').on('change', function () {
            var val = $(this).val();
            if (val) {
                handleExportAction(val);
            }
        });

        $('.filter-section').on('click', '#filterExportFormat ~ .custom-dd-wrapper .custom-dd-option', function () {
            var val = $(this).data('value') || $(this).attr('data-id') || $(this).text();
            setTimeout(function () {
                var currentVal = getDropdownValue('filterExportFormat') || val;
                if (currentVal) {
                    handleExportAction(currentVal);
                }
            }, 50);
        });
    }

})(jQuery);
