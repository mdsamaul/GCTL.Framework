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
        if (!fpInstance || !fpInstance.days) {
            return;
        }

        setTimeout(function () {

            var dayContainer = fpInstance.days;

            if (!dayContainer) return;

            var days = Array.prototype.slice.call(
                dayContainer.querySelectorAll('.flatpickr-day')
            );

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
        if (!fpInstance || !fpInstance.calendarContainer) return;
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
        if (!el) return;
        var $el = $(el);
        if ($el.data('customFpInstance') || el._flatpickr || $el.hasClass('flatpickr-alt-input')) return;
        if ($el.hasClass('input') && $el.hasClass('flatpickr-input')) return;
        if ($el.prev().hasClass('flatpickr-input') && $el.prev()[0] && $el.prev()[0]._flatpickr) {
            $el.data('customFpInstance', $el.prev()[0]._flatpickr);
            return;
        }

        var backendFormat = $el.data('backend-format') || DEFAULTS.backendFormat;
        var displayFormat = $el.data('display-format') || DEFAULTS.displayFormat;
        var holidays = parseHolidays($el);
        var allowInput = $el.data('allow-input') !== false;
        var minDate = $el.data('min-date') || null;
        var maxDate = $el.data('max-date') || null;

        var rawDefault = $el.data('default-date') || (el.value ? el.value : null);
        var parsedDefault = null;
        if (rawDefault) {
            if (rawDefault instanceof Date) {
                parsedDefault = rawDefault;
            } else {
                var s = String(rawDefault).trim();
                if (s.includes('T')) s = s.split('T')[0];
                var dmy = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
                if (dmy) {
                    parsedDefault = new Date(parseInt(dmy[3], 10), parseInt(dmy[2], 10) - 1, parseInt(dmy[1], 10));
                } else {
                    var p = new Date(s);
                    if (!isNaN(p.getTime())) parsedDefault = p;
                }
            }
        }

        var fp = flatpickr(el, {
            altInput: true,
            altFormat: displayFormat,
            dateFormat: backendFormat,
            defaultDate: parsedDefault || rawDefault || undefined,
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
                if ($el.is('[readonly]') || $el.is(':disabled')) {
                    fpInstance._customDisabled = true;
                    if (fpInstance.altInput) {
                        fpInstance.altInput.readOnly = $el.is('[readonly]');
                        fpInstance.altInput.disabled = $el.is(':disabled');
                    }
                }
            },
            onOpen: function (selectedDates, dateStr, fpInstance) {
                if (fpInstance._customDisabled || $el.is('[readonly]') || $el.is(':disabled')) {
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
        if ($el.is('[readonly]') || $el.is(':disabled')) {
            fp._customDisabled = true;
            if (fp.altInput) {
                fp.altInput.readOnly = $el.is('[readonly]');
                fp.altInput.disabled = $el.is(':disabled');
            }
        }
        if (fp.altInput) {
            $(fp.altInput).removeClass('custom-datepicker').addClass('flatpickr-alt-input');
            $(fp.altInput).data('customFpInstance', fp);
            fp.altInput._flatpickr = fp;
        }
        $el.data('customFpInstance', fp);

        if (parsedDefault && !isNaN(parsedDefault.getTime())) {
            var py = parsedDefault.getFullYear();
            var pm = ('0' + (parsedDefault.getMonth() + 1)).slice(-2);
            var pd = ('0' + parsedDefault.getDate()).slice(-2);
            var parsedIso = py + '-' + pm + '-' + pd;
            var parsedDmy = pd + '/' + pm + '/' + py;

            el.value = parsedIso;
            if (fp.altInput) {
                fp.altInput.value = (displayFormat === 'd/m/Y') ? parsedDmy : parsedIso;
            }
            try {
                fp.setDate(parsedDefault, false);
            } catch (e) { }
        } else if (fp.selectedDates && fp.selectedDates.length > 0 && fp.altInput) {
            var d0 = fp.selectedDates[0];
            var dy = d0.getFullYear();
            var dm = ('0' + (d0.getMonth() + 1)).slice(-2);
            var dd = ('0' + d0.getDate()).slice(-2);
            if (displayFormat === 'd/m/Y') {
                fp.altInput.value = dd + '/' + dm + '/' + dy;
            }
            if (backendFormat === 'Y-m-d') {
                el.value = dy + '-' + dm + '-' + dd;
            }
        }
    }
    function initAll(selector) {
        $(selector || 'input.custom-datepicker:not(.flatpickr-alt-input):not([type="hidden"])').each(function () {
            initOne(this);
        });
    }
    function setDatepickerValue(selector, value) {
        window.setDate(selector, value);
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
        if (!$el.length) return;

        var fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
        if (!fp && $el[0]) {
            initOne($el[0]);
            fp = $el.data('customFpInstance') || ($el[0] ? $el[0]._flatpickr : null);
        }

        if (!value) {
            if (fp) {
                fp.clear();
            } else {
                $el.val('');
            }
            $el.prop('disabled', !!disabled);
            return;
        }

        var dateObj = null;
        if (value instanceof Date) {
            dateObj = value;
        } else {
            var s = String(value).trim();
            if (s.includes('T')) s = s.split('T')[0];
            var dmy = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
            if (dmy) {
                dateObj = new Date(parseInt(dmy[3], 10), parseInt(dmy[2], 10) - 1, parseInt(dmy[1], 10));
            } else {
                var p = new Date(s);
                if (!isNaN(p.getTime())) dateObj = p;
            }
        }

        var isoStr = '';
        var dmyStr = '';
        if (dateObj && !isNaN(dateObj.getTime())) {
            var y = dateObj.getFullYear();
            var m = ('0' + (dateObj.getMonth() + 1)).slice(-2);
            var d = ('0' + dateObj.getDate()).slice(-2);
            isoStr = y + '-' + m + '-' + d;
            dmyStr = d + '/' + m + '/' + y;
        } else {
            isoStr = String(value);
            dmyStr = String(value);
        }

        if (!fp) {
            $el.val(isoStr);
            $el.attr('data-default-date', isoStr);
            $el.prop('disabled', !!disabled);
            return;
        }

        try {
            fp.setDate(dateObj || isoStr, true);
        } catch (e) {
            $el.val(isoStr);
        }

        if (fp.altInput && dmyStr) {
            fp.altInput.value = dmyStr;
        }

        var nextInp = $el.next('input')[0];
        if (nextInp && (nextInp.classList.contains('input') || nextInp.classList.contains('flatpickr-alt-input') || nextInp.classList.contains('custom-datepicker'))) {
            nextInp.value = dmyStr;
        }

        fp._customDisabled = !!disabled;
        $el.prop('disabled', !!disabled);
        if (fp.altInput) fp.altInput.disabled = !!disabled;
        if (disabled) fp.close();
    };

    $(function () {
        initAll('input.custom-datepicker:not(.flatpickr-alt-input):not([type="hidden"])');
    });
    window.custom_Datepicker = {
        init: initOne,
        initAll: initAll,
        setValue: setDatepickerValue,
        getValue: getDatepickerValue,
        destroy: destroyDatepicker
    };
})(window, window.jQuery);