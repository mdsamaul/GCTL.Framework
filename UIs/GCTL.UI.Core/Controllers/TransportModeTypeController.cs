using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportModeType;
using GCTL.Service.Common;
using GCTL.Service.ModeType;
using GCTL.Service.TransportType;
using GCTL.UI.Core.ViewModels.TransportModeType;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class TransportModeTypeController : BaseController
    {
        #region Service & Repository
        public readonly IModeTypeService modeTypeService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public TransportModeTypeController(
            IModeTypeService modeTypeService,
            ICommonService commonService
        )
        {
            this.modeTypeService = modeTypeService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await modeTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new TransportModeTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await modeTypeService.GetAllAsync();
                model.TransportModeTypeList = list ?? new List<TransportModeTypeSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "TransportModeId", "Transport_Mode_Type", 3);

                model.Setup = new TransportModeTypeSetupViewModel
                {
                    TransportModeId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.TransportModeTypeList = new List<TransportModeTypeSetupViewModel>();
                model.Setup = new TransportModeTypeSetupViewModel();
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
            TransportModeTypeSetupViewModel model = new TransportModeTypeSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "TransportModeId", "Transport_Mode_Type", 3);

            if (!string.IsNullOrEmpty(id))
            {

                model = await modeTypeService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.TransportModeId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(TransportModeTypeSetupViewModel modelVM)
        {
            try
            {

                if (await modeTypeService.IsExistAsync(modelVM.TransportMode, modelVM.TransportModeId))
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
                    var hasSavePermission = await modeTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await modeTypeService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.TransportModeId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await modeTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await modeTypeService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.TransportModeId });
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

            var hasPermission = await modeTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {

                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await modeTypeService.DeleteTab(ids, model);
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
                var list = await modeTypeService.GetAllAsync();
                return Json(list);
                //return PartialView("_Grid", list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

    }
}
