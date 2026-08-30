using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels.AddSalesCustomer;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.CustomerRelationshipManagement.AddContactPerson;
using GCTL.Service.CustomerRelationshipManagement.AddSalesCustomer;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Helpers;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using iTextAlignment = iText.Layout.Properties.TextAlignment;  // PDF alignment
using iTextParagraph = iText.Layout.Element.Paragraph;  // PDF Paragraph alias
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using PdfDoc = iText.Layout.Document;
using Table = iText.Layout.Element.Table;
using WordDocu = DocumentFormat.OpenXml.Wordprocessing;

namespace GCTL.Controllers.CustomerRelationshipManagement
{
    public class SalesCustomerController : BaseController
    {
        #region
        private readonly ISalesCustomerService _salesCustomerService;
        private readonly IContactPersonService _contactPersonService;
        private readonly IRepository<SalesContactPerson> contactPersonrepository;
        private readonly IRepository<CaDefCountry> caDefCountryRepository;

        public SalesCustomerController(
            ISalesCustomerService salesCustomerService,
            IContactPersonService contactPersonService,
            IRepository<SalesContactPerson> contactPersonrepository,
            IRepository<CaDefCountry> caDefCountryRepository


            ) : base()
        {
            _salesCustomerService = salesCustomerService;
            _contactPersonService = contactPersonService;
            this.contactPersonrepository = contactPersonrepository;
            this.caDefCountryRepository = caDefCountryRepository;
        }

        #endregion

        #region Index & GetTableData
        public async Task<IActionResult> Index()
        {
            var hasPermission = await _salesCustomerService.PagePermissionAsync(LoginInfo.AccessCode);
            if (!hasPermission)
            {
                return RedirectToAction("Login", "Accounts");
            }

            ViewBag.ContactDD = new SelectList(contactPersonrepository.All(), "Cpid", "ContactPersonName");
            ViewBag.CountryDD = new SelectList(caDefCountryRepository.All(), "CountryId", "CountryName");
            SalesCustomerViewModel model = new SalesCustomerViewModel();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GetAll(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "CustomerId",
            string sortOrder = "desc")
        {
            try
            {
                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortColumn = sortColumn,
                    SortOrder = sortOrder
                };

                var result = await _salesCustomerService.GetPaginatedAsync(request);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        draw,
                        data = new List<SalesCustomerViewModel>(),
                        recordsTotal = 0,
                        recordsFiltered = 0
                    });
                }

                return Ok(new
                {
                    draw,
                    data = result.Data,
                    recordsTotal = result.TotalCount,
                    recordsFiltered = result.FilteredCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> Details(string id)
        {
            try
            {
                var customer = await _salesCustomerService.GetByIdAsync(id);

                if (customer == null)
                    return NotFound(new { message = $"Customer with ID {id} not found." });

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(SalesCustomerViewModel model)
        {
            try
            {
                var hasPermission = await _salesCustomerService.SavePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { isSuccess = false, message = "You have no permission to Save." });
                }

                if (model == null)
                    return BadRequest(new { message = "Customer data is required." });

                var result = await _salesCustomerService.SaveAsyncSalesCustomer(model);

                if (!result)
                    return BadRequest(new { message = "Failed to create customer." });

                return Json(new { isSuccess = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Edit(string id, [FromForm] SalesCustomerViewModel model)
        {
            try
            {
                var hasPermission = await _salesCustomerService.UpdatePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { isSuccess = false, message = "You have no permission to Update." });
                }

                if (model == null || id != model.CustomerId)
                    return BadRequest(new { message = "Customer data is invalid." });

                var customerExists = await _salesCustomerService.GetByIdAsync(id);
                if (customerExists == null)
                    return NotFound(new { message = $"Customer with ID {id} not found." });

                var result = await _salesCustomerService.UpdateAsyncSalesCustomer(model);

                if (!result)
                    return BadRequest(new { message = "Failed to update customer." });

                return Ok(new { message = "Data Updated Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] List<string> ids)
        {
            try
            {
                var hasPermission = await _salesCustomerService.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { isSuccess = false, message = "You have no permission to Delete." });
                }

                if (ids == null || !ids.Any())
                {
                    return Json(new { isSuccess = false, message = "No customer selected to delete." });
                }

                var historyModel = new DeleteHistoryViewModel();
                historyModel.ToAudit(LoginInfo);
                historyModel.CompanyCode = LoginInfo.CompanyCode;

                var result = await _salesCustomerService.DeleteTab(ids, historyModel);
                return Json(new { isSuccess = result.succses, message = result.messege });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        [HttpDelete("/SalesCustomer/Delete/{id}")]
        public async Task<IActionResult> DeleteSingle(string id)
        {
            try
            {
                var hasPermission = await _salesCustomerService.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { isSuccess = false, message = "You have no permission to Delete." });
                }

                var historyModel = new DeleteHistoryViewModel();
                historyModel.ToAudit(LoginInfo);
                historyModel.CompanyCode = LoginInfo.CompanyCode;

                var result = await _salesCustomerService.DeleteTab(new List<string> { id }, historyModel);
                return Json(new { isSuccess = result.succses, message = result.messege });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        public async Task<ActionResult> BulkDelete(List<string> ids)
        {
            try
            {
                var hasPermission = await _salesCustomerService.DeletePermissionAsync(LoginInfo.AccessCode);
                if (!hasPermission)
                {
                    return Json(new { isSuccess = false, message = "You have no permission to Delete." });
                }

                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No customer is selected to delete" });
                }

                var historyModel = new DeleteHistoryViewModel();
                historyModel.ToAudit(LoginInfo);
                historyModel.CompanyCode = LoginInfo.CompanyCode;

                var result = await _salesCustomerService.DeleteTab(ids, historyModel);
                return Json(new { isSuccess = result.succses, message = result.messege });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        public async Task<string> GenerateNewCustomerIdAsync()
        {
            var lastCustomerId = await _salesCustomerService.GenerateCustomerIdAsync();

            string newCustomerId;
            if (!string.IsNullOrEmpty(lastCustomerId))
            {
                try
                {
                    // Extract numeric part of the last customer ID
                    var numericPart = int.Parse(lastCustomerId.Substring(3));

                    // Increment the numeric part
                    var incrementedNumber = numericPart + 1;

                    // Generate the new ID
                    newCustomerId = $"CUS{incrementedNumber:D6}";
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Invalid CustomerId format.");
                }
            }
            else
            {
                // If no customers exist, start from CUS00001
                newCustomerId = "CUS000001";
            }

            return newCustomerId;
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomers()
        {
            var customers = await _salesCustomerService.GetAllAsync();

            var filteredCustomers = customers
                .Select(c => new { c.CustomerId, c.CustomerName })
                .ToList();

            return Ok(filteredCustomers);
        }

        //Contact Person Dropdown
        [HttpGet]
        public async Task<IActionResult> GetAllContactPerson()
        {
            var list = await _contactPersonService.DropdownContact();
            return Ok(list);
        }
        //Country Dropdown
        [HttpGet]
        public async Task<IActionResult> GetAllCountryDropdown()
        {
            var list = await _salesCustomerService.GetCountryDropdownAsync();
            return Ok(list);
        }

        #endregion


        #region Customer Report
        public async Task<IActionResult> GetForListReport()
        {
            var customers = await _salesCustomerService.GetCustomerReportAsync();

            return GeneratePdf(customers);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCustomerList(string format)
        {
            var customers = await _salesCustomerService.GetCustomerReportAsync();

            if (format.ToLower() == "pdf")
            {
                return GeneratePdf(customers);
            }
            else if (format.ToLower() == "excel")
            {
                return GenerateExcel(customers);
            }
            else if (format.ToLower() == "word")
            {
                return GenerateWord(customers);
            }
            return BadRequest("Invalid format specified.");
        }


        private IActionResult GeneratePdf(List<CustomerReportVM> customers)
        {
            byte[] pdfBytes;

            using (var memoryStream = new MemoryStream())
            {
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                var document = new PdfDoc(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                document.SetMargins(100, 20, 40, 20);

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                var regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                var blackColor = ColorConstants.BLACK;
                var ashColor = new DeviceRgb(211, 211, 211);

                // Group customers by type
                var generalCustomers = customers.Where(c => c.CustomerType != "1").ToList();
                var inHouseCustomers = customers.Where(c => c.CustomerType == "1").ToList();
                var allGroups = new List<(string Type, List<CustomerReportVM>)> { ("General", generalCustomers) };
                if (inHouseCustomers.Any())
                    allGroups.Add(("In House", inHouseCustomers));

                foreach (var (type, group) in allGroups)
                {
                    if (pdf.GetNumberOfPages() > 0)
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

                    // Customer Type Heading
                    document.Add(new iTextParagraph($"Customer Type: {type}")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetFontColor(blackColor)
                        .SetMarginBottom(5));

                    // Table
                    var table = new Table(new float[] { 60, 150, 150, 70, 80, 120, 650 })
                        .UseAllAvailableWidth()
                        .SetBorder(new SolidBorder(ashColor, 1));

                    string[] headers = { "Customer ID", "Customer", "Address", "Phone", "FAX", "Email", "Contact Person Details" };
                    foreach (var header in headers)
                    {
                        table.AddHeaderCell(new Cell()
                            .Add(new iTextParagraph(header).SetFont(boldFont).SetFontSize(9))
                            .SetTextAlignment(iTextAlignment.CENTER)
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBorder(new SolidBorder(ashColor, 1)));
                    }

                    foreach (var c in group)
                    {
                        table.AddCell(new Cell().Add(new iTextParagraph(c.CustomerID ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));
                        table.AddCell(new Cell().Add(new iTextParagraph(c.CustomerName ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));
                        table.AddCell(new Cell().Add(new iTextParagraph(c.Address ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));
                        table.AddCell(new Cell().Add(new iTextParagraph(c.Phone ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));
                        table.AddCell(new Cell().Add(new iTextParagraph(c.Fax ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));
                        table.AddCell(new Cell().Add(new iTextParagraph(c.Email ?? "").SetFont(regularFont).SetFontSize(9)).SetBorder(new SolidBorder(ashColor, 1)).SetKeepTogether(true));

                        // Contact Person Details (multiline)
                        //table.AddCell(new Cell()
                        //    .Add(new Paragraph(c.ContactPersonDetails ?? "")
                        //    .SetFont(regularFont)
                        //    .SetFontSize(9))
                        //     .SetMultipliedLeading(1.1f))
                        //    .SetBorder(new SolidBorder(ashColor, 1)));

                        string contactDetails = c.ContactPersonDetails ?? "";

                        // Split by line (multiple contacts) and trim
                        var lines = contactDetails
                            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(l => l.Trim())
                            .Where(l => !string.IsNullOrEmpty(l))
                            .ToList();

                        // Join with newline between contacts
                        string finalText = string.Join("\n", lines);

                        table.AddCell(new Cell()
                            .Add(new iTextParagraph(finalText)
                                .SetFont(regularFont)
                                .SetFontSize(9)
                                .SetMultipliedLeading(1.1f))
                            .SetBorder(new SolidBorder(ashColor, 1)));




                    }

                    document.Add(table);
                }

                document.Close();
                pdfBytes = memoryStream.ToArray();
            }

            // Add header & footer on all pages
            var outputStream = new MemoryStream();
            using (var reader = new PdfReader(new MemoryStream(pdfBytes)))
            using (var writer = new PdfWriter(outputStream))
            using (var pdfDoc = new PdfDocument(reader, writer))
            {
                int totalPages = pdfDoc.GetNumberOfPages();

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                var font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                var blackColor = ColorConstants.BLACK;

                for (int i = 1; i <= totalPages; i++)
                {
                    var page = pdfDoc.GetPage(i);
                    var pageSize = page.GetPageSize();
                    var canvas = new PdfCanvas(page);

                    float pageWidth = pageSize.GetWidth();

                    // Company Name
                    string companyName = customers.FirstOrDefault()?.CompanyName ?? "";
                    float companyFontSize = 16;
                    float companyWidth = boldFont.GetWidth(companyName, companyFontSize);
                    canvas.BeginText()
                        .SetFontAndSize(boldFont, companyFontSize)
                        .SetFillColor(blackColor)
                        .MoveText((pageWidth - companyWidth) / 2, pageSize.GetHeight() - 30)
                        .ShowText(companyName)
                        .EndText();

                    // Address
                    string address1 = customers.FirstOrDefault()?.Address1 ?? "";
                    float addressFontSize = 11;
                    float addressWidth = font.GetWidth(address1, addressFontSize);
                    canvas.BeginText()
                        .SetFontAndSize(font, addressFontSize)
                        .MoveText((pageWidth - addressWidth) / 2, pageSize.GetHeight() - 50)
                        .ShowText(address1)
                        .EndText();

                    // Subtitle
                    string reportTitle = "Customer Information Report";
                    float titleFontSize = 14;
                    float titleWidth = boldFont.GetWidth(reportTitle, titleFontSize);
                    float paddingBottom = 5;
                    float titleY = pageSize.GetHeight() - 70 - paddingBottom;

                    canvas.BeginText()
                        .SetFontAndSize(boldFont, titleFontSize)
                        .SetFillColor(blackColor)
                        .MoveText((pageWidth - titleWidth) / 2, titleY)
                        .ShowText(reportTitle)
                        .EndText();

                    // Underline
                    float lineY = titleY - 2;
                    canvas.SetLineWidth(1f)
                          .SetStrokeColor(blackColor)
                          .MoveTo((pageWidth - titleWidth) / 2, lineY)
                          .LineTo((pageWidth + titleWidth) / 2, lineY)
                          .Stroke();

                    // Footer
                    //string printDate = $"Print Date: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    string printDate = $"Print Date: {DateTime.Now:dd/MM/yyyy h:mm tt}";

                    string pageNumber = $"Page {i} of {totalPages}";

                    canvas.BeginText()
                        .SetFontAndSize(font, 9)
                        .MoveText(40, 20)
                        .ShowText(printDate)
                        .EndText();

                    canvas.BeginText()
                        .SetFontAndSize(font, 9)
                        .MoveText(pageSize.GetWidth() - 100, 20)
                        .ShowText(pageNumber)
                        .EndText();
                }
            }

            return File(outputStream.ToArray(), "application/pdf", "CustomerReport.pdf");
        }


        private IActionResult GenerateWord(List<CustomerReportVM> customers)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

                // --- Header ---
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                string headerPartId = mainPart.GetIdOfPart(headerPart);
                var header = new Header();

                void AddHeaderText(string text, int fontSize, bool underline = false, int spacingBefore = 50, int spacingAfter = 100)
                {
                    var runProps = new RunProperties
                    {
                        RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        Bold = new Bold(),
                        FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = (fontSize * 2).ToString() }
                    };
                    runProps.Append(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "000000" });
                    if (underline) runProps.Underline = new DocumentFormat.OpenXml.Wordprocessing.Underline { Val = UnderlineValues.Thick };

                    var run = new Run(runProps);
                    run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(text) { Space = SpaceProcessingModeValues.Preserve });

                    var para = new Paragraph(run)
                    {
                        ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = JustificationValues.Center },
                            new SpacingBetweenLines { Before = spacingBefore.ToString(), After = spacingAfter.ToString() }
                        )
                    };
                    header.Append(para);
                }

                // Add header text
                AddHeaderText(customers.FirstOrDefault()?.CompanyName ?? "", 16, spacingAfter: 50);
                AddHeaderText(customers.FirstOrDefault()?.Address1 ?? "", 14, spacingAfter: 50);
                AddHeaderText("Customer Information Report", 14, underline: true, spacingAfter: 300);

                headerPart.Header = header;
                headerPart.Header.Save();

                // --- Group customers by CustomerType ---
                var groupedCustomers = customers.GroupBy(c => c.CustomerType == "1" ? "In House" : "General");

                // --- Functions to create table rows ---
                TableRow CreateHeaderRow()
                {
                    string[] headers = { "Customer ID", "Customer", "Address", "Phone", "FAX", "Email", "Contact Person Details" };
                    int[] columnWidths = { 2000, 4000, 5000, 2000, 3000, 3000, 8000 }; // widths in twips

                    var headerRow = new TableRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = new TableCell();
                        cell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                            new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = columnWidths[i].ToString() }
                        ));

                        var runProps = new RunProperties
                        {
                            RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                            Bold = new Bold()
                        };

                        var para = new Paragraph(new Run(runProps, new DocumentFormat.OpenXml.Wordprocessing.Text(headers[i]) { Space = SpaceProcessingModeValues.Preserve }))
                        {
                            ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center })
                        };

                        cell.Append(para);
                        headerRow.Append(cell);
                    }
                    headerRow.Append(new TableRowProperties(new TableHeader()));
                    return headerRow;
                }


                TableRow CreateDataRow(CustomerReportVM c)
                {
                    string[] data = { c.CustomerID ?? "", c.CustomerName ?? "", c.Address ?? "", c.Phone ?? "", c.Fax ?? "", c.Email ?? "", c.ContactPersonDetails ?? "" };
                    int[] columnWidths = { 2000, 4000, 5000, 2000, 3000, 3000, 8000 };

                    var row = new TableRow(new TableRowProperties(new CantSplit()));

                    for (int i = 0; i < data.Length; i++)
                    {
                        var cell = new TableCell();
                        cell.Append(new TableCellProperties(
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                            new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = columnWidths[i].ToString() }
                        ));

                        if (i == 6 && !string.IsNullOrEmpty(data[i])) // multiline Contact Person
                        {
                            var lines = data[i].Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var para = new Paragraph(new Run(new RunProperties
                                {
                                    RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "20" }
                                }, new DocumentFormat.OpenXml.Wordprocessing.Text(line) { Space = SpaceProcessingModeValues.Preserve }))
                                {
                                    ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Left })
                                };
                                cell.Append(para);
                            }
                        }
                        else
                        {
                            var para = new Paragraph(new Run(new RunProperties
                            {
                                RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "20" }
                            }, new DocumentFormat.OpenXml.Wordprocessing.Text(data[i]) { Space = SpaceProcessingModeValues.Preserve }))
                            {
                                ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Left })
                            };
                            cell.Append(para);
                        }

                        row.Append(cell);
                    }

                    return row;
                }

                // --- Append grouped tables ---
                foreach (var group in groupedCustomers)
                {
                    // Heading
                    // Heading
                    var headingPara = new Paragraph(
                        new Run(new RunProperties
                        {
                            RunFonts = new RunFonts { Ascii = "Times New Roman" },
                            Bold = new Bold(),
                            FontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "24" }
                        }, new DocumentFormat.OpenXml.Wordprocessing.Text($"Customer Type: {group.Key}")
                        { Space = SpaceProcessingModeValues.Preserve })
                    );

                    headingPara.ParagraphProperties = new ParagraphProperties(
                        new Justification { Val = JustificationValues.Left },
                        new KeepNext(),
                        new KeepLines()
                    );

                    //Force new page before heading if not first group
                    if (group.Key != groupedCustomers.First().Key)
                    {
                        headingPara.ParagraphProperties.Append(new PageBreakBefore());
                    }

                    body.Append(headingPara);


                    // Blank line
                    body.Append(new Paragraph(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(" "))));

                    // Table
                    var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
                    table.Append(new TableProperties(
                        new TableBorders(
                            new TopBorder { Val = BorderValues.Single, Size = 4 },
                            new BottomBorder { Val = BorderValues.Single, Size = 4 },
                            new LeftBorder { Val = BorderValues.Single, Size = 4 },
                            new RightBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                        )
                    ));

                    table.Append(CreateHeaderRow());
                    foreach (var c in group)
                    {
                        table.Append(CreateDataRow(c));
                    }

                    body.Append(table);
                    body.Append(new Paragraph(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(" "))));
                }

                // --- Footer ---
                var footerPart = mainPart.AddNewPart<FooterPart>();
                string footerPartId = mainPart.GetIdOfPart(footerPart);

                var footer = new Footer();
                var footerPara = new Paragraph();

                footerPara.ParagraphProperties = new ParagraphProperties(
                    new Tabs(
                        new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Right, Position = 14500 }
                    )
                );

                // Left: print date
                var leftRun = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(DateTime.Now.ToString("dd/MM/yyyy h:mm tt")));
                footerPara.Append(leftRun);

                // Tab to move to the right
                var tab = new Run(new TabChar());
                footerPara.Append(tab);

                // Right: Page X of Y
                // "Page "
                var pageTextRun = new Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text("Page ")
                    { Space = SpaceProcessingModeValues.Preserve }
                );

                // PAGE field
                var pageNumberFieldRun = new Run();
                pageNumberFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
                pageNumberFieldRun.Append(new FieldCode(" PAGE "));
                pageNumberFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
                pageNumberFieldRun.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("1") { Space = SpaceProcessingModeValues.Preserve });
                pageNumberFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.End });

                // " Of " with spaces preserved
                var ofTextRun = new Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text(" Of ")
                    { Space = SpaceProcessingModeValues.Preserve }
                );

                // NUMPAGES field
                var numPagesFieldRun = new Run();
                numPagesFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
                numPagesFieldRun.Append(new FieldCode(" NUMPAGES "));
                numPagesFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
                numPagesFieldRun.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("1") { Space = SpaceProcessingModeValues.Preserve });
                numPagesFieldRun.Append(new FieldChar { FieldCharType = FieldCharValues.End });


                footerPara.Append(pageTextRun);
                footerPara.Append(pageNumberFieldRun);
                footerPara.Append(ofTextRun);
                footerPara.Append(numPagesFieldRun);

                footer.Append(footerPara);
                footerPart.Footer = footer;
                footerPart.Footer.Save();


                // --- Section properties ---
                var sectionProps = new SectionProperties(
                    new DocumentFormat.OpenXml.Wordprocessing.PageSize
                    {
                        Width = 15840,
                        Height = 12240,
                        Orient = PageOrientationValues.Landscape
                    },
                    new PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720 },
                    new HeaderReference() { Type = HeaderFooterValues.Default, Id = headerPartId },
                    new FooterReference() { Type = HeaderFooterValues.Default, Id = footerPartId }

                );
                body.AppendChild(sectionProps);

                mainPart.Document.Save();
            }

            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "CustomerReport.docx");
        }


        private IActionResult GenerateExcel(List<CustomerReportVM> customers)
        {
            // Order customers
            customers = customers.OrderBy(c => c.CustomerID).ToList();

            // Set CustomerType text
            foreach (var customer in customers)
                customer.CustomerType = customer.CustomerType == "1" ? "In House" : "General";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customer List");

            // Header info
            var companyName = customers.FirstOrDefault()?.CompanyName ?? " ";
            var address1 = customers.FirstOrDefault()?.Address1 ?? "";

            worksheet.Cell(1, 1).Value = companyName;
            worksheet.Range("A1:H1").Merge();
            worksheet.Range("A1:H1").Style.Font.Bold = true;
            worksheet.Range("A1:H1").Style.Font.FontSize = 16;
            worksheet.Range("A1:H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = address1;
            worksheet.Range("A2:H2").Merge();
            worksheet.Range("A2:H2").Style.Font.Bold = true;
            worksheet.Range("A2:H2").Style.Font.FontSize = 14;
            worksheet.Range("A2:H2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(3, 1).Value = "Customer Information Report";
            worksheet.Range("A3:H3").Merge();
            worksheet.Range("A3:H3").Style.Font.Bold = true;
            worksheet.Range("A3:H3").Style.Font.FontSize = 14;
            worksheet.Range("A3:H3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Column headers
            string[] headers = { "Customer ID", "Customer", "Address", "Phone", "FAX", "Email", "Contact Person Details", "Customer Type" };
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(5, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Fill data
            for (int i = 0; i < customers.Count; i++)
            {
                int row = i + 6;
                var c = customers[i];

                // Set cell values
                worksheet.Cell(row, 1).Value = c.CustomerID;
                worksheet.Cell(row, 2).Value = c.CustomerName;
                worksheet.Cell(row, 3).Value = c.Address;
                worksheet.Cell(row, 4).Value = c.Phone;
                worksheet.Cell(row, 5).Value = c.Fax;
                worksheet.Cell(row, 6).Value = c.Email;

                string contactDetails = c.ContactPersonDetails ?? "";
                var lines = contactDetails.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(l => l.Trim())
                                           .Where(l => !string.IsNullOrEmpty(l))
                                           .ToList();
                string finalText = string.Join("\n", lines);
                worksheet.Cell(row, 7).Value = finalText;

                worksheet.Cell(row, 8).Value = c.CustomerType;

                // Style each cell
                for (int col = 1; col <= 8; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    cell.Style.Alignment.WrapText = true;                  // Wrap text
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top; // Vertical top
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    // Horizontal alignment
                    if (col == 1 || col == 8)
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    else
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                // Adjust row height automatically based on content
                worksheet.Row(row).AdjustToContents();
            }



            // Set column widths
            worksheet.Column(1).Width = 15; // ID
            worksheet.Column(2).Width = 40; // Customer Name
            worksheet.Column(3).Width = 40; // Address
            worksheet.Column(4).Width = 15; // Phone
            worksheet.Column(5).Width = 15; // FAX
            worksheet.Column(6).Width = 25; // Email
            worksheet.Column(7).Width = 70; // Contact Person Details
            worksheet.Column(8).Width = 15; // Customer Type

            // Freeze header row
            worksheet.SheetView.FreezeRows(5);

            // Save to MemoryStream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"CustomerReport.xlsx";
            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

        public async Task<IActionResult> Preview(string viewType = "profile")
        {
            ViewBag.ViewType = viewType;
            return View();
        }

        public async Task<IActionResult> DownloadCustomerProfile(string format)
        {
            var allCustomers = await _salesCustomerService.GetCustomerDeliveryReportAsync();

            //Set CustomerType
            foreach (var customer in allCustomers)
            {
                if (string.IsNullOrEmpty(customer.CustomerType) || customer.CustomerType != "1")
                    customer.CustomerType = "General";
                else if (customer.CustomerType == "1")
                    customer.CustomerType = "In House";
            }

            if (format.ToLower() == "pdf")
                return GenerateDetailedPdf(allCustomers);
            else if (format.ToLower() == "excel")
                return GenerateDetailedExcel(allCustomers);
            else if (format.ToLower() == "word")
                return GenerateDetailedWord(allCustomers);

            return BadRequest("Invalid format specified.");
        }

        public async Task<IActionResult> GeneratePreviewPdf()
        {
            var allCustomer = await _salesCustomerService.GetCustomerDeliveryReportAsync();


            foreach (var customer in allCustomer)
            {
                if (string.IsNullOrEmpty(customer.CustomerType) || customer.CustomerType != "1")
                {
                    customer.CustomerType = "General";
                    continue;
                }
                if (customer.CustomerType == "1")
                {
                    customer.CustomerType = "In House";
                }
            }

            // Generate PDF (preview mode)
            var result = GenerateDetailedPdf(allCustomer, true);


            return result;
        }

        private IActionResult GenerateDetailedPdf(List<CustomerDeliveryReportVM> customers, bool isPreview = false)
        {
            byte[] pdfBytes;

            //Generate PDF content normally
            using (var memoryStream = new MemoryStream())
            {
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);
                document.SetMargins(30, 20, 30, 20);

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                var regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                float[] columnWidths = { 120f, 10f, 400f };

                int customerCount = 0;

                foreach (var customer in customers)
                {
                    customerCount++;

                    document.Add(new iText.Layout.Element.Paragraph(customer.CompanyName)
                        .SetFont(boldFont).SetFontSize(16)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK)
                        .SetMarginBottom(1));

                    document.Add(new iText.Layout.Element.Paragraph(customer.CompanyAddress)
                        .SetFont(regularFont).SetFontSize(12)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK)
                        .SetMarginBottom(1));

                    var header = new iText.Layout.Element.Paragraph("Customer Profile")
                        .SetFont(boldFont).SetFontSize(14)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontColor(iText.Kernel.Colors.ColorConstants.BLACK)
                        .SetUnderline()
                        .SetMarginBottom(15);
                    document.Add(header);

                    var customerTable = new Table(columnWidths).UseAllAvailableWidth();

                    var customerHeader = new iText.Layout.Element.Paragraph()
                                    .Add(new iText.Layout.Element.Text("Customer Information")
                                        .SetFont(boldFont)
                                        .SetFontSize(12)
                                        .SetUnderline(1f, -2f)
                                        .SetFontColor(ColorConstants.BLACK)
                                    )
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT)
                                    .SetMarginTop(15)
                                    .SetMarginBottom(5);

                    document.Add(customerHeader);

                    AddFormattedRow(customerTable, "Customer ID", customer.CustomerID ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "Customer", customer.CustomerName ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "Address", customer.CustomerAddress ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "Phone", customer.CustomerPhone ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "Email", customer.CustomerEmail ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "FAX", customer.FAX ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "URL", customer.URL ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "BIN", customer.BIN ?? "", boldFont, regularFont);
                    AddFormattedRow(customerTable, "Type", customer.CustomerType ?? "", boldFont, regularFont);

                    document.Add(customerTable);


                    var deliveryHeader = new iText.Layout.Element.Paragraph()
                               .Add(new iText.Layout.Element.Text("Delivery Address Details")
                                   .SetFont(boldFont)
                                   .SetFontSize(12)
                                   .SetUnderline(1f, -2f)
                                   .SetFontColor(ColorConstants.BLACK)
                               )
                               .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT)
                               .SetMarginTop(15)
                               .SetMarginBottom(5);

                    document.Add(deliveryHeader);

                    // Delivery Table
                    float[] deliveryCols = { 200f, 120f, 100f, 100f, 120f };
                    var deliveryTable = new Table(deliveryCols).UseAllAvailableWidth();

                    //Border Color
                    //deliveryTable.SetBorder(new SolidBorder(iText.Kernel.Colors.ColorConstants.BLACK, 1));


                    // Table headers
                    string[] headers = { "Delivery Address", "Contact Person", "Designation", "Phone", "Email" };
                    foreach (var h in headers)
                    {
                        deliveryTable.AddHeaderCell(
                            new Cell()
                                .Add(new iText.Layout.Element.Paragraph(h).SetFont(boldFont).SetFontSize(10))
                                .SetBackgroundColor(iText.Kernel.Colors.ColorConstants.WHITE)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        );
                    }

                    // Table rows
                    foreach (var delivery in customer.DeliveryDetails)
                    {
                        int contactCount = delivery.ContactPersons.Count;
                        int rowSpan = contactCount > 0 ? contactCount : 1;

                        // Delivery Address cell (merged if multiple contact persons)
                        var deliveryCell = new Cell(rowSpan, 1)
                            .Add(new iText.Layout.Element.Paragraph(delivery.DeliveryAddress ?? "")
                                .SetFont(regularFont)
                                .SetFontSize(9))
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE); // vertical middle
                        deliveryTable.AddCell(deliveryCell);

                        if (contactCount == 0)
                        {
                            // Fill remaining columns with empty cell if no contact person
                            deliveryTable.AddCell(new Cell(1, 4).Add(new iText.Layout.Element.Paragraph("")).SetFont(regularFont).SetFontSize(9));
                        }
                        else
                        {
                            // Add contact persons
                            foreach (var cp in delivery.ContactPersons)
                            {
                                deliveryTable.AddCell(new iText.Layout.Element.Paragraph(cp.Name ?? "").SetFont(regularFont).SetFontSize(9));
                                deliveryTable.AddCell(new iText.Layout.Element.Paragraph(cp.Designation ?? "").SetFont(regularFont).SetFontSize(9));
                                deliveryTable.AddCell(new iText.Layout.Element.Paragraph(cp.Phone ?? "").SetFont(regularFont).SetFontSize(9));
                                deliveryTable.AddCell(new iText.Layout.Element.Paragraph(cp.Email ?? "").SetFont(regularFont).SetFontSize(9));
                            }
                        }
                    }

                    document.Add(deliveryTable);


                    if (customerCount < customers.Count)
                    {
                        document.Add(new iText.Layout.Element.AreaBreak());
                    }
                }

                document.Close();
                pdfBytes = memoryStream.ToArray();
            }

            //add footer
            using (var reader = new PdfReader(new MemoryStream(pdfBytes)))
            using (var finalStream = new MemoryStream())
            using (var writer = new PdfWriter(finalStream))
            using (var pdfDoc = new PdfDocument(reader, writer))
            {
                int totalPages = pdfDoc.GetNumberOfPages();
                var font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                float margin = 20;

                for (int i = 1; i <= totalPages; i++)
                {
                    var page = pdfDoc.GetPage(i);
                    var pageSize = page.GetPageSize();
                    var canvas = new PdfCanvas(page);

                    string printDate = $"Print Date: {DateTime.Now:dd/MM/yyyy h:mm tt}";

                    canvas.BeginText()
                        .SetFontAndSize(font, 9)
                        .MoveText(margin, margin)
                        .ShowText(printDate)
                        .EndText();

                    string pageInfo = $"Page {i} of {totalPages}";
                    canvas.BeginText()
                        .SetFontAndSize(font, 9)
                        .MoveText(pageSize.GetWidth() - margin - font.GetWidth(pageInfo, 9), margin)
                        .ShowText(pageInfo)
                        .EndText();
                }

                pdfDoc.Close();
                pdfBytes = finalStream.ToArray();
            }

            return File(pdfBytes, "application/pdf", isPreview ? null : "CustomerProfile.pdf");
        }

        private void AddFormattedRow(Table table, string label, string value, PdfFont boldFont, PdfFont regularFont)
        {
            table.AddCell(new Cell()
                .Add(new iText.Layout.Element.Paragraph(label).SetFont(boldFont).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT));

            table.AddCell(new Cell()
                .Add(new iText.Layout.Element.Paragraph(":").SetFont(boldFont).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT));

            table.AddCell(new Cell()
                .Add(new iText.Layout.Element.Paragraph(value).SetFont(regularFont).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT));
        }

        private IActionResult GenerateDetailedExcel(List<CustomerDeliveryReportVM> customers)
        {
            using var package = new OfficeOpenXml.ExcelPackage();

            var workbook = package.Workbook;
            var sheet = workbook.Worksheets.Add("Customer Report");

            sheet.DefaultColWidth = 30; // wider for long addresses
            int row = 1;

            var companyName = customers.FirstOrDefault()?.CompanyName ?? " ";
            var address = customers.FirstOrDefault()?.CompanyAddress ?? "";

            // === Top header (only once) ===
            sheet.Cells[row, 1].Value = companyName;
            sheet.Cells[row, 1, row, 6].Merge = true;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 16;
            sheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            row++;

            sheet.Cells[row, 1].Value = address;
            sheet.Cells[row, 1, row, 6].Merge = true;
            sheet.Cells[row, 1].Style.Font.Size = 12;
            sheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            row++;

            sheet.Cells[row, 1].Value = "Customer Profile";
            sheet.Cells[row, 1, row, 6].Merge = true;
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 14;
            sheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            row += 2;

            foreach (var customer in customers)
            {
                // === Customer Information Section ===
                // The header row is now just bold text, not a merged cell
                sheet.Cells[row, 1].Value = "Customer Information";
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 1].Style.Font.Size = 12;
                row += 2; // Add a space below the title


                void AddCustomerInfoRow(string label, string value)
                {
                    // Column 1 → Label only
                    var labelCell = sheet.Cells[row, 1];
                    labelCell.Value = label;
                    labelCell.Style.Font.Bold = true;
                    labelCell.Style.Font.Size = 11;
                    labelCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    labelCell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Column 2 → Colon
                    sheet.Column(2).Width = 2; // small width
                    var colonCell = sheet.Cells[row, 2];
                    colonCell.Value = ":";
                    colonCell.Style.Font.Bold = true;
                    colonCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    colonCell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Column 3 → 6 → Value
                    var valueCell = sheet.Cells[row, 3, row, 6];
                    valueCell.Merge = true;
                    valueCell.Value = value ?? "";
                    valueCell.Style.Font.Bold = false;
                    valueCell.Style.Font.Size = 11;
                    valueCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                    valueCell.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                    // Remove borders for clean look
                    sheet.Cells[row, 1, row, 6].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    sheet.Cells[row, 1, row, 6].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    sheet.Cells[row, 1, row, 6].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                    sheet.Cells[row, 1, row, 6].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;

                    // Hide gridlines
                    sheet.View.ShowGridLines = false;

                    row++;
                }



                AddCustomerInfoRow("Customer ID", customer.CustomerID ?? "");
                AddCustomerInfoRow("Customer", customer.CustomerName ?? "");
                AddCustomerInfoRow("Address", customer.CustomerAddress ?? "");
                AddCustomerInfoRow("Phone", customer.CustomerPhone ?? "");
                AddCustomerInfoRow("Email", customer.CustomerEmail ?? "");
                AddCustomerInfoRow("FAX", customer.FAX ?? "");
                AddCustomerInfoRow("URL", customer.URL ?? "");
                AddCustomerInfoRow("BIN", customer.BIN ?? "");
                AddCustomerInfoRow("Type", customer.CustomerType ?? "");

                row++; // space before Delivery section

                // === Delivery Address Details Section ===
                sheet.Cells[row, 1].Value = "Delivery Address Details";
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 1].Style.Font.Size = 12;
                sheet.Cells[row, 1].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.None;
                row++;

                // Delivery table header
                var deliveryHeaders = new string[] { "Delivery Address", "Contact Person", "Designation", "Phone", "Email" };
                for (int col = 1; col <= deliveryHeaders.Length; col++)
                {
                    sheet.Cells[row, col].Value = deliveryHeaders[col - 1];
                    sheet.Cells[row, col].Style.Font.Bold = true;
                    sheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    sheet.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }
                row++;

                // Delivery table body
                foreach (var delivery in customer.DeliveryDetails)
                {
                    int contactCount = delivery.ContactPersons.Count;
                    if (contactCount == 0)
                    {
                        // No contact persons, just show the delivery address
                        sheet.Cells[row, 1].Value = delivery.DeliveryAddress ?? "";
                        sheet.Cells[row, 1, row, 5].Merge = true; // merge across all columns
                        for (int col = 1; col <= 5; col++)
                            sheet.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                        row++;
                    }
                    else
                    {
                        // Merge delivery address vertically across contact persons
                        sheet.Cells[row, 1, row + contactCount - 1, 1].Merge = true;
                        sheet.Cells[row, 1].Value = delivery.DeliveryAddress ?? "";
                        sheet.Cells[row, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        sheet.Cells[row, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                        // Fill contact persons
                        foreach (var cp in delivery.ContactPersons)
                        {
                            sheet.Cells[row, 2].Value = cp.Name ?? "";
                            sheet.Cells[row, 3].Value = cp.Designation ?? "";
                            sheet.Cells[row, 4].Value = cp.Phone ?? "";
                            sheet.Cells[row, 5].Value = cp.Email ?? "";

                            for (int col = 2; col <= 5; col++)
                                sheet.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                            row++;
                        }
                    }
                }

                row += 2; // space before next customer
            }

            // Auto-fit columns
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CustomerProfile.xlsx");
        }

        private IActionResult GenerateDetailedWord(List<CustomerDeliveryReportVM> customers)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                stream,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

                // --- Header Setup ---
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                string headerPartId = mainPart.GetIdOfPart(headerPart);
                var header = new Header();

                void AddHeaderLine(string text, int fontSize, bool bold = true, bool center = true, bool underline = false)
                {
                    var runProps = new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        bold ? new Bold() : null,
                        new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = (fontSize * 2).ToString() }
                    );

                    if (underline)
                        runProps.Append(new DocumentFormat.OpenXml.Wordprocessing.Underline { Val = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues.Thick });

                    var run = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)) { RunProperties = runProps };
                    var para = new Paragraph(run)
                    {
                        ParagraphProperties = new ParagraphProperties(
                            new Justification { Val = center ? JustificationValues.Center : JustificationValues.Left },
                            new SpacingBetweenLines { After = "100" }
                        )
                    };
                    header.Append(para);
                }

                if (customers.Count > 0)
                {
                    var c = customers[0];
                    AddHeaderLine(c.CompanyName, 16);
                    AddHeaderLine(c.CompanyAddress, 12);
                    AddHeaderLine("Customer Profile", 14, true, true, true);
                }

                headerPart.Header = header;
                headerPart.Header.Save();

                // --- Footer Setup ---
                var footerPart = mainPart.AddNewPart<FooterPart>();
                string footerPartId = mainPart.GetIdOfPart(footerPart);
                var footer = new Footer();

                var footerPara = new Paragraph();
                footerPara.ParagraphProperties = new ParagraphProperties(
                    new Tabs(new DocumentFormat.OpenXml.Wordprocessing.TabStop { Val = TabStopValues.Right, Position = 9500 })
                );

                // Left: Date/Time
                footerPara.Append(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(DateTime.Now.ToString("dd/MM/yyyy h:mm tt")) { Space = SpaceProcessingModeValues.Preserve }));

                // Tab to right
                footerPara.Append(new Run(new TabChar()));

                // Right: Page X of Y
                footerPara.Append(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Page ") { Space = SpaceProcessingModeValues.Preserve }));

                // PAGE field
                var pageField = new Run();
                pageField.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
                pageField.Append(new FieldCode(" PAGE "));
                pageField.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
                pageField.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("1") { Space = SpaceProcessingModeValues.Preserve });
                pageField.Append(new FieldChar { FieldCharType = FieldCharValues.End });
                footerPara.Append(pageField);

                // " of "
                footerPara.Append(new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(" of ") { Space = SpaceProcessingModeValues.Preserve }));

                // NUMPAGES field
                var numPagesField = new Run();
                numPagesField.Append(new FieldChar { FieldCharType = FieldCharValues.Begin });
                numPagesField.Append(new FieldCode(" NUMPAGES "));
                numPagesField.Append(new FieldChar { FieldCharType = FieldCharValues.Separate });
                numPagesField.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("1") { Space = SpaceProcessingModeValues.Preserve });
                numPagesField.Append(new FieldChar { FieldCharType = FieldCharValues.End });
                footerPara.Append(numPagesField);

                footer.Append(footerPara);
                footerPart.Footer = footer;
                footerPart.Footer.Save();


                // --- Helper Methods for Body ---
                void AddHeading(string text, int fontSize, bool underline = false)
                {
                    var runProps = new WordDocu.RunProperties(
                        new WordDocu.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new WordDocu.Bold(),
                        new WordDocu.FontSize() { Val = (fontSize * 2).ToString() }
                    );
                    if (underline) runProps.Append(new WordDocu.Underline() { Val = WordDocu.UnderlineValues.Thick });

                    var run = new WordDocu.Run(new WordDocu.Text(text)) { RunProperties = runProps };
                    var para = new WordDocu.Paragraph(run)
                    {
                        ParagraphProperties = new WordDocu.ParagraphProperties(
                            new WordDocu.Justification() { Val = WordDocu.JustificationValues.Center },
                            new WordDocu.SpacingBetweenLines() { After = "200" }
                        )
                    };
                    body.AppendChild(para);
                }

                void AddSectionTitle(string title, bool underline = false)
                {
                    var runProps = new WordDocu.RunProperties(
                        new WordDocu.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new WordDocu.Bold(),
                        new WordDocu.FontSize() { Val = "28" }
                    );
                    if (underline) runProps.Append(new WordDocu.Underline() { Val = WordDocu.UnderlineValues.Thick });

                    var run = new WordDocu.Run(new WordDocu.Text(title)) { RunProperties = runProps };
                    var para = new WordDocu.Paragraph(run)
                    {
                        ParagraphProperties = new WordDocu.ParagraphProperties(
                            new WordDocu.SpacingBetweenLines() { After = "120" }
                        )
                    };
                    body.AppendChild(para);
                }

                void AddInfoLine(string label, string value)
                {
                    var para = new WordDocu.Paragraph();
                    para.ParagraphProperties = new WordDocu.ParagraphProperties(
                        new WordDocu.Tabs(
                            new WordDocu.TabStop { Val = WordDocu.TabStopValues.Left, Position = 3000 },
                            new WordDocu.TabStop { Val = WordDocu.TabStopValues.Left, Position = 3200 }
                        ),
                        new WordDocu.SpacingBetweenLines { After = "120" },
                        new WordDocu.Justification() { Val = WordDocu.JustificationValues.Left },
                        new WordDocu.Indentation { Left = "3200", Hanging = "3200" }
                    );

                    var labelRun = new WordDocu.Run(
                        new WordDocu.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new WordDocu.RunProperties(new WordDocu.Bold(), new WordDocu.FontSize { Val = "22" }),
                        new WordDocu.Text(label)
                    );

                    para.Append(labelRun, new WordDocu.Run(new WordDocu.TabChar()),
                                new WordDocu.Run(new WordDocu.Text(":") { Space = SpaceProcessingModeValues.Preserve }),
                                new WordDocu.Run(new WordDocu.TabChar()),
                                new WordDocu.Run(new WordDocu.RunProperties(new WordDocu.FontSize { Val = "22" }),
                                                 new WordDocu.Text(value ?? "")));

                    body.Append(para);
                }

                WordDocu.Table CreateTable()
                {
                    var table = new WordDocu.Table();
                    var tblProps = new WordDocu.TableProperties(
                        new WordDocu.TableLayout() { Type = WordDocu.TableLayoutValues.Fixed },
                        new WordDocu.TableBorders(
                            new WordDocu.TopBorder { Val = WordDocu.BorderValues.Single, Size = 4 },
                            new WordDocu.BottomBorder { Val = WordDocu.BorderValues.Single, Size = 4 },
                            new WordDocu.LeftBorder { Val = WordDocu.BorderValues.Single, Size = 4 },
                            new WordDocu.RightBorder { Val = WordDocu.BorderValues.Single, Size = 4 },
                            new WordDocu.InsideHorizontalBorder { Val = WordDocu.BorderValues.Single, Size = 4 },
                            new WordDocu.InsideVerticalBorder { Val = WordDocu.BorderValues.Single, Size = 4 }
                        )
                    );
                    table.AppendChild(tblProps);
                    return table;
                }

                // --- Body Content ---
                for (int i = 0; i < customers.Count; i++)
                {
                    var customer = customers[i];

                    AddSectionTitle("Customer Information", true);
                    AddInfoLine("Customer ID", customer.CustomerID);
                    AddInfoLine("Customer", customer.CustomerName);
                    AddInfoLine("Address", customer.CustomerAddress);
                    AddInfoLine("Phone", customer.CustomerPhone);
                    AddInfoLine("Email", customer.CustomerEmail);
                    AddInfoLine("FAX", customer.FAX);
                    AddInfoLine("URL", customer.URL);
                    AddInfoLine("BIN", customer.BIN);
                    AddInfoLine("Type", customer.CustomerType);

                    body.AppendChild(new WordDocu.Paragraph(new WordDocu.Run(new WordDocu.Text(""))));

                    // Delivery Table
                    AddSectionTitle("Delivery Address Details", true);
                    var deliveryTable = CreateTable();
                    string[] headers = { "Delivery Address", "Contact Person", "Designation", "Phone", "Email" };
                    int[] colWidths = { 3000, 2000, 2000, 1500, 1500 };

                    var headerRow = new WordDocu.TableRow();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        var cell = new WordDocu.TableCell(
                            new WordDocu.TableCellProperties(
                                new WordDocu.TableCellWidth { Type = WordDocu.TableWidthUnitValues.Dxa, Width = colWidths[j].ToString() }
                            ),
                            new WordDocu.Paragraph(
                                new WordDocu.Run(
                                    new WordDocu.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new WordDocu.RunProperties(new WordDocu.Bold(), new WordDocu.FontSize { Val = "22" }),
                                    new WordDocu.Text(headers[j])
                                )
                            )
                        );
                        headerRow.Append(cell);
                    }
                    deliveryTable.Append(headerRow);

                    foreach (var delivery in customer.DeliveryDetails)
                    {
                        if (delivery.ContactPersons.Count == 0)
                        {
                            var row = new WordDocu.TableRow();
                            var cell = new WordDocu.TableCell(
                                new WordDocu.TableCellProperties(
                                    new WordDocu.GridSpan() { Val = 5 },
                                    new WordDocu.TableCellWidth { Type = WordDocu.TableWidthUnitValues.Dxa, Width = "9668" }
                                ),
                                new WordDocu.Paragraph(new WordDocu.Run(new WordDocu.Text(delivery.DeliveryAddress ?? "")))
                            );
                            row.Append(cell);
                            deliveryTable.Append(row);
                        }
                        else
                        {
                            bool firstRow = true;
                            foreach (var cp in delivery.ContactPersons)
                            {
                                var row = new WordDocu.TableRow();
                                var addressCell = new WordDocu.TableCell(
                                    new WordDocu.TableCellProperties(
                                        new WordDocu.VerticalMerge { Val = firstRow ? WordDocu.MergedCellValues.Restart : WordDocu.MergedCellValues.Continue },
                                        new WordDocu.TableCellWidth { Type = WordDocu.TableWidthUnitValues.Dxa, Width = colWidths[0].ToString() }
                                    ),
                                    new WordDocu.Paragraph(new WordDocu.Run(new WordDocu.Text(delivery.DeliveryAddress ?? "")))
                                );
                                firstRow = false;
                                row.Append(addressCell);

                                for (int k = 1; k < headers.Length; k++)
                                {
                                    string value = k == 1 ? cp.Name : k == 2 ? cp.Designation : k == 3 ? cp.Phone : cp.Email;
                                    var dataCell = new WordDocu.TableCell(
                                        new WordDocu.TableCellProperties(
                                            new WordDocu.TableCellWidth { Type = WordDocu.TableWidthUnitValues.Dxa, Width = colWidths[k].ToString() }
                                        ),
                                        new WordDocu.Paragraph(new WordDocu.Run(new WordDocu.Text(value ?? "")))
                                    );
                                    row.Append(dataCell);
                                }
                                deliveryTable.Append(row);
                            }
                        }
                    }

                    body.AppendChild(deliveryTable);

                    // Section break except last
                    if (i < customers.Count - 1)
                    {
                        var sectionBreakPara = new WordDocu.Paragraph(
                            new WordDocu.ParagraphProperties(
                                new WordDocu.SectionProperties(
                                    new WordDocu.PageSize { Width = 11906, Height = 16838 },
                                    new WordDocu.PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134 },
                                    new WordDocu.SectionType() { Val = WordDocu.SectionMarkValues.NextPage },
                                    new HeaderReference() { Type = HeaderFooterValues.Default, Id = headerPartId },
                                    new FooterReference() { Type = HeaderFooterValues.Default, Id = footerPartId }
                                )
                            )
                        );
                        body.AppendChild(sectionBreakPara);
                    }
                }

                // Final section properties
                var finalSectionProps = new SectionProperties(
                    new WordDocu.PageSize { Width = 15840, Height = 12240, Orient = PageOrientationValues.Landscape },
                    new WordDocu.PageMargin { Top = 720, Right = 360, Bottom = 720, Left = 360 },
                    new HeaderReference() { Type = HeaderFooterValues.Default, Id = headerPartId },
                    new FooterReference() { Type = HeaderFooterValues.Default, Id = footerPartId }
                );
                body.AppendChild(finalSectionProps);

                mainPart.Document.Save();
            }

            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "CustomerProfile.docx");
        }
        #endregion
    }
}




