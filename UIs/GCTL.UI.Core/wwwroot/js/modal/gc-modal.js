(function ($) {

    var GcModal = {
        stack: [],
        containerSelector: "#gcModalStack",

        open: function (opts) {
            var self = this;
            var level = self.stack.length;
            var modalId = "gcModal_" + level + "_" + Date.now();

            var state = {
                id: modalId,
                target: opts.target,
                reloadUrl: opts.reloadUrl,
                title: opts.title,
                lastCode: null,
                dataAdded: false,
                level: level
            };

            var $modal = $(
                '<div class="modal fade" id="' + modalId + '" tabindex="-1" role="dialog" ' +
                'data-bs-backdrop="static" data-bs-keyboard="false" aria-hidden="true">' +
                '  <div class="modal-dialog modal-lg" role="document" style="min-width:80% !important">' +
                '    <div class="modal-content">' +
                '      <div class="modal-header" style="display:flex; justify-content:space-between; align-items:center;">' +
                '        <h5 class="modal-title">' + (opts.title || "Quick Add") + '</h5>' +
                '        <button type="button" class="close js-gc-modal-close" aria-label="Close" style="margin-left:auto;">' +
                '          <span aria-hidden="true">&times;</span>' +
                '        </button>' +
                '      </div>' +
                '      <div class="modal-body"><div class="gc-modal-loading text-center p-4">Loading...</div></div>' +
                '    </div>' +
                '  </div>' +
                '</div>'
            );

            $(self.containerSelector).append($modal);
            state.$modal = $modal;

            if (self.stack.length === 0) {
                self.hideChrome();
            }

            self.stack.push(state);

            $.ajax({
                url: opts.url,
                type: "GET",
                headers: { "X-Requested-With": "XMLHttpRequest" },
                // success: function (html) {
                //     var $body = $modal.find(".modal-body");
                //     var $parsed = $("<div>").html(html);
                //     var $scripts = $parsed.find("script").remove();

                //     $body.html($parsed.html());

                //     if ($.validator && $.validator.unobtrusive) {
                //         $.validator.unobtrusive.parse($body.find("form"));
                //     }

                //     var runNext = function (idx) {
                //         if (idx >= $scripts.length) {
                //             $modal.trigger("gc-modal:loaded");
                //             return;
                //         }
                //         var el = $scripts[idx];
                //         if (el.src) {
                //             var s = document.createElement("script");
                //             s.onload = function () { runNext(idx + 1); };
                //             s.onerror = function () { runNext(idx + 1); };
                //             s.src = el.src;
                //             document.body.appendChild(s);
                //         } else {
                //             $.globalEval(el.text || el.textContent || "");
                //             runNext(idx + 1);
                //         }
                //     };
                //     runNext(0);
                // },
                success: function (html) {
                    var $body = $modal.find(".modal-body");
                    var $parsed = $("<div>").html(html);
                    var $scripts = $parsed.find("script").remove();

                    $body.html($parsed.html());

                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse($body.find("form"));
                    }

                    var runNext = function (idx) {
                        if (idx >= $scripts.length) {
                            // modal body er moddhe notun ashe custom select gula reinit
                            window.initCustomDropdowns($body, true);

                            $modal.trigger("gc-modal:loaded");
                            return;
                        }
                        var el = $scripts[idx];
                        if (el.src) {
                            var s = document.createElement("script");
                            s.onload = function () { runNext(idx + 1); };
                            s.onerror = function () { runNext(idx + 1); };
                            s.src = el.src;
                            document.body.appendChild(s);
                        } else {
                            $.globalEval(el.text || el.textContent || "");
                            runNext(idx + 1);
                        }
                    };
                    runNext(0);
                },
                error: function () {
                    $modal.find(".modal-body").html("<p class='text-danger p-3'>Failed to load content.</p>");
                }
            });

            $modal.modal("show");

            return state;
        },

        markSuccess: function ($modalOrChild, lastCode) {
            var state = this.findStateByElement($modalOrChild);
            if (state) {
                state.dataAdded = true;
                state.lastCode = lastCode || null;
            }
        },

        findStateByElement: function ($el) {
            var $modal = $el.closest(".modal");
            for (var i = 0; i < this.stack.length; i++) {
                if (this.stack[i].$modal.attr("id") === $modal.attr("id")) {
                    return this.stack[i];
                }
            }
            return null;
        },

        close: function ($modalEl) {
            
            var self = this;
            var state = this.findStateByElement($modalEl);
            if (!state) return;

            var modalInstance = bootstrap.Modal.getInstance(state.$modal[0]);

            state.$modal.one("hidden.bs.modal", function () {
                state.$modal.remove();

                var idx = self.stack.indexOf(state);
                if (idx > -1) self.stack.splice(idx, 1);

                if (state.dataAdded && state.target && state.reloadUrl) {
                    setTimeout(function () {
                        self.refreshTarget(state.target, state.reloadUrl, state.lastCode, state.title);
                    }, 100);
                }

                if (self.stack.length === 0) {
                    self.showChrome();
                }
            });

            if (modalInstance) modalInstance.hide();
        },

        refreshTarget: function (target, reloadUrl, lastCode, title) {
            var $target = $(target);
            var currentVal = $target.val();

            $.ajax({
                url: reloadUrl,
                method: "GET",
                success: function (response) {
                    $target.empty();
                    $target.append($("<option>", { value: "", text: "--Select " + (title || "") + "--" }));

                    $.each(response, function (i, item) {
                        $target.append($("<option>", { value: item.code, text: item.name }));
                    });

                    var valToSet = lastCode || currentVal;
                    var instance = $target.data('customDdInstance');

                    if (instance) {
                        var items = response.map(function (item) {
                            return { id: item.code, text: item.name, selected: item.code === valToSet };
                        });
                        instance.loadStaticItems(items);

                        if (window.setSelect) {
                            window.setSelect(target, valToSet, false);
                        }
                    } else if (typeof window.setSelect === "function") {
                        window.setSelect(target, valToSet, false);
                    } else if ($target.hasClass("select2-hidden-accessible")) {
                        $target.val(valToSet).trigger('change.select2');
                    } else {
                        $target.val(valToSet).trigger('change');
                    }
                }
            });
        },

        hideChrome: function () {
            $("#header").hide();
            $("#left_menu").hide();
            $("#main-content").toggleClass("collapse-main");
            $("body").removeClass("sidebar-mini");
        },

        showChrome: function () {
            $("#header").show();
            $("#left_menu").show();
            $("#main-content").toggleClass("collapse-main");
            $("body").addClass("sidebar-mini");
        }
    };

    window.GcModal = GcModal;

    $("body").on("click", ".js-quick-add", function (e) {
        e.preventDefault();
        e.stopPropagation();

        GcModal.open({
            url: $(this).data("url"),
            target: $(this).data("target"),
            reloadUrl: $(this).data("reload-url"),
            title: $(this).data("title")
        });
    });

    $("body").on("click", ".js-gc-modal-close", function (e) {
        e.preventDefault();
        e.stopPropagation();
        GcModal.close($(this));
    });

})(jQuery);