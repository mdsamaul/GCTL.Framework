using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class ControlLedgerController : BaseController
    {
        private readonly IControlLedgerService service;
        private readonly IGroupLedgerService groupService;

        public ControlLedgerController(
            IControlLedgerService service,
            IGroupLedgerService groupService) : base()
        {
            this.service = service;
            this.groupService = groupService;
        }

        [HttpGet]
        [Route("ControlLedger")]
        [Route("ControlLedger/Index")]
        public async Task<IActionResult> Index()
        {
            var groups = await groupService.GetDropdownAsync();
            ViewBag.Groups = groups;
            return View();
        }

        [Route("ControlLedger/GetAll")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? groupCode = null, bool showArchived = false)
        {
            var result = await service.GetAllAsync(pageNumber, pageSize, searchTerm, currentSortColumn, currentSortOrder, groupCode, showArchived);
            return Json(result);
        }

        [Route("ControlLedger/GetById")]
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await service.GetByIdAsync(code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("ControlLedger/GetNextCode")]
        [HttpGet]
        public async Task<IActionResult> GetNextCode(string groupCode)
        {
            var nextCode = await service.GetNextCodeAsync(groupCode);
            return Json(new { success = true, code = nextCode });
        }

        [Route("ControlLedger/GetDropdown")]
        [HttpGet]
        public async Task<IActionResult> GetDropdown(string? groupCode = null)
        {
            var items = await service.GetDropdownAsync(groupCode);
            return Json(items);
        }

        [Route("ControlLedger/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(ControlLedgerAddVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.GroupLedgerCodeNo))
                return Json(new { success = false, message = "Group Ledger is required." });

            if (string.IsNullOrWhiteSpace(model.ControlLedgerName))
                return Json(new { success = false, message = "Control Ledger Name is required." });

            var result = await service.AddAsync(model);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("ControlLedger/Update")]
        [HttpPost]
        public async Task<IActionResult> Update(ControlLedgerAddVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.ControlLedgerCodeNo))
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.ControlLedgerName))
                return Json(new { success = false, message = "Control Ledger Name is required." });

            var result = await service.UpdateAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("ControlLedger/SoftDelete")]
        [HttpPost]
        public async Task<IActionResult> SoftDelete(DeleteLedgerRequestVM requestVM)
        {
            var result = await service.SoftDeleteAsync(requestVM);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("ControlLedger/ToggleArchive")]
        [HttpPost]
        public async Task<IActionResult> ToggleArchive(string code)
        {
            var result = await service.ToggleArchiveAsync(code);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
