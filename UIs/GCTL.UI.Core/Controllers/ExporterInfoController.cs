using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ExporterInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.ExporterInfo;
using GCTL.UI.Core.ViewModels.ExporterInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{ 
    public class ExporterInfoController : BaseController
    {
        #region Service & Repository
        public readonly IExporterInfoService exporterInfoService;
        public IRepository<CaDefCountry> caDefCountryRepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public ExporterInfoController(
            IExporterInfoService exporterInfoService, 
            IRepository<CaDefCountry> caDefCountryRepository,
            ICommonService commonService
            
            )
        {
            this.exporterInfoService = exporterInfoService;
            this.caDefCountryRepository = caDefCountryRepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await exporterInfoService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ExporterInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await exporterInfoService.GetAllAsync();
                model.ExporterInfofoList = list ?? new List<ExporterInfoSetupViewModel>();

                var nextCode = commonService.GenerateCode("ExporterID", "Core_ExporterInformation", "EXP_", 6);

                ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");

                model.Setup = new ExporterInfoSetupViewModel
                {

                    ExporterId = nextCode
                };

            }
            catch (Exception ex)
            {

                model.ExporterInfofoList = new List<ExporterInfoSetupViewModel>();
                model.Setup = new ExporterInfoSetupViewModel();
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
            ExporterInfoSetupViewModel model = new ExporterInfoSetupViewModel();

            if (!string.IsNullOrEmpty(id))
            {

                model = await exporterInfoService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {
                var nextCode = commonService.GenerateCode("ExporterID", "Core_ExporterInformation", "EXP_", 6);
                model.ExporterId = nextCode;
            }
            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName", model.CountryId);
            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(ExporterInfoSetupViewModel modelVM)
        {
            try
            {
                if (await exporterInfoService.IsExistAsync(modelVM.ExporterId, modelVM.ExporterName))
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
                    var hasSavePermission = await exporterInfoService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await exporterInfoService.SaveAsync(modelVM);

                        var nextCode = commonService.GenerateCode("ExporterID", "Core_ExporterInformation", "EXP_", 6);

                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = nextCode });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await exporterInfoService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await exporterInfoService.UpdateAsync(modelVM);

                        var nextCode = commonService.GenerateCode("ExporterID", "Core_ExporterInformation", "EXP_", 6);

                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = nextCode });
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

            var hasPermission = await exporterInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await exporterInfoService.DeleteTab(ids, model);
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
                var list = await exporterInfoService.GetAllAsync();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region GenerateNewId
        public async Task<IActionResult> GenerateNewId()
        {
            await Task.Delay(100);
            var nextCode = await exporterInfoService.Autoid();
            return Json(nextCode);
        }

        #endregion

    }
}
