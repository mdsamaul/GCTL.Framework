$(document).ready(function () {
    $.ajax({
        url: '/Language/GetLanguageOnSession',
        type: 'GET',
        dataType: 'text', 
        success: function (response) {
            console.log("Data received:", response);

          

           
            $("#languageCode").val(response); 
        },
        error: function (error) {
            console.error("Error fetching data:", error);
        }
    });
});