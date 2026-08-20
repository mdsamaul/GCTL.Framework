

//#region Group Ledger Modal Js

$(document).on('click', '.openGroupLedgerModal', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id'); 
    console.log("Modal opened from:", currentTab);

    $('#GRLModal').data('from-tab', currentTab);

//    $('#GRLModal').modal('show');
});

$('#GRLModal').on('hidden.bs.modal', function () {
    console.log('Modal closed');
});

//#endregion


//#region Control Ledger Modal Js

$(document).on('click', '.openControlLedgerModal', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Control Ledger Modal opened from:", currentTab);

    $('#UICRLModal').data('from-tab', currentTab);


});

$('#UICRLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Control Ledger Modal closed from tab:', fromTab);
});

//#endregion


//#region Sub-Control Ledger Modal Js

$(document).on('click', '.AddSCRL', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Sub-Control Ledger Modal opened from:", currentTab);

    $('#SCRLModal').data('from-tab', currentTab);
});

$('#SCRLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Sub-Control Ledger Modal closed from tab:', fromTab);
});

//#endregion


//#region Sub-Sidiary Ledger Modal Js

$(document).on('click', '.SubSidiModal', function () {
    let currentTab = $(this).closest('.tab-pane').attr('id');
    console.log("Sub-Sidiary Ledger Modal opened from:", currentTab);

    $('#SSLModal').data('from-tab', currentTab);
});

$('#SSLModal').on('hidden.bs.modal', function () {
    let fromTab = $(this).data('from-tab');
    console.log('Sub-Sidiary Ledger Modal closed from tab:', fromTab);
});

//#endregion


const groupDropdownMap = {
    //group: 'ControlLedgerName',             // Group Ledger Tab
    control: 'GLName',             // Control Ledger Tab
    subControl: 'groupName',  // Sub-Control Ledger Tab
    subSidiary: 'forsidiarygroupName', // Sub-Sidiary Ledger Tab
    general: 'GeneralgrlName',  // General Ledger Tab
    modalControl: 'ModalGLName', // Control Modal
    modalSubControl: 'ModalgroupName', // Sub-Control Modal
    modalSubSidiary: 'ModalforsidiarygroupName', // Sub-Sidiary Modal
};


// Active tab detect function
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



function updateGroupDropdown(savedId, savedName) {
    const tabKey = getActiveTabKey();
    if (!tabKey) return;

    const dropdownId = groupDropdownMap[tabKey];
    const instance = choiceManager.instances[dropdownId];

    if (instance) {
        instance.setChoices([
            { value: String(savedId), label: savedName, selected: true }
        ], 'value', 'label', false);

        choiceManager.setChoiceValue(dropdownId, savedId);
    } else {
        $(`#${dropdownId}`).append(new Option(savedName, savedId, true, true)).trigger('change');
    }
    //if (tabKey === 'group') {
    //    LoadGroupLedger(); 
    //}
    $('#GRLModal').modal('hide');
}

function updateAllGroupDropdowns(savedId, savedName) {
    const activeTabKey = getActiveTabKey();

    Object.entries(groupDropdownMap).forEach(([tabKey, dropdownId]) => {
        if (tabKey === activeTabKey) return; // skip active tab

        const instance = choiceManager.instances[dropdownId];

        if (instance) {
            instance.setChoices([
                { value: String(savedId), label: savedName, selected: false }
            ], 'value', 'label', false);
        } else {
            $(`#${dropdownId}`).append(new Option(savedName, savedId, false, false)).trigger('change');
        }
    });
}






