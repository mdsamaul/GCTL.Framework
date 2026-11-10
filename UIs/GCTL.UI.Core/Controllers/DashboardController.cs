using System.Threading.Tasks;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Service.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.UI.Core.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IDashboardFilterService _filterService;

        public DashboardController(IDashboardFilterService filterService)
        {
            _filterService = filterService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "overview";
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return View(filterData);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            var filterData = await _filterService.GetInitialFilterDataAsync();
            return Json(new { isSuccess = true, data = filterData });
        }
    }
}
