using GCTL.Data.Context;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.Companies;
using GCTL.Service.DeleteHistories;
using GCTL.Service.Menu;
using GCTL.Service.MenuTab;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GCTL.Framework.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection to configure application services and DbContexts.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the EF Core DbContexts used by the application.
        /// The legacy <c>GctlHrmContext</c> inherits from <c>GCTL_ERP_DBContext</c>,
        /// so we register both using the same connection string.
        /// </summary>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <param name="configuration">Application configuration providing the connection string.</param>
        public static void ConfigureContexts(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("connection")
                ?? throw new InvalidOperationException("Connection string 'connection' not found in appsettings.json.");

            // Identity DbContext – already registered elsewhere (ApplicationDbContext).

            // Primary business DbContext.
            services.AddDbContext<GCTL_ERP_DBContext>(options =>
                options.UseSqlServer(connectionString));

            // Register GctlHrmContext as a scoped alias of GCTL_ERP_DBContext.
            // This satisfies constructors that still depend on the older context name.
            services.AddDbContext<GctlHrmContext>(options =>
                options.UseSqlServer(connectionString));
            // Identity DbContext registration
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        /// <summary>
        /// Registers application services, including the menu service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public static void RegisterApplicationServices(this IServiceCollection services)
        {
            // Generic repository registration (kept from original Program.cs).
            services.AddScoped(typeof(GCTL.Core.Data.IRepository<>), typeof(GCTL.Data.Repositories.GenericRepository<>));

            // Single-generic-argument repository (matches modules like
            // OFRApprovalService that inject IGenericRepository<T> directly,
            // e.g. IGenericRepository<CF_OperationalFundRequisitionEntry>).
            services.AddScoped(typeof(GCTL.Core.Data.IGenericRepository<>), typeof(GCTL.Data.Repositories.GcGenericRepository<>));

            // Dashboard service example – retained from original registration.
            services.AddScoped<GCTL.Service.Dashboard.IDashboardService, GCTL.Service.Dashboard.DashboardService>();

            // Menu service – now resolves because GctlHrmContext is registered.
            services.AddScoped<IMenuService, MenuService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<ICommonService, CommonService>();
            services.AddScoped<IDeleteHistoryService, DeleteHistoryService>();
            services.AddScoped<IMenuTabService, MenuTabService>();
        }
    }
}
