using DocumentFormat.OpenXml.Bibliography;
using GCTL.Core.Configurations;
using GCTL.Core.Interfaces.Services;
using GCTL.Data.Context;
using GCTL.Data.Models;
using GCTL.Service.AdmissionTypes;
using GCTL.Service.BankBranchInformations;
using GCTL.Service.BankInformations;
using GCTL.Service.BillService;
using GCTL.Service.BloodGroups;
using GCTL.Service.BranchAddress;
using GCTL.Service.BranchesTypeInfo;
using GCTL.Service.CfDefPlaceOfLoading;
using GCTL.Service.ChartOfAccounts;
using GCTL.Service.ChartOfAccounts.ControlLedger;
using GCTL.Service.ChartOfAccounts.GeneralLedger;
using GCTL.Service.ChartOfAccounts.GroupLedger;
using GCTL.Service.ChartOfAccounts.SubControlLedger;
using GCTL.Service.ChartOfAccounts.SubSidiaryLedger;
using GCTL.Service.ClientInformationEntry;
//using GCTL.Service.CNFMaster.VendorPrefixs;
using GCTL.Service.Common;
using GCTL.Service.Companies;
using GCTL.Service.CompanyInfo;
using GCTL.Service.ContactPersonInfo;
using GCTL.Service.Core_Countrys;
using GCTL.Service.CoreBankAccountInformations;
using GCTL.Service.CoreServiceType;
using GCTL.Service.Country;
using GCTL.Service.CurrencyType;
using GCTL.Service.CustomerRelationshipManagement.AddContactPerson;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.CustomerRelationshipManagement.AddSalesDeliveryLocation;
using GCTL.Service.CustomerReport;
using GCTL.Service.CustomerType;
using GCTL.Service.CustomerWiseApprovalRate;
using GCTL.Service.DailyJobUpdateEntry;
using GCTL.Service.DeleteHistories;
using GCTL.Service.Departments;
using GCTL.Service.Designations;
using GCTL.Service.Districts;
using GCTL.Service.DocumentationReport;
using GCTL.Service.JobStatusReport;
using GCTL.Service.EmailService;
using GCTL.Service.EmployeeContactInfos;
using GCTL.Service.EmployeeOfficialInfo;
using GCTL.Service.ExpenseHead;
using GCTL.Service.ExporterInfo;
using GCTL.Service.Gender;
using GCTL.Service.HolidayLIst;
using GCTL.Service.HRMCompanyWeekEnds;
using GCTL.Service.HrmDefEmpTypes;
using GCTL.Service.HrmEmployees2;
using GCTL.Service.ImporterInfo;
using GCTL.Service.ManageNoticeEntries;
using GCTL.Service.Menu;
using GCTL.Service.MenuBreadcrumbs;
using GCTL.Service.MenuTab;
using GCTL.Service.ModeType;
using GCTL.Service.Nationalitys;
using GCTL.Service.OFRAdjustApproval;
using GCTL.Service.OFRApproval;
using GCTL.Service.OFRBillAdjust;
using GCTL.Service.OpeningBalance;
using GCTL.Service.OperationalFund;
using GCTL.Service.PaymentModes;
using GCTL.Service.PaymentReceiveEntry;
using GCTL.Service.PaymentTerms;
using GCTL.Service.PaymentType;
using GCTL.Service.Permissions;
using GCTL.Service.PortName;
using GCTL.Service.PortType;
using GCTL.Service.Relationships;
using GCTL.Service.Religions;
using GCTL.Service.SepDocumentation;
using GCTL.Service.Services;
using GCTL.Service.ShipmentMode;
using GCTL.Service.ShipmentModeLedgerSetup;
using GCTL.Service.SupplierCategory;
using GCTL.Service.SupplierInformation;
using GCTL.Service.SupplierOrigin;
using GCTL.Service.SupplierType;
using GCTL.Service.TransportInformation;
using GCTL.Service.TransportType;
using GCTL.Service.UnitType;
using GCTL.Service.Update;
using GCTL.Service.Users;
using GCTL.Service.VendorInfo;
using GCTL.Service.VendorPrefix;
using GCTL.Service.VoucherEntry;
using GCTL.Service.VoucherType;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddScoped<IManageNoticeService, ManageNoticeService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ICommonService, CommonService>();
            services.AddScoped<IDeleteHistoryService, DeleteHistoryService>();
            services.AddScoped<IBranchTypeInfoService, BranchTypeInfoService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDesignationService, DesignationService>();
            services.AddScoped<IExpenseHead, ExpenseHeadService>();
            services.AddScoped<IShipmentMode, ShipmentModeService>();
            services.AddScoped<IServiceType, ServiceTypeService>();
            services.AddScoped<IholidayListService, HolidayListService>();
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
            services.AddScoped<ICountryService, CountryService>();
            services.AddScoped<IHRMCompanyWeekEndService, HRMCompanyWeekEndService>();
            services.AddScoped<IPortTypeService, PortTypeService>();
            //services.AddScoped<IVendorPrefixService, VendorPrefixService>();
            services.AddScoped<IPortNameService, PortNameService>();
            services.AddScoped<ITransportInformationService, TransportInformationService>();
            services.AddScoped<ITransportTypeService, TransportTypeService>();
            services.AddScoped<IImporterInfoService, ImporterInfoService>();
            services.AddScoped<IModeTypeService, ModeTypeService>();
            services.AddScoped<IExporterInfoService, ExporterInfoService>();
            services.AddScoped<IVendorInfoService, VendorInfoService>();
            services.AddScoped<IBranchAddressService, BranchAddressService>();
            services.AddScoped<IVendorPrefixService, VendorPrefixService>();
            services.AddScoped<IHrmEmployee2Service, HrmEmployee2Service>();
            services.AddScoped<IEmployeeOfficialInfoService, EmployeeOfficialInfoService>();
            services.AddScoped<IContactPersonInfoService, ContactPersonInfoService>();
            services.AddScoped<IHrmEmployeeBulkImportService, HrmEmployeeBulkImportService>();
            services.AddScoped<IEmployeeContactInfoService, EmployeeContactInfoService>();
            services.AddScoped<IRelationshipsService, RelationshipsService>();
            services.AddScoped<IDistrictsService, DistrictsService>();
            services.AddScoped<IClientInformationEntryService, ClientInformationEntryService>();
            services.AddScoped<ICompanyInfoSupService, CompanyInfoSupService>();
            services.AddScoped<ICustomerTypeService, CustomerTypeService>();
            services.AddScoped<ICustomerReportService, CustomerReportService>();


            services.AddScoped<IMenuTabService, MenuTabService>();

            //services.AddScoped<IJobService, JobService>();
            services.AddScoped<IBillService, BillService>();
            services.AddScoped<ISepDocumentationService, SepDocumentationService>();
            services.AddScoped<ICustomerWiseApprovalRateService, CustomerWiseApprovalRateService>();

            services.AddScoped<IAccessCodeService, AccessCodeService>();

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ICfDefPlaceOfLoadingService, CfDefPlaceOfLoadingService>();
            services.AddScoped<IHrmDefEmpTypeService, HrmDefEmpTypeService>();

            services.AddScoped<IUserService, UserService>();

            #region Added by Abu Sayed
            //services.AddScoped<ICoreCountryService, CoreCountryService>();
            //services.AddScoped<IHrmDefDesignationService, HrmDefDesignationService>();
            //services.AddScoped<IVendorPrefixService, VendorPrefixService>();
            services.AddScoped<ICurrencyType, CurrencyTypeService>();
            services.AddScoped<IUnitType, UnitTypeService>();
            services.AddScoped<IShipmentMode, ShipmentModeService>();
            services.AddScoped<IExpenseHead, ExpenseHeadService>();
            services.AddScoped<IServiceType, ServiceTypeService>();
            services.AddScoped<ISalesCustomerService, SalesCustomerService>();
            services.AddScoped<ISalesDeliveryLocationService, SalesDeliveryLocationService>();
            //services.AddScoped<IDepartment, GCTL.Service.MasterSetup.HrmDefDepartment.DepartmentService>();
            //services.AddScoped<ICoreBranch, CoreBranchService>();
            //services.AddScoped<IBankInfo, BankInfoService>();
            //services.AddScoped<IBankBranch, BankBranchService>();
            //services.AddScoped<IBankAccount, BankAccountService>();
            //services.AddScoped<ICoreCompany, CoreCompanyService>();
            services.AddScoped<IContactPersonService, ContactPersonService>();
            services.AddScoped<IGroupLedger, GroupLedgerService>();
            services.AddScoped<IContrlLedger, ControlLedgerService>();
            services.AddScoped<ISubControlLedger, SubControlLedgerService>();
            services.AddScoped<ISubSidiaryLedger, SubSidiaryLedgerService>();
            services.AddScoped<IGeneralLedger, GeneralLedgerService>();
            services.AddScoped<LedgerReportService>();
            services.AddScoped<IVoucherType, VoucherTypeService>();
            services.AddScoped<IOpeningBalance, OpeningBalanceService>();
            services.AddScoped<IVoucherEntry, VoucherEntryService>();
            services.AddScoped<IVoucherDetails, VoucherDetailsService>();
            services.AddScoped<VoucherReportService>();
            //services.AddScoped<Ioperational, OperationalService>();
            services.AddScoped<IDeleteHistoryService, DeleteHistoryService>();
            //services.AddScoped<IOFRApproval, OFRApprovalService>();
            //services.AddScoped<IOFRBillAdjust, OFRBillAdjustService>();
            //services.AddScoped<IOFRAdjustApproval, OFRAdjustApprovalService>();
            #endregion
            services.AddScoped<IShipmentStatusService, ShipmentStatusService>();
            services.AddScoped<IDailyJobUpdateEntryService, DailyJobUpdateEntryService>();
            services.AddScoped<Ioperational, OperationalService>();
            services.AddScoped<IMenuBreadcrumbService, MenuBreadcrumbService>();
            services.AddScoped<IShipmentModeLedgerSetupService, ShipmentModeLedgerSetupService>();
            services.AddScoped<IOFRApproval, OFRApprovalService>();
            services.AddScoped<IOFRBillAdjust, OFRBillAdjustService>();
            services.AddScoped<IOFRAdjustApproval, OFRAdjustApprovalService>();
            services.AddScoped<IPaymentReceiveEntryService, PaymentReceiveEntryService>();
            services.AddScoped<IDocumentationReportService, DocumentationReportService>();
            services.AddScoped<IJobStatusReportService, JobStatusReportService>();
            services.AddMemoryCache();
        }

        public static void ReadConfigaration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApplicationSettings>(configuration.GetSection("ApplicationSettings"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ApplicationSettings>>().Value);
        }
    }
}
