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
using GCTL.Core.ViewModels.Common;
using GCTL.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCTL.Service.CustomerAgingReport
{
    public class CustomerAgingReportService : ICustomerAgingReportService
    {
        #region Private Fields
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<CoreCompany> _companyRepo;
        private readonly IRepository<CoreBranch> _branchRepo;
        private readonly IWebHostEnvironment? _env;
        private readonly string? _connectionString;
        private byte[]? _logoBytes;
        private bool _logoLoaded;
        #endregion

        #region Constructor
        public CustomerAgingReportService(
            IRepository<SalesCustomer> customerRepo,
            IRepository<CoreCompany> companyRepo,
            IRepository<CoreBranch> branchRepo,
            IConfiguration configuration,
            IWebHostEnvironment? env = null)
        {
            _customerRepo = customerRepo;
            _companyRepo = companyRepo;
            _branchRepo = branchRepo;
            _env = env;
            _connectionString = configuration.GetConnectionString("connection")
                                ?? configuration.GetConnectionString("DefaultConnection");
        }
        #endregion

        #region Filter Options
        public async Task<CustomerAgingFilterOptionsDTO> GetFilterOptionsAsync()
        {
            var options = new CustomerAgingFilterOptionsDTO();
            try
            {
                options.Customers = await _customerRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.CustomerId) && !string.IsNullOrEmpty(x.CustomerName))
                    .OrderBy(x => x.CustomerName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.CustomerId,
                        Name = x.CustomerName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Customer Aging filter options: {ex.Message}");
            }
            return options;
        }
        #endregion

        #region Logo Loader
        private byte[]? GetLogoBytes()
        {
            if (_logoLoaded) return _logoBytes;
            _logoLoaded = true;

            try
            {
                var candidatePaths = new List<string?>
                {
                    _env?.WebRootPath != null ? Path.Combine(_env.WebRootPath, "images", "fermosa-wX200.png") : null,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "fermosa-wX200.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "UIs", "GCTL.UI.Core", "wwwroot", "images", "fermosa-wX200.png"),
                    @"D:\GCTL\GCTL\Project\CNF\GCTL.Framework\UIs\GCTL.UI.Core\wwwroot\images\fermosa-wX200.png"
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
                // Fallback gracefully
            }

            return _logoBytes;
        }
        #endregion

        #region Get Report Data
        public async Task<List<CustomerAgingReportItemDTO>> GetReportDataAsync(CustomerAgingFilter filter)
        {
            var list = new List<CustomerAgingReportItemDTO>();
            if (string.IsNullOrEmpty(_connectionString)) return list;

            try
            {
                DateTime agingDate = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(filter.AgingDate) &&
                    DateTime.TryParseExact(filter.AgingDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    agingDate = dt.Date;
                }
                else if (!string.IsNullOrWhiteSpace(filter.AgingDate) &&
                    DateTime.TryParse(filter.AgingDate.Trim(), out var parsedDt))
                {
                    agingDate = parsedDt.Date;
                }

                bool isBillWise = filter.ReportType?.Contains("Bill", StringComparison.OrdinalIgnoreCase) == true;
                string spName = isBillWise ? "rpt_proc_BillWiseCustomerAgingReport" : "rpt_proc_CustomerAgingReport";

                await using var conn = new SqlConnection(_connectionString);
                var p = new DynamicParameters();
                p.Add("@AgingDate", agingDate, DbType.DateTime);
                p.Add("@CustomerID", filter.CustomerId ?? string.Empty);

                var rows = (await conn.QueryAsync<dynamic>(spName, p, commandType: CommandType.StoredProcedure, commandTimeout: 180)).ToList();

                int sl = 1;
                foreach (var r in rows)
                {
                    var dict = (IDictionary<string, object>)r;
                    var item = new CustomerAgingReportItemDTO
                    {
                        SlNo = sl++,
                        CustomerName = dict.ContainsKey("CustomerName") && dict["CustomerName"] != null ? dict["CustomerName"].ToString()! : "",
                        CustomerNo = dict.ContainsKey("CustomerNo") && dict["CustomerNo"] != null ? dict["CustomerNo"].ToString()! : "",
                        BillNo = dict.ContainsKey("BillNo") && dict["BillNo"] != null ? dict["BillNo"].ToString() : null,
                        Ag_0_30 = dict.ContainsKey("Ag_0_30") && dict["Ag_0_30"] != null ? Convert.ToDecimal(dict["Ag_0_30"]) : 0m,
                        Ag_31_60 = dict.ContainsKey("Ag_31_60") && dict["Ag_31_60"] != null ? Convert.ToDecimal(dict["Ag_31_60"]) : 0m,
                        Ag_61_90 = dict.ContainsKey("Ag_61_90") && dict["Ag_61_90"] != null ? Convert.ToDecimal(dict["Ag_61_90"]) : 0m,
                        Ag_91_120 = dict.ContainsKey("Ag_91_120") && dict["Ag_91_120"] != null ? Convert.ToDecimal(dict["Ag_91_120"]) : 0m,
                        Ag_121_150 = dict.ContainsKey("Ag_121_150") && dict["Ag_121_150"] != null ? Convert.ToDecimal(dict["Ag_121_150"]) : 0m,
                        Ag_151_180 = dict.ContainsKey("Ag_151_180") && dict["Ag_151_180"] != null ? Convert.ToDecimal(dict["Ag_151_180"]) : 0m,
                        Ag_181_210 = dict.ContainsKey("Ag_181_210") && dict["Ag_181_210"] != null ? Convert.ToDecimal(dict["Ag_181_210"]) : 0m,
                        Ag_211_240 = dict.ContainsKey("Ag_211_240") && dict["Ag_211_240"] != null ? Convert.ToDecimal(dict["Ag_211_240"]) : 0m,
                        Ag_241_270 = dict.ContainsKey("Ag_241_270") && dict["Ag_241_270"] != null ? Convert.ToDecimal(dict["Ag_241_270"]) : 0m,
                        Ag_271_300 = dict.ContainsKey("Ag_271_300") && dict["Ag_271_300"] != null ? Convert.ToDecimal(dict["Ag_271_300"]) : 0m,
                        Ag_301_330 = dict.ContainsKey("Ag_301_330") && dict["Ag_301_330"] != null ? Convert.ToDecimal(dict["Ag_301_330"]) : 0m,
                        Ag_331_360 = dict.ContainsKey("Ag_331_360") && dict["Ag_331_360"] != null ? Convert.ToDecimal(dict["Ag_331_360"]) : 0m,
                        Ag_Over360 = dict.ContainsKey("Ag_Over360") && dict["Ag_Over360"] != null ? Convert.ToDecimal(dict["Ag_Over360"]) : 0m,
                        Total = dict.ContainsKey("Total") && dict["Total"] != null ? Convert.ToDecimal(dict["Total"]) : 0m,
                        NameofDate = dict.ContainsKey("NameofDate") && dict["NameofDate"] != null ? dict["NameofDate"].ToString()! : $"Debtors Aging as on {agingDate:dd-MMM-yyyy}"
                    };

                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Customer Aging Data: {ex.Message}");
            }

            return list;
        }
        #endregion

        #region Get Grid Data (DataTables)
        public async Task<CustomerAgingGridResult> GetGridDataAsync(CustomerAgingFilter filter)
        {
            var allData = await GetReportDataAsync(filter);

            var query = allData.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(x =>
                    x.CustomerName.ToLower().Contains(term) ||
                    x.CustomerNo.ToLower().Contains(term) ||
                    (!string.IsNullOrEmpty(x.BillNo) && x.BillNo.ToLower().Contains(term)));
            }

            var filteredList = query.ToList();
            int totalRecords = allData.Count;
            int filteredRecords = filteredList.Count;

            int start = filter.Start ?? 0;
            int length = (filter.Length.HasValue && filter.Length > 0) ? filter.Length.Value : 10;
            var paged = (length == -1) ? filteredList : filteredList.Skip(start).Take(length).ToList();

            var summary = new CustomerAgingSummaryDTO
            {
                TotalCustomers = allData.Select(x => x.CustomerName).Distinct().Count(),
                TotalBills = allData.Count(x => !string.IsNullOrEmpty(x.BillNo)),
                TotalOutstanding = allData.Sum(x => x.Total),
                Total_0_30 = allData.Sum(x => x.Ag_0_30),
                Total_31_90 = allData.Sum(x => x.Ag_31_60 + x.Ag_61_90),
                Total_91_180 = allData.Sum(x => x.Ag_91_120 + x.Ag_121_150 + x.Ag_151_180),
                Total_Over180 = allData.Sum(x => x.Ag_181_210 + x.Ag_211_240 + x.Ag_241_270 + x.Ag_271_300 + x.Ag_301_330 + x.Ag_331_360 + x.Ag_Over360)
            };

            return new CustomerAgingGridResult
            {
                Draw = filter.Draw ?? 1,
                RecordsTotal = totalRecords,
                RecordsFiltered = filteredRecords,
                Data = paged,
                Summary = summary
            };
        }
        #endregion

        #region PDF Report Generation (QuestPDF)
        public async Task<byte[]> GeneratePdfReportAsync(CustomerAgingFilter filter, string printUser = "")
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var dataList = await GetReportDataAsync(filter);
            var logoBytes = GetLogoBytes();

            bool isBillWise = filter.ReportType?.Contains("Bill", StringComparison.OrdinalIgnoreCase) == true;
            string reportTitle = isBillWise ? "Bill Wise Debtors Aging Report" : "Debtors Aging Summary Report";
            string asOnDate = dataList.FirstOrDefault()?.NameofDate ?? $"Debtors Aging as on {DateTime.Today:dd-MMM-yyyy}";

            decimal tot030 = dataList.Sum(x => x.Ag_0_30);
            decimal tot3160 = dataList.Sum(x => x.Ag_31_60);
            decimal tot6190 = dataList.Sum(x => x.Ag_61_90);
            decimal tot91120 = dataList.Sum(x => x.Ag_91_120);
            decimal tot121150 = dataList.Sum(x => x.Ag_121_150);
            decimal tot151180 = dataList.Sum(x => x.Ag_151_180);
            decimal tot181210 = dataList.Sum(x => x.Ag_181_210);
            decimal tot211240 = dataList.Sum(x => x.Ag_211_240);
            decimal tot241270 = dataList.Sum(x => x.Ag_241_270);
            decimal tot271300 = dataList.Sum(x => x.Ag_271_300);
            decimal tot301330 = dataList.Sum(x => x.Ag_301_330);
            decimal tot331360 = dataList.Sum(x => x.Ag_331_360);
            decimal totOver360 = dataList.Sum(x => x.Ag_Over360);
            decimal grandTotal = dataList.Sum(x => x.Total);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(12);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8f));

                    // Watermark
                    page.Background().AlignCenter().AlignMiddle().Rotate(-30).Text("FORMOSA LOGISTICS").FontSize(48).FontColor(Colors.Grey.Lighten3).SemiBold();

                    // Header
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(75).AlignTop().Image(logoBytes).FitArea();
                            }
                            else
                            {
                                row.ConstantItem(75);
                            }

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Formosa Logistics Limited").FontSize(13).Bold();
                                c.Item().AlignCenter().Text("21, Hazaribagh, Dhaka-1209").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignCenter().Text(reportTitle).FontSize(11).Bold().Underline();
                                c.Item().AlignCenter().PaddingTop(2f).Text(asOnDate).FontSize(8.5f).SemiBold();
                            });

                            row.ConstantItem(75);
                        });
                        headerCol.Item().PaddingBottom(4);
                    });

                    // Content Table
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22); // SN
                            if (isBillWise)
                            {
                                cols.RelativeColumn(3.2f); // Customer Name
                                cols.RelativeColumn(2.0f); // Bill No
                            }
                            else
                            {
                                cols.RelativeColumn(4.0f); // Customer Name
                            }
                            cols.RelativeColumn(1.4f); // 0-30
                            cols.RelativeColumn(1.4f); // 31-60
                            cols.RelativeColumn(1.4f); // 61-90
                            cols.RelativeColumn(1.4f); // 91-120
                            cols.RelativeColumn(1.4f); // 121-150
                            cols.RelativeColumn(1.4f); // 151-180
                            cols.RelativeColumn(1.4f); // 181-210
                            cols.RelativeColumn(1.4f); // 211-240
                            cols.RelativeColumn(1.4f); // 241-270
                            cols.RelativeColumn(1.4f); // 271-300
                            cols.RelativeColumn(1.4f); // 301-330
                            cols.RelativeColumn(1.4f); // 331-360
                            cols.RelativeColumn(1.5f); // Over 360
                            cols.RelativeColumn(1.8f); // Total
                        });

                        // Header Row
                        table.Header(th =>
                        {
                            void StyleHeader(IContainer c, string title, bool alignRight = false)
                            {
                                var block = c.Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten3).Padding(2.5f);
                                if (alignRight)
                                    block.AlignRight().Text(title).Bold().FontSize(7.5f);
                                else
                                    block.AlignCenter().Text(title).Bold().FontSize(7.5f);
                            }

                            StyleHeader(th.Cell(), "SN");
                            StyleHeader(th.Cell(), "Customer Name", false);
                            if (isBillWise)
                            {
                                StyleHeader(th.Cell(), "Bill No", false);
                            }
                            StyleHeader(th.Cell(), "0-30", true);
                            StyleHeader(th.Cell(), "31-60", true);
                            StyleHeader(th.Cell(), "61-90", true);
                            StyleHeader(th.Cell(), "91-120", true);
                            StyleHeader(th.Cell(), "121-150", true);
                            StyleHeader(th.Cell(), "151-180", true);
                            StyleHeader(th.Cell(), "181-210", true);
                            StyleHeader(th.Cell(), "211-240", true);
                            StyleHeader(th.Cell(), "241-270", true);
                            StyleHeader(th.Cell(), "271-300", true);
                            StyleHeader(th.Cell(), "301-330", true);
                            StyleHeader(th.Cell(), "331-360", true);
                            StyleHeader(th.Cell(), "> 360", true);
                            StyleHeader(th.Cell(), "Total", true);
                        });

                        // Data Rows
                        int sn = 1;
                        foreach (var item in dataList)
                        {
                            void StyleCell(IContainer c, string text, bool alignRight = false, bool isBold = false)
                            {
                                var cell = c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2f);
                                var txt = alignRight ? cell.AlignRight().Text(text) : cell.AlignLeft().Text(text);
                                if (isBold) txt.Bold();
                                txt.FontSize(7.2f);
                            }

                            string Fmt(decimal val) => val == 0m ? "0.00" : val.ToString("N2");

                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2f).AlignCenter().Text(sn++.ToString()).FontSize(7.2f);
                            StyleCell(table.Cell(), item.CustomerName);
                            if (isBillWise)
                            {
                                StyleCell(table.Cell(), item.BillNo ?? "-");
                            }
                            StyleCell(table.Cell(), Fmt(item.Ag_0_30), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_31_60), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_61_90), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_91_120), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_121_150), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_151_180), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_181_210), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_211_240), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_241_270), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_271_300), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_301_330), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_331_360), true);
                            StyleCell(table.Cell(), Fmt(item.Ag_Over360), true);
                            StyleCell(table.Cell(), Fmt(item.Total), true, isBold: true);
                        }

                        // Grand Total Row
                        void StyleTotal(IContainer c, string text, bool alignRight = true)
                        {
                            var cell = c.Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(2.5f);
                            var txt = alignRight ? cell.AlignRight().Text(text) : cell.AlignCenter().Text(text);
                            txt.Bold().FontSize(7.5f);
                        }

                        string FmtT(decimal val) => val.ToString("N2");

                        table.Cell().ColumnSpan((uint)(isBillWise ? 3 : 2)).Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(2.5f).AlignCenter().Text("GRAND TOTAL").Bold().FontSize(7.5f);
                        StyleTotal(table.Cell(), FmtT(tot030));
                        StyleTotal(table.Cell(), FmtT(tot3160));
                        StyleTotal(table.Cell(), FmtT(tot6190));
                        StyleTotal(table.Cell(), FmtT(tot91120));
                        StyleTotal(table.Cell(), FmtT(tot121150));
                        StyleTotal(table.Cell(), FmtT(tot151180));
                        StyleTotal(table.Cell(), FmtT(tot181210));
                        StyleTotal(table.Cell(), FmtT(tot211240));
                        StyleTotal(table.Cell(), FmtT(tot241270));
                        StyleTotal(table.Cell(), FmtT(tot271300));
                        StyleTotal(table.Cell(), FmtT(tot301330));
                        StyleTotal(table.Cell(), FmtT(tot331360));
                        StyleTotal(table.Cell(), FmtT(totOver360));
                        StyleTotal(table.Cell(), FmtT(grandTotal));
                    });

                    // Footer
                    page.Footer().Column(footCol =>
                    {
                        footCol.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text($"Print Date Time: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(7.5f);
                            row.RelativeItem().AlignCenter().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                            row.RelativeItem().AlignRight().Text($"Print By: {(!string.IsNullOrWhiteSpace(printUser) ? printUser : "Admin")}").FontSize(7.5f);
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }
        #endregion

        #region Excel Export (ClosedXML)
        public async Task<byte[]> GenerateExcelReportAsync(CustomerAgingFilter filter)
        {
            var dataList = await GetReportDataAsync(filter);
            bool isBillWise = filter.ReportType?.Contains("Bill", StringComparison.OrdinalIgnoreCase) == true;
            string reportTitle = isBillWise ? "Bill Wise Debtors Aging Report" : "Debtors Aging Summary Report";
            string asOnDate = dataList.FirstOrDefault()?.NameofDate ?? $"Debtors Aging as on {DateTime.Today:dd-MMM-yyyy}";

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Aging Report");

            // Title Header
            int colSpan = isBillWise ? 16 : 15;
            ws.Cell(1, 1).Value = "Formosa Logistics Limited";
            ws.Range(1, 1, 1, colSpan).Merge().Style.Font.SetBold(true).Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "21, Hazaribagh, Dhaka-1209";
            ws.Range(2, 1, 2, colSpan).Merge().Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.DarkGray).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = reportTitle;
            ws.Range(3, 1, 3, colSpan).Merge().Style.Font.SetBold(true).Font.SetFontSize(11).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(4, 1).Value = asOnDate;
            ws.Range(4, 1, 4, colSpan).Merge().Style.Font.SetFontSize(9.5).Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Table Header (Row 6)
            int row = 6;
            int c = 1;
            ws.Cell(row, c++).Value = "SN";
            ws.Cell(row, c++).Value = "Customer Name";
            if (isBillWise)
            {
                ws.Cell(row, c++).Value = "Bill No";
            }
            ws.Cell(row, c++).Value = "0-30";
            ws.Cell(row, c++).Value = "31-60";
            ws.Cell(row, c++).Value = "61-90";
            ws.Cell(row, c++).Value = "91-120";
            ws.Cell(row, c++).Value = "121-150";
            ws.Cell(row, c++).Value = "151-180";
            ws.Cell(row, c++).Value = "181-210";
            ws.Cell(row, c++).Value = "211-240";
            ws.Cell(row, c++).Value = "241-270";
            ws.Cell(row, c++).Value = "271-300";
            ws.Cell(row, c++).Value = "301-330";
            ws.Cell(row, c++).Value = "331-360";
            ws.Cell(row, c++).Value = "> 360";
            ws.Cell(row, c++).Value = "Total";

            var headerRange = ws.Range(row, 1, row, colSpan);
            headerRange.Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));
            headerRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            headerRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            row++;
            int sn = 1;
            foreach (var item in dataList)
            {
                c = 1;
                ws.Cell(row, c++).Value = sn++;
                ws.Cell(row, c++).Value = item.CustomerName;
                if (isBillWise)
                {
                    ws.Cell(row, c++).Value = item.BillNo ?? "";
                }
                ws.Cell(row, c++).Value = item.Ag_0_30;
                ws.Cell(row, c++).Value = item.Ag_31_60;
                ws.Cell(row, c++).Value = item.Ag_61_90;
                ws.Cell(row, c++).Value = item.Ag_91_120;
                ws.Cell(row, c++).Value = item.Ag_121_150;
                ws.Cell(row, c++).Value = item.Ag_151_180;
                ws.Cell(row, c++).Value = item.Ag_181_210;
                ws.Cell(row, c++).Value = item.Ag_211_240;
                ws.Cell(row, c++).Value = item.Ag_241_270;
                ws.Cell(row, c++).Value = item.Ag_271_300;
                ws.Cell(row, c++).Value = item.Ag_301_330;
                ws.Cell(row, c++).Value = item.Ag_331_360;
                ws.Cell(row, c++).Value = item.Ag_Over360;
                ws.Cell(row, c++).Value = item.Total;

                // Format number columns
                int startNumCol = isBillWise ? 4 : 3;
                for (int colIdx = startNumCol; colIdx <= colSpan; colIdx++)
                {
                    ws.Cell(row, colIdx).Style.NumberFormat.Format = "#,##0.00";
                }

                ws.Range(row, 1, row, colSpan).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Range(row, 1, row, colSpan).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            // Grand Total Row
            int totalTitleCols = isBillWise ? 3 : 2;
            ws.Cell(row, 1).Value = "GRAND TOTAL";
            ws.Range(row, 1, row, totalTitleCols).Merge().Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            c = totalTitleCols + 1;
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_0_30);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_31_60);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_61_90);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_91_120);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_121_150);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_151_180);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_181_210);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_211_240);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_241_270);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_271_300);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_301_330);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_331_360);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Ag_Over360);
            ws.Cell(row, c++).Value = dataList.Sum(x => x.Total);

            var totalRowRange = ws.Range(row, 1, row, colSpan);
            totalRowRange.Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));
            totalRowRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            totalRowRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            for (int colIdx = totalTitleCols + 1; colIdx <= colSpan; colIdx++)
            {
                ws.Cell(row, colIdx).Style.NumberFormat.Format = "#,##0.00";
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion

        #region Export Action Dispatcher
        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(CustomerAgingFilter filter, string format, string printUser = "")
        {
            var fmt = (format ?? "PDF").Trim().ToUpper();
            bool isBillWise = filter.ReportType?.Contains("Bill", StringComparison.OrdinalIgnoreCase) == true;
            string prefix = isBillWise ? "BillWiseCustomerAgingReport" : "CustomerAgingReport";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (fmt == "EXCEL" || fmt == "XLSX")
            {
                var bytes = await GenerateExcelReportAsync(filter);
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{prefix}_{timestamp}.xlsx");
            }
            else
            {
                var bytes = await GeneratePdfReportAsync(filter, printUser);
                return (bytes, "application/pdf", $"{prefix}_{timestamp}.pdf");
            }
        }
        #endregion
    }
}

