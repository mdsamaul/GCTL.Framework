using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CustomerRateReport;
using GCTL.Service.CustomerRateReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerRateReportController : BaseController
    {
        private readonly ICustomerRateReportService _reportService;

        public CustomerRateReportController(ICustomerRateReportService reportService) : base()
        {
            _reportService = reportService;
        }

        #region Index View
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await PopulateDropdowns();

            var gridModel = new GridModel
            {
                GridId = "customerRateReportTable",
                AjaxUrl = Url.Action("GetGridData", "CustomerRateReport"),
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                CheckAllId = "chkSelectAllCustomerRates",
                CheckboxClass = "chk-rate-item",
                CheckColumnData = "particulars",
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 100,
                Hover = true,
                ShowCheckbox = false,
                ShowPagination = false,
                ShowRowNumbers = false,
                ShowSearchBox = false,
                Striped = true,
                Columns = new List<GridColumn>
                {
                    new() { Data = "section", Header = " ", Width = "8%", Align = "center", Sortable = false },
                    new() { Data = "particulars", Header = "Particulars", Width = "52%", Align = "left", Sortable = false },
                    new() { Data = "formattedAmount", Header = "Amount (BDT)", Width = "20%", Align = "right", Sortable = false },
                    new() { Data = "remarks", Header = "Remarks", Width = "20%", Align = "left", Sortable = false }
                }
            };

            return View(gridModel);
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                var customers = await _reportService.GetCustomerListAsync();
                ViewBag.Customers = new SelectList(customers, "Code", "Name");
            }
            catch
            {
                ViewBag.Customers = new SelectList(new List<SelectListItem>());
            }
        }
        #endregion

        #region AJAX Endpoints
        [HttpPost]
        public async Task<IActionResult> GetGridData(CustomerRateReportFilterVM filter)
        {
            try
            {
                filter ??= new CustomerRateReportFilterVM();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    filter.SearchTerm = form["search[value]"].ToString();
                }

                var (data, totalCount, filteredCount) = await _reportService.GetGridDataAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = totalCount,
                    recordsFiltered = filteredCount,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<CustomerRateReportItemDTO>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] CustomerRateReportFilterVM filter)
        {
            try
            {
                filter ??= new CustomerRateReportFilterVM();
                string? ids = !string.IsNullOrWhiteSpace(filter.CustomerIds) ? filter.CustomerIds : filter.CustomerId;
                var summary = await _reportService.GetSummaryAsync(ids);

                return Json(new
                {
                    success = true,
                    totalCustomers = summary.TotalCustomers,
                    totalChcItems = summary.TotalChcItems,
                    totalHandlingAmount = summary.TotalHandlingAmount,
                    minAgencyCommission = summary.MinAgencyCommission,
                    onInvoiceValue = summary.OnInvoiceValue
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    totalCustomers = 0,
                    totalChcItems = 0,
                    totalHandlingAmount = 0m,
                    minAgencyCommission = 0m,
                    onInvoiceValue = 0m
                });
            }
        }
        #endregion

        #region PDF & Excel Export
        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] CustomerRateReportFilterVM filter, [FromQuery] bool download = false)
        {
            try
            {
                filter ??= new CustomerRateReportFilterVM();
                var pdfBytes = await _reportService.GeneratePdfAsync(filter);
                var fileName = $"CustomerRateReport.pdf";

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

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] CustomerRateReportFilterVM filter)
        {
            try
            {
                filter ??= new CustomerRateReportFilterVM();
                var excelBytes = await _reportService.GenerateExcelAsync(filter);
                var fileName = $"CustomerRateReport.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Report export failed: {ex.Message}");
            }
        }
        #endregion
    }
}
