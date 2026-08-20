using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Nationalitys;
using GCTL.Service.Common;
using GCTL.Service.Nationalitys;
using GCTL.Service.SupplierType;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Nationalitys;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class NationalitysController : BaseController
    {
        #region Service & Rep
        private readonly INationalitysService nationalitysService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public NationalitysController(
            INationalitysService nationalitysService, ICommonService commonService
            )
        {
            this.nationalitysService = nationalitysService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false)
        {
            var hasPermission = await nationalitysService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new NationalitysPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await nationalitysService.GetAllAsync();
                model.NationalitysList = list ?? new List<NationalitysSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "NationalityCode", "HRM_Def_Nationality", 2);
                model.Setup = new NationalitysSetupViewModel
                {
                    NationalityCode = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.NationalitysList = new List<NationalitysSetupViewModel>();
                model.Setup = new NationalitysSetupViewModel();
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
            NationalitysSetupViewModel model = new NationalitysSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "NationalityCode", "HRM_Def_Nationality", 2);

            if (!string.IsNullOrEmpty(id))
            {

                model = await nationalitysService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.NationalityCode = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(NationalitysSetupViewModel modelVM)
        {
            try
            {

                if (await nationalitysService.IsExistAsync(modelVM.Nationality, modelVM.NationalityCode))
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
                    var hasSavePermission = await nationalitysService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await nationalitysService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.NationalityCode });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await nationalitysService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await nationalitysService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.NationalityCode });
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
            if (await nationalitysService.IsExistAsync(name, code))
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

            var hasPermission = await nationalitysService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await nationalitysService.DeleteTab(ids, model);
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
                var list = await nationalitysService.GetAllAsync();
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
