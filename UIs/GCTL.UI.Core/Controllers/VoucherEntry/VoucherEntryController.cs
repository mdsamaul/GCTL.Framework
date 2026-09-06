using ClosedXML.Excel;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.VoucherEntry;
using GCTL.Service.OpeningBalance;
using GCTL.Service.VoucherEntry;
using GCTL.UI.Core.Controllers;
using GCTL_App.Controllers.PDfFooterHandler;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Event;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
namespace GCTL_App.Controllers.VoucherEntry
{
    public class VoucherEntryController : BaseController
    {

        #region Service

        private readonly IVoucherEntry _service;
        private readonly IOpeningBalance _openingBalance;
        private readonly VoucherReportService _voucherReportService;

        public VoucherEntryController(IVoucherEntry service, IOpeningBalance openingBalance, VoucherReportService voucherReportService) : base()
        {
            _service = service;
            _openingBalance = openingBalance;
            _voucherReportService = voucherReportService;
        }

        #endregion


        #region Index Page
        public IActionResult Index()
        {
            return View();
        }

        #endregion


        #region Debit & Credit Amount Validation Checking

        [HttpGet]
        [Route("check-debit-credit")]
        public async Task<IActionResult> CheckDebitCredit()
        {
           string ? currentUserId = LoginInfo?.Username;

            var isValid = await _service.IsDebitCreditEqualAsync(currentUserId.ToString());

            if (!isValid)
                return BadRequest(new { success = false, message = "Debit & Credit Amount Must Be Equal." });

            return Ok(new { success = true, message = "Debit & Credit is Equal." });
        }

        #endregion


        #region Voucher Entry Save and Update

        [HttpPost]
        [Route("voucher-entry")]
        public async Task<ActionResult> Create([FromBody] VoucherEntryVM model)
        {
            try
            {
                if (model == null)  return BadRequest(new { message = "Voucher Code No  is Required." });

                model.ToAudit(LoginInfo);
                var result = await _service.SaveAsync(model);


                if (!result)  return BadRequest(new { message = "Insertion Failed." });

                return Ok(new { success = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }


        [HttpPut]
        [Route("voucher-entry/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] VoucherEntryVM model)
        {
            try {
            if (model == null || id != model.autoId) return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Voucher Entry with ID {id} Not Found." });
                model.ToAudit(LoginInfo);
            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        #endregion


        #region Generate Voucher Entry Code

        [HttpGet("generate-voucher-no/{voucherTypeCode}")]
        public async Task<IActionResult> GenerateVoucherNo(string voucherTypeCode)
        {
            try
            {
                var voucherNo = await _service.GetLastVoucherNoAsync(voucherTypeCode);
                return Ok(new { voucherNo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error generating Voucher No", error = ex.Message });
            }

        }
        #endregion


        #region Get All Voucher Entry

        [HttpPost]
        [Route("VoucherEntry/GetTableData")]
        public async Task<IActionResult> GetTableData(int pageNumber = 1, int pageSize = 10, string sortColumn = "VoucherDate", string sortOrder = "desc", string searchTerm = "")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, null, null);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new { data = new List<VoucherEntryVM>(), recordsTotal = 0, recordsFiltered = 0 });
                }

                return Ok(new { data = result.Data, recordsTotal = result.TotalCount, recordsFiltered = result.TotalCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { data = new List<VoucherEntryVM>(), recordsTotal = 0, recordsFiltered = 0, message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("entry-voucher-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherDate", string sortOrder = "desc", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, startDate, endDate);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<VoucherEntryVM>(),
                        TotalCount = 0,
                        PaginationInfo = new
                        {
                            StartItem = 0,
                            EndItem = 0,
                            TotalItems = 0,
                            PageNumbers = new List<int>(),
                            TotalPages = 0,
                            CurrentPage = 0
                        },
                        Message = "No Voucher Found Found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        #endregion


        #region Voucher Type & Account Head & Company & Branch Dropdown

        [HttpGet]
        [Route("voucher-Type-dropdown")]
        public async Task<IActionResult> VoucherTypeDropdown()
        {
            var list = await _service.GetVoucherTypeDropdownInfo();
            return Ok(new { data = list });
        }



        [HttpGet]
        [Route("voucerentry-company-dropdown")]
        public async Task<IActionResult> CompanyDropdown()
        {
            var list = await _openingBalance.GetCompanyDropdownInfo();
            return Ok(new { data = list });
        }

        [HttpGet]
        [Route("voucerentry-branch-dropdown")]
        public async Task<IActionResult> BranchDropdown(string companycode)
        {
            var list = await _openingBalance.GetBranchDropdownInfo(companycode);
            return Ok(new { data = list });
        }
        #endregion


        #region Get All Master & Details Voucher for Edit

        [HttpGet("voucher-entry-details/{id}")]
        public async Task<IActionResult> GetVoucherEntryDetails([FromRoute] decimal id)
        {
            var main = await _service.GetByMasterIdAsync(id);
            if (main == null)
                return NotFound(new { success = false, message = "Voucher not found" });

            //var details = await _service.GetDetailsByVoucherIdAsync(main.autoId);

            return Ok(new { success = true, data = new { Main = main} });
        }


        [HttpPost("details-copied-tmptable/{id}")]
        public async Task<IActionResult> CopyDetailsToTmp([FromRoute] decimal id)
        {
            string currentUserId = LoginInfo?.Username;

            int.TryParse(currentUserId, out int employeeId);
            try
            {
                var success = await _service.DetailsCopiedToTmp(id, currentUserId);

                if (success)
                    return Ok(new { success = true, message = "Details copied to temp table successfully" });

                return BadRequest(new { success = false, message = "Failed to copy details" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {success = false,message = "An error occurred while copying details to temp table.", error = ex.ToString() });
            }
        }
        #endregion


        #region Voucher Delete Singel Or More

        [HttpDelete]
        [Route("voucher-master-all-delete")]
        public async Task<ActionResult> BulkDelete([FromBody] List<decimal> ids)
        {
            try
            {
                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Data Is Selected To Delete" });
                }

                var result = await _service.BulkDeleteAsync(ids);
                if (!result)
                {
                    return Json(new { isSuccess = false, message = "No Data Found to Delete" });
                }
                return Json(new { isSuccess = true, message = $"Data Deleted Successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }


        [HttpDelete]
        [Route("delete-with-user")]
        public async Task<IActionResult> Delete()
        {
            string currentUserId = LoginInfo?.EmployeeId;

            int.TryParse(currentUserId, out int employeeId); try
            {
                var data = await _service.DeleteAsync(employeeId);

                    return Json(new { success = true, message = "Data Deleted Succssfully" });

            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion


        #region Checking Debit & Credit Equal or Not

        //[HttpGet("CheckTmp/{userId}")]
        //public async Task<IActionResult> CheckTmp(string userId)
        //{
        //    bool isBalanced = await _service.ValidateDebitCreditForCreate(userId);
        //    if (isBalanced)
        //        return Ok(new { success = true, message = "Debit and Credit are equal." });
        //    else
        //        return BadRequest(new { success = false, message = "Debit and Credit are NOT equal." });
        //}


        //[HttpGet("CheckUpdate/{voucherEntryAutoId}")]
        //public async Task<IActionResult> CheckUpdate(decimal voucherEntryAutoId)
        //{
        //    bool isBalanced = await _service.ValidateDebitCreditForUpdate(voucherEntryAutoId);
        //    if (isBalanced)
        //        return Ok(new { success = true, message = "Debit and Credit are equal." });
        //    else
        //        return BadRequest(new { success = false, message = "Debit and Credit are NOT equal." });
        //}

        #endregion


        #region Report Section

        #region Preview PDf

        [HttpGet("PdfPreview")]
        public async Task<IActionResult> PdfPreview([FromQuery] string ids)
        {
            var idList = ids.Split(',').Select(d => decimal.Parse(d.Trim())).ToList();
            var data = await _voucherReportService.GetVoucherPreviewDataAsync(idList);

            var pdfBytes = GenerateVoucherPdf(data);
            return File(pdfBytes, "application/pdf");
        }

        #endregion


        #region Download Pdf

        [HttpGet("PdfDownload")]
        public async Task<IActionResult> PdfDownload([FromQuery] string ids)
        {
            var idList = ids.Split(',').Select(d => decimal.Parse(d.Trim())).ToList();
            var data = await _voucherReportService.GetVoucherPreviewDataAsync(idList);

            var pdfBytes = GenerateVoucherPdf(data);
            return File(pdfBytes, "application/pdf", "Voucher.pdf");
        }

        #endregion


        #region Download Excel

        [HttpGet("ExcelDownload")]
        public async Task<IActionResult> ExcelDownload([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest("No voucher IDs provided.");

            // Convert comma-separated IDs to decimal list
            var idList = ids.Split(',').Select(d => decimal.Parse(d.Trim())).ToList();

            // Get the data for the selected vouchers
            var data = await _voucherReportService.GetVoucherPreviewDataAsync(idList);

            if (data == null || !data.Any())
                return NotFound("No voucher data found.");

            // Generate Excel bytes using your existing method
            var excelBytes = GenerateVoucherExcel(data);

            // Return the file for download
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Voucher.xlsx");
        }

        #endregion


        #region Generate Excel Method

        public static byte[] GenerateVoucherExcel(List<VoucherPreviewVM> previewData)
        {
            if (previewData == null || !previewData.Any())
                throw new ArgumentException("No voucher data found.");

            using (var workbook = new XLWorkbook())
            {
                var grouped = previewData.GroupBy(x => x.VoucherNo);

                foreach (var voucherGroup in grouped)
                {
                    var ws = workbook.Worksheets.Add(voucherGroup.Key);
                    int row = 1;

                    var first = voucherGroup.First();

                    // 1. Company Header
                    ws.Cell(row, 1).Value = first.CompanyName;
                    ws.Range(row, 1, row, 4).Merge();
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 16;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    row++;

                    ws.Cell(row, 1).Value = first.CompanyAddress;
                    ws.Range(row, 1, row, 4).Merge();
                    ws.Cell(row, 1).Style.Font.FontSize = 10;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    row++;

                    ws.Cell(row, 1).Value = first.VoucherTypeName.ToUpper();
                    ws.Range(row, 1, row, 4).Merge();
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 12;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    row += 2;

                    // 2. Voucher Info
                    ws.Cell(row, 3).Value = "Voucher No:";
                    ws.Cell(row, 3).Style.Font.Bold = true;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Value = first.VoucherNo;
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    row++;

                    ws.Cell(row, 3).Value = "Voucher Date:";
                    ws.Cell(row, 3).Style.Font.Bold = true;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(row, 4).Value = first.VoucherDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    row += 2;

                    // 3. Details Table Header
                    ws.Cell(row, 1).Value = "Acc Code";
                    ws.Cell(row, 2).Value = "Particulars";
                    ws.Cell(row, 3).Value = "Debit";
                    ws.Cell(row, 4).Value = "Credit";

                    for (int c = 1; c <= 4; c++)
                    {
                        ws.Cell(row, c).Style.Font.Bold = true;
                        ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    row++;

                    decimal totalDebit = 0;
                    decimal totalCredit = 0;

                    // 4. Details Rows
                    foreach (var data in voucherGroup)
                    {
                        ws.Cell(row, 1).Value = data.AccCode;
                        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Cell(row, 2).Value = data.SubSubsidiaryLedgerName;
                        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                        ws.Cell(row, 3).Value = data.DebitAmount ?? 0;
                        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        ws.Cell(row, 4).Value = data.CreditAmount ?? 0;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        for (int c = 1; c <= 4; c++)
                        {
                            ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        totalDebit += data.DebitAmount ?? 0;
                        totalCredit += data.CreditAmount ?? 0;

                        row++;
                    }

                    // 5. Total Row
                    ws.Range(row, 1, row, 2).Merge();
                    ws.Cell(row, 1).Value = "Total";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(row, 3).Value = totalDebit;
                    ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 3).Style.Font.Bold = true;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    ws.Cell(row, 4).Value = totalCredit;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, 4).Style.Font.Bold = true;
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    for (int c = 1; c <= 4; c++)
                    {
                        ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                    row += 2;

                    // In Words
                    ws.Cell(row, 1).Value = "In Words:";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Value = NumberToWords((long)totalDebit) + " Taka Only";
                    ws.Cell(row, 2).Style.Font.Bold = false;
                    row++;

                    // Narration
                    ws.Cell(row, 1).Value = "Narration:";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Value = first.Narration;
                    ws.Cell(row, 2).Style.Font.Bold = false;
                    row += 2;


                    // 7. Signature Block
                    string[] signatures = { "Received By", "Prepared By", "Checked By", "Verified By", "Authorised Signature" };

                    for (int i = 0; i < signatures.Length; i++)
                    {
                        ws.Cell(row, i + 1).Value = "-----------";
                        ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    row++;

                    for (int i = 0; i < signatures.Length; i++)
                    {
                        ws.Cell(row, i + 1).Value = signatures[i];
                        ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, i + 1).Style.Font.Bold = true;
                    }

                    ws.Columns().AdjustToContents();
                }

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        #endregion


        #region Generate Pdf Method
        public static byte[] GenerateVoucherPdf(List<VoucherPreviewVM> previewData)
        {
            if (previewData == null || !previewData.Any())
                throw new ArgumentException("No voucher data found.");

            using (var ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4);
                document.SetMargins(30, 30, 30, 30);

                // Fonts
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                PdfFont companyfont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLDITALIC);
                // PdfFont addressfont = PdfFontFactory.CreateFont(StandardFonts.COURIER);

                // Footer
                var footerHandler = new PdfPrintDateHandler(font);
                pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, footerHandler);

                // Group by VoucherNo (multiple vouchers in one PDF)
                var grouped = previewData.GroupBy(x => x.VoucherNo);

                foreach (var voucherGroup in grouped)
                {
                    var first = voucherGroup.First();

                    //Company Header
                    document.Add(new Paragraph(first.CompanyName.ToUpper())
                        .SetFont(companyfont)
                        .SetFontSize(14)
                        .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(1));
                    //Company Address Header
                    document.Add(new Paragraph(first.CompanyAddress)
                        .SetFont(fontBold)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(0));
                    //Voucher Type Name Header
                    document.Add(new Paragraph(first.VoucherTypeName.ToUpper())
                        .SetFont(fontBold)
                        .SetFontSize(11)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    float labelWidth = 30f;
                    float valueWidth = 70f;

                    Table voucherDetailsTable = new Table(UnitValue.CreatePointArray(new float[] { labelWidth, valueWidth })).UseAllAvailableWidth();
                    voucherDetailsTable.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                    // Voucher No
                    voucherDetailsTable.AddCell(new Cell()
                        .Add(new Paragraph("Voucher No : ").SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(0).SetPaddingRight(2));

                    voucherDetailsTable.AddCell(new Cell()
                        .Add(new Paragraph( first.VoucherNo).SetFont(font).SetFontSize(10))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(0));

                    // Voucher Date
                    voucherDetailsTable.AddCell(new Cell()
                        .Add(new Paragraph("Voucher Date :\t").SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(0).SetPaddingRight(2));

                    voucherDetailsTable.AddCell(new Cell()
                        .Add(new Paragraph(first.VoucherDate.ToString("dd-MM-yyyy")).SetFont(font).SetFontSize(10))
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(0));

                    document.Add(voucherDetailsTable);
                    document.Add(new Paragraph("\n"));


                    //Details Table (Acc Code, Particulars, Debit, Credit)
                    float[] columnWidths = { 20f, 40f, 20f, 20f };
                    Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();
                    table.SetKeepTogether(true);

                    // Header row
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Acc Code").SetFont(fontBold)).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Particulars").SetFont(fontBold)).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Debit").SetFont(fontBold)).SetTextAlignment(TextAlignment.CENTER));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Credit").SetFont(fontBold)).SetTextAlignment(TextAlignment.CENTER));

                    decimal totalDebit = 0;
                    decimal totalCredit = 0;

                    foreach (var row in voucherGroup)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(row.AccCode).SetFont(font)).SetTextAlignment(TextAlignment.CENTER).SetFontSize(10));
                        table.AddCell(new Cell().Add(new Paragraph(row.SubSubsidiaryLedgerName).SetFont(font)).SetTextAlignment(TextAlignment.LEFT).SetFontSize(10));
                        //table.AddCell(new Cell().Add(new Paragraph(row.DebitAmount?.ToString("N2") ?? "0.00").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));
                        //table.AddCell(new Cell().Add(new Paragraph(row.CreditAmount?.ToString("N2") ?? "0.00").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));
                        table.AddCell(new Cell().Add(new Paragraph(row.DebitAmount > 0 ? row.DebitAmount.Value.ToString("N2") : "0").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));

                        table.AddCell(new Cell().Add(new Paragraph(row.CreditAmount > 0 ? row.CreditAmount.Value.ToString("N2") : "0").SetFont(font)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));


                        totalDebit += row.DebitAmount ?? 0;
                        totalCredit += row.CreditAmount ?? 0;
                    }

                    // Total row
                    table.AddCell(new Cell(1, 2).Add(new Paragraph("Total").SetFont(fontBold)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));
                    table.AddCell(new Cell().Add(new Paragraph(totalDebit.ToString("N2")).SetFont(fontBold)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));
                    table.AddCell(new Cell().Add(new Paragraph(totalCredit.ToString("N2")).SetFont(fontBold)).SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10));

                    //In Words and Narration
                    decimal totalAmount = totalDebit;
                    Cell wordsCell = new Cell(1, 4) // colspan = 4 columns
                             .SetBorder(new SolidBorder(0.5f))
                             .SetPadding(5)
                             .SetTextAlignment(TextAlignment.LEFT);

                    // Create Combined Paragraph
                    Paragraph wordsPara = new Paragraph()
                        .Add(new Text("In Words: ").SetFont(fontBold).SetFontSize(10))
                        .Add(new Text($"{NumberToWords((long)totalAmount)} Taka Only").SetFont(font).SetFontSize(10))
                        .Add("\n")
                        .Add(new Text("Narration: ").SetFont(fontBold).SetFontSize(10))
                        .Add(new Text(first.Narration).SetFont(font).SetFontSize(10));

                    wordsCell.Add(wordsPara);
                    wordsCell.SetKeepTogether(true);

                    table.AddCell(wordsCell);

                    document.Add(table);

                    document.Add(new Paragraph("\n \n"));

                    //Signature Block
                    float[] signatureColumnWidths = { 20f, 20f, 20f, 20f, 20f };
                    Table signatureTable = new Table(UnitValue.CreatePercentArray(signatureColumnWidths)).UseAllAvailableWidth();
                    signatureTable.SetMarginTop(30);

                    //Determine the text for Prepared By, based on backend data

                    //string preparedByDisplay = "---------------\nPrepared By";
                    //string ReceivedByDisplay;
                    //if (!string.IsNullOrWhiteSpace(first.ReceivedBy))
                    //{
                    string ReceivedByDisplay = $"{first.ReceivedBy}\n----------------\nReceived By";


                    //string preparedByDisplay;
                    //if (!string.IsNullOrWhiteSpace(first.PreparedBy))
                    //    {
                    string preparedByDisplay = $"{first.PreparedBy}\n---------------\nPrepared By";
                    //}

                    //else
                    //{

                    //    preparedByDisplay = "---------------\nPrepared By";
                    //}
                    //string CheckedByDisplay;
                    //if (!string.IsNullOrWhiteSpace(first.CheckedBy))
                    //{
                    string CheckedByDisplay = $"{first.CheckedBy}\n--------------\nChecked By";
                    //}

                    //else
                    //{

                    //    CheckedByDisplay = "--------------\nChecked By";
                    //}

                    //string VerifiedByDisplay;
                    //if (!string.IsNullOrWhiteSpace(first.VerifiedBy))
                    //{
                    string VerifiedByDisplay = $"{first.VerifiedBy}\n-------------\nVerified By";
                    //}

                    //else
                    //{

                    //    VerifiedByDisplay = "-------------\nVerified By";
                    //}
                    //string AuthorisedDisplay;
                    //if (!string.IsNullOrWhiteSpace(first.AuthorisedSignature))
                    //{
                    string AuthorisedDisplay = $"{first.AuthorisedSignature}\n--------------------------\nAuthorised Signature";
                    //}

                    //else
                    //{

                    //    AuthorisedDisplay = "--------------------------\nAuthorised Signature";
                    //}

                    for (int i = 0; i < 5; i++)
                    {
                        Cell lineCell = new Cell().Add(new Paragraph(" ").SetUnderline().SetMarginBottom(3).SetTextAlignment(TextAlignment.CENTER)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(0);

                        if (i > 0)
                        {
                            lineCell.SetPaddingLeft(10);
                        }
                        signatureTable.AddCell(lineCell);
                    }

                    //signatureTable.AddCell(CreateSignatureTitleCell("----------------\nReceived By", fontBold));
                    signatureTable.AddCell(CreateSignatureTitleCell(ReceivedByDisplay, fontBold));
                    signatureTable.AddCell(CreateSignatureTitleCell(preparedByDisplay, fontBold));
                    //signatureTable.AddCell(CreateSignatureTitleCell("--------------\nChecked By", fontBold));
                    signatureTable.AddCell(CreateSignatureTitleCell(CheckedByDisplay, fontBold));
                    //signatureTable.AddCell(CreateSignatureTitleCell("-------------\nVerified By", fontBold));
                    signatureTable.AddCell(CreateSignatureTitleCell(VerifiedByDisplay, fontBold));
                    //signatureTable.AddCell(CreateSignatureTitleCell("--------------------------\nAuthorised Signature", fontBold));
                    signatureTable.AddCell(CreateSignatureTitleCell(AuthorisedDisplay, fontBold));


                    document.Add(signatureTable);




                    // Page break for multiple vouchers
                    if (voucherGroup.Key != grouped.Last().Key)
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
                //footerHandler.WriteTotalPages(pdf);
                document.Close();

                return ms.ToArray();
            }
        }

        #endregion


        #region Signature Cell Maintain
        private static Cell CreateSignatureTitleCell(string text, PdfFont fontBold)
        {
            return new Cell().Add(new Paragraph(text).SetFont(fontBold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER)).SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(0).SetVerticalAlignment(VerticalAlignment.MIDDLE);
        }

        #endregion


        #region TK(Number) to Convert Text

        public static string NumberToWords(long number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
                return "Minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += NumberToWords(number / 10000000) + " Crore ";
                number %= 10000000;
            }
            if ((number / 100000) > 0)
            {
                words += NumberToWords(number / 100000) + " Lakh ";
                number %= 100000;
            }
            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }
            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }
            if (number > 0)
            {
                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
                               "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }

        #endregion

        #endregion
    }
}
