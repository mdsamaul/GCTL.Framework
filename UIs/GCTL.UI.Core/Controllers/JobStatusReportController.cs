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
                var custList = customers?.Where(x => !string.IsNullOrWhiteSpace(x.Code) && !string.IsNullOrWhiteSpace(x.Name)).ToList() ?? new();
                ViewBag.Customers = new SelectList(custList, "Code", "Name");
                ViewBag.DefaultCustomerId = custList.FirstOrDefault()?.Code ?? "CUS000001";

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

                string activeMode = (filter.ActiveTab ?? "").Trim();
                string shipmentModeVal = (filter.ShipmentMode ?? "").Trim();

                // Accurately determine whether Sea or Air is requested
                bool isAirMode;
                if (string.Equals(shipmentModeVal, "Sea", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(activeMode, "Sea", StringComparison.OrdinalIgnoreCase))
                {
                    isAirMode = false;
                }
                else if (string.Equals(shipmentModeVal, "Air", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(activeMode, "Air", StringComparison.OrdinalIgnoreCase))
                {
                    isAirMode = true;
                }
                else if (shipmentModeVal == "0001" || shipmentModeVal == "0002" || shipmentModeVal == "0003")
                {
                    isAirMode = true;
                }
                else if (!string.IsNullOrWhiteSpace(shipmentModeVal))
                {
                    isAirMode = false; // Expense types like 0004, 0005, etc. are Sea
                }
                else
                {
                    isAirMode = true; // Default fallback to Air
                }

                activeMode = isAirMode ? "Air" : "Sea";

                // Translate ShipmentMode if "Air" or "Sea" is selected
                string actualShipmentTypeId = "";
                if (!string.Equals(shipmentModeVal, "Air", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(shipmentModeVal, "Sea", StringComparison.OrdinalIgnoreCase))
                {
                    actualShipmentTypeId = shipmentModeVal;
                }

                var baseFilter = new JobStatusReportFilterVM
                {
                    FromDate = filter.FromDate,
                    ToDate = filter.ToDate,
                    CustomerId = filter.CustomerId,
                    ShipmentMode = actualShipmentTypeId,
                    PortType = filter.PortType,
                    ActiveTab = activeMode,
                    ReportOption = filter.ReportOption,
                    JobStatus = ""
                };

                // Truly parallel execution: Summary (prc_JobPortal_JobStatusExceptAll) & Job dataset
                var summaryTask = _reportService.GetSummaryAsync(filter.CustomerId, actualShipmentTypeId);
                var airTask = isAirMode ? _reportService.GetAirJobsAsync(baseFilter) : null;
                var seaTask = !isAirMode ? _reportService.GetSeaJobsAsync(baseFilter) : null;

                List<SeaJobReportItemDTO> allSea = new();
                List<AirJobReportItemDTO> allAir = new();

                if (isAirMode && airTask != null)
                {
                    await Task.WhenAll(summaryTask, airTask);
                    allAir = airTask.Result;
                }
                else if (!isAirMode && seaTask != null)
                {
                    await Task.WhenAll(summaryTask, seaTask);
                    allSea = seaTask.Result;
                }
                else
                {
                    await summaryTask;
                }

                var summary = summaryTask.Result;

                int totalCount = summary.TotalJobs;
                int pendingCount = summary.PendingJobs;
                int transitCount = summary.TransitJobs;
                int deliveredCount = summary.DeliveredJobs;

                // Fallback to dataset counts if SP returned 0 (e.g., if CustomerId was empty)
                if (totalCount == 0 && (isAirMode ? allAir.Count : allSea.Count) > 0)
                {
                    totalCount = isAirMode ? allAir.Count : allSea.Count;
                    pendingCount = isAirMode ? allAir.Count(x => x.StatusClass == "pending") : allSea.Count(x => x.StatusClass == "pending");
                    transitCount = isAirMode ? allAir.Count(x => x.StatusClass == "transit") : allSea.Count(x => x.StatusClass == "transit");
                    deliveredCount = isAirMode ? allAir.Count(x => x.StatusClass == "delivered") : allSea.Count(x => x.StatusClass == "delivered");
                }

                // Filter by JobStatus if requested
                var seaJobs = allSea;
                var airJobs = allAir;
                string status = (filter.JobStatus ?? "").Trim().ToLower();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status.Contains("pend"))
                    {
                        if (isAirMode) airJobs = allAir.Where(x => x.StatusClass == "pending").ToList();
                        else seaJobs = allSea.Where(x => x.StatusClass == "pending").ToList();
                    }
                    else if (status.Contains("transit"))
                    {
                        if (isAirMode) airJobs = allAir.Where(x => x.StatusClass == "transit").ToList();
                        else seaJobs = allSea.Where(x => x.StatusClass == "transit").ToList();
                    }
                    else if (status.Contains("deliver"))
                    {
                        if (isAirMode) airJobs = allAir.Where(x => x.StatusClass == "delivered").ToList();
                        else seaJobs = allSea.Where(x => x.StatusClass == "delivered").ToList();
                    }
                }

                // Re-sequence SlNo
                if (isAirMode)
                {
                    for (int i = 0; i < airJobs.Count; i++) airJobs[i].SlNo = i + 1;
                }
                else
                {
                    for (int i = 0; i < seaJobs.Count; i++) seaJobs[i].SlNo = i + 1;
                }

                return Json(new
                {
                    success = true,
                    mode = activeMode,
                    total = totalCount,
                    pending = pendingCount,
                    transit = transitCount,
                    delivered = deliveredCount,
                    seaData = seaJobs,
                    airData = airJobs,
                    rawSeaData = allSea,
                    rawAirData = allAir,
                    airCount = isAirMode ? allAir.Count : 0,
                    seaCount = !isAirMode ? allSea.Count : 0
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mode = "Air",
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
                string activeMode = (filter.ActiveTab ?? "").Trim();
                string shipmentModeVal = (filter.ShipmentMode ?? "").Trim();

                bool isAirMode;
                if (string.Equals(shipmentModeVal, "Sea", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(activeMode, "Sea", StringComparison.OrdinalIgnoreCase))
                {
                    isAirMode = false;
                }
                else if (string.Equals(shipmentModeVal, "Air", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(activeMode, "Air", StringComparison.OrdinalIgnoreCase))
                {
                    isAirMode = true;
                }
                else if (shipmentModeVal == "0001" || shipmentModeVal == "0002" || shipmentModeVal == "0003")
                {
                    isAirMode = true;
                }
                else if (!string.IsNullOrWhiteSpace(shipmentModeVal))
                {
                    isAirMode = false;
                }
                else
                {
                    isAirMode = true;
                }

                filter.ActiveTab = isAirMode ? "Air" : "Sea";

                if (string.Equals(shipmentModeVal, "Air", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(shipmentModeVal, "Sea", StringComparison.OrdinalIgnoreCase))
                {
                    filter.ShipmentMode = "";
                }

                if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _reportService.GenerateExcelReportAsync(filter);
                    string fileName = $"JobStatusReport_{filter.ActiveTab}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var bytes = await _reportService.GeneratePdfReportAsync(filter);
                    string fileName = $"JobStatusReport_{filter.ActiveTab}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
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
