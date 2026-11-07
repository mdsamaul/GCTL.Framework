using GCTL.Core.ViewModels.Finance.VoucherApproval;
using GCTL.Service.Finance.VoucherApproval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers.Finance
{
    [Authorize]
    public class VoucherApprovalController : BaseController
    {
        private readonly IVoucherApprovalService _voucherApprovalService;

        public VoucherApprovalController(IVoucherApprovalService voucherApprovalService)
        {
            _voucherApprovalService = voucherApprovalService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new VoucherApprovalPageVM
            {
                Customers = await _voucherApprovalService.GetCustomerDropdownAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetData([FromBody] VoucherApprovalFilterVM filter)
        {
            filter ??= new VoucherApprovalFilterVM();
            var (items, totalCount, filteredCount) = await _voucherApprovalService.GetApprovalListPagedAsync(filter);
            
            return Json(new
            {
                draw = filter.Draw,
                recordsTotal = totalCount,
                recordsFiltered = filteredCount,
                data = items
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetSummary([FromBody] VoucherApprovalFilterVM filter)
        {
            filter ??= new VoucherApprovalFilterVM();
            var summary = await _voucherApprovalService.GetSummaryAsync(filter);
            return Json(new { success = true, summary });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] VoucherApprovalActionRequestVM request)
        {
            if (request == null || request.VoucherNos == null || !request.VoucherNos.Any())
            {
                return Json(new { success = false, message = "No vouchers selected." });
            }

            var username = User?.Identity?.Name ?? "Admin";
            var success = await _voucherApprovalService.UpdateApprovalStatusAsync(request.VoucherNos, request.Status, username);

            if (success)
            {
                return Json(new { success = true, message = $"Voucher(s) successfully marked as {request.Status}." });
            }

            return Json(new { success = false, message = "Failed to update voucher status." });
        }
    }
}
