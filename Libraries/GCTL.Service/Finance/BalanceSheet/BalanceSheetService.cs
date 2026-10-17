using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public BalanceSheetService(GCTL_ERP_DBContext context)
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

        public async Task<BalanceSheetReportVM> GetBalanceSheetAsync(BalanceSheetFilterVM filter)
        {
            var report = new BalanceSheetReportVM();
            filter ??= new BalanceSheetFilterVM();

            // 1. Date Defaults
            var now = DateTime.Today;
            report.FromDate = filter.FromDate ?? new DateTime(now.Year, now.Month, 1);
            report.ToDate = filter.ToDate ?? new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            var toDateEndOfDay = report.ToDate.Date.AddDays(1).AddTicks(-1);

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

            // 3. Opening Balances
            var obQuery = _context.AccCompanyOpeningBalances
                .Where(ob => ob.DeletedAt == null);

            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                obQuery = obQuery.Where(ob => ob.MainCompanyCode == filter.CompanyCode || ob.CompanyCode == filter.CompanyCode);
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                obQuery = obQuery.Where(ob => ob.BranchCode == filter.BranchCode);
            }

            var obList = await obQuery.ToListAsync();
            var netOpeningByCode = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var ob in obList)
            {
                var code = ob.GeneralLedgerCodeNo?.Trim();
                if (string.IsNullOrEmpty(code)) continue;

                decimal amt = ob.OpeningBalance ?? 0m;
                var tr = (ob.TrType ?? "").Trim().ToLowerInvariant();

                decimal netDr = (tr == "credit" || tr == "cr" || tr == "c") ? -amt : amt;

                if (netOpeningByCode.TryGetValue(code, out var cur))
                    netOpeningByCode[code] = cur + netDr;
                else
                    netOpeningByCode[code] = netDr;
            }

            // 4. Voucher Details up to ToDate
            var voucherQuery = from d in _context.AccVoucherEntryDetails
                               join v in _context.AccVoucherEntries on d.VoucherEntryCodeNo equals v.VoucherEntryCodeNo
                               where v.VoucherDate <= toDateEndOfDay &&
                                     (v.IsActive == null || v.IsActive == "1") &&
                                     (v.IsApproved == "1" || v.IsApproved == "Y" || v.IsApproved == "Posted" || string.IsNullOrEmpty(v.IsApproved))
                               select new { d.AccCode, d.DebitAmount, d.CreditAmount, v.CompanyCode, v.MainCompanyCode, v.BranchCode };

            if (!string.IsNullOrEmpty(filter.CompanyCode) && filter.CompanyCode != "0" && filter.CompanyCode != "All")
            {
                voucherQuery = voucherQuery.Where(x => x.MainCompanyCode == filter.CompanyCode || x.CompanyCode == filter.CompanyCode);
            }

            if (!string.IsNullOrEmpty(filter.BranchCode) && filter.BranchCode != "All")
            {
                voucherQuery = voucherQuery.Where(x => x.BranchCode == filter.BranchCode);
            }

            var periodTotals = await voucherQuery
                .GroupBy(d => d.AccCode)
                .Select(g => new
                {
                    AccCode = g.Key,
                    Debit = g.Sum(x => x.DebitAmount ?? 0m),
                    Credit = g.Sum(x => x.CreditAmount ?? 0m)
                })
                .ToListAsync();

            var vchByCode = periodTotals.ToDictionary(
                x => x.AccCode?.Trim() ?? string.Empty,
                x => (Debit: x.Debit, Credit: x.Credit),
                StringComparer.OrdinalIgnoreCase);

            // 5. Load All Active General Ledgers
            var coaAccounts = await _context.AccGeneralLedgers
                .Where(g => g.IsActive == null || g.IsActive == "1")
                .OrderBy(g => g.GeneralLedgerCodeNo)
                .ToListAsync();

            var controls = new Dictionary<string, string>();
            var subControls = new Dictionary<string, string>();
            try { controls = await _context.AccControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.ControlLedgerCodeNo, x => x.ControlLedgerName); } catch { }
            try { subControls = await _context.AccSubControlLedgers.AsNoTracking().ToDictionaryAsync(x => x.SubControlLedgerCodeNo, x => x.SubControlLedgerName); } catch { }

            // Helper to get closing Net Dr
            decimal GetClosingNetDr(string code)
            {
                decimal net = 0m;
                if (netOpeningByCode.TryGetValue(code, out var op)) net += op;
                if (vchByCode.TryGetValue(code, out var v)) net += (v.Debit - v.Credit);
                return net;
            }

            // 6. Build Assets Section (Group 1)
            var assetAccounts = coaAccounts.Where(g => g.GroupLedgerCodeNo != null && g.GroupLedgerCodeNo.StartsWith("1")).ToList();
            var (assetItems, totalAssets) = BuildTree(assetAccounts, GetClosingNetDr, isAsset: true, controls, subControls);
            report.AssetsItems = assetItems;
            report.TotalAssetsAmount = totalAssets;
            report.TotalAssets = totalAssets;

            // 7. Build Equities & Liabilities Section (Group 2)
            var liabEqAccounts = coaAccounts.Where(g => g.GroupLedgerCodeNo != null && g.GroupLedgerCodeNo.StartsWith("2")).ToList();
            var (liabEqItems, totalLiabEqAccounts) = BuildTree(liabEqAccounts, GetClosingNetDr, isAsset: false, controls, subControls);

            // 8. Calculate Current Period Surplus / (Deficit) from Groups 3 and 4
            var pnlAccounts = coaAccounts.Where(g => g.GroupLedgerCodeNo != null && (g.GroupLedgerCodeNo.StartsWith("3") || g.GroupLedgerCodeNo.StartsWith("4"))).ToList();
            decimal surplus = 0m;
            foreach (var acc in pnlAccounts)
            {
                var code = acc.GeneralLedgerCodeNo?.Trim() ?? string.Empty;
                surplus -= GetClosingNetDr(code);
            }
            report.CurrentPeriodSurplus = surplus;

            // Add Surplus row inside Equities & Liabilities list
            liabEqItems.Add(new BalanceSheetItemVM
            {
                Code = string.Empty,
                Name = "Current period surplus / (deficit)",
                Level = 3,
                Section = "Surplus",
                Amount = surplus,
                AmountFormatted = FormatCurrency(surplus),
                IsHighlighted = true
            });

            decimal totalEquitiesAndLiabilities = totalLiabEqAccounts + surplus;
            report.EquitiesAndLiabilitiesItems = liabEqItems;
            report.TotalEquitiesAndLiabilitiesAmount = totalEquitiesAndLiabilities;

            // 9. Separate Equity vs Liabilities for KPI Summary Cards
            decimal equityWithoutSurplus = 0m;
            foreach (var acc in liabEqAccounts)
            {
                var ctrlCode = acc.ControlLedgerCodeNo ?? string.Empty;
                var ctrlName = controls.TryGetValue(ctrlCode, out var cn) ? cn : string.Empty;
                if (ctrlCode.StartsWith("201") || ctrlName.IndexOf("Equity", StringComparison.OrdinalIgnoreCase) >= 0 || ctrlName.IndexOf("Capital", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    equityWithoutSurplus += -GetClosingNetDr(acc.GeneralLedgerCodeNo?.Trim() ?? string.Empty);
                }
            }

            decimal totalEquity = equityWithoutSurplus + surplus;
            decimal totalLiabilities = totalEquitiesAndLiabilities - totalEquity;

            report.TotalEquity = totalEquity;
            report.TotalLiabilities = totalLiabilities;

            return report;
        }

        private static (List<BalanceSheetItemVM> items, decimal total) BuildTree(
            List<AccGeneralLedger> accounts,
            Func<string, decimal> getClosingNetDr,
            bool isAsset,
            Dictionary<string, string> controls,
            Dictionary<string, string> subControls)
        {
            var result = new List<BalanceSheetItemVM>();
            decimal sectionTotal = 0m;

            // Group by Control Ledger (Level 2)
            var controlGroups = accounts
                .GroupBy(a => new
                {
                    Code = a.ControlLedgerCodeNo ?? string.Empty,
                    Name = controls.TryGetValue(a.ControlLedgerCodeNo ?? string.Empty, out var cn) ? cn : "General"
                })
                .OrderBy(g => g.Key.Code);

            foreach (var cGroup in controlGroups)
            {
                var controlItems = new List<BalanceSheetItemVM>();
                decimal controlTotal = 0m;

                // Group by SubControl Ledger (Level 3)
                var subControlGroups = cGroup
                    .GroupBy(a => new
                    {
                        Code = a.SubControlLedgerCodeNo ?? string.Empty,
                        Name = subControls.TryGetValue(a.SubControlLedgerCodeNo ?? string.Empty, out var scn) ? scn : "General"
                    })
                    .OrderBy(g => g.Key.Code);

                foreach (var scGroup in subControlGroups)
                {
                    var subControlItems = new List<BalanceSheetItemVM>();
                    decimal subControlTotal = 0m;

                    // Leaf Accounts (Level 5)
                    foreach (var acc in scGroup.OrderBy(a => a.GeneralLedgerCodeNo))
                    {
                        string code = acc.GeneralLedgerCodeNo?.Trim() ?? string.Empty;
                        decimal netDr = getClosingNetDr(code);

                        decimal amount = isAsset ? netDr : -netDr;

                        if (Math.Abs(amount) >= 0.005m)
                        {
                            subControlTotal += amount;
                            subControlItems.Add(new BalanceSheetItemVM
                            {
                                Code = acc.GeneralLedgerCodeNo,
                                Name = acc.GeneralLedgerName,
                                Level = 5,
                                Section = isAsset ? "Assets" : "Equities & Liabilities",
                                Amount = amount,
                                AmountFormatted = FormatCurrency(amount)
                            });
                        }
                    }

                    if (subControlItems.Count > 0)
                    {
                        controlTotal += subControlTotal;
                        controlItems.Add(new BalanceSheetItemVM
                        {
                            Code = scGroup.Key.Code,
                            Name = scGroup.Key.Name,
                            Level = 3,
                            Section = isAsset ? "Assets" : "Equities & Liabilities",
                            Amount = subControlTotal,
                            AmountFormatted = FormatCurrency(subControlTotal)
                        });
                        controlItems.AddRange(subControlItems);
                    }
                }

                if (controlItems.Count > 0)
                {
                    sectionTotal += controlTotal;
                    result.Add(new BalanceSheetItemVM
                    {
                        Code = cGroup.Key.Code,
                        Name = cGroup.Key.Name,
                        Level = 2,
                        Section = isAsset ? "Assets" : "Equities & Liabilities",
                        Amount = controlTotal,
                        AmountFormatted = FormatCurrency(controlTotal)
                    });
                    result.AddRange(controlItems);
                }
            }

            return (result, sectionTotal);
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
