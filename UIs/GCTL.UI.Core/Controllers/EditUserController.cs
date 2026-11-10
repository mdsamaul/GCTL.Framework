using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.EditUserVM;
using GCTL.Service.UserEditEntry;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.EditUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    [Authorize]
    public class EditUserController : BaseController
    {
        private readonly IUserEditService _entryService;

        public EditUserController(IUserEditService entryService)
        {
            _entryService = entryService;
        }

        public async Task<IActionResult> Index()
        {
            var username = LoginInfo.Username;
            var setup = await _entryService.GetByIdAsync(username) ?? new EditUserSetupViewModel { Username = username };

            var model = new EditUserPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Breadcrumb = "Edit User Profile",
                PageTitle = "Edit User Profile",
                Setup = setup
            };

            return View(model);
        }

        [HttpGet("EditUser/GetById")]
        public async Task<IActionResult> GetById()
        {
            var result = await _entryService.GetByIdAsync(LoginInfo.Username);
            return Json(new { data = result });
        }

        [HttpGet("EditUser/GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _entryService.GetByIdAsync(id);
            return Json(new { data = result });
        }

        [HttpPost]
        public async Task<IActionResult> GetPaginatedList()
        {
            try
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

                var result = await _entryService.GetPaginatedDataAsync(
                    searchValue ?? string.Empty,
                    page,
                    pageSize,
                    sortColumn ?? string.Empty,
                    sortDirection ?? string.Empty,
                    LoginInfo.Username);

                var response = new
                {
                    draw = draw,
                    recordsTotal = result.totalRecord,
                    recordsFiltered = result.currentRecord,
                    data = result.Data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(EditUserSetupViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { isSuccess = false, message = "Please enter a new password." });
            }

            if (model.Password != model.ConfirmPassword)
            {
                return Json(new { isSuccess = false, message = "New Password and Confirm Password do not match." });
            }

            model.ToAudit(LoginInfo, isUpdate: true);

            var (isSuccess, message, data) = await _entryService.SaveAsync(model);

            return Json(new
            {
                isSuccess = isSuccess,
                message = message,
                data = data
            });
        }
    }
}

