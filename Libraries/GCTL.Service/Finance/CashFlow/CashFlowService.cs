using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.CashFlow;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.CashFlow
{
    public class CashFlowService : ICashFlowService
    {
        private readonly IDbConnection _dbConnection;
        private readonly GCTL_ERP_DBContext _context;

        public CashFlowService(IDbConnection dbConnection, GCTL_ERP_DBContext context)
        {
            _dbConnection = dbConnection;
            _context = context;
        }

        public async Task<CashFlowReportVM> GetInitialViewModelAsync()
        {
            var filter = new CashFlowFilterVM();
            var companies = await GetCompaniesAsync();
            var firstOrg = companies.FirstOrDefault();
            if (firstOrg != null && !string.IsNullOrWhiteSpace(firstOrg.Code))
            {
                filter.Main_CompanyCode = firstOrg.Code;
            }

            var report = new CashFlowReportVM
            {
                Filter = filter,
                Companies = companies,
                Branches = await GetBranchesAsync(filter.Main_CompanyCode)
            };

            if (firstOrg != null && !string.IsNullOrWhiteSpace(firstOrg.Name))
            {
                report.CompanyName = firstOrg.Name;
            }

            return report;
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

        public async Task<CashFlowReportVM> GetCashFlowReportAsync(CashFlowFilterVM filter)
        {
            filter ??= new CashFlowFilterVM();
            var report = new CashFlowReportVM
            {
                Filter = filter
            };

            // Retrieve company name
            if (!string.IsNullOrEmpty(filter.Main_CompanyCode) && filter.Main_CompanyCode != "0" && filter.Main_CompanyCode != "All")
            {
                var comp = await _context.CoreCompanies.FirstOrDefaultAsync(c => c.CompanyCode == filter.Main_CompanyCode);
                if (comp != null && !string.IsNullOrWhiteSpace(comp.CompanyName))
                {
                    report.CompanyName = comp.CompanyName;
                }
            }
            else
            {
                var defaultComp = await _context.CoreCompanies.FirstOrDefaultAsync();
                report.CompanyName = defaultComp?.CompanyName ?? "Formosa Logistics Limited";
            }

            // Attempt 1: Call Stored Procedure via Dapper
            bool spSucceeded = false;
            try
            {
                var conn = _dbConnection ?? _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    if (conn is System.Data.Common.DbConnection dbConn)
                        await dbConn.OpenAsync();
                    else
                        conn.Open();
                }

                var companyParam = (filter.Main_CompanyCode == "All" || filter.Main_CompanyCode == "0") ? string.Empty : (filter.Main_CompanyCode ?? string.Empty);
                if (!string.IsNullOrEmpty(companyParam) && companyParam.Length < 3 && int.TryParse(companyParam, out var cNum))
                {
                    companyParam = cNum.ToString("D3");
                }
                var branchParam = (filter.BranchCode == "All" || string.IsNullOrEmpty(filter.BranchCode)) ? string.Empty : filter.BranchCode;

                var parameters = new DynamicParameters();
                parameters.Add("@Main_CompanyCode", companyParam);
                parameters.Add("@BranchCode", branchParam);
                parameters.Add("@FromDate", filter.FromDate);
                parameters.Add("@ToDate", filter.ToDate);

                using (var multi = await conn.QueryMultipleAsync("SP_Rpt_CashFlowStatement", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 120))
                {
                    var spItems = (await multi.ReadAsync<CashFlowSpResultDto>()).ToList();
                    var spSummary = await multi.ReadFirstOrDefaultAsync<CashFlowSpSummaryDto>();

                    if (spItems.Any() || spSummary != null)
                    {
                        spSucceeded = true;
                        BuildReportFromSp(report, spItems, spSummary);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SP CashFlow error: {ex.Message}");
                spSucceeded = false;
            }

            // Attempt 2: Comprehensive In-Memory / EF Core Calculation fallback
            if (!spSucceeded)
            {
                await BuildReportFromEfAsync(report, filter);
            }

            return report;
        }

        private void BuildReportFromSp(CashFlowReportVM report, List<CashFlowSpResultDto> spItems, CashFlowSpSummaryDto? spSummary)
        {
            var activityTypes = new[]
            {
                new { Type = "Operating", Title = "Operating activities", Order = 1 },
                new { Type = "Investing", Title = "Investing activities", Order = 2 },
                new { Type = "Financing", Title = "Financing activities", Order = 3 }
            };

            report.Activities.Clear();

            foreach (var act in activityTypes)
            {
                var groupItems = spItems
                    .Where(x => string.Equals(x.ActivityType, act.Type, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.NetAmount)
                    .ThenBy(x => x.GeneralLedgerName)
                    .Select(x => new CashFlowLineItemDto
                    {
                        ActivityType = x.ActivityType,
                        ActivityOrder = x.ActivityOrder,
                        GeneralLedgerCodeNo = x.GeneralLedgerCodeNo,
                        GeneralLedgerName = x.GeneralLedgerName,
                        SubSubsidiaryLedgerName = x.SubSubsidiaryLedgerName,
                        Receipts = x.TotalReceipts,
                        Payments = x.TotalPayments,
                        NetAmount = x.NetAmount
                    })
                    .ToList();

                var group = new CashFlowActivityGroupDto
                {
                    ActivityType = act.Type,
                    Title = act.Title,
                    Order = act.Order,
                    Items = groupItems,
                    TotalReceipts = groupItems.Sum(x => x.Receipts),
                    TotalPayments = groupItems.Sum(x => x.Payments),
                    NetCashFlow = groupItems.Sum(x => x.NetAmount)
                };

                report.Activities.Add(group);
            }

            if (spSummary != null)
            {
                report.Summary.OpeningCashAndBank = spSummary.OpeningCashAndBank;
                report.Summary.OperatingCashFlow = spSummary.OperatingCashFlow;
                report.Summary.InvestingCashFlow = spSummary.InvestingCashFlow;
                report.Summary.FinancingCashFlow = spSummary.FinancingCashFlow;
                report.Summary.NetCashFlow = spSummary.NetCashFlow;
                report.Summary.ClosingCashAndBank = spSummary.ClosingCashAndBank;
                report.Summary.Difference = spSummary.Difference;
            }
            else
            {
                report.Summary.OpeningCashAndBank = 0;
                report.Summary.OperatingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Operating")?.NetCashFlow ?? 0;
                report.Summary.InvestingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Investing")?.NetCashFlow ?? 0;
                report.Summary.FinancingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Financing")?.NetCashFlow ?? 0;
                report.Summary.NetCashFlow = report.Activities.Sum(a => a.NetCashFlow);
                report.Summary.ClosingCashAndBank = report.Summary.OpeningCashAndBank + report.Summary.NetCashFlow;
                report.Summary.Difference = 0;
            }
        }

        private async Task BuildReportFromEfAsync(CashFlowReportVM report, CashFlowFilterVM filter)
        {
            var fromDate = filter.FromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var toDate = filter.ToDate ?? DateTime.Now;

            var allGls = await _context.AccGeneralLedgers
                .Where(g => g.IsActive == null || g.IsActive == "1")
                .ToListAsync();

            var subSubs = new Dictionary<string, string>();
            try { subSubs = await _context.AccSubSubsidiaryLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubSubsidiaryLedgerCodeNo, x => x.SubSubsidiaryLedgerName); } catch { }

            var cashBankGls = allGls.Where(g =>
                (g.SubSubsidiaryLedgerCodeNo != null && g.SubSubsidiaryLedgerCodeNo.StartsWith("102002")) ||
                g.SubControlLedgerCodeNo == "102002"
            ).ToList();

            var cashBankCodes = new HashSet<string>(cashBankGls.Select(g => g.GeneralLedgerCodeNo), StringComparer.OrdinalIgnoreCase);

            decimal openingCash = 0;

            var obQuery = _context.AccCompanyOpeningBalances
                .Where(o => o.DeletedAt == null && o.GeneralLedgerCodeNo != null && cashBankCodes.Contains(o.GeneralLedgerCodeNo));

            if (!string.IsNullOrEmpty(filter.Main_CompanyCode) && filter.Main_CompanyCode != "0" && filter.Main_CompanyCode != "All")
            {
                obQuery = obQuery.Where(o => o.MainCompanyCode == filter.Main_CompanyCode || o.CompanyCode == filter.Main_CompanyCode);
            }
            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                obQuery = obQuery.Where(o => o.BranchCode == filter.BranchCode);
            }

            var obList = await obQuery.ToListAsync();
            foreach (var ob in obList)
            {
                var tr = ob.TrType?.Trim().ToLowerInvariant() ?? "debit";
                if (tr == "debit" || tr == "dr")
                    openingCash += ob.OpeningBalance ?? 0;
                else
                    openingCash -= ob.OpeningBalance ?? 0;
            }

            var priorVoucherQuery = from d in _context.AccVoucherEntryDetails
                                    join v in _context.AccVoucherEntries on d.VoucherEntryCodeNo equals v.VoucherEntryCodeNo
                                    where v.VoucherDate < fromDate &&
                                          (v.IsApproved == "Posted" || v.IsApproved == "Y" || v.IsApproved == "1" || string.IsNullOrEmpty(v.IsApproved)) &&
                                          (v.IsActive == null || v.IsActive != "0") &&
                                          cashBankCodes.Contains(d.AccCode)
                                    select new { d.DebitAmount, d.CreditAmount, v.MainCompanyCode, v.CompanyCode, v.BranchCode };

            if (!string.IsNullOrEmpty(filter.Main_CompanyCode) && filter.Main_CompanyCode != "0" && filter.Main_CompanyCode != "All")
                priorVoucherQuery = priorVoucherQuery.Where(x => x.MainCompanyCode == filter.Main_CompanyCode || x.CompanyCode == filter.Main_CompanyCode);
            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
                priorVoucherQuery = priorVoucherQuery.Where(x => x.BranchCode == filter.BranchCode);

            var priorDetails = await priorVoucherQuery.Select(x => new { x.DebitAmount, x.CreditAmount }).ToListAsync();
            openingCash += priorDetails.Sum(d => (d.DebitAmount ?? 0) - (d.CreditAmount ?? 0));

            var periodVoucherQuery = from d in _context.AccVoucherEntryDetails
                                     join v in _context.AccVoucherEntries on d.VoucherEntryCodeNo equals v.VoucherEntryCodeNo
                                     where v.VoucherDate >= fromDate && v.VoucherDate <= toDate &&
                                           (v.IsApproved == "Posted" || v.IsApproved == "Y" || v.IsApproved == "1" || string.IsNullOrEmpty(v.IsApproved)) &&
                                           (v.IsActive == null || v.IsActive != "0")
                                     select new { v.VoucherEntryCodeNo, d.AccCode, d.DebitAmount, d.CreditAmount, v.MainCompanyCode, v.CompanyCode, v.BranchCode };

            if (!string.IsNullOrEmpty(filter.Main_CompanyCode) && filter.Main_CompanyCode != "0" && filter.Main_CompanyCode != "All")
                periodVoucherQuery = periodVoucherQuery.Where(x => x.MainCompanyCode == filter.Main_CompanyCode || x.CompanyCode == filter.Main_CompanyCode);
            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
                periodVoucherQuery = periodVoucherQuery.Where(x => x.BranchCode == filter.BranchCode);

            var rawRows = await periodVoucherQuery.ToListAsync();
            var groupedByVoucher = rawRows.GroupBy(x => x.VoucherEntryCodeNo)
                .Where(g => g.Any(line => cashBankCodes.Contains(line.AccCode)))
                .ToList();

            decimal periodCashMovement = 0;
            foreach (var v in groupedByVoucher)
            {
                foreach (var d in v)
                {
                    if (cashBankCodes.Contains(d.AccCode))
                    {
                        periodCashMovement += ((d.DebitAmount ?? 0) - (d.CreditAmount ?? 0));
                    }
                }
            }

            var glMap = allGls.ToDictionary(g => g.GeneralLedgerCodeNo, StringComparer.OrdinalIgnoreCase);
            var lineItemSums = new Dictionary<string, (decimal Receipts, decimal Payments, string ActType, string GlName, string SubName)>(StringComparer.OrdinalIgnoreCase);

            foreach (var v in groupedByVoucher)
            {
                foreach (var line in v)
                {
                    if (cashBankCodes.Contains(line.AccCode)) continue;

                    string accCode = line.AccCode ?? string.Empty;
                    glMap.TryGetValue(accCode, out var glObj);

                    string glName = glObj?.GeneralLedgerName ?? accCode;
                    string subName = (glObj?.SubSubsidiaryLedgerCodeNo != null && subSubs.TryGetValue(glObj.SubSubsidiaryLedgerCodeNo, out var sn)) ? sn : string.Empty;

                    string groupNo = glObj?.GroupLedgerCodeNo ?? string.Empty;
                    string ctrlNo = glObj?.ControlLedgerCodeNo ?? string.Empty;

                    string actType = "Operating";
                    if (groupNo.StartsWith("3") || groupNo.StartsWith("4"))
                        actType = "Operating";
                    else if (ctrlNo.StartsWith("102") || ctrlNo.StartsWith("203") || ctrlNo.StartsWith("202"))
                        actType = "Operating";
                    else if (ctrlNo.StartsWith("101"))
                        actType = "Investing";
                    else
                        actType = "Financing";

                    decimal dr = line.DebitAmount ?? 0;
                    decimal cr = line.CreditAmount ?? 0;

                    if (!lineItemSums.ContainsKey(accCode))
                        lineItemSums[accCode] = (cr, dr, actType, glName, subName);
                    else
                    {
                        var cur = lineItemSums[accCode];
                        lineItemSums[accCode] = (cur.Receipts + cr, cur.Payments + dr, actType, glName, subName);
                    }
                }
            }

            var activityDefs = new[]
            {
                new { Type = "Operating", Title = "Operating activities", Order = 1 },
                new { Type = "Investing", Title = "Investing activities", Order = 2 },
                new { Type = "Financing", Title = "Financing activities", Order = 3 }
            };

            report.Activities.Clear();

            foreach (var act in activityDefs)
            {
                var matching = lineItemSums
                    .Where(kv => kv.Value.ActType == act.Type)
                    .Select(kv =>
                    {
                        decimal net = kv.Value.Receipts - kv.Value.Payments;
                        return new CashFlowLineItemDto
                        {
                            ActivityType = act.Type,
                            ActivityOrder = act.Order,
                            GeneralLedgerCodeNo = kv.Key,
                            GeneralLedgerName = kv.Value.GlName,
                            SubSubsidiaryLedgerName = kv.Value.SubName,
                            Receipts = net >= 0 ? net : 0,
                            Payments = net < 0 ? Math.Abs(net) : 0,
                            NetAmount = net
                        };
                    })
                    .OrderByDescending(x => x.NetAmount)
                    .ThenBy(x => x.GeneralLedgerName)
                    .ToList();

                report.Activities.Add(new CashFlowActivityGroupDto
                {
                    ActivityType = act.Type,
                    Title = act.Title,
                    Order = act.Order,
                    Items = matching,
                    TotalReceipts = matching.Sum(x => x.Receipts),
                    TotalPayments = matching.Sum(x => x.Payments),
                    NetCashFlow = matching.Sum(x => x.NetAmount)
                });
            }

            report.Summary.OpeningCashAndBank = openingCash;
            report.Summary.OperatingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Operating")?.NetCashFlow ?? 0;
            report.Summary.InvestingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Investing")?.NetCashFlow ?? 0;
            report.Summary.FinancingCashFlow = report.Activities.FirstOrDefault(a => a.ActivityType == "Financing")?.NetCashFlow ?? 0;
            report.Summary.NetCashFlow = report.Activities.Sum(a => a.NetCashFlow);
            report.Summary.ClosingCashAndBank = openingCash + periodCashMovement;
            report.Summary.Difference = report.Summary.ClosingCashAndBank - (openingCash + report.Summary.NetCashFlow);
        }

        public async Task<byte[]> GeneratePdfAsync(CashFlowFilterVM filter)
        {
            var report = await GetCashFlowReportAsync(filter);
            var document = new CashFlowPdf(report);
            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportExcelAsync(CashFlowFilterVM filter)
        {
            var report = await GetCashFlowReportAsync(filter);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Cash Flow Statement");

                int row = 1;

                ws.Cells[row, 1].Value = report.CompanyName;
                ws.Cells[row, 1].Style.Font.Size = 16;
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1, row, 4].Merge = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row++;

                ws.Cells[row, 1].Value = "Cash Flow Statement (Direct Method)";
                ws.Cells[row, 1].Style.Font.Size = 13;
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1, row, 4].Merge = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row++;

                var fromStr = filter.FromDate?.ToString("dd/MM/yyyy") ?? "Start";
                var toStr = filter.ToDate?.ToString("dd/MM/yyyy") ?? "End";
                ws.Cells[row, 1].Value = $"{fromStr} to {toStr}";
                ws.Cells[row, 1].Style.Font.Italic = true;
                ws.Cells[row, 1, row, 4].Merge = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row += 2;

                ws.Cells[row, 1].Value = "Particulars";
                ws.Cells[row, 2].Value = "Receipts (৳)";
                ws.Cells[row, 3].Value = "Payments (৳)";
                ws.Cells[row, 4].Value = "Net Amount (৳)";

                using (var rng = ws.Cells[row, 1, row, 4])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(14, 90, 74));
                    rng.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }
                ws.Cells[row, 2, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                row++;

                foreach (var group in report.Activities)
                {
                    ws.Cells[row, 1].Value = group.Title;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 1, row, 4].Merge = true;
                    using (var rng = ws.Cells[row, 1, row, 4])
                    {
                        rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(227, 237, 232));
                    }
                    row++;

                    foreach (var item in group.Items)
                    {
                        ws.Cells[row, 1].Value = "   " + item.GeneralLedgerName;
                        ws.Cells[row, 2].Value = item.Receipts;
                        ws.Cells[row, 3].Value = item.Payments;
                        ws.Cells[row, 4].Value = item.NetAmount;

                        ws.Cells[row, 2, row, 4].Style.Numberformat.Format = "#,##0.00";
                        ws.Cells[row, 2, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        row++;
                    }

                    ws.Cells[row, 1].Value = "Net Cash Flow from " + group.Title;
                    ws.Cells[row, 1].Style.Font.Bold = true;
                    ws.Cells[row, 2].Value = group.TotalReceipts;
                    ws.Cells[row, 3].Value = group.TotalPayments;
                    ws.Cells[row, 4].Value = group.NetCashFlow;
                    ws.Cells[row, 2, row, 4].Style.Numberformat.Format = "#,##0.00";
                    ws.Cells[row, 2, row, 4].Style.Font.Bold = true;
                    ws.Cells[row, 2, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    using (var rng = ws.Cells[row, 1, row, 4])
                    {
                        rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        rng.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                    row += 2;
                }

                ws.Cells[row, 1].Value = "Opening Cash and Bank Balance";
                ws.Cells[row, 4].Value = report.Summary.OpeningCashAndBank;
                ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                row++;

                ws.Cells[row, 1].Value = "Net Cash Movement for Period";
                ws.Cells[row, 4].Value = report.Summary.NetCashFlow;
                ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                row++;

                ws.Cells[row, 1].Value = "Closing Cash and Bank Balance";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 4].Value = report.Summary.ClosingCashAndBank;
                ws.Cells[row, 4].Style.Font.Bold = true;
                ws.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";

                using (var rng = ws.Cells[row, 1, row, 4])
                {
                    rng.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    rng.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                ws.Column(1).Width = 45;
                ws.Column(2).Width = 18;
                ws.Column(3).Width = 18;
                ws.Column(4).Width = 18;

                return await package.GetAsByteArrayAsync();
            }
        }
    }
}
