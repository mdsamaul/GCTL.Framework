
(function ($) {
    'use strict';

    let customerTable = null;

    $(document).ready(function () {
        initDataTable();
        bindEvents();
        loadCustomerData();
    });

    function initDataTable() {
        // Destroy existing instance if any
        if ($.fn.DataTable.isDataTable('#seacustomerTable')) {
            $('#seacustomerTable').DataTable().destroy();
        }

        customerTable = $('#seacustomerTable').DataTable({
            processing: true,
            responsive: true,
            pageLength: 10,

            lengthMenu: [
                [10, 25, 50, 100, -1],
                [10, 25, 50, 100, "All"]
            ],

            order: [[0, 'asc']],
            autoWidth: false,

            language: {
                emptyTable: "No customer records found matching the filter criteria.",
                search: "_INPUT_",
                searchPlaceholder: "Search customers..."
            },

            columns: [
                {
                    data: 'sl',
                    className: 'text-center align-middle',
                    width: '35px'
                },
                {
                    data: 'customerId',
                    className: 'text-center align-middle job-no-cell',
                    width: '40px',
                    render: function (data) {
                        return escapeHtml(data);
                    }
                },
                {
                    data: 'name',
                    className: 'text-start align-middle',
                    width: '180px',
                    render: function (data) {
                        return '<span style="text-align:left; display:block;">' + escapeHtml(data) + '</span>';
                    }
                },
                {
                    data: 'address',
                    className: 'text-start align-middle',
                    width: '200px',
                    render: function (data) {
                        return '<span style="text-align:left; display:block;">' + escapeHtml(data) + '</span>';
                    }
                },
                {
                    data: 'phone',
                    className: 'text-center align-middle',
                    width: '130px'
                },
                {
                    data: 'fax',
                    className: 'text-center align-middle',
                    width: '120px'
                },
                {
                    data: 'email',
                    className: 'text-center align-middle',
                    width: '160px'
                },
                {
                    data: 'photo',
                    className: 'text-center align-middle',
                    width: '100px',
                    orderable: false,
                    render: function (data) {
                        if (data) {
                            return `
                <img src="data:image/jpeg;base64,${data}"
                     alt="Photo"
                     style="width:36px;
                            height:36px;
                            border-radius:6px;
                            object-fit:cover;" />
            `;
                        }

                        return '<span class="text-muted">-</span>';
                    }
                },
                {
                    data: 'contactPerson',
                    className: 'text-start align-middle',
                    width: '250px',
                    render: function (data) {
                        return '<span style="text-align:left; display:block;">' + escapeHtml(data) + '</span>';
                    }
                }
            ]
        });
    }

    function bindEvents() {
        // Search Button Click
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            loadCustomerData();
        });

        // Stat Card Click to Filter by Customer Type
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            // Text ("General"/"In House") কে dropdown এর actual value ("0"/"1") তে ম্যাপ করা
            var mappedValue = mapStatusToValue(statusFilter);
            setDropdownValue('filterCustomerType', mappedValue);
            loadCustomerData();
        });

        // Dropdown নিজে থেকে পরিবর্তন হলেও ডাটা রিলোড হবে
        $('#filterCustomerType, #filterCustomer').on('change', function () {
            loadCustomerData();
        });

        // Export Report Click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });
    }

    /**
     * Stat card এর টেক্সট ("General" / "In House") কে dropdown value ("0" / "1") এ কনভার্ট করে
     */
    function mapStatusToValue(statusFilter) {
        if (!statusFilter) return '';
        var s = statusFilter.toString().toLowerCase().trim();
        if (s.indexOf('house') !== -1) return '1';
        if (s.indexOf('general') !== -1) return '0';
        return statusFilter; // আগে থেকেই "0"/"1" হলে সরাসরি রিটার্ন
    }

    /**
     * Helper to safely extract value from custom-select-multi or custom-select-single
     */
    function getDropdownValue(id) {
        var $el = $('#' + id);
        if (!$el.length) return '';

        var inst = $el.data('customDdInstance');
        if (inst) {
            if (Array.isArray(inst.selected)) {
                return inst.selected.filter(function (v) { return !!v; }).join(',');
            }
            if (inst.selected !== undefined && inst.selected !== null) {
                return String(inst.selected).trim();
            }
        }

        var val = $el.val();
        if (Array.isArray(val)) {
            return val.filter(function (v) { return !!v; }).join(',');
        }

        return (val || '').trim();
    }

    /**
     * Helper to get dropdown value as an array (for multi-select filters expecting List<string>)
     */
    function getDropdownValueArray(id) {
        var raw = getDropdownValue(id);
        if (!raw) return [];
        return raw.split(',').filter(function (v) { return !!v; });
    }

    /**
     * Helper to set value of a dropdown
     */
    function setDropdownValue(id, value) {
        var $el = $('#' + id);
        if (!$el.length) return;

        $el.val(value);
        var inst = $el.data('customDdInstance');
        if (inst && typeof inst.setSelected === 'function') {
            inst.setSelected(value);
        }
        $el.trigger('change');
    }

    function getFilterParams() {
        return {
            CustomerId: getDropdownValueArray('filterCustomer'),
            CustomerTypeId: getDropdownValueArray('filterCustomerType')
        };
    }

    function loadCustomerData() {
        var filters = getFilterParams();

        // Highlight matching stat card
        highlightStatCard(filters.CustomerTypeId);

        if (customerTable) {
            customerTable.processing(true);
        }

        $.ajax({
            url: '/CustomerReport/GetData',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(filters),
            success: function (res) {
                if (res && res.success) {
                    animateCounter('#statTotalcustomer', res.total || 0);
                    animateCounter('#statinHouse', res.inHouse || 0);
                    animateCounter('#statgeneral', res.general || 0);
                    animateCounter('#statCustomerType', res.customerType || 0);

                    var data = res.data || [];
                    if (customerTable) {
                        customerTable.clear().rows.add(data).draw();
                    }

                    setTimeout(function () {
                        if ($.fn.DataTable) {
                            $.fn.DataTable.tables({ visible: true, api: true }).columns.adjust();
                        }
                    }, 100);
                } else {
                    clearTable();
                    showError(res ? res.message : 'Failed to retrieve data.');
                }
            },
            error: function () {
                clearTable();
                showError('Error connecting to server. Please try again.');
            },
            complete: function () {
                if (customerTable) customerTable.processing(false);
            }
        });
    }

    function clearTable() {
        if (customerTable) customerTable.clear().draw();
    }

    function highlightStatCard(customerTypeArr) {
        $('.stat-card').removeClass('active-card');
        var s = (Array.isArray(customerTypeArr) && customerTypeArr.length ? customerTypeArr[0] : '').toLowerCase().trim();

        if (!s) {
            $('.stat-card[data-status-filter=""]').addClass('active-card');
        } else if (s.indexOf('house') !== -1 || s === '1') {
            $('.stat-card[data-status-filter="In House"]').addClass('active-card');
        } else if (s.indexOf('general') !== -1 || s === '0') {
            $('.stat-card[data-status-filter="General"]').addClass('active-card');
        } else {
            $('.stat-card[data-status-filter="Customer Type"]').addClass('active-card');
        }
    }

    function animateCounter(selector, target) {
        var $el = $(selector);
        var current = parseInt($el.text().replace(/,/g, ''), 10) || 0;
        var duration = 500;
        var steps = 20;
        var stepTime = duration / steps;
        var diff = target - current;

        var step = 0;
        var timer = setInterval(function () {
            step++;
            var val = Math.round(current + (diff * (step / steps)));
            $el.text(val.toLocaleString());
            if (step >= steps) {
                clearInterval(timer);
                $el.text(target.toLocaleString());
            }
        }, stepTime);
    }

    function exportReport() {
        var filters = getFilterParams();
        var exportFormat = $('#filterExportFormat').val() || 'PDF';

        var queryParams = $.param({
            CustomerId: filters.CustomerId,
            CustomerTypeId: filters.CustomerTypeId,
            exportFormat: exportFormat
        }, true); // traditional: true so arrays serialize correctly for List<string> binding

        window.location.href = '/CustomerReport/Export?' + queryParams;
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