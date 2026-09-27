/*!
 * customTableDropdown - Modern Table Multi/Single Select Dropdown
 * Supports: Single & Multi-Select, AJAX & Static HTML Options, Sticky Headers, Live Column Filtering, Sorting
 */
(function (window, $) {
    'use strict';

    class CustomTableDropdown {
        constructor(selector, options) {
            this.originalSelector = selector;
            this.$original = $(selector);
            this.options = Object.assign({
                placeholder: 'Select...',
                isMulti: false,
                isAjax: false,
                ajaxUrl: null,
                ajaxMethod: 'GET',
                columns: [
                    { data: 'name', title: 'Name', sortable: true },
                    { data: 'designation', title: 'Designation', sortable: true },
                    { data: 'phone', title: 'Phone', sortable: true },
                    { data: 'email', title: 'Email', sortable: true }
                ],
                valueField: 'id',
                textField: 'name',
                items: [],
                sortable: true,
                showClear: true,
                onChange: null,
                onClear: null
            }, options || {});

            this.isMulti = this.options.isMulti || this.$original.prop('multiple');
            this.data = [];
            this.filtered = [];
            this.selected = []; // array of IDs
            this.sortColumn = null;
            this.sortAsc = true;
            this._id = 'customtdd_' + Math.random().toString(36).slice(2, 8);
        }

        init() {
            if (!this.$original.length) return this;

            // Remove existing instance if already attached
            var existing = this.$original.data('customTableDdInstance');
            if (existing && existing.$wrapper) {
                existing.$wrapper.remove();
                $(document).off('click.' + existing._id);
            }
            this.$original.next('.custom-tdd-wrapper').remove();

            this._buildSkeleton();
            this._bindEvents();
            this.$original.data('customTableDdInstance', this);

            // Load data from AJAX or Static HTML Options
            if (this.options.isAjax && this.options.ajaxUrl) {
                this.loadFromAjax();
            } else if (this.options.items && this.options.items.length) {
                this.loadItems(this.options.items);
            } else {
                this.loadFromHtmlOptions();
            }

            return this;
        }

        _buildSkeleton() {
            this.$original.hide();

            var multiClass = this.isMulti ? 'custom-tdd-multi' : 'custom-tdd-single';
            this.$wrapper = $(`<div class="custom-tdd-wrapper ${multiClass}" id="${this._id}"></div>`);

            this.$box = $(`
                <div class="custom-tdd-box">
                    <span class="custom-tdd-summary-badge d-none">0 Selected</span>
                    <input type="text" class="custom-tdd-input" placeholder="${this.options.placeholder}" autocomplete="off" />
                    ${this.options.showClear ? '<span class="custom-tdd-clear-icon" title="Clear">&times;</span>' : ''}
                    <span class="custom-tdd-arrow"><i class="fas fa-chevron-down"></i></span>
                </div>
            `);

            this.$input = this.$box.find('.custom-tdd-input');
            this.$summaryBadge = this.$box.find('.custom-tdd-summary-badge');
            this.$clearIcon = this.$box.find('.custom-tdd-clear-icon');

            // Build Dropdown Panel
            this.$panel = $(`<div class="custom-tdd-panel"></div>`);
            
            // Build Table
            this.$tableContainer = $(`<div class="custom-tdd-table-container"></div>`);
            this.$table = $(`<table class="custom-tdd-table table-bordered"></table>`);
            this.$thead = $('<thead></thead>');
            this.$tbody = $('<tbody></tbody>');

            this.$table.append(this.$thead).append(this.$tbody);
            this.$tableContainer.append(this.$table);
            this.$panel.append(this.$tableContainer);

            // Footer info
            this.$footer = $(`
                <div class="custom-tdd-footer">
                    <span class="custom-tdd-count-info">0 records</span>
                    <span class="custom-tdd-selected-info">${this.isMulti ? '0 selected' : ''}</span>
                </div>
            `);
            this.$panel.append(this.$footer);

            this.$wrapper.append(this.$box).append(this.$panel);
            this.$original.after(this.$wrapper);

            this._renderTableHead();
        }

        _renderTableHead() {
            var self = this;
            this.$thead.empty();

            var $tr = $('<tr></tr>');

            // Select All (or Radio column title)
            if (this.isMulti) {
                var $thSelect = $(`
                    <th class="col-select">
                        <input type="checkbox" class="custom-tdd-select-all" title="Select All" />
                    </th>
                `);
                $thSelect.find('.custom-tdd-select-all').on('change', function (e) {
                    e.stopPropagation();
                    self._toggleSelectAll($(this).prop('checked'));
                });
                $tr.append($thSelect);
            } else {
                $tr.append('<th class="col-select">#</th>');
            }

            // Other columns
            this.options.columns.forEach(function (col) {
                var isSortable = self.options.sortable && col.sortable !== false;
                var $th = $(`<th class="${isSortable ? 'sortable' : ''}">${col.title}</th>`);
                if (isSortable) {
                    $th.append('<i class="fas fa-sort sort-icon"></i>');
                    $th.on('click', function (e) {
                        e.stopPropagation();
                        self._sortData(col.data);
                    });
                }
                $tr.append($th);
            });

            this.$thead.append($tr);
        }

        _bindEvents() {
            var self = this;

            // Open on box click
            this.$box.on('click', function (e) {
                if ($(e.target).closest('.custom-tdd-clear-icon').length) return;
                self._togglePanel();
            });

            // Inline search typing
            this.$input.on('input', function () {
                var term = $(this).val();
                self._filterData(term);
                if (!self.$wrapper.hasClass('open')) {
                    self._openPanel();
                }
            });

            // Clear button
            if (this.$clearIcon) {
                this.$clearIcon.on('click', function (e) {
                    e.stopPropagation();
                    self.clear();
                });
            }

            // Prevent panel clicks from closing the dropdown
            this.$panel.on('click', function (e) {
                e.stopPropagation();
            });

            // Outside click to close
            $(document).on('click.' + this._id, function (e) {
                if (!$(e.target).closest(self.$wrapper).length) {
                    self._closePanel();
                }
            });
        }

        loadFromAjax() {
            var self = this;
            $.ajax({
                url: this.options.ajaxUrl,
                method: this.options.ajaxMethod,
                dataType: 'json',
                success: function (data) {
                    if (Array.isArray(data)) {
                        self.loadItems(data);
                    }
                },
                error: function (xhr, err) {
                    console.error('customTableDropdown AJAX error:', err);
                }
            });
        }

        loadFromHtmlOptions() {
            var items = [];
            var self = this;
            this.$original.find('option').each(function () {
                var $opt = $(this);
                var val = $opt.val();
                if (!val) return; // skip placeholder

                var item = {
                    id: val,
                    name: $opt.text().trim(),
                    designation: $opt.data('designation') || '',
                    phone: $opt.data('phone') || $opt.data('mobile') || '',
                    email: $opt.data('email') || '',
                    selected: $opt.is(':selected')
                };

                // Add any other data-* properties
                $.each(this.attributes, function () {
                    if (this.name.startsWith('data-')) {
                        var prop = this.name.replace('data-', '');
                        item[prop] = this.value;
                    }
                });

                items.push(item);
            });

            this.loadItems(items);
        }

        loadItems(items) {
            var self = this;
            this.data = (items || []).map(function (i) {
                var id = String(i[self.options.valueField] ?? i.id ?? i.cpid ?? '');
                var name = String(i[self.options.textField] ?? i.name ?? i.contactPersonName ?? '');
                return Object.assign({}, i, {
                    id: id,
                    name: name,
                    designation: i.designation ?? i.designationName ?? '',
                    phone: i.phone ?? i.contactPersonMobile ?? '',
                    email: i.email ?? i.contactPersonEmail ?? ''
                });
            });

            // Initial selected sync from original select element
            var origVal = this.$original.val();
            if (origVal) {
                this.selected = Array.isArray(origVal) ? origVal.map(String) : [String(origVal)];
            } else {
                this.selected = [];
            }

            this.filtered = this.data.slice();
            this._renderTableBody(this.filtered);
            this._updateDisplay();
        }

        _renderTableBody(items) {
            var self = this;
            this.$tbody.empty();

            if (!items.length) {
                var colCount = this.options.columns.length + 1;
                this.$tbody.append(`<tr><td colspan="${colCount}" class="custom-tdd-empty">No results found</td></tr>`);
                this.$footer.find('.custom-tdd-count-info').text('0 records');
                return;
            }

            var searchTerm = (this.$input.val() || '').trim().toLowerCase();

            items.forEach(function (item) {
                var isSelected = self.selected.indexOf(item.id) !== -1;
                var $tr = $(`<tr class="${isSelected ? 'selected' : ''}" data-id="${item.id}"></tr>`);

                // Selection control
                if (self.isMulti) {
                    $tr.append(`
                        <td class="col-select">
                            <input type="checkbox" class="custom-tdd-row-checkbox" ${isSelected ? 'checked' : ''} />
                        </td>
                    `);
                } else {
                    $tr.append(`
                        <td class="col-select">
                            <input type="radio" name="custom_tdd_radio_${self._id}" ${isSelected ? 'checked' : ''} />
                        </td>
                    `);
                }

                // Columns
                self.options.columns.forEach(function (col) {
                    var val = item[col.data] || '';
                    var displayHtml = self._highlightText(String(val), searchTerm);
                    $tr.append(`<td>${displayHtml}</td>`);
                });

                // Row Click
                $tr.on('click', function (e) {
                    e.stopPropagation();
                    self._onRowClick(item);
                });

                self.$tbody.append($tr);
            });

            this.$footer.find('.custom-tdd-count-info').text(`${items.length} records`);
            if (this.isMulti) {
                this.$footer.find('.custom-tdd-selected-info').text(`${this.selected.length} selected`);
                this._syncSelectAllCheckbox();
            }
        }

        _highlightText(text, term) {
            if (!term || !text) return text;
            var idx = text.toLowerCase().indexOf(term);
            if (idx === -1) return text;
            return text.substring(0, idx) +
                '<span class="custom-tdd-highlight">' + text.substring(idx, idx + term.length) + '</span>' +
                text.substring(idx + term.length);
        }

        _onRowClick(item) {
            if (this.isMulti) {
                var idx = this.selected.indexOf(item.id);
                if (idx === -1) {
                    this.selected.push(item.id);
                } else {
                    this.selected.splice(idx, 1);
                }
                // Keep panel open in multi-select mode!
            } else {
                this.selected = [item.id];
                this._closePanel();
            }

            this._updateDisplay();
            this._renderTableBody(this.filtered);
            this._syncOriginalSelect();
            this._triggerChange();
        }

        _toggleSelectAll(checked) {
            var self = this;
            var currentIds = this.filtered.map(function (i) { return i.id; });

            if (checked) {
                currentIds.forEach(function (id) {
                    if (self.selected.indexOf(id) === -1) self.selected.push(id);
                });
            } else {
                this.selected = this.selected.filter(function (id) { return currentIds.indexOf(id) === -1; });
            }

            this._updateDisplay();
            this._renderTableBody(this.filtered);
            this._syncOriginalSelect();
            this._triggerChange();
        }

        _syncSelectAllCheckbox() {
            if (!this.isMulti) return;
            var currentIds = this.filtered.map(function (i) { return i.id; });
            var allSelected = currentIds.length > 0 && currentIds.every(id => this.selected.indexOf(id) !== -1);
            this.$thead.find('.custom-tdd-select-all').prop('checked', allSelected);
        }

        _filterData(term) {
            term = (term || '').trim().toLowerCase();
            if (!term) {
                this.filtered = this.data.slice();
            } else {
                var cols = this.options.columns.map(c => c.data);
                this.filtered = this.data.filter(function (item) {
                    return cols.some(function (col) {
                        return String(item[col] || '').toLowerCase().indexOf(term) !== -1;
                    });
                });
            }

            this._renderTableBody(this.filtered);
        }

        _sortData(colKey) {
            if (this.sortColumn === colKey) {
                this.sortAsc = !this.sortAsc;
            } else {
                this.sortColumn = colKey;
                this.sortAsc = true;
            }

            var self = this;
            this.filtered.sort(function (a, b) {
                var vA = (a[colKey] || '').toLowerCase();
                var vB = (b[colKey] || '').toLowerCase();
                if (vA < vB) return self.sortAsc ? -1 : 1;
                if (vA > vB) return self.sortAsc ? 1 : -1;
                return 0;
            });

            // Update sort icon
            this.$thead.find('th').find('.sort-icon').attr('class', 'fas fa-sort sort-icon');
            var $th = this.$thead.find(`th:contains("${this.options.columns.find(c => c.data === colKey)?.title}")`);
            $th.find('.sort-icon').attr('class', `fas fa-sort-${this.sortAsc ? 'up' : 'down'} sort-icon`);

            this._renderTableBody(this.filtered);
        }

        _updateDisplay() {
            if (this.isMulti) {
                var count = this.selected.length;
                if (count > 0) {
                    this.$summaryBadge.removeClass('d-none').text(`${count} Selected`);
                    var names = this.selected.map(id => {
                        var item = this.data.find(d => d.id === id);
                        return item ? item.name : id;
                    }).join(', ');
                    this.$input.attr('placeholder', '').val('').attr('title', names);
                } else {
                    this.$summaryBadge.addClass('d-none').text('0 Selected');
                    this.$input.attr('placeholder', this.options.placeholder).val('').removeAttr('title');
                }
            } else {
                this.$summaryBadge.addClass('d-none');
                if (this.selected.length > 0) {
                    var item = this.data.find(d => d.id === this.selected[0]);
                    this.$input.val(item ? item.name : '');
                } else {
                    this.$input.val('');
                }
            }
        }

        _syncOriginalSelect() {
            if (this.isMulti) {
                this.$original.val(this.selected);
            } else {
                this.$original.val(this.selected[0] || '');
            }
        }

        _triggerChange() {
            this.$original.trigger({
                type: 'change',
                customTableDropdown: true
            });
            if (typeof this.options.onChange === 'function') {
                this.options.onChange(this.getValue(), this);
            }
        }

        getValue() {
            return this.isMulti ? this.selected.slice() : (this.selected[0] || null);
        }

        setValue(val) {
            if (val === null || val === undefined || val === '') {
                this.selected = [];
            } else if (Array.isArray(val)) {
                this.selected = val.map(String);
            } else if (typeof val === 'string' && val.includes(',')) {
                this.selected = val.split(',').map(s => s.trim()).filter(Boolean);
            } else {
                this.selected = [String(val)];
            }

            this._syncOriginalSelect();
            this._updateDisplay();
            this._renderTableBody(this.filtered);
        }

        clear() {
            this.selected = [];
            this.$input.val('');
            this._syncOriginalSelect();
            this._updateDisplay();
            this._renderTableBody(this.filtered);

            if (typeof this.options.onClear === 'function') {
                this.options.onClear(this);
            }
            this._triggerChange();
        }

        _openPanel() {
            $('.custom-tdd-wrapper.open').not(this.$wrapper).removeClass('open');
            this.$wrapper.addClass('open');
            this._adjustPanelPosition();
        }

        _closePanel() {
            this.$wrapper.removeClass('open');
            this.$input.val('');
            this._filterData('');
            this._updateDisplay();
        }

        _togglePanel() {
            if (this.$wrapper.hasClass('open')) {
                this._closePanel();
            } else {
                this._openPanel();
            }
        }

        _adjustPanelPosition() {
            var rect = this.$box[0].getBoundingClientRect();
            var panelHeight = 320;
            var windowHeight = window.innerHeight;

            if (rect.bottom + panelHeight > windowHeight && rect.top > panelHeight) {
                this.$panel.css({ 'bottom': '100%', 'top': 'auto', 'margin-top': '0', 'margin-bottom': '4px' });
            } else {
                this.$panel.css({ 'top': '100%', 'bottom': 'auto', 'margin-bottom': '0', 'margin-top': '4px' });
            }
        }
    }

    // Expose globally and helper initializer
    window.CustomTableDropdown = CustomTableDropdown;

    function initCustomTableDropdowns(container, forceReinit) {
        var $container = container ? $(container) : $(document);

        $container.find('select[data-table-view="true"], select.custom-table-dropdown').each(function () {
            var $el = $(this);
            if ($el.data('customTableDdInstance') && !forceReinit) return;

            var isMulti = $el.prop('multiple');
            var ajaxUrl = $el.data('ajax-url');
            var placeholder = $el.data('placeholder') || 'Select...';

            new CustomTableDropdown('#' + $el.attr('id'), {
                placeholder: placeholder,
                isMulti: isMulti,
                isAjax: !!ajaxUrl,
                ajaxUrl: ajaxUrl
            }).init();
        });
    }

    window.initCustomTableDropdowns = initCustomTableDropdowns;

    $(function () {
        initCustomTableDropdowns(document, false);
    });

})(window, jQuery);
