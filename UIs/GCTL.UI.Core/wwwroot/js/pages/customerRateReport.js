// Register renderers for CustomGrid
window.gctlRenderers = window.gctlRenderers || {};

window.gctlRenderers.renderPhoto = function (data) {
    if (data) {
        var src = (data.startsWith('/') || data.startsWith('http') || data.startsWith('data:'))
            ? data
            : 'data:image/jpeg;base64,' + data;
        return '<img src="' + src + '" alt="Photo" loading="lazy" class="js-customer-photo-preview" data-src="' + src + '" title="Click to view large photo" style="width:36px;height:36px;border-radius:6px;object-fit:cover;cursor:pointer;transition:transform 0.15s ease,box-shadow 0.15s ease;" onmouseover="this.style.transform=\'scale(1.15)\';this.style.boxShadow=\'0 2px 8px rgba(0,0,0,0.25)\';" onmouseout="this.style.transform=\'scale(1)\';this.style.boxShadow=\'none\';" onerror="this.onerror=null;this.parentElement.innerHTML=\'<span class=\\\'text-muted\\\'>-</span>\';" />';
    }
    return '<span class="text-muted">-</span>';
};

window.gctlRenderers.renderContactPerson = function (data) {
    return '<span style="text-align:left; display:block; white-space:pre-line;">' + escapeHtml(data) + '</span>';
};

window.gctlRenderers.renderAlignLeft = function (data) {
    return '<span style="text-align:left; display:block;">' + escapeHtml(data) + '</span>';
};

window.gctlRenderers.renderCustomerId = function (data) {
    return '<span class="job-no-cell">' + escapeHtml(data) + '</span>';
};

function escapeHtml(str) {
    if (!str) return '';
    return $('<div>').text(str).html();
}

window.gctlRenderers.renderCustomerRateSubRow = function (row) {
    if (!row) return '';
    var items = row.rateItems || [];
    if (!items.length) {
        return '<div class="customer-rate-subrow p-3 text-muted small"><i class="fas fa-info-circle me-1"></i> No rate details found for this customer.</div>';
    }

    var html = '<div class="customer-rate-subrow p-2 bg-light border rounded m-1">';

    // Rate details table
    html += '<div class="table-responsive">';
    html += '<table class="table table-sm table-bordered bg-white mb-0" style="font-size: 11px;">';
    html += '<thead class="table-light">';
    html += '<tr>';
    html += '<th style="width: 8%;" class="text-center align-middle">Sec.</th>';
    html += '<th style="width: 52%;" class="text-start align-middle">Particulars</th>';
    html += '<th style="width: 20%;" class="text-end align-middle">Amount (BDT)</th>';
    html += '<th style="width: 20%;" class="text-start align-middle">Remarks</th>';
    html += '</tr>';
    html += '</thead>';
    html += '<tbody>';

    items.forEach(function (item) {
        if (item.isHeader) {
            html += '<tr class="table-secondary fw-bold">';
            html += '<td class="text-center">' + (item.section || '') + '</td>';
            html += '<td colspan="3" class="text-start text-primary fw-bold">' + (item.particulars || '') + '</td>';
            html += '</tr>';
        } else {
            html += '<tr>';
            html += '<td class="text-center text-muted">' + (item.section || '') + '</td>';
            html += '<td class="text-start ps-3">' + (item.particulars || '') + '</td>';
            html += '<td class="text-end fw-semibold tabular-number">' + (item.formattedAmount || '') + '</td>';
            html += '<td class="text-start text-muted">' + (item.remarks || '') + '</td>';
            html += '</tr>';
        }
    });

    html += '</tbody>';
    html += '</table>';
    html += '</div>';
    html += '</div>';

    return html;
};

(function ($) {
    'use strict';

    let gridTable = null;
    let filterDebounceTimer = null;

    $(document).ready(function () {
        initGrid();
        bindEvents();
        loadSummaryData();
    });

    /**
     * Helper to safely extract values from custom-select-multi dropdowns
     */
    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst) {
            if (Array.isArray(inst.selected)) {
                return inst.selected.filter(function (v) { return !!v; }).join(',');
            }
            if (inst.selected !== undefined && inst.selected !== null) {
                return String(inst.selected).trim();
            }
        }

        var val = $el.val();
        if (Array.isArray(val)) {
            return val.filter(function (v) { return !!v; }).join(',');
        }

        return (val || '').trim();
    }

    function initGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'customerRateReportTable',
                ajaxData: function (d) {
                    d.CustomerIds = getDropdownValue('filterCustomerId');
                }
            });
        }
    }

    function bindEvents() {
        $('#filterCustomerId').on('change', function () {
            triggerFilterChange();
        });

        // Photo Preview click
        $(document).on('click', '.js-customer-photo-preview', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var src = $(this).attr('data-src') || $(this).attr('src');
            if (src) {
                $('#customerPhotoModalImg').attr('src', src);
                var modalEl = document.getElementById('customerPhotoModal');
                if (modalEl && window.bootstrap) {
                    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                    modal.show();
                } else {
                    window.open(src, '_blank');
                }
            }
        });

        // Handle Export Format dropdown selection (native change)
        $('#filterExportFormat').on('change', function () {
            var val = $(this).val();
            if (val) {
                handleExportAction(val);
            }
        });

        // Handle Export Format dropdown selection (custom-select-2 click)
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

    /**
     * Get customer IDs for export: prioritizes checked checkboxes in grid,
     * falls back to filter dropdown selection.
     */
    function getSelectedCustomerIdsForExport() {
        var checkedIds = [];
        $('.chk-customer-item:checked').each(function () {
            var val = $(this).val();
            if (val && checkedIds.indexOf(val) === -1) {
                checkedIds.push(val);
            }
        });

        if (checkedIds.length > 0) {
            return checkedIds.join(',');
        }

        return getDropdownValue('filterCustomerId');
    }

    function handleExportAction(action) {
        if (!action) return;
        var act = String(action).trim().toUpperCase();

        var customerIds = getSelectedCustomerIdsForExport();
        if (!customerIds) {
            if (typeof toastr !== 'undefined') {
                toastr.warning('Please select at least one Customer (via row checkbox or Customer filter).');
            } else {
                alert('Please select at least one Customer (via row checkbox or Customer filter).');
            }
            clearExportDropdown();
            return;
        }

        var queryParams = $.param({ CustomerIds: customerIds });

        if (act === 'PREVIEW' || act.indexOf('PREVIEW') !== -1) {
            window.open('/CustomerRateReport/ExportPdf?' + queryParams, '_blank');
        } else if (act === 'PDF') {
            window.location.href = '/CustomerRateReport/ExportPdf?' + queryParams + '&download=true';
        } else if (act === 'EXCEL' || act.indexOf('EXCEL') !== -1 || act === 'XLSX') {
            window.location.href = '/CustomerRateReport/ExportExcel?' + queryParams;
        }

        clearExportDropdown();
    }

    function clearExportDropdown() {
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

    function triggerFilterChange() {
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            onCustomerChanged();
        }, 250);
    }

    function onCustomerChanged() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('customerRateReportTable', true);
        } else if (gridTable) {
            gridTable.ajax.reload();
        }
        loadSummaryData();
    }

    function loadSummaryData() {
        var customerIds = getDropdownValue('filterCustomerId');

        if (!customerIds) {
            resetCounters();
            return;
        }

        $.ajax({
            url: '/CustomerRateReport/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ CustomerIds: customerIds }),
            success: function (res) {
                if (res && res.success) {
                    animateCounter('#statTotalChcItems', res.totalChcItems || 0, false);
                    animateCounter('#statTotalHandlingAmount', res.totalHandlingAmount || 0, true);
                    animateCounter('#statMinAgencyCommission', res.minAgencyCommission || 0, true);
                    $('#statOnInvoiceValue').text((res.onInvoiceValue != null ? Number(res.onInvoiceValue).toFixed(2) : '0.00') + '%');
                } else {
                    resetCounters();
                }
            },
            error: function () {
                resetCounters();
            }
        });
    }

    function resetCounters() {
        $('#statTotalChcItems').text('0');
        $('#statTotalHandlingAmount').text('0.00');
        $('#statMinAgencyCommission').text('0.00');
        $('#statOnInvoiceValue').text('0.00%');
    }

    function animateCounter(selector, target, isCurrency) {
        var $el = $(selector);
        if (!$el.length) return;

        var currentText = $el.text().replace(/,/g, '');
        var current = parseFloat(currentText) || 0;
        var diff = target - current;
        var steps = 12;
        var stepTime = 20;
        var step = 0;

        if (diff === 0) {
            $el.text(isCurrency ? target.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : target.toLocaleString());
            return;
        }

        var timer = setInterval(function () {
            step++;
            var val = current + (diff * (step / steps));
            if (isCurrency) {
                $el.text(val.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
            } else {
                $el.text(Math.round(val).toLocaleString());
            }

            if (step >= steps) {
                clearInterval(timer);
                if (isCurrency) {
                    $el.text(target.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                } else {
                    $el.text(target.toLocaleString());
                }
            }
        }, stepTime);
    }

    function getExportQueryParams() {
        var customerIds = getDropdownValue('filterCustomerId');
        return $.param({
            CustomerIds: customerIds
        });
    }

})(jQuery);
