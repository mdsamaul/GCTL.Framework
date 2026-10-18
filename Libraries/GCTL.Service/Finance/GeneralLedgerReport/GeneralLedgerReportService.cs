using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Finance.GeneralLedgerReport;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;

namespace GCTL.Service.Finance.GeneralLedgerReport
{
    public class GeneralLedgerReportService : IGeneralLedgerReportService
    {
        private readonly GCTL_ERP_DBContext _context;

        public GeneralLedgerReportService(GCTL_ERP_DBContext context)
        {
            _context = context;
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

        public async Task<List<CommonSelectVM>> GetAccountHeadsAsync()
        {
            var groups = new Dictionary<string, string>();
            var controls = new Dictionary<string, string>();

            try { groups = await _context.AccGroupLedgers.AsNoTracking().ToDictionaryAsync(x => x.GroupLedgerCodeNo, x => x.GroupLedgerName); } catch { }
            try { controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName); } catch { }

            var gls = await _context.AccGeneralLedgers
                .Where(g => g.IsActive == null || g.IsActive == "1")
                .OrderBy(g => g.GeneralLedgerCodeNo)
                .AsNoTracking()
                .ToListAsync();

            return gls.Select(g =>
            {
                string grpName = g.GroupLedgerCodeNo != null && groups.TryGetValue(g.GroupLedgerCodeNo, out var gn) ? gn : "";
                string ctlName = g.ControlLedgerCodeNo != null && controls.TryGetValue(g.ControlLedgerCodeNo, out var cn) ? cn : "";
                string groupLabel = !string.IsNullOrEmpty(grpName) && !string.IsNullOrEmpty(ctlName)
                    ? $"{grpName} - {ctlName}"
                    : (!string.IsNullOrEmpty(grpName) ? grpName : (!string.IsNullOrEmpty(ctlName) ? ctlName : "General"));

                return new CommonSelectVM
                {
                    Code = g.GeneralLedgerCodeNo,
                    Name = g.GeneralLedgerCodeNo + " – " + g.GeneralLedgerName,
                    GroupName = groupLabel
                };
            }).ToList();
        }

        public async Task<GeneralLedgerReportVM> GetLedgerReportAsync(GeneralLedgerReportFilterVM filter)
        {
            var report = new GeneralLedgerReportVM();
            filter ??= new GeneralLedgerReportFilterVM();

            // Set Date Defaults
            var now = DateTime.Today;
            report.FromDate = filter.FromDate ?? new DateTime(now.Year, now.Month, 1);
            report.ToDate = filter.ToDate ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

            // Select Target Account Head
            if (string.IsNullOrWhiteSpace(filter.GeneralLedgerCodeNo))
            {
                filter.GeneralLedgerCodeNo = "1020020000200001";
            }

            var glAccount = await _context.AccGeneralLedgers
                .FirstOrDefaultAsync(g => g.GeneralLedgerCodeNo == filter.GeneralLedgerCodeNo)
                ?? await _context.AccGeneralLedgers
                .FirstOrDefaultAsync(g => g.GeneralLedgerCodeNo == "1020020000200001")
                ?? await _context.AccGeneralLedgers
                .OrderBy(g => g.GeneralLedgerCodeNo)
                .FirstOrDefaultAsync();

            if (glAccount == null)
            {
                return report;
            }

            report.AccountCode = glAccount.GeneralLedgerCodeNo;
            report.AccountName = glAccount.GeneralLedgerName;

            // Build Breadcrumb Path
            var pathParts = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(glAccount.GroupLedgerCodeNo))
                {
                    var g = await _context.AccGroupLedgers.FirstOrDefaultAsync(x => x.GroupLedgerCodeNo == glAccount.GroupLedgerCodeNo);
                    if (g != null) pathParts.Add($"{g.GroupLedgerCodeNo}-{g.GroupLedgerName}");
                }
                if (!string.IsNullOrEmpty(glAccount.ControlLedgerCodeNo))
                {
                    var c = await _context.AccControlLedgers.FirstOrDefaultAsync(x => x.ControlLedgerCodeNo == glAccount.ControlLedgerCodeNo);
                    if (c != null) pathParts.Add($"{c.ControlLedgerCodeNo}-{c.ControlLedgerName}");
                }
                if (!string.IsNullOrEmpty(glAccount.SubControlLedgerCodeNo))
                {
                    var sc = await _context.AccSubControlLedgers.FirstOrDefaultAsync(x => x.SubControlLedgerCodeNo == glAccount.SubControlLedgerCodeNo);
                    if (sc != null) pathParts.Add($"{sc.SubControlLedgerCodeNo}-{sc.SubControlLedgerName}");
                }
                if (!string.IsNullOrEmpty(glAccount.SubSubsidiaryLedgerCodeNo))
                {
                    var ss = await _context.AccSubSubsidiaryLedgers.FirstOrDefaultAsync(x => x.SubSubsidiaryLedgerCodeNo == glAccount.SubSubsidiaryLedgerCodeNo);
                    if (ss != null) pathParts.Add($"{ss.SubSubsidiaryLedgerCodeNo}-{ss.SubSubsidiaryLedgerName}");
                }
            }
            catch { }
            report.HierarchyPath = string.Join(" › ", pathParts);

            // Company & Branch Names
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

            // 1. Initial Opening Balance from Acc_Company_Opening_Balance
            decimal initialOpeningBalance = 0m;
            var obQuery = _context.AccCompanyOpeningBalances
                .Where(ob => ob.GeneralLedgerCodeNo == report.AccountCode && ob.DeletedAt == null);

            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                var comp = filter.CompanyCode;
                var compTrim = comp.TrimStart('0');
                obQuery = obQuery.Where(ob => ob.MainCompanyCode == comp || ob.CompanyCode == comp || ob.MainCompanyCode == compTrim || ob.CompanyCode == compTrim);
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                var br = filter.BranchCode;
                var brTrim = br.TrimStart('0');
                obQuery = obQuery.Where(ob => ob.BranchCode == br || ob.BranchCode == brTrim);
            }

            var obList = await obQuery.ToListAsync();
            foreach (var ob in obList)
            {
                decimal amt = ob.OpeningBalance ?? 0m;
                var tr = (ob.TrType ?? "").Trim().ToLowerInvariant();
                if (tr == "debit" || tr == "dr" || tr == "d")
                    initialOpeningBalance += amt;
                else if (tr == "credit" || tr == "cr" || tr == "c")
                    initialOpeningBalance -= amt;
                else
                    initialOpeningBalance += amt;
            }

            // 2. Pre-period voucher transactions (VoucherDate < report.FromDate)
            var preVoucherQuery = from d in _context.AccVoucherEntryDetails
                                  join v in _context.AccVoucherEntries on d.VoucherEntryCodeNo equals v.VoucherEntryCodeNo
                                  where d.AccCode == report.AccountCode &&
                                        v.VoucherDate < report.FromDate.Date &&
                                        (v.IsActive == null || v.IsActive == "1") &&
                                        (v.IsApproved == "1" || v.IsApproved == "Y" || v.IsApproved == "Posted" || string.IsNullOrEmpty(v.IsApproved))
                                  select new { d, v };

            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                var comp = filter.CompanyCode;
                var compTrim = comp.TrimStart('0');
                preVoucherQuery = preVoucherQuery.Where(x => x.v.MainCompanyCode == comp || x.v.CompanyCode == comp || x.v.MainCompanyCode == compTrim || x.v.CompanyCode == compTrim);
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                var br = filter.BranchCode;
                var brTrim = br.TrimStart('0');
                preVoucherQuery = preVoucherQuery.Where(x => x.v.BranchCode == br || x.v.BranchCode == brTrim);
            }

            var preVouchers = await preVoucherQuery.Select(x => new
            {
                Dr = x.d.DebitAmount ?? 0m,
                Cr = x.d.CreditAmount ?? 0m
            }).ToListAsync();

            decimal preVoucherNet = preVouchers.Sum(x => x.Dr - x.Cr);
            report.OpeningBalance = initialOpeningBalance + preVoucherNet;
            report.OpeningBalanceFormatted = FormatDrCr(report.OpeningBalance);

            // 3. Transactions in period (report.FromDate <= VoucherDate <= report.ToDate)
            var toDateEndOfDay = report.ToDate.Date.AddDays(1).AddTicks(-1);

            var periodQuery = from d in _context.AccVoucherEntryDetails
                              join v in _context.AccVoucherEntries on d.VoucherEntryCodeNo equals v.VoucherEntryCodeNo
                              where d.AccCode == report.AccountCode &&
                                    v.VoucherDate >= report.FromDate.Date &&
                                    v.VoucherDate <= toDateEndOfDay &&
                                    (v.IsActive == null || v.IsActive == "1") &&
                                    (v.IsApproved == "1" || v.IsApproved == "Y" || v.IsApproved == "Posted" || string.IsNullOrEmpty(v.IsApproved))
                              select new { d, v };

            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                var comp = filter.CompanyCode;
                var compTrim = comp.TrimStart('0');
                periodQuery = periodQuery.Where(x => x.v.MainCompanyCode == comp || x.v.CompanyCode == comp || x.v.MainCompanyCode == compTrim || x.v.CompanyCode == compTrim);
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                var br = filter.BranchCode;
                var brTrim = br.TrimStart('0');
                periodQuery = periodQuery.Where(x => x.v.BranchCode == br || x.v.BranchCode == brTrim);
            }

            var txRawList = await periodQuery
                .OrderBy(x => x.v.VoucherDate)
                .ThenBy(x => x.v.VoucherNo)
                .ThenBy(x => x.d.AutoId)
                .Select(x => new
                {
                    Date = x.v.VoucherDate,
                    VoucherNo = x.v.VoucherNo,
                    VoucherType = x.v.VoucherTypeCode,
                    InvoiceNo = x.v.VoucherNo,
                    Narration = !string.IsNullOrEmpty(x.d.Description) ? x.d.Description : x.v.Narration,
                    Debit = x.d.DebitAmount ?? 0m,
                    Credit = x.d.CreditAmount ?? 0m
                })
                .ToListAsync();

            decimal runningBal = report.OpeningBalance;
            decimal totalDebit = 0m;
            decimal totalCredit = 0m;

            foreach (var tx in txRawList)
            {
                runningBal += (tx.Debit - tx.Credit);
                totalDebit += tx.Debit;
                totalCredit += tx.Credit;

                report.Items.Add(new GeneralLedgerItemVM
                {
                    Date = tx.Date,
                    VoucherNo = tx.VoucherNo,
                    VoucherType = tx.VoucherType,
                    InvoiceNo = tx.InvoiceNo,
                    Narration = tx.Narration,
                    Debit = tx.Debit,
                    Credit = tx.Credit,
                    RunningBalance = runningBal,
                    RunningBalanceFormatted = FormatDrCr(runningBal)
                });
            }

            report.TotalDebit = totalDebit;
            report.TotalCredit = totalCredit;
            report.ClosingBalance = runningBal;
            report.ClosingBalanceFormatted = FormatDrCr(report.ClosingBalance);

            return report;
        }

        public async Task<byte[]> ExportExcelAsync(GeneralLedgerReportFilterVM filter)
        {
            var data = await GetLedgerReportAsync(filter);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("General Ledger");

            // Title Header
            ws.Cells["A1:G1"].Merge = true;
            ws.Cells["A1"].Value = data.CompanyName;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:G2"].Merge = true;
            ws.Cells["A2"].Value = "GENERAL LEDGER REPORT";
            ws.Cells["A2"].Style.Font.Size = 13;
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A3:G3"].Merge = true;
            ws.Cells["A3"].Value = $"Period: {data.PeriodFormatted}  |  Branch: {data.BranchName}";
            ws.Cells["A3"].Style.Font.Size = 10;
            ws.Cells["A3"].Style.Font.Italic = true;
            ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Account Meta
            ws.Cells["A5"].Value = "Account Head:";
            ws.Cells["A5"].Style.Font.Bold = true;
            ws.Cells["B5:D5"].Merge = true;
            ws.Cells["B5"].Value = $"{data.AccountCode} - {data.AccountName}";

            ws.Cells["E5"].Value = "Opening Balance:";
            ws.Cells["E5"].Style.Font.Bold = true;
            ws.Cells["F5:G5"].Merge = true;
            ws.Cells["F5"].Value = data.OpeningBalanceFormatted;
            ws.Cells["F5"].Style.Font.Bold = true;

            ws.Cells["A6"].Value = "Hierarchy:";
            ws.Cells["A6"].Style.Font.Bold = true;
            ws.Cells["B6:G6"].Merge = true;
            ws.Cells["B6"].Value = data.HierarchyPath;

            // Table Column Headers
            int row = 8;
            string[] headers = { "Date", "Voucher No", "Voucher Type", "Narration", "Debit (৳)", "Credit (৳)", "Balance (৳)" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[row, i + 1].Value = headers[i];
            }

            using (var rng = ws.Cells[row, 1, row, headers.Length])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(14, 90, 74));
                rng.Style.Font.Color.SetColor(System.Drawing.Color.White);
                rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            }
            ws.Cells[row, 5, row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            // Opening Balance Row
            row++;
            ws.Cells[row, 1, row, 4].Merge = true;
            ws.Cells[row, 1].Value = "Opening balance brought forward";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 7].Value = data.OpeningBalanceFormatted;
            ws.Cells[row, 7].Style.Font.Bold = true;
            ws.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            using (var rng = ws.Cells[row, 1, row, 7])
            {
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(241, 245, 242));
            }

            // Transaction Data Rows
            foreach (var item in data.Items)
            {
                row++;
                ws.Cells[row, 1].Value = item.DateFormatted;
                ws.Cells[row, 2].Value = item.VoucherNo;
                ws.Cells[row, 3].Value = item.VoucherType ?? "–";
                ws.Cells[row, 4].Value = item.Narration;
                ws.Cells[row, 5].Value = item.Debit > 0 ? item.Debit.ToString("N2") : "";
                ws.Cells[row, 6].Value = item.Credit > 0 ? item.Credit.ToString("N2") : "";
                ws.Cells[row, 7].Value = item.RunningBalanceFormatted;

                ws.Cells[row, 5, row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            // Total / Closing Row
            row++;
            ws.Cells[row, 1, row, 4].Merge = true;
            ws.Cells[row, 1].Value = "Total / Closing Balance";
            ws.Cells[row, 5].Value = data.TotalDebit.ToString("N2");
            ws.Cells[row, 6].Value = data.TotalCredit.ToString("N2");
            ws.Cells[row, 7].Value = data.ClosingBalanceFormatted;

            using (var rng = ws.Cells[row, 1, row, 7])
            {
                rng.Style.Font.Bold = true;
                rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(241, 245, 242));
                rng.Style.Border.Top.Style = ExcelBorderStyle.Medium;
                rng.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
            }
            ws.Cells[row, 5, row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        public async Task<byte[]> GeneratePdfAsync(GeneralLedgerReportFilterVM filter)
        {
            var report = await GetLedgerReportAsync(filter);
            var doc = new GeneralLedgerPdf(report);
            return doc.GeneratePdf();
        }

        private static string FormatDrCr(decimal amount)
        {
            decimal rounded = Math.Round(amount, 2);
            if (Math.Abs(rounded) < 0.005m)
                return "0.00";

            if (rounded > 0)
                return rounded.ToString("N2") + " Dr";

            return Math.Abs(rounded).ToString("N2") + " Cr";
        }
    }
}
