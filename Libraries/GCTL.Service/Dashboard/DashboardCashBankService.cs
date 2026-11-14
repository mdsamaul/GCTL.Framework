using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GCTL.Core.ViewModels.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GCTL.Service.Dashboard
{
    public class DashboardCashBankService : IDashboardCashBankService
    {
        private readonly string _connectionString;

        public DashboardCashBankService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardCashBankSummaryDTO> GetCashBankSummaryAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardCashBankSummaryDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_CashBankSummary",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. 6 KPIs & 5 Minis
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardCashBankKPIs>()) ?? new DashboardCashBankKPIs();

            // 2. Composition (Doughnut)
            result.Compositions = (await multi.ReadAsync<DashboardCashBankComposition>()).ToList();

            // 3. Cash & Bank Monthly Trend
            result.CashBankTrends = (await multi.ReadAsync<DashboardCashBankMonthlyTrend>()).ToList();

            // 4. Income vs Expense Monthly Trend
            result.IncomeExpenseTrends = (await multi.ReadAsync<DashboardCashBankIncomeExpenseTrend>()).ToList();

            // 5. Accounts Detail List (Grid a_g)
            result.AccountsList = (await multi.ReadAsync<DashboardCashBankRow>()).ToList();

            return result;
        }
    }
}
