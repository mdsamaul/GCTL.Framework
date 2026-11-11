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
    public class DashboardJobReconcileService : IDashboardJobReconcileService
    {
        private readonly string _connectionString;

        public DashboardJobReconcileService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("connection")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<DashboardJobReconcileDTO> GetJobReconcileDataAsync(DashboardFilterDTO filter)
        {
            var result = new DashboardJobReconcileDTO();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@BranchCode", filter.BranchCode ?? string.Empty, DbType.String);
            parameters.Add("@ShipmentMode", filter.ShipmentMode ?? string.Empty, DbType.String);
            parameters.Add("@Port", filter.Port ?? string.Empty, DbType.String);
            parameters.Add("@CustomerID", filter.CustomerID ?? string.Empty, DbType.String);

            using var multi = await conn.QueryMultipleAsync(
                "dbo.SP_Dashboard_JobReconcile",
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60);

            // 1. KPIs & Minis
            result.KPIs = (await multi.ReadFirstOrDefaultAsync<DashboardJobReconcileKPIs>()) ?? new DashboardJobReconcileKPIs();

            // 2. Top Expense Heads
            result.TopExpenseHeads = (await multi.ReadAsync<DashboardJobReconcileExpenseHead>()).ToList();

            // 3. GP% Distribution
            result.GPBrackets = (await multi.ReadFirstOrDefaultAsync<DashboardJobReconcileGPBracket>()) ?? new DashboardJobReconcileGPBracket();

            // 4. Customer-wise Revenue, Expense & Profit
            result.TopCustomers = (await multi.ReadAsync<DashboardJobReconcileCustomer>()).ToList();

            // 5. Main Reconcile Rows
            result.ReconcileList = (await multi.ReadAsync<DashboardJobReconcileRow>()).ToList();

            return result;
        }
    }
}
