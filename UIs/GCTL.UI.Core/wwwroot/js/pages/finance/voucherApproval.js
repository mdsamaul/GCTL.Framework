$(document).ready(function () {
    /* Helper to safely extract value from custom dropdown or standard select */
    function getDropdownValue(selector) {
        var $el = $(selector);
        if (!$el.length) return '';
        var inst = $el.data('customDdInstance');
        if (inst && inst.selected !== undefined && inst.selected !== null) {
            return String(inst.selected).trim();
        }
        return ($el.val() || '').trim();
    }

    /* Helper to safely extract date value from custom-datepicker or flatpickr */
    function getDateValue(selector) {
        var id = selector.startsWith('#') ? selector.substring(1) : selector;
        if (window.custom_Datepicker && typeof window.custom_Datepicker.getValue === 'function') {
            var val = window.custom_Datepicker.getValue('#' + id);
            if (val) return val.trim();
        }
        var el = document.getElementById(id);
        if (el && el._flatpickr && el._flatpickr.input) {
            return (el._flatpickr.input.value || '').trim();
        }
        return ($('#' + id).val() || '').trim();
    }

    // Function to reload KPI Dashboard Summary
    function loadKpiSummary() {
        $.ajax({
            url: '/VoucherApproval/GetSummary',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                CustomerCode: getDropdownValue('#filterCustomerId'),
                FromDate: getDateValue('#filterFromDate'),
                ToDate: getDateValue('#filterToDate'),
                VoucherStatus: getDropdownValue('#filterStatus')
            }),
            success: function (res) {
                if (res && res.success && res.summary) {
                    var s = res.summary;
                    $('#statTotalVouchers').text(s.totalVouchers || 0);
                    $('#statTotalAmount').text((parseFloat(s.totalAmount) || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                    $('#statPendingApproval').text(s.pendingApproval || 0);
                    $('#statApprovedVouchers').text(s.approvedVouchers || 0);
                }
            }
        });
    }

    // Initial KPI load
    loadKpiSummary();

    // DataTables Server-side initialization
    var table = $('#tblVoucherApproval').DataTable({
        processing: true,
        serverSide: true,
        responsive: false,
        scrollX: false, // Prevents horizontal scroll on laptop/desktop
        autoWidth: false,
        ordering: false,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        dom: '<"d-flex justify-content-between align-items-center mb-2"<"dt-left"l><"dt-right"f>>rt<"d-flex justify-content-between align-items-center mt-2"<"dt-info"i><"dt-pagination"p>><"clear">',
        ajax: {
            url: '/VoucherApproval/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: function (d) {
                return JSON.stringify({
                    Draw: d.draw,
                    Start: d.start,
                    Length: d.length,
                    SearchValue: d.search ? d.search.value : '',
                    CustomerCode: getDropdownValue('#filterCustomerId'),
                    FromDate: getDateValue('#filterFromDate'),
                    ToDate: getDateValue('#filterToDate'),
                    VoucherStatus: getDropdownValue('#filterStatus')
                });
            },
            dataSrc: 'data'
        },
        columns: [
            {
                data: null,
                orderable: false,
                className: 'text-center align-middle',
                render: function (data, type, row) {
                    return '<input type="checkbox" class="row-select-chk" value="' + (row.voucherNo || '') + '" />';
                }
            },
            {
                data: 'voucherNo',
                className: 'col-voucher-no align-middle',
                render: function (data) {
                    return '<a href="javascript:void(0);" onclick="viewVoucher(\'' + (data || '') + '\')" class="fw-bold text-decoration-underline">' + (data || '') + '</a>';
                }
            },
            { data: 'voucherDate', className: 'text-center align-middle' },
            { 
                data: 'narration', 
                className: 'col-narration align-middle',
                render: function (data) {
                    return data || '';
                }
            },
            { data: 'branch', className: 'align-middle', defaultContent: 'Chattogram Branch' },
            {
                data: 'amount',
                className: 'col-amount align-middle',
                render: function (data) {
                    var val = parseFloat(data) || 0;
                    return val.toFixed(2);
                }
            },
            { data: 'jobNo', className: 'align-middle', defaultContent: '' },
            { data: 'hawb', className: 'align-middle', defaultContent: '' },
            { data: 'beNo', className: 'align-middle', defaultContent: '' },
            { 
                data: 'isApproved', 
                className: 'text-center align-middle', 
                defaultContent: 'Not Approved',
                render: function (data) {
                    if (data === 'Approved') {
                        return '<span class="badge bg-success" style="font-size: 10px; padding: 2px 5px;">Approved</span>';
                    } else if (data === 'Disapproved') {
                        return '<span class="badge bg-danger" style="font-size: 10px; padding: 2px 5px;">Disapproved</span>';
                    }
                    return '<span class="text-secondary" style="font-size: 10.5px;">Not Approved</span>';
                }
            },
            { data: 'approvedBy', className: 'align-middle', defaultContent: '' },
            { data: 'approvedDate', className: 'text-center align-middle', defaultContent: '' },
            {
                data: null,
                orderable: false,
                className: 'text-center col-print align-middle',
                render: function (data, type, row) {
                    return '<a href="javascript:void(0);" class="text-decoration-underline" onclick="printVoucher(\'' + (row.voucherNo || '') + '\')">Print</a>';
                }
            }
        ],
        language: {
            search: "Search:",
            searchPlaceholder: "",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            emptyTable: "No vouchers found"
        }
    });

    // Tooltip on cells for long text & remove unnecessary empty thead elements
    $('#tblVoucherApproval').on('draw.dt', function () {
        $('#tblVoucherApproval tbody td').each(function () {
            var text = $(this).text().trim();
            if (text && text !== '-' && !$(this).attr('title')) {
                $(this).attr('title', text);
            }
        });
        $('#chkSelectAll').prop('checked', false);
    });

    // Select All Checkbox
    $('#chkSelectAll').on('change', function () {
        var isChecked = $(this).is(':checked');
        $('.row-select-chk').prop('checked', isChecked);
    });

    $(document).on('change', '.row-select-chk', function () {
        var total = $('.row-select-chk').length;
        var checked = $('.row-select-chk:checked').length;
        $('#chkSelectAll').prop('checked', total > 0 && total === checked);
    });

    // Auto Refresh on Date Change
    $(document).on('change input blur', '#filterFromDate, #filterToDate', function () {
        reloadGridAndSummary();
    });

    // Auto filter when dropdowns change (Customer, Status) or Select2 events
    $(document).on('change select2:select select2:unselect select2:clear', '#filterCustomerId, #filterStatus', function () {
        reloadGridAndSummary();
    });

    // Preview / Filter Button
    $('#btnPreview').on('click', function () {
        reloadGridAndSummary();
    });

    // Refresh Button Click (Left of Preview)
    $('#btnRefresh').on('click', function () {
        reloadGridAndSummary();
    });

    // Debounce timer for smooth typing / quick selections
    var filterTimer = null;
    function reloadGridAndSummary() {
        clearTimeout(filterTimer);
        filterTimer = setTimeout(function () {
            $('#chkSelectAll').prop('checked', false);
            table.ajax.reload(null, false);
            loadKpiSummary();
        }, 150);
    }

    // Approved Button Click
    $('#btnApproved').on('click', function () {
        executeStatusUpdate('Approved');
    });

    // Disapproved Button Click
    $('#btnDisapproved').on('click', function () {
        executeStatusUpdate('Disapproved');
    });

    function executeStatusUpdate(status) {
        var selected = [];
        $('.row-select-chk:checked').each(function () {
            var vNo = $(this).val();
            if (vNo) selected.push(vNo);
        });

        if (selected.length === 0) {
            if (typeof toastr !== 'undefined') {
                toastr.warning("Please select at least one voucher.");
            } else {
                alert("Please select at least one voucher.");
            }
            return;
        }

        $.ajax({
            url: '/VoucherApproval/UpdateStatus',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                VoucherNos: selected,
                Status: status
            }),
            success: function (res) {
                if (res.success) {
                    if (typeof toastr !== 'undefined') {
                        toastr.success(res.message);
                    } else {
                        alert(res.message);
                    }
                    reloadGridAndSummary();
                } else {
                    if (typeof toastr !== 'undefined') {
                        toastr.error(res.message || "Operation failed.");
                    } else {
                        alert(res.message || "Operation failed.");
                    }
                }
            },
            error: function () {
                if (typeof toastr !== 'undefined') {
                    toastr.error("An error occurred while updating status.");
                } else {
                    alert("An error occurred while updating status.");
                }
            }
        });
    }

    // Bind Print Button in the preview modal
    $(document).on('click', '#btnPrintVoucherDoc', function () {
        var vNo = $('#printVoucherNo').text().trim();
        if (vNo) {
            window.open('/VoucherEntry/Print?id=' + encodeURIComponent(vNo), '_blank');
        } else {
            printModalVoucher();
        }
    });
});

const VOUCHER_TYPE_NAMES = {
    '1': 'Bank Receipt Voucher',
    '2': 'Bank Payment Voucher',
    '3': 'Cash Receipt Voucher',
    '4': 'Cash Payment Voucher',
    '5': 'Journal Voucher',
    '6': 'Contra Voucher',
    'BRV': 'Bank Receipt Voucher',
    'BPV': 'Bank Payment Voucher',
    'CRV': 'Cash Receipt Voucher',
    'CPV': 'Cash Payment Voucher',
    'JV': 'Journal Voucher',
    'CV': 'Contra Voucher'
};

function formatVoucherCurrency(val) {
    if (val === null || val === undefined || isNaN(val)) return '0.00';
    return parseFloat(val).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    return $('<div>').text(text).html();
}

function printVoucher(voucherNo) {
    if (!voucherNo) return;

    $.ajax({
        url: '/VoucherEntry/GetPrint',
        type: 'GET',
        data: { id: voucherNo },
        beforeSend: function () {
            // Optional loader indicator
        },
        success: function (res) {
            if (res && res.success && res.data) {
                const v = res.data;
                $('#printOrgName').text(v.companyName || 'FORMOSA LOGISTICS LIMITED');
                $('#printCompanySub').text(v.companyAddress || (v.branchName ? `${v.branchName}` : '21, Hazaribagh, Dhaka-1209 Phone: Email:'));
                
                const kind = VOUCHER_TYPE_NAMES[v.voucherType_Code] || v.voucherTypeName || v.voucherType_Code || 'JOURNAL VOUCHER';
                $('#printVoucherKind').text(kind.toUpperCase());
                $('#printVoucherNo').text(v.voucherNo || voucherNo);
                
                let vDate = v.voucherDateFormatted || '';
                if (!vDate && v.voucherDate) {
                    try {
                        const d = new Date(v.voucherDate);
                        if (!isNaN(d.getTime())) {
                            const day = String(d.getDate()).padStart(2, '0');
                            const month = String(d.getMonth() + 1).padStart(2, '0');
                            const year = d.getFullYear();
                            vDate = `${day}/${month}/${year}`;
                        }
                    } catch (e) {
                        vDate = (v.voucherDate || '').substring(0, 10);
                    }
                }
                $('#printVoucherDate').text(vDate);

                let viewerName = (v.preparedBy || '').trim();
                if (!viewerName || viewerName === 'Guest User') {
                    if (window.CURRENT_USER_NAME) {
                        viewerName = window.CURRENT_USER_NAME;
                    } else {
                        const domName = ($('.dropdown-profile h6').text() || $('#navbarDropdownUser').text() || '').trim();
                        if (domName && domName !== 'Guest User') {
                            viewerName = domName;
                        }
                    }
                }
                $('#printPreparedByName').text(viewerName || '');

                const tbody = $('#printLinesBody');
                tbody.empty();

                let subText = (v.invoiceNo || v.InvoiceNo || v.reference || v.Reference || '').trim();
                if (!subText && v.narration) {
                    const match = v.narration.match(/\((?:Job No:\s*)?([A-Za-z0-9_\-\/]+)\)/i);
                    if (match && match[1]) {
                        subText = match[1].trim();
                    }
                }

                (v.details || []).forEach(function (d) {
                    let lineDesc = (d.description || d.Description || '').trim();
                    const narr = (v.narration || '').trim();
                    if (lineDesc && narr && (lineDesc.toLowerCase() === narr.toLowerCase() || narr.toLowerCase().startsWith(lineDesc.toLowerCase()))) {
                        lineDesc = '';
                    }

                    const mainAccName = (d.accName || d.AccName || d.accCode || '').trim();

                    let subLine = '';
                    if (subText) {
                        if (lineDesc && lineDesc.toLowerCase() !== mainAccName.toLowerCase() && lineDesc.toLowerCase() !== subText.toLowerCase()) {
                            subLine = `${subText} (${lineDesc})`;
                        } else {
                            subLine = subText;
                        }
                    } else if (lineDesc && lineDesc.toLowerCase() !== mainAccName.toLowerCase()) {
                        subLine = lineDesc;
                    }

                    let particularsHtml = `<div style="color: #000;">${escapeHtml(mainAccName)}</div>`;
                    if (subLine) {
                        particularsHtml += `<div style="font-size: 11.5px; color: #000; margin-top: 2px;">${escapeHtml(subLine)}</div>`;
                    }

                    const drVal = (d.debitAmount !== null && d.debitAmount !== undefined && d.debitAmount > 0) ? formatVoucherCurrency(d.debitAmount) : '0.00';
                    const crVal = (d.creditAmount !== null && d.creditAmount !== undefined && d.creditAmount > 0) ? formatVoucherCurrency(d.creditAmount) : '0.00';

                    tbody.append(`
                        <tr>
                            <td style="border: 1px solid #888 !important; border-left: 1.5px solid #888 !important; border-right: 1px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: center; vertical-align: middle; color: #000; background: transparent !important; white-space: nowrap !important;">${escapeHtml(d.accCode)}</td>
                            <td style="border: 1px solid #888 !important; border-left: 1px solid #888 !important; border-right: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: left; vertical-align: middle; color: #000; background: transparent !important; line-height: 1.35;">${particularsHtml}</td>
                            <td style="border: 1px solid #888 !important; border-left: 1.5px solid #888 !important; border-right: 1px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: right; vertical-align: middle; color: #000; background: transparent !important; white-space: nowrap;">${drVal}</td>
                            <td style="border: 1px solid #888 !important; border-left: 1px solid #888 !important; border-right: 1.5px solid #888 !important; border-top: 1px solid #888 !important; border-bottom: 1px solid #888 !important; padding: 6px 8px; text-align: right; vertical-align: middle; color: #000; background: transparent !important; white-space: nowrap;">${crVal}</td>
                        </tr>
                    `);
                });

                $('#printTotalDebit').text(formatVoucherCurrency(v.totalDebit));
                $('#printTotalCredit').text(formatVoucherCurrency(v.totalCredit));
                $('#printAmountInWords').text(v.amountInWords || '');
                $('#printNarration').text(v.narration || '');

                const modalEl = document.getElementById('voucherPrintModal');
                if (modalEl) {
                    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                    modal.show();
                }
            } else {
                if (typeof toastr !== 'undefined') {
                    toastr.error(res ? res.message : 'Error fetching voucher details.');
                } else {
                    alert(res ? res.message : 'Error fetching voucher details.');
                }
            }
        },
        error: function () {
            if (typeof toastr !== 'undefined') {
                toastr.error('Failed to load voucher details.');
            } else {
                alert('Failed to load voucher details.');
            }
        }
    });
}

function printModalVoucher() {
    const paper = document.getElementById('voucherPaperContent');
    if (!paper) return;

    let iframe = document.getElementById('voucherPrintIframe');
    if (!iframe) {
        iframe = document.createElement('iframe');
        iframe.id = 'voucherPrintIframe';
        iframe.style.position = 'fixed';
        iframe.style.left = '-9999px';
        iframe.style.top = '0';
        iframe.style.width = '1024px';
        iframe.style.height = '768px';
        iframe.style.border = '0';
        iframe.style.opacity = '0';
        iframe.style.pointerEvents = 'none';
        document.body.appendChild(iframe);
    }

    const doc = iframe.contentWindow.document;
    doc.open();
    doc.write(`<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title></title>
    <style>
        * {
            box-sizing: border-box !important;
            margin: 0;
            padding: 0;
        }
        @page {
            size: A4 portrait;
            margin: 0 !important;
        }
        html, body {
            background: #ffffff !important;
            color: #000000 !important;
            font-family: 'Times New Roman', Times, serif, Arial;
            margin: 0 !important;
            padding: 0 !important;
            width: 210mm !important;
            height: auto !important;
            min-height: auto !important;
            box-sizing: border-box !important;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        .print-sheet {
            position: relative !important;
            width: 210mm !important;
            max-width: 210mm !important;
            min-height: auto !important;
            height: auto !important;
            margin: 0 auto !important;
            padding: 16mm 20mm 20mm 20mm !important;
            box-sizing: border-box !important;
            background: #ffffff !important;
        }
        .voucher-content-top {
            width: 100% !important;
            box-sizing: border-box !important;
        }
        .voucher-bottom-block {
            position: static !important;
            width: 100% !important;
            margin-top: 130px !important;
            padding: 0 !important;
            box-sizing: border-box !important;
            page-break-inside: avoid !important;
            break-inside: avoid !important;
        }
        .voucher-signatures-bottom {
            width: 100% !important;
        }
        .voucher-meta-table,
        .voucher-meta-table tr,
        .voucher-meta-table td {
            border: none !important;
            border-style: none !important;
            outline: none !important;
            box-shadow: none !important;
            background: transparent !important;
        }
        #printLinesTable {
            border-collapse: collapse !important;
            table-layout: fixed !important;
            width: 100% !important;
            border: 1.5px solid #888 !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
            box-sizing: border-box !important;
        }
        #printLinesTable th,
        #printLinesTable td {
            border: 1px solid #888 !important;
            background: transparent !important;
            background-color: transparent !important;
            -webkit-print-color-adjust: exact !important;
            print-color-adjust: exact !important;
        }
        #printLinesTable thead th {
            border-bottom: 1.5px solid #888 !important;
        }
        #printLinesTable tbody tr:first-child td {
            border-top: 1.5px solid #888 !important;
        }
        #printLinesTable th:first-child,
        #printLinesTable tbody td:first-child {
            width: 22% !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1px solid #888 !important;
            white-space: nowrap !important;
        }
        #printLinesTable th:nth-child(2),
        #printLinesTable tbody td:nth-child(2) {
            width: 48% !important;
            border-left: 1px solid #888 !important;
            border-right: 1.5px solid #888 !important;
        }
        #printLinesTable th:nth-child(3),
        #printLinesTable tbody td:nth-child(3) {
            width: 15% !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1px solid #888 !important;
            white-space: nowrap !important;
        }
        #printLinesTable th:last-child,
        #printLinesTable tbody td:last-child {
            width: 15% !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
            white-space: nowrap !important;
        }
        #printLinesTable tfoot td {
            border-top: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:first-child {
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:nth-child(2) {
            border-left: 1.5px solid #888 !important;
        }
        #printLinesTable tfoot td:last-child {
            border-right: 1.5px solid #888 !important;
        }
        .voucher-attached-box,
        div[style*="border"] {
            border: 1.5px solid #888 !important;
            border-top: none !important;
            border-left: 1.5px solid #888 !important;
            border-right: 1.5px solid #888 !important;
            border-bottom: 1.5px solid #888 !important;
        }
    </style>
</head>
<body>
    <div class="print-sheet">
        \${paper.innerHTML}
    </div>
</body>
</html>`);
    doc.close();

    setTimeout(function () {
        try {
            iframe.contentWindow.focus();
            iframe.contentWindow.print();
        } catch (e) {
            console.error('Print iframe error:', e);
            window.print();
        }
    }, 350);
}

function viewVoucher(voucherNo) {
    if (voucherNo) {
        window.open('/VoucherEntry/Index?voucherNo=' + encodeURIComponent(voucherNo), '_blank');
    }
}
