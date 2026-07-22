
let statusCurrentPage = 1;
let statusPageSize = 10;
let selectedShipmentStatuses = [];
let statusCurrentSortColumn = 'ShipmentStatusId';
let statusCurrentSortDirection = 'desc';
let statusTotalPages = 1;
let statusCurrentSearchTerm = "";

function getNextShipmentStatusId() {
    $.ajax({
        url: '/next-shipment-status-id',
        type: 'GET',
        success: function (response) {
            $('#shipmentStatusId').val(response);
        },
        error: function (xhr) {
            console.error("Error fetching next Shipment Status ID:", xhr.responseText);
        }
    });
}

function clearShipmentStatusForm() {
    $('#shipmentStatusForm')[0].reset();
    $('#shipmentStatus').removeClass('error-border');
    $('#statusModifyDateContainer').hide();
    $('#statusLDateContainer').hide();
    $("#statusSearchInput").val('');
    $('#shipmentStatusForm input[name="autoId"]').val(0);

    selectedShipmentStatuses = [];
    $('.status-row-checkbox').prop('checked', false);
    $('#statusSelectAll').prop('checked', false);

    statusCurrentPage = 1;
    loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, '');
    getNextShipmentStatusId();
}

function saveOrUpdateShipmentStatus() {
    let autoId = parseInt($('#shipmentStatusForm input[name="autoId"]').val()) || 0;
    const transportModeTypeId = $('#statusTransportModeTypeId').val();
    const shipmentStatusId = $('#shipmentStatusForm input[name="shipmentStatusId"]').val();
    const shipmentStatus = $('#shipmentStatusForm input[name="shipmentStatus"]').val().trim();
    const shortName = $('#shipmentStatusForm input[name="shortName"]').val().trim();

    if (!shipmentStatus) {
        toastr.warning("Shipment Status is required.");
        $('#shipmentStatus').addClass('error-border').focus();
        return;
    }

    let model = {
        AutoId: autoId,
        TransportModeTypeId: transportModeTypeId,
        ShipmentStatusId: shipmentStatusId,
        ShipmentStatus: shipmentStatus,
        ShortName: shortName
    };

    $.ajax({
        url: '/shipment-status/is-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {
            if (response.isDuplicate) {
                toastr.warning(response.message || "Data already exists!");
                return;
            }

            if (autoId === 0) {
                $.ajax({
                    url: '/shipment-status',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data saved successfully.");
                        clearShipmentStatusForm();
                        getNextShipmentStatusId();
                        loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection);
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Error saving data.");
                    }
                });
            } else {
                $.ajax({
                    url: `/shipment-status/${autoId}`,
                    type: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify(model),
                    success: function (res) {
                        toastr.success(res.message || "Data updated successfully.");
                        clearShipmentStatusForm();
                        loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection);
                    },
                    error: function (xhr) {
                        toastr.error(xhr.responseJSON?.message || "Update Failed.");
                    }
                });
            }
        },
        error: function () {
            toastr.error("Error checking duplicate.");
        }
    });
}
function deleteSelectedShipmentStatuses() {
    let selectedIds = [...selectedShipmentStatuses];
    if (selectedIds.length === 0) {
        toastr.warning("Please select at least one item to delete.");
        return;
    }

    $.ajax({
        url: '/shipment-status-multi',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(selectedShipmentStatuses),
        success: function (response) {
            selectedShipmentStatuses = [];
            $('#statusSelectAll').prop('checked', false);
            $("#statusSearchInput").val('');
            clearShipmentStatusForm();
            loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection);

            if (response.isSuccess) {
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function (xhr) {
            let msg = "An error occurred while deleting shipment statuses!";
            try {
                const res = JSON.parse(xhr.responseText);
                if (res.message) msg = res.message;
            } catch { }
            toastr.error(msg);
            console.error('Error deleting shipment statuses:', xhr);
        }
    });
}

function loadShipmentStatuses(page = 1, pageSize = 10, sortColumn = statusCurrentSortColumn, sortDirection = statusCurrentSortDirection, searchTerm = "") {
    statusCurrentPage = page;

    $.ajax({
        url: '/shipment-statuses',
        type: 'GET',
        data: {
            page: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            currentSortColumn: sortColumn,
            sortDirection: sortDirection
        },
        success: function (response) {
            let rows = '';

            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                <td class="text-center align-middle">
                    <input type="checkbox" class="form-check-input status-row-checkbox"
                        data-autoid="${item.autoId}">
                </td>
                <td class="text-center align-middle">
                    <button class="btn btn-transfer-status"
                        data-autoid="${item.autoId}"
                        data-statusid="${item.shipmentStatusId}"
                        data-status="${item.shipmentStatus}"
                        data-shortname="${item.shortName}"
                        data-transportmodeid="${item.transportModeTypeId}"
                        data-ldate="${item.ldate}"
                        data-modifydate="${item.modifyDate}">
                        ${item.shipmentStatusId}
                    </button>
                </td>
                <td>${item.shipmentStatus || ''}</td>
                <td>${item.shortName || ''}</td>
                <td>${item.transportModeType || ''}</td>
            </tr>`;
                });

                $('#statusTblBody').html(rows);

                const totalRecords = response.totalCount || 0;
                const pagination = response.paginationInfo || {};
                statusTotalPages = pagination.totalPages || 1;

                const startIndex = pagination.startItem || 0;
                const endIndex = pagination.endItem || 0;

                $('#statusPaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#statusPaginationInfo').text(`Page ${statusCurrentPage} of ${statusTotalPages}`);
                $('#statusTotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                generateStatusPageButtons(statusCurrentPage, statusTotalPages);
                $('#statusSelectAll').prop('checked', false);
                updateStatusCheckboxState();
                updateStatusSortIndicators();
            } else {
                $('#statusTblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">No Data Available</td></tr>`);
                $('#statusPaginationSummary').text('');
                $('#statusPaginationInfo').text('');
                $('#statusTotalRecordsInfo').text('');
                $('#statusPageNavigation').html('');
                $('#statusSelectAll').prop('checked', false);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading data: ", error);
            $('#statusTblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">Failed to load data.</td></tr>`);
            $('#statusPaginationSummary').text('');
            $('#statusPaginationInfo').text('');
            $('#statusTotalRecordsInfo').text('');
            $('#statusPageNavigation').html('');
            $('#statusSelectAll').prop('checked', false);
        }
    });
}

function updateStatusSortIndicators() {
    $('.sortable').removeClass('sort-asc sort-desc');
    $('.sortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    $(`.sortable[data-column="${statusCurrentSortColumn}"]`).addClass(`sort-${statusCurrentSortDirection}`);

    if (statusCurrentSortDirection === 'asc') {
        $(`.sortable[data-column="${statusCurrentSortColumn}"] .sort-icon`)
            .removeClass('fa-sort fa-sort-down')
            .addClass('fa-sort-up');
    } else {
        $(`.sortable[data-column="${statusCurrentSortColumn}"] .sort-icon`)
            .removeClass('fa-sort fa-sort-up')
            .addClass('fa-sort-down');
    }
}

function updateStatusCheckboxState() {
    $('.status-row-checkbox').each(function () {
        const autoId = $(this).data('autoid');
        $(this).prop('checked', selectedShipmentStatuses.includes(autoId));
    });
    const allChecked = $('.status-row-checkbox:visible').length > 0 &&
        $('.status-row-checkbox:visible:not(:checked)').length === 0;
    $('#statusSelectAll').prop('checked', allChecked);
}

function generateStatusPageButtons(currentPage, totalPages) {
    const navigationDiv = $('#statusPageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        return $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .toggleClass('btn-primary', page === currentPage)
            .toggleClass('btn-outline-primary', page !== currentPage)
            .click(() => loadShipmentStatuses(page, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm));
    }

    if (totalPages <= 1) {
        navigationDiv.append(createButton(1));
        return;
    }

    navigationDiv.append(createButton(1));

    if (currentPage > 3) navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    for (let p = Math.max(2, currentPage - 1); p <= Math.min(totalPages - 1, currentPage + 1); p++) {
        navigationDiv.append(createButton(p));
    }
    if (currentPage < totalPages - 2) navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));

    if (totalPages > 1) navigationDiv.append(createButton(totalPages));
}

function populateShipmentStatusForm(data) {
    
    $('#shipmentStatusForm input[name="shipmentStatusId"]').val(data.statusId);
    $('#shipmentStatusForm input[name="shipmentStatus"]').val(data.status);
    $('#shipmentStatusForm input[name="shortName"]').val(data.shortName);
    $('#shipmentStatusForm input[name="autoId"]').val(data.autoid);
    setSelect('#statusTransportModeTypeId', data.transportModeId?.trim(), false);

    if (data.ldate) {
        const formattedLDate = new Date(data.ldate).toLocaleDateString('en-GB');
        $('#statusDisplayLDate').text(formattedLDate);
        $('#statusLDateContainer').show();
    } else {
        $('#statusLDateContainer').hide();
    }

    if (data.modifydate) {
        const formattedModifyDate = new Date(data.modifydate).toLocaleDateString('en-GB');
        $('#statusDisplayModifyDate').text(formattedModifyDate);
        $('#statusModifyDateContainer').show();
    } else {
        $('#statusModifyDateContainer').hide();
    }
    selectedShipmentStatuses = [parseInt(data.autoid)];
}

$(document).on('click', '.btn-transfer-status', function () {
    
    const data = {
        autoid: $(this).data('autoid'),
        statusId: $(this).data('statusid'),
        status: $(this).data('status'),
        shortName: $(this).data('shortname'),
        transportModeId: $(this).data('transportmodeid'),
        ldate: $(this).data('ldate'),
        modifydate: $(this).data('modifydate')
    };
    populateShipmentStatusForm(data);
});

$(document).on('click', '.status-sortable', function () {
    // reserved if needed separately; using shared .sortable below via namespaced init
});

$(document).on('click', '#statusSelectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.status-row-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.status-row-checkbox:visible').each(function () {
            const autoId = $(this).data('autoid');
            if (!selectedShipmentStatuses.includes(autoId)) {
                selectedShipmentStatuses.push(autoId);
            }
        });
    } else {
        $('.status-row-checkbox:visible').each(function () {
            const autoId = $(this).data('autoid');
            const index = selectedShipmentStatuses.indexOf(autoId);
            if (index !== -1) {
                selectedShipmentStatuses.splice(index, 1);
            }
        });
    }
});

$(document).on('click', '.status-row-checkbox', function () {
    const autoId = $(this).data('autoid');
    const isChecked = $(this).prop('checked');
    if (isChecked) {
        if (!selectedShipmentStatuses.includes(autoId)) {
            selectedShipmentStatuses.push(autoId);
        }
    } else {
        const index = selectedShipmentStatuses.indexOf(autoId);
        if (index !== -1) {
            selectedShipmentStatuses.splice(index, 1);
        }
    }
    const allChecked = $('.status-row-checkbox:visible').length > 0 &&
        $('.status-row-checkbox:visible:not(:checked)').length === 0;
    $('#statusSelectAll').prop('checked', allChecked);
});

$(document).on('change', '#statusTransportModeTypeId', function () {
    var transportModeTypeId = $(this).val();
    if (!transportModeTypeId) {
        loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
        return;
    }

    ajaxRequest({
        url: '/ShipmentStatus/GetShipmentStatusDetails',
        method: 'GET',
        data: { transportModeTypeId: transportModeTypeId },
        successCallback: function (response) {
            renderShipmentStatusGrid(response);
        },
        errorCallback: function (xhr) {
            if (xhr.status === 404) {
                $('#statusTblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">No Data Available</td></tr>`);
            } else {
                console.error('Error:', xhr.responseText);
            }
        }
    });
});

function renderShipmentStatusGrid(data) {
    let rows = '';

    if (data && data.length > 0) {
        data.forEach(function (item) {
            rows += `<tr>
                <td class="text-center align-middle">
                    <input type="checkbox" class="form-check-input status-row-checkbox"
                        data-autoid="${item.autoId}">
                </td>
                <td class="text-center align-middle">
                    <button class="btn btn-transfer-status"
                        data-autoid="${item.autoId}"
                        data-statusid="${item.shipmentStatusId}"
                        data-status="${item.shipmentStatus}"
                        data-shortname="${item.shortName}"
                        data-transportmodeid="${item.transportModeTypeId}"
                        data-ldate="${item.ldate}"
                        data-modifydate="${item.modifyDate}">
                        ${item.shipmentStatusId}
                    </button>
                </td>
                <td>${item.shipmentStatus || ''}</td>
                <td>${item.shortName || ''}</td>
                <td>${item.transportModeType || ''}</td>
            </tr>`;
        });

        $('#statusTblBody').html(rows);
        $('#statusPaginationSummary').text(`Showing ${data.length} of ${data.length} entries`);
        $('#statusPageNavigation').html('');
        $('#statusSelectAll').prop('checked', false);
    } else {
        $('#statusTblBody').html(`<tr class="no-data"><td colspan="5" class="text-center">No Data Available</td></tr>`);
        $('#statusPaginationSummary').text('');
        $('#statusPageNavigation').html('');
    }
}

$(document).ready(function () {
    console.log("oppppp");
    // Shipment Status tab er content lazy load hobe tab click er somoy (nichey দেখুন Index.cshtml)

    $(document).on("click", ".saveShipmentStatusBtn", function (e) {
        e.preventDefault();
        
        saveOrUpdateShipmentStatus();
    });

    $(document).on('click', '#statusFirstPage', function () {
        loadShipmentStatuses(1, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });

    $(document).on('click', '#statusPrevPage', function () {
        if (statusCurrentPage > 1)
            loadShipmentStatuses(statusCurrentPage - 1, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });

    $(document).on('click', '#statusNextPage', function () {
        if (statusCurrentPage < statusTotalPages)
            loadShipmentStatuses(statusCurrentPage + 1, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });

    $(document).on('click', '#statusLastPage', function () {
        loadShipmentStatuses(statusTotalPages, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });

    $(document).on('change', '#statusPageSize', function () {
        statusPageSize = parseInt($(this).val());
        statusCurrentPage = 1;
        loadShipmentStatuses(statusCurrentPage, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });

    $(document).on("click", ".deleteShipmentStatusBtn", function (e) {
        e.preventDefault();

        let selectedIds = [...selectedShipmentStatuses];
        if (selectedIds.length === 0) {
            toastr.warning("Please select at least one item to delete.");
            return;
        }

        GcDelete.confirm("Are you sure to delete?", function () {
            deleteSelectedShipmentStatuses();
        });
    });

    $(document).on("click", ".clearShipmentStatusBtn", function (e) {
        clearShipmentStatusForm();
    });

    $(document).on('input', '#statusSearchInput', debounce(function () {
        statusCurrentSearchTerm = $(this).val().trim();
        loadShipmentStatuses(1, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    }, 500));

    $('.sortable').off('click.status').on('click.status', function () {
        if (!$(this).closest('#statusTabPane').length) return;
        const column = $(this).data('column');

        if (column === statusCurrentSortColumn) {
            statusCurrentSortDirection = (statusCurrentSortDirection === 'asc') ? 'desc' : 'asc';
        } else {
            statusCurrentSortColumn = column;
            statusCurrentSortDirection = 'asc';
        }

        loadShipmentStatuses(1, statusPageSize, statusCurrentSortColumn, statusCurrentSortDirection, statusCurrentSearchTerm);
    });
});