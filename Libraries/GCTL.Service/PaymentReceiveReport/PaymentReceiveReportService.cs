using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.Data;
using GCTL.Core.ViewModels.PaymentReceiveReport;
using GCTL.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.PaymentReceiveReport
{
    public class PaymentReceiveReportService : IPaymentReceiveReportService
    {
        private readonly IRepository<CoreCompany> _companyRepo;
        private readonly IWebHostEnvironment? _env;
        private readonly string? _connectionString;
        private byte[]? _logoBytes;
        private bool _logoLoaded;

        public PaymentReceiveReportService(
            IRepository<CoreCompany> companyRepo,
            IConfiguration configuration,
            IWebHostEnvironment? env = null)
        {
            _companyRepo = companyRepo;
            _env = env;
            _connectionString = configuration.GetConnectionString("connection")
                                ?? configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<PaymentReceiveReportViewModel> GetFilterOptionsAsync()
        {
            var vm = new PaymentReceiveReportViewModel();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Customer Types
            var customerTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Customer Types" },
                new SelectListItem { Value = "1", Text = "In House" },
                new SelectListItem { Value = "0", Text = "General" }
            };

            //var dbCustomerTypes = (await connection.QueryAsync<(string Value, string Text)>(
            //    "SELECT CustomerTypeID AS Value, CustomerType AS Text FROM Sales_Def_CustomerType WHERE CustomerType IS NOT NULL ORDER BY CustomerType"
            //)).Select(x => new SelectListItem { Value = x.Value, Text = x.Text }).ToList();

            //foreach (var ct in dbCustomerTypes)
            //{
            //    if (!customerTypes.Any(x => string.Equals(x.Text, ct.Text, StringComparison.OrdinalIgnoreCase)))
            //    {
            //        customerTypes.Add(ct);
            //    }
            //}
            vm.CustomerTypes = customerTypes;

            // Districts
            var districts = (await connection.QueryAsync<(string Value, string Text)>(
                "SELECT District AS Value, District AS Text FROM HRM_Def_District WHERE District IS NOT NULL ORDER BY District"
            )).Select(x => new SelectListItem { Value = x.Value, Text = x.Text }).ToList();
            districts.Insert(0, new SelectListItem { Value = "", Text = "All Districts" });
            vm.Districts = districts;

            // Thanas (common thanas)
            var thanas = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Thanas" },
                new SelectListItem { Value = "Gulshan", Text = "Gulshan" },
                new SelectListItem { Value = "Banani", Text = "Banani" },
                new SelectListItem { Value = "Uttara", Text = "Uttara" },
                new SelectListItem { Value = "Motijheel", Text = "Motijheel" },
                new SelectListItem { Value = "Dhanmondi", Text = "Dhanmondi" },
                new SelectListItem { Value = "Tejgaon", Text = "Tejgaon" },
                new SelectListItem { Value = "Mirpur", Text = "Mirpur" }
            };
            vm.Thanas = thanas;

            // Employees
            var employees = (await connection.QueryAsync<(string Value, string Text)>(
                "SELECT EmployeeID AS Value, RTRIM(LTRIM(ISNULL(FirstName, '') + ' ' + ISNULL(LastName, ''))) AS Text FROM HRM_Employee WHERE FirstName IS NOT NULL ORDER BY FirstName"
            )).Select(x => new SelectListItem { Value = x.Value, Text = x.Text }).ToList();
            employees.Insert(0, new SelectListItem { Value = "", Text = "All Employees" });
            vm.Employees = employees;

            // Payment Types
            var paymentTypes = (await connection.QueryAsync<(string Value, string Text)>(
                "SELECT PaymentTypeID AS Value, PaymentType AS Text FROM Sales_Def_PaymentType WHERE PaymentType IS NOT NULL ORDER BY PaymentType"
            )).Select(x => new SelectListItem { Value = x.Value, Text = x.Text }).ToList();
            paymentTypes.Insert(0, new SelectListItem { Value = "", Text = "All Payment Types" });
            vm.PaymentTypes = paymentTypes;

            // Payment Modes
            var paymentModes = (await connection.QueryAsync<(string Value, string Text)>(
                "SELECT PaymentModeID AS Value, PaymentModeName AS Text FROM Sales_Def_PaymentMode WHERE PaymentModeName IS NOT NULL ORDER BY PaymentModeName"
            )).Select(x => new SelectListItem { Value = x.Value, Text = x.Text }).ToList();
            paymentModes.Insert(0, new SelectListItem { Value = "", Text = "All Payment Modes" });
            vm.PaymentModes = paymentModes;

            // Payment Statuses
            vm.PaymentStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Statuses" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Pending", Text = "Pending" }
            };

            return vm;
        }

        public async Task<(List<PaymentReceiveReportItemDTO> Items, PaymentReceiveReportSummaryDTO Summary)> GetGridDataAsync(PaymentReceiveReportFilter filter)
        {
            string Clean(string val)
            {
                if (string.IsNullOrWhiteSpace(val)) return null;
                var trimmed = val.Trim();
                if (trimmed.StartsWith("--", StringComparison.OrdinalIgnoreCase) || 
                    trimmed.IndexOf("Select", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    string.Equals(trimmed, "All", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return trimmed;
            }

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", Clean(filter.FromDate));
            parameters.Add("@ToDate", Clean(filter.ToDate));
            parameters.Add("@CustomerTypeId", Clean(filter.CustomerTypeId));
            parameters.Add("@District", Clean(filter.District));
            parameters.Add("@Thana", Clean(filter.Thana));
            parameters.Add("@EmployeeId", Clean(filter.EmployeeId));
            parameters.Add("@PaymentTypeId", Clean(filter.PaymentTypeId));
            parameters.Add("@PaymentModeId", Clean(filter.PaymentModeId));
            parameters.Add("@PaymentStatus", Clean(filter.PaymentStatus));

            var items = (await connection.QueryAsync<PaymentReceiveReportItemDTO>(
                "dbo.rpt_proc_PaymentReceiveReport",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            if (!string.IsNullOrWhiteSpace(filter.SelectedPaymentReceiveNos))
            {
                var selectedSet = new HashSet<string>(
                    filter.SelectedPaymentReceiveNos
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()),
                    StringComparer.OrdinalIgnoreCase
                );
                if (selectedSet.Count > 0)
                {
                    items = items.Where(x => selectedSet.Contains(x.PaymentReceiveNo)).ToList();
                    long newSl = 1;
                    foreach (var item in items)
                    {
                        item.SLNo = newSl++;
                    }
                }
            }

            var summary = new PaymentReceiveReportSummaryDTO
            {
                TotalTransactions = items.Count,
                TotalReceivedAmount = items.Sum(x => x.PaymentAmount),
                TotalBankFee = items.Sum(x => x.BankFeeOnlineCharge),
                RegularAmount = items.Where(x => (x.PaymentType ?? "").IndexOf("Regular", StringComparison.OrdinalIgnoreCase) >= 0).Sum(x => x.PaymentAmount),
                AdvanceAmount = items.Where(x => (x.PaymentType ?? "").IndexOf("Advance", StringComparison.OrdinalIgnoreCase) >= 0).Sum(x => x.PaymentAmount)
            };

            return (items, summary);
        }

        public async Task<byte[]> GeneratePdfReportAsync(PaymentReceiveReportFilter filter)
        {
            var (items, summary) = await GetGridDataAsync(filter);
            var company = await _companyRepo.All().AsNoTracking().FirstOrDefaultAsync();
            var logo = GetLogoBytes();

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(12);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            if (logo != null)
                            {
                                r.ConstantItem(60).Height(30).Image(logo, ImageScaling.FitArea);
                            }
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text(company?.CompanyName ?? "GCTL Framework").Bold().FontSize(13);
                                c.Item().AlignCenter().Text(company?.Address1 ?? company?.City ?? "").FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().AlignCenter().Text("Payment Receive Report").Bold().FontSize(11).FontColor(Colors.Black);
                                
                                var dateRangeStr = $"Date: {(!string.IsNullOrEmpty(filter.FromDate) ? filter.FromDate : "Start")} to {(!string.IsNullOrEmpty(filter.ToDate) ? filter.ToDate : "Today")}";
                                c.Item().AlignCenter().Text(dateRangeStr).FontSize(7.5f).Italic();
                            });
                        });
                    });

                    // Content
                    page.Content().PaddingTop(5).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(20);  // SL
                                cd.ConstantColumn(65);  // PR No
                                cd.ConstantColumn(45);  // Date
                                cd.ConstantColumn(50);  // Cust ID
                                cd.RelativeColumn(2);   // Customer Name
                                cd.ConstantColumn(45);  // Type
                                cd.RelativeColumn(2);   // Address
                                cd.RelativeColumn(1.5f);// Employee
                                cd.RelativeColumn(1.5f);// Invoice No/List
                                cd.ConstantColumn(40);  // Pay Type
                                cd.ConstantColumn(40);  // Mode
                                cd.ConstantColumn(55);  // Amount
                                cd.ConstantColumn(40);  // Bank Fee
                                cd.ConstantColumn(45);  // Cheque No
                                cd.ConstantColumn(40);  // Cheque Date
                                cd.RelativeColumn(1.5f);// Deposit Bank AC
                                cd.ConstantColumn(40);  // Maturity
                            });

                            // Table Header
                            table.Header(h =>
                            {
                                void HeaderCell(string text, bool right = false) =>
                                    h.Cell().Background(Colors.White)
                                            .Border(0.5f).BorderColor(Colors.Grey.Darken1)
                                            .Padding(2)
                                            .AlignMiddle()
                                            .AlignCenter()
                                            .Text(text).Bold().FontSize(6.5f);

                                HeaderCell("SL");
                                HeaderCell("PR No");
                                HeaderCell("Date");
                                HeaderCell("Cust ID");
                                HeaderCell("Customer");
                                HeaderCell("Cust Type");
                                HeaderCell("Billing Address");
                                HeaderCell("Employee");
                                HeaderCell("Invoice List");
                                HeaderCell("Type");
                                HeaderCell("Mode");
                                HeaderCell("Amount");
                                HeaderCell("Bank Fee");
                                HeaderCell("Cheque No");
                                HeaderCell("Cheque Dt");
                                HeaderCell("Deposit Bank A/C");
                                HeaderCell("Maturity");
                            });

                            // Rows
                            int sl = 1;
                            foreach (var item in items)
                            {
                                void DataCell(string text, bool right = false, bool center = false) =>
                                    table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                               .Padding(2)
                                               .AlignMiddle()
                                               .Element(c => center ? c.AlignCenter() : (right ? c.AlignRight() : c.AlignLeft()))
                                               .Text(text ?? "").FontSize(6f);

                                DataCell(sl++.ToString(), center: true);
                                DataCell(item.PaymentReceiveNo, center: true);
                                DataCell(item.PaymentReceiveDate, center: true);
                                DataCell(item.CustomerID, center: true);
                                DataCell(item.Customer);
                                DataCell(item.CustomerType, center: true);
                                DataCell(item.BillingAddress);
                                DataCell(item.Employee);
                                DataCell(item.InvoiceNoList);
                                DataCell(item.PaymentType, center: true);
                                DataCell(item.PaymentMode, center: true);
                                DataCell(item.PaymentAmount.ToString("#,##0.00"), right: true);
                                DataCell(item.BankFeeOnlineCharge > 0 ? item.BankFeeOnlineCharge.ToString("#,##0.00") : "-", right: true);
                                DataCell(item.ChequeNo, center: true);
                                DataCell(item.ChequeDate, center: true);
                                DataCell(item.DepositBankAccount);
                                DataCell(item.MaturityDate, center: true);
                            }

                            // Summary Row
                            table.Cell().ColumnSpan(11).Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4)
                                 .Padding(3).AlignRight().Text("Total:").Bold().FontSize(7f);
                            
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4)
                                 .Padding(3).AlignRight().Text(summary.TotalReceivedAmount.ToString("#,##0.00")).Bold().FontSize(7f);

                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4)
                                 .Padding(3).AlignRight().Text(summary.TotalBankFee > 0 ? summary.TotalBankFee.ToString("#,##0.00") : "-").Bold().FontSize(7f);

                            table.Cell().ColumnSpan(4).Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4)
                                 .Padding(3).Text("");
                        });
                    });

                    // Footer
                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text($"Printed: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(6).FontColor(Colors.Grey.Medium);
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportExcelAsync(PaymentReceiveReportFilter filter)
        {
            var (items, summary) = await GetGridDataAsync(filter);
            var company = await _companyRepo.All().AsNoTracking().FirstOrDefaultAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Payment Receive Report");

            // Title Block
            ws.Cell(1, 1).Value = company?.CompanyName ?? "GCTL Framework";
            ws.Range(1, 1, 1, 17).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Payment Receive Report";
            ws.Range(2, 1, 2, 17).Merge().Style.Font.SetBold().Font.SetFontSize(12).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var dateStr = $"Date: {(!string.IsNullOrEmpty(filter.FromDate) ? filter.FromDate : "Start")} to {(!string.IsNullOrEmpty(filter.ToDate) ? filter.ToDate : "Today")}";
            ws.Cell(3, 1).Value = dateStr;
            ws.Range(3, 1, 3, 17).Merge().Style.Font.SetItalic().Font.SetFontSize(9).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Table Header
            string[] headers = new[]
            {
                "SL No.", "Payment Receive No.", "Customer ID", "Customer", "Customer Type",
                "Billing Address", "Employee", "Invoice No./List", "Payment Type", "Payment Mode",
                "Payment Amount", "Bank Fee/Online Charge", "Cheque No.", "Cheque Date",
                "Cheque Bank", "Deposit Bank Account", "Maturity Date"
            };

            int startRow = 5;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(startRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold();
                cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAEAEA"));
                cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            }

            int row = startRow + 1;
            int sl = 1;
            foreach (var item in items)
            {
                ws.Cell(row, 1).SetValue(sl++).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 2).SetValue(item.PaymentReceiveNo).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 3).SetValue(item.CustomerID).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 4).SetValue(item.Customer);
                ws.Cell(row, 5).SetValue(item.CustomerType).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 6).SetValue(item.BillingAddress);
                ws.Cell(row, 7).SetValue(item.Employee);
                ws.Cell(row, 8).SetValue(item.InvoiceNoList);
                ws.Cell(row, 9).SetValue(item.PaymentType).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 10).SetValue(item.PaymentMode).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                
                var cellAmt = ws.Cell(row, 11);
                cellAmt.SetValue(item.PaymentAmount);
                cellAmt.Style.NumberFormat.Format = "#,##0.00";
                cellAmt.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                var cellFee = ws.Cell(row, 12);
                cellFee.SetValue(item.BankFeeOnlineCharge);
                cellFee.Style.NumberFormat.Format = "#,##0.00";
                cellFee.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                ws.Cell(row, 13).SetValue(item.ChequeNo).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 14).SetValue(item.ChequeDate).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 15).SetValue(item.ChequeBank);
                ws.Cell(row, 16).SetValue(item.DepositBankAccount);
                ws.Cell(row, 17).SetValue(item.MaturityDate).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Range(row, 1, row, 17).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Range(row, 1, row, 17).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            // Total row
            ws.Cell(row, 1).Value = "Total:";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            
            var totalAmt = ws.Cell(row, 11);
            totalAmt.SetValue(summary.TotalReceivedAmount);
            totalAmt.Style.Font.SetBold();
            totalAmt.Style.NumberFormat.Format = "#,##0.00";
            totalAmt.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            var totalFee = ws.Cell(row, 12);
            totalFee.SetValue(summary.TotalBankFee);
            totalFee.Style.Font.SetBold();
            totalFee.Style.NumberFormat.Format = "#,##0.00";
            totalFee.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Range(row, 1, row, 17).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5"));
            ws.Range(row, 1, row, 17).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(row, 1, row, 17).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Columns().AdjustToContents(8.0, 45.0);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                if (_env?.WebRootPath != null)
                {
                    string path = Path.Combine(_env.WebRootPath, "images", "logo.png");
                    if (File.Exists(path))
                    {
                        _logoBytes = File.ReadAllBytes(path);
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return _logoBytes;
        }
    }
}

