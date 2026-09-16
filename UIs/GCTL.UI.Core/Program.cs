using GCTL.Core.Data;
using GCTL.Core.ViewModels.Menu;
using GCTL.Data.Context;
using GCTL.Data.DapperContext;
using GCTL.Data.Models;
using GCTL.Framework.Extensions;
using GCTL.Service.Menu;
//using GCTL.Data.Identity;
using GCTL.UI.Core;
using GCTL.UI.Core.Filters;
using GCTL.UI.Core.Helpers.BackgroundServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Serilog
// ---------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ---------------------------------------------------------------------
// Data Protection (Persist keys across app restarts)
// ---------------------------------------------------------------------
var keysFolder = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys"));
if (!keysFolder.Exists)
{
    keysFolder.Create();
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysFolder)
    .SetApplicationName("GCTL.UI.Core");

// ---------------------------------------------------------------------
// EF Core - Identity DbContext (Auto-decrypts if password is encrypted with ENC(...))
// ---------------------------------------------------------------------
var rawConnectionString = builder.Configuration.GetConnectionString("connection")
    ?? throw new InvalidOperationException("Connection string 'connection' not found in appsettings.json.");

var connectionString = GCTL.UI.Core.Helpers.ConnectionEncryptor.DecryptConnectionString(rawConnectionString);
builder.Configuration["ConnectionStrings:connection"] = connectionString;

// Register DB contexts and application services via extension methods
builder.Services.ConfigureContexts(builder.Configuration);
builder.Services.RegisterApplicationServices();
builder.Services.ReadConfigaration(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var expiresUtc = context.Properties.ExpiresUtc;
                if (expiresUtc.HasValue && expiresUtc.Value < DateTimeOffset.UtcNow)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                //var sessionId = context.Principal?.FindFirst("SessionId")?.Value;
                //if (!string.IsNullOrEmpty(sessionId))
                //{
                //    var cache = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                //    cache?.Set($"UserSession_LastActivity_{sessionId}", DateTime.Now, TimeSpan.FromHours(1));

                //    var logRepo = context.HttpContext.RequestServices.GetRequiredService<IRepository<CoreUserLog>>();
                //    var stillOpen = (await logRepo.FindByAsync(l => l.SessionId == sessionId))
                //        .FirstOrDefault(l => l.LogoutDate == null);

                //    if (stillOpen == null) // this session's row was closed (superseded or timed out)
                //    {
                //        context.RejectPrincipal();
                //        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                //    }
                //}
            }
        };
    });
//builder.Services.AddHostedService<StaleSessionSweepService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ---------------------------------------------------------------------
// MVC + Swagger
// ---------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<BreadcrumbActionFilter>();
});
builder.Services.AddRazorPages(); // needed for Identity UI scaffolded pages
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Make HttpContext available to non-controller code via IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Quest PDF License
QuestPDF.Settings.License = LicenseType.Community;
ExcelPackage.License.SetNonCommercialOrganization("GCTL");

var app = builder.Build();

// Expose the DI service provider to the ServiceLocator for extension helpers.
GCTL.UI.Core.Helpers.ServiceLocator.ServiceProvider = app.Services;

// ---------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

var docInfoDir = Path.Combine(app.Environment.ContentRootPath, "DocumentInfo");
if (!Directory.Exists(docInfoDir))
{
    Directory.CreateDirectory(docInfoDir);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(docInfoDir),
    RequestPath = "/DocumentInfo"
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();