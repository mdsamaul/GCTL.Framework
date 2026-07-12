(function ($) {
    $.logo = function (options) {
        // Default options
        var settings = $.extend({
            baseUrl: '/',
            form: '#layoutManagement-form',
        }, options);

        var gridUrl = settings.baseUrl + "/GetAll";
        $(() => {

            $(document).ready(function () {
                const $input = $("#actualFileInput");
                const $previewImage = $("#previewImage");
                const $removeButton = $("#removeImage");

                $input.on("change", function () {
                    const file = this.files[0];
                    if (file) {
                        const reader = new FileReader();
                        reader.onload = function (e) {
                            $previewImage.attr("src", e.target.result);
                        };
                        reader.readAsDataURL(file);
                    }
                });

                $removeButton.on("click", function (e) {
                    e.preventDefault();
                    $input.val(""); // Clears the input
                    $previewImage.attr("src", "/assets/img/icons/file-bg.png"); // Set default image
                });

                // Optional: if you have a trigger button to open file input
                $(".dz-upload-btn").on("click", function () {
                    $input.click();
                });
            });


        });
    }
}(jQuery));

