


//#region dynamic table version 1


const DynamicTable = {

    
    // Initialize all tables with class 'dyn-table'
    init() {
        document.querySelectorAll('.dyn-table').forEach(table => {
            const tableId = table.id || `dyn-table-${Math.random().toString(36).substr(2, 9)}`; // Unique ID for each table
            if (!table.id) table.id = tableId; // Assign ID if none exists
            this.initializeTable(table, tableId);
        });
    },

    // Initialize column visibility for a single table
    initializeTable(table, tableId) {
        const headers = table.querySelectorAll('thead th[data-column]');
        const columnVisibility = this.loadColumnVisibility(tableId);
        const columnToggleContainer = this.createColumnToggleDropdown(table, headers, tableId);

        // Insert dropdown into the DOM (e.g., in the card header)
        const cardHeader = table.closest('.card')?.querySelector('.card-header');
        if (cardHeader) {
            const rightContent = cardHeader.querySelector('.right-content') || document.createElement('div');
            if (!rightContent.classList.contains('right-content')) {
                rightContent.classList.add('right-content', 'd-flex', 'align-items-center', 'flex-wrap', 'row-gap-3');
                cardHeader.appendChild(rightContent);
            }
            rightContent.prepend(columnToggleContainer);
        }

        // Apply initial column visibility
        headers.forEach(header => {
            const columnIndex = header.dataset.column;
            if (columnVisibility.hasOwnProperty(columnIndex)) {
                this.toggleColumn(table, columnIndex, columnVisibility[columnIndex]);
            }
        });

        // Event listeners for column toggles
        columnToggleContainer.querySelectorAll('.column-toggle').forEach(toggle => {
            toggle.addEventListener('change', () => {
                this.toggleColumn(table, toggle.dataset.column, toggle.checked);
                this.saveColumnVisibility(tableId, toggle.dataset.column, toggle.checked);
            });
        });

        // Select All and Reset buttons
        columnToggleContainer.querySelector('#selectAllColumns')?.addEventListener('click', () => {
            columnToggleContainer.querySelectorAll('.column-toggle').forEach(toggle => {
                toggle.checked = true;
                this.toggleColumn(table, toggle.dataset.column, true);
                this.saveColumnVisibility(tableId, toggle.dataset.column, true);
            });
        });

        columnToggleContainer.querySelector('#resetColumns')?.addEventListener('click', () => {
            columnToggleContainer.querySelectorAll('.column-toggle').forEach(toggle => {
                toggle.checked = true;
                this.toggleColumn(table, toggle.dataset.column, true);
            });
            localStorage.removeItem(`${tableId}_columnVisibility`);
        });

        // Prevent dropdown from closing when clicking inside
        columnToggleContainer.querySelector('.column-toggle-dropdown').addEventListener('click', e => {
            e.stopPropagation();
        });
    },

    // Create column toggle dropdown UI
    createColumnToggleDropdown(table, headers, tableId) {
        const dropdown = document.createElement('div');
        dropdown.classList.add('dropdown', 'me-3');
        dropdown.innerHTML = `
            <button class="btn btn-light dropdown-toggle bg-white text-muted" type="button" data-bs-toggle="dropdown" aria-expanded="false" style="border: 1px solid #cbd0dd;color: #979aa3 !important;">
                <i class="fas fa-columns me-2"></i>Columns
            </button>
            <div class="dropdown-menu column-toggle-dropdown p-2">
                <div class="fw-bold mb-2 text-muted">Show/Hide Columns</div>
                ${Array.from(headers).map(header => `
                    <div class="column-toggle-item">
                        <div class="form-check">
                            <input class="form-check-input column-toggle" type="checkbox" id="col-${tableId}-${header.dataset.column}" data-column="${header.dataset.column}" checked>
                            <label class="form-check-label" for="col-${tableId}-${header.dataset.column}">${header.textContent.trim()}</label>
                        </div>
                    </div>
                `).join('')}
                <hr class="my-2">
                <div class="d-flex gap-2">
                    <button class="btn btn-sm btn-outline-primary" id="selectAllColumns">Select All</button>
                    <button class="btn btn-sm btn-outline-secondary" id="resetColumns">Reset</button>
                </div>
            </div>
        `;
        return dropdown;
    },

    // Toggle column visibility
    toggleColumn(table, columnIndex, isVisible) {
        const headers = table.querySelectorAll(`th[data-column="${columnIndex}"]`);
        const cells = table.querySelectorAll(`td[data-column="${columnIndex}"]`);
        console.log(`Toggling column ${columnIndex} in table ${table.id}: headers=${headers.length}, cells=${cells.length}`);
        if (isVisible) {
            headers.forEach(header => header.style.display = '');
            cells.forEach(cell => cell.style.display = '');
        } else {
            headers.forEach(header => header.style.display = 'none');
            cells.forEach(cell => cell.style.display = 'none');
        }
    },

    // Load column visibility from localStorage
    loadColumnVisibility(tableId) {
        try {
            const saved = localStorage.getItem(`${tableId}_columnVisibility`);
            return saved ? JSON.parse(saved) : {};
        } catch (e) {
            console.error(`Failed to load column visibility for table ${tableId}:`, e);
            return {};
        }
    },

    // Save column visibility to localStorage
    saveColumnVisibility(tableId, columnIndex, isVisible) {
        const columnVisibility = this.loadColumnVisibility(tableId);
        columnVisibility[columnIndex] = isVisible;
        localStorage.setItem(`${tableId}_columnVisibility`, JSON.stringify(columnVisibility));
    },


   // DynamicTable.applyColumnVisibilityToNewRows(document.getElementById('id'), 'id');

    
    applyColumnVisibilityToNewRows(table, tableId) {
        const columnVisibility = this.loadColumnVisibility(tableId);
        Object.keys(columnVisibility).forEach(columnIndex => {
            if (!columnVisibility[columnIndex]) {
                table.querySelectorAll(`td[data-column="${columnIndex}"]`).forEach(cell => {
                    cell.style.display = 'none';
                });
            }
        });
    }
};

// Initialize on document ready
$(document).ready(function () {
    DynamicTable.init();
});


    //#endregion