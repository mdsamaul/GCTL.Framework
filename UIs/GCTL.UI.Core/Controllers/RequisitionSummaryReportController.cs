using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.RequisitionSummaryReport;
using GCTL.Service.RequisitionSummaryReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class RequisitionSummaryReportController : BaseController
    {
        private readonly IRequisitionSummaryReportService _reportService;

        public RequisitionSummaryReportController(IRequisitionSummaryReportService reportService) : base()
        {
            _reportService = reportService;
        }

        #region Index View
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var defaultFromDate = DateTime.Now.ToString("01/MM/yyyy");
            var defaultToDate = DateTime.Now.ToString("dd/MM/yyyy");
            await PopulateDropdowns(defaultFromDate, defaultToDate);

            var gridModel = new GridModel
            {
                GridId = "requisitionSummaryReportTable",
                AjaxUrl = Url.Action("GetGridData", "RequisitionSummaryReport"),
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                CheckAllId = "chkSelectAllRequisitions",
                CheckboxClass = "chk-requisition-item",
                CheckColumnData = "jobNo",
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 10,
                Hover = true,
                ShowCheckbox = true,
                ShowPagination = true,
                ShowRowNumbers = false,
                ShowSearchBox = true,
                Striped = true,
                Columns = new List<GridColumn>
                {
                    new() { Data = "jobNo", Header = "Job No", Width = "12%", Align = "left", SortField = "JobNo" },
                    new() { Data = "jobDate", Header = "Job Date", Width = "9%", Align = "center", SortField = "JobDate" },
                    new() { Data = "customerName", Header = "Customer Name", Width = "20%", Align = "left", SortField = "CustomerName" },
                    new() { Data = "portType", Header = "Port Type", Width = "12%", Align = "left", SortField = "PortType" },
                    new() { Data = "shipmentMode", Header = "Shipment Mode", Width = "15%", Align = "left", SortField = "ShipmentMode" },
                    new() { Data = "receiptable", Header = "Receiptable", ShortHeader = "Receiptable", Width = "10%", Align = "right", RenderFn = "formatCurrency", SortField = "Receiptable" },
                    new() { Data = "nonReceiptable", Header = "Non Receiptable", ShortHeader = "Non-Receiptable", Width = "11%", Align = "right", RenderFn = "formatCurrency", SortField = "NonReceiptable" },
                    new() { Data = "total", Header = "Total", ShortHeader = "Total", Width = "11%", Align = "right", RenderFn = "formatCurrency", SortField = "Total" }
                }
            };

            return View(gridModel);
        }

        private async Task PopulateDropdowns(string? fromDate = null, string? toDate = null)
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync(fromDate, toDate);
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.PortTypes = new SelectList(options.PortTypes, "Code", "Name");
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.JobNos = new SelectList(options.JobNos.Select(x => new SelectListItem { Value = x, Text = x }), "Value", "Text");
            }
            catch
            {
                ViewBag.Customers = new SelectList(new List<string>());
                ViewBag.PortTypes = new SelectList(new List<string>());
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.JobNos = new SelectList(new List<string>());
            }
        }
        #endregion

        #region AJAX Endpoints
        [HttpPost]
        public async Task<IActionResult> GetGridData(RequisitionSummaryReportFilterVM filter)
        {
            try
            {
                filter ??= new RequisitionSummaryReportFilterVM();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

                    if (string.IsNullOrEmpty(filter.FromDate) && form.ContainsKey("fromDate")) filter.FromDate = form["fromDate"];
                    if (string.IsNullOrEmpty(filter.ToDate) && form.ContainsKey("toDate")) filter.ToDate = form["toDate"];
                    if (string.IsNullOrEmpty(filter.CustomerIds) && form.ContainsKey("customerIds")) filter.CustomerIds = form["customerIds"];
                    if (string.IsNullOrEmpty(filter.PortIds) && form.ContainsKey("portIds")) filter.PortIds = form["portIds"];
                    if (string.IsNullOrEmpty(filter.ShipmentModeIds) && form.ContainsKey("shipmentModeIds")) filter.ShipmentModeIds = form["shipmentModeIds"];
                    if (string.IsNullOrEmpty(filter.JobNos) && form.ContainsKey("jobNos")) filter.JobNos = form["jobNos"];

                    var sortColIdx = form["order[0][column]"];
                    var sortDir = form["order[0][dir]"];
                    if (!string.IsNullOrEmpty(form["sortColumn"]))
                    {
                        filter.SortColumn = form["sortColumn"];
                    }
                    else if (!string.IsNullOrEmpty(sortColIdx))
                    {
                        var colDataName = form[$"columns[{sortColIdx}][data]"];
                        filter.SortColumn = colDataName;
                    }

                    if (!string.IsNullOrEmpty(form["sortOrder"]))
                    {
                        filter.SortOrder = form["sortOrder"];
                    }
                    else if (!string.IsNullOrEmpty(sortDir))
                    {
                        filter.SortOrder = sortDir;
                    }
                }

                var result = await _reportService.GetGridDataAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<RequisitionSummaryReportItemDTO>()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<RequisitionSummaryReportItemDTO>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] RequisitionSummaryReportFilterVM filter)
        {
            try
            {
                filter ??= new RequisitionSummaryReportFilterVM();
                var summary = await _reportService.GetSummaryAsync(filter);

                return Json(new
                {
                    success = true,
                    totalJobs = summary.TotalJobs,
                    totalReceiptable = summary.TotalReceiptable,
                    totalNonReceiptable = summary.TotalNonReceiptable,
                    grandTotal = summary.GrandTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    totalJobs = 0,
                    totalReceiptable = 0m,
                    totalNonReceiptable = 0m,
                    grandTotal = 0m
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions(
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? customerIds = null,
            [FromQuery] string? portIds = null,
            [FromQuery] string? shipmentModes = null)
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync(fromDate, toDate, customerIds, portIds, shipmentModes);
                return Json(new
                {
                    success = true,
                    customers = options.Customers.Select(x => new { value = x.Code, text = x.Name }),
                    portTypes = options.PortTypes.Select(x => new { value = x.Code, text = x.Name }),
                    shipmentModes = options.ShipmentModes.Select(x => new { value = x.Code, text = x.Name }),
                    jobNos = options.JobNos.Select(x => new { value = x, text = x })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    customers = new List<object>(),
                    portTypes = new List<object>(),
                    shipmentModes = new List<object>(),
                    jobNos = new List<object>()
                });
            }
        }
        #endregion

        #region PDF Export
        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] RequisitionSummaryReportFilterVM filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new RequisitionSummaryReportFilterVM();
                filter.ToAudit(LoginInfo);
                var pdfBytes = await _reportService.GeneratePdfAsync(filter);
                var fileName = $"RequisitionSummaryReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                if (download)
                {
                    return File(pdfBytes, "application/pdf", fileName);
                }

                Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Report export failed: {ex.Message}");
            }
        }
        #endregion
    }
}
