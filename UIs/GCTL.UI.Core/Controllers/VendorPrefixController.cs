using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.VendorPrefix;
using GCTL.Service.Common;
using GCTL.Service.VendorPrefix;
using GCTL.UI.Core.ViewModels.VendorPrefix;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class VendorPrefixController : BaseController
    {
        #region Service & Repository
        public readonly IVendorPrefixService vendorPrefixService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public VendorPrefixController(
            IVendorPrefixService vendorPrefixService, 
            ICommonService commonService
            
            )
        {
            this.vendorPrefixService = vendorPrefixService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await vendorPrefixService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new VendorPrefixPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await vendorPrefixService.GetAllAsync();
                model.VendorPrefixList = list.Any() ? list : new List<VendorPrefixSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "VendorPrifixId", "CF_Def_VendorPrefix", 4);

                model.Setup = new VendorPrefixSetupViewModel
                {
                    VendorPrifixId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.VendorPrefixList = new List<VendorPrefixSetupViewModel>();
                model.Setup = new VendorPrefixSetupViewModel();
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
            VendorPrefixSetupViewModel model = new VendorPrefixSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "VendorPrifixId", "CF_Def_VendorPrefix", 4);

            if (!string.IsNullOrEmpty(id))
            {

                model = await vendorPrefixService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.VendorPrifixId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(VendorPrefixSetupViewModel modelVM)
        {
            try
            {

                if (await vendorPrefixService.IsExistAsync(modelVM.PrifixName, modelVM.VendorPrifixId))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }


                if (!ModelState.IsValid)
                {

                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.Tc > 0);
                if (modelVM.Tc == 0 || modelVM.Tc == null)
                {
                    var hasSavePermission = await vendorPrefixService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await vendorPrefixService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.VendorPrifixId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await vendorPrefixService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await vendorPrefixService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.VendorPrifixId });
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

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { success = false, message = "No IDs provided for delete." });
            }

            var hasPermission = await vendorPrefixService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {

                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await vendorPrefixService.DeleteTab(ids, model);
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

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await vendorPrefixService.GetAllAsync();
                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

    }
}
