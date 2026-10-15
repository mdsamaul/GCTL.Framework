using GCTL.Core.ViewModels.Finance.ChartOfAccounts;
using GCTL.Service.Finance.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class GroupLedgerController : BaseController
    {
        private readonly IGroupLedgerService service;

        public GroupLedgerController(IGroupLedgerService service) : base()
        {
            this.service = service;
        }

        [HttpGet]
        [Route("GroupLedger")]
        [Route("GroupLedger/Index")]
        public async Task<IActionResult> Index()
        {
            var nextCode = await service.GetNextCodeAsync();
            ViewBag.NextCode = nextCode;
            return View();
        }

        [Route("GroupLedger/GetAll")]
        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "", string currentSortOrder = "", bool showArchived = false)
        {
            var result = await service.GetAllAsync(pageNumber, pageSize, searchTerm, currentSortColumn, currentSortOrder, showArchived);
            return Json(result);
        }

        [Route("GroupLedger/GetById")]
        [HttpGet]
        public async Task<IActionResult> GetById(string code)
        {
            var result = await service.GetByIdAsync(code);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("GroupLedger/GetNextCode")]
        [HttpGet]
        public async Task<IActionResult> GetNextCode()
        {
            var nextCode = await service.GetNextCodeAsync();
            return Json(new { success = true, code = nextCode });
        }

        [Route("GroupLedger/GetDropdown")]
        [HttpGet]
        public async Task<IActionResult> GetDropdown()
        {
            var items = await service.GetDropdownAsync();
            return Json(items);
        }

        [Route("GroupLedger/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(GroupLedgerAddVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.GroupLedgerName))
                return Json(new { success = false, message = "Group Ledger Name is required." });

            var result = await service.AddAsync(model);
            return Json(new { success = result.Success, message = result.Message, data = result.Data });
        }

        [Route("GroupLedger/Update")]
        [HttpPost]
        public async Task<IActionResult> Update(GroupLedgerAddVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.GroupLedgerCodeNo))
                return Json(new { success = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.GroupLedgerName))
                return Json(new { success = false, message = "Group Ledger Name is required." });

            var result = await service.UpdateAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("GroupLedger/SoftDelete")]
        [HttpPost]
        public async Task<IActionResult> SoftDelete(DeleteLedgerRequestVM requestVM)
        {
            var result = await service.SoftDeleteAsync(requestVM);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Route("GroupLedger/ToggleArchive")]
        [HttpPost]
        public async Task<IActionResult> ToggleArchive(string code)
        {
            var result = await service.ToggleArchiveAsync(code);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
