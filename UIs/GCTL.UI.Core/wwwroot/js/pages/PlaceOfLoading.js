(function ($) {

    $.placeOfLoading = function (options) {

        //#region Default Options

        var settings = $.extend({

            baseUrl: "/",

            formSelector: "#PlaceOfLoading-form",
            formContainer: ".js-PlaceOfLoading-form-container",

            gridSelector: "#PlaceOfLoading-grid",
            gridContainer: ".js-PlaceOfLoading-grid-container",

            editSelector: ".js-PlaceOfLoading-edit, .js-grid-edit",
            saveSelector: ".js-PlaceOfLoading-save",

            selectAllSelector: "#PlaceOfLoading-check-all",

            deleteSelector: ".js-PlaceOfLoading-delete-confirm",
            deleteModal: "#gcDeleteModal",
            finalDeleteSelector: ".js-gc-delete-confirm",

            clearSelector: ".js-PlaceOfLoading-clear",

            topSelector: ".js-go",

            decimalSelector: ".js-PlaceOfLoading-decimalplaces",

            maxDecimalPlace: 5,
            showNegativeFormat: false,

            availabilitySelector: ".js-PlaceOfLoading-check-duplicateName",

            hasFile: false,

            quickAddSelector: ".js-quick-add",
            quickAddModal: "#quickAddModal",

            lastCodeSelector: "#lastCode",

            load: function () { }

        }, options);

        //#endregion

        //#region Private Variables

        var saveUrl = settings.baseUrl + "/Setup";
        var deleteUrl = settings.baseUrl + "/Delete";

        var selectedItems = [];
        var deleteModalHandlerAttached = false;
        var quickAddModalVariables = {
            loadUrl: "",
            target: "",
            reloadUrl: "",
            title: ""
        };
        var debounceTimer = null;

        //#endregion

        //#region Initialization

        $(document).ready(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });

        $(() => {
            if ($.gctlGrid) {
                $.gctlGrid.initFromModel({ gridId: 'PlaceOfLoading-grid' });
            }
            bindEventHandlers();
        });

        //#endregion

        //#region Event Handlers

        function bindEventHandlers() {

            //#region Edit

            // Fires when a row's edit link is clicked (grid-generated .js-PlaceOfLoading-edit)
            $("body").on("click",
                settings.editSelector,
                function (e) {
                    debugger
                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();

                    const id = $(this).data("id");
                    const url = saveUrl + (id ? "/" + id : "");

                    loadForm(url)
                        .then(function () {
                            console.log("Form Loaded");
                        })
                        .catch(function (err) {
                            console.error("Form loading error:", err);
                            toastr.error("Failed to load form. Please try again.");
                        });

                    // Keep the delete-confirm button aware of which row is active
                    $(settings.deleteSelector).data("id", id);

                    $("html,body").animate({
                        scrollTop: 0
                    }, 500);

                });

            //#endregion

            //#region Clear

            $("body").on("click",
                settings.clearSelector,
                function (e) {

                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();

                    // Reset the "active row" id so delete-confirm falls back to checkbox selection
                    $(settings.deleteSelector).removeData("id");

                    loadForm(saveUrl)
                        .then(function () {
                            console.log("Form Cleared");
                        })
                        .catch(function (err) {
                            console.error("Form loading error:", err);
                            toastr.error("Failed to clear form. Please try again.");
                        });

                    $("html,body").animate({
                        scrollTop: 0
                    }, 500);

                });

            //#endregion

            //#region Save (handles both single Add and Update)

            $("body").on("click",
                settings.saveSelector,
                function () {

                    const self = this;

                    if (!validation()) {
                        return false;
                    }

                    const isFormValid = $(settings.formSelector).valid();

                    if (!isFormValid) {
                        return false;
                    }

                    let data;
                    if (settings.hasFile) {
                        data = new FormData($(settings.formSelector)[0]);
                    } else {
                        data = $(settings.formSelector).serialize();
                    }

                    const url = $(settings.formSelector).attr("action");

                    const ajaxOption = {
                        url: url,
                        method: "POST",
                        data: data,
                        success: function (response) {
                            handleSaveSuccess(response, self);
                        },
                        error: function (xhr, status, error) {
                            console.error("Save error:", error);
                            toastr.error("An error occurred while saving. Please try again.");
                        }
                    };

                    if (settings.hasFile) {
                        ajaxOption.processData = false;
                        ajaxOption.contentType = false;
                    }

                    $.ajax(ajaxOption);

                });

            //#endregion



            //#region Delete Button (single row OR multi checkbox selection)

            $("body").on("click", settings.deleteSelector, function (e) {

                e.preventDefault();

                selectedItems = []; // Reset before collecting

                const rowId = $(this).data("id");

                if (rowId) {

                    // Single delete: triggered from an edited/active row
                    selectedItems.push(rowId);

                } else {

                    // Multi delete: triggered from grid checkbox selection
                    $("input:checkbox.checkBox:checked").each(function () {
                        const value = $(this).val();
                        if (value && !selectedItems.includes(value)) {
                            selectedItems.push(value);
                        }
                    });

                }

                if (selectedItems.length > 0) {
                    $(settings.deleteModal).modal("show");
                } else {
                    toastr.info("Please select at least one item.");
                }

            });

            //#endregion

            //#region Delete Modal

            $("body").on("show.bs.modal",
                settings.deleteModal,
                function (event) {

                    const modal = $(this);
                    modal.find(".title").html("Are you sure you want to delete these items?");

                    // Attach handler only once per modal instance
                    if (!deleteModalHandlerAttached) {
                        modal.on("click",
                            settings.finalDeleteSelector,
                            function (e) {

                                e.preventDefault();
                                e.stopPropagation();
                                e.stopImmediatePropagation();

                                performDelete(modal);

                            });

                        deleteModalHandlerAttached = true;
                    }

                }).on("hide.bs.modal", function () {
                    deleteModalHandlerAttached = false;
                });

            //#endregion

            //#region Top Button

            $("body").on("click", settings.topSelector, function (e) {
                e.preventDefault();
                $("html,body").animate({
                    scrollTop: 0
                }, 500);
            });

            //#endregion

            //#region Decimal

            $("body").on("keyup",
                settings.decimalSelector,
                function () {
                    const self = $(this);
                    showDecimalPlaces(
                        self.val(),
                        self.parent().find(".input-group-text")
                    );
                });

            //#endregion

            //#region Duplicate Check (Debounced)

            $("body").on("keyup",
                settings.availabilitySelector,
                function () {

                    clearTimeout(debounceTimer);

                    const self = $(this);
                    const code = $(".js-PlaceOfLoadingId-code").val();
                    const name = self.val();

                    if (!name || !code) {
                        return;
                    }

                    debounceTimer = setTimeout(function () {
                        checkAvailability(code, name);
                    }, 500);

                });

            //#endregion

            //#region Quick Add

            $("body").on("click",
                settings.quickAddSelector,
                function (e) {

                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();

                    quickAddModalVariables.loadUrl = $(this).data("url");
                    quickAddModalVariables.target = $(this).data("target");
                    quickAddModalVariables.reloadUrl = $(this).data("reload-url");
                    quickAddModalVariables.title = $(this).data("title");

                    $(settings.quickAddModal + " .modal-title")
                        .html(quickAddModalVariables.title);

                    $(settings.quickAddModal + " .modal-body")
                        .empty()
                        .load(quickAddModalVariables.loadUrl, function () {

                            $(settings.quickAddModal).modal("show");

                            $("#header").hide();
                            $("#left_menu").hide();
                            $("#main-content").toggleClass("collapse-main");
                            $("body").removeClass("sidebar-mini");

                        });

                });

            $("body").on("click",
                ".js-modal-dismiss",
                function () {

                    $("body")
                        .removeClass("sidebar-mini")
                        .addClass("sidebar-mini");

                    $("#header").show();
                    $("#left_menu").show();
                    $("#main-content").toggleClass("collapse-main");

                    const lastCode = $(settings.quickAddModal + " #lastCode").val();

                    $(settings.quickAddModal + " .modal-body").empty();
                    $(settings.quickAddModal).modal("hide");

                    reloadQuickAddDropdown(lastCode);

                });

            //#endregion

        }

        //#endregion

        //#region Helper Functions

        function handleSaveSuccess(response, buttonElement) {

            if (response.isSuccess) {

                toastr.success(response.message);

                const modal = $(buttonElement).closest(".modal");

                if (modal.length && window.GcModal) {

                    GcModal.markSuccess(modal, response.lastCode);
                    GcModal.close(modal);

                } else {

                    loadForm(saveUrl)
                        .then(function () {
                            loadTable();
                        });

                }

            } else {

                toastr.error(response.message);

            }

        }

        function performDelete(modal) {

            if (!selectedItems || selectedItems.length === 0) {
                toastr.error("No items selected for deletion.");
                return;
            }

            $.ajax({

                url: deleteUrl,
                method: "POST",
                contentType: "application/json",
                data: JSON.stringify(selectedItems),

                success: function (response) {

                    modal.modal("hide");

                    if (response.success) {

                        toastr.success(response.message);
                        selectedItems = [];
                        $(settings.deleteSelector).removeData("id");
                        loadTable();
                        loadForm(saveUrl);

                    } else {

                        toastr.error(response.message);

                    }

                },

                error: function (xhr, status, error) {
                    console.error("Delete error:", error);
                    toastr.error("An error occurred while deleting. Please try again.");
                }

            });

        }

        function checkAvailability(code, name) {

            $.ajax({

                url: settings.baseUrl + "/CheckAvailability",
                method: "POST",
                data: {
                    code: code,
                    name: name
                },

                success: function (response) {

                    if (response.isSuccess) {
                        toastr.error(response.message);
                    }

                },

                error: function (xhr, status, error) {
                    console.error("Availability check error:", error);
                }

            });

        }

        function reloadQuickAddDropdown(lastCode) {

            const target = quickAddModalVariables.target;
            const reloadUrl = quickAddModalVariables.reloadUrl;

            $(target).empty().append(
                $("<option>", {
                    value: "",
                    text: "--Select " + quickAddModalVariables.title + "--"
                })
            );

            $.ajax({

                url: reloadUrl,
                method: "GET",

                success: function (response) {

                    if (response && Array.isArray(response)) {

                        $.each(response, function (i, item) {
                            $(target).append(
                                $("<option>", {
                                    value: item.code,
                                    text: item.name
                                })
                            );
                        });

                        if (lastCode) {
                            $(target).val(lastCode);
                        }

                    } else {

                        toastr.error("Invalid response format.");

                    }

                },

                error: function (xhr, status, error) {
                    console.error("Dropdown reload error:", error);
                    toastr.error("Failed to reload dropdown. Please refresh the page.");
                }

            });

        }

        //#endregion

        //#region loadTable

        function loadTable() {
            if ($.fn.DataTable && $.fn.DataTable.isDataTable(settings.gridSelector)) {
                $(settings.gridSelector).DataTable().ajax.reload();
            } else if (window.loadGridPage) {
                window.loadGridPage('PlaceOfLoading-grid', settings.baseUrl + "/GetTableData", 1, 10);
            }
        }

        //#endregion

        //#region loadForm

        function loadForm(url) {

            return new Promise(function (resolve, reject) {

                $.ajax({

                    url: url,
                    type: "GET",
                    cache: false,

                    success: function (data) {

                        $(settings.formContainer).empty().html(data);

                        $.validator.unobtrusive.parse(
                            $(settings.formSelector)
                        );

                        if (window.initCustomDropdowns) {
                            window.initCustomDropdowns(
                                settings.formContainer,
                                true
                            );
                        }

                        resolve(data);

                    },

                    error: function (err) {
                        console.error("Form loading error:", err);
                        reject(err);
                    }

                });

            });

        }

        //#endregion

        //#region Validation

        function validation() {

            const value = $("#PlaceOfLoading").val();

            if (!value || value.trim() === "") {

                toastr.info("Enter Place Of Loading");
                $("#PlaceOfLoading").trigger("focus");
                return false;

            }

            return true;

        }

        //#endregion

    };

})(jQuery);