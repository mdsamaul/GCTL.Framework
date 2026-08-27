using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.SalesDefBankBranchInfos;
using GCTL.Data.Models;
using GCTL.Service.BankBranchInformations;
using GCTL.Service.Common;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.SalesDefBankBranchInfos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class BankBranchInformationsController : BaseController
    {
        #region Properties
        private readonly ISalesDefBankBranchInfosService bankBranchInfosService;
        private readonly ICommonService commonService;
        private readonly IRepository<SalesDefBankInfo> bankRepository;
        string strMaxNO = string.Empty;

        public BankBranchInformationsController(
            ISalesDefBankBranchInfosService bankBranchInfosService,
            IRepository<SalesDefBankInfo> bankRepository,
            ICommonService commonService
        )
        {
            this.bankBranchInfosService = bankBranchInfosService;
            this.bankRepository = bankRepository;
            this.commonService = commonService;
        }

        private async Task PopulateDropdowns(SalesDefBankBranchInfoPageViewModel model)
        {
            var banks = await bankRepository.GetAllAsync();
            model.BankDD = banks.Select(x => new SelectListItem
            {
                Value = x.BankId,
                Text = x.BankName
            }).ToList();

            ViewBag.BankDD = new SelectList(model.BankDD, "Value", "Text");
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await bankBranchInfosService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new SalesDefBankBranchInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            await PopulateDropdowns(model);

            try
            {
                var list = await bankBranchInfosService.GetAllAsync() ?? new List<SalesDefBankBranchInfoSetupViewModel>();
                model.TableList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.BankBranchId != null && x.BankBranchId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankName != null && x.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankBranchName != null && x.BankBranchName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.ShortName != null && x.ShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.Swiftcode != null && x.Swiftcode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.Address != null && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.Phone != null && x.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Bank Name", "Branch Name", "Short Name", "Swift Code", "Address", "Phone" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BankBranchId", item.BankBranchId },
                        { "Bank Name", item.BankName },
                        { "Branch Name", item.BankBranchName },
                        { "Short Name", item.ShortName },
                        { "Swift Code", item.Swiftcode },
                        { "Address", item.Address },
                        { "Phone", item.Phone }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankBranchInformations") ?? "/BankBranchInformations/GetTableData",
                    primaryKey: "BankBranchId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankBranchInformations/Setup/{id}",
                    gridId: "bankBranchInformationGridContainer",
                    editClass: "js-bankbranchinformation-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BankBranchId", "center" },
                        { "Bank Name", "left" },
                        { "Branch Name", "left" },
                        { "Short Name", "left" },
                        { "Swift Code", "center" },
                        { "Address", "left" },
                        { "Phone", "center" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "20px" },
                        { "Bank Name", "150px" },
                        { "Branch Name", "auto" },
                        { "Short Name", "120px" },
                        { "Swift Code", "120px" },
                        { "Address", "180px" },
                        { "Phone", "130px" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "BankBranchId", "SALES_Def_BankBranchInfo", 2);
                model.Setup = new SalesDefBankBranchInfoSetupViewModel
                {
                    BankBranchId = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.TableList = new List<SalesDefBankBranchInfoSetupViewModel>();
                model.Setup = new SalesDefBankBranchInfoSetupViewModel();
                Console.WriteLine("Error: " + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            SalesDefBankBranchInfoSetupViewModel model = new SalesDefBankBranchInfoSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "BankBranchId", "SALES_Def_BankBranchInfo", 2);

            if (!string.IsNullOrEmpty(id))
            {
                model = await bankBranchInfosService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.BankBranchId = strMaxNO;
            }

            var banks = await bankRepository.GetAllAsync();
            ViewBag.BankDD = new SelectList(banks, "BankId", "BankName", model?.BankId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // Direct browser URL navigation
            var hasPermission = await bankBranchInfosService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new SalesDefBankBranchInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            await PopulateDropdowns(indexModel);

            try
            {
                var list = await bankBranchInfosService.GetAllAsync() ?? new List<SalesDefBankBranchInfoSetupViewModel>();
                indexModel.TableList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Bank Name", "Branch Name", "Short Name", "Swift Code", "Address", "Phone" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "BankBranchId", item.BankBranchId },
                        { "Bank Name", item.BankName },
                        { "Branch Name", item.BankBranchName },
                        { "Short Name", item.ShortName },
                        { "Swift Code", item.Swiftcode },
                        { "Address", item.Address },
                        { "Phone", item.Phone }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankBranchInformations") ?? "/BankBranchInformations/GetTableData",
                    primaryKey: "BankBranchId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankBranchInformations/Setup/{id}",
                    gridId: "bankBranchInformationGridContainer",
                    editClass: "js-bankbranchinformation-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "BankBranchId", "center" },
                        { "Bank Name", "left" },
                        { "Branch Name", "left" },
                        { "Short Name", "left" },
                        { "Swift Code", "center" },
                        { "Address", "left" },
                        { "Phone", "center" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Bank Name", "150px" },
                        { "Branch Name", "auto" },
                        { "Short Name", "120px" },
                        { "Swift Code", "120px" },
                        { "Address", "180px" },
                        { "Phone", "130px" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.TableList = new List<SalesDefBankBranchInfoSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(SalesDefBankBranchInfoSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null || modelVM.Tc == 0)
                {
                    if (await bankBranchInfosService.IsExistAsync(modelVM.BankBranchName, modelVM.BankBranchId, modelVM.BankId))
                    {
                        return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, (modelVM.AutoId > 0 || modelVM.Tc > 0));
                if (modelVM.AutoId == 0 || modelVM.AutoId == null || modelVM.Tc == 0)
                {
                    var hasSavePermission = await bankBranchInfosService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await bankBranchInfosService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.BankBranchId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await bankBranchInfosService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await bankBranchInfosService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.BankBranchId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                return RedirectToAction("Login", "Accounts");
            }
        }

        #endregion

        #region CheckAvailability

        [HttpPost]
        public async Task<JsonResult> CheckAvailability(string name, string typeCode, string bankId)
        {
            if (await bankBranchInfosService.IsExistAsync(name, typeCode, bankId))
            {
                return Json(new { isSuccess = true, message = $"Already Exists!" });
            }

            return Json(new { isSuccess = false });
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

            var hasPermission = await bankBranchInfosService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await bankBranchInfosService.DeleteTab(ids, model);
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
                var list = await bankBranchInfosService.GetAllAsync() ?? new List<SalesDefBankBranchInfoSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<SalesDefBankBranchInfoSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

    }
}
