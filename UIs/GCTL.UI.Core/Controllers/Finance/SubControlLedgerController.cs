using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class SubControlLedgerController : BaseController
    {
        private readonly ISubControlLedgerService service;
        private readonly IGroupLedgerService groupService;
        private readonly IControlLedgerService controlService;

        public SubControlLedgerController(
            ISubControlLedgerService service,
            IGroupLedgerService groupService,
            IControlLedgerService controlService) : base()
        {
            this.service = service;
            this.groupService = groupService;
            this.controlService = controlService;
        }

        [HttpGet]
        [Route("SubControlLedger")]
        [Route("SubControlLedger/Index")]
        public async Task<IActionResult> Index()
        {
            var groups = await groupService.GetDropdownAsync();
            ViewBag.Groups = groups;
            return View();
        }

        [Route("SubControlLedger/GetAll")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            var result = await service.GetAllAsync(pageNumber, pageSize, searchTerm, currentSortColumn, currentSortOrder, controlCode, groupCode, showArchived);
            return Json(result);
        }

        [Route("SubControlLedger/GetById")]
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await service.GetByIdAsync(code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("SubControlLedger/GetNextCode")]
        [HttpGet]
        public async Task<IActionResult> GetNextCode(string controlCode)
        {
            var nextCode = await service.GetNextCodeAsync(controlCode);
            return Json(new { success = true, code = nextCode });
        }

        [Route("SubControlLedger/GetDropdown")]
        [HttpGet]
        public async Task<IActionResult> GetDropdown(string? controlCode = null)
        {
            var items = await service.GetDropdownAsync(controlCode);
            return Json(items);
        }

        [Route("SubControlLedger/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(SubControlLedgerAddVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.ControlLedgerCodeNo))
                return Json(new { success = false, message = "Control Ledger is required." });

            if (string.IsNullOrWhiteSpace(model.SubControlLedgerName))
                return Json(new { success = false, message = "Sub-Control Ledger Name is required." });

            var result = await service.AddAsync(model);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("SubControlLedger/Update")]
        [HttpPost]
        public async Task<IActionResult> Update(SubControlLedgerAddVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.SubControlLedgerCodeNo))
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.SubControlLedgerName))
                return Json(new { success = false, message = "Sub-Control Ledger Name is required." });

            var result = await service.UpdateAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("SubControlLedger/SoftDelete")]
        [HttpPost]
        public async Task<IActionResult> SoftDelete(DeleteLedgerRequestVM requestVM)
        {
            var result = await service.SoftDeleteAsync(requestVM);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("SubControlLedger/ToggleArchive")]
        [HttpPost]
        public async Task<IActionResult> ToggleArchive(string code)
        {
            var result = await service.ToggleArchiveAsync(code);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
