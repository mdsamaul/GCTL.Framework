using ClosedXML.Excel;
using GCTL.Core.ViewModels.CustomerReport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.CustomerReport
{
    public class CustomerReportService : ICustomerReportService
    {
        private readonly string _connectionString;

        public CustomerReportService(
            IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection") ?? string.Empty;
        }

        #region Fetch Customer Report Data
        //public async Task<List<CustomerReportResultDto>> GetCustomerReportDataAsync(CustomerReportFilterDto filter)
        //{
        //    var result = new List<CustomerReportResultDto>();

        //    if (string.IsNullOrEmpty(_connectionString))
        //    {
        //        return result;
        //    }

        //    using var connection = new SqlConnection(_connectionString);
        //    await connection.OpenAsync();

        //    using var command = new SqlCommand("Sales_GetCustomerReport", connection);
        //    command.CommandType = CommandType.StoredProcedure;

        //    string customerIds = filter.CustomerName != null && filter.CustomerName.Any()
        //        ? string.Join(",", filter.CustomerName)
        //        : string.Empty;

        //    string customerTypeIds = filter.CustomerType != null && filter.CustomerType.Any()
        //        ? string.Join(",", filter.CustomerType)
        //        : string.Empty;

        //    command.Parameters.Add(new SqlParameter("@CustomerID", string.IsNullOrWhiteSpace(customerIds) ? "" : customerIds.Trim()));
        //    command.Parameters.Add(new SqlParameter("@CustomerTypeID", string.IsNullOrWhiteSpace(customerTypeIds) ? "" : customerTypeIds.Trim()));
        //    command.Parameters.Add(new SqlParameter("@CompanyCode", DBNull.Value));

        //    using var reader = await command.ExecuteReaderAsync();

        //    int sl = 1;
        //    while (await reader.ReadAsync())
        //    {
        //        string GetString(string col)
        //        {
        //            try
        //            {
        //                int idx = reader.GetOrdinal(col);
        //                return reader.IsDBNull(idx) ? string.Empty : reader.GetValue(idx).ToString()?.Trim() ?? string.Empty;
        //            }
        //            catch
        //            {
        //                return string.Empty;
        //            }
        //        }

        //        byte[] GetBytes(string col)
        //        {
        //            try
        //            {
        //                int idx = reader.GetOrdinal(col);
        //                if (reader.IsDBNull(idx)) return null;
        //                return (byte[])reader.GetValue(idx);
        //            }
        //            catch
        //            {
        //                return null;
        //            }
        //        }

        //        var item = new CustomerReportResultDto
        //        {
        //            CustomerId = GetString("CustomerID"),
        //            CustomerName = GetString("CustomerName"),
        //            CustomerAddress = GetString("Address"),
        //            Phone = GetString("Phone"),
        //            Fax = GetString("FAX"),
        //            Email = GetString("Email"),
        //            CustomerPhoto = GetBytes("Photo"),
        //            ContactPerson = GetString("ContactPerson"),
        //            Designation = GetString("Designation"),
        //            CustomerTypeId = GetString("CustomerTypeID"),
        //            CustomerType = GetString("CustomerTypeName")
        //        };

        //        result.Add(item);
        //        sl++;
        //    }

        //    return result;
        //}

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

            string customerIds = filter.CustomerId != null && filter.CustomerId.Any()
                ? string.Join(",", filter.CustomerId)
                : string.Empty;

            string customerTypeIds = filter.CustomerTypeId != null && filter.CustomerTypeId.Any()
                ? string.Join(",", filter.CustomerTypeId)
                : string.Empty;

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
                    CustomerType = GetString("CustomerTypeName")
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

                string companyName = "Formosa Logistics Limited";
                string companyAddress = "21, Hazaribagh, Dhaka-1209";

                string customerHeader = "All";
                if (filter.CustomerId != null && filter.CustomerId.Any())
                {
                    var matched = data.FirstOrDefault(x => !string.IsNullOrEmpty(x.CustomerId))?.CustomerId;
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
                            col.Item().AlignCenter().Text("Customer Report").FontSize(11).Bold().Underline();
                            col.Item().PaddingTop(2).PaddingBottom(4).Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text($"Customer: {customerHeader}").FontSize(8.5f).Bold();
                                row.RelativeItem().AlignRight().Text($"Print Date: {DateTime.Now:dd/MM/yyyy}").FontSize(8);
                            });
                        });

                        // Content Table
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.5f);  // SL
                                cols.RelativeColumn(3.5f);  // Customer ID
                                cols.RelativeColumn(6.0f);  // Name
                                cols.RelativeColumn(7.0f);  // Address
                                cols.RelativeColumn(4.0f);  // Phone
                                cols.RelativeColumn(3.5f);  // Fax
                                cols.RelativeColumn(5.0f);  // Email
                                cols.RelativeColumn(6.5f);  // Contact Person
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
                                        .PaddingHorizontal(2)
                                        .PaddingVertical(3)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(title)
                                        .Bold()
                                        .FontSize(7f)
                                        .WrapAnywhere());
                                }

                                BuildHeaderCell("SL");
                                BuildHeaderCell("Customer ID");
                                BuildHeaderCell("Name");
                                BuildHeaderCell("Address");
                                BuildHeaderCell("Phone");
                                BuildHeaderCell("Fax");
                                BuildHeaderCell("Email");
                                BuildHeaderCell("Contact Person");
                            });

                            // Data Rows
                            int sl = 1;
                            foreach (var d in data)
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
                                        .FontSize(6.5f)
                                        .WrapAnywhere());
                                }

                                BuildDataCell(sl.ToString(), true);
                                BuildDataCell(d.CustomerId, true);
                                BuildDataCell(d.CustomerName);
                                BuildDataCell(d.CustomerAddress);
                                BuildDataCell(d.Phone, true);
                                BuildDataCell(d.Fax, true);
                                BuildDataCell(d.Email);
                                BuildDataCell(d.ContactPerson);

                                sl++;
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
        public async Task<byte[]> GenerateExcelReportAsync(CustomerReportFilterDto filter)
        {
            var data = await GetCustomerReportDataAsync(filter);

            string companyName = "Formosa Logistics Limited";
            string companyAddress = "21, Hazaribagh, Dhaka-1209";

            string customerHeader = "All";
            if (filter.CustomerId != null && filter.CustomerId.Any())
            {
                var matched = data.FirstOrDefault(x => !string.IsNullOrEmpty(x.CustomerId))?.CustomerId;
                if (!string.IsNullOrWhiteSpace(matched))
                    customerHeader = matched;
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Customer Report");

            const int totalCols = 8;

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
            ws.Cell(3, 1).Value = "Customer Report";
            ws.Cell(3, 1).Style.Font.Bold = true;
            ws.Cell(3, 1).Style.Font.FontSize = 12;
            ws.Cell(3, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(3, 1).Style.Font.Underline = XLFontUnderlineValues.Single;
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 4: Customer Header
            var r4 = ws.Range(4, 1, 4, totalCols);
            r4.Merge();
            ws.Cell(4, 1).Value = $"Customer: {customerHeader}";
            ws.Cell(4, 1).Style.Font.Bold = true;
            ws.Cell(4, 1).Style.Font.FontSize = 10;
            ws.Cell(4, 1).Style.Font.FontName = "Times New Roman";
            ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Row 6: Table Headers
            string[] headers = new string[]
            {
        "SL",
        "Customer ID",
        "Name",
        "Address",
        "Phone",
        "Fax",
        "Email",
        "Contact Person"
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
            int sl = 1;
            foreach (var item in data)
            {
                ws.Cell(currRow, 1).Value = sl;
                ws.Cell(currRow, 2).Value = item.CustomerId;
                ws.Cell(currRow, 3).Value = item.CustomerName;
                ws.Cell(currRow, 4).Value = item.CustomerAddress;
                ws.Cell(currRow, 5).Value = item.Phone;
                ws.Cell(currRow, 6).Value = item.Fax;
                ws.Cell(currRow, 7).Value = item.Email;
                ws.Cell(currRow, 8).Value = item.ContactPerson;

                for (int c = 1; c <= totalCols; c++)
                {
                    var cell = ws.Cell(currRow, c);
                    cell.Style.Font.FontSize = 9;
                    cell.Style.Font.FontName = "Times New Roman";
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (c == 1 || c == 2 || c == 5 || c == 6)
                    {
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }
                }

                currRow++;
                sl++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion
    }
}
