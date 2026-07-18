using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortType;
using GCTL.Core.ViewModels.TransportType;
using GCTL.Service.Common;
using GCTL.Service.PortType;
using GCTL.Service.TransportType;
using GCTL.UI.Core.ViewModels.PortType;
using GCTL.UI.Core.ViewModels.TransportType;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class TransportTypeController : BaseController
    {
        #region Service & Repository
        public readonly ITransportTypeService transportTypeService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public TransportTypeController(
            ITransportTypeService transportTypeService, 
            ICommonService commonService

        )
        {
            this.transportTypeService = transportTypeService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await transportTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new TransportTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await transportTypeService.GetAllAsync();
                model.TransportTypeList = list ?? new List<TransportTypeSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "VehicleTypeID", "Sales_Def_VehicleType", 2);

                model.Setup = new TransportTypeSetupViewModel
                {
                    VehicleTypeId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.TransportTypeList = new List<TransportTypeSetupViewModel>();
                model.Setup = new TransportTypeSetupViewModel();
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
            TransportTypeSetupViewModel model = new TransportTypeSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "VehicleTypeID", "Sales_Def_VehicleType", 2);

            if (!string.IsNullOrEmpty(id))
            {

                model = await transportTypeService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.VehicleTypeId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(TransportTypeSetupViewModel modelVM)
        {
            try
            {

                if (await transportTypeService.IsExistAsync(modelVM.VehicleType, modelVM.VehicleTypeId))
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
                    var hasSavePermission = await transportTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await transportTypeService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.VehicleTypeId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await transportTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await transportTypeService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.VehicleTypeId });
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

            var hasPermission = await transportTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {

                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await transportTypeService.DeleteTab(ids, model);
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
                var list = await transportTypeService.GetAllAsync();
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
        //    TransportTypePageViewModel model = new TransportTypePageViewModel
        //    {
        //        Setup = new TransportTypeSetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
