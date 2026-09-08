using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ImporterInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.ImporterInfo;
using GCTL.UI.Core.ViewModels.ImporterInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class ImporterInfoController : BaseController
    {
        #region Service & Repository
        public readonly IImporterInfoService importerInfoService;
        public IRepository<CaDefCountry> caDefCountryRepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public ImporterInfoController(
            IImporterInfoService importerInfoService,
            IRepository<CaDefCountry> caDefCountryRepository,
            ICommonService commonService
        )
        {
            this.importerInfoService = importerInfoService;
            this.caDefCountryRepository = caDefCountryRepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await importerInfoService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ImporterInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await importerInfoService.GetAllAsync();
                model.ImporterInfoList = list ?? new List<ImporterInfoSetupViewModel>();

                // commonService.FindMaxNo(ref strMaxNO, "ImporterID", "Core_ImporterInformation", 10);
                var nextCode = commonService.GenerateCode("ImporterID", "Core_ImporterInformation", "IMP_", 6);

                ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");

                model.Setup = new ImporterInfoSetupViewModel
                {
                    
                    ImporterId = nextCode
                };

            }
            catch (Exception ex)
            {

                model.ImporterInfoList = new List<ImporterInfoSetupViewModel>();
                model.Setup = new ImporterInfoSetupViewModel();
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
            ImporterInfoSetupViewModel model = new ImporterInfoSetupViewModel();
          //  commonService.FindMaxNo(ref strMaxNO, "ImporterID", "Core_ImporterInformation", 10);



            if (!string.IsNullOrEmpty(id))
            {

                model = await importerInfoService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {
                var nextCode = commonService.GenerateCode("ImporterID", "Core_ImporterInformation", "IMP_", 6);
                model.ImporterId = nextCode;
            }
            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName", model.CountryId);
            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(ImporterInfoSetupViewModel modelVM)
        {
            try
            {
                if (await importerInfoService.IsExistAsync(modelVM.ImporterId, modelVM.ImporterName))
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
                    var hasSavePermission = await importerInfoService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await importerInfoService.SaveAsync(modelVM);

                        var nextCode = commonService.GenerateCode("ImporterID", "Core_ImporterInformation", "IMP_", 6);

                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = nextCode });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await importerInfoService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await importerInfoService.UpdateAsync(modelVM);

                        var nextCode = commonService.GenerateCode("ImporterID", "Core_ImporterInformation", "IMP_", 6);

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

            var hasPermission = await importerInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await importerInfoService.DeleteTab(ids, model);
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
                var list = await importerInfoService.GetAllAsync();
                return Json(new { data = list });
                //return PartialView("_Grid", list);
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
            var nextCode = await importerInfoService.Autoid();
            return Json(nextCode);
        }

        #endregion

    }
}
