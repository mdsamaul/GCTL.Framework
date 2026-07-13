(function (window, $) {
    'use strict';
    var DEFAULTS = {
        displayFormat: 'd/m/Y',
        backendFormat: 'Y-m-d',
        holidays: []
    };
    function parseHolidays($el) {
        var attrHolidays = $el.attr('data-holidays');
        var holidays = [];
        try {
            if (attrHolidays) holidays = JSON.parse(attrHolidays);
        } catch (e) {
            console.warn('custom-datepicker: data-holidays JSON parse error', e);
        }
        if (!holidays.length && Array.isArray(window.custom_HOLIDAYS)) {
            holidays = window.custom_HOLIDAYS;
        }
        return holidays;
    }
    function findHoliday(holidays, dateObj) {
        var y = dateObj.getFullYear();
        var m = String(dateObj.getMonth() + 1).padStart(2, '0');
        var d = String(dateObj.getDate()).padStart(2, '0');
        var iso = y + '-' + m + '-' + d;
        return holidays.find(function (h) {
            var raw = h.date || h.Date;
            if (!raw) return false;
            return String(raw).substring(0, 10) === iso;
        });
    }
    function hideEmptyTrailingRow(fpInstance) {
        setTimeout(function () {
            var dayContainer = fpInstance.days;
            var days = Array.prototype.slice.call(dayContainer.querySelectorAll('.flatpickr-day'));
            if (!days.length) return;
            days.forEach(function (d) {
                d.style.removeProperty('visibility');
                d.style.removeProperty('display');
            });
            var perRow = 7;
            var totalRows = Math.ceil(days.length / perRow);
            for (var r = 0; r < totalRows; r++) {
                var rowDays = days.slice(r * perRow, r * perRow + perRow);
                var hasCurrentMonthDay = rowDays.some(function (d) {
                    return !d.classList.contains('prevMonthDay') && !d.classList.contains('nextMonthDay');
                });
                if (!hasCurrentMonthDay) {
                    rowDays.forEach(function (d) { d.style.display = 'none'; });
                } else {
                    rowDays.forEach(function (d) {
                        d.style.display = '';
                        if (d.classList.contains('prevMonthDay') || d.classList.contains('nextMonthDay')) {
                            d.style.visibility = 'hidden';
                        } else {
                            d.style.removeProperty('visibility');
                        }
                    });
                }
            }
        }, 50);
    }
    function addClearButton(fpInstance) {
        if (fpInstance.calendarContainer.querySelector('.custom-fp-clear-btn')) return;
        var footer = document.createElement('div');
        footer.className = 'custom-fp-footer';
        var todayBtn = document.createElement('button');
        todayBtn.type = 'button';
        todayBtn.className = 'custom-fp-today-btn';
        todayBtn.textContent = 'Today';
        todayBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            fpInstance.setDate(new Date(), true);
            fpInstance.close();
        });
        var clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.className = 'custom-fp-clear-btn';
        clearBtn.textContent = 'Clear';
        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            fpInstance.clear();
            fpInstance.close();
        });
        footer.appendChild(todayBtn);
        footer.appendChild(clearBtn);
        fpInstance.calendarContainer.appendChild(footer);
    }
    function refreshHolidaysForView(fpInstance) {
        if (!window.customHolidayService || typeof window.customHolidayService.refresh !== 'function') return;
        var viewYear = fpInstance.currentYear;
        var existing = window.customHolidayService.getHolidays();
        var alreadyHasYear = existing.some(function (h) {
            return h.date.indexOf(viewYear + '-') === 0;
        });
        if (alreadyHasYear) {
            fpInstance.redraw();
            return;
        }
        window.customHolidayService.refresh({ year: viewYear }).then(function () {
            fpInstance.redraw();
        });
    }

    var activeTooltip = null;


    function positionTooltip(tooltip, dayElem, arrow) {
        tooltip.style.display = 'block';
        var dayRect = dayElem.getBoundingClientRect();
        var ttRect = tooltip.getBoundingClientRect();
        var vw = window.innerWidth;
        var vh = window.innerHeight;

        var left = dayRect.left + (dayRect.width / 2) - (ttRect.width / 2);
        var top = dayRect.top - ttRect.height - 10;
        var placement = 'top';

        if (top < 4) {
            top = dayRect.bottom + 10;
            placement = 'bottom';
        }

        var originalLeft = left;
        if (left < 4) left = 4;
        if (left + ttRect.width > vw - 4) left = vw - ttRect.width - 4;

        tooltip.style.left = left + 'px';
        tooltip.style.top = top + 'px';
        tooltip.style.transform = placement === 'top' ? 'translateY(4px)' : 'translateY(-4px)';

        var arrowLeft = (originalLeft + ttRect.width / 2) - left;
        arrow.style.left = arrowLeft + 'px';

        if (placement === 'top') {
            arrow.style.top = '100%';
            arrow.style.bottom = 'auto';
            arrow.style.borderTop = '5px solid #212529';
            arrow.style.borderBottom = 'none';
        } else {
            arrow.style.bottom = '100%';
            arrow.style.top = 'auto';
            arrow.style.borderBottom = '5px solid #212529';
            arrow.style.borderTop = 'none';
        }
    }

    function initOne(el) {
        var $el = $(el);
        if ($el.data('customFpInstance')) return;
        var backendFormat = $el.data('backend-format') || DEFAULTS.backendFormat;
        var displayFormat = $el.data('display-format') || DEFAULTS.displayFormat;
        var holidays = parseHolidays($el);
        var allowInput = $el.data('allow-input') !== false;
        var minDate = $el.data('min-date') || null;
        var maxDate = $el.data('max-date') || null;
        var fp = flatpickr(el, {
            altInput: true,
            altFormat: displayFormat,
            dateFormat: backendFormat,
            allowInput: allowInput,
            animate: false,
            monthSelectorType: 'dropdown',
            minDate: minDate,
            maxDate: maxDate,
            locale: { firstDayOfWeek: 0 },
            onDayCreate: function (dObj, dStr, fpInstance, dayElem) {
                var dayDate = dayElem.dateObj;
                var currentHolidays = Array.isArray(window.custom_HOLIDAYS) ? window.custom_HOLIDAYS : holidays;
                var holiday = findHoliday(currentHolidays, dayDate);
                if (holiday) {
                    dayElem.classList.add('custom-holiday');
                    dayElem.setAttribute('data-holiday-name', holiday.name);

                
                    dayElem.addEventListener('mouseenter', function () {
                        if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }

                        var tooltip = document.createElement('span');
                        tooltip.textContent = holiday.name;
                        tooltip.style.position = 'fixed';
                        tooltip.style.background = '#212529';
                        tooltip.style.color = '#fff';
                        tooltip.style.padding = '5px 10px';
                        tooltip.style.borderRadius = '5px';
                        tooltip.style.fontSize = '11px';
                        tooltip.style.fontWeight = '500';
                        tooltip.style.whiteSpace = 'nowrap';
                        tooltip.style.zIndex = '999999';
                        tooltip.style.pointerEvents = 'none';
                        tooltip.style.opacity = '0';
                        tooltip.style.transition = 'opacity .15s ease, transform .15s ease';

                        var arrow = document.createElement('span');
                        arrow.style.position = 'absolute';
                        arrow.style.left = '50%';
                        arrow.style.transform = 'translateX(-50%)';
                        arrow.style.width = '0';
                        arrow.style.height = '0';
                        arrow.style.borderLeft = '5px solid transparent';
                        arrow.style.borderRight = '5px solid transparent';
                        tooltip.appendChild(arrow);

                        document.body.appendChild(tooltip);
                        activeTooltip = tooltip;

                        positionTooltip(tooltip, dayElem, arrow);

                        requestAnimationFrame(function () {
                            tooltip.style.opacity = '1';
                            tooltip.style.transform = 'translateY(0)';
                        });
                    });

                    dayElem.addEventListener('mouseleave', function () {
                        if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }
                    });
                }
            },
            onMonthChange: function (selectedDates, dateStr, fpInstance) {
                if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }
                hideEmptyTrailingRow(fpInstance);
                refreshHolidaysForView(fpInstance);
            },
            onYearChange: function (selectedDates, dateStr, fpInstance) {
                if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }
                hideEmptyTrailingRow(fpInstance);
                refreshHolidaysForView(fpInstance);
            },
            onReady: function (selectedDates, dateStr, fpInstance) {
                hideEmptyTrailingRow(fpInstance);
                addClearButton(fpInstance);
            },
            onOpen: function (selectedDates, dateStr, fpInstance) {
                if (fpInstance._customDisabled) {
                    fpInstance.close();
                    return;
                }
                fpInstance.calendarContainer.style.transformOrigin = 'top center';
                hideEmptyTrailingRow(fpInstance);
            },
            onClose: function () {
                if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }
            }
        });
        $el.data('customFpInstance', fp);
    }
    function initAll(selector) {
        $(selector || '.custom-datepicker').each(function () {
            initOne(this);
        });
    }
    function setDatepickerValue(selector, value) {
        var $el = $(selector);
        var fp = $el.data('customFpInstance');
        if (!fp) { $el.val(value); return; }
        if (!value) { fp.clear(); return; }
        fp.setDate(value, true);
    }
    function getDatepickerValue(selector) {
        var $el = $(selector);
        return $el.val();
    }
    function destroyDatepicker(selector) {
        var $el = $(selector);
        var fp = $el.data('customFpInstance');
        if (fp) {
            if (activeTooltip) { activeTooltip.remove(); activeTooltip = null; }
            fp.destroy();
            $el.removeData('customFpInstance');
        }
    }

    // ---------------- Global setDate ----------------
    window.setDate = function (selector, value, disabled = false) {
        var $el = $(selector);
        var fp = $el.data('customFpInstance');

        if (!fp) {
            $el.val(value || '');
            $el.prop('disabled', !!disabled);
            return;
        }

        if (!value) {
            fp.clear();
        } else {
            fp.setDate(value, true);
        }

        fp._customDisabled = !!disabled;
        $el.prop('disabled', !!disabled);
        if (fp.altInput) fp.altInput.disabled = !!disabled;
        if (disabled) fp.close();
    };

    
    $(function () {
        initAll('.custom-datepicker');
    });
    window.custom_Datepicker = {
        init: initOne,
        initAll: initAll,
        setValue: setDatepickerValue,
        getValue: getDatepickerValue,
        destroy: destroyDatepicker
    };
})(window, window.jQuery);