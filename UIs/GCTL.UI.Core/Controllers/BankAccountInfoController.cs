using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.CoreBankAccountInformations;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.CoreBankAccountInformations;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.CoreBankAccountInformations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class BankAccountInfoController : BaseController
    {
        private readonly ICommonService commonService;
        private readonly IRepository<SalesDefBankInfo> bankRepository;
        private readonly ICoreBankAccountInformationService bankAccountInformationRepository;
        private readonly IRepository<SalesDefBankBranchInfo> branchBankRepository;
        string maxNo = "";

        public BankAccountInfoController(
            ICommonService commonService,
            IRepository<SalesDefBankBranchInfo> branchBankRepository,
            IRepository<SalesDefBankInfo> bankRepository,
            ICoreBankAccountInformationService bankAccountInformationRepository
        )
        {
            this.commonService = commonService;
            this.bankRepository = bankRepository;
            this.bankAccountInformationRepository = bankAccountInformationRepository;
            this.branchBankRepository = branchBankRepository;
        }

        private async Task PopulateDropdowns(CoreBankAccountInformationPageViewModel model, string? selectedBankId = null)
        {
            var banks = await bankRepository.GetAllAsync();
            model.BankDD = banks.Select(x => new SelectListItem
            {
                Value = x.BankId,
                Text = x.BankName,
                Selected = x.BankId == selectedBankId
            }).ToList();
            ViewBag.BankDD = new SelectList(model.BankDD, "Value", "Text", selectedBankId);

            if (!string.IsNullOrEmpty(selectedBankId))
            {
                var branches = await branchBankRepository.All().Where(x => x.BankId == selectedBankId).ToListAsync();
                model.BranchDD = branches.Select(x => new SelectListItem
                {
                    Value = x.BankBranchId,
                    Text = x.BankBranchName
                }).ToList();
            }
            else
            {
                var branches = await branchBankRepository.GetAllAsync();
                model.BranchDD = branches.Select(x => new SelectListItem
                {
                    Value = x.BankBranchId,
                    Text = x.BankBranchName
                }).ToList();
            }
            ViewBag.BranchDD = new SelectList(model.BranchDD, "Value", "Text");
        }

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await bankAccountInformationRepository.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new CoreBankAccountInformationPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            await PopulateDropdowns(model);

            try
            {
                var list = await bankAccountInformationRepository.GetAllAsync() ?? new List<CoreBankAccountInformationSetupViewModel>();
                model.TableList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.AccInfoId != null && x.AccInfoId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.AccountName != null && x.AccountName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.AccountNo != null && x.AccountNo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankName != null && x.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BranchName != null && x.BranchName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Account Name", "Account No", "Bank Name", "Branch Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "AccInfoId", item.AccInfoId },
                        { "Account Name", item.AccountName },
                        { "Account No", item.AccountNo },
                        { "Bank Name", item.BankName },
                        { "Branch Name", item.BranchName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankAccountInfo") ?? "/BankAccountInfo/GetTableData",
                    primaryKey: "AccInfoId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankAccountInfo/Setup/{id}",
                    gridId: "bankAccountInfoGridContainer",
                    editClass: "js-bankaccountinfo-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "AccInfoId", "center" },
                        { "Account Name", "left" },
                        { "Account No", "left" },
                        { "Bank Name", "left" },
                        { "Branch Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Account Name", "180px" },
                        { "Account No", "150px" },
                        { "Bank Name", "160px" },
                        { "Branch Name", "auto" }
                    }
                );

                commonService.FindMaxNo(ref maxNo, "AccInfoID", "Core_BankAccountInformation", 2);
                model.Setup = new CoreBankAccountInformationSetupViewModel
                {
                    AccInfoId = maxNo
                };
            }
            catch (Exception ex)
            {
                model.TableList = new List<CoreBankAccountInformationSetupViewModel>();
                model.Setup = new CoreBankAccountInformationSetupViewModel();
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
            CoreBankAccountInformationSetupViewModel model = new CoreBankAccountInformationSetupViewModel();
            commonService.FindMaxNo(ref maxNo, "AccInfoID", "Core_BankAccountInformation", 2);

            if (!string.IsNullOrEmpty(id))
            {
                model = await bankAccountInformationRepository.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.AccInfoId = maxNo;
            }

            var banks = await bankRepository.GetAllAsync();
            ViewBag.BankDD = new SelectList(banks, "BankId", "BankName", model?.BankId);

            if (!string.IsNullOrEmpty(model?.BankId))
            {
                var branches = await branchBankRepository.All().Where(x => x.BankId == model.BankId).ToListAsync();
                ViewBag.BranchDD = new SelectList(branches, "BankBranchId", "BankBranchName", model?.BranchId);
            }
            else
            {
                var branches = await branchBankRepository.GetAllAsync();
                ViewBag.BranchDD = new SelectList(branches, "BankBranchId", "BankBranchName", model?.BranchId);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // Direct browser URL navigation
            var hasPermission = await bankAccountInformationRepository.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new CoreBankAccountInformationPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            await PopulateDropdowns(indexModel, model?.BankId);

            try
            {
                var list = await bankAccountInformationRepository.GetAllAsync() ?? new List<CoreBankAccountInformationSetupViewModel>();
                indexModel.TableList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Account Name", "Account No", "Bank Name", "Branch Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "AccInfoId", item.AccInfoId },
                        { "Account Name", item.AccountName },
                        { "Account No", item.AccountNo },
                        { "Bank Name", item.BankName },
                        { "Branch Name", item.BranchName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankAccountInfo") ?? "/BankAccountInfo/GetTableData",
                    primaryKey: "AccInfoId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankAccountInfo/Setup/{id}",
                    gridId: "bankAccountInfoGridContainer",
                    editClass: "js-bankaccountinfo-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "AccInfoId", "center" },
                        { "Account Name", "left" },
                        { "Account No", "left" },
                        { "Bank Name", "left" },
                        { "Branch Name", "left" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Account Name", "180px" },
                        { "Account No", "150px" },
                        { "Bank Name", "160px" },
                        { "Branch Name", "auto" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.TableList = new List<CoreBankAccountInformationSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CoreBankAccountInformationSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null || modelVM.Tc == 0)
                {
                    if (await bankAccountInformationRepository.IsExistAsync(modelVM.AccountName, modelVM.AccInfoId, modelVM.AccountNo, modelVM.BankId ?? "", modelVM.BranchId ?? ""))
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
                    var hasSavePermission = await bankAccountInformationRepository.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await bankAccountInformationRepository.SaveAsync(modelVM, LoginInfo.CompanyCode);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.AccInfoId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await bankAccountInformationRepository.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await bankAccountInformationRepository.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.AccInfoId });
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
        public async Task<JsonResult> CheckAvailability(string accountName, string typeCode, string accountNo, string bankId, string branchId)
        {
            if (await bankAccountInformationRepository.IsExistAsync(accountName, typeCode, accountNo, bankId, branchId))
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

            var hasPermission = await bankAccountInformationRepository.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await bankAccountInformationRepository.DeleteTab(ids, model);
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

        #region Table Loading (Dynamic Grid)

        [HttpGet]
        public async Task<IActionResult> GetTableData(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var list = await bankAccountInformationRepository.GetAllAsync() ?? new List<CoreBankAccountInformationSetupViewModel>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.AccInfoId != null && x.AccInfoId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.AccountName != null && x.AccountName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.AccountNo != null && x.AccountNo.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BankName != null && x.BankName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BranchName != null && x.BranchName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var gridModel = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Account Name", "Account No", "Bank Name", "Branch Name" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "AccInfoId", item.AccInfoId },
                        { "Account Name", item.AccountName },
                        { "Account No", item.AccountNo },
                        { "Bank Name", item.BankName },
                        { "Branch Name", item.BranchName }
                    },
                    ajaxUrl: Url.Action("GetTableData", "BankAccountInfo") ?? "/BankAccountInfo/GetTableData",
                    primaryKey: "AccInfoId",
                    hasCheckbox: true,
                    actionUrlPattern: "/BankAccountInfo/Setup/{id}",
                    gridId: "bankAccountInfoGridContainer",
                    editClass: "js-bankaccountinfo-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "AccInfoId", "center" },
                        { "Account Name", "left" },
                        { "Account No", "left" },
                        { "Bank Name", "left" },
                        { "Branch Name", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Account Name", "180px" },
                        { "Account No", "150px" },
                        { "Bank Name", "160px" },
                        { "Branch Name", "auto" }
                    }
                );

                return PartialView("_DynamicGrid", gridModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Branch Filtering according to bank
        [HttpGet]
        public IActionResult GetBranchesByBankId(string bankId)
        {
            var branches = branchBankRepository.All()
                .Where(x => x.BankId == bankId)
                .Select(x => new
                {
                    value = x.BankBranchId,
                    text = x.BankBranchName,
                    address = x.Address
                }).ToList();

            return Json(branches);
        }
        #endregion
    }
}
