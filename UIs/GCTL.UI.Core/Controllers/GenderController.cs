using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Gender;
using GCTL.Service.Common;
using GCTL.Service.Gender;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Gender;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class GenderController : BaseController
    {
        #region Ser & Repo
        private readonly IGenderService genderService;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;
        public GenderController(
                                ICommonService commonService,
                                IGenderService genderService
                               )
        {
            this.genderService = genderService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false)
        {
            var hasPermission = await genderService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new GenderPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await genderService.GetAllAsync();
                model.GenderList = list ?? new List<GenderSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "SexCode", "HRM_Def_Sex", 1);
                model.Setup = new GenderSetupViewModel
                {
                    SexCode = strMaxNO
                };
            }
            catch (Exception ex)
            {

                model.GenderList = new List<GenderSetupViewModel>();
                model.Setup = new GenderSetupViewModel();
                Console.WriteLine("Error" + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            GenderSetupViewModel model = new GenderSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "SexCode", "HRM_Def_Sex", 1);

            if (!string.IsNullOrEmpty(id))
            {

                model = await genderService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.SexCode = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(GenderSetupViewModel modelVM)
        {
            try
            {
                if (await genderService.IsExistAsync(modelVM.Sex, modelVM.SexCode))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }

                if (!ModelState.IsValid)
                {

                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    var hasSavePermission = await genderService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await genderService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.SexCode });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await genderService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await genderService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.SexCode });
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
        public async Task<JsonResult> CheckAvailability(string name, string code)
        {
            if (await genderService.IsExistAsync(name, code))
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

            var hasPermission = await genderService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await genderService.DeleteTab(ids,model);
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
                var list = await genderService.GetAllAsync();
                return PartialView("_Grid", list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion
    }
}
