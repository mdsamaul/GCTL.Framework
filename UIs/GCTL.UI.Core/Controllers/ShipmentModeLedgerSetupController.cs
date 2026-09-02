using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ShipmentModeLedgerSetup;
using GCTL.Service.Common;
using GCTL.Service.ShipmentModeLedgerSetup;
using GCTL.UI.Core.ViewModels.ShipmentModeLedgerSetup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class ShipmentModeLedgerSetupController : BaseController
    {
        #region Service & Repository
        private readonly IShipmentModeLedgerSetupService _service;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;

        public ShipmentModeLedgerSetupController(
            IShipmentModeLedgerSetupService service,
            ICommonService commonService
            )
        {
            _service = service;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index()
        {
            var hasPermission = await _service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new ShipmentModeLedgerSetupPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {

                var list = await _service.GetAllAsync();
                model.ShipmentModeLedgerSetupList = list ?? new List<ShipmentModeLedgerSetupSetupViewModel>();

                commonService.FindMaxNo(ref strMaxNO, "ExpenseTypeID", "CF_Def_ExpenseType", 4);

                ViewBag.AdvanceDD = new SelectList(await _service.SelectAdvanceLedger(), "Code", "Name");
                ViewBag.RevenueDD = new SelectList(await _service.SelectRevenueLedger(), "Code", "Name");
                ViewBag.ExpenseDD = new SelectList(await _service.SelectExpenseLedger(), "Code", "Name");
                ViewBag.ShipmentDD = new SelectList(await _service.SelectShipmentMode(), "Code", "Name");

                model.Setup = new ShipmentModeLedgerSetupSetupViewModel
                {
                    ExpenseTypeId = strMaxNO
                };

            }
            catch (Exception ex)
            {

                model.ShipmentModeLedgerSetupList = new List<ShipmentModeLedgerSetupSetupViewModel>();
                model.Setup = new ShipmentModeLedgerSetupSetupViewModel();
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
            ShipmentModeLedgerSetupSetupViewModel model = new ShipmentModeLedgerSetupSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "ExpenseTypeID", "CF_Def_ExpenseType", 4);

            ViewBag.AdvanceDD = new SelectList(await _service.SelectAdvanceLedger(), "Code", "Name");
            ViewBag.RevenueDD = new SelectList(await _service.SelectRevenueLedger(), "Code", "Name");
            ViewBag.ExpenseDD = new SelectList(await _service.SelectExpenseLedger(), "Code", "Name");
            ViewBag.ShipmentDD = new SelectList(await _service.SelectShipmentMode(), "Code", "Name");

            if (!string.IsNullOrEmpty(id))
            {
                model = await _service.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.ExpenseTypeId = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        #endregion

        #region GetById

        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var data = await _service.GetByIdAsync(code); 
            return Json(data);
        }

        [HttpGet]
        [Route("/ShipmentModeLedgerSetup/GetNextId")]
        public IActionResult GetNextId()
        {
            string nextId = string.Empty;
            commonService.FindMaxNo(ref nextId, "ExpenseTypeID", "CF_Def_ExpenseType", 4);
            return Ok(nextId);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(ShipmentModeLedgerSetupSetupViewModel modelVM)
        {
            try
            {
                if (await _service.IsExistAsync(modelVM.ExpenseType, modelVM.ExpenseTypeId))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                bool isUpdate = !string.IsNullOrEmpty(modelVM.ExpenseTypeId);

                modelVM.ToAudit(LoginInfo, isUpdate);

                if (!isUpdate)
                {
                    var hasSavePermission = await _service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await _service.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.ExpenseTypeId });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await _service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        var updated = await _service.UpdateAsync(modelVM);
                        if (updated)
                            return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.ExpenseTypeId });
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

            var hasPermission = await _service.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await _service.DeleteTab(ids, model);
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
                var list = await _service.GetAllAsync();
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
