using GCTL.Data.Context;
using GCTL.Data.DapperContext;
using GCTL.Data.Identity;
using GCTL.UI.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using GCTL.Service.Menu;
using GCTL.Core.ViewModels.Menu;
using GCTL.Data.Models;
using GCTL.Framework.Extensions;
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
// EF Core - Identity DbContext
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("connection")
    ?? throw new InvalidOperationException("Connection string 'connection' not found in appsettings.json.");

// Register DB contexts and application services via extension methods
builder.Services.ConfigureContexts(builder.Configuration);
builder.Services.RegisterApplicationServices();

// ---------------------------------------------------------------------
// ASP.NET Core Identity
// ---------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password policy - tune to your organization's requirements
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        options.User.RequireUniqueEmail = true;

        // Lockout after failed attempts
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // Set true once email sending (MailKit) is wired up
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ---------------------------------------------------------------------
// AutoMapper - registers any Profile classes found in this assembly.
// Add your mapping profiles (e.g. EmployeeProfile : Profile) anywhere in
// GCTL.UI.Core and they'll be picked up automatically.
// ---------------------------------------------------------------------
//builder.Services.AddAutoMapper(typeof(Program).Assembly);

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// ---------------------------------------------------------------------
// MVC + Swagger
// ---------------------------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // needed for Identity UI scaffolded pages
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------------------
// Seed default roles / admin user (development convenience)
// ---------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
