using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class SubSubsidiaryLedgerController : BaseController
    {
        private readonly ISubSubsidiaryLedgerService service;
        private readonly IGroupLedgerService groupService;
        private readonly IControlLedgerService controlService;
        private readonly ISubControlLedgerService subControlService;

        public SubSubsidiaryLedgerController(
            ISubSubsidiaryLedgerService service,
            IGroupLedgerService groupService,
            IControlLedgerService controlService,
            ISubControlLedgerService subControlService) : base()
        {
            this.service = service;
            this.groupService = groupService;
            this.controlService = controlService;
            this.subControlService = subControlService;
        }

        [HttpGet]
        [Route("SubSubsidiaryLedger")]
        [Route("SubSubsidiaryLedger/Index")]
        public async Task<IActionResult> Index()
        {
            var groups = await groupService.GetDropdownAsync();
            ViewBag.Groups = groups;
            return View();
        }

        [Route("SubSubsidiaryLedger/GetAll")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            var result = await service.GetAllAsync(pageNumber, pageSize, searchTerm, currentSortColumn, currentSortOrder, subControlCode, controlCode, groupCode, showArchived);
            return Json(result);
        }

        [Route("SubSubsidiaryLedger/GetById")]
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await service.GetByIdAsync(code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("SubSubsidiaryLedger/GetNextCode")]
        [HttpGet]
        public async Task<IActionResult> GetNextCode(string subControlCode)
        {
            var nextCode = await service.GetNextCodeAsync(subControlCode);
            return Json(new { success = true, code = nextCode });
        }

        [Route("SubSubsidiaryLedger/GetDropdown")]
        [HttpGet]
        public async Task<IActionResult> GetDropdown(string? subControlCode = null)
        {
            var items = await service.GetDropdownAsync(subControlCode);
            return Json(items);
        }

        [Route("SubSubsidiaryLedger/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(SubSubsidiaryLedgerAddVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.SubControlLedgerCodeNo))
                return Json(new { success = false, message = "Sub-Control Ledger is required." });

            if (string.IsNullOrWhiteSpace(model.SubSubsidiaryLedgerName))
                return Json(new { success = false, message = "Sub-Subsidiary Ledger Name is required." });

            var result = await service.AddAsync(model);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("SubSubsidiaryLedger/Update")]
        [HttpPost]
        public async Task<IActionResult> Update(SubSubsidiaryLedgerAddVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.SubSubsidiaryLedgerCodeNo))
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.SubSubsidiaryLedgerName))
                return Json(new { success = false, message = "Sub-Subsidiary Ledger Name is required." });

            var result = await service.UpdateAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("SubSubsidiaryLedger/SoftDelete")]
        [HttpPost]
        public async Task<IActionResult> SoftDelete(DeleteLedgerRequestVM requestVM)
        {
            var result = await service.SoftDeleteAsync(requestVM);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("SubSubsidiaryLedger/ToggleArchive")]
        [HttpPost]
        public async Task<IActionResult> ToggleArchive(string code)
        {
            var result = await service.ToggleArchiveAsync(code);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
