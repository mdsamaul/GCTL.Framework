function ajaxRequest(options) {
    var settings = $.extend({
        url: '',
        method: 'GET',
        data: {},
        contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        async: true,
        targetSelector: null,
        targetProperty: 'val',
        targetAttr: null,
        loaderSelector: null,
        disableSelector: null,
        showSuccessToastr: false,
        showErrorToastr: true,
        confirmBefore: false,
        confirmMessage: 'Are you sure?',
        resetFormSelector: null,
        redirectUrl: null,
        successCallback: null,
        errorCallback: null,
        completeCallback: null,
        beforeSend: null
    }, options);

    function execute() {

        if (settings.disableSelector) {
            $(settings.disableSelector).prop('disabled', true);
        }
        if (settings.loaderSelector) {
            $(settings.loaderSelector).show();
        }

        return $.ajax({
            url: settings.url,
            type: settings.method,
            data: settings.data,
            contentType: settings.method === 'GET' ? undefined : settings.contentType,
            async: settings.async,
            beforeSend: function (xhr) {
                if (typeof settings.beforeSend === 'function') {
                    settings.beforeSend(xhr);
                }
            },
            success: function (response) {

                if (settings.targetSelector) {
                    var $target = $(settings.targetSelector);

                    switch (settings.targetProperty) {
                        case 'val':
                            $target.val(response);
                            break;
                        case 'html':
                            $target.html(response);
                            break;
                        case 'text':
                            $target.text(response);
                            break;
                        case 'append':
                            $target.append(response);
                            break;
                        case 'prepend':
                            $target.prepend(response);
                            break;
                        case 'attr':
                            if (settings.targetAttr) {
                                $target.attr(settings.targetAttr, response);
                            }
                            break;
                        case 'prop':
                            if (settings.targetAttr) {
                                $target.prop(settings.targetAttr, response);
                            }
                            break;
                    }
                }

                if (settings.showSuccessToastr && response && response.message && typeof toastr !== 'undefined') {
                    toastr.success(response.message);
                }

                if (settings.resetFormSelector) {
                    $(settings.resetFormSelector)[0].reset();
                }

                if (settings.redirectUrl) {
                    window.location.href = settings.redirectUrl;
                }

                if (typeof settings.successCallback === 'function') {
                    settings.successCallback(response);
                }
            },
            error: function (xhr) {
                console.error("AJAX Error [" + settings.method + " " + settings.url + "]:", xhr.responseText);

                if (settings.showErrorToastr && typeof toastr !== 'undefined') {
                    var errMsg = 'Something went wrong!';
                    try {
                        var parsed = JSON.parse(xhr.responseText);
                        errMsg = parsed.message || parsed.Message || errMsg;
                    } catch (e) {
                        if (xhr.status === 403) {
                            errMsg = 'You do not have permission to perform this action.';
                        }
                    }
                    toastr.error(errMsg);
                }

                if (typeof settings.errorCallback === 'function') {
                    settings.errorCallback(xhr);
                }
            },
            complete: function (xhr) {
                if (settings.disableSelector) {
                    $(settings.disableSelector).prop('disabled', false);
                }
                if (settings.loaderSelector) {
                    $(settings.loaderSelector).hide();
                }
                if (typeof settings.completeCallback === 'function') {
                    settings.completeCallback(xhr);
                }
            }
        });
    }

    if (settings.confirmBefore) {
        if (typeof Swal !== 'undefined') {
            return Swal.fire({
                title: settings.confirmMessage,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'Cancel'
            }).then(function (result) {
                if (result.isConfirmed) {
                    return execute();
                }
                return $.Deferred().reject('cancelled').promise();
            });
        } else if (confirm(settings.confirmMessage)) {
            return execute();
        }
        return $.Deferred().reject('cancelled').promise();
    } else {
        return execute();
    }
}


// async function loadCustomerInfo(customerId) {
//     var response = await ajaxRequest({
//         url: '/SepDocUpdate/CustomerInfo',
//         method: 'GET',
//         data: { customerId: customerId }
//     });

//     console.log(response);
//     $('#customerAddress').val(response.address);
// }

// loadCustomerInfo(customerId);
// ajaxRequest({
//     url: '/SepDocUpdate/CustomerInfo',
//     method: 'GET',
//     data: { customerId: customerId },
//     successCallback: function (response) {
//         console.log(response);
//         $('#customerAddress').val(response.address);
//     }
// });

// // 1. Simple GET - value bosano
// ajaxRequest({
//     url: '/next-shipment-status-id',
//     method: 'GET',
//     targetSelector: '#shipmentStatusId',
//     targetProperty: 'val'
// });

// // 2. GET with query data
// ajaxRequest({
//     url: '/shipment-details',
//     method: 'GET',
//     data: { id: shipmentId },
//     targetSelector: '#shipmentDetailBox',
//     targetProperty: 'html'
// });

// // 3. Simple POST - form data soho
// ajaxRequest({
//     url: '/save-shipment-status',
//     method: 'POST',
//     data: { statusId: 1, remarks: 'Delivered' },
//     successCallback: function (response) {
//         console.log(response);
//     }
// });

// // 4. POST with FormData (file upload)
// var formData = new FormData($('#uploadForm')[0]);
// ajaxRequest({
//     url: '/upload-document',
//     method: 'POST',
//     data: formData,
//     contentType: false,
//     successCallback: function (response) {
//         toastr.success('Uploaded successfully');
//     }
// });

// // 5. POST with confirm dialog (delete)
// ajaxRequest({
//     url: '/shipment/delete/' + id,
//     method: 'POST',
//     confirmBefore: true,
//     confirmMessage: 'Delete this shipment?',
//     showSuccessToastr: true,
//     successCallback: function (response) {
//         $('#shipmentTable').DataTable().ajax.reload();
//     }
// });

// // 6. POST with loader + disable button
// ajaxRequest({
//     url: '/save-shipment',
//     method: 'POST',
//     data: $('#shipmentForm').serialize(),
//     loaderSelector: '#pageLoader',
//     disableSelector: '#saveBtn',
//     showSuccessToastr: true,
//     resetFormSelector: '#shipmentForm'
// });

// // 7. POST with redirect on success
// ajaxRequest({
//     url: '/shipment/create',
//     method: 'POST',
//     data: $('#createForm').serialize(),
//     redirectUrl: '/shipment/list'
// });

// // 8. targetProperty: html
// ajaxRequest({
//     url: '/get-summary',
//     method: 'GET',
//     targetSelector: '#summaryBox',
//     targetProperty: 'html'
// });

// // 9. targetProperty: text
// ajaxRequest({
//     url: '/get-count',
//     method: 'GET',
//     targetSelector: '#totalCount',
//     targetProperty: 'text'
// });

// // 10. targetProperty: append
// ajaxRequest({
//     url: '/get-more-rows',
//     method: 'GET',
//     targetSelector: '#tableBody',
//     targetProperty: 'append'
// });

// // 11. targetProperty: prepend
// ajaxRequest({
//     url: '/get-latest-notification',
//     method: 'GET',
//     targetSelector: '#notificationList',
//     targetProperty: 'prepend'
// });

// // 12. targetProperty: attr - image src set
// ajaxRequest({
//     url: '/get-image-url',
//     method: 'GET',
//     targetSelector: '#profileImg',
//     targetProperty: 'attr',
//     targetAttr: 'src'
// });

// // 13. targetProperty: prop - disable/enable
// ajaxRequest({
//     url: '/check-status',
//     method: 'GET',
//     targetSelector: '#actionBtn',
//     targetProperty: 'prop',
//     targetAttr: 'disabled'
// });

// // 14. errorCallback diye custom error handling (403 ityadi)
// ajaxRequest({
//     url: '/restricted-action',
//     method: 'POST',
//     data: { id: 5 },
//     errorCallback: function (xhr) {
//         if (xhr.status === 403) {
//             window.location.href = '/access-denied';
//         }
//     }
// });

// // 15. beforeSend diye header set
// ajaxRequest({
//     url: '/secure-data',
//     method: 'GET',
//     beforeSend: function (xhr) {
//         xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
//     },
//     successCallback: function (response) {
//         console.log(response);
//     }
// });

// // 16. completeCallback (success/error dutoteo cholbe)
// ajaxRequest({
//     url: '/process-data',
//     method: 'POST',
//     data: { id: 10 },
//     completeCallback: function (xhr) {
//         $('#loadingSpinner').hide();
//     }
// });

// // 17. All options ekshathe (full example)
// ajaxRequest({
//     url: '/shipment/update-status',
//     method: 'POST',
//     data: { shipmentId: 10, statusId: 3 },
//     confirmBefore: true,
//     confirmMessage: 'Update shipment status?',
//     loaderSelector: '#pageLoader',
//     disableSelector: '#updateBtn',
//     showSuccessToastr: true,
//     showErrorToastr: true,
//     targetSelector: '#currentStatusLabel',
//     targetProperty: 'text',
//     successCallback: function (response) {
//         console.log('Updated:', response);
//     },
//     errorCallback: function (xhr) {
//         console.error('Failed:', xhr);
//     },
//     completeCallback: function () {
//         console.log('Request finished');
//     }
// });