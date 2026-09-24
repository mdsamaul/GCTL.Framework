using GCTL.Core.Data;
using GCTL.Core.ViewModels.CustomerReport;
using GCTL.Data.Models;
using GCTL.Service.CustomerReport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerReportController : BaseController
    {
        #region Fields & Constructor
        private readonly string _connectionString;
        private readonly ICustomerReportService _customerReportService;
        private readonly IRepository<SalesCustomer> caDefCustomerRepository;
        private readonly IRepository<SalesDefCustomerType> caDefCustomerTypeRepository;
        private readonly IRepository<SalesContactPerson> caDefContactPersonRepository;
        private readonly IRepository<HrmDefDesignation> caDefDesignationRepository;

        public CustomerReportController(
            IConfiguration configuration,
            ICustomerReportService customerReportService,
            IRepository<SalesCustomer> caDefCustomerRepository,
            IRepository<SalesDefCustomerType> caDefCustomerTypeRepository,
            IRepository<SalesContactPerson> caDefContactPersonRepository,
            IRepository<HrmDefDesignation> caDefDesignationRepository)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
            _customerReportService = customerReportService;
            this.caDefCustomerRepository = caDefCustomerRepository;
            this.caDefCustomerTypeRepository = caDefCustomerTypeRepository;
            this.caDefContactPersonRepository = caDefContactPersonRepository;
            this.caDefDesignationRepository = caDefDesignationRepository;
        }

        #endregion

        #region Index View

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await PopulateDropdowns();
            return View();
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                var customers = await caDefCustomerRepository.GetAllAsync();
                var customerList = customers?
                    .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId) && !string.IsNullOrWhiteSpace(x.CustomerName))
                    .Select(x => new SelectListItem
                    {
                        Value = x.CustomerId,
                        Text = x.CustomerName
                    })
                    .ToList();
                ViewBag.Customers = new SelectList(customerList ?? new(), "Value", "Text");

                var customerTypes = await caDefCustomerRepository.GetAllAsync();

                var customerTypeList = customerTypes?
                    .Where(x => !string.IsNullOrWhiteSpace(x.CustomerType))
                    .Select(x => x.CustomerType.Trim())
                    .Distinct()
                    .Select(t => new SelectListItem
                    {
                        Value = t,
                        Text = t switch
                        {
                            "0" => "General",
                            "1" => "In House",
                            _ => t
                        }
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                ViewBag.CustomerTypes = new SelectList(customerTypeList ?? new List<SelectListItem>(), "Value", "Text");

                ViewBag.ExportFormats = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Value = "PDF", Text = "PDF Format" },
                new SelectListItem { Value = "Excel", Text = "Excel Format" }
            }, "Value", "Text");
            }
            catch
            {
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.CustomerTypes = new SelectList(new List<string>());
                ViewBag.ExportFormats = new SelectList(new List<string>());
            }
        }

        #endregion

        #region AJAX Data Endpoint

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] CustomerReportFilterDto filter)
        {
            try
            {
                filter ??= new CustomerReportFilterDto();

                var data = await _customerReportService.GetCustomerReportDataAsync(filter);

                int total = data.Count;

                var typeCounts = data
                    .Where(x => !string.IsNullOrWhiteSpace(x.CustomerTypeId))
                    .GroupBy(x => x.CustomerTypeId.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                int inHouse = typeCounts.FirstOrDefault(t => t.Key.Contains("1", StringComparison.OrdinalIgnoreCase)).Value;
                int general = typeCounts.FirstOrDefault(t => t.Key.Contains("0", StringComparison.OrdinalIgnoreCase)).Value;

                var responseData = data.Select((d, idx) => new
                {
                    sl = idx + 1,
                    customerId = d.CustomerId,
                    name = d.CustomerName,
                    address = d.CustomerAddress,
                    phone = d.Phone,
                    fax = d.Fax,
                    email = d.Email,
                    photo = d.CustomerPhoto != null ? Convert.ToBase64String(d.CustomerPhoto) : null,
                    contactPerson = d.ContactPerson,
                    designation = d.Designation,
                    customerType = d.CustomerType,

                    // ===== নতুন =====
                    companyCode = d.CompanyCode,
                    companyName = d.CompanyName,
                    companyAddress1 = d.CompanyAddress1
                });

                return Json(new
                {
                    success = true,
                    total,
                    inHouse,
                    general,
                    customerType = typeCounts.Count,
                    typeCounts,
                    data = responseData
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    total = 0,
                    inHouse = 0,
                    general = 0,
                    customerType = 0,
                    data = new List<object>()
                });
            }
        }

        #endregion

        #region Export Action

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] CustomerReportFilterDto filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                filter ??= new CustomerReportFilterDto();

                if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _customerReportService.GenerateExcelReportAsync(filter);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CustomerReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else
                {
                    var bytes = await _customerReportService.GeneratePdfReportAsync(filter);
                    return File(bytes, "application/pdf", $"CustomerReport_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Report export failed: {ex.Message}");
            }
        }

        #endregion

    }
}
