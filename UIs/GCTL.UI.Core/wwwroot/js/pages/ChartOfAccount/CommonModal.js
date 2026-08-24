//#region Modal Open & Close Event Handlers

// Group Ledger Modal
$(document).on('click', '.openGroupLedgerModal, .js-quick-add[data-target="#GRLModal"]', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Group Ledger Modal opened from:", currentTab);
    $('#GRLModal').data('from-tab', currentTab);
});

$('#GRLModal').on('hidden.bs.modal', function () {
    console.log('Group Ledger Modal closed');
});

// Control Ledger Modal
$(document).on('click', '.openControlLedgerModal, .js-quick-add[data-target="#UICRLModal"]', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Control Ledger Modal opened from:", currentTab);
    $('#UICRLModal').data('from-tab', currentTab);
});

$('#UICRLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Control Ledger Modal closed from tab:', fromTab);
});

// Sub-Control Ledger Modal
$(document).on('click', '.AddSCRL, .js-quick-add[data-target="#SCRLModal"]', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Sub-Control Ledger Modal opened from:", currentTab);
    $('#SCRLModal').data('from-tab', currentTab);
});

$('#SCRLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Sub-Control Ledger Modal closed from tab:', fromTab);
});

// Sub-Sidiary Ledger Modal
$(document).on('click', '.SubSidiModal, .js-quick-add[data-target="#SSLModal"]', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Sub-Sidiary Ledger Modal opened from:", currentTab);
    $('#SSLModal').data('from-tab', currentTab);
});

$('#SSLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Sub-Sidiary Ledger Modal closed from tab:', fromTab);
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