
(function ($) {
    'use strict';

    let customerTable = null;
    let suppressReload = false;   // customer dropdown rebuild er somoy extra reload atkabe
    let lookupReq = 0;            // purono ajax response ignore korar jonno

    // Register renderers for CustomGrid
    window.gctlRenderers = window.gctlRenderers || {};

    window.gctlRenderers.renderPhoto = function (data) {
        if (data) {
            var src = (data.startsWith('/') || data.startsWith('http') || data.startsWith('data:'))
                ? data
                : 'data:image/jpeg;base64,' + data;
            return '<img src="' + src + '" alt="Photo" loading="lazy" class="js-customer-photo-preview" data-src="' + src + '" title="Click to view large photo" style="width:36px;height:36px;border-radius:6px;object-fit:cover;cursor:pointer;transition:transform 0.15s ease,box-shadow 0.15s ease;" onmouseover="this.style.transform=\'scale(1.15)\';this.style.boxShadow=\'0 2px 8px rgba(0,0,0,0.25)\';" onmouseout="this.style.transform=\'scale(1)\';this.style.boxShadow=\'none\';" onerror="this.onerror=null;this.parentElement.innerHTML=\'<span class=\\\'text-muted\\\'>-</span>\';" />';
        }
        return '<span class="text-muted">-</span>';
    };

    window.gctlRenderers.renderContactPerson = function (data) {
        return '<span style="text-align:left; display:block; white-space:pre-line;">' + escapeHtml(data) + '</span>';
    };

    window.gctlRenderers.renderAlignLeft = function (data) {
        return '<span style="text-align:left; display:block;">' + escapeHtml(data) + '</span>';
    };

    window.gctlRenderers.renderCustomerId = function (data) {
        return '<span class="job-no-cell">' + escapeHtml(data) + '</span>';
    };

    $(document).ready(function () {
        initGrid();
        bindEvents();
    });

    function initGrid() {

        if ($.fn.dataTable) {
            $.extend(true, $.fn.dataTable.defaults, { autoWidth: false });
        }

        if ($.gctlGrid && typeof $.gctlGrid.initFromModel === 'function') {
            customerTable = $.gctlGrid.initFromModel({
                gridId: 'customerReportTable',
                ajaxData: function (d) {
                    var filters = getFilterParams();
                    d.customerId = filters.CustomerId;
                    d.customerTypeId = filters.CustomerTypeId;
                },
                onLoaded: function (res) {
                    if (res && res.success) {
                        animateCounter('#statTotalcustomer', res.total || 0);
                        animateCounter('#statinHouse', res.inHouse || 0);
                        animateCounter('#statgeneral', res.general || 0);
                        animateCounter('#statCustomerType', res.customerType || 0);
                    }
                }
            });
        }
    }

    function bindEvents() {
        // Search Button Click
        $('#btnSearch').on('click', function (e) {
            e.preventDefault();
            reloadGrid();
        });

        // Stat Card Click: shudhu dropdown set korbe, reload change handler nije korbe
        $('.stat-card').on('click', function () {
            var statusFilter = $(this).data('status-filter') || '';
            $('.stat-card').removeClass('active-card');
            $(this).addClass('active-card');

            var mappedValue = mapStatusToValue(statusFilter);
            setDropdownValue('filterCustomerType', mappedValue);
            // ekhane ar reloadGrid() nai, na hole double reload hoto
        });

        // Customer Type change: age customer dropdown reload, tarpor grid reload (ek bar)
        $('#filterCustomerType').on('change', function () {
            if (suppressReload) return;
            loadCustomersByType().always(function () {
                reloadGrid();
            });
        });

        // Customer change: shudhu grid reload
        $('#filterCustomer').on('change', function () {
            if (suppressReload) return;
            reloadGrid();
        });

        // Export Report Click
        $('#btnExport').on('click', function (e) {
            e.preventDefault();
            exportReport();
        });

        // Photo Preview Modal on Click
        $(document).on('click', '.js-customer-photo-preview', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var src = $(this).attr('data-src') || $(this).attr('src');
            if (src) {
                $('#customerPhotoModalImg').attr('src', src);
                var modalEl = document.getElementById('customerPhotoModal');
                if (modalEl) {
                    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
                    modal.show();
                }
            }
        });
    }

    function reloadGrid() {
        var filters = getFilterParams();
        highlightStatCard(filters.CustomerTypeId);

        if ($.gctlGrid && typeof $.gctlGrid.reload === 'function') {
            $.gctlGrid.reload('customerReportTable', true);
        } else if (customerTable && customerTable.ajax) {
            customerTable.ajax.reload(null, true);
        }
    }

    /* ---------- Cascading: Customer Type -> Customer dropdown ---------- */

    function loadCustomersByType() {
        var types = getDropdownValueArray('filterCustomerType');
        var reqId = ++lookupReq;

        return $.ajax({
            url: '/CustomerReport/GetCustomersByType',
            type: 'GET',
            data: { customerTypeId: types },
            traditional: true   // List<string> binding er jonno
        }).done(function (items) {
            if (reqId !== lookupReq) return; // purono response ignore
            rebuildCustomerDropdown(items || []);
        }).fail(function () {
            showError('Failed to load customers.');
        });
    }

    function rebuildCustomerDropdown(items) {
        var $el = $('#filterCustomer');
        if (!$el.length) return;

        suppressReload = true; // selection clear hole extra reload na hoy
        try {
            var inst = $el.data('customDdInstance');

            // 1) plugin er UI age destroy koro (jodi method thake)
            if (inst) {
                var destroyFn = ['destroy', 'dispose', 'remove']
                    .find(function (n) { return typeof inst[n] === 'function'; });
                if (destroyFn) {
                    try { inst[destroyFn](); } catch (e) { console.warn('destroy failed', e); }
                }
            }

            // 2) native select er option gulo notun kore boshao
            $el.empty();
            (items || []).forEach(function (i) {
                $el.append($('<option>').val(i.value).text(i.text));
            });
            $el.val(null);
            $el.removeData('customDdInstance');

            // 3) plugin ke abar init koro
            var initFn = window.initCustomDropdowns || window.initCustomSelect || window.initCustomSelects;
            if (typeof initFn === 'function') {
                initFn($el.parent());
            } else if (inst) {
                // init function na thakle purono instance er refresh method try koro
                inst.selected = [];
                var m = ['refresh', 'rebuild', 'reload', 'update', 'render', 'buildOptions', 'init']
                    .find(function (n) { return typeof inst[n] === 'function'; });
                if (m) inst[m]();
                else console.warn('customDdInstance e refresh method nai. Keys:', Object.keys(inst));
            }
        } finally {
            suppressReload = false;
        }
    }

    /* ---------- Helpers ---------- */

    /* Stat card text ("General" / "In House") converted to dropdown value ("0" / "1") */
    function mapStatusToValue(statusFilter) {
        if (!statusFilter) return '';
        var s = statusFilter.toString().toLowerCase().trim();
        if (s.indexOf('house') !== -1) return '1';
        if (s.indexOf('general') !== -1) return '0';
        return statusFilter;
    }

    /* Helper to safely extract value from custom-select-multi or custom-select-single */
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

    /* Helper to get dropdown value as an array (for multi-select filters expecting List<string>) */
    function getDropdownValueArray(id) {
        var raw = getDropdownValue(id);
        if (!raw) return [];
        return raw.split(',').filter(function (v) { return !!v; });
    }

    /* Helper to set value of a dropdown */
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
        if (!$el.length) return;

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

        // Collect checked customer IDs from CustomGrid checkboxes
        var selectedCustomerIds = [];
        $('#customerReportTable tbody input.chk-customer-item:checked, #customerReportTable tbody input[type="checkbox"]:checked').each(function () {
            var val = $(this).val() || $(this).data('id');
            if (val) {
                selectedCustomerIds.push(val);
            }
        });

        var queryParams = $.param({
            CustomerId: filters.CustomerId,
            CustomerTypeId: filters.CustomerTypeId,
            SelectedCustomerIds: selectedCustomerIds,
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
