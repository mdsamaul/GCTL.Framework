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

    function getDeleteModal() {
        var $m = $("#sepdoc-delete-modal");
        if ($m.length) return $m;
        $m = $("#deleteModal");
        if ($m.length) return $m;
        return $("#gcDeleteModal");
    }

    function showDeleteModal($modal) {
        if (window.bootstrap && bootstrap.Modal) {
            var inst = bootstrap.Modal.getOrCreateInstance($modal[0]);
            inst.show();
        } else {
            $modal.modal('show');
        }
    }

    function hideDeleteModal($modal) {
        if (window.bootstrap && bootstrap.Modal) {
            var inst = bootstrap.Modal.getInstance($modal[0]);
            if (inst) inst.hide();
            else $modal.modal('hide');
        } else {
            $modal.modal('hide');
        }
    }

    let pendingDeleteType = null; // 'jobs' or 'document'
    let pendingDeleteIds = [];
    let pendingDocTc = null;
    let pendingOnSuccess = null;

    $(document).on('click', '.doc-delete-entry-btn', function (e) {
        e.preventDefault();

        // Check if this is the form delete button
        const isFormDelete = $(this).closest('.entry-only').length > 0;
        const tc = $('#tc').val();
        const jobNo = $('#jobNo').val();

        pendingDeleteType = 'jobs';
        pendingDeleteIds = [];
        pendingDocTc = null;
        pendingOnSuccess = null;

        if (isFormDelete) {
            if (!tc || tc === '0') {
                toastr.warning('Please select a job first to delete');
                return;
            }

            pendingDeleteIds = [tc];
            pendingOnSuccess = function () {
                clearForm(true);
            };
        } else {
            // Check bottom grid selection first, fallback to form if active
            if (selectedBottomIds.length > 0) {
                pendingDeleteIds = [...selectedBottomIds];
            } else if (tc && tc !== '0') {
                pendingDeleteIds = [tc];
                pendingOnSuccess = function () {
                    clearForm(true);
                };
            } else {
                toastr.info('Please select at least one item.');
                return;
            }
        }

        var $modal = getDeleteModal();
        var msg = pendingDeleteIds.length > 1
            ? `Are you sure you want to delete these ${pendingDeleteIds.length} item(s)? This process cannot be undone.`
            : (jobNo && pendingDeleteIds[0] == tc ? `Are you sure you want to delete Job No "${jobNo}"? This process cannot be undone.` : 'Are you sure you want to delete these item(s)? This process cannot be undone.');

        $modal.find('.title').html(msg);
        showDeleteModal($modal);
    });

    // Single trash icon delete
    $(document).on('click', '.single-delete-btn', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        if (!id) return;

        pendingDeleteType = 'jobs';
        pendingDeleteIds = [String(id)];
        pendingDocTc = null;
        pendingOnSuccess = null;

        var $modal = getDeleteModal();
        $modal.find('.title').html('Are you sure you want to delete this record? This process cannot be undone.');
        showDeleteModal($modal);
    });

    $(document).on('click', '.js-sepdoc-delete, #sepdoc-delete-modal .js-delete, #sepdoc-delete-modal .js-gc-delete-confirm', function (e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();

        var $modal = getDeleteModal();
        hideDeleteModal($modal);

        if (pendingDeleteType === 'document') {
            const docCodes = pendingDeleteIds.length > 0 ? [...pendingDeleteIds] : (pendingDocTc ? [String(pendingDocTc)] : []);
            pendingDeleteIds = [];
            pendingDocTc = null;
            pendingDeleteType = null;

            if (docCodes.length === 0) return;

            $.ajax({
                url: '/SepDocUpdate/DeleteDocuments',
                type: 'POST',
                data: JSON.stringify({ docCodes: docCodes }),
                contentType: 'application/json',
                headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        const jobNo = $('#uploadJobNo').val() || currentJobNo;
                        loadDocuments(jobNo);
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Error occurred while deleting document');
                }
            });
            return;
        }

        if (pendingDeleteType === 'jobs' && pendingDeleteIds.length > 0) {
            const ids = [...pendingDeleteIds];
            const onSuccess = pendingOnSuccess;
            pendingDeleteIds = [];
            pendingOnSuccess = null;
            pendingDeleteType = null;

            deleteJobsByIds(ids, onSuccess);
        }
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
                    updateDeleteSelectedButton();
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
        $('#uploadJobNo').val(data.jobNo || '').trigger('change.select2');
        $('#uploadJobNoDisplay').val(data.jobNo || '');
        $('#uploadJobDateDisplay').val(data.date ? new Date(data.date).toLocaleDateString('en-GB') : '');
        $('#uploadCustomerDisplay').val(data.customerName || data.customerId || '');
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
        $('#sepDocForm .custom-select-single').val('').trigger('change.select2');
    }

    //#endregion

    //#region Document Upload tab
    let currentJobNo;

    $(document).on('shown.bs.tab', '#upload-tab', function () {
        const selectedJob = $('#uploadJobNo').val() || currentJobNo;
        if (selectedJob) {
            if ($('#uploadJobNo').val() !== selectedJob) {
                $('#uploadJobNo').val(selectedJob).trigger('change.select2');
            } else {
                loadDocuments(selectedJob);
            }
        }
    });

    // When Job No is changed in Document Upload Info tab
    $(document).on('change', '#uploadJobNo', function () {
        debugger
        const jobNo = $(this).val();
        currentJobNo = jobNo;
        if (!jobNo) {
            $('#uploadJobDateDisplay').val('');
            $('#uploadCustomerDisplay').val('');
            $('#uploadAddressDisplay').val('');
            $('#docEntryDate').text('');
            $('#docModifyDate').text('');
            renderDocumentTable([]);
            return;
        }

        // Fetch job details (date, customer, address)
        $.ajax({
            url: '/SepDocUpdate/GetJobDetailsForUpload',
            type: 'GET',
            data: { jobNo: jobNo },
            success: function (res) {
                debugger
                if (res.success) {
                    $('#uploadJobDateDisplay').val(res.date || '');
                    $('#uploadCustomerDisplay').val(res.customerName || '');
                    $('#uploadAddressDisplay').val(res.customerAddress || '');
                } else {
                    $('#uploadJobDateDisplay').val('');
                    $('#uploadCustomerDisplay').val('');
                    $('#uploadAddressDisplay').val('');
                }
            },
            error: function () {
                console.error('Error fetching job details for upload');
            }
        });

        loadDocuments(jobNo);
    });

    function loadDocuments(jobNo) {
        document
        if (!jobNo) {
            renderDocumentTable([]);
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
        $('#checkAllDocs').prop('checked', false);

        if (!data || data.length === 0) {
            rows = '<tr><td colspan="5" class="text-center py-3 text-muted">No data available</td></tr>';
            $('#docEntryDate').text('');
            $('#docModifyDate').text('');
        } else {
            let lastItem = data[data.length - 1];
            $('#docEntryDate').text(lastItem.lDate || '');
            $('#docModifyDate').text(lastItem.modifyDate || '');

            data.forEach((d, idx) => {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="doc-item-check form-check-input" value="${d.docCode}" data-autoid="${d.autoId}" />
                    </td>
                    <td class="text-center fw-bold">${idx + 1}</td>
                    <td>${d.documentName || ''}</td>
                    <td>${d.documentDiscription || ''}</td>
                    <td class="text-center">
                        <a href="javascript:void(0);" class="preview-doc-btn text-danger fw-bold text-decoration-underline" 
                           data-file="${d.filePath}" 
                           data-filename="${d.documentType}" 
                           data-docname="${d.documentName || ''}">
                           ${d.documentType || 'View File'}
                        </a>
                    </td>
                </tr>`;
            });
        }
        $('#document-list-body').html(rows);
    }

    // Select all checkboxes in document table
    $(document).on('change', '#checkAllDocs', function () {
        const isChecked = $(this).is(':checked');
        $('#documentTable tbody .doc-item-check').prop('checked', isChecked);
    });

    $(document).on('change', '#documentTable tbody .doc-item-check', function () {
        const total = $('#documentTable tbody .doc-item-check').length;
        const checked = $('#documentTable tbody .doc-item-check:checked').length;
        $('#checkAllDocs').prop('checked', total > 0 && total === checked);
    });

    // Preview Document Modal
    $(document).on('click', '.preview-doc-btn', function (e) {
        e.preventDefault();
        const filePath = $(this).data('file');
        const fileName = $(this).data('filename');
        const docName = $(this).data('docname');

        if (!filePath) {
            toastr.warning('No file path available.');
            return;
        }

        $('#docPreviewModalLabel').html(`<i class="fas fa-file-alt text-primary me-2"></i>Preview: ${docName || fileName}`);
        $('#docPreviewFileInfo').text(fileName);
        $('#docPreviewDownloadBtn').attr('href', filePath).attr('download', fileName);

        const ext = (fileName || '').split('.').pop().toLowerCase();
        let contentHtml = '';

        if (ext === 'pdf') {
            contentHtml = `<iframe src="${filePath}" style="width: 100%; height: 72vh; border: none; border-radius: 8px;"></iframe>`;
        } else if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg'].includes(ext)) {
            contentHtml = `<div class="p-3"><img src="${filePath}" class="img-fluid rounded shadow-sm" style="max-height: 72vh; object-fit: contain;" alt="${fileName}" /></div>`;
        } else {
            contentHtml = `<div class="py-5 text-center">
                <i class="fas fa-file fa-4x text-secondary mb-3"></i>
                <p class="text-muted">Preview is not available for this file type (<b>.${ext}</b>).</p>
                <a href="${filePath}" download="${fileName}" class="btn btn-primary"><i class="fas fa-download me-1"></i> Download File</a>
            </div>`;
        }

        $('#docPreviewContainer').html(contentHtml);

        if (window.bootstrap && bootstrap.Modal) {
            bootstrap.Modal.getOrCreateInstance(document.getElementById('docPreviewModal')).show();
        } else {
            $('#docPreviewModal').modal('show');
        }
    });

    // Upload & Save Document
    $(document).on('click', '.doc-seve-upload-btn, #uploadDocumentBtn', function (e) {
        e.preventDefault();
        debugger
        const jobNo = $('#uploadJobNo').val() || currentJobNo;
        if (!jobNo) {
            toastr.warning('Please select a Job No first.');
            $('#uploadJobNo').focus();
            return;
        }

        const fileInput = $('#uploadFile')[0];
        if (!fileInput || !fileInput.files || !fileInput.files.length) {
            toastr.warning('Please choose a file to upload.');
            $('#uploadFile').focus();
            return;
        }

        const docName = $('#documentName').val();
        if (!docName || !docName.trim()) {
            toastr.warning('Document Name is required.');
            $('#documentName').focus();
            return;
        }

        const formData = new FormData();
        formData.append('JobNo', jobNo);
        formData.append('DocumentName', docName.trim());
        formData.append('Description', ($('#documentDescription').val() || '').trim());
        formData.append('uploadFile', fileInput.files[0]);

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/SepDocUpdate/UploadDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: token ? { 'RequestVerificationToken': token } : {},
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message || 'Document uploaded successfully.');
                    $('#uploadFile').val('');
                    $('#documentName').val('');
                    $('#documentDescription').val('');
                    loadDocuments(jobNo);
                } else {
                    toastr.warning(response.message || 'Failed to upload document.');
                }
            },
            error: function () {
                toastr.error('Error occurred while uploading');
            }
        });
    });

    // Delete Document(s) from Upload tab
    $(document).on('click', '.doc-delete-upload-btn', function (e) {
        e.preventDefault();

        const selectedCodes = [];
        $('#documentTable tbody .doc-item-check:checked').each(function () {
            const val = $(this).val();
            if (val) selectedCodes.push(val);
        });

        if (selectedCodes.length === 0) {
            toastr.warning('Please select at least one document to delete.');
            return;
        }

        pendingDeleteType = 'document';
        pendingDeleteIds = selectedCodes;
        pendingDocTc = null;

        var $modal = getDeleteModal();
        $modal.find('.title').html(`Are you sure you want to delete ${selectedCodes.length} selected document(s)? This process cannot be undone.`);
        showDeleteModal($modal);
    });

    // Clear Document Upload inputs
    $(document).on('click', '.doc-clear-upload-btn', function (e) {
        e.preventDefault();
        $('#uploadFile').val('');
        $('#documentName').val('');
        $('#documentDescription').val('');
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