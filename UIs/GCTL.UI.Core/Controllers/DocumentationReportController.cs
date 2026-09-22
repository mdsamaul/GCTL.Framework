using GCTL.Core.Data;
using GCTL.Core.ViewModels.DocumentationReport;
using GCTL.Data.Models;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.DocumentationReport;
using GCTL.Service.PortType;
using GCTL.Service.ShipmentMode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class DocumentationReportController : BaseController
    {
        private readonly IDocumentationReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly IPortTypeService _portTypeService;
        private readonly ISalesCustomerService _customerService;
        private readonly IGenericRepository<SalesDefShipmentStatusInfo> _shipmentStatusRepo;
        private readonly IGenericRepository<SepDocumentation> _sepDocRepo;

        public DocumentationReportController(
            IDocumentationReportService reportService,
            IShipmentMode shipmentMode,
            IPortTypeService portTypeService,
            ISalesCustomerService customerService,
            IGenericRepository<SalesDefShipmentStatusInfo> shipmentStatusRepo,
            IGenericRepository<SepDocumentation> sepDocRepo) : base()
        {
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _portTypeService = portTypeService;
            _customerService = customerService;
            _shipmentStatusRepo = shipmentStatusRepo;
            _sepDocRepo = sepDocRepo;
        }

        #region Index
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
                var shipmentModes = await _shipmentMode.GetAllExpenseTypesAsync();
                ViewBag.ShipmentModes = new SelectList(shipmentModes ?? new(), "ExpenseTypeID", "ExpenseType");

                var portTypes = await _portTypeService.SelectionPortTypeAsync();
                ViewBag.PortTypes = new SelectList(portTypes ?? System.Linq.Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var customers = await _customerService.SelectionCustomerAsync();
                ViewBag.Customers = new SelectList(customers ?? System.Linq.Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var statuses = await _shipmentStatusRepo.All()
                    .OrderBy(x => x.ShipmentStatusId)
                    .Select(x => new { id = x.ShipmentStatusId, name = x.ShipmentStatus })
                    .ToListAsync();
                ViewBag.JobStatuses = new SelectList(statuses, "id", "name");

                var jobNos = await _sepDocRepo.All()
                    .Where(x => !string.IsNullOrEmpty(x.JobNo))
                    .OrderByDescending(x => x.Tc)
                    .Select(x => x.JobNo)
                    .Distinct()
                    .Take(300)
                    .ToListAsync();
                ViewBag.JobNos = new SelectList(jobNos);

                ViewBag.Formats = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "PDF", Text = "PDF" },
                    new SelectListItem { Value = "Excel", Text = "Excel" }
                }, "Value", "Text");
            }
            catch (Exception ex)
            {
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.PortTypes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.JobStatuses = new SelectList(new List<string>());
                ViewBag.JobNos = new SelectList(new List<string>());
                ViewBag.Formats = new SelectList(new List<string> { "PDF", "Excel" });
                Console.WriteLine($"Error loading dropdowns: {ex.Message}");
            }
        }
        #endregion

        #region Dropdown Cascades
        [HttpGet]
        public async Task<IActionResult> GetJobNos(string? customerId)
        {
            try
            {
                var query = _sepDocRepo.All().AsNoTracking().Where(x => !string.IsNullOrEmpty(x.JobNo));

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    query = query.Where(x => x.CustomerId == customerId.Trim());
                }

                var list = await query
                    .OrderByDescending(x => x.Tc)
                    .Select(x => x.JobNo)
                    .Distinct()
                    .ToListAsync();

                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(new List<string>());
            }
        }
        #endregion

        #region Preview Report
        [HttpGet]
        public async Task<IActionResult> Preview([FromQuery] DocumentationReportFilterVM filter)
        {
            try
            {
                // Date range validation
                if (!string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate))
                {
                    bool validFrom = DateTime.TryParseExact(filter.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fDate);
                    bool validTo = DateTime.TryParseExact(filter.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tDate);

                    if (validFrom && validTo && fDate > tDate)
                    {
                        return BadRequest("From Date cannot be greater than To Date.");
                    }
                }

                string format = (filter.Format ?? "PDF").Trim().ToLower();

                if (format.Contains("excel") || format.Contains("xlsx"))
                {
                    byte[] excelData = await _reportService.GenerateExcelReportAsync(filter);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Job_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                }
                else if (format.Contains("word") || format.Contains("doc"))
                {
                    byte[] excelData = await _reportService.GenerateExcelReportAsync(filter);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Job_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                }
                else
                {
                    byte[] pdfData = await _reportService.GeneratePdfReportAsync(filter);
                    return File(pdfData, "application/pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred previewing the report: {ex.Message}");
            }
        }
        #endregion

        #region Download Report
        [HttpGet]
        public async Task<IActionResult> Download([FromQuery] DocumentationReportFilterVM filter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filter.Format))
                {
                    return BadRequest("Please select a Format (PDF or Excel).");
                }

                // Date range validation
                if (!string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate))
                {
                    bool validFrom = DateTime.TryParseExact(filter.FromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fDate);
                    bool validTo = DateTime.TryParseExact(filter.ToDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tDate);

                    if (validFrom && validTo && fDate > tDate)
                    {
                        return BadRequest("From Date cannot be greater than To Date.");
                    }
                }

                string format = filter.Format.Trim().ToLower();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (format.Contains("excel") || format.Contains("xlsx"))
                {
                    byte[] excelData = await _reportService.GenerateExcelReportAsync(filter);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Job_Report_{timestamp}.xlsx");
                }
                else if (format.Contains("word") || format.Contains("doc"))
                {
                    byte[] excelData = await _reportService.GenerateExcelReportAsync(filter);
                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Job_Report_{timestamp}.xlsx");
                }
                else
                {
                    byte[] pdfData = await _reportService.GeneratePdfReportAsync(filter);
                    return File(pdfData, "application/pdf", $"Job_Report_{timestamp}.pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred generating the report: {ex.Message}");
            }
        }
        #endregion
    }
}
