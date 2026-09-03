document.body.addEventListener('click', function(e) {
    let btn = e.target.closest('.js-quick-add');
    if (!btn) return;
    
    let target = btn.getAttribute('data-target');
    
    // Only intercept for the static Ledger modals
    if (target === '#GRLModal' || target === '#UICRLModal' || target === '#SCRLModal' || target === '#SSLModal') {
        e.preventDefault();
        e.stopPropagation(); // Stops gc-modal.js from making an AJAX request
        
        let tabPane = btn.closest('.tab-pane');
        let currentTabId = tabPane ? tabPane.getAttribute('id') : null;
        let $modal = $(target);
        
        $modal.data('from-tab', currentTabId);
        
        // Load the fresh data for the specific modal using DataTables
        if (target === '#GRLModal' && $.fn.DataTable.isDataTable('#modalGroupLedgerTable')) {
            $('#modalGroupLedgerTable').DataTable().ajax.reload(null, false);
        }
        if (target === '#UICRLModal' && $.fn.DataTable.isDataTable('#modalControlLedgerTable')) {
            $('#modalControlLedgerTable').DataTable().ajax.reload(null, false);
        }
        if (target === '#SCRLModal' && $.fn.DataTable.isDataTable('#modalSubControlLedgerTable')) {
            $('#modalSubControlLedgerTable').DataTable().ajax.reload(null, false);
        }
        if (target === '#SSLModal' && $.fn.DataTable.isDataTable('#modalSubSidiaryLedgerTable')) {
            $('#modalSubSidiaryLedgerTable').DataTable().ajax.reload(null, false);
        }
        
        // Open the static modal
        $modal.modal('show');
    }
}, true); // Use capture phase to run before jQuery delegated events

// Optional: Log when modals close
$('#GRLModal, #UICRLModal, #SCRLModal, #SSLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log(this.id + ' closed from tab:', fromTab);
});

//#endregion

//#region Dropdown Mapping & Helpers

const groupDropdownMap = {
    control: 'GLName',                          // Control Ledger Tab
    subControl: 'groupName',                   // Sub-Control Ledger Tab
    subSidiary: 'forsidiarygroupName',          // Sub-Sidiary Ledger Tab
    general: 'GeneralgrlName',                 // General Ledger Tab
    modalControl: 'ModalGLName',               // Control Modal
    modalSubControl: 'ModalgroupName',         // Sub-Control Modal
    modalSubSidiary: 'ModalforsidiarygroupName'// Sub-Sidiary Modal
};

// Active Tab Detector
function getActiveTabKey() {
    const activeTabId = $('#ledgertab .nav-link.active').attr('id');

    switch (activeTabId) {
        case 'controll-tab': return 'control';
        case 'subcontroll-tab': return 'subControl';
        case 'subsidiary-tab': return 'subSidiary';
        case 'general-tab': return 'general';
        default: return null;
    }
}

// Helper: Custom Select Single & Native Select Sync
function syncSingleDropdownOption(dropdownId, savedId, savedName, isSelected = false) {
    const $select = $(`#${dropdownId}`);
    if (!$select.length) return;

    // 1. Native Select Update
    let $existingOption = $select.find(`option[value="${savedId}"]`);
    if ($existingOption.length === 0) {
        $select.append(new Option(savedName, savedId, isSelected, isSelected));
    }

    if (isSelected) {
        $select.val(savedId).trigger('change');
    }

    // 2. Custom Select Single UI (.custom-dd-wrapper) Update
    const $wrapper = $select.siblings('.custom-dd-wrapper');
    if ($wrapper.length > 0) {
        const $list = $wrapper.find('.custom-dd-list');
        $list.find('.custom-dd-empty').remove();

        let $customItem = $list.find(`.custom-dd-item[data-value="${savedId}"]`);
        if ($customItem.length === 0) {
            $customItem = $(`<div class="custom-dd-item" data-value="${savedId}">${savedName}</div>`);
            $list.append($customItem);
        }

        if (isSelected) {
            $wrapper.find('.custom-dd-item').removeClass('selected');
            $customItem.addClass('selected');
            $wrapper.find('.custom-dd-input').val(savedName);
        }
    }
}

// Update Active Tab Dropdown & Select
function updateGroupDropdown(savedId, savedName) {
    const tabKey = getActiveTabKey();
    if (!tabKey) return;

    const dropdownId = groupDropdownMap[tabKey];
    if (dropdownId) {
        syncSingleDropdownOption(dropdownId, savedId, savedName, true);
    }

    $('#GRLModal').modal('hide');
}

// Update Remaining Tab Dropdowns without auto-selecting
function updateAllGroupDropdowns(savedId, savedName) {
    const activeTabKey = getActiveTabKey();

    Object.entries(groupDropdownMap).forEach(([tabKey, dropdownId]) => {
        if (tabKey === activeTabKey) return; // Active tab skip
        syncSingleDropdownOption(dropdownId, savedId, savedName, false);
    });
}

//#endregion