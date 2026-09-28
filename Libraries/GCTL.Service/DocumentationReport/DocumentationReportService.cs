using ClosedXML.Excel;
using GCTL.Core.ViewModels.DocumentationReport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.DocumentationReport
{
    public class DocumentationReportService : IDocumentationReportService
    {
        private readonly string _connectionString;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _webHostEnvironment;
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        public DocumentationReportService(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
            _webHostEnvironment = env;
        }

        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                var candidatePaths = new List<string?>
                {
                    _webHostEnvironment?.WebRootPath != null ? Path.Combine(_webHostEnvironment.WebRootPath, "images", "fermosa-wX200.png") : null,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\Project\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
                };

                foreach (var path in candidatePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        _logoBytes = File.ReadAllBytes(path);
                        break;
                    }
                }
            }
            catch
            {
                // Fallback gracefully without throwing
            }

            return _logoBytes;
        }

        #region Fetch Report Data
        public async Task<List<DocumentationReportItemDTO>> GetDocumentationReportDataAsync(DocumentationReportFilterVM filter)
        {
            var result = new List<DocumentationReportItemDTO>();

            if (string.IsNullOrEmpty(_connectionString))
            {
                return result;
            }

            filter ??= new DocumentationReportFilterVM();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("DocumentationReport", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 240; // 4 minutes timeout to avoid 'Execution Timeout Expired'

            // DateUse: 'Y' if both dates are provided, otherwise 'N'
            bool hasDates = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate);
            string dateUse = hasDates ? "Y" : "N";

            string fromDateStr = "1900-01-01";
            string toDateStr = "2099-12-31";
            if (hasDates)
            {
                if (DateTime.TryParseExact(filter.FromDate!.Trim(), "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fDate))
                    fromDateStr = fDate.ToString("yyyy-MM-dd");
                else
                    fromDateStr = filter.FromDate.Trim();

                if (DateTime.TryParseExact(filter.ToDate!.Trim(), "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime tDate))
                    toDateStr = tDate.ToString("yyyy-MM-dd");
                else
                    toDateStr = filter.ToDate.Trim();
            }

            // Exactly 7 parameters matching [dbo].[DocumentationReport]
            command.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            command.Parameters.Add(new SqlParameter("@fromDate", fromDateStr));
            command.Parameters.Add(new SqlParameter("@toDate", toDateStr));
            command.Parameters.Add(new SqlParameter("@CustomerID", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
            command.Parameters.Add(new SqlParameter("@PortId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
            command.Parameters.Add(new SqlParameter("@ExpenseTypeID", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));
            command.Parameters.Add(new SqlParameter("@JobNo", string.IsNullOrWhiteSpace(filter.JobNo) ? "" : filter.JobNo.Trim()));

            using var reader = await command.ExecuteReaderAsync();

            int sl = 1;
            while (await reader.ReadAsync())
            {
                string GetString(string col)
                {
                    try
                    {
                        int idx = reader.GetOrdinal(col);
                        return reader.IsDBNull(idx) ? string.Empty : reader.GetValue(idx).ToString()?.Trim() ?? string.Empty;
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }

                decimal? GetDecimal(string col)
                {
                    try
                    {
                        int idx = reader.GetOrdinal(col);
                        if (reader.IsDBNull(idx)) return null;
                        var val = reader.GetValue(idx);
                        if (decimal.TryParse(val.ToString(), out decimal parsed))
                            return parsed;
                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                }

                string jobNo = GetString("JobNo");
                string lcaNo = !string.IsNullOrEmpty(GetString("LCANo")) ? GetString("LCANo") : GetString("LCNo");
                string invoiceNo = GetString("InvoiceNo");

                decimal? numInvoiceVal = GetDecimal("DataColumn1");
                string currency = GetString("DataColumn2");
                string invoiceVal = GetString("LCValue");
                if (numInvoiceVal.HasValue && !string.IsNullOrWhiteSpace(currency))
                {
                    invoiceVal = $"{currency} {numInvoiceVal.Value:N2}";
                }
                else if (numInvoiceVal.HasValue)
                {
                    invoiceVal = numInvoiceVal.Value.ToString("N2");
                }

                string containerNo = GetString("ContainerNo");
                string qty1 = GetString("Quntity1");
                string qty2 = GetString("Quntity2");
                string containerPkgs = "";
                if (!string.IsNullOrWhiteSpace(containerNo) && !string.IsNullOrWhiteSpace(qty1))
                    containerPkgs = $"{containerNo} / {qty1}";
                else if (!string.IsNullOrWhiteSpace(containerNo))
                    containerPkgs = containerNo;
                else if (!string.IsNullOrWhiteSpace(qty1))
                    containerPkgs = qty1;
                else if (!string.IsNullOrWhiteSpace(qty2))
                    containerPkgs = qty2;

                string shipper = !string.IsNullOrEmpty(GetString("SupplierName")) ? GetString("SupplierName") : GetString("Shipper");
                string blNo = !string.IsNullOrEmpty(GetString("BillNo")) ? GetString("BillNo") : GetString("BENo");
                string loadingPort = GetString("PlaceOfLoadingName");
                string etdDt = GetString("ETDDate");
                string etaCtg = GetString("ETACTGPORT");
                string shipArrived = GetString("SHIPARRIVEDDATE");
                string dischargeDt = !string.IsNullOrEmpty(GetString("Dischargedate")) ? GetString("Dischargedate") : GetString("Un_StuffingLocationDate");
                string shedYard = GetString("ShedYardName");
                string bankSign = GetString("DocumentHandoverDate");
                string etaDelivery = GetString("ETA");
                string actualDelivery = !string.IsNullOrEmpty(GetString("DeliveryDate")) ? GetString("DeliveryDate") : GetString("DateOfClearence");
                string remarks = GetString("Remarks");
                string term = GetString("FreightCharge");
                string vesselRot = GetString("Vessel_RottNo");
                string forwarder = GetString("FWDR_ShippingAgent");
                string surrendered = !string.IsNullOrEmpty(GetString("CargoHandoverDate")) ? GetString("CargoHandoverDate") : GetString("BookingConfirmationReference");

                var item = new DocumentationReportItemDTO
                {
                    SlNo = sl++,
                    JobNo = jobNo,
                    LCANo = lcaNo,
                    InvoiceNo = invoiceNo,
                    InvoiceValue = invoiceVal,
                    NumericInvoiceValue = numInvoiceVal,
                    Currency = currency,
                    ContainerPackages = containerPkgs,
                    Shipper = shipper,
                    BLNo = blNo,
                    LoadingPort = loadingPort,
                    ETDDate = etdDt,
                    ETACTGPORT = etaCtg,
                    SHIPARRIVEDDATE = shipArrived,
                    DischargeDate = dischargeDt,
                    ShedYard = shedYard,
                    DocumentBankSign = bankSign,
                    ETADelivery = etaDelivery,
                    ActualDelivery = actualDelivery,
                    Remarks = remarks,
                    Term = term,
                    VesselRotNo = vesselRot,
                    ContainerNo = containerNo,
                    ForwarderName = forwarder,
                    SurrenderedBLIssue = surrendered,

                    CustomerName = GetString("CustomerName"),
                    CustomerAddress = GetString("CustomerAddress"),
                    CompanyName = GetString("CompanyName"),
                    Address1 = GetString("Address1"),
                    ShipmentMode = GetString("ExpenseType"),
                    PortName = GetString("PortName"),
                    JobDate = GetString("JobDate"),
                    DocReceivedDate = GetString("DocReceivedDate"),
                    ShipmentStatus = GetString("ShipmentStatus"),
                    Weight = !string.IsNullOrWhiteSpace(qty1) ? qty1 : qty2,
                    DateOfClearence = GetString("DateOfClearence")
                };

                result.Add(item);
            }

            // Client-side filtering by JobStatus if specified
            if (!string.IsNullOrWhiteSpace(filter.JobStatus))
            {
                string statusFilter = filter.JobStatus.Trim();
                result = result.Where(x =>
                    string.Equals(x.ShipmentStatus, statusFilter, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(x.ShipmentStatus) && x.ShipmentStatus.Contains(statusFilter, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return result;
        }
        #endregion

        #region Generate PDF Report
        public async Task<byte[]> GeneratePdfReportAsync(DocumentationReportFilterVM filter)
        {
            var data = await GetDocumentationReportDataAsync(filter);

            string companyName = data.FirstOrDefault()?.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Formosa Logistics Limited";

            var customerGroups = data.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            var logoBytes = GetLogoBytes();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.MarginHorizontal(8);
                    page.MarginVertical(8);
                    page.Size(PageSizes.A4.Landscape());

                    // Header
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(r =>
                        {
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                r.ConstantItem(60).AlignMiddle().Height(18).Image(logoBytes).FitArea();
                            }
                            else
                            {
                                r.ConstantItem(60);
                            }

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text(companyName).FontSize(11).Bold();
                                c.Item().AlignCenter().Text("Job Report").FontSize(8.5f).Bold();

                                string dateRange = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                                    ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
                                c.Item().AlignCenter().Text(dateRange).FontSize(6.8f).SemiBold();
                            });

                            r.ConstantItem(60);
                        });
                    });

                    // Content Table
                    page.Content().Column(col =>
                    {
                        if (customerGroups.Count == 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1);
                                });
                                table.Cell().Element(c => c
                                    .Border(0.4f)
                                    .BorderColor(Colors.Grey.Lighten1)
                                    .Padding(12)
                                    .AlignCenter()
                                    .Text("No documentation records found for the selected criteria.")
                                    .FontSize(8)
                                    .Italic());
                            });
                            return;
                        }

                        for (int groupIdx = 0; groupIdx < customerGroups.Count; groupIdx++)
                        {
                            var group = customerGroups[groupIdx];

                            if (groupIdx > 0)
                            {
                                col.Item().PageBreak();
                            }

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(1.4f); // SL No
                                    cols.RelativeColumn(3.2f); // JOB NO
                                    cols.RelativeColumn(3.0f); // LCAF L/C NO
                                    cols.RelativeColumn(2.8f); // INVOICE NO
                                    cols.RelativeColumn(3.0f); // INVOICE VALUE
                                    cols.RelativeColumn(3.2f); // CONTAINER / PKGS
                                    cols.RelativeColumn(3.2f); // SHIPPER
                                    cols.RelativeColumn(2.6f); // BL NO
                                    cols.RelativeColumn(2.8f); // LOADING PORT
                                    cols.RelativeColumn(2.4f); // ETD DT
                                    cols.RelativeColumn(2.5f); // ETA-CTG PORT
                                    cols.RelativeColumn(2.5f); // SHIP ARRIVED DT
                                    cols.RelativeColumn(2.8f); // UNSTAFFING/DISCHARGE
                                    cols.RelativeColumn(2.6f); // SHED/ YARD
                                    cols.RelativeColumn(2.6f); // DOCS. BANK SIGN
                                    cols.RelativeColumn(2.5f); // ETA DELIVERY
                                    cols.RelativeColumn(2.5f); // ACTUAL DELIVERY
                                    cols.RelativeColumn(2.8f); // REMARKS
                                    cols.RelativeColumn(1.8f); // TERM
                                    cols.RelativeColumn(2.8f); // VESSEL NAME & ROT NO
                                    cols.RelativeColumn(2.8f); // CONTAINER NO
                                    cols.RelativeColumn(3.0f); // FORWARDER & PHONE
                                    cols.RelativeColumn(2.6f); // SURRENDERED BL ISSUE
                                });

                                table.Header(header =>
                                {
                                    // Customer Header inside table.Header: repeats automatically on every new page for this customer
                                    header.Cell().ColumnSpan(23).PaddingTop(2).PaddingBottom(2).Row(r =>
                                    {
                                        r.RelativeItem().AlignLeft().Text($"CUSTOMER: {group.Key.ToUpper()} ({group.Count()} Jobs)").FontSize(7.5f).Bold().FontColor(Colors.Black);
                                    });

                                    void BuildHeaderCell(string title)
                                    {
                                        header.Cell().Element(c => c
                                            .Border(0.4f)
                                            .BorderColor(Colors.Grey.Medium)
                                            .PaddingHorizontal(1)
                                            .PaddingVertical(1.5f)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text(title)
                                            .Bold()
                                            .FontSize(4.6f)
                                            .WrapAnywhere());
                                    }

                                    BuildHeaderCell("SL");
                                    BuildHeaderCell("Job No.");
                                    BuildHeaderCell("LCAF/LC No.");
                                    BuildHeaderCell("Inv No");
                                    BuildHeaderCell("Inv Value");
                                    BuildHeaderCell("Cont/Pkgs");
                                    BuildHeaderCell("Shipper");
                                    BuildHeaderCell("B/L No.");
                                    BuildHeaderCell("Load Port");
                                    BuildHeaderCell("ETD");
                                    BuildHeaderCell("ETA-CTG");
                                    BuildHeaderCell("Ship Arr");
                                    BuildHeaderCell("Unstaff/Disch");
                                    BuildHeaderCell("Shed/Yard");
                                    BuildHeaderCell("Bank Sign");
                                    BuildHeaderCell("ETA Deliv");
                                    BuildHeaderCell("Act Deliv");
                                    BuildHeaderCell("Remarks");
                                    BuildHeaderCell("Term");
                                    BuildHeaderCell("Vessel/Rot No");
                                    BuildHeaderCell("Cont No.");
                                    BuildHeaderCell("Forwarder");
                                    BuildHeaderCell("Surrendered BL");
                                });

                                int customerSl = 1;
                                foreach (var d in group)
                                {
                                    void BuildDataCell(string text, bool center = false, bool right = false)
                                    {
                                        table.Cell().Element(c => c
                                            .Border(0.3f)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .PaddingHorizontal(1)
                                            .PaddingVertical(1)
                                            .AlignMiddle()
                                            .Element(e => right ? e.AlignRight() : (center ? e.AlignCenter() : e.AlignLeft()))
                                            .Text(text ?? "")
                                            .FontSize(4.4f)
                                            .WrapAnywhere());
                                    }

                                    BuildDataCell(customerSl++.ToString(), center: true);
                                    BuildDataCell(d.JobNo, center: true);
                                    BuildDataCell(d.LCANo, center: true);
                                    BuildDataCell(d.InvoiceNo);
                                    BuildDataCell(d.InvoiceValue, right: true);
                                    BuildDataCell(d.ContainerPackages);
                                    BuildDataCell(d.Shipper);
                                    BuildDataCell(d.BLNo, center: true);
                                    BuildDataCell(d.LoadingPort);
                                    BuildDataCell(d.ETDDate, center: true);
                                    BuildDataCell(d.ETACTGPORT, center: true);
                                    BuildDataCell(d.SHIPARRIVEDDATE, center: true);
                                    BuildDataCell(d.DischargeDate, center: true);
                                    BuildDataCell(d.ShedYard);
                                    BuildDataCell(d.DocumentBankSign, center: true);
                                    BuildDataCell(d.ETADelivery, center: true);
                                    BuildDataCell(d.ActualDelivery, center: true);
                                    BuildDataCell(d.Remarks);
                                    BuildDataCell(d.Term, center: true);
                                    BuildDataCell(d.VesselRotNo);
                                    BuildDataCell(d.ContainerNo);
                                    BuildDataCell(d.ForwarderName);
                                    BuildDataCell(d.SurrenderedBLIssue, center: true);
                                }
                            });
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text($"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}")
                            .FontSize(5.5f)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignCenter().Text("GCTL Infosys – C&F System")
                            .FontSize(5.5f)
                            .FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignRight().Text(txt =>
                        {
                            txt.Span("Page ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.CurrentPageNumber().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.Span(" of ").FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                            txt.TotalPages().FontSize(5.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }
        #endregion

        #region Generate Excel Report
        public async Task<byte[]> GenerateExcelReportAsync(DocumentationReportFilterVM filter)
        {
            var data = await GetDocumentationReportDataAsync(filter);

            string companyName = data.FirstOrDefault()?.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Formosa Logistics Limited";

            var customerGroups = data.GroupBy(x => string.IsNullOrWhiteSpace(x.CustomerName) ? "General Customer" : x.CustomerName.Trim()).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Documentation Report");

            const int totalCols = 23;

            // Formosa Logo at top-left
            var logoBytes = GetLogoBytes();
            if (logoBytes != null && logoBytes.Length > 0)
            {
                ws.Row(1).Height = 20;
                ws.Row(2).Height = 18;
                ws.Row(3).Height = 18;
                try
                {
                    using var logoStream = new MemoryStream(logoBytes);
                    var logoPicture = ws.AddPicture(logoStream).MoveTo(ws.Cell(1, 1), 4, 10);
                    logoPicture.Width = 85;
                    logoPicture.Height = 28;
                }
                catch { }
            }

            // Row 1: Company Name
            var r1 = ws.Range(1, 1, 1, totalCols);
            r1.Merge();
            ws.Cell(1, 1).Value = companyName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 2: Report Title
            var r2 = ws.Range(2, 1, 2, totalCols);
            r2.Merge();
            ws.Cell(2, 1).Value = "Job Report";
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 11;
            ws.Cell(2, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 3: Date Range
            var r3 = ws.Range(3, 1, 3, totalCols);
            r3.Merge();
            string dateRangeStr = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate)
                ? $"Date Range: {filter.FromDate} to {filter.ToDate}" : "Date Range: All Dates";
            ws.Cell(3, 1).Value = dateRangeStr;
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 9.5;
            ws.Cell(3, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = new string[]
            {
                "SL No.",
                "JOB NO.",
                "LCAF L/C NO.",
                "INVOICE NO",
                "INVOICE VALUE",
                "CONTAINER / PKGS",
                "SHIPPER",
                "BL NO.",
                "LOADING PORT",
                "ETD DT",
                "ETA-CTG PORT",
                "SHIP ARRIVED DT.",
                "UNSTAFFING/DISCHARGE",
                "SHED/ YARD",
                "DOCS. BANK SIGN",
                "ETA DELIVERY",
                "ACTUAL DELIVERY",
                "REMARKS",
                "TERM",
                "VESSEL NAME & ROT NO.",
                "CONTAINER NO.",
                "FORWARDER NAME & PHONE NO.",
                "SURRENDERED BL ISSUE"
            };

            int currRow = 5;
            if (customerGroups.Count == 0)
            {
                ws.Row(currRow).Height = 25;
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(currRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 8.5;
                    cell.Style.Font.FontName = "Times New Roman";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#999999");
                }
                currRow++;

                ws.Range(currRow, 1, currRow, totalCols).Merge();
                var emptyCell = ws.Cell(currRow, 1);
                emptyCell.Value = "No documentation records found for the selected criteria.";
                emptyCell.Style.Font.FontSize = 9;
                emptyCell.Style.Font.FontName = "Times New Roman";
                emptyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                emptyCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            else
            {
                foreach (var group in customerGroups)
                {
                    ws.Range(currRow, 1, currRow, totalCols).Merge();
                    var custCell = ws.Cell(currRow, 1);
                    custCell.Value = $"CUSTOMER: {group.Key.ToUpper()}  (Total Jobs: {group.Count()})";
                    custCell.Style.Font.Bold = true;
                    custCell.Style.Font.FontSize = 10;
                    custCell.Style.Font.FontName = "Times New Roman";
                    custCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    custCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Row(currRow).Height = 22;
                    currRow++;

                    ws.Row(currRow).Height = 25;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cell(currRow, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontSize = 8.5;
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Alignment.WrapText = true;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#999999");
                    }
                    currRow++;

                    int customerSl = 1;
                    foreach (var item in group)
                    {
                        ws.Row(currRow).Height = 20;
                        ws.Cell(currRow, 1).Value = customerSl++;
                        ws.Cell(currRow, 2).Value = item.JobNo;
                        ws.Cell(currRow, 3).Value = item.LCANo;
                        ws.Cell(currRow, 4).Value = item.InvoiceNo;

                        if (item.NumericInvoiceValue.HasValue)
                        {
                            ws.Cell(currRow, 5).Value = item.NumericInvoiceValue.Value;
                            ws.Cell(currRow, 5).Style.NumberFormat.Format = item.NumericInvoiceValue.Value % 1 == 0 ? "#,##0" : "#,##0.00";
                        }
                        else
                        {
                            ws.Cell(currRow, 5).Value = item.InvoiceValue;
                        }

                        ws.Cell(currRow, 6).Value = item.ContainerPackages;
                        ws.Cell(currRow, 7).Value = item.Shipper;
                        ws.Cell(currRow, 8).Value = item.BLNo;
                        ws.Cell(currRow, 9).Value = item.LoadingPort;
                        ws.Cell(currRow, 10).Value = item.ETDDate;
                        ws.Cell(currRow, 11).Value = item.ETACTGPORT;
                        ws.Cell(currRow, 12).Value = item.SHIPARRIVEDDATE;
                        ws.Cell(currRow, 13).Value = item.DischargeDate;
                        ws.Cell(currRow, 14).Value = item.ShedYard;
                        ws.Cell(currRow, 15).Value = item.DocumentBankSign;
                        ws.Cell(currRow, 16).Value = item.ETADelivery;
                        ws.Cell(currRow, 17).Value = item.ActualDelivery;
                        ws.Cell(currRow, 18).Value = item.Remarks;
                        ws.Cell(currRow, 19).Value = item.Term;
                        ws.Cell(currRow, 20).Value = item.VesselRotNo;
                        ws.Cell(currRow, 21).Value = item.ContainerNo;
                        ws.Cell(currRow, 22).Value = item.ForwarderName;
                        ws.Cell(currRow, 23).Value = item.SurrenderedBLIssue;

                        for (int c = 1; c <= totalCols; c++)
                        {
                            var cell = ws.Cell(currRow, c);
                            cell.Style.Font.FontSize = 8.5;
                            cell.Style.Font.FontName = "Times New Roman";
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CCCCCC");
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            if (c == 5)
                            {
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            }
                            else if (c == 1 || c == 2 || c == 8 || c == 10 || c == 11 || c == 12 || c == 13 || c == 15 || c == 16 || c == 17 || c == 19 || c == 23)
                            {
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }
                            else
                            {
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            }
                        }
                        currRow++;
                    }
                    currRow++; // Spacer row between customers
                }
            }

            ws.Columns(1, totalCols).AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion
    }
}
