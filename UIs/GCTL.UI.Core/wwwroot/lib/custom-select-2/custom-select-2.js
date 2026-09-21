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
                pageSize: 10,
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
            this.data = [];
            this.filtered = [];
            this.selected = [];
            this._id = 'customdd_' + Math.random().toString(36).slice(2, 8);
            this._ajaxTimer = null;
            this.page = 1;
            this.hasMore = false;
            this.isLoadingMore = false;
            this._currentTerm = '';
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
            this.$arrow = $arrow;
            this.$panel = $panel;
            this.$list = $list;
        }

        _bindCommonEvents() {
            var self = this;

            this.$arrow.on('click', function (e) {
                e.stopPropagation();
                if (self.$wrapper.hasClass('disabled')) return;
                if (self.$wrapper.hasClass('open')) {
                    self._closePanel();
                } else {
                    self._openPanelFull();
                    self.$input.trigger('focus');
                }
            });

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

            // Infinite Scroll: fetch next page when user scrolls to bottom of panel
            var checkScroll = function () {
                if (self.hasMore && !self.isLoadingMore) {
                    var el = self.$panel[0];
                    if (el && (el.scrollTop + el.clientHeight >= el.scrollHeight - 50)) {
                        self._loadNextPage();
                    }
                }
            };
            this.$panel.on('scroll', checkScroll);
            this.$list.on('scroll', checkScroll);

            $(document).on('click.' + this._id, function (e) {
                if (!$(e.target).closest(self.$wrapper).length) {
                    self._closePanel();
                }
            });

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

        _openPanelFull() {
            if (this.$wrapper.hasClass('disabled')) return;
            this.$wrapper.addClass('open');
            if (!this.options.isAjax) {
                this._renderList(this.data);
            } else if (!this._hasFetchedInitially || this.data.length <= 1) {
                this._hasFetchedInitially = true;
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
            }
        }

        _closePanel() {
            this.$wrapper.removeClass('open');
            this.$panel.css({ 'width': '', 'min-width': '', 'max-width': '' });
        }

        _ajaxSearch(term) {
            var self = this;
            clearTimeout(this._ajaxTimer);
            this._currentTerm = term || '';
            this.page = 1;
            this.hasMore = false;
            this.isLoadingMore = false;

            this._ajaxTimer = setTimeout(function () {
                var payload = { 
                    term: self._currentTerm,
                    page: self.page,
                    pageSize: self.options.pageSize || 10
                };
                if (self.options.parentSelector) {
                    payload[self.options.parentParamName] = $(self.options.parentSelector).val();
                }
                if (typeof self.options.ajaxDataMapper === 'function') {
                    payload = self.options.ajaxDataMapper(payload, self);
                }

                self.$list.html('<div class="custom-dd-loading p-2 text-center text-muted small"><span class="spinner-border spinner-border-sm me-1"></span> Loading...</div>');

                $.ajax({
                    url: self.options.ajaxUrl,
                    method: self.options.ajaxMethod,
                    data: payload
                }).done(function (resp) {
                    if (typeof resp === 'string') {
                        try { resp = JSON.parse(resp); } catch (e) {}
                    }

                    var results;
                    if (typeof self.options.ajaxResultsMapper === 'function') {
                        results = self.options.ajaxResultsMapper(resp, self);
                    } else {
                        results = resp.results || resp.Results || resp.data || resp.Data || resp;
                    }

                    var pagination = resp.pagination || resp.Pagination;
                    var more = pagination ? (pagination.more !== undefined ? pagination.more : pagination.More) : false;
                    self.hasMore = !!more;

                    self.data = (results || []).map(function (r) {
                        var id = r.id !== undefined ? r.id : r.Id;
                        var text = r.text !== undefined ? r.text : (r.name !== undefined ? r.name : (r.Name !== undefined ? r.Name : id));
                        return { id: String(id), text: text };
                    });

                    // If a selected item is not in page 1 results, preserve it
                    if (self.selected.length) {
                        var selId = String(self.selected[0]);
                        var hasSel = self.data.some(function (d) { return d.id === selId; });
                        if (!hasSel && self.$original.find('option[value="' + selId + '"]').length) {
                            var selText = self.$original.find('option[value="' + selId + '"]').text();
                            self.data.unshift({ id: selId, text: selText });
                        }
                    }

                    self._renderList(self.data);
                }).fail(function () {
                    self.$list.html('<div class="custom-dd-empty text-danger">Failed to load data</div>');
                });
            }, this.options.ajaxDelay);
        }

        _appendItemRow(item) {
            var self = this;
            var isSelected = this.selected.indexOf(item.id) !== -1;
            var $row = $('<div class="custom-dd-option' + (isSelected ? ' selected' : '') + '"></div>');
            if (this.isMulti) {
                $row.append('<input type="checkbox" class="custom-dd-opt-checkbox" ' + (isSelected ? 'checked' : '') + ' />');
            }
            $row.append('<span class="custom-dd-opt-text"></span>');
            $row.find('.custom-dd-opt-text').text(item.text);
            $row.attr('data-id', item.id);
            $row.on('click', function (e) {
                e.stopPropagation();
                self._selectItem(item);
            });
            this.$list.append($row);
        }

        _loadNextPage() {
            var self = this;
            if (this.isLoadingMore || !this.hasMore) return;
            this.isLoadingMore = true;
            this.page++;

            var $loader = $('<div class="custom-dd-more-loader p-1 text-center text-muted small border-top"><span class="spinner-border spinner-border-sm me-1"></span> Loading more...</div>');
            this.$list.append($loader);

            if (this.options.isAjax) {
                var payload = {
                    term: this._currentTerm,
                    page: this.page,
                    pageSize: this.options.pageSize || 10
                };
                if (this.options.parentSelector) {
                    payload[this.options.parentParamName] = $(this.options.parentSelector).val();
                }
                if (typeof this.options.ajaxDataMapper === 'function') {
                    payload = this.options.ajaxDataMapper(payload, this);
                }

                $.ajax({
                    url: this.options.ajaxUrl,
                    method: this.options.ajaxMethod,
                    data: payload
                }).done(function (resp) {
                    $loader.remove();
                    self.isLoadingMore = false;

                    if (typeof resp === 'string') {
                        try { resp = JSON.parse(resp); } catch (e) {}
                    }

                    var results;
                    if (typeof self.options.ajaxResultsMapper === 'function') {
                        results = self.options.ajaxResultsMapper(resp, self);
                    } else {
                        results = resp.results || resp.Results || resp.data || resp.Data || resp;
                    }

                    var pagination = resp.pagination || resp.Pagination;
                    var more = pagination ? (pagination.more !== undefined ? pagination.more : pagination.More) : false;
                    self.hasMore = !!more;

                    var newItems = (results || []).map(function (r) {
                        var id = r.id !== undefined ? r.id : r.Id;
                        var text = r.text !== undefined ? r.text : (r.name !== undefined ? r.name : (r.Name !== undefined ? r.Name : id));
                        return { id: String(id), text: text };
                    });

                    if (newItems.length) {
                        newItems.forEach(function (item) {
                            if (!self.data.some(function (d) { return d.id === item.id; })) {
                                self.data.push(item);
                                self._appendItemRow(item);
                            }
                        });
                    }
                }).fail(function () {
                    $loader.remove();
                    self.isLoadingMore = false;
                });
            } else {
                // Client-side pagination for static / local items
                var pageSize = this.options.pageSize || 10;
                var startIndex = (this.page - 1) * pageSize;
                var endIndex = this.page * pageSize;
                var all = this._allCurrentItems || [];
                var nextItems = all.slice(startIndex, endIndex);

                setTimeout(function () {
                    $loader.remove();
                    self.isLoadingMore = false;
                    self.hasMore = endIndex < all.length;

                    if (nextItems.length) {
                        nextItems.forEach(function (item) {
                            if (!self.$list.find('.custom-dd-option[data-id="' + item.id + '"]').length) {
                                self._appendItemRow(item);
                            }
                        });
                    }
                }, 50);
            }
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
            if (!items || !items.length) {
                this.$list.append('<div class="custom-dd-empty">No results found</div>');
                this.hasMore = false;
                return;
            }

            this._allCurrentItems = items;
            this.page = 1;
            var pageSize = this.options.pageSize || 10;
            if (!this.options.isAjax) {
                this.hasMore = this._allCurrentItems.length > pageSize;
            }

            var displayItems = this.options.isAjax ? items : this._allCurrentItems.slice(0, pageSize);

            // If an item is already selected but beyond first 10, ensure it appears at top
            if (this.selected.length) {
                var selId = String(this.selected[0]);
                var selInDisplay = displayItems.some(function (d) { return d.id === selId; });
                if (!selInDisplay) {
                    var selItem = this._allCurrentItems.find(function (d) { return d.id === selId; });
                    if (selItem) {
                        displayItems = [selItem].concat(displayItems.filter(function (d) { return d.id !== selId; }).slice(0, pageSize - 1));
                    }
                }
            }

            displayItems.forEach(function (item) {
                self._appendItemRow(item);
            });
        }

        destroy() {
            if (this.$wrapper) {
                this.$wrapper.remove();
            }
            $(document).off('click.' + this._id);
            if (this.options.parentSelector) {
                $(this.options.parentSelector).off('change.customCascade-' + this._id);
            }
            this.$original.removeData('customDdInstance');
            this.$original.show();
        }

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
            if (this.options.isAjax) {
                this._hasFetchedInitially = false;
            }
            this._renderList(this.data);
            if (typeof this.options.onClear === 'function') this.options.onClear(this);
            this._triggerChange();
        }

        _triggerChange() {
            this.$original.trigger({
                type: 'change',
                customDropdown: true
            });

            if (typeof this.options.onChange === 'function') {
                this.options.onChange(this.getValue(), this);
            }
        }

        setValue(id, text) {
            if (id === null || id === undefined || id === '' || (Array.isArray(id) && id.length === 0)) {
                this.clear();
                return;
            }
            if (Array.isArray(id)) {
                id = id.filter(function (v) { return v && String(v).trim() !== ''; });
                if (id.length === 0) {
                    this.clear();
                    return;
                }
            }
            id = String(id);
            text = text || (this.$original.find('option[value="' + id + '"]').text()) || id;

            var existing = this.data.find(function (d) { return d.id === id; });
            if (!existing) {
                existing = { id: id, text: text };
                this.data.unshift(existing);
            } else if (text && existing.text !== text) {
                existing.text = text;
            }

            this._addSelected(existing);
            this._syncOriginalSelect();
            if (this.options.isAjax) {
                this._hasFetchedInitially = false;
            }
            if (this.data.length) {
                this._renderList(this.data);
            }
        }

        init() {
            if (!this.$original.length) {
                console.warn('customDropdown: element pawa jayni -> ' + this.originalSelector);
                return this;
            }

            var existing = this.$original.data('customDdInstance');
            if (existing && existing.$wrapper) {
                existing.$wrapper.remove();
                $(document).off('click.' + existing._id);
            }

            this.$original.next('.custom-dd-wrapper').remove();

            this._buildSkeleton();
            this._bindCommonEvents();
            this.$original.data('customDdInstance', this);

            if (this.$original.is(':disabled') || this.$original.is('[readonly]')) {
                this.setDisabled(true);
            }

            if (this.options.items) this.loadStaticItems(this.options.items);

            var initVal = this.$original.val();
            if (initVal) {
                var hasValidInit = Array.isArray(initVal)
                    ? initVal.some(function (v) { return v && String(v).trim() !== ''; })
                    : (String(initVal).trim() !== '');
                if (hasValidInit) {
                    var initText = this.$original.find('option:selected').text() || initVal;
                    this.setValue(initVal, initText);
                }
            }

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
            var val = this.selected[0] || '';
            if (val && !this.$original.find('option[value="' + val + '"]').length) {
                var text = this.getText()[0] || val;
                this.$original.append('<option value="' + val + '" selected>' + text + '</option>');
            }
            this.$original.val(val);
        }
    }

    class customDropdownMulti extends customDropdownBase {
        constructor(selector, options) {
            super(selector, Object.assign({
                showSelectAll: true,
                showCountBadge: true,
                countThreshold: 2
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
            if (!item || !item.id || String(item.id).trim() === '') return;
            if (this.selected.indexOf(item.id) === -1) this.selected.push(item.id);
            this._renderTags();
        }

        _selectAll(items) {
            var self = this;
            var allIds = items.map(function (i) { return i.id; });
            var allSelected = allIds.length > 0 && allIds.every(function (id) { return self.selected.indexOf(id) !== -1; });

            if (allSelected) {
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

            if (this.options.showSelectAll && items && items.length) {
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

            if (!items || !items.length) {
                this.$list.append('<div class="custom-dd-empty">No results found</div>');
                this.hasMore = false;
                return;
            }

            this._allCurrentItems = items;
            this.page = 1;
            var pageSize = this.options.pageSize || 10;
            if (!this.options.isAjax) {
                this.hasMore = this._allCurrentItems.length > pageSize;
            }

            var displayItems = this.options.isAjax ? items : this._allCurrentItems.slice(0, pageSize);

            displayItems.forEach(function (item) {
                self._appendItemRow(item);
            });
        }

        _renderTags() {
            var self = this;
            this.$tagsArea.empty();

            this.selected = (this.selected || []).filter(function (id) {
                return id && String(id).trim() !== '';
            });

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

            if (this.selected.length === 0) {
                this.$tagsArea.hide();
                this.$input.attr("placeholder", this.options.placeholder);
            } else {
                this.$tagsArea.show();
                this.$input.attr("placeholder", "");
            }
        }

        _syncOriginalSelect() {
            this.$original.val(this.selected);
            this._renderTags();
        }
    }

    function setSelect(selectorOrElement, value, disabled = false, newItems = null, tableShow = false, tableTitle = 'Select an Option', extraClass = '') {

        var $el = $(selectorOrElement);

        if ($el.length > 1) {
            $el.each(function () {
                setSelect(this, value, disabled, newItems, tableShow, tableTitle, extraClass);
            });
            return;
        }

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
            instance.data = rawItems.map(function (i) {
                return { id: String(i.id ?? i.value), text: i.text };
            });
        }

        if (instance.isMulti) {
            var arr = Array.isArray(value) ? value : (value ? [String(value)] : []);
            instance.selected = arr.map(String).filter(function (s) { return s && s.trim() !== ''; });
            instance._renderTags();
        } else {
            instance.selected = value ? [String(value)] : [];
            var found = instance.data.find(function (d) { return d.id === String(value); });
            instance.$input.val(found ? found.text : (value || ''));
        }

        instance._syncOriginalSelect();
        instance.setDisabled(!!disabled);

        if (extraClass) {
            instance.$wrapper.addClass(extraClass);
        }

        if (tableShow) {
            instance.$input.off('focus.tableShow click.tableShow input.tableShow');

            instance._openPanel = function () {
                if (instance.$wrapper.hasClass('disabled')) return;
                instance.$wrapper.addClass('open');
                renderTableInPanel(instance, rawItems, tableTitle);
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
                renderTableInPanel(instance, filteredRaw, tableTitle);
            });
        } else {
            instance._renderList(instance.data);
        }
    }

    function renderTableInPanel(instance, items, title) {
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
        var theadHtml = '<th class="text-center"></th>' + columns.map(function (col) {
            return '<th class="text-center">' + formatHeader(col) + '</th>';
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

            if (instance.isMulti) {
                var idx = instance.selected.indexOf(selectedId);
                if (idx === -1) {
                    instance.selected.push(selectedId);
                } else {
                    instance.selected.splice(idx, 1);
                }
                instance._syncOriginalSelect();
                instance._triggerChange();
                renderTableInPanel(instance, items, title);
            } else {
                instance.selected = [selectedId];
                instance.$input.val(found ? found.text : selectedId);
                instance._syncOriginalSelect();
                instance._closePanel();
                instance._triggerChange();
                instance.$panel.css({ 'width': '', 'min-width': '', 'max-width': '' });
            }
        });
    }

    window.customDropdownBase = customDropdownBase;
    window.customDropdownSingle = customDropdownSingle;
    window.customDropdownMulti = customDropdownMulti;
    window.setSelect = setSelect;

})(window, window.jQuery);