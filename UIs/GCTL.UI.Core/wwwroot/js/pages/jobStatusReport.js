/**
 * Job Status Report / Dashboard Page Script
 * Formosa Logistics Limited
 */
(function ($) {
    'use strict';

    let allData = [];
    let displayedData = [];
    let currentCardFilter = 'all';
    let currentPage = 1;
    const pageSize = 10;

    $(document).ready(function () {
        initDefaultDates();
        loadJobStatusData();
        bindEvents();
    });

    function initDefaultDates() {
        // By default, set the From date to the 1st of current month and To date to today
        const now = new Date();
        const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);

        const formatDate = (d) => {
            const month = '' + (d.getMonth() + 1);
            const day = '' + d.getDate();
            const year = d.getFullYear();
            return [year, month.padStart(2, '0'), day.padStart(2, '0')].join('-');
        };

        // If not already set, provide initial date range
        if (!$('#filterFromDate').val()) {
            $('#filterFromDate').val(formatDate(firstDay));
        }
        if (!$('#filterToDate').val()) {
            $('#filterToDate').val(formatDate(now));
        }
    }

    function bindEvents() {
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            currentPage = 1;
            loadJobStatusData();
        });

        // Search on Enter key in date inputs
        $('#filterFromDate, #filterToDate').on('keypress', function (e) {
            if (e.which === 13) {
                $('#btnSearch').trigger('click');
            }
        });

        // Export button click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });

        // Interactive stat card click filtering
        $('.stat-card').on('click', function () {
            const filter = $(this).data('status-filter') || 'all';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');
            currentCardFilter = filter;
            applyCardFilter();
        });
    }

    function applyCardFilter() {
        if (currentCardFilter === 'all') {
            displayedData = allData;
        } else {
            displayedData = allData.filter(function (item) {
                return (item.statusClass || '').toLowerCase() === currentCardFilter.toLowerCase();
            });
        }
        currentPage = 1;
        renderTablePage();
    }

    function getFilterParams() {
        return {
            FromDate: $('#filterFromDate').val() || '',
            ToDate: $('#filterToDate').val() || '',
            CustomerId: $('#filterCustomerId').val() || '',
            ShipmentMode: $('#filterShipmentMode').val() || '',
            PortType: $('#filterPortType').val() || '',
            JobStatus: $('#filterJobStatus').val() || '',
            ReportOption: 'Job Report'
        };
    }

    function loadJobStatusData() {
        const filters = getFilterParams();

        $('#loadingRow').show();
        $('#emptyRow').hide();
        $('.job-data-row').remove();

        $.ajax({
            url: '/JobStatusReport/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                $('#loadingRow').hide();

                if (res && res.success) {
                    animateCounter('#statTotalJobs', res.total || 0);
                    animateCounter('#statPendingJobs', res.pending || 0);
                    animateCounter('#statTransitJobs', res.transit || 0);
                    animateCounter('#statDeliveredJobs', res.delivered || 0);

                    allData = res.data || [];
                    currentCardFilter = 'all';
                    $('.stat-card').removeClass('active-card');
                    $('.stat-card[data-status-filter="all"]').addClass('active-card');
                    applyCardFilter();
                } else {
                    allData = [];
                    displayedData = [];
                    renderTablePage();
                    showError(res ? res.message : 'Failed to retrieve data.');
                }
            },
            error: function (xhr, status, err) {
                $('#loadingRow').hide();
                allData = [];
                displayedData = [];
                renderTablePage();
                showError('Error connecting to server. Please try again.');
            }
        });
    }

    function renderTablePage() {
        $('.job-data-row').remove();

        const totalRecords = displayedData.length;
        if (totalRecords === 0) {
            $('#emptyRow').show();
            $('#tableInfo').text('Showing 0 to 0 of 0 entries');
            $('#paginationBtns').empty();
            return;
        }

        $('#emptyRow').hide();

        const totalPages = Math.ceil(totalRecords / pageSize) || 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        const startIndex = (currentPage - 1) * pageSize;
        const endIndex = Math.min(startIndex + pageSize, totalRecords);
        const pageItems = displayedData.slice(startIndex, endIndex);

        const rowsHtml = pageItems.map(function (item, index) {
            const sl = startIndex + index + 1;
            const badgeClass = item.statusClass || 'pending';
            const badgeText = item.status || 'Pending';

            return `
                <tr class="job-data-row">
                    <td>${sl}</td>
                    <td>
                        <span class="job-no-link">${escapeHtml(item.jobNo)}</span>
                    </td>
                    <td>${escapeHtml(item.serviceType)}</td>
                    <td>${escapeHtml(item.portType)}</td>
                    <td>${escapeHtml(item.dateReceived)}</td>
                    <td>${escapeHtml(item.vesselName)}</td>
                    <td>${escapeHtml(item.weight)}</td>
                    <td>${escapeHtml(item.container)}</td>
                    <td>
                        <span class="status-badge ${badgeClass}">${escapeHtml(badgeText)}</span>
                    </td>
                </tr>
            `;
        }).join('');

        $('#jobStatusTableBody').append(rowsHtml);

        // Update Info
        $('#tableInfo').text(`Showing ${startIndex + 1} to ${endIndex} of ${totalRecords.toLocaleString()} entries`);

        // Render Pagination
        renderPagination(totalPages);
    }

    function renderPagination(totalPages) {
        const $btns = $('#paginationBtns').empty();
        if (totalPages <= 1) return;

        // Prev button
        const $prev = $('<button type="button" class="page-btn"><i class="fas fa-chevron-left"></i></button>')
            .prop('disabled', currentPage === 1)
            .on('click', function () {
                if (currentPage > 1) {
                    currentPage--;
                    renderTablePage();
                }
            });
        $btns.append($prev);

        // Page numbers
        let startPage = Math.max(1, currentPage - 2);
        let endPage = Math.min(totalPages, startPage + 4);
        if (endPage - startPage < 4) {
            startPage = Math.max(1, endPage - 4);
        }

        if (startPage > 1) {
            $btns.append(createPageBtn(1));
            if (startPage > 2) {
                $btns.append('<span class="px-1 text-muted">...</span>');
            }
        }

        for (let p = startPage; p <= endPage; p++) {
            $btns.append(createPageBtn(p));
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                $btns.append('<span class="px-1 text-muted">...</span>');
            }
            $btns.append(createPageBtn(totalPages));
        }

        // Next button
        const $next = $('<button type="button" class="page-btn"><i class="fas fa-chevron-right"></i></button>')
            .prop('disabled', currentPage === totalPages)
            .on('click', function () {
                if (currentPage < totalPages) {
                    currentPage++;
                    renderTablePage();
                }
            });
        $btns.append($next);
    }

    function createPageBtn(page) {
        const $btn = $(`<button type="button" class="page-btn ${page === currentPage ? 'active' : ''}">${page}</button>`);
        $btn.on('click', function () {
            currentPage = page;
            renderTablePage();
        });
        return $btn;
    }

    function animateCounter(selector, target) {
        const $el = $(selector);
        const current = parseInt($el.text().replace(/,/g, ''), 10) || 0;
        const duration = 600;
        const steps = 25;
        const stepTime = duration / steps;
        const diff = target - current;

        let step = 0;
        const timer = setInterval(function () {
            step++;
            const val = Math.round(current + (diff * (step / steps)));
            $el.text(val.toLocaleString());
            if (step >= steps) {
                clearInterval(timer);
                $el.text(target.toLocaleString());
            }
        }, stepTime);
    }

    function exportReport() {
        const filters = getFilterParams();
        const exportFormat = $('#filterExportFormat').val() || 'PDF';

        const queryParams = $.param({
            FromDate: filters.FromDate,
            ToDate: filters.ToDate,
            CustomerId: filters.CustomerId,
            ShipmentMode: filters.ShipmentMode,
            PortType: filters.PortType,
            JobStatus: filters.JobStatus,
            ReportOption: filters.ReportOption,
            exportFormat: exportFormat
        });

        // Trigger native browser file download
        window.location.href = '/JobStatusReport/Export?' + queryParams;
    }

    function escapeHtml(str) {
        if (!str) return '';
        return $('<div>').text(str).html();
    }

    function showError(msg) {
        if (window.toastr) {
            toastr.error(msg);
        } else {
            console.error(msg);
        }
    }

})(jQuery);
