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
        #region Ser & Repo
        private readonly ICompanyService companyService;
        private readonly ICommonService commonService;
        private readonly IMapper mapper;

        string strMaxNO = string.Empty;

        public CompaniesController(
            ICommonService commonService,
            ICompanyService companyService,
            IMapper mapper
        )
        {
            this.companyService = companyService;
            this.commonService = commonService;
            this.mapper = mapper;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await companyService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new CompanyPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);
                model.Setup = new CompanySetupViewModel
                {
                    CompanyCode = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.CompanyList = new List<CompanySetupViewModel>();
                model.Setup = new CompanySetupViewModel();
                Console.WriteLine("Error" + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            CompanySetupViewModel model = new CompanySetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "CompanyCode", "Core_Company", 3);

            if (!string.IsNullOrEmpty(id))
            {
                model = await companyService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.CompanyCode = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await companyService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new CompanyPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await companyService.GetAllAsync() ?? new List<CompanySetupViewModel>();
                indexModel.CompanyList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Company Name", "Address", "Phone", "Email" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "CompanyCode", item.CompanyCode },
                        { "Company Name", item.CompanyName },
                        { "Address", item.Address1 },
                        { "Phone", item.Phone1 },
                        { "Email", item.Email }
                    },
                    ajaxUrl: Url.Action("GetAll", "Companies") ?? "/Companies/GetAll",
                    primaryKey: "CompanyCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/Companies/Setup/{id}",
                    gridId: "companyGridContainer",
                    editClass: "js-company-edit",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "CompanyCode", "center" },
                        { "Company Name", "left" },
                        { "Address", "left" },
                        { "Phone", "center" },
                        { "Email", "left" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Company Name", "auto" },
                        { "Address", "auto" },
                        { "Phone", "120px" },
                        { "Email", "auto" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.CompanyList = new List<CompanySetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(CompanySetupViewModel modelVM)
        {
            try
            {
                bool isExist = !string.IsNullOrEmpty(modelVM.CompanyCode) && await companyService.IsExistByCodeAsync(modelVM.CompanyCode);

                if (!isExist)
                {
                    if (await companyService.IsExistAsync(modelVM.CompanyName, modelVM.CompanyCode))
                    {
                        return Json(new { isSuccess = false, message = "Already Exists!", isDuplicate = true });
                    }
                }
                else
                {
                    if (await companyService.IsExistAsync(modelVM.CompanyName, modelVM.CompanyCode))
                    {
                        return Json(new { isSuccess = false, message = "Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, isExist);
                if (!isExist)
                {
                    var hasSavePermission = await companyService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await companyService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.CompanyCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await companyService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await companyService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.CompanyCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to update.", noUpdatePermission = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ex.Message}");
                return RedirectToAction("Login", "Accounts");
            }
        }

        #endregion

        #region CheckAvailability
        [HttpPost]
        public async Task<JsonResult> CheckAvailability(string name, string code)
        {
            if (await companyService.IsExistAsync(name, code))
            {
                return Json(new { isSuccess = true, message = "Already Exists!" });
            }

            return Json(new { isSuccess = false });
        }
        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(new { success = false, message = "No IDs provided for delete." });
            }

            var hasPermission = await companyService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await companyService.DeleteTab(ids, model);
            if (success.succses)
            {
                return Json(new { success = true, message = "Deleted Successfully." });
            }
            else
            {
                return Json(new { success = false, message = success.messege });
            }
        }

        #endregion

        #region Table Loading (Custom Grid)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await companyService.GetAllAsync() ?? new List<CompanySetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<CompanySetupViewModel>(), error = ex.Message });
            }
        }

        public ActionResult Grid()
        {
            var result = companyService.GetCompanies();
            return Json(new { data = result });
        }

        #endregion

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




