//#region Validation

function isValidEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

function getLabelText(inputId) {
    var $label = $('label[for="' + inputId + '"]');
    return $label.length ? $label.text().trim() : 'This field';
}

function normalizeId(selectorOrId) {
    return selectorOrId.startsWith("#") ? selectorOrId.slice(1) : selectorOrId;
}

function showError(inputId, message) {

    const inputId1 = normalizeId(inputId);


    const $input = $("#" + inputId1);

    if ($input.is("input")) {
        $input.addClass("is-invalid"); // <-- add this line 'test' +

        if ($input.next(".text-danger").length === 0) {
            const $error = $('<span class="text-danger">' + message + '</span>').css({
                position: 'absolute',
                fontSize: '0.8rem',
                zIndex: 10,
                //marginTop: '2px',
                left: 0
            });
            $input.after($error);
        }
        attachInputValidationHandler($input);
    }




}

function removeError(inputId) {
    const $input = $("#" + inputId);
    $input.removeClass("is-invalid"); // <-- remove the class

    $input.next(".text-danger").remove();
}

function clearErrors() {
    $(".text-danger").remove();
}

// New function to validate fields
function validateFields(fields) {
    clearErrors();
    let valid = true;

    //fields.forEach(fieldId => {
    //    const value = $("#" + fieldId).val().trim();
    //    const labelText = getLabelText(fieldId);

    //    if (!value) {
    //        showError(fieldId, labelText + " is required.");
    //        valid = false;
    //    }
    //    // No custom validation functions here
    //});

    const foundFields = [];
    const missingFields = [];


    fields.forEach(fieldId1 => {
        const $field = $("#" + fieldId1);
        if ($field.length === 0) {
            console.warn(`Element with ID '${fieldId1}' not found.`);
            missingFields.push(fieldId1);

        } else {

            foundFields.push(fieldId1);

        }
    });


    foundFields.forEach(fieldId => {

        const $field = $("#" + fieldId);
        if ($field.length === 0) {
            console.warn(`Element with ID '${fieldId}' not found.`);
            valid = false;
            return;
        }

        const value = $field.val().trim();
        const labelText = getLabelText(fieldId);

        if (!value) {
            showError(fieldId, labelText + " is required.");
            valid = false;
        }
    });

    return valid;
}


function attachInputValidationHandler(selectors) {
    $(selectors).on("input", function () {
        const id = $(this).attr("id");
        const value = $(this).val().trim();

        if (value !== "") {
            if (id === "personalEmail" && !isValidEmail(value)) {
                return; // still invalid email, keep error
            }
            removeError(id);
        }
    });
}

//#endregion
