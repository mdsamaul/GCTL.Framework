using GCTL.Core.Data;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Core.ViewModels.VendorInfo;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.TransportInformation;
using GCTL.Service.VendorInfo;
using GCTL.UI.Core.ViewModels.TransportInformation;
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

            ViewBag.ContactDD = new SelectList(vendorPrefixrepository.All(), "VendorPrifixId", "PrifixName");
            ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName");
            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");

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

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

    }
}
