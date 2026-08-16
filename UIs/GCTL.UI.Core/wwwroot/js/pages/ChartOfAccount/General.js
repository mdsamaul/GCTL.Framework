let selectedGeneralLedger = [];
let currenGeneraltPage = 1;
let GeneralpageSize = 10;
let GeneralcurrentSortColumn = 'SubSusidiaryLedgerCodeNo';
let GeneralcurrentSortDirection = 'desc';
console.log(`General Page Global Called: ${selectedGeneralLedger}`);
let isEditModeGeneral = false; 


//Selected dropdown values (for pagination reload)
let selectedGroupCodeforGL = "";
let selectedControlCodeforGL = "";
let selectedSubControlCodeforGL = "";
let selectedSubsidiaryCodeforGL = "";


//#region Custom Event Listener For Group Ledger Dropdown Updated

window.addEventListener('groupLedgerUpdated', function (e) {
    const newControlLedgerCodeNo = e.detail.newControlLedgerCodeNo;
    console.log("Updated Group Ledger Code:", newControlLedgerCodeNo);

    loadGeneralLedgerData();
    $("#Generalgrlcode").val("");
    $("#GeneralgrlshortName").val("");

    $("#GeneralCRLCode").val("");
    $("#GeneralCRLShortName").val("");

    $("#GeneralSCRLCode").val("");
    $("#GeneralSCRLShortname").val("");

    $("#GeneralSSLCode").val("");
    $("#GeneralSSLShortName").val("");
    $("#GeneralLedgerCode").val("");

    GeneralLedgerAll();

});

//#endregion


//#region Ready Part

$(document).ready(function () {

    loadGeneralLedgerData();

    CashFlowDropdown();

    GeneralLedgerAll();

    // Set Entry Date
    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    //Handle Save Or Update Button Handle
    $(document).on("submit", "#generalLedgerForm", function (e) {
        e.preventDefault();
        GeneralLedgerSaveOrUpdate();
    });

    // Handle Delete Button
    $(document).on("click", "#GeneraldeleteBtn", function (e) {
        e.preventDefault();
        GeneralLedgerDelete();
    });

    // Handle Clear button
    $(document).on("click", ".GeneralresetBtn", function () {
        console.log("Clerar Button Clicked")
        GeneralLedgerFormClear();
    });

    // Handle print button
    $(".printBtn").on("click", function (e) {
        e.preventDefault();
        printForm();
    });

    // Handle favorite button
    $(".GeneralfavBtn").on("click", function () {
        toggleFavorite();
    });


    //Searching Handle
    $(document).on('input', '.GeneralsearchInput', Generaldebounce(function () {
        const searchValue = $('.GeneralsearchInput').val();
        console.log("General Ledger Search Value: ", searchValue);
        GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, searchValue);
    }, 500));

    //Fast Page Button
    $(document).on('click', '#GeneralfirstPage', function () {
        currenGeneraltPage = 1;
        //    GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
        GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);

    });

    //Next Page Button
    $(document).on('click', '#GeneralnextPage', function () {
        currenGeneraltPage++;
        //    GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
        GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);

    });

    //Previous Page Button
    $(document).on('click', '#GeneralprevPage', function () {
        if (currenGeneraltPage > 1) {
            currenGeneraltPage--;
            //    GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
            GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);

        }
    });

    //Last Page Button
    $(document).on('click', '#GenerallastPage', function () {
        let lastPage = $('#GeneralpageNavigation button').last().text();
        lastPage = parseInt(lastPage);
        if (!isNaN(lastPage)) {
            currenGeneraltPage = lastPage;
            //    GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
            GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);

        }
    });

    //Showing Page Size
    $(document).on('change', '.GeneralpageSize', function () {
        GeneralpageSize = parseInt($(this).val());
        GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
    });

    // Sorting
    $(document).on('click', '.Generalsortable', function () {
        const column = $(this).data('column');

        if (column === GeneralcurrentSortColumn) {
            GeneralcurrentSortDirection = (GeneralcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            GeneralcurrentSortColumn = column;
            GeneralcurrentSortDirection = 'desc';
        }

        GeneralLedgerAll(1, SubSidiarypageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, $('#GeneralsearchInput').val().trim());
    });

    GeneralbindCheckboxHandlers();

    setGeneralLedgerTabIndex();
    //    setDropdownTabIndex();
});

//#endregion


//#region TabIndex & Entry key
function setGeneralLedgerTabIndex() {
    const dropdownIds = ['GeneralgrlName', 'GeneralCRLName', 'GeneralSCRLName', 'GeneralSSLName',];

    // Choices.js dropdowns and tabindex
    dropdownIds.forEach((id, index) => {
        const choiceInstance = choiceManager.instances[id];
        console.log(choiceInstance)
        if (choiceInstance) {

            const el = choiceInstance.containerOuter.element;
            //el.setAttribute('tabindex', index + 1);

            // Enter key with next dropdown focus
            el.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault(); // Form submit avoid
                    if (index + 1 < dropdownIds.length) {
                        const nextChoice = choiceManager.instances[dropdownIds[index + 1]];
                        if (nextChoice) nextChoice.containerOuter.element.focus();
                    }
                    else {                       
                        const generalLedgerInput = document.getElementById('GeneralLName');
                        if (generalLedgerInput) generalLedgerInput.focus();
                    }
                }
            });
        }
    });

    // General Ledger Name Input tabindex and Enter key handle
    const generalLedgerInput = document.getElementById('GeneralLName');
    if (generalLedgerInput) {
        //generalLedgerInput.setAttribute('tabindex', dropdownIds.length + 1);
        generalLedgerInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();

                const submitBtn = document.querySelector('.saveBtn');
                if (submitBtn) submitBtn.focus();
            }
        });
    }

}


function setDropdownTabIndex() {
    const dropdownIds = [
        'GeneralgrlName',
        'GeneralCRLName',
        'GeneralSCRLName',
        'GeneralSSLName',
        'CashFlowTypeID'
    ];

    dropdownIds.forEach((id, index) => {
        const choiceInstance = choiceManager.instances[id];
        if (choiceInstance) {
            choiceInstance.containerOuter.element.setAttribute('tabindex', index + 1);
        }
    });
}

//#endregion


//#region General Ledger UI: Async Functions for Dropdowns and Data Loading

async function loadGeneralLedgerData() {
    try {
        // Load Group Ledger Dropdown
        const groupResponse = await $.ajax({
            url: "/generalUi-grl-dropdown",
            type: "GET",
        });
        choiceManager.populateDropdown('GeneralgrlName', groupResponse.data);


        // Bind Change Event for Group Ledger
        let $groupDropdown = $("#GeneralgrlName");
        $groupDropdown.off('change').on('change', async function () {
            const selectedGroupCode = $(this).val();
            if (!selectedGroupCode) {
                resetGroupLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", "");
                return;
            }

            // Load Group Info for the selected Group Code
            const groupInfo = await $.ajax({
                url: '/generalUi-grl-info',
                type: 'GET',
                data: { groupcode: selectedGroupCode },
            });

            // Populate Group Info
            $('#Generalgrlcode').val(groupInfo.controlLedgerCodeNo);
            $('#GeneralgrlshortName').val(groupInfo.shortName || '');
            GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL,  "", "", "");

        });


    } catch (error) {
        console.error("Error loading general ledger data:", error);
    }
}




$('#GeneralgrlName').on('change', function (e) {
    e.preventDefault();
    selectedGroupCodeforGL = $(this).val() || "";

    loadControlLedgerDropdown(selectedGroupCodeforGL);
    //var id = $(this).val();
    //loadControlLedgerDropdown(id);
})


async function loadControlLedgerDropdown(groupCode) {
    try {
        const controlResponse = await $.ajax({
            url: '/generalUi-crl-dropdown',
            type: 'GET',
            data: { groupcode: groupCode },
        });
        choiceManager.populateDropdown('GeneralCRLName', controlResponse.data);



        let $controlDropdown = $("#GeneralCRLName");
        $controlDropdown.off('change').on('change', async function () {
            const selectedControlCode = $(this).val();
            if (!selectedControlCode) {
                resetControlLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", groupCode, "", "", "");
                return;
            }

            // Load Control Info for the selected Control Code
            const controlInfo = await $.ajax({
                url: '/generalUi-crl-info',
                type: 'GET',
                data: { contrlcode: selectedControlCode },
            });

            $('#GeneralCRLCode').val(controlInfo.subControlLedgerCodeNo);
            $('#GeneralCRLShortName').val(controlInfo.shortName || '');

            GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", groupCode, selectedControlCodeforGL, "", "");

        });

    } catch (error) {
        console.error("Error loading control ledger:", error);
    }
}


$('#GeneralCRLName').on('change', function (e) {
    e.preventDefault();

    selectedControlCodeforGL = $(this).val() || "";
    loadSubControlLedgerDropdown(selectedControlCodeforGL);


    //var id = $(this).val();
    // loadSubControlLedgerDropdown(id);
})


async function loadSubControlLedgerDropdown(controlCode) {
    try {
        const subControlResponse = await $.ajax({
            url: '/generalUi-subcrl-dropdown',
            type: 'GET',
            data: { controlcode: controlCode },
        });
        choiceManager.populateDropdown('GeneralSCRLName', subControlResponse.data);



        let $subControlDropdown = $("#GeneralSCRLName");
        $subControlDropdown.off('change').on('change', async function () {
            const selectedSubControlCode = $(this).val();
            if (!selectedSubControlCode) {
                resetSubControlLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", controlCode, "", "");
                return;
            }

            // Load Sub-Control Info for the selected Sub-Control Code
            const subControlInfo = await $.ajax({
                url: '/generalUi-subcrl-info',
                type: 'GET',
                data: { subcontrlcode: selectedSubControlCode },
            });

            $('#GeneralSCRLCode').val(subControlInfo.generalLedgerCodeNo);
            $('#GeneralSCRLShortname').val(subControlInfo.shortName || '');

            GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", controlCode, selectedSubControlCodeforGL, "");

        });

    } catch (error) {
        console.error("Error loading sub-control ledger:", error);
    }
}


$('#GeneralSCRLName').on('change', function (e) {
    e.preventDefault(); 
    selectedSubControlCodeforGL = $(this).val() || "";
    loadSubSidiaryLedgerDropdown(selectedSubControlCodeforGL);

    //var id = $(this).val();
    // loadSubSidiaryLedgerDropdown(id);
})


async function loadSubSidiaryLedgerDropdown(subControlCode) {
    try {
        
        const sslResponse = await $.ajax({
            url: '/generalUi-ssl-dropdown',
            type: 'GET',
            data: { subcontrolcode: subControlCode },
        });
        choiceManager.populateDropdown('GeneralSSLName', sslResponse.data);

        let $sslDropdown = $("#GeneralSSLName");
        $sslDropdown.off('change').on('change', async function () {
            const selectedSSLCode = $(this).val();
            selectedSubsidiaryCodeforGL = selectedSSLCode || ""; //for updated global variable
            if (!selectedSSLCode) {
                resetSubSidiaryLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "","");
                return;
            }

            // Load Sub-Sidiary Info for the selected Sub-Sidiary Code
            const sslInfo = await $.ajax({
                url: '/generalUi-ssl-info',
                type: 'GET',
                data: { subsidiarycode: selectedSSLCode },
            });

            $('#GeneralSSLCode').val(sslInfo.susidiaryLedgerCodeNo);
            $('#GeneralSSLShortName').val(sslInfo.shortName || '');

            if (!isEditModeGeneral) {
                $.ajax({
                    url: '/next-general-ledger-code', 
                    type: 'GET',
                    data: { subsidiaryLedgercode: selectedSSLCode }, 
                    success: function (nextCode) {
                        $('#GeneralLedgerCode').val(nextCode);
                    },
                    error: function (xhr, status, error) {
                        console.error("Error fetching next General Ledger code:", error);
                    }
                });
            }


            GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", selectedSSLCode);
        });

    } catch (error) {
        console.error("Error loading subsidiary ledger:", error);
    }
}

function resetGroupLedgerFields() {
    $('#Generalgrlcode').val('');
    $('#GeneralgrlshortName').val('');
  
    choiceManager.resetChoice('GeneralCRLName', 'GeneralSCRLName');
   
    choiceManager.resetChoice('GeneralSSLName');
    //choiceManager.clearChoice('GeneralCRLName');
    //choiceManager.clearChoice('GeneralSCRLName');
    //choiceManager.clearChoice('GeneralSSLName');
    $('#GeneralCRLCode').val('');
    $('#GeneralCRLShortName').val('');
    $('#GeneralSCRLCode').val('');
    $('#GeneralSCRLShortname').val('');
    $('#GeneralSSLCode').val('');
    $('#GeneralSSLShortName').val('');
}

function resetControlLedgerFields() {
    $('#GeneralCRLCode').val('');
    $('#GeneralCRLShortName').val('');
    choiceManager.clearChoice('GeneralSCRLName');
    choiceManager.clearChoice('GeneralSSLName');
    $('#GeneralSCRLCode').val('');
    $('#GeneralSCRLShortname').val('');
    $('#GeneralSSLCode').val('');
    $('#GeneralSSLShortName').val('');
}

function resetSubControlLedgerFields() {
    $('#GeneralSCRLCode').val('');
    $('#GeneralSCRLShortname').val('');
    choiceManager.clearChoice('GeneralSSLName');
    $('#GeneralSSLCode').val('');
    $('#GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');

}

function resetSubSidiaryLedgerFields() {
    $('#GeneralSSLCode').val('');
    $('#GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');
}

//#endregion


//#region Cash Flow Dropdown

function CashFlowDropdown() {
    $.ajax({
        url: "/cash-flow-dropdown",
        type: "GET",
        success: function (cashdata) {
            choiceManager.populateDropdown('CashFlowTypeID', cashdata.data);
        },
        error: function () {
            console.log("Failed Cash Flow Dropdown");
        }
    });
}

//#endregion


//#region Load All General Ledger Data
function GeneralLedgerAll(page = 1, GeneralpageSize = 10, sortColumn = GeneralcurrentSortColumn, sortDirection = GeneralcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "", subcontrolLedgerCode = "", subSididaryLedger = "") {

    $.ajax({
        url: '/general-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: GeneralpageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupLedgerCode,
            controlLedgerCode: controlLedgerCode,
            subcontrolLedgerCode: subcontrolLedgerCode,
            subSididaryLedger: subSididaryLedger
        },
        success: function (response) {
            console.log("General Ledger Data :", response);
            let rows = '';
            response.data.forEach(function (item) {
                rows += `<tr>
                    <td class="text-center">
                        <input type="checkbox" class="form-check-input form-check-input-sm General-checkbox"
                            data-autoid="${item.autoId}" style="width:12px; height:12px;">
                    </td>
                    <td>
                            <button class="btn btn-transfer General-ledger"
                                data-autoid="${item.autoId}"
                            </button>
                              ${item.subSusidiaryLedgerCodeNo}
                    </td>
                    <td>${item.subSubsidiaryLedgerName || ''}</td>
                    <td>${item.cashFlowTypeName || ''}</td>
                 <td style="text-align:center; width:50px;">${item.isActive === 'Y' ? 'Yes' : 'No'}</td>

                 </tr>`;
            });
            $('#GeneraltblBody').html(rows);

            // Pagination Info
            let page = response.paginationInfo.currentPage;
            let totalPages = response.paginationInfo.totalPages;
            let totalRecords = response.totalCount;
            let startIndex = response.paginationInfo.startItem;
            let endIndex = response.paginationInfo.endItem;

            $('#GeneralpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
            $('#GeneralpaginationInfo').text(`Page ${page} of ${Math.max(1, totalPages)}`);
            $('#GeneraltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

            currenControltPage = page;
            GeneralgeneratePageButtons(currenControltPage, totalPages);
            GeneralupdateCheckboxState();
            GeneralupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);

        }
    });
}
//#endregion


//#region Save Or Update General Ledger
function GeneralLedgerSaveOrUpdate() {

    let autoID = $('#GeneralAutoID').val();

    let isActive = $("#IsActive").prop('checked') ? 'Y' : 'N'; 

    let GeneralLedgerData = {
        autoId: autoID || 0,
        SubsidiaryLedgerCodeNo: $("#GeneralSSLCode").val(),
        SubSusidiaryLedgerCodeNo: $("#GeneralLedgerCode").val(),
        SubSubsidiaryLedgerName: $("#GeneralLName").val(),
        ShortName: $("#GeneralShortName").val(),
        CashFlowTypeID: $("#CashFlowTypeID").val(),
        IsActive: isActive,
        GeneralgrlName: $("#GeneralgrlName").val(),
        GeneralCRLName: $("#GeneralCRLName").val(),
        GeneralSCRLName: $("#GeneralSCRLName").val(),
        GeneralSSLName: $("#GeneralSSLName").val(),
    };

    //validation
    if (!GeneralLedgerData.GeneralgrlName) {
        toastr.error("Please Select a Group Ledger.");
        let choicesWrapper = document.querySelector('#GeneralgrlName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }
  

    if (!GeneralLedgerData.GeneralCRLName) {
        toastr.error("Please Select a Control Ledger.");
        let choicesWrapper = document.querySelector('#GeneralCRLName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }
 

    if (!GeneralLedgerData.GeneralSCRLName) {
        toastr.error("Please Select a Sub-Control Ledger.");
        let choicesWrapper = document.querySelector('#GeneralSCRLName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }



    if (!GeneralLedgerData.GeneralSSLName) {
        toastr.error("Please Select a Sub-Sidiary Ledger .");
        let choicesWrapper = document.querySelector('#GeneralSSLName').closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.add('border-error');
        }
        return false;
    }

    if (!GeneralLedgerData.SubSusidiaryLedgerCodeNo) {
        GeneralSSLCode
        toastr.error("Please Enter General Ledger.");
        $('#GeneralLedgerCode').addClass('border-error');
        return false;
    }
    else {
        $('#GeneralLedgerCode').removeClass('border-error');

    }

    if (!GeneralLedgerData.SubSubsidiaryLedgerName) {
        GeneralSSLCode
        toastr.error("Please Enter General Ledger.");
        $('#GeneralLName').addClass('border-error');
        return false;
    }
    else {
        $('#GeneralLName').removeClass('border-error');

    }


    //Check for duplicate
    $.ajax({
        url: '/general-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(GeneralLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            //Decide if POST or PUT
            let url = '/general-ledger';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully.';
            if (autoID) {
                url = `/general-ledger/${autoID}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully.';
            }

            //Save or Update
            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(GeneralLedgerData),
                success: function (response) {
                    if (response.success) {
                        console.log("Generaly Ledger Save or UPdate Data : ", response);
                        toastr.success(successMessage);

                        //Custom Event For Update Control Ledger Dropdown in Control Ledger UI
                        //const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
                        //window.dispatchEvent(event);

                        GeneralLedgerFormClear();
                        selectedGeneralLedger = [];
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                        console.error('Error saving Sub-Sidiary Ledger :', response);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error Saving Sub-Sidiary Ledger :', error);
                }
            });
        },
        error: function (xhr, status, error) {
            toastr.error("Server error occurred during duplicate check!");
            console.error('Error checking duplicate:', error);
        }
    });
}
//#endregion


//#region Populate Data For Edit - General Ledger
$(document).on('click', '.General-ledger', function (e) {
    e.preventDefault();
    const id = parseInt($(this).data('autoid'));
    if (!id) return;

    isEditModeGeneral = true;

    selectedGeneralLedger = [id];

    $.ajax({
        url: `/genral-ledger/details/${id}`,
        type: 'GET',

        success: async function (data) {
            console.log("General Ledger Edit Details Data :", data);

            $('#GeneralAutoID').val(id);

            //Populate Group Ledger
            const groupCode = data.generalgrlcode || '';
            $('#Generalgrlcode').val(groupCode);
            choiceManager.setChoiceValue('GeneralgrlName', groupCode);
            $('#GeneralgrlshortName').val(data.generalgrlshortName || '');

            //Load Control Ledger 
            const controlCode = data.generalCRLCode || '';
            if (groupCode) {
                await loadControlLedgerDropdown(groupCode);
            }

            //Populate Control Ledger
            $('#GeneralCRLCode').val(controlCode);
            choiceManager.setChoiceValue('GeneralCRLName', controlCode);
            $('#GeneralCRLShortName').val(data.generalCRLShortName || '');

            //Load Sub-Control Ledger
            const subControlCode = data.generalSCRLCode || '';
            if (controlCode) {
                await loadSubControlLedgerDropdown(controlCode);
            }

            //Populate Sub-Control Ledger
            $('#GeneralSCRLCode').val(subControlCode);
            choiceManager.setChoiceValue('GeneralSCRLName', subControlCode);
            $('#GeneralSCRLShortname').val(data.generalSCRLShortname || '');

            //Load Sub-Subsidiary Ledger
            const subSididaryLedger = data.subsidiaryLedgerCodeNo || '';
            if (subControlCode) {
                await loadSubSidiaryLedgerDropdown(subControlCode);
            }

            $('#GeneralSSLCode').val(subSididaryLedger);
            choiceManager.setChoiceValue('GeneralSSLName', subSididaryLedger);
            $('#GeneralSSLShortName').val(data.generalSSLShortName || '');


            $('#GeneralLedgerCode').val(data.subSusidiaryLedgerCodeNo || '');
            $('#GeneralLName').val(data.subSubsidiaryLedgerName || '');
            $('#GeneralShortName').val(data.shortName || '');

            const cashFlowTypeID = data.cashFlowTypeID || ''; 
            choiceManager.setChoiceValue('CashFlowTypeID', cashFlowTypeID);

            if (data.isActive === 'Y') {
                $('#IsActive').prop('checked', true);
            } else {
                $('#IsActive').prop('checked', false);
            }


            // Display Dates
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#GeneralldisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#GeneralldisplayLDate').text('');
            }

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: 'numeric',
                    minute: '2-digit',
                    hour12: true
                }).replace(',', '');
                $('#GeneraldisplayModifyDate').text(updateDate.toUpperCase());
            } else {
                $('#GeneraldisplayModifyDate').text('');
            }

            $('.General-checkbox').prop('checked', false);
            $(`.General-checkbox[data-autoid="${id}"]`).prop('checked', true);
            $("#GeneralselectAll").prop('checked', false);
            GeneralupdateCheckboxState();
        },
        error: function (xhr, status, error) {
            toastr.error("Failed To Fetch General Ledger Details.");
            console.error("Error Fetching Details:", error);
        }
    });
});
//#endregion


//#region Single & More Delete General Ledger

function GeneralLedgerDelete() {
    const ids = selectedGeneralLedger.map(x => parseInt(x)).filter(x => !isNaN(x));
    console.log("IDs to delete:", ids);

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    // Single URL for both single & multiple delete
    const url = '/general-ledger-all-delete';

    $.ajax({
        url: url,
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            //const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
            //window.dispatchEvent(event);

            // Clear form, reload table, reset selection
            GeneralLedgerFormClear();
            GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
            selectedGeneralLedger = [];
            $("#GeneralselectAll").prop("checked", false);
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}

//#endregion


//#region Sort & Pagination Helpers 
function GeneralupdateSortIndicators() {
    $('.Generalsortable').removeClass('sort-asc sort-desc');
    $('.Generalsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Generalsortable[data-column="${GeneralcurrentSortColumn}"]`);
    header.addClass(GeneralcurrentSortColumn === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(GeneralcurrentSortColumn === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function GeneralgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#GeneralpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button>')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => GeneralLedgerAll(page, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, $('#GeneralsearchInput').val().trim()));

        if (page === currentPage) {
            btn.removeClass('btn-outline-primary').addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    // Always show first page
    navigationDiv.append(createButton(1));

    if (currentPage > 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    // Show current page if it's not first or last
    if (currentPage > 1 && currentPage < totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    // Always show last page if more than one
    if (totalPages > 1) {
        navigationDiv.append(createButton(totalPages));
    }
}

function GeneralupdateCheckboxState() {
    $('.General-checkbox').each(function () {
        const ID = $(this).data('autoid');
        $(this).prop('checked', selectedGeneralLedger.includes(ID));
    });
    const allChecked = $('.General-checkbox:visible').length > 0 &&
        $('.General-checkbox:visible:not(:checked)').length === 0;
    $('#GeneralselectAll').prop('checked', allChecked);
}

function Generaldebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function GeneralbindCheckboxHandlers() {
    $(document).off('click', '.General-checkbox');
    $(document).on('click', '.General-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedGeneralLedger.includes(id)) {
                selectedGeneralLedger.push(id);
            }
        } else {
            const index = selectedGeneralLedger.indexOf(id);
            if (index !== -1) {
                selectedGeneralLedger.splice(index, 1);
            }
        }

        const allChecked = $('.General-checkbox:visible').length > 0 &&
            $('.General-checkbox:visible:not(:checked)').length === 0;
        $('#GeneralselectAll').prop('checked', allChecked);

        console.log("Selected General Ledger:", selectedGeneralLedger);
    });
}
//#endregion


//#region Select all checkbox

$(document).on('click', '#GeneralselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.General-checkbox:visible').prop('checked', isChecked);

    if (isChecked) {
        $('.General-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            if (!selectedGeneralLedger.includes(id)) {
                selectedGeneralLedger.push(id);
            }
        });
    } else {
        $('.General-checkbox:visible').each(function () {
            const id = parseFloat($(this).data('autoid'));
            const index = selectedGeneralLedger.indexOf(id);
            if (index !== -1) {
                selectedGeneralLedger.splice(index, 1);
            }
        });
    }
    console.log("Selected General Ledger:", selectedGeneralLedger);
});
//#endregion


//#region General Ledger Ledger Clear
function GeneralLedgerFormClear() {
    isEditModeGeneral = false;

    $("#GeneralAutoID").val("");

    $('#GeneralgrlName').off('change');
    $('#GeneralCRLName').off('change');
    $('#GeneralSCRLName').off('change');
    $('#GeneralSSLName').off('change');

    //choiceManager.resetAllChoices();
    choiceManager.clearChoice('GeneralgrlName');

    choiceManager.resetChoice('GeneralCRLName');
    choiceManager.resetChoice('GeneralSCRLName');
    choiceManager.resetChoice('GeneralSSLName');

    loadGeneralLedgerData();
    CashFlowDropdown();

    $("#Generalgrlcode").val("");
    $("#GeneralgrlshortName").val("");

    $("#GeneralCRLCode").val("");
    $("#GeneralCRLShortName").val("");

    $("#GeneralSCRLCode").val("");
    $("#GeneralSCRLShortname").val("");

    $("#GeneralSSLCode").val("");
    $("#GeneralSSLShortName").val("");

    $("#GeneralLName").val(""); 
    $("#GeneralLedgerCode").val("");
    $("#GeneralShortName").val("");

    $("#IsActive").prop('checked', false); 


    $(".form-control").removeClass("is-invalid is-valid");
    $(".invalid-feedback, .valid-feedback").remove();
    $(".form-control").removeClass("border-error");

    $('#GeneraldisplayModifyDate').text('');
    $('#GeneralldisplayLDate').text('');

    //$(".Subsidiary-checkbox").prop("checked", false);
    $("#GeneralselectAll").prop("checked", false);
    $('#GeneralsearchInput').val('');

    // Set Entry Date
    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    document.querySelectorAll('.choiceDD').forEach(function (dropdown) {
        //choiceManager.clearChoice(dropdown.id);
        let choicesWrapper = dropdown.closest('.choices');
        if (choicesWrapper) {
            choicesWrapper.classList.remove('border-error');
        }
    });

    selectedGeneralLedger = [];
    currenGeneraltPage = 1;
    //const emptySearch = '';
    //const emptySubControlCode = '';
    GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", "");
}
//#endregion

