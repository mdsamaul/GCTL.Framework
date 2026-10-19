/**
 * Trial Balance Page Script
 * Follows GPAnalysisReport architecture:
 * Dynamic server-side DataTable loading via $.gctlGrid.initFromModel,
 * Real-time filter synchronization, KPI card updates, and PDF/Excel export.
 */
(function ($) {
    'use strict';

    // Global Renderers for Currency
    window.gctlRenderers = window.gctlRenderers || {};
    window.gctlRenderers.formatCurrency = function (data, type) {
        if (data === null || data === undefined || data === '' || isNaN(data)) {
            return (type === 'sort' || type === 'type') ? 0 : '-';
        }
        var num = parseFloat(data);
        if (type === 'sort' || type === 'type') {
            return num;
        }
        if (num === 0) return '-';
        return '৳' + num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    function formatNumberOnly(val) {
        if (val === null || val === undefined || isNaN(val)) return '0.00';
        return parseFloat(val).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    let gridTable = null;
    let filterDebounceTimer = null;
    let isInitialized = false;

    $(document).ready(function () {
        if ($.fn.dataTable && $.fn.dataTable.ext) {
            $.fn.dataTable.ext.errMode = 'none';
        }

        initDatePickers();
        initGrid();
        bindEvents();

        setTimeout(function () {
            isInitialized = true;
        }, 500);
    });

    function initDatePickers() {
        if (typeof flatpickr !== 'undefined') {
            flatpickr('.tb-datepicker', {
                dateFormat: 'd/m/Y',
                allowInput: true,
                onClose: function (selectedDates, dateStr, instance) {
                    if (instance.element.id === 'tbFilterToDate' && dateStr) {
                        updateAsOnDate(dateStr);
                    }
                    onFilterChanged();
                }
            });
        }
    }

    function updateAsOnDate(dateStr) {
        if (!dateStr) return;
        $('#tbAsOnDate').text(dateStr);
    }

    function getFilterParams() {
        return {
            companyCode: $('#tbFilterCompany').val() || '',
            branchCode: $('#tbFilterBranch').val() || '',
            fromDate: $('#tbFilterFromDate').val() || '',
            toDate: $('#tbFilterToDate').val() || ''
        };
    }

    function updateKpiCards(summary) {
        if (!summary) return;
        $('#kpiTotalDebit').text(window.gctlRenderers.formatCurrency(summary.totalDebit));
        $('#kpiTotalCredit').text(window.gctlRenderers.formatCurrency(summary.totalCredit));
        $('#kpiDifference').text(window.gctlRenderers.formatCurrency(summary.difference));
        $('#kpiAccountsCount').text(summary.accountsWithBalance != null ? summary.accountsWithBalance.toLocaleString() : '0');
    }

    function initGrid() {
        // Ensure tfoot exists for total summary row
        var $table = $('#trialBalanceTable');
        if ($table.find('tfoot').length === 0) {
            $table.append('<tfoot><tr>' +
                '<td colspan="3" class="text-center fw-bold">Total</td>' +
                '<td class="text-end fw-bold" id="tbFooterTotalDebit">৳0.00</td>' +
                '<td class="text-end fw-bold" id="tbFooterTotalCredit">৳0.00</td>' +
                '</tr></tfoot>');
        }

        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            gridTable = $.gctlGrid.initFromModel({
                gridId: 'trialBalanceTable',
                extraOptions: {
                    columnDefs: [
                        { defaultContent: '-', targets: '_all' }
                    ]
                },
                ajaxData: function (d) {
                    var p = getFilterParams();
                    d.companyCode = p.companyCode;
                    d.branchCode = p.branchCode;
                    d.fromDate = p.fromDate;
                    d.toDate = p.toDate;
                },
                onLoaded: function (json) {
                    isInitialized = true;
                    if (json && json.summary) {
                        updateKpiCards(json.summary);
                        $('#tbFooterTotalDebit').text(window.gctlRenderers.formatCurrency(json.summary.totalDebit));
                        $('#tbFooterTotalCredit').text(window.gctlRenderers.formatCurrency(json.summary.totalCredit));
                    }
                }
            });

            $('#trialBalanceTable').on('error.dt', function (e, settings, techNote, message) {
                console.error('DataTables error:', techNote, message);
                $('#trialBalanceTable tbody').html('<tr><td colspan="5" class="text-center text-muted py-3">No matching records found.</td></tr>');
            });
        }
    }

    function reloadGrid() {
        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('trialBalanceTable', true);
        } else if (gridTable && gridTable.ajax) {
            gridTable.ajax.reload(null, true);
        }
    }

    function onFilterChanged() {
        if (!isInitialized) return;
        clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(function () {
            reloadGrid();
        }, 300);
    }

    function bindEvents() {
        $('#tbFilterCompany, #tbFilterBranch').on('change', function () {
            onFilterChanged();
        });

        $('#tbFilterToDate').on('change', function () {
            updateAsOnDate($(this).val());
            onFilterChanged();
        });

        $('#tbFilterFromDate').on('change', function () {
            onFilterChanged();
        });

        // 1. Print / Preview
        $('#tbBtnPrint').on('click', function () {
            window.print();
        });

        // 2. PDF Export
        $('#tbBtnPdf').on('click', function () {
            exportToPdf();
        });

        // 3. Excel Export
        $('#tbBtnExcel').on('click', function () {
            exportToExcel();
        });
    }

    function exportToExcel() {
        var p = getFilterParams();
        var url = '/TrialBalance/ExportExcel?companyCode=' + encodeURIComponent(p.companyCode) +
            '&branchCode=' + encodeURIComponent(p.branchCode) +
            '&fromDate=' + encodeURIComponent(p.fromDate) +
            '&toDate=' + encodeURIComponent(p.toDate);

        var a = document.createElement('a');
        a.href = url;
        a.download = 'TrialBalance_' + new Date().toISOString().slice(0, 10) + '.xlsx';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        if (typeof toastr !== 'undefined') {
            toastr.success('Downloading Excel report...');
        }
    }

    function exportToPdf() {
        if (typeof toastr !== 'undefined') {
            toastr.info('Generating PDF report...');
        }

        var company = $('#tbFilterCompany option:selected').text() || 'Company';
        var branch = $('#tbFilterBranch option:selected').text() || 'All';
        var toDate = $('#tbAsOnDate').text() || '';
        var fromDate = $('#tbFilterFromDate').val() || '';

        var rowsHtml = '';
        var totalDebit = 0;
        var totalCredit = 0;

        if (gridTable) {
            var data = gridTable.rows({ search: 'applied' }).data().toArray();
            data.forEach(function (row) {
                var debitStr = row.debit > 0 ? ('৳' + formatNumberOnly(row.debit)) : '-';
                var creditStr = row.credit > 0 ? ('৳' + formatNumberOnly(row.credit)) : '-';
                if (row.debit) totalDebit += parseFloat(row.debit);
                if (row.credit) totalCredit += parseFloat(row.credit);

                rowsHtml += '<tr>' +
                    '<td style="padding: 5px 8px; border-bottom: 1px solid #e2e8f0; font-size: 11px;">' + (row.code || '') + '</td>' +
                    '<td style="padding: 5px 8px; border-bottom: 1px solid #e2e8f0; font-size: 11px;">' + (row.accountHead || '') + '</td>' +
                    '<td style="padding: 5px 8px; border-bottom: 1px solid #e2e8f0; font-size: 11px;">' + (row.group || '') + '</td>' +
                    '<td style="padding: 5px 8px; border-bottom: 1px solid #e2e8f0; font-size: 11px; text-align: right;">' + debitStr + '</td>' +
                    '<td style="padding: 5px 8px; border-bottom: 1px solid #e2e8f0; font-size: 11px; text-align: right;">' + creditStr + '</td>' +
                    '</tr>';
            });
        }

        var pdfHtml = '<div style="font-family: Arial, sans-serif; padding: 20px; color: #1e293b;">' +
            '<div style="text-align: center; margin-bottom: 16px;">' +
            '<h2 style="margin: 0; font-size: 18px; color: #003366;">' + company + '</h2>' +
            '<h3 style="margin: 4px 0; font-size: 14px; color: #0056b3;">Trial Balance as on ' + toDate + '</h3>' +
            '<p style="margin: 0; font-size: 11px; color: #64748b;">Branch: ' + branch + ' | Period: ' + fromDate + ' to ' + toDate + '</p>' +
            '</div>' +
            '<table style="width: 100%; border-collapse: collapse; margin-top: 10px;">' +
            '<thead>' +
            '<tr style="background-color: #0056b3; color: #ffffff;">' +
            '<th style="padding: 7px 8px; font-size: 11px; text-align: left;">CODE</th>' +
            '<th style="padding: 7px 8px; font-size: 11px; text-align: left;">ACCOUNT HEAD</th>' +
            '<th style="padding: 7px 8px; font-size: 11px; text-align: left;">GROUP</th>' +
            '<th style="padding: 7px 8px; font-size: 11px; text-align: right;">DEBIT</th>' +
            '<th style="padding: 7px 8px; font-size: 11px; text-align: right;">CREDIT</th>' +
            '</tr>' +
            '</thead>' +
            '<tbody>' + rowsHtml + '</tbody>' +
            '<tfoot>' +
            '<tr style="font-weight: bold; border-top: 2px solid #000; border-bottom: 2px solid #000;">' +
            '<td colspan="3" style="padding: 8px; text-align: center; font-size: 12px;">Total</td>' +
            '<td style="padding: 8px; text-align: right; font-size: 12px;">৳' + formatNumberOnly(totalDebit) + '</td>' +
            '<td style="padding: 8px; text-align: right; font-size: 12px;">৳' + formatNumberOnly(totalCredit) + '</td>' +
            '</tr>' +
            '</tfoot>' +
            '</table>' +
            '</div>';

        if (typeof html2pdf !== 'undefined') {
            var element = document.createElement('div');
            element.innerHTML = pdfHtml;
            var opt = {
                margin: 10,
                filename: 'TrialBalance_' + new Date().toISOString().slice(0, 10) + '.pdf',
                image: { type: 'jpeg', quality: 0.98 },
                html2canvas: { scale: 2 },
                jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
            };
            html2pdf().set(opt).from(element).save();
        } else {
            var printWin = window.open('', '_blank');
            printWin.document.write('<html><head><title>Trial Balance</title></head><body>' + pdfHtml + '</body></html>');
            printWin.document.close();
            printWin.focus();
            setTimeout(function () {
                printWin.print();
            }, 300);
        }
    }

})(jQuery);
