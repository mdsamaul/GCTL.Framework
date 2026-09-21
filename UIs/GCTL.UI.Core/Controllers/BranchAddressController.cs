using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.BranchAddress;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.OFRSection.OperationalFund;
using GCTL.Data.Models;
using GCTL.Service.BranchAddress;
using GCTL.Service.Common;
using GCTL.UI.Core.ViewModels.BranchAddress;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class BranchAddressController : BaseController
    {
        #region Service & Repository
        public readonly IBranchAddressService branchAddressService;
        private readonly IRepository<CoreVendorInformation> coreVendorinformationrepository;
        private readonly IRepository<SalesContactPerson> contactPersonrepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;


        public BranchAddressController(
            IBranchAddressService branchAddressService,
            IRepository<CoreVendorInformation> coreVendorinformationrepository,
            IRepository<SalesContactPerson> contactPersonrepository,
            IRepository<CaDefCountry> caDefCountryRepository,
            ICommonService commonService
            
            )
        {
            this.branchAddressService = branchAddressService;
            this.coreVendorinformationrepository = coreVendorinformationrepository;
            this.contactPersonrepository = contactPersonrepository;
            this.caDefCountryRepository = caDefCountryRepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await branchAddressService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new BranchAddressPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await branchAddressService.GetAllAsync();
                model.BranchAddressList = list ?? new List<BranchAddressSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "VendorBranchID", "VendorBranchInfo", 5);

                ViewBag.VendorDD = new SelectList(coreVendorinformationrepository.All(), "VendorId", "Name");
                ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName");
                ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");
                model.TabModel = new TabGroupModel
                        {
                            GroupId = "vendorTabGroup",
                            Tabs = new List<TabItemModel>
                        {
                            new TabItemModel { TabId = "vendorInfo", TabTitle = "Vendor Info", ControllerName = "VendorInfo", ActionName = "Index" },
                            new TabItemModel { TabId = "branchAddress", TabTitle = "Branch Address", ControllerName = "BranchAddress", ActionName = "Index", IsActive = true }
                        }
                };
                model.Setup = new BranchAddressSetupViewModel
                {
                    VendorId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.BranchAddressList = new List<BranchAddressSetupViewModel>();
                model.Setup = new BranchAddressSetupViewModel();
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
            BranchAddressSetupViewModel model = new BranchAddressSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "VendorBranchID", "VendorBranchInfo", 5);

            ViewBag.VendorDD = new SelectList(coreVendorinformationrepository.All(), "VendorId", "Name");
            ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName");
            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");

            if (!string.IsNullOrEmpty(id))
            {

                model = await branchAddressService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.VendorBranchId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region GetById

        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var data = await branchAddressService.GetByIdAsync(code); // বা যা কল হয়
            return Json(data);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(BranchAddressSetupViewModel modelVM)
        {
            try
            {
                if (await branchAddressService.IsExistAsync(modelVM.LocationAddress, modelVM.VendorBranchId))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                bool isUpdate = !string.IsNullOrEmpty(modelVM.VendorBranchId);

                modelVM.ToAudit(LoginInfo, isUpdate);

                if (!isUpdate)
                {
                    var hasSavePermission = await branchAddressService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await branchAddressService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.VendorBranchId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await branchAddressService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        var updated = await branchAddressService.UpdateAsync(modelVM);
                        if (updated)
                            return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.VendorBranchId });
                        else
                            return Json(new { isSuccess = false, message = "Update failed. Record not found." });
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

            var hasPermission = await branchAddressService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await branchAddressService.DeleteTab(ids, model);
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
                var list = await branchAddressService.GetAllAsync();
                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region GetContactPersonsForTable

        [HttpGet]
        public async Task<IActionResult> GetContactPersonsForTable()
        {
            var result = await branchAddressService.GetContactPersonsForTableAsync();
            return Json(result);
        }

        #endregion
    }
}
