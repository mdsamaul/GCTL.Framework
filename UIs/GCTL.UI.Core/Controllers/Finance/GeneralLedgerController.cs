using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class GeneralLedgerController : BaseController
    {
        private readonly IGeneralLedgerService service;
        private readonly IGroupLedgerService groupService;
        private readonly IControlLedgerService controlService;
        private readonly ISubControlLedgerService subControlService;
        private readonly ISubSubsidiaryLedgerService subSubsidiaryService;

        public GeneralLedgerController(
            IGeneralLedgerService service,
            IGroupLedgerService groupService,
            IControlLedgerService controlService,
            ISubControlLedgerService subControlService,
            ISubSubsidiaryLedgerService subSubsidiaryService) : base()
        {
            this.service = service;
            this.groupService = groupService;
            this.controlService = controlService;
            this.subControlService = subControlService;
            this.subSubsidiaryService = subSubsidiaryService;
        }

        [HttpGet]
        [Route("GeneralLedger")]
        [Route("GeneralLedger/Index")]
        public async Task<IActionResult> Index()
        {
            var groups = await groupService.GetDropdownAsync();
            ViewBag.Groups = groups;
            return View();
        }

        [Route("GeneralLedger/GetAll")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", string? subSubsidiaryCode = null, string? subControlCode = null, string? controlCode = null, string? groupCode = null, bool showArchived = false)
        {
            var result = await service.GetAllAsync(pageNumber, pageSize, searchTerm, currentSortColumn, currentSortOrder, subSubsidiaryCode, subControlCode, controlCode, groupCode, showArchived);
            return Json(result);
        }

        [Route("GeneralLedger/GetById")]
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await service.GetByIdAsync(code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("GeneralLedger/GetNextCode")]
        [HttpGet]
        public async Task<IActionResult> GetNextCode(string subSubsidiaryCode)
        {
            var nextCode = await service.GetNextCodeAsync(subSubsidiaryCode);
            return Json(new { success = true, code = nextCode });
        }

        [Route("GeneralLedger/GetDropdown")]
        [HttpGet]
        public async Task<IActionResult> GetDropdown(string? subSubsidiaryCode = null)
        {
            var items = await service.GetDropdownAsync(subSubsidiaryCode);
            return Json(items);
        }

        [Route("GeneralLedger/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(GeneralLedgerAddVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.SubsidiaryLedgerCodeNo))
                return Json(new { success = false, message = "Sub-Subsidiary Ledger is required." });

            if (string.IsNullOrWhiteSpace(model.GeneralLedgerName))
                return Json(new { success = false, message = "General Ledger Name is required." });

            var result = await service.AddAsync(model);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("GeneralLedger/Update")]
        [HttpPost]
        public async Task<IActionResult> Update(GeneralLedgerAddVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.GeneralLedgerCodeNo))
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.GeneralLedgerName))
                return Json(new { success = false, message = "General Ledger Name is required." });

            var result = await service.UpdateAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("GeneralLedger/SoftDelete")]
        [HttpPost]
        public async Task<IActionResult> SoftDelete(DeleteLedgerRequestVM requestVM)
        {
            var result = await service.SoftDeleteAsync(requestVM);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("GeneralLedger/ToggleArchive")]
        [HttpPost]
        public async Task<IActionResult> ToggleArchive(string code)
        {
            var result = await service.ToggleArchiveAsync(code);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
