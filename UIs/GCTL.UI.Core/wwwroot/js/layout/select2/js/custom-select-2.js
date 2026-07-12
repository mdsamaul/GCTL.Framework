$(function () {

    function initSingle($el) {
        if ($el.data('customDdInstance')) return;
        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true
        };

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
        options.items = items;

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        var instance = new customDropdownSingle('#' + $el.attr('id'), options).init();
        $el.data('customDdInstance', instance);
    }

    function initMulti($el) {
        if ($el.data('customDdInstance')) return;
        var options = {
            placeholder: $el.data('placeholder') || 'Select',
            showClear: true,
            showCopyId: true,
            copyAsJsonArray: true
        };

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
        options.items = items;

        if ($el.data('parent')) {
            options.parentSelector = $el.data('parent');
        }

        var instance = new customDropdownMulti('#' + $el.attr('id'), options).init();
        $el.data('customDdInstance', instance);
    }

    // Page load hote e shob select initialize
    $('.custom-select-single').each(function () { initSingle($(this)); });
    $('.custom-select-multi').each(function () { initMulti($(this)); });

});