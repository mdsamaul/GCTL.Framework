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
using Microsoft.Extensions.Caching.Memory;
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
        var sessionId = User.FindFirst("SessionId")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
        {
            var cache = HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            cache?.Set($"UserSession_LastActivity_{sessionId}", DateTime.Now, TimeSpan.FromHours(1));
        }

        if (LoginInfo != null)
        {
            ViewBag.LoginUser = LoginInfo.Username;

            // If EmployeeId was not in claims, try to find it from CoreUserInfo
            if (string.IsNullOrEmpty(LoginInfo.EmployeeId) && !string.IsNullOrEmpty(LoginInfo.Username))
            {
                try
                {
                    var userEntity = await DbContext.CoreUserInfos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Username == LoginInfo.Username);
                    if (!string.IsNullOrEmpty(userEntity?.EmployeeId))
                    {
                        LoginInfo.EmployeeId = userEntity.EmployeeId;
                    }
                }
                catch { /* best-effort */ }
            }

            string? photo = null;

            if (!string.IsNullOrEmpty(LoginInfo.EmployeeId))
            {
                try
                {
                    // 1. Try to get photo from HRM_EmployeePhoto table (with cache)
                    photo = await GetEmployeePhotoBase64Async(LoginInfo.EmployeeId);

                    // 2. Also retrieve employee info from stored procedure
                    var employeeInfo = await AllInfoEmployeeById(LoginInfo.EmployeeId);
                    if (employeeInfo != null)
                    {
                        // If photo was not found in HRM_EmployeePhoto, try to extract from employeeInfo
                        if (string.IsNullOrEmpty(photo))
                        {
                            try
                            {
                                if (employeeInfo is IDictionary<string, object> dict && dict.TryGetValue("Photo", out var pObj) && pObj is byte[] pBytes && pBytes.Length > 0)
                                {
                                    photo = $"data:image/jpeg;base64,{Convert.ToBase64String(pBytes)}";
                                }
                                else
                                {
                                    byte[]? pDynBytes = employeeInfo.Photo;
                                    if (pDynBytes != null && pDynBytes.Length > 0)
                                    {
                                        photo = $"data:image/jpeg;base64,{Convert.ToBase64String(pDynBytes)}";
                                    }
                                }

                                if (!string.IsNullOrEmpty(photo))
                                {
                                    var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
                                    cache?.Set($"EmployeePhoto_{LoginInfo.EmployeeId.Trim()}", photo, TimeSpan.FromMinutes(30));
                                }
                            }
                            catch { /* best-effort */ }
                        }

                        // Update LoginInfo.CompanyCode if empty
                        if (string.IsNullOrEmpty(LoginInfo.CompanyCode))
                        {
                            try
                            {
                                string? compCode = null;
                                if (employeeInfo is IDictionary<string, object> dict && dict.TryGetValue("CompanyCode", out var ccObj))
                                    compCode = ccObj?.ToString();
                                else
                                    compCode = employeeInfo.CompanyCode?.ToString();

                                if (!string.IsNullOrEmpty(compCode))
                                    LoginInfo.CompanyCode = compCode;
                            }
                            catch { }
                        }
                    }
                }
                catch { /* best-effort */ }
            }

            ViewBag.LoginUserPhoto = !string.IsNullOrEmpty(photo)
                ? photo
                : "/images/user-avatar.svg";
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
        try
        {
            var connectionString = DbContext.Database.GetConnectionString();

            await using var conn = new SqlConnection(connectionString);

            return await conn.QueryFirstOrDefaultAsync(
                "FullEmployeeDetailsGetByid",
                new { EmployeeCode = employeeCode?.Trim() },
                commandType: CommandType.StoredProcedure
            );
        }
        catch
        {
            return null;
        }
    }

    protected string? GetEmployeePhotoBase64(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            return null;

        var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
        var cacheKey = $"EmployeePhoto_{employeeCode.Trim()}";
        if (cache != null && cache.TryGetValue(cacheKey, out string? cachedPhoto) && !string.IsNullOrEmpty(cachedPhoto))
        {
            return cachedPhoto;
        }

        string? photoBase64 = null;

        try
        {
            var empPhoto = DbContext.HrmEmployeePhotos
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeCode || p.EmployeeId == employeeCode.Trim())
                .Select(p => new { p.Photo, p.ImgType })
                .FirstOrDefault();

            if (empPhoto?.Photo != null && empPhoto.Photo.Length > 0)
            {
                var imgType = string.IsNullOrWhiteSpace(empPhoto.ImgType) ? "image/jpeg" : empPhoto.ImgType.Trim();
                if (!imgType.Contains("/"))
                    imgType = $"image/{imgType.TrimStart('.')}";

                photoBase64 = $"data:{imgType};base64,{Convert.ToBase64String(empPhoto.Photo)}";
            }
        }
        catch { /* best-effort */ }

        if (string.IsNullOrEmpty(photoBase64))
        {
            try
            {
                var connectionString = DbContext.Database.GetConnectionString();
                using var conn = new SqlConnection(connectionString);
                var row = conn.QueryFirstOrDefault(
                    "FullEmployeeDetailsGetByid",
                    new { EmployeeCode = employeeCode.Trim() },
                    commandType: CommandType.StoredProcedure
                );

                if (row is IDictionary<string, object> dict && dict.TryGetValue("Photo", out var pObj) && pObj is byte[] pBytes && pBytes.Length > 0)
                {
                    photoBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(pBytes)}";
                }
            }
            catch { /* best-effort */ }
        }

        if (!string.IsNullOrEmpty(photoBase64))
        {
            cache?.Set(cacheKey, photoBase64, TimeSpan.FromMinutes(30));
        }

        return photoBase64;
    }

    protected async Task<string?> GetEmployeePhotoBase64Async(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            return null;

        var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
        var cacheKey = $"EmployeePhoto_{employeeCode.Trim()}";
        if (cache != null && cache.TryGetValue(cacheKey, out string? cachedPhoto) && !string.IsNullOrEmpty(cachedPhoto))
        {
            return cachedPhoto;
        }

        string? photoBase64 = null;

        try
        {
            var empPhoto = await DbContext.HrmEmployeePhotos
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeCode || p.EmployeeId == employeeCode.Trim())
                .Select(p => new { p.Photo, p.ImgType })
                .FirstOrDefaultAsync();

            if (empPhoto?.Photo != null && empPhoto.Photo.Length > 0)
            {
                var imgType = string.IsNullOrWhiteSpace(empPhoto.ImgType) ? "image/jpeg" : empPhoto.ImgType.Trim();
                if (!imgType.Contains("/"))
                    imgType = $"image/{imgType.TrimStart('.')}";

                photoBase64 = $"data:{imgType};base64,{Convert.ToBase64String(empPhoto.Photo)}";
            }
        }
        catch { /* best-effort */ }

        if (string.IsNullOrEmpty(photoBase64))
        {
            try
            {
                var connectionString = DbContext.Database.GetConnectionString();
                await using var conn = new SqlConnection(connectionString);
                var row = await conn.QueryFirstOrDefaultAsync(
                    "FullEmployeeDetailsGetByid",
                    new { EmployeeCode = employeeCode.Trim() },
                    commandType: CommandType.StoredProcedure
                );

                if (row is IDictionary<string, object> dict && dict.TryGetValue("Photo", out var pObj) && pObj is byte[] pBytes && pBytes.Length > 0)
                {
                    photoBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(pBytes)}";
                }
            }
            catch { /* best-effort */ }
        }

        if (!string.IsNullOrEmpty(photoBase64))
        {
            cache?.Set(cacheKey, photoBase64, TimeSpan.FromMinutes(30));
        }

        return photoBase64;
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