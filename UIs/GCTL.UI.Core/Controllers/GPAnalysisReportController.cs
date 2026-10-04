using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Service.BillService;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.Service.GPAnalysisReport;
using GCTL.Service.ShipmentMode;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class GPAnalysisReportController : Controller
    {
        private readonly IGPAnalysisReportService gPAnalysisReportService;
        private readonly IJobBillEntryReportService _reportService;
        private readonly IShipmentMode _shipmentMode;
        private readonly ISalesCustomerService _customerService;

        public GPAnalysisReportController(
            IGPAnalysisReportService gPAnalysisReportService,
            IJobBillEntryReportService reportService,
            IShipmentMode shipmentMode, 
            ISalesCustomerService customerService
        )
        {
            this.gPAnalysisReportService = gPAnalysisReportService;
            _reportService = reportService;
            _shipmentMode = shipmentMode;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var defaultFromDate = DateTime.Now.ToString("01/MM/yyyy");
            var defaultToDate = DateTime.Now.ToString("dd/MM/yyyy");
            await PopulateDropdowns(defaultFromDate, defaultToDate);

            var customHeaderHtml = @"
<tr class=""text-center align-middle small table-header-tier-1"">
    <th rowspan=""3"" class=""align-middle text-center"" style=""width:35px;"">Sl.No.</th>
    <th rowspan=""3"" class=""align-middle text-center"" style=""width:75px;"">Billing Date</th>
    <th rowspan=""3"" class=""align-middle text-center"" style=""width:130px;"">Bill No</th>
    <th rowspan=""3"" class=""align-middle text-center"" style=""width:85px;"">Total Bill<br/>(BDT)</th>
    <th colspan=""5"" class=""align-middle text-center text-white"" style=""background-color:#0d6efd;"">Billing</th>
    <th colspan=""7"" class=""align-middle text-center text-white"" style=""background-color:#495057;"">Costing</th>
    <th colspan=""6"" class=""align-middle text-center text-white"" style=""background-color:#198754;"">PNL</th>
</tr>
<tr class=""text-center align-middle small table-header-tier-2"">
    <!-- Billing Sub -->
    <th colspan=""3"" class=""align-middle text-center"">C &amp; F</th>
    <th rowspan=""2"" class=""align-middle text-center"" style=""width:70px;"">Transport</th>
    <th rowspan=""2"" class=""align-middle text-center"" style=""width:80px;"">TOT Bill</th>
    <!-- Costing Sub -->
    <th colspan=""3"" class=""align-middle text-center"">C &amp; F Cost</th>
    <th colspan=""3"" class=""align-middle text-center"">Transport Cost</th>
    <th rowspan=""2"" class=""align-middle text-center"" style=""width:75px;"">TOT Cost</th>
    <!-- PNL Sub -->
    <th colspan=""3"" class=""align-middle text-center"">GP</th>
    <th colspan=""3"" class=""align-middle text-center"">GP%</th>
</tr>
<tr class=""text-center align-middle small table-header-tier-3"">
    <!-- C & F sub -->
    <th class=""align-middle text-center"" style=""width:70px;"">Comm.</th>
    <th class=""align-middle text-center"" style=""width:75px;"">Others</th>
    <th class=""align-middle text-center"" style=""width:75px;"">TOT</th>
    <!-- C & F Cost sub -->
    <th class=""align-middle text-center"" style=""width:70px;"">Rece</th>
    <th class=""align-middle text-center"" style=""width:70px;"">NON Rece</th>
    <th class=""align-middle text-center"" style=""width:75px;"">TOT Cost</th>
    <!-- Transport Cost sub -->
    <th class=""align-middle text-center"" style=""width:60px;"">Swift</th>
    <th class=""align-middle text-center"" style=""width:65px;"">Hair</th>
    <th class=""align-middle text-center"" style=""width:65px;"">TOT</th>
    <!-- GP sub -->
    <th class=""align-middle text-center"" style=""width:75px;"">C &amp; F</th>
    <th class=""align-middle text-center"" style=""width:70px;"">TRANS</th>
    <th class=""align-middle text-center"" style=""width:75px;"">TOT</th>
    <!-- GP% sub -->
    <th class=""align-middle text-center"" style=""width:60px;"">C &amp; F</th>
    <th class=""align-middle text-center"" style=""width:60px;"">TRANS</th>
    <th class=""align-middle text-center"" style=""width:60px;"">G. GP</th>
</tr>";

            var gridModel = new GridModel
            {
                GridId = "gPAnalysisReportTable",
                AjaxUrl = Url.Action("GetGridData", "GPAnalysisReport"),
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 25,
                Hover = true,
                ShowCheckbox = false,
                ShowPagination = true,
                ShowRowNumbers = false,
                ShowSearchBox = true,
                Striped = true,
                CustomHeaderHtml = customHeaderHtml,
                Columns = new List<GridColumn>
                {
                    // 1-4
                    new() { Data = "slNo", Header = "Sl.No.", Width = "35px", Align = "center", Sortable = false },
                    new() { Data = "billDate", Header = "Billing Date", Width = "75px", Align = "center", SortField = "BillDate" },
                    new() { Data = "billNo", Header = "Bill No", Width = "130px", Align = "left", IsWrapable = true, SortField = "BillNo" },
                    new() { Data = "totalBill", Header = "Total Bill (BDT)", Width = "85px", Align = "right", RenderFn = "formatCurrency", SortField = "TotalBilling" },
                    
                    // 5-9: Billing
                    new() { Data = "billingCommission", Header = "Comm.", Width = "70px", Align = "right", RenderFn = "formatCurrency", SortField = "BillingCommission" },
                    new() { Data = "othersBill", Header = "Others", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "OthersBill" },
                    new() { Data = "totalCNFBilling", Header = "TOT", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "TotalCNFBilling" },
                    new() { Data = "billingTransportBill", Header = "Transport", Width = "70px", Align = "right", RenderFn = "formatCurrency", SortField = "BillingTransportBill" },
                    new() { Data = "totalBilling", Header = "TOT Bill", Width = "80px", Align = "right", RenderFn = "formatCurrency", SortField = "TotalBilling" },

                    // 10-16: Costing
                    new() { Data = "costingReceptableAmount", Header = "Rece", Width = "70px", Align = "right", RenderFn = "formatCurrency", Sortable = false },
                    new() { Data = "costingNonReceptableAmount", Header = "NON Rece", Width = "70px", Align = "right", RenderFn = "formatCurrency", Sortable = false },
                    new() { Data = "cnfCosting", Header = "TOT Cost", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "CNFCosting" },
                    new() { Data = "costingSwiftTransportAmount", Header = "Swift", Width = "60px", Align = "right", RenderFn = "formatCurrency", Sortable = false },
                    new() { Data = "costingHairTransportAmount", Header = "Hair", Width = "65px", Align = "right", RenderFn = "formatCurrency", Sortable = false },
                    new() { Data = "costingTransport", Header = "TOT", Width = "65px", Align = "right", RenderFn = "formatCurrency", SortField = "CostingTransport" },
                    new() { Data = "totalCosting", Header = "TOT Cost", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "TotalCosting" },

                    // 17-22: PNL
                    new() { Data = "cnfGP", Header = "C & F", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "CNFGP" },
                    new() { Data = "transportGP", Header = "TRANS", Width = "70px", Align = "right", RenderFn = "formatCurrency", SortField = "TransportGP" },
                    new() { Data = "totalGP", Header = "TOT", Width = "75px", Align = "right", RenderFn = "formatCurrency", SortField = "TotalGP" },
                    new() { Data = "cnfGPPercent", Header = "C & F", Width = "60px", Align = "right", Sortable = false },
                    new() { Data = "transportGPPercent", Header = "TRANS", Width = "60px", Align = "right", Sortable = false },
                    new() { Data = "totalGPPercent", Header = "G. GP", Width = "60px", Align = "right", Sortable = false }
                }
            };

            return View(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetGridData(GPAnalysisReportFilter filter)
        {
            try
            {
                filter ??= new GPAnalysisReportFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (int.TryParse(form["draw"], out int drawVal)) filter.Draw = drawVal;
                    if (int.TryParse(form["start"], out int startVal)) filter.Start = startVal;
                    if (int.TryParse(form["length"], out int lengthVal)) filter.Length = lengthVal;
                    if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

                    var sortColIdx = form["order[0][column]"];
                    var sortDir = form["order[0][dir]"];
                    if (!string.IsNullOrEmpty(sortColIdx))
                    {
                        var colDataName = form[$"columns[{sortColIdx}][data]"];
                        filter.SortColumn = colDataName;
                    }
                    if (!string.IsNullOrEmpty(sortDir))
                    {
                        filter.SortOrder = sortDir;
                    }
                }

                var result = await gPAnalysisReportService.GetGridDataAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<GPAnalysisReportDTO>()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<GPAnalysisReportDTO>(),
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] GPAnalysisReportFilter filter)
        {
            try
            {
                filter ??= new GPAnalysisReportFilter();
                var summary = await gPAnalysisReportService.GetSummaryAsync(filter);
                return Json(summary);
            }
            catch (Exception ex)
            {
                return Json(new GPAnalysisReportSummaryDTO());
            }
        }

        private async Task PopulateDropdowns(string? fromDate = null, string? toDate = null)
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync(fromDate, toDate);
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
            }
            catch
            {
                ViewBag.ShipmentModes = new SelectList(new List<string>());
                ViewBag.Customers = new SelectList(new List<string>());
            }
        }
    }
}
