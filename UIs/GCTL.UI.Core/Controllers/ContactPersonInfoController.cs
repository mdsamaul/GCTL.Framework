using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.ContactPersonInfo;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortName;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.ContactPersonInfo;
using GCTL.Service.PortName;
using GCTL.UI.Core.ViewModels.ContactPersonInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class ContactPersonInfoController : BaseController
    {
        #region Service & Repository
        public readonly IContactPersonInfoService contactPersonInfoService;
        private readonly IRepository<HrmDefDesignation> designationrepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public ContactPersonInfoController(
            IContactPersonInfoService contactPersonInfoService, 
            IRepository<HrmDefDesignation> designationrepository, 
            ICommonService commonService
            
            )
        {
            this.contactPersonInfoService = contactPersonInfoService;
            this.designationrepository = designationrepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await contactPersonInfoService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ContactPersonInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await contactPersonInfoService.GetAllAsync();
                model.ContactPersonList = list ?? new List<ContactPersonInfoSetupViewModel>();
                ViewBag.DesignationDD = new SelectList(designationrepository.All(), "DesignationCode", "DesignationName");

                model.Setup = new ContactPersonInfoSetupViewModel
                {
                    // Cpid = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.ContactPersonList = new List<ContactPersonInfoSetupViewModel>();
                model.Setup = new ContactPersonInfoSetupViewModel();
                Console.WriteLine("Error" + ex.Message);
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
            ContactPersonInfoSetupViewModel model = new ContactPersonInfoSetupViewModel();
            var nextCode = commonService.GenerateCode("CPID", "Sales_ContactPerson", "CP", 5);

            if (!string.IsNullOrEmpty(id))
            {
                model = await contactPersonInfoService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.Cpid = strMaxNO;
            }

            ViewBag.DesignationDD = new SelectList(designationrepository.All(), "DesignationCode", "DesignationName");

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(ContactPersonInfoSetupViewModel modelVM)
        {
            try
            {

                if (await contactPersonInfoService.IsExistAsync(modelVM.ContactPersonName, modelVM.Cpid))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }


                if (!ModelState.IsValid)
                {

                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                modelVM.CompanyCode = LoginInfo.CompanyCode;
                if (modelVM.AutoId == 0)
                {
                    var hasSavePermission = await contactPersonInfoService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await contactPersonInfoService.SaveAsync(modelVM);
                      //  var nextCode = commonService.GenerateCode("CPID", "Sales_ContactPerson", "CP", 3);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.Cpid });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await contactPersonInfoService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await contactPersonInfoService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.Cpid });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                }   }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                return RedirectToAction("Login", "Accounts");

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

            var hasPermission = await contactPersonInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await contactPersonInfoService.DeleteTab(ids, model);
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

        #region Tabele Lodaing

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await contactPersonInfoService.GetAllAsync() ?? new List<ContactPersonInfoSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<ContactPersonInfoSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region GenerateNewId

        public async Task<IActionResult> GenerateNewId()
        {
            await Task.Delay(100);
            var nextCode = commonService.GenerateNextCode("CPID", "Sales_ContactPerson", 5, "CP");
            return Json(nextCode);
        }

        #endregion

    }
}
