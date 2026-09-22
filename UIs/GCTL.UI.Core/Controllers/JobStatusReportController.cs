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
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class JobStatusReportController : BaseController
    {
        private readonly IDocumentationReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly IPortTypeService _portTypeService;
        private readonly ISalesCustomerService _customerService;
        private readonly IGenericRepository<CfDefDeliveryStatusType> deliveryStatusRepo;
        private readonly IGenericRepository<SalesDefShipmentStatusInfo> _shipmentStatusRepo;

        public JobStatusReportController(
            IDocumentationReportService reportService,
            IShipmentMode shipmentMode,
            IPortTypeService portTypeService,
            ISalesCustomerService customerService,
            IGenericRepository<CfDefDeliveryStatusType> deliveryStatusRepo,
            IGenericRepository<SalesDefShipmentStatusInfo> shipmentStatusRepo) : base()
        {
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _portTypeService = portTypeService;
            _customerService = customerService;
            this.deliveryStatusRepo = deliveryStatusRepo;
            _shipmentStatusRepo = shipmentStatusRepo;
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
                var shipmentModes = await _shipmentMode.GetAllExpenseTypesAsync();
                ViewBag.ShipmentModes = new SelectList(shipmentModes ?? new(), "ExpenseTypeID", "ExpenseType");

                var portTypes = await _portTypeService.SelectionPortTypeAsync();
                ViewBag.PortTypes = new SelectList(portTypes ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var customers = await _customerService.SelectionCustomerAsync();
                ViewBag.Customers = new SelectList(customers ?? Enumerable.Empty<GCTL.Core.ViewModels.Common.CommonSelectModel>(), "Code", "Name");

                var statuses = await deliveryStatusRepo.All()
                    .OrderBy(x => x.Tc)
                    .Select(x => new { id = x.DeliveryStatusTypeId, name = x.DeliveryStatus })
                    .ToListAsync();
                ViewBag.JobStatuses = new SelectList(statuses, "id", "name");

                ViewBag.ExportFormats = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem { Value = "PDF", Text = "PDF" },
                    new SelectListItem { Value = "Excel", Text = "Excel" }
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

        #region AJAX Data Endpoint
        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] DocumentationReportFilterVM filter)
        {
            try
            {
                filter ??= new DocumentationReportFilterVM();
                var rawData = await _reportService.GetDocumentationReportDataAsync(filter);

                int totalCount = rawData.Count;
                int deliveredCount = 0;
                int transitCount = 0;
                int pendingCount = 0;

                var list = new List<object>(rawData.Count);
                int sl = 1;

                foreach (var item in rawData)
                {
                    string statusDisplay = DetermineStatus(item, out string statusClass);

                    if (statusClass == "delivered")
                        deliveredCount++;
                    else if (statusClass == "transit")
                        transitCount++;
                    else
                        pendingCount++;

                    list.Add(new
                    {
                        sl = sl++,
                        jobNo = item.JobNo ?? "",
                        serviceType = !string.IsNullOrWhiteSpace(item.ExpenseType) ? item.ExpenseType : "General",
                        portType = !string.IsNullOrWhiteSpace(item.PortName) ? item.PortName : (!string.IsNullOrWhiteSpace(item.LoadingPort) ? item.LoadingPort : "-"),
                        dateReceived = !string.IsNullOrWhiteSpace(item.DocReceivedDate) ? item.DocReceivedDate : (!string.IsNullOrWhiteSpace(item.JobDate) ? item.JobDate : "-"),
                        vesselName = !string.IsNullOrWhiteSpace(item.VesselRotNo) ? item.VesselRotNo : (!string.IsNullOrWhiteSpace(item.ForwarderName) ? item.ForwarderName : "-"),
                        weight = !string.IsNullOrWhiteSpace(item.Weight) ? item.Weight : "-",
                        container = !string.IsNullOrWhiteSpace(item.ContainerPackages) ? item.ContainerPackages : (!string.IsNullOrWhiteSpace(item.ContainerNo) ? item.ContainerNo : "-"),
                        status = statusDisplay,
                        statusClass = statusClass
                    });
                }

                return Json(new
                {
                    success = true,
                    total = totalCount,
                    delivered = deliveredCount,
                    transit = transitCount,
                    pending = pendingCount,
                    data = list
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    total = 0,
                    delivered = 0,
                    transit = 0,
                    pending = 0,
                    data = new List<object>()
                });
            }
        }

        private static string DetermineStatus(DocumentationReportItemDTO item, out string statusClass)
        {
            var status = (item.ShipmentStatus ?? "").Trim();
            var clearance = (item.DateOfClearence ?? "").Trim();
            var delivery = (item.ActualDelivery ?? "").Trim();

            if (!string.IsNullOrEmpty(clearance) || !string.IsNullOrEmpty(delivery) ||
                status.Contains("Deliver", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Complete", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Clear", StringComparison.OrdinalIgnoreCase))
            {
                statusClass = "delivered";
                return !string.IsNullOrEmpty(status) ? status : "Delivered";
            }

            if (status.Contains("Transit", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Port", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Arrived", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Discharge", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Shed", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Stuffing", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Loading", StringComparison.OrdinalIgnoreCase))
            {
                statusClass = "transit";
                return !string.IsNullOrEmpty(status) ? status : "In Transit";
            }

            statusClass = "pending";
            return !string.IsNullOrEmpty(status) ? status : "Pending";
        }
        #endregion

        #region Export Action
        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DocumentationReportFilterVM filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                filter ??= new DocumentationReportFilterVM();

                if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _reportService.GenerateExcelReportAsync(filter);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JobStatusReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else
                {
                    var bytes = await _reportService.GeneratePdfReportAsync(filter);
                    return File(bytes, "application/pdf", $"JobStatusReport_{DateTime.Now:yyyyMMddHHmmss}.pdf");
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
