using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels.Dashboard;
using GCTL.Core.ViewModels.Finance.BalanceSheet;
using GCTL.Service.Finance.BalanceSheet;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.Dashboard
{
    public class DashboardAccountsService : IDashboardAccountsService
    {
        private readonly string _connectionString;
        private readonly IBalanceSheetService _balanceSheetService;

        public DashboardAccountsService(IConfiguration configuration, IBalanceSheetService balanceSheetService)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
            _balanceSheetService = balanceSheetService;
        }

        public async Task<DashboardAccountsDTO> GetAccountsDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardAccountsDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_AccountsSummary",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardAccountsKPIs>()) ?? new DashboardAccountsKPIs();

            // 2. Account Compositions
            result.AccountCompositions = (await multi.ReadAsync<DashboardAccountsBalanceComp>()).ToList();

            // 3. Monthly Cash Flow
            result.MonthlyCashFlows = (await multi.ReadAsync<DashboardAccountsMonthlyCashFlow>()).ToList();

            // 4. Accounts List
            result.AccountsList = (await multi.ReadAsync<DashboardCashBankAccountRow>()).ToList();

            try
            {
                result.BalanceSheet = await GetBalanceSheetDataAsync(filter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving balance sheet in accounts: {ex.Message}");
            }

            return result;
        }

        public async Task<DashboardBalanceSheetDTO> GetBalanceSheetDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardBalanceSheetDTO();

            var bsFilter = new BalanceSheetFilterVM
            {
                BranchCode = filter.BranchCode ?? string.Empty,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate
            };

            var bsReport = await _balanceSheetService.GetBalanceSheetAsync(bsFilter);
            if (bsReport == null) return result;

            var assets = bsReport.AssetsItems ?? new List<BalanceSheetItemVM>();
            var eqLiab = bsReport.EquitiesAndLiabilitiesItems ?? new List<BalanceSheetItemVM>();

            // Select Level 3 items if available, or Level 2
            var assetsItems = assets.Where(x => x.Level == 3).ToList();
            if (!assetsItems.Any())
            {
                assetsItems = assets.Where(x => x.Level == 2).ToList();
            }

            var eqLiabItems = eqLiab.Where(x => x.Level == 3 && x.Section != "Surplus").ToList();
            if (!eqLiabItems.Any())
            {
                eqLiabItems = eqLiab.Where(x => x.Level == 2 && x.Section != "Surplus").ToList();
            }

            decimal nca = assetsItems.Where(x => (x.Code ?? "").StartsWith("101")).Sum(x => x.Amount);
            decimal ca = assetsItems.Where(x => (x.Code ?? "").StartsWith("102")).Sum(x => x.Amount);
            if (nca == 0 && ca == 0)
            {
                nca = assets.Where(x => x.Level == 2 && (x.Code ?? "").StartsWith("101")).Sum(x => x.Amount);
                ca = assets.Where(x => x.Level == 2 && (x.Code ?? "").StartsWith("102")).Sum(x => x.Amount);
            }
            decimal totalAssets = bsReport.TotalAssets != 0 ? bsReport.TotalAssets : (nca + ca);

            decimal profit = bsReport.CurrentPeriodSurplus;
            decimal eqBase = eqLiabItems.Where(x => (x.Code ?? "").StartsWith("201")).Sum(x => x.Amount);
            if (eqBase == 0)
            {
                eqBase = eqLiab.Where(x => x.Level == 2 && (x.Code ?? "").StartsWith("201")).Sum(x => x.Amount);
            }
            decimal totalEq = eqBase + profit;

            decimal ncl = eqLiabItems.Where(x => (x.Code ?? "").StartsWith("202")).Sum(x => x.Amount);
            if (ncl == 0)
            {
                ncl = eqLiab.Where(x => x.Level == 2 && (x.Code ?? "").StartsWith("202")).Sum(x => x.Amount);
            }

            decimal cl = eqLiabItems.Where(x => (x.Code ?? "").StartsWith("203")).Sum(x => x.Amount);
            if (cl == 0)
            {
                cl = eqLiab.Where(x => x.Level == 2 && (x.Code ?? "").StartsWith("203")).Sum(x => x.Amount);
            }

            decimal totalLiab = ncl + cl;
            decimal workingCapital = ca - cl;
            string currentRatio = cl != 0 ? Math.Round(ca / cl, 2).ToString("0.00") + " : 1" : "–";

            result.KPIs = new DashboardBalanceSheetKPIs
            {
                TotalAssets = totalAssets,
                NonCurrentAssets = nca,
                CurrentAssets = ca,
                TotalLiabilities = totalLiab,
                NonCurrentLiabilities = ncl,
                CurrentLiabilities = cl,
                TotalEquity = totalEq,
                ProfitOrLoss = profit,
                WorkingCapital = workingCapital,
                CurrentRatio = currentRatio
            };

            decimal P(decimal v) => totalAssets != 0 ? Math.Round((v / totalAssets) * 100m, 2) : 0m;

            // Build Rows matching CNF_Dashboard.html
            var rows = new List<DashboardBalanceSheetRowDTO>();

            // 1. Assets
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "ASSETS", Kind = "head" });
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Non-Current Assets", Kind = "head" });
            foreach (var item in assetsItems.Where(x => (x.Code ?? "").StartsWith("101") && x.Amount != 0))
            {
                rows.Add(new DashboardBalanceSheetRowDTO
                {
                    Label = $"{item.Code} – {item.Name}",
                    Amt = item.Amount,
                    Pct = P(item.Amount),
                    Kind = "item"
                });
            }
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Total Non-Current Assets", Amt = nca, Pct = P(nca), Kind = "sub" });

            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Current Assets", Kind = "head" });
            foreach (var item in assetsItems.Where(x => (x.Code ?? "").StartsWith("102") && x.Amount != 0))
            {
                rows.Add(new DashboardBalanceSheetRowDTO
                {
                    Label = $"{item.Code} – {item.Name}",
                    Amt = item.Amount,
                    Pct = P(item.Amount),
                    Kind = "item"
                });
            }
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Total Current Assets", Amt = ca, Pct = P(ca), Kind = "sub" });
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "TOTAL ASSETS", Amt = totalAssets, Pct = P(totalAssets), Kind = "total" });

            // 2. Equity & Liabilities
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "EQUITY & LIABILITIES", Kind = "head" });
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Equity", Kind = "head" });
            foreach (var item in eqLiabItems.Where(x => (x.Code ?? "").StartsWith("201") && x.Amount != 0))
            {
                rows.Add(new DashboardBalanceSheetRowDTO
                {
                    Label = $"{item.Code} – {item.Name}",
                    Amt = item.Amount,
                    Pct = P(item.Amount),
                    Kind = "item"
                });
            }
            if (profit != 0)
            {
                rows.Add(new DashboardBalanceSheetRowDTO
                {
                    Label = "Current Period Profit / (Loss)",
                    Amt = profit,
                    Pct = P(profit),
                    Kind = "item"
                });
            }
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Total Equity", Amt = totalEq, Pct = P(totalEq), Kind = "sub" });

            if (ncl != 0 || eqLiabItems.Any(x => (x.Code ?? "").StartsWith("202")))
            {
                rows.Add(new DashboardBalanceSheetRowDTO { Label = "Non-Current Liabilities", Kind = "head" });
                foreach (var item in eqLiabItems.Where(x => (x.Code ?? "").StartsWith("202") && x.Amount != 0))
                {
                    rows.Add(new DashboardBalanceSheetRowDTO
                    {
                        Label = $"{item.Code} – {item.Name}",
                        Amt = item.Amount,
                        Pct = P(item.Amount),
                        Kind = "item"
                    });
                }
                rows.Add(new DashboardBalanceSheetRowDTO { Label = "Total Non-Current Liabilities", Amt = ncl, Pct = P(ncl), Kind = "sub" });
            }

            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Current Liabilities", Kind = "head" });
            foreach (var item in eqLiabItems.Where(x => (x.Code ?? "").StartsWith("203") && x.Amount != 0))
            {
                rows.Add(new DashboardBalanceSheetRowDTO
                {
                    Label = $"{item.Code} – {item.Name}",
                    Amt = item.Amount,
                    Pct = P(item.Amount),
                    Kind = "item"
                });
            }
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "Total Current Liabilities", Amt = cl, Pct = P(cl), Kind = "sub" });
            rows.Add(new DashboardBalanceSheetRowDTO { Label = "TOTAL EQUITY & LIABILITIES", Amt = totalEq + totalLiab, Pct = P(totalEq + totalLiab), Kind = "total" });

            result.Rows = rows;

            // 3. Charts Data
            result.AssetsComposition = assetsItems
                .Where(x => Math.Abs(x.Amount) > 0)
                .Select(x => new DashboardChartSliceDTO { Label = x.Name, Amount = Math.Abs(x.Amount) })
                .ToList();

            var leParts = new List<DashboardChartSliceDTO>();
            if (totalEq > 0)
            {
                leParts.Add(new DashboardChartSliceDTO { Label = "Equity (incl. profit)", Amount = totalEq });
            }
            if (ncl > 0)
            {
                leParts.Add(new DashboardChartSliceDTO { Label = "Non-current liabilities", Amount = ncl });
            }
            foreach (var item in eqLiabItems.Where(x => (x.Code ?? "").StartsWith("203") && x.Amount > 0))
            {
                leParts.Add(new DashboardChartSliceDTO { Label = item.Name, Amount = item.Amount });
            }
            result.EquityLiabilitiesComposition = leParts;

            return result;
        }
    }
}
