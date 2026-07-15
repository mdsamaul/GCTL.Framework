using GCTL.Data.Context;
using GCTL.Data.Models;
using GCTL.Service.BankBranchInformations;
using GCTL.Service.BankInformations;
using GCTL.Service.BloodGroups;
using GCTL.Service.BranchesTypeInfo;
using GCTL.Service.ChartOfAccounts.ControlLedger;
using GCTL.Service.ChartOfAccounts.GeneralLedger;
using GCTL.Service.ChartOfAccounts.GroupLedger;
using GCTL.Service.ChartOfAccounts.SubControlLedger;
using GCTL.Service.ChartOfAccounts.SubSidiaryLedger;
using GCTL.Service.Common;
using GCTL.Service.Companies;
using GCTL.Service.Core_Countrys;
using GCTL.Service.CoreBankAccountInformations;
using GCTL.Service.CoreServiceType;
using GCTL.Service.CurrencyType;
using GCTL.Service.CustomerRelationshipManagement.AddContactPerson;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.CustomerRelationshipManagement.AddSalesDeliveryLocation;
using GCTL.Service.DeleteHistories;
using GCTL.Service.Departments;
using GCTL.Service.Designations;
using GCTL.Service.ExpenseHead;
using GCTL.Service.Gender;
using GCTL.Service.HolidayLIst;
using GCTL.Service.Menu;

using GCTL.Service.MenuTab;
using GCTL.Service.Nationalitys;
using GCTL.Service.OpeningBalance;
using GCTL.Service.PaymentModes;
using GCTL.Service.PaymentTerms;
using GCTL.Service.PaymentType;
using GCTL.Service.Religions;
using GCTL.Service.SepDocumentation;
using GCTL.Service.ShipmentMode;
using GCTL.Service.SupplierCategory;
using GCTL.Service.SupplierInformation;
using GCTL.Service.SupplierOrigin;
using GCTL.Service.SupplierType;
using GCTL.Service.UnitType;
using GCTL.Service.Update;

using GCTL.Service.Users;
using GCTL.Service.VoucherType;
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
            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(connectionString));
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
            services.AddScoped<IBranchTypeInfoService, BranchTypeInfoService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDesignationService, DesignationService>();
            services.AddScoped<IExpenseHead, ExpenseHeadService>();
            services.AddScoped<IShipmentMode, ShipmentModeService>();
            services.AddScoped<IServiceType, ServiceTypeService>();
            services.AddScoped<IReligionService, ReligionService>();
            services.AddScoped<IBloodGroupService, BloodGroupService>();
            services.AddScoped<INationalitysService, NationalitysService>();
            services.AddScoped<IGenderService, GenderService>();
            services.AddScoped<ICore_CountryService, Core_CountryService>();
            services.AddScoped<IBankInformationsService, BankInformationsService>();
            services.AddScoped<ISalesDefBankBranchInfosService, SalesDefBankBranchInfosService>();
            services.AddScoped<ICoreBankAccountInformationService, CoreBankAccountInformationService>();
            services.AddScoped<ICurrencyType, CurrencyTypeService>();
            services.AddScoped<IUnitType, UnitTypeService>();
            services.AddScoped<IPaymentTypeService, PaymentTypeService>();
            services.AddScoped<IPaymentTermsService, PaymentTermsService>();
            services.AddScoped<IPaymentModeService, PaymentModeService>();
            services.AddScoped<ISalesCustomerService, SalesCustomerService>();
            services.AddScoped<ISalesDeliveryLocationService, SalesDeliveryLocationService>();
            services.AddScoped<IContactPersonService, ContactPersonService>();
            services.AddScoped<ISupplierTypeService, SupplierTypeService>();
            services.AddScoped<ISupplierOriginService, SupplierOriginService>();
            services.AddScoped<ISupplierCategoryService, SupplierCategoryService>();
            services.AddScoped<ISupplierInformationService, SupplierInformationService>();
            services.AddScoped<IContrlLedger, ControlLedgerService>();
            services.AddScoped<IGeneralLedger, GeneralLedgerService>();
            services.AddScoped<IGroupLedger, GroupLedgerService>();
            services.AddScoped<ISubControlLedger, SubControlLedgerService>();
            services.AddScoped<IVoucherType, VoucherTypeService>();
            services.AddScoped<IOpeningBalance, OpeningBalanceService>();

            services.AddScoped<IMenuTabService, MenuTabService>();

            services.AddScoped<IJobService, JobService>();
            services.AddScoped<ISepDocumentationService, SepDocumentationService>();

            services.AddScoped<IAccessCodeService, AccessCodeService>();
            services.AddScoped<IUserService, UserService>();
        }
    }
}
