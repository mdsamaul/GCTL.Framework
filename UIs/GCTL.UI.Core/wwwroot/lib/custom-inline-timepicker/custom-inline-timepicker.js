// /*!
//  * customTimePicker - Flatpickr diye INLINE (always-visible) time picker
//  * -----------------------------------------------------------------
//  * REQUIRES: Flatpickr CDN (js + css) age theke load thakte hobe.
//  *
//  * USAGE (dynamic, class name diye auto-init):
//  *   HTML e ekta <input class="custom-timepicker" id="attendanceTime" />
//  *   dile, page load howar shathe shathe eta automatic inline time
//  *   picker hoye jabe - kono manual JS call lagbe na.
//  *
//  * MANUAL USAGE (jodi kono ekta specific element e custom option diye
//  * call korte chao):
//  *   new customTimePicker('#attendanceTime', {
//  *       defaultTime: '09:00 AM',
//  *       minuteIncrement: 5
//  *   }).init();
//  * -----------------------------------------------------------------
//  */
// (function (window, $) {
//     'use strict';

//     if (typeof flatpickr === 'undefined') {
//         console.error('customTimePicker: Flatpickr load hoyni. Age CDN script/css add koro.');
//         return;
//     }

//     class customTimePicker {
//         /**
//          * @param {string} selector - e.g. '#attendanceTime'
//          * @param {object} options
//          */
//         constructor(selector, options) {
//             this.selector = selector;
//             this.$el = $(selector);

//             // ---------- DEFAULT PARAMETERS (override korte paro) ----------
//             this.options = Object.assign({
//                 inline: true,           // shobshomoy open/visible thakbe (true inline)
//                 enableTime: true,
//                 enableSeconds: true,    // second field-o dekhabe
//                 noCalendar: true,       // shudhu time, date dorkar nai
//                 dateFormat: 'h:i:S K',  // 12-hour output format, e.g. 02:30:15 PM
//                 time_24hr: false,       // 12-hour mode (AM/PM)
//                 minuteIncrement: 1,
//                 secondIncrement: 1,
//                 defaultTime: null,      // e.g. '09:00:00 AM' - null hole current time
//                 minTime: null,          // e.g. '08:00 AM'
//                 maxTime: null,          // e.g. '06:00 PM'
//                 onChange: null,         // function(selectedTimeStr, instance)
//                 appendTo: null          // kono specific container e boshate chaile DOM element dao
//             }, options || {});

//             this._fpInstance = null;
//         }

//         init() {
//             if (!this.$el.length) {
//                 console.warn('customTimePicker: element pawa jayni -> ' + this.selector);
//                 return this;
//             }

//             var self = this;
//             var fpConfig = {
//                 inline: this.options.inline,
//                 enableTime: this.options.enableTime,
//                 enableSeconds: this.options.enableSeconds,
//                 noCalendar: this.options.noCalendar,
//                 dateFormat: this.options.dateFormat,
//                 time_24hr: this.options.time_24hr,
//                 minuteIncrement: this.options.minuteIncrement,
//                 secondIncrement: this.options.secondIncrement,
//                 defaultDate: this.options.defaultTime || undefined,
//                 minTime: this.options.minTime || undefined,
//                 maxTime: this.options.maxTime || undefined,
//                 onChange: function (selectedDates, dateStr, instance) {
//                     if (typeof self.options.onChange === 'function') {
//                         self.options.onChange(dateStr, instance);
//                     }
//                     self.$el.trigger('custom:timechange', [dateStr]);
//                 },
//                 onReady: function (selectedDates, dateStr, instance) {
//                     // Original text <input> UI e kothao dekhabe na - shudhu
//                     // niche generated time-picker widget (hour/min/AM-PM) e-i UI hobe.
//                     $(instance.input).hide();
//                     self._enforceTwoDigitInputs(instance);
//                     if (typeof self.options.onReady === 'function') {
//                         self.options.onReady(instance);
//                     }
//                 }
//             };

//             if (this.options.appendTo) {
//                 fpConfig.appendTo = this.options.appendTo;
//             }

//             this._fpInstance = flatpickr(this.$el[0], fpConfig);
//             return this;
//         }

//         // Hour/Minute/Second numeric box gulo te 2 digit er beshi type kora
//         // atkano hoy (input event e 3rd+ character shathe shathe kete deya hoy)
//         _enforceTwoDigitInputs(instance) {
//             var $calendar = $(instance.calendarContainer);
//             var $numInputs = $calendar.find('.flatpickr-hour, .flatpickr-minute, .flatpickr-second');

//             $numInputs.attr('maxlength', '2');

//             $numInputs.off('keyup.customMaxTwo').on('keyup.customMaxTwo', function () {
//                 var $this = $(this);
//                 var val = $this.val().replace(/[^0-9]/g, '');
//                 if (val.length > 2) {
//                     val = val.slice(0, 2);
//                     $this.val(val);
//                 }
//             });
//         }

//         // ---------- Public helpers ----------
//         getValue() {
//             return this.$el.val();
//         }

//         setValue(timeStr) {
//             if (this._fpInstance) this._fpInstance.setDate(timeStr, true);
//             return this;
//         }

//         clear() {
//             if (this._fpInstance) this._fpInstance.clear();
//             return this;
//         }

//         destroy() {
//             if (this._fpInstance) this._fpInstance.destroy();
//             return this;
//         }
//     }

//     // ================================================================
//     // AUTO-INIT: class name diye automatically shob element e boshe jabe
//     // ================================================================
//     function autoInitcustomTimePickers(scope) {
//         var $scope = scope ? $(scope) : $(document);

//         $scope.find('.custom-timepicker').each(function () {
//             var $el = $(this);
//             if ($el.data('custom-time-initialized')) return;
//             $el.data('custom-time-initialized', true);

//             var opts = {
//                 defaultTime: $el.data('default-time') || getCurrentTimeStr(),
//                 minTime: $el.data('min-time') || null,
//                 maxTime: $el.data('max-time') || null,
//                 minuteIncrement: parseInt($el.data('minute-increment'), 10) || 1,
//                 secondIncrement: parseInt($el.data('second-increment'), 10) || 1
//             };

//             var instance = new customTimePicker('#' + $el.attr('id'), opts).init();
//             $el.data('custom-instance', instance);
//         });
//     }

//     function getCurrentTimeStr() {
//         var now = new Date();
//         var h = now.getHours();
//         var m = now.getMinutes();
//         var s = now.getSeconds();
//         var ampm = h >= 12 ? 'PM' : 'AM';
//         h = h % 12;
//         if (h === 0) h = 12;
//         return pad2(h) + ':' + pad2(m) + ':' + pad2(s) + ' ' + ampm;
//     }

//     function pad2(n) {
//         return n < 10 ? '0' + n : '' + n;
//     }

//     $(function () {
//         autoInitcustomTimePickers(document);
//     });

//     // Onno kono script theke (e.g. AJAX diye partial view load korar por)
//     // manually re-init korte chaile: customTimePicker.refresh('#myContainer');
//     window.customTimePicker = customTimePicker;
//     window.customTimePicker.refresh = function (scope) {
//         autoInitcustomTimePickers(scope || document);
//     };

// })(window, window.jQuery);


/*!
 * customTimePicker - Flatpickr diye INLINE (always-visible) time picker
 * -----------------------------------------------------------------
 * REQUIRES: Flatpickr CDN (js + css) age theke load thakte hobe.
 *
 * USAGE (dynamic, class name diye auto-init - id lagbe na):
 *   HTML e ekta <input class="custom-timepicker" /> dile,
 *   page load howar shathe shathe eta automatic inline time
 *   picker hoye jabe - kono id ba manual JS call lagbe na.
 * -----------------------------------------------------------------
 */


(function (window, $) {
    'use strict';

    if (typeof flatpickr === 'undefined') {
        console.error('customTimePicker: Flatpickr load hoyni. Age CDN script/css add koro.');
        return;
    }

    class customTimePicker {
        constructor(selector, options) {
            this.selector = selector;
            this.$el = $(selector);

            this.options = Object.assign({
                inline: true,
                enableTime: true,
                enableSeconds: true,
                noCalendar: true,
                dateFormat: 'h:i:S K',
                time_24hr: false,
                minuteIncrement: 1,
                secondIncrement: 1,
                defaultTime: null,
                minTime: null,
                maxTime: null,
                onChange: null,
                appendTo: null
            }, options || {});

            this._fpInstance = null;
        }

        init() {
            if (!this.$el.length) {
                console.warn('customTimePicker: element pawa jayni -> ' + this.selector);
                return this;
            }

            var self = this;
            var fpConfig = {
                inline: this.options.inline,
                enableTime: this.options.enableTime,
                enableSeconds: this.options.enableSeconds,
                noCalendar: this.options.noCalendar,
                dateFormat: this.options.dateFormat,
                time_24hr: this.options.time_24hr,
                minuteIncrement: this.options.minuteIncrement,
                secondIncrement: this.options.secondIncrement,
                defaultDate: this.options.defaultTime || undefined,
                minTime: this.options.minTime || undefined,
                maxTime: this.options.maxTime || undefined,
                onChange: function (selectedDates, dateStr, instance) {
                    if (typeof self.options.onChange === 'function') {
                        self.options.onChange(dateStr, instance);
                    }
                    self.$el.trigger('custom:timechange', [dateStr]);
                },
                onReady: function (selectedDates, dateStr, instance) {
                    $(instance.input).hide();
                    self._enforceTwoDigitInputs(instance);
                    if (typeof self.options.onReady === 'function') {
                        self.options.onReady(instance);
                    }
                }
            };

            if (this.options.appendTo) {
                fpConfig.appendTo = this.options.appendTo;
            }

            this._fpInstance = flatpickr(this.$el[0], fpConfig);
            return this;
        }

        _enforceTwoDigitInputs(instance) {          
            var $calendar = $(instance.calendarContainer);
            var $numInputs = $calendar.find('.flatpickr-hour, .flatpickr-minute, .flatpickr-second');

            $numInputs.attr('maxlength', '2');

            $numInputs.off('keydown.customMaxTwo').on('keydown.customMaxTwo', function (e) {
                var allowedKeys = [8, 9, 37, 38, 39, 40, 46];
                if (allowedKeys.indexOf(e.keyCode) !== -1) return;

                var el = this;
                var selectionLength = (el.selectionEnd || 0) - (el.selectionStart || 0);
                var currentVal = el.value;

                // Sob select kora thakle (2 digit e cursor selection full) - allow, replace hobe
                if (selectionLength >= currentVal.length && currentVal.length > 0) return;

                // Kono selection thakle (partial hole o) - allow, replace hobe
                if (selectionLength > 0) return;

                // Selection nai and already 2 digit ache - block
                if (currentVal.length >= 2) {
                    e.preventDefault();
                }
            });

            // Selection kore type korar por browser default behavior e select kora part
            // replace hoye jay - kintu flatpickr/browser er kichu case e select thakle
            // o value change na hoye purono value thake jay, tai eta ekbar force clear kore dey
            $numInputs.off('keypress.customMaxTwo').on('keypress.customMaxTwo', function (e) {
                var el = this;
                var selectionLength = (el.selectionEnd || 0) - (el.selectionStart || 0);
                if (selectionLength > 0 && selectionLength === el.value.length) {
                    // pura select kora - explicitly clear kore dao jate notun char boshe
                    el.value = '';
                }
            });

            $numInputs.off('input.customMaxTwo').on('input.customMaxTwo', function () {
                var $this = $(this);
                var val = $this.val().replace(/[^0-9]/g, '');
                if (val.length > 2) {
                    val = val.slice(0, 2);
                }
                $this.val(val);
            });
        }

        getValue() {
            return this.$el.val();
        }

        setValue(timeStr) {
            if (this._fpInstance) this._fpInstance.setDate(timeStr, true);
            return this;
        }

        clear() {
            if (this._fpInstance) this._fpInstance.clear();
            return this;
        }

        destroy() {
            if (this._fpInstance) this._fpInstance.destroy();
            return this;
        }
    }

    // ================================================================
    // AUTO-INIT: shudhu class name dile-i kaj hobe, id lagbe na
    // ================================================================
    function autoInitcustomTimePickers(scope) {
        var $scope = scope ? $(scope) : $(document);

        $scope.find('.custom-timepicker').each(function () {
            var $el = $(this);
            if ($el.data('custom-time-initialized')) return;
            $el.data('custom-time-initialized', true);

            // id na thakle nijei ekta unique id boshiye dao
            if (!$el.attr('id')) {
                var autoId = 'customTime_' + Math.random().toString(36).slice(2, 9);
                $el.attr('id', autoId);
            }

            var opts = {
                defaultTime: $el.data('default-time') || getCurrentTimeStr(),
                minTime: $el.data('min-time') || null,
                maxTime: $el.data('max-time') || null,
                minuteIncrement: parseInt($el.data('minute-increment'), 10) || 1,
                secondIncrement: parseInt($el.data('second-increment'), 10) || 1
            };

            var instance = new customTimePicker('#' + $el.attr('id'), opts).init();
            $el.data('custom-instance', instance);
        });
    }

    function getCurrentTimeStr() {
        var now = new Date();
        var h = now.getHours();
        var m = now.getMinutes();
        var s = now.getSeconds();
        var ampm = h >= 12 ? 'PM' : 'AM';
        h = h % 12;
        if (h === 0) h = 12;
        return pad2(h) + ':' + pad2(m) + ':' + pad2(s) + ' ' + ampm;
    }

    function pad2(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    $(function () {
        autoInitcustomTimePickers(document);
    });
    // ---------------- Global setTime ----------------
    window.setTime = function (selector, value, disabled = false) {
        var $el = $(selector);
        var instance = $el.data('custom-instance');

        if (!instance) {
            $el.val(value || '');
            $el.prop('disabled', !!disabled);
            return;
        }

        if (value) instance._fpInstance.setDate(value, true);

        instance.$el.prop('disabled', !!disabled);
        var $calendar = $(instance._fpInstance.calendarContainer);
        $calendar.toggleClass('custom-time-disabled', !!disabled);

        var $inputs = $calendar.find('.flatpickr-hour, .flatpickr-minute, .flatpickr-second, .flatpickr-am-pm');
        $inputs.prop('disabled', !!disabled);
        $inputs.css('pointer-events', disabled ? 'none' : 'auto');

        var $arrows = $calendar.find('.arrowUp, .arrowDown');
        $arrows.css('pointer-events', disabled ? 'none' : 'auto');
    };
    // AJAX diye partial view load howar por: customTimePicker.refresh('#container');
    window.customTimePicker = customTimePicker;
    window.customTimePicker.refresh = function (scope) {
        autoInitcustomTimePickers(scope || document);
    };

})(window, window.jQuery);