using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Service.ChartOfAccounts;
using GCTL.UI.Core.Controllers;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Event;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class LedgerReportController : BaseController
    {
        #region

        private readonly LedgerReportService _service;
        public LedgerReportController(LedgerReportService service) : base()
        {
            _service = service;
        }

        #endregion

        #region View Ledger
        public IActionResult Index()
        {
            var treeData = _service.GetLedgerTreeDataAsync();

            return View(treeData);
        }

        #endregion

        #region  Listview Report
        public static List<AccountReportVM> FlattenTree(List<TreeLedgerReportVM> nodes, int level = 0)
        {
            var list = new List<AccountReportVM>();

            foreach (var node in nodes)
            {
                list.Add(new AccountReportVM
                {
                    GroupLedger = level == 0 ? node.Name : "",      // Level 0 = Group Ledger
                    ControlLedger = level == 1 ? node.Name : "",    // Level 1 = Control Ledger
                    SubControlLedger = level == 2 ? node.Name : "", // Level 2 = Sub-Control Ledger
                    SubSidiaryLedger = level == 3 ? node.Name : "", // Level 3 = Sub-Sidiary Ledger
                    //GeneralLedger = level == 4 ? node.Name : "", 
                    GeneralLedger = level >= 4 ? node.Name : "",
                    AccountCode = node.CodeNo,
                    Level = level
                });

                if (node.Children != null && node.Children.Count > 0)
                {
                    list.AddRange(FlattenTree(node.Children, level + 1));
                }

            }

            return list;
        }


        [HttpPost]
        public IActionResult Export(string format, string expandedNodes)
        {
            var expandedList = (expandedNodes ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            Console.WriteLine("Expanded Codes:");
            expandedList.ForEach(x => Console.WriteLine(x));

            var treeData = _service.GetLedgerTreeDataAsync();
            var filteredTree = FilterTree(treeData, expandedList);

            // Company Name from DB
            string companyName = _service.GetCompanyName();

            if (format == "PDF")
            {
                byte[] pdfBytes = GeneratePdf(filteredTree, companyName);
                return File(pdfBytes, "application/pdf", "Chart of Accounts Listview Report.pdf");
            }
            else if (format == "Excel")
            {
                byte[] excelBytes = GenerateExcelTree(filteredTree, companyName);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Chart of Accounts Listview Report.xlsx");
            }

            //return RedirectToAction("ReportView");
            return Ok("Invalid formate");
        }
         
        private List<TreeLedgerReportVM> FilterTree(List<TreeLedgerReportVM> nodes, IEnumerable<string> expandedCodes)
        {
            List<TreeLedgerReportVM> result = new();

            foreach (var node in nodes)
            {
                var newNode = new TreeLedgerReportVM
                {
                    CodeNo = node.CodeNo,
                    Name = node.Name,
                    ParentName = node.ParentName
                };

                if (expandedCodes.Contains(node.CodeNo))
                {
                    newNode.Children = FilterTree(node.Children, expandedCodes);
                }
                else
                {
                    newNode.Children = new List<TreeLedgerReportVM>();
                }

                result.Add(newNode);
            }

            return result;
        }
        #endregion

        #region Generate PDF
        public static byte[] GeneratePdf(List<TreeLedgerReportVM> treeData, string companyName)
        {
            var rows = FlattenTree(treeData);

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                document.SetMargins(30, 30, 30, 30);

                PdfFont timesNewRoman = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

                PdfFont timesNewRomanBold = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);

                // Footer
                PdfFormXObject totalPagesPlaceholder = new PdfFormXObject(new Rectangle(0, 0, 50, 10));
                var footerHandler = new PdfFooterHandler(timesNewRoman, totalPagesPlaceholder);
                pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, footerHandler);

                // Header
                //document.Add(new Paragraph(companyName).SetFont(timesNewRoman).SetTextAlignment(TextAlignment.CENTER).SetFontSize(16).SetBold());

                //document.Add(new Paragraph("Chart of Accounts Report").SetFont(timesNewRoman).SetTextAlignment(TextAlignment.CENTER).SetFontSize(14).SetBold().SetUnderline().SetMarginBottom(12));

                document.Add( new Paragraph(companyName)
                        .SetFont(timesNewRomanBold)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(16));

                document.Add( new Paragraph("Chart of Accounts Report")
                        .SetFont(timesNewRomanBold)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(14)
                        .SetUnderline()
                        .SetMarginBottom(12));

                float[] columnWidths = { 10, 15, 15, 15, 20, 25 };
                Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                string[] headers = { "Group Ledger", "Control Ledger", "Sub-Control Ledger", "Sub-Sidiary Ledger", "Account Code", "Account Head" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(header).SetFont(timesNewRomanBold)
                        .SetBackgroundColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(5)));
                }

                int i = 0;
                while (i < rows.Count)
                {
                    var groupRow = rows[i];
                    int groupSpan = rows.Skip(i).TakeWhile(r => r.GroupLedger == groupRow.GroupLedger).Count();

                    // Group Ledger
                    table.AddCell(new Cell(groupSpan, 1)
                        .Add(new Paragraph(groupRow.GroupLedger).SetFont(timesNewRoman).SetFontSize(10).SetMarginLeft(2))
                        .SetTextAlignment(TextAlignment.LEFT).SetKeepTogether(true));

                    int groupEnd = i + groupSpan;
                    int rowPtr = i;

                    while (rowPtr < groupEnd)
                    {
                        var controlRow = rows[rowPtr];
                        int controlSpan = rows.Skip(rowPtr).TakeWhile(r => r.GroupLedger == groupRow.GroupLedger && r.ControlLedger == controlRow.ControlLedger).Count();

                        // Control Ledger
                        table.AddCell(new Cell(controlSpan, 1)
                            .Add(new Paragraph(controlRow.ControlLedger).SetFont(timesNewRoman).SetFontSize(10).SetMarginLeft(2))
                            .SetTextAlignment(TextAlignment.LEFT).SetKeepTogether(true));

                        int controlEnd = rowPtr + controlSpan;
                        int subPtr = rowPtr;

                        while (subPtr < controlEnd)
                        {
                            var subControlRow = rows[subPtr];
                            int subControlSpan = rows.Skip(subPtr).TakeWhile(r =>
                                r.GroupLedger == groupRow.GroupLedger &&
                                r.ControlLedger == controlRow.ControlLedger &&
                                r.SubControlLedger == subControlRow.SubControlLedger).Count();

                            // Sub-Control Ledger
                            table.AddCell(new Cell(subControlSpan, 1)
                                .Add(new Paragraph(subControlRow.SubControlLedger).SetFont(timesNewRoman).SetFontSize(10).SetMarginLeft(2))
                                .SetTextAlignment(TextAlignment.LEFT).SetKeepTogether(true));

                            int subControlEnd = subPtr + subControlSpan;
                            int subSubPtr = subPtr;

                            while (subSubPtr < subControlEnd)
                            {
                                var subSubRow = rows[subSubPtr];
                                int subSubSpan = rows.Skip(subSubPtr).TakeWhile(r =>
                                    r.GroupLedger == groupRow.GroupLedger &&
                                    r.ControlLedger == controlRow.ControlLedger &&
                                    r.SubControlLedger == subControlRow.SubControlLedger &&
                                    r.SubSidiaryLedger == subSubRow.SubSidiaryLedger).Count();

                                // Sub-Sidiary Ledger
                                table.AddCell(new Cell(subSubSpan, 1)
                                    .Add(new Paragraph(subSubRow.SubSidiaryLedger).SetFont(timesNewRoman).SetFontSize(10).SetMarginLeft(2))
                                    .SetTextAlignment(TextAlignment.LEFT).SetKeepTogether(true));

                                // AccountCode & AccountHead row-wise
                                for (int k = 0; k < subSubSpan; k++)
                                {
                                    var rowData = rows[subSubPtr + k];
                                    table.AddCell(new Cell().Add(new Paragraph(rowData.AccountCode).SetFont(timesNewRoman).SetFontSize(10)).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                    table.AddCell(new Cell().Add(new Paragraph(rowData.GeneralLedger).SetFont(timesNewRoman).SetFontSize(10).SetMarginLeft(2)).SetTextAlignment(TextAlignment.LEFT).SetKeepTogether(true));
                                }

                                subSubPtr += subSubSpan;
                            }

                            subPtr += subControlSpan;
                        }

                        rowPtr += controlSpan;
                    }

                    i += groupSpan;
                }

                document.Add(table);

                footerHandler.WriteTotalPages(pdf);
                document.Close();

                return ms.ToArray();
            }
        }
        #endregion

        #region Generate Excel
        public static byte[] GenerateExcelTree(List<TreeLedgerReportVM> treeData, string companyName)
        {
            var rows = FlattenTree(treeData);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Chart Of Accounts");

            //Header
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 1).Value = "Chart of Accounts Report";
            ws.Range(2, 1, 2, 6).Merge();
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(2, 1).Style.Font.Underline = ClosedXML.Excel.XLFontUnderlineValues.Single;
            ws.Cell(2, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            int startRow = 4;

            //Table Header
            string[] headers = { "Group Ledger", "Control Ledger", "Sub-Control Ledger", "Sub-Sidiary Ledger", "Account Code", "Account Head" };
            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(startRow, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Font.FontSize = 10;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            }

            int currentRow = startRow + 1;
            int totalRows = rows.Count;

            //Table Body
            for (int i = 0; i < totalRows;)
            {
                var groupRow = rows[i];
                int groupSpan = rows.Skip(i).TakeWhile(r => r.GroupLedger == groupRow.GroupLedger).Count();

                // Group Ledger
                var groupRange = ws.Range(currentRow, 1, currentRow + groupSpan - 1, 1);
                groupRange.Merge();
                groupRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                ws.Cell(currentRow, 1).Value = groupRow.GroupLedger;
                ws.Cell(currentRow, 1).Style.Font.FontName = "Times New Roman";
                ws.Cell(currentRow, 1).Style.Font.FontSize = 10;
                ws.Cell(currentRow, 1).Style.Alignment.Indent = 1;
                ws.Cell(currentRow, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;

                int groupEnd = i + groupSpan;
                int rowPtr = i;

                while (rowPtr < groupEnd)
                {
                    var controlRow = rows[rowPtr];
                    int controlSpan = rows.Skip(rowPtr).TakeWhile(r =>
                        r.ControlLedger == controlRow.ControlLedger &&
                        r.GroupLedger == groupRow.GroupLedger).Count();

                    // Control Ledger
                    var controlRange = ws.Range(currentRow, 2, currentRow + controlSpan - 1, 2);
                    controlRange.Merge();
                    controlRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    ws.Cell(currentRow, 2).Value = controlRow.ControlLedger;
                    ws.Cell(currentRow, 2).Style.Font.FontName = "Times New Roman";
                    ws.Cell(currentRow, 2).Style.Font.FontSize = 10;
                    ws.Cell(currentRow, 2).Style.Alignment.Indent = 1;
                    ws.Cell(currentRow, 2).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;

                    int controlEnd = rowPtr + controlSpan;
                    int subPtr = rowPtr;

                    while (subPtr < controlEnd)
                    {
                        var subControlRow = rows[subPtr];
                        int subControlSpan = rows.Skip(subPtr).TakeWhile(r =>
                            r.GroupLedger == groupRow.GroupLedger &&
                            r.ControlLedger == controlRow.ControlLedger &&
                            r.SubControlLedger == subControlRow.SubControlLedger).Count();

                        // Sub-Control Ledger
                        var subControlRange = ws.Range(currentRow, 3, currentRow + subControlSpan - 1, 3);
                        subControlRange.Merge();
                        subControlRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        ws.Cell(currentRow, 3).Value = subControlRow.SubControlLedger;
                        ws.Cell(currentRow, 3).Style.Font.FontName = "Times New Roman";
                        ws.Cell(currentRow, 3).Style.Font.FontSize = 10;
                        ws.Cell(currentRow, 3).Style.Alignment.Indent = 1;
                        ws.Cell(currentRow, 3).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                        ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;

                        int subControlEnd = subPtr + subControlSpan;
                        int subSubPtr = subPtr;

                        while (subSubPtr < subControlEnd)
                        {
                            var subSubRow = rows[subSubPtr];
                            int subSubSpan = rows.Skip(subSubPtr).TakeWhile(r =>
                                r.GroupLedger == groupRow.GroupLedger &&
                                r.ControlLedger == controlRow.ControlLedger &&
                                r.SubControlLedger == subControlRow.SubControlLedger &&
                                r.SubSidiaryLedger == subSubRow.SubSidiaryLedger).Count();

                            // Sub-Sidiary Ledger
                            var subSidiaryRange = ws.Range(currentRow, 4, currentRow + subSubSpan - 1, 4);
                            subSidiaryRange.Merge();
                            subSidiaryRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                            ws.Cell(currentRow, 4).Value = subSubRow.SubSidiaryLedger;
                            ws.Cell(currentRow, 4).Style.Font.FontName = "Times New Roman";
                            ws.Cell(currentRow, 4).Style.Font.FontSize = 10;
                            ws.Cell(currentRow, 4).Style.Alignment.Indent = 1;
                            ws.Cell(currentRow, 4).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;

                            // Account Code & General Ledger
                            for (int k = 0; k < subSubSpan; k++)
                            {
                                var rowData = rows[subSubPtr + k];
                                ws.Cell(currentRow + k, 5).Value = rowData.AccountCode;
                                ws.Cell(currentRow + k, 6).Value = rowData.GeneralLedger;

                                for (int c = 5; c <= 6; c++)
                                {
                                    var cell = ws.Cell(currentRow + k, c);
                                    cell.Style.Font.FontName = "Times New Roman";
                                    cell.Style.Font.FontSize = 10;
                                    cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                                    cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                                    if (c == 5)
                                    {
                                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                    }
                                    else
                                    {
                                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                                        cell.Style.Alignment.Indent = 1;
                                    }
                                }
                            }

                            currentRow += subSubSpan;
                            subSubPtr += subSubSpan;
                        }

                        subPtr += subControlSpan;
                    }

                    rowPtr += controlSpan;
                }

                i += groupSpan;
            }

            //Global Border Fix (Full Table)
            ws.Range(startRow, 1, currentRow - 1, 6).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            ws.Range(startRow, 1, currentRow - 1, 6).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

            //Auto-fit
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion

        #region Footer Handler

        public class PdfFooterHandler : AbstractPdfDocumentEventHandler
        {
            private readonly PdfFont _font;
            private readonly PdfFormXObject _totalPagesPlaceholder;

            public PdfFooterHandler(
                PdfFont font,
                PdfFormXObject totalPagesPlaceholder)
            {
                _font = font;
                _totalPagesPlaceholder = totalPagesPlaceholder;
            }

            protected override void OnAcceptedEvent(AbstractPdfDocumentEvent @event)
            {
                var pdfEvent = (PdfDocumentEvent)@event;

                var pdf = pdfEvent.GetDocument();
                var page = pdfEvent.GetPage();
                var pageSize = page.GetPageSize();

                var canvas = new Canvas(
                    new PdfCanvas(
                        page.NewContentStreamBefore(),
                        page.GetResources(),
                        pdf),
                    pageSize);

                float bottomY = 12;
                float leftX = 30;
                float rightX = pageSize.GetWidth() - 30;

                int pageNumber = pdf.GetPageNumber(page);

                string dateTime =
                    DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");

                string leftText = $"Print Datetime: {dateTime}";
                string rightText = $"Page {pageNumber} of ";

                canvas.ShowTextAligned(
                    new Paragraph(leftText)
                        .SetFont(_font)
                        .SetFontSize(8)
                        .SetFontColor(ColorConstants.GRAY),
                    leftX,
                    bottomY,
                    TextAlignment.LEFT);

                canvas.ShowTextAligned(
                    new Paragraph(rightText)
                        .SetFont(_font)
                        .SetFontSize(8)
                        .SetFontColor(ColorConstants.GRAY),
                    rightX - 4,
                    bottomY,
                    TextAlignment.RIGHT);

                canvas.Add(
                    new Image(_totalPagesPlaceholder)
                        .SetFixedPosition(rightX, bottomY + 2, 50));

                canvas.Close();
            }

            public void WriteTotalPages(PdfDocument pdfDoc)
            {
                int totalPages = pdfDoc.GetNumberOfPages();

                PdfCanvas canvas =
                    new PdfCanvas(_totalPagesPlaceholder, pdfDoc);

                canvas.BeginText();
                canvas.SetFontAndSize(_font, 8);
                canvas.SetFillColor(ColorConstants.GRAY);
                canvas.MoveText(0, 0);
                canvas.ShowText(totalPages.ToString());
                canvas.EndText();
            }
        }
        #endregion
    }
}
