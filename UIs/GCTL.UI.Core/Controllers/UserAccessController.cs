using AutoMapper;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.UserAccesses;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.Users;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.UserAcesses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class UserAccessController : BaseController
    {
        private readonly IUserService userService;
        private readonly ICommonService commonService;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IMapper mapper;
        string strMaxNO = "";

        public UserAccessController(IUserService userService,
                                    ICommonService commonService,
                                    IRepository<CoreAccessCode> accessCodeRepository,
                                    IMapper mapper)
        {
            this.userService = userService;
            this.commonService = commonService;
            this.accessCodeRepository = accessCodeRepository;
            this.mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            UserAccessPageViewModel model = new UserAccessPageViewModel()
            {
                PageUrl = Url.Action(nameof(Index))
            };
            commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);
            model.Setup = new UserAccessSetupViewModel { UserType = "Employee" };

            ViewBag.Roles = new SelectList(new List<string>() { "Admin", "User" });
            ViewBag.Sessions = new SelectList(new List<string>() { "Single", "Multiple" });
            ViewBag.AccessCodes = new SelectList(accessCodeRepository.All().GroupBy(p => p.AccessCodeId).Select(x => x.FirstOrDefault()).ToList(), "AccessCodeId", "AccessCodeName");
            ViewBag.Employees = new SelectList(await userService.GetEmployees(), "Code", "Name");
            ViewBag.Companies = new SelectList(await userService.GetCompanies(), "Code", "Name");
            ViewBag.Branches = new SelectList(await userService.GetBranch(), "Code", "Name");
            ViewBag.Departments = new SelectList(await userService.GetDepartments(), "Code", "Name");
            ViewBag.Customers = new SelectList(await userService.GetCustomers(), "Code", "Name");

            return View(model);
        }

        #region Table Loading (Server-Side Pagination)
        [HttpGet]
        public async Task<IActionResult> GetTableData(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "id",
            string sortOrder = "desc")
        {
            try
            {
                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortColumn = sortColumn,
                    SortOrder = sortOrder
                };

                var result = await userService.GetTableDataAsync(request);
                var hasData = result.Data?.Any() == true;
                return Json(new
                {
                    draw = draw,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<UserViewModel>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching user data.", error = ex.Message });
            }
        }
        #endregion

        #region Setup
        public async Task<IActionResult> Setup(int id, string? employeeId = null)
        {
            UserAccessSetupViewModel model = new UserAccessSetupViewModel { UserType = "Employee" };

            if (id > 0)
            {
                var result = await userService.GetUser(id);
                if (result != null)
                {
                    model = mapper.Map<UserAccessSetupViewModel>(result);
                    model.UserId = result.Id;
                    model.Code = result.EmployeeId;
                    model.UserType = !string.IsNullOrEmpty(result.UserType)
                        ? result.UserType
                        : (!string.IsNullOrEmpty(result.CustomerId) ? "Customer" : "Employee");
                    model.CustomerId = result.CustomerId;
                    model.EmployeeId = result.EmployeeId;
                    model.UserPassword = string.Empty; // Leave empty when editing with PasswordHasher

                    if (model.UserType == "Employee" && !string.IsNullOrEmpty(result.EmployeeId))
                    {
                        var employee = await userService.GetEmployeeDetails(result.EmployeeId);
                        if (employee != null)
                        {
                            model.EmployeeName = employee.EmployeeName;
                            model.DepartmentName = employee.DepartmentName;
                            model.DesignationName = employee.DesignationName;
                            model.NationalId = employee.NationalId;
                            model.Company = employee.Company;
                            model.Branch = employee.Branch;
                            model.EmployeeType = employee.EmpType;
                            model.EmployeeNature = employee.EmpNature;
                            model.OfficePhone = employee.OffPhone;
                            model.OfficeEmail = employee.OffEmail;
                            model.JoiningDate = employee.JoiningDate;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(employeeId))
            {
                var employee = await userService.GetEmployeeDetails(employeeId);
                if (employee != null)
                {
                    model.UserType = "Employee";
                    model.EmployeeId = employeeId;
                    model.EmployeeName = employee.EmployeeName;
                    model.DepartmentName = employee.DepartmentName;
                    model.DesignationName = employee.DesignationName;
                    model.NationalId = employee.NationalId;
                    model.Company = employee.Company;
                    model.Branch = employee.Branch;
                    model.EmployeeType = employee.EmpType;
                    model.EmployeeNature = employee.EmpNature;
                    model.OfficePhone = employee.OffPhone;
                    model.OfficeEmail = employee.OffEmail;
                    model.JoiningDate = employee.JoiningDate;
                }
            }

            ViewBag.Roles = new SelectList(Enum.GetNames<DefaultRoles>(), model.Role);
            ViewBag.Sessions = new SelectList(new List<string>() { "Single", "Multiple" }, model.SingleSession);
            ViewBag.AccessCodes = new SelectList(accessCodeRepository.All().GroupBy(p => p.AccessCodeId).Select(x => x.FirstOrDefault()).ToList(), "AccessCodeId", "AccessCodeName", model.AccessCode);
            ViewBag.Employees = new SelectList(await userService.GetEmployees(), "Code", "Name", model.EmployeeId);
            ViewBag.Customers = new SelectList(await userService.GetCustomers(), "Code", "Name", model.CustomerId);
            ViewBag.Companies = new SelectList(await userService.GetCompanies(), "Code", "Name");
            ViewBag.Branches = new SelectList(await userService.GetBranch(), "Code", "Name");
            ViewBag.Departments = new SelectList(await userService.GetDepartments(), "Code", "Name");

            return PartialView($"_{nameof(Setup)}", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(UserAccessSetupViewModel model)
        {
            if (await userService.IsUserExist(model.UserId, model.Username))
                return Json(new { success = false, message = "Username already exists." });

            if (model.UserId == 0 && string.IsNullOrWhiteSpace(model.UserPassword))
                return Json(new { success = false, message = "Password is required for new users." });

            if (model.UserType == "Employee" && string.IsNullOrWhiteSpace(model.EmployeeId))
                return Json(new { success = false, message = "Employee is required." });

            if (model.UserType == "Customer" && string.IsNullOrWhiteSpace(model.CustomerId))
                return Json(new { success = false, message = "Customer is required." });

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => model.UserId > 0 && x.Key.Contains(nameof(model.UserPassword)) ? false : x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                if (errors.Any())
                    return Json(new { success = false, message = errors.FirstOrDefault() });
            }

            model.CompanyCode = "001";
            var user = (model.UserId > 0 ? await userService.GetUser(model.UserId) : null)
                       ?? (!string.IsNullOrWhiteSpace(model.EmployeeId) ? await userService.GetBaseEmpData(model.EmployeeId) : new CoreUserInfo());

            model.ToAudit(LoginInfo, model.UserId > 0);

            if (model.UserId == 0)
            {
                user.Ldate = DateTime.Now;
                user.EntryDate = DateTime.Now;
            }
            else
            {
                user.Ldate = DateTime.Now;
            }

            var existingPassword = user.Password;

            mapper.Map(model, user);

            user.UserType = model.UserType;
            if (model.UserType == "Customer")
            {
                user.CustomerId = model.CustomerId;
                user.EmployeeId = string.Empty;
            }
            else
            {
                user.EmployeeId = model.EmployeeId;
                user.CustomerId = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(model.UserPassword))
            {
                var hasher = new PasswordHasher<CoreUserInfo>();
                user.Password = hasher.HashPassword(user, model.UserPassword);
            }
            else if (model.UserId > 0)
            {
                user.Password = existingPassword;
            }

            await userService.SaveUser(user);
            return Json(new { isSuccess = true, message = "Saved Successfully" });
        }
        #endregion

        public async Task<ActionResult> Grid()
        {
            var result = await userService.GetAllUsers();
            return Json(new { data = result });
        }

        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            bool success = false;

            DeleteHistoryViewModel dm = new DeleteHistoryViewModel();
            dm.ToAudit(LoginInfo);
            dm.CompanyCode = LoginInfo.CompanyCode;

            foreach (var item in id.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(item, out var parsedId))
                {
                    success = await userService.DeleteUser(parsedId, dm);
                }
            }
            return Json(new { success = success, message = "Deleted Successfully" });
        }

        public async Task<IActionResult> GetEmployee(string userId)
        {
            int id = await userService.GetIdByEmployee(userId);
            return await Setup(id, id == 0 ? userId : null);
        }

        public async Task<IActionResult> GetEmployeeByUser(string username)
        {
            int id = await userService.GetIdByUser(username);
            return await Setup(id);
        }
    }
}
