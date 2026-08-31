using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SupplierCategory;
using GCTL.Service.Common;
using GCTL.Service.SupplierCategory;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.SupplierCategory;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class SupplierCategoryController : BaseController
    {
        #region Service & Repository
        public readonly ISupplierCategoryService supplierCategoryService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public SupplierCategoryController(
            ISupplierCategoryService supplierCategoryService,
            ICommonService commonService
            
            )
        {
            this.supplierCategoryService = supplierCategoryService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await supplierCategoryService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new SupplierCategoryPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await supplierCategoryService.GetAllAsync() ?? new List<SupplierCategorySetupViewModel>();
                model.SupplierCategoryList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.SupplierCategoryId != null && x.SupplierCategoryId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.SupplierCategory != null && x.SupplierCategory.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Supplier Category" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                { "SupplierCategoryId", item.SupplierCategoryId },
                { "Supplier Category", item.SupplierCategory }
                    },
                    ajaxUrl: Url.Action("GetTableData", "SupplierCategory") ?? "/SupplierCategory/GetTableData",
                    primaryKey: "SupplierCategoryId",
                    hasCheckbox: true,
                    actionUrlPattern: "/SupplierCategory/Setup/{id}",
                    gridId: "supplierCategoryGridContainer",
                    editClass: "js-SupplierCategory-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                { "SupplierCategoryId", "center" },
                { "Supplier Category", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                { "SelectAll", "120px" },
                { "Code", "100px" },
                { "Supplier Category", "auto" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "SupplierCategoryID", "Inv_Def_SupplierCategory", 3);

                model.Setup = new SupplierCategorySetupViewModel
                {
                    SupplierCategoryId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.SupplierCategoryList = new List<SupplierCategorySetupViewModel>();
                model.Setup = new SupplierCategorySetupViewModel();
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
            var model = new SupplierCategorySetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "SupplierCategoryID", "Inv_Def_SupplierCategory", 3);

            if (!string.IsNullOrEmpty(id) && id != "0")
            {
                var existing = await supplierCategoryService.GetByIdAsync(id);
                if (existing != null)
                {
                    model = existing;
                }
                else
                {
                    model.SupplierCategoryId = strMaxNO;
                }
            }
            else
            {
                model.SupplierCategoryId = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_Setup", model);
            }

            var pageModel = new SupplierCategoryPageViewModel
            {
                Setup = model,
                PageUrl = Url.Action(nameof(Index))
            };

            var list = await supplierCategoryService.GetAllAsync() ?? new List<SupplierCategorySetupViewModel>();
            int totalRecords = list.Count;
            var pagedList = list.Take(10).ToList();

            pageModel.DynamicGrid = GridBuilder.BuildGrid(
                data: pagedList,
                totalRecords: totalRecords,
                currentPage: 1,
                pageSize: 10,
                headers: new List<string> { "Code", "Supplier Category" },
                rowMapper: item => new Dictionary<string, object?>
                {
            { "SupplierCategoryId", item.SupplierCategoryId },
            { "Supplier Category", item.SupplierCategory }
                },
                ajaxUrl: Url.Action("GetTableData", "SupplierCategory") ?? "/SupplierCategory/GetTableData",
                primaryKey: "SupplierCategoryId",
                hasCheckbox: true,
                actionUrlPattern: "/SupplierCategory/Setup/{id}",
                gridId: "supplierCategoryGridContainer",
                editClass: "js-SupplierCategory-edit",
                columnAlignments: new Dictionary<string, string>
                {
            { "SupplierCategoryId", "center" },
            { "Supplier Category", "left" }
                },
                columnWidths: new Dictionary<string, string>
                {
            { "SelectAll", "120px" },
            { "Code", "100px" },
            { "Supplier Category", "auto" }
                }
            );

            return View("Index", pageModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(SupplierCategorySetupViewModel modelVM)
        {
            try
            {
                if (modelVM.SupplierCategoryCode == 0 || modelVM.SupplierCategoryCode == null)
                {
                    if (await supplierCategoryService.IsExistAsync(modelVM.SupplierCategory, modelVM.SupplierCategoryId))
                    {
                        return Json(new { isSuccess = false, message = "Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.SupplierCategoryCode > 0);

                if (modelVM.SupplierCategoryCode == 0 || modelVM.SupplierCategoryCode == null)
                {
                    var hasSavePermission = await supplierCategoryService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (!hasSavePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }

                    bool result = await supplierCategoryService.SaveAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierCategoryID", "Inv_Def_SupplierCategory", 3);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to save Supplier Category." });
                }
                else
                {
                    var hasUpdatePermission = await supplierCategoryService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (!hasUpdatePermission)
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }

                    bool result = await supplierCategoryService.UpdateAsync(modelVM);
                    if (result)
                    {
                        commonService.FindMaxNo(ref strMaxNO, "SupplierCategoryID", "Inv_Def_SupplierCategory", 3);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = strMaxNO });
                    }
                    return Json(new { isSuccess = false, message = "Failed to update Supplier Category." });
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

            var hasPermission = await supplierCategoryService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {

                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await supplierCategoryService.DeleteTab(ids, model);
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
                var list = await supplierCategoryService.GetAllAsync() ?? new List<SupplierCategorySetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<SupplierCategorySetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region Chake Degian

        //public IActionResult Index()
        //{
        //    SupplierCategoryPageViewModel model = new SupplierCategoryPageViewModel
        //    {
        //        Setup = new SupplierCategorySetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion

    }
}
