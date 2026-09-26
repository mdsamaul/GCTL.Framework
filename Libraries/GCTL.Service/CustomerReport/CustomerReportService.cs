using ClosedXML.Excel;
using GCTL.Core.ViewModels.CustomerReport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Data;

namespace GCTL.Service.CustomerReport
{
    public class CustomerReportService : ICustomerReportService
    {
        #region Fields & Constructor

        private readonly string _connectionString;

        public CustomerReportService(
            IConfiguration configuration
            
            )
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
        }

        #endregion

        #region Fetch Customer Report Data

        public async Task<List<CustomerReportResultDto>> GetCustomerReportDataAsync(CustomerReportFilterDto filter)
        {
            var result = new List<CustomerReportResultDto>();

            if (string.IsNullOrEmpty(_connectionString))
            {
                return result;
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("Sales_GetCustomerReport", connection);
            command.CommandType = CommandType.StoredProcedure;

            string customerIds = filter.CustomerId != null && filter.CustomerId.Any() ? string.Join(",", filter.CustomerId) : string.Empty;
            string customerTypeIds = filter.CustomerTypeId != null && filter.CustomerTypeId.Any() ? string.Join(",", filter.CustomerTypeId) : string.Empty;

            command.Parameters.Add(new SqlParameter("@CustomerID", string.IsNullOrWhiteSpace(customerIds) ? "" : customerIds.Trim()));
            command.Parameters.Add(new SqlParameter("@CustomerTypeID", string.IsNullOrWhiteSpace(customerTypeIds) ? "" : customerTypeIds.Trim()));
            command.Parameters.Add(new SqlParameter("@CompanyCode", DBNull.Value));

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string GetString(string col)
                {
                    try
                    {
                        int idx = reader.GetOrdinal(col);
                        return reader.IsDBNull(idx) ? string.Empty : reader.GetValue(idx).ToString()?.Trim() ?? string.Empty;
                    }
                    catch { return string.Empty; }
                }

                byte[] GetBytes(string col)
                {
                    try
                    {
                        int idx = reader.GetOrdinal(col);
                        if (reader.IsDBNull(idx)) return null;
                        return (byte[])reader.GetValue(idx);
                    }
                    catch { return null; }
                }

                result.Add(new CustomerReportResultDto
                {
                    CustomerId = GetString("CustomerID"),
                    CustomerName = GetString("CustomerName"),
                    CustomerAddress = GetString("Address"),
                    Phone = GetString("Phone"),
                    Fax = GetString("FAX"),
                    Email = GetString("Email"),
                    CustomerPhoto = GetBytes("Photo"),
                    ContactPerson = GetString("ContactPerson"),
                    Designation = GetString("Designation"),
                    CustomerTypeId = GetString("CustomerTypeID"),
                    CustomerType = GetString("CustomerTypeName"),
                    CompanyCode = GetString("CompanyCode"),
                    CompanyName = GetString("CompanyName"),
                    CompanyAddress1 = GetString("CompanyAddress1")
                });
            }

            return result;
        }

        #endregion

        #region Generate PDF Report

        public async Task<byte[]> GeneratePdfReportAsync(CustomerReportFilterDto filter)
        {
            try
            {
                var data = await GetCustomerReportDataAsync(filter);

                var firstItem = data.FirstOrDefault();
                string companyName = !string.IsNullOrWhiteSpace(firstItem?.CompanyName) ? firstItem.CompanyName : "Formosa Logistics Limited"; 
                string companyAddress = !string.IsNullOrWhiteSpace(firstItem?.CompanyAddress1) ? firstItem.CompanyAddress1 : "21, Hazaribagh, Dhaka-1209"; 

                string GetTypeLabel(string typeId)
                {
                    if (string.IsNullOrWhiteSpace(typeId)) return "N/A";
                    return typeId.Trim() switch
                    {
                        "0" => "General",
                        "1" => "In House",
                        _ => typeId.Trim()
                    };
                }

                var groupedData = data .GroupBy(x => GetTypeLabel(x.CustomerTypeId)) .OrderBy(g => g.Key) .ToList();

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.MarginHorizontal(10);
                        page.MarginVertical(12);
                        page.Size(PageSizes.A4.Landscape());

                        page.Header().Column(col =>
                        {
                            col.Item().AlignCenter().Text(companyName).FontSize(13).Bold();
                            col.Item().AlignCenter().Text(companyAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                            col.Item().AlignCenter().Text("Customer Report").FontSize(11).Bold().Underline();
                            //col.Item().PaddingTop(2).PaddingBottom(4).Row(row =>
                            //{
                            //    row.RelativeItem().AlignRight().Text($"Print Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8);
                            //});
                        });

                        page.Content().Column(mainCol =>
                        {
                           // int sl = 1;
                            bool isFirstGroup = true;

                            foreach (var group in groupedData)
                            {
                                //if (!isFirstGroup)
                                //{
                                //    mainCol.Item().PageBreak();
                                //}
                                //isFirstGroup = false;
                                int sl = 1;
                                mainCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1.7f);
                                        cols.RelativeColumn(3.0f);
                                        cols.RelativeColumn(6.0f);
                                        cols.RelativeColumn(7.0f);
                                        cols.RelativeColumn(3.0f);
                                        cols.RelativeColumn(4.0f);
                                        cols.RelativeColumn(6.0f);
                                        cols.RelativeColumn(3.0f);
                                        cols.RelativeColumn(9.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().ColumnSpan(9).Element(c => c
                                            .PaddingTop(6)
                                            .PaddingBottom(3)
                                            .Text(text =>
                                            {
                                                text.Span("Customer Type: ").FontSize(10).Bold();
                                                text.Span(group.Key).FontSize(10).Bold();
                                            }));

                                        void BuildHeaderCell(string title)
                                        {
                                            header.Cell().Element(c => c
                                                .Border(0.5f)
                                                .BorderColor(Colors.Grey.Darken1)
                                                .Background(Colors.Grey.Lighten3)
                                                .PaddingHorizontal(2)
                                                .PaddingVertical(3)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text(title)
                                                .Bold()
                                                .FontSize(8f)
                                                .WrapAnywhere());
                                        }

                                        BuildHeaderCell("SN");
                                        BuildHeaderCell("Customer ID");
                                        BuildHeaderCell("Name");
                                        BuildHeaderCell("Address");
                                        BuildHeaderCell("Phone");
                                        BuildHeaderCell("Fax");
                                        BuildHeaderCell("Email");
                                        BuildHeaderCell("Photo");
                                        BuildHeaderCell("Contact Person");
                                    });

                                    foreach (var d in group)
                                    {
                                        void BuildDataCell(string text, bool center = false)
                                        {
                                            table.Cell().Element(c => c
                                                .Border(0.5f)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .PaddingHorizontal(2)
                                                .PaddingVertical(2)
                                                .AlignMiddle()
                                                .Element(e => center ? e.AlignCenter() : e.AlignLeft())
                                                .Text(text ?? "")
                                                .FontSize(7.5f)
                                                .WrapAnywhere());
                                        }

                                        BuildDataCell(sl.ToString(), true);
                                        BuildDataCell(d.CustomerId, true);
                                        BuildDataCell(d.CustomerName);
                                        BuildDataCell(d.CustomerAddress);
                                        BuildDataCell(d.Phone, true);
                                        BuildDataCell(d.Fax, true);
                                        BuildDataCell(d.Email, true);

                                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1)
                                            .PaddingHorizontal(2).PaddingVertical(2)
                                            .AlignCenter().AlignMiddle()
                                            .Height(28)
                                            .Column(imgCol =>
                                            {
                                                if (d.CustomerPhoto != null && d.CustomerPhoto.Length > 0)
                                                {
                                                    imgCol.Item().Image(d.CustomerPhoto).FitArea();
                                                }
                                                else
                                                {
                                                    imgCol.Item().AlignCenter().Text("-").FontSize(6.5f);
                                                }
                                            });

                                        BuildDataCell(d.ContactPerson);

                                        sl++;
                                    }
                                });
                            }
                        });

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

        public async Task<byte[]> GenerateExcelReportAsync(CustomerReportFilterDto filter)
        {
            var data = await GetCustomerReportDataAsync(filter);

            var firstItem = data.FirstOrDefault();
            string companyName = !string.IsNullOrWhiteSpace(firstItem?.CompanyName) ? firstItem.CompanyName : "Formosa Logistics Limited";
            string companyAddress = !string.IsNullOrWhiteSpace(firstItem?.CompanyAddress1) ? firstItem.CompanyAddress1 : "21, Hazaribagh, Dhaka-1209";

            string GetTypeLabel(string typeId)
            {
                if (string.IsNullOrWhiteSpace(typeId)) return "N/A";
                return typeId.Trim() switch
                {
                    "0" => "General",
                    "1" => "In House",
                    _ => typeId.Trim()
                };
            }

            var groupedData = data .GroupBy(x => GetTypeLabel(x.CustomerTypeId)) .OrderBy(g => g.Key) .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Customer Report");

            const int totalCols = 9;

            float[] relativeWidths = { 1.5f, 3.0f, 9.0f, 10.0f, 5.0f, 5.0f, 4.0f, 3.5f, 12.5f };
            const double scale = 4.5;

            for (int i = 0; i < relativeWidths.Length; i++)
            {
                ws.Column(i + 1).Width = relativeWidths[i] * scale;
            }

            var r1 = ws.Range(1, 1, 1, totalCols);
            r1.Merge();
            ws.Cell(1, 1).Value = companyName;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var r2 = ws.Range(2, 1, 2, totalCols);
            r2.Merge();
            ws.Cell(2, 1).Value = companyAddress;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var r3 = ws.Range(3, 1, 3, totalCols);
            r3.Merge();
            ws.Cell(3, 1).Value = "Customer Report";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Cell(3, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(3, 1).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] headers = new string[]
            {
              "SN", "Customer ID", "Name", "Address", "Phone", "Fax", "Email", "Photo", "Contact Person"
            };

            int currRow = 5;
           // int sl = 1;
            const int photoColIndex = 8;
            const double rowHeightForPhoto = 40;

            var groupPageBreakRows = new List<int>();

            foreach (var group in groupedData)
            {
                int sl = 1;
                if (groupPageBreakRows.Count > 0 || currRow > 5)
                {
                    groupPageBreakRows.Add(currRow);
                }

                var groupRange = ws.Range(currRow, 1, currRow, totalCols);
                groupRange.Merge();
                ws.Cell(currRow, 1).Value = $"Customer Type: {group.Key}";
                ws.Cell(currRow, 1).Style.Font.Bold = true;
                ws.Cell(currRow, 1).Style.Font.FontSize = 10;
                ws.Cell(currRow, 1).Style.Font.FontName = "Times New Roman";
                ws.Cell(currRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E2F3");
                ws.Cell(currRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                currRow++;

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(currRow, i + 1);
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
                currRow++;

                foreach (var item in group)
                {
                    ws.Cell(currRow, 1).Value = sl;
                    ws.Cell(currRow, 2).Value = item.CustomerId;
                    ws.Cell(currRow, 3).Value = item.CustomerName;
                    ws.Cell(currRow, 4).Value = item.CustomerAddress;
                    ws.Cell(currRow, 5).Value = item.Phone;
                    ws.Cell(currRow, 6).Value = item.Fax;
                    ws.Cell(currRow, 7).Value = item.Email;
                    ws.Cell(currRow, 9).Value = item.ContactPerson;

                    if (item.CustomerPhoto != null && item.CustomerPhoto.Length > 0)
                    {
                        try
                        {
                            using var imgStream = new MemoryStream(item.CustomerPhoto);
                            var picture = ws.AddPicture(imgStream)
                                .MoveTo(ws.Cell(currRow, photoColIndex), 2, 2);
                            picture.Width = 32;
                            picture.Height = 32;
                            ws.Row(currRow).Height = rowHeightForPhoto;
                        }
                        catch
                        {
                            ws.Cell(currRow, photoColIndex).Value = "-";
                            ws.Cell(currRow, photoColIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                    }
                    else
                    {
                        ws.Cell(currRow, photoColIndex).Value = "-";
                        ws.Cell(currRow, photoColIndex).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    for (int c = 1; c <= totalCols; c++)
                    {
                        var cell = ws.Cell(currRow, c);
                        cell.Style.Font.FontSize = 9;
                        cell.Style.Font.FontName = "Times New Roman";
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Alignment.WrapText = true;

                        if (c == 1 || c == 2 || c == 5 || c == 6 || c == 8)
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        else
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    currRow++;
                    sl++;
                }

                currRow++;
            }

            foreach (var breakRow in groupPageBreakRows)
            {
                ws.PageSetup.AddHorizontalPageBreak(breakRow - 1);
            }

            ws.Rows(5, currRow - 1).AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        #endregion

    }
}
