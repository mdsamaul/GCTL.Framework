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
    public class DashboardAccountsService : IDashboardAccountsService
    {
        private readonly string _connectionString;

        public DashboardAccountsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
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

            return result;
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
    }
}
