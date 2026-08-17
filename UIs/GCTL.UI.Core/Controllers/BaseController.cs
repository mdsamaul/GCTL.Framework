using Dapper;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Data.Context;
using GCTL.Data.Models;
using GCTL.Service.HolidayLIst;
using GCTL.Service.Permissions;
using GCTL.UI.Core.Filters;
using GCTL.UI.Core.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace GCTL.UI.Core.Controllers;

[Authorize]
public class BaseController : Controller
{
    protected GctlHrmContext DbContext =>
        (GctlHrmContext)HttpContext.RequestServices.GetService(typeof(GctlHrmContext))!;

    private IRepository<CoreUserInfo>? _userManager;
    protected IRepository<CoreUserInfo> UserManager =>
        _userManager ??= (IRepository<CoreUserInfo>)HttpContext.RequestServices
            .GetService(typeof(UserManager<CoreUserInfo>))!;

    protected UserInfoViewModel? LoginInfo => _loginInfo ??= LoadLoginInfo();
    private UserInfoViewModel? _loginInfo;

    private UserInfoViewModel? LoadLoginInfo()
    {
        if (User.Identity?.IsAuthenticated != true) return null;

        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        var role = Enum.TryParse<DefaultRoles>(roleClaim, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : DefaultRoles.User;

        return new UserInfoViewModel
        {
            Username = User.Identity.Name ?? string.Empty,
            AccessCode = User.FindFirst("AccessCode")?.Value ?? string.Empty,
            EmployeeId = User.FindFirst("EmployeeId")?.Value ?? string.Empty,
            CompanyCode = User.FindFirst("CompanyCode")?.Value ?? string.Empty,
            Role = role,
            IPAddress = User.FindFirst("IPAddress")?.Value
                ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? string.Empty,
            MacAddress = User.FindFirst("MacAddress")?.Value
                ?? NetworkHelper.GetMacAddress(HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty)
        };
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (LoginInfo != null)
        {
            ViewBag.LoginUser = LoginInfo.Username;
            ViewBag.LoginUserPhoto = string.IsNullOrEmpty(LoginInfo.EmployeeId)
                ? "/images/UserIcon.png"
                : GetEmployeePhotoBase64(LoginInfo.EmployeeId) ?? "/images/UserIcon.png";

            if (!string.IsNullOrEmpty(LoginInfo.EmployeeId))
            {
                try
                {
                    var employeeInfo = await AllInfoEmployeeById(LoginInfo.EmployeeId);

                    if (employeeInfo != null)
                    {
                        // employeeInfo দিয়ে LoginInfo.CompanyCode আপডেট করতে চাইলে এখানে করো
                        // LoginInfo.CompanyCode = employeeInfo.CompanyCode;
                    }
                    else
                    {
                        var employeePhoto = GetEmployeePhotoBase64(LoginInfo.EmployeeId);
                        if (!string.IsNullOrEmpty(employeePhoto))
                            ViewBag.LoginUserPhoto = employeePhoto;
                    }
                }
                catch { /* best-effort */ }
            }
        }

        await next();
    }

    protected OkObjectResult DataTablesResult<T>(PagedList<T> paginatedItems)
    {
        return Ok(new
        {
            recordsTotal = paginatedItems.TotalCount,
            recordsFiltered = paginatedItems.TotalCount,
            data = paginatedItems
        });
    }

    protected async Task<dynamic?> AllInfoEmployeeById(string employeeCode)
    {
        var connectionString = DbContext.Database.GetConnectionString();

        await using var conn = new SqlConnection(connectionString);

        return await conn.QueryFirstOrDefaultAsync(
            "FullEmployeeDetailsGetByid",
            new { EmployeeCode = employeeCode },
            commandType: CommandType.StoredProcedure
        );
    }

    private string? GetEmployeePhotoBase64(string employeeCode)
    {
        return null;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<JsonResult> GetHolidays(int? year = null)
    {
        var holidayService = HttpContext.RequestServices.GetService<IholidayListService>();
        if (holidayService == null)
            return Json(new List<object>());

        var holidays = await holidayService.GetHolidayListAsync(year ?? DateTime.Now.Year);

        var mapped = holidays.Select(h => new {
            date = h.Date,
            name = h.Name
        }).ToList();

        return Json(mapped);
    }

    protected async Task<IActionResult?> CheckAddOrEditPermissionAsync(bool isNewRecord)
    {
        var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var accessCode = User.FindFirst("AccessCode")?.Value;
        var controllerName = RouteData.Values["controller"]?.ToString();

        if (string.IsNullOrEmpty(accessCode))
        {
            return Json(ApiResponse.PermissionDenied(AppMessages.SessionExpired));
        }

        bool allowed = await permissionService.HasAddOrEditPermissionByControllerAsync(accessCode, controllerName, isNewRecord);

        if (!allowed)
        {
            var message = isNewRecord ? AppMessages.NoPermissionAdd : AppMessages.NoPermissionEdit;
            return Json(ApiResponse.PermissionDenied(message));
        }

        return null;
    }
}