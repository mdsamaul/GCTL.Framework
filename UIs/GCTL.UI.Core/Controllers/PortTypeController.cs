using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortType;
using GCTL.Service.Common;
using GCTL.Service.PortType;
using GCTL.UI.Core.ViewModels.PortType;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class PortTypeController : BaseController
    {
        #region Service & Repository
        public readonly IPortTypeService portTypeService;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public PortTypeController(
            IPortTypeService portTypeService,
            ICommonService commonService
            
        )
        {
            this.portTypeService = portTypeService;
            this.commonService = commonService;
        }

        #endregion

        #region Index
        //public async Task<IActionResult> Index(bool child = false)
        //{
        //    var hasPermission = await portTypeService.PagePermissionAsync(LoginInfo.AccessCode);
        //    if (!hasPermission)
        //    {
        //        return RedirectToAction("Login", "Accounts");
        //    }

        //    var model = new PortTypePageViewModel
        //    {
        //        PageUrl = Url.Action(nameof(Index))
        //    };

        //    try
        //    {

        //        var list = await portTypeService.GetAllAsync();
        //        model.PortTypeList = list ?? new List<PortTypeSetupViewModel>();

        //        commonService.FindMaxNo(ref strMaxNO, "PortId", "Sales_Def_PortType", 4);

        //        model.Setup = new PortTypeSetupViewModel
        //        {
        //            PortId = strMaxNO
        //        };

        //    }
        //    catch (Exception ex)
        //    {

        //        model.PortTypeList = new List<PortTypeSetupViewModel>();
        //        model.Setup = new PortTypeSetupViewModel();
        //        Console.WriteLine("Error" + ex.Message);
        //    }

        //    if (child)
        //        return PartialView(model);

        //    return View(model);
        //}
        public async Task<IActionResult> Index()
        {
            var hasPermission = await portTypeService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PortTypePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await portTypeService.GetAllAsync();
                model.PortTypeList = list ?? new List<PortTypeSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "PortId", "Sales_Def_PortType", 4);

                model.Setup = new PortTypeSetupViewModel
                {
                    PortId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.PortTypeList = new List<PortTypeSetupViewModel>();
                model.Setup = new PortTypeSetupViewModel();
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
            PortTypeSetupViewModel model = new PortTypeSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "PortId", "Sales_Def_PortType", 4);

            if (!string.IsNullOrEmpty(id))
            {

                model = await portTypeService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.PortId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(PortTypeSetupViewModel modelVM)
        {
            try
            {

                if (await portTypeService.IsExistAsync(modelVM.PortType, modelVM.PortId))
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
                    var hasSavePermission = await portTypeService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await portTypeService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.PortId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await portTypeService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await portTypeService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.PortId });
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

            var hasPermission = await portTypeService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {

                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;
            var success = await portTypeService.DeleteTab(ids, model);
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
                var list = await portTypeService.GetAllAsync();
                return PartialView("_Grid", list);
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
        //    PortTypePageViewModel model = new PortTypePageViewModel
        //    {
        //        Setup = new PortTypeSetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
