using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardJobStatus")]
    [Route("Dashboard/JobStatus")]
    public class DashboardJobStatusController : BaseController
    {
        private readonly IDashboardJobStatusService _jobStatusService;
        private readonly IDashboardFilterService _filterService;

        public DashboardJobStatusController(
            IDashboardJobStatusService jobStatusService,
            IDashboardFilterService filterService)
        {
            _jobStatusService = jobStatusService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "jobs";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _jobStatusService.GetJobStatusDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
    }
}
