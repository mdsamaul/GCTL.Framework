// $(function () {

//     function initSingle($el) {
//         if ($el.data('customDdInstance')) return;
//         var options = {
//             placeholder: $el.data('placeholder') || 'Select',
//             showClear: true,
//             showCopyId: true
//         };

//         var items = [];
//         $el.find('option').each(function () {
//             var val = $(this).val();
//             if (!val) return;
//             items.push({
//                 id: val,
//                 text: $(this).text(),
//                 selected: $(this).is(':selected')
//             });
//         });
//         options.items = items;

//         if ($el.data('parent')) {
//             options.parentSelector = $el.data('parent');
//         }

//         var instance = new customDropdownSingle('#' + $el.attr('id'), options).init();
//         $el.data('customDdInstance', instance);
//     }

//     function initMulti($el) {
//         if ($el.data('customDdInstance')) return;
//         var options = {
//             placeholder: $el.data('placeholder') || 'Select',
//             showClear: true,
//             showCopyId: true,
//             copyAsJsonArray: true
//         };

//         var items = [];
//         $el.find('option').each(function () {
//             var val = $(this).val();
//             if (!val) return;
//             items.push({
//                 id: val,
//                 text: $(this).text(),
//                 selected: $(this).is(':selected')
//             });
//         });
//         options.items = items;

//         if ($el.data('parent')) {
//             options.parentSelector = $el.data('parent');
//         }

//         var instance = new customDropdownMulti('#' + $el.attr('id'), options).init();
//         $el.data('customDdInstance', instance);
//     }

//     // Page load hote e shob select initialize
//     $('.custom-select-single').each(function () { initSingle($(this)); });
//     $('.custom-select-multi').each(function () { initMulti($(this)); });

// });

$(function () {

    function getItems($el) {
        var items = [];
        $el.find('option').each(function () {
            var val = $(this).val();
            if (!val) return;
            items.push({
                id: val,
                text: $(this).text(),
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

        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true,
            items: getItems($el)
        };

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        var instance = new customDropdownSingle('#' + $el.attr('id'), options).init();
        $el.data('customDdInstance', instance);
        return instance;
    }

    function initMulti($el, forceReinit) {
        if ($el.data('customDdInstance')) {
            if (!forceReinit) return $el.data('customDdInstance');
            destroyInstance($el);
        }

        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true,
            copyAsJsonArray: true,
            items: getItems($el)
        };

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        var instance = new customDropdownMulti('#' + $el.attr('id'), options).init();
        $el.data('customDdInstance', instance);
        return instance;
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
    }

    // Ekta single select ke id diye reinit korar jonno (dorkar hole)
    function reinitById(id) {
        var $el = $('#' + id);
        if (!$el.length) return;
        if ($el.hasClass('custom-select-multi')) {
            initMulti($el, true);
        } else {
            initSingle($el, true);
        }
    }

    // Ekta select clear korar jonno (id diye)
    function clearById(id) {
        var $el = $('#' + id);
        if (!$el.length) return;
        var instance = $el.data('customDdInstance');
        if (instance && typeof instance.clear === 'function') {
            instance.clear();
        } else {
            // fallback: value reset kore reinit
            $el.val('');
            if ($el.hasClass('custom-select-multi')) {
                initMulti($el, true);
            } else {
                initSingle($el, true);
            }
        }
    }

    // Container er moddhe shob custom select clear korar jonno
    function clearAll($container) {
        $container = $container ? $($container) : $(document);
        $container.find('.custom-select-single, .custom-select-multi').each(function () {
            clearById($(this).attr('id'));
        });
    }

    // Global e expose kora hocche jate baire theke call kora jay
    // Example: window.initCustomDropdowns('.js-branch-form-container', true);
    // Example: window.reinitCustomDropdownById('CompanyCode');
    // Example: window.clearCustomDropdownById('CompanyCode');
    // Example: window.clearAllCustomDropdowns('.js-branch-form-container');
    window.initCustomDropdowns = initCustomDropdowns;
    window.reinitCustomDropdownById = reinitById;
    window.clearCustomDropdownById = clearById;
    window.clearAllCustomDropdowns = clearAll;

    // Page load hote e shob select initialize (first time)
    initCustomDropdowns(document, false);

});