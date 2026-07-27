using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Relationship;
using GCTL.Service.Common;
using GCTL.Service.Relationships;
using GCTL.UI.Core.ViewModels.Relationship;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class RelationshipsController : BaseController
    {
        private readonly IRelationshipsService service;
        private readonly ICommonService commonService;
        string strMaxNO = "";
        public RelationshipsController(IRelationshipsService service, ICommonService commonService)
        {
            this.service = service;
            this.commonService = commonService;
        }

        #region GettALLById
        public async Task<IActionResult> Index(string? id)
        {
            var hasPermission = await service.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }
            //Get all
            RelationshipPageViewModel model = new RelationshipPageViewModel();
            var list = await service.GetAllAsync();
            model.RelationshipList = list ?? new List<RelationshipSetupViewModel>();

            //Get By Id
            if (!string.IsNullOrEmpty(id))
            {

                model.Setup = await service.GetByIdAsync(id);

            }
            //

            //
            model.PageUrl = Url.Action(nameof(Index));
            return View(model);
        }
        #endregion


        #region Post Update 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(RelationshipSetupViewModel modelVM)
        {
            try
            {

                if (await service.IsExistAsync(modelVM.Relationship, modelVM.RelationshipCode))
                {
                    return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                }


                if (string.IsNullOrEmpty(modelVM.RelationshipCode))
                {
                    modelVM.RelationshipCode = await service.GenerateNextCode();
                }


                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0)
                {
                    var hasSavePermission = await service.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await service.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = modelVM.RelationshipCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save", noSavePermission = true });
                    }
                }
                else
                {

                    var hasUpdatePermission = await service.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await service.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully", lastCode = modelVM.RelationshipCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion


        #region CheckAvailability
        [HttpPost]
        public async Task<JsonResult> CheckAvailability(string name, string code)
        {
            if (await service.IsExistAsync(name, code))
            {

                return Json(new { isSuccess = true, message = $"Already <span style='color: blue;'>'{name}'</span> Exists" });

            }

            return Json(new { isSuccess = false });
        }
        #endregion


        #region Delete
        [HttpPost]
        public async Task<ActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No IDs provided for delete." });
                }

                var hasPermission = await service.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { success = false, message = "You have no access.", refSuccess = false });
                }

                DeleteHistoryViewModel model = new DeleteHistoryViewModel();
                model.ToAudit(LoginInfo);
                model.CompanyCode = LoginInfo.CompanyCode;

                var success = await service.DeleteDistrictsAsync(ids, model);

                return Json(new { success = success.success, message = success.message, refSuccess = success.refSuccess });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }



        //[HttpPost]
        //public async Task<IActionResult> Delete(List<string> ids)
        //{
        //    try
        //    {

        //        var hasPermission = await service.DeletePermissionAsync(LoginInfo.AccessCode);
        //        if (hasPermission)
        //        {

        //            foreach (var id in ids)
        //            {
        //                var result = service.DeleteLeaveType(id);

        //            }

        //            return Json(new { isSuccess = true, message = "Deleted Successfully" });
        //        }
        //        else
        //        {

        //            return Json(new { isSuccess = false, message = "You have no access" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        Console.WriteLine($"Error deleting leave type: {ex.Message}");

        //        return StatusCode(500, new { isSuccess = false, message = ex.Message });
        //    }
        //}
        #endregion


        #region NeaxtCode
        [HttpGet]
        public async Task<IActionResult> GenerateNextCode()
        {
            var nextCode = await service.GenerateNextCode();
            return Json(nextCode);
        }
        #endregion


        #region TabeleLodaing

        [HttpGet]
        public async Task<IActionResult> GetTableData()
        {
            try
            {
                var list = await service.GetAllAsync();
                return PartialView("_Grid", list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        #endregion
    }
}
