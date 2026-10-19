using GCTL.Service.GPAnalysisReport;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class GPAnalysisReportController : BaseController
    {
        #region Private Fields
        private readonly IGPAnalysisReportService _gpAnalysisReportService;

        public GPAnalysisReportController(
            IGPAnalysisReportService gpAnalysisReportService) : base()
        {
            _gpAnalysisReportService = gpAnalysisReportService;
        }

        #endregion

        #region Index Action

        public async Task<IActionResult> Index()
        {
            var filterOptions = await _gpAnalysisReportService.GetFilterOptionsAsync();
            ViewBag.ShipmentModes = new SelectList(filterOptions.ShipmentModes, "Code", "Name");
            ViewBag.Customers = new SelectList(filterOptions.Customers, "Code", "Name");

            var gridModel = CreateGridModel(Url.Action("GetGridData", "GPAnalysisReport")!);
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
                    if (!string.IsNullOrEmpty(form["searchTerm"])) filter.SearchTerm = form["searchTerm"];
                    else if (!string.IsNullOrEmpty(form["search[value]"])) filter.SearchTerm = form["search[value]"];

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

                var result = await _gpAnalysisReportService.GetGridDataAsync(filter);
                var summary = await _gpAnalysisReportService.GetSummaryAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<GPAnalysisReportDTO>(),
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "GetGridData error: {Message}", ex.Message);
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<GPAnalysisReportDTO>(),
                    summary = new GPAnalysisReportSummaryDTO()
                });
            }
        }

        #endregion

        #region Summary Action

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] GPAnalysisReportFilter filter)
        {
            try
            {
                filter ??= new GPAnalysisReportFilter();
                var summary = await _gpAnalysisReportService.GetSummaryAsync(filter);
                return Json(summary);
            }
            catch (Exception)
            {
                return Json(new GPAnalysisReportSummaryDTO());
            }
        }

        #endregion

        #region Filter Options Action

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var options = await _gpAnalysisReportService.GetFilterOptionsAsync();
                return Json(new
                {
                    success = true,
                    shipmentModes = options.ShipmentModes.Select(m => new { value = m.Code, text = m.Name }),
                    customers = options.Customers.Select(c => new { value = c.Code, text = c.Name })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Export Actions

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] GPAnalysisReportFilter filter, [FromQuery] string exportFormat = "PDF")
        {
            try
            {
                var (fileBytes, contentType, fileName) = await _gpAnalysisReportService.ExportReportAsync(filter, exportFormat);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] GPAnalysisReportFilter filter)
        {
            try
            {
                var (fileBytes, contentType, fileName) = await _gpAnalysisReportService.ExportReportAsync(filter, "PDF");
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] GPAnalysisReportFilter filter)
        {
            try
            {
                var (fileBytes, contentType, fileName) = await _gpAnalysisReportService.ExportReportAsync(filter, "Excel");
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv([FromQuery] GPAnalysisReportFilter filter)
        {
            try
            {
                var (fileBytes, contentType, fileName) = await _gpAnalysisReportService.ExportReportAsync(filter, "CSV");
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Private Helper: Grid Model Configuration

        private GridModel CreateGridModel(string ajaxUrl)
        {
            var customHeaderHtml = @"
<tr class=""text-center align-middle small table-header-tier-1"">
    <th rowspan=""3"" class=""align-middle text-center col-gp-slno"" style=""width:35px;"">Sl.No.</th>
    <th rowspan=""3"" class=""align-middle text-center col-gp-date"" style=""width:75px;""><span class=""th-full"">Billing Date</span><span class=""th-compact"">Date</span></th>
    <th rowspan=""3"" class=""align-middle text-center col-gp-billno"" style=""width:130px;"">Bill No</th>
    <th rowspan=""3"" class=""align-middle text-center col-gp-totalbill"" style=""width:85px;""><span class=""th-full"">Total Bill<br/>(BDT)</span><span class=""th-compact"">Total Bill</span></th>
    <th colspan=""5"" class=""align-middle text-center text-white"" style=""background-color:#0d6efd;"">Billing</th>
    <th colspan=""7"" class=""align-middle text-center text-white"" style=""background-color:#495057;"">Costing</th>
    <th colspan=""6"" class=""align-middle text-center text-white"" style=""background-color:#198754;"">PNL</th>
</tr>
<tr class=""text-center align-middle small table-header-tier-2"">
    <th colspan=""3"" class=""align-middle text-center"">C &amp; F</th>
    <th rowspan=""2"" class=""align-middle text-center col-gp-trans"" style=""width:70px;""><span class=""th-full"">Transport</span><span class=""th-compact"">Trans.</span></th>
    <th rowspan=""2"" class=""align-middle text-center col-gp-totbill"" style=""width:80px;"">TOT Bill</th>
    <th colspan=""3"" class=""align-middle text-center"">C &amp; F Cost</th>
    <th colspan=""3"" class=""align-middle text-center"">Transport Cost</th>
    <th rowspan=""2"" class=""align-middle text-center col-gp-totcost"" style=""width:75px;"">TOT Cost</th>
    <th colspan=""3"" class=""align-middle text-center"">GP</th>
    <th colspan=""3"" class=""align-middle text-center"">GP%</th>
</tr>
<tr class=""text-center align-middle small table-header-tier-3"">
    <th class=""align-middle text-center col-gp-comm"" style=""width:70px;"">Comm.</th>
    <th class=""align-middle text-center col-gp-others"" style=""width:75px;"">Others</th>
    <th class=""align-middle text-center col-gp-totcnf"" style=""width:75px;"">TOT</th>
    <th class=""align-middle text-center col-gp-rece"" style=""width:70px;"">Rece</th>
    <th class=""align-middle text-center col-gp-nonrece"" style=""width:70px;""><span class=""th-full"">NON Rece</span><span class=""th-compact"">Non Rec</span></th>
    <th class=""align-middle text-center col-gp-totcostcnf"" style=""width:75px;"">TOT Cost</th>
    <th class=""align-middle text-center col-gp-swift"" style=""width:60px;"">Swift</th>
    <th class=""align-middle text-center col-gp-hair"" style=""width:65px;"">Hair</th>
    <th class=""align-middle text-center col-gp-totcosttrans"" style=""width:65px;"">TOT</th>
    <th class=""align-middle text-center col-gp-gpcnf"" style=""width:75px;"">C &amp; F</th>
    <th class=""align-middle text-center col-gp-gptrans"" style=""width:70px;"">TRANS</th>
    <th class=""align-middle text-center col-gp-gptot"" style=""width:75px;"">TOT</th>
    <th class=""align-middle text-center col-gp-gpcnfper"" style=""width:60px;"">C &amp; F</th>
    <th class=""align-middle text-center col-gp-gptransper"" style=""width:60px;"">TRANS</th>
    <th class=""align-middle text-center col-gp-gpgptotper"" style=""width:60px;"">G. GP</th>
</tr>";

            return new GridModel
            {
                GridId = "gPAnalysisReportTable",
                AjaxUrl = ajaxUrl,
                AjaxMethod = "POST",
                ServerSide = true,
                Bordered = true,
                Compact = true,
                CustomGridWrapper = false,
                DefaultPageLength = 10,
                PageLengths = new[] { 10, 25, 50, 100, -1 },
                Hover = true,
                ShowCheckbox = false,
                ShowPagination = true,
                ShowRowNumbers = false,
                ShowSearchBox = true,
                Striped = true,
                DefaultSortColumn = "billDate",
                DefaultSortDirection = "desc",
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
                    new() { Data = "cnfGPPercent", Header = "C & F", Width = "60px", Align = "right", RenderFn = "formatPercent", Sortable = false },
                    new() { Data = "transportGPPercent", Header = "TRANS", Width = "60px", Align = "right", RenderFn = "formatPercent", Sortable = false },
                    new() { Data = "totalGPPercent", Header = "G. GP", Width = "60px", Align = "right", RenderFn = "formatPercent", Sortable = false }
                }
            };
        }

        #endregion

    }
}
