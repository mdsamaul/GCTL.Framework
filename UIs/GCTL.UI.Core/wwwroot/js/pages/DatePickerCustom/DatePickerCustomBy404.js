
function validateRequiredFields(fieldIds) {
    let isValid = true;
    const idArray = fieldIds.split(',');

    idArray.forEach(function (id) {
        const fieldName = id.trim();
        const $input = $('#' + fieldName);
        const $validationSpan = $('span[data-valmsg-for="' + fieldName + '"]');

        // Clear previous validation
        $input.css('border', '');
        $validationSpan.text('');

        // Detect empty value
        const value = $input.val()?.trim();
        if (!value) {
            // For standard input/select
            $input.css('border', '1px solid red');

            // For Choice.js, Select2, or similar (custom UI)
            const $choiceWrapper = $input.closest('.choices').find('.choices__inner');
            if ($choiceWrapper.length > 0) {
                $choiceWrapper.css('border', '1px solid red');
            }

            // Show error message in span
            $validationSpan.text('This field is required.');
            isValid = false;
        } else {
            // Reset Choice.js border if filled
            $input.closest('.choices').find('.choices__inner').css('border', '');
        }
    });

    return isValid;
}

$(document).on('input change', 'input, select, textarea', function () {
    const $el = $(this);
    let val = $el.val();

    // Normalize value: handle null, arrays, numbers
    if (Array.isArray(val)) {
        // multi-select returns array
        val = val.length ? val.join(',') : '';
    } else if (val === null || typeof val === 'undefined') {
        val = '';
    } else {
        val = String(val);
    }

    if (val.trim() !== '') {
        // remove inline border
        $el.css('border', '');

        // remove validation message if present
        const id = $el.attr('id');
        if (id) {
            $('span[data-valmsg-for="' + id + '"]').text('');
        }

        // If using Choices.js, remove from wrapper
        const $choicesInner = $el.closest('.choices').find('.choices__inner');
        if ($choicesInner.length) {
            $choicesInner.css('border', '');
        }

        // If using Select2, remove from select2 selection container
        const $select2Container = $el.next('.select2-container').find('.select2-selection');
        if ($select2Container.length) {
            $select2Container.css('border', '');
        }
    }
});

// Additional listeners for Select2 plugin events (if used)
$(document).on('select2:select select2:unselect select2:close', 'select', function () {
    $(this).trigger('change'); // reuse the above handler
});
//
function initializeMonthYearPicker(selector) {
    $(selector).flatpickr({
        altInput: true,
        altFormat: "F Y",
        dateFormat: "Y-m-d",
        plugins: [
            new monthSelectPlugin({
                shorthand: true,
                dateFormat: "Y-m-d",
                altFormat: "F Y",  // Shown in the input
                theme: "light"
            })
        ]
    });
}

//
function initializeAssignedDatePickers(fromId, toId) {

    let toPicker; // reference for "To" picker

    // FROM DATE
    const fromPicker = flatpickr(`#${fromId}`, {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d/m/Y",
        allowInput: true,
        onReady: function (selectedDates, dateStr, instance) {
            instance.input.placeholder = "dd/mm/yyyy";
        },
        onChange: function (selectedDates) {

            if (selectedDates.length > 0) {
                const fromDate = selectedDates[0];

                // set minDate for TO
                toPicker.set('minDate', fromDate);

                // clear TO if invalid
                if (toPicker.selectedDates.length > 0 &&
                    toPicker.selectedDates[0] < fromDate) {
                    toPicker.clear();
                }
            }
        }
    });

    // TO DATE
    toPicker = flatpickr(`#${toId}`, {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d/m/Y",
        allowInput: true,
        onReady: function (selectedDates, dateStr, instance) {
            instance.input.placeholder = "dd/mm/yyyy";
        }
    });
}

//
function initializeDatepickerDMY(dateIds) {
    if (!dateIds) return;
    if (typeof dateIds !== 'string') {
        const el = (dateIds instanceof HTMLElement) ? dateIds : ((window.jQuery && dateIds instanceof jQuery) ? dateIds[0] : null);
        if (el && typeof flatpickr !== 'undefined' && !el._flatpickr) {
            flatpickr(el, {
                dateFormat: "Y-m-d",
                altInput: true,
                altFormat: "d/m/Y",
                allowInput: true,
                onReady: function (selectedDates, dateStr, instance) {
                    instance.input.placeholder = "dd/mm/yyyy";
                }
            });
        }
        return;
    }
    dateIds.split(',').forEach(function (id) {
        const trimmedId = id.trim(); // remove any leading/trailing spaces
        if (!trimmedId) return;
        flatpickr(`#${trimmedId}`, {
            dateFormat: "Y-m-d",       // for backend
            altInput: true,            // enables display input
            altFormat: "d/m/Y",        // what the user sees
            allowInput: true,
            onReady: function (selectedDates, dateStr, instance) {
                instance.input.placeholder = "dd/mm/yyyy";
            }
        });
    });
}

// OnlyTOday  Selected
function initializeDatepickerDMYOnlyToday(dateIds) {
    const today = new Date();

    dateIds.split(',').forEach(function (id) {
        const trimmedId = id.trim();
        const inputElement = document.getElementById(trimmedId);

        if (!inputElement) return;

        flatpickr(`#${trimmedId}`, {
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            allowInput: false,
            clickOpens: false,
            minDate: today,
            maxDate: today,
            defaultDate: today,
            onReady: function (selectedDates, dateStr, instance) {
                instance.input.placeholder = "dd/mm/yyyy";
            }
        });
    });
}

//
function initializeDatepickerDMY2(dateIds, minDate = null, maxDate = null) {
   
    dateIds.split(',').forEach(function (id) {
        const trimmedId = id.trim();
        flatpickr(`#${trimmedId}`, {
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d/m/Y",
            allowInput: true,
            minDate: minDate || undefined,
            maxDate: maxDate || undefined,
            onReady: function (selectedDates, dateStr, instance) {
                instance.input.placeholder = "dd/mm/yyyy";
            }
        });
    });
}


// Hide Date less than Today date
function updateDatepickerWithMinDate(dateId, minDate, options = {}) {
    // Destroy existing flatpickr instance if any
    const input = document.getElementById(dateId);
    if (input && input._flatpickr) {
        input._flatpickr.destroy();
    }
    console.log(dateId, minDate);
    const defaultOptions = {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d/m/Y",
        allowInput: true,
        minDate: minDate,
        maxDate: options.maxDate || null,
        onReady: function (selectedDates, dateStr, instance) {
            instance.input.placeholder = "dd/mm/yyyy";
        }
    };

    flatpickr(`#${dateId}`, { ...defaultOptions, ...options });
}


function updateDatepickerWithMinDateTotalDays(dateId, minDate, options = {}, displayDaysId = null, baseDateId = null) {
    const input = document.getElementById(dateId);
    if (input && input._flatpickr) {
        input._flatpickr.destroy();
    }

    const defaultOptions = {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "d/m/Y",
        allowInput: true,
        minDate: minDate,
        onReady: function (selectedDates, dateStr, instance) {
            instance.input.placeholder = "dd/mm/yyyy";
        },
        onChange: function (selectedDates, dateStr, instance) {
            if (selectedDates.length && displayDaysId && baseDateId) {
                const selectedDate = selectedDates[0];
                const baseDateVal = document.getElementById(baseDateId).value;
                if (baseDateVal) {
                    const baseDate = new Date(baseDateVal);
                    const daysDiff = Math.ceil((selectedDate - baseDate) / (1000 * 60 * 60 * 24)) + 1;

                    // Display in span
                    document.getElementById(displayDaysId).innerText = daysDiff;

                    // Set in hidden field if exists
                    const hiddenInput = document.getElementById(displayDaysId);
                    if (hiddenInput) {
                        hiddenInput.value = daysDiff;
                    }
                }
            }
        }
    };

    flatpickr(`#${dateId}`, { ...defaultOptions, ...options });
}

// Date range Global   Pair calender 

function initializeGlobalDateRangePicker(pickerId, fromHiddenId, toHiddenId, onChangeCallback) {

    $(`#${pickerId}`).dateRangePicker({
        format: 'DD/MM/YYYY',
        separator: ' to ',
        language: 'en',
        autoClose: true,
        getValue: function () {
            return $(this).val();
        },
        setValue: function (s) {
            $(this).val(s);
        }
    })
        .bind('datepicker-change datepicker-apply', function (event, obj) {
            const start = moment(obj.date1).format("YYYY-MM-DD");
            const end = moment(obj.date2).format("YYYY-MM-DD");

            $(`#${fromHiddenId}`).val(start);
            $(`#${toHiddenId}`).val(end);

            if (typeof onChangeCallback === 'function') {
                onChangeCallback();
            }
        });
}

// Date range Global   Pair calender  end

// Time Picker

function initializeTimePickerById(elementClassList) {
    const classes = elementClassList.split(',');
    classes.forEach(className => {
        flatpickr(`.${className.trim()}`, {
            enableTime: true,
            noCalendar: true,
            dateFormat: "H:i",
            time_24hr: true,
            disableMobile: true,
            allowInput: true,
            clickOpens: true,
            defaultDate: null
        });
    });
}


// Global Method for Delete

async function globalSoftDelete(controllerName, ids, reloadCallback = null, actionName = "SoftDelete") {
    try {
        const response = await fetch(`/${controllerName}/${actionName}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                ids: Array.isArray(ids) ? ids : [ids]
            })
        });

        const data = await response.json();

        if (data.success)
        {
            toastr.success(data.message);

            if (reloadCallback && typeof reloadCallback === "function")
            {
                reloadCallback();
            }
        } else
        {
            toastr.error(data.message);
        }
    } catch (error)
    {
        toastr.error("Error occurred while deleting.");
    }
}
