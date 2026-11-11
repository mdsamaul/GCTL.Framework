using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardJobReconcile")]
    [Route("Dashboard/JobReconcile")]
    public class DashboardJobReconcileController : BaseController
    {
        private readonly IDashboardJobReconcileService _jobReconcileService;
        private readonly IDashboardFilterService _filterService;

        public DashboardJobReconcileController(
            IDashboardJobReconcileService jobReconcileService,
            IDashboardFilterService filterService)
        {
            _jobReconcileService = jobReconcileService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "recon";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _jobReconcileService.GetJobReconcileDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
    }
}
