using GCTL.Core.Data;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.TransportInformation;
using GCTL.Data.Models;
using GCTL.Core.Helpers;
using GCTL.Service.Common;
using GCTL.Service.TransportInformation;
using GCTL.UI.Core.ViewModels.TransportInformation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class TransportInformationController : BaseController
    {
        #region Service & Repository
        public readonly ITransportInformationService transportInformationService;
        public readonly IRepository<SalesDefVehicleType> vehicleTypeRepository;
        public readonly IRepository<SalesDefPortName> portNameRepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public TransportInformationController(
            ITransportInformationService transportInformationService,
                IRepository<SalesDefVehicleType> vehicleTypeRepository,
                IRepository<SalesDefPortName> portNameRepository,
               ICommonService commonService

        )
        {
            this.transportInformationService = transportInformationService;
            this.vehicleTypeRepository = vehicleTypeRepository;
            this.portNameRepository = portNameRepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await transportInformationService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new TransportInformationPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await transportInformationService.GetAllAsync();
                model.TransportList = list ?? new List<TransportInformationSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "VehicleID", "Sales_Def_Vehicle", 4);

                ViewBag.VehicleDD = new SelectList(vehicleTypeRepository.All(), "VehicleTypeId", "VehicleType");
                ViewBag.PortNameDD = new SelectList(portNameRepository.All(), "PortNameId", "PortName");

                model.Setup = new TransportInformationSetupViewModel
                {
                    VehicleId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.TransportList = new List<TransportInformationSetupViewModel>();
                model.Setup = new TransportInformationSetupViewModel();
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
            TransportInformationSetupViewModel model = new TransportInformationSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "VehicleID", "Sales_Def_Vehicle", 4);

            ViewBag.VehicleDD = new SelectList(vehicleTypeRepository.All(), "VehicleTypeId", "VehicleType");
            ViewBag.PortNameDD = new SelectList(portNameRepository.All(), "PortNameId", "PortName");

            if (!string.IsNullOrEmpty(id))
            {

                model = await transportInformationService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.VehicleId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(TransportInformationSetupViewModel modelVM)
        {
            try
            {
                if (await transportInformationService.IsExistAsync(modelVM.VehicleNo, modelVM.VehicleId))
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
                    var hasSavePermission = await transportInformationService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await transportInformationService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.PortNameId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await transportInformationService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await transportInformationService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.PortNameId });
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

            var hasPermission = await transportInformationService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await transportInformationService.DeleteTab(ids, model);
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
                var list = await transportInformationService.GetAllAsync();
                return Json(list);
                //return PartialView("_Grid", list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Chake Degian

        //public IActionResult Index()
        //{
        //    TransportInformationPageViewModel model = new TransportInformationPageViewModel
        //    {
        //        Setup = new TransportInformationSetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
