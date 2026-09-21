using System;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CustomerWiseApprovalRate;
using GCTL.Service.CustomerWiseApprovalRate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class CustomerWiseApprovalRateController : BaseController
    {
        private readonly ICustomerWiseApprovalRateService _service;

        public CustomerWiseApprovalRateController(ICustomerWiseApprovalRateService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _service.GetCustomersAsync();
            var chcHeads = await _service.GetChcHeadsAsync();
            var expenseTypes = await _service.GetExpenseTypesAsync();
            var nextCwarId = await _service.GetNextCostingIdAsync();

            ViewBag.CustomerList = new SelectList(customers, "Id", "Name");
            ViewBag.ChcHeadList = new SelectList(chcHeads, "Id", "Name");
            ViewBag.ExpenseTypeList = new SelectList(expenseTypes, "Id", "Name");
            ViewBag.NextCwarId = nextCwarId;

            var model = new CustomerWiseApprovalRateViewModel
            {
                CostingId = nextCwarId,
                PageTitle = "Customer Wise Approval Rate Entry",
                Breadcrumb = "Customer Wise Approval Rate Entry"
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerData(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return Json(new { success = false, message = "Invalid customer ID." });
                }

                var data = await _service.GetByCustomerIdAsync(customerId);
                if (data == null)
                {
                    return Json(new { success = false, message = "Customer not found." });
                }

                // Sort temp table details by Shipment Mode ID (ExpenseTypeId)
                if (data.Details != null && data.Details.Count > 0)
                {
                    data.Details = data.Details
                        .OrderBy(d => d.ExpenseTypeId)
                        .ThenBy(d => d.CostingDetailsId)
                        .ToList();
                }

                // If customer has no existing CWAR ID, generate the next one
                if (string.IsNullOrEmpty(data.CostingId))
                {
                    data.CostingId = await _service.GetNextCostingIdAsync();
                }

                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving customer data: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNextCostingId()
        {
            var nextId = await _service.GetNextCostingIdAsync();
            return Json(new { success = true, costingId = nextId });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] CustomerWiseApprovalRateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Invalid model state."
                });
            }

            var result = await _service.SaveAsync(model, LoginInfo);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMasterList()
        {
            var data = await _service.GetAllMasterListAsync();
            return Json(new { data });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string? costingId, [FromBody] List<string>? costingIds)
        {
            if (costingIds != null && costingIds.Any())
            {
                var multiResult = await _service.DeleteMultipleAsync(costingIds, LoginInfo);
                return Json(multiResult);
            }

            if (!string.IsNullOrWhiteSpace(costingId))
            {
                var result = await _service.DeleteAsync(costingId, LoginInfo);
                return Json(result);
            }

            return Json(new CommonReturnViewModel
            {
                Success = false,
                Message = "No ID provided to delete."
            });
        }
    }
}
