using ClosedXML.Excel;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Departments;
using GCTL.Service.Common;
using GCTL.Service.Departments;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Departments;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.UI.Core.Controllers
{
    public class DepartmentsController : BaseController
    {
        #region Ser & Repo
        private readonly IDepartmentService departmentService;
        private readonly ICommonService commonService;

        string strMaxNO = string.Empty;
        public DepartmentsController(
            ICommonService commonService,
            IDepartmentService departmentService
        )
        {
            this.departmentService = departmentService;
            this.commonService = commonService;
        }

        #endregion

        #region Index

        public async Task<IActionResult> Index(bool child = false, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var hasPermission = await departmentService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var model = new DepartmentPageViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            try
            {
                var list = await departmentService.GetAllAsync() ?? new List<DepartmentSetupViewModel>();
                model.DepartmentList = list;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    list = list.Where(x => (x.DepartmentCode != null && x.DepartmentCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.DepartmentName != null && x.DepartmentName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.DepartmentShortName != null && x.DepartmentShortName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                                        || (x.BanglaDepartment != null && x.BanglaDepartment.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                               .ToList();
                }

                int totalRecords = list.Count;
                var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                model.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: page,
                    pageSize: pageSize,
                    headers: new List<string> { "Code", "Department Name", "Short Name", "Department (বাংলা)" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "DepartmentCode", item.DepartmentCode },
                        { "Department Name", item.DepartmentName },
                        { "Short Name", item.DepartmentShortName },
                        { "Department (বাংলা)", item.BanglaDepartment }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Departments") ?? "/Departments/GetTableData",
                    primaryKey: "DepartmentCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/Departments/Setup/{id}",
                    gridId: "departmentGridContainer",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "DepartmentCode", "center" },
                        { "Department Name", "left" },
                        { "Short Name", "center" },
                        { "Department (বাংলা)", "left" }
                    },
                    searchTerm: searchTerm,
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Department Name", "auto" },
                        { "Short Name", "120px" },
                        { "Department (বাংলা)", "auto" }
                    }
                );

                commonService.FindMaxNo(ref strMaxNO, "DepartmentCode", "HRM_Def_Department", 2);
                model.Setup = new DepartmentSetupViewModel
                {
                    DepartmentCode = strMaxNO
                };
            }
            catch (Exception ex)
            {
                model.DepartmentList = new List<DepartmentSetupViewModel>();
                model.Setup = new DepartmentSetupViewModel();
                Console.WriteLine("Error" + ex.Message);
            }

            if (child)
                return PartialView(model);

            return View(model);
        }

        #endregion

        #region Setup

        public async Task<IActionResult> Setup(string id)
        {
            DepartmentSetupViewModel model = new DepartmentSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "DepartmentCode", "HRM_Def_Department", 2);

            if (!string.IsNullOrEmpty(id))
            {
                model = await departmentService.GetByIdAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
            }
            else
            {
                model.DepartmentCode = strMaxNO;
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView($"_{nameof(Setup)}", model);
            }

            // For direct browser URL navigation, render full Index page with pre-selected item
            var hasPermission = await departmentService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            var indexModel = new DepartmentPageViewModel
            {
                PageUrl = Url.Action(nameof(Index)),
                Setup = model
            };

            try
            {
                var list = await departmentService.GetAllAsync() ?? new List<DepartmentSetupViewModel>();
                indexModel.DepartmentList = list;

                int totalRecords = list.Count;
                var pagedList = list.Take(10).ToList();

                indexModel.DynamicGrid = GridBuilder.BuildGrid(
                    data: pagedList,
                    totalRecords: totalRecords,
                    currentPage: 1,
                    pageSize: 10,
                    headers: new List<string> { "Code", "Department Name", "Short Name", "Department (বাংলা)" },
                    rowMapper: item => new Dictionary<string, object?>
                    {
                        { "DepartmentCode", item.DepartmentCode },
                        { "Department Name", item.DepartmentName },
                        { "Short Name", item.DepartmentShortName },
                        { "Department (বাংলা)", item.BanglaDepartment }
                    },
                    ajaxUrl: Url.Action("GetTableData", "Departments") ?? "/Departments/GetTableData",
                    primaryKey: "DepartmentCode",
                    hasCheckbox: true,
                    actionUrlPattern: "/Departments/Setup/{id}",
                    gridId: "departmentGridContainer",
                    columnAlignments: new Dictionary<string, string>
                    {
                        { "DepartmentCode", "center" },
                        { "Department Name", "left" },
                        { "Short Name", "center" },
                        { "Department (বাংলা)", "left" }
                    },
                    columnWidths: new Dictionary<string, string>
                    {
                        { "SelectAll", "120px" },
                        { "Code", "100px" },
                        { "Department Name", "auto" },
                        { "Short Name", "120px" },
                        { "Department (বাংলা)", "auto" }
                    }
                );
            }
            catch (Exception ex)
            {
                indexModel.DepartmentList = new List<DepartmentSetupViewModel>();
                Console.WriteLine("Error: " + ex.Message);
            }

            return View("Index", indexModel);
        }

        #endregion

        #region Post Update 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(DepartmentSetupViewModel modelVM)
        {
            try
            {
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    if (await departmentService.IsExistAsync(modelVM.DepartmentName, modelVM.DepartmentCode))
                    {
                        return Json(new { isSuccess = false, message = $"Already Exists!", isDuplicate = true });
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errorMessage = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return Json(new { isSuccess = false, message = errorMessage });
                }

                modelVM.ToAudit(LoginInfo, modelVM.AutoId > 0);
                if (modelVM.AutoId == 0 || modelVM.AutoId == null)
                {
                    var hasSavePermission = await departmentService.SavePermissionAsync(LoginInfo.AccessCode);
                    if (hasSavePermission)
                    {
                        await departmentService.SaveAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Saved Successfully.", lastCode = modelVM.DepartmentCode });
                    }
                    else
                    {
                        return Json(new { isSuccess = false, message = "You have no access to save.", noSavePermission = true });
                    }
                }
                else
                {
                    var hasUpdatePermission = await departmentService.UpdatePermissionAsync(LoginInfo.AccessCode);
                    if (hasUpdatePermission)
                    {
                        await departmentService.UpdateAsync(modelVM);
                        return Json(new { isSuccess = true, message = "Updated Successfully.", lastCode = modelVM.DepartmentCode });
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
            if (await departmentService.IsExistAsync(name, code))
            {
                return Json(new { isSuccess = true, message = $"Already Exists!" });
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

            var hasPermission = await departmentService.DeletePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return Json(new { success = false, message = "You have no access." });
            }

            DeleteHistoryViewModel model = new DeleteHistoryViewModel();
            model.ToAudit(LoginInfo);
            model.CompanyCode = LoginInfo.CompanyCode;

            var success = await departmentService.DeleteTab(ids, model);
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

        #region Table Loading (Dynamic Grid)

        [HttpPost]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await departmentService.GetAllAsync() ?? new List<DepartmentSetupViewModel>();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new List<DepartmentSetupViewModel>(), error = ex.Message });
            }
        }

        #endregion


        #region Xls
        public async Task<IActionResult> ExportToExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Departments");

                // ── Title Row ──────────────────────────────────────────────
                var titleCell = worksheet.Cell(1, 1);
                titleCell.Value = "Department Information";
                worksheet.Range(1, 1, 1, 4).Merge();
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                titleCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                titleCell.Style.Font.FontColor = XLColor.White;
                worksheet.Row(1).Height = 28;

                // ── Empty Spacer Row ───────────────────────────────────────
                worksheet.Range(2, 1, 2, 4).Merge();
                worksheet.Row(2).Height = 6;

                // ── Header Row ─────────────────────────────────────────────
                int dataStartRow = 3;
                string[] headers = { "Department Id", "Department Name", "Short Name", "Department (বাংলা)" };

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
                var departments = departmentService.GetDepartments().ToList();

                // Track max char length per column for manual width calculation
                double[] colWidths = headers.Select(h => (double)h.Length).ToArray();

                int row = dataStartRow + 1;
                bool alternate = false;

                foreach (var dept in departments)
                {
                    string colId = "'" + (dept.DepartmentCode ?? "").PadLeft(2, '0');
                    string colName = dept.DepartmentName ?? "";
                    string colShort = dept.DepartmentShortName ?? "";
                    string colBangla = dept.BanglaDepartment ?? "";

                    string[] values = { colId, colName, colShort, colBangla };

                    for (int col = 0; col < values.Length; col++)
                    {
                        var cell = worksheet.Cell(row, col + 1);
                        cell.Value = values[col];

                        // Alignment: center for Id & ShortName, left for Name & Bangla
                        cell.Style.Alignment.Horizontal = col == 0 || col == 2
                            ? XLAlignmentHorizontalValues.Center
                            : XLAlignmentHorizontalValues.Left;

                        // Alternating row color
                        if (alternate)
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF0FA");

                        // Border
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        // Track max display length (strip leading quote)
                        string displayText = values[col].TrimStart('\'');
                        colWidths[col] = Math.Max(colWidths[col], displayText.Length);
                    }

                    worksheet.Row(row).Height = 18;
                    row++;
                    alternate = !alternate;
                }

                // ── Column Widths (no System.Drawing needed) ───────────────
                double[] minWidths = { 14, 28, 14, 24 };
                double[] maxWidths = { 20, 50, 22, 45 };

                for (int col = 0; col < 4; col++)
                {
                    double calculated = colWidths[col] + 4;   // padding
                    double clamped = Math.Max(minWidths[col],
                                       Math.Min(maxWidths[col], calculated));
                    worksheet.Column(col + 1).Width = clamped;
                }

                // ── Freeze Header Rows ─────────────────────────────────────
                worksheet.SheetView.FreezeRows(dataStartRow);

                // ── Save & Return ──────────────────────────────────────────
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Departments.xlsx"
                    );
                }
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

                // Add title
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Add Title
                Paragraph title = new Paragraph("Department Information")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetFont(font);
                document.Add(title);

                // Add some space
                document.Add(new Paragraph("\n"));

                // Create a table with 4 columns
                Table table = new Table(UnitValue.CreatePercentArray(4)).UseAllAvailableWidth();

                // Add headers
                string[] headers = { "Department Id", "Department Name", "Short Name", "Department (বাংলা)" };
                float headerFontSize = 10f;
                float dataFontSize = 9f;

                foreach (var header in headers)
                {
                    table.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(header)
                                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                                .SetFontSize(headerFontSize))
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetBorder(new SolidBorder(0.5f)));
                }

                // Add data
                var departments = departmentService.GetDepartments();
                foreach (var department in departments)
                {
                    table.AddCell(new Cell()
                        .Add(new Paragraph(department.DepartmentCode.PadLeft(2, '0') ?? "")
                            .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(department.DepartmentName ?? "")
                            .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.LEFT));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(department.DepartmentShortName ?? "")
                            .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.CENTER));

                    table.AddCell(new Cell()
                        .Add(new Paragraph(department.BanglaDepartment ?? "")
                            .SetFontSize(dataFontSize))
                        .SetTextAlignment(TextAlignment.LEFT));
                }

                // Add the table to the document
                document.Add(table);

                // Close the document
                document.Close();

                // Return the PDF as a file
                return File(stream.ToArray(), "application/pdf", "Departments.pdf");
            }
        }




        #endregion
    }
}