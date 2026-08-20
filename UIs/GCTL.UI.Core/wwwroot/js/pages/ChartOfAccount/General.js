let selectedGeneralLedger = [];
let currenGeneraltPage = 1;
let GeneralpageSize = 10;
let GeneralcurrentSortColumn = 'SubSusidiaryLedgerCodeNo';
let GeneralcurrentSortDirection = 'desc';
let isEditModeGeneral = false;

// Selected dropdown values (for pagination reload)
let selectedGroupCodeforGL = "";
let selectedControlCodeforGL = "";
let selectedSubControlCodeforGL = "";
let selectedSubsidiaryCodeforGL = "";

// Helper: Bangladesh Local Date & Time Formatter
function getBangladeshDateTime() {
    return new Date().toLocaleString('en-GB', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
    }).replace(',', '').toUpperCase();
}

//#region Error Border Cleanup (Native / Custom Dropdown)
document.addEventListener('change', function (e) {
    if (e.target && $(e.target).hasClass('form-select')) {
        $(e.target).removeClass('border-error');
        $(e.target).siblings('.custom-dd-wrapper').removeClass('border-error');
    }
});
//#endregion

//#region Custom Event Listener For Group Ledger Dropdown Updated
window.addEventListener('groupLedgerUpdated', function (e) {
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

//#region General Ledger UI: Cascading Dropdowns and Data Loading

// 1. Group Ledger Dropdown
async function loadGeneralLedgerData(selectedId = null) {
    try {
        const groupResponse = await $.ajax({
            url: "/generalUi-grl-dropdown",
            type: "GET"
        });

        let $dropdown = $('#GeneralgrlName');
        $dropdown.find('option:not(:first)').remove();

        if (groupResponse && groupResponse.data) {
            $.each(groupResponse.data, function (index, item) {
                $dropdown.append($('<option></option>').val(item.id).text(item.name));
            });
        }

        if (selectedId) {
            $dropdown.val(selectedId);
        }

        window.reinitCustomDropdownById('GeneralgrlName');

        // Bind Change Event
        $dropdown.off('change').on('change', async function () {
            const selectedGroupCode = $(this).val();
            selectedGroupCodeforGL = selectedGroupCode || "";
            $(this).removeClass('border-error');

            if (!selectedGroupCode) {
                resetGroupLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", "");
                return;
            }

            // Load Control Ledger Dropdown
            await loadControlLedgerDropdown(selectedGroupCode);

            // Fetch Group Ledger Details
            try {
                const groupInfo = await $.ajax({
                    url: '/generalUi-grl-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode }
                });

                $('#Generalgrlcode').val(groupInfo.controlLedgerCodeNo || '');
                $('#GeneralgrlshortName').val(groupInfo.shortName || '');
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, "", "", "");
            } catch (err) {
                console.error("Error fetching group info:", err);
            }
        });

    } catch (error) {
        console.error("Error loading general ledger data:", error);
    }
}

// 2. Control Ledger Dropdown
async function loadControlLedgerDropdown(groupCode, selectedId = null) {
    try {
        const controlResponse = await $.ajax({
            url: '/generalUi-crl-dropdown',
            type: 'GET',
            data: { groupcode: groupCode }
        });

        let $dropdown = $("#GeneralCRLName");
        $dropdown.find('option:not(:first)').remove();

        if (controlResponse && controlResponse.data) {
            $.each(controlResponse.data, function (index, item) {
                $dropdown.append($('<option></option>').val(item.id).text(item.name));
            });
        }

        if (selectedId) {
            $dropdown.val(selectedId);
        }

        window.reinitCustomDropdownById('GeneralCRLName');

        $dropdown.off('change').on('change', async function () {
            const selectedControlCode = $(this).val();
            selectedControlCodeforGL = selectedControlCode || "";
            $(this).removeClass('border-error');

            if (!selectedControlCode) {
                resetControlLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", groupCode, "", "", "");
                return;
            }

            // Load Sub-Control Dropdown
            await loadSubControlLedgerDropdown(selectedControlCode);

            // Fetch Control Details
            try {
                const controlInfo = await $.ajax({
                    url: '/generalUi-crl-info',
                    type: 'GET',
                    data: { contrlcode: selectedControlCode }
                });

                $('#GeneralCRLCode').val(controlInfo.subControlLedgerCodeNo || '');
                $('#GeneralCRLShortName').val(controlInfo.shortName || '');
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", groupCode, selectedControlCodeforGL, "", "");
            } catch (err) {
                console.error("Error loading control info:", err);
            }
        });

    } catch (error) {
        console.error("Error loading control ledger:", error);
    }
}

// 3. Sub-Control Ledger Dropdown
async function loadSubControlLedgerDropdown(controlCode, selectedId = null) {
    try {
        const subControlResponse = await $.ajax({
            url: '/generalUi-subcrl-dropdown',
            type: 'GET',
            data: { controlcode: controlCode }
        });

        let $dropdown = $("#GeneralSCRLName");
        $dropdown.find('option:not(:first)').remove();

        if (subControlResponse && subControlResponse.data) {
            $.each(subControlResponse.data, function (index, item) {
                $dropdown.append($('<option></option>').val(item.id).text(item.name));
            });
        }

        if (selectedId) {
            $dropdown.val(selectedId);
        }

        window.reinitCustomDropdownById('GeneralSCRLName');

        $dropdown.off('change').on('change', async function () {
            const selectedSubControlCode = $(this).val();
            selectedSubControlCodeforGL = selectedSubControlCode || "";
            $(this).removeClass('border-error');

            if (!selectedSubControlCode) {
                resetSubControlLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", controlCode, "", "");
                return;
            }

            // Load Sub-Sidiary Dropdown
            await loadSubSidiaryLedgerDropdown(selectedSubControlCode);

            // Fetch Sub-Control Details
            try {
                const subControlInfo = await $.ajax({
                    url: '/generalUi-subcrl-info',
                    type: 'GET',
                    data: { subcontrlcode: selectedSubControlCode }
                });

                $('#GeneralSCRLCode').val(subControlInfo.generalLedgerCodeNo || '');
                $('#GeneralSCRLShortname').val(subControlInfo.shortName || '');
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", controlCode, selectedSubControlCodeforGL, "");
            } catch (err) {
                console.error("Error loading sub-control info:", err);
            }
        });

    } catch (error) {
        console.error("Error loading sub-control ledger:", error);
    }
}

// 4. Sub-Sidiary Ledger Dropdown
async function loadSubSidiaryLedgerDropdown(subControlCode, selectedId = null) {
    try {
        const sslResponse = await $.ajax({
            url: '/generalUi-ssl-dropdown',
            type: 'GET',
            data: { subcontrolcode: subControlCode }
        });

        let $dropdown = $("#GeneralSSLName");
        $dropdown.find('option:not(:first)').remove();

        if (sslResponse && sslResponse.data) {
            $.each(sslResponse.data, function (index, item) {
                $dropdown.append($('<option></option>').val(item.id).text(item.name));
            });
        }

        if (selectedId) {
            $dropdown.val(selectedId);
        }

        window.reinitCustomDropdownById('GeneralSSLName');

        $dropdown.off('change').on('change', async function () {
            const selectedSSLCode = $(this).val();
            selectedSubsidiaryCodeforGL = selectedSSLCode || "";
            $(this).removeClass('border-error');

            if (!selectedSSLCode) {
                resetSubSidiaryLedgerFields();
                GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", "");
                return;
            }

            // Fetch Sub-Sidiary Info
            try {
                const sslInfo = await $.ajax({
                    url: '/generalUi-ssl-info',
                    type: 'GET',
                    data: { subsidiarycode: selectedSSLCode }
                });

                $('#GeneralSSLCode').val(sslInfo.susidiaryLedgerCodeNo || '');
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
            } catch (err) {
                console.error("Error loading subsidiary info:", err);
            }
        });

    } catch (error) {
        console.error("Error loading subsidiary ledger:", error);
    }
}

// 5. Cash Flow Dropdown
function CashFlowDropdown(selectedId = null) {
    $.ajax({
        url: "/cash-flow-dropdown",
        type: "GET",
        success: function (cashdata) {
            let $dropdown = $('#CashFlowTypeID');
            $dropdown.find('option:not(:first)').remove();

            if (cashdata && cashdata.data) {
                $.each(cashdata.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('CashFlowTypeID');
        },
        error: function () {
            console.log("Failed Cash Flow Dropdown");
        }
    });
}

//#endregion

//#region Reset Cascading Helpers

function resetGroupLedgerFields() {
    $('#Generalgrlcode').val('');
    $('#GeneralgrlshortName').val('');

    window.clearCustomDropdownById('GeneralCRLName');
    window.clearCustomDropdownById('GeneralSCRLName');
    window.clearCustomDropdownById('GeneralSSLName');

    $('#GeneralCRLCode, #GeneralCRLShortName').val('');
    $('#GeneralSCRLCode, #GeneralSCRLShortname').val('');
    $('#GeneralSSLCode, #GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');
}

function resetControlLedgerFields() {
    $('#GeneralCRLCode, #GeneralCRLShortName').val('');
    window.clearCustomDropdownById('GeneralSCRLName');
    window.clearCustomDropdownById('GeneralSSLName');

    $('#GeneralSCRLCode, #GeneralSCRLShortname').val('');
    $('#GeneralSSLCode, #GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');
}

function resetSubControlLedgerFields() {
    $('#GeneralSCRLCode, #GeneralSCRLShortname').val('');
    window.clearCustomDropdownById('GeneralSSLName');

    $('#GeneralSSLCode, #GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');
}

function resetSubSidiaryLedgerFields() {
    $('#GeneralSSLCode, #GeneralSSLShortName').val('');
    $('#GeneralLedgerCode').val('');
}

//#endregion

//#region Save Or Update General Ledger

function GeneralLedgerSaveOrUpdate() {
    let autoID = $('#GeneralAutoID').val();
    let isActive = $("#IsActive").prop('checked') ? 'Y' : 'N';

    let GeneralLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        SubsidiaryLedgerCodeNo: $("#GeneralSSLCode").val(),
        SubSusidiaryLedgerCodeNo: $("#GeneralLedgerCode").val(),
        SubSubsidiaryLedgerName: $("#GeneralLName").val(),
        ShortName: $("#GeneralShortName").val(),
        CashFlowTypeID: $("#CashFlowTypeID").val(),
        IsActive: isActive,
        GeneralgrlName: $("#GeneralgrlName").val(),
        GeneralCRLName: $("#GeneralCRLName").val(),
        GeneralSCRLName: $("#GeneralSCRLName").val(),
        GeneralSSLName: $("#GeneralSSLName").val()
    };

    // Validations
    if (!GeneralLedgerData.GeneralgrlName) {
        toastr.error("Please Select a Group Ledger.");
        $('#GeneralgrlName').addClass('border-error');
        $('#GeneralgrlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralCRLName) {
        toastr.error("Please Select a Control Ledger.");
        $('#GeneralCRLName').addClass('border-error');
        $('#GeneralCRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralSCRLName) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#GeneralSCRLName').addClass('border-error');
        $('#GeneralSCRLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.GeneralSSLName) {
        toastr.error("Please Select a Sub-Sidiary Ledger.");
        $('#GeneralSSLName').addClass('border-error');
        $('#GeneralSSLName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!GeneralLedgerData.SubSusidiaryLedgerCodeNo) {
        toastr.error("Please Enter General Ledger Code.");
        $('#GeneralLedgerCode').addClass('border-error');
        return false;
    } else {
        $('#GeneralLedgerCode').removeClass('border-error');
    }

    if (!GeneralLedgerData.SubSubsidiaryLedgerName) {
        toastr.error("Please Enter General Ledger Name.");
        $('#GeneralLName').addClass('border-error');
        return false;
    } else {
        $('#GeneralLName').removeClass('border-error');
    }

    // Check duplicate
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

            let url = autoID ? `/general-ledger/${autoID}` : '/general-ledger';
            let method = autoID ? 'PUT' : 'POST';
            let successMessage = autoID ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(GeneralLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);
                        GeneralLedgerFormClear();
                        selectedGeneralLedger = [];
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("Error Saving General Ledger.");
                    console.error('Error Saving General Ledger :', error);
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

//#region Populate Data For Edit

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
            $('#GeneralAutoID').val(id);

            // Group Ledger
            const groupCode = data.generalgrlcode || '';
            $('#Generalgrlcode').val(groupCode);
            $('#GeneralgrlshortName').val(data.generalgrlshortName || '');
            $('#GeneralgrlName').val(groupCode);
            window.reinitCustomDropdownById('GeneralgrlName');

            // Control Ledger
            const controlCode = data.generalCRLCode || '';
            if (groupCode) {
                await loadControlLedgerDropdown(groupCode, controlCode);
            }
            $('#GeneralCRLCode').val(controlCode);
            $('#GeneralCRLShortName').val(data.generalCRLShortName || '');

            // Sub-Control Ledger
            const subControlCode = data.generalSCRLCode || '';
            if (controlCode) {
                await loadSubControlLedgerDropdown(controlCode, subControlCode);
            }
            $('#GeneralSCRLCode').val(subControlCode);
            $('#GeneralSCRLShortname').val(data.generalSCRLShortname || '');

            // Sub-Sidiary Ledger
            const subSididaryLedger = data.subsidiaryLedgerCodeNo || '';
            if (subControlCode) {
                await loadSubSidiaryLedgerDropdown(subControlCode, subSididaryLedger);
            }
            $('#GeneralSSLCode').val(subSididaryLedger);
            $('#GeneralSSLShortName').val(data.generalSSLShortName || '');

            // Input Fields
            $('#GeneralLedgerCode').val(data.subSusidiaryLedgerCodeNo || '');
            $('#GeneralLName').val(data.subSubsidiaryLedgerName || '');
            $('#GeneralShortName').val(data.shortName || '');

            // Cash Flow
            const cashFlowTypeID = data.cashFlowTypeID || '';
            $('#CashFlowTypeID').val(cashFlowTypeID);
            window.reinitCustomDropdownById('CashFlowTypeID');

            $('#IsActive').prop('checked', data.isActive === 'Y');

            // Dates
            if (data.lDate) {
                let entryDate = new Date(data.lDate).toLocaleString('en-GB', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: 'numeric', minute: '2-digit', hour12: true
                }).replace(',', '');
                $('#GeneralldisplayLDate').text(entryDate.toUpperCase());
            } else {
                $('#GeneralldisplayLDate').text('');
            }

            if (data.modifyDate) {
                let updateDate = new Date(data.modifyDate).toLocaleString('en-GB', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: 'numeric', minute: '2-digit', hour12: true
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

//#region Clear Form

function GeneralLedgerFormClear() {
    isEditModeGeneral = false;

    $("#GeneralAutoID").val("");
    $("#Generalgrlcode, #GeneralgrlshortName").val("");
    $("#GeneralCRLCode, #GeneralCRLShortName").val("");
    $("#GeneralSCRLCode, #GeneralSCRLShortname").val("");
    $("#GeneralSSLCode, #GeneralSSLShortName").val("");
    $("#GeneralLName, #GeneralLedgerCode, #GeneralShortName").val("");
    $("#IsActive").prop('checked', false);

    // Clear all custom dropdowns
    window.clearAllCustomDropdowns('#generalLedgerForm');

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#GeneraldisplayModifyDate').text('');
    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    $(".General-checkbox").prop("checked", false);
    $("#GeneralselectAll").prop("checked", false);
    $('#GeneralsearchInput').val('');

    selectedGeneralLedger = [];
    currenGeneraltPage = 1;

    loadGeneralLedgerData();
    CashFlowDropdown();
    GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", "", "", "", "");
}

//#endregion

//#region Load All General Ledger Data

function GeneralLedgerAll(page = 1, pageSize = GeneralpageSize, sortColumn = GeneralcurrentSortColumn, sortDirection = GeneralcurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "", subcontrolLedgerCode = "", subSididaryLedger = "") {
    $.ajax({
        url: '/general-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupLedgerCode,
            controlLedgerCode: controlLedgerCode,
            subcontrolLedgerCode: subcontrolLedgerCode,
            subSididaryLedger: subSididaryLedger
        },
        success: function (response) {
            let rows = '';
            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center">
                            <input type="checkbox" class="form-check-input form-check-input-sm General-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-link p-0 text-decoration-none General-ledger" data-autoid="${item.autoId}">
                                ${item.subSusidiaryLedgerCodeNo}
                            </button>
                        </td>
                        <td>${item.subSubsidiaryLedgerName || ''}</td>
                        <td>${item.cashFlowTypeName || ''}</td>
                        <td style="text-align:center; width:50px;">${item.isActive === 'Y' ? 'Yes' : 'No'}</td>
                    </tr>`;
                });
            } else {
                rows = `<tr><td colspan="5" class="text-center text-muted">No records found</td></tr>`;
            }
            $('#GeneraltblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#GeneralpaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#GeneralpaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#GeneraltotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                currenGeneraltPage = currPage;
                GeneralgeneratePageButtons(currenGeneraltPage, totalPages);
            }

            GeneralupdateCheckboxState();
            GeneralupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);
        }
    });
}

//#endregion

//#region Delete General Ledger

function GeneralLedgerDelete() {
    const ids = selectedGeneralLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/general-ledger-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");
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

//#region Pagination & Checkbox Helpers

function GeneralupdateSortIndicators() {
    $('.Generalsortable').removeClass('sort-asc sort-desc');
    $('.Generalsortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.Generalsortable[data-column="${GeneralcurrentSortColumn}"]`);
    header.addClass(GeneralcurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(GeneralcurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function GeneralgeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#GeneralpageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => GeneralLedgerAll(page, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, $('#GeneralsearchInput').val().trim()));

        if (page === currentPage) {
            btn.addClass('btn-primary');
        } else {
            btn.addClass('btn-outline-primary');
        }
        return btn;
    }

    if (totalPages <= 1) return;

    navigationDiv.append(createButton(1));

    if (currentPage > 2) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    if (currentPage > 1 && currentPage < totalPages) {
        navigationDiv.append(createButton(currentPage));
    }

    if (currentPage < totalPages - 1) {
        navigationDiv.append($('<span>').text('...').css('padding', '0 5px'));
    }

    navigationDiv.append(createButton(totalPages));
}

function GeneralupdateCheckboxState() {
    $('.General-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
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
    $(document).off('click', '.General-checkbox').on('click', '.General-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedGeneralLedger.includes(id)) selectedGeneralLedger.push(id);
        } else {
            const index = selectedGeneralLedger.indexOf(id);
            if (index !== -1) selectedGeneralLedger.splice(index, 1);
        }

        const allChecked = $('.General-checkbox:visible').length > 0 &&
            $('.General-checkbox:visible:not(:checked)').length === 0;
        $('#GeneralselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#GeneralselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.General-checkbox:visible').prop('checked', isChecked);

    $('.General-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedGeneralLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedGeneralLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedGeneralLedger.splice(index, 1);
        }
    });
});

//#endregion

//#region Document Ready

$(document).ready(function () {
    loadGeneralLedgerData();
    CashFlowDropdown();
    GeneralLedgerAll();
    GeneralbindCheckboxHandlers();

    $('#GeneralldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#generalLedgerForm", function (e) {
        e.preventDefault();
        GeneralLedgerSaveOrUpdate();
    });

    $(document).on("click", "#GeneraldeleteBtn, .js-GeneralLedger-delete-confirm", function (e) {
        e.preventDefault();
        GeneralLedgerDelete();
    });

    $(document).on("click", ".GeneralresetBtn, .js-GeneralLedger-clear", function () {
        GeneralLedgerFormClear();
    });

    $(document).on('input', '.GeneralsearchInput', Generaldebounce(function () {
        const searchValue = $('.GeneralsearchInput').val().trim();
        GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, searchValue);
    }, 500));

    $(document).on('click', '#GeneralfirstPage', function () {
        currenGeneraltPage = 1;
        GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);
    });

    $(document).on('click', '#GeneralnextPage', function () {
        currenGeneraltPage++;
        GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);
    });

    $(document).on('click', '#GeneralprevPage', function () {
        if (currenGeneraltPage > 1) {
            currenGeneraltPage--;
            GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);
        }
    });

    $(document).on('click', '#GenerallastPage', function () {
        let lastPage = parseInt($('#GeneralpageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            currenGeneraltPage = lastPage;
            GeneralLedgerAll(currenGeneraltPage, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, "", selectedGroupCodeforGL, selectedControlCodeforGL, selectedSubControlCodeforGL, selectedSubsidiaryCodeforGL);
        }
    });

    $(document).on('change', '.GeneralpageSize', function () {
        GeneralpageSize = parseInt($(this).val());
        GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection);
    });

    $(document).on('click', '.Generalsortable', function () {
        const column = $(this).data('column');
        if (column === GeneralcurrentSortColumn) {
            GeneralcurrentSortDirection = (GeneralcurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            GeneralcurrentSortColumn = column;
            GeneralcurrentSortDirection = 'desc';
        }
        GeneralLedgerAll(1, GeneralpageSize, GeneralcurrentSortColumn, GeneralcurrentSortDirection, $('#GeneralsearchInput').val().trim());
    });
});

//#endregion