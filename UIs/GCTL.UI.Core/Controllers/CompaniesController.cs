using AutoMapper;
using ClosedXML.Excel;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.Companies;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.Companies;

using GCTL.UI.Core.Filters;
using GCTL.UI.Core.Helpers;

using GCTL.UI.Core.ViewModels.Companies;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;


namespace GCTL.UI.Core.Controllers
{
    public class CompaniesController : BaseController
    {
        private readonly ICompanyService companyService;
        private readonly ICommonService commonService;
        private readonly IMapper mapper;
        string strMaxNO = string.Empty;
        public CompaniesController(ICompanyService companyService,
                                   ICommonService commonService,
                                   IMapper mapper
            
            )
        {
            this.companyService = companyService;
            this.commonService = commonService;
            this.mapper = mapper;
        }

        //public IActionResult Index()
        //{
        //    CompanyPageViewModel model = new CompanyPageViewModel()
        //    {
        //        PageUrl = Url.Action(nameof(Index))
        //    };
        //    commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);
        //    model.Setup = new CompanySetupViewModel
        //    {
        //        CompanyCode = strMaxNO,
        //    };

        //    return View(model);
        //}
        public IActionResult Index()
        {
            CompanyPageViewModel model = new CompanyPageViewModel()
            {
                PageUrl = Url.Action(nameof(Index))
            };
            commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);
            model.Setup = new CompanySetupViewModel
            {
                CompanyCode = strMaxNO,
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        public IActionResult Setup(string id)
        {
            CompanySetupViewModel model = new CompanySetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);
            var result = companyService.GetCompany(id);
            if (result != null)
            {
                model = mapper.Map<CompanySetupViewModel>(result);
                model.Code = id;
            }
            else
            {
                model.CompanyCode = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Setup(CompanySetupViewModel model)
        {
            if (companyService.IsCompanyExist(model.CompanyName, model.CompanyCode))
            {
                return Json(new { isSuccess = false, message = "Already Exists" });
            }

            if (ModelState.IsValid)
            {
                if (companyService.IsCompanyExistByCode(model.CompanyCode))
                {
                    var hasPermission = companyService.UpdatePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        CoreCompany company = companyService.GetCompany(model.CompanyCode) ?? new CoreCompany();
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, company);
                        companyService.SaveCompany(company);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = model.CompanyCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access" });
                    }
                }
                else
                {
                    var hasPermission = companyService.SavePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        CoreCompany company = companyService.GetCompany(model.CompanyCode) ?? new CoreCompany();
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, company);
                        companyService.SaveCompany(company);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = model.CompanyCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access" });
                    }
                }
            }

            return Json(new { isSuccess = false, message = ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage });
        }

        public ActionResult Grid()
        {
            var resutl = companyService.GetCompanies();
            return Json(new { data = resutl });
        }

        [HttpPost]
        [ModulePermission(PermissionType.Delete)]
        public async Task<ActionResult> Delete(string id)
        {
            var hasPermission = companyService.DeletePermission(LoginInfo.AccessCode);

            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access" });
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { success = false, message = "No IDs provided" });
            }

            var results = new List<(bool success, bool refSuccess, string message)>();

            var ids = id.Split(",", StringSplitOptions.RemoveEmptyEntries)
                 .Select(x => x.Trim())
                 .ToList();

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var result = await companyService.DeleteCompany(ids, model);

            results.Add(result);

            // Aggregate outcome

            bool overallSuccess = results.All(r => r.success);

            bool refSuccess = results.All(r => r.refSuccess);

            string combinedMessage = overallSuccess

                ? "Deleted Successfully"

                : string.Join("; ", results.Where(r => !r.success).Select(r => r.message));

            return Json(new { success = overallSuccess, message = combinedMessage, refSuccess = refSuccess });

        }


        //[HttpPost]
        //public ActionResult Delete(string id)
        //{
        //    var hasPermission = companyService.DeletePermission(LoginInfo.AccessCode);
        //    if (hasPermission)
        //    {
        //        bool success = false;
        //        foreach (var item in id.Split(",", StringSplitOptions.RemoveEmptyEntries))
        //        {
        //            success = companyService.DeleteCompany(item);
        //        }

        //        return Json(new { success = success, message = "Deleted Successfully" });
        //    }
        //    else
        //    {
        //        return Json(new { isSuccess = false, message = "You have no access" });
        //    }
        //}


        [HttpPost]
        public JsonResult CheckAvailability(string name, string code)
        {
            if (companyService.IsCompanyExist(name, code))
            {
                return Json(new { isSuccess = true, message = "Already Exists" });
            }

            return Json(new { isSuccess = false });
        }


        //

        #region Xls
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                var company = companyService.GetCompanies().ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Companies");

                    // ── Title Row ──────────────────────────────────────────────
                    var titleCell = worksheet.Cell(1, 1);
                    titleCell.Value = "Company Information";
                    worksheet.Range(1, 1, 1, 5).Merge();
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 14;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                    titleCell.Style.Font.FontColor = XLColor.White;
                    worksheet.Row(1).Height = 28;

                    // ── Empty spacer row ───────────────────────────────────────
                    worksheet.Range(2, 1, 2, 5).Merge();
                    worksheet.Row(2).Height = 6;

                    // ── Header Row ─────────────────────────────────────────────
                    int dataStartRow = 3;
                    string[] headers = { "Company Id", "Company Name", "Address", "Phone", "Email" };

                    for (int col = 0; col < headers.Length; col++)
                    {
                        var cell = worksheet.Cell(dataStartRow, col + 1);
                        cell.Value = headers[col];
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
                        cell.Style.Font.FontColor = XLColor.FromHtml("#1F3864");
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    worksheet.Row(dataStartRow).Height = 20;

                    // ── Data Rows ──────────────────────────────────────────────
                    // Track max char length per column for manual width calculation
                    double[] colWidths = headers.Select(h => (double)h.Length).ToArray();

                    int row = dataStartRow + 1;
                    bool alternate = false;

                    foreach (var com in company)
                    {
                        // Column values
                        string colId = "'" + (com.CompanyCode ?? "").PadLeft(2, '0');
                        string colName = com.CompanyName ?? "";
                        string colAddr = com.Address1 ?? "";
                        string colPhone = "'" + (com.Phone1 ?? "");
                        string colEmail = com.Email ?? "";

                        string[] values = { colId, colName, colAddr, colPhone, colEmail };

                        for (int col = 0; col < values.Length; col++)
                        {
                            var cell = worksheet.Cell(row, col + 1);
                            cell.Value = values[col];

                            // Alignment
                            cell.Style.Alignment.Horizontal = col == 0 || col == 4
                                ? XLAlignmentHorizontalValues.Center
                                : XLAlignmentHorizontalValues.Left;

                            // Alternating row color
                            if (alternate)
                                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF0FA");

                            // Border
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                            // Track max width (use actual text length, not the leading quote)
                            string displayText = values[col].TrimStart('\'');
                            colWidths[col] = Math.Max(colWidths[col], displayText.Length);
                        }

                        worksheet.Row(row).Height = 18;
                        row++;
                        alternate = !alternate;
                    }

                    // ── Column widths (no System.Drawing needed) ───────────────
                    double[] minWidths = { 12, 28, 36, 16, 30 };
                    double[] maxWidths = { 20, 45, 55, 25, 45 };

                    for (int col = 0; col < 5; col++)
                    {
                        double calculated = colWidths[col] + 4;          // padding
                        double clamped = Math.Max(minWidths[col],
                                           Math.Min(maxWidths[col], calculated));
                        worksheet.Column(col + 1).Width = clamped;
                    }

                    // ── Freeze header rows ─────────────────────────────────────
                    worksheet.SheetView.FreezeRows(dataStartRow);

                    // ── Save & return ──────────────────────────────────────────
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "Company.xlsx"
                        );
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Pdf


        public async Task<IActionResult> ExportToPdf()
        {
            using (var stream = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Add Title
                Paragraph title = new Paragraph("Company Information")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetFont(font);
                document.Add(title);

                // Add some space
                document.Add(new Paragraph("\n"));

                // Create a table with 5 columns
                Table table = new Table(UnitValue.CreatePercentArray(5)).UseAllAvailableWidth();

                // Define font sizes
                float headerFontSize = 10f;
                float dataFontSize = 9f;

                // Add headers
                string[] headers = new string[]
                {
            "Company Id", "Company Name", "Address", "Phone", "Email"
                };

                foreach (var header in headers)
                {
                    table.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(header)
                            .SetFont(font)
                            .SetFontSize(headerFontSize))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetBorder(new SolidBorder(0.5f))
                    );
                }

                // Fetch and add data dynamically
                var companies = companyService.GetCompanies(); // Assuming async service call
                foreach (var com in companies)
                {
                    table.AddCell(new Cell()
                        .Add(new Paragraph(com.CompanyCode?.PadLeft(2, '0') ?? "")
                        .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(com.CompanyName ?? "")
                        .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.LEFT));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(com.Address1 ?? "")
                        .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.LEFT));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(com.Phone1 ?? "")
                        .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.CENTER));



                    table.AddCell(new Cell()
                        .Add(new Paragraph(com.Email ?? "")
                        .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                // Add the table to the document
                document.Add(table);

                // Close the document
                document.Close();

                // Return the PDF as a file
                return File(stream.ToArray(), "application/pdf", "Company.pdf");
            }
        }

        #endregion

    }
}




