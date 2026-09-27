using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.PortName;
using GCTL.Core.ViewModels.PortType;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.PortName;
using GCTL.Service.PortType;
using GCTL.UI.Core.ViewModels.PortName;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class PortNameController : BaseController
    {
        #region Service & Repository
        public readonly IPortNameService portNameService;
        private readonly IRepository<SalesDefPortType> portTyperepository;
        private readonly ICommonService commonService;
        string strMaxNO = string.Empty;

        public PortNameController(
            IPortNameService portNameService,
            IRepository<SalesDefPortType> portTyperepository,
            ICommonService commonService
            )
        {
            this.portNameService = portNameService;
            this.portTyperepository = portTyperepository;
            this.commonService = commonService;
        }

        #endregion

        #region Index
        public async Task<IActionResult> Index()
        {
            var hasPermission = await portNameService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new PortNamePageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await portNameService.GetAllAsync();
                model.PortNameList = list ?? new List<PortNameSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "PortNameId", "Sales_Def_PortName", 4);

                ViewBag.PortTypeDD = new SelectList(portTyperepository.All(), "PortId", "PortType");


                model.Setup = new PortNameSetupViewModel
                {
                    PortNameId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.PortNameList = new List<PortNameSetupViewModel>();
                model.Setup = new PortNameSetupViewModel();
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
            PortNameSetupViewModel model = new PortNameSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "PortNameId", "Sales_Def_PortName", 4);

            if (!string.IsNullOrEmpty(id))
            {

                model = await portNameService.GetByIdAsync(id);
                if (model == null)
                {

                    return NotFound();
                }
            }
            else
            {

                model.PortNameId = strMaxNO;
            }
            ViewBag.PortTypeDD = new SelectList(portTyperepository.All(), "PortId", "PortType", model.PortId);

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Setup(PortNameSetupViewModel modelVM)
        {
            try
            {
                if (await portNameService.IsExistAsync(modelVM.PortName, modelVM.PortId, modelVM.PortNameId))
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
                    var hasSavePermission = await portNameService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await portNameService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.PortNameId });

                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await portNameService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await portNameService.UpdateAsync(modelVM);
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

            var hasPermission = await portNameService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await portNameService.DeleteTab(ids, model);
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
                var list = await portNameService.GetAllAsync() ?? new List<PortNameSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<PortNameSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion

        #region Chake Degian

        //public IActionResult Index()
        //{
        //    PortNamePageViewModel model = new PortNamePageViewModel
        //    {
        //        Setup = new PortNameSetupViewModel()
        //    };
        //    return View(model);
        //}

        #endregion
    }
}
