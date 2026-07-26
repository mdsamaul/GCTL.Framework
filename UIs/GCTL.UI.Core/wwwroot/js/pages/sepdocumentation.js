$(document).ready(function () {
    setDate("#jobDate", new Date());
    const entrySections = document.querySelectorAll('.entry-only');

    document.getElementById('entry-tab').addEventListener('shown.bs.tab', function () {

        toolbarEntry.classList.add('show');
        toolbarUpload.classList.remove('show');

        entrySections.forEach(x => x.classList.add('show'));
    });

    document.getElementById('upload-tab').addEventListener('shown.bs.tab', function () {

        toolbarEntry.classList.remove('show');
        toolbarUpload.classList.add('show');

        entrySections.forEach(x => x.classList.remove('show'));
    });

    //#region Populate Top Table

    let currentPage = 1;
    let pageSize = 5;
    let filteredRecords = 0;

    $('#pageSizeSelect').val(pageSize);
    loadJobs();

    let searchTimer;

    // Search
    $(document).on('keyup', '#searchInput', function () {

        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {

            currentPage = 1;
            loadJobs();

        }, 300);
    });

    // Large Screen Filters
    $(document).on(
        'change',
        '#customerFilter,#shipmentModeFilter,#dateFrom,#dateTo,#pageSizeSelect',
        function () {

            currentPage = 1;
            pageSize = parseInt($('#pageSizeSelect').val());

            syncFiltersFromLarge();

            loadJobs();
        });

    // Small Screen Filters
    $(document).on(
        'change',
        '#customerFilterSm,#shipmentModeFilterSm,#dateFromSm,#dateToSm',
        function () {

            currentPage = 1;
            pageSize = parseInt($('#pageSizeSelect').val());

            syncFiltersFromSmall();

            loadJobs();
        });

    function syncFiltersFromLarge() {

        $('#customerFilterSm')
            .val($('#customerFilter').val())
            .trigger('change.select2');

        $('#shipmentModeFilterSm')
            .val($('#shipmentModeFilter').val())
            .trigger('change.select2');

        $('#dateFromSm').val($('#dateFrom').val());
        $('#dateToSm').val($('#dateTo').val());
    }

    function syncFiltersFromSmall() {

        $('#customerFilter')
            .val($('#customerFilterSm').val())
            .trigger('change.select2');

        $('#shipmentModeFilter')
            .val($('#shipmentModeFilterSm').val())
            .trigger('change.select2');

        $('#dateFrom').val($('#dateFromSm').val());
        $('#dateTo').val($('#dateToSm').val());
    }

    // Pagination
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

    // Load Data
    function loadJobs() {

        const filters = {

            page: currentPage,
            pageSize: pageSize,

            customerId: $('#customerFilter').val() || $('#customerFilterSm').val(),

            shipmentMode: $('#shipmentModeFilter').val() || $('#shipmentModeFilterSm').val(),

            dateFrom: $('#dateFrom').val() || $('#dateFromSm').val(),

            dateTo: $('#dateTo').val() || $('#dateToSm').val(),

            search: $('#searchInput').val()
        };

        console.log(filters); // Debug

        $.ajax({

            url: '/SepDocUpdate/GetJobs',

            type: 'GET',

            data: filters,

            success: function (response) {

                console.log(response);

                renderJobTable(response.data);

                filteredRecords = response.filteredRecords;

                $('#dataListInfo').text(
                    `Showing ${(currentPage - 1) * pageSize + 1} to ${Math.min(currentPage * pageSize, filteredRecords)} of ${filteredRecords} entries`
                );

                renderPagination(Math.ceil(filteredRecords / pageSize));
            },

            error: function () {

                toastr.error('Error loading jobs');
            }
        });
    }
    function renderJobTable(data) {

        let rows = '';

        if (!data || data.length === 0) {

            rows = `
            <tr>
                <td colspan="5" class="text-center">
                    No records found
                </td>
            </tr>`;
        }
        else {

            data.forEach(function (j) {

                rows += `
                <tr>

                    <td class="ps-2">
                        <a href="#"
                           class="text-primary fw-semibold job-detail-link"
                           data-id="${j.tc}">
                            ${j.jobNo}
                        </a>
                    </td>

                    <td>${j.shipmentMode ?? ''}</td>

                    <td class="text-center">
                        ${j.jobDateFormatted ?? '-'}
                    </td>

                    <td>
                        ${j.customerName ?? ''}
                    </td>

                    <td class="text-center">
                        ${j.docReceivedDateFormatted ?? '-'}
                    </td>

                </tr>`;
            });

        }

        $('#job-settings-table').html(rows);
    }
    // Pagination UI
    function renderPagination(totalPages) {

        let html = '';

        if (totalPages <= 7) {

            for (let i = 1; i <= totalPages; i++) {

                html += `
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">
                    ${i}
                </a>
            </li>`;
            }
        }
        else {

            html += `
        <li class="page-item ${currentPage === 1 ? 'active' : ''}">
            <a class="page-link" href="#" data-page="1">1</a>
        </li>`;

            if (currentPage > 4) {

                html += `<li class="page-item disabled"><a class="page-link">...</a></li>`;
            }

            let start = Math.max(2, currentPage - 1);
            let end = Math.min(totalPages - 1, currentPage + 1);

            for (let i = start; i <= end; i++) {

                html += `
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">
                    ${i}
                </a>
            </li>`;
            }

            if (currentPage < totalPages - 3) {

                html += `<li class="page-item disabled"><a class="page-link">...</a></li>`;
            }

            html += `
        <li class="page-item ${currentPage === totalPages ? 'active' : ''}">
            <a class="page-link" href="#" data-page="${totalPages}">
                ${totalPages}
            </a>
        </li>`;
        }

        $('#paginationNumbers').html(html);

        $('#paginationPrev').prop('disabled', currentPage === 1);

        $('#paginationNext').prop(
            'disabled',
            currentPage >= totalPages || totalPages === 0
        );
    }

    //#endregion

    //#region Load detail into form

    $(document).on('click', '.job-detail-link', function (e) {
        e.preventDefault();
        const tc = $(this).data('id');

        $.ajax({
            url: '/SepDocUpdate/GetJobDetail',
            type: 'GET',
            data: { tc: tc },
            success: function (data) {
                populateForm(data);
            },
            error: function () {
                toastr.error('Error loading job details');
            }
        });
    });

    function populateForm(data) {
        clearForm(false);

        $('#tc').val(data.tc || 0);
        setSelect('#shipmentModeId', data.shipmentModeId);
        $('#shipmentModeId').trigger('change');

        $('#isCustomJobNo').prop('checked', data.isCustomJobNo || false);
        setField('#jobNo', data.jobNo);
        setDate('#jobDate', data.date);
        setSelect('#customerId', data.customerId);
        setField('#customerAddress', data.customerAddress);
        setSelect('#deliveryLocationCode', data.deliveryLocationCode);
        setSelect('#portId', data.portId);
        setDate('#docReceivedDate', data.docReceivedDate);
        setField('#expNo', data.expNo);
        setDate('#expDate', data.expDate);
        setField('#lcNo', data.lcNo);
        setDate('#lcDate', data.lcDate);
        setField('#lcValue', data.lcValue);
        setSelect('#currencyId', data.currencyId);
        setField('#bdtRate', data.bdtRate);
        setField('#ipNo', data.ipNo);
        setDate('#ipDate', data.ipDate);
        setSelect('#importerId', data.importerID);
        setField('#invoiceNo', data.invoiceNo);
        setDate('#invoiceDate', data.invoiceDate);
        setField('#invoiceValue', data.invoiceValue);
        setField('#assessableRate', data.assessableRate);
        setField('#assessableValue', data.assessableValue);
        setField('#blNo', data.blNo);
        setDate('#blDate', data.blDate);
        setField('#containerNo', data.containerNo);
        setField('#containerSize', data.containerSize);
        setField('#lcaNo', data.lcaNo);
        setField('#materialDescription', data.materialDescription);
        setField('#quntity1', data.quntity1);
        setSelect('#unit1', data.unit1);
        setField('#quntity2', data.quntity2);
        setSelect('#unit2', data.unit2);
        setField('#fwdrShippingAgent', data.fwdr_ShippingAgent);
        setField('#nameOfFreightForwarder', data.nameOfFreightForwarder);
        setField('#vesselRottNo', data.vesselRottNo);
        setSelect('#placeOfLoadingId', data.placeOfLoadingID);
        setSelect('#shedYardId', data.shedYardID);
        setField('#hawb', data.hawb);
        setField('#mawb', data.mawb);
        setField('#assessmentNo', data.assessmentNo);
        setField('#rNo', data.rNo);
        setField('#clientStationwiseSLNo', data.clientStationwiseSLNo);
        setSelect('#freightChargeId', data.freightCharge);
        setField('#term', data.term);
        setField('#remarks', data.remarks);

        // shared delivery
        setDate('#etdDate', data.etdDate);
        setDate('#docsBankSignDate', data.docsBankSignDate);
        setField('#beNo', data.benO);
        setDate('#beDate', data.beDate);
        setDate('#actualDeliveryDate', data.actualDeliveryDate);

        // sea
        setDate('#finalIgmRecDate', data.finalIgmRecDate);
        setDate('#shipArrivedDate', data.shipArrivedDate);
        setDate('#unstuffDischargeDate', data.unstuffDischargeDate);
        setDate('#estimatedDeliveryDate', data.estimatedDeliveryDate);

        // air
        setDate('#flightArrivalDate', data.flightArrivalDate);
        setDate('#arrivalNoticeDate', data.arrivalNoticeDate);

        currentJobNo = data.jobNo;
        $('#uploadJobNoDisplay').val(data.jobNo || '');
        $('#uploadJobDateDisplay').val(data.date ? new Date(data.date).toLocaleDateString('en-GB') : '');
        $('#uploadCustomerDisplay').val(data.customerId || '');
        $('#uploadAddressDisplay').val(data.customerAddress || '');

        loadDocuments(currentJobNo);
    }

    //#endregion

    //#region Save

    $(document).on('click', '.saveBtn', function (e) {
        e.preventDefault();

        const model = collectFormData();

        $.ajax({
            url: '/SepDocUpdate/Save',
            type: 'POST',
            data: model,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message || 'Saved successfully');
                    currentPage = 1;
                    loadJobs();
                } else {
                    toastr.warning(response.message || 'Save failed');
                }
            },
            error: function () {
                toastr.error('Error occurred while saving');
            }
        });
    });

    function collectFormData() {
        return {
            Tc: $('#tc').val() || 0,
            ShipmentModeId: $('#shipmentModeId').val(),
            TransportModeTypeId: $('#transportModeTypeId').val(),
            IsCustomJobNo: $('#isCustomJobNo').is(':checked'),
            JobNo: $('#jobNo').val(),
            Date: $('#jobDate').val(),
            CustomerId: $('#customerId').val(),
            CustomerAddress: $('#customerAddress').val(),
            DeliveryLocationCode: $('#deliveryLocationCode').val(),
            PortId: $('#portId').val(),
            DocReceivedDate: $('#docReceivedDate').val(),
            ExpNo: $('#expNo').val(),
            ExpDate: $('#expDate').val(),
            LCNo: $('#lcNo').val(),
            LCDate: $('#lcDate').val(),
            LCValue: $('#lcValue').val(),
            CurrencyId: $('#currencyId').val(),
            BDTRate: $('#bdtRate').val(),
            IPNo: $('#ipNo').val(),
            IPDate: $('#ipDate').val(),
            ImporterID: $('#importerId').val(),
            InvoiceNo: $('#invoiceNo').val(),
            InvoiceDate: $('#invoiceDate').val(),
            InvoiceValue: $('#invoiceValue').val(),
            AssessableRate: $('#assessableRate').val(),
            AssessableValue: $('#assessableValue').val(),
            BLNo: $('#blNo').val(),
            BLDate: $('#blDate').val(),
            ContainerNo: $('#containerNo').val(),
            ContainerSize: $('#containerSize').val(),
            LCANo: $('#lcaNo').val(),
            MaterialDescription: $('#materialDescription').val(),
            Quntity1: $('#quntity1').val(),
            Unit1: $('#unit1').val(),
            Quntity2: $('#quntity2').val(),
            Unit2: $('#unit2').val(),
            FWDR_ShippingAgent: $('#fwdrShippingAgent').val(),
            NameOfFreightForwarder: $('#nameOfFreightForwarder').val(),
            VesselRottNo: $('#vesselRottNo').val(),
            PlaceOfLoadingID: $('#placeOfLoadingId').val(),
            ShedYardID: $('#shedYardId').val(),
            HAWB: $('#hawb').val(),
            MAWB: $('#mawb').val(),
            AssessmentNo: $('#assessmentNo').val(),
            RNo: $('#rNo').val(),
            ClientStationwiseSLNo: $('#clientStationwiseSLNo').val(),
            FreightCharge: $('#freightChargeId').val(),
            Remarks: $('#remarks').val(),

            // shared
            ETDDate: $('#etdDate').val(),
            DocsBankSignDate: $('#docsBankSignDate').val(),
            BENo: $('#beNo').val(),
            BEDate: $('#beDate').val(),
            ActualDeliveryDate: $('#actualDeliveryDate').val(),

            // sea
            FinalIgmRecDate: $('#finalIgmRecDate').val(),
            ShipArrivedDate: $('#shipArrivedDate').val(),
            UnstuffDischargeDate: $('#unstuffDischargeDate').val(),
            EstimatedDeliveryDate: $('#estimatedDeliveryDate').val(),

            // air
            FlightArrivalDate: $('#flightArrivalDate').val(),
            ArrivalNoticeDate: $('#arrivalNoticeDate').val()
        };
    }

    //#endregion

    //#region Delete

    $(document).on('click', '#deleteBtn', function (e) {
        e.preventDefault();
        const ids = $('.job-row-checkbox:checked').map(function () { return $(this).data('id'); }).get();

        if (ids.length === 0) {
            toastr.warning('Please select at least one record to delete');
            return;
        }

        if (!confirm('Are you sure you want to delete ' + ids.length + ' record(s)?')) return;

        $.ajax({
            url: '/SepDocUpdate/DeleteJobs',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ ids: ids }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    loadJobs();
                } else {
                    toastr.error(response.message);
                }
            },
            error: function () {
                toastr.error('Error occurred while deleting');
            }
        });
    });

    //#endregion

    //#region Clear

    $(document).on('click', '#clearBtn', function (e) {
        e.preventDefault();
        clearForm(true);
    });

    function clearForm(clearJobNo) {
        $('#tc').val(0);
        setSelect('#shipmentModeId', '');
        $('#commonDeliveryGroup, seaFieldsGroup, .airFieldsGroup').hide();
        $('#transportModeTypeId').val('');
        $('#isCustomJobNo').prop('checked', false);
        if (clearJobNo !== false) setField('#jobNo', '');

        $('#sepDocForm')[0].reset();
        $('.custom-select-single').val('').trigger('change.select2');
    }

    //#endregion

    //#region Document Upload tab
    let currentJobNo;
    $(document).on('shown.bs.tab', '#upload-tab', function () {
        if (currentJobNo) loadDocuments(currentJobNo);
    });

    function loadDocuments(jobNo) {
        if (!jobNo) {
            $('#document-list-body').html('<tr><td colspan="6" class="text-center">No data available</td></tr>');
            return;
        }

        $.ajax({
            url: '/SepDocUpdate/GetJobDocuments',
            type: 'GET',
            data: { jobNo: jobNo },
            success: function (data) {
                renderDocumentTable(data);
            },
            error: function () {
                toastr.error('Error loading documents');
            }
        });
    }

    function renderDocumentTable(data) {
        let rows = '';
        if (!data || data.length === 0) {
            rows = '<tr><td colspan="6" class="text-center">No data available</td></tr>';
        } else {
            data.forEach(d => {
                rows += `<tr>
                    <td class="text-center"><input type="checkbox" /></td>
                    <td class="text-center">${d.slNo}</td>
                    <td>${d.documentName}</td>
                    <td>${d.description || ''}</td>
                    <td><a href="${d.filePath}" target="_blank">${d.fileName}</a></td>
                    <td class="text-center">
                        <button type="button" class="btn btn-sm btn-outline-danger delete-doc-btn" data-tc="${d.tc}">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>`;
            });
        }
        $('#document-list-body').html(rows);
    }

    $(document).on('click', '#uploadDocumentBtn', function (e) {
        e.preventDefault();

        if (!currentJobNo) {
            toastr.warning('Please select/save a Job first.');
            return;
        }

        const fileInput = $('#uploadFile')[0];
        if (!fileInput.files.length) {
            toastr.warning('Please choose a file to upload.');
            return;
        }
        if (!$('#documentName').val()) {
            toastr.warning('Document Name is required.');
            return;
        }

        const formData = new FormData();
        formData.append('JobNo', currentJobNo);
        formData.append('DocumentName', $('#documentName').val());
        formData.append('Description', $('#documentDescription').val());
        formData.append('uploadFile', fileInput.files[0]);

        $.ajax({
            url: '/SepDocUpdate/UploadDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    $('#uploadFile').val('');
                    $('#documentName').val('');
                    $('#documentDescription').val('');
                    loadDocuments(currentJobNo);
                } else {
                    toastr.warning(response.message);
                }
            },
            error: function () {
                toastr.error('Error occurred while uploading');
            }
        });
    });

    $(document).on('click', '.delete-doc-btn', function () {
        const tc = $(this).data('tc');
        if (!confirm('Delete this document?')) return;

        $.ajax({
            url: '/SepDocUpdate/DeleteDocument',
            type: 'POST',
            data: { tc: tc },
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    loadDocuments(currentJobNo);
                } else {
                    toastr.error(response.message);
                }
            }
        });
    });

    //#endregion

    //#region Shipment Mode Change - Show/Hide Dynamic Fields

    // $(document).on('change', '#shipmentModeId', function () {
    //     
    //     const shipmentModeId = $(this).val();
        

    //     // Set the TransportModeTypeId hidden field
    //     $('#transportModeTypeId').val(transportModeTypeId || shipmentModeId);

    //     // Hide all dynamic groups first
    //     $('#commonDeliveryGroup, seaFieldsGroup, .airFieldsGroup').hide();

    //     // Show appropriate groups based on TransportModeTypeId
    //     // 001 = Air, 002 = Sea
    //     if (transportModeTypeId === '001' || shipmentModeId === '001') {
    //         // Air shipment - show Air fields
    //         $('.airFieldsGroup').show();
    //     } else if (transportModeTypeId === '002' || shipmentModeId === '002') {
    //         // Sea shipment - show Sea fields
    //         $('seaFieldsGroup').show();
    //     }

    //     generateJobNo();
    // });
    $(document).on('change', '#shipmentModeId', function () {
        
        const shipmentModeId = $("#shipmentModeId").val();

        $('seaFieldsGroup, .airFieldsGroup').hide();

        if (!shipmentModeId) {
            $('#transportModeTypeId').val('');
            generateJobNo();
            return;
        }

        JobNoGenerator(shipmentModeId);
    });

    function JobNoGenerator(shipmentModeId) {
        
        $.ajax({
            url: '/SepDocUpdate/GetTransportModeType',
            type: 'GET',
            data: { shipmentModeId: shipmentModeId },
            success: function (response) {

                if (response.success) {

                    const transportModeTypeId = response.transportModeTypeId;
                    $('#transportModeTypeId').val(transportModeTypeId);

                    if (transportModeTypeId === '001') {
                        $('.airFieldsGroup').show();     // Air
                    } else if (transportModeTypeId === '002') {
                        $('.seaFieldsGroup').show();     // Sea
                    }
                }

                generateJobNo();
            },
            error: function () {
                toastr.error('Error retrieving transport mode');
                generateJobNo();
            }
        });
    }
    //#endregion

    $(document).on('change', '#customerId', function () {
        generateJobNo();
        let customerId = $(this).val();
        customerInfo(customerId);
    });

    $(document).on('change', '#isCustomJobNo', function () {
        var isChecked = $(this).is(':checked');
        $('#jobNo').prop('readonly', !isChecked);
        if (!isChecked) {
            generateJobNo();
        }
    });

    function generateJobNo() {
        
        if ($('#isCustomJobNo').is(':checked')) {
            return;
        }

        var shipmentModeId = $('#shipmentModeId').val();
        var customerId = $('#customerId').val();

        if (!shipmentModeId && !customerId) {
            $('#jobNo').val('');
            ajaxRequest({
                url: '/SepDocUpdate/GenerateJobNo',
                method: 'GET',
                data: { shipmentModeId: shipmentModeId, customerId: customerId },
                targetSelector: '#jobNo',
                targetProperty: 'val',
                errorCallback: function (xhr) {
                    console.error('Error generating job no:', xhr.responseText);
                    $('#jobNo').val('');
                }
            });
        }

        ajaxRequest({
            url: '/SepDocUpdate/GenerateJobNo',
            method: 'GET',
            data: { shipmentModeId: shipmentModeId, customerId: customerId },
            targetSelector: '#jobNo',
            targetProperty: 'val',
            errorCallback: function (xhr) {
                console.error('Error generating job no:', xhr.responseText);
                $('#jobNo').val('');
            }
        });
    }

    async function customerInfo(customerId) {
        var response = await ajaxRequest({
            url: '/SepDocUpdate/CustomerInfo',
            method: 'GET',
            data: { customerId: customerId }
        });

        if (response && response.success) {
            var customer = response.data;

            $('#customerName').val(customer.customerName);
            $('#customerAddress').val(customer.customerAddress);

            var deliveryItems = customer.delivaryAddress.map(function (d) {
                return {
                    id: d.delivaryId,
                    text: d.delivaryAddress
                };
            });

            setSelect('#deliveryLocationCode', '', false, deliveryItems);
        }
    }
    generateJobNo();
});