let currentView = 'profile';

$(document).ready(function () {

    console.log("viewType from Razor:",viewType);

    if (viewType === 'list') {
        loadListPreview();
        currentView = 'list';
        $("#reportTitle").text("Customer List Report");
        $("#listViewBtn").addClass('btn-primary').removeClass('btn-outline-primary');
        $("#profileViewBtn").removeClass('btn-primary').addClass('btn-outline-primary');
    } else {
        loadProfilePreview();
        currentView = 'profile';
        $("#reportTitle").text("Customer Profile Report");
        $("#profileViewBtn").addClass('btn-primary').removeClass('btn-outline-primary');
        $("#listViewBtn").removeClass('btn-primary').addClass('btn-outline-primary');
    }
});


function loadProfilePreview() {
    $.ajax({
        url: profileUrl, 
        type: 'GET',
        xhrFields: { responseType: 'blob' },
        success: function (data) {
            var blob = new Blob([data], { type: 'application/pdf' });
            var url = URL.createObjectURL(blob);
            $('#pdfPreview').attr('src', url); 
        },
        error: function () {
            alert('Error loading PDF preview');
        }
    });
}

function loadListPreview() {
    $.ajax({
        url: listUrl,
        type: 'GET',
        xhrFields: { responseType: 'blob' },
        success: function (data) {
            var blob = new Blob([data], { type: 'application/pdf' });
            var url = URL.createObjectURL(blob);
            $('#pdfPreview').attr('src', url);
        },
        error: function () { }
    });
}

function downloadReport(format) {
    if (currentView === 'profile') {
        downloadProfileReport(format);
    } else {
        downloadListReport(format);
    }
}

function downloadProfileReport(format) {
    var form = $('<form>', {
        'action': downloadProfileUrl,
        'method': 'POST'
    });

    form.append($('<input>', {
        'type': 'hidden',
        'name': 'format',
        'value': format
    }));

    $('body').append(form);
    form.submit();
    form.remove();
}

function downloadListReport(format) {
    var form = $('<form>', {
        'action': downloadListUrl,
        'method': 'GET'
    });

    form.append($('<input>', {
        'type': 'hidden',
        'name': 'format',
        'value': format
    }));

    $('body').append(form);
    form.submit();
    form.remove();
}


