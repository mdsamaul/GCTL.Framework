




//#region Universal Flatpickr Methods Version 2


const flatpickrHelper = {


    //// ------Set Date
    //flatpickrHelper.setDate('dob', '2025-06-16');


    setDate: function (elementId, dateValue) {
        if (!dateValue) return;

      

        // Normalize the selector
        const normalizedId = elementId.startsWith('#') ? elementId.substring(1) : elementId;
        const element = document.getElementById(normalizedId);

       //const element = document.getElementById(elementId);
        if (element && element._flatpickr) {
            element._flatpickr.setDate(dateValue, true); // true = trigger change event
        } else if (element) {
            element.value = dateValue;
        }
    },

    //flatpickrHelper.disable('editNoticeDate', 'editResignationDate');
    disable: function (...elementIds) {
        elementIds.forEach(elementId => {
            const normalizedId = elementId.startsWith('#') ? elementId.substring(1) : elementId;
            const element = document.getElementById(normalizedId);

            if (element && element._flatpickr) {
                element._flatpickr.set('disable', [() => true]); // disables all dates
            }

            if (element) {
                element.readOnly = true;
            }
        });
    },

    //flatpickrHelper.enable('editNoticeDate', 'editResignationDate');
    enable: function (...elementIds) {
        elementIds.forEach(elementId => {
            const normalizedId = elementId.startsWith('#') ? elementId.substring(1) : elementId;
            const element = document.getElementById(normalizedId);

            if (element && element._flatpickr) {
                element._flatpickr.set('disable', []); // clears all disabled rules
            }

            if (element) {
                element.readOnly = false;
            }
        });
    },


    ////----- Get Date
    //let dobValue = flatpickrHelper.getDate('dob');
    //console.log('Selected Date:', dobValue);

    getDate: function (elementId) {

       

        // Normalize the selector
        const normalizedId = elementId.startsWith('#') ? elementId.substring(1) : elementId;
        const element = document.getElementById(normalizedId);

       // const element = document.getElementById(elementId);
        if (element && element._flatpickr) {
            return element._flatpickr.input.value; // returns formatted date
        } else if (element) {
            return element.value; // fallback if flatpickr not initialized
        }
        return null;
    },

    //// -----Clear Date
    //flatpickrHelper.clearDate('dob');

    clearDate: function (...elementIds) {
        elementIds.forEach(elementId => {
            const element = document.getElementById(elementId);
            if (element && element._flatpickr) {
                element._flatpickr.clear();
            } else if (element) {
                element.value = '';
            }
        });
    }
};

// ✅ Optional global access
window.flatpickrHelper = flatpickrHelper;
//#endregion


//setDate: function (elementId, dateValue) {
//    if (!dateValue) return;

//    // Normalize the selector
//    const normalizedId = elementId.startsWith('#') ? elementId.substring(1) : elementId;
//    const element = document.getElementById(normalizedId);

//    if (element && element._flatpickr) {
//        element._flatpickr.setDate(dateValue, true); // true = trigger change event
//    } else if (element) {
//        element.value = dateValue;
//    }
//}



//// Override jQuery .val() method to auto-detect dateAuto class
//(function ($) {
//    const originalVal = $.fn.val;

//    $.fn.val = function (value) {
//        const result = originalVal.apply(this, arguments);

//        // If setting a value, check for dateAuto class and update Flatpickr
//        if (arguments.length > 0) {
//            this.each(function () {
//                const $this = $(this);

//                // Check if element has dateAuto class
//                if ($this.hasClass('dateAuto')) {
//                    const element = this;

//                    // Small delay to ensure Flatpickr is ready
//                    setTimeout(() => {
//                        if (element._flatpickr) {
//                            if (value) {
//                                element._flatpickr.setDate(value, false);
//                            } else {
//                                element._flatpickr.clear();
//                            }
//                        }
//                    }, 10);
//                }
//            });
//        }

//        return result;
//    };
//})(jQuery);

// Now your existing code will work automatically:
// $("#additionalPasportIssueDate").val(employee.pasportIssueDate || '');
// $("#additionalPasportExpireDate").val(employee.pasportExpireDate || '');
// etc...
//
// The calendar will automatically update for any element with 'dateAuto' class



//// Global Flatpickr configuration for all dateAuto elements
//document.addEventListener('DOMContentLoaded', function () {

//    // Global configuration
//    const globalDateConfig = {
//        dateFormat: "d-m-Y",          // DD-MM-YYYY format
//        disableMobile: true,
//        allowInput: true,
//        // This is the key - listen for changes and update calendar
//        onChange: function (selectedDates, dateStr, instance) {
//            // Calendar is already updated by user interaction
//        }
//    };

//    // Initialize all elements with dateAuto class
//    const dateAutoElements = document.querySelectorAll('.dateAuto');

//    dateAutoElements.forEach(element => {
//        let elementConfig = globalDateConfig;

//        if (element.dataset.options) {
//            try {
//                const dataOptions = JSON.parse(element.dataset.options);
//                elementConfig = { ...globalDateConfig, ...dataOptions };
//            } catch (e) {
//                console.warn('Invalid data-options JSON:', e);
//            }
//        }

//        flatpickr(element, elementConfig);
//    });
//});

//// Enhanced jQuery method that updates both input value and calendar
//$.fn.setDateVal = function (value) {
//    return this.each(function () {
//        const $this = $(this);

//        // Set the input value
//        $this.val(value || '');

//        // Update the Flatpickr calendar if it exists
//        if (this._flatpickr && value) {
//            this._flatpickr.setDate(value, false); // false = don't trigger change event
//        }
//    });
//};

//// Now you can use it like this:
//// $("#additionalPasportIssueDate").setDateVal(employee.pasportIssueDate || '');

//// OR if you want to keep using .val(), add this helper function:
//function syncFlatpickrAfterVal() {
//    $('.dateAuto').each(function () {
//        const element = this;
//        const value = $(element).val();

//        if (element._flatpickr && value) {
//            element._flatpickr.setDate(value, false);
//        }
//    });
//}

//// Usage with your existing code:
//// $("#additionalPasportIssueDate").val(employee.pasportIssueDate || '');
//// $("#additionalPasportExpireDate").val(employee.pasportExpireDate || '');
//// // ... set all your values first
////
//// // Then sync all calendars at once
//// syncFlatpickrAfterVal();