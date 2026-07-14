using AutoMapper;
using ClosedXML.Excel;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Departments;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.Departments;
using GCTL.Service.Designations;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.Departments;
using iText.IO.Font;
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
    public class DepartmentsController : BaseController
    {
        private readonly IDepartmentService departmentService;
        private readonly ICommonService commonService;
        private readonly IMapper mapper;
        string strMaxNO = "";
        public DepartmentsController(
            IDepartmentService departmentService,
            ICommonService commonService,
            IMapper mapper
            
        )
        {
            this.departmentService = departmentService;
            this.commonService = commonService;
            this.mapper = mapper;
        }

        public IActionResult Index(bool child = false)
        {
            DepartmentPageViewModel model = new DepartmentPageViewModel()
            {
                PageUrl = Url.Action(nameof(Index))
            };
            commonService.FindMaxNo(ref strMaxNO, "DepartmentCode", "HRM_Def_Department", 3);
            model.Setup = new DepartmentSetupViewModel
            {
                DepartmentCode = strMaxNO
            };

            if (child)
                return PartialView(model);

            return View(model);
        }


        public IActionResult Setup(string id)
        {
            DepartmentSetupViewModel model = new DepartmentSetupViewModel();
            commonService.FindMaxNo(ref strMaxNO, "DepartmentCode", "HRM_Def_Department", 3);
            var result = departmentService.GetDepartment(id);
            if (result != null)
            {
                model = mapper.Map<DepartmentSetupViewModel>(result);
                model.Code = id;
                model.Id = (int)result.AutoId;
            }
            else
            {
                model.DepartmentCode = strMaxNO;
            }

            return PartialView($"_{nameof(Setup)}", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Setup(DepartmentSetupViewModel model)
        {
            if (departmentService.IsDepartmentExist(model.DepartmentName, model.DepartmentCode))
            {
                return Json(new { isSuccess = false, message = "Already Exists" });
            }

            if (ModelState.IsValid)
            {
                if (departmentService.IsDepartmentExistByCode(model.DepartmentCode))
                {
                    var hasPermission = departmentService.UpdatePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        HrmDefDepartment department = departmentService.GetDepartment(model.DepartmentCode) ?? new HrmDefDepartment();
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, department);

                        departmentService.SaveDepartment(department);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = department.DepartmentCode });
                    }
                    else
                    {

                        return Json(new { isSuccess = false, message = "You have no access" });
                    }

                }
                else
                {
                    var hasPermission = departmentService.SavePermission(LoginInfo.AccessCode);
                    if (hasPermission)
                    {
                        HrmDefDepartment department = departmentService.GetDepartment(model.DepartmentCode) ?? new HrmDefDepartment();
                        model.ToAudit(LoginInfo, model.Id > 0);
                        mapper.Map(model, department);
                        departmentService.SaveDepartment(department);
                        return Json(new { isSuccess = true, message = "Saved Successfully", lastCode = department.DepartmentCode });
                    }
                    else
                    {

                        return Json(new { isSuccess = false, message = "You have no access" });
                    }

                }


            }

            return Json(new { success = false, message = ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage });
        }

        public ActionResult Grid()
        {
            var result = departmentService.GetDepartments();
            return Json(new { data = result });
        }


        [HttpPost]

        public async Task<ActionResult> Delete(string id)

        {

            var hasPermission = departmentService.DeletePermission(LoginInfo.AccessCode);

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
            var result = await departmentService.DeleteDepartment(ids, model);

            results.Add(result);


            // Aggregate outcome

            bool overallSuccess = results.All(r => r.success);

            bool refSuccess = results.All(r => r.refSuccess);

            string combinedMessage = overallSuccess

                ? "Deleted Successfully"

                : string.Join("; ", results.Where(r => !r.success).Select(r => r.message));

            return Json(new { success = overallSuccess, message = combinedMessage, refSuccess = refSuccess });

        }



        [HttpPost]
        public JsonResult CheckAvailability(string name, string code)
        {
            if (departmentService.IsDepartmentExist(name, code))
            {
                return Json(new { isSuccess = true, message = "Already Exists!" });
            }

            return Json(new { isSuccess = false });
        }


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