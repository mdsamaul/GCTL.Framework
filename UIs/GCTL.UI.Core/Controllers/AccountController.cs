//using GCTL.Core.Common;
//using GCTL.Data.Identity;
//using GCTL.UI.Core.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;

//namespace GCTL.UI.Core.Controllers;

//public class AccountController : BaseController
//{
//    private readonly SignInManager<ApplicationUser> _signInManager;
//    private readonly UserManager<ApplicationUser> _userManager;
//    private readonly ILogger<AccountController> _logger;

//    public AccountController(
//        SignInManager<ApplicationUser> signInManager,
//        UserManager<ApplicationUser> userManager,
//        ILogger<AccountController> logger)
//    {
//        _signInManager = signInManager;
//        _userManager = userManager;
//        _logger = logger;
//    }

//    [HttpGet]
//    [AllowAnonymous]
//    public IActionResult Login(string? returnUrl = null)
//    {
//        ViewData["ReturnUrl"] = returnUrl;
//        return View(new LoginViewModel());
//    }

//    [HttpPost]
//    [AllowAnonymous]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
//    {
//        ViewData["ReturnUrl"] = returnUrl;

//        if (!ModelState.IsValid)
//        {
//            return View(model);
//        }

//        var result = await _signInManager.PasswordSignInAsync(
//            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

//        if (result.Succeeded)
//        {
//            _logger.LogInformation("User {Email} signed in.", model.Email);
//            return LocalRedirectOrHome(returnUrl);
//        }

//        if (result.IsLockedOut)
//        {
//            ModelState.AddModelError(string.Empty, "This account is locked out. Try again later.");
//            return View(model);
//        }

//        ModelState.AddModelError(string.Empty, "Invalid email or password.");
//        return View(model);
//    }

//    [HttpGet]
//    [AllowAnonymous]
//    public IActionResult Register()
//    {
//        return View(new RegisterViewModel());
//    }

//    [HttpPost]
//    [AllowAnonymous]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Register(RegisterViewModel model)
//    {
//        if (!ModelState.IsValid)
//        {
//            return View(model);
//        }

//        var user = new ApplicationUser
//        {
//            UserName = model.Email,
//            Email = model.Email,
//            FullName = model.FullName,
//            AccessCode = AccessCodes.User, // default role for self-registered accounts
//            IsActive = true
//        };

//        var result = await _userManager.CreateAsync(user, model.Password);

//        if (result.Succeeded)
//        {
//            await _userManager.AddToRoleAsync(user, "User");
//            _logger.LogInformation("New account created for {Email}.", model.Email);

//            await _signInManager.SignInAsync(user, isPersistent: false);
//            return RedirectToAction("Index", "Home");
//        }

//        foreach (var error in result.Errors)
//        {
//            ModelState.AddModelError(string.Empty, error.Description);
//        }

//        return View(model);
//    }

//    [HttpPost]
//    [Authorize]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Logout()
//    {
//        await _signInManager.SignOutAsync();
//        _logger.LogInformation("User signed out.");
//        return RedirectToAction("Index", "Home");
//    }

//    [HttpGet]
//    [AllowAnonymous]
//    public IActionResult AccessDenied()
//    {
//        return View();
//    }

//    private IActionResult LocalRedirectOrHome(string? returnUrl)
//    {
//        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
//        {
//            return Redirect(returnUrl);
//        }
//        return RedirectToAction("Index", "Home");
//    }
//}

using DocumentFormat.OpenXml.Spreadsheet;
using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Data.Context;
using GCTL.Data.Models;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Security.Claims;
using static QRCoder.PayloadGenerator;

public class AccountController : BaseController
{
    private readonly IRepository<CoreUserInfo> _userRepo;
    private readonly IRepository<CoreUserLog> _logRepo;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IRepository<CoreUserInfo> userRepo,
        IRepository<CoreUserLog> logRepo,
        ILogger<AccountController> logger)
    {
        _userRepo = userRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    //[HttpGet("MigrateLegacyPasswords")]
    //public async Task<IActionResult> MigrateLegacyPasswords([FromServices] IServiceProvider services)
    //{
    //    //if (LoginInfo?.Role != DefaultRoles.Admin)
    //    //    return Forbid();

    //    await LegacyPasswordMigration.RunAsync(services);
    //    return Content("Migration complete. Check logs for counts. DELETE THIS CONTROLLER NOW.");
    //}

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (returnUrl == "/") returnUrl = null;
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (returnUrl == "/") returnUrl = null;
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid) return View(model);

        var users = await _userRepo.FindByAsync(u => u.Username == model.Email);
        var user = users.FirstOrDefault();

        if (user == null || user.Status != "1")
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        bool passwordOk;
        var hasher = new PasswordHasher<CoreUserInfo>();

        if (LegacyPasswordHelper.IsLegacyFormat(user.Password))
        {
            var decoded = LegacyPasswordHelper.Decode(user.Password);
            passwordOk = decoded == model.Password;

            if (passwordOk)
            {
                // migrate in place on successful legacy login
                user.Password = hasher.HashPassword(user, model.Password);
                await _userRepo.UpdateAsync(user);
            }
        }
        else
        {
            var verifyResult = hasher.VerifyHashedPassword(user, user.Password, model.Password);
            passwordOk = verifyResult != PasswordVerificationResult.Failed;

            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = hasher.HashPassword(user, model.Password);
                await _userRepo.UpdateAsync(user);
            }
        }

        if (!passwordOk)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }
        var employeeInfo = await DbContext.HrmEmployees   // confirm exact DbSet name when you have it
            .AsNoTracking()
            .Where(e => e.EmployeeId == user.EmployeeId)
            .Select(e => new { e.CompanyCode })
            .FirstOrDefaultAsync();

        var sessionId = Guid.NewGuid().ToString("N");

        if (user.SingleSession == "Single")
        {
            var openSessions = await _logRepo.FindByAsync(l =>
                l.Username == user.Username &&
                l.LogoutDate == null &&
                l.SessionId != sessionId);

            foreach (var old in openSessions.ToList())
            {
                old.LogoutDate = DateTime.Now;
                await _logRepo.UpdateAsync(old);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("EmployeeId", user.EmployeeId ?? string.Empty),
            new("AccessCode", user.AccessCode ?? string.Empty),
            new("CompanyCode", employeeInfo?.CompanyCode ?? string.Empty),
            new(ClaimTypes.Role, user.Role ?? string.Empty),
            new("SessionId", sessionId),
            new("IPAddress", GetLocalIP()),
            new("MacAddress", GetMacAddress()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await _logRepo.AddAsync(new CoreUserLog
        {
            Username = user.Username,
            LoginDate = DateTime.Now,
            Lip = HttpContext.Connection.RemoteIpAddress?.ToString(),
            WorkStation = HttpContext.Request.Headers["User-Agent"].ToString(),
            SessionId = sessionId
        });

        _logger.LogInformation("User {Username} signed in.", user.Username);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        return LocalRedirectOrHome(returnUrl);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;

        var openLogs = await _logRepo.FindByAsync(l => l.Username == username && l.LogoutDate == null);
        var lastLog = openLogs.OrderByDescending(l => l.LoginDate).FirstOrDefault();
        if (lastLog != null)
        {
            lastLog.LogoutDate = DateTime.Now;
            await _logRepo.UpdateAsync(lastLog);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Username} signed out.", username);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");

    public string GetLocalIP()
    {
        string ipAddress = string.Empty;
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        foreach (var networkInterface in networkInterfaces)
        {
            var properties = networkInterface.GetIPProperties();
            var ipv4Address = properties.UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipv4Address != null)
            {
                ipAddress = ipv4Address.Address.ToString();
                break;
            }
        }

        return ipAddress;
    }


    public string GetMacAddress()
    {
        string macAddress = string.Empty;
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
       .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
        foreach (var networkInterface in networkInterfaces)
        {
            macAddress = networkInterface.GetPhysicalAddress().ToString();
            if (!string.IsNullOrEmpty(macAddress))
            {
                break;
            }
        }

        return macAddress;
    }
}