using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Service.TrialBalance;
using GCTL.UI.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class TrialBalanceController : BaseController
    {
        #region Private Fields
        private readonly ITrialBalanceService _trialBalanceService;

        public TrialBalanceController(ITrialBalanceService trialBalanceService) : base()
        {
            _trialBalanceService = trialBalanceService;
        }
        #endregion

        #region Actions

        public async Task<IActionResult> Index()
        {
            var filterOptions = await _trialBalanceService.GetFilterOptionsAsync();
            if (filterOptions.Companies != null && filterOptions.Companies.Any())
            {
                ViewBag.Companies = new SelectList(filterOptions.Companies, "Value", "Text");
            }
            if (filterOptions.Branches != null && filterOptions.Branches.Any())
            {
                ViewBag.Branches = new SelectList(filterOptions.Branches, "Value", "Text");
            }

            var gridModel = CreateGridModel(Url.Action("GetGridData", "TrialBalance")!);
            return View(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetGridData(TrialBalanceFilter filter)
        {
            try
            {
                filter ??= new TrialBalanceFilter();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (!string.IsNullOrEmpty(form["companyCode"])) filter.CompanyCode = form["companyCode"];
                    if (!string.IsNullOrEmpty(form["branchCode"])) filter.BranchCode = form["branchCode"];
                    if (!string.IsNullOrEmpty(form["fromDate"])) filter.FromDate = form["fromDate"];
                    if (!string.IsNullOrEmpty(form["toDate"])) filter.ToDate = form["toDate"];

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

                var (result, summary) = await _trialBalanceService.GetGridDataWithSummaryAsync(filter);

                return Json(new
                {
                    draw = filter.Draw ?? 1,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount,
                    data = result.Data ?? new List<TrialBalanceItemDTO>(),
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = filter?.Draw ?? 1,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<TrialBalanceItemDTO>(),
                    summary = new TrialBalanceSummaryDTO()
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] TrialBalanceFilter filter)
        {
            try
            {
                filter ??= new TrialBalanceFilter();
                var summary = await _trialBalanceService.GetSummaryAsync(filter);
                return Json(summary);
            }
            catch (Exception)
            {
                return Json(new TrialBalanceSummaryDTO());
            }
        }

        #endregion

        #region Export Actions

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] TrialBalanceFilter filter, [FromQuery] string exportFormat = "Excel")
        {
            try
            {
                filter ??= new TrialBalanceFilter();
                var (fileBytes, contentType, fileName) = await _trialBalanceService.ExportReportAsync(filter, exportFormat);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] TrialBalanceFilter filter)
        {
            return await Export(filter, "Excel");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] TrialBalanceFilter filter)
        {
            return await Export(filter, "PDF");
        }

        #endregion

        #region Helper: Grid Model Configuration

        private GridModel CreateGridModel(string ajaxUrl)
        {
            return new GridModel
            {
                GridId = "trialBalanceTable",
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
                DefaultSortColumn = "code",
                DefaultSortDirection = "asc",
                Columns = new List<GridColumn>
                {
                    new() { Data = "code", Header = "CODE", Width = "150px", Align = "left", SortField = "Code" },
                    new() { Data = "accountHead", Header = "ACCOUNT HEAD", Width = "300px", Align = "left", SortField = "AccountHead" },
                    new() { Data = "group", Header = "GROUP", Width = "160px", Align = "left", SortField = "Group" },
                    new() { Data = "debit", Header = "DEBIT", Width = "140px", Align = "right", RenderFn = "formatCurrency", SortField = "Debit" },
                    new() { Data = "credit", Header = "CREDIT", Width = "140px", Align = "right", RenderFn = "formatCurrency", SortField = "Credit" }
                }
            };
        }

        #endregion
    }
}
