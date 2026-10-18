$(function () {

    function getItems($el) {
        var items = [];
        $el.find('option').each(function () {
            var val = $(this).val();
            if (!val) return;
            var group = $(this).parent('optgroup').attr('label') || '';
            items.push({
                id: val,
                text: $(this).text(),
                group: group,
                selected: $(this).is(':selected')
            });
        });
        return items;
    }

    function destroyInstance($el) {
        var instance = $el.data('customDdInstance');
        if (instance) {
            if (typeof instance.destroy === 'function') {
                instance.destroy();
            } else {
                // fallback: remove generated wrapper if plugin has no destroy()
                $el.nextAll('.custom-dd-wrapper').first().remove();
                $el.show();
            }
        }
        $el.removeData('customDdInstance');
    }

    function initSingle($el, forceReinit) {
        if ($el.data('customDdInstance')) {
            if (!forceReinit) return $el.data('customDdInstance');
            destroyInstance($el);
        }

        var ajaxUrl = $el.data('ajax-url') || $el.data('ajaxUrl') || $el.attr('data-ajax-url');
        var isAjax = !!ajaxUrl;
        var pageSize = parseInt($el.data('page-size') || $el.attr('data-page-size') || 10, 10);

        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true,
            isAjax: isAjax,
            ajaxUrl: ajaxUrl,
            pageSize: pageSize,
            items: isAjax ? null : getItems($el)
        };

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        var elId = $el.attr('id');
        if (!elId) {
            elId = 'csd_' + Math.random().toString(36).substring(2, 9);
            $el.attr('id', elId);
        }

        var instance = new customDropdownSingle('#' + elId, options).init();
        $el.data('customDdInstance', instance);
        return instance;
    }

    function initMulti($el, forceReinit) {
        if ($el.data('customDdInstance')) {
            if (!forceReinit) return $el.data('customDdInstance');
            destroyInstance($el);
        }

        var ajaxUrl = $el.data('ajax-url') || $el.data('ajaxUrl') || $el.attr('data-ajax-url');
        var isAjax = !!ajaxUrl;
        var pageSize = parseInt($el.data('page-size') || $el.attr('data-page-size') || 10, 10);

        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true,
            copyAsJsonArray: true,
            isAjax: isAjax,
            ajaxUrl: ajaxUrl,
            pageSize: pageSize,
            items: isAjax ? null : getItems($el)
        };

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        if ($el.data('count-threshold') !== undefined) {
            options.countThreshold = parseInt($el.data('count-threshold'), 10);
        }

        var elMultiId = $el.attr('id');
        if (!elMultiId) {
            elMultiId = 'cmd_' + Math.random().toString(36).substring(2, 9);
            $el.attr('id', elMultiId);
        }

        var instance = new customDropdownMulti('#' + elMultiId, options).init();
        $el.data('customDdInstance', instance);
        return instance;
    }

    // data-placeholder attribute-based select gulo (jemon _DynamicFieldColumn theke asha
    // select, .custom-select-single/.custom-select-multi class chara) initialize korar jonno.
    // <select multiple> attribute thakle multi, na thakle single hisebe init hobe.
    function initDynamicPlaceholderSelects($container, forceReinit) {
        $container = $container ? $($container) : $(document);

        $container.find('select[data-placeholder]').each(function () {
            var $el = $(this);

            // custom-select-single/custom-select-multi or custom-table-dropdown class thakle upore already handle hoyeche, skip
            if ($el.hasClass('custom-select-single') || $el.hasClass('custom-select-multi') || $el.hasClass('custom-table-dropdown') || $el.data('table-view') === true || $el.data('tableView') === true) return;

            if ($el.data('customDdInstance')) {
                if (!forceReinit) return;
                destroyInstance($el);
            }

            var isMulti = $el.prop('multiple');

            if (isMulti) {
                initMulti($el, forceReinit);
            } else {
                initSingle($el, forceReinit);
            }
        });
    }

    // Container er moddhe shob single/multi select initialize/reinitialize kore
    // $container: jekono jQuery element/selector (default: document)
    // forceReinit: true dile already-init hoye thakleo notun kore init hobe (AJAX load er por use korun)
    function initCustomDropdowns($container, forceReinit) {
        $container = $container ? $($container) : $(document);

        $container.find('.custom-select-single').each(function () {
            initSingle($(this), forceReinit);
        });

        $container.find('.custom-select-multi').each(function () {
            initMulti($(this), forceReinit);
        });

        // notun: data-placeholder based select gulo (jemon deliveryLocationCode)
        initDynamicPlaceholderSelects($container, forceReinit);
    }

    // Ekta single select ke id diye reinit korar jonno (dorkar hole)
    function reinitById(id) {
        if (!id) return;
        if (id.startsWith('#')) id = id.substring(1);
        var $el = $('#' + id);
        if (!$el.length) return;
        if ($el.hasClass('custom-select-multi') || $el.prop('multiple')) {
            initMulti($el, true);
        } else {
            initSingle($el, true);
        }
    }

    // Ekta select clear korar jonno (id diye)
    function clearById(id) {
        if (!id) return;
        if (id.startsWith('#')) id = id.substring(1);
        var $el = $('#' + id);
        if (!$el.length) return;
        var instance = $el.data('customDdInstance');
        if (instance && typeof instance.clear === 'function') {
            instance.clear();
        } else {
            // fallback: value reset kore reinit
            $el.val('');
            if ($el.hasClass('custom-select-multi') || $el.prop('multiple')) {
                initMulti($el, true);
            } else {
                initSingle($el, true);
            }
        }
    }

    // Ekta select e value set korar jonno (id, value, text)
    function setValueById(id, value, text) {
        if (!id) return;
        if (id.startsWith('#')) id = id.substring(1);
        var $el = $('#' + id);
        if (!$el.length) return;
        var instance = $el.data('customDdInstance');
        if (instance && typeof instance.setValue === 'function') {
            instance.setValue(value, text);
        } else {
            if (value !== undefined && value !== null) {
                if ($el.find("option[value='" + value + "']").length === 0 && text) {
                    $el.append(new Option(text, value, true, true));
                }
                $el.val(value).trigger('change');
            }
        }
    }

    // Container er moddhe shob custom select clear korar jonno
    function clearAll($container) {
        $container = $container ? $($container) : $(document);
        $container.find('.custom-select-single, .custom-select-multi, select[data-placeholder]').each(function () {
            clearById($(this).attr('id'));
        });
    }

    // Global e expose kora hocche jate baire theke call kora jay
    // Example: window.initCustomDropdowns('.js-branch-form-container', true);
    // Example: window.reinitCustomDropdownById('CompanyCode');
    // Example: window.clearCustomDropdownById('CompanyCode');
    // Example: window.clearAllCustomDropdowns('.js-branch-form-container');
    // Example: window.setCustomDropdownValue('SubSusidiaryLedgerCodeNo', '00000001', 'Cash');
    window.initCustomDropdowns = initCustomDropdowns;
    window.reinitCustomDropdownById = reinitById;
    window.clearCustomDropdownById = clearById;
    window.clearAllCustomDropdowns = clearAll;
    window.setCustomDropdownValue = setValueById;

    // Page load hote e shob select initialize (first time)
    initCustomDropdowns(document, false);

});