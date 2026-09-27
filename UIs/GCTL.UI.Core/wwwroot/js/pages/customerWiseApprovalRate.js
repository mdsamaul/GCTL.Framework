(function ($) {
    "use strict";

    let gridItems = [];
    let selectedItemIndex = -1;
    let masterDataTable = null;

    $(document).ready(function () {
        initCustomDropdowns();
        initEventHandlers();
        renderGrid();
        initMasterGrid();
    });

    function initCustomDropdowns() {
        if (window.initCustomDropdowns) {
            window.initCustomDropdowns(document, false);
        }
    }

    function setDropdownValue(id, val) {
        $("#" + id).val(val);
        if (window.reinitCustomDropdownById) {
            window.reinitCustomDropdownById(id);
        } else {
            $("#" + id).trigger("change");
        }
    }

    function clearDropdownValue(id) {
        if (window.clearCustomDropdownById) {
            window.clearCustomDropdownById(id);
        } else {
            $("#" + id).val("").trigger("change");
        }
    }

    function initEventHandlers() {
        // Customer change
        $("#ddlCustomer").on("change", function () {
            const custId = $(this).val();
            if (custId) {
                loadCustomerData(custId);
            } else {
                resetForm();
            }
        });

        // Temp CHC Grid actions
        $("#btnAddGridRow").on("click", function () {
            addOrUpdateGridRow();
        });

        $("#btnRemoveGridRow").on("click", function () {
            removeSelectedGridRow();
        });

        $("#btnResetGridInputs").on("click", function () {
            resetGridInputs();
        });

        // Main Toolbar actions
        $("#btnSave").on("click", function () {
            saveApprovalRate();
        });

        $("#btnClear").on("click", function () {
            resetForm();
        });

        $("#btnDelete").on("click", function () {
            const checkedIds = getCheckedMasterIds();
            const currentCwarId = $("#txtCwarId").val();
            const currentCustId = $("#ddlCustomer").val();

            if (checkedIds.length === 0 && (!currentCustId || !currentCwarId)) {
                toastr.warning("Please select record(s) to delete.");
                return;
            }

            let msg = "Are you sure you want to delete this Customer Wise Approval Rate entry?";
            if (checkedIds.length > 1) {
                msg = `Are you sure you want to delete ${checkedIds.length} selected records?`;
            }
            $("#deleteConfirmModal .modal-body p").text(msg);
            $("#deleteConfirmModal").modal("show");
        });

        $("#btnConfirmDelete").on("click", function () {
            deleteApprovalRate();
        });

        // Check All on Master Grid
        $(document).on("change", "#checkAllCwar", function () {
            const isChecked = $(this).is(":checked");
            $(".cwar-row-check").prop("checked", isChecked);
        });

        // Row selection in Master Grid
        $(document).on("click", "#cwarMasterGrid tbody tr", function (e) {
            if ($(e.target).is("input[type='checkbox']") || $(e.target).closest(".col-select-cell").length || $(e.target).is(".cwar-id-link")) {
                return;
            }
            const $link = $(this).find(".cwar-id-link");
            const custId = $link.data("customer-id") || $link.attr("data-customer-id") || $(this).data("customer-id");
            if (custId) {
                setDropdownValue("ddlCustomer", custId);
                loadCustomerData(custId);
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        });

        // ID link click in Master Grid
        $(document).on("click", ".cwar-id-link", function (e) {
            e.preventDefault();
            e.stopPropagation();
            const custId = $(this).data("customer-id") || $(this).closest("tr").data("customer-id");
            if (custId) {
                setDropdownValue("ddlCustomer", custId);
                loadCustomerData(custId);
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        });
    }

    function getCheckedMasterIds() {
        const ids = [];
        $("#cwarMasterGrid tbody input[type='checkbox']:checked").each(function () {
            const val = $(this).val();
            if (val) {
                ids.push(val);
            }
        });
        return ids;
    }

    function loadCustomerData(customerId) {
        $.ajax({
            url: "/CustomerWiseApprovalRate/GetCustomerData",
            type: "GET",
            data: { customerId: customerId },
            success: function (res) {
                if (res.success && res.data) {
                    const d = res.data;
                    $("#txtAddress").val(d.address || "");
                    $("#txtPhone").val(d.phone || "");
                    $("#txtEmail").val(d.email || "");
                    $("#txtCwarId").val(d.costingId || "");

                    $("#txtMinAgencyComm").val(d.minimumAgencyCommission !== null ? d.minimumAgencyCommission : "");
                    $("#txtOnInvoiceValue").val(d.onInvoiceValue !== null ? d.onInvoiceValue : "");

                    if (d.ldate) {
                        const dt = new Date(d.ldate);
                        $("#lblEntryDate").text(dt.toLocaleDateString() + " " + dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
                    } else {
                        $("#lblEntryDate").text("-");
                    }

                    if (d.modifyDate) {
                        const dt = new Date(d.modifyDate);
                        $("#lblModifyDate").text(dt.toLocaleDateString() + " " + dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
                    } else {
                        $("#lblModifyDate").text("-");
                    }

                    gridItems = (d.details || []).map(function (item, idx) {
                        return {
                            tc: item.tc || 0,
                            costingDetailsId: item.costingDetailsId || (idx + 1).toString().padStart(4, "0"),
                            costingId: d.costingId,
                            customerId: customerId,
                            chcid: item.chcid,
                            chchead: item.chchead || item.chcid,
                            expenseTypeId: item.expenseTypeId,
                            expenseTypeName: item.expenseTypeName || item.expenseTypeId,
                            billAmount: parseFloat(item.billAmount || 0)
                        };
                    });

                    selectedItemIndex = -1;
                    resetGridInputs();
                    renderGrid();
                } else {
                    toastr.error(res.message || "Failed to load customer data.");
                }
            },
            error: function () {
                toastr.error("Server error while loading customer data.");
            }
        });
    }

    function renderGrid() {
        const $tbody = $("#cwarGridBody");
        $tbody.empty();

        if (!gridItems || gridItems.length === 0) {
            $tbody.html(`
                <tr>
                    <td colspan="5" class="text-center text-muted py-3">
                        No data available in table
                    </td>
                </tr>
            `);
            return;
        }

        gridItems.forEach(function (item, index) {
            const rowClass = index === selectedItemIndex ? "table-primary" : "";
            const rowHtml = `
                <tr class="${rowClass}" data-index="${index}" style="cursor: pointer;">
                    <td class="text-center col-select-cell">
                        <input type="checkbox" class="form-check-input m-0 shadow-none temp-chc-check" value="${index}" />
                    </td>
                    <td class="text-center">${item.costingDetailsId || (index + 1).toString().padStart(4, "0")}</td>
                    <td class="text-start ps-2">${item.chchead || ""}</td>
                    <td class="text-start ps-2">${item.expenseTypeName || ""}</td>
                    <td class="text-end pe-2">${item.billAmount ? parseFloat(item.billAmount).toFixed(2) : "0.00"}</td>
                </tr>
            `;
            $tbody.append(rowHtml);
        });

        // Row click to select/edit row (ignoring clicks on the checkbox itself)
        $tbody.find("tr").on("click", function (e) {
            if ($(e.target).is("input[type='checkbox']") || $(e.target).closest(".col-select-cell").length) {
                return;
            }
            const idx = $(this).data("index");
            if (idx !== undefined) {
                selectGridRow(idx);
            }
        });
    }

    function selectGridRow(index) {
        if (index < 0 || index >= gridItems.length) return;

        selectedItemIndex = index;
        const item = gridItems[index];

        setDropdownValue("ddlChcHead", item.chcid);
        setDropdownValue("ddlExpenseType", item.expenseTypeId);
        $("#txtBillAmount").val(item.billAmount);

        renderGrid();
    }

    function addOrUpdateGridRow() {
        const chcid = $("#ddlChcHead").val();
        const chchead = $("#ddlChcHead option:selected").text().replace("--Select CHC Head--", "").trim();
        const expenseTypeId = $("#ddlExpenseType").val();
        const expenseTypeName = $("#ddlExpenseType option:selected").text().replace("--Select Type--", "").trim();
        const billAmountRaw = $("#txtBillAmount").val();
        const billAmount = parseFloat(billAmountRaw);

        if (!chcid) {
            toastr.warning("Please select a CHC Head.");
            $("#ddlChcHead").focus();
            return;
        }

        if (!expenseTypeId) {
            toastr.warning("Please select a Type / Shipment Mode.");
            $("#ddlExpenseType").focus();
            return;
        }

        if (isNaN(billAmount) || billAmount < 0) {
            toastr.warning("Please enter a valid Bill Amount.");
            $("#txtBillAmount").focus();
            return;
        }

        if (selectedItemIndex >= 0 && selectedItemIndex < gridItems.length) {
            gridItems[selectedItemIndex].chcid = chcid;
            gridItems[selectedItemIndex].chchead = chchead;
            gridItems[selectedItemIndex].expenseTypeId = expenseTypeId;
            gridItems[selectedItemIndex].expenseTypeName = expenseTypeName;
            gridItems[selectedItemIndex].billAmount = billAmount;
            toastr.info("Row updated.");
        } else {
            const existingIdx = gridItems.findIndex(x => x.chcid === chcid && x.expenseTypeId === expenseTypeId);
            if (existingIdx >= 0) {
                toastr.warning("This CHC Head with selected Type already exists in the table. Updating rate.");
                gridItems[existingIdx].billAmount = billAmount;
            } else {
                const newDetailId = (gridItems.length + 1).toString().padStart(4, "0");
                gridItems.push({
                    tc: 0,
                    costingDetailsId: newDetailId,
                    costingId: $("#txtCwarId").val(),
                    customerId: $("#ddlCustomer").val(),
                    chcid: chcid,
                    chchead: chchead,
                    expenseTypeId: expenseTypeId,
                    expenseTypeName: expenseTypeName,
                    billAmount: billAmount
                });
            }
        }

        resetGridInputs();
        renderGrid();
    }

    function removeSelectedGridRow() {
        const checkedIndexes = [];
        $("#cwarGridBody .temp-chc-check:checked").each(function () {
            const idx = parseInt($(this).val());
            if (!isNaN(idx)) {
                checkedIndexes.push(idx);
            }
        });

        // Fallback: if no checkbox is checked, but a row is currently selected for edit
        if (checkedIndexes.length === 0 && selectedItemIndex >= 0 && selectedItemIndex < gridItems.length) {
            checkedIndexes.push(selectedItemIndex);
        }

        if (checkedIndexes.length === 0) {
            toastr.warning("Please select row(s) with checkbox to delete.");
            return;
        }

        // Sort descending so splicing doesn't corrupt subsequent indices
        checkedIndexes.sort((a, b) => b - a).forEach(function (idx) {
            if (idx >= 0 && idx < gridItems.length) {
                gridItems.splice(idx, 1);
            }
        });

        gridItems.forEach(function (it, idx) {
            it.costingDetailsId = (idx + 1).toString().padStart(4, "0");
        });

        selectedItemIndex = -1;
        resetGridInputs();
        renderGrid();
        toastr.info(`Removed ${checkedIndexes.length} item(s) from table.`);
    }

    function resetGridInputs() {
        selectedItemIndex = -1;
        clearDropdownValue("ddlChcHead");
        clearDropdownValue("ddlExpenseType");
        $("#txtBillAmount").val("");
        $("#cwarGridBody .temp-chc-check").prop("checked", false);
        $("#cwarGridBody tr").removeClass("table-primary");
    }

    function resetForm() {
        clearDropdownValue("ddlCustomer");

        $("#txtAddress").val("");
        $("#txtPhone").val("");
        $("#txtEmail").val("");
        $("#txtMinAgencyComm").val("");
        $("#txtOnInvoiceValue").val("");
        $("#lblEntryDate").text("-");
        $("#lblModifyDate").text("-");

        gridItems = [];
        resetGridInputs();
        renderGrid();

        // Fetch fresh next CWAR ID
        $.ajax({
            url: "/CustomerWiseApprovalRate/GetNextCostingId",
            type: "GET",
            success: function (res) {
                if (res.success) {
                    $("#txtCwarId").val(res.costingId);
                }
            }
        });
    }

    function initMasterGrid() {
        if ($.gctlGrid && $.gctlGrid.initFromModel) {
            masterDataTable = $.gctlGrid.initFromModel({
                gridId: 'cwarMasterGrid'
            });
        }
    }

    function reloadMasterGrid() {
        if ($.gctlGrid && $.gctlGrid.reload) {
            $.gctlGrid.reload('cwarMasterGrid');
        } else if (masterDataTable && masterDataTable.ajax) {
            masterDataTable.ajax.reload(null, false);
        }
    }

    function saveApprovalRate() {
        const customerId = $("#ddlCustomer").val();
        if (!customerId) {
            toastr.warning("Please select a Customer first.");
            $("#ddlCustomer").focus();
            return;
        }

        const model = {
            customerId: customerId,
            costingId: $("#txtCwarId").val(),
            minimumAgencyCommission: parseFloat($("#txtMinAgencyComm").val()) || 0,
            onInvoiceValue: parseFloat($("#txtOnInvoiceValue").val()) || 0,
            details: gridItems
        };

        $("#btnSave").prop("disabled", true);

        $.ajax({
            url: "/CustomerWiseApprovalRate/Save",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(model),
            success: function (res) {
                $("#btnSave").prop("disabled", false);
                if (res.success) {
                    toastr.success(res.message || "Saved successfully!");
                    loadCustomerData(customerId);
                    reloadMasterGrid();
                } else {
                    toastr.error(res.message || "Failed to save.");
                }
            },
            error: function () {
                $("#btnSave").prop("disabled", false);
                toastr.error("Server error while saving.");
            }
        });
    }

    function deleteApprovalRate() {
        const checkedIds = getCheckedMasterIds();
        const costingId = $("#txtCwarId").val();

        $("#btnConfirmDelete").prop("disabled", true).text("Deleting...");

        let ajaxData;
        let ajaxContentType = "application/x-www-form-urlencoded; charset=UTF-8";

        if (checkedIds.length > 0) {
            ajaxData = JSON.stringify(checkedIds);
            ajaxContentType = "application/json";
        } else {
            ajaxData = { costingId: costingId };
        }

        $.ajax({
            url: "/CustomerWiseApprovalRate/Delete",
            type: "POST",
            contentType: ajaxContentType,
            data: ajaxData,
            success: function (res) {
                $("#btnConfirmDelete").prop("disabled", false).text("Delete");
                $("#deleteConfirmModal").modal("hide");
                if (res.success) {
                    toastr.success(res.message || "Deleted successfully!");
                    resetForm();
                    reloadMasterGrid();
                } else {
                    toastr.error(res.message || "Failed to delete.");
                }
            },
            error: function () {
                $("#btnConfirmDelete").prop("disabled", false).text("Delete");
                $("#deleteConfirmModal").modal("hide");
                toastr.error("Server error while deleting.");
            }
        });
    }

})(jQuery);
