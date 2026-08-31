using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierOrigin;
using GCTL.Service.Common;
using GCTL.Service.SupplierOrigin;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.SupplierOrigin;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class SupplierOriginController : BaseController
    {
        #region Service & Repository
        public readonly ISupplierOriginService supplierOriginService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public SupplierOriginController(
            ISupplierOriginService supplierOriginService,
            ICommonService commonService
            
            )
        {
            this.supplierOriginService = supplierOriginService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await supplierOriginService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new SupplierOriginPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await supplierOriginService.GetAllAsync() ?? new List<SupplierOriginSetupViewModel>();
                model.SupplierOriginList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.SupplierOriginId != null && x.SupplierOriginId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.SupplierOrigin != null && x.SupplierOrigin.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Supplier Origin" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                { "SupplierOriginId", item.SupplierOriginId },
                { "Supplier Origin", item.SupplierOrigin }
                    },
                    ajaxUrl: Url.Action("GetTableData", "SupplierOrigin") ?? "/SupplierOrigin/GetTableData",
                    primaryKey: "SupplierOriginId",
                    hasCheckbox: true,
                    actionUrlPattern: "/SupplierOrigin/Setup/{id}",
                    gridId: "supplierOriginGridContainer",
                    editClass: "js-SupplierOrigin-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                { "SupplierOriginId", "center" },
                { "Supplier Origin", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                { "SelectAll", "120px" },
                { "Code", "100px" },
                { "Supplier Origin", "auto" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "SupplierOriginID", "Inv_Def_SupplierOrigin", 3);

                model.Setup = new SupplierOriginSetupViewModel
                {
                    SupplierOriginId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.SupplierOriginList = new List<SupplierOriginSetupViewModel>();
                model.Setup = new SupplierOriginSetupViewModel();
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
            var model = new SupplierOriginSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "SupplierOriginID", "Inv_Def_SupplierOrigin", 3);

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await supplierOriginService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.SupplierOriginId = strMaxNO;
                }
            }
            else
            {
                model.SupplierOriginId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new SupplierOriginPageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await supplierOriginService.GetAllAsync() ?? new List<SupplierOriginSetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Supplier Origin" },
                rowMapper: item => new Dictionary<string, object?>
                {
            { "SupplierOriginId", item.SupplierOriginId },
            { "Supplier Origin", item.SupplierOrigin }
                },
                ajaxUrl: Url.Action("GetTableData", "SupplierOrigin") ?? "/SupplierOrigin/GetTableData",
                primaryKey: "SupplierOriginId",
                hasCheckbox: true,
                actionUrlPattern: "/SupplierOrigin/Setup/{id}",
                gridId: "supplierOriginGridContainer",
                editClass: "js-SupplierOrigin-edit",
                columnAlignments: new Dictionary<string, string>
                {
            { "SupplierOriginId", "center" },
            { "Supplier Origin", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
            { "SelectAll", "120px" },
            { "Code", "100px" },
            { "Supplier Origin", "auto" }
                }
            );

            return View("Index", pageModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(SupplierOriginSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.Tc == 0 || modelVM.Tc == null)
                {
                    if (await supplierOriginService.IsExistAsync(modelVM.SupplierOrigin, modelVM.SupplierOriginId))
                    {
                        return Json(new { isSuccess = false, message = "Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.Tc > 0);

                if (modelVM.Tc == 0 || modelVM.Tc == null)
                {
                    var hasSavePermission = await supplierOriginService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }

                    bool result = await supplierOriginService.SaveAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierOriginID", "Inv_Def_SupplierOrigin", 3);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Supplier Origin." });
                }
                else
                {
                    var hasUpdatePermission = await supplierOriginService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }

                    bool result = await supplierOriginService.UpdateAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierOriginID", "Inv_Def_SupplierOrigin", 3);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Supplier Origin." });
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

            var hasPermission = await supplierOriginService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;


            var success = await supplierOriginService.DeleteTab(ids, model);
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

        #region Table Loading (Custom DataTable)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await supplierOriginService.GetAllAsync() ?? new List<SupplierOriginSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<SupplierOriginSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

    }
}
