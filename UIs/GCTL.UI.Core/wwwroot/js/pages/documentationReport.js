/**
 * Documentation Report Page Script
 * Formosa Logistics Limited - C&F Management System
 */

$(document).ready(function () {
    bindEvents();
});

/**
 * Event bindings
 */
function bindEvents() {
    // Cascade Customer -> Job Nos
    $('#CustomerId').on('change', function () {
        var customerId = $(this).val();
        loadJobNosByCustomer(customerId);
    });

    // Preview Button Click
    $('#btnPreview').on('click', function () {
        handleReportPreview();
    });

    // Export Report / Download Button Click
    $('#downloadReport, #btnDownload').on('click', function () {
        handleReportDownload();
    });

    // Toolbar Print Dropdown buttons
    $('#btnPrintPdfBtn').on('click', function (e) {
        e.preventDefault();
        $('#reportText').val('downloadPdf');
        if (typeof window.setCustomDropdownValue === 'function') {
            window.setCustomDropdownValue('reportText', 'downloadPdf', 'Pdf');
        }
        handleReportPreview();
    });

    $('#btnPrintExcelBtn').on('click', function (e) {
        e.preventDefault();
        $('#reportText').val('downloadExcel');
        if (typeof window.setCustomDropdownValue === 'function') {
            window.setCustomDropdownValue('reportText', 'downloadExcel', 'Excel');
        }
        handleReportDownload();
    });

    // Toolbar Clear Button Click
    $('#btnClear, .js-docreport-clear').on('click', function () {
        handleClearForm();
    });

    // Clean up iframe memory when preview modal closes
    $('#docReportPreviewModal').on('hidden.bs.modal', function () {
        $('#pdfPreviewFrame').attr('src', '').hide();
        $('#pdfLoadingSpinner').show();
    });
}

/**
 * Helper to safely get value from custom-select-multi or custom-select-single
 */
function getDropdownValue(id) {
    var $el = $('#' + id);
    if (!$el.length) return '';

    // Check customDropdown instance first if multi
    var inst = $el.data('customDdInstance');
    if (inst && Array.isArray(inst.selected)) {
        return inst.selected.filter(function (v) { return !!v; }).join(',');
    }

    var val = $el.val();
    if (Array.isArray(val)) {
        return val.filter(function (v) { return !!v; }).join(',');
    }

    return (val || '').trim();
}

/**
 * Helper to normalize selected export format
 */
function getSelectedFormat() {
    var raw = getDropdownValue('reportText') || getDropdownValue('Format') || 'downloadPdf';
    raw = raw.toLowerCase();

    if (raw.indexOf('excel') !== -1 || raw.indexOf('xlsx') !== -1) {
        return 'Excel';
    } else if (raw.indexOf('word') !== -1 || raw.indexOf('doc') !== -1) {
        return 'Word';
    } else {
        return 'PDF';
    }
}

/**
 * Gather form filter object
 */
function getFormFilters() {
    var reportOption = $('input[name="ReportOptionRadio"]:checked').val() || 'Job Report';

    return {
        ShipmentMode: getDropdownValue('ShipmentMode'),
        PortType: getDropdownValue('PortType'),
        FromDate: ($('#FromDate').val() || '').trim(),
        ToDate: ($('#ToDate').val() || '').trim(),
        CustomerId: getDropdownValue('CustomerId'),
        JobStatus: getDropdownValue('JobStatus'),
        JobNo: getDropdownValue('JobNo'),
        ReportOption: reportOption,
        Format: getSelectedFormat()
    };
}

/**
 * Load Job Nos filtered by selected Customer
 */
function loadJobNosByCustomer(customerId) {
    var $jobNo = $('#JobNo');
    $jobNo.empty().append('<option value="">-- Select Job No --</option>');

    $.ajax({
        url: '/DocumentationReport/GetJobNos',
        type: 'GET',
        data: { customerId: customerId || '' },
        success: function (data) {
            if (Array.isArray(data)) {
                $.each(data, function (index, item) {
                    $jobNo.append(new Option(item, item));
                });
            }
            // Reinitialize customDropdownSingle if available
            if (typeof window.reinitCustomDropdownById === 'function') {
                window.reinitCustomDropdownById('JobNo');
            } else {
                $jobNo.trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error fetching Job Nos:', error);
        }
    });
}

/**
 * Parse dd/MM/yyyy date string to Date object
 */
function parseDateDMY(dateStr) {
    if (!dateStr) return null;
    var parts = dateStr.trim().split('/');
    if (parts.length === 3) {
        var day = parseInt(parts[0], 10);
        var month = parseInt(parts[1], 10) - 1;
        var year = parseInt(parts[2], 10);
        var date = new Date(year, month, day);
        if (!isNaN(date.getTime())) {
            return date;
        }
    }
    return null;
}

/**
 * Validate date ranges
 */
function validateDates(fromDateStr, toDateStr) {
    if (fromDateStr && toDateStr) {
        var fromDate = parseDateDMY(fromDateStr);
        var toDate = parseDateDMY(toDateStr);

        if (fromDate && toDate && fromDate > toDate) {
            if (typeof toastr !== 'undefined') {
                toastr.error('From Date cannot be greater than To Date.');
            } else {
                alert('From Date cannot be greater than To Date.');
            }
            $('#FromDate').focus();
            return false;
        }
    }
    return true;
}

/**
 * Handle report preview
 */
function handleReportPreview() {
    var filters = getFormFilters();

    if (!filters.Format) {
        if (typeof toastr !== 'undefined') {
            toastr.warning('Please select a Format (PDF or Excel).');
        } else {
            alert('Please select a Format (PDF or Excel).');
        }
        return;
    }

    if (!validateDates(filters.FromDate, filters.ToDate)) return;

    var queryString = $.param(filters);
    var previewUrl = '/DocumentationReport/Preview?' + queryString;

    if (filters.Format.toUpperCase() === 'PDF') {
        // Open PDF in dedicated in-page preview modal
        var modalEl = document.getElementById('docReportPreviewModal');
        if (modalEl) {
            var $spinner = $('#pdfLoadingSpinner');
            var $frame = $('#pdfPreviewFrame');

            $spinner.show();
            $frame.hide();

            // Set up button actions in the preview modal header
            $('#btnModalOpenTab').off('click').on('click', function () {
                window.open(previewUrl, '_blank');
            });

            var pdfFilters = $.extend({}, filters, { Format: 'PDF' });
            var pdfDownloadUrl = '/DocumentationReport/Download?' + $.param(pdfFilters);
            $('#btnModalDownloadPdf').off('click').on('click', function () {
                window.location.href = pdfDownloadUrl;
            });

            var excelFilters = $.extend({}, filters, { Format: 'Excel' });
            var excelDownloadUrl = '/DocumentationReport/Download?' + $.param(excelFilters);
            $('#btnModalDownloadExcel').off('click').on('click', function () {
                window.location.href = excelDownloadUrl;
            });

            $frame.off('load').on('load', function () {
                $spinner.hide();
                $frame.show();
            });

            $frame.attr('src', previewUrl);

            var bsModal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            bsModal.show();
        } else {
            // Fallback to opening in new window/tab
            window.open(previewUrl, '_blank');
        }
    } else {
        // For Excel / Word, preview downloads directly
        if (typeof toastr !== 'undefined') {
            toastr.info('Downloading Excel Report preview...');
        }
        window.location.href = previewUrl;
    }
}

/**
 * Handle report download / export submission
 */
function handleReportDownload() {
    var filters = getFormFilters();

    if (!filters.Format) {
        if (typeof toastr !== 'undefined') {
            toastr.warning('Please select a Format (PDF or Excel).');
        } else {
            alert('Please select a Format (PDF or Excel).');
        }
        return;
    }

    if (!validateDates(filters.FromDate, filters.ToDate)) return;

    var queryString = $.param(filters);
    var downloadUrl = '/DocumentationReport/Download?' + queryString;

    // Show button loading spinner
    var $btn = $('#downloadReport');
    if (!$btn.length) $btn = $('#btnDownload');
    var originalHtml = $btn.html();
    $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i> <span>Exporting...</span>');

    if (typeof toastr !== 'undefined') {
        toastr.info('Generating ' + filters.Format + ' Report...');
    }

    // Trigger download
    window.location.href = downloadUrl;

    setTimeout(function () {
        $btn.prop('disabled', false).html(originalHtml);
    }, 2500);
}

/**
 * Handle clearing all filters and restoring default form state
 */
function handleClearForm() {
    // Clear date inputs
    $('#FromDate').val('');
    $('#ToDate').val('');

    var fpFrom = document.getElementById('FromDate')?._flatpickr;
    if (fpFrom) fpFrom.clear();
    var fpTo = document.getElementById('ToDate')?._flatpickr;
    if (fpTo) fpTo.clear();

    // Clear custom-select dropdowns
    if (typeof window.clearAllCustomDropdowns === 'function') {
        window.clearAllCustomDropdowns('#docReportForm');
    } else {
        $('#docReportForm select').val('').trigger('change');
    }

    // Set Format back to Pdf
    $('#reportText').val('downloadPdf');
    if (typeof window.setCustomDropdownValue === 'function') {
        window.setCustomDropdownValue('reportText', 'downloadPdf', 'Pdf');
    }

    // Reset Job Report radio
    $('input[name="ReportOptionRadio"][value="Job Report"]').prop('checked', true);

    // Reload full job numbers list
    loadJobNosByCustomer('');

    if (typeof toastr !== 'undefined') {
        toastr.info('Filters have been cleared.');
    }
}
