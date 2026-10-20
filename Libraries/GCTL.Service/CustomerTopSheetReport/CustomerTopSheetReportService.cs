using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

namespace GCTL.Service.CustomerTopSheetReport
{
    public class CustomerTopSheetReportService : ICustomerTopSheetReportService
    {
        #region Private Fields
        private readonly IRepository<CoreCompany> _companyRepo;
        private readonly IRepository<CoreBranch> _branchRepo;
        private readonly IRepository<SalesCustomer> _customerRepo;
        private readonly IRepository<CfDefExpenseType> _shipmentModeRepo;
        private readonly IRepository<TransportModeType> _transportModeTypeRepo;
        private readonly IRepository<CfBillEntry> _billEntryRepo;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IWebHostEnvironment? _env;
        #endregion

        #region Constructor
        public CustomerTopSheetReportService(
            IRepository<CoreCompany> companyRepo,
            IRepository<CoreBranch> branchRepo,
            IRepository<SalesCustomer> customerRepo,
            IRepository<CfDefExpenseType> shipmentModeRepo,
            IRepository<TransportModeType> transportModeTypeRepo,
            IRepository<CfBillEntry> billEntryRepo,
            IConfiguration configuration,
            IWebHostEnvironment? env = null)
        {
            _companyRepo = companyRepo;
            _branchRepo = branchRepo;
            _customerRepo = customerRepo;
            _shipmentModeRepo = shipmentModeRepo;
            _transportModeTypeRepo = transportModeTypeRepo;
            _billEntryRepo = billEntryRepo;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? string.Empty;
            _env = env;
        }
        #endregion

        #region Filter Options & Cascading
        public async Task<CustomerTopSheetFilterOptionsDTO> GetFilterOptionsAsync(
            string? fromDate = null, string? toDate = null, string? companyCode = null, string? branchCode = null)
        {
            var options = new CustomerTopSheetFilterOptionsDTO();

            try
            {
                options.Companies = await _companyRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.CompanyCode) && !string.IsNullOrEmpty(x.CompanyName))
                    .OrderBy(x => x.CompanyName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.CompanyCode,
                        Name = x.CompanyName
                    })
                    .ToListAsync();

                options.Branches = await _branchRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.BranchCode) && !string.IsNullOrEmpty(x.BranchName))
                    .OrderBy(x => x.BranchName)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.BranchCode,
                        Name = x.BranchName
                    })
                    .ToListAsync();

                // Load Shipment Modes from TransportModeType (e.g. 001 Air, 002 Sea, etc.)
                var transportModes = await _transportModeTypeRepo.All()
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.TransportModeId) && !string.IsNullOrEmpty(x.TransportMode))
                    .OrderBy(x => x.TransportMode)
                    .Select(x => new CommonSelectModel
                    {
                        Code = x.TransportModeId.Trim(),
                        Name = x.TransportMode.Trim()
                    })
                    .ToListAsync();

                if (transportModes.Any())
                {
                    options.ShipmentModes = transportModes;
                }
                else
                {
                    options.ShipmentModes = await _shipmentModeRepo.All()
                        .AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.ExpenseTypeId))
                        .OrderBy(x => x.ExpenseType)
                        .Select(x => new CommonSelectModel
                        {
                            Code = x.ExpenseTypeId,
                            Name = !string.IsNullOrEmpty(x.ExpenseType) ? x.ExpenseType : x.ExpenseTypeId
                        })
                        .ToListAsync();
                }

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

                options.JobNos = await GetJobNosAsync(null, null, fromDate, toDate);
                options.BillNos = await GetBillNosAsync(null, null, fromDate, toDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFilterOptionsAsync: {ex.Message}");
            }

            return options;
        }

        public async Task<List<CommonSelectModel>> GetBranchesAsync(string? companyCode)
        {
            try
            {
                var query = _branchRepo.All().AsNoTracking();
                if (!string.IsNullOrEmpty(companyCode) && !companyCode.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.CompanyCode == companyCode);
                }

                return await query
                    .Where(b => !string.IsNullOrEmpty(b.BranchCode) && !string.IsNullOrEmpty(b.BranchName))
                    .OrderBy(b => b.BranchName)
                    .Select(b => new CommonSelectModel
                    {
                        Code = b.BranchCode,
                        Name = b.BranchName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBranchesAsync: {ex.Message}");
                return new List<CommonSelectModel>();
            }
        }

        public async Task<List<string>> GetJobNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                var query = _billEntryRepo.All().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    var custIds = customerId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (custIds.Length > 0)
                        query = query.Where(x => x.CustomerId != null && custIds.Contains(x.CustomerId));
                }

                if (!string.IsNullOrWhiteSpace(shipmentModeId))
                {
                    var modeIds = shipmentModeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (modeIds.Length > 0)
                        query = query.Where(x => x.ExpenseTypeId != null && modeIds.Contains(x.ExpenseTypeId));
                }

                if (!string.IsNullOrWhiteSpace(fromDate) &&
                    DateTime.TryParseExact(fromDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fDate))
                {
                    query = query.Where(x => (x.BillDate ?? DateTime.MinValue) >= fDate);
                }

                if (!string.IsNullOrWhiteSpace(toDate) &&
                    DateTime.TryParseExact(toDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tDate))
                {
                    var tDateEnd = tDate.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => (x.BillDate ?? DateTime.MaxValue) <= tDateEnd);
                }

                return await query
                    .Where(x => !string.IsNullOrEmpty(x.JobNo))
                    .OrderByDescending(x => x.BillDate)
                    .Select(x => x.JobNo)
                    .Distinct()
                    .Take(1000)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Job Nos: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetBillNosAsync(string? customerId, string? shipmentModeId, string? fromDate, string? toDate)
        {
            try
            {
                var query = _billEntryRepo.All().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    var custIds = customerId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (custIds.Length > 0)
                        query = query.Where(x => x.CustomerId != null && custIds.Contains(x.CustomerId));
                }

                if (!string.IsNullOrWhiteSpace(shipmentModeId))
                {
                    var modeIds = shipmentModeId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (modeIds.Length > 0)
                    {
                        var matchedExpTypeIds = await _shipmentModeRepo.All().AsNoTracking()
                            .Where(et => (et.TransportModeTypeId != null && modeIds.Contains(et.TransportModeTypeId.Trim())) || modeIds.Contains(et.ExpenseTypeId))
                            .Select(et => et.ExpenseTypeId)
                            .ToListAsync();

                        query = query.Where(x => x.ExpenseTypeId != null && matchedExpTypeIds.Contains(x.ExpenseTypeId));
                    }
                }

                var fDate = ParseDate(fromDate);
                if (fDate.HasValue)
                {
                    query = query.Where(x => (x.BillDate ?? DateTime.MinValue) >= fDate.Value);
                }

                var tDate = ParseDate(toDate);
                if (tDate.HasValue)
                {
                    var tDateEnd = tDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => (x.BillDate ?? DateTime.MaxValue) <= tDateEnd);
                }

                return await query
                    .Where(x => !string.IsNullOrEmpty(x.BillNo))
                    .OrderByDescending(x => x.BillDate)
                    .Select(x => x.BillNo)
                    .Distinct()
                    .Take(1000)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Bill Nos: {ex.Message}");
                return new List<string>();
            }
        }
        #endregion

        #region Fetch Report Data via High-Speed Stored Procedure
        public async Task<List<CustomerTopSheetCustomerDTO>> GetReportDataAsync(CustomerTopSheetFilter filter)
        {
            var results = new List<CustomerTopSheetCustomerDTO>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);

                DateTime? fromDate = ParseDate(filter.FromDate);
                DateTime? toDate = ParseDate(filter.ToDate);

                var p = new DynamicParameters();
                p.Add("@CompanyCode", filter.CompanyCode ?? string.Empty);
                p.Add("@BranchCode", filter.BranchCode ?? string.Empty);
                p.Add("@FromDate", fromDate, DbType.DateTime);
                p.Add("@ToDate", toDate, DbType.DateTime);
                p.Add("@CustomerId", filter.CustomerId ?? string.Empty);
                p.Add("@ShipmentModeId", filter.ShipmentModeId ?? string.Empty);
                p.Add("@JobNo", filter.JobNo ?? string.Empty);
                p.Add("@BillNo", filter.BillNo ?? string.Empty);

                using var multi = await conn.QueryMultipleAsync(
                    "dbo.Rpt_proc_CustomerTopSheetReport",
                    p,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120);

                var customers = (await multi.ReadAsync<CustomerTopSheetCustomerDTO>()).ToList();
                var billItems = (await multi.ReadAsync<CustomerTopSheetBillItemDTO>()).ToList();

                var billsByCustomer = billItems
                    .GroupBy(b => b.CustomerId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var cust in customers)
                {
                    if (billsByCustomer.TryGetValue(cust.CustomerId, out var bills))
                    {
                        cust.Bills = bills;
                        int sl = 1;
                        foreach (var b in cust.Bills)
                        {
                            b.SlNo = sl++;
                        }
                    }

                    cust.AmountInWords = ConvertNumberToWords(cust.GrandTotalAmount);
                    results.Add(cust);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CustomerTopSheetReportService.GetReportDataAsync: {ex.Message}");
            }

            return results;
        }
        #endregion

        #region Grid Data & Dashboard Summary (For UI Screen)
        public async Task<CustomerTopSheetGridResult> GetGridDataAsync(CustomerTopSheetFilter filter)
        {
            var result = new CustomerTopSheetGridResult();
            try
            {
                var customers = await GetReportDataAsync(filter);
                var allBills = customers.SelectMany(c => c.Bills).ToList();

                result.TotalCount = allBills.Count;

                // Search filtering
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var s = filter.SearchTerm.Trim().ToLower();
                    allBills = allBills.Where(b =>
                        (b.CustomerName?.ToLower().Contains(s) ?? false) ||
                        (b.BillNo?.ToLower().Contains(s) ?? false) ||
                        (b.JobNo?.ToLower().Contains(s) ?? false) ||
                        (b.LcLcaNo?.ToLower().Contains(s) ?? false) ||
                        (b.InvoiceNo?.ToLower().Contains(s) ?? false) ||
                        (b.BeRegNo?.ToLower().Contains(s) ?? false)
                    ).ToList();
                }

                result.FilteredCount = allBills.Count;

                // Sorting
                if (!string.IsNullOrEmpty(filter.SortColumn))
                {
                    bool isDesc = string.Equals(filter.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                    allBills = filter.SortColumn.ToLower() switch
                    {
                        "customername" => isDesc ? allBills.OrderByDescending(x => x.CustomerName).ToList() : allBills.OrderBy(x => x.CustomerName).ToList(),
                        "billno" => isDesc ? allBills.OrderByDescending(x => x.BillNo).ToList() : allBills.OrderBy(x => x.BillNo).ToList(),
                        "invoiceno" => isDesc ? allBills.OrderByDescending(x => x.InvoiceNo).ToList() : allBills.OrderBy(x => x.InvoiceNo).ToList(),
                        "beregno" => isDesc ? allBills.OrderByDescending(x => x.BeRegNo).ToList() : allBills.OrderBy(x => x.BeRegNo).ToList(),
                        "totalbilltk" => isDesc ? allBills.OrderByDescending(x => x.TotalBillTk).ToList() : allBills.OrderBy(x => x.TotalBillTk).ToList(),
                        _ => allBills
                    };
                }

                // Paging
                int skip = filter.Start ?? 0;
                int take = filter.Length ?? 10;
                if (take == -1) take = allBills.Count;

                result.Data = allBills.Skip(skip).Take(take).ToList();

                // Compute Summary for the 5 Stats Cards
                result.Summary = new CustomerTopSheetSummaryDTO
                {
                    TotalCustomers = customers.Count,
                    TotalBills = customers.Sum(c => c.TotalBills),
                    TotalBillAmount = customers.Sum(c => c.GrandTotalAmount),
                    TotalTransportLabour = customers.Sum(c => c.TotalTransport + c.TotalLabour),
                    TotalCommission = customers.Sum(c => c.TotalCommission)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGridDataAsync: {ex.Message}");
            }

            return result;
        }

        public async Task<CustomerTopSheetSummaryDTO> GetSummaryAsync(CustomerTopSheetFilter filter)
        {
            var gridResult = await GetGridDataAsync(filter);
            return gridResult.Summary;
        }
        #endregion

        #region Excel Export in Tabs (One Worksheet per Customer)
        public async Task<byte[]> GenerateExcelReportAsync(CustomerTopSheetFilter filter)
        {
            var customers = await GetReportDataAsync(filter);
            if (!customers.Any())
            {
                customers.Add(new CustomerTopSheetCustomerDTO
                {
                    CustomerName = "No Data Available",
                    Bills = new List<CustomerTopSheetBillItemDTO>()
                });
            }

            using var workbook = new XLWorkbook();

            foreach (var cust in customers)
            {
                string tabName = cust.CustomerName;
                if (string.IsNullOrWhiteSpace(tabName)) tabName = "Customer Top Sheet";
                // Sanitize worksheet name (max 31 characters, no invalid chars : \ / ? * [ ])
                tabName = SanitizeWorksheetName(tabName);

                // Handle duplicate tab names
                int suffix = 1;
                string originalTabName = tabName;
                while (workbook.Worksheets.Any(w => w.Name.Equals(tabName, StringComparison.OrdinalIgnoreCase)))
                {
                    string sufStr = $"_{suffix}";
                    if (originalTabName.Length + sufStr.Length > 31)
                    {
                        tabName = originalTabName.Substring(0, 31 - sufStr.Length) + sufStr;
                    }
                    else
                    {
                        tabName = originalTabName + sufStr;
                    }
                    suffix++;
                }

                var ws = workbook.Worksheets.Add(tabName);
                ws.ShowGridLines = true;

                // --- HEADER SECTION ---
                // Row 1: Company Name
                ws.Cell("B1").Value = cust.CompanyName;
                ws.Cell("B1").Style.Font.Bold = true;
                ws.Cell("B1").Style.Font.FontSize = 14;

                // Row 2: Customer Name & Head Office
                ws.Cell("A2").Value = "To:";
                ws.Cell("A2").Style.Font.Bold = true;
                ws.Cell("B2").Value = cust.CustomerName;
                ws.Cell("B2").Style.Font.Bold = true;
                ws.Cell("B2").Style.Font.FontSize = 11;
                ws.Cell("J2").Value = "Head Office";
                ws.Cell("J2").Style.Font.Bold = true;

                // Row 3: Customer Address & HQ Address
                ws.Cell("B3").Value = cust.CustomerAddress;
                ws.Cell("J3").Value = cust.HeadOfficeAddress;

                // Row 5: HQ Contact
                ws.Cell("J5").Value = cust.ContactPhoneFax;

                // Row 6: ATTN
                ws.Cell("A6").Value = "ATTN";
                ws.Cell("A6").Style.Font.Bold = true;
                ws.Cell("B6").Value = cust.Attn;
                ws.Cell("B6").Style.Font.Bold = true;

                // Row 7: REF
                ws.Cell("A7").Value = "REF";
                ws.Cell("A7").Style.Font.Bold = true;
                ws.Cell("B7").Value = cust.Ref;
                ws.Cell("B7").Style.Font.Bold = true;

                // Row 8: Date
                ws.Cell("J8").Value = $"Date: {cust.ReportDate}";
                ws.Cell("J8").Style.Font.Bold = true;

                // --- ROW 9: 18 TABLE HEADERS ---
                int headerRow = 9;
                string[] headers = new[]
                {
                    "SL No.", "Bill No", "LC/LCA No.", "Invoice No.", "Invoice Date", "B/E Reg. No", "B/E Reg. Date",
                    "DTI & Association Service Charge", "Vat & Global Tax", "Customs Misc", "Warehouse Rent",
                    "Freight Forwarder NOC", "Labour Charge", "Others Misc.", "Air Frieght Charge",
                    "Transport Bill", "Commission", "Total Bill / Tk."
                };

                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(headerRow, col);
                    cell.Value = headers[col - 1];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontSize = 9.5;
                    cell.Style.Alignment.Horizontal = col <= 7 ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Right;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(235, 241, 248);
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(180, 195, 215);
                }
                ws.Row(headerRow).Height = 28;

                // --- DATA ROWS ---
                int currentRow = 10;
                int startDataRow = currentRow;

                foreach (var b in cust.Bills)
                {
                    ws.Cell(currentRow, 1).Value = b.SlNo;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 2).Value = b.BillNo;
                    ws.Cell(currentRow, 3).Value = b.LcLcaNo;
                    ws.Cell(currentRow, 4).Value = b.InvoiceNo;
                    ws.Cell(currentRow, 5).Value = b.InvoiceDate;
                    ws.Cell(currentRow, 6).Value = b.BeRegNo;
                    ws.Cell(currentRow, 7).Value = b.BeRegDate;

                    for (int c = 2; c <= 7; c++)
                    {
                        ws.Cell(currentRow, c).Style.Alignment.Horizontal = (c == 5 || c == 7)
                            ? XLAlignmentHorizontalValues.Center
                            : XLAlignmentHorizontalValues.Left;
                    }

                    // Amounts (Columns 8 to 18)
                    ws.Cell(currentRow, 8).Value = b.DtiAssociationServiceCharge;
                    ws.Cell(currentRow, 9).Value = b.VatGlobalTax;
                    ws.Cell(currentRow, 10).Value = b.CustomsMisc;
                    ws.Cell(currentRow, 11).Value = b.WarehouseRent;
                    ws.Cell(currentRow, 12).Value = b.FreightForwarderNOC;
                    ws.Cell(currentRow, 13).Value = b.LabourCharge;
                    ws.Cell(currentRow, 14).Value = b.OthersMisc;
                    ws.Cell(currentRow, 15).Value = b.AirFreightCharge;
                    ws.Cell(currentRow, 16).Value = b.TransportBill;
                    ws.Cell(currentRow, 17).Value = b.Commission;
                    ws.Cell(currentRow, 18).Value = b.TotalBillTk;

                    for (int c = 8; c <= 18; c++)
                    {
                        var cell = ws.Cell(currentRow, c);
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }

                    for (int c = 1; c <= 18; c++)
                    {
                        ws.Cell(currentRow, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        ws.Cell(currentRow, c).Style.Border.OutsideBorderColor = XLColor.FromArgb(220, 225, 230);
                        ws.Cell(currentRow, c).Style.Font.FontSize = 9;
                    }

                    currentRow++;
                }

                // If no bills
                int endDataRow = currentRow - 1;
                if (endDataRow < startDataRow) endDataRow = startDataRow;

                // --- GRAND TOTAL ROW ---
                int totalRow = currentRow;
                ws.Range(totalRow, 1, totalRow, 7).Merge();
                var totalLabelCell = ws.Cell(totalRow, 1);
                totalLabelCell.Value = "Grand Total:";
                totalLabelCell.Style.Font.Bold = true;
                totalLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                for (int col = 8; col <= 18; col++)
                {
                    string colLetter = ws.Column(col).ColumnLetter();
                    var cell = ws.Cell(totalRow, col);
                    if (cust.Bills.Any())
                    {
                        cell.FormulaA1 = $"SUM({colLetter}{startDataRow}:{colLetter}{endDataRow})";
                    }
                    else
                    {
                        cell.Value = 0;
                    }
                    cell.Style.Font.Bold = true;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                for (int c = 1; c <= 18; c++)
                {
                    var cell = ws.Cell(totalRow, c);
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 248, 252);
                }

                // --- FOOTER & SIGNATURES ---
                int inWordRow = totalRow + 1;
                ws.Cell(inWordRow, 1).Value = $"In Word: {cust.AmountInWords}";
                ws.Cell(inWordRow, 1).Style.Font.Bold = true;
                ws.Range(inWordRow, 1, inWordRow, 18).Merge();

                int noteRow = inWordRow + 1;
                ws.Cell(noteRow, 1).Value = "PLEASE ACKNOWLEDGE RECEIPT AND ARRANGE THE PAYMENT AS SOON AS POSSIBLE";
                ws.Cell(noteRow, 1).Style.Font.Italic = true;
                ws.Range(noteRow, 1, noteRow, 18).Merge();

                int sigRow = noteRow + 4;
                ws.Cell(sigRow, 2).Value = "Checked by";
                ws.Cell(sigRow, 2).Style.Font.Bold = true;
                ws.Cell(sigRow, 2).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Cell(sigRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(sigRow, 17).Value = "Prepared by";
                ws.Cell(sigRow, 17).Style.Font.Bold = true;
                ws.Cell(sigRow, 17).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Cell(sigRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Auto-fit column widths
                ws.Columns(1, 18).AdjustToContents(10.0, 35.0);
                ws.Column(1).Width = 7;
                ws.Column(2).Width = 24;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        #endregion

        #region PDF Report Generation (QuestPDF without Dashboard)
        public async Task<byte[]> GeneratePdfReportAsync(CustomerTopSheetFilter filter, string printUser = "")
        {
            var customers = await GetReportDataAsync(filter);
            if (!customers.Any())
            {
                customers.Add(new CustomerTopSheetCustomerDTO
                {
                    CustomerName = "No Records Found",
                    Bills = new List<CustomerTopSheetBillItemDTO>()
                });
            }

            var document = Document.Create(container =>
            {
                foreach (var cust in customers)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(14, Unit.Point);
                        page.DefaultTextStyle(x => x.FontSize(7.2f).FontFamily("Arial"));

                        page.Header().Element(header =>
                        {
                            header.Column(col =>
                            {
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text(cust.CompanyName).FontSize(13).Bold().FontColor("#003366");
                                        c.Item().Row(toRow =>
                                        {
                                            toRow.AutoItem().Text("To: ").Bold();
                                            toRow.RelativeItem().Text(cust.CustomerName).Bold().FontColor("#111827");
                                        });
                                        if (!string.IsNullOrWhiteSpace(cust.CustomerAddress))
                                        {
                                            c.Item().Text(cust.CustomerAddress).FontSize(7).FontColor("#4b5563");
                                        }
                                    });

                                    r.ConstantItem(260).AlignRight().Column(c =>
                                    {
                                        c.Item().Text("Head Office").Bold().FontSize(8);
                                        c.Item().Text(cust.HeadOfficeAddress).FontSize(6.8f).FontColor("#4b5563");
                                        c.Item().Text(cust.ContactPhoneFax).FontSize(6.5f).FontColor("#4b5563");
                                    });
                                });

                                col.Item().PaddingTop(3).Row(r =>
                                {
                                    r.AutoItem().Text("ATTN: ").Bold();
                                    r.AutoItem().Text(cust.Attn).Bold();
                                    r.ConstantItem(30);
                                    r.AutoItem().Text("REF: ").Bold();
                                    r.AutoItem().Text(cust.Ref).Bold();
                                    r.RelativeItem().AlignRight().Text($"Date: {cust.ReportDate}").Bold();
                                });

                                col.Item().PaddingTop(3).LineHorizontal(1).LineColor("#cbd5e1");
                            });
                        });

                        page.Content().PaddingTop(5).Element(content =>
                        {
                            content.Column(mainCol =>
                            {
                                mainCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(22); // SL
                                        cols.RelativeColumn(3.2f); // Bill No
                                        cols.RelativeColumn(2.5f); // LC
                                        cols.RelativeColumn(2.6f); // Inv
                                        cols.ConstantColumn(38); // Inv Date
                                        cols.RelativeColumn(2.0f); // B/E No
                                        cols.ConstantColumn(38); // B/E Date
                                        cols.RelativeColumn(1.8f); // DTI
                                        cols.RelativeColumn(1.8f); // Vat
                                        cols.RelativeColumn(1.8f); // Customs
                                        cols.RelativeColumn(2.0f); // Warehouse
                                        cols.RelativeColumn(1.8f); // NOC
                                        cols.RelativeColumn(1.8f); // Labour
                                        cols.RelativeColumn(1.8f); // Misc
                                        cols.RelativeColumn(1.8f); // Air Freight
                                        cols.RelativeColumn(2.0f); // Transport
                                        cols.RelativeColumn(2.0f); // Commission
                                        cols.RelativeColumn(2.4f); // Total
                                    });

                                    // Table Headers (18 Columns)
                                    string[] headers = new[]
                                    {
                                        "SL", "Bill No", "LC/LCA No.", "Invoice No.", "Inv Date", "B/E Reg.", "B/E Date",
                                        "DTI & Ass.", "Vat & Tax", "Cust. Misc", "Wh. Rent", "NOC",
                                        "Labour", "Misc.", "Air Frt.", "Transport", "Comm.", "Total (Tk.)"
                                    };

                                    table.Header(header =>
                                    {
                                        foreach (var h in headers)
                                        {
                                            header.Cell().Background("#f1f5f9").Border(0.5f).BorderColor("#cbd5e1").Padding(2)
                                                .AlignCenter().AlignMiddle().Text(h).Bold().FontSize(6.5f);
                                        }
                                    });

                                    // Line items
                                    foreach (var b in cust.Bills)
                                    {
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignCenter().Text(b.SlNo.ToString()).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).Text(b.BillNo).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).Text(b.LcLcaNo).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).Text(b.InvoiceNo).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignCenter().Text(b.InvoiceDate).FontSize(6.0f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).Text(b.BeRegNo).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignCenter().Text(b.BeRegDate).FontSize(6.0f);

                                        // Numbers
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.DtiAssociationServiceCharge.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.VatGlobalTax.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.CustomsMisc.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.WarehouseRent.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.FreightForwarderNOC.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.LabourCharge.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.OthersMisc.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.AirFreightCharge.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.TransportBill.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.Commission.ToString("N0")).FontSize(6.2f);
                                        table.Cell().Border(0.5f).BorderColor("#e2e8f0").Padding(1.5f).AlignRight().Text(b.TotalBillTk.ToString("N2")).Bold().FontSize(6.2f);
                                    }

                                    // Grand Total Row
                                    table.Cell().ColumnSpan(7).Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text("Grand Total:").Bold().FontSize(6.8f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalDti.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalVat.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalCustomsMisc.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalWarehouseRent.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalFfNoc.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalLabour.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalOthersMisc.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalAirFreight.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalTransport.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.TotalCommission.ToString("N0")).Bold().FontSize(6.5f);
                                    table.Cell().Border(0.5f).BorderColor("#cbd5e1").Background("#f8fafc").Padding(2).AlignRight().Text(cust.GrandTotalAmount.ToString("N2")).Bold().FontSize(7.0f);
                                });

                                // In Word & Notice
                                mainCol.Item().PaddingTop(4).Text($"In Word: {cust.AmountInWords}").Bold().FontSize(7.0f);
                                mainCol.Item().PaddingTop(2).Text("PLEASE ACKNOWLEDGE RECEIPT AND ARRANGE THE PAYMENT AS SOON AS POSSIBLE").Italic().FontSize(6.5f).FontColor("#4b5563");

                                // Signatures
                                mainCol.Item().PaddingTop(25).Row(r =>
                                {
                                    r.ConstantItem(120).Column(c =>
                                    {
                                        c.Item().LineHorizontal(0.5f).LineColor("#94a3b8");
                                        c.Item().AlignCenter().Text("Checked by").Bold().FontSize(7.0f);
                                    });

                                    r.RelativeItem();

                                    r.ConstantItem(120).Column(c =>
                                    {
                                        c.Item().LineHorizontal(0.5f).LineColor("#94a3b8");
                                        c.Item().AlignCenter().Text("Prepared by").Bold().FontSize(7.0f);
                                    });
                                });
                            });
                        });

                        page.Footer().Row(r =>
                        {
                            r.RelativeItem().Text($"Printed By: {(string.IsNullOrEmpty(printUser) ? "System" : printUser)} | Date: {DateTime.Now:dd/MM/yyyy hh:mm tt}").FontSize(6).FontColor("#64748b");
                            r.AutoItem().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }
        #endregion

        #region CSV Export
        public async Task<byte[]> GenerateCsvReportAsync(CustomerTopSheetFilter filter)
        {
            var customers = await GetReportDataAsync(filter);
            var sb = new StringBuilder();

            // Header line
            sb.AppendLine("Customer,SL No.,Bill No,Job No,LC/LCA No.,Invoice No.,Invoice Date,B/E Reg. No,B/E Reg. Date,DTI & Association Service Charge,Vat & Global Tax,Customs Misc,Warehouse Rent,Freight Forwarder NOC,Labour Charge,Others Misc.,Air Frieght Charge,Transport Bill,Commission,Total Bill / Tk.");

            foreach (var cust in customers)
            {
                foreach (var b in cust.Bills)
                {
                    sb.AppendLine($"\"{cust.CustomerName}\",{b.SlNo},\"{b.BillNo}\",\"{b.JobNo}\",\"{b.LcLcaNo}\",\"{b.InvoiceNo}\",\"{b.InvoiceDate}\",\"{b.BeRegNo}\",\"{b.BeRegDate}\",{b.DtiAssociationServiceCharge},{b.VatGlobalTax},{b.CustomsMisc},{b.WarehouseRent},{b.FreightForwarderNOC},{b.LabourCharge},{b.OthersMisc},{b.AirFreightCharge},{b.TransportBill},{b.Commission},{b.TotalBillTk}");
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        #endregion

        #region Dispatch Export
        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(
            CustomerTopSheetFilter filter, string exportFormat, string printUser = "")
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await GenerateExcelReportAsync(filter);
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Customer_Top_Sheet_{timeStamp}.xlsx");
            }
            else if (string.Equals(exportFormat, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await GenerateCsvReportAsync(filter);
                return (bytes, "text/csv", $"Customer_Top_Sheet_{timeStamp}.csv");
            }
            else
            {
                var bytes = await GeneratePdfReportAsync(filter, printUser);
                return (bytes, "application/pdf", $"Customer_Top_Sheet_{timeStamp}.pdf");
            }
        }
        #endregion

        #region Private Helpers (Sanitize & Number to Words)
        private static string SanitizeWorksheetName(string name)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            name = name.Trim();
            if (name.Length > 31)
            {
                name = name.Substring(0, 31);
            }
            return name;
        }

        private static string ConvertNumberToWords(decimal amount)
        {
            try
            {
                long whole = (long)Math.Floor(amount);
                int fraction = (int)Math.Round((amount - whole) * 100);

                string words = NumberToWordsHelper(whole);
                if (string.IsNullOrWhiteSpace(words)) words = "Zero";

                string result = $"Taka {words}";
                if (fraction > 0)
                {
                    result += $" and {NumberToWordsHelper(fraction)} Paisa";
                }
                return result + " Only.";
            }
            catch
            {
                return $"{amount:N2} Taka Only.";
            }
        }

        private static string NumberToWordsHelper(long number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWordsHelper(Math.Abs(number));

            var units = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            string words = "";

            if ((number / 10000000) > 0)
            {
                words += NumberToWordsHelper(number / 10000000) + " Crore ";
                number %= 10000000;
            }

            if ((number / 100000) > 0)
            {
                words += NumberToWordsHelper(number / 100000) + " Lakh ";
                number %= 100000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWordsHelper(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWordsHelper(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (number < 20)
                    words += units[number];
                else
                {
                    words += tens[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + units[number % 10];
                }
            }

            return words.Trim();
        }

        private static DateTime? ParseDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "M/d/yyyy" };
            if (DateTime.TryParseExact(dateStr.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                return d.Date;
            }
            if (DateTime.TryParse(dateStr.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            {
                return d.Date;
            }
            return null;
        }
        #endregion
    }
}
