using Dapper;
using GCTL.Core.DataTables;
using GCTL.Data.Context;
using GCTL.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GCTL.UI.Core.Controllers;

/// <summary>
/// Rebuilt from your original BaseController.cs.
///
/// KEY DIFFERENCE FROM YOUR ORIGINAL: your old BaseController checked
/// `HttpContext.Session.Get&lt;UserInfoViewModel&gt;(...)` and manually redirected
/// to Login if the session was empty. This project uses ASP.NET Core Identity
/// (cookie auth) instead of raw session state, so that job is now handled by
/// the [Authorize] attribute + the cookie's LoginPath (set in Program.cs) -
/// no manual OnActionExecuting redirect logic needed, and no dependency on
/// UserInfoViewModel/ApplicationConstants (neither of those files were in
/// your uploaded zip, so they couldn't be ported as-is anyway).
///
/// Everything else - LoginInfo, DataTablesResult, AllInfoEmployeeById - is
/// kept with the same names/shapes so your existing controllers need minimal
/// changes when you port them over.
/// </summary>
[Authorize]
public abstract class BaseController : Controller
{
    protected GctlHrmContext DbContext =>
        (GctlHrmContext)HttpContext.RequestServices.GetService(typeof(GctlHrmContext))!;

    private UserManager<ApplicationUser>? _userManager;
    protected UserManager<ApplicationUser> UserManager =>
        _userManager ??= (UserManager<ApplicationUser>)HttpContext.RequestServices
            .GetService(typeof(UserManager<ApplicationUser>))!;

    /// <summary>
    /// Replaces your original session-based LoginInfo. Pulls the signed-in
    /// user's info from Identity (via claims) instead of session state.
    /// Extend ApplicationUser (in GCTL.Data/Identity/ApplicationUser.cs) with
    /// any extra fields your views need, e.g. EmployeeId is already there.
    /// </summary>
    protected ApplicationUser? LoginInfo => _loginInfo ??= LoadLoginInfo();
    private ApplicationUser? _loginInfo;

    private ApplicationUser? LoadLoginInfo()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        return UserManager.GetUserAsync(User).GetAwaiter().GetResult();
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        // Auth redirect is handled by [Authorize] + cookie LoginPath now.
        // We still populate ViewBag for the layout (photo, username) same as before.
        if (LoginInfo != null)
        {
            ViewBag.LoginUser = LoginInfo.FullName ?? LoginInfo.UserName;
            ViewBag.LoginUserPhoto = "/images/UserIcon.png";

            if (!string.IsNullOrEmpty(LoginInfo.EmployeeId))
            {
                try
                {
                    var employeePhoto = GetEmployeePhotoBase64(LoginInfo.EmployeeId);
                    if (!string.IsNullOrEmpty(employeePhoto))
                    {
                        ViewBag.LoginUserPhoto = employeePhoto;
                    }
                }
                catch
                {
                    // Photo lookup is best-effort; don't block page load if it fails
                    // (e.g. stored procedure not yet scaffolded/created).
                }
            }
        }

        base.OnActionExecuting(context);
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

    /// <summary>
    /// Restored from your original AllInfoEmployeeById. Calls the same
    /// FullEmployeeDetailsGetByid stored procedure via Dapper. Update the
    /// return type from `dynamic` to your real FullEmployeeDetailsGetByIdViewModel
    /// once you port that view model over (it wasn't present in your uploaded zip).
    /// </summary>
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
        // Placeholder for your PhotoBase64 lookup logic from AllInfoEmployeeById.
        // Wire this up once FullEmployeeDetailsGetByIdViewModel is ported.
        return null;
    }
}
