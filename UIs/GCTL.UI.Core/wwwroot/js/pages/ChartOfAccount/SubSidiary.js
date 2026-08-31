let selectedSubSidiaryLedger = [];
let currenSubSidiarytPage = 1;
let SubSidiarypageSize = 10;
let SubSidiarycurrentSortColumn = 'SusidiaryLedgerCodeNo';
let SubSidiarycurrentSortDirection = 'desc';
let isEditModeSubSidiary = false;

// Selected dropdown values (used for reload, pagination, sorting)
let selectedGroupCodeSSL = "";
let selectedControlCodeSSL = "";
let selectedSubControlCodeSSL = "";

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
    ForSubSidiGRLDropdown();
    $("#forsidiarygroupcode").val("");
    $("#forsidiarygroupshortName").val("");

    $("#forsidiaryControlCode").val("");
    $("#forsidiaryCLShortName").val("");

    $("#forsidiaryGeneralLedgerCodeNo").val("");
    $("#forsidiarySubCRLshortName").val("");

    $("#SusidiaryLedgerCodeNo").val("");
    SubSididiarLedgerAll();
});
//#endregion

//#region Load Cascading Dropdowns

// 1. Group Ledger Dropdown
function ForSubSidiGRLDropdown(selectedId = null) {
    $.ajax({
        url: "/forsubsidiary-group-ledger-dropdown",
        type: "GET",
        success: function (groupResponse) {
            let $dropdown = $('#forsidiarygroupName');
            $dropdown.find('option:not(:first)').remove();

            if (groupResponse && groupResponse.data) {
                $.each(groupResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('forsidiarygroupName');

            $dropdown.off('change').on('change', function () {
                const selectedGroupCode = $(this).val();
                selectedGroupCodeSSL = selectedGroupCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedGroupCode) {
                    $('#forsidiarygroupcode').val('');
                    $('#forsidiarygroupshortName').val('');

                    // Control & Sub-Control Clear
                    window.clearCustomDropdownById('forsidiaryControlName');
                    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');

                    window.clearCustomDropdownById('forsidiaryGeneralLedgerName');
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName, #SusidiaryLedgerCodeNo').val('');

                    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", "", "", "");
                    return;
                }

                // Load Control Ledger Dropdown
                getControlLedger(selectedGroupCode);

                // Fetch Group Info
                $.ajax({
                    url: '/forsubsidiary-group-ledger-info',
                    type: 'GET',
                    data: { groupcode: selectedGroupCode },
                    success: function (data) {
                        $('#forsidiarygroupcode').val(data.controlLedgerCodeNo || '');
                        $('#forsidiarygroupshortName').val(data.shortName || '');
                        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, "", "");
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
function getControlLedger(selectedGroupCode, selectedId = null) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-dropdown',
        type: 'GET',
        data: { groupcode: selectedGroupCode },
        success: function (controlResponse) {
            let $dropdown = $('#forsidiaryControlName');
            $dropdown.find('option:not(:first)').remove();

            if (controlResponse && controlResponse.data) {
                $.each(controlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('forsidiaryControlName');

            $dropdown.off('change').on('change', function () {
                const selectedControlCode = $(this).val();
                selectedControlCodeSSL = selectedControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedControlCode) {
                    $('#forsidiaryControlCode, #forsidiaryCLShortName').val('');

                    window.clearCustomDropdownById('forsidiaryGeneralLedgerName');
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName, #SusidiaryLedgerCodeNo').val('');

                    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, "", "");
                    return;
                }

                // Load Sub-Control Dropdown
                getSubControllerLedger(selectedControlCode);

                // Fetch Control Info
                GetControlLedgerInfo(selectedControlCode);
            });
        },
        error: function () {
            console.error("Failed to fetch Control Ledger dropdown");
        }
    });
}

function GetControlLedgerInfo(selectedControlCode) {
    $.ajax({
        url: '/forSubsidiary-control-ledger-info',
        type: 'GET',
        data: { contrlcode: selectedControlCode },
        success: function (controlInfo) {
            $('#forsidiaryControlCode').val(controlInfo.subControlLedgerCodeNo || '');
            $('#forsidiaryCLShortName').val(controlInfo.shortName || '');
            SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, "");
        }
    });
}

// 3. Sub-Control Ledger Dropdown
function getSubControllerLedger(selectedControlCode, selectedId = null) {
    $.ajax({
        url: '/subcontrol-ledger-dropdown',
        type: 'GET',
        data: { controlcode: selectedControlCode },
        success: function (subControlResponse) {
            let $dropdown = $('#forsidiaryGeneralLedgerName');
            $dropdown.find('option:not(:first)').remove();

            if (subControlResponse && subControlResponse.data) {
                $.each(subControlResponse.data, function (index, item) {
                    $dropdown.append($('<option></option>').val(item.id).text(item.name));
                });
            }

            if (selectedId) {
                $dropdown.val(selectedId);
            }

            window.reinitCustomDropdownById('forsidiaryGeneralLedgerName');

            $dropdown.off('change').on('change', function () {
                const selectedSubControlCode = $(this).val();
                selectedSubControlCodeSSL = selectedSubControlCode || "";
                $(this).removeClass('border-error');
                $(this).siblings('.custom-dd-wrapper').removeClass('border-error');

                if (!selectedSubControlCode) {
                    $('#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName, #SusidiaryLedgerCodeNo').val('');
                    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, "");
                    return;
                }

                // Fetch Sub-Control Info
                GetSubcontrolInfo(selectedSubControlCode);
            });
        },
        error: function () {
            console.error("Failed to fetch Sub-Control Ledger dropdown");
        }
    });
}

function GetSubcontrolInfo(selectedSubControlCode) {
    $.ajax({
        url: '/forsudsidiary-subcontrol-ledger-info',
        type: 'GET',
        data: { subcontrlcode: selectedSubControlCode },
        success: function (subControlInfo) {
            $('#forsidiaryGeneralLedgerCodeNo').val(subControlInfo.generalLedgerCodeNo || '');
            $('#forsidiarySubCRLshortName').val(subControlInfo.shortName || '');

            if (!isEditModeSubSidiary) {
                $.ajax({
                    url: '/next-subsidiary-ledger-code',
                    type: 'GET',
                    data: { subcontrolLedgercode: subControlInfo.generalLedgerCodeNo },
                    success: function (nextCode) {
                        $('#SusidiaryLedgerCodeNo').val(nextCode);
                    }
                });
            }

            SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
        }
    });
}

//#endregion

//#region Load All Sub-Sidiary Ledger Data
function SubSididiarLedgerAll(page = 1, pageSize = SubSidiarypageSize, sortColumn = SubSidiarycurrentSortColumn, sortDirection = SubSidiarycurrentSortDirection, searchTerm = "", groupLedgerCode = "", controlLedgerCode = "", subcontrolLedgerCode = "") {
    if ($.fn.DataTable && $.fn.DataTable.isDataTable('#subSidiaryLedgerTable')) {
        $('#subSidiaryLedgerTable').DataTable().ajax.reload(null, false);
        return;
    }
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
                            <input type="checkbox" class="form-check-input form-check-input-sm Subsidiary-checkbox"
                                data-autoid="${item.autoId}" style="width:12px; height:12px;">
                        </td>
                        <td>
                            <button type="button" class="btn btn-link p-0 text-decoration-none subsidiary-ledger" data-autoid="${item.autoId}">
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
            $('#SubSidiarytblBody').html(rows);

            if (response.paginationInfo) {
                let currPage = response.paginationInfo.currentPage;
                let totalPages = response.paginationInfo.totalPages;
                let totalRecords = response.totalCount || 0;
                let startIndex = response.paginationInfo.startItem || 0;
                let endIndex = response.paginationInfo.endItem || 0;

                $('#SubSidiarypaginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                $('#SubSidiarypaginationInfo').text(`Page ${currPage} of ${Math.max(1, totalPages)}`);
                $('#SubSidiarytotalRecordsInfo').text(`Total Records: ${totalRecords}`);

                currenSubSidiarytPage = currPage;
                SubSidiarygeneratePageButtons(currenSubSidiarytPage, totalPages);
            }

            SubSidiaryupdateCheckboxState();
            SubSidiaryupdateSortIndicators();
        },
        error: function (xhr, status, error) {
            console.error("Error Loading Data: ", error);
        }
    });
}
//#endregion

//#region Save Or Update Sub-Sidiary Ledger
function SubSidiaryLedgerSaveOrUpdate() {
    let autoID = $('#forsidiaryAutoID').val();

    let SubSidiaryLedgerData = {
        autoId: autoID ? parseInt(autoID) : 0,
        GeneralLedgerCodeNo: $("#forsidiaryGeneralLedgerCodeNo").val(),
        SusidiaryLedgerCodeNo: $("#SusidiaryLedgerCodeNo").val(),
        SubsidiaryLedgerName: $("#SubsidiaryLedgerName").val(),
        ShortName: $("#forsidiaryshortName").val(),
        CRLCode: $("#forsidiaryControlName").val(),
        GLCode: $("#forsidiarygroupName").val()
    };

    // Validations
    if (!SubSidiaryLedgerData.GLCode) {
        toastr.error("Please Select a Group Ledger.");
        $('#forsidiarygroupName').addClass('border-error');
        $('#forsidiarygroupName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.CRLCode) {
        toastr.error("Please Select a Control Ledger.");
        $('#forsidiaryControlName').addClass('border-error');
        $('#forsidiaryControlName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.GeneralLedgerCodeNo) {
        toastr.error("Please Select a Sub-Control Ledger.");
        $('#forsidiaryGeneralLedgerName').addClass('border-error');
        $('#forsidiaryGeneralLedgerName').siblings('.custom-dd-wrapper').addClass('border-error');
        return false;
    }

    if (!SubSidiaryLedgerData.SubsidiaryLedgerName) {
        toastr.error("Please Enter Sub-Sidiary Ledger Name.");
        $('#SubsidiaryLedgerName').addClass('border-error');
        return false;
    } else {
        $('#SubsidiaryLedgerName').removeClass('border-error');
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

                        SubSidiaryLedgerFormClear();
                        selectedSubSidiaryLedger = [];

                        if (typeof ModalSubSididiarLedgerAll === "function") {
                            ModalSubSididiarLedgerAll();
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
$(document).on('click', '.js-subsidiaryledger-edit, .subsidiary-ledger', function (e) {
    e.preventDefault();
    const autoId = parseInt($(this).data('id') || $(this).data('autoid'));
    if (!autoId) return;

    isEditModeSubSidiary = true;
    selectedSubSidiaryLedger = [autoId];

    $.ajax({
        url: `/subsidiary-ledger/details/${autoId}`,
        type: 'GET',
        success: function (response) {
            if (response) {
                $('#forsidiaryAutoID').val(response.autoId);
                $('#SusidiaryLedgerCodeNo').val(response.susidiaryLedgerCodeNo || '');
                $('#SubsidiaryLedgerName').val(response.subsidiaryLedgerName || '');
                $('#forsidiaryshortName').val(response.shortName || '');

                // Group Ledger
                $('#forsidiarygroupcode').val(response.glCode || '');
                $('#forsidiarygroupshortName').val(response.glShortName || '');
                $('#forsidiarygroupName').val(response.glCode);
                window.reinitCustomDropdownById('forsidiarygroupName');

                // Control Ledger
                if (response.glCode) {
                    getControlLedger(response.glCode, response.crlCode);
                }
                $('#forsidiaryControlCode').val(response.crlCode || '');
                $('#forsidiaryCLShortName').val(response.crLShortName || '');

                // Sub-Control Ledger
                if (response.crlCode) {
                    getSubControllerLedger(response.crlCode, response.generalLedgerCodeNo);
                }
                $('#forsidiaryGeneralLedgerCodeNo').val(response.generalLedgerCodeNo || '');
                $('#forsidiarySubCRLshortName').val(response.subCRLShortName || '');

                // Dates
                if (response.lDate) {
                    let entryDate = new Date(response.lDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#SubSidiaryldisplayLDate').text(entryDate.toUpperCase());
                } else {
                    $('#SubSidiaryldisplayLDate').text('');
                }

                if (response.modifyDate) {
                    let updateDate = new Date(response.modifyDate).toLocaleString('en-GB', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: 'numeric', minute: '2-digit', hour12: true
                    }).replace(',', '');
                    $('#SubSidiarydisplayModifyDate').text(updateDate.toUpperCase());
                } else {
                    $('#SubSidiarydisplayModifyDate').text('');
                }

                $('.Subsidiary-checkbox').prop('checked', false);
                $(`.Subsidiary-checkbox[data-autoid="${autoId}"]`).prop('checked', true);
                $("#SubSidiaryselectAll").prop('checked', false);

                SubSidiaryupdateCheckboxState();
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
function SubSidiaryLedgerFormClear() {
    isEditModeSubSidiary = false;

    $("#forsidiaryAutoID").val("");

    // Clear all custom dropdowns
    window.clearAllCustomDropdowns('#subSidiaryForm');

    $("#forsidiarygroupcode, #forsidiarygroupshortName").val("");
    $("#forsidiaryControlCode, #forsidiaryCLShortName").val("");
    $("#forsidiaryGeneralLedgerCodeNo, #forsidiarySubCRLshortName").val("");
    $("#SusidiaryLedgerCodeNo, #SubsidiaryLedgerName, #forsidiaryshortName").val("");

    $(".form-control, .form-select").removeClass("is-invalid is-valid border-error");
    $(".custom-dd-wrapper").removeClass("border-error");
    $(".invalid-feedback, .valid-feedback").remove();

    $('#SubSidiarydisplayModifyDate').text('');
    $('#SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $(".Subsidiary-checkbox").prop("checked", false);
    $("#SubSidiaryselectAll").prop("checked", false);
    $('#SubSidiarysearchInput').val('');

    selectedSubSidiaryLedger = [];
    currenSubSidiarytPage = 1;
    selectedGroupCodeSSL = "";
    selectedControlCodeSSL = "";
    selectedSubControlCodeSSL = "";

    ForSubSidiGRLDropdown();
    SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", "", "", "");
}
//#endregion

//#region Delete Sub-Sidiary Ledger
function SubSidiaryLedgerDelete() {
    const ids = selectedSubSidiaryLedger.map(x => parseInt(x)).filter(x => !isNaN(x));

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

            SubSidiaryLedgerFormClear();
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection);
            selectedSubSidiaryLedger = [];
            $("#SubSidiaryselectAll").prop("checked", false);

            if (typeof ModalSubSididiarLedgerAll === "function") {
                ModalSubSididiarLedgerAll();
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
function SubSidiaryupdateSortIndicators() {
    $('.SubSidiarysortable').removeClass('sort-asc sort-desc');
    $('.SubSidiarysortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

    const header = $(`.SubSidiarysortable[data-column="${SubSidiarycurrentSortColumn}"]`);
    header.addClass(SubSidiarycurrentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');

    const icon = header.find('.sort-icon');
    icon.removeClass('fa-sort fa-sort-up fa-sort-down');
    icon.addClass(SubSidiarycurrentSortDirection === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
}

function SubSidiarygeneratePageButtons(currentPage, totalPages) {
    const navigationDiv = $('#SubSidiarypageNavigation');
    navigationDiv.empty();

    function createButton(page) {
        const btn = $('<button type="button">')
            .addClass('btn btn-sm mx-1')
            .text(page)
            .click(() => SubSididiarLedgerAll(page, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, $('#SubSidiarysearchInput').val().trim(), selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL));

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

function SubSidiaryupdateCheckboxState() {
    $('.Subsidiary-checkbox').each(function () {
        const ID = parseInt($(this).data('autoid'));
        $(this).prop('checked', selectedSubSidiaryLedger.includes(ID));
    });
    const allChecked = $('.Subsidiary-checkbox:visible').length > 0 &&
        $('.Subsidiary-checkbox:visible:not(:checked)').length === 0;
    $('#SubSidiaryselectAll').prop('checked', allChecked);
}

function SubSidiarydebounce(func, delay) {
    let timer;
    return function () {
        const context = this, args = arguments;
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(context, args), delay);
    };
}

function SubSidiarybindCheckboxHandlers() {
    $(document).off('click', '.Subsidiary-checkbox').on('click', '.Subsidiary-checkbox', function () {
        const id = parseInt($(this).data('autoid'));
        const isChecked = $(this).prop('checked');

        if (isChecked) {
            if (!selectedSubSidiaryLedger.includes(id)) selectedSubSidiaryLedger.push(id);
        } else {
            const index = selectedSubSidiaryLedger.indexOf(id);
            if (index !== -1) selectedSubSidiaryLedger.splice(index, 1);
        }

        const allChecked = $('.Subsidiary-checkbox:visible').length > 0 &&
            $('.Subsidiary-checkbox:visible:not(:checked)').length === 0;
        $('#SubSidiaryselectAll').prop('checked', allChecked);
    });
}

$(document).on('click', '#SubSidiaryselectAll', function () {
    const isChecked = $(this).prop('checked');
    $('.Subsidiary-checkbox:visible').prop('checked', isChecked);

    $('.Subsidiary-checkbox:visible').each(function () {
        const id = parseInt($(this).data('autoid'));
        const index = selectedSubSidiaryLedger.indexOf(id);
        if (isChecked && index === -1) {
            selectedSubSidiaryLedger.push(id);
        } else if (!isChecked && index !== -1) {
            selectedSubSidiaryLedger.splice(index, 1);
        }
    });
});
//#endregion

//#region Document Ready
$(document).ready(function () {
    ForSubSidiGRLDropdown();
    SubSididiarLedgerAll();
    SubSidiarybindCheckboxHandlers();

    $('#SubSidiaryldisplayLDate').text(getBangladeshDateTime());

    $(document).on("submit", "#subSidiaryForm", function (e) {
        e.preventDefault();
        SubSidiaryLedgerSaveOrUpdate();
    });
    $(document).on("click", ".js-SubSidiary-save", function (e) {
        e.preventDefault();
        SubSidiaryLedgerSaveOrUpdate();
    });

    $(document).on("click", "#SubSidiarydeleteBtn, .js-SubSidiary-delete-confirm", function (e) {
        e.preventDefault();
        SubSidiaryLedgerDelete();
    });

    $(document).on("click", ".SubSidiaryresetBtn, .js-SubSidiary-clear", function () {
        SubSidiaryLedgerFormClear();
    });

    $(document).on('input', '.SubSidiarysearchInput', SubSidiarydebounce(function () {
        const searchValue = $(this).val().trim();
        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, searchValue, selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    }, 500));

    $(document).on('click', '#SubSidiaryfirstPage', function () {
        currenSubSidiarytPage = 1;
        SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });

    $(document).on('click', '#SubSidiarynextPage', function () {
        currenSubSidiarytPage++;
        SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });

    $(document).on('click', '#SubSidiaryprevPage', function () {
        if (currenSubSidiarytPage > 1) {
            currenSubSidiarytPage--;
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
        }
    });

    $(document).on('click', '#SubSidiarylastPage', function () {
        let lastPage = parseInt($('#SubSidiarypageNavigation button').last().text());
        if (!isNaN(lastPage)) {
            currenSubSidiarytPage = lastPage;
            SubSididiarLedgerAll(currenSubSidiarytPage, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, "", selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
        }
    });

    $(document).on('change', '.SubSidiarypageSize', function () {
        SubSidiarypageSize = parseInt($(this).val());
        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection);
    });

    $(document).on('click', '.SubSidiarysortable', function () {
        const column = $(this).data('column');
        if (column === SubSidiarycurrentSortColumn) {
            SubSidiarycurrentSortDirection = (SubSidiarycurrentSortDirection === 'desc') ? 'asc' : 'desc';
        } else {
            SubSidiarycurrentSortColumn = column;
            SubSidiarycurrentSortDirection = 'desc';
        }
        SubSididiarLedgerAll(1, SubSidiarypageSize, SubSidiarycurrentSortColumn, SubSidiarycurrentSortDirection, $('#SubSidiarysearchInput').val().trim(), selectedGroupCodeSSL, selectedControlCodeSSL, selectedSubControlCodeSSL);
    });
});
//#endregion