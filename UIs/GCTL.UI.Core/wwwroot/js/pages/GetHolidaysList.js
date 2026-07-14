(function (window, $) {
    'use strict';

    var _cache = null;
    var _loading = null;
    var DEFAULTS = {
        url: null, // fixed URL na rekhe dynamic detect korbe
        year: null,
        method: 'GET',
        data: {},
        selector: '.custom-datepicker',
        autoApply: true,
        cacheMs: 5 * 60 * 1000
    };

    function getDynamicHolidayUrl() {
        var controllerName = window.location.pathname.split('/')[1] || 'Home';
        return '/' + controllerName + '/GetHolidays';
    }
   

    var _lastFetchedAt = 0;
    var _settings = {};

    function normalizeHoliday(h) {
        var rawDate = h.date || h.Date || h.holidayDate || h.HolidayDate;
        var rawName = h.name || h.Name || h.holidayName || h.HolidayName || 'Holiday';

        if (!rawDate) return null;

        var d = new Date(rawDate);
        if (isNaN(d.getTime())) return null;

        var iso = d.getFullYear() + '-' +
            String(d.getMonth() + 1).padStart(2, '0') + '-' +
            String(d.getDate()).padStart(2, '0');

        return { date: iso, name: rawName };
    }

    function applyToExistingPickers(selector) {
        $(selector || _settings.selector).each(function () {
            var $el = $(this);
            var fp = $el.data('customFpInstance');
            if (!fp) return;

            if (typeof fp.redraw === 'function') {
                fp.redraw();
            } else {
                fp.jumpToDate(fp.currentYear + '-' + (fp.currentMonth + 1) + '-01');
            }
        });
    }

    function fetchHolidays(opts) {
        var settings = $.extend({}, DEFAULTS, _settings, opts || {});
        if (!settings.url) settings.url = getDynamicHolidayUrl();

        var now = Date.now();
        // year change hole cache invalidate kore notun year fetch koro
        var requestedYear = settings.year;
        var cacheCoversYear = _cache && _cache.some(function (h) {
            return !requestedYear || h.date.indexOf(requestedYear + '-') === 0;
        });

        if (_cache && cacheCoversYear && (now - _lastFetchedAt) < settings.cacheMs) {
            return $.Deferred().resolve(_cache).promise();
        }

        if (_loading) return _loading;

        var reqData = $.extend({}, settings.data);
        if (settings.year) reqData.year = settings.year;

        _loading = $.ajax({
            url: settings.url,
            method: settings.method,
            data: reqData,
            dataType: 'json'
        }).then(function (response) {
            var list = Array.isArray(response) ? response
                : (response && Array.isArray(response.data)) ? response.data
                    : [];

            var normalized = list
                .map(normalizeHoliday)
                .filter(function (h) { return h !== null; });

            // notun year er data purono cache er sathe merge kore rakho (na hole onno month e giye purono year er holiday muche jabe)
            if (_cache) {
                var existingDates = _cache.map(function (h) { return h.date; });
                normalized.forEach(function (h) {
                    if (existingDates.indexOf(h.date) === -1) {
                        _cache.push(h);
                    }
                });
            } else {
                _cache = normalized;
            }

            _lastFetchedAt = Date.now();
            window.custom_HOLIDAYS = _cache;

            if (settings.autoApply) {
                applyToExistingPickers(settings.selector);
            }

            return _cache;
        }).fail(function (xhr, status, err) {
            console.error('custom-holiday-service: fetch failed', status, err);
        }).always(function () {
            _loading = null;
        });

        return _loading;
    }

    function init(opts) {
        _settings = $.extend({}, DEFAULTS, opts || {});
        return fetchHolidays(_settings);
    }

    function refresh(opts) {
        return fetchHolidays(opts);
    }

    function getHolidays() {
        return _cache || [];
    }

    function isHoliday(dateObj) {
        if (!_cache) return null;
        var y = dateObj.getFullYear();
        var m = String(dateObj.getMonth() + 1).padStart(2, '0');
        var d = String(dateObj.getDate()).padStart(2, '0');
        var iso = y + '-' + m + '-' + d;
        return _cache.find(function (h) { return h.date === iso; }) || null;
    }

    window.customHolidayService = {
        init: init,
        refresh: refresh,
        getHolidays: getHolidays,
        isHoliday: isHoliday,
        applyToExistingPickers: applyToExistingPickers
    };

})(window, window.jQuery);