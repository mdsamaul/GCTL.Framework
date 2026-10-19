using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GCTL.Core.ViewModels.Finance.OpeningBalancesVM;
using GCTL.Service.Finance.OpeningBalance;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GCTL.UI.Core.Controllers.Finance
{
    public class OpeningBalanceController : BaseController
    {
        private readonly IOpeningBalanceService _openingBalanceService;

        public OpeningBalanceController(IOpeningBalanceService openingBalanceService)
            : base()
        {
            _openingBalanceService = openingBalanceService;
        }

        [HttpGet]
        [Route("OpeningBalance")]
        [Route("OpeningBalance/Index")]
        public async Task<IActionResult> Index()
        {
            var dropdowns = await _openingBalanceService.GetDropdownsAsync();
            ViewBag.Dropdowns = dropdowns;
            return View();
        }

        [HttpGet]
        [Route("OpeningBalance/ComIndex")]
        public async Task<IActionResult> ComIndex()
        {
            var dropdowns = await _openingBalanceService.GetDropdownsAsync();
            ViewBag.Dropdowns = dropdowns;
            return View("Index");
        }

        [HttpPost]
        [Route("OpeningBalance/GetGridData")]
        public async Task<IActionResult> GetGridData(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            int start = 0,
            int length = 10,
            string searchTerm = "",
            string sortColumn = "ComOpeningBalanceCode",
            string sortOrder = "desc",
            string? financialCodeNo = null,
            string? generalLedgerCodeNo = null,
            string? fromDate = null,
            string? toDate = null)
        {
            if (length > 0 && pageNumber == 1 && start > 0)
            {
                pageNumber = (start / length) + 1;
                pageSize = length;
            }
            else if (length > 0)
            {
                pageSize = length;
            }

            var filter = new OpeningBalanceFilterVM
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortColumn = sortColumn,
                SortOrder = sortOrder,
                FinancialCodeNo = string.IsNullOrWhiteSpace(financialCodeNo) ? null : financialCodeNo.Trim(),
                GeneralLedgerCodeNo = string.IsNullOrWhiteSpace(generalLedgerCodeNo) ? null : generalLedgerCodeNo.Trim(),
                FromDateStr = fromDate,
                ToDateStr = toDate
            };

            var result = await _openingBalanceService.GetAllAsync(filter);

            return Json(new
            {
                draw = draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.TotalCount,
                data = result.Items,
                summary = result.Summary
            });
        }

        [HttpPost]
        [Route("OpeningBalance/GetAll")]
        public async Task<IActionResult> GetAll([FromBody] OpeningBalanceFilterVM filter)
        {
            filter ??= new OpeningBalanceFilterVM();
            var result = await _openingBalanceService.GetAllAsync(filter);
            return Json(new { success = true, data = result });
        }

        [HttpGet]
        [Route("OpeningBalance/GetById")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid code." });

            var result = await _openingBalanceService.GetByIdAsync(id);
            return Json(result);
        }

        [HttpPost]
        [Route("OpeningBalance/Save")]
        public async Task<IActionResult> Save([FromBody] OpeningBalanceVM model)
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

            int? userId = GetCurrentUserId();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? mac = null;

            var result = await _openingBalanceService.SaveAsync(model, userId, ip, mac);
            return Json(result);
        }

        [HttpPost]
        [Route("OpeningBalance/Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid code." });

            int? userId = GetCurrentUserId();
            var result = await _openingBalanceService.DeleteAsync(id, userId);
            return Json(result);
        }

        [HttpPost]
        [Route("OpeningBalance/BulkDelete")]
        public async Task<IActionResult> BulkDelete([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return Json(new { success = false, message = "Please select at least one item to delete." });

            int? userId = GetCurrentUserId();
            var result = await _openingBalanceService.BulkDeleteAsync(ids, userId);
            return Json(result);
        }

        [HttpGet]
        [Route("OpeningBalance/GetNextCode")]
        public async Task<IActionResult> GetNextCode(string financialYear)
        {
            string code = await _openingBalanceService.GetNextCodeAsync(financialYear);
            return Json(new { success = true, code = code });
        }

        [HttpGet]
        [Route("OpeningBalance/DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("GCTL");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("OpeningBalanceTemplate");

            string[] headers = new[]
            {
                "OB Code", "Financial Year", "Account Code", "Opening Balance", "Tr. Type", "OB Date", "Reference", "Description"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 244, 248));
            }

            ws.Column(1).Style.Numberformat.Format = "@";
            ws.Column(2).Style.Numberformat.Format = "@";
            ws.Column(3).Style.Numberformat.Format = "@";
            ws.Column(4).Style.Numberformat.Format = "#,##0.00";
            ws.Column(5).Style.Numberformat.Format = "@";
            ws.Column(6).Style.Numberformat.Format = "yyyy-MM-dd";
            ws.Column(7).Style.Numberformat.Format = "@";
            ws.Column(8).Style.Numberformat.Format = "@";

            ws.Cells[2, 1].Value = "";
            ws.Cells[2, 2].Value = "2026-2027";
            ws.Cells[2, 3].Value = "1010010000100001";
            ws.Cells[2, 4].Value = 50000.00;
            ws.Cells[2, 5].Value = "Dr";
            ws.Cells[2, 6].Value = "2026-07-01";
            ws.Cells[2, 7].Value = "REF-001";
            ws.Cells[2, 8].Value = "Cash brought forward";

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "opening_balance_template.xlsx");
        }

        [HttpPost]
        [Route("OpeningBalance/UploadExcel")]
        [Route("OpeningBalance/UploadCsv")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please select a valid Excel or CSV file to upload." });

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".csv")
                return Json(new { success = false, message = "Invalid file format. Please upload an Excel (.xlsx/.xls) or CSV file." });

            var items = new List<OpeningBalanceVM>();

            try
            {
                using var stream = file.OpenReadStream();

                if (ext == ".csv")
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    string? headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(headerLine))
                        return Json(new { success = false, message = "CSV file is empty." });

                    var headers = ParseCsvLine(headerLine.Trim().ToLowerInvariant());
                    int ixCode = headers.IndexOf("account code");
                    int ixAmt = headers.IndexOf("opening balance");
                    int ixType = headers.IndexOf("tr. type");
                    int ixFy = headers.IndexOf("financial year");
                    int ixDate = headers.IndexOf("ob date");
                    int ixRef = headers.IndexOf("reference");
                    int ixDesc = headers.IndexOf("description");

                    if (ixCode == -1 || ixAmt == -1 || ixType == -1)
                        return Json(new { success = false, message = "Required columns ('Account Code', 'Opening Balance', 'Tr. Type') are missing in header." });

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var cols = ParseCsvLine(line);
                        if (cols.Count <= ixCode || cols.Count <= ixAmt || cols.Count <= ixType) continue;

                        var vm = ParseRowToVM(
                            cols[ixCode],
                            cols[ixAmt],
                            cols[ixType],
                            ixFy >= 0 && cols.Count > ixFy ? cols[ixFy] : null,
                            ixDate >= 0 && cols.Count > ixDate ? cols[ixDate] : null,
                            ixRef >= 0 && cols.Count > ixRef ? cols[ixRef] : null,
                            ixDesc >= 0 && cols.Count > ixDesc ? cols[ixDesc] : null
                        );
                        if (vm != null) items.Add(vm);
                    }
                }
                else
                {
                    OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("GCTL");
                    using var package = new ExcelPackage(stream);
                    var ws = package.Workbook.Worksheets.FirstOrDefault();

                    if (ws == null || ws.Dimension == null || ws.Dimension.Rows < 2)
                        return Json(new { success = false, message = "The Excel file contains no data rows." });

                    int rowCount = ws.Dimension.Rows;
                    int colCount = ws.Dimension.Columns;

                    int ixCode = -1, ixAmt = -1, ixType = -1, ixFy = -1, ixDate = -1, ixRef = -1, ixDesc = -1;

                    for (int c = 1; c <= colCount; c++)
                    {
                        string colName = (ws.Cells[1, c].Text ?? "").Trim().ToLowerInvariant();
                        if (colName.Contains("account code") || colName == "accountcode" || colName == "code") ixCode = c;
                        else if (colName.Contains("opening balance") || colName.Contains("balance") || colName == "amount") ixAmt = c;
                        else if (colName.Contains("tr. type") || colName.Contains("tr type") || colName.Contains("transaction type") || colName == "type") ixType = c;
                        else if (colName.Contains("financial year") || colName == "year" || colName == "fy") ixFy = c;
                        else if (colName.Contains("ob date") || colName.Contains("opening date") || colName == "date") ixDate = c;
                        else if (colName.Contains("reference") || colName == "ref") ixRef = c;
                        else if (colName.Contains("description") || colName == "desc" || colName == "remarks") ixDesc = c;
                    }

                    if (ixCode == -1 || ixAmt == -1 || ixType == -1)
                        return Json(new { success = false, message = "Required columns ('Account Code', 'Opening Balance', 'Tr. Type') were not found in the Excel sheet." });

                    for (int r = 2; r <= rowCount; r++)
                    {
                        var vm = ParseRowToVM(
                            ixCode > 0 ? ws.Cells[r, ixCode].Text : null,
                            ixAmt > 0 ? ws.Cells[r, ixAmt].Text : null,
                            ixType > 0 ? ws.Cells[r, ixType].Text : null,
                            ixFy > 0 ? ws.Cells[r, ixFy].Text : null,
                            ixDate > 0 ? ws.Cells[r, ixDate].Text : null,
                            ixRef > 0 ? ws.Cells[r, ixRef].Text : null,
                            ixDesc > 0 ? ws.Cells[r, ixDesc].Text : null
                        );
                        if (vm != null) items.Add(vm);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error reading file: " + ex.Message });
            }

            if (items.Count == 0)
                return Json(new { success = false, message = "No valid data rows found in the uploaded file." });

            int? userId = GetCurrentUserId();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? mac = null;

            var importResult = await _openingBalanceService.ImportCsvAsync(items, userId, ip, mac);
            return Json(importResult);
        }

        [HttpPost]
        [Route("OpeningBalance/ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromBody] OpeningBalanceFilterVM filter)
        {
            filter ??= new OpeningBalanceFilterVM();
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;

            var result = await _openingBalanceService.GetAllAsync(filter);
            var list = result.Items ?? new List<OpeningBalanceVM>();

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("GCTL");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("OpeningBalances");

            string[] headers = new[]
            {
                "OB Code", "Financial Year", "Account Code", "Account Name", "Opening Balance", "Tr. Type", "OB Date", "Reference", "Description"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 244, 248));
            }

            ws.Column(1).Style.Numberformat.Format = "@";
            ws.Column(2).Style.Numberformat.Format = "@";
            ws.Column(3).Style.Numberformat.Format = "@";
            ws.Column(5).Style.Numberformat.Format = "#,##0.00";
            ws.Column(6).Style.Numberformat.Format = "@";
            ws.Column(7).Style.Numberformat.Format = "@";

            int r = 2;
            foreach (var item in list)
            {
                ws.Cells[r, 1].Value = item.ComOpeningBalanceCode;
                ws.Cells[r, 2].Value = item.FinancialCodeNo;
                ws.Cells[r, 3].Value = item.GeneralLedgerCodeNo;
                ws.Cells[r, 4].Value = item.GeneralLedgerName;
                ws.Cells[r, 5].Value = item.OpeningBalance;
                ws.Cells[r, 6].Value = item.TrType;
                ws.Cells[r, 7].Value = item.OpeningDateFormatted;
                ws.Cells[r, 8].Value = item.Reference;
                ws.Cells[r, 9].Value = item.Description;
                r++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            var bytes = package.GetAsByteArray();

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Opening_Balance_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        [Route("OpeningBalance/ExportPdf")]
        public async Task<IActionResult> ExportPdf([FromBody] OpeningBalanceFilterVM filter)
        {
            try
            {
                filter ??= new OpeningBalanceFilterVM();
                var pdfBytes = await _openingBalanceService.GeneratePdfAsync(filter);
                var fileName = $"Opening_Balance_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Failed to export PDF: " + ex.Message });
            }
        }

        private static OpeningBalanceVM? ParseRowToVM(
            string? rawCode,
            string? rawAmt,
            string? rawType,
            string? rawFy,
            string? rawDate,
            string? rawRef,
            string? rawDesc)
        {
            if (string.IsNullOrWhiteSpace(rawCode)) return null;

            string accCode = rawCode.Trim();
            if (accCode.StartsWith("=", StringComparison.Ordinal))
                accCode = accCode.Substring(1).Trim('"', '\'');
            accCode = accCode.Trim('"', '\'', '\t', ' ');

            if (accCode.Contains('E') || accCode.Contains('e'))
            {
                if (decimal.TryParse(accCode, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decimal sciVal))
                {
                    accCode = sciVal.ToString("0");
                }
            }

            if (string.IsNullOrWhiteSpace(rawAmt)) return null;
            string cleanAmt = rawAmt.Trim('"', '\'', '$', '৳', ' ', ',');
            if (!decimal.TryParse(cleanAmt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
                return null;

            if (string.IsNullOrWhiteSpace(rawType)) return null;
            string trType = rawType.Trim().Trim('"', '\'');
            if (trType.StartsWith("dr", StringComparison.OrdinalIgnoreCase)) trType = "Dr";
            else if (trType.StartsWith("cr", StringComparison.OrdinalIgnoreCase)) trType = "Cr";
            else return null;

            string fy = !string.IsNullOrWhiteSpace(rawFy) ? rawFy.Trim().Trim('"', '\'', '=') : "2026-2027";
            DateTime? opDate = null;
            if (!string.IsNullOrWhiteSpace(rawDate))
            {
                if (DateTime.TryParse(rawDate.Trim().Trim('"', '\''), out var d))
                    opDate = d;
            }

            return new OpeningBalanceVM
            {
                FinancialCodeNo = fy,
                GeneralLedgerCodeNo = accCode,
                OpeningBalance = amount,
                TrType = trType,
                OpeningDate = opDate,
                Reference = rawRef?.Trim().Trim('"', '\''),
                Description = rawDesc?.Trim().Trim('"', '\'')
            };
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

        private static List<string> ParseCsvLine(string line)
        {
            var list = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            list.Add(sb.ToString());
            return list;
        }
    }
}
