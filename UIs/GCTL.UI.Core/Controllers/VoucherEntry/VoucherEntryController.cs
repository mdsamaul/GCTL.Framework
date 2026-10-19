using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.VoucherEntryVM;
using GCTL.Service.Finance.VoucherEntry;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GCTL.UI.Core.Controllers.VoucherEntry
{
    public class VoucherEntryController : BaseController
    {
        private readonly IVoucherEntryService _voucherEntryService;

        public VoucherEntryController(IVoucherEntryService voucherEntryService)
            : base()
        {
            _voucherEntryService = voucherEntryService;
        }

        [HttpGet]
        [Route("VoucherEntry")]
        [Route("VoucherEntry/Index")]
        public async Task<IActionResult> Index()
        {
            var dropdowns = await _voucherEntryService.GetDropdownsAsync();
            ViewBag.Dropdowns = dropdowns;
            ViewBag.CurrentUserName = await GetCurrentViewerNameAsync();
            return View();
        }

        [HttpPost]
        [Route("VoucherEntry/GetAll")]
        public async Task<IActionResult> GetAll([FromBody] VoucherEntryFilterVM filter)
        {
            filter ??= new VoucherEntryFilterVM();
            var result = await _voucherEntryService.GetAllAsync(filter);
            return Json(new { success = true, data = result });
        }

        [HttpGet]
        [Route("VoucherEntry/GetById")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid voucher code." });

            var result = await _voucherEntryService.GetByIdAsync(id);
            return Json(result);
        }

        [HttpGet]
        [Route("VoucherEntry/GetNextVoucherNo")]
        public async Task<IActionResult> GetNextVoucherNo(string type, string? fy = null)
        {
            if (string.IsNullOrWhiteSpace(type))
                type = "BRV";

            var nextNo = await _voucherEntryService.GetNextVoucherNoAsync(type, fy);
            return Json(new { success = true, voucherNo = nextNo });
        }

        [HttpPost]
        [Route("VoucherEntry/Save")]
        public async Task<IActionResult> Save([FromBody] VoucherEntryVM model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data submitted." });

            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var state in ModelState.Values)
                {
                    foreach (var err in state.Errors)
                    {
                        errors.Add(err.ErrorMessage);
                    }
                }
                return Json(new { success = false, message = string.Join("<br/>", errors) });
            }

            if (model.VoucherDate.Kind == DateTimeKind.Utc)
            {
                model.VoucherDate = model.VoucherDate.ToLocalTime().Date;
            }
            else
            {
                model.VoucherDate = model.VoucherDate.Date;
            }

            int? userId = GetCurrentUserId();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? mac = null;

            var result = await _voucherEntryService.SaveAsync(model, userId, ip, mac);
            return Json(result);
        }

        [HttpPost]
        [Route("VoucherEntry/Post")]
        public async Task<IActionResult> Post(string? id)
        {
            var voucherId = !string.IsNullOrWhiteSpace(id) ? id : Request.Query["id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(voucherId) && Request.HasFormContentType)
            {
                voucherId = Request.Form["id"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(voucherId))
                return Json(new { success = false, message = "Invalid voucher id." });

            int? userId = GetCurrentUserId();
            var result = await _voucherEntryService.PostAsync(voucherId, userId);
            return Json(result);
        }

        [HttpPost]
        [Route("VoucherEntry/Delete")]
        public async Task<IActionResult> Delete(string? id)
        {
            var voucherId = !string.IsNullOrWhiteSpace(id) ? id : Request.Query["id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(voucherId) && Request.HasFormContentType)
            {
                voucherId = Request.Form["id"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(voucherId))
                return Json(new { success = false, message = "Invalid voucher id." });

            int? userId = GetCurrentUserId();
            var result = await _voucherEntryService.DeleteAsync(voucherId, userId);
            return Json(result);
        }

        [HttpPost]
        [Route("VoucherEntry/BulkDelete")]
        public async Task<IActionResult> BulkDelete([FromBody] List<string> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = "No vouchers selected for deletion." });

            int? userId = GetCurrentUserId();
            int successCount = 0;
            var errors = new List<string>();

            foreach (var id in ids)
            {
                var res = await _voucherEntryService.DeleteAsync(id, userId);
                if (res.Success)
                    successCount++;
                else
                    errors.Add($"{id}: {res.Message}");
            }

            return Json(new
            {
                success = successCount > 0,
                message = $"{successCount} of {ids.Count} vouchers deleted.",
                errors
            });
        }

        private async Task<string> GetCurrentViewerNameAsync()
        {
            var claimFullName = User?.FindFirst("FullName")?.Value;
            if (!string.IsNullOrWhiteSpace(claimFullName) && !claimFullName.Equals("Guest User", StringComparison.OrdinalIgnoreCase))
            {
                return claimFullName.Trim();
            }

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User?.Identity?.Name;

            try
            {
                var resolved = await _voucherEntryService.GetUserFullNameAsync(userId, userName);
                if (!string.IsNullOrWhiteSpace(resolved) && !resolved.Equals("Guest User", StringComparison.OrdinalIgnoreCase))
                {
                    return resolved.Trim();
                }
            }
            catch { }

            string? viewerName = ViewData["FullName"] as string;
            if (!string.IsNullOrWhiteSpace(viewerName) && !viewerName.Equals("Guest User", StringComparison.OrdinalIgnoreCase))
            {
                return viewerName.Trim();
            }

            return !string.IsNullOrWhiteSpace(userName) ? userName.Trim() : "User";
        }

        [HttpGet]
        [Route("VoucherEntry/GetPrint")]
        public async Task<IActionResult> GetPrint(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid voucher id." });

            var result = await _voucherEntryService.GetVoucherPrintAsync(id);
            if (result.Success && result.Data is VoucherEntryVM vm)
            {
                string viewer = await GetCurrentViewerNameAsync();
                if (!string.IsNullOrWhiteSpace(viewer))
                {
                    vm.PreparedBy = viewer;
                }
            }
            return Json(result);
        }

        [HttpGet]
        [Route("VoucherEntry/Print")]
        public async Task<IActionResult> Print(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Invalid voucher ID.");

            var result = await _voucherEntryService.GetVoucherPrintAsync(id);
            if (!result.Success || result.Data == null)
                return NotFound("Voucher not found.");

            if (result.Data is VoucherEntryVM vm)
            {
                string viewer = await GetCurrentViewerNameAsync();
                if (!string.IsNullOrWhiteSpace(viewer))
                {
                    vm.PreparedBy = viewer;
                }
                return View("Print", vm);
            }

            return View("Print", result.Data);
        }

        [HttpGet]
        [Route("VoucherEntry/ExportExcel")]
        public async Task<IActionResult> ExportExcel(
            string? fromDate = null,
            string? toDate = null,
            string? voucherType = null,
            string? status = null,
            string? searchTerm = null)
        {
            var filter = new VoucherEntryFilterVM
            {
                VoucherType = voucherType,
                Status = status,
                SearchTerm = searchTerm,
                PageNumber = 1,
                PageSize = 100000
            };

            if (!string.IsNullOrWhiteSpace(fromDate))
            {
                if (DateTime.TryParseExact(fromDate, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var from))
                    filter.FromDate = from;
                else if (DateTime.TryParse(fromDate, out var f))
                    filter.FromDate = f;
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                if (DateTime.TryParseExact(toDate, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var to))
                    filter.ToDate = to;
                else if (DateTime.TryParse(toDate, out var t))
                    filter.ToDate = t;
            }

            var result = await _voucherEntryService.GetAllAsync(filter);

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("GCTL");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Voucher List");
            ws.View.ShowGridLines = true;

            string[] headers = { "Voucher No.", "Date", "Type", "Branch", "Narration", "Amount (৳)", "Status" };
            ws.Row(1).Height = 26;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Name = "Segoe UI";
                cell.Style.Font.Size = 10;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(19, 82, 60));
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.HorizontalAlignment = (i == 5) ? ExcelHorizontalAlignment.Right : ((i == 1 || i == 2 || i == 6) ? ExcelHorizontalAlignment.Center : ExcelHorizontalAlignment.Left);

                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Top.Color.SetColor(System.Drawing.Color.FromArgb(19, 82, 60));
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(19, 82, 60));
                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Color.SetColor(System.Drawing.Color.FromArgb(19, 82, 60));
                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Color.SetColor(System.Drawing.Color.FromArgb(19, 82, 60));
            }

            int row = 2;
            decimal totalAmount = 0;
            var borderColor = System.Drawing.Color.FromArgb(217, 217, 217);

            if (result?.Items != null)
            {
                foreach (var v in result.Items)
                {
                    ws.Row(row).Height = 21;

                    ws.Cells[row, 1].Value = v.VoucherNo;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    ws.Cells[row, 2].Value = v.VoucherDateFormatted;
                    ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    ws.Cells[row, 3].Value = v.VoucherType_Code;
                    ws.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[row, 3].Style.Font.Bold = true;

                    ws.Cells[row, 4].Value = v.BranchName ?? "Head Office";
                    ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    ws.Cells[row, 5].Value = v.Narration;
                    ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    ws.Cells[row, 6].Value = v.TotalAmount;
                    ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    var isApproved = string.Equals(v.IsApproved?.Trim(), "Posted", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(v.IsApproved?.Trim(), "Approved", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(v.IsApproved?.Trim(), "Approval", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(v.IsApproved?.Trim(), "Y", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(v.IsApproved?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

                    ws.Cells[row, 7].Value = isApproved ? "Approved" : "Pending";
                    ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    for (int c = 1; c <= 7; c++)
                    {
                        ws.Cells[row, c].Style.Font.Name = "Segoe UI";
                        ws.Cells[row, c].Style.Font.Size = 9.5f;
                        ws.Cells[row, c].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        ws.Cells[row, c].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        ws.Cells[row, c].Style.Border.Top.Color.SetColor(borderColor);
                        ws.Cells[row, c].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        ws.Cells[row, c].Style.Border.Bottom.Color.SetColor(borderColor);
                        ws.Cells[row, c].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        ws.Cells[row, c].Style.Border.Left.Color.SetColor(borderColor);
                        ws.Cells[row, c].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        ws.Cells[row, c].Style.Border.Right.Color.SetColor(borderColor);
                    }

                    totalAmount += v.TotalAmount;
                    row++;
                }
            }

            // Total Row
            ws.Row(row).Height = 22;
            ws.Cells[row, 1, row, 5].Merge = true;
            ws.Cells[row, 1].Value = "Total";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Name = "Segoe UI";
            ws.Cells[row, 1].Style.Font.Size = 10;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            ws.Cells[row, 6].Value = totalAmount;
            ws.Cells[row, 6].Style.Font.Bold = true;
            ws.Cells[row, 6].Style.Font.Name = "Segoe UI";
            ws.Cells[row, 6].Style.Font.Size = 10;
            ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            ws.Cells[row, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            for (int c = 1; c <= 7; c++)
            {
                ws.Cells[row, c].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                ws.Cells[row, c].Style.Border.Top.Color.SetColor(System.Drawing.Color.Black);
                ws.Cells[row, c].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                ws.Cells[row, c].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                ws.Cells[row, c].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                ws.Cells[row, c].Style.Border.Left.Color.SetColor(borderColor);
                ws.Cells[row, c].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                ws.Cells[row, c].Style.Border.Right.Color.SetColor(borderColor);
            }

            ws.Cells.AutoFitColumns();
            if (ws.Column(1).Width < 18) ws.Column(1).Width = 18;
            if (ws.Column(2).Width < 14) ws.Column(2).Width = 14;
            if (ws.Column(3).Width < 10) ws.Column(3).Width = 10;
            if (ws.Column(4).Width < 22) ws.Column(4).Width = 22;
            if (ws.Column(5).Width < 38) ws.Column(5).Width = 38;
            if (ws.Column(6).Width < 16) ws.Column(6).Width = 16;
            if (ws.Column(7).Width < 18) ws.Column(7).Width = 18;

            var bytes = package.GetAsByteArray();
            string fileName = $"Voucher_List_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        [Route("VoucherEntry/ExportPdf")]
        public async Task<IActionResult> ExportPdf(
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null,
            [FromQuery] string? voucherType = null,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool preview = false)
        {
            try
            {
                var filter = new VoucherEntryFilterVM
                {
                    VoucherType = voucherType,
                    Status = status,
                    SearchTerm = searchTerm,
                    PageNumber = 1,
                    PageSize = 100000
                };

                if (!string.IsNullOrWhiteSpace(fromDate))
                {
                    if (DateTime.TryParseExact(fromDate, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var from))
                        filter.FromDate = from;
                    else if (DateTime.TryParse(fromDate, out var f))
                        filter.FromDate = f;
                }

                if (!string.IsNullOrWhiteSpace(toDate))
                {
                    if (DateTime.TryParseExact(toDate, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var to))
                        filter.ToDate = to;
                    else if (DateTime.TryParse(toDate, out var t))
                        filter.ToDate = t;
                }

                var fileBytes = await _voucherEntryService.GeneratePdfAsync(filter);
                var fromStr = filter.FromDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var toStr = filter.ToDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd");
                var fileName = $"Voucher_List_{fromStr}_{toStr}.pdf";

                if (preview)
                {
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    return File(fileBytes, "application/pdf");
                }

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to export PDF: " + ex.Message);
            }
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return null;
        }
    }
}
