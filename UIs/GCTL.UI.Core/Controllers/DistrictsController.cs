
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Districts;
using GCTL.Service.Common;
using GCTL.Service.Designations;
using GCTL.Service.Districts;
using GCTL.UI.Core.ViewModels.Districts;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class DistrictsController : BaseController
    {
        private readonly IDistrictsService service;
        private readonly ICommonService commonService;
        string strMaxNO = "";
        public DistrictsController(IDistrictsService service, ICommonService commonService)
        {
            this.service = service;
            this.commonService = commonService;        
        }

        #region GettALLById

        public async Task<IActionResult> Index(string? id)
        {
            var hasPermission = await service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new DistrictsPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                // Get all districts
                var list = await service.GetAllAsync();
                model.DistrictList = list ?? new List<DistrictsSetupViewModel>();

                // Get existing record or create new setup
                if (!string.IsNullOrEmpty(id))
                {
                    model.Setup = await service.GetByIdAsync(id);
                }
                else
                {
                    commonService.FindMaxNo(ref strMaxNO, "DistrictID", "HRM_Def_District", 2);

                    model.Setup = new DistrictsSetupViewModel
                    {
                        DistrictId = strMaxNO
                    };
                }
            }
            catch (Exception ex)
            {
                model.DistrictList = new List<DistrictsSetupViewModel>();
                model.Setup = new DistrictsSetupViewModel();

                Console.WriteLine("Error: " + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        //public async Task<IActionResult> Index(string? id)
        //{
        //    var hasPermission = await service.PagePermissionAsync(LoginInfo.AccessCode);
        //    if (!hasPermission)
        //    {
        //        return RedirectToAction("Login", "Accounts");
        //    }
        //    //Get all
        //    DistrictsPageViewModel model = new DistrictsPageViewModel();
        //    var list = await service.GetAllAsync();
        //    model.DistrictList = list ?? new List<DistrictsSetupViewModel>();

        //    //Get By Id
        //    if (!string.IsNullOrEmpty(id))
        //    {

        //        model.Setup = await service.GetByIdAsync(id);

        //    }
        //    //

        //    //
        //    model.PageUrl = Url.Action(nameof(Index));
        //    return View(model);
        //}
        #endregion


        #region Post Update 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(DistrictsSetupViewModel modelVM)
        {
            try
            {

                if (await service.IsExistAsync(modelVM.District, modelVM.DistrictId))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }


                if (string.IsNullOrEmpty(modelVM.DistrictId))
                {
                    modelVM.DistrictId = await service.GenerateNextCode();
                }


                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0)
                {
                    var hasSavePermission = await service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await service.SaveAsync(modelVM, LoginInfo.CompanyCode);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = modelVM.DistrictId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await service.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully", lastCode = modelVM.DistrictId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion


        //#region CheckAvailability
        //[HttpPost]
        //public async Task<JsonResult> CheckAvailability(string name, string code)
        //{
        //    if (await service.IsExistAsync(name, code))
        //    {

        //        return Json(new { isSuccess = true, message = $"Already <span style='color: blue;'>'{name}'</span> Exists" });

        //    }

        //    return Json(new { isSuccess = false });
        //}
        //#endregion


        #region Delete

        [HttpPost]
        public async Task<ActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No IDs provided for delete." });
                }

                var hasPermission = await service.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { success = false, message = "You have no access.", refSuccess = false });
                }

                DeleteHistoryViewModel model = new DeleteHistoryViewModel();
                model.ToAudit(LoginInfo);
                model.CompanyCode = LoginInfo.CompanyCode;

                var success = await service.DeleteDistrictsAsync(ids, model);

                return Json(new { success = success.success, message = success.message, refSuccess = success.refSuccess });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }


        #endregion


        #region NeaxtCode
        [HttpGet]
        public async Task<IActionResult> GenerateNextCode()
        {
            var nextCode = await service.GenerateNextCode();
            return Json(nextCode);
        }
        #endregion


        #region TabeleLodaing

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await service.GetAllAsync();
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
