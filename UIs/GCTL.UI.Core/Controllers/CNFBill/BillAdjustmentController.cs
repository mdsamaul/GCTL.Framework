using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ClearAndF.BillAdjustment;
using GCTL.Service.BillAdjustment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GCTL.UI.Core.Controllers.CNFBill
{
    [Authorize]
    public class BillAdjustmentController : BaseController
    {
        private readonly IBillAdjustmentService _billAdjustmentService;
        private readonly ILogger<BillAdjustmentController> _logger;

        public BillAdjustmentController(
            IBillAdjustmentService billAdjustmentService,
            ILogger<BillAdjustmentController> logger)
        {
            _billAdjustmentService = billAdjustmentService;
            _logger = logger;
        }

        // GET: /BillAdjustment or /BillAdjustment/Index
        [HttpGet]
        [Route("BillAdjustment")]
        [Route("BillAdjustment/Index")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Bill Adjustment Entry";
            return View();
        }

        // GET: /BillAdjustment/GetApprovedBills?dateFrom=...&dateTo=...&searchTerm=...
        [HttpGet]
        [Route("BillAdjustment/GetApprovedBills")]
        public async Task<IActionResult> GetApprovedBills(DateTime? dateFrom, DateTime? dateTo, string? searchTerm)
        {
            try
            {
                // In C&F bill adjust, bills are loaded across the company without blocking on code mismatches (e.g. '001' vs CompanyName text)
                var bills = await _billAdjustmentService.GetApprovedBillsAsync(dateFrom, dateTo, searchTerm, null);
                return Json(new { success = true, data = bills, message = "Approved bills loaded." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to load approved bills: {ex.Message}" });
            }
        }

        // GET: /BillAdjustment/GetBillDetails?billNo=...
        [HttpGet]
        [Route("BillAdjustment/GetBillDetails")]
        public async Task<IActionResult> GetBillDetails(string billNo)
        {
            if (string.IsNullOrWhiteSpace(billNo))
                return Json(new { success = false, message = "Bill number is required." });

            try
            {
                var data = await _billAdjustmentService.GetBillDetailsByBillNoAsync(billNo);
                if (data == null)
                    return Json(new { success = false, message = $"No information found for Bill No '{billNo}'." });

                return Json(new { success = true, data = data, message = "Bill details loaded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error loading bill details: {ex.Message}" });
            }
        }

        // GET: /BillAdjustment/GetNextAdjustmentNo?adjustmentType=Credit Note
        [HttpGet]
        [Route("BillAdjustment/GetNextAdjustmentNo")]
        public async Task<IActionResult> GetNextAdjustmentNo(string? adjustmentType)
        {
            try
            {
                var nextNo = await _billAdjustmentService.GetNextAdjustmentNoAsync(adjustmentType ?? "Credit Note");
                return Json(new { success = true, adjustmentNo = nextNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /BillAdjustment/GetChcHeads
        [HttpGet]
        [Route("BillAdjustment/GetChcHeads")]
        public async Task<IActionResult> GetChcHeads()
        {
            try
            {
                var heads = await _billAdjustmentService.GetChcHeadsAsync();
                return Json(new { success = true, data = heads });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /BillAdjustment/SaveAdjustment
        [HttpPost]
        [Route("BillAdjustment/SaveAdjustment")]
        public async Task<IActionResult> SaveAdjustment([FromBody] BillAdjustmentSaveRequestDto? model)
        {
            if (model == null)
            {
                // Fallback attempt: read raw JSON stream if ASP.NET Core body deserializer produced null
                try
                {
                    Request.EnableBuffering();
                    if (Request.Body.CanSeek)
                    {
                        Request.Body.Position = 0;
                    }
                    using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                    var rawBody = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(rawBody))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            NumberHandling = JsonNumberHandling.AllowReadingFromString
                        };
                        options.Converters.Add(new StringOrNumberConverter());
                        options.Converters.Add(new FlexibleDateTimeConverter());
                        model = JsonSerializer.Deserialize<BillAdjustmentSaveRequestDto>(rawBody, options);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize raw request body fallback in SaveAdjustment.");
                }
            }

            if (model == null)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                string errorDetails = errors.Count > 0 
                    ? string.Join("; ", errors) 
                    : "No adjustment data was received by the server. Please check your inputs and try again.";

                return Json(new CommonReturnViewModel
                {
                    Success = false,
                    Message = errorDetails
                });
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(err => !string.IsNullOrWhiteSpace(err)) ?? "Invalid request payload.";

                return Json(new CommonReturnViewModel
                {
                    Success = false,
                    Message = firstError
                });
            }

            try
            {
                // Inject audit & security metadata with safe defaults
                model.CompanyCode = !string.IsNullOrWhiteSpace(LoginInfo?.CompanyCode) ? LoginInfo.CompanyCode : "001";
                model.BranchCode = string.Empty;
                model.EmployeeId = !string.IsNullOrWhiteSpace(LoginInfo?.EmployeeId) ? LoginInfo.EmployeeId : "001";
                model.Luser = LoginInfo?.Username ?? User.Identity?.Name ?? "Admin";
                model.Lip = LoginInfo?.IPAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "::1";
                model.Lmac = LoginInfo?.MacAddress ?? string.Empty;

                var result = await _billAdjustmentService.SaveAdjustmentAsync(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SaveAdjustment for BillNo: {BillNo}", model?.BillNo);
                return Json(new CommonReturnViewModel
                {
                    Success = false,
                    Message = $"An error occurred while saving: {ex.Message}"
                });
            }
        }
    }
}
