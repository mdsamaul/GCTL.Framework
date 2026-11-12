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
    }
}
