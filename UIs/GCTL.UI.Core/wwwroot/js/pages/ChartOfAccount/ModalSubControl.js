let selectedModalSubSidiaryLedger = [];
let ModalcurrenSubSidiarytPage = 1;
let ModalSubSidiarypageSize = 10;
let ModalSubSidiarycurrentSortColumn = 'SusidiaryLedgerCodeNo';
let ModalSubSidiarycurrentSortDirection = 'desc';
let ModalisEditModeSubSidiary = false;

// Selected dropdown values (used for reload, pagination, sorting)
let ModalselectedGroupCodeSSL = "";
let ModalselectedControlCodeSSL = "";
let ModalselectedSubControlCodeSSL = "";

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
    ModalSubSidiaryForGeneralUI();
    $("#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName").val("");
    $("#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName").val("");
    $("#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName").val("");
    $("#ModalSusidiaryLedgerCodeNo").val("");
    ModalSubSididiarLedgerAll();
});
//#endregion

//#region Load Cascading Dropdowns

// 1. Group Ledger Dropdown
function ModalSubSidiaryForGeneralUI(selectedId = null) {
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            let $dropdown = $('#ModalforsidiarygroupName');
            $dropdown.find('option:not(:first)').remove();

            if (groupResponse && groupResponse.data) {
                $.each(groupResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalforsidiarygroupName');

            $dropdown.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                ModalselectedGroupCodeSSL = selectedGroupCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName').val('');

                    // Control & Sub-Control Clear
                    window.clearCustomDropdownById('ModalforsidiaryControlName');
                    $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');

                    window.clearCustomDropdownById('ModalforsidiaryGeneralLedgerName');
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName, #ModalSusidiaryLedgerCodeNo').val('');

                    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", "", "", "");
                    return;
                }

                // Load Control Ledger Dropdown
                ModalgetControlLedger(selectedGroupCode);

                // Fetch Group Info
                $.ajax({
                    url: '/forsubsidiary-group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#Modalforsidiarygroupcode').val(data.controlLedgerCodeNo || '');
                        $('#ModalforsidiarygroupshortName').val(data.shortName || '');
                        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, "", "");
                    }
                });
            });
        },
        error: function () {
            console.error("Failed to fetch Group Ledger dropdown");
        }
    });
}

// 2. Control Ledger Dropdown
function ModalgetControlLedger(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-dropdown',
        type: 'GET',
        data: { groupcode: selectedGroupCode },
        success: function (controlResponse) {
            let $dropdown = $('#ModalforsidiaryControlName');
            $dropdown.find('option:not(:first)').remove();

            if (controlResponse && controlResponse.data) {
                $.each(controlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalforsidiaryControlName');

            $dropdown.off('change').on('change', function () {
                const selectedControlCode = $(this).val();
                ModalselectedControlCodeSSL = selectedControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName').val('');

                    window.clearCustomDropdownById('ModalforsidiaryGeneralLedgerName');
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName, #ModalSusidiaryLedgerCodeNo').val('');

                    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, "", "");
                    return;
                }

                // Load Sub-Control Dropdown
                ModalgetSubControllerLedger(selectedControlCode);

                // Fetch Control Info
                ModalGetControlLedgerInfo(selectedControlCode);
            });
        },
        error: function () {
            console.error("Failed to fetch Control Ledger dropdown");
        }
    });
}

function ModalGetControlLedgerInfo(selectedControlCode) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-info',
        type: 'GET',
        data: { contrlcode: selectedControlCode },
        success: function (controlInfo) {
            $('#ModalforsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo || '');
            $('#ModalforsidiaryCLShortName').val(controlInfo.shortName || '');
            ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, "");
        }
    });
}

// 3. Sub-Control Ledger Dropdown
function ModalgetSubControllerLedger(selectedControlCode, selectedId = null) {
    $.ajax({
        url: '/subcontrol-ledger-dropdown',
        type: 'GET',
        data: { controlcode: selectedControlCode },
        success: function (subControlResponse) {
            let $dropdown = $('#ModalforsidiaryGeneralLedgerName');
            $dropdown.find('option:not(:first)').remove();

            if (subControlResponse && subControlResponse.data) {
                $.each(subControlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('ModalforsidiaryGeneralLedgerName');

            $dropdown.off('change').on('change', function () {
                const selectedSubControlCode = $(this).val();
                ModalselectedSubControlCodeSSL = selectedSubControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSubControlCode) {
                    $('#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName, #ModalSusidiaryLedgerCodeNo').val('');
                    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, "");
                    return;
                }

                // Fetch Sub-Control Info
                ModalGetSubcontrolInfo(selectedSubControlCode);
            });
        },
        error: function () {
            console.error("Failed to fetch Sub-Control Ledger dropdown");
        }
    });
}

function ModalGetSubcontrolInfo(selectedSubControlCode) {
    $.ajax({
        url: '/forsudsidiary-subcontrol-ledger-info',
        type: 'GET',
        data: { subcontrlcode: selectedSubControlCode },
        success: function (subControlInfo) {
            $('#ModalforsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo || '');
            $('#ModalforsidiarySubCRLshortName').val(subControlInfo.shortName || '');

            if (!ModalisEditModeSubSidiary) {
                $.ajax({
                    url: '/next-subsidiary-ledger-code',
                    type: 'GET',
                    data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                    success: function (nextCode) {
                        $('#ModalSusidiaryLedgerCodeNo').val(nextCode);
                    }
                });
            }

            ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
        }
    });
}

//#endregion

//#region Load All Sub-Sidiary Ledger Data
function ModalSubSididiarLedgerAll(page = 1, pageSize = ModalSubSidiarypageSize, sortColumn = ModalSubSidiarycurrentSortColumn, sortDirection = ModalSubSidiarycurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "", subcontrolLedgerCode = "") {
    $.ajax({
        url: '/subsidiary-ledger-list',
        type: 'GET',
        data: {
            pageNumber: page,
            pageSize: pageSize,
            searchTerm: searchTerm,
            sortColumn: sortColumn,
            sortOrder: sortDirection,
            groupLedgerCode: groupLedgerCode,
            controlLedgerCode: controlLedgerCode,
            subcontrolLedgerCode: subcontrolLedgerCode
        },
        success: function (response) {
            let rows = '';
            if (response.data && response.data.length > 0) {
                response.data.forEach(function (item) {
                    rows += `<tr>
                        <td class="text-center">
                            <input type="checkbox" class="form-check-input form-check-input-sm ModalSubsidiary-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-link p-0 text-decoration-none Modalsubsidiary-ledger" data-autoid="${item.autoId}">
                                ${item.susidiaryLedgerCodeNo}
                            </button>
                        </td>
                        <td>${item.subsidiaryLedgerName || ''}</td>
                        <td>${item.shortName || ''}</td>
                    </tr>`;
                });
            } else {
                rows = `<tr><td colspan="4" class="text-center text-muted">No records found</td></tr>`;
            }
            $('#ModalSubSidiarytblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#ModalSubSidiarypaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#ModalSubSidiarypaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#ModalSubSidiarytotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                ModalcurrenSubSidiarytPage = currPage;
                ModalSubSidiarygeneratePageButtons(ModalcurrenSubSidiarytPage, totalPages);
            }

            ModalSubSidiaryupdateCheckboxState();
            ModalSubSidiaryupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);
        }
    });
}
//#endregion

//#region Save Or Update Sub-Sidiary Ledger
function ModalSubSidiaryLedgerSaveOrUpdate() {
    let autoID = $('#ModalforsidiaryAutoID').val();

    let SubSidiaryLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        GeneralLedgerCodeNo: $("#ModalforsidiaryGeneralLedgerCodeNo").val(),
        SusidiaryLedgerCodeNo: $("#ModalSusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: $("#ModalSubsidiaryLedgerName").val(),
        ShortName: $("#ModalforsidiaryshortName").val(),
        CRLCode: $("#ModalforsidiaryControlName").val(),
        GLCode: $("#ModalforsidiarygroupName").val()
    };

    // Validations
    if (!SubSidiaryLedgerData.GLCode) {
        toastr.error("Please Select a Group Ledger.");
        $('#ModalforsidiarygroupName').addClass('border-error');
        $('#ModalforsidiarygroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.CRLCode) {
        toastr.error("Please Select a Control Ledger.");
        $('#ModalforsidiaryControlName').addClass('border-error');
        $('#ModalforsidiaryControlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.GeneralLedgerCodeNo) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#ModalforsidiaryGeneralLedgerName').addClass('border-error');
        $('#ModalforsidiaryGeneralLedgerName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger Name.");
        $('#ModalSubsidiaryLedgerName').addClass('border-error');
        return false;
    } else {
        $('#ModalSubsidiaryLedgerName').removeClass('border-error');
    }

    // Check duplicate
    $.ajax({
        url: '/subsidiary-ledger/check-duplicate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(SubSidiaryLedgerData),
        success: function (dupResponse) {
            if (dupResponse.isDuplicate) {
                toastr.error(dupResponse.message);
                return;
            }

            let url = autoID ? `/subsidiary-ledger/${autoID}` : '/subsidiary-ledger';
            let method = autoID ? 'PUT' : 'POST';
            let successMessage = autoID ? 'Data Updated Successfully.' : 'Data Saved Successfully.';

            $.ajax({
                url: url,
                type: method,
                contentType: 'application/json',
                data: JSON.stringify(SubSidiaryLedgerData),
                success: function (response) {
                    if (response.success) {
                        toastr.success(successMessage);

                        const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
                        window.dispatchEvent(event);

                        ModalSubSidiaryLedgerFormClear();
                        selectedModalSubSidiaryLedger = [];

                        // Load parent Sub-Sidiary Table if exists
                        if (typeof SubSididiarLedgerAll === "function") {
                            SubSididiarLedgerAll();
                        }
                    } else {
                        toastr.error(response.message || "Operation Failed!");
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("Error Saving Sub-Sidiary Ledger.");
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

//#region Populate Data For Edit
$(document).on('click', '.Modalsubsidiary-ledger', function (e) {
    e.preventDefault();
    const autoId = parseInt($(this).data('autoid'));
    if (!autoId) return;

    ModalisEditModeSubSidiary = true;
    selectedModalSubSidiaryLedger = [autoId];

    $.ajax({
        url: `/subsidiary-ledger/details/${autoId}`,
        type: 'GET',
        success: function (response) {
            if (response) {
                $('#ModalforsidiaryAutoID').val(response.autoId);
                $('#ModalSusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                $('#ModalSubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                $('#ModalforsidiaryshortName').val(response.shortName || '');

                // Group Ledger
                $('#Modalforsidiarygroupcode').val(response.glCode || '');
                $('#ModalforsidiarygroupshortName').val(response.glShortName || '');
                $('#ModalforsidiarygroupName').val(response.glCode);
                window.reinitCustomDropdownById('ModalforsidiarygroupName');

                // Control Ledger
                if (response.glCode) {
                    ModalgetControlLedger(response.glCode, response.crlCode);
                }
                $('#ModalforsidiaryControlCode').val(response.crlCode || '');
                $('#ModalforsidiaryCLShortName').val(response.crLShortName || '');

                // Sub-Control Ledger
                if (response.crlCode) {
                    ModalgetSubControllerLedger(response.crlCode, response.generalLedgerCodeNo);
                }
                $('#ModalforsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                $('#ModalforsidiarySubCRLshortName').val(response.subCRLShortName || '');

                // Dates
                if (response.lDate) {
                    let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#ModalSubSidiaryldisplayLDate').text('');
                }

                if (response.modifyDate) {
                    let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#ModalSubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#ModalSubSidiarydisplayModifyDate').text('');
                }

                $('.ModalSubsidiary-checkbox').prop('checked', false);
                $(`.ModalSubsidiary-checkbox[data-autoid="${autoId}"]`).prop('checked', true);
                $("#ModalSubSidiaryselectAll").prop('checked', false);

                ModalSubSidiaryupdateCheckboxState();
            } else {
                toastr.error("Sub-Sidiary Ledger not found!");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error fetching Sub-Sidiary Ledger details!");
            console.error("Error fetching details:", error);
        }
    });
});
//#endregion

//#region Clear Form
function ModalSubSidiaryLedgerFormClear() {
    ModalisEditModeSubSidiary = false;

    $("#ModalforsidiaryAutoID").val("");

    // Clear all custom dropdowns
    window.clearAllCustomDropdowns('#ModalsubSidiaryForm');

    $("#Modalforsidiarygroupcode, #ModalforsidiarygroupshortName").val("");
    $("#ModalforsidiaryControlCode, #ModalforsidiaryCLShortName").val("");
    $("#ModalforsidiaryGeneralLedgerCodeNo, #ModalforsidiarySubCRLshortName").val("");
    $("#ModalSusidiaryLedgerCodeNo, #ModalSubsidiaryLedgerName, #ModalforsidiaryshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#ModalSubSidiarydisplayModifyDate').text('');
    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $(".ModalSubsidiary-checkbox").prop("checked", false);
    $("#ModalSubSidiaryselectAll").prop("checked", false);
    $('#ModalSubSidiarysearchInput').val('');

    selectedModalSubSidiaryLedger = [];
    ModalcurrenSubSidiarytPage = 1;
    ModalselectedGroupCodeSSL = "";
    ModalselectedControlCodeSSL = "";
    ModalselectedSubControlCodeSSL = "";

    ModalSubSidiaryForGeneralUI();
    ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", "", "", "");
}
//#endregion

//#region Delete Sub-Sidiary Ledger
function ModalSubSidiaryLedgerDelete() {
    const ids = selectedModalSubSidiaryLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

    if (!ids.length) {
        toastr.warning("No Record Selected For Deletion.");
        return;
    }

    $.ajax({
        url: '/subsidiary-ledger-all-delete',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        success: function (response) {
            toastr.success(response.message || "Data Deleted Successfully.");

            const event = new CustomEvent('SubSidiaryLedgerUpdated', { detail: { newSubSidiaryLedgerCodeNo: response.susidiaryLedgerCodeNo } });
            window.dispatchEvent(event);

            ModalSubSidiaryLedgerFormClear();
            ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
            selectedModalSubSidiaryLedger = [];
            $("#ModalSubSidiaryselectAll").prop("checked", false);

            if (typeof SubSididiarLedgerAll === "function") {
                SubSididiarLedgerAll();
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Error Occurred During Deletion.");
            console.error("Delete Error:", error);
        }
    });
}
//#endregion

//#region Pagination & Checkbox Helpers
function ModalSubSidiaryupdateSortIndicators() {
    $('.ModalSubSidiarysortable').removeClass('sort-asc sort-desc');
    $('.ModalSubSidiarysortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.ModalSubSidiarysortable[data-column="${ModalSubSidiarycurrentSortColumn}"]`);
    header.addClass(ModalSubSidiarycurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(ModalSubSidiarycurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function ModalSubSidiarygeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#ModalSubSidiarypageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => ModalSubSididiarLedgerAll(
                page,
                ModalSubSidiarypageSize,
                ModalSubSidiarycurrentSortColumn,
                ModalSubSidiarycurrentSortDirection,
                $('#ModalSubSidiarysearchInput').val().trim(),
                ModalselectedGroupCodeSSL,
                ModalselectedControlCodeSSL,
                ModalselectedSubControlCodeSSL
            ));

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

function ModalSubSidiaryupdateCheckboxState() {
    $('.ModalSubsidiary-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
        $(this).prop('checked', selectedModalSubSidiaryLedger.includes(ID));
    });
    const allChecked = $('.ModalSubsidiary-checkbox:visible').length > 0 &&
        $('.ModalSubsidiary-checkbox:visible:not(:checked)').length === 0;
    $('#ModalSubSidiaryselectAll').prop('checked', allChecked);
}

function ModalSubSidiarydebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function ModalSubSidiarybindCheckboxHandlers() {
    $(document).off('click', '.ModalSubsidiary-checkbox').on('click', '.ModalSubsidiary-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedModalSubSidiaryLedger.includes(id)) selectedModalSubSidiaryLedger.push(id);
        } else {
            const index = selectedModalSubSidiaryLedger.indexOf(id);
            if (index !== -1) selectedModalSubSidiaryLedger.splice(index, 1);
        }

        const allChecked = $('.ModalSubsidiary-checkbox:visible').length > 0 &&
            $('.ModalSubsidiary-checkbox:visible:not(:checked)').length === 0;
        $('#ModalSubSidiaryselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#ModalSubSidiaryselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.ModalSubsidiary-checkbox:visible').prop('checked', isChecked);

    $('.ModalSubsidiary-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedModalSubSidiaryLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedModalSubSidiaryLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedModalSubSidiaryLedger.splice(index, 1);
        }
    });
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ModalSubSidiaryForGeneralUI();
    ModalSubSididiarLedgerAll();
    ModalSubSidiarybindCheckboxHandlers();

    $('#ModalSubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#ModalsubSidiaryForm", function (e) {
        e.preventDefault();
        ModalSubSidiaryLedgerSaveOrUpdate();
    });

    $(document).on("click", "#ModalSubSidiarydeleteBtn, .js-ModalSubSidiary-delete-confirm", function (e) {
        e.preventDefault();
        ModalSubSidiaryLedgerDelete();
    });

    $(document).on("click", ".ModalSubSidiaryresetBtn, .js-ModalSubSidiary-clear", function () {
        ModalSubSidiaryLedgerFormClear();
    });

    $(document).on('input', '.ModalSubSidiarysearchInput', ModalSubSidiarydebounce(function () {
        const searchValue = $(this).val().trim();
        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, searchValue, ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
    }, 500));

    $(document).on('click', '#ModalSubSidiaryfirstPage', function () {
        ModalcurrenSubSidiarytPage = 1;
        ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
    });

    $(document).on('click', '#ModalSubSidiarynextPage', function () {
        ModalcurrenSubSidiarytPage++;
        ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
    });

    $(document).on('click', '#ModalSubSidiaryprevPage', function () {
        if (ModalcurrenSubSidiarytPage > 1) {
            ModalcurrenSubSidiarytPage--;
            ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
        }
    });

    $(document).on('click', '#ModalSubSidiarylastPage', function () {
        let lastPage = parseInt($('#ModalSubSidiarypageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            ModalcurrenSubSidiarytPage = lastPage;
            ModalSubSididiarLedgerAll(ModalcurrenSubSidiarytPage, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, "", ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
        }
    });

    $(document).on('change', '.ModalSubSidiarypageSize', function () {
        ModalSubSidiarypageSize = parseInt($(this).val());
        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection);
    });

    $(document).on('click', '.ModalSubSidiarysortable', function () {
        const column = $(this).data('column');
        if (column === ModalSubSidiarycurrentSortColumn) {
            ModalSubSidiarycurrentSortDirection = (ModalSubSidiarycurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            ModalSubSidiarycurrentSortColumn = column;
            ModalSubSidiarycurrentSortDirection = 'desc';
        }
        ModalSubSididiarLedgerAll(1, ModalSubSidiarypageSize, ModalSubSidiarycurrentSortColumn, ModalSubSidiarycurrentSortDirection, $('#ModalSubSidiarysearchInput').val().trim(), ModalselectedGroupCodeSSL, ModalselectedControlCodeSSL, ModalselectedSubControlCodeSSL);
    });
});
//#endregion