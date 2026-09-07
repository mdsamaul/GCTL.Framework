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

    //#region Initialize Grids
    
    function getFilters(isBottom) {
        if (isBottom) {
            return {
                customerId: $('#customerLoadGridFilter').val(),
                dateFrom: $('#dateLoadGridFrom').val(),
                dateTo: $('#dateLoadGridTo').val(),
                searchTerm: $('#statusSearchInput').val()
            };
        } else {
            return {
                customerId: $('#customerFilter').val() || $('#customerFilterSm').val(),
                shipmentMode: $('#shipmentModeFilter').val() || $('#shipmentModeFilterSm').val(),
                dateFrom: $('#dateFrom').val() || $('#dateFromSm').val(),
                dateTo: $('#dateTo').val() || $('#dateToSm').val(),
                searchTerm: $('#searchInput').val()
            };
        }
    }

    var topGrid = $.gctlGrid.initFromModel({
        gridId: "JobSettingsTable",
        ajaxData: function(d) {
            $.extend(d, getFilters(false));
        }
    });

    var bottomGrid = $.gctlGrid.initFromModel({
        gridId: "jobListTable",
        ajaxData: function(d) {
            $.extend(d, getFilters(true));
        }
    });

    let searchTimer;
    $(document).on('keyup', '#searchInput', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            if (topGrid) topGrid.ajax.reload();
        }, 300);
    });

    $(document).on('change', '#customerFilter,#shipmentModeFilter,#dateFrom,#dateTo,#pageSizeSelect', function () {
        syncFiltersFromLarge();
        if (topGrid) topGrid.ajax.reload();
    });

    $(document).on('change', '#customerFilterSm,#shipmentModeFilterSm,#dateFromSm,#dateToSm', function () {
        syncFiltersFromSmall();
        if (topGrid) topGrid.ajax.reload();
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

    let bottomSearchTimer;
    $(document).on('keyup', '#statusSearchInput', function () {
        clearTimeout(bottomSearchTimer);
        bottomSearchTimer = setTimeout(function () {
            if (bottomGrid) bottomGrid.ajax.reload();
        }, 300);
    });

    $(document).on('change', '#customerLoadGridFilter,#dateLoadGridFrom,#dateLoadGridTo,#statusPageSizeSelect', function () {
        if (bottomGrid) bottomGrid.ajax.reload();
    });

    //#endregion

    //#region Bottom Grid Selection + Delete (Single + Multi via checkbox)

    let selectedBottomIds = [];

    $('#jobListTable').on('draw.dt', function () {
        selectedBottomIds = [];
        updateDeleteSelectedButton();
    });

    $(document).on('change', '#jobListTable tbody .form-check-input', function () {
        const id = String($(this).val());
        if (!id || id === 'on') return;

        const checked = $(this).is(':checked');

        if (checked) {
            if (!selectedBottomIds.includes(id)) selectedBottomIds.push(id);
        } else {
            selectedBottomIds = selectedBottomIds.filter(x => x !== id);
        }

        updateDeleteSelectedButton();
    });

    function updateDeleteSelectedButton() {
        $('#selectedCountBottom').text(selectedBottomIds.length);
    }

    $(document).on('click', '.job-detail-link', function (e) {
        e.preventDefault();
        const tc = $(this).data('id');
        selectedBottomIds = [];
        selectedBottomIds.push(String(tc));
        updateDeleteSelectedButton();

        $.ajax({
            url: '/SepDocUpdate/GetJobDetail',
            type: 'GET',
            data: { tc: tc },
            success: async function (data) {
                await populateForm(data);
            },
            error: function () {
                toastr.error('Error loading job details');
            }
        });
    });

    $(document).on('click', '.doc-delete-entry-btn', function (e) {
        e.preventDefault();

        // Check if this is the form delete button
        const isFormDelete = $(this).closest('.entry-only').length > 0;

        if (isFormDelete) {
            // Form থেকে ডিলিট
            const tc = $('#tc').val();
            const jobNo = $('#jobNo').val();

            if (!tc || tc === '0') {
                toastr.warning('Please select a job first to delete');
                return;
            }

            if (!confirm(`Are you sure you want to delete Job No "${jobNo}"?`)) return;

            deleteJobsByIds([tc], function () {
                clearForm(true);
            });
        } else {
            // Bottom grid থেকে multi-select delete
            if (selectedBottomIds.length === 0) {
                toastr.warning('Please select at least one record to delete');
                return;
            }

            if (!confirm(`Are you sure you want to delete ${selectedBottomIds.length} record(s)?`)) return;

            deleteJobsByIds(selectedBottomIds);
        }
    });

    // Single trash icon থেকে delete
    $(document).on('click', '.single-delete-btn', function (e) {
        e.preventDefault();
        const id = $(this).data('id');

        // Clear previous selection and add current ID
        selectedBottomIds = [];
        selectedBottomIds.push(String(id));

        if (!confirm('Are you sure you want to delete this record?')) {
            selectedBottomIds = [];
            return;
        }

        deleteJobsByIds([id]);
    });

    function deleteJobsByIds(ids, onSuccessExtra) {
        $.ajax({
            url: '/SepDocUpdate/DeleteJobs',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ ids: ids }),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    selectedBottomIds = [];
                    loadBottomJobs();
                    loadJobs();
                    if (typeof onSuccessExtra === 'function') onSuccessExtra();
                } else {
                    toastr.error(response.message);
                }
            },
            error: function () {
                toastr.error('Error occurred while deleting');
            }
        });
    }

    //#endregion
    //#region Load detail into form

    async function populateForm(data) {        
        console.log("Populating form with data:", data);
        if (!data) return;

        clearForm(false);
        $('#tc').val(data.tc || 0);

        // Custom Job No & Job No
        const isCustom = data.isCustomJobNo === true || data.isCustomJobNo === "true" || data.isCustomJobNo === "1";
        $('#isCustomJobNo').prop('checked', isCustom);
        $('#jobNo').prop('readonly', !isCustom);
        setField('#jobNo', data.jobNo || '');

        // Dates
        setDate('#jobDate', data.date);
        setDate('#docReceivedDate', data.docReceivedDate);
        setDate('#expDate', data.expDate);
        setDate('#lcDate', data.lcDate);
        setDate('#beDate', data.beDate || data.beNoDate);
        setDate('#beNoDate', data.beNoDate || data.beDate);
        setDate('#ipDate', data.ipDate);
        setDate('#invoiceDate', data.invoiceDate);
        setDate('#blDate', data.blDate);
        setDate('#etdDate', data.etdDate);
        setDate('#etactgport', data.etactgport || data.etdDate);
        setDate('#seaFinalIgmRecDate', data.finalIgmRecDate);
        setDate('#docsBankSignDate', data.docsBankSignDate);
        setDate('#actualDeliveryDate', data.actualDeliveryDate);
        setDate('#seaShipArrivedDate', data.shipArrivedDate);
        setDate('#unstuffDischargeDate', data.unstuffDischargeDate);
        setDate('#seaEstimatedDeliveryDate', data.estimatedDeliveryDate);
        setDate('#flightArrivalDate', data.flightArrivalDate);
        setDate('#arrivalNoticeDate', data.arrivalNoticeDate);

        // Shipment Mode & Transport Group Visibility
        setSelect('#shipmentModeId', data.shipmentModeId);
        const transportModeTypeId = data.transportModeTypeId || $('#transportModeTypeId').val();
        $('#transportModeTypeId').val(transportModeTypeId || '');

        if (transportModeTypeId === '001') {
            $('.airFieldsGroup').css('display', 'block');
            $('.seaFieldsGroup').css('display', 'none');
        } else if (transportModeTypeId === '002') {
            $('.seaFieldsGroup').css('display', 'block');
            $('.airFieldsGroup').css('display', 'none');
        } else {
            $('.seaFieldsGroup, .airFieldsGroup').hide();
        }

        // Customer & Delivery Address
        setSelect('#customerId', data.customerId);
        setField('#customerAddress', data.customerAddress);

        if (data.customerId) {
            await customerInfoForPopulate(data.customerId, data.deliveryLocationCode);
        } else {
            setSelect('#deliveryLocationCode', data.deliveryLocationCode);
        }

        // Select Dropdowns
        setSelect('#portId', data.portId || data.portNameId);
        setSelect('#currencyId', data.currencyId);
        setSelect('#importerId', data.importerID || data.importerId);
        setSelect('#invoiceCurrencyId', data.invoiceCurrencyID || data.invoiceCurrencyId);
        setSelect('#unit1', data.unit1);
        setSelect('#unit2', data.unit2);
        setSelect('#placeOfLoadingId', data.placeOfLoadingID || data.placeOfLoadingId);
        setSelect('#shedYardId', data.shedYardID || data.shedYardId);
        setSelect('#freightChargeId', data.freightCharge);

        // Text & Number Inputs
        setField('#expNo', data.expNo);
        setField('#lcNo', data.lcNo);
        setField('#lcValue', data.lcValue);
        setField('#bdtRate', data.bdtRate);
        setField('#ipNo', data.ipNo);
        setField('#invoiceNo', data.invoiceNo);
        setField('#invoiceValue', data.invoiceValue);
        setField('#assessableRate', data.assessableRate);
        setField('#assessableValue', data.assessableValue);
        setField('#blNo', data.blNo);
        setField('#beNo', data.beNo);
        setField('#containerNo', data.containerNo);
        setField('#containerSize', data.containerSize);
        setField('#materialDescription', data.materialDescription);
        setField('#quntity1', data.quntity1);
        setField('#quntity2', data.quntity2);
        setField('#fwdrShippingAgent', data.fwdr_ShippingAgent || data.fwdR_ShippingAgent || data.fwdrShippingAgent || data.FWDR_ShippingAgent || data.nameOfFreightForwarder);
        setField('#vesselRottNo', data.vesselRottNo);
        setField('#hawb', data.hawb);
        setField('#mawb', data.mawb);
        setField('#assessmentNo', data.assessmentNo);
        setField('#RNo', data.rNo || data.RNo);
        setField('#rNo', data.rNo || data.RNo);
        setField('#term', data.term);
        setField('#remarks', data.remarks);

        // Upload Tab Info Display
        currentJobNo = data.jobNo;
        $('#uploadJobNoDisplay').val(data.jobNo || '');
        $('#uploadJobDateDisplay').val(data.date ? new Date(data.date).toLocaleDateString('en-GB') : '');
        $('#uploadCustomerDisplay').val(data.customerId || '');
        $('#uploadAddressDisplay').val(data.customerAddress || '');

        loadDocuments(currentJobNo);
        calculateLCValueBDT();
        calculateAssessableValue();
    }

    async function customerInfoForPopulate(customerId, selectedDeliveryLocation) {
        var response = await ajaxRequest({
            url: '/SepDocUpdate/CustomerInfo',
            method: 'GET',
            data: { customerId: customerId }
        });

        if (response && response.success) {
            var customer = response.data;
            $('#customerAddress').val(customer.customerAddress);

            var deliveryItems = (customer.delivaryAddress || []).map(function (d) {
                return {
                    id: d.delivaryId,
                    text: d.delivaryAddress
                };
            });

            setSelect(
                '#deliveryLocationCode',
                selectedDeliveryLocation || '',
                false,
                deliveryItems,
                false,
                'Select Customer Delivery'
            );
        }
    }

    //#endregion

    //#region Save (Create + Update)

    $(document).on('click', '.doc-save-entry-btn', function (e) {
        e.preventDefault();

        var $valid = $('#sepDocForm').valid();
        if (!$valid) {
            toastr.warning('Please fill required fields');
            return;
        }

        const model = collectFormData();
        debugger
        console.log(model);
        $.ajax({
            url: '/SepDocUpdate/Save',
            type: 'POST',
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            data: $.param(model),
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message || 'Saved successfully');
                    currentPage = 1;
                    bottomCurrentPage = 1;
                    loadJobs();
                    loadBottomJobs();
                    clearForm();
                    setDate("#jobDate", new Date());
                } else {
                    console.error(response.errors);
                    toastr.warning(response.message || 'Save failed');
                }
            },
            error: function (xhr) {
                console.error(xhr.responseText);
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
            InvoiceCurrencyID: $('#invoiceCurrencyId').val(),
            AssessableRate: $('#assessableRate').val(),
            AssessableValue: $('#assessableValue').val(),
            BLNo: $('#blNo').val(),
            BLDate: $('#blDate').val(),
            ContainerNo: $('#containerNo').val(),
            ContainerSize: $('#containerSize').val(),
            MaterialDescription: $('#materialDescription').val(),
            Quntity1: $('#quntity1').val(),
            Unit1: $('#unit1').val(),
            Quntity2: $('#quntity2').val(),
            Unit2: $('#unit2').val(),
            FWDR_ShippingAgent: $('#fwdrShippingAgent').val(),
            VesselRottNo: $('#vesselRottNo').val(),
            PlaceOfLoadingID: $('#placeOfLoadingId').val(),
            ShedYardID: $('#shedYardId').val(),
            HAWB: $('#hawb').val(),
            MAWB: $('#mawb').val(),
            AssessmentNo: $('#assessmentNo').val(),
            RNo: $('#rNo').val(),
            FreightCharge: $('#freightChargeId').val(),
            Remarks: $('#remarks').val(),

            ETDDate: $('#etdDate').val(),
            ETACTGPORT: $('#etactgport').val(),
            BENoDate: $('#beNoDate').val(),
            BENo: $('#beNo').val(),
            BEDate: $('#beDate').val(),
            RNo: $('#RNo').val(),
            ActualDeliveryDate: $('#actualDeliveryDate').val(),

            FinalIgmRecDate: $('#seaFinalIgmRecDate').val(),
            ShipArrivedDate: $('#seaShipArrivedDate').val(),
            UnstuffDischargeDate: $('#unstuffDischargeDate').val(),
            EstimatedDeliveryDate: $('#seaEstimatedDeliveryDate').val(),
            Term: $('#term').val(),

            FlightArrivalDate: $('#flightArrivalDate').val(),
            ArrivalNoticeDate: $('#arrivalNoticeDate').val(),
            DocsBankSignDate: $('#docsBankSignDate').val()
        };
    }

    //#endregion

    //#region Clear

    $(document).on('click', '#clearBtn', function (e) {
        e.preventDefault();
        clearForm(true);
        
    });

    function clearForm(clearJobNo) {
       
        $('#tc').val(0);
        setSelect('#shipmentModeId', '');
        $('.seaFieldsGroup, .airFieldsGroup').hide();
        $('#transportModeTypeId').val('');
        $('#isCustomJobNo').prop('checked', false);
        if (clearJobNo !== false) setField('#jobNo', '', true);

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
                    <td class="text-center">${d.tc}</td>
                    <td>${d.documentName || ''}</td>
                    <td>${d.description || ''}</td>
                    <td><a href="${d.filePath || '#'}" target="_blank">${d.fileName || ''}</a></td>
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

    $(document).on('change', '#shipmentModeId', function () {
        const shipmentModeId = $("#shipmentModeId").val();

        $('.seaFieldsGroup, .airFieldsGroup').hide();

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
                        $('.airFieldsGroup').css('display', 'block');
                        $('.seaFieldsGroup').css('display', 'none');
                    } else if (transportModeTypeId === '002') {
                        $('.seaFieldsGroup').css('display', 'block');
                        $('.airFieldsGroup').css('display', 'none');
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

            $('#customerAddress').val(customer.customerAddress);

            var deliveryItems = customer.delivaryAddress.map(function (d) {
                return {
                    id: d.delivaryId,
                    text: d.delivaryAddress
                };
            });

            setSelect(
                '#deliveryLocationCode',              [],
                false,
                deliveryItems,
                false,
                'Select Delivery Location(s)'
            );
        }
    }

    generateJobNo();

    function calculateLCValueBDT() {        
        var lcValue = parseFloat($('#lcValue').val()) || 0;
        var bdtRate = parseFloat($('#bdtRate').val()) || 0;

        var lcValueBDT = lcValue * bdtRate;

        $('#LCValueBDTAmount')
            .val(lcValueBDT ? lcValueBDT.toFixed(2) : '')
            .prop('readonly', true);
    }

    $('#lcValue, #bdtRate').on('input', calculateLCValueBDT);
    calculateLCValueBDT();

    function calculateAssessableValue() {
        var invoiceValue = parseFloat($('#invoiceValue').val()) || 0;
        var assessableRate = parseFloat($('#assessableRate').val()) || 0;

        var assessableValue = invoiceValue * assessableRate;

        $('#assessableValue')
            .val(assessableValue ? assessableValue.toFixed(2) : '')
            .prop('readonly', true);
    }

    $('#invoiceValue, #assessableRate').on('input', calculateAssessableValue);
    calculateAssessableValue();
});