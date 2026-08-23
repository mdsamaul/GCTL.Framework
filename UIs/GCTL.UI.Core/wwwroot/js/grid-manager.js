/**
 * Global Grid Manager JavaScript Module
 * Handles AJAX page navigation, live search box, page size selection,
 * master/row checkbox state synchronization, and selection retrieval.
 */
(function (window, document) {
    'use strict';

    var gridSearchTimers = {};

    var GridManager = {
        /**
         * Generic AJAX page switching function for dynamic grids.
         * @param {string} containerId - The HTML DOM container element ID.
         * @param {string} url - The controller endpoint URL.
         * @param {number} page - Target page index.
         * @param {number} pageSize - Number of items per page.
         * @param {string} [searchTerm] - Optional search query filter.
         */
        loadPage: function (containerId, url, page, pageSize, searchTerm) {
            if (!containerId || !url) {
                console.error('[GridManager] Container ID and URL are required.');
                return;
            }

            var container = document.getElementById(containerId);
            if (!container) {
                console.error('[GridManager] Target container element #' + containerId + ' not found.');
                return;
            }

            // Auto extract current search term if omitted
            if (searchTerm === undefined || searchTerm === null) {
                searchTerm = GridManager.getSearchTerm(containerId);
            }

            // Extract page size from DOM if omitted
            if (!pageSize) {
                var pageSizeSelect = container.querySelector('.grid-pagesize-select');
                pageSize = pageSizeSelect ? pageSizeSelect.value : 10;
            }

            // Build URL with query parameters
            var separator = url.indexOf('?') !== -1 ? '&' : '?';
            var targetUrl = url + separator + 'page=' + encodeURIComponent(page)
                                  + '&pageSize=' + encodeURIComponent(pageSize)
                                  + '&searchTerm=' + encodeURIComponent(searchTerm || '');

            // Add visual loading state overlay
            container.style.opacity = '0.5';
            container.style.pointerEvents = 'none';

            fetch(targetUrl, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('HTTP error! Status: ' + response.status);
                }
                return response.text();
            })
            .then(function (html) {
                var tempDiv = document.createElement('div');
                tempDiv.innerHTML = html;

                // Robust lookup: search by element ID first, then class, fallback to first child
                var newGridElement = tempDiv.querySelector('#' + containerId) || 
                                     tempDiv.querySelector('.dynamic-grid-card') || 
                                     tempDiv.firstElementChild;

                if (newGridElement) {
                    container.parentNode.replaceChild(newGridElement, container);
                    
                    // Maintain cursor focus in search box after AJAX refresh if active
                    var newSearchInput = newGridElement.querySelector('.grid-search-input');
                    if (newSearchInput && searchTerm) {
                        newSearchInput.focus();
                        newSearchInput.setSelectionRange(newSearchInput.value.length, newSearchInput.value.length);
                    }

                    // Dispatch custom event for external listeners
                    document.dispatchEvent(new CustomEvent('grid:loaded', {
                        detail: { containerId: containerId, page: page, pageSize: pageSize, searchTerm: searchTerm }
                    }));
                } else {
                    container.style.opacity = '1';
                    container.style.pointerEvents = 'auto';
                }
            })
            .catch(function (error) {
                console.error('[GridManager] Failed to load grid page:', error);
                container.style.opacity = '1';
                container.style.pointerEvents = 'auto';
            });
        },

        /**
         * Triggered when the page size dropdown value changes.
         */
        changePageSize: function (containerId, url, pageSize) {
            var searchTerm = GridManager.getSearchTerm(containerId);
            GridManager.loadPage(containerId, url, 1, pageSize, searchTerm);
        },

        /**
         * Debounced search handler for search input keyup events.
         */
        handleSearchKeyup: function (event, containerId, url, pageSize) {
            if (gridSearchTimers[containerId]) {
                clearTimeout(gridSearchTimers[containerId]);
            }

            var searchTerm = event.target.value;
            gridSearchTimers[containerId] = setTimeout(function () {
                GridManager.loadPage(containerId, url, 1, pageSize, searchTerm);
            }, 350); // 350ms debounce
        },

        /**
         * Reads the current search term value from the grid container.
         */
        getSearchTerm: function (containerId) {
            var container = document.getElementById(containerId);
            if (!container) return '';
            var input = container.querySelector('.grid-search-input');
            return input ? input.value : '';
        },

        /**
         * Global helper to retrieve an array of selected (checked) primary key IDs.
         * @param {string} tableOrContainerId - DOM element ID of the grid wrapper.
         * @returns {Array<string>} Array of checked values.
         */
        getSelectedIds: function (tableOrContainerId) {
            var container = document.getElementById(tableOrContainerId);
            if (!container) {
                console.warn('[GridManager] Container element #' + tableOrContainerId + ' not found.');
                return [];
            }

            var checkedBoxes = container.querySelectorAll('.grid-row-checkbox:checked');
            var selectedIds = [];
            checkedBoxes.forEach(function (cb) {
                if (cb.value) {
                    selectedIds.push(cb.value);
                }
            });

            return selectedIds;
        },

        /**
         * Synchronizes master "Select All" checkbox state based on row checkbox selection states.
         * @param {string} gridId - The DOM element ID of the grid.
         */
        syncSelectAllState: function (gridId) {
            var container = document.getElementById(gridId);
            if (!container) return;

            var selectAllCb = container.querySelector('.grid-select-all');
            if (!selectAllCb) return;

            var rowBoxes = container.querySelectorAll('.grid-row-checkbox');
            if (rowBoxes.length === 0) {
                selectAllCb.checked = false;
                selectAllCb.indeterminate = false;
                return;
            }

            var checkedCount = container.querySelectorAll('.grid-row-checkbox:checked').length;
            if (checkedCount === 0) {
                selectAllCb.checked = false;
                selectAllCb.indeterminate = false;
            } else if (checkedCount === rowBoxes.length) {
                selectAllCb.checked = true;
                selectAllCb.indeterminate = false;
            } else {
                selectAllCb.checked = false;
                selectAllCb.indeterminate = true;
            }
        }
    };

    // Initialize Event Delegation for Checkbox Interactions
    document.addEventListener('change', function (event) {
        var target = event.target;

        // Master "Select All" Checkbox handler
        if (target && target.classList.contains('grid-select-all')) {
            var gridId = target.getAttribute('data-grid-id');
            var container = document.getElementById(gridId);
            if (container) {
                var rowBoxes = container.querySelectorAll('.grid-row-checkbox');
                var isChecked = target.checked;
                rowBoxes.forEach(function (cb) {
                    cb.checked = isChecked;
                });
            }
        }

        // Row-level Checkbox handler
        if (target && target.classList.contains('grid-row-checkbox')) {
            var gridId = target.getAttribute('data-grid-id');
            if (gridId) {
                GridManager.syncSelectAllState(gridId);
            }
        }
    });

    // Expose GridManager globally on window
    window.GridManager = GridManager;
    window.loadGridPage = GridManager.loadPage;
    window.getSelectedGridIds = GridManager.getSelectedIds;

})(window, document);
