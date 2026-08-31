using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierType;
using GCTL.Service.Common;
using GCTL.Service.SupplierType;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.SupplierType;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class SupplierTypeController : BaseController
    {
        #region Service & Repository
        public readonly ISupplierTypeService supplierTypeService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public SupplierTypeController(
            ISupplierTypeService supplierTypeService,
            ICommonService commonService
            
            )
        {
            this.supplierTypeService = supplierTypeService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await supplierTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new SupplierTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await supplierTypeService.GetAllAsync() ?? new List<SupplierTypeSetupViewModel>();
                model.SupplierTypeList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.SupplierTypeId != null && x.SupplierTypeId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.SupplierTypeName != null && x.SupplierTypeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Supplier Type" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                { "SupplierTypeId", item.SupplierTypeId },
                { "Supplier Type", item.SupplierTypeName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "SupplierType") ?? "/SupplierType/GetTableData",
                    primaryKey: "SupplierTypeId",
                    hasCheckbox: true,
                    actionUrlPattern: "/SupplierType/Setup/{id}",
                    gridId: "supplierTypeGridContainer",
                    editClass: "js-SupplierType-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                { "SupplierTypeId", "center" },
                { "Supplier Type", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                { "SelectAll", "120px" },
                { "Code", "100px" },
                { "Supplier Type", "auto" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "SupplierTypeID", "Inv_Def_SupplierType", 3);

                model.Setup = new SupplierTypeSetupViewModel
                {
                    SupplierTypeId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.SupplierTypeList = new List<SupplierTypeSetupViewModel>();
                model.Setup = new SupplierTypeSetupViewModel();
                Console.WriteLine("Error: " + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            var model = new SupplierTypeSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "SupplierTypeID", "Inv_Def_SupplierType", 3);

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await supplierTypeService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.SupplierTypeId = strMaxNO;
                }
            }
            else
            {
                model.SupplierTypeId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new SupplierTypePageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await supplierTypeService.GetAllAsync() ?? new List<SupplierTypeSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Supplier Type" },
                rowMapper: item => new Dictionary<string, object?>
                {
            { "SupplierTypeId", item.SupplierTypeId },
            { "Supplier Type", item.SupplierTypeName }
                },
                ajaxUrl: Url.Action("GetTableData", "SupplierType") ?? "/SupplierType/GetTableData",
                primaryKey: "SupplierTypeId",
                hasCheckbox: true,
                actionUrlPattern: "/SupplierType/Setup/{id}",
                gridId: "supplierTypeGridContainer",
                editClass: "js-SupplierType-edit",
                columnAlignments: new Dictionary<string, string>
                {
            { "SupplierTypeId", "center" },
            { "Supplier Type", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
            { "SelectAll", "120px" },
            { "Code", "100px" },
            { "Supplier Type", "auto" }
                }
            );

            return View("Index", pageModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(SupplierTypeSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.SupplierTypeCode == 0 || modelVM.SupplierTypeCode == null)
                {
                    if (await supplierTypeService.IsExistAsync(modelVM.SupplierTypeName, modelVM.SupplierTypeId))
                    {
                        return Json(new { isSuccess = false, message = "Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.SupplierTypeCode > 0);

                if (modelVM.SupplierTypeCode == 0 || modelVM.SupplierTypeCode == null)
                {
                    var hasSavePermission = await supplierTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }

                    bool result = await supplierTypeService.SaveAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierTypeID", "Inv_Def_SupplierType", 3);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Supplier Type." });
                }
                else
                {
                    var hasUpdatePermission = await supplierTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }

                    bool result = await supplierTypeService.UpdateAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierTypeID", "Inv_Def_SupplierType", 3);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Supplier Type." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { isSuccess = false, message = "An error occurred while saving." });
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { success = false, message = "No IDs provided for delete." });
            }

            var hasPermission = await supplierTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await supplierTypeService.DeleteTab(ids, model);
            if (success.succses)
            {
                return Json(new { success = true, message = "Deleted Successfully." });
            }
            else
            {
                return Json(new { success = false, message = success.messege });
            }
        }

        #endregion

        #region TabeleLodaing

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await supplierTypeService.GetAllAsync() ?? new List<SupplierTypeSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<SupplierTypeSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

    }
}
