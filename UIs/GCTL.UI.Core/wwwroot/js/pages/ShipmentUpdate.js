$(document).ready(function () {


    //shipment update page js
    showDev('loaded')

    //#region Select 2

    const initializeSelect = () => {
        $(".create-modify-dateshow").addClass('d-none').removeClass('d-flex');
    };

    initializeSelect();

    //#endregion

    //#region Populate TopTable

    let currentPage = 1;
    let pageSize = 5;
    let filteredRecords = 0;


    $('#pageSizeSelect').val(pageSize);
    loadJobs();

    // Reload on filter change (large screen filters)

    let searchTimer;

    $(document).on('keyup', '#searchInput', function () {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {
            currentPage = 1;
            loadJobs();
        }, 300);
    });

    $(document).on(
        'change',
        '#customerFilter, #shipmentModeFilter, #dateFrom, #dateTo, #pageSizeSelect',
        function () {
            currentPage = 1;
            pageSize = parseInt($('#pageSizeSelect').val());

            syncFiltersFromLarge();
            loadJobs();
        }
    );
    // Reload on filter change (small screen filters) - sync to large then reload
    $(document).on(
        'change', '#customerFilterSm, #shipmentModeFilterSm, #dateFromSm, #dateToSm',
        function () {
            currentPage = 1;
            pageSize = parseInt($('#pageSizeSelect').val());

            // sync large screen filters with small screen values
            syncFiltersFromSmall();

            loadJobs();
        });

    function syncFiltersFromLarge() {
        $('#customerFilterSm').val($('#customerFilter').val()).trigger('change.select2');
        $('#shipmentModeFilterSm').val($('#shipmentModeFilter').val()).trigger('change.select2');
        $('#dateFromSm').val($('#dateFrom').val());
        $('#dateToSm').val($('#dateTo').val());
    }

    function syncFiltersFromSmall() {
        $('#customerFilter').val($('#customerFilterSm').val()).trigger('change.select2');
        $('#shipmentModeFilter').val($('#shipmentModeFilterSm').val()).trigger('change.select2');
        $('#dateFrom').val($('#dateFromSm').val());
        $('#dateTo').val($('#dateToSm').val());
    }

    // Pagination clicks
    $(document).on('click', '#paginationPrev', function () {
        if (currentPage > 1) {
            currentPage--;
            loadJobs();
        }
    });

    $(document).on('click', '#paginationNext', function () {
        if (currentPage < Math.ceil(filteredRecords / pageSize)) {
            currentPage++;
            loadJobs();
        }
    });

    $(document).on('click', '#paginationNumbers .page-link', function (e) {
        e.preventDefault();
        currentPage = parseInt($(this).data('page'));
        loadJobs();
    });

    function loadJobs() {        
        // Prefer large-screen filter values; fall back to small-screen if large is empty
        const customerVal = $('#customerFilter').val() || $('#customerFilterSm').val();
        const shipmentVal = $('#shipmentModeFilter').val() || $('#shipmentModeFilterSm').val();
        const dateFromVal = $('#dateFrom').val() || $('#dateFromSm').val();
        const dateToVal = $('#dateTo').val() || $('#dateToSm').val();

        const filters = {
            page: currentPage,
            pageSize: pageSize,
            customerId: customerVal,
            shipmentMode: shipmentVal,
            dateFrom: dateFromVal,
            dateTo: dateToVal,
            search: $('#searchInput').val()
        };

        $.ajax({
            url: '/SepDocUpdate/GetJobs',
            type: 'GET',
            data: filters,
            success: function (response) {

                renderTable(response.data);
                filteredRecords = response.filteredRecords;

                $('#dataListInfo').text(
                    `Showing ${(currentPage - 1) * pageSize + 1} to ${Math.min(currentPage * pageSize, filteredRecords)} of ${filteredRecords} entries`
                );

                renderPagination(Math.ceil(filteredRecords / pageSize));
                populateDropdowns(response.data);
            },
            error: function () {
                alert('Error loading data');
            }
        });
    }

    function renderTable(data) {
        let rows = '';
        if (data.length === 0) {
            rows = '<tr><td colspan="6" class="text-center">No records found</td></tr>';
        } else {
            data.forEach(job => {
                rows += `<tr>
                            <td class="ps-2 text-left">
                                <a href="#" class="text-primary fw-bold  job-detail-link" data-id="${job.id}">${job.jobNo}</a>
                            </td>
                            
                            <td class="ps-2 text-left">${job.shipmentMode}</td>
                            <td class="text-center">${job.jobDateFormatted}</td>
                            <td class="ps-2 text-left">${job.customerName}</td>
                            <td class="text-center">${job.docReceivedDateFormatted || '-'}</td>
                           
                        </tr>`;
            });
        }
        $('#job-settings-table').html(rows);
    }
    $(document).on('click', '#paginationFirst', function () {
        if (currentPage !== 1) {
            currentPage = 1;
            loadJobs();
        }
    });

    $(document).on('click', '#paginationLast', function () {
        const totalPages = Math.ceil(filteredRecords / pageSize);
        if (currentPage !== totalPages) {
            currentPage = totalPages;
            loadJobs();
        }
    });
    function renderPagination(totalPages) {
        let pagesHtml = '';

        if (totalPages <= 7) {
            for (let i = 1; i <= totalPages; i++) {
                pagesHtml += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">${i}</a>
            </li>`;
            }
        } else {
            pagesHtml += `<li class="page-item ${currentPage === 1 ? 'active' : ''}">
            <a class="page-link" href="#" data-page="1">1</a>
        </li>`;

            if (currentPage > 4) {
                pagesHtml += `<li class="page-item disabled"><a class="page-link">...</a></li>`;
            }

            let start = Math.max(2, currentPage - 1);
            let end = Math.min(totalPages - 1, currentPage + 1);

            for (let i = start; i <= end; i++) {
                pagesHtml += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">${i}</a>
            </li>`;
            }

            if (currentPage < totalPages - 3) {
                pagesHtml += `<li class="page-item disabled"><a class="page-link">...</a></li>`;
            }

            pagesHtml += `<li class="page-item ${currentPage === totalPages ? 'active' : ''}">
            <a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a>
        </li>`;
        }

        $('#paginationNumbers').html(pagesHtml || '<li class="page-item disabled"><a class="page-link">1</a></li>');

        // $('#paginationPrev').prop('disabled', currentPage === 1);
        // $('#paginationNext').prop('disabled', currentPage >= totalPages || totalPages === 0);
        $('#paginationFirst').prop('disabled', currentPage === 1);
        $('#paginationPrev').prop('disabled', currentPage === 1);
        $('#paginationNext').prop('disabled', currentPage >= totalPages || totalPages === 0);
        $('#paginationLast').prop('disabled', currentPage >= totalPages || totalPages === 0);
    }

    function populateDropdowns(data) {
        const customers = [...new Set(data.map(x => x.customerName))].sort();

        // Large screen dropdown
        const $cust = $('#customerFilter');
        if ($cust.children('option').length === 1) {
            customers.forEach(c => $cust.append(`<option value="${c}">${c}</option>`));
        }

        // Small screen dropdown
        const $custSm = $('#customerFilterSm');
        if ($custSm.children('option').length === 1) {
            customers.forEach(c => $custSm.append(`<option value="${c}">${c}</option>`));
        }

        const modes = [...new Set(data.map(x => x.shipmentMode))].sort();

        // Large screen dropdown
        const $mode = $('#shipmentModeFilter');
        if ($mode.children('option').length === 1) {
            modes.forEach(m => $mode.append(`<option value="${m}">${m}</option>`));
        }

        // Small screen dropdown
        const $modeSm = $('#shipmentModeFilterSm');
        if ($modeSm.children('option').length === 1) {
            modes.forEach(m => $modeSm.append(`<option value="${m}">${m}</option>`));
        }
    }

    //#endregion

    //#region On click populate Form

    $(document).on('click', '.job-detail-link, .btn-view-job', function (e) {
        e.preventDefault();
        const jobId = $(this).data('id');
        
        $.ajax({
            url: '/SepDocUpdate/GetJobDetail',
            type: 'GET',
            data: { id: jobId },
            success: function (data) {
                clearForm();
                populateForm(data);
                loadJobTimeline(data.jobNo);
            },
            error: function () {
                alert('Error loading job details');
            }
        });
    });

    //#endregion

    //#region nicher table
    let statusCurrentPage = 1;
    let statusPageSize = 10;
    let statusFilteredRecords = 0;



    $('#statusPageSizeSelect').val(statusPageSize);


    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    console.log(today, firstDay);
    const formatDate = (date) => {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    };

    $('#dateLoadGridFrom').val(formatDate(firstDay));
    $('#dateLoadGridTo').val(formatDate(today));
    
    setDate('#dateLoadGridFrom', formatDate(firstDay));
    setDate('#dateLoadGridTo',formatDate(today));

    loadJobStatuses();

    $('#statusSearchInput, #statusPageSizeSelect').on('keyup change', function () {
        statusCurrentPage = 1;
        statusPageSize = parseInt($('#statusPageSizeSelect').val());
        loadJobStatuses();
    });

    $(document).on('click', '#statusPaginationPrev', function () {
        if (statusCurrentPage > 1) {
            statusCurrentPage--;
            loadJobStatuses();
        }
    });

    $(document).on('click', '#statusPaginationNext', function () {
        if (statusCurrentPage < Math.ceil(statusFilteredRecords / statusPageSize)) {
            statusCurrentPage++;
            loadJobStatuses();
        }
    });

    $(document).on('click', '#statusPaginationNumbers .page-link', function (e) {
        e.preventDefault();
        statusCurrentPage = parseInt($(this).data('page'));
        loadJobStatuses();
    });

    // function loadJobStatuses() {
    //     const params = {
    //         page: statusCurrentPage,
    //         pageSize: statusPageSize,
    //         search: $('#statusSearchInput').val()
    //     };

    //     $.ajax({
    //         url: '/SepDocUpdate/GetJobStatuses',
    //         type: 'GET',
    //         data: params,
    //         success: function (response) {

    //             renderStatusTable(response.data);
    //             statusFilteredRecords = response.filteredRecords;

    //             const from = (statusCurrentPage - 1) * statusPageSize + 1;
    //             const to = Math.min(statusCurrentPage * statusPageSize, statusFilteredRecords);
    //             $('#statusDataListInfo').text(`Showing ${from} to ${to} of ${statusFilteredRecords} entries`);

    //             renderStatusPagination(Math.ceil(statusFilteredRecords / statusPageSize));
    //         },
    //         error: function () {
    //             alert('Error loading job status history');
    //         }
    //     });
    // }
    $(document).on(
        'change keyup',
        '#customerLoadGridFilter,#dateLoadGridFrom,#dateLoadGridTo,#statusPageSizeSelect,#statusSearchInput',
        function () {

            statusCurrentPage = 1;

            statusPageSize = parseInt($('#statusPageSizeSelect').val());

            loadJobStatuses();

        });
    function loadJobStatuses() {

        $.ajax({

            url: '/SepDocUpdate/GetJobStatuses',

            type: 'GET',

            data: {

                page: statusCurrentPage,

                pageSize: statusPageSize,

                search: $('#statusSearchInput').val(),

                customerCode: $('#customerLoadGridFilter').val(),

                fromDate: $('#dateLoadGridFrom').val(),

                toDate: $('#dateLoadGridTo').val()

            },

            success: function (response) {

                renderStatusTable(response.data);

                statusFilteredRecords = response.filteredRecords;

                const from = (statusCurrentPage - 1) * statusPageSize + 1;

                const to = Math.min(statusCurrentPage * statusPageSize, statusFilteredRecords);

                $('#statusDataListInfo')
                    .text(`Showing ${from} to ${to} of ${statusFilteredRecords} entries`);

                renderStatusPagination(Math.ceil(statusFilteredRecords / statusPageSize));

            }

        });

    }

    function renderStatusTable(data) {
        
        let rows = '';
        if (data.length === 0) {
            rows = '<tr><td colspan="5" class="text-center">No records found</td></tr>';
        } else {
            data.forEach(s => {
                rows += `<tr>
                       <td style="text-align: center;">
                            <input type="checkbox" class="form-check-input job-checkbox" data-id="${s.id}" />
                        </td>
                        <td class="text-center">${s.jobUpdateCode}</td>
                        <td class="ps-2">
                             <a href="#" class="job-no-link" data-jobno="${s.jobNo}" data-id="${s.id}">
                                <strong>${s.jobNo}</strong>
                            </a>
                        </td>
                        <td class="ps-2">${s.shipmentStatus}</td>
                        <td class="text-center">${s.dateTimeText}</td>
                    </tr>`;
            });
        }
        $('#jobStatusTableBody').html(rows);

        // Check All functionality
        $('#checkAll').off('change').on('change', function () {
            $('.job-checkbox').prop('checked', $(this).prop('checked'));
        });

        // Individual checkbox
        $(document).off('change', '.job-checkbox').on('change', '.job-checkbox', function () {
            const allChecked = $('.job-checkbox').length === $('.job-checkbox:checked').length;
            $('#checkAll').prop('checked', allChecked);
        });

        // Job link click
        $('.job-no-link').off('click').on('click', function (e) {
            e.preventDefault();
            const jobNo = $(this).data('jobno');
            const id = $(this).data('id');

            $.ajax({
                url: '/SepDocUpdate/GetJobDetails',
                type: 'GET',
                data: { jobNo: jobNo, id: id },
                success: function (response) {
                    populateForm(response.jobTop)
                    populateBottomForm(response.jobBottom)
                    loadJobTimeline(jobNo);
                    $('#isEditHidden').prop('checked', true);
                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            });
        });
    }
    $('#deleteBtn').on('click', function (e) {
        e.preventDefault();

        const checkedIds = $('.job-checkbox:checked').map(function () {
            return $(this).data('id');
        }).get();

        if (checkedIds.length === 0) {
            toastr.warning('Please select at least one record to delete');
            return;
        }

        if (confirm('Are you sure you want to delete ' + checkedIds.length + ' record(s)?')) {
            $.ajax({
                url: '/SepDocUpdate/DeleteJobs',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ ids: checkedIds }),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message || 'Records deleted successfully');
                        statusCurrentPage = 1;  // Reset to first page
                        loadJobStatuses();
                    } else {
                        toastr.error(response.message || 'Error deleting records');
                    }
                },
                error: function (xhr) {
                    if (xhr.status === 403) {
                        try {
                            const resp = JSON.parse(xhr.responseText);
                            toastr.error(resp.message || 'You do not have permission to perform this action');
                        } catch (e) {
                            toastr.error('You do not have permission to perform this action');
                        }
                    } else {
                        toastr.error('Error occurred while deleting records');
                    }
                }
            });
        }
    });
    function renderStatusPagination(totalPages) {
        let pagesHtml = '';
        for (let i = 1; i <= totalPages; i++) {
            pagesHtml += `<li class="page-item ${i === statusCurrentPage ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${i}">${i}</a>
                    </li>`;
        }
        $('#statusPaginationNumbers').html(pagesHtml || '<li class="page-item active"><a class="page-link">1</a></li>');

        $('#statusPaginationPrev').prop('disabled', statusCurrentPage === 1);
        $('#statusPaginationNext').prop('disabled', statusCurrentPage >= totalPages || totalPages === 0);
    }
    //#endregion

    //#region Submit

    $('.saveBtn').on('click', function (e) {
        e.preventDefault();

        var formData = $('#shipmentForm').serialize();

        $.ajax({
            url: $('#shipmentForm').attr('action'),
            type: $('#shipmentForm').attr('method'),
            data: formData,
            success: function (response) {
                if (response.success) {
                    clearForm();
                    statusCurrentPage = 1;  // Reset to first page
                    loadJobStatuses();
                    toastr.success(response.message || "Shipment saved successfully!");
                }
                else {
                    toastr.warning(response.message || "Shipment not saved!");
                }
            },
            error: function (xhr, status, error) {
                console.error(error);
                toastr.warning("Error occurred while saving shipment.");
            }
        });
    });

    //#endregion

});

$('#clearBtn').on('click', function (e) {
    e.preventDefault();
    clearForm();
});

//#region Clr
function clearForm() {

    setSelect('#shipmentModeId', '', false);
    $('#isCustomJobNo').prop('checked', false);
    setField('#jobNo', '', false);
    setField('#customerAddress', '', false);
    setField('#jobNoHidden', '', false);
    setDate('#jobDate', '', false);
    setSelect('#customerId', '', false);
    setField('#customerDeliveryAddress', '', false);
    setSelect('#portId', '', false);
    setSelect('#lcUnitId', '', false);
    setDate('#docsReceivedDate', '', false);
    setField('#lcExpNo', '', false);
    setField('#lcValue', '', false);
    setField('#ipEpNo', '', false);
    setDate('#ipDate', '', false);
    setSelect('#importerId', '', false);
    setField('#invoiceNo', '', false);
    setDate('#invoiceDate', '', false);
    setField('#blNo', '', false);
    setDate('#blDate', '', false);
    setField('#beNo', '', false);
    setDate('#beDate', '', false);
    setField('#containerNo', '', false);
    setField('#containerSize', '', false);
    setField('#lcaNo', '', false);
    setDate('#dischargeDate', '', false);
    setSelect('#quantityUnitId', '', false)
    setSelect('#weightUnitId', '', false);
    setSelect('#forwarderId', '', false);
    setField('#materialDescription', '', false);
    setField('#quantity', '', false);
    setField('#weight', '', false);
    setField('#vesselRottNo', '', false);
    setField('#UpdateRemarks', '', false);
    setDate('#ETADeliveryDatev', '', false);
    setDate('#actualDeliveryDate', '', false);
    setDate('#unstuffingDate', '', false);
    setDate('#unstuffingDate', '', false);
    setDate('#etdDate', '', false);
    setDate('#Date', '', false);
    setSelect('#placeOfLoadingId', '', false);
    setSelect('#shedYardId', '', false);
    setSelect('#freightChargeId', '', false);
    setSelect('#freightChargeId', '', false);
    setSelect('#ShipmentStatus', '', false);
    setField('#creationDate', '', false);
    setField('#updatedDate', '', false);
    setField('#autoId', '0', false);
    setTime('#Time', new Date().toTimeString().split(' ')[0], true);
    $('.create-modify-dateshow').addClass('d-none');
    $('#timelineCard').addClass('d-none');
    $('#shipmentTimeline').html('<div class="timeline-line" id="timelineProgressLine" style="width: 0%;"></div>');
}
//#endregion

//#region Function to populate the form with data
function populateBottomForm(data) {
    showDev(data);    
    if (data.etaDeliveryDate) {
        setDate('#ETADeliveryDate', data.etaDeliveryDate.split('T')[0]);
    }
    if (data.actualDeliveryDate) {
        setDate('#actualDeliveryDate', data.actualDeliveryDate.split('T')[0]);
    }
    if (data.unstuffingDate) {
        setDate('#unstuffingDate', data.unstuffingDate.split('T')[0]);
    }
    if (data.etdDate) {
        setDate('#etdDate', data.etdDate.split('T')[0]);
    }
    if (data.date) {
        setDate('#Date', data.date.split('T')[0]);
    }
    if (data.time) {
        const formattedTime = formatTimeToAMPM(data.time);
        setTime('#Time', formattedTime);
    }

    if (data.placeOfLoadingID) {
        setSelect('#placeOfLoadingId', data.placeOfLoadingID);
    }
    if (data.shedYardID) {
        setSelect('#shedYardId', data.shedYardID);
    }
    if (data.freightChargeID) {
        setSelect('#freightChargeId', data.freightChargeID);
    }

    if (data.shipmentStatus) {
        setSelect('#ShipmentStatus', data.shipmentStatus);
    }
    setField('#UpdateRemarks', data.updateRemarks || '', false);

    if (data.autoId) {
        setField('#autoId', data.autoId);
    }
    setField('#creationDate', data.createdAt || '', true);
    setField('#updatedDate', data.updatedAt || '', true);
    if (data.createdAt || data.updatedAt) {
        $('.create-modify-dateshow').removeClass('d-none').each(function () {
            if ($(this).hasClass('d-flex') === false && $(this).find('label').length) {
                $(this).addClass('d-flex');
            }
        });
    } else {
        $('.create-modify-dateshow').addClass('d-none');
    }
}
function formatTimeToAMPM(timeString) {
    // timeString format: "HH:mm:ss"
    const [hour, minute, second] = timeString.split(':');
    let hourNum = parseInt(hour);
    const ampm = hourNum >= 12 ? 'PM' : 'AM';
    hourNum = hourNum % 12;
    if (hourNum === 0) hourNum = 12;

    return `${String(hourNum).padStart(2, '0')}:${minute}:${second} ${ampm}`;
}

function updateShipmentTimeline(jobStatuses) {
    // jobStatuses = array of status objects with status code and datetime

    const statusMap = {
        '01': '#status01-date',
        '02': '#status02-date',
        '03': '#status03-date',
        '04': '#status04-date',
        '05': '#status05-date',
        '06': '#status06-date',
        '07': '#status07-date'
    };

    // Clear all dates first
    Object.values(statusMap).forEach(selector => {
        $(selector).text('-');
    });

    // Update with actual data
    jobStatuses.forEach(status => {
        const selector = statusMap[status.code];
        if (selector) {
            const dateTime = formatDateTimeToDDMMYYYYHHMMSS(status.dateTime);
            $(selector).text(dateTime);
        }
    });

    // Update timeline item states
    updateTimelineStates(jobStatuses);
}

function updateTimelineStates(jobStatuses) {
    const timelineItems = $('.timeline-item');
    let latestStatusCode = '00';

    if (jobStatuses.length > 0) {
        latestStatusCode = jobStatuses[jobStatuses.length - 1].code;
    }

    timelineItems.each(function (index) {
        const statusCode = String(index + 1).padStart(2, '0');
        const $item = $(this);

        if (parseInt(statusCode) < parseInt(latestStatusCode)) {
            $item.removeClass('active pending').addClass('completed');
        } else if (statusCode === latestStatusCode) {
            $item.removeClass('completed pending').addClass('active');
        } else {
            $item.removeClass('completed active').addClass('pending');
        }
    });
}
function loadJobTimeline(jobNo) {
    
    if (!jobNo) {
        $('#timelineCard').addClass('d-none');
        $('#shipmentTimeline').html('<div class="timeline-line" id="timelineProgressLine" style="width: 0%;"></div>');
        return;
    }

    $.ajax({
        url: '/SepDocUpdate/GetJobTimeline',
        type: 'GET',
        data: { jobNo: jobNo },
        success: function (data) {
            renderTimeline(data);
            $('#timelineCard').removeClass('d-none');
        },
        error: function () {
            $('#timelineCard').addClass('d-none');
        }
    });
}
function renderTimeline(steps) {
    const iconMap = {
        completed: 'fas fa-check',
        active: 'fas fa-truck',
        pending: 'fas fa-box'
    };

    let html = '';

    steps.forEach((step, index) => {
        const icon = iconMap[step.state] || 'fas fa-box';
        const delay = index * 0.15;
        html += `<div class="timeline-item ${step.state}" style="--delay:${delay}s">
            <div class="timeline-marker"><i class="${icon}"></i></div>
            <h6>${step.title}</h6>`;
        html += step.dateText
            ? `<small class="timeline-date">${step.dateText}</small>`
            : `<small class="timeline-date">&nbsp;</small>`;
        html += `</div>`;
    });

    $('#shipmentTimeline').html(html);
}
function populateForm(data) {
    showDev(data)
    setSelect('#shipmentModeId', data.shipmentModeId, true);
    $('#isCustomJobNo').prop('checked', data.isCustomJobNo || false);
    setField('#jobNo', data.jobNo || '', true);
    setField('#customerAddress', data.updatedAt || '', true);
    setField('#jobNoHidden', data.jobNo || '', true);
    setDate('#jobDate', data.jobDate || '', true);
    setDate('#lcDate', data.lcDate || '', true);
    setSelect('#customerId', data.customerId, true);
    setField('#customerDeliveryAddress', data.customerDeliveryAddress || '', true);
    setSelect('#portId', data.portId, true);
    setSelect('#lcUnitId', "", true);
    setDate('#docsReceivedDate', data.customerAddress || '', true);
    setField('#lcExpNo', data.lcExpNo || '', true);
    setField('#lcValue', data.lcValue || '', true);
    setField('#ipEpNo', data.ipEpNo || '', true);
    setDate('#ipDate', data.ipDate || '', true);
    setSelect('#importerId', data.importerId, true);
    setField('#invoiceNo', data.invoiceNo || '', true);
    setDate('#invoiceDate', data.invoiceDate || '', true);
    setField('#blNo', data.blNo || '', true);
    setDate('#blDate', data.blDate || '', true);
    setField('#beNo', data.beNo || '', true);
    setDate('#beDate', data.beDate || '', true);
    setField('#containerNo', data.containerNo || '', true);
    setField('#containerSize', data.containerSize || '', true);
    setField('#lcaNo', data.lcaNo || '', true);
    setDate('#dischargeDate', data.dischargeDate || '', true);
    setSelect('#quantityUnitId', data.quantityUnitId, true);
    setSelect('#weightUnitId', data.weightUnitId, true);
    setSelect('#forwarderId', data.forwarderId, true);
    setField('#materialDescription', data.materialDescription || '', true);
    setField('#quantity', data.quantity || '', true);
    setField('#weight', data.weight || '', true);
    setField('#vesselRottNo', data.vesselRottNo || '', true);
    setField('#read-remarks', data.remarks || '', true);
    
}
//#endregion