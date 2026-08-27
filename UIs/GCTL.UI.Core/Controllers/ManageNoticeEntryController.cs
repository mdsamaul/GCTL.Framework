using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.ManageNoticeEntries;
using GCTL.Data.Models;
using GCTL.Service.ManageNoticeEntries;
using GCTL.UI.Core.ViewModels.ManageNotices;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class ManageNoticeEntryController : BaseController
    {
        private readonly IManageNoticeService entryService;
        private readonly IRepository<NoticeDocumentFile> noticeRepo;

        public ManageNoticeEntryController(IManageNoticeService entryService, IRepository<NoticeDocumentFile> noticeRepo)
        {
            this.entryService = entryService;
            this.noticeRepo = noticeRepo;
        }


        public async Task<IActionResult> Index()
        {
            var hasPermission = await entryService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
                return RedirectToAction("Login", "Accounts");

            ManageNoticePageViewModel model = new ManageNoticePageViewModel()
            {
                PageUrl = Url.Action(nameof(Index)),
            };

            return View(model);
        }

        public async Task<IActionResult> getFilterEmp([FromBody] EmployeeFilterViewModel model)
        {
            var data = await entryService.GetFilterEmpAsync(model);
            return Json(data);
        }

        public async Task<IActionResult> GenerateNewId()
        {
            var newId = await entryService.GenerateIdAsync();
            return Json(newId);
        }

        public async Task<IActionResult> GetById(int id)
        {
            var result = await entryService.GetByIdAsync(id);
            return Json(new { data = result });
        }

        public async Task<ActionResult> GetPaginatedData()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            var sortColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
            var sortColumn = Request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
            var sortDirection = Request.Form["order[0][dir]"].FirstOrDefault();

            var pageSize = string.IsNullOrEmpty(length) ? 10 : Convert.ToInt32(length);
            var page = string.IsNullOrEmpty(start) ? 1 : (Convert.ToInt32(start) / pageSize) + 1;

            var result = await entryService.GetPaginatedDataAsync(searchValue, page, pageSize, sortColumn, sortDirection);

            var response = new
            {
                draw = draw,
                recordsTotal = result.TotalRecords,
                recordsFiltered = result.FilterRecords,
                data = result.Data
            };

            return Ok(response);
        }
        public async Task<IActionResult> SaveNotice([FromForm] ManageNoticeSetupViewModel model)
        {
            if (model.Tc == 0)
            {
                var hasPermission = await entryService.SavePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                    return Json(new
                    {
                        success = false,
                        message = "You have no permission."
                    });

                model.ToAudit(LoginInfo);
                var result = await entryService.SaveAsync(model);
                return Json(new
                {
                    success = result,
                    message = "Saved Successfully"
                });
            }
            else
            {
                var hasPermission = await entryService.UpdatePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                    return Json(new
                    {
                        success = false,
                        message = "You have no permission."
                    });

                model.ToAudit(LoginInfo, true);
                var result = await entryService.EditAsync(model);
                return Json(new
                {
                    success = result,
                    message = "Updated Successfully"
                });
            }
        }

        public async Task<IActionResult> BulkDelete([FromBody] ManageNoticeSetupViewModel model)
        {
            try
            {
                var hasPermission = await entryService.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                    return Json(new
                    {
                        success = false,
                        message = "You have no permission."
                    });

                if (model.Tcs == null || !model.Tcs.Any() || model.Tcs.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Notice is Selected" });
                }

                DeleteHistoryViewModel dm = new DeleteHistoryViewModel();
                dm.ToAudit(LoginInfo);

                var result = await entryService.BulkDeleteAsync(model.Tcs, dm);

                if (!result)
                {
                    return Json(new { isSuccess = false, message = "No notice is found to delete" });
                }

                return Json(new { isSuccess = true, message = $"Deleted {model.Tcs.Count} Notice Successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> SendNoticeToEmployee([FromBody] EmailSentParamDTO request)
        {
            if (request.EmployeeIds.Count == 0 || request.Tcs.Count == 0)
                return Json(new { isSuccess = false, message = "Employee IDs and Notice IDs are required" });

            var result = await entryService.SentNoticeToEmployeeAsync(request.EmployeeIds, request.Tcs);

            if (result)
                return Json(new { isSuccess = true, message = "Notice sent successfully" });
            else
                return Json(new { isSuccess = false, message = "Failed to send notices" });

        }
    }
}
