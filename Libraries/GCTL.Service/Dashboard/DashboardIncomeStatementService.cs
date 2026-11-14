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
using QuestPDF.Fluent;

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
            var data = await GetIncomeStatementDataAsync(filter);
            filter ??= new DashboardFilterDTO();

            // Fetch company name
            string compName = "Formosa Logistics Limited";
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var c = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 CompanyName FROM dbo.Core_Company WHERE CompanyCode = @c",
                    new { c = string.IsNullOrWhiteSpace(filter.CompanyCode) ? "001" : filter.CompanyCode });
                if (!string.IsNullOrWhiteSpace(c)) compName = c;
            }
            catch { }

            var report = new IncomeStatementReportVM
            {
                CompanyName = compName,
                BranchName = string.Empty,
                FromDate = filter.FromDate ?? DateTime.Today.AddMonths(-3),
                ToDate = filter.ToDate ?? DateTime.Today,
                Items = data.Rows.Select(r =>
                {
                    var isHead = r.Kind == "head";
                    var isTotal = r.Kind == "total";
                    var isSub = r.Kind == "sub";
                    return new IncomeStatementItemVM
                    {
                        Name = r.Label,
                        Amount = r.Amt ?? 0,
                        AmountFormatted = r.Amt.HasValue ? (r.Amt.Value == 0 ? "–" : (r.Amt.Value < 0 ? $"({Math.Abs(r.Amt.Value):N2})" : r.Amt.Value.ToString("N2"))) : string.Empty,
                        Pct = r.Pct,
                        PctFormatted = r.Pct.HasValue ? $"{r.Pct.Value:F2}%" : string.Empty,
                        IsTotal = isTotal || isSub,
                        Level = isHead ? 1 : (isSub ? 2 : 3)
                    };
                }).ToList()
            };

            var doc = new IncomeStatementPdf(report);
            return doc.GeneratePdf();
        }

        public async Task<byte[]> GenerateExcelAsync(DashboardFilterDTO filter)
        {
            var data = await GetIncomeStatementDataAsync(filter);
            filter ??= new DashboardFilterDTO();

            string compName = "Formosa Logistics Limited";
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                var c = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 CompanyName FROM dbo.Core_Company WHERE CompanyCode = @c",
                    new { c = string.IsNullOrWhiteSpace(filter.CompanyCode) ? "001" : filter.CompanyCode });
                if (!string.IsNullOrWhiteSpace(c)) compName = c;
            }
            catch { }

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Income Statement");
            const int lastCol = 3;

            // 1. Company Name Header
            ws.Cell(1, 1).Value = compName;
            ws.Range(1, 1, 1, lastCol).Merge().Style
                .Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#0b2546"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 2. Report Title
            ws.Cell(2, 1).Value = "Income Statement";
            ws.Range(2, 1, 2, lastCol).Merge().Style
                .Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(XLColor.FromHtml("#183b78"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 3. Date Header (Branch removed, middle center)
            var fromDate = (filter.FromDate ?? DateTime.Today.AddMonths(-3)).ToString("dd/MM/yyyy");
            var toDate = (filter.ToDate ?? DateTime.Today).ToString("dd/MM/yyyy");
            ws.Cell(3, 1).Value = $"Date {fromDate} to {toDate}";
            ws.Range(3, 1, 3, lastCol).Merge().Style
                .Font.SetFontSize(10).Font.SetFontColor(XLColor.FromHtml("#546580"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 4. Table Header (middle center, light style)
            int hRow = 5;
            ws.Cell(hRow, 1).Value = "Particulars";
            ws.Cell(hRow, 2).Value = "Amount (BDT)";
            ws.Cell(hRow, 3).Value = "% of Revenue";

            var header = ws.Range(hRow, 1, hRow, lastCol);
            header.Style.Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"))
                .Font.SetFontColor(XLColor.FromHtml("#1E293B"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                .Border.SetBottomBorderColor(XLColor.FromHtml("#CBD5E1"));

            // 5. Data Rows (Light matching screenshot & UI)
            int rIdx = 6;
            foreach (var row in data.Rows)
            {
                ws.Cell(rIdx, 1).Value = row.Label;
                if (row.Amt.HasValue) ws.Cell(rIdx, 2).Value = row.Amt.Value;
                if (row.Pct.HasValue) ws.Cell(rIdx, 3).Value = row.Pct.Value / 100m;

                var line = ws.Range(rIdx, 1, rIdx, lastCol);
                line.Style.Border.SetBottomBorder(XLBorderStyleValues.Hair)
                    .Border.SetBottomBorderColor(XLColor.FromHtml("#F1F5F9"));

                if (row.Kind == "head")
                {
                    line.Style.Font.SetBold()
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"))
                        .Font.SetFontColor(XLColor.FromHtml("#1E293B"));
                }
                else if (row.Kind == "total")
                {
                    line.Style.Font.SetBold()
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"))
                        .Font.SetFontColor(XLColor.FromHtml("#0F172A"))
                        .Border.SetTopBorder(XLBorderStyleValues.Thin)
                        .Border.SetTopBorderColor(XLColor.FromHtml("#94A3B8"))
                        .Border.SetBottomBorder(XLBorderStyleValues.Double)
                        .Border.SetBottomBorderColor(XLColor.FromHtml("#94A3B8"));
                }
                else if (row.Kind == "sub")
                {
                    line.Style.Font.SetBold()
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"))
                        .Font.SetFontColor(XLColor.FromHtml("#1E293B"))
                        .Border.SetTopBorder(XLBorderStyleValues.Hair)
                        .Border.SetTopBorderColor(XLColor.FromHtml("#CBD5E1"));
                }
                else
                {
                    ws.Cell(rIdx, 1).Style.Alignment.SetIndent(2);
                }

                ws.Cell(rIdx, 2).Style.NumberFormat.Format = "#,##0.00;(#,##0.00);\"–\"";
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

