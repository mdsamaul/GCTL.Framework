using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Core.ViewModels.Finance.IncomeStatement;
using GCTL.Service.Finance.IncomeStatement;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.Dashboard
{
    public class DashboardIncomeStatementService : IDashboardIncomeStatementService
    {
        private readonly string _connectionString;
        private readonly IIncomeStatementService _incomeStatementService;

        public DashboardIncomeStatementService(
            IConfiguration configuration,
            IIncomeStatementService incomeStatementService)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
            _incomeStatementService = incomeStatementService;
        }

        public async Task<DashboardIncomeStatementDTO> GetIncomeStatementDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardIncomeStatementDTO();
            filter ??= new DashboardFilterDTO();

            var today = DateTime.Today;
            var fromDate = filter.FromDate ?? today.AddMonths(-3);
            var toDate = filter.ToDate ?? today;
            var compCode = string.IsNullOrWhiteSpace(filter.CompanyCode) ? "001" : filter.CompanyCode;
            var branchCode = string.IsNullOrWhiteSpace(filter.BranchCode) || filter.BranchCode == "All" ? string.Empty : filter.BranchCode;

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var p = new DynamicParameters();
            p.Add("@FromDate", fromDate, DbType.Date);
            p.Add("@ToDate", toDate, DbType.Date);
            p.Add("@BranchCode", branchCode, DbType.String);
            p.Add("@Main_CompanyCode", compCode, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_IncomeStatementSummary",
                p,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardIncomeStatementKPIs>()) ?? new DashboardIncomeStatementKPIs();

            // 2. Expense Composition (Doughnut)
            result.ExpenseComposition = (await multi.ReadAsync<DashboardChartSliceDTO>()).ToList();

            // 3. Grid Rows
            result.Rows = (await multi.ReadAsync<DashboardIncomeStatementRowDTO>()).ToList();

            // 4. Monthly Trends (Bar + Line)
            result.MonthlyTrends = (await multi.ReadAsync<DashboardIncomeStatementMonthlyTrend>()).ToList();

            return result;
        }

        public async Task<byte[]> GeneratePdfAsync(DashboardFilterDTO filter)
        {
            var today = DateTime.Today;
            var reportFilter = new IncomeStatementFilterVM
            {
                CompanyCode = string.IsNullOrWhiteSpace(filter.CompanyCode) ? "001" : filter.CompanyCode,
                BranchCode = string.IsNullOrWhiteSpace(filter.BranchCode) ? "All" : filter.BranchCode,
                FromDate = filter.FromDate ?? today.AddMonths(-3),
                ToDate = filter.ToDate ?? today
            };

            return await _incomeStatementService.GeneratePdfAsync(reportFilter);
        }

        public async Task<byte[]> GenerateExcelAsync(DashboardFilterDTO filter)
        {
            var data = await GetIncomeStatementDataAsync(filter);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Income Statement");
            const int lastCol = 3;

            // Title
            ws.Cell(1, 1).Value = "Income Statement";
            ws.Range(1, 1, 1, lastCol).Merge().Style
                .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#0a2f6c"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var fromDate = (filter.FromDate ?? DateTime.Today.AddMonths(-3)).ToString("dd/MM/yyyy");
            var toDate = (filter.ToDate ?? DateTime.Today).ToString("dd/MM/yyyy");
            ws.Cell(2, 1).Value = $"Period: {fromDate} to {toDate}   |   Branch: {(string.IsNullOrWhiteSpace(filter.BranchCode) ? "All" : filter.BranchCode)}";
            ws.Range(2, 1, 2, lastCol).Merge().Style
                .Font.SetFontSize(10).Font.SetFontColor(XLColor.FromHtml("#546580"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Table Header
            int hRow = 4;
            ws.Cell(hRow, 1).Value = "Particulars";
            ws.Cell(hRow, 2).Value = "Amount (BDT)";
            ws.Cell(hRow, 3).Value = "% of Revenue";

            var header = ws.Range(hRow, 1, hRow, lastCol);
            header.Style.Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#0e5a4a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Cell(hRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(hRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            // Data Rows
            int rIdx = 5;
            foreach (var row in data.Rows)
            {
                ws.Cell(rIdx, 1).Value = row.Label;
                if (row.Amt.HasValue) ws.Cell(rIdx, 2).Value = row.Amt.Value;
                if (row.Pct.HasValue) ws.Cell(rIdx, 3).Value = row.Pct.Value / 100m;

                var line = ws.Range(rIdx, 1, rIdx, lastCol);
                if (row.Kind == "head")
                {
                    line.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#e2ede8"));
                }
                else if (row.Kind == "total")
                {
                    line.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#dfeaf9"))
                        .Border.SetTopBorder(XLBorderStyleValues.Thin)
                        .Border.SetBottomBorder(XLBorderStyleValues.Double);
                }
                else if (row.Kind == "sub")
                {
                    line.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#f1f5f3"));
                }
                else
                {
                    ws.Cell(rIdx, 1).Style.Alignment.SetIndent(2);
                }

                ws.Cell(rIdx, 2).Style.NumberFormat.Format = "#,##0.00;(#,##0.00);\"-\"";
                ws.Cell(rIdx, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(rIdx, 3).Style.NumberFormat.Format = "0.00%";
                ws.Cell(rIdx, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                rIdx++;
            }

            ws.Column(1).Width = 55;
            ws.Column(2).Width = 22;
            ws.Column(3).Width = 18;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}

