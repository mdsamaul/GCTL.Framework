using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.BalanceSheet;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.BalanceSheet
{
    public class BalanceSheetService : IBalanceSheetService
    {
        private readonly GCTL_ERP_DBContext _context;
        private readonly IDbConnection _dbConnection;

        public BalanceSheetService(GCTL_ERP_DBContext context, IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
        }

        public BalanceSheetService(GCTL_ERP_DBContext context)
        {
            _context = context;
            _dbConnection = context.Database.GetDbConnection();
        }

        public async Task<List<CommonSelectVM>> GetCompaniesAsync()
        {
            try
            {
                return await _context.CoreCompanies
                    .OrderBy(o => o.CompanyName)
                    .Select(o => new CommonSelectVM
                    {
                        Id = 1,
                        Code = o.CompanyCode,
                        Name = o.CompanyName
                    })
                    .ToListAsync();
            }
            catch
            {
                return new List<CommonSelectVM>
                {
                    new() { Id = 1, Code = "001", Name = "Formosa Logistics Limited" }
                };
            }
        }

        public async Task<List<CommonSelectVM>> GetBranchesAsync(string? companyCode = null)
        {
            try
            {
                var query = _context.CoreBranches.AsQueryable();
                if (!string.IsNullOrEmpty(companyCode) && companyCode != "0" && companyCode != "All")
                {
                    query = query.Where(b => b.CompanyCode == companyCode);
                }

                return await query
                    .OrderBy(b => b.BranchName)
                    .Select(b => new CommonSelectVM
                    {
                        Id = (int)b.AutoId,
                        Code = b.BranchCode,
                        Name = b.BranchName
                    })
                    .ToListAsync();
            }
            catch
            {
                return new List<CommonSelectVM>();
            }
        }

        public async Task<BalanceSheetReportVM> GetBalanceSheetAsync(BalanceSheetFilterVM filter)
        {
            var report = new BalanceSheetReportVM();
            filter ??= new BalanceSheetFilterVM();

            // 1. Date Defaults
            var now = DateTime.Today;
            report.FromDate = filter.FromDate ?? new DateTime(now.Year, now.Month, 1);
            report.ToDate = filter.ToDate ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            // 2. Company & Branch Information
            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                var comp = await _context.CoreCompanies.FirstOrDefaultAsync(c => c.CompanyCode == filter.CompanyCode);
                report.CompanyName = comp?.CompanyName ?? "All Companies";
            }
            else
            {
                var defaultComp = await _context.CoreCompanies.FirstOrDefaultAsync();
                report.CompanyName = defaultComp?.CompanyName ?? "Formosa Logistics Limited";
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                var br = await _context.CoreBranches.FirstOrDefaultAsync(b => b.BranchCode == filter.BranchCode);
                report.BranchName = br?.BranchName ?? filter.BranchCode;
            }
            else
            {
                report.BranchName = "All Branches";
            }

            // 3. Execute Stored Procedure [Rpt_proc_BalanceSheetReportF2] via Dapper
            var branchParam = (filter.BranchCode == "All" || string.IsNullOrEmpty(filter.BranchCode)) ? string.Empty : filter.BranchCode;
            var companyParam = (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All") ? filter.CompanyCode : string.Empty;

            if (!string.IsNullOrEmpty(companyParam) && companyParam.Length < 3 && int.TryParse(companyParam, out var cNum))
            {
                companyParam = cNum.ToString("D3");
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Main_CompanyCode", companyParam);
            parameters.Add("@BranchCode", branchParam);
            parameters.Add("@FromDate", report.FromDate);
            parameters.Add("@ToDate", report.ToDate);

            List<BalanceSheetSpResultDto> spList = new List<BalanceSheetSpResultDto>();
            try
            {
                var conn = _dbConnection ?? _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    if (conn is System.Data.Common.DbConnection dbConn)
                    {
                        await dbConn.OpenAsync();
                    }
                    else
                    {
                        conn.Open();
                    }
                }

                var results = await conn.QueryAsync<BalanceSheetSpResultDto>(
                    "Rpt_proc_BalanceSheetReportF2",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );
                spList = results.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SP BalanceSheet error: {ex.Message}");
                spList = new List<BalanceSheetSpResultDto>();
            }

            // 4. Map SP Result to BalanceSheetReportVM
            if (spList.Any())
            {
                // Assets items (excluding grand total row)
                report.AssetsItems = spList
                    .Where(x => !x.IsTotal && x.Section == "Assets")
                    .Select(x => new BalanceSheetItemVM
                    {
                        Code = x.Code,
                        Name = x.ParticularName,
                        Level = x.Level,
                        Section = x.Section,
                        Amount = x.Amount,
                        AmountFormatted = FormatCurrency(x.Amount),
                        IsTotal = x.IsTotal,
                        IsHighlighted = x.IsHighlighted
                    })
                    .ToList();

                // Equities & Liabilities items (including Surplus row, excluding grand total row)
                report.EquitiesAndLiabilitiesItems = spList
                    .Where(x => !x.IsTotal && (x.Section == "Equities & Liabilities" || x.Section == "Surplus"))
                    .Select(x => new BalanceSheetItemVM
                    {
                        Code = x.Code,
                        Name = x.ParticularName,
                        Level = x.Level,
                        Section = x.Section,
                        Amount = x.Amount,
                        AmountFormatted = FormatCurrency(x.Amount),
                        IsTotal = x.IsTotal,
                        IsHighlighted = x.IsHighlighted
                    })
                    .ToList();

                var totalAssetRow = spList.FirstOrDefault(x => x.IsTotal && x.Section == "Assets");
                var totalLiabEqRow = spList.FirstOrDefault(x => x.IsTotal && x.Section == "Equities & Liabilities");
                var surplusRow = spList.FirstOrDefault(x => x.Section == "Surplus");

                decimal totalAssets = totalAssetRow?.Amount ?? report.AssetsItems.Where(x => x.Level == 2).Sum(x => x.Amount);
                decimal surplus = surplusRow?.Amount ?? 0m;

                report.TotalAssetsAmount = totalAssets;
                report.TotalAssets = totalAssets;
                report.TotalEquitiesAndLiabilitiesAmount = totalLiabEqRow?.Amount ?? totalAssets;
                report.CurrentPeriodSurplus = surplus;

                // KPI Card Separation:
                // Control Ledgers in Group 2 not starting with '201' are Liabilities (202 Non-Current, 203 Current)
                decimal totalLiabilities = report.EquitiesAndLiabilitiesItems
                    .Where(x => x.Level == 2 && !x.Code.StartsWith("201"))
                    .Sum(x => x.Amount);

                report.TotalLiabilities = totalLiabilities;
                report.TotalEquity = report.TotalEquitiesAndLiabilitiesAmount - totalLiabilities;
            }
            else
            {
                report.AssetsItems = new List<BalanceSheetItemVM>();
                report.EquitiesAndLiabilitiesItems = new List<BalanceSheetItemVM>();
                report.TotalAssets = 0;
                report.TotalAssetsAmount = 0;
                report.TotalLiabilities = 0;
                report.TotalEquity = 0;
                report.TotalEquitiesAndLiabilitiesAmount = 0;
                report.CurrentPeriodSurplus = 0;
            }

            return report;
        }

        private static string FormatCurrency(decimal val)
        {
            if (val < 0)
            {
                return "(" + Math.Abs(val).ToString("N2") + ")";
            }
            return val.ToString("N2");
        }

        public async Task<byte[]> ExportExcelAsync(BalanceSheetFilterVM filter)
        {
            var data = await GetBalanceSheetAsync(filter);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Balance Sheet");

            // Header Banner
            ws.Cells["A1:B1"].Merge = true;
            ws.Cells["A1"].Value = data.CompanyName;
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:B2"].Merge = true;
            ws.Cells["A2"].Value = "Balance Sheet";
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3:B3"].Merge = true;
            ws.Cells["A3"].Value = $"As at {data.ToDateFormatted} · {data.BranchName} · Amounts in Bangladeshi Taka (৳)";
            ws.Cells["A3"].Style.Font.Size = 9.5f;
            ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Table Header
            int row = 5;
            ws.Cells[row, 1].Value = "Particulars";
            ws.Cells[row, 2].Value = "Amount (৳)";
            using (var rng = ws.Cells[row, 1, row, 2])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(14, 90, 74));
                rng.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }
            ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            // Assets Section
            row++;
            ws.Cells[row, 1].Value = "Assets";
            ws.Cells[row, 1].Style.Font.Bold = true;
            using (var rng = ws.Cells[row, 1, row, 2])
            {
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(227, 237, 232));
            }

            foreach (var item in data.AssetsItems)
            {
                row++;
                string indent = item.Level == 2 ? "" : (item.Level == 3 ? "   " : "      ");
                ws.Cells[row, 1].Value = indent + item.DisplayText;
                ws.Cells[row, 2].Value = item.Amount;
                ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                if (item.Level == 2) ws.Cells[row, 1, row, 2].Style.Font.Bold = true;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            // Total Assets
            row++;
            ws.Cells[row, 1].Value = "Total Assets";
            ws.Cells[row, 2].Value = data.TotalAssets;
            ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            using (var rng = ws.Cells[row, 1, row, 2])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(241, 245, 242));
                rng.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
            }
            ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            // Equities & Liabilities Section
            row += 2;
            ws.Cells[row, 1].Value = "Equities & Liabilities";
            ws.Cells[row, 1].Style.Font.Bold = true;
            using (var rng = ws.Cells[row, 1, row, 2])
            {
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(227, 237, 232));
            }

            foreach (var item in data.EquitiesAndLiabilitiesItems)
            {
                row++;
                string indent = item.Level == 2 ? "" : (item.Level == 3 ? "   " : "      ");
                ws.Cells[row, 1].Value = indent + item.DisplayText;
                ws.Cells[row, 2].Value = item.Amount;
                ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                if (item.Level == 2 || item.IsHighlighted) ws.Cells[row, 1, row, 2].Style.Font.Bold = true;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            // Total Equities & Liabilities
            row++;
            ws.Cells[row, 1].Value = "Total Equities & Liabilities";
            ws.Cells[row, 2].Value = data.TotalEquitiesAndLiabilitiesAmount;
            ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            using (var rng = ws.Cells[row, 1, row, 2])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(241, 245, 242));
                rng.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
            }
            ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Column(1).Width = 55;
            ws.Column(2).Width = 25;

            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GeneratePdfAsync(BalanceSheetFilterVM filter)
        {
            var report = await GetBalanceSheetAsync(filter);
            var doc = new BalanceSheetPdf(report);
            return doc.GeneratePdf();
        }
    }
}
