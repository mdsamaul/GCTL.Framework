using GCTL.Core.ViewModels.JobStatusReport;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.JobStatusReport;
using GCTL.Service.PortType;
using GCTL.Service.ShipmentMode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class JobStatusReportController : BaseController
    {
        private readonly IJobStatusReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly IPortTypeService _portTypeService;
        private readonly ISalesCustomerService _customerService;

        public JobStatusReportController(
            IJobStatusReportService reportService,
            IShipmentMode shipmentMode,
            IPortTypeService portTypeService,
            ISalesCustomerService customerService) : base()
        {
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _portTypeService = portTypeService;
            _customerService = customerService;
        }

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
                var rawShipmentModes = await _shipmentMode.GetAllExpenseTypesAsync();
                var shipmentModes = rawShipmentModes?
                    .Where(x => !string.IsNullOrWhiteSpace(x.ExpenseTypeID) && !string.IsNullOrWhiteSpace(x.ExpenseType))
                    .ToList();
                ViewBag.ShipmentModes = new SelectList(shipmentModes ?? new(), "ExpenseTypeID", "ExpenseType");

                var rawPortTypes = await _portTypeService.SelectionPortTypeAsync();
                var portTypes = rawPortTypes?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name))
                    .ToList();
                ViewBag.PortTypes = new SelectList(portTypes ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var customers = await _customerService.SelectionCustomerAsync();
                ViewBag.Customers = new SelectList(customers ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                ViewBag.JobStatuses = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- Job Status --" },
                    new SelectListItem { Value = "Pending", Text = "Pending" },
                    new SelectListItem { Value = "In Transit", Text = "In Transit" },
                    new SelectListItem { Value = "Delivered", Text = "Delivered" }
                }, "Value", "Text");

                ViewBag.ExportFormats = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "PDF", Text = "PDF Format" },
                    new SelectListItem { Value = "Excel", Text = "Excel Format" }
                }, "Value", "Text");
            }
            catch
            {
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.PortTypes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.JobStatuses = new SelectList(new List<string>());
                ViewBag.ExportFormats = new SelectList(new List<string>());
            }
        }
        #endregion

        #region AJAX Data Endpoint (Parallel Execution in Milliseconds)
        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] JobStatusReportFilterVM filter)
        {
            try
            {
                filter ??= new JobStatusReportFilterVM();

                // Run summary, Sea and Air jobs simultaneously in parallel
                var summaryTask = _reportService.GetSummaryAsync(filter.CustomerId);
                var seaTask = _reportService.GetSeaJobsAsync(filter);
                var airTask = _reportService.GetAirJobsAsync(filter);

                await Task.WhenAll(summaryTask, seaTask, airTask);

                var summary = summaryTask.Result;
                var seaJobs = seaTask.Result;
                var airJobs = airTask.Result;

                // Calculate status counts
                int calculatedTotal = seaJobs.Count + airJobs.Count;
                int calculatedDelivered = seaJobs.Count(x => x.StatusClass == "delivered") + airJobs.Count(x => x.StatusClass == "delivered");
                int calculatedTransit = seaJobs.Count(x => x.StatusClass == "transit") + airJobs.Count(x => x.StatusClass == "transit");
                int calculatedPending = seaJobs.Count(x => x.StatusClass == "pending") + airJobs.Count(x => x.StatusClass == "pending");

                bool hasSpecificFilter = !string.IsNullOrWhiteSpace(filter.FromDate) ||
                                         !string.IsNullOrWhiteSpace(filter.ShipmentMode) ||
                                         !string.IsNullOrWhiteSpace(filter.PortType) ||
                                         !string.IsNullOrWhiteSpace(filter.JobStatus);

                int totalCount = hasSpecificFilter ? calculatedTotal : (summary.TotalJobs > 0 ? summary.TotalJobs : calculatedTotal);
                int pendingCount = hasSpecificFilter ? calculatedPending : (summary.PendingJobs > 0 ? summary.PendingJobs : calculatedPending);
                int transitCount = hasSpecificFilter ? calculatedTransit : (summary.TransitJobs > 0 ? summary.TransitJobs : calculatedTransit);
                int deliveredCount = hasSpecificFilter ? calculatedDelivered : (summary.DeliveredJobs > 0 ? summary.DeliveredJobs : calculatedDelivered);

                return Json(new
                {
                    success = true,
                    total = totalCount,
                    pending = pendingCount,
                    transit = transitCount,
                    delivered = deliveredCount,
                    seaData = seaJobs,
                    airData = airJobs
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    total = 0,
                    pending = 0,
                    transit = 0,
                    delivered = 0,
                    seaData = new List<SeaJobReportItemDTO>(),
                    airData = new List<AirJobReportItemDTO>()
                });
            }
        }
        #endregion

        #region Export Action
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] JobStatusReportFilterVM filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                filter ??= new JobStatusReportFilterVM();

                if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _reportService.GenerateExcelReportAsync(filter);
                    string fileName = $"JobStatusReport_{filter.ActiveTab ?? "All"}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var bytes = await _reportService.GeneratePdfReportAsync(filter);
                    string fileName = $"JobStatusReport_{filter.ActiveTab ?? "All"}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    return File(bytes, "application/pdf", fileName);
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
