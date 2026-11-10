using System;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers.Dashboard
{
    [Route("DashboardGPAnalysis")]
    [Route("Dashboard/GPAnalysis")]
    public class DashboardGPAnalysisController : BaseController
    {
        private readonly IDashboardGPAnalysisService _gpAnalysisService;
        private readonly IDashboardFilterService _filterService;

        public DashboardGPAnalysisController(
            IDashboardGPAnalysisService gpAnalysisService,
            IDashboardFilterService filterService)
        {
            _gpAnalysisService = gpAnalysisService;
            _filterService = filterService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "gp";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View("~/Views/Dashboard/Index.cshtml", filterData);
        }

        [HttpPost("GetData")]
        [HttpPost("")]
        public async Task<IActionResult> GetData([FromBody] DashboardFilterDTO filter)
        {
            try
            {
                var data = await _gpAnalysisService.GetGPAnalysisDataAsync(filter ?? new DashboardFilterDTO());
                return Json(new { isSuccess = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }
    }
}
