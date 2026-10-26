using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.JobWiseReconcileStatement;
using GCTL.Service.JobWiseReconcileStatement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class JobWiseReconcileStatementController : BaseController
    {
        #region Private Fields
        private readonly IJobWiseReconcileStatementService _reportService;
        #endregion

        #region Constructor
        public JobWiseReconcileStatementController(IJobWiseReconcileStatementService reportService) : base()
        {
            _reportService = reportService;
        }
        #endregion

        #region Index Action
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var options = await _reportService.GetFilterOptionsAsync();
                ViewBag.ShipmentModes = new SelectList(options.ShipmentModes, "Code", "Name");
                ViewBag.Customers = new SelectList(options.Customers, "Code", "Name");
                ViewBag.JobNos = new SelectList(options.JobNos);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error initializing JobWiseReconcileStatement Index");
                ViewBag.ShipmentModes = new SelectList(new List<CommonSelectModel>());
                ViewBag.Customers = new SelectList(new List<CommonSelectModel>());
                ViewBag.JobNos = new SelectList(new List<string>());
            }

            return View();
        }
        #endregion

        #region Cascading Job Nos
        [HttpGet]
        public async Task<IActionResult> GetJobNos(string? expenseTypeId, string? customerId, string? fromDate, string? toDate)
        {
            try
            {
                var jobs = await _reportService.GetJobNosAsync(expenseTypeId, customerId, fromDate, toDate);
                return Json(jobs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error fetching cascading JobNos");
                return Json(new List<string>());
            }
        }
        #endregion

        #region AJAX Report Data
        [HttpPost]
        public async Task<IActionResult> GetGridData(JobWiseReconcileStatementFilterVM filter)
        {
            try
            {
                filter ??= new JobWiseReconcileStatementFilterVM();

                if (Request.HasFormContentType)
                {
                    var form = Request.Form;
                    if (!string.IsNullOrEmpty(form["expenseTypeId"])) filter.ExpenseTypeId = form["expenseTypeId"];
                    if (!string.IsNullOrEmpty(form["fromDate"])) filter.FromDate = form["fromDate"];
                    if (!string.IsNullOrEmpty(form["toDate"])) filter.ToDate = form["toDate"];
                    if (!string.IsNullOrEmpty(form["customerId"])) filter.CustomerId = form["customerId"];
                    if (!string.IsNullOrEmpty(form["jobNo"])) filter.JobNo = form["jobNo"];
                    if (!string.IsNullOrEmpty(form["exportFormat"])) filter.ExportFormat = form["exportFormat"];
                }

                var reportResult = await _reportService.GetReportDataAsync(filter);

                return Json(new
                {
                    success = true,
                    headers = reportResult.Headers,
                    expenseHeadColumns = reportResult.ExpenseHeadColumns,
                    data = reportResult.Rows,
                    summary = reportResult.Summary
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error generating JobWiseReconcileStatement grid data");
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    headers = new List<string>(),
                    data = new List<object>(),
                    summary = new JobWiseReconcileSummaryVM()
                });
            }
        }
        #endregion

        #region Export Excel (.xls / .xlsx)
        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] JobWiseReconcileStatementFilterVM filter)
        {
            try
            {
                filter ??= new JobWiseReconcileStatementFilterVM();
                var (fileBytes, contentType, fileName) = await _reportService.ExportExcelAsync(filter);
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error exporting JobWiseReconcileStatement Excel");
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion
    }
}
