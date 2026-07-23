/*!
 * customDropdown - Fully Custom Dropdown (NO Select2 dependency)
 * -----------------------------------------------------------------
 * Parent class: customDropdownBase
 * Child classes: customDropdownSingle, customDropdownMulti
 *
 * - Select box er VISIBLE part-i ekta real <input> - shorashori shekhane
 *   type korle inline e filter hoy, kono alada search row/box kholena.
 * - Copy icon clear (X) button er LEFT e.
 * - Multi-select e proti option er বাম পাশে checkbox.
 * -----------------------------------------------------------------
 */
(function (window, $) {
    'use strict';

    function toast(msg) {
        var $t = $('<div class="custom-dd-toast">' + msg + '</div>');
        $('body').append($t);
        requestAnimationFrame(function () { $t.addClass('show'); });
        setTimeout(function () {
            $t.removeClass('show');
            setTimeout(function () { $t.remove(); }, 300);
        }, 1300);
    }

    function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text);
        } else {
            var $t = $('<textarea>').val(text).appendTo('body').select();
            document.execCommand('copy');
            $t.remove();
        }
    }

    class customDropdownBase {
        constructor(selector, options) {
            this.originalSelector = selector;
            this.$original = $(selector);
            this.options = Object.assign({
                placeholder: '-- Select --',
                showClear: true,
                showCopyId: true,
                copyAsJsonArray: false,
                items: null,
                isAjax: false,
                ajaxUrl: null,
                ajaxMethod: 'GET',
                ajaxDelay: 300,
                ajaxDataMapper: null,
                ajaxResultsMapper: null,
                parentSelector: null,
                parentParamName: 'parentId',
                clearOnParentChange: true,
                disableUntilParentSelected: true,
                onChange: null,
                onClear: null
            }, options || {});

            this.isMulti = false;
            this.data = [];       // full item list {id, text}
            this.filtered = [];
            this.selected = [];   // array of ids (single: max 1)
            this._id = 'customdd_' + Math.random().toString(36).slice(2, 8);
            this._ajaxTimer = null;
        }

        _buildSkeleton() {
            this.$original.hide();
            var wrapperClass = this.isMulti ? 'custom-dd-wrapper custom-dd-multi' : 'custom-dd-wrapper custom-dd-single';
            var $wrapper = $('<div class="' + wrapperClass + '" tabindex="0"></div>');

            var $box = $('<div class="custom-dd-box"></div>');
            var $input = $('<input type="text" class="custom-dd-input" autocomplete="off" placeholder="' + this.options.placeholder + '" />');
            var $tagsArea = this.isMulti ? $('<div class="custom-dd-tags"></div>') : null;
            var $copyIcon = $('<span class="custom-dd-copy-icon" title="Copy selected ID(s)"><i class="fas fa-copy"></i></span>');
            var $clearIcon = $('<span class="custom-dd-clear-icon" title="Clear">&times;</span>');
            var $arrow = $('<span class="custom-dd-arrow">&#9662;</span>');

            if (this.isMulti) $box.append($tagsArea);
            $box.append($input);
            if (this.options.showCopyId) $box.append($copyIcon);
            if (this.options.showClear) $box.append($clearIcon);
            $box.append($arrow);

            var $panel = $('<div class="custom-dd-panel"></div>');
            var $list = $('<div class="custom-dd-list"></div>');
            $panel.append($list);

            $wrapper.append($box).append($panel);
            this.$original.after($wrapper);

            this.$wrapper = $wrapper;
            this.$box = $box;
            this.$input = $input;
            this.$tagsArea = $tagsArea;
            this.$copyIcon = $copyIcon;
            this.$clearIcon = $clearIcon;
            this.$panel = $panel;
            this.$list = $list;
        }

        _bindCommonEvents() {
            var self = this;

            this.$input.on('focus click', function () {
                self._openPanelFull();
            });

            this.$input.on('input', function () {
                self._openPanel();
                var term = self.$input.val();
                if (self.options.isAjax) {
                    self._ajaxSearch(term);
                } else {
                    self._filterLocal(term);
                }
            });
            // var self = this;

            // this.$input.on('focus click', function () { self._openPanel(); });

            // this.$input.on('input', function () {
            //     self._openPanel();
            //     var term = self.$input.val();
            //     if (self.options.isAjax) {
            //         self._ajaxSearch(term);
            //     } else {
            //         self._filterLocal(term);
            //     }
            // });

            $(document).on('click.' + this._id, function (e) {
                if (!$(e.target).closest(self.$wrapper).length) {
                    self._closePanel();
                }
            });

            // this.$clearIcon.on('click', function (e) {
            //     e.stopPropagation();
            //     self.clear();
            // });

            // this.$copyIcon.on('click', function (e) {
            //     e.stopPropagation();
            //     var val = self.getValue();
            //     var isEmpty = Array.isArray(val) ? val.length === 0 : !val;
            //     if (isEmpty) { toast('Kono item select kora nai'); return; }
            //     var text = Array.isArray(val)
            //         ? (self.options.copyAsJsonArray ? JSON.stringify(val) : val.join(','))
            //         : val;
            //     copyText(text);
            //     toast('Copied: ' + text);


            //     self.$copyIcon.html('<i class="fas fa-check"></i>');
            //     self.$copyIcon.addClass('copied');

            //     clearTimeout(self._copyIconTimer);
            //     self._copyIconTimer = setTimeout(function () {
            //         self.$copyIcon.html('<i class="fas fa-copy"></i>');
            //         self.$copyIcon.removeClass('copied');
            //     }, 1500);
            // });

            this.$clearIcon.on('click', function (e) {
                e.stopPropagation();
                if (self.$wrapper.hasClass('disabled')) return;
                self.clear();
            });

            this.$copyIcon.on('click', function (e) {
                e.stopPropagation();
                if (self.$wrapper.hasClass('disabled')) return;
                var val = self.getValue();
                var isEmpty = Array.isArray(val) ? val.length === 0 : !val;
                if (isEmpty) { toast('Kono item select kora nai'); return; }
                var text = Array.isArray(val)
                    ? (self.options.copyAsJsonArray ? JSON.stringify(val) : val.join(','))
                    : val;
                copyText(text);
                toast('Copied: ' + text);

                self.$copyIcon.html('<i class="fas fa-check"></i>');
                self.$copyIcon.addClass('copied');

                clearTimeout(self._copyIconTimer);
                self._copyIconTimer = setTimeout(function () {
                    self.$copyIcon.html('<i class="fas fa-copy"></i>');
                    self.$copyIcon.removeClass('copied');
                }, 1500);
            });

            if (this.options.parentSelector) {
                var evtNs = 'change.customCascade-' + this._id;
                $(this.options.parentSelector).off(evtNs).on(evtNs, function () {
                    var parentVal = $(this).val();
                    if (self.options.clearOnParentChange) self.clear();
                    if (self.options.disableUntilParentSelected) {
                        self.setDisabled(!parentVal);
                    }
                });
                if (this.options.disableUntilParentSelected && !$(this.options.parentSelector).val()) {
                    this.setDisabled(true);
                }
            }
        }

        // _openPanel() {
        //     if (this.$wrapper.hasClass('disabled')) return;
        //     this.$wrapper.addClass('open');
        //     if (!this.options.isAjax) {
        //         this._filterLocal(this.$input.val());
        //     } else if (!this.data.length) {
        //         this._ajaxSearch('');
        //     } else {
        //         this._renderList(this.data);
        //     }
        // }
        _openPanelFull() {
            if (this.$wrapper.hasClass('disabled')) return;
            this.$wrapper.addClass('open');
            if (!this.options.isAjax) {
                this._renderList(this.data);
            } else if (!this.data.length) {
                this._ajaxSearch('');
            } else {
                this._renderList(this.data);
            }
        }

        _openPanel() {
            if (this.$wrapper.hasClass('disabled')) return;
            this.$wrapper.addClass('open');
            if (!this.options.isAjax) {
                this._filterLocal(this.$input.val());
            } else if (!this.data.length) {
                this._ajaxSearch('');
            } else {
                this._renderList(this.data);
            }
        }

        _closePanel() {
            this.$wrapper.removeClass('open');
            this.$panel.css({ 'width': '', 'min-width': '', 'max-width': '' });
        }
        _ajaxSearch(term) {
            var self = this;
            clearTimeout(this._ajaxTimer);
            this._ajaxTimer = setTimeout(function () {
                var payload = { term: term || '' };
                if (self.options.parentSelector) {
                    payload[self.options.parentParamName] = $(self.options.parentSelector).val();
                }
                if (typeof self.options.ajaxDataMapper === 'function') {
                    payload = self.options.ajaxDataMapper(payload, self);
                }
                $.ajax({
                    url: self.options.ajaxUrl,
                    method: self.options.ajaxMethod,
                    data: payload
                }).done(function (resp) {
                    var results;
                    if (typeof self.options.ajaxResultsMapper === 'function') {
                        results = self.options.ajaxResultsMapper(resp, self);
                    } else {
                        results = resp.results || resp.data || resp;
                    }
                    self.data = (results || []).map(function (r) {
                        return { id: String(r.id), text: r.text };
                    });
                    self._renderList(self.data);
                });
            }, this.options.ajaxDelay);
        }

        _filterLocal(term) {
            term = (term || '').toLowerCase();
            this.filtered = this.data.filter(function (item) {
                return item.text.toLowerCase().indexOf(term) !== -1;
            });
            this._renderList(this.filtered);
        }

        _renderList(items) {
            var self = this;
            this.$list.empty();
            if (!items.length) {
                this.$list.append('<div class="custom-dd-empty">No results found</div>');
                return;
            }
            items.forEach(function (item) {
                var isSelected = self.selected.indexOf(item.id) !== -1;
                var $row = $('<div class="custom-dd-option' + (isSelected ? ' selected' : '') + '"></div>');
                if (self.isMulti) {
                    $row.append('<input type="checkbox" class="custom-dd-opt-checkbox" ' + (isSelected ? 'checked' : '') + ' />');
                }
                $row.append('<span class="custom-dd-opt-text"></span>');
                $row.find('.custom-dd-opt-text').text(item.text);
                $row.attr('data-id', item.id);
                $row.on('click', function (e) {
                    e.stopPropagation();
                    self._selectItem(item);
                });
                self.$list.append($row);
            });
        }

        // setDisabled(disabled) {
        //     this.$wrapper.toggleClass('disabled', !!disabled);
        //     this.$input.prop('disabled', !!disabled);
        // }

        setDisabled(disabled) {
            this.$wrapper.toggleClass('disabled', !!disabled);
            this.$input.prop('disabled', !!disabled);

            if (disabled) {
                this.$copyIcon.addClass('dd-icon-disabled');
                this.$clearIcon.addClass('dd-icon-disabled');
            } else {
                this.$copyIcon.removeClass('dd-icon-disabled');
                this.$clearIcon.removeClass('dd-icon-disabled');
            }
        }
        loadStaticItems(items) {
            this.data = (items || []).map(function (i) {
                return { id: String(i.id ?? i.value), text: i.text, selected: !!i.selected };
            });
            var self = this;
            this.data.forEach(function (i) {
                if (i.selected) self._addSelected(i, false);
            });
            this._syncOriginalSelect();
        }

        _addSelected() { /* override in child */ }
        _selectItem() { /* override in child */ }
        _syncOriginalSelect() { /* override in child */ }

        getValue() { return this.isMulti ? this.selected.slice() : (this.selected[0] || null); }

        getText() {
            var self = this;
            return this.selected.map(function (id) {
                var found = self.data.find(function (d) { return d.id === id; });
                return found ? found.text : id;
            });
        }

        clear() {
            this.selected = [];
            this.$input.val('');
            if (this.isMulti) this.$tagsArea.empty();
            this._syncOriginalSelect();
            this._renderList(this.data);
            if (typeof this.options.onClear === 'function') this.options.onClear(this);
            this._triggerChange();
        }

        // _triggerChange() {
        //     if (typeof this.options.onChange === 'function') {
        //         this.options.onChange(this.getValue(), this);
        //     }
        //     this.$original.trigger('custom:change');
        // }
        _triggerChange() {

            this.$original.trigger({
                type: 'change',
                customDropdown: true
            });

            if (typeof this.options.onChange === 'function') {
                this.options.onChange(this.getValue(), this);
            }
        }
        // init() {
        //     if (!this.$original.length) {
        //         console.warn('customDropdown: element pawa jayni -> ' + this.originalSelector);
        //         return this;
        //     }
        //     this._buildSkeleton();
        //     this._bindCommonEvents();
        //     if (this.options.items) this.loadStaticItems(this.options.items);
        //     return this;
        // }

        init() {            
            if (!this.$original.length) {
                console.warn('customDropdown: element pawa jayni -> ' + this.originalSelector);
                return this;
            }

            // Age theke instance thakle purono wrapper soraw (re-init er khetre)
            var existing = this.$original.data('customDdInstance');
            if (existing && existing.$wrapper) {
                existing.$wrapper.remove();
                $(document).off('click.' + existing._id);
            }

            // Doubly-safe: kono orphan wrapper thakle o soraw
            this.$original.next('.custom-dd-wrapper').remove();

            this._buildSkeleton();
            this._bindCommonEvents();
            this.$original.data('customDdInstance', this);

            if (this.options.items) this.loadStaticItems(this.options.items);
            return this;
        }
    }

    class customDropdownSingle extends customDropdownBase {
        constructor(selector, options) {
            super(selector, options);
            this.isMulti = false;
        }

        _selectItem(item) {
            this.selected = [item.id];
            this.$input.val(item.text);
            this._syncOriginalSelect();
            this._closePanel();
            this._renderList(this.data);
            this._triggerChange();
        }

        _addSelected(item) {
            this.selected = [item.id];
            this.$input.val(item.text);
        }

        _syncOriginalSelect() {
            this.$original.val(this.selected[0] || '');
        }
    }

    class customDropdownMulti extends customDropdownBase {
        constructor(selector, options) {
            super(selector, Object.assign({
                showSelectAll: true,
                showCountBadge: true,
                countThreshold: 2 // eyi shonkhar beshi select korle tags er bodole count dekhabe
            }, options || {}));
            this.isMulti = true;
        }

        _selectItem(item) {
            var idx = this.selected.indexOf(item.id);
            if (idx === -1) {
                this.selected.push(item.id);
            } else {
                this.selected.splice(idx, 1);
            }
            this._renderTags();
            this._syncOriginalSelect();
            this._renderList(this.filtered.length ? this.filtered : this.data);
            this._triggerChange();
        }

        _addSelected(item) {
            if (this.selected.indexOf(item.id) === -1) this.selected.push(item.id);
        }

        _selectAll(items) {
            var self = this;
            var allIds = items.map(function (i) { return i.id; });
            var allSelected = allIds.length > 0 && allIds.every(function (id) { return self.selected.indexOf(id) !== -1; });

            if (allSelected) {
                // shob already selected thakle -> unselect
                this.selected = this.selected.filter(function (id) { return allIds.indexOf(id) === -1; });
            } else {
                allIds.forEach(function (id) {
                    if (self.selected.indexOf(id) === -1) self.selected.push(id);
                });
            }
            this._renderTags();
            this._syncOriginalSelect();
            this._renderList(items);
            this._triggerChange();
        }

        _renderList(items) {
            var self = this;
            this.$list.empty();

            if (this.options.showSelectAll && items.length) {
                var allIds = items.map(function (i) { return i.id; });
                var allSelected = allIds.every(function (id) { return self.selected.indexOf(id) !== -1; });
                var $selectAllRow = $('<div class="custom-dd-option custom-dd-select-all"></div>');
                $selectAllRow.append('<input type="checkbox" class="custom-dd-opt-checkbox" ' + (allSelected ? 'checked' : '') + ' />');
                $selectAllRow.append('<span class="custom-dd-opt-text"><strong>Select All</strong></span>');
                $selectAllRow.on('click', function (e) {
                    e.stopPropagation();
                    self._selectAll(items);
                });
                this.$list.append($selectAllRow);
                this.$list.append('<div class="custom-dd-divider"></div>');
            }

            if (!items.length) {
                this.$list.append('<div class="custom-dd-empty">No results found</div>');
                return;
            }

            items.forEach(function (item) {
                var isSelected = self.selected.indexOf(item.id) !== -1;
                var $row = $('<div class="custom-dd-option' + (isSelected ? ' selected' : '') + '"></div>');
                $row.append('<input type="checkbox" class="custom-dd-opt-checkbox" ' + (isSelected ? 'checked' : '') + ' />');
                $row.append('<span class="custom-dd-opt-text"></span>');
                $row.find('.custom-dd-opt-text').text(item.text);
                $row.attr('data-id', item.id);
                $row.on('click', function (e) {
                    e.stopPropagation();
                    self._selectItem(item);
                });
                self.$list.append($row);
            });
        }

        _renderTags() {
            var self = this;
            this.$tagsArea.empty();

            var count = this.selected.length;

            if (this.options.showCountBadge && count > this.options.countThreshold) {
                var $badge = $('<span class="custom-dd-count-badge"></span>');
                $badge.text(count + ' selected');
                this.$tagsArea.append($badge);
                return;
            }

            this.selected.forEach(function (id) {
                var found = self.data.find(function (d) { return d.id === id; });
                var text = found ? found.text : id;
                var $tag = $('<span class="custom-dd-tag"></span>');
                $tag.append('<span class="custom-dd-tag-text"></span>');
                $tag.find('.custom-dd-tag-text').text(text);
                $tag.append('<span class="custom-dd-tag-remove">&times;</span>');
                $tag.find('.custom-dd-tag-remove').on('click', function (e) {
                    e.stopPropagation();
                    self._selectItem({ id: id, text: text });
                });
                self.$tagsArea.append($tag);
            });
        }

        _syncOriginalSelect() {
            this.$original.val(this.selected);
            this._renderTags();
        }
    }

    // function setSelect(selector, value) {
    //     var $el = $(selector);
    //     var instance = $el.data('customDdInstance');

    //     if (!instance) {
    //         $el.val(value).trigger('change');
    //         return;
    //     }

    //     if (instance.isMulti) {
    //         var arr = Array.isArray(value) ? value : (value ? [String(value)] : []);
    //         instance.selected = arr.map(String);
    //         instance._renderTags();
    //     } else {
    //         instance.selected = value ? [String(value)] : [];
    //         var found = instance.data.find(function (d) { return d.id === String(value); });
    //         instance.$input.val(found ? found.text : (value || ''));
    //     }

    //     instance._syncOriginalSelect();
    //     instance._renderList(instance.data);
    // }

    // function setSelect(selector, value, disabled = false) {

    //     var $el = $(selector);
    //     var instance = $el.data('customDdInstance');

    //     if (!instance) {
    //         $el.val(value).trigger('change');
    //         $el.prop('disabled', !!disabled);
    //         if ($el.hasClass('select2-hidden-accessible')) {
    //             $el.prop('disabled', !!disabled).trigger('change.select2');
    //         }
    //         return;
    //     }

    //     if (instance.isMulti) {
    //         var arr = Array.isArray(value) ? value : (value ? [String(value)] : []);
    //         instance.selected = arr.map(String);
    //         instance._renderTags();
    //     } else {
    //         instance.selected = value ? [String(value)] : [];
    //         var found = instance.data.find(function (d) { return d.id === String(value); });
    //         instance.$input.val(found ? found.text : (value || ''));
    //     }

    //     instance._syncOriginalSelect();
    //     instance._renderList(instance.data);
    //     instance.setDisabled(!!disabled);
    // }
    // function setSelect(selector, value, disabled = false, newItems = null) {

    //     var $el = $(selector);
    //     var instance = $el.data('customDdInstance');

    //     if (!instance) {
    //         $el.val(value).trigger('change');
    //         $el.prop('disabled', !!disabled);
    //         if ($el.hasClass('select2-hidden-accessible')) {
    //             $el.prop('disabled', !!disabled).trigger('change.select2');
    //         }
    //         return;
    //     }

    //     if (Array.isArray(newItems)) {
    //         instance.data = newItems.map(function (i) {
    //             return { id: String(i.id ?? i.value), text: i.text };
    //         });
    //     }

    //     if (instance.isMulti) {
    //         var arr = Array.isArray(value) ? value : (value ? [String(value)] : []);
    //         instance.selected = arr.map(String);
    //         instance._renderTags();
    //     } else {
    //         instance.selected = value ? [String(value)] : [];
    //         var found = instance.data.find(function (d) { return d.id === String(value); });
    //         instance.$input.val(found ? found.text : (value || ''));
    //     }

    //     instance._syncOriginalSelect();
    //     instance._renderList(instance.data);
    //     instance.setDisabled(!!disabled);
    // }

    // function setSelect(selector, value, disabled = false, newItems = null, tableShow = false, tableTitle = 'Select an Option') {

    //     var $el = $(selector);
    //     var instance = $el.data('customDdInstance');

    //     if (!instance) {
    //         $el.val(value).trigger('change');
    //         $el.prop('disabled', !!disabled);
    //         if ($el.hasClass('select2-hidden-accessible')) {
    //             $el.prop('disabled', !!disabled).trigger('change.select2');
    //         }
    //         return;
    //     }

    //     var rawItems = Array.isArray(newItems) ? newItems : [];

    //     if (rawItems.length) {
    //         instance.data = rawItems.map(function (i) {
    //             return { id: String(i.id ?? i.value), text: i.text };
    //         });
    //     }

    //     instance.selected = [];
    //     instance.$input.val('');
    //     instance._syncOriginalSelect();
    //     instance._renderList(instance.data);
    //     instance.setDisabled(!!disabled);

    //     if (tableShow) {
    //         instance.$input.off('click.tableShow focus.tableShow');
    //         instance.$input.on('click.tableShow focus.tableShow', function (e) {
    //             e.preventDefault();
    //             e.stopPropagation();
    //             instance.$input.trigger('blur');
    //             instance._closePanel();
    //             openSelectTableModal(instance, rawItems, tableTitle);
    //         });
    //     }
    // }

    // function openSelectTableModal(instance, items, title) {
    //     $('#customDdTableModal').remove();

    //     if (!items.length) return;

    //     // id/value বাদে বাকি সব keys dynamically column hisebe নেবে
    //     var excludedKeys = ['id', 'value'];
    //     var columns = Object.keys(items[0]).filter(function (key) {
    //         return excludedKeys.indexOf(key) === -1;
    //     });

    //     function formatHeader(key) {
    //         // camelCase -> Title Case (e.g. delivaryAddress -> Delivary Address)
    //         var result = key.replace(/([A-Z])/g, ' $1');
    //         return result.charAt(0).toUpperCase() + result.slice(1);
    //     }

    //     var theadHtml = columns.map(function (col) {
    //         return '<th>' + formatHeader(col) + '</th>';
    //     }).join('');

    //     var rowsHtml = items.map(function (item) {
    //         var idVal = item.id ?? item.value;
    //         var cells = columns.map(function (col) {
    //             return '<td>' + (item[col] ?? '') + '</td>';
    //         }).join('');
    //         return '<tr class="custom-dd-modal-row" data-id="' + idVal + '" style="cursor:pointer;">' + cells + '</tr>';
    //     }).join('');

    //     var modalHtml =
    //         '<div class="modal fade" id="customDdTableModal" tabindex="-1" data-bs-backdrop="true" data-bs-keyboard="true">' +
    //         '<div class="modal-dialog modal-lg modal-dialog-centered">' +
    //         '<div class="modal-content">' +
    //         '<div class="modal-header">' +
    //         '<h5 class="modal-title">' + title + '</h5>' +
    //         '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
    //         '</div>' +
    //         '<div class="modal-body">' +
    //         '<table class="table table-bordered table-hover">' +
    //         '<thead><tr>' + theadHtml + '</tr></thead>' +
    //         '<tbody>' + rowsHtml + '</tbody>' +
    //         '</table>' +
    //         '</div>' +
    //         '</div>' +
    //         '</div>' +
    //         '</div>';

    //     $('body').append(modalHtml);

    //     var modalEl = document.getElementById('customDdTableModal');
    //     var modalInstance = new bootstrap.Modal(modalEl);
    //     modalInstance.show();

    //     $(modalEl).on('click', '.custom-dd-modal-row', function () {
    //         var selectedId = $(this).data('id').toString();
    //         var found = instance.data.find(function (d) { return d.id === selectedId; });

    //         instance.selected = [selectedId];
    //         instance.$input.val(found ? found.text : selectedId);
    //         instance._syncOriginalSelect();
    //         instance._renderList(instance.data);
    //         instance._triggerChange();

    //         modalInstance.hide();
    //     });

    //     $(modalEl).on('hidden.bs.modal', function () {
    //         $(this).remove();
    //     });
    // }
    // function setSelect(selector, value, disabled = false, newItems = null, tableShow = false, tableTitle = 'Select an Option') {

    //     var $el = $(selector);
    //     var instance = $el.data('customDdInstance');

    //     if (!instance) {
    //         $el.val(value).trigger('change');
    //         $el.prop('disabled', !!disabled);
    //         if ($el.hasClass('select2-hidden-accessible')) {
    //             $el.prop('disabled', !!disabled).trigger('change.select2');
    //         }
    //         return;
    //     }

    //     var rawItems = Array.isArray(newItems) ? newItems : [];

    //     if (rawItems.length) {
    //         instance.data = rawItems.map(function (i) {
    //             return { id: String(i.id ?? i.value), text: i.text };
    //         });
    //     }

    //     instance.selected = [];
    //     instance.$input.val('');
    //     instance._syncOriginalSelect();
    //     instance.setDisabled(!!disabled);

    //     if (tableShow) {
    //         instance.$input.off('focus.tableShow click.tableShow input.tableShow');

    //         _openPanel override kore dilam jate default _filterLocal/_renderList na chole
    //         instance._openPanel = function () {
    //             if (instance.$wrapper.hasClass('disabled')) return;
    //             instance.$wrapper.addClass('open');
    //             renderTableInPanel(instance, rawItems);
    //         };

    //         instance.$input.on('focus.tableShow click.tableShow', function (e) {
    //             e.stopImmediatePropagation();
    //             instance._openPanel();
    //         });

    //         instance.$input.on('input.tableShow', function () {
    //             var term = (instance.$input.val() || '').toLowerCase();
    //             var filteredRaw = rawItems.filter(function (item) {
    //                 return Object.keys(item).some(function (k) {
    //                     return String(item[k] || '').toLowerCase().indexOf(term) !== -1;
    //                 });
    //             });
    //             instance.$wrapper.addClass('open');
    //             renderTableInPanel(instance, filteredRaw);
    //         });
    //     } else {
    //         instance._renderList(instance.data);
    //     }
    // }
    function setSelect(selector, value, disabled = false, newItems = null, tableShow = false, tableTitle = 'Select an Option') {

        var $el = $(selector);
        var instance = $el.data('customDdInstance');

        if (!instance) {
            $el.val(value).trigger('change');
            $el.prop('disabled', !!disabled);
            if ($el.hasClass('select2-hidden-accessible')) {
                $el.prop('disabled', !!disabled).trigger('change.select2');
            }
            return;
        }

        var rawItems = Array.isArray(newItems) ? newItems : [];

        if (rawItems.length) {
            // notun items dile purono data replace hobe, tারপর value select hobe
            instance.data = rawItems.map(function (i) {
                return { id: String(i.id ?? i.value), text: i.text };
            });
        }

        // value ke selected e apply kora hocche (notun items thakuk ba na thakuk)
        if (instance.isMulti) {
            var arr = Array.isArray(value) ? value : (value ? [String(value)] : []);
            instance.selected = arr.map(String);
            instance._renderTags();
        } else {
            instance.selected = value ? [String(value)] : [];
            var found = instance.data.find(function (d) { return d.id === String(value); });
            instance.$input.val(found ? found.text : (value || ''));
        }

        instance._syncOriginalSelect();
        instance.setDisabled(!!disabled);

        if (tableShow) {
            instance.$input.off('focus.tableShow click.tableShow input.tableShow');

            instance._openPanel = function () {
                if (instance.$wrapper.hasClass('disabled')) return;
                instance.$wrapper.addClass('open');
                renderTableInPanel(instance, rawItems);
            };

            instance.$input.on('focus.tableShow click.tableShow', function (e) {
                e.stopImmediatePropagation();
                instance._openPanel();
            });

            instance.$input.on('input.tableShow', function () {
                var term = (instance.$input.val() || '').toLowerCase();
                var filteredRaw = rawItems.filter(function (item) {
                    return Object.keys(item).some(function (k) {
                        return String(item[k] || '').toLowerCase().indexOf(term) !== -1;
                    });
                });
                instance.$wrapper.addClass('open');
                renderTableInPanel(instance, filteredRaw);
            });
        } else {
            instance._renderList(instance.data);
        }
    }
    function renderTableInPanel(instance, items) {
        instance.$list.empty();

        if (!items.length) {
            instance.$list.append('<div class="custom-dd-empty">No results found</div>');
            return;
        }

        var excludedKeys = ['id', 'value'];
        var columns = Object.keys(items[0]).filter(function (key) {
            return excludedKeys.indexOf(key) === -1;
        });

        function formatHeader(key) {
            var result = key.replace(/([A-Z])/g, ' $1');
            return result.charAt(0).toUpperCase() + result.slice(1);
        }

        var theadHtml = '<th></th>' + columns.map(function (col) {
            return '<th>' + formatHeader(col) + '</th>';
        }).join('');

        var rowsHtml = items.map(function (item) {
            var idVal = item.id ?? item.value;
            var isSelected = instance.selected.indexOf(String(idVal)) !== -1;
            var cells = columns.map(function (col) {
                return '<td>' + (item[col] ?? '') + '</td>';
            }).join('');
            return '<tr class="custom-dd-table-row' + (isSelected ? ' selected' : '') + '" data-id="' + idVal + '" style="cursor:pointer;">' +
                '<td><input type="checkbox" class="custom-dd-table-checkbox" ' + (isSelected ? 'checked' : '') + ' /></td>' +
                cells +
                '</tr>';
        }).join('');

        var tableHtml =
            '<table class="table table-sm table-bordered table-hover mb-0">' +
            '<thead><tr>' + theadHtml + '</tr></thead>' +
            '<tbody>' + rowsHtml + '</tbody>' +
            '</table>';

        instance.$list.append(tableHtml);

        instance.$panel.css({
            'width': 'max-content',
            'min-width': '100%',
            'max-width': '50vw'
        });

        instance.$list.off('click', '.custom-dd-table-row').on('click', '.custom-dd-table-row', function (e) {
            e.stopPropagation();
            var selectedId = $(this).data('id').toString();
            var found = instance.data.find(function (d) { return d.id === selectedId; });

            instance.selected = [selectedId];
            instance.$input.val(found ? found.text : selectedId);
            instance._syncOriginalSelect();
            instance._closePanel();
            instance._triggerChange();

            instance.$panel.css({ 'width': '', 'min-width': '', 'max-width': '' });
        });
    }
    window.customDropdownBase = customDropdownBase;
    window.customDropdownSingle = customDropdownSingle;
    window.customDropdownMulti = customDropdownMulti;
    window.setSelect = setSelect; 

})(window, window.jQuery);
