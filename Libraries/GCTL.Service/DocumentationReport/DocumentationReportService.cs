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

        public DocumentationReportService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
        }

        #region Fetch Report Data
        public async Task<List<DocumentationReportItemDTO>> GetDocumentationReportDataAsync(DocumentationReportFilterVM filter)
        {
            var result = new List<DocumentationReportItemDTO>();

            if (string.IsNullOrEmpty(_connectionString))
            {
                return result;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("DocumentationReport", connection);
            command.CommandType = CommandType.StoredProcedure;

            // DateUse: 'Y' if dates are provided, otherwise 'N'
            bool hasDates = !string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate);
            string dateUse = hasDates ? "Y" : "N";

            command.Parameters.Add(new SqlParameter("@DateUse", dateUse));
            command.Parameters.Add(new SqlParameter("@fromDate", string.IsNullOrWhiteSpace(filter.FromDate) ? (object)DBNull.Value : filter.FromDate.Trim()));
            command.Parameters.Add(new SqlParameter("@toDate", string.IsNullOrWhiteSpace(filter.ToDate) ? (object)DBNull.Value : filter.ToDate.Trim()));
            command.Parameters.Add(new SqlParameter("@CustomerID", string.IsNullOrWhiteSpace(filter.CustomerId) ? "" : filter.CustomerId.Trim()));
            command.Parameters.Add(new SqlParameter("@PortId", string.IsNullOrWhiteSpace(filter.PortType) ? "" : filter.PortType.Trim()));
            command.Parameters.Add(new SqlParameter("@ExpenseTypeID", string.IsNullOrWhiteSpace(filter.ShipmentMode) ? "" : filter.ShipmentMode.Trim()));
            command.Parameters.Add(new SqlParameter("@JobNo", string.IsNullOrWhiteSpace(filter.JobNo) ? "" : filter.JobNo.Trim()));
            command.Parameters.Add(new SqlParameter("@JobStatus", string.IsNullOrWhiteSpace(filter.JobStatus) ? "" : filter.JobStatus.Trim()));
            command.Parameters.Add(new SqlParameter("@ReportOption", string.IsNullOrWhiteSpace(filter.ReportOption) ? "Job Report" : filter.ReportOption.Trim()));

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

                // Prefer specialized aliases if present, fallback to raw columns
                string jobNo = GetString("JobNo");
                string lcaNo = !string.IsNullOrEmpty(GetString("LCANo")) ? GetString("LCANo") : GetString("LCNo");
                string invoiceNo = GetString("InvoiceNo");
                string invoiceVal = GetString("LCValue");
                decimal? numInvoiceVal = GetDecimal("DataColumn1");
                string currency = GetString("DataColumn2");

                string containerPkgs = !string.IsNullOrEmpty(GetString("ContainerPackages"))
                    ? GetString("ContainerPackages")
                    : GetString("ContainerNo");

                string shipper = !string.IsNullOrEmpty(GetString("Shipper"))
                    ? GetString("Shipper")
                    : GetString("SupplierName");

                string blNo = !string.IsNullOrEmpty(GetString("BLNo"))
                    ? GetString("BLNo")
                    : (!string.IsNullOrEmpty(GetString("BillNo")) ? GetString("BillNo") : GetString("BENo"));

                string loadingPort = !string.IsNullOrEmpty(GetString("LoadingPort"))
                    ? GetString("LoadingPort")
                    : GetString("PlaceOfLoadingName");

                string etdDt = GetString("ETDDate");
                string etaCtg = GetString("ETACTGPORT");
                string shipArrived = GetString("SHIPARRIVEDDATE");
                string dischargeDt = !string.IsNullOrEmpty(GetString("DischargeDate")) ? GetString("DischargeDate") : GetString("Dischargedate");
                string shedYard = !string.IsNullOrEmpty(GetString("ShedYard")) ? GetString("ShedYard") : GetString("ShedYardName");
                string bankSign = !string.IsNullOrEmpty(GetString("DocumentBankSign")) ? GetString("DocumentBankSign") : GetString("DocumentHandoverDate");
                string etaDelivery = !string.IsNullOrEmpty(GetString("ETADelivery")) ? GetString("ETADelivery") : GetString("ETA");
                string actualDelivery = !string.IsNullOrEmpty(GetString("ActualDelivery")) ? GetString("ActualDelivery") : GetString("DateOfClearence");
                string remarks = GetString("Remarks");
                string term = !string.IsNullOrEmpty(GetString("Term")) ? GetString("Term") : GetString("FreightCharge");
                string vesselRot = !string.IsNullOrEmpty(GetString("VesselRotNo")) ? GetString("VesselRotNo") : GetString("Vessel_RottNo");
                string containerNo = GetString("ContainerNo");
                string forwarder = !string.IsNullOrEmpty(GetString("ForwarderName")) ? GetString("ForwarderName") : GetString("FWDR_ShippingAgent");
                string surrendered = GetString("SurrenderedBLIssue");

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
                    Weight = !string.IsNullOrEmpty(GetString("Quntity1")) ? GetString("Quntity1") : GetString("Quntity2"),
                    DateOfClearence = GetString("DateOfClearence")
                };

                result.Add(item);
            }

            return result;
        }
        #endregion

        #region Generate PDF Report
        public async Task<byte[]> GeneratePdfReportAsync(DocumentationReportFilterVM filter)
        {
            try
            {
                var data = await GetDocumentationReportDataAsync(filter);

                string companyName = data.FirstOrDefault()?.CompanyName;
                if (string.IsNullOrWhiteSpace(companyName)) companyName = "Formosa Logistics Limited";

                string companyAddress = data.FirstOrDefault()?.Address1;
                if (string.IsNullOrWhiteSpace(companyAddress)) companyAddress = "21, Hazaribagh, Dhaka-1209";

                string customerHeader = "All";
                if (!string.IsNullOrWhiteSpace(filter.CustomerId))
                {
                    var matched = data.FirstOrDefault(x => !string.IsNullOrEmpty(x.CustomerName))?.CustomerName;
                    if (!string.IsNullOrWhiteSpace(matched))
                        customerHeader = matched;
                }

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.MarginHorizontal(10);
                        page.MarginVertical(12);
                        page.Size(PageSizes.A4.Landscape());

                        // Header
                        page.Header().Column(col =>
                        {
                            col.Item().AlignCenter().Text(companyName).FontSize(13).Bold();
                            col.Item().AlignCenter().Text(companyAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                            col.Item().AlignCenter().Text("Job Report").FontSize(11).Bold().Underline();
                            col.Item().PaddingTop(2).PaddingBottom(4).Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text($"Customer Name: {customerHeader}").FontSize(8.5f).Bold();
                                if (!string.IsNullOrWhiteSpace(filter.FromDate) && !string.IsNullOrWhiteSpace(filter.ToDate))
                                {
                                    row.RelativeItem().AlignRight().Text($"Date: {filter.FromDate} to {filter.ToDate}").FontSize(8);
                                }
                            });
                        });

                        // Content Table
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.8f); // SL No
                                cols.RelativeColumn(4.2f); // JOB NO
                                cols.RelativeColumn(3.8f); // LCAF L/C NO
                                cols.RelativeColumn(3.8f); // INVOICE NO
                                cols.RelativeColumn(4.0f); // INVOICE VALUE
                                cols.RelativeColumn(4.0f); // CONTAINER / PKGS
                                cols.RelativeColumn(4.5f); // SHIPPER
                                cols.RelativeColumn(3.5f); // BL NO
                                cols.RelativeColumn(3.5f); // LOADING PORT
                                cols.RelativeColumn(3.0f); // ETD DT
                                cols.RelativeColumn(3.0f); // ETA-CTG PORT
                                cols.RelativeColumn(3.0f); // SHIP ARRIVED DT
                                cols.RelativeColumn(3.5f); // UNSTAFFING/DISCHARGE
                                cols.RelativeColumn(3.2f); // SHED/ YARD
                                cols.RelativeColumn(3.2f); // DOCS. BANK SIGN
                                cols.RelativeColumn(3.0f); // ETA DELIVERY
                                cols.RelativeColumn(3.0f); // ACTUAL DELIVERY
                                cols.RelativeColumn(3.8f); // REMARKS
                                cols.RelativeColumn(2.5f); // TERM
                                cols.RelativeColumn(3.8f); // VESSEL NAME & ROT NO
                                cols.RelativeColumn(3.8f); // CONTAINER NO
                                cols.RelativeColumn(4.0f); // FORWARDER & PHONE
                                cols.RelativeColumn(3.0f); // SURRENDERED BL ISSUE
                            });

                            // Header Row
                            table.Header(header =>
                            {
                                void BuildHeaderCell(string title)
                                {
                                    header.Cell().Element(c => c
                                        .Border(0.5f)
                                        .BorderColor(Colors.Grey.Darken1)
                                        .Background(Colors.Grey.Lighten3)
                                        .PaddingHorizontal(1)
                                        .PaddingVertical(2)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(title)
                                        .Bold()
                                        .FontSize(5.5f)
                                        .WrapAnywhere());
                                }

                                BuildHeaderCell("SL No.");
                                BuildHeaderCell("JOB NO.");
                                BuildHeaderCell("LCAF L/C NO.");
                                BuildHeaderCell("INVOICE NO");
                                BuildHeaderCell("INVOICE VALUE");
                                BuildHeaderCell("CONTAINER / PKGS");
                                BuildHeaderCell("SHIPPER");
                                BuildHeaderCell("BL NO.");
                                BuildHeaderCell("LOADING PORT");
                                BuildHeaderCell("ETD DT");
                                BuildHeaderCell("ETA-CTG PORT");
                                BuildHeaderCell("SHIP ARRIVED DT.");
                                BuildHeaderCell("UNSTAFFING/ DISCHARGE");
                                BuildHeaderCell("SHED/ YARD");
                                BuildHeaderCell("DOCS. BANK SIGN");
                                BuildHeaderCell("ETA DELIVERY");
                                BuildHeaderCell("ACTUAL DELIVERY");
                                BuildHeaderCell("REMARKS");
                                BuildHeaderCell("TERM");
                                BuildHeaderCell("VESSEL NAME & ROT NO.");
                                BuildHeaderCell("CONTAINER NO.");
                                BuildHeaderCell("FORWARDER NAME & PHONE NO.");
                                BuildHeaderCell("SURRENDERED BL ISSUE");
                            });

                            // Data Rows
                            foreach (var d in data)
                            {
                                void BuildDataCell(string text, bool center = false)
                                {
                                    table.Cell().Element(c => c
                                        .Border(0.5f)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .PaddingHorizontal(1)
                                        .PaddingVertical(1)
                                        .AlignMiddle()
                                        .Element(e => center ? e.AlignCenter() : e.AlignLeft())
                                        .Text(text ?? "")
                                        .FontSize(5.2f)
                                        .WrapAnywhere());
                                }

                                BuildDataCell(d.SlNo.ToString(), true);
                                BuildDataCell(d.JobNo, true);
                                BuildDataCell(d.LCANo);
                                BuildDataCell(d.InvoiceNo);
                                BuildDataCell(d.InvoiceValue, true);
                                BuildDataCell(d.ContainerPackages);
                                BuildDataCell(d.Shipper);
                                BuildDataCell(d.BLNo);
                                BuildDataCell(d.LoadingPort);
                                BuildDataCell(d.ETDDate, true);
                                BuildDataCell(d.ETACTGPORT, true);
                                BuildDataCell(d.SHIPARRIVEDDATE, true);
                                BuildDataCell(d.DischargeDate, true);
                                BuildDataCell(d.ShedYard);
                                BuildDataCell(d.DocumentBankSign, true);
                                BuildDataCell(d.ETADelivery, true);
                                BuildDataCell(d.ActualDelivery, true);
                                BuildDataCell(d.Remarks);
                                BuildDataCell(d.Term, true);
                                BuildDataCell(d.VesselRotNo);
                                BuildDataCell(d.ContainerNo);
                                BuildDataCell(d.ForwarderName);
                                BuildDataCell(d.SurrenderedBLIssue, true);
                            }
                        });

                        // Footer
                        page.Footer().Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Print Datetime: {DateTime.Now:dd/MM/yyyy hh:mm:ss tt}").FontSize(7).FontColor(Colors.Grey.Medium);
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                                txt.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                }).GeneratePdf();

                return pdfBytes;
            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion

        #region Generate Excel Report
        public async Task<byte[]> GenerateExcelReportAsync(DocumentationReportFilterVM filter)
        {
            var data = await GetDocumentationReportDataAsync(filter);

            string companyName = data.FirstOrDefault()?.CompanyName;
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Formosa Logistics Limited";

            string companyAddress = data.FirstOrDefault()?.Address1;
            if (string.IsNullOrWhiteSpace(companyAddress)) companyAddress = "21, Hazaribagh, Dhaka-1209";

            string customerHeader = "All";
            if (!string.IsNullOrWhiteSpace(filter.CustomerId))
            {
                var matched = data.FirstOrDefault(x => !string.IsNullOrEmpty(x.CustomerName))?.CustomerName;
                if (!string.IsNullOrWhiteSpace(matched))
                    customerHeader = matched;
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Documentation Report");

            const int totalCols = 23;

            // Row 1: Company Name
            var r1 = ws.Range(1, 1, 1, totalCols);
            r1.Merge();
            ws.Cell(1, 1).Value = companyName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 2: Company Address
            var r2 = ws.Range(2, 1, 2, totalCols);
            r2.Merge();
            ws.Cell(2, 1).Value = companyAddress;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 3: Report Title
            var r3 = ws.Range(3, 1, 3, totalCols);
            r3.Merge();
            ws.Cell(3, 1).Value = "Job Report";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Cell(3, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(3, 1).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 4: Customer Name Header
            var r4 = ws.Range(4, 1, 4, totalCols);
            r4.Merge();
            ws.Cell(4, 1).Value = $"Customer Name: {customerHeader}";
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 10;
            ws.Cell(4, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Row 5: Blank spacer
            // Row 6: Table Headers
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

            int startRow = 6;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(startRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 9;
                cell.Style.Font.FontName = "Times New Roman";
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int currRow = startRow + 1;
            foreach (var item in data)
            {
                ws.Cell(currRow, 1).Value = item.SlNo;
                ws.Cell(currRow, 2).Value = item.JobNo;
                ws.Cell(currRow, 3).Value = item.LCANo;
                ws.Cell(currRow, 4).Value = item.InvoiceNo;
                ws.Cell(currRow, 5).Value = item.InvoiceValue;
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
                    cell.Style.Font.FontSize = 9;
                    cell.Style.Font.FontName = "Times New Roman";
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Centering specific columns
                    if (c == 1 || c == 2 || c == 5 || c == 10 || c == 11 || c == 12 || c == 13 || c == 15 || c == 16 || c == 17 || c == 19 || c == 23)
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

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion
    }
}
