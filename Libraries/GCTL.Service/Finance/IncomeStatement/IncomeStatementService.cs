using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.IncomeStatement;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.IncomeStatement
{
    public class IncomeStatementService : IIncomeStatementService
    {
        private readonly GCTL_ERP_DBContext _context;
        private readonly IDbConnection _dbConnection;

        public IncomeStatementService(GCTL_ERP_DBContext context, IDbConnection dbConnection)
        {
            _context = context;
            _dbConnection = dbConnection;
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

        public async Task<IncomeStatementReportVM> GetIncomeStatementAsync(IncomeStatementFilterVM filter)
        {
            var report = new IncomeStatementReportVM();
            filter ??= new IncomeStatementFilterVM();

            // 1. Date Defaults (Current Month)
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

            // 3. Execute Stored Procedure [Rpt_proc_IncomeStatementReportF2] via Dapper
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

            List<IncomeStatementSpResultDto> spList;
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

                var results = await conn.QueryAsync<IncomeStatementSpResultDto>(
                    "Rpt_proc_IncomeStatementReportF2",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120
                );
                spList = results.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SP IncomeStatement error: {ex.Message}");
                spList = new List<IncomeStatementSpResultDto>();
            }

            report.RawItems = spList;

            // 4. Map SP Result Set to Report Items & Calculate Totals
            if (spList.Any())
            {
                var incomeTotalRow = spList.FirstOrDefault(x => x.IsTotal && (x.Section == "Income" || (x.ParticularName ?? "").Contains("Total Income")));
                var expenseTotalRow = spList.FirstOrDefault(x => x.IsTotal && (x.Section == "Expense" || (x.ParticularName ?? "").Contains("Total Expense")));
                var netRow = spList.FirstOrDefault(x => x.IsTotal && (x.Section == "Net" || (x.ParticularName ?? "").Contains("Net Surplus") || (x.ParticularName ?? "").Contains("Net Deficit")));

                decimal sumIncome = spList.Where(x => !x.IsTotal && x.Section == "Income" && (x.Level == 5 || (x.Level == 2 && !spList.Any(c => c.Level == 5 && c.Section == "Income")))).Sum(x => x.Amount);
                decimal sumExpense = spList.Where(x => !x.IsTotal && x.Section == "Expense" && (x.Level == 5 || (x.Level == 2 && !spList.Any(c => c.Level == 5 && c.Section == "Expense")))).Sum(x => x.Amount);

                report.TotalIncome = (incomeTotalRow != null && incomeTotalRow.Amount != 0) ? incomeTotalRow.Amount : sumIncome;
                report.TotalExpenses = (expenseTotalRow != null && expenseTotalRow.Amount != 0) ? expenseTotalRow.Amount : sumExpense;
                report.NetSurplus = (netRow != null && netRow.Amount != 0) ? netRow.Amount : (report.TotalIncome - report.TotalExpenses);

                if (report.TotalIncome != 0)
                {
                    decimal margin = (report.NetSurplus / Math.Abs(report.TotalIncome)) * 100m;
                    report.SurplusMargin = margin.ToString("F1") + "%";
                }
                else
                {
                    report.SurplusMargin = "0.0%";
                }

                report.Items = spList.Select(x => new IncomeStatementItemVM
                {
                    Level = x.Level,
                    Code = x.Code ?? string.Empty,
                    Name = x.ParticularName ?? string.Empty,
                    ParentCode = x.ParentCode,
                    Section = x.Section ?? string.Empty,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    Amount = x.Amount,
                    AmountFormatted = FormatCurrency(x.Amount),
                    IsTotal = x.IsTotal
                }).ToList();
            }
            else
            {
                report.TotalIncome = 0;
                report.TotalExpenses = 0;
                report.NetSurplus = 0;
                report.SurplusMargin = "0.0%";
            }

            return report;
        }

        public async Task<byte[]> ExportExcelAsync(IncomeStatementFilterVM filter)
        {
            var data = await GetIncomeStatementAsync(filter);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Income Statement");

            // Title Header Banner
            ws.Cells["A1:B1"].Merge = true;
            ws.Cells["A1"].Value = data.CompanyName;
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:B2"].Merge = true;
            ws.Cells["A2"].Value = "Income Statement";
            ws.Cells["A2"].Style.Font.Size = 12;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3:B3"].Merge = true;
            ws.Cells["A3"].Value = $"{data.FromDateFormatted} to {data.ToDateFormatted} · {data.BranchName} · Amounts in Bangladeshi Taka (৳)";
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

            // Data Rows
            foreach (var item in data.Items)
            {
                row++;
                if (item.Level == 1 && !item.IsTotal) // Section Header
                {
                    ws.Cells[row, 1].Value = item.Name;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    using (var rng = ws.Cells[row, 1, row, 2])
                    {
                        rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(227, 237, 232));
                    }
                }
                else if (item.IsTotal) // Totals
                {
                    ws.Cells[row, 1].Value = item.Name;
                    ws.Cells[row, 2].Value = item.Amount;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
                    using (var rng = ws.Cells[row, 1, row, 2])
                    {
                        rng.Style.Font.Bold = true;
                        rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(241, 245, 242));
                        rng.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                        rng.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                }
                else // Hierarchical items
                {
                    string indent = item.Level switch
                    {
                        2 => "   ",
                        3 => "      ",
                        _ => "         "
                    };

                    ws.Cells[row, 1].Value = indent + item.DisplayText;
                    ws.Cells[row, 2].Value = item.Amount;
                    ws.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";

                    if (item.Level == 2)
                    {
                        ws.Cells[row, 1, row, 2].Style.Font.Bold = true;
                    }
                }

                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            ws.Column(1).Width = 55;
            ws.Column(2).Width = 25;

            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GeneratePdfAsync(IncomeStatementFilterVM filter)
        {
            var report = await GetIncomeStatementAsync(filter);
            var doc = new IncomeStatementPdf(report);
            return doc.GeneratePdf();
        }

        private static string FormatCurrency(decimal amount)
        {
            if (amount < 0)
                return $"({Math.Abs(amount):N2})";

            return amount.ToString("N2");
        }
    }
}
