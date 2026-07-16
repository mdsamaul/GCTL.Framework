(function ($) {

    var GcDelete = {
        modalSelector: "#gcDeleteModal",
        currentCallback: null,

        confirm: function (message, onConfirm) {
            var $modal = $(this.modalSelector);
            $modal.find(".title").text(message || "Are you sure you want to delete this item?");
            this.currentCallback = onConfirm;

            var modalInstance = bootstrap.Modal.getOrCreateInstance($modal[0]);
            modalInstance.show();
        }
    };

    window.GcDelete = GcDelete;

    $("body").on("click", ".js-gc-delete-confirm", function () {
        var $modal = $(GcDelete.modalSelector);
        var modalInstance = bootstrap.Modal.getInstance($modal[0]);

        if (typeof GcDelete.currentCallback === "function") {
            GcDelete.currentCallback();
        }

        if (modalInstance) modalInstance.hide();
    });

})(jQuery);