
function showDeleteModal(deleteCallback) {
    // Show the modal
    $('#confirmDeleteModal').modal('show');

    $('#confirmDeleteBtn').off('click').on('click', function () {
        deleteCallback();
        $('#confirmDeleteModal').modal('hide');
    });
};

$(function () {
    $('.toolbar-btn-group').closest('div[class*="d-flex"]').each(function () {
        $(this).addClass('sticky-bar');
    });
});



function showLoadingIndicator() {
    if ($('#loadingIndicator').length === 0) {
        $('body').append(`
                    <div id="loadingIndicator" style="display:none; position: fixed; top: 35%; left: 35%; z-index: 9999;">
                        <img src="/media/indicator/spinner.gif" alt="Loading..." />
                    </div>
                `);
    }

    $("#loadingIndicator").show();
    // $('body').css('filter', 'blur(5px)');
}
function hideLoadingIndicator() {
    $("#loadingIndicator").hide();
}


//#region base Loading indicator 

function showLoadingBaseIndicator(message) {
    // Optional: If you want to show a message, you can add a <h5> or similar in your partial view and control it here.
    if (message) {
        var messageElement = document.getElementById('loadingMessage');
        if (messageElement) {
            messageElement.textContent = message;
            messageElement.style.display = 'block';
        }
    }

    // Show the loader
    var loaderWrapper = document.querySelector('.custom-loader-wrapper');
    if (loaderWrapper) {
        loaderWrapper.style.display = 'flex'; // flex to center properly
    }
}

function hideLoadingBaseIndicator() {
   
    var loaderWrapper = document.querySelector('.custom-loader-wrapper');
    if (loaderWrapper) {
        loaderWrapper.style.display = 'none';
    }
}

//#endregion




// document.addEventListener("DOMContentLoaded", function () {
//     feather.replace();
// });




// #region For sort icon
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("th.sort").forEach(function (th) {
        const icon = document.createElement("span");
        icon.className = "sort-icon ms-2";
        icon.innerHTML = `<i class="fas fa-sort small text-muted"></i>`;
        th.appendChild(icon);
    });
});
// #endregion




// #region For flatpicker datepicker
//document.addEventListener("DOMContentLoaded", function () {
//    flatpickr(".datetimepicker", {
//        //onDayCreate: function (dObj, dStr, fp, dayElem) {
//        //    const dayNumber = parseInt(dayElem.textContent);
//        //    const year = fp.currentYear;
//        //    const month = fp.currentMonth;

//        //    // Check if the day is part of the currently displayed month
//        //    if (
//        //        !isNaN(dayNumber) &&
//        //        !dayElem.classList.contains("prevMonthDay") &&
//        //        !dayElem.classList.contains("nextMonthDay")
//        //    ) {
//        //        const date = new Date(year, month, dayNumber);
//        //        const dayOfWeek = date.getDay(); // 5 = Friday, 6 = Saturday

//        //        if (dayOfWeek === 5 || dayOfWeek === 6) {
//        //            dayElem.style.backgroundColor = "#FFA500"; // #FFA500 = Orange, gray = #e8eaec
//        //            dayElem.style.color = "#ffffff";
//        //            dayElem.style.borderRadius = "50%";
//        //        }
//        //    }
//        //}
//    });
//});
// #endregion

window.setField = function (selector, value, disabled = false) {
    var $el = $(selector);

    if ($el.is('input, textarea, select')) {
        $el.val(value || '');

        if ($el.is('select')) {
            $el.prop('disabled', !!disabled);
        } else {
            $el.prop('readonly', !!disabled);
            $el.prop('disabled', false);
        }
    } else {
        // span, div etc. - plaintext display
        $el.text(value || '');
    }
};



// #region Validation
function validateField(fieldId, response) {
    const $field = $('#' + fieldId);
    const $errorSpan = $('#' + fieldId + 'Error');
    const hasError = response.field === fieldId;
    const hasValue = $field.val();

    const flatpickrInstance = $field[0]?._flatpickr;
    const $flatpickerInner = flatpickrInstance?.altInput ? $(flatpickrInstance.altInput) : $field;

    const isChoices = $field.closest('.choices').length > 0;
    const $choicesInner = isChoices ? $field.closest('.choices').find('.choices__inner') : null;

    // Reset previous styles
    $flatpickerInner.removeClass('is-invalid is-valid');
    $errorSpan.hide().text('');
    if ($choicesInner) {
        $choicesInner.removeClass('border-danger border-success');
    }

    // Apply validation feedback
    if (hasError) {
        $flatpickerInner.addClass('is-invalid');
        $errorSpan.text(response.message).show();
        if ($choicesInner) $choicesInner.addClass('border-danger');
    } else if (hasValue) {
        $flatpickerInner.addClass('is-valid');
        if ($choicesInner) $choicesInner.addClass('border-success');
    }
}
// #endregion

//

//#region Dev messgae

const dev = true;



function showDev(message, headerText = 'console') {
    // Show only in localhost
    if (!dev || (location.hostname !== "localhost" && location.hostname !== "127.0.0.1")) return;

    let container = document.getElementById("custom-toast-container");
    if (!container) {
        container = document.createElement("div");
        container.id = "custom-toast-container";
        document.body.appendChild(container);

        Object.assign(container.style, {
            position: "fixed",
            display: "flex",
            flexDirection: "column",
            gap: "10px",
            zIndex: "9999",
        });

        // Load position from localStorage
        let savedPosition = localStorage.getItem("dev-toast-position") || "bottom-left";
        setContainerPosition(container, savedPosition);
    }

    const toast = document.createElement("div");

    Object.assign(toast.style, {
        background: "#333",
        color: "#fff",
        borderRadius: "5px",
        fontSize: "14px",
        boxShadow: "0 2px 6px rgba(0,0,0,0.3)",
        opacity: "0",
        transform: "translateY(20px)",
        transition: "opacity 0.3s ease, transform 0.3s ease",
        pointerEvents: "auto",
        width: "300px",
        maxWidth: "800px",
        resize: "both",
        overflow: "auto",
        maxHeight: "450px",
        display: "flex",
        flexDirection: "column",
    });

    // Header with Position Toggle + Copy + Close
    const header = document.createElement("div");
    Object.assign(header.style, {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        background: "#444",
        padding: "3px 5px",
        borderTopLeftRadius: "5px",
        borderTopRightRadius: "5px",
        gap: "5px",
    });

    const positionBtn = document.createElement("button");
    positionBtn.textContent = "⇔";
    Object.assign(positionBtn.style, {
        background: "#555",
        color: "#fff",
        border: "none",
        padding: "2px 6px",
        fontSize: "12px",
        borderRadius: "3px",
        cursor: "pointer",
    });

    positionBtn.addEventListener("click", () => {
        let current = localStorage.getItem("dev-toast-position") || "bottom-left";
        let next;
        switch (current) {
            case "bottom-left": next = "bottom-right"; break;
            case "bottom-right": next = "top-right"; break;
            case "top-right": next = "top-left"; break;
            default: next = "bottom-left";
        }
        localStorage.setItem("dev-toast-position", next);
        setContainerPosition(container, next);
    });

    const lineInfo = getLineInfo();

    const consoleText = document.createElement("span");
    consoleText.textContent = `${headerText} ${lineInfo}`;
    //consoleText.textContent = headerText;
    Object.assign(consoleText.style, {
        fontSize: "14px",
        whiteSpace: "nowrap",
        flex: "1",
    });

    const headerActions = document.createElement("div");
    Object.assign(headerActions.style, {
        display: "flex",
        gap: "5px",
        alignItems: "center",
    });

    const copyBtn = document.createElement("button");
    copyBtn.textContent = "Copy";
    Object.assign(copyBtn.style, {
        background: "#666",
        color: "#fff",
        border: "none",
        padding: "2px 6px",
        fontSize: "12px",
        borderRadius: "3px",
        cursor: "pointer",
    });

    const closeBtn = document.createElement("button");
    closeBtn.textContent = "×";
    Object.assign(closeBtn.style, {
        background: "transparent",
        color: "#fff",
        border: "none",
        fontSize: "16px",
        fontWeight: "bold",
        cursor: "pointer",
        lineHeight: "1",
    });

    closeBtn.addEventListener("click", () => {
        toast.style.opacity = "0";
        toast.style.transform = "translateY(20px)";
        setTimeout(() => toast.remove(), 300);
    });

    header.appendChild(positionBtn);
    header.appendChild(consoleText);
    headerActions.appendChild(copyBtn);
    headerActions.appendChild(closeBtn);
    header.appendChild(headerActions);
    toast.appendChild(header);

    // Body
    const body = document.createElement("div");
    Object.assign(body.style, {
        padding: "10px 15px",
        overflowY: "auto",
        fontFamily: "monospace",
        whiteSpace: "pre-wrap",
        wordBreak: "break-word",
    });

    let rawText;
    if (typeof message === "object") {
        rawText = JSON.stringify(message, null, 2);
        body.textContent = rawText;
    } else {
        rawText = message;
        body.textContent = message;
    }

    copyBtn.addEventListener("click", () => {
        navigator.clipboard.writeText(rawText).then(() => {
            copyBtn.textContent = "Copied!";
            setTimeout(() => (copyBtn.textContent = "Copy"), 1000);
        });
    });

    toast.appendChild(body);
    container.appendChild(toast);

    requestAnimationFrame(() => {
        toast.style.opacity = "1";
        toast.style.transform = "translateY(0)";
    });

    let hideTimeout;
    let isHovered = false;

    const scheduleRemoval = () => {
        hideTimeout = setTimeout(() => {
            if (!isHovered) {
                toast.style.opacity = "0";
                toast.style.transform = "translateY(20px)";
                setTimeout(() => toast.remove(), 300);
            }
        }, 3000);
    };

    toast.addEventListener("mouseenter", () => {
        isHovered = true;
        clearTimeout(hideTimeout);
    });

    toast.addEventListener("mouseleave", () => {
        isHovered = false;
        scheduleRemoval();
    });

    scheduleRemoval();
}

function setContainerPosition(container, position) {
    // Reset all
    container.style.top = "";
    container.style.bottom = "";
    container.style.left = "";
    container.style.right = "";

    switch (position) {
        case "bottom-left":
            container.style.bottom = "20px";
            container.style.left = "20px";
            break;
        case "bottom-right":
            container.style.bottom = "20px";
            container.style.right = "20px";
            break;
        case "top-right":
            container.style.top = "20px";
            container.style.right = "20px";
            break;
        case "top-left":
            container.style.top = "20px";
            container.style.left = "20px";
            break;
    }
}

function getLineInfo() {
    const err = new Error();
    const stack = err.stack?.split("\n");

    if (stack && stack.length >= 3) {
        // 3rd line = caller
        const line = stack[2];

        // Extract file:line:column
        const match = line.match(/:(\d+):(\d+)/);
        if (match) {
            return ` ${match[1]}`;
        }
    }
    return "";
}


//#endregion

//#region HideModal

//hideModal('new_resignation');
function hideModal(id) {
   
    const modalEl = document.getElementById(id);
    if (!modalEl) return;

    let modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (!modalInstance) {
        modalInstance = new bootstrap.Modal(modalEl);
    }

    modalInstance.hide();
}

//#endregion