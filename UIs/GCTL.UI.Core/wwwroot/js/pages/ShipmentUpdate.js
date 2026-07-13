$(document).ready(function () {

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
                            <td class="text-center">${job.jobDate}</td>
                            <td class="ps-2 text-left">${job.customerName}</td>
                            <td class="text-center">${job.docReceivedDate || '-'}</td>
                           
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
            rows = '<tr><td colspan="6" class="text-center">No records found</td></tr>';
        } else {
            data.forEach(s => {
                const badgeClass = s.Status === "Success" ? "bg-success" :
                    s.Status === "Warning" ? "bg-warning" :
                        s.Status === "Danger" ? "bg-danger" : "bg-secondary";

                rows += `<tr>
                           <td class="text-center">
                                <input type="checkbox"
                                       class="form-check-input"
                                       ${s.status === 'Approved' ? 'checked' : ''}
                                        />
                            </td>

                            <td class="text-center">${s.jobUpdateCode}</td>
                            <td class="ps-2">
                                 <a href="#" class="job-no-link"
                                 data-jobno="${s.jobNo}"
                                  data-id="${s.id}" >
                                    <strong>${s.jobNo}</strong>
                                </a>

                            </td>
                            <td class="ps-2  ">${s.shipmentStatus}</td>
                            <td class="text-center">${s.dateTime}</td>
                            
                        </tr>`;
            });
        }
        $('#jobStatusTableBody').html(rows);


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

                    $('#isEditHidden').prop('checked', true);
                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            });
        });

    }

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

    $('#saveShipmentBtn').on('click', function (e) {
        e.preventDefault();

        var formData = $('#shipmentForm').serialize();

        $.ajax({
            url: $('#shipmentForm').attr('action'),
            type: $('#shipmentForm').attr('method'),
            data: formData,
            success: function (response) {
                if (response.success) {
                    clearForm();
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

$('#clearShipmentBtn').on('click', function (e) {
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
    setField('#remarks', '', false);
    setDate('#etaDeliveryDate', '', false);
    setDate('#actualDeliveryDate', '', false);
    setDate('#unstuffingDate', '', false);
    setDate('#unstuffingDate', '', false);
    setDate('#etdDate', '', false);
    setDate('#etdDate', '', false);
    setSelect('#placeOfLoadingId', '', false);
    setSelect('#shedYardId', '', false);
    setSelect('#freightChargeId', '', false);
    setSelect('#freightChargeId', '', false);
    setField('#shipmentStatus', '', false);
    setField('#creationDate', '', false);
    setField('#updatedDate', '', false);
    setField('#autoId', '', false);
    $('.create-modify-dateshow').addClass('d-none');
}
//#endregion

//#region Function to populate the form with data
function populateBottomForm(data) {

    showDev(data);

    if (data.etaDeliveryDate) {
        setDate('#etaDeliveryDate', data.etaDeliveryDate.split('T')[0]);
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
        setField('#shipmentStatus', data.shipmentStatus);
    }


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

function populateForm(data) {

    showDev(data)
    setSelect('#shipmentModeId', data.shipmentModeId, true);
    $('#isCustomJobNo').prop('checked', data.isCustomJobNo || false);
    setField('#jobNo', data.jobNo || '', true);
    setField('#customerAddress', data.updatedAt || '', true);
    setField('#jobNoHidden', data.jobNo || '', true);
    setDate('#jobDate', data.jobDate || '', true);
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
    setField('#remarks', data.remarks || '', true);
}
//#endregion