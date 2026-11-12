using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Core.ViewModels.Finance.TrialBalance;
using GCTL.Service.Finance.TrialBalance;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.Dashboard
{
    public class DashboardTrialBalanceService : IDashboardTrialBalanceService
    {
        private readonly string _connectionString;
        private readonly ITrialBalanceService _trialBalanceReportService;

        public DashboardTrialBalanceService(IConfiguration configuration, ITrialBalanceService trialBalanceReportService)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
            _trialBalanceReportService = trialBalanceReportService;
        }

        public async Task<DashboardTrialBalanceDTO> GetTrialBalanceDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardTrialBalanceDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);
            parameters.Add("@Main_CompanyCode", filter.CompanyCode ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_TrialBalanceSummary",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardTrialBalanceKPIs>()) ?? new DashboardTrialBalanceKPIs();

            // 2. Closing Balance by Group (Level 1)
            result.GroupChart = (await multi.ReadAsync<DashboardTrialBalanceGroupChart>()).ToList();

            // 3. Top Control Ledgers by Debit (Level 2)
            result.ControlChart = (await multi.ReadAsync<DashboardTrialBalanceControlChart>()).ToList();

            // 4. Full Grid Rows (Level 1 to 5)
            result.Rows = (await multi.ReadAsync<DashboardTrialBalanceRow>()).ToList();

            return result;
        }

        // ---------------------------------------------------------------
        // PDF (Preview / Download) - reuses the existing Trial Balance report
        // ---------------------------------------------------------------
        public async Task<byte[]> GeneratePdfAsync(DashboardFilterDTO filter, int level)
        {
            var today = DateTime.Today;
            var reportFilter = new TrialBalanceFilterVM
            {
                Main_CompanyCode = filter.CompanyCode ?? string.Empty,
                BranchCode = string.IsNullOrWhiteSpace(filter.BranchCode) ? "All" : filter.BranchCode,
                FromDate = filter.FromDate ?? today.AddMonths(-3),
                ToDate = filter.ToDate ?? today,
                Level = Math.Clamp(level, 1, 5)
            };

            return await _trialBalanceReportService.GeneratePdfAsync(reportFilter);
        }

        // ---------------------------------------------------------------
        // Excel
        // ---------------------------------------------------------------
        public async Task<byte[]> GenerateExcelAsync(DashboardFilterDTO filter, int level, bool hideZero)
        {
            level = Math.Clamp(level, 1, 5);
            var data = await GetTrialBalanceDataAsync(filter);

            var rows = data.Rows
                .Where(r => r.Level <= level)
                .Where(r => !hideZero || r.OpDr != 0 || r.OpCr != 0 || r.TrDr != 0 || r.TrCr != 0 || r.ClDr != 0 || r.ClCr != 0)
                .ToList();

            var fromDate = (filter.FromDate ?? DateTime.Today.AddMonths(-3)).ToString("dd/MM/yyyy");
            var toDate = (filter.ToDate ?? DateTime.Today).ToString("dd/MM/yyyy");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Trial Balance");
            const int lastCol = 7;
            const string numFmt = "#,##0.00;(#,##0.00);\"-\"";

            // Title
            ws.Cell(1, 1).Value = "Trial Balance";
            ws.Range(1, 1, 1, lastCol).Merge().Style
                .Font.SetBold().Font.SetFontSize(15).Font.SetFontColor(XLColor.FromHtml("#0a2f6c"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Period: {fromDate} to {toDate}   |   Levels up to: {level}   |   Branch: {(string.IsNullOrWhiteSpace(filter.BranchCode) ? "All" : filter.BranchCode)}";
            ws.Range(2, 1, 2, lastCol).Merge().Style
                .Font.SetFontSize(10).Font.SetFontColor(XLColor.FromHtml("#546580"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Group header (row 4) + sub header (row 5)
            int h1 = 4, h2 = 5;
            ws.Cell(h1, 1).Value = "Particulars";
            ws.Range(h1, 1, h2, 1).Merge();
            ws.Cell(h1, 2).Value = "Opening Balance";
            ws.Range(h1, 2, h1, 3).Merge();
            ws.Cell(h1, 4).Value = "Transactions";
            ws.Range(h1, 4, h1, 5).Merge();
            ws.Cell(h1, 6).Value = "Closing Balance";
            ws.Range(h1, 6, h1, 7).Merge();
            for (int c = 2; c <= lastCol; c += 2)
            {
                ws.Cell(h2, c).Value = "Debit";
                ws.Cell(h2, c + 1).Value = "Credit";
            }

            var header = ws.Range(h1, 1, h2, lastCol);
            header.Style.Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#eef3fc"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Data rows
            int r0 = h2 + 1, rIdx = r0;
            foreach (var row in rows)
            {
                var name = string.IsNullOrWhiteSpace(row.Code) ? row.ParticularName : $"{row.Code} – {row.ParticularName}";
                var cell = ws.Cell(rIdx, 1);
                cell.Value = name;
                cell.Style.Alignment.SetIndent(Math.Max(0, row.Level - 1) * 2);

                ws.Cell(rIdx, 2).Value = row.OpDr;
                ws.Cell(rIdx, 3).Value = row.OpCr;
                ws.Cell(rIdx, 4).Value = row.TrDr;
                ws.Cell(rIdx, 5).Value = row.TrCr;
                ws.Cell(rIdx, 6).Value = row.ClDr;
                ws.Cell(rIdx, 7).Value = row.ClCr;

                var line = ws.Range(rIdx, 1, rIdx, lastCol);
                if (row.Level == 1)
                    line.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#dfeee6"));
                else if (row.Level == 2)
                    line.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#edf4f0"));
                else if (row.Level == 3)
                    line.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#f8fbf9"));

                rIdx++;
            }

            // Grand total (from Level 1 = sum of whole TB)
            var l1 = data.Rows.Where(x => x.Level == 1).ToList();
            ws.Cell(rIdx, 1).Value = "Grand Total";
            ws.Cell(rIdx, 2).Value = l1.Sum(x => x.OpDr);
            ws.Cell(rIdx, 3).Value = l1.Sum(x => x.OpCr);
            ws.Cell(rIdx, 4).Value = l1.Sum(x => x.TrDr);
            ws.Cell(rIdx, 5).Value = l1.Sum(x => x.TrCr);
            ws.Cell(rIdx, 6).Value = l1.Sum(x => x.ClDr);
            ws.Cell(rIdx, 7).Value = l1.Sum(x => x.ClCr);
            ws.Range(rIdx, 1, rIdx, lastCol).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#dfeaf9"))
                .Border.SetTopBorder(XLBorderStyleValues.Medium);

            // Formatting
            var body = ws.Range(r0, 1, rIdx, lastCol);
            body.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);
            ws.Range(r0, 2, rIdx, lastCol).Style.NumberFormat.Format = numFmt;
            ws.Range(r0, 2, rIdx, lastCol).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Column(1).Width = 55;
            for (int c = 2; c <= lastCol; c++) ws.Column(c).Width = 18;
            ws.SheetView.FreezeRows(h2);
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.SetRowsToRepeatAtTop(h1, h2);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
