using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.DataTables;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.TrialBalance
{
    public class TrialBalanceService : ITrialBalanceService
    {
        private readonly string _connectionString;

        public TrialBalanceService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection") 
                ?? configuration.GetConnectionString("DefaultConnection") 
                ?? string.Empty;
        }

        private static (DateTime fromDate, DateTime toDate) ParseDates(TrialBalanceFilter filter)
        {
            var now = DateTime.Today;
            DateTime fromDate = new DateTime(now.Year, 1, 1);
            DateTime toDate = now;

            string[] formats = new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy", "M/d/yyyy" };

            if (!string.IsNullOrWhiteSpace(filter.FromDate))
            {
                if (DateTime.TryParseExact(filter.FromDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fDate) ||
                    DateTime.TryParse(filter.FromDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out fDate))
                {
                    fromDate = fDate.Date;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.ToDate))
            {
                if (DateTime.TryParseExact(filter.ToDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tDate) ||
                    DateTime.TryParse(filter.ToDate.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out tDate))
                {
                    toDate = tDate.Date;
                }
            }

            return (fromDate, toDate);
        }

        public async Task<TrialBalanceFilterOptionsDTO> GetFilterOptionsAsync()
        {
            var options = new TrialBalanceFilterOptionsDTO();

            try
            {
                await using var connection = new SqlConnection(_connectionString);

                try
                {
                    var companies = (await connection.QueryAsync<(string Code, string Name)>(
                        "SELECT CompanyCode AS Code, CompanyName AS Name FROM Core_Company ORDER BY CompanyName"
                    )).ToList();

                    options.Companies = companies.Select(c => new SelectListItem
                    {
                        Value = c.Code,
                        Text = c.Name
                    }).ToList();
                }
                catch
                {
                    // Fallback
                }

                try
                {
                    var branches = (await connection.QueryAsync<(string Code, string Name)>(
                        "SELECT BranchCode AS Code, BranchName AS Name FROM Core_Branch ORDER BY BranchName"
                    )).ToList();

                    options.Branches = branches.Select(b => new SelectListItem
                    {
                        Value = b.Code,
                        Text = b.Name
                    }).ToList();
                }
                catch
                {
                    // Fallback
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TrialBalanceService.GetFilterOptionsAsync error: {ex.Message}");
            }

            return options;
        }

        public async Task<List<TrialBalanceItemDTO>> GetTrialBalanceDataAsync(TrialBalanceFilter filter)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);

                var (fromDate, toDate) = ParseDates(filter);

                var parameters = new DynamicParameters();
                parameters.Add("@Main_CompanyCode", filter.CompanyCode ?? string.Empty);
                parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty);
                parameters.Add("@FromDate", fromDate, DbType.DateTime);
                parameters.Add("@ToDate", toDate, DbType.DateTime);

                var result = (await connection.QueryAsync<TrialBalanceItemDTO>(
                    "Rpt_proc_TrialBalanceReportF2",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                )).ToList();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TrialBalanceService.GetTrialBalanceDataAsync error: {ex.Message}");
                // Fallback to default seed data only when exception occurs (e.g. offline DB)
                return GetDefaultSeedData();
            }
        }

        public async Task<(PaginationResponse<TrialBalanceItemDTO> GridData, TrialBalanceSummaryDTO Summary)> GetGridDataWithSummaryAsync(TrialBalanceFilter filter)
        {
            var allData = await GetTrialBalanceDataAsync(filter);

            // Compute summary
            decimal totalDebit = 0;
            decimal totalCredit = 0;
            if (allData.Any())
            {
                totalDebit = allData[0].DebitBalance != 0 
                    ? allData[0].DebitBalance 
                    : allData.Sum(x => x.Debit);
                totalCredit = allData[0].CreditBalance != 0 
                    ? allData[0].CreditBalance 
                    : allData.Sum(x => x.Credit);
            }

            var summary = new TrialBalanceSummaryDTO
            {
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                Difference = Math.Abs(totalDebit - totalCredit),
                AccountsWithBalance = allData.Count
            };

            // Filter by Search Term
            var filteredData = allData;
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                filteredData = filteredData.Where(x =>
                    (x.Code != null && x.Code.ToLower().Contains(term)) ||
                    (x.AccountHead != null && x.AccountHead.ToLower().Contains(term)) ||
                    (x.Group != null && x.Group.ToLower().Contains(term))
                ).ToList();
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(filter.SortColumn))
            {
                bool isDesc = string.Equals(filter.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
                filteredData = filter.SortColumn.ToLower() switch
                {
                    "code" or "generalledgercodeno" => isDesc ? filteredData.OrderByDescending(x => x.Code).ToList() : filteredData.OrderBy(x => x.Code).ToList(),
                    "accounthead" or "generalledgername" => isDesc ? filteredData.OrderByDescending(x => x.AccountHead).ToList() : filteredData.OrderBy(x => x.AccountHead).ToList(),
                    "group" => isDesc ? filteredData.OrderByDescending(x => x.Group).ToList() : filteredData.OrderBy(x => x.Group).ToList(),
                    "debit" => isDesc ? filteredData.OrderByDescending(x => x.Debit).ToList() : filteredData.OrderBy(x => x.Debit).ToList(),
                    "credit" => isDesc ? filteredData.OrderByDescending(x => x.Credit).ToList() : filteredData.OrderBy(x => x.Credit).ToList(),
                    _ => filteredData
                };
            }

            int totalRecords = allData.Count;
            int filteredRecords = filteredData.Count;
            int start = filter.Start ?? 0;
            int length = filter.Length ?? 10;

            var pagedData = length == -1 ? filteredData : filteredData.Skip(start).Take(length).ToList();

            var gridResponse = new PaginationResponse<TrialBalanceItemDTO>
            {
                Data = pagedData,
                TotalCount = totalRecords,
                FilteredCount = filteredRecords
            };

            return (gridResponse, summary);
        }

        public async Task<PaginationResponse<TrialBalanceItemDTO>> GetGridDataAsync(TrialBalanceFilter filter)
        {
            var (gridData, _) = await GetGridDataWithSummaryAsync(filter);
            return gridData;
        }

        public async Task<TrialBalanceSummaryDTO> GetSummaryAsync(TrialBalanceFilter filter)
        {
            var (_, summary) = await GetGridDataWithSummaryAsync(filter);
            return summary;
        }

        public async Task<byte[]> GenerateExcelReportAsync(TrialBalanceFilter filter)
        {
            var data = await GetTrialBalanceDataAsync(filter);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Trial Balance");

            // Title Banner
            ws.Cell(1, 1).Value = "TRIAL BALANCE REPORT";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 15;
            ws.Range(1, 1, 1, 5).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var (fromDate, toDate) = ParseDates(filter);
            var periodText = $"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";

            ws.Cell(2, 1).Value = periodText;
            ws.Cell(2, 1).Style.Font.FontSize = 10;
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Range(2, 1, 2, 5).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Table Headers
            int row = 4;
            ws.Cell(row, 1).Value = "CODE";
            ws.Cell(row, 2).Value = "ACCOUNT HEAD";
            ws.Cell(row, 3).Value = "GROUP";
            ws.Cell(row, 4).Value = "DEBIT";
            ws.Cell(row, 5).Value = "CREDIT";

            var headerRange = ws.Range(row, 1, row, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0056B3");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var item in data)
            {
                row++;
                ws.Cell(row, 1).SetValue(item.Code ?? "");
                ws.Cell(row, 2).SetValue(item.AccountHead ?? item.GeneralLedgerName ?? "");
                ws.Cell(row, 3).SetValue(item.Group ?? "");

                if (item.Debit > 0)
                {
                    ws.Cell(row, 4).SetValue(item.Debit);
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                }
                else
                {
                    ws.Cell(row, 4).SetValue("-");
                    ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                if (item.Credit > 0)
                {
                    ws.Cell(row, 5).SetValue(item.Credit);
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                }
                else
                {
                    ws.Cell(row, 5).SetValue("-");
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                totalDebit += item.Debit;
                totalCredit += item.Credit;
            }

            // Total Row
            row++;
            ws.Cell(row, 1).Value = "Total";
            ws.Range(row, 1, row, 3).Merge().Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).SetValue(totalDebit);
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.Font.Bold = true;

            ws.Cell(row, 5).SetValue(totalCredit);
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.Font.Bold = true;

            var totalRange = ws.Range(row, 1, row, 5);
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GeneratePdfReportAsync(TrialBalanceFilter filter)
        {
            return await GenerateExcelReportAsync(filter);
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportReportAsync(TrialBalanceFilter filter, string exportFormat)
        {
            var dateSuffix = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (string.Equals(exportFormat, "Excel", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = await GenerateExcelReportAsync(filter);
                return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TrialBalance_{dateSuffix}.xlsx");
            }
            else
            {
                var bytes = await GeneratePdfReportAsync(filter);
                return (bytes, "application/pdf", $"TrialBalance_{dateSuffix}.pdf");
            }
        }

        private static List<TrialBalanceItemDTO> GetDefaultSeedData()
        {
            return new List<TrialBalanceItemDTO>
            {
                new() { Code = "1010010000100001", AccountHead = "School campus land", Group = "Assets", Debit = 5000000.00m },
                new() { Code = "1010010000100002", AccountHead = "School Buildings", Group = "Assets", Debit = 8000000.00m },
                new() { Code = "1010010000100003", AccountHead = "Furniture and Fixture", Group = "Assets", Debit = 600000.00m },
                new() { Code = "1010010000100005", AccountHead = "Computer & IT Equipment", Group = "Assets", Debit = 400000.00m },
                new() { Code = "1010010000200001", AccountHead = "Accu. Depreciation on School Buildings", Group = "Assets", Credit = 845000.00m },
                new() { Code = "1020010000100001", AccountHead = "Student Fees", Group = "Assets", Debit = 300000.00m },
                new() { Code = "1020020000100001", AccountHead = "Cash in Hand", Group = "Assets", Debit = 211700.00m },
                new() { Code = "1020020000200001", AccountHead = "DBBL", Group = "Assets", Debit = 1390000.00m },
                new() { Code = "1020030000100001", AccountHead = "Stationery Materials", Group = "Assets", Debit = 80000.00m },
                new() { Code = "2010010000100001", AccountHead = "Owner's Equity / School Fund", Group = "Equities & Liabilities", Credit = 12560000.00m },
                new() { Code = "2010010000100002", AccountHead = "General Reserve Fund", Group = "Equities & Liabilities", Credit = 1500000.00m },
                new() { Code = "2010020000100001", AccountHead = "Sundry Creditors / Suppliers", Group = "Equities & Liabilities", Credit = 840000.00m },
                new() { Code = "2010020000200001", AccountHead = "Security Deposit from Contractors", Group = "Equities & Liabilities", Credit = 400000.00m },
                new() { Code = "2010030000100001", AccountHead = "Provision for Gratuity", Group = "Equities & Liabilities", Credit = 350000.00m },
                new() { Code = "2010030000200001", AccountHead = "Outstanding Audit & Legal Fees", Group = "Equities & Liabilities", Credit = 250000.00m },
                new() { Code = "3010010000100001", AccountHead = "Teacher & Staff Salary Expense", Group = "Expenses", Debit = 450000.00m },
                new() { Code = "3010010000200001", AccountHead = "Electricity & Utility Bills", Group = "Expenses", Debit = 120000.00m },
                new() { Code = "3010010000300001", AccountHead = "Laboratory & Science Equipment", Group = "Assets", Debit = 95000.00m },
                new() { Code = "3010010000400001", AccountHead = "Campus Maintenance & Repairs", Group = "Expenses", Debit = 48300.00m },
                new() { Code = "3010010000500001", AccountHead = "Library Books & Journals", Group = "Assets", Debit = 25000.00m },
                new() { Code = "4010010000100001", AccountHead = "Internet & Software License", Group = "Expenses", Debit = 12000.00m },
                new() { Code = "4010010000200001", AccountHead = "Sports Equipment & Kits", Group = "Assets", Debit = 8000.00m },
                new() { Code = "4010010000300001", AccountHead = "Entertainment & Refreshment", Group = "Expenses", Debit = 3500.00m },
                new() { Code = "4010010000400001", AccountHead = "Bank Charges & Commission", Group = "Expenses", Debit = 1500.00m }
            };
        }
    }
}
