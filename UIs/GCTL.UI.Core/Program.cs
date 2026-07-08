using GCTL.Data.Context;
using GCTL.Data.DapperContext;
using GCTL.Data.Identity;
using GCTL.UI.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
// EF Core - Identity DbContext
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("connection")
    ?? throw new InvalidOperationException("Connection string 'connection' not found in appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


Console.WriteLine($"Connection String: {connectionString}");

// ---------------------------------------------------------------------
// EF Core - your business DbContext (scaffolded via EF Power Tools).
// Once you reverse-engineer your real database, this context will pick
// up the generated DbSets automatically. Uses the same connection string
// by default; point it at a different one in appsettings.json if your
// business DB is separate from your auth DB.
// ---------------------------------------------------------------------
builder.Services.AddDbContext<GctlHrmContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------------------------------------------------------------
// Dapper (for stored-procedure-heavy reports)
// ---------------------------------------------------------------------
builder.Services.AddScoped<DapperContext>();

// ---------------------------------------------------------------------
// Generic repository (GCTL.Service module services depend on this).
// Register your I*Service / *Service pairs from GCTL.Service here too,
// one line per module, e.g.:
//   builder.Services.AddScoped<IAdmissionTypeService, AdmissionTypeService>();
// ---------------------------------------------------------------------
builder.Services.AddScoped(typeof(GCTL.Core.Data.IRepository<>), typeof(GCTL.Data.Repositories.GenericRepository<>));

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
builder.Services.AddAutoMapper(typeof(Program).Assembly);

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
