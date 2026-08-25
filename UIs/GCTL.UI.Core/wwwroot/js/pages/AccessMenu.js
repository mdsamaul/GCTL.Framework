$(document).ready(function () {
    //getMenuData(null);

    // Right Side Table
    loadAccessList();
    loadAccessCodeDropdown();
    loadParentMenus();
    setupCheckboxLogic11();

    $(document).on('change', '#access-code-id', function () {
        const accessCodeId = $(this).val();

        if (accessCodeId) {
            getAccessName(accessCodeId);
            //getMenuData(accessCodeId);
        } else {
            $('#access-code-name').val('');
            //getMenuData(null);
        }

        // Reset module filter
        $('#module-select').val('').trigger('change');

        updateTableVisibility();
        //renderMenuTable([]);
        applyModuleFilter('');
    });

    $('#module-select').on('change', function () {
        const moduleId = $(this).val();
        updateTableVisibility();

        if ($('#access-code-id').val() && moduleId)
            getMenuData($('#access-code-id').val());
        else {
            //renderMenuTable([]);
            applyModuleFilter(moduleId);
        }
        applyModuleFilter($(this).val());
    });


    $(document).on('click', '.access-id-btn', function () {
        const id = $(this).data('id');
        $('#access-code-id').val(id).trigger('change');
        //$('#access-code-id');
    });
    //#endregion

    $('#clearBtn').on('click', function () {
        $('#access-code-id').val('').trigger('change');
        $('#access-code-name').val('');
        $('#module-select').val('').trigger('change');

        renderMenuTable([]);
        updateTableVisibility();
        //getMenuData(null);
        //applyModuleFilter('');
    });

    $('#openAddAccessCodeModal').on('click', function () {
        $('#modal-access-code-id').val('');
        $('#modal-access-code-name').val('');
        showModal('#addAccessCodeModal');
        loadAccessModal();
    });


    $('#saveNewAccessCodeBtn').on('click', function () {
        const id = $('#modal-access-code-id').val().trim();
        const name = $('#modal-access-code-name').val().trim();

        if (!id || !name) {
            alert('Both fields are required.');
            return;
        }

        $.ajax({
            url: '/MenuTab/AddAccessCode',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ accessCodeId: id, accessCodeName: name }),
            success: function () {
                hideModal('#addAccessCodeModal');
                loadAccessCodeDropdown(id); // reload and select new one
                loadAccessList();
            },
            error: function (err) {
                console.error(err);
                alert('Error adding access code.');
            }
        });
    });

    // Save button
    $('#saveAccessMenuBtn').on('click', function () {
        const accessCodeId = $('#access-code-id').val().trim();
        const accessCodeName = $('#access-code-name').val().trim();

        if (!accessCodeId || !accessCodeName) {
            alert('Access Code Id and Name are required.');
            return;
        }

        const allMenus = [];

        // Iterate ALL rows, including hidden ones (module filter uses hide/show)
        $('#menuTabLoad tr').each(function () {
            const $row = $(this);
            const menuId = $row.data('menuid');
            const parentId = $row.data('parentid') || null;

            allMenus.push({
                menuId: menuId + "",
                parentId: parentId + "",
                isSelected: $row.find('.row-main-check').is(':checked'),
                canAdd: $row.find('.add-check').is(':checked'),
                canEdit: $row.find('.edit-check').is(':checked'),
                canDelete: $row.find('.delete-check').is(':checked'),
                canPrint: $row.find('.print-check').is(':checked')
            });
        });

        $.ajax({
            url: '/menuTab/SaveAccessCode',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ accessCodeId, accessCodeName, menuAccessList: allMenus }),
            success: function () {
                alert('Data saved successfully.');
                loadAccessList();
                loadAccessCodeDropdown(accessCodeId);
            },
            error: function (err) {
                console.error(err);
                alert('Error saving data.');
            }
        });
    });


    //#endregion Save Button


});

//#region Bootstrap 5 modal helpers
// Bootstrap 5 dropped the jQuery modal() plugin - use the vanilla bootstrap.Modal API instead.
function getModalInstance(selector) {
    const el = document.querySelector(selector);
    return bootstrap.Modal.getOrCreateInstance(el);
}
function showModal(selector) {
    getModalInstance(selector).show();
}
function hideModal(selector) {
    getModalInstance(selector).hide();
}
//#endregion

function loadAccessList() {
    $.gctlGrid.initFromModel({
        gridId: 'accessListTable'
    });
}

$('#selectAllAccess').on('change', function () {
    $('.checkBox').prop('checked', this.checked);
});

$('#deleteSelectedBtn').on('click', function (e) {
    e.preventDefault();
    let selectedIds = [];

    $('.checkBox:checked').each(function () {
        selectedIds.push($(this).val());
    });

    if (selectedIds.length === 0 && $(this).data('id')) {
        selectedIds.push($(this).data('id'));
    } else if (selectedIds.length === 0 && $('#access-code-id').val()) {
        selectedIds.push($('#access-code-id').val().trim());
    }

    if (selectedIds.length === 0) {
        toastr.info('Please select at least one item.');
        return;
    }

    GcDelete.confirm("Are you sure to delete?", function () {
        $.ajax({
            url: '/menuTab/DeleteAccessCodes',
            method: 'POST',
            data: JSON.stringify(selectedIds),
            contentType: 'application/json',
            success: function (res) {
                toastr.success(res.message || "Deleted Successfully");
                const currentId = $('#access-code-id').val().trim();
                loadAccessList();
                loadAccessCodeDropdown();
                if (selectedIds.includes(currentId)) {
                    $('#access-code-id').val('').trigger('change');
                    $('#access-code-name').val('');
                    renderMenuTable([]);
                    updateTableVisibility();
                }
                $('#deleteSelectedBtn').removeData('id');
            },
            error: function (err) {
                toastr.error("Error deleting access code.");
            }
        });
    });
});

//#region Modal

function loadAccessModal() {
    $("#addAccessCodeContent").html('<div class="text-center"><i class="fa fa-spinner fa-spin fa-3x"></i></div>');

    $.ajax({
        url: '/MenuTab/AccessCodeIndex',
        data: { child: true },
        type: 'GET',
        success: function (res) {
            if (res.message) {
                showNotification(res.message, 'warning');
                return;
            }

            $("#addAccessCodeContent").html(res);

            if (typeof $.accessCodeEntries === 'function') {
                var options = {
                    isModal: true,
                    onSaved: function (e) {
                        if (e) {
                            loadAccessCodeDropdown(e);
                            getMenuData(e);
                        } else {
                            loadAccessCodeDropdown(null);
                            getMenuData(null);
                        }
                        loadAccessList();
                        loadAccessCodeDropdown();
                        hideModal('#addAccessCodeModal');
                    },
                    onDelete: function () {
                        const currentId = $('#access-code-id').val().trim();
                        loadAccessList();
                        loadAccessCodeDropdown();
                        //if (selectedIds.includes(currentId)) {
                        $('#access-code-id').val('');
                        $('#access-code-name').val('');
                        getMenuData(null);
                        //}
                        loadAccessCodeDropdown(null);

                        hideModal('#addAccessCodeModal');
                    }
                };
                $.accessCodeEntries(options);
            }
            showModal('#addAccessCodeModal');
        },
        error: function (error) {
            $("#addAccessCodeContent").html('<div class="alert alert-danger">Error loading content. Please try again.</div>');
        }
    })
}

//#endregion Modal


//#region Main Table

function renderMenuTable(data) {
    console.log(data);
    const $tableBody = $('#menuTabLoad');
    $tableBody.empty();

    function renderRow(item, level) {
        const indent = '&nbsp;'.repeat(level * 4);
        const isParent = item.children && item.children.length > 0;
        const row = `
            <tr class="${isParent ? 'bg-light fw-bold parent-row' : 'child-row'}" 
                data-menuid="${item.menuId}" 
                data-parentid="${item.parentId || ''}"
                data-level="${level}"
                ${level > 0 ? 'style="display:none;"' : ''}>
                
                <td class="text-center align-middle">
                    <input class="row-main-check" type="checkbox">
                </td>
                <td class="${isParent ? 'fw-bold' : 'text-primary'}" style="cursor:${isParent ? 'pointer' : 'default'}">
                    ${indent}
                    ${isParent ? '<span class="toggle-icon me-1">▶</span>' : ''}
                    ${item.title || ''}
                </td>
                <td class="text-center">
                    <input class="add-check" type="checkbox" ${item.checkAdd ? 'checked' : ''}>
                </td>
                <td class="text-center">
                    <input class="edit-check" type="checkbox" ${item.checkEdit ? 'checked' : ''}>
                </td>
                <td class="text-center">
                    <input class="delete-check" type="checkbox" ${item.checkDelete ? 'checked' : ''}>
                </td>
                <td class="text-center">
                    <input class="print-check" type="checkbox" ${item.checkPrint ? 'checked' : ''}>
                </td>
            </tr>
        `;
        $tableBody.append(row);

        if (isParent) {
            item.children.forEach(child => renderRow(child, level + 1));
        }
    }

    data.forEach(item => renderRow(item, 0));

    // Collapse/Expand logic
    $tableBody.off('click', '.parent-row').on('click', '.parent-row', function (e) {
        if ($(e.target).is('input')) return;

        const parentId = $(this).data('menuid');
        const $icon = $(this).find('.toggle-icon');
        const isCollapsed = $icon.text().trim() === '▶';

        $icon.text(isCollapsed ? '▼' : '▶');
        toggleChildren(parentId, isCollapsed);
    });

    function toggleChildren(parentId, show) {
        $tableBody.find(`tr[data-parentid="${parentId}"]`).each(function () {
            if (show) {
                $(this).show();
            } else {
                $(this).hide();
                const childId = $(this).data('menuid');
                if ($(this).hasClass('parent-row')) {
                    $(this).find('.toggle-icon').text('▶');
                    toggleChildren(childId, false);
                }
            }
        });
    }
}
function setupCheckboxLogic11() {
    $('#selectAll').on('change', function () {
        const checked = $(this).is(':checked');
        $('.row-main-check, .add-check, .edit-check, .delete-check, .print-check').prop('checked', checked);
        $('#addSelectAll, #editSelectAll, #deleteSelectAll, #printSelectAll').prop('checked', checked);
    });

    $('#menuTabLoad').on('change', '.row-main-check', function () {
        const $row = $(this).closest('tr');
        const isChecked = $(this).is(':checked');
        $row.find('.add-check, .edit-check, .delete-check, .print-check').prop('checked', isChecked);
        checkChildren($row.data('menuid'), isChecked);
        updateSelectAllState();
    });

    $('#menuTabLoad').on('change', '.add-check, .edit-check, .delete-check, .print-check', function () {
        const $row = $(this).closest('tr');
        const allRightsChecked = $row.find('.add-check, .edit-check, .delete-check, .print-check').filter(':checked').length === 4;
        $row.find('.row-main-check').prop('checked', allRightsChecked);
        const rightClass = $(this).attr('class').split(' ')[0];
        checkRightInChildren($row.data('menuid'), rightClass, $(this).is(':checked'));
        updateSelectAllState();
    });

    function updateSelectAllState() {
        const totalBoxes = $('.row-main-check').length;
        const checkedBoxes = $('.row-main-check:checked').length;
        $('#selectAll').prop('checked', totalBoxes === checkedBoxes);
        ['add', 'edit', 'delete', 'print'].forEach(function (type) {
            const total = $(`.${type}-check`).length;
            const checked = $(`.${type}-check:checked`).length;
            $(`#${type}SelectAll`).prop('checked', total === checked);
        });
    }

    function checkChildren(parentId, isChecked) {
        $(`tr[data-parentid="${parentId}"]`).each(function () {
            $(this).find('.row-main-check').prop('checked', isChecked);
            $(this).find('.add-check, .edit-check, .delete-check, .print-check').prop('checked', isChecked);
            checkChildren($(this).data('menuid'), isChecked);
        });
    }

    function checkRightInChildren(parentId, rightClass, isChecked) {
        $(`tr[data-parentid="${parentId}"]`).each(function () {
            $(this).find(`.${rightClass}`).prop('checked', isChecked);
            checkRightInChildren($(this).data('menuid'), rightClass, isChecked);
        });
    }

    $('#addSelectAll').on('change', function () { $('.add-check').prop('checked', $(this).is(':checked')); });
    $('#editSelectAll').on('change', function () { $('.edit-check').prop('checked', $(this).is(':checked')); });
    $('#deleteSelectAll').on('change', function () { $('.delete-check').prop('checked', $(this).is(':checked')); });
    $('#printSelectAll').on('change', function () { $('.print-check').prop('checked', $(this).is(':checked')); });
}

//#endregion Main Table

function loadAccessCodeDropdown(selectAfterLoad) {
    $.ajax({
        url: '/menuTab/GetAccessListTable',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            const list = response.data ?? response;

            const $select = $('#access-code-id');
            const currentVal = selectAfterLoad || $select.val();
            $select.empty().append('<option value="">-- Select --</option>');
            list.forEach(function (item) {
                $select.append(
                    $('<option>', { value: item.accessCodeId, text: item.accessCodeName })
                );
            });
            if (currentVal) {
                $select.val(currentVal);
            }
        },
        error: function (err) {
            console.error('Failed to load access codes:', err);
        }
    });
}

function loadParentMenus() {
    $.ajax({
        url: '/MenuTab/GetParentMenus',
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            const $select = $('#module-select');
            $select.empty().append('<option value="">-- All --</option>');
            data.forEach(function (item) {
                $select.append(
                    $('<option>', { value: item.menuId, text: item.title })
                );
            });
        },
        error: function (err) {
            console.error('Failed to load parent menus:', err);
        }
    });
}

function applyModuleFilter(moduleId) {
    if (!moduleId) {
        // Show only level-0 rows; children remain collapsed as per renderMenuTable default
        $('#menuTabLoad tr').each(function () {
            const level = parseInt($(this).data('level'));
            if (level === 0) {
                $(this).show();
            } else {
                $(this).hide();
                // Reset toggle icon
                $(this).find('.toggle-icon').text('▶');
            }
        });
        return;
    }

    // Hide everything first
    $('#menuTabLoad tr').hide();

    // Show the selected parent row
    const $parentRow = $(`#menuTabLoad tr[data-menuid="${moduleId}"]`);
    $parentRow.show();
    $parentRow.find('.toggle-icon').text('▼');

    // Show all its direct and nested children
    showAllChildren(moduleId);
}

function showAllChildren(parentId) {
    $(`#menuTabLoad tr[data-parentid="${parentId}"]`).each(function () {
        $(this).show();
        const childId = $(this).data('menuid');
        if ($(this).hasClass('parent-row')) {
            $(this).find('.toggle-icon').text('▼'); // ✅ expand nested parents too
        }
        showAllChildren(childId);
    });
}


function getAccessName(accessCodeId) {
    if (!accessCodeId) return;
    $.ajax({
        url: '/MenuTab/GetAccessName',
        method: 'GET',
        data: { accessCodeId },
        dataType: 'text',
        success: function (response) {
            $('#access-code-name').val(response);
        },
        error: function (err) {
            console.error('Failed to load access name:', err);
        }
    });
}
function getMenuData(accessCodeId) {
    const moduleId = $('#module-select').val();
    $.ajax({
        url: '/MenuTab/AccessMenus',
        method: 'GET',
        data: { accessCodeId },
        dataType: 'json',
        success: function (response) {
            renderMenuTable(response.data);
            // Re-apply module filter after table re-render
            applyModuleFilter(moduleId);
        },
        error: function (err) {
            console.error('Failed to load menu data:', err);
        }
    });
}

function updateTableVisibility() {
    const hasAccessCode = !!$('#access-code-id').val();
    const hasModule = !!$('#module-select').val();

    if (hasAccessCode && hasModule)
        $('.menuTableWrapper').show();
    else
        $('.menuTableWrapper').hide();
}

//#endregion
function showNotification(message, type) {
    if (typeof toastr !== 'undefined') {
        toastr[type](message, type === 'success' ? 'Success' : type === 'error' ? 'Error' : 'Warning');
    } else {
        alert(message);
    }
}