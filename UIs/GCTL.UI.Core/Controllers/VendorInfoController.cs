using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.VendorInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.SupplierInformation;
using GCTL.Service.VendorInfo;
using GCTL.UI.Core.ViewModels.VendorInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class VendorInfoController : BaseController
    {
        #region Service & Repository
        public readonly IVendorInfoService vendorInfoService;
        private readonly IRepository<SalesContactPerson> contactPersonrepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly IRepository<CfDefVendorPrefix> vendorPrefixrepository;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;

        public VendorInfoController(
            IVendorInfoService vendorInfoService,
            IRepository<SalesContactPerson> contactPersonrepository, 
            IRepository<CaDefCountry> caDefCountryRepository,
            IRepository<CfDefVendorPrefix> vendorPrefixrepository,
            ICommonService commonService
            
        )
        {
            this.vendorInfoService = vendorInfoService;
            this.contactPersonrepository = contactPersonrepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.vendorPrefixrepository = vendorPrefixrepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await vendorInfoService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new VendorInfoPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await vendorInfoService.GetAllAsync();
                model.VendorInfoList = list ?? new List<VendorInfoSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "VendorID", "Core_VendorInformation", 4);

                ViewBag.VendorPrefixDD = new SelectList(vendorPrefixrepository.All(), "VendorPrifixId", "PrifixName");
                ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName");
                ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");

                model.Setup = new VendorInfoSetupViewModel
                {
                    VendorId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.VendorInfoList = new List<VendorInfoSetupViewModel>();
                model.Setup = new VendorInfoSetupViewModel();
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
            VendorInfoSetupViewModel model = new VendorInfoSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "VendorID", "Core_VendorInformation", 4);

            if (!string.IsNullOrEmpty(id))
            {

                model = await vendorInfoService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.VendorId = strMaxNO;
            }


            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName", model.CountryId);
            ViewBag.VendorPrefixDD = new SelectList(vendorPrefixrepository.All(), "VendorPrefixId", "VendorPrefixName", model.VendorPrefix);
            ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName", model.Cpid);

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region GetById

        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await vendorInfoService.GetByIdAsync(code);
            return Json(result);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(VendorInfoSetupViewModel modelVM)
        {
            try
            {
                if (await vendorInfoService.IsExistAsync(modelVM.Name, modelVM.VendorId))
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
                    var hasSavePermission = await vendorInfoService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await vendorInfoService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.VendorId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await vendorInfoService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await vendorInfoService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.VendorId });
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

            var hasPermission = await vendorInfoService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await vendorInfoService.DeleteTab(ids, model);
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
                var list = await vendorInfoService.GetAllAsync();
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
